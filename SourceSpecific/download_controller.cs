using System.Threading.Tasks;

namespace MDR_Downloader
{
    internal interface download_controller
    {
        Task<DownloadResult> ObtainDatafromSourceAsync();
    }
}

