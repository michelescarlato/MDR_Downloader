using MDR_Downloader.Helpers;
using System.Text.RegularExpressions;

namespace MDR_Downloader.who;

public class WHOHelpers
{
    public List<string>? split_string(string? input_string)
    {
        string? string_list = input_string.Tidy();
        if (string.IsNullOrEmpty(string_list))
        {
            return null;
        }
        return string_list.Split(";").ToList();
    }


    public List<string> split_and_dedup_countries(string countries)
    {
        // countries known to be non-null and already 'tidied'.

        List<string> out_strings = new List<string>();
        List<string> in_strings = countries.Split(";").ToList();

        foreach (string s in in_strings)
        {
            if (out_strings.Count == 0)
            {
                out_strings.Add(s);
            }
            else
            {
                bool add_string = true;
                foreach (string s2 in out_strings)
                {
                    if (s2 == s)
                    {
                        add_string = false;
                        break;
                    }
                }
                if (add_string) out_strings.Add(s);
            }
        }
        return out_strings;
    }


    public List<WhoCondition> GetConditions(string sd_sid, string in_string)
    {
        List<WhoCondition> conditions = new List<WhoCondition>();
        if (!string.IsNullOrEmpty(in_string))
        {
            string? condition_list = in_string.Tidy();
            if (!string.IsNullOrEmpty(condition_list))
            {
                // replace escaped characters to remove the semi-colons
                string rsq = "’";
                condition_list = condition_list.Replace("&lt;", "<").Replace("&gt;", ">");
                condition_list = condition_list.Replace("&#39;", rsq).Replace("&rsquo;", rsq);

                // replace line breaks and hashes with semi-colons, and split
                condition_list = condition_list.Replace("<br>", ";").Replace("<br/>", ";");
                condition_list = condition_list.Replace("#", ";");
                List<string> conds = condition_list.Split(";").ToList();

                foreach (string s in conds)
                {
                    char[] chars_to_lose = { ' ', '(', '.', '-', ';' };
                    string s1 = s.Trim(chars_to_lose);
                    if (s1 != "" && s1.Length > 4)
                    {
                        // does it have an ICD code or similar at the front?
                        // if so extract and put code in code field

                        string code = "", code_system = "";

                        if (s1.Contains("generalization"))
                        {
                            string code_string = "";
                            if (Regex.Match(s1, @"^[A-Z]\d{2}.\d{2} - \[generalization [A-Z]\d{2}.\d:").Success)
                            {
                                code_string = Regex.Match(s1, @"^[A-Z]\d{2}.\d{2} - \[generalization [A-Z]\d{2}.\d:").Value.Trim();
                                code = Regex.Match(code_string, @"[A-Z]\d{2}.\d:$").Value.Trim(':');
                            }

                            else if (Regex.Match(s1, @"^[A-Z]\d{2}.\d{2} - \[generalization [A-Z]\d{2}:").Success)
                            {
                                code_string = Regex.Match(s1, @"^[A-Z]\d{2}.\d{2} - \[generalization [A-Z]\d{2}:").Value.Trim();
                                code = Regex.Match(code_string, @"[A-Z]\d{2}.\d:$").Value.Trim(':');
                            }

                            else if (Regex.Match(s1, @"^[A-Z]\d{2}.\d - \[generalization [A-Z]\d{2}").Success)
                            {
                                code_string = Regex.Match(s1, @"^[A-Z]\d{2}.\d - \[generalization [A-Z]\d{2}:").Value.Trim();
                                code = Regex.Match(code_string, @"[A-Z]\d{2}:$").Value.Trim(':');
                            }

                            code_system = "ICD 10";
                            s1 = s1.Substring(code_string.Length).Trim(']').Trim();
                        }

                        else if (Regex.Match(s1, @"^[A-Z]\d{2}(.\d)? ").Success)
                        {
                            code = Regex.Match(s1, @"^[A-Z]\d{2}(.\d)? ").Value.Trim();
                            code_system = "ICD 10";
                            s1 = s1.Substring(code.Length).Trim();
                        }

                        else if (Regex.Match(s1, @"^[A-Z]\d{2}-[A-Z]\d{2} ").Success)
                        {
                            code = Regex.Match(s1, @"^[A-Z]\d{2}-[A-Z]\d{2} ").Value.Trim();
                            code_system = "ICD 10";
                            s1 = s1.Substring(code.Length).Trim();
                        }

                        else if (Regex.Match(s1, @"^[A-Z]\d{2} - [A-Z]\d{2} ").Success)
                        {
                            code = Regex.Match(s1, @"^[A-Z]\d{2} - [A-Z]\d{2} ").Value.Trim();
                            code_system = "ICD 10";
                            s1 = s1.Substring(code.Length).Trim();
                        }

                        else if (Regex.Match(s1, @"^[A-Z]\d{3} ").Success)
                        {
                            code = Regex.Match(s1, @"^[A-Z]\d{3} ").Value.Trim();
                            code_system = "ICD 10";
                            s1 = s1.Substring(code.Length).Trim();
                        }

                        char[] chars_to_lose2 = { ' ', '-', ',' };
                        s1 = s1.Trim(chars_to_lose2);

                        // check not duplicated.

                        bool add_condition = true;
                        if (conditions.Count > 0)
                        {
                            foreach (WhoCondition sc in conditions)
                            {
                                if (s1.ToLower() == sc.condition?.ToLower())
                                {
                                    add_condition = false;
                                    break;
                                }
                            }
                        }

                        // check not a too broad (range) ICD10 classification.

                        if (Regex.Match(s1, @"^[A-Z]\d{2}-[A-Z]\d{2}$").Success)
                        {
                            add_condition = false;
                        }

                        if (add_condition)
                        {
                            conditions.Add(code == "" ? new WhoCondition(s1) 
                                                      : new WhoCondition(s1, code, code_system));
                        }
                    }
                }
            }
        }
        return conditions;
    }


