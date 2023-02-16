using MDR_Downloader.Helpers;
using System.Text.RegularExpressions;

namespace MDR_Downloader.who
{
    public class WHO_Processor
    {
        private readonly WHOHelpers _wh;

        public WHO_Processor()
        {
            _wh = new WHOHelpers();
        }

        public WHORecord? ProcessStudyDetails(WHO_SourceRecord sr)
        {
            WHORecord r = new WHORecord();

            string sd_sid = sr.TrialID.Replace("/", "-").Replace(@"\", "-").Replace(".", "-").Trim();
            r.sd_sid = sd_sid;
            int source_id = _wh.get_reg_source(sd_sid);

            if (source_id == 100120 || source_id == 100123 || source_id == 100126)
            {
                // no need to process these - details input directly from registry
                // (for CGT, ISRCTN, EU CTR).

                return null;
            }
            if (sd_sid == "null")
            {
                // Seems to happen with one Dutch trial.

                return null;
            }

            // otherwise proceed.

            r.source_id = source_id;
            r.record_date = sr.last_updated.AsISODate();

            r.public_title = sr.public_title.Tidy().ReplaceUnicodes();
            r.scientific_title = sr.Scientific_title.Tidy().ReplaceUnicodes();
            r.remote_url = sr.url.Tidy();

            r.public_contact_givenname = sr.Public_Contact_Firstname.Tidy();
            r.public_contact_familyname = sr.Public_Contact_Lastname.Tidy();
            r.public_contact_affiliation = sr.Public_Contact_Affiliation.Tidy();
            r.public_contact_email = sr.Public_Contact_Email.Tidy();
            r.scientific_contact_givenname = sr.Scientific_Contact_Firstname.Tidy();
            r.scientific_contact_familyname = sr.Scientific_Contact_Lastname.Tidy();
            r.scientific_contact_affiliation = sr.Scientific_Contact_Affiliation.Tidy();
            r.scientific_contact_email = sr.Scientific_Contact_Email.Tidy();

            r.date_registration = sr.Date_registration.AsISODate();
            r.date_enrolment = sr.Date_enrollement.AsISODate();

            r.target_size = sr.Target_size.Tidy();
            r.primary_sponsor = sr.Primary_sponsor.Tidy();
            r.secondary_sponsors = sr.Secondary_sponsors.Tidy();
            r.source_support = sr.Source_Support.Tidy();

            List<Secondary_Id> secondary_ids = new List<Secondary_Id>();

            string? sec_ids = sr.SecondaryIDs.Tidy();
            if (!string.IsNullOrEmpty(sec_ids))
            {
                _wh.SplitAndAddIds(secondary_ids, sd_sid, sec_ids, "secondary ids");
            }

            string? study_type = sr.study_type.Tidy();
            if (study_type is not null)
            {
                string stype = study_type.ToLower();
                if (stype.StartsWith("intervention"))
                {
                    r.study_type = "Interventional";
                }
                else if (stype.StartsWith("observation")
                      || stype.StartsWith("epidem"))
                {
                    r.study_type = "Observational";
                }
                else
                {
                    r.study_type = "Other (" + r.study_type + ")";
                }
            }
           
            string? study_status = sr.Recruitment_status.Tidy();
            if (study_status is not null && study_status.Length > 5)
            {
                r.study_status = _wh.GetStatus(study_status);
            }

            List<WhoStudyFeature> study_features = new();

            string? design_list = sr.study_design.Tidy();
            string? phase_statement= sr.phase.Tidy();

            r.design_string = design_list;
            r.phase_string = phase_statement;

            if (design_list is not null)
            {
                if (r.study_type == "Observational")
                {
                    study_features = _wh.AddObsStudyFeatures(study_features, design_list);
                }
                else
                {               
                    study_features = _wh.AddIntStudyFeatures(study_features, design_list);
                    study_features = _wh.AddMaskingFeatures(study_features, design_list);
                }
            }

            if (phase_statement is not null)
            {
                 study_features = _wh.AddPhaseFeatures(study_features, phase_statement);
            }

            string? countries = sr.Countries.Tidy();
            if (!string.IsNullOrEmpty(countries))
            {
                r.country_list = _wh.split_and_dedup_countries(countries);
            }

            if (sr.Conditions is not null)
            {
                r.condition_list = _wh.GetConditions(sd_sid, sr.Conditions);
            }

            r.interventions = sr.Interventions.Tidy();

            string? agemin = sr.Age_min.Tidy();
            if(agemin is not null)
            {
                if (Regex.Match(agemin, @"\d+").Success)
                {
                    r.agemin = Regex.Match(agemin, @"\d+").Value;
                    r.agemin_units = agemin.GetTimeUnits();
                }
            }

            string? agemax = sr.Age_max.Tidy();
            if (agemax is not null)
            {
                if (Regex.Match(agemax, @"\d+").Success)
                {
                    r.agemax = Regex.Match(agemax, @"\d+").Value;
                    r.agemax_units = agemax.GetTimeUnits();
                }
            }

            string? gender = sr.Gender.Tidy();
            if (gender is not null)
            {
                string gen = gender.ToLower();
                if (gen.Contains("both"))
                {
                    r.gender = "Both";
                }
                else
                {
                    string gender_string = "";
                    bool F = gen.Contains("female") || gen.Contains("women") || gen == "f";
                    string gen2 = F ? gen.Replace("female", "").Replace("women", "") : gen;
                    bool M = gen2.Contains("male") || gen.Contains("men") || gen == "m";
                    
                    if (M && F)
                    {
                        gender_string = "Both";
                    }
                    else
                    {
                        if (M) gender_string = "Male";
                        if (F) gender_string = "Female";
                    }

                    if (gender == "-")
                    {
                        gender_string = "Not provided";
                    }

                    if (gender_string == "")
                    {
                        // still no match...
                        gender_string = "?? Unable to classify (" + gender + ")";
                    }
                    r.gender = gender_string;
                }
            }
            else
            {
                r.gender = "Not provided";
            }

            char[] trim_chars = { ':', ',', ' ' };
            string? inc_crit = sr.Inclusion_Criteria.Tidy();
            if (inc_crit is not null && inc_crit.ToLower().StartsWith("inclusion criteria"))
            {
                inc_crit = inc_crit[18..].Trim(trim_chars);
            }
            inc_crit = inc_crit.ReplaceUnicodes().ReplaceHtmlTags();
            r.inclusion_criteria = inc_crit;

            string? exc_crit = sr.Exclusion_Criteria.Tidy();
            if (exc_crit is not null && exc_crit.ToLower().StartsWith("exclusion criteria"))
            {
                exc_crit = exc_crit[18..].Trim(trim_chars);
            }               
            exc_crit = exc_crit.ReplaceUnicodes().ReplaceHtmlTags();
            r.exclusion_criteria = exc_crit;

            r.primary_outcome = sr.Primary_Outcome.Tidy().ReplaceUnicodes().ReplaceHtmlTags();
            r.secondary_outcomes = sr.Secondary_Outcomes.Tidy().ReplaceUnicodes().ReplaceHtmlTags();

            r.bridging_flag = sr.Bridging_flag.Tidy();
            if (!string.IsNullOrEmpty(r.bridging_flag) && r.bridging_flag != r.sd_sid)
            {
                _wh.SplitAndAddIds(secondary_ids, r.sd_sid, r.bridging_flag, "bridging flag");
            }

            r.bridged_type = sr.Bridged_type.Tidy();

            r.childs = sr.Childs.Tidy();
            if (!string.IsNullOrEmpty(r.childs))
            {
                _wh.SplitAndAddIds(secondary_ids, r.sd_sid, r.childs, "bridged child recs");
            }

            r.type_enrolment = sr.type_enrolment.Tidy();
            r.retrospective_flag = sr.Retrospective_flag.Tidy();

            r.results_yes_no = sr.results_yes_no.Tidy();
            r.results_actual_enrollment = sr.results_actual_enrollment.Tidy();
            r.results_url_link = sr.results_url_link.Tidy();
            r.results_summary = sr.results_summary.Tidy();
            r.results_date_posted = sr.results_date_posted.AsISODate();
            r.results_date_first_publication = sr.results_date_first_publication.AsISODate();
            r.results_url_protocol = sr.results_url_protocol.Tidy();
            r.results_date_completed = sr.results_date_completed.AsISODate();

            string? ipd_plan = sr.results_IPD_plan.Tidy();

            if (ipd_plan is not null && ipd_plan.Length > 10)
            {
                if (ipd_plan.ToLower() != "not available" && ipd_plan.ToLower() != "not avavilable"
                && ipd_plan.ToLower() != "not applicable" && !ipd_plan.ToLower().StartsWith("justification or reason for"))
                {
                    r.ipd_plan = ipd_plan.ReplaceUnicodes().ReplaceHtmlTags();
                }
            }

            string? ipd_description = sr.results_IPD_description.Tidy();
            if (ipd_description is not null && ipd_description.Length > 10)
            {
                if (ipd_description.ToLower() != "not available" && ipd_description.ToLower() != "not avavilable"
                && ipd_description.ToLower() != "not applicable" && !ipd_description.ToLower().StartsWith("justification or reason for"))
                {
                    r.ipd_description = ipd_description.ReplaceUnicodes().ReplaceHtmlTags();
                }
            }

            r.folder_name = _wh.get_folder(source_id);
            r.secondary_ids = secondary_ids;
            r.study_features = study_features;

            return r;
        }
    }
}



