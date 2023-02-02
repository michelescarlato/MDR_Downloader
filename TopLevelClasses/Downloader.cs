using MDR_Downloader.biolincc;
using MDR_Downloader.ctg;
using MDR_Downloader.euctr;
using MDR_Downloader.isrctn;
using MDR_Downloader.pubmed;
using MDR_Downloader.vivli;
using MDR_Downloader.who;
using MDR_Downloader.yoda;

namespace MDR_Downloader;

public class Downloader
{
    private readonly ILoggingHelper _loggingHelper;
    private readonly IMonDataLayer _monDataLayer;

    public Downloader(IMonDataLayer mon_data_layer, ILoggingHelper logging_repo)
    {
        _loggingHelper = logging_repo;
        _monDataLayer = mon_data_layer;
    }

    public async Task RunDownloaderAsync(Options opts, Source source)
    {
        // Set up the search and fetch summary record for this download,
        // Open log file and log parameters, and establish appropriate controller class,

        opts.saf_id = _monDataLayer.GetNextSearchFetchId();
        SAFEvent saf = new(opts, source.id);
        _loggingHelper.OpenLogFile(opts.FileName, source.database_name!);
        _loggingHelper.LogCommandLineParameters(opts);
        IDLController? dl_controller = null;
        switch (source.id)
        {
            case 101900:
                {
                    dl_controller = new BioLINCC_Controller(_monDataLayer, _loggingHelper); break;
                }
            case 101901:
                {
                    dl_controller = new Yoda_Controller(_monDataLayer, _loggingHelper); break;
                }
            case 100120:
                {
                    dl_controller = new CTG_Controller(_monDataLayer, _loggingHelper); break;
                }
            case 100126:
                {
                    dl_controller = new ISRCTN_Controller(_monDataLayer, _loggingHelper); break;
                }
            case 100123:
                {
                    dl_controller = new EUCTR_Controller(_monDataLayer, _loggingHelper); break;
                }
            case 100115:
                {
                    dl_controller = new WHO_Controller(_monDataLayer, _loggingHelper); break;
                }
            case 100135:
                {
                    dl_controller = new PubMed_Controller(_monDataLayer, _loggingHelper); break;
                }
            case 101940:
                {
                    dl_controller = new Vivli_Controller(_monDataLayer, _loggingHelper); break;
                }
        }

        if (dl_controller is not null)
        {
            // Instantiate a source controller class and call the 
            // 'ObtainData' routine with parameters.
            
            SourceController sc = new(dl_controller);
            saf = await sc.ObtainDataFromSourceAsync(opts, source, saf);        
            _loggingHelper.LogRes(saf);
        }
        
        // Store the saf log record (unless specifically requested not to).
        
        if (opts.NoLogging is not true)
        {
            _monDataLayer.InsertSAFEventRecord(saf);
        }
        _loggingHelper.CloseLog();
    }
}




