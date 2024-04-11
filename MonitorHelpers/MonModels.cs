using Dapper.Contrib.Extensions;

namespace MDR_Downloader;

[Table("sf.source_parameters")]
public class Source
{
    public int id { get; }
    public string? source_type { get; }
    public int? preference_rating { get; }
    public string? database_name { get; }
    public string? repo_name { get; }
    public string? db_conn { get; set; }
    public bool? uses_who_harvest { get; }
    public int? harvest_chunk { get; }
    public string? local_folder { get; set; }
    public bool? local_files_grouped { get; }
    public int? grouping_range_by_id { get; }
    public string? local_file_prefix { get; }
}


[Table("sf.dl_types")]
public class DLType
{
    public int id { get; set; }
    public string? name { get; set; }
    public bool? requires_cutoff_date { get; set; }
    public bool? requires_end_date { get; set; }
    public bool? requires_file { get; set; }
    public bool? requires_folder { get; set; }
    public bool? requires_startandendnumbers { get; set; }
    public bool? requires_offsetandamountids { get; set; }
    public bool? requires_search_id { get; set; }
    public bool? requires_prev_dl_ids { get; set; }
    public string? description { get; set; }
    public string? list_order { get; set; }
}


[Table("sf.dl_events")]
public class DLEvent
{
    [ExplicitKey]
    public int? id { get; set; }
    public int? source_id { get; set; }
    public DateTime? time_started { get; set; }
    public DateTime? time_ended { get; set; }
    public int? num_records_checked { get; set; }
    public int? num_records_added { get; set; }
    public int? num_records_downloaded { get; set; }
    public int? type_id { get; set; }
    public string? filefolder_path { get; set; }
    public DateTime? cut_off_date { get; set; }
    public DateTime? end_date { get; set; }
    public int? filter_id { get; set; }
    public string? previous_dl_ids { get; set; }
    public int? start_page{ get; set; }
    public int? end_page{ get; set; }
    public int? ids_offset{ get; set; }
    public int? ids_amount { get; set; }
    public string? comments { get; set; }

    public DLEvent() { }

    public DLEvent(Options _opts, int? _source_id)
    {
        id = _opts.dl_id;
        source_id = _source_id;
        type_id = _opts.FetchTypeId;
        filefolder_path = _opts.FileName;
        cut_off_date = _opts.CutoffDate;
        end_date = _opts.EndDate;
        filter_id = _opts.FocusedSearchId;
        previous_dl_ids = _opts.previous_dl_ids;
        start_page = _opts.StartPage;
        end_page = _opts.EndPage;
        ids_offset = _opts.OffsetIds;
        ids_amount = _opts.AmountIds;
        time_started = DateTime.Now;
    }
}


[Table("mn.source_data")]
public class StudyFileRecord
{
    [Key] 
    public int id { get; set; }
    public string sd_sid { get; set; } = null!;
    public string? remote_url { get; set; }
    public DateTime? last_revised { get; set; }
    public int? download_status { get; set; }
    public string? local_path { get; set; }
    public int? last_dl_id { get; set; }
    public DateTime? last_downloaded { get; set; }
    public int? last_harvest_id { get; set; }
    public DateTime? last_harvested { get; set; }
    public int? last_import_id { get; set; }
    public DateTime? last_imported { get; set; }
    public int? last_coding_id { get; set; }
    public DateTime? last_coded { get; set; }
    public int? last_aggregation_id { get; set; }
    public DateTime? last_aggregated { get; set; }

    // constructor when a revision data can be expected (not always there)
    public StudyFileRecord(string _sd_sid, string? _remote_url, int? _last_dl_id,
                                          DateTime? _last_revised, string? _local_path)
    {
        sd_sid = _sd_sid;
        remote_url = _remote_url;
        last_dl_id = _last_dl_id;
        last_revised = _last_revised;
        download_status = 2;
        last_downloaded = DateTime.Now;
        local_path = _local_path;
    }

    public StudyFileRecord()
    { }

}

// This only used in the context of PubMed (or other later object based resources)

[Table("mn.source_data")]
public class ObjectFileRecord
{
    [Key]
    public int id { get; set; }
    public string? sd_oid { get; set; }
    public string? remote_url { get; set; }
    public DateTime? last_revised { get; set; }
    public int? download_status { get; set; }
    public string? local_path { get; set; }
    public int? last_dl_id { get; set; }
    public DateTime? last_downloaded { get; set; }
    public int? last_harvest_id { get; set; }
    public DateTime? last_harvested { get; set; }
    public int? last_import_id { get; set; }
    public DateTime? last_imported { get; set; }
    public int? last_coding_id { get; set; }
    public DateTime? last_coded { get; set; }
    public int? last_aggregation_id { get; set; }
    public DateTime? last_aggregated { get; set; }


    // Constructor when a revision date can be expected (not always there).
    
    public ObjectFileRecord(string? _sd_oid, string?_remote_url, int? _last_dl_id,
                              DateTime? _last_revised, string? _local_path)
    {
        sd_oid = _sd_oid;
        remote_url = _remote_url;
        last_dl_id = _last_dl_id;
        last_revised = _last_revised;
        download_status = 2;
        last_downloaded = DateTime.Now;
        local_path = _local_path;
    }

    // Constructor when a new file record required, when a pmid new to the system is found.
    
    public ObjectFileRecord(string? _sd_oid, string? _remote_url, int? _last_dl_id)
    {
        sd_oid = _sd_oid;
        remote_url = _remote_url;
        last_dl_id = _last_dl_id;
        download_status = 0;
    }

    public ObjectFileRecord()
    { }

}


public class DownloadResult
{
    public int num_checked { get; set; }
    public int num_added { get; set; }
    public int num_downloaded { get; set; }
    public string? error_message { get; set; }

    public DownloadResult()
    {
        num_checked = 0;
        num_added = 0;
        num_downloaded = 0;
    }

    public DownloadResult(string _error_message)
    {
        num_checked = 0;
        num_added = 0;
        num_downloaded = 0;
        error_message = _error_message;
    }

    public DownloadResult(int _num_checked, int _num_added, int _num_downloaded, string _error_message)
    {
        num_checked = _num_checked;
        num_added = _num_added;
        num_downloaded = _num_downloaded;
        error_message = _error_message;
    }
}

