using HtmlAgilityPack;
using MDR_Downloader.Helpers;
using ScrapySharp.Extensions;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System.Collections.Generic;

namespace MDR_Downloader.biolincc;

public class BioLINCC_Processor
{
    private readonly ScrapingHelpers _ch;
    private readonly ILoggingHelper _logging_helper;
    private readonly BioLinccDataLayer _repo;

    string? pubs_link;

    public BioLINCC_Processor(ScrapingHelpers ch, ILoggingHelper logging_helper, BioLinccDataLayer repo)
    {
        _ch = ch;
        _logging_helper = logging_helper;
        _repo = repo;
    }


    public BioLincc_Basics? GetStudyBasics(HtmlNode row)
    {
        // get the basic information from the summary table row
        // 4 columns in each row, first of which has a link
        // the second the acronym, the third the summary of resources and 
        // the fourth the collection type
        // Note, for BioLINCC,the use of the acronym as the sd_sid 

        HtmlNode[]? cols = row.CssSelect("td").ToArray();
        if (cols is not null)
        {
            BioLincc_Basics bb = new();
            HtmlNode? link = cols[0]?.CssSelect("a").FirstOrDefault();
            if (link is not null)
            {
                bb.title = link.InnerText.Trim();
                bb.remote_url = "https://biolincc.nhlbi.nih.gov" + link.Attributes["href"].Value;
            }
            bb.acronym = cols[1].InnerText.Replace("\n", "").Replace("\r", "").Trim();
            bb.resources_available = cols[2].InnerText.Replace("\n", "").Replace("\r", "").Trim();
            bb.collection_type = cols[3].InnerText.Replace("\n", "").Replace("\r", "").Trim();
            bb.sd_sid = bb.acronym.Replace("\\", "-").Replace("/", "-").Replace(".", "-");

            return bb;
        }
        else
        {
            return null;
        }

    }


    public async Task<BioLincc_Record?> GetStudyDetailsAsync(BioLincc_Basics bb)
    {
        // First check the study main page can be reached.

        if (bb.remote_url is null)
        {
            _logging_helper.LogError("No remote url for BioLInnc study page for " + bb.acronym + ", access sttempt failed");
            return null;
        }

        WebPage? studyPage = await _ch.GetPageAsync(bb.remote_url);
        if (studyPage is null)
        {
            _logging_helper.LogError("Attempt to access main BioLInnc study page for " + bb.acronym + " at " + bb.remote_url + " failed");
            return null;
        }

        var main = studyPage.Find("div", By.Class("main"));
        if (main is null)
        {
            _logging_helper.LogError("Could not find 'main' class on page for " + bb.acronym + " at " + bb.remote_url + ".");
            return null;
        }

        HtmlNode[]? tables = main.CssSelect("div.study-box").ToArray();
        if (tables is null)
        {
            _logging_helper.LogError("Could not find 'div.study-box' class on page for " + bb.acronym + " at " + bb.remote_url + ".");
            return null;
        }
              
        // Establish record to be filled, and obtain major page components.

        BioLincc_Record st = new(bb);
        List<PrimaryDoc> primary_docs = new();
        List<RegistryId> registry_ids = new();
        List<Resource> study_resources = new();
        List<AssocDoc> assoc_docs = new();
        List<RelatedStudy> related_studies = new();
 
        HtmlNode? BasicDetails = tables[0];
        HtmlNode? ConsentDetails = tables[1];
        HtmlNode? DescriptiveParas = studyPage.Find("div", By.Id("study-info")).FirstOrDefault();
        IEnumerable<HtmlNode>? SideBar = main.CssSelect("div.col-md-3");

        if (BasicDetails is not null)
        {
            var entries = BasicDetails.CssSelect("p");
            if (entries?.Any() == true)
            {
                await ProcessBasicDetails(st, entries, primary_docs, registry_ids, related_studies);
            }
        }


        if (DescriptiveParas is not null)
        {
            HtmlNode? description = DescriptiveParas.CssSelect("div.col-sm-12").FirstOrDefault();
            if (description is not null)
            {
                IEnumerable<HtmlNode>? desc_headings = description.CssSelect("h2");

                if (desc_headings?.Any() == true)
                {
                    ProcessDescriptiveParas(st, description, desc_headings);
                }
            }
        }


        if (SideBar is not null)
        {
            IEnumerable<HtmlNode>? sections = SideBar.CssSelect("div.detail-aside-row");

            if (sections?.Any() == true)
            {
                pubs_link = ProcessSideBar(st, sections, study_resources);
            }
        }


        if (st.resources_available is not null &&
            st.resources_available.ToLower().Contains("study datasets") && ConsentDetails is not null)
        {
            var entries = ConsentDetails.CssSelect("p");
            if (entries?.Any() == true)
            {
                ProcessConsents(st, entries);
            }
        }


        if (pubs_link is not null)
        {
            string pubURL = "https://biolincc.nhlbi.nih.gov" + pubs_link + "&page_size=200";
            WebPage? pubsPage = await _ch.GetPageAsync(pubURL);
            if (pubsPage is null)
            {
                _logging_helper.LogError("Attempt to access study links page for " + bb.sd_sid + " failed");
            }
            else
            {
                HtmlNode? pubTable = pubsPage.Find("div", By.Class("table-responsive")).FirstOrDefault();
                if (pubTable is not null)
                {
                    IEnumerable<HtmlNode>? pubLinks = pubTable.CssSelect("td a");
                    if (pubLinks?.Any() == true)
                    {
                        st.num_associated_papers = pubLinks.Count();
                        await ProcessPublicationData(st, pubLinks, assoc_docs);
                    }
                }
            }
        }

        // get sponsor details from linked NCT record
        // (or first one listed if multiple).

        if (registry_ids.Count > 0)
        {
            string? NCTId = registry_ids[0].nct_id;
            if (NCTId is not null)
            {
                var sponsor_details = _repo.FetchSponsorFromNCT(NCTId);
                if (sponsor_details is not null)
                {
                    st.sponsor_id = sponsor_details.org_id;
                    st.sponsor_name = sponsor_details.org_name;
                }
                st.nct_base_name = _repo.FetchNameBaseFromNCT(NCTId);
            }
        }

        st.in_multiple_biolincc_group = false;   // default, may be changed in later process.

        st.primary_docs = primary_docs;
        st.registry_ids = registry_ids;
        st.resources = study_resources;
        st.assoc_docs = assoc_docs;
        st.related_studies = related_studies;

        return st;
    }



