using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MDR_Downloader;

string AssemblyLocation = Assembly.GetExecutingAssembly().Location;
string? BasePath = Path.GetDirectoryName(AssemblyLocation);
if (string.IsNullOrWhiteSpace(BasePath))
{
    return -1;
}

var configFiles = new ConfigurationBuilder()
    .SetBasePath(BasePath)
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
    .Build();

// Set up the host for the app,
// adding the services used in the system to support DI.
// Note ALL listed services are singletons.

IHost host = Host.CreateDefaultBuilder()
    .UseContentRoot(BasePath)
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddConfiguration(configFiles);
    })
    .ConfigureServices((services) =>
    {
        services.AddSingleton<ICredentials, Credentials>();
        services.AddSingleton<ILoggingHelper, LoggingHelper>();
        services.AddSingleton<IMonDataLayer, MonDataLayer>();
    })
    .Build();

// Establish logger, at this stage as an object reference
// because the log file(s) are yet to be opened, and 
// establish a reference to a Monitor repository.

ILoggingHelper logging_helper = ActivatorUtilities.CreateInstance<ILoggingHelper>(host.Services);
IMonDataLayer mon_data_layer = ActivatorUtilities.CreateInstance<IMonDataLayer>(host.Services);

// Create a new parameter checker class, which first checks
// if the program's arguments can be parsed and, if they can,
// then checks if they are valid.
// If both tests are passed the object returned includes both the
// original arguments and the 'source' object with details of the
// single data source being downloaded. 

ParameterChecker _param_checker = new(logging_helper, mon_data_layer);
ParamsCheckResult paramsCheck = _param_checker.CheckParams(args);
if (paramsCheck.ParseError || paramsCheck.ValidityError)
{
    // End program, parameter errors should have been logged
    // in a 'no source' file by the ParameterChecker class.

    return -1;  
}
try
{
    // Should be able to proceed - (opts and source are known to be non-null).
    // Create Downloader class and call the main downloader function

    var opts = paramsCheck.Pars!;     
    var source = paramsCheck.Source!;
    Downloader dl = new(mon_data_layer, logging_helper);
    await dl.RunDownloaderAsync(opts, source);
    return 0;
}
catch (Exception e)
{
    // If an error bubbles up to here there is an issue with the code.
    // A file should normally have been created.

    if (logging_helper.LogFilePath == "")
    {
        logging_helper.OpenNoSourceLogFile();
    }
    logging_helper.LogHeader("UNHANDLED EXCEPTION");
    logging_helper.LogCodeError("MDR_Downloader application aborted", e.Message, e.StackTrace);
    logging_helper.CloseLog();

    return -1;
}






