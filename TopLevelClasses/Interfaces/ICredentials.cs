namespace MDR_Downloader;

public interface ICredentials
{
    string GetPubMedApiKey();
    string GetConnectionString(string database_name);
}
