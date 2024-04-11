﻿using CommandLine;
using System.Text.RegularExpressions;

namespace MDR_Downloader;

internal class ParameterChecker
{
    private readonly ILoggingHelper _logging_helper;
    private readonly IMonDataLayer _mon_data_layer;

    internal ParameterChecker(ILoggingHelper logging_helper, IMonDataLayer mon_data_layer)
    {
        _logging_helper = logging_helper;
        _mon_data_layer = mon_data_layer;
    }

    internal ParamsCheckResult CheckParams(string[]? args)
    {
        // Calls the CommandLine parser. If an error in the initial parsing, log it 
        // and return an error. If parameters can be passed, check their validity
        // and if invalid log the issue and return an error, otherwise return the 
        // parameters, processed as an instance of the Options class, and the source.
        // N.B. No log file is available yet - needs to be created in the error parser.

        var parsedArguments = Parser.Default.ParseArguments<Options>(args);
        if (parsedArguments.Errors.Any())
        {
            LogParseError(((NotParsed<Options>)parsedArguments).Errors);
            return new ParamsCheckResult(true, false, null, null);
        }

        var opts = parsedArguments.Value; 
        return CheckArgumentValuesAreValid(opts);  
    }

    internal ParamsCheckResult CheckArgumentValuesAreValid(Options opts)
    {
        // 'opts' is passed by reference and may be changed by the checking mechanism.
        // N.B. No log file is available yet - needs to be created in exception handler.

        try
        {
            // If the 'pubmed publishers' option (-P) is selected set s = 100135, t = 302
            // whatever the values passed
            if (opts.PubMedPubs is true)
            {
                opts.SourceId = 100135;
                opts.FetchTypeId = 302;
            }
            
            // First check source id is valid. If so, set up local folder path (DN name for WHO sourcs added later).

            Source? source = _mon_data_layer.FetchSourceParameters(opts.SourceId);
            if (source is null)
            {
                throw new ArgumentException("The first argument does not correspond to a known source");
            }
            else
            {
                source.local_folder = _logging_helper.DataFolderPath;
                if (source.id != 100135)
                {
                    source.local_folder += source.database_name;
                }
            }

            // Check source fetch type id is valid and obtain parameter requirements associated with this type. 

            DLType? dl_type = _mon_data_layer.FetchTypeParameters(opts.FetchTypeId);
            if (dl_type is null)
            {
                throw new ArgumentException("The type argument does not correspond to a known search / fetch type");
            }

            // Examine the focused search type, if any, before the date
            // as search type may be required in the fetch last date function.
                                
            if (dl_type.requires_search_id is true)
            {
                if (opts.FocusedSearchId == 0 || opts.FocusedSearchId is null)
                {
                    string error_message = "This search fetch type requires an integer referencing a search type";
                    error_message += " and no valid filter (search) id is supplied";
                    throw new ArgumentException(error_message);
                }
            }

            // If a date is required check one is present and is valid. 
            // It should be in the ISO YYYY-MM-DD format.

            if (dl_type.requires_cutoff_date is true)
            {
                if (!string.IsNullOrEmpty(opts.CutoffDateAsString))
                {
                    string cutoffDateAsString = opts.CutoffDateAsString;
                    if (Regex.Match(cutoffDateAsString, @"^20\d{2}-[0,1]\d{1}-[0, 1, 2, 3]\d{1}$").Success)
                    {
                        opts.CutoffDate = new DateTime(
                                    Int32.Parse(cutoffDateAsString.Substring(0, 4)),
                                    Int32.Parse(cutoffDateAsString.Substring(5, 2)),
                                    Int32.Parse(cutoffDateAsString.Substring(8, 2)));
                    }
                }
                else
                {
                    // Try and find the last download date and use that
                    if (dl_type.requires_search_id is true && opts.FocusedSearchId is not null)
                    {
                        opts.CutoffDate = _mon_data_layer.ObtainLastDownloadDateWithFilter(source.id, (int)opts.FocusedSearchId);
                    }
                    else
                    {
                        opts.CutoffDate = _mon_data_layer.ObtainLastDownloadDate(source.id);
                    }
                }

                if (opts.CutoffDate is null)
                {
                    string error_message = "This search fetch type requires a cutoff date";
                    error_message += " in the format YYYY-MM-DD and this is missing";
                    throw new ArgumentException(error_message);
                }
            }

            if (dl_type.requires_end_date is true)
            {
                if (!string.IsNullOrEmpty(opts.EndDateAsString))
                {
                    string EndDateAsString = opts.EndDateAsString;
                    if (Regex.Match(EndDateAsString, @"^20\d{2}-[0,1]\d{1}-[0, 1, 2, 3]\d{1}$").Success)
                    {
                        opts.EndDate = new DateTime(
                                    Int32.Parse(EndDateAsString.Substring(0, 4)),
                                    Int32.Parse(EndDateAsString.Substring(5, 2)),
                                    Int32.Parse(EndDateAsString.Substring(8, 2)));
                    }
                }

                if (opts.EndDate is null)
                {
                    string error_message = "This search fetch type requires an end date";
                    error_message += " in the format YYYY-MM-DD and this is missing";
                    throw new ArgumentException(error_message);
                }
            }


            // If a file (or for some download types a folder path) is required check a name is 
            // supplied and that it corresponds to an existing file or folder.

            if (dl_type.requires_file is true)
            {
                if (string.IsNullOrEmpty(opts.FileName) || !File.Exists(opts.FileName))
                {
                    string error_message = "This search fetch type requires a file path";
                    error_message += " and no valid file path and name is supplied";
                    throw new ArgumentException(error_message);
                }
            }

            if (dl_type.requires_folder is true)
            {
                if (string.IsNullOrEmpty(opts.FileName) || !Directory.Exists(opts.FileName))
                {
                    string error_message = "This search fetch type requires a folder path";
                    error_message += " and no valid folder path supplied";
                    throw new ArgumentException(error_message);
                }
            }


            if (dl_type.requires_startandendnumbers is true)
            {
                if (!opts.StartPage.HasValue || opts.StartPage == 0)
                {
                    string error_message = "This search fetch type requires an integer";
                    error_message += " value for a start page index (> 0) and none was supplied";
                    throw new ArgumentException(error_message);
                }
                if (!opts.EndPage.HasValue || opts.EndPage == 0)
                {
                    string error_message = "This search fetch type requires an integer";
                    error_message += " value for an end page index (> 0) and none was supplied";
                    throw new ArgumentException(error_message);
                }

            }

            if (dl_type.requires_offsetandamountids is true)
            {
                if (!opts.OffsetIds.HasValue)
                {
                    string error_message = "This search fetch type requires an integer";
                    error_message += " value for an Id offset (within an ordered list of";
                    error_message += " study ids, and none was supplied";
                    throw new ArgumentException(error_message);
                }
                if (!opts.AmountIds.HasValue)
                {
                    string error_message = "This search fetch type requires an integer";
                    error_message += " value for the number of Ids to be examined after";
                    error_message += " the offset and none was supplied";
                    throw new ArgumentException(error_message);
                }
            }

            if (dl_type.requires_prev_dl_ids is true)
            {
                if (opts.PreviousSearches?.Any() is true)
                {
                    foreach (int i in opts.PreviousSearches)
                    {
                        opts.previous_dl_ids += ", " + i.ToString();
                    }
                    opts.previous_dl_ids = opts.previous_dl_ids![2..];
                }
                else
                {
                    string error_message = "This search fetch type requires one or more";
                    error_message += " previous search-fetch ids and none were supplied.";
                    throw new ArgumentException(error_message);
                }
            }

            // Parameters are valid - return opts and the source.

            return new ParamsCheckResult(false, false, opts, source);
        }
 
        catch (Exception e)
        {
            _logging_helper.OpenNoSourceLogFile();
            _logging_helper.LogHeader("INVALID PARAMETERS");
            _logging_helper.LogCommandLineParameters(opts);
            _logging_helper.LogCodeError("MDR_Downloader application aborted", e.Message, e.StackTrace ?? "");
            _logging_helper.CloseLog();
            return new ParamsCheckResult(false, true, null, null);
        }
    }


