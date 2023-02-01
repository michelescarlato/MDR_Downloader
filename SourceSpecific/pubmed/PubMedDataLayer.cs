using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PostgreSQLCopyHelper;

namespace MDR_Downloader.pubmed
{
    public class PubMedDataLayer
    {
        private readonly NpgsqlConnectionStringBuilder builder;
        private readonly string connString;
        private readonly string mon_connString;
        private readonly string context_connString;
        private readonly string folder_base;
        private readonly ILoggingHelper _logging_helper;

        public PubMedDataLayer(ILoggingHelper logging_helper)
        {
            IConfigurationRoot settings = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            builder = new()
            {
                Host = settings["host"],
                Username = settings["user"],
                Password = settings["password"]
            };
            string? PortAsString = settings["port"];
            if (string.IsNullOrWhiteSpace(PortAsString))
            {
                builder.Port = 5432;
            }
            else
            {
                if (Int32.TryParse(PortAsString, out int port_num))
                {
                    builder.Port = port_num;
                }
                else
                {
                    builder.Port = 5432;
                }
            }

            builder.Database = "pubmed";
            connString = builder.ConnectionString;

            builder.Database = "mon";
            mon_connString = builder.ConnectionString;

            builder.Database = "context";
            context_connString = builder.ConnectionString;

            folder_base = settings["folder_base"] ?? "";

            _logging_helper = logging_helper;
        }

        // Tables and functions used for the PMIDs collected from DB Sources

        public void SetUpTempPMIDsBySourceTables()
        {
            using NpgsqlConnection conn = new(connString);

            string sql_string = @"DROP TABLE IF EXISTS pp.pmids_by_source_total;
                       CREATE TABLE IF NOT EXISTS pp.pmids_by_source_total(
                        source_id int
                      , sd_sid varchar
                      , pmid int)";
            conn.Execute(sql_string);

            sql_string = @"DROP TABLE IF EXISTS pp.distinct_pmids;
                       CREATE TABLE IF NOT EXISTS pp.distinct_pmids(
                         identity int GENERATED ALWAYS AS IDENTITY
                       , group_id int
                       , pmid int)";
            conn.Execute(sql_string);

            sql_string = @"DROP TABLE IF EXISTS pp.pmid_id_strings;
                       CREATE TABLE IF NOT EXISTS pp.pmid_id_strings(
                       id_string varchar)";
            conn.Execute(sql_string);
        }


        public IEnumerable<Source> FetchSourcesWithReferences()
        {
            using NpgsqlConnection conn = new(mon_connString);
            string sql_string = @"select * from sf.source_parameters
                where has_study_references = true";
            return conn.Query<Source>(sql_string);
        }


        public IEnumerable<PMIDBySource> FetchSourceReferences(string db_name)
        {
            builder.Database = db_name;
            string db_conn_string = builder.ConnectionString;

            using NpgsqlConnection conn = new(db_conn_string);
            string sql_string = @"SELECT DISTINCT 
                        sd_sid, pmid::int from ad.study_references 
                        where pmid is not null and pmid <> ''";
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
            
            string sql_string = @"INSERT INTO pp.distinct_pmids(pmid)
                          SELECT DISTINCT pmid
                          FROM pp.pmids_by_source_total
                          ORDER BY pmid;";
            conn.Execute(sql_string);

            sql_string = @"Update pp.distinct_pmids SET group_id = identity / 100;";
            conn.Execute(sql_string);

            // fill the id list (100 ids in each string).

            sql_string = @"INSERT INTO pp.pmid_id_strings(
                        id_string)
                        SELECT DISTINCT string_agg(pmid::varchar, ', ') 
                        OVER (PARTITION BY group_id) 
                        from pp.distinct_pmids;";
            conn.Execute(sql_string);
        }

        public IEnumerable<string> FetchSourcePMIDStrings()
        {
            using NpgsqlConnection conn = new(connString);
            string sql_string = @"select id_string from pp.pmid_id_strings;";
            return conn.Query<string>(sql_string);
        }

        public void DropPMIDSourceTempTables()
        {
            using NpgsqlConnection conn = new(connString);
            string sql_string = @"DROP TABLE IF EXISTS pp.pmids_by_source_total;
                    DROP TABLE IF EXISTS pp.distinct_pmids;";
            conn.Execute(sql_string);
        }


        public IEnumerable<PMSource> FetchDatabanks()
        {
            using NpgsqlConnection Conn = new(context_connString);
            string SQLString = "select id, nlm_abbrev from ctx.nlm_databanks where id not in (100156, 100157, 100158)";
            return Conn.Query<PMSource>(SQLString);
        }


        // gets a 2 letter language code rather than thean the original 3
        public string? lang_3_to_2(string lang_code_3)
        {
            using NpgsqlConnection Conn = new(context_connString);
            string sql_string = "select code from lup.language_codes where ";
            sql_string += " marc_code = '" + lang_code_3 + "';";
            return Conn.Query<string>(sql_string).FirstOrDefault();
        }
    }

}