    public List<Secondary_Id> SplitAndAddIds(List<Secondary_Id> existing_ids, string sd_sid,
                                         string in_string, string source_field)
    {
        // in_string already known to be non-null, non-empty.

        List<string> ids = in_string.Split(";").ToList();
        foreach (string s in ids)
        {
            char[] chars_to_lose = { ' ', '\'', '‘', '’', ';' };
            string secid = s.Trim(chars_to_lose);
            if (secid.Length >= 3 && secid != sd_sid)
            {
                string secid_low = secid.ToLower();
                if (Regex.Match(secid_low, @"\d").Success   // has to include at least 1 number
                    && !(secid_low.StartsWith("none"))
                    && !(secid_low.StartsWith("nil"))
                    && !(secid_low.StartsWith("not "))
                    && !(secid_low.StartsWith("date"))
                    && !(secid_low.StartsWith("version"))
                    && !(secid_low.StartsWith("??")))
                {
                    SecIdBase sec_id_base = GetSecondIdDetails(secid, sd_sid);
                    
                    // Has this id been added before?
                        
                    bool add_id = true;
                    if (existing_ids.Count > 0)
                    {
                        foreach (Secondary_Id sec_id in existing_ids)
                        {
                            if (sec_id_base.processed_id == sec_id.processed_id)
                            {
                                add_id = false;
                                break;
                            }
                        }
                    }
                    if (add_id)
                    {
                        existing_ids.Add(new Secondary_Id(source_field, secid,
                        sec_id_base.processed_id, sec_id_base.sec_id_source,
                        sec_id_base.sec_id_type_id, sec_id_base.sec_id_type));
                    }
                }
            }
        }

        return existing_ids;
    }
     

