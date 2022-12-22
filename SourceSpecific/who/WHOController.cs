using CsvHelper;
using CsvHelper.Configuration;
using MDR_Downloader.Helpers;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Serialization;

namespace MDR_Downloader.who
{
    class WHO_Controller
    {
        private readonly LoggingHelper _logging_helper;
        private readonly MonDataLayer _mon_data_layer;

        public WHO_Controller(MonDataLayer mon_data_layer, LoggingHelper logging_helper)
        {
            _logging_helper = logging_helper;
            _mon_data_layer = mon_data_layer;
        }

        public async Task<DownloadResult> ObtainDatafromSourceAsync(Options opts, int saf_id, Source source)
        {
            // WHO processing unusual in that it is from a csv file
            // The program loops through the file and creates an XML file from each row
            // It then distributes it to the correct source folder for 
            // later harvesting.

            // In some cases the file will be one of a set created from a large
            // 'all data' download, in other cases it will be a weekly update file
            // In both cases any existing XML files of the same name shoud be overwritten

            WHO_Processor who_processor = new();
            XmlSerializer writer = new(typeof(WHORecord));
            DownloadResult res = new();
            string sourcefile = opts.FileName!;     // checked as non-null
            var csv_reader_config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };
            var json_options = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

            using (var reader = new StreamReader(sourcefile, true))
            {
                using (var csv = new CsvReader(reader, csv_reader_config))
                {
                    var records = csv.GetRecords<WHO_SourceRecord>();
                    _logging_helper.LogLine("Rows loaded into WHO record structure");

                    // Consider each study in turn.

                    foreach (WHO_SourceRecord sr in records)
                    {
                        res.num_checked++;
                        WHORecord? r = who_processor.ProcessStudyDetails(sr, _logging_helper);

                        if (r is not null)
                        {
                            // Write out study record as XML, log the download
                            string? file_base = r.folder_name;
                            if (file_base is not null)
                            {
                                if (!Directory.Exists(file_base))
                                {
                                    Directory.CreateDirectory(file_base);
                                }
                                string file_name = r.sd_sid + ".json";
                                string full_path = Path.Combine(file_base, file_name);
                                try
                                {
                                    using FileStream jsonStream = File.Create(full_path);
                                    await JsonSerializer.SerializeAsync(jsonStream, r, json_options);
                                    await jsonStream.DisposeAsync();
                                }
                                catch (Exception e)
                                {
                                    _logging_helper.LogLine("Error in trying to save file at " + full_path + ":: " + e.Message);
                                }

                                bool added = _mon_data_layer.UpdateStudyDownloadLog(r.source_id, r.sd_sid!, r.remote_url, saf_id,
                                                                   r.record_date?.FetchDateTimeFromISO(), full_path);
                                res.num_downloaded++;
                                if (added) res.num_added++;
                            }
                        }

                        if (res.num_checked % 100 == 0) _logging_helper.LogLine(res.num_checked.ToString());
                    }
                }
            }
            return res;
        }
    }
}
