namespace MDR_Downloader;

public interface ISourceController
{
    public Task<SAFEvent> ObtainDataFromSourceAsync(Options opts, Source source, SAFEvent saf);
}