    public SecIdBase GetSecondIdDetails(string sec_id, string sd_sid)
    {
        string? interim_id, processed_id = null;
        int? sec_id_source = null;
        int? sec_id_type_id = null;
        
        if (sec_id.Contains("NCT"))
        {
            interim_id = sec_id.Replace("NCT ", "NCT");
            interim_id = interim_id.Replace("NCTNumber", "");
            if (Regex.Match(interim_id, @"NCT[0-9]{8}").Success && 
            processed_id != "NCT11111111" && processed_id != "NCT99999999" 
            && processed_id !=  "NCT12345678" && processed_id != "NCT87654321")
            {
                processed_id = Regex.Match(interim_id, @"NCT[0-9]{8}").Value;
                sec_id_source = 100120;
                sec_id_type_id = 11;
            }
        }

        else if (Regex.Match(sec_id, @"[0-9]{4}-[0-9]{6}-[0-9]{2}").Success)
        {
            processed_id = Regex.Match(sec_id, @"[0-9]{4}-[0-9]{6}-[0-9]{2}").Value;
            sec_id_source = 100123;
            sec_id_type_id = 11;
        }

        else if (sec_id.Contains("ISRCTN"))
        {
            interim_id = sec_id.Replace("ISRCTN ", "ISRCTN");
            interim_id = interim_id.Replace("(ISRCTN)", "");
            interim_id = interim_id.Replace("ISRCTN(International", "");
            interim_id = interim_id.Replace("ISRCTN: ", "ISRCTN");
            interim_id = interim_id.Replace("ISRCTNISRCTN", "ISRCTN");
            
            if (Regex.Match(interim_id, @"ISRCTN[0-9]{8}").Success)
            {
                processed_id = Regex.Match(interim_id, @"ISRCTN[0-9]{8}").Value;
                sec_id_source = 100126;
                sec_id_type_id = 11;
            }
        }

        else if (Regex.Match(sec_id, @"ACTRN[0-9]{14}").Success)
        {
            processed_id = Regex.Match(sec_id, @"ACTRN[0-9]{14}").Value;
            sec_id_source = 100116;
            sec_id_type_id = 11;
        }

        else if (Regex.Match(sec_id, @"DRKS[0-9]{8}").Success)
        {
            processed_id = Regex.Match(sec_id, @"DRKS[0-9]{8}").Value;
            sec_id_source = 100124;
            sec_id_type_id = 11;
        }

        else if (Regex.Match(sec_id, @"CTRI/[0-9]{4}/[0-9]{2,3}/[0-9]{6}").Success)
        {
            processed_id = Regex.Match(sec_id, @"CTRI/[0-9]{4}/[0-9]{2,3}/[0-9]{6}").Value;
            processed_id = processed_id.Replace('/', '-');  // internal representation for CTRI
            sec_id_source = 100121;
            sec_id_type_id = 11;
        }

        else if (Regex.Match(sec_id, @"1111-[0-9]{4}-[0-9]{4}").Success)
        {
            processed_id = "U" + Regex.Match(sec_id, @"1111-[0-9]{4}-[0-9]{4}").Value;
            sec_id_source = 100115;
            sec_id_type_id = 11;
        }

        else if (Regex.Match(sec_id, @"UMIN[0-9]{9}").Success || Regex.Match(sec_id, @"UMIN-CTR[0-9]{9}").Success)
        {
            processed_id = "JPRN-UMIN" + Regex.Match(sec_id, @"[0-9]{9}").Value;
            sec_id_source = 100127;
            sec_id_type_id = 11;
        }

        else if (Regex.Match(sec_id, @"jRCTs[0-9]{9}").Success)
        {
            processed_id = "JPRN-jRCTs" + Regex.Match(sec_id, @"[0-9]{9}").Value;
            sec_id_source = 100127;
            sec_id_type_id = 11;
        }

        else if (Regex.Match(sec_id, @"jRCT[0-9]{10}").Success)
        {
            processed_id = "JPRN-jRCT" + Regex.Match(sec_id, @"[0-9]{10}").Value;
            sec_id_source = 100127;
            sec_id_type_id = 11;
        }

        else if (sec_id.StartsWith("JPRN"))
        {
            if (Regex.Match(sec_id, @"^[0-9]{8}$").Success)
            {
                processed_id = "JPRN-UMIN" + Regex.Match(sec_id, @"[0-9]{8}").Value;
                sec_id_source = 100127;
                sec_id_type_id = 11;
            }
            else
            {
                processed_id = sec_id;
                sec_id_source = 100127;
                sec_id_type_id = 11;
            }
        }
        
        else if (sec_id.StartsWith("RBR"))
        {
            sec_id_source = 100117;
            processed_id = sec_id;
            sec_id_type_id = 11;
        }
        
        else if (sec_id.StartsWith("ChiCTR"))
        {
            sec_id_source = 100118;
            processed_id = sec_id;
            sec_id_type_id = 11;
        }

        else if (sec_id.StartsWith("ChiMCTR"))
        {
            sec_id_source = 104545;   
            processed_id = sec_id;
            sec_id_type_id = 11;
        }

        else if (sec_id.StartsWith("KCT"))
        {
            sec_id_source = 100119;
            processed_id = sec_id;
            sec_id_type_id = 11;
        }
        
        else if (sec_id.StartsWith("RPCEC"))
        {
            sec_id_source = 100122;
            processed_id = sec_id;
            sec_id_type_id = 11;
        }
        
        else if (sec_id.StartsWith("DRKS"))
        {
            sec_id_source = 100124;
            processed_id = sec_id;
            sec_id_type_id = 11;
        }
        
        else if (sec_id.StartsWith("IRCT"))
        {
            sec_id_source = 100125;
            processed_id = sec_id;
            sec_id_type_id = 11;
        }
        
        else if (sec_id.StartsWith("PACTR"))
        {
            sec_id_source = 100128;
            processed_id = sec_id;
            sec_id_type_id = 11;
        }
        
        else if (sec_id.StartsWith("PER"))
        {
            sec_id_source = 100129;
            processed_id = sec_id;
            sec_id_type_id = 11;
        }
        
        else if (sec_id.StartsWith("SLCTR"))
        {
            sec_id_source = 100130;
            processed_id = sec_id;
            sec_id_type_id = 11;
        }
       
        else if (sec_id.StartsWith("TCTR"))
        {
            sec_id_source = 100131;
            processed_id = sec_id;
            sec_id_type_id = 11;
        }
        
        // Avoid Dutch CCMO numbers, which also start with NL, by regex tests
        
        else if (sec_id.StartsWith("NL") && Regex.Match(sec_id, @"^NL\d{1,4}$").Success)
        {
            sec_id_source = 100132;
            processed_id = sec_id;
            sec_id_type_id = 11;
        }
        
        else if (sec_id.StartsWith("NTR") && Regex.Match(sec_id, @"^NTR\d{1,4}$").Success)
        {
            sec_id_source = 100132;
            processed_id = sec_id;
            sec_id_type_id = 45;      // obsolete dutch registry id
        }
        
        else if (sec_id.StartsWith("LBCTR"))
        {
            sec_id_source = 101989;
            processed_id = sec_id;
            sec_id_type_id = 11;
        }

        if (sd_sid.StartsWith("RBR"))
        {
            // Extract Brazilian ethics Ids
            
            if (Regex.Match(sec_id, @"[0-9]{8}.[0-9].[0-9]{4}.[0-9]{4}").Success)
            {
                sec_id_source = 102000;  // Brazilian regulatory authority, ANVISA
                processed_id = Regex.Match(sec_id, @"[0-9]{8}.[0-9].[0-9]{4}.[0-9]{4}").Value;
                sec_id_type_id = 41;
            }

            if (Regex.Match(sec_id, @"[0-9].[0-9]{3}.[0-9]{3}").Success)
            {
                sec_id_source = 102001;  // Brazilian ethics committee approval number
                processed_id = Regex.Match(sec_id, @"[0-9].[0-9]{3}.[0-9]{3}").Value;
                sec_id_type_id = 12;
            }
        }

        string? sec_id_type = sec_id_type_id switch
        {
            11 => "Trial Registry ID",
            45 => "Obsolete NTR number",
            41 => "Regulatory Body ID",
            12 => "Ethics Review ID",
            _ => null
        };
       
        // Return the source / processed id process if discovery successful,
        // otherwise return the original secondary id without any source.
        
        return processed_id is not null 
            ? new SecIdBase(processed_id, sec_id_source, sec_id_type_id, sec_id_type) 
            : new SecIdBase(sec_id, null, null, null) ;
    }

    
    public string GetStatus(string study_status)
    {
        string status = study_status.ToLower();
        if (status is "complete" or "completed" 
            or "complete: follow-up complete" or "complete: follow up complete" 
            or "data analysis completed" or "main results already published")
        {
            return "Completed";
        }
        else if (status is "complete: follow-up continuing" 
                 or "complete: follow up continuing" or "active, not recruiting" 
                 or "closed to recruitment of participants" or "no longer recruiting" 
                 or "not recruiting" or "recruitment completed")
        {
            return "Active, not recruiting";
        }
        else if (status is "recruiting" or "open public recruiting" 
                 or "open to recruitment")
        {
            return "Recruiting";
        }
        else if (status.Contains("pending")
              || status == "not yet recruiting")
        {
            return "Not yet recruiting";
        }
        else if (status.Contains("suspended")
              || status.Contains("temporarily closed"))
        {
            return "Suspended";
        }
        else if (status.Contains("terminated")
              || status.Contains("stopped early"))
        {
            return "Terminated";
        }
        else if (status.Contains("withdrawn"))
        {
            return "Withdrawn";
        }
        else if (status.Contains("enrolling by invitation"))
        {
            return "Enrolling by invitation";
        }
        else
        {
            return "Other (" + study_status + ")";
        }
    }


