using MDR_Downloader;

// Establish logger, at this stage as an object reference
// because the log file(s) are yet to be opened.
// Establish a reference to a Monitor repository and pass  
// both references to a new parameter checker class.

LoggingHelper _logging_helper = new();
MonDataLayer _mon_data_layer = new(_logging_helper);
ParameterChecker _param_checker = new(_logging_helper, _mon_data_layer);

// The parameter checker first checks if the program's arguments 
// can be parsed and if they can then checks if they are valid.
// If both tests are passed the object returned includes both the
// original arguments and the 'source' object with details of the
// single data source being downloaded. 

ParamsCheckResult paramsCheck = _param_checker.CheckParams(args);
if (paramsCheck.ParseError || paramsCheck.ValidityError)
{
    // End program, parameter errors should have been logged
    // in a 'no source' file by the ParameterChecker class.

    return -1;  
}
else
{
    try
    {
        // Should be able to proceed - (opts and srce are known to be non-null).
        // Open log file, create Downloader class and call the main downloader function

        var opts = paramsCheck.Pars!;     
        var source = paramsCheck.Srce!;
        _logging_helper.OpenLogFile(opts.FileName, source.database_name!);

        Downloader dl = new(_mon_data_layer, _logging_helper);
        await dl.RunDownloaderAsync(opts, source);

        return 0;
    }
    catch (Exception e)
    {
        // If an error bubbles up to here there is an issue with the code.

        _logging_helper.LogHeader("UNHANDLED EXCEPTION");
        _logging_helper.LogCodeError("MDR_Downloader application aborted", e.Message, e.StackTrace);
        _logging_helper.CloseLog();

        return -1;
    }
}





