using HtmlAgilityPack;
using MDR_Downloader.ctg;
using MDR_Downloader.Helpers;
using MDR_Downloader.pubmed;
using ScrapySharp.Extensions;
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

namespace MDR_Downloader.biolincc
{
    public class BioLINCC_Controller
    {
        private readonly LoggingHelper _logging_helper;
        private readonly MonDataLayer _mon_data_layer;
        private readonly BioLinccDataLayer _biolincc_repo;

        public BioLINCC_Controller(MonDataLayer mon_data_layer, LoggingHelper logging_helper)
        {
            _logging_helper = logging_helper;
            _mon_data_layer = mon_data_layer;
            _biolincc_repo = new ();
        }


        public async Task<DownloadResult> ObtainDatafromSourceAsync(Options opts, int saf_id, Source source)
        {
            /******************************************************************************
            * For BioLincc, all data is downloaded each time during a download, 
            * as it takes a relatively short time to run through about 300 studies.
            * 
            * Obtaining the data has two stages.
            * The first gets the web page list and then loops through it to fetch details
            * of each study, which are then stored as local files in the usual fashion.
            * Noter that id a n object cannot be classified it is filed, for later 
            * inspection, and the study id is not stored.
            * 
            * The second uses the data collected about which BIOLINCC stuydies linke to 
            * which NCT studies (it is always not 1-to-1) to update the 'in multi-BioLINCC
            * group' field, when multiple BioLINCC studies correspond to a single NCT study,
            * for those records that require it.
            * 
            ******************************************************************************/

            DownloadResult res = await LoopThroughPagesAsync(opts, saf_id, source);
            await PostProcessDataAsync(source, saf_id);
            return res;
        }


        public async Task<DownloadResult> LoopThroughPagesAsync(Options opts, int saf_id, Source source)
        {
            DownloadResult res = new ();
            ScrapingHelpers ch = new(_logging_helper);
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

            // Get list of studies from the Biolincc start page.

            WebPage? homePage = await ch.GetPageAsync("https://biolincc.nhlbi.nih.gov/studies/");
            if (homePage is null)
            {
                _logging_helper.LogError("Initial attempt to access BioLInnc studies list page failed");
                return res; // return zero result
            }

            HtmlNode? study_list_table = homePage.Find("div", By.Class("table-responsive")).FirstOrDefault();
            IEnumerable<HtmlNode>? studyRows = study_list_table.CssSelect("tbody tr");

            if (studyRows?.Any() == true)
            {
                _logging_helper.LogHeader("Processing Data");
                _logging_helper.LogLine($"File list obtained, of {studyRows.Count()} rows");

                BioLINCC_Processor biolincc_processor = new (ch, _logging_helper, _biolincc_repo);

                // Consider each study in turn, starting by looking at the list entries.

                foreach (HtmlNode row in studyRows)
                {
                    res.num_checked++;

                    //if (res.num_checked == 3) continue;
                    //if (res.num_checked > 5) break;

                    BioLincc_Basics? bb = biolincc_processor.GetStudyBasics(row);
                    if (bb is not null && bb.sd_sid is not null)
                    {
                        if (bb.collection_type == "Non-BioLINCC Resource")
                        {
                            _logging_helper.LogLine($"#{res.num_checked}: {bb.sd_sid} is a Non-BioLINCC Resource, not processed further");
                        }
                        else
                        {
                            // If record already downloaded recently, days_ago parameter may cause it
                            // to be ignored... (may be used if re-running after an error or object name update).

                            if (days_ago is null || !_mon_data_layer.Downloaded_recently(source_id, bb.sd_sid, (int)days_ago))
                            {
                                // Fetch the constructed study record.

                                BioLincc_Record? st = await biolincc_processor.GetStudyDetailsAsync(bb);
                                _logging_helper.LogLine($"Obtaining #{res.num_checked}: {bb.sd_sid}");

                                if (st is not null)
                                {
                                    // Store the links between Biolincc and NCT records.

                                    if (st.sd_sid is not null && st.registry_ids is not null)
                                    {
                                        _biolincc_repo.StoreLinks(st.sd_sid, st.registry_ids);
                                    }

                                    // store any nonmatched documents in the table
                                    // and if any exist abort the download for that record

                                    if (st.UnmatchedDocTypes?.Any() == true)
                                    {
                                        foreach (string s in st.UnmatchedDocTypes)
                                        {
                                            _biolincc_repo.InsertUnmatchedDocumentType(s);
                                        }
                                    }
                                    else
                                    {
                                        // Write out study record as json.

                                        string file_name = st.sd_sid + ".json";
                                        string full_path = Path.Combine(folder_path, file_name);
                                        string assoc_docs_num = (st.assoc_docs is null) ? "0" : st.assoc_docs.Count.ToString();
                                        try
                                        {
                                            using FileStream jsonStream = File.Create(full_path);
                                            await JsonSerializer.SerializeAsync(jsonStream, st, json_options);
                                            await jsonStream.DisposeAsync();
                                            _logging_helper.LogLine($"{res.num_checked}: {bb.sd_sid} downloaded, with {assoc_docs_num} linked publications");
                                        }
                                        catch (Exception e)
                                        {
                                            _logging_helper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
                                        }

                                        bool added = _mon_data_layer.UpdateStudyDownloadLog(source_id, st.sd_sid!, st.remote_url, saf_id,
                                                                          st.last_revised_date, full_path);
                                        res.num_downloaded++;
                                        if (added) res.num_added++;

                                        // Put a pause here 

                                        Thread.Sleep(1000);
                                    }
                                }
                            }
                        }
                        
                    }
                    
                    if (res.num_checked % 10 == 0)
                    {
                        _logging_helper.LogLine("files checked: " + res.num_checked.ToString());
                        _logging_helper.LogLine("files downloaded: " + res.num_downloaded.ToString());
                    }
                }

                _biolincc_repo.UpdateLinkStatus();  // identifies the multi-link studies inthe DB
            }
            return res;
        }