    public List<WhoStudyFeature> AddIntStudyFeatures(List<WhoStudyFeature> study_features, string design_list)
    {
        string design = design_list.Replace(" :", ":").ToLower(); // to make comparisons easier

        if (design.Contains("purpose: treatment"))
        {
            study_features.Add(new WhoStudyFeature(21, "Primary purpose", 400, "Treatment"));
        }
        if (design.Contains("purpose: diagnosis")
            || design.Contains("diagnostic"))
        {
            study_features.Add(new WhoStudyFeature(21, "Primary purpose", 410, "Diagnostic"));
        }    
        if (design.Contains("supportive care")
            || design.Contains("purpose: supportive"))
        {
            study_features.Add(new WhoStudyFeature(21, "Primary purpose", 415, "Supportive care"));
        }


        if (design.Contains("non-randomized")
         || design.Contains("nonrandomized")
         || design.Contains("non-randomised")
         || design.Contains("nonrandomised")
         || design.Contains("non-rct"))
        {
            study_features.Add(new WhoStudyFeature(22, "Allocation type", 210, "Nonrandomised"));
        }
        else if ((design.Contains("randomized")
             || design.Contains("randomised")
             || design.Contains(" rct")))
        {
            study_features.Add(new WhoStudyFeature(22, "Allocation type", 205, "Randomised"));
        }


        if (design.Contains("parallel"))
        {
            study_features.Add(new WhoStudyFeature(23, "Intervention model", 305, "Parallel assignment"));
        }

        if (design.Contains("crossover"))
        {
            study_features.Add(new WhoStudyFeature(23, "Intervention model", 310, "Crossover assignment"));
        }

        if (design.Contains("factorial"))
        {
            study_features.Add(new WhoStudyFeature(23, "Intervention model", 315, "Factorial assignment"));
        }

        return study_features;

    }


