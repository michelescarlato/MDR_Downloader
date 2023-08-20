using HtmlAgilityPack;
using MDR_Downloader.Helpers;
using ScrapySharp.Extensions;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System.Security.Cryptography;

namespace MDR_Downloader.yoda
{
    public class Yoda_Processor
    {
        private readonly ScrapingHelpers _ch;
        private readonly ILoggingHelper _logging_helper;
        private readonly YodaDataLayer _repo;

        public Yoda_Processor(ScrapingHelpers ch, ILoggingHelper logging_helper, YodaDataLayer repo)
        {
            _ch = ch;
            _logging_helper = logging_helper;
            _repo = repo;

        }

        public List<Summary> GetStudyInitialDetails(WebPage homePage)
        {
            List<Summary> page_study_list = new();

            HtmlNode? pageContent = homePage.Find("div", By.Class("trials-list__body")).FirstOrDefault();
            if (pageContent is null)
            {
                _logging_helper.LogError("Unable to find trial list div on summary page");
                return page_study_list; // empty list
            }

            List<HtmlNode> studyRows = pageContent.CssSelect(".trial").ToList();
            foreach (HtmlNode row in studyRows)
            {
                // 6 columns in each row, 
                // 0: NCT number, 1: generic name, 2: title, with link 3: Enrolment number, 
                // 4: CSR download link, 5: view field ops (?)

                HtmlNode? nct_id = row.CssSelect(".trial__nct-id").FirstOrDefault();
                HtmlNode? generic_name = row.CssSelect(".trial__generic").FirstOrDefault();
                HtmlNode? trial_title = row.CssSelect(".trial__title").FirstOrDefault();
                HtmlNode? enrollment = row.CssSelect(".trial__enrollment").FirstOrDefault();
                HtmlNode? crs_summary = row.CssSelect(".trial__crs-summary").FirstOrDefault();
                Summary sm = new();
                
                HtmlNode? nct_link = nct_id.CssSelect("a").FirstOrDefault();
                if (nct_link is not null)
                {
                    sm.registry_id = nct_link.InnerText?.TidyYodaText();
                }
                sm.generic_name = generic_name?.InnerText?.TidyYodaText();
                HtmlNode? page_link = trial_title.CssSelect("a").FirstOrDefault();
                if (page_link is not null)
                {
                    sm.details_link = page_link.Attributes["href"].Value; // url for details page.
                    sm.study_name = page_link.InnerText?.TidyYodaText();
                }
                sm.enrolment_num = enrollment?.InnerText?.TidyYodaText()?.Replace(" ", "");
                HtmlNode? csr_link = crs_summary.CssSelect("a").FirstOrDefault();
                if (csr_link is not null)
                {
                    sm.csr_link = csr_link.Attributes["href"].Value;
                }

                // obtain an sd_id as either the registry id, prefixed by Y
                // or the details link - Within a single extraction this should be unique
                // and so can be used to pick up possible duplicates

                if (sm.registry_id is not null &&
                    (sm.registry_id.StartsWith("NCT") || sm.registry_id.StartsWith("ISRCTN")))
                {
                    sm.sd_sid = "Y-" + sm.registry_id;
                }
                else
                {
                    if (sm.details_link != "")
                    {
                        sm.sd_sid = "X-" + sm.details_link;
                    }
                    else
                    {
                        sm.sd_sid = sm.study_name ?? "No study identifier - ????"; // should never happen, but...
                    }
                }
                
                page_study_list.Add(sm);
            }
            
            return page_study_list;
        }


