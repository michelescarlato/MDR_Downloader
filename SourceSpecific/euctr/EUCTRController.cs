using MDR_Downloader.Helpers;
using ScrapySharp.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace MDR_Downloader.euctr
{
    class EUCTR_Controller
    {
        LoggingHelper _logging_helper;
        MonDataLayer _mon_data_layer;

        EUCTR_Processor processor;

        int access_error_num = 0;
        int pause_error_num = 0;

        public EUCTR_Controller(MonDataLayer mon_data_layer, LoggingHelper logging_helper)
        {
            processor = new EUCTR_Processor();
            
            _logging_helper = logging_helper;
            _mon_data_layer = mon_data_layer;
        }

        public async Task<DownloadResult> ObtainDatafromSourceAsync(Options opts, int saf_id, Source source)
        {
            // consider type - from args
            // if sf_type = 141, 142, 143 (normally 142 here) only download files 
            // not already marked as 'complete' - i.e. very unlikely to change. This is 
            // signalled by including the flag in the call to the processor routine.

            DownloadResult res = new ();
            string? file_base = source.local_folder;
            if (file_base is null)
            {
                _logging_helper.LogError("Null value passed for local folder value for this source");
                return res;   // return zero result
            }

            int? days_ago = opts.SkipRecentDays;
            int sf_type_id = opts.FetchTypeId;
            int source_id = source.id;
            ScrapingHelpers ch = new (_logging_helper);
            XmlSerializer writer = new (typeof(EUCTR_Record));
            
            bool do_all_records = !(sf_type_id == 141 || sf_type_id == 142 || sf_type_id == 143);
            string baseURL = "https://www.clinicaltrialsregister.eu/ctr-search/search?query=&page=";
            WebPage? searchPage = await ch.GetPageAsync(baseURL);
            if (searchPage is null)
            {
                _logging_helper.LogError("Attempt to access first search page failed");
                return res;   // return zero result
            }

            int skipped = 0;
            int rec_num = processor.GetListLength(searchPage);
            if (rec_num != 0)
            {
                int loop_limit = rec_num % 20 == 0 ? rec_num / 20 : (rec_num / 20) + 1;
                for (int i = 0; i <= loop_limit; i++)
                {
                    // Go to the summary page indicated by current value of i
                    // Each page has up to 20 listed studies.
                    // Once on that page each of the studies is processed in turn...
                    searchPage = await ch.GetPageAsync(baseURL + i.ToString());
                    if (searchPage is not null)
                    {
                        List<EUCTR_Summmary> summaries = processor.GetStudySuummaries(searchPage);

                        foreach (EUCTR_Summmary s in summaries)
                        {
                            // Check the euctr_id (sd_id) is not 'assumed complete' if only incomplete 
                            // records are being considered; only proceed if this is the case
                            bool do_download = false;
                            res.num_checked++;

                            StudyFileRecord? file_record = _mon_data_layer.FetchStudyFileRecord(s.eudract_id!, source_id);
                            if (file_record is null)
                            {
                                do_download = true;  // record does not yet exist
                            }
                            else if (do_all_records || file_record.assume_complete != true)
                            {
                                // if record exists only consider it if the 'incomplete only' flag is being ignored,
                                // or the completion status is false or null
                                // Even then do a double check to ensure the record has not been recently downloaded
                                if (days_ago == null || !_mon_data_layer.Downloaded_recently(source_id, s.eudract_id!, (int)days_ago))
                                {
                                    do_download = true; // download if not assumed complete, or incomplete only flag does not apply
                                }
                            }
                            if (!do_download) skipped++;

                            if (do_download)
                            {
                                // transfer summary details to the main EUCTR_record object
                                EUCTR_Record st = new EUCTR_Record(s);

                                WebPage? detailsPage = await ch.GetPageAsync(st.details_url!);
                                if (detailsPage is not null)
                                {
                                    st = processor.ExtractProtocolDetails(st, detailsPage);

                                    // Then get results details

                                    if (st.results_url is not null)
                                    {
                                        System.Threading.Thread.Sleep(800);
                                        WebPage? resultsPage = await ch.GetPageAsync(st.results_url);
                                        if (resultsPage is not null)
                                        {
                                            st = processor.ExtractResultDetails(st, resultsPage);
                                        }
                                        else
                                        {
                                            _logging_helper.LogError("Problem in navigating to result details, id is " + s.eudract_id);
                                        }
                                    }

                                    // Write out study record as XML.
                                    if (!Directory.Exists(file_base))
                                    {
                                        Directory.CreateDirectory(file_base);
                                    }
                                    string file_name = "EU " + st.eudract_id + ".xml";
                                    string full_path = Path.Combine(file_base, file_name);
                                    FileStream file = File.Create(full_path);
                                    writer.Serialize(file, st);
                                    file.Close();

                                    bool assume_complete = false;
                                    if (st.trial_status == "Completed" && st.results_url is not null)
                                    {
                                        assume_complete = true;
                                    }
                                    bool added = _mon_data_layer.UpdateStudyDownloadLogWithCompStatus(source_id, st.eudract_id!,
                                                                       st.details_url!, saf_id,
                                                                       assume_complete, full_path);
                                    res.num_downloaded++;
                                    if (added) res.num_added++;
                                }
                                else
                                {
                                    _logging_helper.LogError("Problem in navigating to protocol details, id is " + s.eudract_id);
                                    CheckAccessErrorCount();
                                }

                                System.Threading.Thread.Sleep(800);
                            }

                            if (res.num_checked % 10 == 0)
                            {
                                _logging_helper.LogLine("EUCTR pages checked: " + res.num_checked.ToString());
                                _logging_helper.LogLine("EUCTR pages skipped: " + skipped.ToString());
                            }
                        }

                    }
                    else
                    {
                        _logging_helper.LogError("Problem in navigating to summary page, page value is " + i.ToString());
                        CheckAccessErrorCount();
                    }
                }
            }

            _logging_helper.LogLine("Number of errors: " + access_error_num.ToString());
            return res;
        }


        private void CheckAccessErrorCount()
        {
            access_error_num++;
            if (access_error_num % 5 == 0)
            {
                // do a 5 minute pause
                TimeSpan pause = new TimeSpan(0, 1, 0);
                System.Threading.Thread.Sleep(pause);
                pause_error_num++;
                access_error_num = 0;
            }
            //if (pause_error_num > 5)
            //{
                //  throw new Exception("Too many access errors");
            //}
        }
        
    }
}
