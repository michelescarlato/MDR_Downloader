namespace MDR_Downloader;

public interface ICredentials
{
    string Host { get; set; }
    string Password { get; set; }
    string Username { get; set; }
    int Port { get; set; }
    string PubmedAPIKey { get; }
    
    string GetConnectionString(string database_name);
}
