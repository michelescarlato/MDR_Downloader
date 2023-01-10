using Microsoft.Extensions.Configuration;


namespace MDR_Downloader
{
    public class LoggingHelper
    {
        private string logfile_startofpath;
        private string summary_logfile_startofpath;
        private string logfile_path = "";
        private string summary_logfile_path = "";
        string dt_string;

        private StreamWriter? sw;

        public LoggingHelper()
        {
            IConfigurationRoot settings = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            dt_string = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture)
                              .Replace(":", "").Replace("T", " ");

            logfile_startofpath = settings["logfilepath"] ?? "";
            summary_logfile_startofpath = settings["summaryfilepath"] ?? "";
        }


        // Used to check if a log file with a named source has been created.

        public string LogFilePath => logfile_path;


        public void OpenLogFile(string? source_file_name, string database_name)
        {
            string dt_string = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture)
                              .Replace(":", "").Replace("T", " ");
            
            string log_folder_path = Path.Combine(logfile_startofpath, database_name);
            if (!Directory.Exists(log_folder_path))
            {
                Directory.CreateDirectory(log_folder_path);
            }

            logfile_path = Path.Combine(log_folder_path, "DL " + database_name + " " + dt_string);
            summary_logfile_path = Path.Combine(summary_logfile_startofpath, "DL " + database_name + " " + dt_string);

            // source file name used for WHO case, where the source is a file
            // In other cases is not required

            if (source_file_name is not null)
            {
                string file_name = source_file_name.Substring(source_file_name.LastIndexOf("\\") + 1);
                logfile_path += " USING " + file_name + ".log";
            }
            else
            {
                logfile_path += ".log";

            }
            sw = new StreamWriter(logfile_path, true, System.Text.Encoding.UTF8);
        }



        public void OpenNoSourceLogFile()
        {
            logfile_path += logfile_startofpath + "DL Source not set " + dt_string + ".log";
            sw = new StreamWriter(logfile_path, true, System.Text.Encoding.UTF8);
        }


        internal void LogLine(string message, string identifier = "")
        {
            string dt_string = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
            string feedback = dt_string + message + identifier;
            Transmit(feedback);
        }


        public void LogCommandLineParameters(Options opts)
        {
            LogLine("****** DOWNLOAD ******");
            LogHeader("Set up");
            LogLine($"Download event Id is {opts.saf_id}");
            LogLine("");
            LogLine($"Source_id is {opts.SourceId}");
            LogLine($"Type_id is {opts.FetchTypeId}");
            if (opts.FileName is not null)
            {
                LogLine($"File name is {opts.FileName}");
            }
            if (opts.CutoffDate is not null)
            {
                LogLine($"Cutoff date is {opts.CutoffDateAsString}");
            }
            if (opts.EndDate is not null)
            {
                LogLine($"End date is {opts.EndDateAsString}");
            }
            if (opts.FocusedSearchId is not null)
            {
                LogLine($"Filter is {opts.FocusedSearchId}");
            }
            if (opts.SkipRecentDays is not null)
            {
                string day_word = "";
                if (opts.SkipRecentDays > 1)
                {
                    day_word = $"in most recent {opts.SkipRecentDays} days";
                    
                }
                else
                {
                    day_word = opts.SkipRecentDays == 0 ? "today" : "today or yesterday";
                }
                LogLine($"Ignore files already downloaded {day_word}");
            }
            if (opts.FetchTypeId == 141 || opts.FetchTypeId == 142)
            {
                LogLine($"Offset for record Ids: {opts.OffsetIds}");
                LogLine($"Amount of record Ids: {opts.AmountIds}");
            }
            if (opts.FetchTypeId == 146)
            {
                LogLine($"Start Page: {opts.StartPage}");
                LogLine($"End pages: {opts.EndPage}");
            }
            if (opts.PreviousSearches?.Any() == true)
            {
                foreach (int i in opts.PreviousSearches)
                {
                    LogLine($"previous_search is {i}");
                }
            }
            if (opts.NoLogging is not null)
            {
                LogLine($"Logging suppressed");
            }
        }


        internal void LogHeader(string message)
        {
            string dt_string = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
            string header = dt_string + "**** " + message + " ****";
            Transmit("");
            Transmit(header);
        }


        internal void LogError(string message)
        {
            string dt_string = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
            string error_message = dt_string + "***ERROR*** " + message;
            Transmit("");
            Transmit("+++++++++++++++++++++++++++++++++++++++");
            Transmit(error_message);
            Transmit("+++++++++++++++++++++++++++++++++++++++");
            Transmit("");
        }


        internal void LogParseError(string header, string errorNum, string errorType)
        {
            string dt_string = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
            string error_message = dt_string + "***ERROR*** " + "Error " + errorNum + ": " + header + " " + errorType;
            Transmit(error_message);
        }


        internal void LogCodeError(string header, string errorMessage, string? stackTrace)
        {
            string dt_string = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
            string headerMessage = dt_string + "***ERROR*** " + header + "\n";
            Transmit("");
            Transmit("+++++++++++++++++++++++++++++++++++++++");
            Transmit(headerMessage);
            Transmit(errorMessage + "\n");
            Transmit(stackTrace ?? "No stack trace provided by error.");
            Transmit("+++++++++++++++++++++++++++++++++++++++");
            Transmit("");
        }


        internal void CloseLog()
        {
            if (sw is not null)
            {
                LogHeader("Closing Log");
                sw.Flush();
                sw.Close();
            }
        }

        private void Transmit(string message)
        {
            sw?.WriteLine(message);
            Console.WriteLine(message);
        }

        public void LogRes(DownloadResult res)
        {
            string dt_string = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
            Transmit("");
            Transmit(dt_string + "**** " + "Download Result" + " ****");
            Transmit(dt_string + "**** " + "Records checked: " + res.num_checked.ToString() + " ****");
            Transmit(dt_string + "**** " + "Records downloaded: " + res.num_downloaded.ToString() + " ****");
            Transmit(dt_string + "**** " + "Records added: " + res.num_added.ToString() + " ****");
        }
    }
}