    async Task ProcessBasicDetails(BioLincc_Record st, IEnumerable<HtmlNode> entries, 
                                            List<PrimaryDoc> primary_docs, List<RegistryId> registry_ids, 
                                            List<RelatedStudy> related_studies)
    {
        // Scan top table with main details and parameters

        foreach (HtmlNode ent_node in entries)
        {
            HtmlNode? entryBold = ent_node.CssSelect("b").FirstOrDefault();
            string? attribute_name = entryBold?.InnerText.Trim();

            HtmlNode? entrySupp = ent_node.CssSelect("em").FirstOrDefault();
            string? supp_text = entrySupp?.InnerText.Trim();

            string? attribute_value = ent_node.InnerText;

            if (!string.IsNullOrEmpty(attribute_name) && !string.IsNullOrEmpty(attribute_value))
            {
                if (attribute_name == "Accession Number") st.accession_number = attribute_value.RemoveLabelAndSupp(attribute_name, supp_text);

                if (attribute_name == "Study Type")
                {
                    string study_type = attribute_value.RemoveLabelAndSupp(attribute_name, supp_text);
                    if (study_type.Contains("Clinical Trial"))
                    {
                        st.study_type_id = 11;
                        st.study_type = "Interventional";
                    }
                    else if (study_type == "Epidemiology Study")
                    {
                        st.study_type_id = 12;
                        st.study_type = "Observational";
                    }
                    else
                    {
                        st.study_type_id = 0;
                        st.study_type = "Not yet known";
                    }
                }

                if (attribute_name == "Study Period") st.study_period = attribute_value.RemoveLabelAndSupp(attribute_name, supp_text);

                if (attribute_name == "Date Prepared")
                {
                    st.date_prepared = attribute_value.RemoveLabelAndSupp(attribute_name, supp_text);
                    if (st.date_prepared == "N/A" || string.IsNullOrEmpty(st.date_prepared))
                    {
                        st.page_prepared_date = null;
                    }
                    else
                    {
                        // date is in the foem of MMMM d, yyyy, and needs to be split accordingly
                        string date_prepared_string = st.date_prepared.Replace(", ", "|").Replace(",", "|").Replace(" ", "|");
                        string[] updated_parts = date_prepared_string.Split("|");
                        int month = updated_parts[0].GetMonthAsInt();
                        if (month > 0
                            && Int32.TryParse(updated_parts[1], out int day)
                            && Int32.TryParse(updated_parts[2], out int year))
                        {
                            st.page_prepared_date = new DateTime(year, month, day);
                        }
                    }
                }

                if (attribute_name.ToLower().Contains("dataset(s) last updated"))
                {
                    st.datasets_updated = attribute_value.RemoveLabelAndSupp(attribute_name, supp_text);
                    if (st.datasets_updated == "N/A" || string.IsNullOrEmpty(st.datasets_updated))
                    {
                        st.datasets_updated_date = null;
                    }
                    else
                    {
                        // date is in the forem of MMMM d, yyyy, and needs to be split accordingly
                        string last_updated_string = st.datasets_updated.Replace(", ", "|").Replace(",", "|").Replace(" ", "|");
                        string[] updated_parts = last_updated_string.Split("|");
                        int month = updated_parts[0].GetMonthAsInt();
                        if (month > 0
                            && Int32.TryParse(updated_parts[1], out int day)
                            && Int32.TryParse(updated_parts[2], out int year))
                        {
                            st.datasets_updated_date = new DateTime(year, month, day);
                        }
                    }
                }

                if (st.page_prepared_date is not null)
                {
                    st.publication_year = ((DateTime)st.page_prepared_date).Year;
                }
                else if (st.datasets_updated_date is not null)
                {
                    st.publication_year = ((DateTime)st.datasets_updated_date).Year;
                }

                if (attribute_name == "Clinical Trial URLs")
                {
                    var entryRefs = ent_node.CssSelect("a");
                    int n = 0; string link_value; string link_text = "";
                    if (entryRefs?.Any() == true)
                    {
                        string nct_id;
                        foreach (var er in entryRefs)
                        {
                            n++;
                            // get the url and any link text if different
                            link_value = er.Attributes["href"].Value.Trim();
                            if (er.InnerText.Trim() != link_value)
                            {
                                link_text = er.InnerText.Trim();
                            }

                            // derive NCT number
                            int NCTPos = link_value.ToUpper().IndexOf("NCT");
                            if (NCTPos > -1 && NCTPos <= link_value.Length - 11)
                            {
                                nct_id = link_value.Substring(NCTPos, 11).ToUpper();
                            }
                            else
                            {
                                nct_id = "Unknown";
                            }
                            registry_ids.Add(new RegistryId(link_value, nct_id, link_text));
                        }
                    }
                    st.num_clinical_trial_urls = n;
                }

                if (attribute_name == "Primary Publication URLs")
                {
                    var entryRefs = ent_node.CssSelect("a");
                    int n = 0; string link_value; string link_text = "";
                    if (entryRefs?.Any() == true)
                    {
                        string? pubmed_id = ""; int pubmed_pos;
                        foreach (var er in entryRefs)
                        {
                            n++;
                            link_value = er.Attributes["href"].Value.Trim();
                            if (er.InnerText.Trim() != link_value)
                            {
                                link_text = er.InnerText.Trim();
                            }

                            // get pubmed id

                            pubmed_pos = link_value.IndexOf("/pubmed/");
                            if (pubmed_pos != -1)
                            {
                                pubmed_id = link_value[(pubmed_pos + 8)..];
                            }
                            else
                            {
                                // may need to interrogate NLM API 
                                int pmc_pos = link_value.IndexOf("/pmc/articles/");
                                if (pmc_pos != -1)
                                {
                                    string pmc_id = link_value[(pmc_pos + 14)..];
                                    pmc_id = pmc_id.Replace("/", "");
                                    pubmed_id = await _ch.GetPMIDFromNLMAsync(pmc_id);
                                }
                            }

                            if (pubmed_id is not null)
                            {
                                PrimaryDoc primaryDoc = new (link_value, pubmed_id, link_text);
                                primary_docs.Add(primaryDoc);
                            }
                        }
                    }
                    st.num_primary_pub_urls = n;
                }

                if (attribute_name == "Study Website")
                {
                    var entryRefs = ent_node.CssSelect("a");
                    if (entryRefs?.Any() == true)
                    {
                        st.study_website = (entryRefs.ToArray())[0].Attributes["href"].Value;
                    }
                }

                if (attribute_name == "Related Studies")
                {
                    var entryRefs = ent_node.CssSelect("a");

                    if (entryRefs?.Any() == true)
                    {
                        string link_value; string link_text;
                        foreach (var er in entryRefs)
                        {
                            link_value = "https://biolincc.nhlbi.nih.gov" + er.Attributes["href"].Value.Trim();
                            link_text = er.InnerText.Trim();

                            related_studies.Add(new RelatedStudy(link_value, link_text));
                        }
                    }
                }
            }
        }
    }


