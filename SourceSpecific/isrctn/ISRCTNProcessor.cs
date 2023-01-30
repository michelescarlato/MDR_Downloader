using HtmlAgilityPack;
using MDR_Downloader.euctr;
using MDR_Downloader.Helpers;
using MDR_Downloader.isrctn;
using MDR_Downloader.pubmed;
using ScrapySharp.Extensions;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;

namespace MDR_Downloader.isrctn;

public class ISRCTN_Processor
{
    public async Task<Study> GetFullDetails(FullTrial ft, ILoggingHelper logging_helper)
    {
        Study st = new();

        List<Identifier>? identifiers = new();
        List<string>? recruitmentCountries = new();
        List<StudyCentre>? centres = new();
        List<StudyOutput>? outputs = new();
        List<StudyAttachedFile>? attachedFiles = new();
        List<StudyContact>? contacts = new();
        List<StudySponsor>? sponsors = new();
        List<StudyFunder>? funders = new();
        List<string>? dataPolicies = new();

        var tr = ft.trial;
        if (tr is not null)
        {
            st.sd_sid = "ISRCTN" + tr.isrctn?.value.ToString("00000000");
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
                    // Attempt to find the beginningof the 'discarded' sections.
                    // If found discard those sections.

                    int endpos = pes.IndexOf("What are the possible benefits and risks");
                    if (endpos == -1)
                    {
                        endpos = pes.IndexOf("What are the potential benefits and risks");
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

                var tcentres = p.trialCentres;
                if (tcentres?.Any() == true)
                {
                    foreach (var cr in tcentres)
                    {
                        centres.Add(new StudyCentre(cr.name, cr.address, cr.city, 
                                                    cr.state, cr.country));
                    }
                }

                string[]? reccountries = p.recruitmentCountries;
                if (reccountries?.Any() == true)
                {
                    foreach(string s in reccountries)
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
                if (dps?.Any() == true)
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
                string? eref = er.doi;
                if (!string.IsNullOrEmpty(eref) && eref != "N/A" && eref != "Not Applicable" && eref != "Nil known")
                {
                    st.doi = eref;
                }

                eref = er.eudraCTNumber;
                if (!string.IsNullOrEmpty(eref) && eref != "N/A" && eref != "Not Applicable" && eref != "Nil known")
                {
                    identifiers.Add(new Identifier(11, "Trial Registry ID", eref, 100123, "EU Clinical Trials Register"));
                }

                eref = er.irasNumber;
                if (!string.IsNullOrEmpty(eref) && eref != "N/A" && eref != "Not Applicable" && eref != "Nil known")
                {
                    identifiers.Add(new Identifier(41, "Regulatory Body ID", eref, 101409, "Health Research Authority"));
                }

                eref = er.clinicalTrialsGovNumber;
                if (!string.IsNullOrEmpty(eref) && eref != "N/A" && eref != "Not Applicable" && eref != "Nil known")
                {
                    identifiers.Add(new Identifier(11, "Trial Registry ID", eref, 100120, "Clinicaltrials.gov"));
                }

                eref = er.protocolSerialNumber;
                if (!string.IsNullOrEmpty(eref) && eref != "N/A" && eref != "Not Applicable" && eref != "Nil known")
                {
                    if (eref.Contains(";"))
                    {
                        string[] iditems = eref.Split(";");
                        foreach (string iditem in iditems)
                        {
                            identifiers.Add(new Identifier(0, "To be determined", iditem.Trim(), 0, "To be determined"));
                        }
                    }
                    else if (eref.Contains(",") && (eref.ToLower().Contains("iras") || eref.ToLower().Contains("hta")))
                    {
                        // Don't split on commas unless these common id types are included.

                        string[] iditems = eref.Split(",");
                        foreach (string iditem in iditems)
                        {
                            identifiers.Add(new Identifier(0, "To be determined", iditem.Trim(), 0, "To be determined"));
                        }
                    }
                    else
                    {
                        identifiers.Add(new Identifier(0, "To be determined", eref.Trim(), 0, "To be determined"));
                    }
                }
            }

            // Do aditional files first
            // so that details can be checked from the outputs data

            var afs = tr.attachedFiles;
            if (afs?.Any() == true)
            {
                foreach (var v in afs)
                {
                    attachedFiles.Add(new StudyAttachedFile(v.description, v.name, v.id, v.@public));
                }
            }

            var ops = tr.outputs;
            if (ops?.Any() == true)
            {
                bool local_urls_collected = false;
                Dictionary<string, string>? output_urls = null;

                foreach (var v in ops)
                {
                    StudyOutput sop = new StudyOutput(v.description, v.productionNotes, v.outputType,
                                v.artefactType, v.dateCreated, v.dateUploaded, v.peerReviewed,
                                v.patientFacing, v.createdBy, v.externalLink?.url, v.localFile?.fileId,
                                v.localFile?.originalFilename, v.localFile?.downloadFilename,
                                v.localFile?.version, v.localFile?.mimeType);
                    
                    if (sop.artefactType == "LocalFile")
                    {
                        // First check it is in the attached files list and public.
                        // (Not all listed local outputs are in the attached files
                        // list - though the great majority are).

                        if (attachedFiles?.Any() == true)
                        {
                            foreach (var af in attachedFiles) 
                            { 
                                if (sop.fileId == af.id) 
                                {
                                    sop.localFilePublic = af.@public;
                                    break;
                                }
                            }
                        }

                        // need to go to the page and get the url for the local file
                        // (Not available in the API data)
                        // May have already been collected from an earlier output. and
                        // fill it by web scraping.

                        // get the url data for this study if not already collected.

                        if (!local_urls_collected)
                        {
                            string details_url = "https://www.isrctn.com/" + st.sd_sid;
                            ScrapingHelpers ch = new(logging_helper);
                            Thread.Sleep(500);
                            WebPage? study_page = await ch.GetPageAsync(details_url);
                            Thread.Sleep(100); 
                            study_page = await ch.GetPageAsync(details_url);
                            if (study_page is not null)
                            {
                                output_urls = new();
                                HtmlNode? section_div = study_page.Find("div", By.Class("l-Main")).FirstOrDefault();
                                HtmlNode? article = section_div?.SelectSingleNode("article[1]");
                                IEnumerable<HtmlNode>? publications = article?.SelectNodes("//section/div[1]/h2[text()='Results and Publications']/following-sibling::div[1]/h3");
                                if (publications?.Any() == true)
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
                                                if (table_rows?.Any() == true)
                                                {
                                                    foreach (var table_row in table_rows)
                                                    {
                                                        var output_attributes = table_row.SelectNodes("td")?.ToArray();
                                                        if (output_attributes?.Any() == true)
                                                        {
                                                            HtmlNode? output_link = output_attributes[0]?.SelectSingleNode("a[1]");
                                                            if (output_link is not null)
                                                            {
                                                                string? output_title = output_link.GetAttributeValue("title").ReplaceUnicodes();
                                                                string? output_url = output_link.GetAttributeValue("href");
                                                                if (!string.IsNullOrEmpty(output_url))
                                                                {
                                                                    if (!output_url.ToLower().StartsWith("http"))
                                                                    {
                                                                        output_url = output_url.StartsWith("/") ? "https://www.isrctn.com" + output_url : "https://www.isrctn.com/" + output_url;
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

                        if (output_urls.Count > 0)
                        {
                            // Not clear if the original or download file name shopuld
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
                    }

                    outputs.Add(sop);
                }
            }


            
        }

        var tr_contacts = ft.contact;
        if(tr_contacts?.Any() == true)
        {
            foreach(var v in tr_contacts)
            {
                contacts.Add(new StudyContact(v.forename, v.surname, v.orcid, v.contactType,
                             v.contactDetails?.address, v.contactDetails?.city, v.contactDetails?.country,
                             v.contactDetails?.email));
            }
        }


        var tr_sponsors = ft.sponsor;
        if (tr_sponsors?.Any() == true)
        {
            foreach (var v in tr_sponsors)
            {
                sponsors.Add(new StudySponsor(v.organisation, v.website, v.sponsorType, v.gridId,
                             v.contactDetails?.city, v.contactDetails?.country));            }
        }


        var tr_funders = ft.funder;
        if (tr_funders?.Any() == true)
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
