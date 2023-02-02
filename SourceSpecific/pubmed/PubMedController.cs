using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Serialization;
using MDR_Downloader.Helpers;

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

public class PubMed_Controller : IDLController
{
    private readonly IMonDataLayer _monDataLayer;
    private readonly ILoggingHelper _loggingHelper;   
    private readonly PubMedDataLayer _pubmed_repo;
    
    private readonly JsonSerializerOptions? _json_options;    
    private string postBaseURL = "", searchBaseURL = "", fetchBaseURL = "";

    public PubMed_Controller(IMonDataLayer monDataLayer, ILoggingHelper loggingHelper)
    {
        _monDataLayer = monDataLayer;
        _loggingHelper = loggingHelper;
        _pubmed_repo = new(monDataLayer.Credentials);
        _json_options = new()
        {
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
    }

    public async Task<DownloadResult> ObtainDataFromSourceAsync(Options opts, Source source)
    {
        
        // API key belongs to NCBI user 'ECRINarthur' 
        // stored in appsettings.json and accessed via the logging repo.
        var apiKey = "&api_key=" + _monDataLayer.PubmedAPIKey;
        
        postBaseURL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/epost.fcgi?db=pubmed" + apiKey;
        searchBaseURL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi?db=pubmed" + apiKey;
        fetchBaseURL = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=pubmed" + apiKey;
        
        DownloadResult res = new();
        string date_string = "";
        string? file_base = source.local_folder;
        if (file_base is null)
        {
            _loggingHelper.LogError("Null value passed for local folder value for this source");
            return new DownloadResult();   // return zero result
        }

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

            res = await ProcessPMIDsListFromBanksAsync(opts, source, date_string);
        }

        if (opts.FocusedSearchId == 10004)
        {
            // Download pmids listed as references in other sources,
            // that have been revised since the cutoff date.

            res = await ProcessPMIDsListFromDBSourcesAsync(opts, source, date_string);
        }

        return res;
    }


    // Normally, the 'databank' pmid records will be identified by an initial search, but include only those 
    // that have been modified since the last similar download (represented by the cutoff date). 
    // This includes new records.

    // The records found for each bank are stored in a table in the pp schema - pp.temp_by_bank_records.
    // These are transferred in turn to the pp.pmids_with_bank_records table

    // At the end of the loop through all banks the *distinct* pmid records are transferred to a list in memory.
    // The system loops through them - if the record exists it is replaced and the object source record
    // is updated in the mon database. if the record is new it is downloaded and a new object source record is created.
    // This strategy is represented by saf_type 114 (with cutoff date, and filter 10003)

    // A variant allows all pmids with bank records to bne downloaded, irrespective of date, but otherwise 
    // the process is the same. This strategy is represented by saf_type 121 Filtered records (download) (with filter 10003)