    void ProcessDescriptiveParas(BioLincc_Record st, HtmlNode description, IEnumerable<HtmlNode> section_headings)
    {
        string descriptive_text = description.InnerHtml;
        int section_start_pos, section_end_pos;
        string desc_header, check_text;
        string? descriptive_paras = "";

        foreach (HtmlNode section in section_headings)
        {
            desc_header = section.InnerText.Trim();
            if (desc_header == "Objectives")
            {
                check_text = "Objectives</h2>";
                section_start_pos = descriptive_text.IndexOf(check_text) + check_text.Length;
                section_end_pos = descriptive_text.IndexOf("<h2", section_start_pos);
                descriptive_paras += desc_header + ": ";
                if (section_end_pos != -1)
                {
                    descriptive_paras += descriptive_text[section_start_pos..section_end_pos];
                }
            }
            if (desc_header == "Background")
            {
                check_text = "Background</h2>";
                section_start_pos = descriptive_text.IndexOf(check_text) + check_text.Length;
                section_end_pos = descriptive_text.IndexOf("<h2", section_start_pos);
                if (section_end_pos != -1)
                {
                    descriptive_paras += desc_header + ": " + descriptive_text[section_start_pos..section_end_pos];
                }
            }
            if (desc_header == "Subjects")
            {
                check_text = "Subjects</h2>";
                section_start_pos = descriptive_text.IndexOf(check_text) + check_text.Length;
                section_end_pos = descriptive_text.IndexOf("<h2", section_start_pos);
                if (section_end_pos != -1)
                {
                    descriptive_paras += desc_header + ": " + descriptive_text[section_start_pos..section_end_pos];
                }
            }
        }            
        
        // This should do most if not all of the necessary text cleaning.

        descriptive_paras = descriptive_paras.Replace("<br>", "\n").Replace("<p>", "").Replace("</p>", "");
        descriptive_paras = descriptive_paras.Replace("<b>", "").Replace("</b>", "");
        descriptive_paras = descriptive_paras.CompressSpaces();
        
        st.brief_description = descriptive_paras;
    }


