using Microsoft.Extensions.Configuration;

namespace MDR_Downloader;

public class ILoggingHelper : IILoggingHelper
{
    private readonly string  _logfileStartOfPath;
    private readonly string _summaryLogfileStartOfPath;
    private string _logfilePath = "";
    private string _summaryLogfilePath = "";
    private StreamWriter? _sw;

    public ILoggingHelper()
    {
        IConfigurationRoot settings = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        _logfileStartOfPath = settings["logfilepath"] ?? "";
        _summaryLogfileStartOfPath = settings["summaryfilepath"] ?? "";
    }

    // Used to check if a log file with a named source has been created.

    public string LogFilePath => _logfilePath;

    public void OpenLogFile(string? sourceFileName, string databaseName)
    {
        string dt_string = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture)
                          .Replace(":", "").Replace("T", " ");
        
        string log_folder_path = Path.Combine(_logfileStartOfPath, databaseName);
        if (!Directory.Exists(log_folder_path))
        {
            Directory.CreateDirectory(log_folder_path);
        }
        
        string log_file_name = "DL " + databaseName + " " + dt_string;
        
        // source file name used for WHO case, where the source is a file.
        // In other cases it is not required.
        
        if (sourceFileName is not null)
        {
            int LastBackSlashPos = sourceFileName.LastIndexOf("\\", StringComparison.Ordinal);
            string file_name = sourceFileName[(LastBackSlashPos + 1)..];
            log_file_name += " USING " + file_name + ".log";
        }
        else
        {
            log_file_name += ".log";

        }
        _logfilePath = Path.Combine(log_folder_path, log_file_name);            
        _summaryLogfilePath = Path.Combine(_summaryLogfileStartOfPath, log_file_name);
        _sw = new StreamWriter(_logfilePath, true, System.Text.Encoding.UTF8);
    }


    public void OpenNoSourceLogFile()
    {
        string dt_string = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture)
            .Replace(":", "").Replace("T", " ");
        
        string log_file_name = "DL Source not set " + dt_string + ".log";
        _logfilePath = Path.Combine(_logfileStartOfPath, log_file_name);
        _summaryLogfilePath = Path.Combine(_summaryLogfileStartOfPath, log_file_name);
        _sw = new StreamWriter(_logfilePath, true, System.Text.Encoding.UTF8);
    }


    public void LogLine(string message, string identifier = "")
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string feedback = dt_prefix + message + identifier;
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
            string day_word;
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
        if (opts.PreviousSearches?.Any() is true)
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


    public void LogHeader(string message)
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string header = dt_prefix + "**** " + message + " ****";
        Transmit("");
        Transmit(header);
    }


    public void LogError(string message)
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string error_message = dt_prefix + "***ERROR*** " + message;
        Transmit("");
        Transmit("+++++++++++++++++++++++++++++++++++++++");
        Transmit(error_message);
        Transmit("+++++++++++++++++++++++++++++++++++++++");
        Transmit("");
    }


    public void LogParseError(string header, string errorNum, string errorType)
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string error_message = dt_prefix + "***ERROR*** " + "Error " + errorNum + ": " + header + " " + errorType;
        Transmit(error_message);
    }


    public void LogCodeError(string header, string errorMessage, string? stackTrace)
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string headerMessage = dt_prefix + "***ERROR*** " + header + "\n";
        Transmit("");
        Transmit("+++++++++++++++++++++++++++++++++++++++");
        Transmit(headerMessage);
        Transmit(errorMessage + "\n");
        Transmit(stackTrace ?? "No stack trace provided by error.");
        Transmit("+++++++++++++++++++++++++++++++++++++++");
        Transmit("");
    }


    public void CloseLog()
    {
        if (_sw is not null)
        {
            LogHeader("Closing Log");
            _sw.Flush();
            _sw.Close();
        }
        
        // Write out the summary file.
    
        var sw_summary = new StreamWriter(_summaryLogfilePath, true, System.Text.Encoding.UTF8);
    
        sw_summary.Flush();
        sw_summary.Close();
    }

    private void Transmit(string message)
    {
        _sw?.WriteLine(message);
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