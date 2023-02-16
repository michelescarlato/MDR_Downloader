using HtmlAgilityPack;
using MDR_Downloader.Helpers;
using ScrapySharp.Extensions;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System.Security.Cryptography;
using System.Web;


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
            List<Summary> page_study_list = new ();

            HtmlNode? pageContent = homePage.Find("div", By.Class("view-content")).FirstOrDefault();
            if (pageContent is null) 
            {
                _logging_helper.LogError("Unable to find top level div on summary page");
                return page_study_list;  // empty list
            }

            List<HtmlNode> studyRows = pageContent.CssSelect("tbody tr").ToList();
            if (!studyRows.Any())
            {
                _logging_helper.LogError("Unable to find study rows listing on summary page");
                return page_study_list;  // empty list
            }

            foreach (HtmlNode row in studyRows)
            {
                // 6 columns in each row, 
                // 0: NCT number, 1: generic name, 2: title, with link 3: Enrolment number, 
                // 4: CSR download link, 5: view field ops (?)

                Summary sm = new();
                HtmlNode[] cols = row.CssSelect("td").ToArray();
                string link_text = "";

                for (int i = 0; i < 5; i++)
                {
                    HtmlNode col = cols[i];
                    string cellValue = col.InnerText?.Replace("\n", "").Replace("\r", "").Trim() ?? "";
                    if (!string.IsNullOrEmpty(cellValue))
                    {
                        cellValue = cellValue.Replace("??", " ").Replace("&#039;", "’").Replace("'", "’");
                    }
                    switch (i)
                    {
                        case 0: sm.registry_id = cellValue; break;   // Usually NCT number, may be an ISRCTN id, may be blank
                        case 1: sm.generic_name = cellValue; break;
                        case 2:
                            {
                                sm.study_name = cellValue;
                                HtmlNode? link = col.CssSelect("a").FirstOrDefault();
                                if (link is not null)
                                {
                                    link_text = link.Attributes["href"].Value;
                                    sm.details_link = "https://yoda.yale.edu" + link_text; // url for details page.
                                }
                                break;
                            }
                        case 3:
                            {
                                if (cellValue != "") cellValue = cellValue.Replace(" ", "");
                                sm.enrolment_num = cellValue;
                                break;
                            }
                        case 4:
                            {
                                sm.csr_link = "";
                                if (cellValue != "")
                                {
                                    HtmlNode? link = col.CssSelect("a").FirstOrDefault();
                                    if (link is not null)
                                    {
                                        sm.csr_link = link.Attributes["href"].Value;
                                    }
                                }
                                break;
                            }
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
                        if (link_text != "")
                        {
                            sm.sd_sid = "X-" + link_text;
                        }
                        else
                        {
                            sm.sd_sid = sm.study_name ?? "No study identifier - ????";  // should never happen, but...
                        }
                    }
                }
                page_study_list.Add(sm);
            }

            return page_study_list;
        }


        public async Task<Yoda_Record?> GetStudyDetailsAsync (HtmlNode page, Summary sm)
        {
            Yoda_Record st = new (sm);
            string sid = st.sd_sid;

            // Get page components.

            HtmlNode? propsBlock = page.CssSelect("#block-views-trial-details-block-2").FirstOrDefault();
            IEnumerable<HtmlNode>? leftCols = propsBlock.CssSelect(".left-col");
            IEnumerable<HtmlNode>? rightCols = propsBlock.CssSelect(".right-col");
            IEnumerable<HtmlNode> props = leftCols.CssSelect(".views-field").Concat(rightCols.CssSelect(".views-field"));

            List<SuppDoc> supp_docs = new ();
            List<Identifier> study_identifiers = new();
            List<Title> study_titles = new();
            List<Reference> study_references = new();

            foreach (HtmlNode fieldNode in props)
            {
                // get label
                var labelNode = fieldNode.CssSelect(".views-label").FirstOrDefault();
                if (labelNode is not null)
                {
                    string label = labelNode.InnerText.Trim();
                    string? value = HttpUtility.UrlDecode(fieldNode.InnerText);
                    value = value.Replace("\n", "").Replace("\r", "").Trim();
                    value = value.Replace("&amp;", "&").Replace("&nbsp;", " ").Trim();
                    value = value.Replace("&#039;", "'");
                    value = value.ReplaceNBSpaces();
                    value = value?[label.Length..]?.Trim();

                    switch (label)
                    {
                        case "Generic Name": st.compound_generic_name = value; break;
                        case "Product Name": st.compound_product_name = value; break;
                        case "Therapeutic Area": st.therapeutic_area = value; break;
                        case "Enrollment": st.enrolment = value; break;
                        case "% Female": st.percent_female = value; break;
                        case "% White": st.percent_white = value; break;
                        case "Product Class": st.product_class = value; break;
                        case "Sponsor Protocol Number": st.sponsor_protocol_id = value; break;
                        case "Data Partner": st.data_partner = value; break;
                        case "Condition Studied": st.conditions_studied = value; break;
                    }
                }
            }

            // org = sponsor - from CGT / ISRCTN tables if registered, otherwise from pp table
            // In one case there is no sponsor id.
            // First obtain the data from the other sources...

            string? reg_id = st.registry_id;
            SponsorDetails? sponsor = null;
            StudyDetails? sd = null;

            bool isRegistered = sm.sd_sid.StartsWith("Y-");
            if (isRegistered)
            {
                if (reg_id is not null)
                {
                    if (reg_id.StartsWith("NCT"))
                    {
                        // use nct_id to get sponsor id and name
                        sponsor = _repo.FetchSponsorFromNCT(reg_id);
                        sd = _repo.FetchStudyDetailsFromNCT(reg_id);
                        study_identifiers.Add(new Identifier(reg_id, 11, "Trial Registry ID", 100120, "ClinicalTrials.gov"));
                    }
                    else if (reg_id.StartsWith("ISRCTN"))
                    {
                        sponsor = _repo.FetchSponsorFromISRCTN(reg_id);
                        sd = _repo.FetchStudyDetailsFromISRCTN(reg_id);
                        study_identifiers.Add(new Identifier(reg_id, 11, "Trial Registry ID", 100126, "ISRCTN"));
                    }
                }
                // Insert the data if available
                // Otherwise add as a new record to be manually completed

                if (sponsor == null)
                {
                    _logging_helper.LogError("No sponsor found for " + st.yoda_title + ", at " + st.remote_url);
                }
                else
                {
                    st.sponsor_id = sponsor.org_id ?? 0;
                    st.sponsor = sponsor.org_name ?? "";
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
                // study is in Yoda but not registered elsewhere
                // Details may be available from Yoda documents and
                // manually added to local table pp.not_registered

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
                            _ => st.data_partner
                        };
                    }
                    pp_id = "Y-" + sponsor_code + "-" + protid;
                }
                else
                {
                    string input = sm.study_name + sm.enrolment_num + sm.csr_link;
                    byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                    byte[] hashBytes = MD5.HashData(inputBytes);
                    pp_id = "Y-" + string.Concat(hashBytes.Select(x => x.ToString("X2"))).ToLower();
                }

                // does this record already exist in the pp.not_registered table?
                // if so get details, if not add it and log the fact that the
                // table will need manually updating

                NotRegisteredDetails details = _repo.FetchNonRegisteredDetailsFromTable(pp_id);
                if (details.title is null)
                {
                    _repo.AddNewNotRegisteredRecord(pp_id, st.yoda_title!, sponsor_code, protid);
                    _logging_helper.LogError("Further details required for " + st.yoda_title + " in pp.not_registered table, from " + st.remote_url);
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


            // list of documents
            HtmlNode? docsBlock = page.CssSelect("#block-views-trial-details-block-1").FirstOrDefault();
            List<HtmlNode> docs = docsBlock.CssSelect(".views-field").ToList();

            if (docs.Any())
            {
                foreach (HtmlNode docType in docs)
                {
                    string docName = docType.InnerText.Trim();
                    if (docName != "")
                    {
                        SuppDoc supp_doc = new(docName);
                        supp_docs.Add(supp_doc);
                    }
                }
            }


            //icons at top of page
            HtmlNode? iconsBlock = page.CssSelect("#block-views-trial-details-block-3").FirstOrDefault();
            if (iconsBlock is not null)
            {
                // csr summary
                HtmlNode? csrSummary = iconsBlock.CssSelect(".views-field-field-study-synopsis").FirstOrDefault();
                if (csrSummary is not null)
                {
                    HtmlNode? csrLinkNode = csrSummary.CssSelect("b a").FirstOrDefault();
                    string csrLink = "", csrComment = "";

                    if (csrLinkNode is not null)
                    {
                        csrLink = csrLinkNode.Attributes["href"].Value;
                    }

                    var csrCommentNode = csrSummary.CssSelect("p").FirstOrDefault();
                    if (csrCommentNode is not null)
                    {
                        csrComment = csrCommentNode.InnerText.Trim();
                    }

                    if (csrLink != "" || csrComment != "")
                    {
                        // add a new supp doc record
                        SuppDoc supp_doc = new("CSR Summary")
                        {
                            url = csrLink,
                            comment = csrComment
                        };
                        supp_docs.Add(supp_doc);

                        // is this the same link as in the main table
                        // ought to be but...
                        if (supp_doc.url != sm.csr_link)
                        {
                            string report = "mismatch in csr summary link - study id " + sid;
                            report += "\nicon csr link = " + supp_doc.url;
                            report += "\ntable csr link = " + sm.csr_link + "\n\n";
                            _logging_helper.LogLine(report);
                        }
                    }
                }

                // primary citation
                HtmlNode? primCitation = iconsBlock.CssSelect(".views-field-field-primary-citation").FirstOrDefault();
                if (primCitation is not null)
                {
                    var citationLink = primCitation.CssSelect("a").FirstOrDefault();
                    if (citationLink is not null)
                    {
                        st.primary_citation_link = citationLink.Attributes["href"].Value;
                    }
                    else
                    {
                        var citationCommentNode = primCitation.CssSelect("p").FirstOrDefault();
                        if (citationCommentNode is not null)
                        {
                            st.primary_citation_link = citationCommentNode.InnerText.Trim();
                        }
                    }
                }

                // data specification
                
                HtmlNode? dataSpec = iconsBlock.CssSelect(".views-field-field-data-specification-spreads").FirstOrDefault();
                if (dataSpec is not null)
                {
                    var dataLinkNode = dataSpec.CssSelect("b a").FirstOrDefault();
                    string dataLink = "", dataComment = "";
                    if (dataLinkNode is not null)
                    {
                        dataLink = dataLinkNode.Attributes["href"].Value;
                    }

                    var dataCommentNode = dataSpec.CssSelect("p").FirstOrDefault();
                    if (dataCommentNode is not null)
                    {
                        dataComment = dataCommentNode.InnerText.Trim();
                    }

                    if (dataLink != "" || dataComment != "")
                    {
                        SuppDoc? matchingSD = FindSuppDoc(supp_docs, "Data Definition Specification");
                        if (matchingSD is not null)
                        {
                            matchingSD.url = dataLink;
                            matchingSD.comment = dataComment;
                        }
                        else
                        {
                            // add a new supp doc record
                            SuppDoc supp_doc = new("Data Definition Specification")
                            {
                                url = dataLink,
                                comment = dataComment
                            };
                            supp_docs.Add(supp_doc);
                        }
                    }
                }

                //annotated CRFs
                HtmlNode? annotCRF = iconsBlock.CssSelect(".views-field-field-annotated-crf").FirstOrDefault();
                if (annotCRF is not null)
                {
                    var crfLinkNode = annotCRF.CssSelect("b a").FirstOrDefault();
                    string crfLink = "", crfComment = "";
                    if (crfLinkNode is not null)
                    {
                        crfLink = crfLinkNode.Attributes["href"].Value;
                    }

                    var crfCommentNode = annotCRF.CssSelect("p").FirstOrDefault();
                    if (crfCommentNode is not null)
                    {
                        crfComment = crfCommentNode.InnerText.Trim();
                    }

                    if (crfLink != "" || crfComment != "")
                    {
                        SuppDoc? matchingSD = FindSuppDoc(supp_docs, "Annotated Case Report Form");
                        if (matchingSD is not null)
                        {
                            matchingSD.url = crfLink;
                            matchingSD.comment = crfComment;
                        }
                        else
                        {
                            // add a new supp doc record
                            SuppDoc supp_doc = new("Annotated Case Report Form")
                            {  
                                url = crfLink,
                                comment = crfComment
                            };
                            supp_docs.Add(supp_doc);
                        }
                    }
                }
            }


            // Review supp_docs.

            List<SuppDoc> supp_docs_available = new();
            foreach (SuppDoc supp_doc in supp_docs)
            {
                bool add_this_doc;
                if (!string.IsNullOrEmpty(supp_doc.comment) && 
                    (supp_doc.comment.ToLower() == "not available" || supp_doc.comment.ToLower() == "not yet available"
                                                                   || supp_doc.comment.ToLower() == "not yet avaiable"))
                {
                    // Exclude docs explicitly described as not available.

                     add_this_doc = false;
                }
                else
                {
                    // If a URL link present...indicate that in the comment, otherwise
                    // add the presumed default condition in the comment

                    supp_doc.comment = !string.IsNullOrEmpty(supp_doc.url) 
                                             ? "Available now" 
                                             : "Available upon approval of data request";
                    add_this_doc = true;
                }

                if (add_this_doc) supp_docs_available.Add(supp_doc);
            }
           

            if (st.sponsor_protocol_id != "")
            {
                study_identifiers.Add(new Identifier(st.sponsor_protocol_id, 14, "Sponsor ID", st.sponsor_id, st.sponsor));
            }

            // for the study, add the yoda title (seems to be the full scientific title)

            study_titles.Add(new Title(st.yoda_title!, 18, "Other scientific title", true, "From YODA web page"));

            // create study references (pmids)
            if (st.primary_citation_link is not null && st.primary_citation_link.Contains("http"))
            {   
                // try to extract pmid
                if (st.primary_citation_link.Contains("pubmed"))
                {
                    // drop this common suffix if it is present
                    string link = st.primary_citation_link.Replace("?dopt=Abstract", "");
                    string pos_pmid = "";
                        
                    int pubmed_pos = link.IndexOf("pubmed/", StringComparison.Ordinal);
                    if (pubmed_pos != -1)
                    {
                        pos_pmid = link[(pubmed_pos + 7)..];
                    }

                    if (pos_pmid == "")
                    {
                        pubmed_pos = link.IndexOf("pubmed.ncbi.nlm.nih.gov/", StringComparison.Ordinal);
                        if (pubmed_pos != -1)
                        {
                            pos_pmid = link[(pubmed_pos + 24)..];
                        }
                    }

                    if (pos_pmid != "")
                    {
                        if (int.TryParse(pos_pmid, out _))
                        {
                            study_references.Add(new Reference(pos_pmid, link));
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

                else
                {
                    // else try and retrieve from linking out to the pubmed page
                    string pubmed_id = await GetPMIDFromPageAsync(st.primary_citation_link);
                    if (!string.IsNullOrEmpty(pubmed_id))
                    {
                        study_references.Add(new Reference(pubmed_id, st.primary_citation_link));
                    }
                }
            }

            // for all studies there is a data object which is the YODA page itself, 
            // as a web based study overview...
            st.supp_docs = supp_docs_available;
            st.study_identifiers = study_identifiers;
            st.study_titles = study_titles;
            st.study_references = study_references;
            return st;
        }


        private SuppDoc? FindSuppDoc(List<SuppDoc> supp_docs, string name)
        {
            SuppDoc? sd = null;
            foreach (SuppDoc s in supp_docs)
            {
                if (s.doc_name == name)
                {
                    sd = s;
                    break;
                }
            }
            return sd;
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
