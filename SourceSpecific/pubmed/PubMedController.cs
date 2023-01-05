using System.Data;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;
using MDR_Downloader.Helpers;

namespace MDR_Downloader.pubmed
{

    // Pubmed searches and / or file downloads are often associated with filters...
    // 10001	"PubMed CTG"	"PubMed abstracts with references to ClinicalTrials.gov entries"
    // 10002	"PubMed COVID"	"PubMed abstracts found on searches related to COVID-19 (SARS, MERS etc.)"
    // 10003	"PubMed Registries"	"PubMed abstracts with references to any trial registry"
    // 10004	"Pubmed - Study References"	"Identifies PubMed references in Study sources that have not yet been downloaded"

    // Pubmed data has two sources - the study references in certain databases
    // and the pubmed records themselves - specifically those that have a reference to a 'databank' 
    // (trial registry in this context)

    // Normally, the 'bank' pmid records will be identified by an initial search, but include only those 
    // that have been modified since the last similar download (represented by the cutoff date). 
    // This includes new records.

    // The records found for each bank are stored in a table in the pp schema - pp.temp_by_bank_records.
    // These are transferred in turn to the pp.pmids_with_bank_records table

    // At the end of the loop through all banks the *distinct* pmid records are transfered to a list in memory.
    // The system loops through these - if the record exists it is replaced and the object source record
    // is updated in the mon database.
    // if the record is new the record is downloaded and a new object source record is created.
    // This strategy is represented by saf_type 114 (with cutoff date, and filter 10003)

    // A variant allows all pmids with bank records to bne downloaded, irrespective of date, but otherwise 
    // the process is the same.
    // This strategy is represented by saf_type 121 Filtered records (download) (with filter 10003)

    // The study_reference records are collected - one database at a time - and transferred to a single database.
    // At the end of that process a distinct list is created in memory.
    // The system loops through this 10 records at a time. If there is no object source record one is created 
    // and the file is downloaded - it is new to the system.
    // If a source record exists the record needs checking to see if it has been revised since the last similar exercise.
    // If it has the record is downloaded and the source file is updated. If not, no download takes place. 
    // This strategy is represented by saf_type 114 (with cutoff date, and filter 10004)

    // A variant allows all pmids derived from study references, irrespective of revision date, to be downloaded.
    // This strategy is represented by saf_type 121 Filtered records (download) (with filter 10004)

    public class PubMed_Controller
    {
        private readonly LoggingHelper _logging_helper;
        private readonly MonDataLayer _mon_data_layer;

        private readonly PubMedDataLayer pubmed_repo;

        private readonly string api_key;

        public PubMed_Controller(MonDataLayer mon_data_layer, LoggingHelper logging_helper)
        {
            _logging_helper = logging_helper;
            _mon_data_layer = mon_data_layer;

            pubmed_repo = new PubMedDataLayer(_logging_helper);

            // API key belongs to NCBI user stevecanhamn (steve.canham@ecrin.org,
            // stored in appsettings.json and accessed via the logging repo.
            api_key = "&api_key=" + _mon_data_layer.PubmedAPIKey;
        }


        public async Task<DownloadResult> ObtainDatafromSourceAsync(Options opts, int saf_id, Source source)
        {
            DownloadResult res = new();

            if (opts.FocusedSearchId == 10003)
            {
                // download articles with references to trial registries, that
                // have been revised since the cutoff date
                res = await ProcessPMIDsListfromBanksAsync(opts, saf_id, source);
            }

            if (opts.FocusedSearchId == 10004)
            {
                // download pmids listed as references in other sources,
                // that have been revised since the cutoff date 
                res = await ProcessPMIDsListfromDBSourcesAsync(opts, saf_id, source);
            }
            return res;
        }


