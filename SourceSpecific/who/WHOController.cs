using CsvHelper;
using CsvHelper.Configuration;
using MDR_Downloader.Helpers;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MDR_Downloader.who;

class WHO_Controller : IDLController
{
    private readonly IMonDataLayer _monDataLayer;
    private readonly ILoggingHelper _loggingHelper;

    public WHO_Controller(IMonDataLayer monDataLayer, ILoggingHelper loggingHelper)
    {
        _monDataLayer = monDataLayer;
        _loggingHelper = loggingHelper;
    }
    
    public async Task<DownloadResult> ObtainDataFromSourceAsync(Options opts, Source source)
    {
        // WHO processing unusual in that it is from a csv file
        // The program loops through the file and creates a JSON file from each row
        // It then distributes it to the correct source folder for 
        // later harvesting.

        // In some cases the file will be one of a set created from a large
        // 'all data' download, in other cases it will be a weekly update file
        // In both cases any existing JSON files of the same name should be overwritten.

        DownloadResult res = new();
        string? file_base = source.local_folder;

        if (file_base is null)
        {
            _loggingHelper.LogError("Null value passed for local folder value for this source");
            return res;   // return zero result
        }

        WHO_Processor who_processor = new();
        string source_file = opts.FileName!;     // already checked as non-null

        var csv_reader_config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
        };

        var json_options = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        using (var reader = new StreamReader(source_file, true))
        {
            using var csv = new CsvReader(reader, csv_reader_config);
            var records = csv.GetRecords<WHO_SourceRecord>();
            _loggingHelper.LogLine("Rows loaded into WHO record structure");

            // Consider each study row in turn and turn it into a WHO record class.

            foreach (WHO_SourceRecord sr in records)
            {
                res.num_checked++;
                WHORecord? r = who_processor.ProcessStudyDetails(sr);

                if (r is not null)
                {
                    // Write out study record as JSON, log the download.

                    if (!string.IsNullOrEmpty(r.db_name))
                    {
                        string folder_name = @"F:\MDR_Data\" + r.db_name + @"\";
                        if (!Directory.Exists(folder_name))
                        {
                            Directory.CreateDirectory(folder_name);
                        }
                        string file_name = r.sd_sid + ".json";
                        string full_path = Path.Combine(folder_name, file_name);
                        try
                        {
                            await using FileStream jsonStream = File.Create(full_path);
                            await JsonSerializer.SerializeAsync(jsonStream, r, json_options);
                            await jsonStream.DisposeAsync();
                            
                            if (_monDataLayer.IsWHOTestStudy(r.db_name, r.sd_sid))
                            {
                                // write out copy of the file in the test folder
                                string test_path = _loggingHelper.TestFilePath;
                                string full_test_path = Path.Combine(test_path, file_name);
                                await using FileStream jsonStream2 = File.Create(full_test_path);
                                await JsonSerializer.SerializeAsync(jsonStream2, r, json_options);
                                await jsonStream2.DisposeAsync();
                            }
                        }
                        catch (Exception e)
                        {
                            _loggingHelper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
                        }

                        bool added = _monDataLayer.UpdateWhoStudyLog(r.db_name, r.sd_sid, r.remote_url, (int)opts.dl_id!,
                                                            r.record_date?.FetchDateTimeFromISO(), full_path);
                        res.num_downloaded++;
                        if (added) res.num_added++;
                    }
                }

                if (res.num_checked % 100 == 0) _loggingHelper.LogLine(res.num_checked.ToString());
            }
        }
        return res;
    }
}