    string? ProcessSideBar(BioLincc_Record st, IEnumerable<HtmlNode> sections, List<Resource> study_resources)
    {
        string? pubs_link = null;
        foreach (HtmlNode section in sections)
        {
            HtmlNode? headerLine = section.CssSelect("h2").FirstOrDefault();
            string? headerText = headerLine?.InnerText.Trim();

            if (!string.IsNullOrEmpty(headerText) && headerText != "Study Catalog")
            {
                if (headerText == "Resources Available")
                {
                    // remove heading and replace original value from summary table
                    // as this is slightly more descriptive

                    string att_value = section.InnerText.Replace("Resources Available", "");
                    st.resources_available = att_value.Replace("\n", "").Replace("\r", "").Trim();
                }

                if (headerText.Length > 18 && headerText[..18] == "Study Publications")
                {
                    // just record the link to the index page for now
                    // the data from publications will be collected later

                    HtmlNode? refNode = headerLine.CssSelect("a").FirstOrDefault();
                    if (refNode is not null)
                    {
                        pubs_link = refNode.Attributes["href"].Value;
                    }
                }

                if (headerText == "Study Documents")
                {
                    HtmlNode[] documents = section.CssSelect("ul a").ToArray();
                    if (documents?.Any() == true)
                    {
                        string? doc_name, doc_type, docInfo, object_type, url, size, sizeUnits;
                        int? object_type_id, doc_type_id, access_type_id;

                        foreach (HtmlNode node in documents)
                        {
                            // re-initialise.

                            doc_name = ""; doc_type = ""; docInfo = ""; object_type = ""; url = ""; size = ""; sizeUnits = "";
                            object_type_id = 0; doc_type_id = 0; access_type_id = 0;

                            // get the url for the document. Add site prefix and
                            // chop off time stamp parameter, if one has been added.

                            url = "https://biolincc.nhlbi.nih.gov" + node.Attributes["href"].Value; ;
                            if (url.IndexOf("?") > 0) url = url[..(url.IndexOf("?"))];

                            // get the text and split off the bracketed data on type and size.

                            string docString = node.InnerText.Trim();
                            string[] subparts = docString.Split('(');

                            if (subparts.Length == 1)
                            {
                                // if no brackets - seems to indicate a linked list of web links (usually of forms)

                                doc_name = docString.Trim();
                                object_type = "List of web links";
                                object_type_id = 86;
                                doc_type = "Web text";
                                doc_type_id = 35;
                                access_type_id = 12;
                                sizeUnits = "";
                                size = "";
                            }
                            else
                            {
                                // in case there is bracketed text in the name - assumed not more than one such - 
                                // recombine first and second subparts for the doc name, with the bracket.
                                // drop rightmost bracket and split doc info using the hyphen

                                if (subparts.Length == 2)
                                {
                                    doc_name = subparts[0].Trim();
                                    docInfo = subparts[1].Trim();
                                }
                                else
                                {
                                    doc_name = subparts[0];
                                    for (int k = 1; k < subparts.Length - 1; k++)
                                    {
                                        doc_name += "(" + subparts[k];
                                    }
                                    doc_name = doc_name.Trim();
                                    docInfo = subparts[subparts.Length - 1].Trim();  // last of the array
                                }
                                docInfo = docInfo[..^1];
                                string[] parameters = docInfo.Split('-');

                                if (parameters.Length == 1)   // no hyphen
                                {
                                    doc_type = docInfo.Trim();
                                    sizeUnits = "";
                                    size = "";
                                }
                                else
                                {
                                    doc_type = parameters[0].Trim();

                                    // replace any non breaking spaces with spaces, split on space
                                    // to separate size from size units (if the latter given)

                                    string sizeString = parameters[1].Replace('\u00A0', '\u0020').Trim();
                                    string[] sizePars = sizeString.Split(' ');
                                    if (sizePars.Length == 1)
                                    {
                                        sizeUnits = sizeString.Trim();
                                        size = "";
                                    }
                                    else
                                    {
                                        size = sizePars[0].Trim();
                                        sizeUnits = sizePars[1].Trim();
                                    }
                                }
                            }

                            // identify resource type in MDR terms

                            if (doc_type == "PDF")
                            {
                                doc_type_id = 11;
                                access_type_id = 11;
                            }

                            if (doc_type == "HTM")
                            {
                                object_type = "List of web links";
                                object_type_id = 86;
                                doc_type = "Web text";
                                doc_type_id = 35;
                                access_type_id = 12;
                            }

                            // code the common object types using the object name

                            if (object_type_id == 0)
                            {
                                string doc = doc_name.ToLower();
                                if (doc.Contains("protocol"))
                                {
                                    object_type = "Study Protocol";
                                    object_type_id = 11;
                                }
                                else if (doc.Contains("data dictionar"))
                                {
                                    object_type = "Data Dictionary";
                                    object_type_id = 31;
                                }
                                else if (doc.Contains("manual of operations"))
                                {
                                    object_type = "Manual of Operations";
                                    object_type_id = 35;
                                }
                                else if (doc.Contains("manual of procedures"))
                                {
                                    object_type = "Manual of Procedures";
                                    object_type_id = 36;
                                }
                                else if (doc.Contains("forms"))
                                {
                                    object_type = "Data collection forms";
                                    object_type_id = 21;
                                }

                                // for all those left call into the database table 
                                if (object_type_id == 0)
                                {
                                    ObjectTypeDetails? object_type_details = _repo.FetchDocTypeDetails(doc_name);
                                    if (object_type_details?.type_id is not null)
                                    {
                                        object_type_id = object_type_details.type_id;
                                        object_type = object_type_details.type_name;
                                    }
                                    else
                                    {
                                        _logging_helper.LogLine("!!!! Need to map " + doc_name + " in pp.document_types table !!!!");
                                        st.UnmatchedDocTypes.Add(doc_name);
                                    }
                                }
                            }

                            study_resources.Add(new Resource(doc_name, object_type_id, object_type, doc_type_id, doc_type,
                                                                    access_type_id, url, size, sizeUnits));
                        }
                    }
                }
            }
        }                
        return pubs_link;
    }


