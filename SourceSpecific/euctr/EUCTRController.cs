using MDR_Downloader.Helpers;
using ScrapySharp.Network;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MDR_Downloader.euctr;

class EUCTR_Controller : IDLController
{
    private readonly IMonDataLayer _monDataLayer;
    private readonly ILoggingHelper _loggingHelper;    
    
    private readonly string _baseURL;
    private readonly JsonSerializerOptions? _json_options;
    private readonly EUCTR_Processor _processor;
    private readonly EUCTR_Helper _euctrHelper;
    private int _access_error_num;
    
    public EUCTR_Controller(IMonDataLayer monDataLayer, ILoggingHelper loggingHelper)
    {
        _monDataLayer = monDataLayer;
        _loggingHelper = loggingHelper;
        
        _baseURL = "https://www.clinicaltrialsregister.eu/ctr-search/search?page=";
        _json_options = new()
        {
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        
        _processor = new(loggingHelper);
        _euctrHelper = new(loggingHelper);
    }

    public async Task<DownloadResult> ObtainDataFromSourceAsync(Options opts, Source source)
        
    {
        DownloadResult res = new();
        ScrapingHelpers ch = new(_loggingHelper);
        string? file_base = source.local_folder;

        if (file_base is null)
        {
            _loggingHelper.LogError("Null value passed for local folder value for this source");
            return res;   // return zero result
        }

        // first ensure that the web site is up
        // and get total record numbers and total page numbers
        
        int dl_id = (int)opts.dl_id!;    // will be non-null
        WebPage? initialPage = await ch. GetPageWithRetriesAsync(_baseURL + "1", 500, "page 1");
        if (initialPage is null)
        {
            _loggingHelper.LogError("Unable to open initial summary page (web site may be down), so unable to proceed");
            return res;   // return zero result
        }

        // first get total number of records 
        // Only proceed if that initial task is possible

        int rec_num = _processor.GetListLength(initialPage);
        if (rec_num == 0)
        {
            _loggingHelper.LogError("Unable to capture total record numbers in preliminary set up, so unable to proceed");
            return res;  // return zero result
        }

        int total_summary_pages = rec_num % 20 == 0 ? rec_num / 20 : (rec_num / 20) + 1;

        // if sf_type = 145 only scrape & download files with a download status of 0
        // if type = 146 scrape all records in the designated page range (20 records per page)
        // in both cases ignore records that have been downloaded in recent interval (I, 'skip recent' parameter)
        int start_page, end_page;

        if (opts.FetchTypeId == 145)
        {
            // by default start at the beginning, but can be over-written by StartPage parameter
            
            opts.StartPage ??= 0;
            start_page = (int)opts.StartPage;
            end_page = total_summary_pages;
        }
        else if (opts.FetchTypeId == 146)
        {
            if (opts.StartPage is not null && opts.EndPage is not null)
            {
                start_page = (int)opts.StartPage;
                end_page = opts.EndPage > total_summary_pages ? total_summary_pages : (int)opts.EndPage;
            }
            else
            {
                _loggingHelper.LogError("Valid start and end page numbers not provided for page based download");
                return res;  // return zero result
            }
        }
        else
        {
            _loggingHelper.LogError("Download type requested not in allowed list for EU CTR"); 
            return res;  // return zero result
        }

        res = await LoopThroughDesignatedPagesAsync(opts.FetchTypeId, start_page, end_page, opts.SkipRecentDays, 
                                                    source.id, dl_id, file_base);

        return res;
    }


    private async Task<DownloadResult> LoopThroughDesignatedPagesAsync(int type_id, int start_page, int end_page, 
                               int? days_ago, int source_id, int dl_id, string file_base)
    {
        DownloadResult res = new DownloadResult();
        ScrapingHelpers ch = new(_loggingHelper);

        for (int i = start_page; i <= end_page; i++)
        {
            // if (res.num_downloaded > 2) break; // for testing
            // Go to the summary page indicated by current value of i
            // Each page has up to 20 listed studies. Get a list of their Ids.

            WebPage? summaryPage = await ch.GetPageWithRetriesAsync(_baseURL + i, 600, "summary " + i);
            if (summaryPage is null)
            {
                _loggingHelper.LogError($"Unable to reach summary data page {i} - skipping this page");
                break;
            }

            List<Study_Summary>? summaries = _processor.GetStudyList(summaryPage);
            if (summaries?.Any() != true)
            {
                _loggingHelper.LogError($"Problem in collecting summary data on page {i} - skipping this page");
                break;
            }

            // Calculate how many of the 20 need to be downloaded and mark those that do.
            // No 'last updated' field to check, so have to be selected using much cruder techniques

            int num_to_download = 0;
            foreach (Study_Summary s in summaries)
            {
                bool do_download = false;
                res.num_checked++;
                StudyFileRecord? file_record = _monDataLayer.FetchStudyFileRecord(s.eudract_id, "");
                if (file_record is null)
                {
                    // a new record not yet existing in study source table - must be downloaded.

                    do_download = true;
                }
                else
                {
                    if (type_id == 146 || (type_id == 145 && file_record.download_status == 0))
                    {
                        // download all records for download type 146 (in the designated pages),
                        // but only those with download status 0 for type 145.

                        do_download = true;

                        // However, in either case may have been downloaded in designated recent days,
                        // in which case do not need to download again.

                        if (days_ago is not null)
                        {
                            if (_monDataLayer.Downloaded_recently(s.eudract_id, (int)days_ago))
                            {
                                do_download = false;
                            }
                        }
                    }
                }
                
                s.do_download = do_download;
                if (do_download) num_to_download++;
            }

            // only proceed further if there are any studies to download on this page 
            // For each study that needs to be downloaded...
            // First, get full details into the summaries - transfer those details to
            // a new main EUCTR Study object and then get full protocol details

            if (num_to_download > 0)
            {
                foreach (var s in summaries)
                {
                    if (s.do_download is true)
                    {
                        Euctr_Record? st = _euctrHelper.GetInfoFromSummaryBox(s.details_box!);
                        if (st is null)
                        {
                            _loggingHelper.LogError(
                                $"Problem in obtaining summary details from summary page, for {s.eudract_id}");
                            CheckAccessErrorCount();
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(st.details_url))
                            {
                                _loggingHelper.LogError(
                                    $"Problem in obtaining protocol details url from summary page, for {s.eudract_id}");
                                CheckAccessErrorCount();
                            }
                            else
                            {
                                Thread.Sleep(300);
                                WebPage? detailsPage = await ch.GetPageWithRetriesAsync(st.details_url, 500, st.sd_sid);
                                if (detailsPage is null)
                                {
                                    _loggingHelper.LogError(
                                        $"Problem in navigating to protocol details page for {s.eudract_id}");
                                    CheckAccessErrorCount();
                                }
                                else
                                {
                                    st = _processor.ExtractProtocolDetails(st, detailsPage);

                                    // Then get results details if available

                                    if (st.results_url != null)
                                    {
                                        Thread.Sleep(600);
                                        WebPage? resultsPage = await ch.GetPageAsync(st.results_url);
                                        if (resultsPage is not null)
                                        {
                                            st = _euctrHelper.ExtractResultDetails(st, resultsPage);
                                        }
                                        else
                                        {
                                            _loggingHelper.LogError(
                                                $"Problem in navigating to result details, for {s.eudract_id}");
                                            CheckAccessErrorCount();
                                        }
                                    }

                                    // Write out study record as json.
                                    // Update the source data record, modifying it or adding a new one.

                                    string full_path = await WriteOutFile(st, st.sd_sid, file_base);
                                    if (full_path != "error")
                                    {
                                        string? remote_url = st.details_url;
                                        bool added = _monDataLayer.UpdateStudyLog(s.eudract_id,
                                            remote_url, dl_id, null, full_path);
                                        res.num_downloaded++;
                                        if (added) res.num_added++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            string feedback = $"Page {i} done: {res.num_checked} studies checked, {res.num_downloaded} downloaded";
            _loggingHelper.LogLine(feedback);
        }

        return res;
    }
      
    // Writes out the file with the correct name to the correct folder, as indented json.
    // Called from the DownloadBatch function.
    // Returns the full file path as constructed, or an 'error' string if an exception occurred.

    private async Task<string> WriteOutFile(Euctr_Record s, string sd_sid, string file_base)
    {
        string file_name = "EU " + sd_sid + ".json";
        string full_path = Path.Combine(file_base, file_name);
        try
        {
            await using FileStream jsonStream = File.Create(full_path);
            await JsonSerializer.SerializeAsync(jsonStream, s, _json_options);
            await jsonStream.DisposeAsync();
            return full_path;
        }
        catch (Exception e)
        {
            _loggingHelper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
            return "error";
        }
    }

    private void CheckAccessErrorCount()
    {
        _access_error_num++;
        if (_access_error_num % 5 == 0)
        {
            TimeSpan pause = new TimeSpan(0, 1, 0);   // a 5 minute pause
            Thread.Sleep(pause);
        }
    }

}
