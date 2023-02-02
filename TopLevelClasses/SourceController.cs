namespace MDR_Downloader;

public class SourceController : ISourceController
{
    private readonly IDLController _dlController;
    
    public SourceController(IDLController dlController) 
    {
        _dlController = dlController;
    }
    
    public async Task<SAFEvent> ObtainDataFromSourceAsync(Options opts, Source source, SAFEvent saf)
    {
        
        DownloadResult res = await _dlController.ObtainDataFromSourceAsync(opts, source);
        saf.time_ended = DateTime.Now;
        saf.num_records_checked = res.num_checked;
        saf.num_records_downloaded = res.num_downloaded;
        saf.num_records_added = res.num_added;
        return saf;
    }
    
    
}