    public List<WhoStudyFeature> AddObsStudyFeatures(List<WhoStudyFeature> study_features, string design_list)
    {
        string des_list = design_list.Replace(" :", ":").ToLower();  // to make comparisons easier
        if (des_list.Contains("observational study model"))
        {
            if (des_list.Contains("cohort"))
            {
                study_features.Add(new WhoStudyFeature(30, "Observational model", 600, "Cohort"));
            }
            if (des_list.Contains("case-control") || des_list.Contains("case control"))
            {
                study_features.Add(new WhoStudyFeature(30, "Observational model", 605, "Case-control"));
            }
            if (des_list.Contains("case-crossover") || des_list.Contains("case crossover"))
            {
                study_features.Add(new WhoStudyFeature(30, "Observational model", 615, "Case-crossover"));
            }

        }
        if (des_list.Contains("time perspective"))
        {
            if (des_list.Contains("retrospective"))
            {
                study_features.Add(new WhoStudyFeature(31, "Time perspective", 700, "Retrospective"));
            }
            if (des_list.Contains("prospective"))
            {
                study_features.Add(new WhoStudyFeature(31, "Time perspective", 705, "Prospective"));
            }
            if (des_list.Contains("cross-sectional") || des_list.Contains("crosssectional"))
            {
                study_features.Add(new WhoStudyFeature(31, "Time perspective", 710, "Cross-sectional"));
            }
            if (des_list.Contains("longitudinal"))
            {
                study_features.Add(new WhoStudyFeature(31, "Time perspective", 730, "longitudinal"));
            }
        }


        if (des_list.Contains("biospecimen retention"))
        {
            if (des_list.Contains("not collect nor archive"))
            {
                study_features.Add(new WhoStudyFeature(32, "Biospecimens retained", 800, "None retained"));
            }
            if (des_list.Contains("collect & archive- sample with dns"))
            {
                study_features.Add(new WhoStudyFeature(32, "Biospecimens retained", 805, "Samples with DNA"));
            }
        }
        return study_features;
    }


