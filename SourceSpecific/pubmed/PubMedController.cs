using System.Data;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;
using MDR_Downloader.biolincc;
using MDR_Downloader.Helpers;
using MDR_Downloader.isrctn;

namespace MDR_Downloader.pubmed;


// Pubmed searches and file downloads are often associated with filters...
// 10001	"PubMed CTG"	"PubMed abstracts with references to ClinicalTrials.gov entries"
// 10002	"PubMed COVID"	"PubMed abstracts found on searches related to COVID-19 (SARS, MERS etc.)"
// and especially...
// 10003	"PubMed Registries"	"PubMed abstracts with references to any trial registry"
// 10004	"Pubmed - Study References"	"Identifies PubMed references in Study sources that have not yet been downloaded"

// Pubmed data has two sources - the study references in certain databases
// and the pubmed records themselves - specifically those that have a reference to a 'databank' 
// (trial registry in this context)

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
    private readonly PubMed_Processor pubmed_processor;

    private readonly string api_key;
    private readonly string post_baseURL, search_baseURL, fetch_baseURL;


    public PubMed_Controller(MonDataLayer mon_data_layer, LoggingHelper logging_helper)
    {
        _logging_helper = logging_helper;
        _mon_data_layer = mon_data_layer;

        pubmed_repo = new(_logging_helper);
        pubmed_processor = new();

        // API key belongs to NCBI user stevecanhamn (steve.canham@ecrin.org,
        // stored in appsettings.json and accessed via the logging repo.

        api_key = "&api_key=" + _mon_data_layer.PubmedAPIKey;

        post_baseURL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/epost.fcgi?db=pubmed" + api_key;
        search_baseURL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi?db=pubmed" + api_key;
        fetch_baseURL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=pubmed" + api_key;
    }


    public async Task<DownloadResult> ObtainDatafromSourceAsync(Options opts, Source source)
    {
        DownloadResult res = new();
        string date_string = "";

        // If opts.FetchTypeId == 114 date_string is constructed, giving
        // min and max dates. If opts.FetchTypeId == 121 date_string remains "".

        if (opts.FetchTypeId == 114)
        {
            string today = DateTime.Now.ToString("yyyy/MM/dd");
            if (opts.CutoffDate is not null)
            {
                string cutoff = ((DateTime)opts.CutoffDate).ToString("yyyy/MM/dd");
                date_string = "&mindate=" + cutoff + "&maxdate=" + today + "&datetype=mdat";
            }
        }

        if (opts.FocusedSearchId == 10003)
        {
            // Download articles with references to trial registries, that
            // have been revised since the cutoff date.

            res = await ProcessPMIDsListfromBanksAsync(opts, source, date_string);
        }

        if (opts.FocusedSearchId == 10004)
        {
            // Download pmids listed as references in other sources,
            // that have been revised since the cutoff date.

            res = await ProcessPMIDsListfromDBSourcesAsync(opts, source, date_string);
        }

        return res;
    }


    // Normally, the 'databank' pmid records will be identified by an initial search, but include only those 
    // that have been modified since the last similar download (represented by the cutoff date). 
    // This includes new records.

    // The records found for each bank are stored in a table in the pp schema - pp.temp_by_bank_records.
    // These are transferred in turn to the pp.pmids_with_bank_records table

    // At the end of the loop through all banks the *distinct* pmid records are transfered to a list in memory.
    // The system loops through them - if the record exists it is replaced and the object source record
    // is updated in the mon database. if the record is new it is downloaded and a new object source record is created.
    // This strategy is represented by saf_type 114 (with cutoff date, and filter 10003)

    // A variant allows all pmids with bank records to bne downloaded, irrespective of date, but otherwise 
    // the process is the same. This strategy is represented by saf_type 121 Filtered records (download) (with filter 10003)

    public async Task<DownloadResult> ProcessPMIDsListfromBanksAsync(Options opts, Source source, string date_string)
    {
        DownloadResult res = new();
        ScrapingHelpers ch = new(_logging_helper);

        // Get list of potential linked data banks (includes trial registries).

        IEnumerable<PMSource> banks = pubmed_repo.FetchDatabanks();
        string web_env = "";
        int query_key = 0;

        foreach (PMSource s in banks)
        {
            // Use databank details to construct search string
            // if no cutoff date (t = 121) date_string is "".
            Thread.Sleep(300); 
            string search_term = "&term=" + s.nlm_abbrev + "[SI]" + date_string;
            string search_url = search_baseURL + search_term + "&usehistory=y";

            // Get the number of total records that have this databank reference
            // and that (usually) have been revised recently 
            // and calculate the loop parameters.

            int totalRecords = 0;
            string? search_responseBody = await ch.GetAPIResponseAsync(search_url);
            if (search_responseBody is not null)
            {
                eSearchResult? search_result = Deserialize<eSearchResult?>(search_responseBody);
                if (search_result is not null)
                {
                    totalRecords = search_result.Count;
                    query_key = search_result.QueryKey;
                    web_env = search_result.WebEnv;
                }
            }

            // loop through the records and obtain and store relevant
            // records, of PubMed Ids, retmax (= 100) at a time     

            if (totalRecords > 0)
            {
                int retmax = 100;
                string fetch_Url = fetch_baseURL + "&WebEnv=" + web_env + "&query_key=" + query_key.ToString();
                int numCallsNeeded = (int)(totalRecords / retmax) + 1;
                for (int i = 0; i < numCallsNeeded; i++)
                {
                    try
                    {
                        // Retrieve the articles as nodes.

                        fetch_Url += "&retstart=" + (i * retmax).ToString() + "&retmax=" + retmax.ToString();
                        fetch_Url += "&retmode=xml";
                        await FetchPubMedRecordsAsync(fetch_Url, res, source, (int)opts.saf_id!);
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

        return res;
    }

    // The study_reference records are collected - one database at a time - and transferred to a single database.
    // At the end of that process a distinct list is created in memory.
    // The system loops through this 10 records at a time. If there is no object source record one is created 
    // and the file is downloaded - it is new to the system.
    // If a source record exists the record needs checking to see if it has been revised since the last similar exercise.
    // If it has the record is downloaded and the source file is updated. If not, no download takes place. 
    // This strategy is represented by saf_type 114 (with cutoff date, and filter 10004)

    // A variant allows all pmids derived from study references, irrespective of revision date, to be downloaded.
    // This strategy is represented by saf_type 121 Filtered records (download) (with filter 10004)

    public async Task<DownloadResult> ProcessPMIDsListfromDBSourcesAsync(Options opts, Source source, string date_string)
    {
        DownloadResult res = new();
        string web_env = "";
        int query_key = 0;
        ScrapingHelpers ch = new(_logging_helper);
        CopyHelpers helper = new();

        try
        {
            // Establish tables and support objects to support
            // the PMIDs found in each source database with References.
            // Loop through those databases and deposit pmids in the
            // pp.pmids_by_source_total table. This is not sensitive to a 
            // cutoff date as the last revised date is not known at this time
            // - has to be checked later.

            string post_URL, search_URL, fetch_URL;
            pubmed_repo.SetUpTempPMIDsBySourceTables();
            IEnumerable<Source> sources = pubmed_repo.FetchSourcesWithReferences();
            foreach (Source s in sources)
            {
                IEnumerable<PMIDBySource> references = pubmed_repo.FetchSourceReferences(s.database_name!);
                pubmed_repo.StorePmidsBySource(helper.source_ids_helper, references);
            }

            // Groups the ids into lists of a 100 (max) each, in  
            // pp.pmid_id_strings table.

            pubmed_repo.CreatePMID_IDStrings();

            // Then take each string
            // and post it to the Entry history server
            // getting back the web environment and query key parameters

            int string_num = 0;
            IEnumerable<string> idstrings = pubmed_repo.FetchSourcePMIDStrings();
            foreach (string idstring in idstrings)
            {
                string_num++;
                post_URL = post_baseURL + "&id=" + idstring;
                Thread.Sleep(300);

                string? post_responseBody = await ch.GetAPIResponseAsync(post_URL);
                if (post_responseBody is not null)
                {
                    ePostResult? post_result = Deserialize<ePostResult?>(post_responseBody);
                    {
                        if (post_result is not null)
                        {
                            query_key = post_result.QueryKey;
                            web_env = post_result.WebEnv;

                            if (date_string == "")
                            {
                                // (t = 121) No need to search - fetch all 100 pubmed records immediately.

                                fetch_URL = fetch_baseURL + "&WebEnv=" + web_env + "&query_key=" + query_key.ToString();
                                fetch_URL += "&retmax=100&retmode=xml";
                                Thread.Sleep(200);
                                await FetchPubMedRecordsAsync(fetch_URL, res, source, (int)opts.saf_id!);
                            }
                            else
                            {
                                // (t = 114) Search for those that have been revised on or since the cutoff date.

                                search_URL = search_baseURL + "&term=%23" + query_key.ToString() + "+AND+" + date_string;
                                search_URL += "&WebEnv=" + web_env + "&usehistory=y";

                                Thread.Sleep(200);
                                int totalRecords = 0;
                                string? search_responseBody = await ch.GetAPIResponseAsync(search_URL);
                                eSearchResult? search_result = Deserialize<eSearchResult?>(search_responseBody);

                                // The eSearchResult class corresponds to the returned data.

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
                                        await FetchPubMedRecordsAsync(fetch_URL, res, source, (int)opts.saf_id!);
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


    public async Task FetchPubMedRecordsAsync(string fetch_URL, DownloadResult res, Source source, int saf_id)
    {
        ScrapingHelpers ch = new(_logging_helper);
        string? responseBody = await ch.GetAPIResponseAsync(fetch_URL);
        if (responseBody is not null)
        {
            PubmedArticleSet? search_result = Deserialize<PubmedArticleSet?>(responseBody);
            if (search_result is not null)
            {
                var articles = search_result.PubmedArticles;
                if (articles?.Any() == true)
                {
                    //XmlDocument xdoc = new();
                    //xdoc.LoadXml(responseBody);
                    //XmlNodeList articles = xdoc.GetElementsByTagName("PubmedArticle");
                    foreach (PubmedArticle article in articles)
                    {
                        int? ipmid = article.MedlineCitation?.PMID?.Value;
                        string? pmid = ipmid.ToString();
                        FullObject s = pubmed_processor.ProcessData(article);
                        
                        if (!string.IsNullOrEmpty(pmid))
                        {
                            // get current or new file download record, calculate
                            // and store last revised date. Write new or replace
                            // file and update file_record (by ref).

                            res.num_checked++;
                            //DateTime? last_revised_datetime = GetDateLastRevised(article);
                            ObjectFileRecord? file_record = _mon_data_layer.FetchObjectFileRecord(pmid, source.id);
                                                        /*
                            string full_path = await WriteOutFile(article, ipmid, file_record, source);

                            if (full_path != "error")
                            {
                                string remote_url = "https://www.isrctn.com/" + s.sd_sid;
                                DateTime? last_updated = s.lastUpdated?.FetchDateTimeFromISO();
                                bool added = _mon_data_layer.UpdateObjectDownloadLog(source_id, s.sd_sid, remote_url, saf_id,
                                                        last_updated, full_path);
                                res.num_downloaded++;
                                if (added) res.num_added++;
                            }


                            if (file_record is null)
                            {
                                string remote_url = "https://www.ncbi.nlm.nih.gov/pubmed/" + pmid;
                                file_record = new(source.id, pmid, remote_url, saf_id);
                                file_record.last_revised = last_revised_datetime;

                                WriteOutFile(article, ipmid, file_record, source);

                                _mon_data_layer.InsertObjectFileRec(file_record);
                                res.num_added++;
                                res.num_downloaded++;
                            }
                            else
                            {
                                file_record.last_saf_id = saf_id;
                                file_record.last_revised = last_revised_datetime;

                                ReplaceFile(article, file_record)

                                _mon_data_layer.StoreObjectFileRec(file_record);
                                res.num_downloaded++;
                            }
                            //

                            if (res.num_checked % 100 == 0) _logging_helper.LogLine("Checked so far: " + res.num_checked.ToString());
                        }
                        */
                        }
                    }
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


        // Writes out the file with the correct name to the correct folder, as indented json.
        // Called from the FetchPubMedRecordsAsync function.
        // Returns the full file path as constructed, or an 'error' string if an exception occurred.

        /*
        private async Task<string> WriteOutFile(Study s, int ipmid, string file_base)
        {
            string folder_name = Path.Combine(file_base, "PM" + (ipmid / 10000).ToString("00000") + "xxxx");
            if (!Directory.Exists(folder_name))
            {
                Directory.CreateDirectory(folder_name);
            }
            string filename = "PM" + ipmid.ToString("000000000") + ".json";
            string full_path = Path.Combine(file_base, file_name!);
            try
            {
                using FileStream jsonStream = File.Create(full_path);
                await JsonSerializer.SerializeAsync(jsonStream, s, _json_options);
                await jsonStream.DisposeAsync();
                return full_path;
            }
            catch (Exception e)
            {
                _logging_helper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
                return "error";
            }
        }
        */

        /*
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
        */

        // General XML Deserialize function.

    private T? Deserialize<T>(string? inputString)
    {
        if (inputString is null)
        {
            return default;
        }

        T? instance = default;
        try
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using (var stringreader = new StringReader(inputString))
            {
                instance = (T?)xmlSerializer.Deserialize(stringreader);
            }
        }
        catch (Exception e)
        {
            _logging_helper.LogCodeError("Error when deserialising " + inputString[0..1000], e.Message, e.StackTrace);
            return default;
        }

        return instance;
    }
}


