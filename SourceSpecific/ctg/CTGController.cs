using System;
using System.Runtime.Intrinsics.X86;
using System.Text.Encodings.Web;
using System.Text.Json;
using MDR_Downloader.Helpers;

namespace MDR_Downloader.ctg;

class CTG_Controller
{
    private readonly LoggingHelper _logging_helper;
    private readonly MonDataLayer _mon_data_layer;

    public CTG_Controller(MonDataLayer mon_data_layer, LoggingHelper logging_helper)
    {
        _logging_helper = logging_helper;
        _mon_data_layer = mon_data_layer;
    }


    public async Task<DownloadResult> ObtainDatafromSourceAsync(Options opts, int saf_id, Source source)
    {
        // Data retrieval is normally via an API call to revised files using a cut off revision date, 
        // that date being normally the date of the most recent dowenload.
        // Alternatively a range of Ids can be provided as the basis of the query string - the latter 
        // used during bilk downloads of large numbers of pre-determined files
        // The opts.FetchTypeId parameter needs to be inspected to determine which.

        // New files will be added, the amended files replaced, as necessary.

        DownloadResult res = new ();
        string? file_base = source.local_folder;
        if (file_base is null)
        {
            _logging_helper.LogError("Null value passed for local folder value for this source");
            return res;   // return zero result
        }

        int t = opts.FetchTypeId;
        DateTime? cutoff_date = opts.CutoffDate;
        ScrapingHelpers ch = new (_logging_helper);
        var json_options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        if (t == 111 && cutoff_date is not null)
        {
            cutoff_date = (DateTime)cutoff_date;
            string year = cutoff_date.Value.Year.ToString();
            string month = cutoff_date.Value.Month.ToString("00");
            string day = cutoff_date.Value.Day.ToString("00");

            int min_rank = 1;
            int max_rank = 1;

            string start_url = "https://clinicaltrials.gov/api/query/full_studies?expr=AREA%5BLastUpdatePostDate%5DRANGE%5B";
            string cut_off_params = month + "%2F" + day + "%2F" + year;
            string end_url = "%2C+MAX%5D&min_rnk=" + min_rank.ToString() + "&max_rnk=" + max_rank.ToString() + "&fmt=json";
            string url = start_url + cut_off_params + end_url;

            // Do initial search 

            string? responseBody = await ch.GetStringFromURLAsync(url);
            if (responseBody is not null)
            {
                CTGRootobject? resp = JsonSerializer.Deserialize<CTGRootobject?>(responseBody);
                if (resp is null)
                {
                    return res;
                }
                int? nums_studies_found = resp.FullStudiesResponse?.NStudiesFound;

                if (nums_studies_found is not null && nums_studies_found > 0)
                {
                    // Then go through the identified records 20 at a time.

                    int loop_count = nums_studies_found % 20 == 0
                            ? (int)nums_studies_found / 20
                            : ((int)nums_studies_found / 20) + 1;

                    for (int i = 0; i < loop_count; i++)
                    {
                        Thread.Sleep(800);
                        min_rank = (i * 20) + 1;
                        max_rank = (i * 20) + 20;
                        end_url = "%2C+MAX%5D&min_rnk=" + min_rank.ToString() + "&max_rnk=" + max_rank.ToString() + "&fmt=json";
                        url = start_url + cut_off_params + end_url;

                        responseBody = await ch.GetStringFromURLAsync(url);
                        if (responseBody is not null)
                        {
                            DownloadResult batch_res = await DownloadBatch(responseBody, file_base, json_options, source.id, saf_id);

                            res.num_checked += batch_res.num_checked;
                            res.num_downloaded += batch_res.num_downloaded;
                            res.num_added += batch_res.num_added;


                        }
                    }
                }
                else
                {
                    return res;
                }
            }
        }

        if (t == 146 && opts.AmountIds is not null && opts.OffsetIds is not null)
        {
            int amount = (int)opts.AmountIds;
            int offset = (int)opts.OffsetIds;

            int loop_count = amount % 20 == 0
                            ? (int)amount / 20
                            : ((int)amount / 20) + 1;
            int min_id, max_id;

            for (int i = 0; i < loop_count; i++)
            {
                Thread.Sleep(800);
                min_id = offset + (i * 20) + 1;
                max_id = offset + (i * 20) + 20;

                // get the correct NCTIds
                string firstNctId = "NCT" + min_id.ToString("00000000");
                string lastNctId = "NCT" + max_id.ToString("00000000");

                string start_url = "https://clinicaltrials.gov/api/query/full_studies?expr=AREA%5BNCTId%5DRANGE%5B";
                string id_params = firstNctId + "%2C" + lastNctId;
                string end_url = $"%5D&min_rnk=1&max_rnk=20&fmt=json";
                string url = start_url + id_params + end_url;

                string? responseBody = await ch.GetStringFromURLAsync(url);
                if (responseBody is not null)
                {
                    DownloadResult batch_res = await DownloadBatch(responseBody, file_base, json_options, source.id, saf_id);
                    
                    res.num_checked += batch_res.num_checked;
                    res.num_downloaded += batch_res.num_downloaded;
                    res.num_added += batch_res.num_added;

                    _logging_helper.LogLine($"Records checked from {firstNctId} to {lastNctId}. Downloaded: {batch_res.num_downloaded}");
                }
                else
                {
                    return res;
                }
            }
        }

        return res;
    }