        public async Task<Yoda_Record?> GetStudyDetailsAsync (HtmlNode page, Summary sm)
        {
            Yoda_Record st = new (sm);
            
            ///////////////////////////////////////////////////////////////////////////////////////
            // Identify main page components
            ///////////////////////////////////////////////////////////////////////////////////////
            
            HtmlNode? iconsBlock = page.CssSelect(".trial-resources").FirstOrDefault();
            HtmlNode? infoBlock = page.CssSelect(".trial-info-grid").FirstOrDefault();
            HtmlNode? propsPanel = infoBlock.CssSelect(".trial-info-cell").FirstOrDefault();
            HtmlNode? suppDocsPanel = infoBlock.CssSelect(".support-docs-cell").FirstOrDefault();
            HtmlNode? LHprops = propsPanel.CssSelect(".info-cell").ToArray()[0];
            HtmlNode? RHprops = propsPanel.CssSelect(".info-cell").ToArray()[1];
            
            
            ///////////////////////////////////////////////////////////////////////////////////////
            // Get data from components
            ///////////////////////////////////////////////////////////////////////////////////////
            
            // Icons block is a ul with 4 components
            // The first is a link to the CSR but this has already been obtained in the summary data
            // The second is a link to the NCT (occasionally ISCTRN) web page, but the NCT if it exists will already have been obtained
            // The third is a link to a primary citation and needs to be captured in the JSON file as a reference.
            // The fourth is a link to a dataset specification - rarely present.
            
            List<SuppDoc> supp_docs = new ();
            List<Identifier> study_identifiers = new();
            List<Title> study_titles = new();
            List<Reference> study_references = new();

            string? data_spec_url = null;
            if (iconsBlock is not null)
            {
                HtmlNode[] icons = iconsBlock.CssSelect("li").ToArray();
                if (icons.Count() > 4)
                {
                    // may be some annotated forms
                }

                HtmlNode? citation = iconsBlock.CssSelect(".citation").FirstOrDefault();
                HtmlNode? citation_link = citation.CssSelect("a").FirstOrDefault();
                if (citation_link is not null)
                {
                    st.primary_citation_link = citation_link.Attributes["href"].Value;
                }

                HtmlNode? data_spec = iconsBlock.CssSelect(".data-specification").FirstOrDefault();
                HtmlNode? data_spec_link = data_spec.CssSelect("a").FirstOrDefault();
                if (data_spec_link is not null)
                {
                    data_spec_url = data_spec_link.Attributes["href"].Value;
                }
            }

            if (LHprops is not null)
            {
                HtmlNode[] props = LHprops.CssSelect("span").ToArray();
                if (props[0].InnerText?.Trim() == "Generic Name")
                {
                    st.compound_generic_name = props[1].InnerText?.TidyYodaText();
                }
                if (props[2].InnerText?.Trim() == "Product Name")
                {
                    st.compound_product_name = props[3].InnerText?.TidyYodaText();
                }
                if (props[4].InnerText?.Trim() == "Therapeutic Area")
                {
                    st.therapeutic_area = props[5].InnerText?.TidyYodaText();
                }
                if (props[6].InnerText?.Trim() == "Enrollment")
                {
                    st.enrolment = props[7].InnerText?.TidyYodaText();
                }
                if (props[8].InnerText?.Trim() == "% Female")
                {
                    st.percent_female = props[9].InnerText?.TidyYodaText();
                }
                if (props[10].InnerText?.Trim() == "% White")
                {
                    st.percent_white = props[11].InnerText?.TidyYodaText();
                }
            }
            
            if (RHprops is not null)
            {
                HtmlNode[] props = RHprops.CssSelect("span").ToArray();
                if (props[0].InnerText?.Trim() == "Product Class")
                {
                    st.product_class = props[1].InnerText?.TidyYodaText();
                }
                if (props[2].InnerText?.Trim() == "Sponsor Protocol Number")
                {
                    st.sponsor_protocol_id = props[3].InnerText?.TidyYodaText();
                }
                if (props[4].InnerText?.Trim() == "Data Partner")
                {
                    st.data_partner = props[5].InnerText?.TidyYodaText();
                }
                if (props[6].InnerText?.Trim() == "Condition Studied")
                {
                    st.conditions_studied = props[7].InnerText?.TidyYodaText();
                }
            }

            bool? datasets_av = null, protocol_av = null;
            bool? sap_av = null, full_csr_av = null;
            bool? data_defs_av = null, annotated_crfs_av = null, analysis_dsets_av = null;
            if (suppDocsPanel is not null)
            {
                HtmlNode[] sp_docs = suppDocsPanel.CssSelect("li").ToArray();
                if (sp_docs.Any())
                {
                    foreach (HtmlNode li in sp_docs)
                    {
                        string? li_text = li.InnerText?.TidyYodaText()?.Replace("::before", "");
                        if (li_text is not null)
                        {
                            if (li_text.Contains("Collected Datasets"))
                            {
                                datasets_av = true;
                            }
                            if (li_text.Contains("Protocol with Amendments"))
                            {
                                protocol_av = true;
                            }
                            if (li_text.Contains("Statistical Analysis"))
                            {
                                sap_av = true;
                            }
                            if (li_text.Contains("Clinical Study Report"))
                            {
                                full_csr_av = true;
                            }
                            if (li_text.Contains("Analysis Datasets"))
                            {
                                analysis_dsets_av = true;
                            }
                            if (li_text.Contains("Data Definition"))
                            {
                                data_defs_av = true;
                            }
                            if (li_text.Contains("nnotated"))
                            {
                                annotated_crfs_av = true;
                            }
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Get study basics and sponsor 
            ///////////////////////////////////////////////////////////////////////////////////////
            
            string? reg_id = st.registry_id;  // i.e. as constructed in the initial summary data phase
            StudyDetails? sd = null;
            string? sponsor_name = "";
            bool isRegistered = sm.sd_sid.StartsWith("Y-");
            if (isRegistered)
            {
                
                ///////////////////////////////////////////////////////////////////////////////////////
                // Use data from other sources if registered already (almost always CTG)
                ///////////////////////////////////////////////////////////////////////////////////////
                
                if (reg_id is not null)
                {
                    if (reg_id.StartsWith("NCT"))
                    {
                        // use nct_id to get sponsor id and name
                        sponsor_name = _repo.FetchSponsorFromNCT(reg_id);
                        sd = _repo.FetchStudyDetailsFromNCT(reg_id);
                        study_identifiers.Add(new Identifier(reg_id, 11, "Trial Registry ID", 100120, "ClinicalTrials.gov"));
                    }
                    else if (reg_id.StartsWith("ISRCTN"))
                    {
                        sponsor_name = _repo.FetchSponsorFromISRCTN(reg_id);
                        sd = _repo.FetchStudyDetailsFromISRCTN(reg_id);
                        study_identifiers.Add(new Identifier(reg_id, 11, "Trial Registry ID", 100126, "ISRCTN"));
                    }
                }
                
                // Insert the data, for sponsor name and study details respectively, if available -
                // Otherwise log as an error 

                if (string.IsNullOrEmpty(sponsor_name))
                {
                    _logging_helper.LogError("No sponsor found for " + st.yoda_title + ", at " + st.remote_url);
                }
                else
                {
                    st.sponsor = sponsor_name;
                }

                if (sd == null)
                {
                    _logging_helper.LogError("No study details found for " + st.yoda_title + ", at " + st.remote_url);
                }
                else
                {
                    st.name_base_title = sd.display_title ?? "";
                    st.brief_description = sd.brief_description ?? "";
                    st.study_type_id = sd.study_type_id ?? 0;
                }
            }
            else
            {
                ///////////////////////////////////////////////////////////////////////////////////////
                // Not registered elsewhere - get details and change sd_sid
                ///////////////////////////////////////////////////////////////////////////////////////
                
                // Study is in Yoda but not registered elsewhere. Details may be available from Yoda
                // documents or elsewhere and manually added to local table mn.not_registered. sd_sid changed
                // from the page link text to reflect (in most cases) the sponsor's protocol id 

                string protid = "", sponsor_code = "", pp_id;
                if (!string.IsNullOrEmpty(st.sponsor_protocol_id))
                {
                    protid = st.sponsor_protocol_id.Replace("/", "-").Replace("\\", "-").Replace(" ", "");
                    if (st.data_partner == null)
                    {
                        sponsor_code = "XX";
                    }
                    else
                    {
                        sponsor_code = st.data_partner switch 
                        {
                            "Johnson & Johnson" => "JandJ",
                            "Queen Mary University of London" => "QMUL",
                            "McNeil Consumer Healthcare" => "McNeil CH",
                            "Robert Wood Johnson Foundation" => "RWJFound",
                            "SI-BONE, Inc" => "SIB",
                            _ => st.data_partner
                        };
                    }
                    pp_id = "Y-" + sponsor_code + "-" + protid;
                }
                else
                {
                    // As a last resort - only applies to a single study at present 
                    
                    string input = sm.study_name + sm.enrolment_num + sm.csr_link;
                    byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                    byte[] hashBytes = MD5.HashData(inputBytes);
                    pp_id = "Y-" + string.Concat(hashBytes.Select(x => x.ToString("X2"))).ToLower();
                }

                // Does this record already exist in the mn.not_registered table? If so get details, if not
                // add it and log the fact that the table will need manually updating

                NotRegisteredDetails? details = _repo.FetchNonRegisteredDetailsFromTable(pp_id);
                if (details is null)
                {
                    _repo.AddNewNotRegisteredRecord(pp_id, st.yoda_title!, sponsor_code, protid);
                    _logging_helper.LogLine("Further details required for " + st.yoda_title + " in mn.not_registered table, from " + st.remote_url);
                }
                else
                {
                    st.sponsor_id = details.sponsor_id ?? 0;
                    st.sponsor = details.sponsor_name ?? "";
                    st.name_base_title = details.title ?? "";
                    st.brief_description = details.brief_description ?? "";
                    st.study_type_id = details.study_type_id ?? 0;
                }

                st.sd_sid = pp_id;    // replace the link id used initially (link id may not be fixed)
            }
            
            ///////////////////////////////////////////////////////////////////////////////////////
            // Add final study identifier
            ///////////////////////////////////////////////////////////////////////////////////////
           
            // Studies that are registered elsewhere already have the relevant identifiers entered
            // as trial registry Ids. The Yoda internal identifier (sd_sid) is manufactured by the 
            // MDR and is not a 'true' identifier, so is not added. The sponsor's identifier, if one 
            // is present, should be added however.
            
            if (!string.IsNullOrEmpty(st.sponsor_protocol_id))
            {
                study_identifiers.Add(new Identifier(st.sponsor_protocol_id, 14, "Sponsor ID", st.sponsor_id, st.sponsor));
            }

            
            ///////////////////////////////////////////////////////////////////////////////////////
            // Study title - Add Yoda title as a scientific title
            ///////////////////////////////////////////////////////////////////////////////////////

            if (!string.IsNullOrEmpty(st.yoda_title))
            {
                study_titles.Add(new Title(st.yoda_title!, 18, "Other scientific title", true, "From YODA web page"));
            }


            ///////////////////////////////////////////////////////////////////////////////////////
            // Study reference - almost always a PMID
            ///////////////////////////////////////////////////////////////////////////////////////
            
            if (st.primary_citation_link is not null && st.primary_citation_link.Contains("http"))
            {   
                if (st.primary_citation_link.Contains("pubmed"))  // try to extract pmid
                {
                    // first drop this common suffix if it is present
                    
                    string link = st.primary_citation_link.Replace("?dopt=Abstract", "");
                    
                    // Then try to find the pubmed id in the string, using two common url patterns
                    
                    string poss_pmid = "";
                    int pubmed_pos = link.IndexOf("pubmed/", StringComparison.Ordinal);
                    if (pubmed_pos != -1)
                    {
                        poss_pmid = link[(pubmed_pos + 7)..];
                    }
                    if (poss_pmid == "")
                    {
                        pubmed_pos = link.IndexOf("pubmed.ncbi.nlm.nih.gov/", StringComparison.Ordinal);
                        if (pubmed_pos != -1)
                        {
                            poss_pmid = link[(pubmed_pos + 24)..];
                        }
                    }
                    
                    if (poss_pmid != "")    // a pmid id found, check it is an integer
                    {
                        if (int.TryParse(poss_pmid, out _))
                        {
                            study_references.Add(new Reference(poss_pmid, link));
                        }
                    }
                    else
                    {
                        // primary citation link includes pubmed but no number
                        // This usually seems to be just a link to the pubmed site (!)
                        // if so the citation itself should be blank.
                        
                        if (link == "https://pubmed.ncbi.nlm.nih.gov/" || link.EndsWith("pubmed/"))
                        {
                            st.primary_citation_link = null;
                        }
                    }
                }

                else if (st.primary_citation_link.Contains("/pmc/articles/"))
                {
                    // need to interrogate NLM API 
                    
                    int pmc_pos = st.primary_citation_link.IndexOf("/pmc/articles/", StringComparison.Ordinal);
                    string pmc_id = st.primary_citation_link[(pmc_pos + 14)..];
                    pmc_id = pmc_id.Replace("/", "");
                    string? pubmed_id = await _ch.GetPmidFromNlmAsync(pmc_id);
                    if (!string.IsNullOrEmpty(pubmed_id))
                    {
                        study_references.Add(new Reference(pubmed_id, st.primary_citation_link));
                    }
                }
                
                // previous attempts to link to page in citation url for cases not caught above
                // now abandoned as they never work  - servers return a 403 code ('forbidden').
            }
            
            
            ///////////////////////////////////////////////////////////////////////////////////////
            // Identify and add supplementary docs
            ///////////////////////////////////////////////////////////////////////////////////////

            if (!string.IsNullOrEmpty(sm.csr_link))
            {
                supp_docs.Add(new SuppDoc(79, "Results or CSR summary", sm.csr_link));
            }
            if (!string.IsNullOrEmpty(data_spec_url))
            {
                supp_docs.Add(new SuppDoc(31, "Data dictionary", data_spec_url));
            }
            if (data_defs_av == true && string.IsNullOrEmpty(data_spec_url))
            {
                supp_docs.Add(new SuppDoc(31, "Data dictionary"));
            }
            if (datasets_av == true)
            {
                supp_docs.Add(new SuppDoc(80, "Individual participant data"));
            }
            if (protocol_av == true)
            {
                supp_docs.Add(new SuppDoc(11, "Study protocol"));
            }
            if (sap_av == true)
            {
                supp_docs.Add(new SuppDoc(22, "Statistical analysis plan"));
            }
            if (full_csr_av == true)
            {
                supp_docs.Add(new SuppDoc(26, "Clinical study report"));
            }
            if (annotated_crfs_av == true)
            {
                supp_docs.Add(new SuppDoc(30, "Annotated data collection forms"));
            }
            if (analysis_dsets_av == true)
            {
                supp_docs.Add(new SuppDoc(69, "Aggregated result dataset"));
            }

            st.supp_docs = supp_docs;
            st.study_identifiers = study_identifiers;
            st.study_titles = study_titles;
            st.study_references = study_references;
            
            return st;
        }


        public async Task<string> GetPMIDFromPageAsync(string citation_link)
        {
            string pmid = "";
            WebPage? page = await _ch.GetPageAsync(citation_link);
            if (page is not null)
            {
                // only works with pmid pages, that have this dl tag....
                
                HtmlNode? ids_div = page.Find("dl", By.Class("rprtid")).FirstOrDefault();
                if (ids_div is not null)
                {
                    HtmlNode[] dts = ids_div.CssSelect("dt").ToArray();
                    HtmlNode[] dds = ids_div.CssSelect("dd").ToArray();

                    if (dts.Length > 0 && dds.Length > 0)
                    {
                        for (int i = 0; i < dts.Length; i++)
                        {
                            string dts_type = dts[i].InnerText.Trim();
                            if (dts_type == "PMID:")
                            {
                                pmid = dds[i].InnerText.Trim();
                            }
                        }
                    }
                }
            }
            return pmid;
        }
    }

}
