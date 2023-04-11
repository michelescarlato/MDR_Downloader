namespace MDR_Downloader;

public class SourceController : ISourceController
{
    private readonly IDLController _dlController;
    
    public SourceController(IDLController dlController) 
    {
        _dlController = dlController;
    }
    
    public async Task<DLEvent> ObtainDataFromSourceAsync(Options opts, Source source, DLEvent dl)
    {
        
        DownloadResult res = await _dlController.ObtainDataFromSourceAsync(opts, source);
        dl.time_ended = DateTime.Now;
        dl.num_records_checked = res.num_checked;
        dl.num_records_downloaded = res.num_downloaded;
        dl.num_records_added = res.num_added;
        return dl;
    }
    
    
}