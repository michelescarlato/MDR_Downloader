using HtmlAgilityPack;
using MDR_Downloader.Helpers;
using ScrapySharp.Extensions;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MDR_Downloader.biolincc;

public class BioLinccController : IDLController
{
    private readonly IMonDataLayer _monDataLayer;
    private readonly ILoggingHelper _loggingHelper;    
    private readonly BioLinccDataLayer _biolinccRepo;    
    private readonly JsonSerializerOptions _jsonOptions;

    public BioLinccController(IMonDataLayer monDataLayer, ILoggingHelper loggingHelper)
    {
        _monDataLayer = monDataLayer;
        _loggingHelper = loggingHelper;
        _biolinccRepo = new BioLinccDataLayer(monDataLayer.Credentials);  
        
        _jsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
    }

    public async Task<DownloadResult> ObtainDataFromSourceAsync(Options opts, Source source)
    {
        // Ensure that the private fields are instantiated. Property injection used here 
        // rather than a constructor, to keep the Controller constructor (to be in a
        // separate dll) more independent of main program. This function common to all 
        // download constructors as the single download call.
  
        // For BioLincc, all data is downloaded each time during a download, 
        // (t= 102) as it takes a relatively short time to run through about 300 studies.
        // 
        // The first gets the web page list and then loops through it to fetch details
        // of each study, which are then stored as local files in the usual fashion.
        // Note that if an object cannot be classified it is filed, for later 
        // inspection, and the study data is not stored at all.

        DownloadResult res = await LoopThroughPagesAsync(opts, source);
        
        // A second routine uses the data collected about which biolincc studies link to 
        // which NCT studies (it is not always 1-to-1) to update the 'in multi-BioLINCC
        // group' field, when multiple BioLINCC studies correspond to a single NCT study,
        // for those records that require it.

        if (res.num_downloaded > 0)
        {
            _biolinccRepo.UpdateLinkStatus(); 
            List<StudyFileRecord> file_list = _monDataLayer.FetchStudyIds(source.id).ToList();
            await PostProcessDataAsync(file_list);
        }
        
        return res;
    }

    
    private async Task<DownloadResult> LoopThroughPagesAsync(Options opts, Source source)
    {
        DownloadResult res = new ();
        string? folder_path = source.local_folder;
        if (folder_path is null)
        {
            _loggingHelper.LogError("Null value passed for local folder value for this source");
            return res;   // return zero result if no place to put the files!
        }
        if (!Directory.Exists(folder_path))
        {
            Directory.CreateDirectory(folder_path);  // ensure folder is present
        }

        int source_id = source.id;
        int? days_ago = opts.SkipRecentDays;
        ScrapingHelpers ch = new(_loggingHelper);        

        // Get list of studies from the Biolincc start page.

        WebPage? homePage = await ch.GetPageAsync("https://biolincc.nhlbi.nih.gov/studies/");
        if (homePage is null)
        {
            _loggingHelper.LogError("Initial attempt to access biolincc studies list page failed");
            return res; // return zero result
        }

        HtmlNode? study_list_table = homePage.Find("div", By.Class("table-responsive")).FirstOrDefault();
        List<HtmlNode>? studyRows = study_list_table?.CssSelect("tbody tr").ToList();

        if (studyRows?.Any() is not true)
        {
            _loggingHelper.LogError("Unable to find summary listing of rows of Biolincc trials");
            return res; // return zero result
        }
        
        // Consider each study in turn, starting by looking at the list entries.
        _loggingHelper.LogHeader("Processing Data");
        _loggingHelper.LogLine($"File list obtained, of {studyRows.Count} rows");

        BioLINCC_Processor biolincc_processor = new (ch, _loggingHelper, _biolinccRepo);

        foreach (HtmlNode row in studyRows)
        {
            res.num_checked++;
            BioLincc_Basics? bb = biolincc_processor.GetStudyBasics(row);
            if (bb is not null)
            {
                if (bb.collection_type == "Non-BioLINCC Resource")
                {
                    _loggingHelper.LogLine($"#{res.num_checked}: {bb.sd_sid} is a Non-BioLINCC Resource, not processed further");
                }
                else
                {
                    // If record already downloaded recently, days_ago parameter may cause it
                    // to be ignored... (may be used if re-running after an error or object name update).

                    if (days_ago is null || !_monDataLayer.Downloaded_recently(source_id, bb.sd_sid, (int)days_ago))
                    {
                        // Fetch the study record, as constructed by the biolincc_processor
                        // Assuming successful record creation, store the links between Biolincc
                        // and NCT record for later inspection, and store any non-matched document
                        // details in the relevant table. Any non-matched documents will abort
                        // the download for that record - but an error is posted so that the 
                        // issue can be resolved.
                        
                        BioLinccRecord? st = await biolincc_processor.GetStudyDetailsAsync(bb);
                        _loggingHelper.LogLine($"Obtaining #{res.num_checked}: {bb.sd_sid}");

                        if (st is not null)
                        {
                            if (st.registry_ids is not null)
                            {
                                _biolinccRepo.StoreLinks(st.sd_sid, st.registry_ids);
                            }
                            
                            if (st.UnmatchedDocTypes.Any())
                            {
                                foreach (string s in st.UnmatchedDocTypes)
                                {
                                    _biolinccRepo.InsertUnmatchedDocumentType(s);
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
                                    await using FileStream jsonStream = File.Create(full_path);
                                    await JsonSerializer.SerializeAsync(jsonStream, st, _jsonOptions);
                                    await jsonStream.DisposeAsync();
                                    _loggingHelper.LogLine($"{res.num_checked}: {bb.sd_sid} downloaded, with {assoc_docs_num} linked publications");
                                }
                                catch (Exception e)
                                {
                                    _loggingHelper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
                                }

                                bool added = _monDataLayer.UpdateStudyDownloadLog(source_id, st.sd_sid, st.remote_url, opts.saf_id,
                                                                  st.datasets_updated_date, full_path);
                                res.num_downloaded++;
                                if (added) res.num_added++;
                                Thread.Sleep(1000);  // Put a pause here 
                            }
                        }
                    }
                }
            }

            if (res.num_checked % 10 != 0) continue;
            _loggingHelper.LogLine("files checked: " + res.num_checked);
            _loggingHelper.LogLine("files downloaded: " + res.num_downloaded);
        }
        
        return res;
    }


