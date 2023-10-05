using Dapper;
using Dapper.Contrib.Extensions;
using Npgsql;

namespace MDR_Downloader;

public class MonDataLayer : IMonDataLayer
{
    private readonly ILoggingHelper _logging_helper;
    private readonly ICredentials _credentials;
    
    private readonly string monConnString;
    private readonly string pubmedAPIKey;
    private Source? source;
    private string thisDBconnString = "";
    
    public MonDataLayer(ILoggingHelper logging_helper, ICredentials credentials)
    {
        _logging_helper = logging_helper;
        _credentials = credentials;
        
        monConnString = credentials.GetConnectionString("mon");
        pubmedAPIKey = credentials.GetPubMedApiKey();
    }

    public string PubmedAPIKey => pubmedAPIKey;
    
    public Credentials Credentials => (Credentials)_credentials;

    public Source? FetchSourceParameters(int source_id)
    {
        using NpgsqlConnection Conn = new(monConnString);
        source = Conn.Get<Source>(source_id);
        thisDBconnString = _credentials.GetConnectionString(source.database_name!);
        return source;
    }

    public DateTime? ObtainLastDownloadDate(int source_id)
    {
        using NpgsqlConnection Conn = new(monConnString);
        string sql_string = $@"select max(time_ended) from sf.dl_events 
                               where source_id = {source_id}";
        return Conn.QuerySingleOrDefault<DateTime>(sql_string);
    }


    public DateTime? ObtainLastDownloadDateWithFilter(int source_id, int filter_id)
    {
        using NpgsqlConnection Conn = new(monConnString);
        string sql_string = $@"select max(time_ended) from sf.dl_events 
                               where source_id = {source_id} and filter_id = {filter_id}";
        return Conn.QuerySingleOrDefault<DateTime>(sql_string);
    }


    public DLType FetchTypeParameters(int dl_type_id)
    {
        using NpgsqlConnection Conn = new(monConnString);
        return Conn.Get<DLType>(dl_type_id);
    }


    public int GetNextDownloadId()
    {
        using NpgsqlConnection Conn = new(monConnString);
        string sql_string = "select max(id) from sf.dl_events ";
        int last_id = Conn.ExecuteScalar<int>(sql_string);
        int new_id = last_id + 1;
        
        // create the new record
        sql_string = $"Insert into sf.dl_events(id) values ({new_id})";
        Conn.Execute(sql_string);
        return new_id;
    }
    

    public StudyFileRecord? FetchStudyFileRecord(string sd_sid, string db_name = "")
    {
        string connString = db_name == "" ? thisDBconnString 
                                          : _credentials.GetConnectionString(db_name); 

        using NpgsqlConnection conn = new(connString);
        string sql_string = @$"select id, sd_sid, remote_url, last_revised,
                    download_status, local_path, last_dl_id, last_downloaded, 
                    last_harvest_id, last_harvested, last_import_id, last_imported 
                    from mn.source_data where sd_sid = '{sd_sid}';";
        return conn.Query<StudyFileRecord>(sql_string).FirstOrDefault();
    }

    
    public ObjectFileRecord? FetchObjectFileRecord(string sd_oid)
    {
        using NpgsqlConnection conn = new(thisDBconnString);
        string sql_string = @$"select id, sd_oid, remote_url, last_revised, 
                   download_status, local_path, last_dl_id, last_downloaded, 
                   last_harvest_id, last_harvested, last_import_id, last_imported 
                   from mn.source_data where sd_oid = '{sd_oid}';";
        return conn.Query<ObjectFileRecord>(sql_string).FirstOrDefault();
    }
    

    // The function below used for biolincc only.
    
    public IEnumerable<StudyFileRecord> FetchStudyIds()
    {
        string sql_string = $@"select id, sd_sid, local_path 
            from mn.source_data 
            order by local_path";
        using NpgsqlConnection Conn = new(thisDBconnString);
        return Conn.Query<StudyFileRecord>(sql_string);
    }

    public bool UpdateDLEventRecord(DLEvent dl)
    {
        // Occasionally a simple insert of a new record at the end of a download process can result 
        // in an error - if a long running DL takes place at the same time as a pre-scheduled one.
        // In that case the id originally set up will have already been used.
        // To get round that a new DL record should be created at the beginning of each DL, 
        // and then updated with the dl details at the end.
        
        using NpgsqlConnection Conn = new(monConnString);
        return Conn.Update(dl);
    }

    private void UpdateStudyFileRec(StudyFileRecord file_record, string db_name = "")
    {
        string connString = db_name == "" ? thisDBconnString 
            : _credentials.GetConnectionString(db_name); 
        using NpgsqlConnection conn = new(connString);
        conn.Update(file_record);
    }
   
    private void UpdateObjectFileRec(ObjectFileRecord file_record)
    {
        using NpgsqlConnection conn = new(thisDBconnString);
        conn.Update(file_record);
    }

    private void InsertStudyFileRec(StudyFileRecord file_record, string db_name = "")
    {
        string connString = db_name == "" ? thisDBconnString 
            : _credentials.GetConnectionString(db_name); 
        using NpgsqlConnection conn = new(connString);
        conn.Insert(file_record);
    }
   