    internal void LogParseError(IEnumerable<Error> errs)
    {
        _logging_helper.OpenNoSourceLogFile();
        _logging_helper.LogHeader("UNABLE TO PARSE PARAMETERS");
        _logging_helper.LogHeader("Error in input parameters");
        _logging_helper.LogLine("Error in the command line arguments - they could not be parsed");

        int n = 0;
        foreach (Error e in errs)
        {
            n++;
            _logging_helper.LogParseError("Error {n}: Tag was {Tag}", n.ToString(), e.Tag.ToString());
            if (e.GetType().Name == "UnknownOptionError")
            {
                _logging_helper.LogParseError("Error {n}: Unknown option was {UnknownOption}", n.ToString(), ((UnknownOptionError)e).Token);
            }
            if (e.GetType().Name == "MissingRequiredOptionError")
            {
                _logging_helper.LogParseError("Error {n}: Missing option was {MissingOption}", n.ToString(), ((MissingRequiredOptionError)e).NameInfo.NameText);
            }
            if (e.GetType().Name == "BadFormatConversionError")
            {
                _logging_helper.LogParseError("Error {n}: Wrongly formatted option was {MissingOption}", n.ToString(), ((BadFormatConversionError)e).NameInfo.NameText);
            }
        }
        _logging_helper.LogLine("MDR_Downloader application aborted");
        _logging_helper.CloseLog();
    }
}


