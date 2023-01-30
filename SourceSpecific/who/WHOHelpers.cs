using MDR_Downloader.Helpers;
using System.Text.RegularExpressions;

namespace MDR_Downloader.who;

public class WHOHelpers
{
    public List<string>? split_string(string? instring)
    {
        string? string_list = instring.Tidy();
        if (string.IsNullOrEmpty(string_list))
        {
            return null;
        }
        else
        {
            return string_list.Split(";").ToList();
        }
    }


    public List<string>? split_and_dedup_countries(string countries)
    {
        // countries known to be non-null and already 'tidied'.

        List<string> outstrings = new List<string>();
        List<string> instrings = countries.Split(";").ToList();

        foreach (string s in instrings)
        {
            if (outstrings.Count == 0)
            {
                outstrings.Add(s);
            }
            else
            {
                bool add_string = true;
                foreach (string s2 in outstrings)
                {
                    if (s2 == s)
                    {
                        add_string = false;
                        break;
                    }
                }
                if (add_string) outstrings.Add(s);
            }
        }
        return outstrings;
    }


    public List<WhoCondition> GetConditions(string sd_sid, string instring)
    {
        List<WhoCondition> conditions = new List<WhoCondition>();
        if (!string.IsNullOrEmpty(instring))
        {
            string? condition_list = instring.Tidy();
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
                            if (code == "")
                            {
                                conditions.Add(new WhoCondition(s1));
                            }
                            else
                            {
                                conditions.Add(new WhoCondition(s1, code, code_system));

                            }
                        }
                    }
                }
            }
        }
        return conditions;
    }


    public List<Secondary_Id> SplitAndAddIds(List<Secondary_Id> existing_ids, string sd_sid,
                                         string instring, string source_field)
    {
        // instring already known to be non-null, non-empty.

        List<string> ids = instring.Split(";").ToList();
        foreach (string s in ids)
        {
            char[] chars_to_lose = { ' ', '\'', '‘', '’', ';' };
            string s1 = s.Trim(chars_to_lose);
            if (s1.Length >= 4 && s1 != sd_sid)
            {
                string s2 = s1.ToLower();
                if (Regex.Match(s2, @"\d").Success   // has to include at least 1 number
                    && !(s2.StartsWith("none"))
                    && !(s2.StartsWith("nil"))
                    && !(s2.StartsWith("not "))
                    && !(s2.StartsWith("date"))
                    && !(s2.StartsWith("version"))
                    && !(s2.StartsWith("??")))
                {
                    SecIdBase? sec_id_base = GetSeconIdDetails(s1, sd_sid);
                    if (sec_id_base is not null)
                    {
                        // has this id been added before?
                        bool add_id = true;
                        if (existing_ids.Count > 0)
                        {
                            foreach (Secondary_Id secid in existing_ids)
                            {
                                if (sec_id_base.processed_id == secid.processed_id)
                                {
                                    add_id = false;
                                    break;
                                }
                            }
                        }
                        if (add_id)
                        {
                            existing_ids.Add(new Secondary_Id(source_field, s1,
                            sec_id_base.processed_id, sec_id_base.sec_id_source));
                        }
                    }
                }
            }
        }

        return existing_ids;
    }
     

    public SecIdBase? GetSeconIdDetails(string sec_id, string sd_sid)
    {
        string? interim_id = "", processed_id = null;
        int? sec_id_source = null;

        if (sec_id.Contains("NCT"))
        {
            interim_id = sec_id.Replace("NCT ", "NCT");
            interim_id = interim_id.Replace("NCTNumber", "");
            if (Regex.Match(interim_id, @"NCT[0-9]{8}").Success)
            {
                processed_id = Regex.Match(interim_id, @"NCT[0-9]{8}").Value;
                sec_id_source = 100120;
            }
            if (processed_id == "NCT11111111" || processed_id == "NCT99999999"
                || processed_id == "NCT12345678" || processed_id == "NCT87654321")
            {
                // remove these 
                processed_id = null;
                sec_id_source = null;
            }
        }

        else if (Regex.Match(sec_id, @"[0-9]{4}-[0-9]{6}-[0-9]{2}").Success)
        {
            processed_id = Regex.Match(sec_id, @"[0-9]{4}-[0-9]{6}-[0-9]{2}").Value;
            sec_id_source = 100123;

            if (processed_id == "--------------")
            {
                // remove these 
                processed_id = null;
                sec_id_source = null;
            }
        }

        else if (sec_id.Contains("ISRCTN"))
        {
            interim_id = interim_id.Replace("(ISRCTN)", "");
            interim_id = interim_id.Replace("ISRCTN(International", "");
            interim_id = sec_id.Replace("ISRCTN ", "ISRCTN");
            interim_id = interim_id.Replace("ISRCTN: ", "ISRCTN");
            interim_id = interim_id.Replace("ISRCTNISRCTN", "ISRCTN");

            if (Regex.Match(interim_id, @"ISRCTN[0-9]{8}").Success)
            {
                processed_id = Regex.Match(interim_id, @"ISRCTN[0-9]{8}").Value;
                sec_id_source = 100126;
            }
        }

        else if (Regex.Match(sec_id, @"ACTRN[0-9]{14}").Success)
        {
            processed_id = Regex.Match(sec_id, @"ACTRN[0-9]{14}").Value;
            sec_id_source = 100116;
        }

        else if (Regex.Match(sec_id, @"DRKS[0-9]{8}").Success)
        {
            processed_id = Regex.Match(sec_id, @"DRKS[0-9]{8}").Value;
            sec_id_source = 100124;
        }

        else if (Regex.Match(sec_id, @"CTRI/[0-9]{4}/[0-9]{2,3}/[0-9]{6}").Success)
        {
            processed_id = Regex.Match(sec_id, @"CTRI/[0-9]{4}/[0-9]{2,3}/[0-9]{6}").Value;
            processed_id = processed_id.Replace('/', '-');  // internal representation for CTRI
            sec_id_source = 100121;
        }

        else if (Regex.Match(sec_id, @"1111-[0-9]{4}-[0-9]{4}").Success)
        {
            processed_id = "U" + Regex.Match(sec_id, @"1111-[0-9]{4}-[0-9]{4}").Value;
            sec_id_source = 100115;
        }

        else if (Regex.Match(sec_id, @"UMIN[0-9]{9}").Success || Regex.Match(sec_id, @"UMIN-CTR[0-9]{9}").Success)
        {
            processed_id = "JPRN-UMIN" + Regex.Match(sec_id, @"[0-9]{9}").Value;
            sec_id_source = 100127;
        }

        else if (Regex.Match(sec_id, @"jRCTs[0-9]{9}").Success)
        {
            processed_id = "JPRN-jRCTs" + Regex.Match(sec_id, @"[0-9]{9}").Value;
            sec_id_source = 100127;
        }

        else if (Regex.Match(sec_id, @"jRCT[0-9]{10}").Success)
        {
            processed_id = "JPRN-jRCT" + Regex.Match(sec_id, @"[0-9]{10}").Value;
            sec_id_source = 100127;
        }

        else if (sec_id.StartsWith("JPRN"))
        {
            if (Regex.Match(sec_id, @"^[0-9]{8}$").Success)
            {
                processed_id = "JPRN-UMIN" + Regex.Match(sec_id, @"[0-9]{8}").Value;
                sec_id_source = 100127;
            }
            else
            {
                processed_id = sec_id;
                sec_id_source = 100127;
            }
        }
        
        else if (sec_id.StartsWith("RBR"))
        {
            sec_id_source = 100117;
            processed_id = sec_id;
        }
        
        else if (sec_id.StartsWith("ChiCTR"))
        {
            sec_id_source = 100118;
            processed_id = sec_id;
        }

        else if (sec_id.StartsWith("ChiMCTR"))
        {
            sec_id_source = 104545;   
            processed_id = sec_id;
        }

        else if (sec_id.StartsWith("KCT"))
        {
            sec_id_source = 100119;
            processed_id = sec_id;
        }
        
        else if (sec_id.StartsWith("RPCEC"))
        {
            sec_id_source = 100122;
            processed_id = sec_id;
        }
        
        else if (sec_id.StartsWith("DRKS"))
        {
            sec_id_source = 100124;
            processed_id = sec_id;
        }
        
        else if (sec_id.StartsWith("IRCT"))
        {
            sec_id_source = 100125;
            processed_id = sec_id;
        }
        
        else if (sec_id.StartsWith("PACTR"))
        {
            sec_id_source = 100128;
            processed_id = sec_id;
        }
        
        else if (sec_id.StartsWith("PER"))
        {
            sec_id_source = 100129;
            processed_id = sec_id;
        }
        
        else if (sec_id.StartsWith("SLCTR"))
        {
            sec_id_source = 100130;
            processed_id = sec_id;
        }
       
        else if (sec_id.StartsWith("TCTR"))
        {
            sec_id_source = 100131;
            processed_id = sec_id;
        }
        
        else if (sec_id.StartsWith("NL") || sec_id.StartsWith("NTR"))
        {
            sec_id_source = 100132;
            processed_id = sec_id;
        }
        
        else if (sec_id.StartsWith("LBCTR"))
        {
            sec_id_source = 101989;
            processed_id = sec_id;
        }


        if (sd_sid.StartsWith("RBR"))
        {
            // Extract Brazilian ethics Ids
            if (Regex.Match(sec_id, @"[0-9]{8}.[0-9].[0-9]{4}.[0-9]{4}").Success)
            {
                processed_id = Regex.Match(sec_id, @"[0-9]{8}.[0-9].[0-9]{4}.[0-9]{4}").Value;
                sec_id_source = 102000;  // Brasilian regulatory authority, ANVISA
                // number is an ethics approval submission id
            }

            if (Regex.Match(sec_id, @"[0-9].[0-9]{3}.[0-9]{3}").Success)
            {
                processed_id = Regex.Match(sec_id, @"[0-9].[0-9]{3}.[0-9]{3}").Value;
                sec_id_source = 102001;  // Brasilian ethics committee approval number
            }
        }

        if (processed_id is not null)
        {
            return new SecIdBase(processed_id, sec_id_source);
        }
        else
        {
            return null;
        }
    }



    public string GetStatus(string study_status)
    {
        string status = study_status.ToLower();
        if (status == "complete"
              || status == "completed"
              || status == "complete: follow-up complete"
              || status == "complete: follow up complete"
              || status == "data analysis completed"
              || status == "main results already published")
        {
            return "Completed";
        }
        else if (status == "complete: follow-up continuing"
              || status == "complete: follow up continuing"
              || status == "active, not recruiting"
              || status == "closed to recruitment of participants"
              || status == "no longer recruiting"
              || status == "not recruiting"
              || status == "recruitment completed")
        {
            return "Active, not recruiting";
        }
        else if (status == "recruiting"
              || status == "open public recruiting"
              || status == "open to recruitment")
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
            if (phase == "phase 0"
             || phase == "phase-0"
             || phase == "phase0"
             || phase == "0"
             || phase == "0 (exploratory trials)"
             || phase == "phase 0 (exploratory trials)"
             || phase == "0 (exploratory trials)")
            {
                study_features.Add(new WhoStudyFeature(20, "Phase", 105, "Early phase 1"));
            }
            else if (phase == "1"
                  || phase == "i"
                  || phase == "i (phase i study)"
                  || phase == "phase-1"
                  || phase == "phase 1"
                  || phase == "phase i"
                  || phase == "phase1")
            {
                study_features.Add(new WhoStudyFeature(20, "phase", 110, "Phase 1"));
            }
            else if (phase == "1-2"
                  || phase == "1 to 2"
                  || phase == "i-ii"
                  || phase == "i+ii (phase i+phase ii)"
                  || phase == "phase 1-2"
                  || phase == "phase 1 / phase 2"
                  || phase == "phase 1/ phase 2"
                  || phase == "phase 1/phase 2"
                  || phase == "phase i,ii"
                  || phase == "phase1/phase2")
            {
                study_features.Add(new WhoStudyFeature(20, "Phase", 115, "Phase 1/Phase 2"));
            }
            else if (phase == "2"
                  || phase == "2a"
                  || phase == "2b"
                  || phase == "ii"
                  || phase == "ii (phase ii study)"
                  || phase == "iia"
                  || phase == "iib"
                  || phase == "phase-2"
                  || phase == "phase 2"
                  || phase == "phase ii"
                  || phase == "phase2")
            {
                study_features.Add(new WhoStudyFeature(20, "Phase", 120, "Phase 2"));
            }
            else if (phase == "2-3"
                 || phase == "ii-iii"
                 || phase == "phase 2-3"
                 || phase == "phase 2 / phase 3"
                 || phase == "phase 2/ phase 3"
                 || phase == "phase 2/phase 3"
                 || phase == "phase2/phase3"
                 || phase == "phase ii,iii")
            {
                study_features.Add(new WhoStudyFeature(20, "Phase", 125, "Phase 2/Phase 3"));
            }
            else if (phase == "3"
                  || phase == "iii"
                  || phase == "iii (phase iii study)"
                  || phase == "iiia"
                  || phase == "iiib"
                  || phase == "3-4"
                  || phase == "phase-3"
                  || phase == "phase 3"
                  || phase == "phase 3 / phase 4"
                  || phase == "phase 3/ phase 4"
                  || phase == "phase3"
                  || phase == "phase iii")
            {
                study_features.Add(new WhoStudyFeature(20, "Phase", 130, "Phase 3"));
            }
            else if (phase == "4"
                   || phase == "iv"
                   || phase == "iv (phase iv study)"
                   || phase == "phase-4"
                   || phase == "phase 4"
                   || phase == "post-market"
                   || phase == "post marketing surveillance"
                   || phase == "phase4"
                   || phase == "phase iv")
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
                string when tid.StartsWith("NCT") => 100120,
                string when tid.StartsWith("EUCTR") => 100123,
                string when tid.StartsWith("JPRN") => 100127,
                string when tid.StartsWith("ACTRN") => 100116,
                string when tid.StartsWith("RBR") => 100117,
                string when tid.StartsWith("CHICTR") => 100118,
                string when tid.StartsWith("KCT") => 100119,
                string when tid.StartsWith("CTRI") => 100121,
                string when tid.StartsWith("RPCEC") => 100122,
                string when tid.StartsWith("DRKS") => 100124,
                string when tid.StartsWith("IRCT") => 100125,
                string when tid.StartsWith("ISRCTN") => 100126,
                string when tid.StartsWith("PACTR") => 100128,
                string when tid.StartsWith("PER") => 100129,
                string when tid.StartsWith("SLCTR") => 100130,
                string when tid.StartsWith("TCTR") => 100131,
                string when tid.StartsWith("NL") || tid.StartsWith("NTR") => 100132,
                string when tid.StartsWith("LBCTR") => 101989,
                _ => 0
            };
        }
    }


    public string get_folder(int source_id)
    {
        return source_id switch
        {
            100116 => @"C:\MDR_Data\anzctr\",
            100117 => @"C:\MDR_Data\rebec\",
            100118 => @"C:\MDR_Data\chictr\",
            100119 => @"C:\MDR_Data\cris\",
            100121 => @"C:\MDR_Data\ctri\",
            100122 => @"C:\MDR_Data\rpcec\",
            100124 => @"C:\MDR_Data\drks\",
            100125 => @"C:\MDR_Data\irct\",
            100127 => @"C:\MDR_Data\jprn\",
            100128 => @"C:\MDR_Data\pactr\",
            100129 => @"C:\MDR_Data\rpuec\",
            100130 => @"C:\MDR_Data\slctr\",
            100131 => @"C:\MDR_Data\thctr\",
            100132 => @"C:\MDR_Data\nntr\",
            101989 => @"C:\MDR_Data\lebctr\",
            _ => ""
        };
    }
}
