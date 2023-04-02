using HtmlAgilityPack;
using ScrapySharp.Extensions;

using MDR_Downloader.Helpers;

namespace MDR_Downloader.euctr;

public class EUCTR_Helper
{
    private readonly ILoggingHelper _loggingHelper;
    
    public EUCTR_Helper( ILoggingHelper loggingHelper)
    {
        _loggingHelper = loggingHelper;
    }

    public Euctr_Record? GetInfoFromSummaryBox(HtmlNode box)
    {
        // Study summary contains the html node with the summary details.

        HtmlNode[] studyDetails = box.Elements("tr").ToArray();
        if (studyDetails.Length < 8)
        {
            _loggingHelper.LogError("Unable to obtain expected number of rows in box");
            return null; // empty record
        }

        // Eudract number, sponsor's id and the start date in the top row.
        
        Euctr_Record st = new();
        HtmlNode[] idDetails = studyDetails[0].CssSelect("td").ToArray();
        if (idDetails.Length >= 3)
        {
            st.sd_sid = idDetails[0].InnerValue()!;
            st.sponsors_id = idDetails[1].InnerValue();
            st.start_date = idDetails[2].InnerValue();
        }
        
        if (string.IsNullOrEmpty(st.sd_sid))
        {
            _loggingHelper.LogError("Very odd - unable to get EUDRACT number!!!");
            return null; // empty record
        }

        // Sponsor name in second row - also extracted later from the details page.

        st.sponsor_name = studyDetails[1].InnerValue();
        if (st.sponsor_name is not null)
        {
            st.sponsor_name = st.sponsor_name.Replace("[...]", "");
        }

        // medical condition as a text description
        st.medical_condition = studyDetails[3].InnerValue();

        // Disease (MedDRA details) - five td elements in a fixed order, 
        // if they are there at all appear in a nested table, class = 'meddra',
        // which has at 2 least rows, the first with the headers (so row 0 can 
        // be ignored, with 5 columns (td elements) in each row.

        HtmlNode? meddraTable = studyDetails[4].CssSelect(".meddra").FirstOrDefault();
        if (meddraTable is not null)
        {
            List<MeddraTerm> meddra_terms = new List<MeddraTerm>();
            HtmlNode[] disDetails = meddraTable.Descendants("tr").ToArray();
            for (int k = 1; k < disDetails.Length; k++)
            {
                MeddraTerm stm = new MeddraTerm();
                HtmlNode[] meddraDetails = disDetails[k].Elements("td").ToArray();
                stm.version = meddraDetails[0].InnerText?.Trim();
                stm.soc_term = meddraDetails[1].InnerText?.Trim();
                stm.code = meddraDetails[2].InnerText?.Trim();
                stm.term = meddraDetails[3].InnerText?.Trim();
                stm.level = meddraDetails[4].InnerText?.Trim();
                meddra_terms.Add(stm);
            }

            st.meddra_terms = meddra_terms;
        }

        // population age and gender - 2 td elements in a fixed order.

        HtmlNode[] popDetails = studyDetails[5].CssSelect("td").ToArray();
        if (popDetails.Length >= 2)
        {
            st.population_age = popDetails[0].InnerValue();
            st.gender = popDetails[1].InnerValue();
        }

        // Protocol links 
        // These are often multiple and we need to consider the whole
        // list to get the countries involved.

        List<HtmlNode> links = studyDetails[6].CssSelect("a").ToList();
        List<HtmlNode> statuses = studyDetails[6].CssSelect("span").ToList();
        if (links.Count > 0)
        {
            char[] parentheses = { '(', ')' };
            List<EMACountry> countries = new List<EMACountry>();

            // Because of an additional initial 'Trial protocol:' span
            // there should normally be links + 1 span (status) numbers.
            // Status does not seem to be given for 'Outside EU/EEA'
            // though this is usually (but not always) seems to be at the end of the list.
            // If it is links and span numbers will be equal.
            // first valid status found used as overall trial status

            if (links.Count == statuses.Count - 1
                || (links.Count == statuses.Count
                    && studyDetails[6].InnerText.Contains("Outside EU/EEA")))
            {
                // get country names and study status
                // For GB status no longer available for ongoing studies
                // Status also generally not given for 'Outside EU/EEA'
                // though this always seems to be at the end of the list

                int status_diff = 1;
                for (int j = 0; j < links.Count; j++)
                {
                    int status_num = 0;
                    string country_code = links[j].InnerText;
                    if (country_code == "Outside EU/EEA")
                    {
                        // The inclusion of the Outside EU/EEA puts the 
                        // status list back into 'sync' with the country list.

                        status_diff = 0;
                    }
                    else
                    {
                        string country_name = GetCountryName(country_code);
                        if (country_name != "")
                        {
                            string study_status = statuses[j + status_diff].InnerText.Trim(parentheses);
                            if (study_status != "GB - no longer in EU/EEA")
                            {
                                countries.Add(new EMACountry(country_name, study_status));
                                status_num++;
                                if (status_num == 1)
                                {
                                    st.trial_status = study_status;
                                }
                            }
                            else
                            {
                                countries.Add(new EMACountry(country_name, null));
                            }
                        }
                    }
                }
            }
            else
            {
                // Just get the country names.

                foreach (var t in links)
                {
                    string country_code = t.InnerText;
                    string country_name = GetCountryName(country_code);
                    if (country_name != "")
                    {
                        countries.Add(new EMACountry(country_name, null));
                    }
                }
            }

            st.countries = countries;

            // Only the first listed country used to obtain the protocol details and overall trial status

            st.details_url = "https://www.clinicaltrialsregister.eu" + links[0].Attributes["href"].Value;
        }

        // Results link, if any.

        HtmlNode? resultLink = studyDetails[7].CssSelect("a").FirstOrDefault();

        if (resultLink is not null)
        {
            st.results_url = "https://www.clinicaltrialsregister.eu" + resultLink.Attributes["href"].Value;

            // if results link present and Status not completed make status "Completed"
            // (some entries may not have been updated)

            if (st.trial_status != "Completed") st.trial_status = "Completed";
        }

        return st;
    }


    private string GetCountryName(string country_code)
    {
        return country_code switch
        {
            "ES" => "Spain",
            "PT" => "Portugal",
            "IE" => "Ireland",
            "GB" => "United Kingdom",
            "FR" => "France",
            "BE" => "Belgium",
            "NL" => "Netherlands",
            "LU" => "Luxembourg",
            "DE" => "Germany",
            "LI" => "Liechtenstein",
            "IT" => "Italy",
            "DK" => "Denmark",
            "SE" => "Sweden",
            "NO" => "Norway",
            "FI" => "Finland",
            "IS" => "Iceland",
            "EE" => "Estonia",
            "LV" => "Latvia",
            "LT" => "Lithuania",
            "AT" => "Austria",
            "PL" => "Poland",
            "HU" => "Hungary",
            "RO" => "Romania",
            "BG" => "Bulgaria",
            "CZ" => "Czechia",
            "SK" => "Slovakia",
            "SI" => "Slovenia",
            "HR" => "Croatia",
            "GR" => "Greece",
            "CY" => "Cyprus",
            "MT" => "Malta",
            _ => "??"
        };
    }
}