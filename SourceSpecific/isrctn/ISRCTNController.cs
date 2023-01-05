using MDR_Downloader;
using MDR_Downloader.Helpers;
using MDR_Downloader.pubmed;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Serialization;

namespace MDR_Downloader.isrctn;

class ISRCTN_Controller
{
    private readonly LoggingHelper _logging_helper;
    private readonly MonDataLayer _mon_data_layer;

    public ISRCTN_Controller(MonDataLayer mon_data_layer, LoggingHelper logging_helper)
    {
        _logging_helper = logging_helper;
        _mon_data_layer = mon_data_layer;
    }

    // ISRCTN data obtained from an API.
    // Normally (t = 111) this is by identifying those studies edited since a cut-off date, 
    // usually from the previous week (i.e., the date of the most recent download).
    // Alternatively (t = 115) it is possible to download all records that were last edited
    // between two dates. Doing the latter in batches allows all ISRCTN records to be
    // re-downloaded, if and when necessary.

    public async Task<DownloadResult> ObtainDatafromSourceAsync(Options opts, int saf_id, Source source)
    {
        DownloadResult res = new();

        string? file_base = source.local_folder;
        if (file_base is null)
        {
            _logging_helper.LogError("Null value passed for local folder value for this source");
            return res;   // return zero result
        }
        int? days_ago = opts.SkipRecentDays;
        int t = opts.FetchTypeId;

        var json_options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        if (t == 111 && opts.CutoffDate is not null)
        {
            return await DownloadRevisedRecords(file_base, (DateTime)opts.CutoffDate, json_options, source.id, saf_id, days_ago);
        }
        else if (t == 115 && opts.CutoffDate is not null && opts.EndDate is not null)
        {
            return await DownloadRecordsBetweenDates(file_base, (DateTime)opts.CutoffDate, (DateTime)opts.EndDate, json_options, source.id, saf_id, days_ago);
        }
        else
        {
            _logging_helper.LogError("Invalid parameters passed to download controller - unable to proceed");
            return new DownloadResult();   // return zero result
        }
    }

    // Unfortunately there does not appear to be a way to rank or order results and 
    // select from within a returned set. A default limit of 100 records is set in the API 
    // call but if the returned number is greater than that the call must be broken down
    // into individual days 


    // Get the number of records required and set up the loop

    public async Task<DownloadResult> DownloadRevisedRecords(string file_base, DateTime cut_off_date, JsonSerializerOptions json_options, 
                                                             int source_id, int saf_id, int? days_ago)
    {
        DownloadResult res = new();
        ScrapingHelpers ch = new(_logging_helper); 
        ISRCTN_Processor ISRCTN_processor = new();

        string start_url = "https://www.isrctn.com/api/query/format/default?q=";
        string cut_off_date_param = $"{cut_off_date.Year}-{cut_off_date.Month.ToString("00")}-{cut_off_date.Day.ToString("00")}";
        string id_params = "lastEdited%20GE%20" + cut_off_date_param + "T00:00:00%20";

        // initially get the amount of studies to be downloaded

        string end_url = "&limit=1";
        string url = start_url + id_params + end_url;
        string? responseBodyAsString = await ch.GetAPIResponseAsync(url);

        XmlSerializer xSerializer = new(typeof(allTrials));

        int record_num = 0;
        if (responseBodyAsString is not null)
        {
            using (TextReader reader = new StringReader(responseBodyAsString))
            {
                allTrials? result = (allTrials?)xSerializer.Deserialize(reader);

                if (result is not null)
                {
                    record_num = result.totalCount;
                }
            }
        }

        if (record_num > 0)
        {
            end_url = "&limit=100";
            if (record_num > 100)
            {
                // Split the calls to a per day basis.

                DateTime date_to_check = cut_off_date;
                DateTime end_date = date_to_check.AddDays(1);
                string date_to_check_param, end_date_param;
                while (date_to_check.Date <= DateTime.Now.Date)
                {
                    // do the call with an end date which is the next day
                    date_to_check_param = $"{date_to_check.Year}-{date_to_check.Month.ToString("00")}-{date_to_check.Day.ToString("00")}";
                    end_date_param = $"{end_date.Year}-{end_date.Month.ToString("00")}-{end_date.Day.ToString("00")}";
                    id_params = "lastEdited%20GE%20" + date_to_check_param + "T00:00:00%20AND%20lastEdited%20LT%20" + end_date_param + "T00:00:00";
                    url = start_url + id_params + end_url;

                    responseBodyAsString = await ch.GetAPIResponseAsync(url);

                    if (responseBodyAsString is not null)
                    {
                        DownloadResult batch_res = await DownloadBatch(responseBodyAsString, file_base, json_options, source_id, saf_id);
                        res.num_checked += batch_res.num_checked;
                        res.num_downloaded += batch_res.num_downloaded;
                        res.num_added += batch_res.num_added;
                    }

                    date_to_check = date_to_check.AddDays(1);
                    end_date = date_to_check.AddDays(1);
                }
            }
            else
            {
                url = start_url + id_params + end_url;
                responseBodyAsString = await ch.GetAPIResponseAsync(url);

                if (responseBodyAsString is not null)
                {
                    DownloadResult batch_res = await DownloadBatch(responseBodyAsString, file_base, json_options, source_id, saf_id);
                    res.num_checked += batch_res.num_checked;
                    res.num_downloaded += batch_res.num_downloaded;
                    res.num_added += batch_res.num_added;
                }
            }
        }
        

        return res;
    }


