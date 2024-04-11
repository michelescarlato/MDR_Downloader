using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
namespace MDR_Downloader;

public class LoggingHelper : ILoggingHelper
{
    private readonly string _dataFolderPath;
    private readonly string _logfileStartOfPath;
    private readonly string _testFilePath;    
    private readonly string _summaryLogfileStartOfPath;
    private string _logfilePath = "";
    //private string _summaryLogfilePath = "";
    private string summary_string = "";
    private StreamWriter? _sw;

    public LoggingHelper(IConfiguration settings)
    {
        /*
        IConfigurationRoot settings = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
        */

        _logfileStartOfPath = settings["logFilePath"] ?? "";
        _summaryLogfileStartOfPath = settings["summaryFilePath"] ?? "";
        _testFilePath = settings["testFilePath"] ?? "";
        _dataFolderPath = settings["dataFolderPath"] ?? "";

    }

    public string LogFilePath => _logfilePath;  // Used to check if a log file exists.
    public string TestFilePath => _testFilePath;
    public string DataFolderPath => _dataFolderPath;


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
        //_summaryLogfilePath = Path.Combine(_summaryLogfileStartOfPath, log_file_name);
        _sw = new StreamWriter(_logfilePath, true, System.Text.Encoding.UTF8);
    }


    public void OpenNoSourceLogFile()
    {
        string dt_string = DateTime.Now.ToString("s", System.Globalization.CultureInfo.InvariantCulture)
            .Replace(":", "").Replace("T", " ");
        
        string log_file_name = "DL Source not set " + dt_string + ".log";
        _logfilePath = Path.Combine(_logfileStartOfPath, log_file_name);
        //_summaryLogfilePath = Path.Combine(_summaryLogfileStartOfPath, log_file_name);
        _sw = new StreamWriter(_logfilePath, true, System.Text.Encoding.UTF8);
    }


    public void LogLine(string message)
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string feedback = dt_prefix + message;
        Transmit(feedback);
    }

    public void LogHeader(string message)
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string header = dt_prefix + "**** " + message + " ****";
        Transmit("");
        Transmit(header);
    }


    public void LogCommandLineParameters(Options opts)
    {
        LogLine("****** DOWNLOAD ******");
        summary_string = $"\nRecord of Download :\n";
        LogHeader("Set up");
        LogLine($"Download event Id is {opts.dl_id}");
        LogLine("");
        LogLine($"Source_id is {opts.SourceId}");
        summary_string += $"\nSource_id is {opts.SourceId}";
        LogLine($"Type_id is {opts.FetchTypeId}");
        summary_string += $"\nType_id is {opts.FetchTypeId}";
        if (opts.FileName is not null)
        {
            LogLine($"File name is {opts.FileName}");
            summary_string += $"\nFile name is {opts.FileName}";
        }
        if (opts.CutoffDate is not null)
        {
            opts.CutoffDateAsString ??= ((DateTime)opts.CutoffDate).ToString("dd/MM/yyyy");
            LogLine($"Cutoff date is {opts.CutoffDateAsString}");
            summary_string += $"\nCutoff date is {opts.CutoffDateAsString}";
        }
        if (opts.EndDate is not null)
        {
            opts.EndDateAsString ??= ((DateTime)opts.EndDate).ToString("dd/MM/yyyy");
            LogLine($"End date is {opts.EndDateAsString}");
            summary_string += $"\nEnd date is {opts.EndDateAsString}";
        }
        if (opts.FocusedSearchId is not null)
        {
            LogLine($"Filter is {opts.FocusedSearchId}");
            summary_string += $"\nFilter is {opts.FocusedSearchId}";
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
            summary_string += $"\nIgnore files already downloaded {day_word}";
        }
        if (opts.FetchTypeId == 141 || opts.FetchTypeId == 142)
        {
            LogLine($"Offset for record Ids: {opts.OffsetIds}");
            LogLine($"Amount of record Ids: {opts.AmountIds}");
            summary_string += $"\nOffset for record Ids: {opts.OffsetIds}";
            summary_string += $"\nAmount of record Ids: {opts.AmountIds}";
        }
        if (opts.FetchTypeId == 146)
        {
            LogLine($"Start Page: {opts.StartPage}");
            LogLine($"End pages: {opts.EndPage}");
            summary_string += $"\nStart Page: {opts.StartPage}";
            summary_string += $"\nEnd pages: {opts.EndPage}";
        }
        if (opts.PreviousSearches?.Any() is true)
        {
            foreach (int i in opts.PreviousSearches)
            {
                LogLine($"previous_search is {i}");
                summary_string += $"\nprevious_search is {i}:";
            }
        }
        if (opts.NoLogging is not null)
        {
            LogLine($"Logging suppressed");
            summary_string += $"\nLogging suppressed";
        }

        SendMailMessage();
    }


    private void SendMailMessage()
    { 
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("MDR_Downloader", "downloader@mdr2"));
        message.To.Add(new MailboxAddress("Steve Canham", "stevecanham@outlook.com"));
        message.Subject = "Test message";

        message.Body = new TextPart("plain")
        {
            Text = @"I just did a download"
        };

        using (var client = new SmtpClient()) 
        {
            client.Connect("127.0.0.1", 25, false);

            client.Send(message);
            client.Disconnect(true);
        }
    }


    public void LogError(string message)
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string error_message = dt_prefix + "***ERROR*** " + message;
        
        LogLine("");
        LogLine("+++++++++++++++++++++++++++++++++++++++");
        LogLine(error_message);
        LogLine("+++++++++++++++++++++++++++++++++++++++");
        LogLine("");
        
        summary_string += $"\n\n+++++++++++++++++++++++++++++++++++++++";
        summary_string += $"\nError or exception:";
        summary_string += $"\n{error_message}";
    }


    public void LogParseError(string header, string errorNum, string errorType)
    {
        string error_message = "***ERROR*** " + "Error " + errorNum + ": " + header + " " + errorType;
        LogLine(error_message);
        
        summary_string += $"\n\n+++++++++++++++++++++++++++++++++++++++";
        summary_string += $"\nParse error:";
        summary_string += $"\n{error_message}";
    }


    public void LogCodeError(string header, string errorMessage, string? stackTrace)
    {
        string dt_prefix = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString() + " :   ";
        string headerMessage = dt_prefix + "***ERROR*** " + header + "\n";
        string stack = stackTrace ?? "No stack trace provided by error.";
        LogLine("");
        LogLine("+++++++++++++++++++++++++++++++++++++++");
        LogLine(headerMessage);
        LogLine(errorMessage + "\n");
        LogLine(stack);
        LogLine("+++++++++++++++++++++++++++++++++++++++");
        LogLine("");
        
        summary_string += $"\n\n+++++++++++++++++++++++++++++++++++++++";
        summary_string += $"\nCode error:";
        summary_string += $"\n{header}";
        summary_string += $"\n{errorMessage}";
        summary_string += $"\n{stack}";
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
        /*
        var sw_summary = new StreamWriter(_summaryLogfilePath, true, System.Text.Encoding.UTF8);
        sw_summary.WriteLine(summary_string);
        sw_summary.Flush();
        sw_summary.Close();
        */
    }


    private void Transmit(string message)
    {
        _sw?.WriteLine(message);
        Console.WriteLine(message);
    }


    public void LogRes(DLEvent dl)
    {
        LogHeader("Download Result");
        LogLine("Source Id: " + dl.source_id);
        LogLine("Download Event Id: " + dl.id);
        LogLine("Start time: " + dl.time_started);
        LogLine("End time: " + dl.time_ended);
        LogLine("Records checked: " + dl.num_records_checked);
        LogLine("Records downloaded: " + dl.num_records_downloaded);
        LogLine("Records added: " + dl.num_records_added);
        
        summary_string += $"\n\nDownload Result:";
        summary_string += $"\nDownload Event Id: {dl.id}";
        summary_string += $"\nStart time: {dl.time_started}";
        summary_string += $"\nEnd time: {dl.time_ended}";
        summary_string += $"\nRecords checked: {dl.num_records_checked}";
        summary_string += $"\nRecords downloaded: {dl.num_records_downloaded}";
        summary_string += $"\nRecords added: {dl.num_records_added}";
    }
}