    void ProcessConsents(BioLincc_Record st, IEnumerable<HtmlNode> entries)
    {
        // Scan second table with any consent restriction details
        bool comm_use_data_restrics = false;
        bool data_restrics_based_on_aor = false;
        string specific_consent_restrics = "";

        foreach (HtmlNode ent_node in entries)
        {
            HtmlNode? entryBold = ent_node.CssSelect("b").FirstOrDefault();
            string? attribute_name = entryBold?.InnerText.Trim();

            HtmlNode? entrySupp = ent_node.CssSelect("em").FirstOrDefault();
            string? supp_text = entrySupp?.InnerText.Trim();

            string? attribute_value = ent_node.InnerText;

            if (!string.IsNullOrEmpty(attribute_name) && !string.IsNullOrEmpty(attribute_value))
            {
                if (attribute_name == "Commercial Use Data Restrictions")
                {
                    string? comm_use_restrictions = attribute_value.RemoveLabelAndSupp(attribute_name, supp_text);
                    if (comm_use_restrictions is not null)
                    {
                        comm_use_data_restrics = (comm_use_restrictions.ToLower() == "yes");
                    }
                }

                if (attribute_name == "Data Restrictions Based On Area Of Research")
                {
                    string? aor_use_restrictions = attribute_value.RemoveLabelAndSupp(attribute_name, supp_text);
                    if (aor_use_restrictions is not null)
                    {
                        data_restrics_based_on_aor = (aor_use_restrictions.ToLower() == "yes");
                    }
                }

                if (attribute_name == "Specific Consent Restrictions")
                {
                    specific_consent_restrics = attribute_value.RemoveLabelAndSupp(attribute_name, supp_text);
                }

                // for the datasets, construct any consent constraints
                string restrictions = "";

                if (comm_use_data_restrics && data_restrics_based_on_aor)
                {
                    restrictions += "Restrictions reported on use of data for commercial purposes, and depending on the area of research. ";
                }
                else if (data_restrics_based_on_aor)
                {
                    restrictions += "Restrictions reported on the use of data depending on the area of research. ";
                }
                else if (comm_use_data_restrics)
                {
                    restrictions += "Restrictions reported on use of data for commercial purposes. ";
                }

                if (!string.IsNullOrEmpty(specific_consent_restrics))
                {
                    restrictions += specific_consent_restrics;
                }

                if (restrictions != "")
                {
                    st.dataset_consent_type_id = 9;
                    st.dataset_consent_type = "Not classified but comment on consent present";
                }
                else
                {
                    st.dataset_consent_type_id = 0;
                    st.dataset_consent_type = "Not yet known";
                }

                st.dataset_consent_restrictions = restrictions;
            }
        }
    }


