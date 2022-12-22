using HtmlAgilityPack;
using MDR_Downloader.Helpers;
using ScrapySharp.Extensions;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MDR_Downloader.isrctn
{
    class ISRCTN_Controller
    {
        private readonly LoggingHelper _logging_helper;
        private readonly MonDataLayer _mon_data_layer;

        public ISRCTN_Controller(MonDataLayer mon_data_layer, LoggingHelper logging_helper)
        {
            _logging_helper = logging_helper;
            _mon_data_layer = mon_data_layer;
       }

        // Get the number of records required and set up the loop

        public async Task<DownloadResult> ObtainDatafromSourceAsync(Options opts, int saf_id, Source source)
        {
            DownloadResult res = new();

            string? file_base = source.local_folder;
            if (file_base is null)
            {
                _logging_helper.LogError("Null value passed for local folder value for this source");
                return res;   // return zero result
            }

            int source_id = source.id;
            int? days_ago = opts.SkipRecentDays;     
            string? cut_off_date_string = opts.CutoffDateAsString;
            if (cut_off_date_string is null)
            {
                _logging_helper.LogError("Null value passed for cut off date for thios source");
                return res;   // return zero result
            }

            ISRCTN_Processor ISRCTN_processor = new();
            XmlSerializer writer = new(typeof(ISCTRN_Record));
            ScrapingHelpers ch = new(_logging_helper);

            string part1_of_url = "http://www.isrctn.com/search?pageSize=100&page=";
            string part2_of_url = "&q=&filters=GT+lastEdited%3A";
            string end_of_url = "T00%3A00%3A00.000Z&searchType=advanced-search";

            string start_url = part1_of_url + "1" + part2_of_url + cut_off_date_string + end_of_url;
            WebPage? prePage = await ch.GetPageAsync(start_url);   // throw away the initial page received - gives a 'use API' message
            WebPage? summaryPage = await ch.GetPageAsync(start_url);
            if (summaryPage is null)
            {
                _logging_helper.LogError("Unable to reach iniital page to start scraping process");
                return res;   // return zero result
            }

            int rec_num = ISRCTN_processor.GetListLength(summaryPage);  // unable to scrape the summary page
            if (rec_num == 0) 
            {
                _logging_helper.LogError("Unable to read total of records to be scraped in system - unable to proceed");
                return res;   // return zero result
            }
            else
            {
                // Obtain and go through each page of 100 entries.

                int loop_limit = rec_num % 100 == 0 ? rec_num / 100 : (rec_num / 100) + 1;

                for (int i = 1; i <= loop_limit; i++)
                {
                    WebPage? homePage = await ch.GetPageAsync(part1_of_url + i.ToString() + 
                                          part2_of_url + cut_off_date_string + end_of_url);
                    if (homePage is not null)
                    {
                        int n = 0;
                        var pageContent = homePage.Find("ul", By.Class("ResultsList"));
                        HtmlNode[] studyRows = pageContent.CssSelect("li article").ToArray();
                        string ISRCTNNumber, remote_link;
                        int colonPos;

                        // Now process each study, one row at a time

                        foreach (HtmlNode row in studyRows)
                        {
                            HtmlNode? main = row.CssSelect(".ResultsList_item_main").FirstOrDefault();
                            HtmlNode? title = main.CssSelect(".ResultsList_item_title a").FirstOrDefault();
                            if (title is not null)
                            {
                                string titleString = title.InnerText?.Replace("\n", "")?.Replace("\r", "")?.Trim() ?? "";
                                if (titleString.Contains(':'))
                                {
                                    // get ISRCTN id

                                    colonPos = titleString.IndexOf(":");
                                    ISRCTNNumber = titleString[..(colonPos - 1)].Trim();
                                    res.num_checked++;

                                    // record has been added or revised since the cutoff date (normally the last download), 
                                    // but...should it be downloaded.

                                    bool do_download = false;
                                    StudyFileRecord? sfr = _mon_data_layer.FetchStudyFileRecord(ISRCTNNumber, source_id);
                                    if (sfr is null)
                                    {
                                        do_download = true;  // record does not exist
                                    }
                                    else if (days_ago is null || !_mon_data_layer.Downloaded_recently(source_id, ISRCTNNumber, (int)days_ago))
                                    {
                                        // if record already within last days_ago today, ignore it... (may happen if re-running after an error)
                                        do_download = true;  // record has not been downloaded recently
                                    }
                                    if (do_download)
                                    {
                                        remote_link = "https://www.isrctn.com/" + ISRCTNNumber;

                                        // obtain details of that study but pause every 10 accesses.

                                        if (n % 10 == 0)
                                        {
                                            Thread.Sleep(2000);
                                        }

                                        ISCTRN_Record st = new();
                                        WebPage? detailsPage = await ch.GetPageAsync(remote_link);
                                        if (detailsPage is not null)
                                        {
                                            st = ISRCTN_processor.GetFullDetails(detailsPage, ISRCTNNumber);

                                            // Write out study record as XML.
                                            if (!Directory.Exists(file_base))
                                            {
                                                Directory.CreateDirectory(file_base);
                                            }
                                            string file_name = st.isctrn_id + ".xml";
                                            string full_path = Path.Combine(file_base, file_name);
                                            FileStream file = File.Create(full_path);
                                            writer.Serialize(file, st);
                                            file.Close();

                                            bool added = _mon_data_layer.UpdateStudyDownloadLog(source_id, st.isctrn_id!, remote_link, saf_id,
                                                                               st.last_edited, full_path);
                                            res.num_downloaded++;
                                            if (added) res.num_added++;
                                        }
                                    }
                                }
                            }

                            if (res.num_checked % 10 == 0) _logging_helper.LogLine(res.num_checked.ToString() + " files downloaded");
                        }
                    }

                }
            }
            
            return res;
        }
    }
}
