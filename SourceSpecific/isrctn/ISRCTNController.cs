using MDR_Downloader.Helpers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Serialization;

namespace MDR_Downloader.isrctn;

class ISRCTN_Controller : IDLController
{
    private readonly IMonDataLayer _monDataLayer;
    private readonly ILoggingHelper _loggingHelper;    
    private readonly JsonSerializerOptions? _json_options;
    private readonly string _base_url;

    public ISRCTN_Controller(IMonDataLayer monDataLayer, ILoggingHelper loggingHelper)
    {
        _monDataLayer = monDataLayer;
        _loggingHelper = loggingHelper;
        
        _json_options = new()
        {
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        _base_url = "https://www.isrctn.com/api/query/format/default?q=";
    }

    // ISRCTN data obtained from an API.
    // Normally (t = 111) this is by identifying those studies edited since a cut-off date, 
    // usually from the previous week (i.e., the date of the most recent download).
    // Alternatively (t = 115) it is possible to download all records that were last edited
    // between two dates. Doing the latter in batches allows all ISRCTN records to be
    // re-downloaded, if and when necessary.

    // There does not appear to be a way to rank or order results and select
    // from within a returned set. If the number of available records for a selected 
    // period is > 100 records the call is broken down calls for individual days. 
    // If a day returns > 100 the limit must be raised to the amount concerned.
    // Note also that the 'days_ago' check is not able to be implemented
    // in a batch download process (unless the system switches to
    // downloading one study at a time).

    public async Task<DownloadResult> ObtainDataFromSourceAsync(Options opts, Source source)
    {
        DownloadResult res = new();

        string? file_base = source.local_folder;
        _loggingHelper.LogLine($"file_base variable: {file_base}");
        if (file_base is null)
        {
            _loggingHelper.LogError("Null value passed for local folder value for this source");
            return res;   // return zero result
        }
        int t = opts.FetchTypeId;
        if (opts.CutoffDate is not null && opts.dl_id is not null)
        {
            if (t == 111)
            {
                return await DownloadRevisedRecords(file_base, opts, source.id);
            }
            if (t == 115 && opts.EndDate is not null)
            {
                return await DownloadRecordsBetweenDates(file_base, opts, source.id);
            }
        }
        _loggingHelper.LogError("Invalid parameters passed to download controller - unable to proceed");
        return new DownloadResult();   // return zero result

    }

    // DownloadRevisedRecords returns all records that have been revised on or since 
    // the cutoff date, including today's records. This means that successive calls will
    // often overlap on the day of the call. This is by design as the call day's records will
    // not necessarily be complete when the call is made.

    public async Task<DownloadResult> DownloadRevisedRecords(string file_base, Options opts, int source_id)
    {
        DownloadResult res = new();
        ScrapingHelpers ch = new(_loggingHelper); 

        // initially get a single study to indicate total number to be downloaded.

        DateTime cut_off_date = (DateTime)opts.CutoffDate!;
        int dl_id = (int)opts.dl_id!;
        string url = GetUrl(1, cut_off_date);
        string? responseBodyAsString =
            await ch.GetAPIResponseWithRetriesAsync(url, 1000, cut_off_date.ToString("dd/MM/yyyy"));
        allTrials? initial_result = DeserializeXML<allTrials?>(responseBodyAsString, _loggingHelper);

        if (initial_result is not null)
        {
            int record_num = initial_result.totalCount;
            if (record_num > 0)
            {
                if (record_num <= 100)
                {
                    // Do a single call but with an increased limit.

                    url = GetUrl(record_num, cut_off_date);
                    responseBodyAsString =
                        await ch.GetAPIResponseWithRetriesAsync(url, 1000, cut_off_date.ToString("dd/MM/yyyy"));
                    if (responseBodyAsString is not null)
                    {
                        DownloadResult batch_res = await DownloadBatch(responseBodyAsString, file_base, dl_id);
                        res.num_checked += batch_res.num_checked;
                        res.num_downloaded += batch_res.num_downloaded;
                        res.num_added += batch_res.num_added;
                    }
                }
                else
                { 
                    // Split the calls to a per day basis.

                    DateTime date_to_check = cut_off_date;
                    while (date_to_check.Date <= DateTime.Now.Date)
                    {
                        DownloadResult day_res = await DownloadStudiesFromSingleDay(date_to_check, file_base, dl_id);
                        res.num_checked += day_res.num_checked;
                        res.num_downloaded += day_res.num_downloaded;
                        res.num_added += day_res.num_added;

                        string feedback = $"{day_res.num_downloaded} studies downloaded, for {date_to_check.ToShortDateString()}.";
                        feedback += $" Total downloaded: {res.num_downloaded}";
                        _loggingHelper.LogLine(feedback);

                        date_to_check = date_to_check.AddDays(1);
                        Thread.Sleep(800);  // Add a pause between calls.
                    }
                }
            }
        }

        return res;
    }

    // Downloads all studies last edited from the first date (inclusively) and the 
    // last date (exclusively) - i.e. GE date 1 and LT date 2.
    // By default the download is done in batches of 4 days. If the end date is included
    // in a batch, the batch is made up to the end date.

    public async Task<DownloadResult> DownloadRecordsBetweenDates(string file_base, Options opts, int source_id)
    {
        DownloadResult res = new();
        ScrapingHelpers ch = new(_loggingHelper);
        DateTime start_date = (DateTime)opts.CutoffDate!;
        DateTime end_date = (DateTime)opts.EndDate!;
        int dl_id = (int)opts.dl_id!;

        // If the start date is earlier than 10/11/2005 it is made into 10/11/2005,
        // the earliest date in the ISRCTN system for 'date last edited'.
        // If the end date is later than today it is made today.
        // Dates are transformed into number of days post 01/01/2005.
        // Day numbers are then used to loop through the requested period.

        start_date = start_date < new DateTime(2005, 11, 10) ? new DateTime(2005, 11, 10) : start_date;
        end_date = end_date > DateTime.Now ? DateTime.Now.Date : end_date;

        DateTime baseDate = new DateTime(2005, 1, 1);
        int startDay = (start_date - baseDate).Days;
        int endDay = (end_date - baseDate).Days;

        for (int d = startDay; d < endDay; d += 4)
        {
            // The 4 days being considered are the start date
            // and the following three days. 

            DateTime date_GE = baseDate.AddDays(d);
            DateTime date_LT = date_GE.AddDays(4);

            // Must end on the correct day, therefore
            // check and truncate end of period if necessary.

            date_LT = date_LT > end_date ? end_date : date_LT;

            // Initial call to get number of studies in this period

            string url = GetUrl(1, date_GE, date_LT);
            string? responseBodyAsString =
                await ch.GetAPIResponseWithRetriesAsync(url, 1000, 
                    date_GE.ToString("dd/MM/yyyy") + " to " + date_LT.ToString("dd/MM/yyyy"));
            allTrials? result = DeserializeXML<allTrials?>(responseBodyAsString, _loggingHelper);
            if (result is not null)
            {
                int record_num = result.totalCount;
                if (record_num > 0)
                {
                    if (record_num <= 100)
                    {
                        // Do a single call but with the increased limit.

                        url = GetUrl(record_num, date_GE, date_LT);
                        responseBodyAsString =
                            await ch.GetAPIResponseWithRetriesAsync(url, 1000, 
                                date_GE.ToString("dd/MM/yyyy") + " to " + date_LT.ToString("dd/MM/yyyy"));
                        if (responseBodyAsString is not null)
                        {
                            DownloadResult batch_res = await DownloadBatch(responseBodyAsString, file_base, dl_id);
                            res.num_checked += batch_res.num_checked;
                            res.num_downloaded += batch_res.num_downloaded;
                            res.num_added += batch_res.num_added;

                            string feedback = $"{batch_res.num_downloaded} studies downloaded, ";
                            feedback += $"with last edited GE { date_GE.ToShortDateString()} and LT { date_LT.ToShortDateString()}. ";
                            feedback += $"Total downloaded: {res.num_downloaded}";
                            _loggingHelper.LogLine(feedback);
                            Thread.Sleep(800);  // Add a pause between calls.
                        }
                    }
                    else
                    { 
                        // Split the calls to a per day basis.

                        DateTime date_to_check = date_GE;
                        while (date_to_check.Date < date_LT)
                        {
                            DownloadResult day_res = await DownloadStudiesFromSingleDay(date_to_check, file_base, dl_id);
                            res.num_checked += day_res.num_checked;
                            res.num_downloaded += day_res.num_downloaded;
                            res.num_added += day_res.num_added;

                            string feedback = $"{day_res.num_downloaded} studies downloaded, for {date_to_check.ToShortDateString()}.";
                            feedback += $" Total downloaded: {res.num_downloaded}";
                            _loggingHelper.LogLine(feedback);
                            date_to_check = date_to_check.AddDays(1);
                            Thread.Sleep(800);  // Add a pause between calls.
                        }
                    }
                }
            }
        }

        return res;
    }

    // Downloads the study records where day = last edited is a single designated day
    // Called from both DownloadRevisedRecords and DownloadRecordsBetweenDates when amounts for
    // a period exceed 100 and the system switches to getting records one day at a time.
    // First gets a single record to calculate total amount to be retrieved, and
    // then sets the limit in a following call to retrieve all records.

    private async Task<DownloadResult> DownloadStudiesFromSingleDay(DateTime date_to_check, string file_base, int dl_id)
    {
        DownloadResult res = new();
        ScrapingHelpers ch = new(_loggingHelper);
        DateTime next_day_date = date_to_check.AddDays(1);

        string url = GetUrl(1, date_to_check, next_day_date);
        string? responseBodyAsString =
            await ch.GetAPIResponseWithRetriesAsync(url, 1000, date_to_check.ToString("dd/MM/yyyy"));
        allTrials? day_result = DeserializeXML<allTrials?>(responseBodyAsString, _loggingHelper);

        if (day_result is not null)
        {
            int day_record_num = day_result.totalCount;
            if (day_record_num > 0) 
            {
                Thread.Sleep(300);
                url = GetUrl(day_record_num, date_to_check, next_day_date);
                responseBodyAsString = await ch.GetAPIResponseWithRetriesAsync(url, 1000, 
                                                   date_to_check.ToString("dd/MM/yyyy"));
                if (responseBodyAsString is not null)
                {
                    DownloadResult batch_res = await DownloadBatch(responseBodyAsString, file_base, dl_id);
                    res.num_checked += batch_res.num_checked;
                    res.num_downloaded += batch_res.num_downloaded;
                    res.num_added += batch_res.num_added;
                }
            }
        }
        return res;
    }

    // Batch download, called by other functions whenever a set of study records has been obtained, (as a string
    // from an API call). The string first needs deserializing to the response object, and then each individual 
    // study needs to be transformed into the json file model, and saved as a json file in the appropriate folder.

    private async Task<DownloadResult> DownloadBatch(string responseBodyAsString, string file_base, int dl_id)
    {
        DownloadResult res = new();
        allTrials? result = DeserializeXML<allTrials?>(responseBodyAsString, _loggingHelper);
        if(result is null)
        {
            _loggingHelper.LogError("Error de-serialising " + responseBodyAsString);
            return res;
        }

        ISRCTN_Processor isrctn_processor = new();
        int number_returned = result.totalCount;
        if (number_returned > 0 && result.fullTrials?.Any() is true) 
        { 
            foreach (FullTrial f in result.fullTrials)
            {
                res.num_checked++;
                Study? s = isrctn_processor.GetFullDetails(f, _loggingHelper);
                if (s is not null)
                {
                    string full_path = await WriteOutFile(s, s.sd_sid, file_base);
                    if (full_path != "error")
                    {
                        string remote_url = "https://www.isrctn.com/" + s.sd_sid;
                        DateTime? last_updated = s.lastUpdated?.FetchDateTimeFromISO();
                        bool added = _monDataLayer.UpdateStudyLog(s.sd_sid, remote_url, dl_id,
                                                last_updated, full_path);
                        res.num_downloaded++;
                        if (added) res.num_added++;
                    }
                }
            }
        }
        return res;
    }

    // Writes out the file with the correct name to the correct folder, as indented json.
    // Called from the DownloadBatch function.
    // Returns the full file path as constructed, or an 'error' string if an exception occurred.

    private async Task<string> WriteOutFile(Study s, string sd_sid, string file_base)
    {
        string file_name = sd_sid + ".json";
        string full_path = Path.Combine(file_base, file_name);
        try
        {
            await using FileStream jsonStream = File.Create(full_path);
            await JsonSerializer.SerializeAsync(jsonStream, s, _json_options);
            await jsonStream.DisposeAsync();
            
            if (_monDataLayer.IsTestStudy(sd_sid))
            {
                // write out copy of the file in the test folder
                string test_path = _loggingHelper.TestFilePath;
                string full_test_path = Path.Combine(test_path, file_name);
                await using FileStream jsonStream2 = File.Create(full_test_path);
                await JsonSerializer.SerializeAsync(jsonStream2, s, _json_options);
                await jsonStream2.DisposeAsync();
            }
            return full_path;
        }
        catch (Exception e)
        {
            _loggingHelper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
            return "error";
        }
    }

    // String function that constructs the required URL for the ISRCTN API.

    private string GetUrl(int limit, DateTime startDate, DateTime? endDate = null)
    {
        string start_date_param, id_params;
        if (endDate is null)
        {
            start_date_param = $"{startDate.Year}-{startDate.Month:00}-{startDate.Day:00}";
            id_params = "lastEdited%20GE%20" + start_date_param + "T00:00:00%20";
        }
        else
        {
            DateTime end_date = (DateTime)endDate;
            start_date_param = $"{startDate.Year}-{startDate.Month:00}-{startDate.Day:00}";
            string end_date_param = $"{end_date.Year}-{end_date.Month:00}-{end_date.Day:00}";
            id_params = "lastEdited%20GE%20" + start_date_param + "T00:00:00%20AND%20lastEdited%20LT%20" + end_date_param + "T00:00:00";
        }
        string end_url = $"&limit={limit}";
        return _base_url + id_params + end_url;
    }


    // General XML Deserialize function.

    private T? DeserializeXML<T>(string? inputString, ILoggingHelper logging_helper)
    {
        if (inputString is null)
        {
            return default;
        }

        T? instance;
        try
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using var stringReader = new StringReader(inputString);
            instance = (T?)xmlSerializer.Deserialize(stringReader);
        }
        catch(Exception e)
        {
            string error_heading = "Error when de-serialising ";
            error_heading += inputString.Length >= 750 ? inputString[..750] : inputString;
            logging_helper.LogCodeError(error_heading, e.Message, e.StackTrace);
            return default;
        }
        return instance;
    }
}

