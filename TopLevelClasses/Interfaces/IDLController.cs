namespace MDR_Downloader;

public interface IDLController
{
    Task<DownloadResult> ObtainDataFromSourceAsync(Options opts, Source source);
}