        public async Task<DownloadResult> ProcessPMIDsListfromBanksAsync(Options opts, int saf_id, Source source)
        {
            DownloadResult res = new();
            string date_string = "";

            XmlSerializer xSerializer = new(typeof(eSearchResult));
            ScrapingHelpers ch = new(_logging_helper);

            // This search can be (and usually is) date sensitive.

            if (opts.FetchTypeId == 114)
            {
                string today = DateTime.Now.ToString("yyyy/MM/dd");
                string cutoff = "2020/01/01";   // normally replaced below
                if (opts.CutoffDate is not null)
                { 
                    cutoff = ((DateTime)opts.CutoffDate).ToString("yyyy/MM/dd");
                }
                date_string = "&mindate=" + cutoff + "&maxdate=" + today + "&datetype=mdat";
            }

            // Get list of potential linked data banks (includes trial registries).

            IEnumerable<PMSource> banks = pubmed_repo.FetchDatabanks();
            string web_env = "";
            int query_key = 0;

            foreach (PMSource s in banks)
            {
                // Use databank details to construct search string

                string search_baseURL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi?db=pubmed";
                string search_term = "&term=" + s.nlm_abbrev + "[SI]" + date_string;
                string search_url = search_baseURL + api_key + search_term + "&usehistory=y";

                // Get the number of total records that have this databank reference
                // and that (usually) have been revised recently 
                // and calculate the loop parameters.

                string? responseBody = await ch.GetAPIResponseAsync(search_url);
                if (responseBody is not null)
                {
                    int totalRecords = 0;
                    using (TextReader reader = new StringReader(responseBody))
                    {
                        // The eSearchResult class corresponds to the returned data.

                        eSearchResult? result = (eSearchResult?)xSerializer.Deserialize(reader);

                        if (result is not null)
                        {
                            totalRecords = result.Count;
                            query_key = result.QueryKey;
                            web_env = result.WebEnv;
                        }
                    }

                    // loop through the records and obtain and store relevant
                    // records, of PubMed Ids, retmax (= 100) at a time     

                    if (totalRecords > 0)
                    {
                        int retmax = 100;
                        string fetch_baseURL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=pubmed";
                        fetch_baseURL += api_key + "&WebEnv=" + web_env + "&query_key=" + query_key.ToString();
                        int numCallsNeeded = (int)(totalRecords / retmax) + 1;
                        for (int i = 0; i < numCallsNeeded; i++)
                        {
                            try
                            {
                                // Retrieve the articles as nodes.

                                string fetch_URL = fetch_baseURL + "&retstart=" + (i * retmax).ToString() + "&retmax=" + retmax.ToString();
                                fetch_URL += "&retmode=xml";
                                await FetchPubMedRecordsAsync(fetch_URL, res, source, saf_id);
                                Thread.Sleep(300);
                            }
                            catch (HttpRequestException e)
                            {
                                _logging_helper.LogError("In PubMed ProcessPMIDsListfromBanksAsync(): " + e.Message);
                            }
                        }
                    }

                    _logging_helper.LogLine("Processed " + totalRecords.ToString() + " from " + s.nlm_abbrev);
                }
            }
            return res;
        }


