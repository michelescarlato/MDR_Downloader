using MDR_Downloader.Helpers;
namespace MDR_Downloader.isrctn;

public class ISRCTN_Processor
{
    public Study? GetFullDetails(FullTrial ft, ILoggingHelper logging_helper)
    {
        Study st = new();

        List<Identifier> identifiers = new();
        List<string> recruitmentCountries = new();
        List<StudyCentre> centres = new();
        List<StudyOutput> outputs = new();
        List<StudyAttachedFile> attachedFiles = new();
        List<StudyContact> contacts = new();
        List<StudySponsor> sponsors = new();
        List<StudyFunder> funders = new();
        List<string> dataPolicies = new();

        var tr = ft.trial;
        if (tr is null)
        {
            logging_helper.LogError("Unable to find ISRCTN trial data - cannot proceed");
            return null;
        }
        if (tr.isrctn?.value is null)
        {
            logging_helper.LogError("Unable to find ISRCTN value - cannot proceed");
            return null;
        }
        
        st.sd_sid = "ISRCTN" + tr.isrctn.value.ToString("00000000");
        st.dateIdAssigned = tr.isrctn?.dateAssigned;
        st.lastUpdated = tr.lastUpdated;

        var d = tr.trialDescription;
        if (d is not null)
        {
            st.title = d.title;
            st.scientificTitle = d.scientificTitle;
            st.acronym = d.acronym;
            st.studyHypothesis = d.studyHypothesis;
            st.primaryOutcome = d.primaryOutcome;
            st.secondaryOutcome = d.secondaryOutcome;
            st.trialWebsite = d.trialWebsite;
            st.ethicsApproval = d.ethicsApproval;

            string? pes = d.plainEnglishSummary;
            if (pes is not null)
            {
                // Attempt to find the beginning of the 'discarded' sections.
                // If found discard those sections.

                int endpos = pes.IndexOf("What are the possible benefits and risks", StringComparison.Ordinal);
                if (endpos == -1)
                {
                    endpos = pes.IndexOf("What are the potential benefits and risks", StringComparison.Ordinal);
                }
                if (endpos != -1)
                {
                    pes = pes[..endpos];
                }

                pes = pes.Replace("Background and study aims", "Background and study aims\n");
                pes = pes.Replace("Who can participate?", "\nWho can participate?\n");
                pes = pes.Replace("What does the study involve?", "\nWhat does the study involve?\n");
                pes = pes.CompressSpaces();
                
                st.plainEnglishSummary = pes;
            }
        }

        var g = tr.trialDesign;
        if (g is not null)
        {
            st.studyDesign = g.studyDesign;
            st.primaryStudyDesign = g.primaryStudyDesign;
            st.secondaryStudyDesign = g.secondaryStudyDesign;
            st.trialSetting = g.trialSetting;
            st.trialType = g.trialType;
            st.overallStatusOverride = g.overallStatusOverride;
            st.overallStartDate = g.overallStartDate;
            st.overallEndDate = g.overallEndDate;
        }

        var p = tr.participants;
        if (p is not null)
        {
            st.participantType = p.participantType;
            st.inclusion = p.inclusion;
            st.ageRange = p.ageRange;
            st.gender = p.gender;
            st.targetEnrolment = p.targetEnrolment;
            st.totalFinalEnrolment = p.totalFinalEnrolment;
            st.totalTarget = p.totalTarget;
            st.exclusion = p.exclusion;
            st.patientInfoSheet = p.patientInfoSheet;
            st.recruitmentStart = p.recruitmentStart;
            st.recruitmentEnd = p.recruitmentEnd;
            st.recruitmentStatusOverride = p.recruitmentStatusOverride;

            var trial_centres = p.trialCentres;
            if (trial_centres?.Any() is true)
            {
                foreach (var cr in trial_centres)
                {
                    centres.Add(new StudyCentre(cr.name, cr.address, cr.city, 
                                                cr.state, cr.country));
                }
            }

            string[]? recruitment_countries = p.recruitmentCountries;
            if (recruitment_countries?.Any() is true)
            {
                foreach(string s in recruitment_countries)
                {
                    // regularise these common alternative spellings
                    var t = s.Replace("Korea, South", "South Korea");
                    t = t.Replace("Congo, Democratic Republic", "Democratic Republic of the Congo");

                    string t2 = t.ToLower();
                    if (t2 == "england" || t2 == "scotland" ||
                                    t2 == "wales" || t2 == "northern ireland")
                    {
                         t = "United Kingdom";
                    }
                    if (t2 == "united states of america")
                    {
                         t = "United States";
                    }

                    // Check for duplicates before adding,
                    // especially after changes above

                    if (recruitmentCountries.Count == 0)
                    {
                        recruitmentCountries.Add(t);
                    }
                    else
                    {
                        bool add_country = true;
                        foreach (string cnt in recruitmentCountries)
                        {
                            if (cnt == t)
                            {
                                add_country = false;
                                break;
                            }
                        }
                        if (add_country)
                        {
                            recruitmentCountries.Add(t);
                        }
                    }
                }
            }
        }

        var c = tr.conditions?.condition;
        if (c is not null)
        {
            st.conditionDescription = c.description;
            st.diseaseClass1 = c.diseaseClass1;
            st.diseaseClass2 = c.diseaseClass2;
        }

        var i = tr.interventions?.intervention;
        if (i is not null)
        {
            st.interventionDescription = i.description;
            st.interventionType = i.interventionType;
            st.phase = i.phase;
            st.drugNames = i.drugNames;
        }

        var r = tr.results;
        if (r is not null)
        {
            st.publicationPlan = r.publicationPlan;
            st.ipdSharingStatement = r.ipdSharingStatement;
            st.intentToPublish = r.intentToPublish;
            st.publicationDetails = r.publicationDetails;
            st.publicationStage = r.publicationStage;
            st.biomedRelated = r.biomedRelated;
            st.basicReport = r.basicReport;
            st.plainEnglishReport = r.plainEnglishReport;

            var dps = r.dataPolicies;
            if (dps?.Any() is true)
            {
                foreach (string s in dps)
                {
                    dataPolicies.Add(s);
                }
            }
        }
        
        var er = tr.externalRefs;
        if (er is not null)
        {
            string? ext_ref = er.doi;
            if (!string.IsNullOrEmpty(ext_ref) && ext_ref != "N/A" 
                                               && ext_ref != "Not Applicable" && ext_ref != "Nil known")
            {
                st.doi = ext_ref;
            }

            ext_ref = er.eudraCTNumber;
            if (!string.IsNullOrEmpty(ext_ref) && ext_ref != "N/A" 
                                               && ext_ref != "Not Applicable" && ext_ref != "Nil known")
            {
                identifiers.Add(new Identifier(11, "Trial Registry ID", ext_ref, 100123, "EU Clinical Trials Register"));
            }

            ext_ref = er.irasNumber;
            if (!string.IsNullOrEmpty(ext_ref) && ext_ref != "N/A" 
                                               && ext_ref != "Not Applicable" && ext_ref != "Nil known")
            {
                identifiers.Add(new Identifier(41, "Regulatory Body ID", ext_ref, 101409, "Health Research Authority"));
            }

            ext_ref = er.clinicalTrialsGovNumber;
            if (!string.IsNullOrEmpty(ext_ref) && ext_ref != "N/A" 
                                               && ext_ref != "Not Applicable" && ext_ref != "Nil known")
            {
                identifiers.Add(new Identifier(11, "Trial Registry ID", ext_ref, 100120, "Clinicaltrials.gov"));
            }

            ext_ref = er.protocolSerialNumber;
            if (!string.IsNullOrEmpty(ext_ref) && ext_ref != "N/A" 
                                               && ext_ref != "Not Applicable" && ext_ref != "Nil known")
            {
                if (ext_ref.Contains(';'))
                {
                    string[] id_items = ext_ref.Split(";");
                    foreach (string id_item in id_items)
                    {
                        identifiers.Add(new Identifier(0, "To be determined", id_item.Trim(), 0, "To be determined"));
                    }
                }
                else if (ext_ref.Contains(',') && (ext_ref.ToLower().Contains("iras") || ext_ref.ToLower().Contains("hta")))
                {
                    // Don't split on commas unless these common id types are included.

                    string[] id_items = ext_ref.Split(",");
                    foreach (string id_item in id_items)
                    {
                        identifiers.Add(new Identifier(0, "To be determined", id_item.Trim(), 0, "To be determined"));
                    }
                }
                else
                {
                    identifiers.Add(new Identifier(0, "To be determined", ext_ref.Trim(), 0, "To be determined"));
                }
            }
        }

        // Do additional files first
        // so that details can be checked from the outputs data

        var afs = tr.attachedFiles;
        if (afs?.Any() is true)
        {
            foreach (var v in afs)
            {
                attachedFiles.Add(new StudyAttachedFile(v.description, v.name, v.id, v.@public));
            }
        }

        var ops = tr.outputs;
        if (ops?.Any() is true)
        {
            //bool local_urls_collected = false;
            //Dictionary<string, string>? output_urls = null;

            foreach (var v in ops)
            {
                StudyOutput sop = new StudyOutput(v.description, v.productionNotes, v.outputType,
                            v.artefactType, v.dateCreated, v.dateUploaded, v.peerReviewed,
                            v.patientFacing, v.createdBy, v.externalLink?.url, v.localFile?.fileId,
                            v.localFile?.originalFilename, v.localFile?.downloadFilename,
                            v.localFile?.version, v.localFile?.mimeType);
                
                if (sop.artefactType == "LocalFile")
                {
                    // Check it is in the attached files list and public.

                    if (attachedFiles?.Any() is true)
                    {
                        foreach (var af in attachedFiles) 
                        { 
                            if (sop.fileId == af.id) 
                            {
                                sop.localFilePublic = af.@public;
                                sop.localFileURL = af.downloadUrl;
                                break;
                            }
                        }
                    }
                    
                    /*

                    // need to go to the page to get the url for any local file
                    // (Not available in the API data)
                    // May have already been collected from an earlier output
                    // in the 'ops' collection (i.e. if a study hgs 2 or more
                    // local files). If not fill the url collection by web scraping.

                    if (!local_urls_collected)
                    {
                        string details_url = "https://www.isrctn.com/" + st.sd_sid;
                        ScrapingHelpers ch = new(logging_helper);
                        Thread.Sleep(500);
                        
                        // The initial web page access results in a blocking page
                        // A second access is required to actually access the page.
                        
                        WebPage? study_page = await ch.GetPageWithRetriesAsync(details_url, 100, st.sd_sid);
                        if (study_page is not null)
                        {
                            output_urls = new();
                            HtmlNode? section_div = study_page.Find("div", By.Class("l-Main")).FirstOrDefault();
                            HtmlNode? article = section_div?.SelectSingleNode("article[1]");
                            IEnumerable<HtmlNode>? publications = article?.SelectNodes("//section/div[1]/h2[text()='Results and Publications']/following-sibling::div[1]/h3");
                            if (publications?.Any() is true)
                            {
                                foreach (var pub in publications)
                                {
                                    string? pub_name = pub.InnerText.Tidy();
                                    if (pub_name == "Trial outputs")
                                    {
                                        HtmlNode? output_table = pub.SelectSingleNode("following-sibling::div[1]/table[1]/tbody[1]");
                                        if (output_table is not null)
                                        {
                                            var table_rows = output_table.SelectNodes("tr");
                                            if (table_rows?.Any() is true)
                                            {
                                                foreach (var table_row in table_rows)
                                                {
                                                    var output_attributes = table_row.SelectNodes("td")?.ToArray();
                                                    if (output_attributes?.Any() is true)
                                                    {
                                                        HtmlNode? output_link = output_attributes[0]?.SelectSingleNode("a[1]");
                                                        if (output_link is not null)
                                                        {
                                                            string? output_title = output_link.GetAttributeValue("title").ReplaceUnicodes();
                                                            string? output_url = output_link.GetAttributeValue("href");
                                                            if (!string.IsNullOrEmpty(output_title) && !string.IsNullOrEmpty(output_url))
                                                            {
                                                                if (!output_url.ToLower().StartsWith("http"))
                                                                {
                                                                    output_url = output_url.StartsWith("/") 
                                                                        ? "https://www.isrctn.com" + output_url 
                                                                        : "https://www.isrctn.com/" + output_url;
                                                                }

                                                                // Very occasionally the same file and output url is duplicated.
                                                                // This must be trapped to avoid an exception.

                                                                bool add_entry = true;
                                                                if(output_urls.Count > 0)
                                                                {
                                                                    foreach(KeyValuePair<string, string> entry in output_urls)
                                                                    {
                                                                        if (output_title == entry.Key)
                                                                        {
                                                                            add_entry = false;
                                                                            break;
                                                                        }
                                                                    }
                                                                }
                                                                
                                                                if (add_entry)
                                                                {
                                                                    output_urls.Add(output_title, output_url);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            local_urls_collected = true;
                        }
                        
                    }

                    if (output_urls?.Any() is true)
                    {
                        // Not clear if the original or download file name should
                        // be used to try and match the url (normally identical).

                        if (sop.downloadFilename is not null)
                        {
                            sop.localFileURL = output_urls[sop.downloadFilename];

                            if (sop.localFileURL is null && sop.originalFilename is not null)
                            {
                                sop.localFileURL = output_urls[sop.originalFilename];
                            }
                        }
                    }
                    */
                }

                outputs.Add(sop);
            }
           
        }

        var tr_contacts = ft.contact;
        if(tr_contacts?.Any() is true)
        {
            foreach(var v in tr_contacts)
            {
                contacts.Add(new StudyContact(v.forename, v.surname, v.orcid, v.contactType,
                             v.contactDetails?.address, v.contactDetails?.city, v.contactDetails?.country,
                             v.contactDetails?.email));
            }
        }

        var tr_sponsors = ft.sponsor;
        if (tr_sponsors?.Any() is true)
        {
            foreach (var v in tr_sponsors)
            {
                sponsors.Add(new StudySponsor(v.organisation, v.website, v.sponsorType, v.gridId,
                             v.contactDetails?.city, v.contactDetails?.country));            }
        }

        var tr_funders = ft.funder;
        if (tr_funders?.Any() is true)
        {
            foreach (var v in tr_funders)
            {
                funders.Add(new StudyFunder(v.name, v.fundRef));
            }
        }

        st.identifiers = identifiers;
        st.recruitmentCountries = recruitmentCountries;
        st.centres = centres;
        st.outputs = outputs;
        st.attachedFiles = attachedFiles;
        st.contacts = contacts;
        st.sponsors = sponsors;
        st.funders = funders;
        st.dataPolicies = dataPolicies;

        return st;
    }

}
