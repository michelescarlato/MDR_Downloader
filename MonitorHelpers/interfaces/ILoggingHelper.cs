namespace MDR_Downloader;

public interface ILoggingHelper
{
        public string LogFilePath { get; }
        public void OpenLogFile(string? source_file_name, string database_name);
        public void OpenNoSourceLogFile();
        public void LogLine(string message);
        public void LogCommandLineParameters(Options opts);
        public void LogHeader(string message);
        public void LogError(string message);
        public void LogParseError(string header, string errorNum, string errorType);
        public void LogCodeError(string header, string errorMessage, string? stackTrace);
        public void CloseLog();
        public void LogRes(SAFEvent saf);
}