using MDR_Downloader;
using System.ComponentModel.Design;

LoggingHelper _logging_helper = new();
MonDataLayer _mon_data_layer = new(_logging_helper);
ParameterChecker _param_checker = new(_logging_helper, _mon_data_layer);

ParamsCheckResult paramsCheck = _param_checker.CheckParams(args);
if (paramsCheck.ParseError || paramsCheck.ValidityError)
{
    return -1;  // end program, parameter errors should have been logged
}
else
{
    try
    {
        // Should be able to proceed - (opts and srce are known to be non-null)
        // open log file, create Downloader class and call the main downloader function

        var opts = paramsCheck.Pars!;     
        var source = paramsCheck.Srce!;
        _logging_helper.OpenLogFile(opts.FileName, source.database_name!);

        Downloader dl = new (_mon_data_layer, _logging_helper);
        await dl.RunDownloaderAsync(opts, source);

        return 0;
    }
    catch (Exception e)
    {
        // if an error bubbles up to here there is an issue with the code.

        _logging_helper.LogHeader("UNHANDLED EXCEPTION");
        _logging_helper.LogCodeError("MDR_Downloader application aborted", e.Message, e.StackTrace);
        _logging_helper.CloseLog();

        return -1;
    }
}





