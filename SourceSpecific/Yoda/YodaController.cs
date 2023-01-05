using HtmlAgilityPack;
using MDR_Downloader.Helpers;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Serialization;

namespace MDR_Downloader.yoda
{
    class Yoda_Controller
    {
        private readonly LoggingHelper _logging_helper;
        private readonly MonDataLayer _mon_data_layer;

        public Yoda_Controller(MonDataLayer mon_data_layer, LoggingHelper logging_helper)
        {
            _logging_helper = logging_helper;
            _mon_data_layer = mon_data_layer;
        }

        public async Task<DownloadResult> ObtainDatafromSourceAsync(Options opts, int saf_id, Source source)
        {
            // For Yoda, all data is downloaded each time during a download, as it takes a relatively short time
            // and the files simply replaced or - if new - added to the folder. There is therefore not a concept of an
            // update or focused download, as opposed to a full download.

            DownloadResult res = new();
            ScrapingHelpers ch = new (_logging_helper);
            string? folder_path = source.local_folder;
            if (folder_path is null)
            {
                _logging_helper.LogError("Null value passed for local folder value for this source");
                return res;   // return zero result
            }
            else
            {
                if (!Directory.Exists(folder_path))
                {
                    Directory.CreateDirectory(folder_path);  // ensure folder is present
                }
            }

            int source_id = source.id;
            int? days_ago = opts.SkipRecentDays;
            var json_options = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

            // Get list of studies from the Yoda start page.

            string baseURL = "https://yoda.yale.edu/trials-search?amp%3Bpage=0&page=";
            int search_page_limit;
            WebPage? firstPage = await ch.GetPageAsync(baseURL + "0");
            if (firstPage is null)
            {
                _logging_helper.LogError("Attempt to access first Yoda studies list page failed");
                return res;   // return zero result
            }

            HtmlNode? resultCountStatement = firstPage.Find("div", By.Class("result-count")).FirstOrDefault();
            if (resultCountStatement is null) 
            {
                _logging_helper.LogError("Unable to find record count details section on first search page");
                return res;
            }

            string record_string = resultCountStatement.InnerText;
            int of_pos = record_string.IndexOf(" of ");
            string record_number_string = record_string[(of_pos + 4)..];

            if (Int32.TryParse(record_number_string, out int record_count))
            {
                search_page_limit = (record_count % 10 == 0) ? record_count / 10 : (record_count / 10) + 1;
            }
            else
            {
                _logging_helper.LogError("Unable to extract record count total on first search page");
                return res;
            }

            YodaDataLayer yoda_repo = new();
            Yoda_Processor yoda_processor = new(ch, _logging_helper, yoda_repo);
            List<Summary> all_study_list = new();

            // Loop through the search pages and build up a list of stuudy summaries

            for (int i = 0; i < search_page_limit; i++)
            {
                WebPage? searchPage = await ch.GetPageAsync(baseURL + i.ToString());
                if (searchPage is null)
                {
                    _logging_helper.LogError($"Attempt to access Yoda studies list page {i} failed");
                    return res;  // return zero res
                }
                else
                {
                    List<Summary> page_study_list = yoda_processor.GetStudyInitialDetails(searchPage);
                    all_study_list.AddRange(page_study_list);
                    _logging_helper.LogLine($"search page: {i}, yielding {page_study_list.Count} study summaries");
                    Thread.Sleep(300);
                }
            }

            // Do a check on any possible id duplicates. Consider each study in turn.
            // Duplicates rare but do occur but seem to be temporary features.

            int n = 0;
            List<Summary> study_list = new();

            foreach (Summary sm in all_study_list)
            {
                n++;
                bool transfer_to_list = true;
                string id_to_check = sm.sd_sid!;
                int s_pos = 0;
                foreach (Summary s in study_list)
                {
                    s_pos++;
                    if (id_to_check == s.sd_sid)
                    {
                        _logging_helper.LogLine("More than one id found for " + n.ToString() + ": " + sm.study_name);
                        transfer_to_list = false;
                    }
                }

                if (transfer_to_list)
                {
                    study_list.Add(sm);
                }
            }

            // Finally ready to process the Yoda study details
            _logging_helper.LogLine($"Studies to download: {study_list.Count}");

            foreach (Summary sm in study_list)
            {
                if (sm.sd_sid is not null && sm.details_link is not null)
                {
                    // sd_sid and details_link should normally always be present but just in case...
                    // Unless record already downloaded in stipulated period get the web page and,
                    // assuming it has been retrieved OK, process it.

                    if (days_ago is null || !_mon_data_layer.Downloaded_recently(source_id, sm.sd_sid, (int)days_ago))
                    {
                        WebPage? studyPage = await ch.GetPageAsync(sm.details_link);
                        res.num_checked++;
                        if (studyPage is not null)
                        {
                            HtmlNode? page = studyPage.Find("div", By.Class("region-content")).FirstOrDefault();
                            if (page is not null)
                            {
                                Yoda_Record? st = await yoda_processor.GetStudyDetailsAsync(page, sm);
                                 
                                if (st is not null)
                                {
                                    // Write out study record as XML.

                                    string file_name = st.sd_sid + ".json";
                                    string full_path = Path.Combine(folder_path, file_name);
                                    try
                                    {
                                        using FileStream jsonStream = File.Create(full_path);
                                        await JsonSerializer.SerializeAsync(jsonStream, st, json_options);
                                        await jsonStream.DisposeAsync();
                                        _logging_helper.LogLine($"{res.num_checked}: {st.sd_sid} downloaded");
                                    }
                                    catch (Exception e)
                                    {
                                        _logging_helper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
                                    }

                                    bool added = _mon_data_layer.UpdateStudyDownloadLog(source_id, st.sd_sid!, st.remote_url, saf_id,
                                                                      null, full_path);
                                    res.num_downloaded++;
                                    if (added) res.num_added++;

                                    // Put a pause here if necessary.

                                    Thread.Sleep(500);
                                }
                                else
                                {
                                    _logging_helper.LogLine($"Null study details for {sm.sd_sid}");
                                }
                            }
                        }
                    }
                }
            }

            return res;
        }
    }
}