    private void InsertObjectFileRec(ObjectFileRecord file_record)
    {
        using NpgsqlConnection conn = new(thisDBconnString);
        conn.Insert(file_record);
    }
    
    public bool IsTestStudy(string sd_sid)
    {
        using NpgsqlConnection conn = new(thisDBconnString);
        string sql_string = @$"select for_testing
                    from mn.source_data where sd_sid = '{sd_sid}';";
        bool? res = conn.QueryFirstOrDefault<bool?>(sql_string);
        return res == true;
    }
    
    public bool IsWHOTestStudy(string dbname, string sd_sid)
    {
        string whoConnString = _credentials.GetConnectionString(dbname);
        using NpgsqlConnection conn = new(whoConnString);
        string sql_string = @$"select for_testing
                    from mn.source_data where sd_sid = '{sd_sid}';";
        bool? res = conn.QueryFirstOrDefault<bool?>(sql_string);
        return res == true;
    }

    public bool IsTestObject(string sd_oid)
    {
        string sql_string = @$"select for_testing
                    from mn.source_data where sd_oid = '{sd_oid}';";
        using NpgsqlConnection conn = new(thisDBconnString);
        bool? res = conn.QueryFirstOrDefault<bool?>(sql_string);
        return res == true;
    }

    public bool UpdateWhoStudyLog(string db_name, string sd_sid, string? remote_url,
        int? dl_id, DateTime? last_revised_date, string? full_path)
    {
        bool added = false; // indicates if a new record or update of an existing one

        // Get the source data record and modify it or add a new one.
        
        StudyFileRecord? file_record = FetchStudyFileRecord(sd_sid, db_name);
        try
        {
            if (file_record is null)
            {
                file_record = new StudyFileRecord(sd_sid, remote_url, dl_id,
                    last_revised_date, full_path);
                InsertStudyFileRec(file_record, db_name);
                added = true;
            }
            else
            {
                file_record.remote_url = remote_url;
                file_record.last_dl_id = dl_id;
                file_record.last_revised = last_revised_date;
                file_record.download_status = 2;
                file_record.last_downloaded = DateTime.Now;
                file_record.local_path = full_path;

                UpdateStudyFileRec(file_record, db_name);
            }

            return added;
        }
        catch(Exception e)
        {
            _logging_helper.LogError("In UpdateStudyDownloadLog: " + e.Message);
            return false;
        }
    }
    
    
    public bool UpdateStudyLog(string sd_sid, string? remote_url,
                     int? dl_id, DateTime? last_revised_date, string? full_path)
    {
        bool added = false; // indicates if a new record or update of an existing one

        // Get the source data record and modify it or add a new one.
        
        StudyFileRecord? file_record = FetchStudyFileRecord(sd_sid);
        try
        {
            if (file_record is null)
            {
                file_record = new StudyFileRecord(sd_sid, remote_url, dl_id,
                                                last_revised_date, full_path);
                InsertStudyFileRec(file_record);
                added = true;
            }
            else
            {
                file_record.remote_url = remote_url;
                file_record.last_dl_id = dl_id;
                file_record.last_revised = last_revised_date;
                file_record.download_status = 2;
                file_record.last_downloaded = DateTime.Now;
                file_record.local_path = full_path;

                UpdateStudyFileRec(file_record);
            }

            return added;
        }
        catch(Exception e)
        {
            _logging_helper.LogError("In UpdateStudyDownloadLog: " + e.Message);
            return false;
        }
    }

    public bool UpdateObjectLog(string sd_oid, string? remote_url,
                     int? dl_id, DateTime? last_revised_date, string? full_path)
    {
        bool added = false; // indicates if a new record or update of an existing one

        // Get the source data record and modify it or add a new one...
        ObjectFileRecord? file_record = FetchObjectFileRecord(sd_oid);

        if (file_record is null)
        {
            file_record = new ObjectFileRecord(sd_oid, remote_url, dl_id,
                                            last_revised_date, full_path);
            InsertObjectFileRec(file_record);
            added = true;
        }
        else
        {
            file_record.remote_url = remote_url;
            file_record.last_dl_id = dl_id;
            file_record.last_revised = last_revised_date;
            file_record.download_status = 2;
            file_record.last_downloaded = DateTime.Now;
            file_record.local_path = full_path;
            UpdateObjectFileRec(file_record);
        }

        return added;
    }

    public bool Downloaded_recently(string sd_sid, int days_ago)
    {
        string sql_string = $@"select id from mn.source_data 
                               where last_downloaded::date >= now()::date - {days_ago} 
                               and sd_sid = '{sd_sid}'";
        using NpgsqlConnection conn = new(thisDBconnString);
        return conn.Query<int>(sql_string).FirstOrDefault() > 0;
    }
     
    
    public bool Downloaded_recently_with_link(string details_link, int days_ago)
    {
        string sql_string = $@"select id from mn.source_data 
                               where last_downloaded::date >= now()::date - {days_ago} 
                               and remote_url = '{details_link}'";
        using NpgsqlConnection conn = new(thisDBconnString);
        return conn.Query<int>(sql_string).FirstOrDefault() > 0;
    }
    


}