        public async Task<DownloadResult> ProcessPMIDsListfromDBSourcesAsync(Options opts, int saf_id, Source source)
        {
            DownloadResult res = new();
            XmlSerializer post_xSerializer = new(typeof(ePostResult));
            XmlSerializer search_xSerializer = new(typeof(eSearchResult));
            string date_string = "";
            int string_num = 0;
            string web_env = "";
            int query_key = 0;
            ScrapingHelpers ch = new(_logging_helper);

            try
            {
                // Set up bases of search strings

                string post_baseURL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/epost.fcgi?db=pubmed";
                post_baseURL += api_key;
                string search_baseURL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi?db=pubmed";
                search_baseURL += api_key;

                if (opts.FetchTypeId == 114)
                {
                    string today = DateTime.Now.ToString("yyyy/MM/dd");
                    string cutoff = "2020/01/01";   // normally replaced below
                    if (opts.CutoffDate is not null)
                    {
                        cutoff = ((DateTime)opts.CutoffDate).ToString("yyyy/MM/dd");
                    }
                    date_string = "&mindate=" + cutoff + "&maxdate=" + today + "&datetype=mdat";
                }
                string fetch_baseURL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=pubmed";
                fetch_baseURL += api_key;
                string post_URL, search_URL, fetch_URL;

                // Make a list of all PMIDs in the relevant DBs, 
                // as PMID strings 100 ids long. Then take each string
                // and post it to the Entry history server
                // getting back the web environment and query key parameters

                CreatePMIDsListfromSources();

                IEnumerable<string> idstrings = pubmed_repo.FetchSourcePMIDStrings();
                foreach (string idstring in idstrings)
                {
                    // Construct the post URL using the 100 Ids
                    string_num++;
                    post_URL = post_baseURL + "&id=" + idstring;
                    System.Threading.Thread.Sleep(200);
                    string? post_responseBody = await ch.GetAPIResponseAsync(post_URL);
                    if (post_responseBody is not null)
                    {
                        using (TextReader post_reader = new StringReader(post_responseBody))
                        {
                            // The eSearchResult class corresponds to the returned data.
                            ePostResult? post_result = (ePostResult?)post_xSerializer.Deserialize(post_reader);

                            if (post_result is not null)
                            {
                                // search the articles in these ids for recent revisions

                                query_key = post_result.QueryKey;
                                web_env = post_result.WebEnv;
                                if (date_string == "")
                                {
                                    // No need to search - fetch all 100 pubmed records immediately
                                    fetch_URL = fetch_baseURL + "&WebEnv=" + web_env + "&query_key=" + query_key.ToString();
                                    fetch_URL += "&retmax=100&retmode=xml";
                                    Thread.Sleep(200);
                                    await FetchPubMedRecordsAsync(fetch_URL, res, source, saf_id);
                                }
                                else
                                {
                                    // search for those that have been revised on or since the cutoff date

                                    search_URL = search_baseURL + "&term=%23" + query_key.ToString() + "+AND+" + date_string;
                                    search_URL += "&WebEnv=" + web_env + "&usehistory=y";

                                    Thread.Sleep(200);
                                    string? search_responseBody = await ch.GetAPIResponseAsync(search_URL);
                                    if (search_responseBody is not null)
                                    {
                                        int totalRecords = 0;

                                        // The eSearchResult class corresponds to the returned data.

                                        using TextReader search_reader = new StringReader(search_responseBody);
                                        eSearchResult? search_result = (eSearchResult?)search_xSerializer.Deserialize(search_reader);
                                        if (search_result is not null)
                                        {
                                            totalRecords = search_result.Count;
                                            query_key = search_result.QueryKey;
                                            web_env = search_result.WebEnv;

                                            if (totalRecords > 0)
                                            {
                                                fetch_URL = fetch_baseURL + "&WebEnv=" + web_env + "&query_key=" + query_key.ToString();
                                                fetch_URL += "&retmax=100&retmode=xml";
                                                Thread.Sleep(200);
                                                await FetchPubMedRecordsAsync(fetch_URL, res, source, saf_id);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (string_num % 10 == 0) _logging_helper.LogLine(string_num.ToString() + " lines checked");
                }

                return res;
            }

            catch (HttpRequestException e)
            {
                _logging_helper.LogError("In PubMed ProcessPMIDsListfromDBSourcesAsync(): " + e.Message);
                return res;
            }

        }


        public void CreatePMIDsListfromSources()
        {
            // Establish tables and support objects.

            pubmed_repo.SetUpTempPMIDsBySourceTables();
            CopyHelpers helper = new();
            IEnumerable<PMIDBySource> references;

            // Loop through the study databases that hold
            // study_reference tables, i.e. with pmid ids
            // this is not cutoff date sensitive as last revised date
            // not known at this time - has to be checked later.

            IEnumerable<Source> sources = pubmed_repo.FetchSourcesWithReferences();
            foreach (Source s in sources)
            {
                references = pubmed_repo.FetchSourceReferences(s.database_name!);
                pubmed_repo.StorePmidsBySource(helper.source_ids_helper, references);
            }
            pubmed_repo.CreatePMID_IDStrings();
        }


        public async Task FetchPubMedRecordsAsync(string fetch_URL, DownloadResult res, Source source, int saf_id)
        {
            ScrapingHelpers ch = new(_logging_helper);
            string? responseBody = await ch.GetAPIResponseAsync(fetch_URL);
            if (responseBody is not null)
            {
                XmlWriterSettings xml_settings = new()
                {
                    Async = true,
                    Encoding = System.Text.Encoding.UTF8
                };

                XmlDocument xdoc = new();
                xdoc.LoadXml(responseBody);
                XmlNodeList articles = xdoc.GetElementsByTagName("PubmedArticle");
                foreach (XmlNode article in articles)
                {
                    string? pmid = article.SelectSingleNode("MedlineCitation/PMID")?.InnerText;
                    if (!string.IsNullOrEmpty(pmid) && Int32.TryParse(pmid, out int ipmid))
                    {
                        // get current or new file download record, calculate
                        // and store last revised date. Write new or replace
                        // file and update file_record (by ref).

                        res.num_checked++;
                        DateTime? last_revised_datetime = GetDateLastRevised(article);
                        ObjectFileRecord? file_record = _mon_data_layer.FetchObjectFileRecord(pmid, source.id);
                        if (file_record is null)
                        {
                            string remote_url = "https://www.ncbi.nlm.nih.gov/pubmed/" + pmid;
                            file_record = new(source.id, pmid, remote_url, saf_id);
                            file_record.last_revised = last_revised_datetime;

                            WriteNewFile(article, ipmid, file_record, source, xml_settings);

                            _mon_data_layer.InsertObjectFileRec(file_record);
                            res.num_added++;
                            res.num_downloaded++;
                        }
                        else
                        {
                            file_record.last_saf_id = saf_id;
                            file_record.last_revised = last_revised_datetime;

                            ReplaceFile(article, file_record, xml_settings);

                            _mon_data_layer.StoreObjectFileRec(file_record);
                            res.num_downloaded++;
                        }

                        if (res.num_checked % 100 == 0) _logging_helper.LogLine("Checked so far: " + res.num_checked.ToString());
                    }
                }
            }
        }


        private DateTime? GetDateLastRevised(XmlNode article)
        {
            DateTime? date_last_revised = null;

            string? year = article.SelectSingleNode("MedlineCitation/DateRevised/Year")?.InnerText;
            string? month = article.SelectSingleNode("MedlineCitation/DateRevised/Month")?.InnerText;
            string? day = article.SelectSingleNode("MedlineCitation/DateRevised/Day")?.InnerText;

            if (year is not null && month is not null && day is not null)
            {
                if (Int32.TryParse(year, out int iyear) 
                && Int32.TryParse(month, out int imonth)
                && Int32.TryParse(day, out int iday))
                {
                    date_last_revised = new DateTime(iyear, imonth, iday);
                }
            }
            return date_last_revised;
        }


        private void WriteNewFile(XmlNode article, int ipmid, ObjectFileRecord file_record, Source source, XmlWriterSettings xml_settings)
        {
            string folder_name = Path.Combine(source.local_folder!, "PM" + (ipmid / 10000).ToString("00000") + "xxxx");
            if (!Directory.Exists(folder_name))
            {
                Directory.CreateDirectory(folder_name);
            }
            string filename = "PM" + ipmid.ToString("000000000") + ".xml";
            string full_path = Path.Combine(folder_name, filename);

            using (XmlWriter writer = XmlWriter.Create(full_path, xml_settings))
            {
                article.WriteTo(writer);
            }

            file_record.local_path = full_path;
            file_record.download_status = 2;
            file_record.last_downloaded = DateTime.Now;
        }


        private void ReplaceFile(XmlNode article, ObjectFileRecord file_record, XmlWriterSettings xml_settings)
        {
            string? full_path = file_record.local_path;
            if (full_path is not null)
            {
                // ensure can over write
                if (File.Exists(full_path))
                {
                    File.Delete(full_path);
                }
                using (XmlWriter writer = XmlWriter.Create(full_path, xml_settings))
                {
                    article.WriteTo(writer);
                }
                file_record.last_downloaded = DateTime.Now;
            }
        }

    }
}
