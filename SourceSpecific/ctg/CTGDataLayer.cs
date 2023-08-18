using Dapper;
using Npgsql;

namespace MDR_Downloader.ctg;

public class CtgDataLayer
{
    private readonly string _connString;
        
    public CtgDataLayer(Credentials credentials)
    {
        _connString = credentials.GetConnectionString("ctg");
    }

    public void EstablishIdTables()
    {
        // (re) creates tables with sets of Ids, arranged in 25s,
        // to be used by the 'collect by Id batch' (t=142) method
        
        using NpgsqlConnection conn = new(_connString);
        
        string sql_string = @"DROP TABLE IF EXISTS mn.temp_sd_sids;
        CREATE TABLE mn.temp_sd_sids(
            identity int GENERATED ALWAYS AS IDENTITY
            , group_id int
            , sd_sid varchar);";
        conn.Execute(sql_string);

        sql_string = @"DROP TABLE IF EXISTS mn.temp_id_strings;
            CREATE TABLE mn.temp_id_strings(
            group_id int primary key,
            id_string varchar);";
        conn.Execute(sql_string);
   
        sql_string = @"INSERT INTO mn.temp_sd_sids (sd_sid)
        SELECT sd_sid FROM mn.source_data
            ORDER BY sd_sid;";
        conn.Execute(sql_string);
        
        sql_string = @"UPDATE mn.temp_sd_sids SET group_id = identity / 25;";
        conn.Execute(sql_string);

        sql_string = @"INSERT INTO mn.temp_id_strings(group_id,id_string)
            SELECT DISTINCT group_id, string_agg(sd_sid, ',') 
            OVER (PARTITION BY group_id) 
            from mn.temp_sd_sids;";
        conn.Execute(sql_string);
    }

    public string? FetchIdString(int lineNumber)
    {
        using NpgsqlConnection conn = new(_connString);
        string sql_string = $@"SELECT id_string 
                              FROM mn.temp_id_strings 
                              WHERE group_id = {lineNumber} ";
        return conn.QuerySingleOrDefault<string>(sql_string);
    }
    
    
    public void RemoveIdTables()
    {
        using NpgsqlConnection conn = new(_connString);

        string sql_string = @"DROP TABLE IF EXISTS mn.temp_sd_sids;
        DROP TABLE IF EXISTS mn.temp_id_strings;";
        
        conn.Execute(sql_string);
    }
}