    public List<WhoStudyFeature> AddMaskingFeatures(List<WhoStudyFeature> study_features, string design_list)
    {
        string design = design_list.Replace(" :", ":").ToLower(); // to make comparisons easier

        if (design.Contains("open label")
           || design.Contains("open-label")
           || design.Contains("no mask")
           || design.Contains("masking not used")
           || design.Contains("not blinded")
           || design.Contains("non-blinded")
           || design.Contains("no blinding")
           || design.Contains("no masking")
           || design.Contains("masking: none")
           || design.Contains("masking: open")
           || design.Contains("blinding: open")
           )
        {
            study_features.Add(new WhoStudyFeature(24, "Masking", 500, "None (Open Label)"));
        }
        else if (design.Contains("single blind")
         || design.Contains("single-blind")
         || design.Contains("single - blind")
         || design.Contains("masking: single")
         || design.Contains("outcome assessor blinded")
         || design.Contains("participant blinded")
         || design.Contains("investigator blinded")
         || design.Contains("blinded (patient/subject)")
         || design.Contains("blinded (investigator/therapist)")
         || design.Contains("blinded (assessor)")
         || design.Contains("blinded (data analyst)")
         || design.Contains("uni-blind")
         )
        {
            study_features.Add(new WhoStudyFeature(24, "Masking", 505, "Single"));
        }
        else if (design.Contains("double blind")
         || design.Contains("double-blind")
         || design.Contains("doble-blind")
         || design.Contains("double - blind")
         || design.Contains("double-masked")
         || design.Contains("masking: double")
         || design.Contains("blinded (assessor, data analyst)")
         || design.Contains("blinded (patient/subject, investigator/therapist")
         || design.Contains("masking:participant, investigator, outcome assessor")
         || design.Contains("participant and investigator blinded")
         )
        {
            study_features.Add(new WhoStudyFeature(24, "Masking", 510, "Double"));
        }
        else if (design.Contains("triple blind")
         || design.Contains("triple-blind")
         || design.Contains("blinded (patient/subject, caregiver, investigator/therapist, assessor")
         || design.Contains("masking:participant, investigator, outcome assessor")
         )
        {
            study_features.Add(new WhoStudyFeature(24, "Masking", 515, "Triple"));
        }
        else if (design.Contains("quadruple blind")
         || design.Contains("quadruple-blind")
         )
        {
            study_features.Add(new WhoStudyFeature(24, "Masking", 520, "Quadruple"));
        }
        else if (design.Contains("masking used") || design.Contains("blinding used"))
        {
            study_features.Add(new WhoStudyFeature(24, "Masking", 502, "Blinded (no details)"));
        }
        else if (design.Contains("masking:not applicable")
         || design.Contains("blinding:not applicable")
         || design.Contains("masking not applicable")
         || design.Contains("blinding not applicable")
         )
        {
            study_features.Add(new WhoStudyFeature(24, "Masking", 599, "Not applicable"));
        }
        else if (design.Contains("masking: unknown"))
        {
            study_features.Add(new WhoStudyFeature(24, "Masking", 525, "Not provided"));
        }

        return study_features;
    }


