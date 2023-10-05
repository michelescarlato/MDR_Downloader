using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Serialization;
using MDR_Downloader.Helpers;

namespace MDR_Downloader.pubmed;

// Pubmed searches and file downloads are usually associated with filters, especially...
// 10003 PubMed Registries - PubMed abstracts with references to any trial registry
// 10004 Pubmed - Study References: Identifies PubMed references in Study sources that have not yet been downloaded

// This is because Pubmed data has two sources - the study references in certain databases
// and the pubmed records themselves - specifically those that have a reference to a 'databank' 
// (a trial registry in this context). Further details are provided above each of the main methods.

// For details on using the NLM / NCBI 'entrez' API systems, see https://www.ncbi.nlm.nih.gov/books/NBK25501/

// If t= 302 NOT a normal download, but a call to get publisher data from NLM.

public class PubMed_Controller : IDLController
{
    private readonly IMonDataLayer _monDataLayer;
    private readonly ILoggingHelper _loggingHelper;
    private readonly PubMedDataLayer _pubmedRepo;

    private readonly JsonSerializerOptions? _jsonOptions;
    private readonly string postBaseURL;
    private readonly string searchBaseURL;
    private readonly string fetchBaseURL;
    private readonly string postNlmBaseURL;
    private readonly string searchNlmBaseURL;
    private readonly string fetchNlmBaseURL;

    private readonly ScrapingHelpers ch;
    private readonly CopyHelpers helper;

    public PubMed_Controller(IMonDataLayer monDataLayer, ILoggingHelper loggingHelper)
    {
        _monDataLayer = monDataLayer;
        _loggingHelper = loggingHelper;
        _pubmedRepo = new(monDataLayer.Credentials);
        _jsonOptions = new()
        {
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        ch = new(_loggingHelper);
        helper = new();

        string apiKey = "&api_key=" + _monDataLayer.PubmedAPIKey;
        string entrez_base = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/";
        postBaseURL = entrez_base + "epost.fcgi?db=pubmed" + apiKey;
        searchBaseURL = entrez_base + "esearch.fcgi?db=pubmed" + apiKey;
        fetchBaseURL = entrez_base + "efetch.fcgi?db=pubmed" + apiKey;
        postNlmBaseURL = entrez_base + "epost.fcgi?db=nlmcatalog" + apiKey;
        searchNlmBaseURL = entrez_base + "esearch.fcgi?db=nlmcatalog" + apiKey;
        fetchNlmBaseURL = entrez_base + "efetch.fcgi?db=nlmcatalog" + apiKey;
    }


    public async Task<DownloadResult> ObtainDataFromSourceAsync(Options opts, Source source)
    {
        DownloadResult res = new();
        string date_string = "";
        string? file_base = source.local_folder;
        if (file_base is null)
        {
            _loggingHelper.LogError("Null value passed for local folder value for this source");
            return new DownloadResult();   // return zero result
        }

        // If opts.FetchTypeId == 114 date_string is constructed, giving
        // min and max dates. If opts.FetchTypeId == 121 date_string remains as "".

        if (opts.FetchTypeId == 302)
        {
            res = await GetPublisherDataAsync(opts, source);
        }
        else
        {
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

                res = await ProcessPMIDsListFromBanksAsync(opts, source, date_string);
            }

            if (opts.FocusedSearchId == 10004)
            {
                // Download pmids listed as references in other sources,
                // that have been revised since the cutoff date.

                res = await ProcessPMIDsListFromDBSourcesAsync(opts, source, date_string);
            }
        }

        return res;
    }


    //********************************************************************************************************
    //               USING DATABASE REFERENCES DATA TO OBTAIN PUBMED RECORDS              (-q 10004)
    //********************************************************************************************************

    // The study_reference records are collected - one database at a time - and transferred to a single table.
    // The pmids are aggregated into strings of 100, which are presented to the PubMed server to see if any have 
    // been revised or added since the supplied cut-off date (if no date, all records are downloaded).
    // Revised or new records are downloaded. This is dl_type 114 (with cutoff date) or dl_type 121 (with no
    // cutoff date) - both also requiring filter type 10004.

