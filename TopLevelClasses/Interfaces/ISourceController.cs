namespace MDR_Downloader;

public interface ISourceController
{
    public Task<DLEvent> ObtainDataFromSourceAsync(Options opts, Source source, DLEvent dl);
}