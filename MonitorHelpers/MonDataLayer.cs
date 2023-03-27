using Dapper;
using Dapper.Contrib.Extensions;
using Npgsql;

namespace MDR_Downloader;

public class MonDataLayer : IMonDataLayer
{
    private readonly ILoggingHelper _logging_helper;
    private readonly ICredentials _credentials;
    
    private readonly string connString;
    private readonly string sql_file_select_string;
    private readonly string pubmedAPIKey;
    private Source? source;

    public MonDataLayer(ILoggingHelper logging_helper, ICredentials credentials)
    {
        _logging_helper = logging_helper;
        _credentials = credentials;
        
        connString = credentials.GetConnectionString("mon");
        pubmedAPIKey = credentials.GetPubMedApiKey();
 
        sql_file_select_string = "select id, source_id, sd_id, remote_url, last_revised, ";
        sql_file_select_string += " assume_complete, download_status, local_path, last_saf_id, last_downloaded, ";
        sql_file_select_string += " last_harvest_id, last_harvested, last_import_id, last_imported ";
    }

    public string PubmedAPIKey => pubmedAPIKey;
    
    public Credentials Credentials => (Credentials)_credentials;

    public Source? FetchSourceParameters(int source_id)
    {
        using NpgsqlConnection Conn = new(connString);
        source = Conn.Get<Source>(source_id);
        return source;
    }

    public DateTime? ObtainLastDownloadDate(int source_id)
    {
        using NpgsqlConnection Conn = new(connString);
        string sql_string = $@"select max(time_ended) from sf.saf_events 
                               where source_id = {source_id}";
        return Conn.QuerySingleOrDefault<DateTime>(sql_string);
    }


    public DateTime? ObtainLastDownloadDateWithFilter(int source_id, int filter_id)
    {
        using NpgsqlConnection Conn = new(connString);
        string sql_string = $@"select max(time_ended) from sf.saf_events 
                               where source_id = {source_id} and filter_id = {filter_id}";
        return Conn.QuerySingleOrDefault<DateTime>(sql_string);
    }


    public SFType FetchTypeParameters(int sf_type_id)
    {
        using NpgsqlConnection Conn = new(connString);
        return Conn.Get<SFType>(sf_type_id);
    }


    public int GetNextSearchFetchId()
    {
        using NpgsqlConnection Conn = new(connString);
        string sql_string = "select max(id) from sf.saf_events ";
        int last_id = Conn.ExecuteScalar<int>(sql_string);
        return last_id + 1;
    }


    public StudyFileRecord? FetchStudyFileRecord(string sd_id, int? source_id)
    {
        using NpgsqlConnection Conn = new(connString);
        string sql_string = sql_file_select_string;
        sql_string += " from sf.source_data_studies ";
        sql_string += " where sd_id = '" + sd_id + "' and source_id = " + source_id.ToString();
        return Conn.Query<StudyFileRecord>(sql_string).FirstOrDefault();
    }

    
    public ObjectFileRecord? FetchObjectFileRecord(string sd_id, int source_id)
    {
        using NpgsqlConnection Conn = new(connString);
        string sql_string = sql_file_select_string;
        sql_string += " from sf.source_data_objects ";
        sql_string += " where sd_id = '" + sd_id + "' and source_id = " + source_id.ToString();
        return Conn.Query<ObjectFileRecord>(sql_string).FirstOrDefault();
    }
    

    // Used for biolincc only.
    
    public IEnumerable<StudyFileRecord> FetchStudyIds(int source_id)
    {
        string sql_string = $@"select id, sd_id, local_path 
            from sf.source_data_studies 
            where source_id = {source_id} order by local_path";
        using NpgsqlConnection Conn = new(connString);
        return Conn.Query<StudyFileRecord>(sql_string);
    }

    public int InsertSAFEventRecord(SAFEvent saf)
    {
        using NpgsqlConnection Conn = new(connString);
        return (int)Conn.Insert(saf);
    }

    public bool StoreStudyFileRec(StudyFileRecord file_record)
    {
        using NpgsqlConnection conn = new(connString);
        return conn.Update(file_record);
    }

    public bool StoreObjectFileRec(ObjectFileRecord file_record)
    {
        using NpgsqlConnection conn = new(connString);
        return conn.Update(file_record);
    }

    public int InsertStudyFileRec(StudyFileRecord file_record)
    {
        using NpgsqlConnection conn = new(connString);
        return (int)conn.Insert(file_record);
    }

    public int InsertObjectFileRec(ObjectFileRecord file_record)
    {
        using NpgsqlConnection conn = new(connString);
        return (int)conn.Insert(file_record);
    }
            

    public bool UpdateStudyDownloadLog(int? source_id, string sd_id, string? remote_url,
                     int? saf_id, DateTime? last_revised_date, string? full_path)
    {
        bool added = false; // indicates if a new record or update of an existing one

        // Get the source data record and modify it or add a new one.
        
        StudyFileRecord? file_record = FetchStudyFileRecord(sd_id, source_id);
        try
        {
            if (file_record is null)
            {
                file_record = new StudyFileRecord(source_id, sd_id, remote_url, saf_id,
                                                last_revised_date, full_path);
                InsertStudyFileRec(file_record);
                added = true;
            }
            else
            {
                file_record.remote_url = remote_url;
                file_record.last_saf_id = saf_id;
                file_record.last_revised = last_revised_date;
                file_record.download_status = 2;
                file_record.last_downloaded = DateTime.Now;
                file_record.local_path = full_path;

                StoreStudyFileRec(file_record);
            }

            return added;
        }
        catch(Exception e)
        {
            _logging_helper.LogError("In UpdateStudyDownloadLog: " + e.Message);
            return false;
        }
    }


    public bool UpdateObjectDownloadLog(int source_id, string sd_id, string remote_url,
                     int saf_id, DateTime? last_revised_date, string full_path)
    {
        bool added = false; // indicates if a new record or update of an existing one

        // Get the source data record and modify it or add a new one...
        ObjectFileRecord? file_record = FetchObjectFileRecord(sd_id, source_id);

        if (file_record is null)
        {
            file_record = new ObjectFileRecord(source_id, sd_id, remote_url, saf_id,
                                            last_revised_date, full_path);
            InsertObjectFileRec(file_record);
            added = true;
        }
        else
        {
            file_record.remote_url = remote_url;
            file_record.last_saf_id = saf_id;
            file_record.last_revised = last_revised_date;
            file_record.download_status = 2;
            file_record.last_downloaded = DateTime.Now;
            file_record.local_path = full_path;
            StoreObjectFileRec(file_record);
        }

        return added;
    }

    public bool Downloaded_recently(int source_id, string sd_sid, int days_ago)
    {
        string sql_string = $@"select id from sf.source_data_studies 
                               where last_downloaded::date >= now()::date - {days_ago} 
                               and sd_id = '{sd_sid}' and source_id = {source_id}";
        using NpgsqlConnection conn = new(connString);
        return conn.Query<int>(sql_string).FirstOrDefault() > 0;
    }
    
    
    public bool Downloaded_recentlywithlink(int source_id, string details_link, int days_ago)
    {
        string sql_string = $@"select id from sf.source_data_studies 
                               where last_downloaded::date >= now()::date - {days_ago} 
                               and remote_url = '{details_link}' and source_id = {source_id}";
        using NpgsqlConnection conn = new(connString);
        return conn.Query<int>(sql_string).FirstOrDefault() > 0;
    }

}

