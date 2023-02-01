namespace MDR_Downloader;

public interface ISourceController
{
    public Task<DownloadResult> ObtainDataFromSourceAsync(Options opts, Source source);
}