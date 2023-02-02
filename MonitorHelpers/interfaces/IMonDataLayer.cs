namespace MDR_Downloader;

public interface IMonDataLayer
{
    
    
    public string? PubmedAPIKey { get; }
    public Credentials Credentials { get; }
    
    public Source? FetchSourceParameters(int source_id);
    public DateTime? ObtainLastDownloadDate(int source_id);
    public DateTime? ObtainLastDownloadDateWithFilter(int source_id, int filter_id);
    public SFType FetchTypeParameters(int sftype_id);
    public int GetNextSearchFetchId();
    public StudyFileRecord? FetchStudyFileRecord(string sd_id, int? source_id);
    public ObjectFileRecord? FetchObjectFileRecord(string sd_id, int source_id);
    public IEnumerable<StudyFileRecord> FetchStudyIds(int source_id);
    public int InsertSAFEventRecord(SAFEvent saf);
    public bool StoreStudyFileRec(StudyFileRecord file_record);
    public bool StoreObjectFileRec(ObjectFileRecord file_record);
    public int InsertStudyFileRec(StudyFileRecord file_record);
    public int InsertObjectFileRec(ObjectFileRecord file_record);

    public bool UpdateStudyDownloadLog(int? source_id, string sd_id, string? remote_url,
        int? saf_id, DateTime? last_revised_date, string? full_path);

    public bool UpdateObjectDownloadLog(int source_id, string sd_id, string remote_url,
        int saf_id, DateTime? last_revised_date, string full_path);

    public bool Downloaded_recently(int source_id, string sd_sid, int days_ago);
}