    public async Task<DownloadResult> DownloadRecordsBetweenDates(string file_base, DateTime cut_off_date, DateTime end_date, JsonSerializerOptions json_options,
                                                             int source_id, int saf_id, int? days_ago)
    {
        DownloadResult res = new();
        ISRCTN_Processor ISRCTN_processor = new();
        ScrapingHelpers ch = new(_logging_helper);

        string start_url = "https://www.isrctn.com/api/query/format/default?q=";
        string cut_off_date_param = $"{cut_off_date.Year}-{cut_off_date.Month}-{cut_off_date.Day}";
        string end_date_param = $"{end_date.Year}-{end_date.Month}-{end_date.Day}";
        string id_params = "lastEdited%20GE%20" + cut_off_date_param + "T00:00:00%20AND%20lastEdited%20LT%20" + end_date_param + "T00:00:00";
        string end_url = "&limit=100";
        string url = start_url + id_params + end_url;


        return res;
    }


    async Task<DownloadResult> DownloadBatch(string responseBody, string file_base, JsonSerializerOptions json_options, int source_id, int saf_id)
    {
        DownloadResult res = new();
        allTrials? result;
        XmlSerializer xSerializer = new(typeof(allTrials));

        try
        {
            using TextReader reader = new StringReader(responseBody);
            result = (allTrials?)xSerializer.Deserialize(reader);
        }
        catch (Exception e)
        {
            _logging_helper.LogCodeError("Error with json with " + responseBody, e.Message, e.StackTrace);
            return res;
        }

        if (result is not null)
        {
            ISRCTN_Processor isrctn_processor = new();

            FullTrial[]? full_trials = result.fullTrials;
            if (full_trials?.Any() == true)
            {
                foreach (FullTrial f in full_trials)
                {
                    res.num_checked++;
                    Study s = isrctn_processor.GetFullDetails(f);
                    if (s is not null && s.sd_sid is not null)
                    {
                        string full_path = await WriteOutFile(s, s.sd_sid, file_base, json_options);
                        if (full_path != "error")
                        {
                            string remote_url = "https://clinicaltrials.gov/ct2/show/" + s.sd_sid;
                            DateTime? last_updated = s.lastUpdated?.FetchDateTimeFromISO();
                            bool added = _mon_data_layer.UpdateStudyDownloadLog(source_id, s.sd_sid, remote_url, saf_id,
                                                    last_updated, full_path);
                            res.num_downloaded++;
                            if (added) res.num_added++;
                        }
                    }
                }
            }
        }

        return res;
    }


    // Writes out the file with the correct name to the correct folder, as indented json.
    // Called from both the DownloadBatch function.
    // Returns the full file path as constructed, or an 'error' string if an exception occurred.

    async Task<string> WriteOutFile(Study s, string sd_sid, string file_base, JsonSerializerOptions json_options)
    {
        string file_name = sd_sid + ".json";
        string full_path = Path.Combine(file_base, file_name!);
        try
        {
            using FileStream jsonStream = File.Create(full_path);
            await JsonSerializer.SerializeAsync(jsonStream, s, json_options);
            await jsonStream.DisposeAsync();
            return full_path;
        }
        catch (Exception e)
        {
            _logging_helper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
            return "error";
        }
    }
}
