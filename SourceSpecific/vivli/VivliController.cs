using ScrapySharp.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace MDR_Downloader.vivli
{
    public class Vivli_Controller
    {
        private readonly ILoggingHelper _logging_helper;
        private readonly IMonDataLayer _mon_data_layer;

        private readonly VivliDataLayer vivli_repo;
        private readonly Vivli_Processor processor;

        public Vivli_Controller(IMonDataLayer mon_data_layer, ILoggingHelper logging_helper)
        {
            _logging_helper = logging_helper;
            _mon_data_layer = mon_data_layer;

            vivli_repo = new VivliDataLayer();
            processor = new Vivli_Processor();
        }

        public async Task<DownloadResult> ObtainDatafromSourceAsync(Options opts, Source source)
        {
            FetchURLDetails(opts, source);
            await LoopThroughPagesAsync(opts, source);

            // for now
            DownloadResult res = new();
            return res;
        }

        public void FetchURLDetails(Options opts, Source source)
        {
            string? file_base = source.local_folder;
            int source_id = source.id;
            ScrapingBrowser browser = new()
            {
                AllowAutoRedirect = true,
                AllowMetaRedirect = true,
                Encoding = Encoding.UTF8
            };
            VivliCopyHelpers vch = new();
            
            // Set up initial study list
            // store it in pp table

            List<VivliURL> all_study_list = new();
            vivli_repo.SetUpParameterTable();

            string baseURL = "https://search.datacite.org/works?query=vivli&resource-type-id=dataset";
            WebPage startPage = browser.NavigateToPage(new Uri(baseURL));

            // Entries on DataCite search are 25 / page
            int totalNumber = processor.GetStudyNumbers(startPage);
            int loopEndNumber = (totalNumber / 25) + 2;

            // for (int i = 1; i < 5; i++)  // testing only
            for (int i = 1; i < loopEndNumber; i++)
            {
                string URL = baseURL + " &page=" + i.ToString();
                WebPage web_page = browser.NavigateToPage(new Uri(URL));

                List<VivliURL> page_study_list = processor.GetStudyInitialDetails(web_page, i);
                vivli_repo.StoreRecs(vch.api_url_copyhelper, page_study_list);

                // Log to console and pause before the next page

                _logging_helper.LogLine(i.ToString());
                System.Threading.Thread.Sleep(1000);
            }
        }

        public async Task LoopThroughPagesAsync(Options opts, Source source)
        {

            // Go through the vivli data, fetcvhing the stored urls
            // and using these to call the api directly, receiving json
            // that can be extracted directly from the response

            vivli_repo.SetUpStudiesTable();
            vivli_repo.SetUpPackagesTable();
            vivli_repo.SetUpDataObectsTable();

            IEnumerable<VivliURL> all_study_list = vivli_repo.FetchVivliApiUrLs();

            foreach (VivliURL s in all_study_list)
            {
                await processor.GetAndStoreStudyDetails(s, vivli_repo, _logging_helper);

                // logging to go here

                // write to console...
                _logging_helper.LogLine(s.id.ToString() + ": " + s.vivli_url);

                // put a pause here if necessary
                System.Threading.Thread.Sleep(800);

            }
        }
    }
}