    public async Task<DownloadResult> ProcessPMIDsListFromBanksAsync(Options opts, Source source, string date_string)
    {
        DownloadResult res = new();
        ScrapingHelpers ch = new(_loggingHelper);

        // Get list of potential linked data banks (includes trial registries).

        IEnumerable<PMSource> banks = _pubmed_repo.FetchDatabanks();
        string? web_env = "";
        int query_key = 0;

        foreach (PMSource s in banks)
        {
            // Use databank details to construct search string
            // if no cutoff date (t = 121) date_string is "".

            if (s.nlm_abbrev != "PACTR")
            {
                continue; // temp for testing
            }

            string search_term = "&term=" + s.nlm_abbrev + "[SI]" + date_string;
            string searchUrl = searchBaseURL + search_term + "&usehistory=y";

            // Get the number of total records that have this databank reference
            // and that (usually) have been revised recently 
            // and calculate the loop parameters.

            int totalRecords = 0;
            string? search_responseBody = await ch.GetAPIResponseAsync(searchUrl);
            if (search_responseBody is not null)
            {
                eSearchResult? search_result = Deserialize<eSearchResult?>(search_responseBody, _loggingHelper);
                if (search_result is not null)
                {
                    totalRecords = search_result.Count;
                    query_key = search_result.QueryKey;
                    web_env = search_result.WebEnv;
                }
            }

            // loop through the records and obtain and store relevant
            // records, of PubMed Ids, retmax (= 100) at a time     

            if (totalRecords > 0 && web_env is not null)
            {
                int retmax = 100;
                int numCallsNeeded = totalRecords / retmax + 1;
                for (int i = 0; i < numCallsNeeded; i++)
                {
                    try
                    {
                        // Retrieve the articles as nodes.
                        string fetchUrl = fetchBaseURL + "&WebEnv=" + web_env + "&query_key=" + query_key.ToString();
                        fetchUrl += "&retstart=" + (i * retmax).ToString() + "&retmax=" + retmax.ToString();
                        fetchUrl += "&retmode=xml";
                        await FetchPubMedRecordsAsync(fetchUrl, res, source, (int)opts.saf_id!, source.local_folder!);
                        Thread.Sleep(300);
                    }
                    catch (HttpRequestException e)
                    {
                        _loggingHelper.LogError("In PubMed ProcessPMIDsListFromBanksAsync(): " + e.Message);
                    }

                    if ((i + 1) * retmax < totalRecords)
                    {
                        _loggingHelper.LogLine($"Processed {(i + 1) * retmax} records from {s.nlm_abbrev}");
                    }
                }
            }
            _loggingHelper.LogLine($"Processed {totalRecords} from {s.nlm_abbrev}");
            Thread.Sleep(800);
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

    public async Task<DownloadResult> ProcessPMIDsListFromDBSourcesAsync(Options opts, Source source, 
        string date_string)
    {
        DownloadResult res = new();
        ScrapingHelpers ch = new(_loggingHelper);
        CopyHelpers helper = new();

        try
        {
            // Establish tables and support objects to support
            // the PMIDs found in each source database with References.
            // Loop through those databases and deposit pmids in the
            // pp.pmids_by_source_total table. This initial stage is not sensitive to a 
            // cutoff date as the last revised date is not known at this time
            // - has to be checked later.

            _pubmed_repo.SetUpTempPMIDsBySourceTables();
            IEnumerable<Source> sources = _pubmed_repo.FetchSourcesWithReferences();
            foreach (Source s in sources)
            {
                IEnumerable<PMIDBySource> references = _pubmed_repo.FetchSourceReferences(s.database_name!);
                _pubmed_repo.StorePmidsBySource(helper.source_ids_helper, references);
            }

            // Groups the ids into lists of a 100 (max) each, in  
            // pp.pmid_id_strings table.

            _pubmed_repo.CreatePMID_IDStrings();

            // Then take each string
            // and post it to the Entry history server
            // getting back the web environment and query key parameters

            int string_num = 0;
            IEnumerable<string> id_strings = _pubmed_repo.FetchSourcePMIDStrings();
            foreach (string id_string in id_strings)
            {
                string_num++;
                string postUrl = postBaseURL + "&id=" + id_string;
                Thread.Sleep(300);

                string? post_responseBody = await ch.GetAPIResponseAsync(postUrl);
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
                            int query_key = post_result.QueryKey;
                            string? web_env = post_result.WebEnv;
                            if (web_env is not null)
                            {
                                string fetchUrl;
                                if (date_string == "")
                                {
                                    // (t = 121) No need to search - fetch all 100 pubmed records immediately.

                                    fetchUrl = fetchBaseURL + "&WebEnv=" + web_env + "&query_key=" +
                                               query_key.ToString();
                                    fetchUrl += "&retmax=100&retmode=xml";
                                    
                                    _loggingHelper.LogLine(fetchUrl);
                                    
                                    Thread.Sleep(200);
                                    await FetchPubMedRecordsAsync(fetchUrl, res, source, (int)opts.saf_id!,
                                        source.local_folder!);
                                }
                                else
                                {
                                    // (t = 114) Search for those that have been revised on or since the cutoff date.

                                    string searchUrl = searchBaseURL + "&term=%23" + query_key.ToString() + "+AND+" +
                                                       date_string;
                                    searchUrl += "&WebEnv=" + web_env + "&usehistory=y";

                                    Thread.Sleep(200);
                                    string? search_responseBody = await ch.GetAPIResponseAsync(searchUrl);
                                    eSearchResult? search_result = Deserialize<eSearchResult?>(search_responseBody, _loggingHelper);

                                    // The eSearchResult class corresponds to the returned data.

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
                                            await FetchPubMedRecordsAsync(fetchUrl, res, source, (int)opts.saf_id!,
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


    public async Task FetchPubMedRecordsAsync(string fetch_URL, DownloadResult res, Source source, int saf_id, 
        string file_base)
    {
        ScrapingHelpers ch = new(_loggingHelper);
        string? responseBody = await ch.GetAPIResponseAsync(fetch_URL);
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
                    PubMed_Processor pubmed_processor = new(_pubmed_repo, _loggingHelper);
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
                            // Not clear at the moment why this line was originally here - to investigate!
                            
                            // ObjectFileRecord? file_record = _mon_data_layer.FetchObjectFileRecord(fob.sd_oid!, source.id);

                            // ?? Here insert a lookup - for PMIDs originating in data sources - 
                            // always do after the ones originating from data banks - 
                            // that allows us to see the exact type of data object that is being added 
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
                                bool added = _monDataLayer.UpdateObjectDownloadLog(source.id, fob.sd_oid, remote_url, saf_id,
                                                        last_revised_datetime, full_path);
                                res.num_downloaded++;
                                if (added) res.num_added++;
                            }
                        }
                    }
                    _loggingHelper.LogLine("Checked so far: " + res.num_checked.ToString());
                }
            }
        }
    }



    // Writes out the file with the correct name to the correct folder, as indented json.
    // Called from the FetchPubMedRecordsAsync function.
    // Returns the full file path as constructed, or an 'error' string if an exception occurred.

    private async Task<string> WriteOutFile(FullObject fob, int ipmid, string file_base)
    {
        string folder_name = Path.Combine(file_base, "PM" + (ipmid / 10000).ToString("00000") + "xxxx");
        if (!Directory.Exists(folder_name))
        {
            Directory.CreateDirectory(folder_name);
        }
        string file_name = "PM" + ipmid.ToString("000000000") + ".json";
        string full_path = Path.Combine(folder_name, file_name);
        try
        {
            await using FileStream jsonStream = File.Create(full_path);
            await JsonSerializer.SerializeAsync(jsonStream, fob, _json_options);
            await jsonStream.DisposeAsync();
            return full_path;
        }
        catch (Exception e)
        {
            _loggingHelper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
            return "error";
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
            logging_helper.LogCodeError("Error when de-serialising " + inputString[0..1000], e.Message, e.StackTrace);
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
        // But also need for a few titles

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

        return inputString;
    }
}