    async Task ProcessPublicationData(BioLincc_Record st, IEnumerable<HtmlNode> publinks, List<AssocDoc> assoc_docs)
    {
        string? pubNodeId, link_url;
        foreach (HtmlNode pubnode in publinks)
        {
            pubNodeId = pubnode.Attributes["href"].Value;
            link_url = "https://biolincc.nhlbi.nih.gov/publications/" + pubNodeId;

            Thread.Sleep(500);
            WebPage? pubsDetailsPage = await _ch.GetPageAsync(link_url);
            if (pubsDetailsPage is null)
            {
                _logging_helper.LogError("Attempt to access specific study link details at " + link_url + " for " + st.sd_sid + " failed");
            }
            else
            {
                // set up publication record

                AssocDoc pubdets = new(link_url);
                var mainData = pubsDetailsPage.Find("div", By.Class("main"));
                HtmlNode? pubTitle = mainData.CssSelect("h1 b").FirstOrDefault();
                if (pubTitle is not null)
                {
                    pubdets.title = pubTitle.InnerText.Trim();
                }

                // Other available details
                IEnumerable<HtmlNode>? pubData = mainData.CssSelect("p");
                if (pubData?.Any() == true)
                {
                    foreach (HtmlNode node in pubData)
                    {
                        HtmlNode? inBold = node.CssSelect("b").FirstOrDefault();
                        if (inBold is not null)
                        {
                            string attType = inBold.InnerText.Trim();
                            string att_value = node.InnerText.Replace(attType, "").Replace("\n", "").Replace("\r", "").Trim();

                            if (attType.EndsWith(":")) attType = attType[..^1];

                            switch (attType)
                            {
                                case "Pubmed ID":
                                    {
                                        pubdets.pubmed_id = att_value;
                                        break;
                                    }
                                case "Pubmed Central ID":
                                    {
                                        pubdets.pmc_id = att_value;
                                        break;
                                    }
                                case "Cite As":
                                    {
                                        pubdets.display_title = att_value;
                                        break;
                                    }
                                case "Journal":
                                    {
                                        pubdets.journal = att_value;
                                        break;
                                    }
                                case "Publication Date":
                                    {
                                        pubdets.pub_date = att_value;
                                        break;
                                    }
                            }
                        }
                    }
                }
                assoc_docs.Add(pubdets);
            }
        }
    }
}