        public async Task PostProcessDataAsync(Source source, int saf_id)
        {
            // Allows groups of Biolinnc trials that equate to a single NCT registry to be identified.

            var json_options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };
            IEnumerable<StudyFileRecord> file_list = _mon_data_layer.FetchStudyFileRecords(source.id);
            int n = 0, r= 0; 

            foreach (StudyFileRecord rec in file_list)
            {
                n++;
                //if (n == 3) continue;
                //if (n > 5) break;
                bool in_multiple_biolincc_group = _biolincc_repo.GetMultiLinkStatus(rec.sd_id!);
                if (in_multiple_biolincc_group)
                { 
                    string filePath = rec.local_path ?? "";
                    if (File.Exists(filePath) && filePath[^4..] == "json")  
                    { 
                        // update the linkage data 
                        // read the file into the json object.

                        string jsonString = File.ReadAllText(filePath);
                        BioLincc_Record? biolincc_study = JsonSerializer.Deserialize<BioLincc_Record?>(jsonString, json_options);

                        if (biolincc_study is not null)
                        {
                            biolincc_study.in_multiple_biolincc_group = true;
                            r++;
                            try
                            {
                                using FileStream jsonStream = File.Create(filePath);
                                await JsonSerializer.SerializeAsync(jsonStream, biolincc_study, json_options);
                                await jsonStream.DisposeAsync();
                            }
                            catch (Exception e)
                            {
                                _logging_helper.LogLine("Error in trying to save file at " + filePath + ":: " + e.Message);
                            }
                        }
                    }
                }
                if (n % 10 == 0) _logging_helper.LogLine("Checked " + n.ToString());
            }
            _logging_helper.LogLine("Updated " + r.ToString() + " as being in 'multiple Biolincc to 1 NCT' group");
        }
    }
}
