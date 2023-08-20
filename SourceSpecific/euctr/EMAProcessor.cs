using HtmlAgilityPack;
using MDR_Downloader.Helpers;
using ScrapySharp.Extensions;
using ScrapySharp.Html;
using ScrapySharp.Network;
namespace MDR_Downloader.euctr;

public class EMAProcessor
{
    private readonly ILoggingHelper _loggingHelper;
    
    public EMAProcessor(ILoggingHelper loggingHelper)
    {
        _loggingHelper = loggingHelper;
    }

    public async Task<Euctr_Record?> ProcessTrial(Trial t, DateTime date_revised)
    {
        Main? mn = t.main;
        if (mn is null)
        {
            _loggingHelper.LogError($"Unable to find 'main' entity in trial details - cannot proceed");
            return null;  // main details are missing, seems very unlikely but...
        }
        string? search_url = mn.url;
        if (search_url is null)
        {
            _loggingHelper.LogError($"Unable to find search url in trial details for {t.main?.trial_id} - cannot proceed");
            return null;  // cannot get to summary page to get essential details...
        }

        // get summary, using routine in Euctr processor
        // then get information from summary, using euctr processor function
        
        ScrapingHelpers ch = new(_loggingHelper);
        
        WebPage? summaryPage = await ch.GetPageAsync(search_url);
        if (summaryPage is null)
        {
            _loggingHelper.LogError($"Unable to reach search results page");
            return null;  // cannot get to summary page to get essential details...
        }

        HtmlNode? pageContent = summaryPage.Find("div", By.Class("results")).FirstOrDefault();
        if (pageContent is null)
        {
            _loggingHelper.LogError($"Unable to find results boxes");
            return null;
        }
        
        HtmlNode? studyBox = pageContent.CssSelect(".result").FirstOrDefault();
        if (studyBox is null)
        {
            _loggingHelper.LogError($"Unable to find results box");
            return null;
        }

        EUCTR_Helper ep = new(_loggingHelper);
        Euctr_Record? ed = ep.GetInfoFromSummaryBox(studyBox);

        
        // ed now has sd_sid (Eudract Id), sponsors id and start date, 
        // sponsor name, medical condition as text and possibly as a Meddra list,
        // age and gender as text strings, list of countries (protocols) with
        // associated statuses, details_url and results url, if one exists.
        
        if (ed is null)
        {
            _loggingHelper.LogError($"Unable to use results box to get basic information on study");
            return null;
        }
        ed.search_url = search_url;
        if (mn.trial_id is not null && mn.trial_id.Length >= 17)
        {
            ed.details_url = @"https://www.clinicaltrialsregister.eu/ctr-search/trial/"
                             + mn.trial_id[..14] + @"/" + mn.trial_id[^2..];
        }
        ed.date_registration = mn.date_registration;
        ed.date_last_revised = date_revised;
        
        // ed.start_date = mn.date_enrolment;  // don't overwrite the value from the 'whole study' box
        
        if (mn.study_type is not null && mn.study_type.StartsWith("Interventional clinical trial"))
        {
            ed.study_type = "Interventional"; // They all are at the moment!
        }
        
        ed.recruitment_status = mn.recruitment_status;
        
        // Titles - sorted out in harvest process.
        
        ed.scientific_title = mn.scientific_title;
        ed.public_title = mn.public_title;
        ed.scientific_acronym = mn.scientific_acronym;
        ed.acronym = mn.acronym;
        
        // Sponsors and funders
        
        ed.organisations = new List<EMAOrganisation>();
        
        string? pri_spons = mn.primary_sponsor; 
        string[]? secspons = t.secondary_sponsor;
        string[]? srceSupp = t.source_support;

        if (!string.IsNullOrEmpty(pri_spons))
        {
            ed.organisations.Add(new EMAOrganisation(54, "Trial Sponsor", pri_spons));
        }
        if (secspons?.Any() is true)
        {
            foreach (string ss in secspons)
            {
                if (!string.IsNullOrEmpty(ss) && !already_in_org_list(ss))
                {
                    ed.organisations.Add(new EMAOrganisation(54, "Trial Sponsor", ss));
                }

            }
        }
        if (srceSupp?.Any() is true)
        {
            foreach (string ss in srceSupp)
            {
                if (!string.IsNullOrEmpty(ss))    // add even if the same as the sponsor
                {
                    ed.organisations.Add(new EMAOrganisation(58, "Study Funder", ss));
                }
            }
        }

        bool already_in_org_list(string candidate_org)
        {
            bool already_in_list = false;
            foreach (EMAOrganisation g in ed.organisations)
            {
                if (g.org_name == candidate_org)
                {
                    already_in_list = true;
                    break;
                }
            }
            return already_in_list;
        }
        
        
        // Add identifiers.
       
        ed.identifiers = new List<Identifier>   // Initialise with the Eudract Id
        {
            new (11, "Trial Registry ID", ed.sd_sid, 100123, "EU Clinical Trials Register")
        };
        if (!string.IsNullOrEmpty(ed.sponsors_id))    // Add the sponsor's id
        {
            string sp_name = !string.IsNullOrEmpty(ed.sponsor_name)
                ? ed.sponsor_name
                : "No organisation name provided in source data";
            ed.identifiers.Add(new Identifier(14, "Sponsor ID", ed.sponsors_id, null, sp_name));
        }
        string? who_utn = mn.utrn;
        if (!string.IsNullOrEmpty(who_utn))
        {
            ed.identifiers.Add(new Identifier(11, "Trial Registry ID", who_utn, 100115, 
                "International Clinical Trials Registry Platform"));
        }

        Secondary_id[]? sec_ids = t.secondary_ids;
        if (sec_ids?.Any() is true)
        {
            foreach (Secondary_id secid in sec_ids)
            {
                if (secid.issuing_authority == "US NCT Number")
                {
                    ed.identifiers.Add(new Identifier(11, "Trial Registry ID", 
                        secid.sec_id, 100120, "ClinicalTrials.gov"));
                }
                if (secid.issuing_authority == "ISRCTN Number")
                {
                    ed.identifiers.Add(new Identifier(11, "Trial Registry ID", 
                        secid.sec_id, 100126, "ISRCTN"));
                }
            }
        }
       
        // Enrolment figures.
        
        ed.target_size = mn.target_size;
        ed.results_actual_enrolment = mn.results_actual_enrolment;

        // Primary outcomes and endpoints.
        
        string[]? outcome_info = t.primary_outcome;
        if (outcome_info?.Any() is true)
        {
            foreach (string outstring in outcome_info)
            {
                if (outstring.ToLower().StartsWith("main objective"))
                {
                    ed.primary_objectives = outstring;
                }
                else if (outstring.ToLower().StartsWith("primary end point"))
                {
                    ed.primary_endpoints = outstring;
                }
            }
        }
        
        // Inclusion / exclusion criteria and gender, age information.

        Criteria? crits = t.criteria;
        if (crits is not null)
        {
            string age_string = "";
            ed.exclusion_criteria = crits.exclusion_criteria;  
            if (!string.IsNullOrEmpty(crits.inclusion_criteria))
            {
                int age_pos = crits.inclusion_criteria.IndexOf("Are the trial subjects ",
                    0, StringComparison.Ordinal);
                ed.inclusion_criteria = crits.inclusion_criteria[..age_pos];
                age_string = crits.inclusion_criteria[age_pos..];
            }
            
            ed.minage = crits.agemin;      // usually null at present
            ed.maxage = crits.agemax;      // usually null at present
            if (string.IsNullOrEmpty(crits.agemin) && string.IsNullOrEmpty(crits.agemax))
            {
                // Need to use the structured textual description in 'inclusion criteria'.
                
                if (!string.IsNullOrEmpty(age_string))
                {
                    string[] age_strings = age_string.Split('\n',
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (age_strings.Any())
                    {
                        ed = GetStudyPopulation(ed, age_strings);
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(crits.gender))
            {
                if (crits.gender.Contains("Female: yes") && crits.gender.Contains("Male: yes"))
                {
                    ed.gender = "All";
                }
                else if (crits.gender.Contains("Female: yes"))
                {
                    ed.gender = "Female";
                }
                else if (crits.gender.Contains("Male: yes"))
                {
                    ed.gender = "Male";
                }
                else
                {
                    ed.gender = "Not provided";
                }
            }
            else
            {
                ed.gender = "Not provided";
            }
        }

        // Add design information, including phase.
        
        string? design_info = mn.study_design;
        string? phase_info = mn.phase;
        ed.features = new List<EMAFeature>();

        if (!string.IsNullOrEmpty(phase_info))
        {
            string[] phase_strings = phase_info.Split('\n',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (phase_strings.Any())
            {
                foreach (string ast in phase_strings)
                {
                    if (ast.ToLower().EndsWith("yes"))
                    {
                        if (ast.Contains("(Phase I)"))
                        {
                            ed.features.Add(new EMAFeature(20, "phase", 110, "Phase 1"));
                        }
                        else if (ast.Contains("(Phase II)"))
                        {
                            ed.features.Add(new EMAFeature(20, "phase", 120, "Phase 2"));
                        }
                        else if (ast.Contains("(Phase III)"))
                        {
                            ed.features.Add(new EMAFeature(20, "phase", 130, "Phase 3"));
                        }
                        else if (ast.Contains("(Phase IV)"))
                        {
                            ed.features.Add(new EMAFeature(20, "phase", 135, "Phase 4"));
                        }
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(design_info))
        {
            string[] design_strings = design_info.Split('\n',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (design_strings.Any())
            {
                foreach (string ast in design_strings)
                {
                    if (ast.ToLower().EndsWith("yes"))
                    {
                        if (ast.Contains("Randomised"))
                        {
                            ed.features.Add(new EMAFeature(22, "allocation type", 205, "Randomised"));
                        }
                        else if (ast.Contains("Open:"))
                        {
                            ed.features.Add(new EMAFeature(24, "masking", 500, "None (Open Label)"));
                        }
                        else if (ast.Contains("Single blind:"))
                        {
                            ed.features.Add(new EMAFeature(24, "masking", 505, "Single"));
                        }
                        else if (ast.Contains("Double blind:"))
                        {
                            ed.features.Add(new EMAFeature(24, "masking", 510, "Double"));
                        }
                        else if (ast.Contains("Parallel group:"))
                        {
                            ed.features.Add(new EMAFeature(23, "intervention model", 305, "Parallel assignment"));
                        }
                        else if (ast.Contains("Cross over:"))
                        {
                            ed.features.Add(new EMAFeature(23, "intervention model", 310, "Crossover assignment"));
                        }
                    }
                }
            }
        }
        
        // Add countries.
        
        string[]? countries = t.countries;
        if (countries?.Any() is true)
        {
            ed.countries ??= new List<EMACountry>();  // usually will not be null but just in case

            foreach (string country_name in countries)
            {
                if (!country_already_listed(country_name))
                {
                    ed.countries.Add(new EMACountry(country_name, null));
                }
            }
        }

        bool country_already_listed(string country_name)
        {
            bool country_already_there = false;
            foreach (EMACountry ec in ed.countries)
            {
                if (country_name == ec.country_name)
                {
                    country_already_there = true;
                    break;
                }
            }
            return country_already_there;
        }
        
        // Add IMPs information.
        
        string? imps_info = mn.i_freetext;
        ed.imp_topics = new List<EMAImp>();
        if (!string.IsNullOrEmpty(imps_info))
        {
            string[] imps_blocks = imps_info.Split("\n\n",
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (imps_blocks.Any())
            {
                int imp_n = 0;
                foreach (string imp_block in imps_blocks)
                {
                    imp_n++;
                    EMAImp this_imp = new EMAImp(imp_n);
                    string[] imp_strings = imp_block.Split('\n',
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (imp_strings.Any())
                    {
                        foreach (string imp_string in imp_strings)
                        {
                            if (imp_string.ToLower().StartsWith("trade name"))
                            {
                                int colon_pos = imp_string.IndexOf(':');
                                this_imp.trade_name = imp_string[(colon_pos +1)..].Trim();
                            }
                            else if (imp_string.ToLower().StartsWith("product name")) 
                            {
                                int colon_pos = imp_string.IndexOf(':');
                                this_imp.product_name = imp_string[(colon_pos +1)..].Trim();
                            }
                            else if (imp_string.ToLower().StartsWith("inn ")) 
                            {
                                int colon_pos = imp_string.IndexOf(':');
                                this_imp.inn = imp_string[(colon_pos +1)..].Trim();
                            }
                            else if (imp_string.ToLower().StartsWith("cas number")) 
                            {
                                int colon_pos = imp_string.IndexOf(':');
                                this_imp.cas_number = imp_string[(colon_pos +1)..].Trim();
                            }
                        }
                    }
                    ed.imp_topics.Add(this_imp);
                }
            }
        }
        
        // Add any further conditions.
        
        ed.conditions = new List<EMACondition>();
        string? conds_info = mn.hc_freetext;
        if (!string.IsNullOrEmpty(conds_info))
        {
            ed.conditions.Add(new EMACondition(conds_info));
        }
        
        // Contacts.
        // These do not really exist in the EU CTR web page - only as an organisational address.
        // Potentially as a person or organisation in the EMA files, but difficult often to
        // distinguish between them. Almost always the same name is given for both contact roles.
       
        Contact[]? contacts_info = t.contacts;
        if (contacts_info?.Any() is true)
        {
            /*   foreach (Contact cont in contacts_info)
            {
                int contact_type_id = 0;
                string contact_type = "";
                string contact_name = cont.firstname ?? "";
                if (!string.IsNullOrEmpty(cont.middlename))
                {
                    contact_name += " " + cont.middlename;
                }
                if (!string.IsNullOrEmpty(cont.lastname))
                {
                    contact_name += " " + cont.lastname;
                }
                if (cont.type == "Public")
                {
                    // public contact
                }
                else if (cont.type == "Scientific")
                {
                    // scientific contact
                }
                // To add to study_people or study_organisations
                //ed.organisations.Add(new EMAOrganisation(contact_type_id, contact_type, contact_name));
               
            } */
            
        }
        
        // results information

        if (string.IsNullOrEmpty(ed.results_url))
        {
            ed.results_url = mn.results_url_link;  // May already be there
        }
        ed.results_url_protocol = mn.results_url_protocol;
        ed.results_date_posted = mn.results_date_posted;
        
        if (!string.IsNullOrEmpty(ed.results_url))
        {
            Thread.Sleep(500);
            WebPage? resultsPage = await ch.GetPageAsync(ed.results_url);
            if (resultsPage is not null)
            {
                ed = ep.ExtractResultDetails(ed, resultsPage);
            }
            else
            {
                _loggingHelper.LogError(
                    $"Problem in navigating to result details, for {ed.sd_sid}");
            }
        }
        ed.results_IPD_plan = mn.results_IPD_plan;
        ed.results_IPD_description = mn.results_IPD_description;
        
        return ed;
    }
    
    
    private Euctr_Record GetStudyPopulation(Euctr_Record st, string[] age_strings)
    {
        bool includes_under18 = false; bool includes_in_utero = false;
        bool includes_preterm = false; bool includes_newborns = false;
        bool includes_infants = false; bool includes_children = false;
        bool includes_ados = false;
        bool includes_adults = false; bool includes_elderly = false;

        foreach(string ast in age_strings)
        {
            if (ast.ToLower().EndsWith("yes"))
            {
                if (ast.StartsWith("F.1.2 "))
                {
                    includes_adults = true; 
                }
                else if (ast.StartsWith("F.1.3 "))
                {
                    includes_elderly = true;
                }
                else if (ast.StartsWith("F.1.1 "))
                {
                    includes_under18 = true;
                }
                else if (ast.StartsWith("F.1.1.1"))
                {
                    includes_in_utero = true;
                }
                else if (ast.StartsWith("F.1.1.2"))
                {
                    includes_preterm = true;
                }
                else if (ast.StartsWith("F.1.1.3"))
                {
                    includes_newborns = true;
                }
                else if (ast.StartsWith("F.1.1.4"))
                {
                    includes_infants = true;
                }
                else if (ast.StartsWith("F.1.1.5"))
                {
                    includes_children = true;
                }
                else if (ast.StartsWith("F.1.1.6"))
                {
                    includes_ados = true;
                }
            }
        }
    
        if (!includes_under18)
        {
            // No children or adolescents included. If 'elderly' are included no age maximum is presumed.

            if (includes_adults && includes_elderly)
            {
                st.minage = "18";
            }
            else if (includes_adults)
            {
                st.minage = "18";
                st.maxage = "64";
            }
            else if (includes_elderly)
            {
                st.minage = "65";
            }
        }
        else
        {
            // Some under 18s included. First discount the situation where under-18s,
            // adults and elderly are all included corresponds to no age restrictions

            if (includes_under18 && includes_adults && includes_elderly)
            {
                // Leave min and max ages blank
            }
            else
            {
                // First try and obtain a minimum age. Start with the youngest included and work up.

                if (includes_in_utero || includes_preterm || includes_newborns)
                {
                    st.minage = "0 (days)";
                }
                else if (includes_infants)
                {
                    st.minage = "28 (days)";
                }
                else if (includes_children)
                {
                    st.minage = "2";
                }
                else if (includes_ados)
                {
                    st.minage = "12";
                }

                // Then try and obtain a maximum age. Start with the oldest included and work down.

                if (includes_adults)
                {
                    st.maxage = "64";
                }
                else if (includes_ados)
                {
                    st.maxage = "17";
                }
                else if (includes_children)
                {
                    st.maxage = "11";
                }
                else if (includes_infants)
                {
                    st.maxage = "23 (months)";
                }
                else if (includes_newborns)
                {
                    st.maxage = "27 (days)";
                }
                else if (includes_in_utero || includes_preterm)
                {
                    st.maxage = "0 (days)";
                }
            }
        }
        return st;
    }
}