    async Task<DownloadResult>DownloadBatch(string responseBody, string file_base, JsonSerializerOptions json_options, int source_id, int saf_id)
    {
        CTGRootobject? json_resp;
        DownloadResult res = new();
        try
        {
            json_resp = JsonSerializer.Deserialize<CTGRootobject?>(responseBody, json_options);
        }
        catch (Exception e)
        {
            _logging_helper.LogCodeError("Error with json with " + responseBody, e.Message, e.StackTrace);
            return res;
        }

        if (json_resp is not null)
        {
            Fullstudy[]? full_studies = json_resp.FullStudiesResponse?.FullStudies;
            if (full_studies?.Any() == true)
            {
                foreach (Fullstudy f in full_studies)
                {
                    res.num_checked++;
                    Study s = f.Study!;

                    // Obtain basic information from the file - enough for 
                    // the details to be filed in source_study_data table.

                    string? sd_sid = s.ProtocolSection?.IdentificationModule?.NCTId;
                    if (sd_sid is not null)
                    {
                        string file_name = sd_sid + ".json";
                        string file_folder = sd_sid[..7] + "xxxx";
                        string remote_url = "https://clinicaltrials.gov/ct2/show/" + sd_sid;

                        string? last_updated_string = s.ProtocolSection?.StatusModule?.LastUpdatePostDateStruct?.LastUpdatePostDate;
                        DateTime? last_updated = last_updated_string?.FetchDateTimeFromDateString();

                        // Then write out study file as indented json

                        string folder_path = file_base + file_folder;
                        if (!Directory.Exists(folder_path))
                        {
                            Directory.CreateDirectory(folder_path);
                        }
                        string full_path = Path.Combine(folder_path, file_name!);
                        try
                        {
                            using FileStream jsonStream = File.Create(full_path);
                            await JsonSerializer.SerializeAsync(jsonStream, s, json_options);
                            await jsonStream.DisposeAsync();
                        }
                        catch (Exception e)
                        {
                            _logging_helper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
                        }

                        // Record details of updated or new record in source_study_data.

                        bool added = _mon_data_layer.UpdateStudyDownloadLog(source_id, sd_sid, remote_url, saf_id,
                                                    last_updated, full_path);
                        res.num_downloaded++;
                        if (added) res.num_added++;
                    }
                }
            }
        }
            
        return res;
    }
       
}