public class Options
{
    // Lists the command line arguments and options

    [Option('s', "source", Required = true, HelpText = "Integer id of data source.")]
    public int SourceId { get; set; }

    [Option('t', "dl_type_id", Required = true, HelpText = "Integer id representing type of download.")]
    public int FetchTypeId { get; set; }

    public int? dl_id { get; set; }

    [Option('d', "cutoff_date", Required = false, HelpText = "Only data revised or added since this date will be considered")]
    public string? CutoffDateAsString { get; set; }

    public DateTime? CutoffDate { get; set; }

    [Option('e', "end_date", Required = false, HelpText = "Only data revised or added before this date will be considered")]
    public string? EndDateAsString { get; set; }

    public DateTime? EndDate { get; set; }

    [Option('f', "file_name", Required = false, HelpText = "Filename of csv file with data.")]
    public string? FileName { get; set; }

    [Option('o', "offset for id based download", Required = false, HelpText = "Integer indicating the start line number of Id sets to be used in fetching data (ctg only).")]
    public int? OffsetIds { get; set; }  
    
    [Option('a', "amount for id based download", Required = false, HelpText = "Integer indicating the number of lines of Id sets to be used (ctg only).")]
    public int? AmountIds { get; set; }

    [Option('I', "skip_recent", Required = false, HelpText = "Integer id representing the number of days ago, to skip recent downloads (0 = today).")]
    public int? SkipRecentDays { get; set; }

    [Option('L', "no_Logging", Required = false, HelpText = "If present prevents the logging record in sf.dl_events")]
    public bool? NoLogging { get; set; }

    [Option('S', "start_page", Required = false, HelpText = "First summary page number to be considered if downloading all EU CTR records (starts at 1)")]
    public int? StartPage { get; set; }

    [Option('E', "end_page", Required = false, HelpText = "Last summary page number to be considered if downloading all EU CTR record")]
    public int? EndPage { get; set; }

    [Option('q', "focused-search_id", Required = false, HelpText = "Integer id representing id of focused search / fetch.")]
    public int? FocusedSearchId { get; set; }

    [Option('p', "previous_searches", Required = false, Separator = ',', HelpText = "One or more ids of the search(es) that will be used to retrieve the data")]
    public IEnumerable<int>? PreviousSearches { get; set; }
    
    public string? previous_dl_ids { get; set; }
    
    [Option('P', "pubmed_publishers", Required = false, Separator = ',', HelpText = "Downloads publisher data from NLM for use by PubMed routines")]
    public bool? PubMedPubs { get; set; }
    
  

}

internal class ParamsCheckResult
{
    internal bool ParseError { get; set; }
    internal bool ValidityError { get; set; }
    internal Options? Pars { get; set; }
    internal Source? Source { get; set; }

    internal ParamsCheckResult(bool _ParseError, bool _ValidityError, Options? _Pars, Source? _Source)
    {
        ParseError = _ParseError;
        ValidityError = _ValidityError;
        Pars = _Pars;
        Source = _Source;
    }
}