    // Allows groups of Biolincc trials that equate to a single NCT registry to be identified.
    // It runs through each Biolincc file and interrogates the database to see if it is one
    // of a group linked to a single NCT record. If so it de-serialises the file and updates
    // the relevant field, before re-serialising the data back to the file.
    
    private async Task PostProcessDataAsync(List<StudyFileRecord> fileList)
    {
        int n = 0, r= 0; 
        foreach (StudyFileRecord rec in fileList)
        {
            n++;
            bool in_multiple_biolincc_group = _biolinccRepo.GetMultiLinkStatus(rec.sd_id);
            if (in_multiple_biolincc_group)
            { 
                string filePath = rec.local_path ?? "";
                if (File.Exists(filePath) && filePath[^4..] == "json")  
                { 
                    string jsonString = await File.ReadAllTextAsync(filePath);
                    BioLinccRecord? biolincc_study = JsonSerializer.Deserialize<BioLinccRecord?>(jsonString, _jsonOptions);
                    if (biolincc_study is not null)
                    {
                        biolincc_study.in_multiple_biolincc_group = true;
                        r++;
                        try
                        {
                            await using FileStream jsonStream = File.Create(filePath);
                            await JsonSerializer.SerializeAsync(jsonStream, biolincc_study, _jsonOptions);
                            await jsonStream.DisposeAsync();
                        }
                        catch (Exception e)
                        {
                            _loggingHelper.LogLine("Error in trying to save file at " + filePath + ":: " + e.Message);
                        }
                    }
                }
            }
            if (n % 10 == 0) _loggingHelper.LogLine("Checked " + n.ToString());
        }
        _loggingHelper.LogLine("Updated " + r.ToString() + " as being in 'multiple biolincc to 1 NCT' group");
    }
}