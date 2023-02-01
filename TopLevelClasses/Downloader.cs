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
    private readonly ILoggingHelper _logging_helper;
    private readonly IMonDataLayer _mon_data_layer;

    public Downloader(IMonDataLayer mon_data_layer, ILoggingHelper logging_repo)
    {
        _logging_helper = logging_repo;
        _mon_data_layer = mon_data_layer;
    }

    public async Task RunDownloaderAsync(Options opts, Source source)
    {
        // Set up the search and fetch summary record for this download,
        // Log parameters, and establish appropriate controller class,
        // then call it with parameters.

        opts.saf_id = _mon_data_layer.GetNextSearchFetchId();
        SAFEvent saf = new(opts, source.id);
        _logging_helper.OpenLogFile(opts.FileName, source.database_name!);
        _logging_helper.LogCommandLineParameters(opts);
        
        DownloadResult res = new();
        ISourceController? dl_controller = null;
        switch (source.id)
        {
            case 101900:
                {
                    dl_controller = new BioLINCC_Controller(_mon_data_layer, _logging_helper); break;
                }
            case 101901:
                {
                    dl_controller = new Yoda_Controller(_mon_data_layer, _logging_helper); break;
                }
            case 100120:
                {
                    dl_controller = new CTG_Controller(_mon_data_layer, _logging_helper); break;
                }
            case 100126:
                {
                    dl_controller = new ISRCTN_Controller(_mon_data_layer, _logging_helper); break;
                }
            case 100123:
                {
                    dl_controller = new EUCTR_Controller(_mon_data_layer, _logging_helper); break;
                }
            case 100115:
                {
                    dl_controller = new WHO_Controller(_mon_data_layer, _logging_helper); break;
                }
            case 100135:
                {
                    dl_controller = new PubMed_Controller(_mon_data_layer, _logging_helper); break;
                }
            case 101940:
                {
                    dl_controller = new Vivli_Controller(_mon_data_layer, _logging_helper); break;
                }
        }

        if (dl_controller is not null)
        {
            res = await dl_controller.ObtainDataFromSourceAsync(opts, source);
            saf.time_ended = DateTime.Now;
            saf.num_records_checked = res.num_checked;
            saf.num_records_downloaded = res.num_downloaded;
            saf.num_records_added = res.num_added;
        }
        
        // Tidy up and ensure logging up to date.
        // Store the saf log record (unless specifically requested not to).
        
        _logging_helper.LogRes(res);
        if (opts.NoLogging is null || opts.NoLogging == false)
        {
            _mon_data_layer.InsertSAFEventRecord(saf);
        }
        _logging_helper.CloseLog();
    }
}




