using Dapper;
using Npgsql;
using PostgreSQLCopyHelper;

namespace MDR_Downloader.pubmed;

public class PubMedDataLayer
{
    private readonly string connString;
    private readonly string mon_connString;
    private readonly string context_connString;
    private readonly Credentials _credentials;

    public PubMedDataLayer(Credentials credentials)
    {
        _credentials = credentials;
        connString = credentials.GetConnectionString("pubmed");
        mon_connString = credentials.GetConnectionString("mon");
        context_connString = credentials.GetConnectionString("context");
    }

    // Tables and functions used for the PMIDs collected from DB Sources

    public void SetUpTempPMIDsBySourceTables()
    {
        using NpgsqlConnection conn = new(connString);

        string sql_string = @"DROP TABLE IF EXISTS mn.pmid_studies_total;
                   CREATE TABLE IF NOT EXISTS mn.pmid_studies_total(
                    source_id int
                  , sd_sid varchar
                  , pmid varchar
                  , citation varchar
                  , doi varchar
                  , type_id int
                  , comments varchar)";
        conn.Execute(sql_string);

        sql_string = @"DROP TABLE IF EXISTS mn.distinct_pmids;
                   CREATE TABLE IF NOT EXISTS mn.distinct_pmids(
                     identity int GENERATED ALWAYS AS IDENTITY
                   , group_id int
                   , pmid varchar)";
        conn.Execute(sql_string);

        sql_string = @"DROP TABLE IF EXISTS mn.pmid_id_strings;
                   CREATE TABLE IF NOT EXISTS mn.pmid_id_strings(
                   id_string varchar)";
        conn.Execute(sql_string);
    }


    public IEnumerable<Source> FetchSourcesWithReferences()
    {
        using NpgsqlConnection conn = new(mon_connString);
        string sql_string = @"select * from sf.source_parameters
                   where has_study_references = true and source_type <> 'test'";
        return conn.Query<Source>(sql_string);
    }


    public IEnumerable<PMIDBySource> FetchSourceReferences(string db_name, int source_id)
    {
        string db_conn_string = _credentials.GetConnectionString(db_name);
        using NpgsqlConnection conn = new(db_conn_string);
        string sql_string = $@"SELECT {source_id} as source_id,
                    sd_sid, pmid, citation, doi,
                    coalesce(type_id, 12) as type_id, comments
                    from ad.study_references ";
        return conn.Query<PMIDBySource>(sql_string);
    }
    
    public ulong StorePmidsBySource(PostgreSQLCopyHelper<PMIDBySource> copyHelper, 
                 IEnumerable<PMIDBySource> entities)
    {
        using NpgsqlConnection conn = new(connString);
        conn.Open();
        return copyHelper.SaveAll(conn, entities);
    }


    public void CreatePMID_IDStrings()
    {
        using NpgsqlConnection conn = new(connString);
        
        string sql_string = @"INSERT INTO mn.distinct_pmids(pmid)
                      SELECT DISTINCT pmid
                      FROM mn.pmid_studies_total
                      where pmid is not null
                      ORDER BY pmid;";
        conn.Execute(sql_string);

        sql_string = @"Update mn.distinct_pmids SET group_id = identity / 100;";
        conn.Execute(sql_string);

        // fill the id list (100 ids in each string).

        sql_string = @"INSERT INTO mn.pmid_id_strings(
                    id_string)
                    SELECT DISTINCT string_agg(pmid, ', ') 
                    OVER (PARTITION BY group_id) 
                    from mn.distinct_pmids;";
        conn.Execute(sql_string);
    }

    public IEnumerable<string> FetchSourcePMIDStrings()
    {
        using NpgsqlConnection conn = new(connString);
        string sql_string = @"select id_string from mn.pmid_id_strings;";
        return conn.Query<string>(sql_string);
    }

    public void DropPMIDSourceTempTables()
    {
        using NpgsqlConnection conn = new(connString);
        string sql_string = @"DROP TABLE IF EXISTS mn.pmids_by_source_total;
                DROP TABLE IF EXISTS mn.distinct_pmids;";
        conn.Execute(sql_string);
    }


    public IEnumerable<PMSource> FetchDatabanks()
    {
        using NpgsqlConnection Conn = new(context_connString);
        string SQLString = "select id, nlm_abbrev from ctx.nlm_databanks where id not in (100156, 100157, 100158)";
        return Conn.Query<PMSource>(SQLString);
    }


    // Gets a 2 letter language code rather than than the original 3.
    
    public string? lang_3_to_2(string lang_code_3)
    {
        using NpgsqlConnection Conn = new(context_connString);
        string sql_string = $@"select code from lup.language_codes where marc_code = '{lang_code_3}'";
        return Conn.Query<string>(sql_string).FirstOrDefault();
    }
}