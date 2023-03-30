using System.Text;
using System.Text.Encodings.Web;
using MDR_Downloader.Helpers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace MDR_Downloader.euctr;

public class EMA_Controller : IDLController
{
    private readonly IMonDataLayer _monDataLayer;
    private readonly ILoggingHelper _loggingHelper;

    public EMA_Controller(IMonDataLayer monDataLayer, ILoggingHelper loggingHelper)
    {
        _monDataLayer = monDataLayer;
        _loggingHelper = loggingHelper;
    }

    public async Task<DownloadResult> ObtainDataFromSourceAsync(Options opts, Source source)
    {
        // The EMA Controller and process have similarities with the WHO controller, in that it 
        // takes its input from a designated file, but it is linked to, and shares the same namespace as,
        // the EUCTR controller.
        // The only allowed download type is therefore 103, and it requires an associated file, the full
        // path of which is included in options.
        
        DownloadResult res = new();
        string? file_base = source.local_folder;
        int dl_id = (int)opts.dl_id!;    // will be non-null

        if (file_base is null)
        {
            _loggingHelper.LogError("Null value passed for local folder value for this source");
            return res;   // return zero result
        }
       
        // Firstly does the specified file actually exist?
        
        string file_name = opts.FileName!;
        if (!File.Exists(file_name))
        {
            _loggingHelper.LogError($"File does not appear to exist at {file_name}");
            return res;  // empty result
        }
        
        // Get the (approximate) date of revision from the file date stamp.
               
        string date = Regex.Match(file_name, @"\d{8}").Value;
        int y = int.Parse(date[..4]);
        int m = int.Parse(date[4..6]);
        int d = int.Parse(date[6..]);
        DateTime date_revised = new DateTime(y, m, d);
        
        // set up JSON options for later writing
        
        var json_options = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        
        // then open and read the file
        
        using StreamReader streamReader = new StreamReader(file_name, Encoding.UTF8);
        string responseBodyAsString = await streamReader.ReadToEndAsync();
        trials? foundTrials = DeserializeXML<trials?>(responseBodyAsString, _loggingHelper);
        if (foundTrials?.trials_list is not null)
        {
            EMAProcessor ep = new(_loggingHelper);
            foreach (Trial t in foundTrials.trials_list)
            {
                // Send t to the processing function, to construct the study model class.
                
                res.num_checked++;
                Euctr_Record? euctr_record = await ep.ProcessTrial(t, date_revised);
                if (euctr_record is not null)
                { 
                    // Write out study record as JSON, log the download.

                    string new_file_name = "EU " + euctr_record.sd_sid + ".json";
                    string full_path = Path.Combine(file_base, new_file_name);
                    try
                    {
                        await using FileStream jsonStream = File.Create(full_path);
                        await JsonSerializer.SerializeAsync(jsonStream, euctr_record, json_options);
                        await jsonStream.DisposeAsync();
                    }
                    catch (Exception e)
                    {
                        _loggingHelper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
                    }
                   
                    bool added = _monDataLayer.UpdateStudyLog(euctr_record.sd_sid, euctr_record.details_url, 
                        dl_id, null, full_path);     
                    res.num_downloaded++;
                    if (added) res.num_added++;
                }
            }
        }

        return res;
    }
    
    
    // General XML Deserialize function.

    private T? DeserializeXML<T>(string? inputString, ILoggingHelper logging_helper)
    {
        if (inputString is null)
        {
            return default;
        }

        T? instance;
        try
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using var stringReader = new StringReader(inputString);
            instance = (T?)xmlSerializer.Deserialize(stringReader);
        }
        catch(Exception e)
        {
            logging_helper.LogCodeError("Error when de-serialising " + inputString, e.Message, e.StackTrace);
            return default;
        }

        return instance;
    }
    
}