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

        switch (source.id)
        {
            case 101900:
                {
                    BioLINCC_Controller biolincc_controller = new(_mon_data_layer, _logging_helper);
                    res = await biolincc_controller.ObtainDatafromSourceAsync(opts, source);
                    break;
                }
            case 101901:
                {
                    Yoda_Controller yoda_controller = new(_mon_data_layer, _logging_helper);
                    res = await yoda_controller.ObtainDatafromSourceAsync(opts, source);
                    break;
                }
            case 100120:
                {
                    CTG_Controller ctg_controller = new(_mon_data_layer, _logging_helper);
                    res = await ctg_controller.ObtainDatafromSourceAsync(opts, source);
                    break;
                }
            case 100126:
                {
                    ISRCTN_Controller isrctn_controller = new(_mon_data_layer, _logging_helper);
                    res = await isrctn_controller.ObtainDatafromSourceAsync(opts, source); 
                    break;
                }
            case 100123:
                {
                    EUCTR_Controller euctr_controller = new(_mon_data_layer, _logging_helper);
                    res = await euctr_controller.ObtainDatafromSourceAsync(opts, source); 
                    break;
                }
            case 100115:
                {
                    WHO_Controller who_controller = new(_mon_data_layer, _logging_helper);
                    res = await who_controller.ObtainDatafromSourceAsync(opts, source);
                    break;
                }
            case 100135:
                {
                    PubMed_Controller pubmed_controller = new(_mon_data_layer, _logging_helper);
                    res = await pubmed_controller.ObtainDatafromSourceAsync(opts, source);
                    break;
                }
            case 101940:
                {
                    Vivli_Controller vivli_controller = new(_mon_data_layer, _logging_helper);
                    res = await vivli_controller.ObtainDatafromSourceAsync(opts, source);
                    break;
                }
        }

        // Tidy up and ensure logging up to date.
        // Store the saf log record (unless specifically requested not to).
        
        saf.time_ended = DateTime.Now;
        saf.num_records_checked = res.num_checked;
        saf.num_records_downloaded = res.num_downloaded;
        saf.num_records_added = res.num_added;
        _logging_helper.LogRes(res);
        if (opts.NoLogging is null || opts.NoLogging == false)
        {
            _mon_data_layer.InsertSAFEventRecord(saf);
        }
        _logging_helper.CloseLog();
    }
}




