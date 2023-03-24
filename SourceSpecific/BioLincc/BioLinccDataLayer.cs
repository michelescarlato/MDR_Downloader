using Dapper;
using Npgsql;

namespace MDR_Downloader.biolincc;

public class BioLinccDataLayer
{
    private readonly string _biolinccConnString;
    private readonly string _ctgConnString;

    public BioLinccDataLayer(ICredentials credentials)
    {
        _biolinccConnString = credentials.GetConnectionString("biolincc");
        _ctgConnString = credentials.GetConnectionString("ctg");
    }


    public void RecreateBiolinccNctLinksTable()
    {
        using var conn = new NpgsqlConnection(_biolinccConnString);
        string sql_string = @"DROP TABLE IF EXISTS pp.biolincc_nct_links;
            CREATE TABLE pp.biolincc_nct_links (
                sd_sid VARCHAR,
                nct_id VARCHAR,
                multi_biolincc_to_nct BOOL default false);";
        conn.Execute(sql_string);
    }


    public void StoreLinks(string sdSid, List<RegistryId> registryIds)
    {
        using var conn = new NpgsqlConnection(_biolinccConnString);
        foreach (RegistryId id in registryIds)
        {
            // Insert must follow a delete of any relevant records.
            
            string sql_string = $@"Delete from pp.biolincc_nct_links 
                    where sd_sid = '{sdSid}';
                    Insert into pp.biolincc_nct_links(sd_sid, nct_id)
                    values('{sdSid}', '{id.nct_id}');";
            conn.Execute(sql_string);
        }
    }


    public void UpdateLinkStatus()
    {
        using var conn = new NpgsqlConnection(_biolinccConnString);
        string sql_string = @"Update pp.biolincc_nct_links k
            set multi_biolincc_to_nct = true
            from 
                (select nct_id
                 from pp.biolincc_nct_links
                 group by nct_id
                 having count(sd_sid) > 1) multiples
            where k.nct_id = multiples.nct_id;";
        conn.Execute(sql_string);
    } 


    public bool GetMultiLinkStatus(string sdSid)
    {
        using var conn = new NpgsqlConnection(_biolinccConnString);
        string sql_string = $@"select multi_biolincc_to_nct
            from pp.biolincc_nct_links
            where sd_sid = '{sdSid}'";
        return conn.Query<bool>(sql_string).FirstOrDefault();
    }


    public ObjectTypeDetails? FetchDocTypeDetails(string docName)
    {
        using var conn = new NpgsqlConnection(_biolinccConnString);
        string sql_string = $@"Select type_id, type_name from pp.document_types 
                               where resource_name = '{docName}';";
        ObjectTypeDetails? res = conn.QueryFirstOrDefault<ObjectTypeDetails>(sql_string);
        if (res is null)
        {
            // store the details in the table for later matching
            sql_string = $@"Insert into pp.document_types (resource_name, type_id, type_name) 
                            values('{docName}', 0, 'to be added to lookup');";
            conn.Execute(sql_string);
        } 
        return res;
    }

    public SponsorDetails? FetchSponsorFromNct(string nctId)
    {
        using var conn = new NpgsqlConnection(_ctgConnString);
        string sql_string = $@"Select organisation_id as org_id, 
                              organisation_name as org_name from ad.study_contributors 
                              where sd_sid = '{nctId}' and contrib_type_id = 54;";
        return conn.QueryFirstOrDefault<SponsorDetails>(sql_string);
    }


    public string? FetchNameBaseFromNct(string sdSid)
    {
        using var conn = new NpgsqlConnection(_ctgConnString);
        string sql_string = $@"Select display_title from ad.studies
                               where sd_sid = '{sdSid}'";
        return conn.QueryFirstOrDefault<string>(sql_string);
    }


    public void InsertUnmatchedDocumentType(string documentType)
    {
        using var conn = new NpgsqlConnection(_biolinccConnString);
        string sql_string = $@"INSERT INTO pp.document_types(resource_name) 
                SELECT '{documentType}'
                WHERE NOT EXISTS (SELECT id FROM pp.document_types 
                       WHERE resource_name = '{documentType}');";
        conn.Execute(sql_string);
    }
}