    public async Task<DownloadResult> ProcessPMIDsListFromDBSourcesAsync(Options opts, Source source,
        string date_string)
    {
        DownloadResult res = new();
        try
        {
            // Establish tables and support objects to support the PMIDs found in each source database
            // with References. Loop through those databases and deposit pmids in the mn.pmids_by_source_total
            // table. This initial stage is not sensitive to a cutoff date - all references are aggregated.

            _pubmedRepo.SetUpTempPMIDsBySourceTables();
            IEnumerable<Source> sources = _pubmedRepo.FetchSourcesWithReferences();
            foreach (Source s in sources)
            {
                IEnumerable<PMIDBySource> references = _pubmedRepo.FetchSourceReferences(s.database_name!, s.id);
                _pubmedRepo.StorePmidsBySource(helper.dbrefs_ids_helper, references);
            }

            // Group the ids into lists of a 100 (max) each, in mn.pmid_id_strings table and then in memory. 

            _pubmedRepo.CreateDBRef_IDStrings();
            IEnumerable<string> id_strings = _pubmedRepo.FetchSourcePMIDStrings();

            // Take each string and post it to the Entry history server, getting back the web environment
            // and query key parameters that can then be used to reference those ids on the server.

            int string_num = 0;
            foreach (string id_string in id_strings)
            {
                string_num++;
                string postUrl = postBaseURL + "&id=" + id_string;
                Thread.Sleep(300);
                string? post_responseBody = await ch.GetAPIResponseWithRetriesAsync(postUrl, 1000,
                                             id_string.Length > 30 ? id_string[..30] : id_string);
                if (post_responseBody is null)
                {
                    _loggingHelper.LogError($"Null post result received, with {postUrl}");
                }
                else
                {
                    ePostResult? post_result = Deserialize<ePostResult?>(post_responseBody, _loggingHelper);
                    {
                        if (post_result is not null)
                        {
                            // The 100 PMIDs have been successfully uploaded to the Entrez system.
                            // Get parameters required to reference them.

                            int query_key = post_result.QueryKey;
                            string? web_env = post_result.WebEnv;
                            if (web_env is not null)
                            {
                                string fetchUrl;
                                if (date_string == "")
                                {
                                    // (t = 121) No need to search - fetch all 100 pubmed records immediately.

                                    fetchUrl = fetchBaseURL + "&WebEnv=" + web_env + "&query_key=" + query_key;
                                    fetchUrl += "&retmax=100&retmode=xml";
                                    Thread.Sleep(300);
                                    await FetchPubMedRecordsAsync(fetchUrl, res, (int)opts.dl_id!,
                                        source.local_folder!);
                                }
                                else
                                {
                                    // (t = 114) Search for those that have been revised on or since the cutoff date.

                                    string searchUrl = searchBaseURL + "&term=%23" + query_key + "+AND+" +
                                                       date_string;
                                    searchUrl += "&WebEnv=" + web_env + "&usehistory=y";
                                    Thread.Sleep(300);
                                    string? search_responseBody = await ch.GetAPIResponseWithRetriesAsync(searchUrl,
                                        1000, id_string.Length > 30 ? id_string[..30] : id_string);
                                    eSearchResult? search_result = Deserialize<eSearchResult?>(search_responseBody,
                                                                   _loggingHelper);

                                    // The eSearchResult class corresponds to the returned data (list of PMIDs).
                                    // They can be referenced using the query key / web environment parameters
                                    // returned with the search result, so these are used in the fetch string.

                                    if (search_result is not null)
                                    {
                                        int totalRecords = search_result.Count;
                                        query_key = search_result.QueryKey;
                                        web_env = search_result.WebEnv;
                                        if (totalRecords > 0 && web_env is not null)
                                        {
                                            fetchUrl = fetchBaseURL + "&WebEnv=" + web_env + "&query_key=" + query_key;
                                            fetchUrl += "&retmax=100&retmode=xml";
                                            Thread.Sleep(200);
                                            await FetchPubMedRecordsAsync(fetchUrl, res, (int)opts.dl_id!,
                                                source.local_folder!);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (string_num % 10 == 0) _loggingHelper.LogLine($"{string_num} lines of (up to 100) Ids processed");
            }
            return res;
        }

        catch (HttpRequestException e)
        {
            _loggingHelper.LogError("In PubMed ProcessPMIDsListFromDBSourcesAsync(): " + e.Message);
            return res;
        }
    }


    //********************************************************************************************************
    //        USING PUBMED DATA TO IDENTIFY RECORDS THAT REFERENCE TRIAL REGISTRIES              (-q 10003)
    //********************************************************************************************************

    // 'Databank' pmid records are identified by an API call, one for each bank (trial registry), and usually
    // include only those that have been modified or added since the last similar download (t= 114). 
    // The records found for each bank are stored in a table in the DB These are compared with those 
    // already imported from the db references tables. Only those not pmids not already in the system, i.e. 
    // not in those tables, are downloaded from pubmed (about 20% of the total found). The same fetch mechanism
    // (downloading a 100 at a time after posting 100 pmids on the server) is followed as for the q=10004 method above.

    // It is therefore IMPORTANT that this method runs soon after the method above, that uses -q = 100004.
    // It may be that in the future the two methods will be combined.

    // A variant allows all pmids with bank records to bne downloaded, irrespective of date, but otherwise 
    // the process is the same (t = 121). 

    // There is a small chance that a study-pubmed link that is not listed in the db references,
    // but is listed in the pubmed bank linkage data, will be missed using this approach, if the pubmed data
    // includes some but not of the relevant links. This small risk of loss of data is seen as worth it
    // given the relative efficiency of this method.

    public async Task<DownloadResult> ProcessPMIDsListFromBanksAsync(Options opts, Source source, string date_string)
    {
        // Establish relevant temporary tables, get list of potential linked data banks
        // (only includes trial registries) and then get linked PMIDs for each

        DownloadResult res = new();
        _pubmedRepo.SetUpTempPMIDsByBankTables();
        IEnumerable<PMSource> banks = _pubmedRepo.FetchDatabanks();
        foreach (PMSource s in banks)
        {
            // Use databank details to construct search string. If no cutoff date (t = 121) date_string is "".
            // Get the number of total records that have this databank reference
            // and that (in most cases) have been revised recently. 

            string search_term = "&term=" + s.nlm_abbrev + "[SI]" + date_string;
            await GetBankPMIDsIntoDatabase(opts, search_term, s.id, date_string);
        }

        // DB has all the relevant PMIDs - registry links - create a table with
        // only the ones not already in DB references tables (if they are in these tables
        // they should have been already downloaded if they have been revised or added in the period).

        _pubmedRepo.CreatePMBanks_IDStrings();

        // The pmbanks_id_strings now has the data for the id strings to be presented for fetching. All 
        // of the listed pubmed ids must be fetched, 100 at a time.

        IEnumerable<string> id_strings = _pubmedRepo.FetchBankPMIDStrings();
        int string_num = 0;
        foreach (string id_string in id_strings)
        {
            string_num++;
            string postUrl = postBaseURL + "&id=" + id_string;
            Thread.Sleep(300);
            string? post_responseBody = await ch.GetAPIResponseWithRetriesAsync(postUrl, 1000,
                                        id_string.Length > 30 ? id_string[..30] : id_string);
            if (post_responseBody is null)
            {
                _loggingHelper.LogError($"Null post result received, with {postUrl}");
            }
            else
            {
                ePostResult? post_result = Deserialize<ePostResult?>(post_responseBody, _loggingHelper);
                {
                    if (post_result is not null)
                    {
                        // The 100 PMIDs have been successfully uploaded to the Entrez system.
                        // Get parameters required to reference them.

                        int query_key = post_result.QueryKey;
                        string? web_env = post_result.WebEnv;
                        if (web_env is not null)
                        {
                            string fetchUrl = fetchBaseURL + "&WebEnv=" + web_env + "&query_key=" + query_key;
                            fetchUrl += "&retmax=100&retmode=xml";
                            Thread.Sleep(300);
                            await FetchPubMedRecordsAsync(fetchUrl, res, (int)opts.dl_id!, source.local_folder!);
                        }
                    }
                }
            }
        }

        if (string_num % 10 == 0) _loggingHelper.LogLine($"{string_num} lines of (up to 100) Ids processed");
        return res;
    }


    private async Task<int> GetBankPMIDsIntoDatabase(Options opts, string search_base, int? source_id,
                                                     string date_string)
    {
        string search_term = search_base + date_string;
        int totalRecords = await GetPMIDsBYSourceAndPeriod(search_term, source_id);
        if (totalRecords <= 10000)
        {
            return totalRecords;
        }

        // Download will not have taken place - need to retry with a smaller period...

        int overall_total = 0;
        DateTime mindate, maxdate;
        string min_datestring, max_datestring;
        if (date_string == "")
        {
            // Here use edat (not mdat) to get ALL records
            // do initial ten years separately.

            mindate = new DateTime(1995, 1, 1);
            maxdate = new DateTime(2004, 12, 31);
            min_datestring = mindate.ToString("yyyy/MM/dd");
            max_datestring = maxdate.ToString("yyyy/MM/dd");
            date_string = "&mindate=" + min_datestring
                                      + "&maxdate=" + max_datestring + "&datetype=edat";
            search_term = search_base + date_string;
            overall_total = await GetPMIDsBYSourceAndPeriod(search_term, source_id);

            // then do by year (but unlikely to work with CGT for later years).
            // After 2010 done by months - necessary for CGT!

            for (int y = 2005; y < 2011; y++)
            {
                min_datestring = new DateTime(y, 1, 1).ToString("yyyy/MM/dd");
                max_datestring = new DateTime(y + 1, 12, 31).ToString("yyyy/MM/dd");
                search_term = search_base + "&mindate=" + min_datestring
                                            + "&maxdate=" + max_datestring + "&datetype=edat";
                overall_total += await GetPMIDsBYSourceAndPeriod(search_term, source_id);
            }

            for (int y = 2019; y < DateTime.Now.Year + 1; y++)
            {
                for (int m = 1; m < 13; m++)
                {
                    if (y == DateTime.Now.Year && m > DateTime.Now.Month)
                    {
                        break;
                    }
                    min_datestring = new DateTime(y, m, 1).ToString("yyyy/MM/dd");
                    max_datestring = new DateTime(y, m, 1).AddMonths(1).AddDays(-1).ToString("yyyy/MM/dd");
                    search_term = search_base + "&mindate=" + min_datestring
                                  + "&maxdate=" + max_datestring + "&datetype=edat";
                    overall_total += await GetPMIDsBYSourceAndPeriod(search_term, source_id);
                }
            }

        }
        else
        {
            // usually at least a few months (rather than the default week), even with CGT 
            // split by week of date of entry. Get cutoff date and increment by 7 days.
            // Still use mdat to only get modified or new records.

            if (opts.CutoffDate is not null)
            {
                mindate = (DateTime)opts.CutoffDate;
                min_datestring = mindate.ToString("yyyy/MM/dd");

                while (mindate <= DateTime.Now)
                {
                    maxdate = mindate = mindate.AddDays(6);
                    max_datestring = maxdate.ToString("yyyy/MM/dd");
                    date_string = "&mindate=" + min_datestring
                                              + "&maxdate=" + max_datestring + "&datetype=mdat";
                    search_term = search_base + date_string;
                    overall_total += await GetPMIDsBYSourceAndPeriod(search_term, source_id);
                    mindate = mindate.AddDays(7);
                }
            }
        }
        return overall_total;
    }


    private async Task<int> GetPMIDsBYSourceAndPeriod(string search_term, int? source_id)
    {
        string searchUrl = searchBaseURL + search_term;
        string? search_responseBody = await ch.GetAPIResponseAsync(searchUrl);
        if (search_responseBody is null)
        {
            _loggingHelper.LogLine("No response obtained for initial search query: " + search_term);
            return 0;
        }
        var search_result = Deserialize<eSearchResult?>(search_responseBody, _loggingHelper);
        if (search_result is null)
        {
            _loggingHelper.LogLine("Unable to deserialise result from initial search term:" + search_term);
            return 0;
        }
        if (search_result.Count == 0)
        {
            _loggingHelper.LogLine("No records found when using search term:" + search_term);
            return 0;
        }
        int totalRecords = search_result.Count;
        if (totalRecords > 10000)
        {
            _loggingHelper.LogLine("More than 10000 records (" + totalRecords
                                   + ") found from search term:" + search_term);
        }
        else
        {
            // Ids should be retrievable - download and store in DB.

            searchUrl += $"&retmax={totalRecords}";
            search_responseBody = await ch.GetAPIResponseAsync(searchUrl);
            search_result = Deserialize<eSearchResult?>(search_responseBody, _loggingHelper);
            if (search_result is not null && search_result.Count > 0)
            {
                int[]? idlist = search_result.IdList;
                if (idlist?.Any() is true)
                {
                    List<BankPmid> bank_pmids = new();
                    foreach (int i in idlist)
                    {
                        bank_pmids.Add(new BankPmid(source_id, i.ToString()));
                    }

                    _pubmedRepo.StorePmidsByBank(helper.pnbank_ids_helper, bank_pmids);
                }
            }
            _loggingHelper.LogLine(totalRecords + " retrieved and stored for source " + source_id);
        }
        return totalRecords;
    }


    public async Task FetchPubMedRecordsAsync(string fetch_URL, DownloadResult res, int dl_id, string file_base)
    {
        string? responseBody = await ch.GetAPIResponseWithRetriesAsync(fetch_URL, 1000, fetch_URL);
        if (responseBody is null)
        {
            _loggingHelper.LogError($"Null fetch result received, with {fetch_URL}");
        }
        else
        {
            responseBody = EscapeHtmlTags(responseBody);
            PubmedArticleSet? search_result = Deserialize<PubmedArticleSet?>(responseBody, _loggingHelper);
            if (search_result is not null)
            {
                var articles = search_result.PubmedArticles;
                if (articles?.Any() is true)
                {
                    PubMed_Processor pubmed_processor = new(_pubmedRepo, _loggingHelper);
                    foreach (PubmedArticle article in articles)
                    {
                        // Send each pubmed article object, as obtained from the XML, to the 
                        // processor for conversion to the Full Object model structure. 
                        // Assuming successful, returned object is serialised as JSON
                        // and the monitor table updated accordingly.

                        res.num_checked++;
                        FullObject? fob = pubmed_processor.ProcessData(article);
                        if (fob is not null && !string.IsNullOrEmpty(fob.sd_oid))
                        {
                            // ?? Here insert a lookup - for PMIDs originating in data sources - 
                            // Use that to change the object type in the database record before writing it out ??

                            string full_path = await WriteOutFile(fob, fob.ipmid, file_base);
                            if (full_path != "error")
                            {
                                string remote_url = "https://pubmed.ncbi.nlm.nih.gov/" + fob.sd_oid;
                                DateTime? last_revised_datetime = null;
                                int? year = fob.dateCitationRevised?.Year;
                                int? month = fob.dateCitationRevised?.Month;
                                int? day = fob.dateCitationRevised?.Day;
                                if (year.HasValue && month.HasValue && day.HasValue)
                                {
                                    last_revised_datetime = new DateTime((int)year, (int)month, (int)day);
                                }
                                bool added = _monDataLayer.UpdateObjectLog(fob.sd_oid, remote_url, dl_id,
                                                        last_revised_datetime, full_path);
                                res.num_downloaded++;
                                if (added) res.num_added++;
                            }
                        }
                    }
                    _loggingHelper.LogLine("Checked so far: " + res.num_checked);
                    _loggingHelper.LogLine("Downloaded so far: " + res.num_downloaded);
                    _loggingHelper.LogLine("Added so far: " + res.num_added);
                }
            }
        }
    }


    // Writes out the file with the correct name to the correct folder, as indented json.
    // Called from the FetchPubMedRecordsAsync function.
    // Returns the full file path as constructed, or an 'error' string if an exception occurred.

    private async Task<string> WriteOutFile(FullObject fob, int ipmid, string fileBase)
    {
        string folder_name = Path.Combine(fileBase, "PM" + (ipmid / 100000).ToString("0000") + "xxxxx");
        if (!Directory.Exists(folder_name))
        {
            Directory.CreateDirectory(folder_name);
        }
        string file_name = "PM" + ipmid.ToString("000000000") + ".json";
        string full_path = Path.Combine(folder_name, file_name);
        try
        {
            await using FileStream jsonStream = File.Create(full_path);
            await JsonSerializer.SerializeAsync(jsonStream, fob, _jsonOptions);
            await jsonStream.DisposeAsync();

            if (_monDataLayer.IsTestObject(fob.sd_oid))
            {
                // write out copy of the file in the test folder
                string test_path = _loggingHelper.TestFilePath;
                string full_test_path = Path.Combine(test_path, file_name);
                await using FileStream jsonStream2 = File.Create(full_test_path);
                await JsonSerializer.SerializeAsync(jsonStream2, fob, _jsonOptions);
                await jsonStream2.DisposeAsync();
            }

            return full_path;
        }
        catch (Exception e)
        {
            _loggingHelper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
            return "error";
        }
    }

    //********************************************************************************************************
    //                                   OBTAIN NLM PUBLISHER DETAILS         
    //********************************************************************************************************

    public async Task<DownloadResult> GetPublisherDataAsync(Options opts, Source source)
    {
        DownloadResult res = new();
        try
        {
            // Establish tables to support the UIDs found in the NLM Catalog, and then do an initial
            // search to discover the total number of records to be downloaded. Obtain the UIds in
            // batches of 1000 and store in mn.journal_uids table. Then group the ids into lists of
            // a 100 (max) each, in mn.journal_uid_strings table - retrieve and fetch on id string.

            _pubmedRepo.SetUpTempJournalDataTables();
            string searchUrl = searchNlmBaseURL + "&term=%22Periodical%22[Publication%20Type]";
            string? search_responseBody = await ch.GetAPIResponseWithRetriesAsync(searchUrl,
                                                            1000, "initial journal query");
            if (string.IsNullOrEmpty(search_responseBody))
            {
                return res; // zero result
            }
            eSearchResult? search_result = Deserialize<eSearchResult?>(search_responseBody,
                _loggingHelper);

            if (search_result is null)
            {
                return res; // zero result
            }

            int totalRecords = search_result.Count;
            for (int j = 0; j < totalRecords; j += 1000)
            {
                searchUrl = searchNlmBaseURL + $"&term=%22Periodical%22[Publication%20Type]&retstart={j}&retmax=1000";
                search_responseBody = await ch.GetAPIResponseWithRetriesAsync(searchUrl, 1000, "journal id query");
                if (!string.IsNullOrEmpty(search_responseBody))
                {
                    search_result = Deserialize<eSearchResult?>(search_responseBody, _loggingHelper);
                    if (search_result is not null)
                    {
                        int[]? idlist = search_result.IdList;
                        if (idlist?.Any() is true)
                        {
                            var idlistAsString = idlist.Select(i => new JournalUID(i));
                            _pubmedRepo.StoreJournalUIDs(helper.journal_uids_helper, idlistAsString);
                        }
                    }
                }
                Thread.Sleep(300);
            }

            _pubmedRepo.CreateDJournal_IDStrings();
            _pubmedRepo.TruncatePeriodicalsTable();
            IEnumerable<string> id_strings = _pubmedRepo.FetchJournalIDStrings();

            // Take each string and post it to the Entry history server, getting back the web environment
            // and query key parameters that can then be used to reference those ids on the server.

            int string_num = 0;
            foreach (string id_string in id_strings)
            {
                string_num++;
                string postUrl = postNlmBaseURL + "&id=" + id_string;
                Thread.Sleep(300);
                string? post_responseBody = await ch.GetAPIResponseWithRetriesAsync(postUrl, 1000,
                                             id_string.Length > 30 ? id_string[..30] : id_string);
                if (post_responseBody is null)
                {
                    _loggingHelper.LogError($"Null post result received, with {postUrl}");
                }
                else
                {
                    ePostResult? post_result = Deserialize<ePostResult?>(post_responseBody, _loggingHelper);
                    {
                        if (post_result is not null)
                        {
                            // The 100 UIDs have been successfully uploaded to the Entrez system.
                            // Get parameters required to reference them.

                            int query_key = post_result.QueryKey;
                            string? web_env = post_result.WebEnv;
                            if (web_env is not null)
                            {
                                string fetchUrl = fetchNlmBaseURL + "&WebEnv=" + web_env + "&query_key=" + query_key;
                                fetchUrl += "&retmax=100&retmode=xml";
                                Thread.Sleep(300);
                                await FetchNlmRecordsAsync(fetchUrl, res, (int)opts.dl_id!, source.local_folder!);
                            }
                        }
                    }
                }

                if (string_num % 10 == 0) _loggingHelper.LogLine($"{string_num} lines of (up to 100) Ids processed");
            }

            return res;
        }
        catch (HttpRequestException e)
        {
            _loggingHelper.LogError("In PubMed GetPublisherDataAsync(): " + e.Message);
            return res;
        }
    }

    public async Task FetchNlmRecordsAsync(string fetch_URL, DownloadResult res, int dl_id, string file_base)
    {
        string? responseBody = await ch.GetAPIResponseWithRetriesAsync(fetch_URL, 1000, fetch_URL);
        if (responseBody is null)
        {
            _loggingHelper.LogError($"Null fetch result received, with {fetch_URL}");
        }
        else
        {
            responseBody = EscapeCharacters(responseBody);
            NLMCatalogRecordSet? search_result = Deserialize<NLMCatalogRecordSet?>(responseBody, _loggingHelper);
            if (search_result is not null)
            {
                NLMRecord[]? recordset = search_result.NLMCatalogRecord;
                if (recordset?.Any() is true)
                {
                    PubMed_Processor pubmed_processor = new(_pubmedRepo, _loggingHelper);
                    foreach (NLMRecord rec in recordset)
                    {
                        // Send each pubmed article object, as obtained from the XML, to the 
                        // processor for conversion to a publisher details data object. 
                        // Assuming successful, returned object is stored in the database.

                        res.num_checked++;
                        Periodical? p = pubmed_processor.ProcessNLMData(rec);
                        if (p is not null)
                        {
                            _pubmedRepo.StorePublisherDetails(p);
                        }
                    }
                    _loggingHelper.LogLine("Checked so far: " + res.num_checked);
                    _loggingHelper.LogLine("Added so far: " + res.num_added);
                }
            }
        }
    }

    // General XML Deserialize function.

    private T? Deserialize<T>(string? inputString, ILoggingHelper logging_helper)
    {
        if (string.IsNullOrEmpty(inputString))
        {
            return default;
        }

        try
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using var string_reader = new StringReader(inputString);
            return (T?)xmlSerializer.Deserialize(string_reader);
        }
        catch (Exception e)
        {
            string error_heading = "Error when de-serialising ";
            error_heading += inputString.Length >= 750 ? inputString[..750] : inputString;
            logging_helper.LogCodeError(error_heading, e.Message, e.StackTrace);
            return default;
        }
    }


    private string? EscapeHtmlTags(string? inputString)
    {
        if (string.IsNullOrEmpty(inputString))
        {
            return null;
        }

        // Temp to help with debugging

        inputString = inputString.Replace("<PubmedArticle", "\n\n<PubmedArticle");
        inputString = inputString.Replace("<AuthorList", "\n<AuthorList");
        inputString = inputString.Replace("<ReferenceList", "\n<ReferenceList");

        // Mainly Required if we add extraction of abstracts - unable to do
        // so at the moment because of copyright restrictions.
        // There may be better ways of handling this problem!
        // But also needed for a few titles otherwise errors are thrown on deserialisation.

        inputString = inputString.Replace("<i>", "&lt;i&gt;");
        inputString = inputString.Replace("</i>", "&lt;/i&gt;");
        inputString = inputString.Replace("<b>", "&lt;b&gt;");
        inputString = inputString.Replace("</b>", "&lt;/b&gt;");
        inputString = inputString.Replace("<u>", "&lt;u&gt;");
        inputString = inputString.Replace("</u>", "&lt;/u&gt;");

        inputString = inputString.Replace("<sup>", "&lt;sup&gt;");
        inputString = inputString.Replace("</sup>", "&lt;/sup&gt;");
        inputString = inputString.Replace("<sub>", "&lt;sub&gt;");
        inputString = inputString.Replace("</sub>", "&lt;/sub&gt;");

        // Remove odd empty tags.

        inputString = inputString.Replace("<sup/>", "");
        inputString = inputString.Replace("<sub/>", "");

        return inputString;
    }


    private string? EscapeCharacters(string? inputString)
    {
        if (string.IsNullOrEmpty(inputString))
        {
            return null;
        }
        inputString = inputString.Replace("&", "&amp;");
        //inputString = inputString.Replace("\"", "&quote;");
        //inputString = inputString.Replace("'", "&apos;");

        return inputString;
    }
}

