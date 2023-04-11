namespace MDR_Downloader;

public interface IMonDataLayer
{
    public string? PubmedAPIKey { get; }
    public Credentials Credentials { get; }
    
    public Source? FetchSourceParameters(int source_id);
    public DateTime? ObtainLastDownloadDate(int source_id);
    public DateTime? ObtainLastDownloadDateWithFilter(int source_id, int filter_id);
    public DLType FetchTypeParameters(int sftype_id);
    public int GetNextDownloadId();
    public StudyFileRecord? FetchStudyFileRecord(string sd_id, string db_name);
    public ObjectFileRecord? FetchObjectFileRecord(string sd_id);
    public IEnumerable<StudyFileRecord> FetchStudyIds();
    public bool UpdateDLEventRecord(DLEvent dl);

    public bool UpdateStudyLog(string sd_sid, string? remote_url,
        int? dl_id, DateTime? last_revised_date, string? full_path);
    
    public bool UpdateWhoStudyLog(string db_name, string sd_sid, string? remote_url,
        int? dl_id, DateTime? last_revised_date, string? full_path);
    
    public bool UpdateObjectLog(string sd_oid, string? remote_url,
        int? dl_id, DateTime? last_revised_date, string? full_path);

    public bool Downloaded_recently(string sd_sid, int days_ago);
    public bool Downloaded_recently_with_link(string sd_sid, int days_ago);
}