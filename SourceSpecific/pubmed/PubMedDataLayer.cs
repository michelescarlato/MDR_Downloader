using Dapper;
using Dapper.Contrib.Extensions;
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

        string sql_string = @"DROP TABLE IF EXISTS mn.dbrefs_all;
                   CREATE TABLE mn.dbrefs_all(
                    id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY    
                  , source_id int
                  , sd_sid varchar
                  , pmid varchar
                  , citation varchar
                  , doi varchar
                  , type_id int
                  , comments varchar
                  , datetime_of_data_fetch timestamptz)";
        conn.Execute(sql_string);

        sql_string = @"DROP TABLE IF EXISTS mn.dbrefs_distinct_pmids;
                   CREATE TABLE mn.dbrefs_distinct_pmids(
                     identity int GENERATED ALWAYS AS IDENTITY
                   , group_id int
                   , pmid varchar)";
        conn.Execute(sql_string);

        sql_string = @"DROP TABLE IF EXISTS mn.dbrefs_id_strings;
                   CREATE TABLE mn.dbrefs_id_strings(
                   id_string varchar)";
        conn.Execute(sql_string);
    }


    public void SetUpTempPMIDsByBankTables()
    {
        using NpgsqlConnection conn = new(connString);

        string sql_string = @"DROP TABLE IF EXISTS mn.pmbanks_all;
                   CREATE TABLE mn.pmbanks_all(
                    source_id int
                  , pmid varchar
                 )";
        conn.Execute(sql_string);

        sql_string = @"DROP TABLE IF EXISTS mn.pmbanks_distinct_pmids;
                   CREATE TABLE mn.pmbanks_distinct_pmids(
                     identity int GENERATED ALWAYS AS IDENTITY
                   , group_id int
                   , pmid varchar)";
        conn.Execute(sql_string);

        sql_string = @"DROP TABLE IF EXISTS mn.pmbanks_id_strings;
                   CREATE TABLE  mn.pmbanks_id_strings(
                   id_string varchar)";
        conn.Execute(sql_string);
    }


    public void SetUpTempJournalDataTables()
    {
        using NpgsqlConnection conn = new(connString);

        string sql_string = @"DROP TABLE IF EXISTS mn.journal_uids;
                   CREATE TABLE mn.journal_uids(
                     identity int GENERATED ALWAYS AS IDENTITY
                   , group_id int
                   , uid varchar)";
        conn.Execute(sql_string);

        sql_string = @"DROP TABLE IF EXISTS mn.journal_uid_strings;
                   CREATE TABLE mn.journal_uid_strings(
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


    public IEnumerable<PMSource> FetchDatabanks()
    {
        using NpgsqlConnection Conn = new(context_connString);
        string SQLString = @"select id, nlm_abbrev from ctx.nlm_databanks 
                             where id not in (100156, 100157, 100158)";
        return Conn.Query<PMSource>(SQLString);
    }


    public IEnumerable<PMIDBySource> FetchSourceReferences(string db_name, int source_id)
    {
        string db_conn_string = _credentials.GetConnectionString(db_name);
        using NpgsqlConnection conn = new(db_conn_string);
        string sql_string = $@"SELECT {source_id} as source_id,
                    r.sd_sid, r.pmid, r.citation, r.doi,
                    coalesce(r.type_id, 12) as type_id, r.comments, 
                    s.datetime_of_data_fetch
                    from ad.study_references r 
                    inner join ad.studies s 
                    on r.sd_sid = s.sd_sid";
        return conn.Query<PMIDBySource>(sql_string);
    }

    public ulong StorePmidsBySource(PostgreSQLCopyHelper<PMIDBySource> copyHelper,
                 IEnumerable<PMIDBySource> entities)
    {
        using NpgsqlConnection conn = new(connString);
        conn.Open();
        return copyHelper.SaveAll(conn, entities);
    }


    public ulong StorePmidsByBank(PostgreSQLCopyHelper<BankPmid> copyHelper,
        IEnumerable<BankPmid> entities)
    {
        using NpgsqlConnection conn = new(connString);
        conn.Open();
        return copyHelper.SaveAll(conn, entities);
    }

    public ulong StoreJournalUIDs(PostgreSQLCopyHelper<JournalUID> copyHelper,
        IEnumerable<JournalUID> entities)
    {
        using NpgsqlConnection conn = new(connString);
        conn.Open();
        return copyHelper.SaveAll(conn, entities);
    }


    public void CreateDBRef_IDStrings()
    {
        using NpgsqlConnection conn = new(connString);

        string sql_string = @"INSERT INTO mn.dbrefs_distinct_pmids(pmid)
                      SELECT DISTINCT pmid
                      FROM mn.dbrefs_all
                      where pmid is not null
                      ORDER BY pmid;";
        conn.Execute(sql_string);

        sql_string = @"Update mn.dbrefs_distinct_pmids SET group_id = identity / 100;";
        conn.Execute(sql_string);

        // fill the id list (100 ids in each string).

        sql_string = @"INSERT INTO mn.dbrefs_id_strings(
                    id_string)
                    SELECT DISTINCT string_agg(pmid, ', ') 
                    OVER (PARTITION BY group_id) 
                    from mn.dbrefs_distinct_pmids;";
        conn.Execute(sql_string);
    }

    public IEnumerable<string> FetchSourcePMIDStrings()
    {
        using NpgsqlConnection conn = new(connString);
        string sql_string = @"select id_string from mn.dbrefs_id_strings;";
        return conn.Query<string>(sql_string);
    }

    public IEnumerable<string> FetchBankPMIDStrings()
    {
        using NpgsqlConnection conn = new(connString);
        string sql_string = @"select id_string from mn.pmbanks_id_strings;";
        return conn.Query<string>(sql_string);
    }

    public IEnumerable<string> FetchJournalIDStrings()
    {
        using NpgsqlConnection conn = new(connString);
        string sql_string = @"select id_string from mn.journal_uid_strings;";
        return conn.Query<string>(sql_string);
    }

    public void CreatePMBanks_IDStrings()
    {
        using NpgsqlConnection conn = new(connString);

        // Add in the PM Bank pubmed Ids not already in the dbref table

        string sql_string = @"INSERT INTO mn.pmbanks_distinct_pmids(pmid)
                              SELECT distinct b.pmid from mn.pmbanks_all b
                              left join mn.dbrefs_distinct_pmids d
                              on b.pmid = d.pmid
                              where d.pmid is null
                              ORDER BY pmid;";
        conn.Execute(sql_string);

        sql_string = @"Update mn.pmbanks_distinct_pmids SET group_id = identity / 100;";
        conn.Execute(sql_string);

        // fill the id list (100 ids in each string).

        sql_string = @"INSERT INTO mn.pmbanks_id_strings(
                    id_string)
                    SELECT DISTINCT string_agg(pmid, ', ') 
                    OVER (PARTITION BY group_id) 
                    from mn.pmbanks_distinct_pmids;";
        conn.Execute(sql_string);
    }


    public void CreateDJournal_IDStrings()
    {
        using NpgsqlConnection conn = new(connString);
        string sql_string = @"Update mn.journal_uids SET group_id = identity / 100;";
        conn.Execute(sql_string);

        // fill the id list (100 ids in each string).

        sql_string = @"INSERT INTO mn.journal_uid_strings(
                    id_string)
                    SELECT DISTINCT string_agg(uid, ', ') 
                    OVER (PARTITION BY group_id) 
                    from mn.journal_uids;";
        conn.Execute(sql_string);
    }


    // Gets a 2 letter language code rather than than the original 3.

    public string? lang_3_to_2(string lang_code_3)
    {
        using NpgsqlConnection Conn = new(context_connString);
        string sql_string = $@"select code from lup.language_codes where marc_code = '{lang_code_3}'";
        return Conn.Query<string>(sql_string).FirstOrDefault();
    }

    public void TruncatePeriodicalsTable()
    {
        using NpgsqlConnection conn = new(context_connString);
        string sql_string = @"truncate table ctx.periodicals";
        conn.Execute(sql_string);
    }

    public void StorePublisherDetails(Periodical p)
    {
        using NpgsqlConnection conn = new(context_connString);
        conn.Insert(p);
    }
}