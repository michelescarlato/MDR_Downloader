using ScrapySharp.Extensions;
using ScrapySharp.Html;
using ScrapySharp.Network;
using HtmlAgilityPack;
using System.Text;
using System.Text.Json;

namespace MDR_Downloader.Helpers
{

    public class ScrapingHelpers
    {
        private readonly LoggingHelper _logging_helper;
        private readonly ScrapingBrowser browser;
        private readonly HttpClient webClient;

        public ScrapingHelpers(LoggingHelper logging_helper)
        {
            _logging_helper = logging_helper;
            webClient = new HttpClient();

            browser = new()
            {
                AllowAutoRedirect = true,
                AllowMetaRedirect = true,
                Encoding = Encoding.UTF8
            };
        }



        public async Task<WebPage?> GetPageAsync(string url)
        {
            try
            {
                return await browser.NavigateToPageAsync(new Uri(url));
            }
            catch (Exception e)
            {
                _logging_helper.LogError("Error in obtaining page from " + url + ": " + e.Message);
                return null;
            }
        }


        public async Task<string> GetPMIDFromPageAsync(string citation_link)
        {
            string pmid = "";
            WebPage? page = await GetPageAsync(citation_link);
            if (page is not null)
            {
                // only works with pmid pages, that have this dl tag....
                HtmlNode? ids_div = page.Find("dl", By.Class("rprtid")).FirstOrDefault();
                if (ids_div is not null)
                {
                    HtmlNode[] dts = ids_div.CssSelect("dt").ToArray();
                    HtmlNode[] dds = ids_div.CssSelect("dd").ToArray();

                    if (dts is not null && dds is not null)
                    {
                        for (int i = 0; i < dts.Length; i++)
                        {
                            string dts_type = dts[i].InnerText.Trim();
                            if (dts_type == "PMID:")
                            {
                                pmid = dds[i].InnerText.Trim();
                            }
                        }
                    }
                }
            }
            return pmid;
        }



        public async Task<string?> GetStringFromURLAsync(string url)
        {
            try
            {
                return  await webClient.GetStringAsync(url);
            }
            catch (Exception e)
            {
                _logging_helper.LogError("Problem in using webClient.GetStringAsync at "
                                       + url + ": " + e.Message);
                return null;
            }
        }


        public async Task<string?> GetPMIDFromNLMAsync(string pmc_id)
        {
            string base_url = "https://www.ncbi.nlm.nih.gov/pmc/utils/idconv/v1.0/";
            base_url += "?tool=ECRIN-MDR&email=steve@canhamis.eu&versions=no&ids=";
            string query_url = base_url + pmc_id + "&format=json";
          
            try
            {
                HttpResponseMessage response = await webClient.GetAsync(query_url);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                PMCResponse? PMC_object = JsonSerializer.Deserialize<PMCResponse>(responseBody, options);
                return PMC_object?.records?[0].pmid;
            }
            catch (Exception e)
            {
                _logging_helper.LogError("Error in obtaining pmid from PMC page" + pmc_id + ": " + e.Message);
                return null;
            }
        }


    }
}