    public List<WhoStudyFeature> AddPhaseFeatures(List<WhoStudyFeature> study_features, string phase_list)
    {
        string phase = phase_list.ToLower();
        if (phase != "not selected" && phase != "not applicable"
            && phase != "na" && phase != "n/a")
        {
            if (phase is "phase 0" or "phase-0" or "phase0" 
                or "0" or "0 (exploratory trials)" 
                or "phase 0 (exploratory trials)" or "0 (exploratory trials)")
            {
                study_features.Add(new WhoStudyFeature(20, "Phase", 105, "Early phase 1"));
            }
            else if (phase is "1" or "i" or "i (phase i study)" 
                     or "phase-1" or "phase 1" or "phase i" or "phase1")
            {
                study_features.Add(new WhoStudyFeature(20, "phase", 110, "Phase 1"));
            }
            else if (phase is "1-2" or "1 to 2" or "i-ii" 
                     or "i+ii (phase i+phase ii)" or "phase 1-2" 
                     or "phase 1 / phase 2" or "phase 1/ phase 2" 
                     or "phase 1/phase 2" or "phase i,ii" or "phase1/phase2")
            {
                study_features.Add(new WhoStudyFeature(20, "Phase", 115, "Phase 1/Phase 2"));
            }
            else if (phase is "2" or "2a" or "2b" 
                     or "ii" or "ii (phase ii study)" or "iia" 
                     or "iib" or "phase-2" or "phase 2" or "phase ii" or "phase2")
            {
                study_features.Add(new WhoStudyFeature(20, "Phase", 120, "Phase 2"));
            }
            else if (phase is "2-3" or "ii-iii" or "phase 2-3" 
                     or "phase 2 / phase 3" or "phase 2/ phase 3" 
                     or "phase 2/phase 3" or "phase2/phase3" or "phase ii,iii")
            {
                study_features.Add(new WhoStudyFeature(20, "Phase", 125, "Phase 2/Phase 3"));
            }
            else if (phase is "3" or "iii" or "iii (phase iii study)" 
                     or "iiia" or "iiib" or "3-4" or "phase-3" 
                     or "phase 3" or "phase 3 / phase 4" 
                     or "phase 3/ phase 4" or "phase3" or "phase iii")
            {
                study_features.Add(new WhoStudyFeature(20, "Phase", 130, "Phase 3"));
            }
            else if (phase is "4" or "iv" or "iv (phase iv study)" 
                     or "phase-4" or "phase 4" or "post-market" 
                     or "post marketing surveillance" or "phase4" or "phase iv")
            {
                study_features.Add(new WhoStudyFeature(20, "Phase", 135, "Phase 4"));
            }
            else
            {
                study_features.Add(new WhoStudyFeature(20, "Phase", 1500, phase_list));
            }
        }

        return study_features;
    }


    public int get_reg_source(string trial_id)
    {
        if (string.IsNullOrEmpty(trial_id))
        {
            return 0;
        }
        else
        {
            string tid = trial_id.ToUpper();
            return tid switch
            {
                _ when tid.StartsWith("NCT") => 100120,
                _ when tid.StartsWith("EUCTR") => 100123,
                _ when tid.StartsWith("JPRN") => 100127,
                _ when tid.StartsWith("ACTRN") => 100116,
                _ when tid.StartsWith("RBR") => 100117,
                _ when tid.StartsWith("CHICTR") => 100118,
                _ when tid.StartsWith("KCT") => 100119,
                _ when tid.StartsWith("CTRI") => 100121,
                _ when tid.StartsWith("RPCEC") => 100122,
                _ when tid.StartsWith("DRKS") => 100124,
                _ when tid.StartsWith("IRCT") => 100125,
                _ when tid.StartsWith("ISRCTN") => 100126,
                _ when tid.StartsWith("PACTR") => 100128,
                _ when tid.StartsWith("PER") => 100129,
                _ when tid.StartsWith("SLCTR") => 100130,
                _ when tid.StartsWith("TCTR") => 100131,
                _ when tid.StartsWith("NL") || tid.StartsWith("NTR") => 100132,
                _ when tid.StartsWith("LBCTR") => 101989,
                _ when tid.StartsWith("ITMCTR") => 109108,
                _ => 0
            };
        }
    }


    public string get_db(int source_id)
    {
        return source_id switch
        {
            100116 => "anzctr",
            100117 => "rebec",
            100118 => "chictr",
            100119 => "cris",
            100121 => "ctri",
            100122 => "rpcec",
            100124 => "drks",
            100125 => "irct",
            100127 => "jprn",
            100128 => "pactr",
            100129 => "rpuec",
            100130 => "slctr",
            100131 => "thctr",
            100132 => "nntr",
            101989 => "lebctr",
            109108 => "itmctr",
            _ => ""
        };
    }
}
