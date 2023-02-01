using HtmlAgilityPack;
using MDR_Downloader.Helpers;
using ScrapySharp.Extensions;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System.Web;

namespace MDR_Downloader.euctr;

public class EUCTR_Processor
{
    public int GetListLength(WebPage homePage)
    {
        // gets the numbers of records found for the current search.

        HtmlNode? total_link = homePage.Find("a", By.Id("ui-id-1")).FirstOrDefault();
        string? results = total_link?.TrimmedContents();
        if (results is null)
        {
            // log problem
            return 0;
        }
        
        int left_bracket_pos = results.IndexOf("(", StringComparison.Ordinal);
        int right_bracket_pos = results.IndexOf(")", StringComparison.Ordinal);

        string results_value = results[(left_bracket_pos + 1)..right_bracket_pos];
        results_value = results_value.Replace(",", "");

        return int.TryParse(results_value, out int result_count) ? result_count : 0;
    }


    public List<Study_Summary>? GetStudyList(WebPage homePage)
    {
        HtmlNode? pageContent = homePage.Find("div", By.Class("results")).FirstOrDefault();
        if (pageContent is null)
        {
            // log problem
            return null;
        }
        List<HtmlNode> studyBoxes = pageContent.CssSelect(".result").ToList();
        if (studyBoxes.Count == 0)
        {
            // log problem
            return null;
        }

        List<Study_Summary> summaries = new();
        foreach (HtmlNode box in studyBoxes)
        {
            // Ids and start date - three td elements in a fixed order in first row.
            
            HtmlNode? studyDetails = box.Elements("tr").FirstOrDefault();
            if (studyDetails is not null)
            {
                HtmlNode[] idDetails = studyDetails.CssSelect("td").ToArray();
                if (idDetails.Any())
                {
                    string euctr_id = idDetails[0].InnerValue()!;
                    summaries.Add(new Study_Summary(euctr_id, box));
                }
            }
        }
        return summaries;
    }


    public Euctr_Record GetInfoFromSummary(Study_Summary s)
    {
        // Study summary contains the html node with the summary details.

        string sd_sid = s.eudract_id;
        Euctr_Record st = new(sd_sid);

        HtmlNode box = s.details_box!;
        HtmlNode[] studyDetails = box.Elements("tr").ToArray();
        if (studyDetails.Length < 8)
        {
            // log problem
            return st;   // empty record
        }
        
        // Sponsor's id and the start date in the top row.

        HtmlNode[] idDetails = studyDetails[0].CssSelect("td").ToArray();
        if (idDetails.Length >= 3)
        {
            st.sponsor_id = idDetails[1].InnerValue();
            st.start_date = idDetails[2].InnerValue();
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
            List<Country> countries = new List<Country>();

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
                                countries.Add(new Country(country_name, study_status));
                                status_num++;
                                if (status_num == 1)
                                {
                                    st.trial_status = study_status;
                                }
                            }
                            else
                            {
                                countries.Add(new Country(country_name, null));
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
                        countries.Add(new Country(country_name, ""));
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


    public Euctr_Record ExtractProtocolDetails(Euctr_Record st, WebPage detailsPage)
    {
        var summary = detailsPage.Find("table", By.Class("summary")).FirstOrDefault();

        // if no summary probably missing page - seems to occur for one record.

        if (summary == null) return st;

        // Get the date added to system from the 6th row of the summary table.

        HtmlNode[] summary_rows = summary.CssSelect("tbody tr").ToArray();
        if (summary_rows.Any())
        {
            foreach (HtmlNode row in summary_rows)
            {
                var cells = row.CssSelect("td").ToArray();

                if (cells[0].InnerText.StartsWith("Date on "))
                {
                    st.entered_in_db = cells[1].InnerText;
                    break;
                }
            }
        }

        HtmlNode? identifiers = detailsPage.Find("table", By.Id("section-a")).FirstOrDefault();
        IEnumerable<HtmlNode>? identifier_rows = identifiers?.CssSelect("tbody tr");
        if (identifier_rows is not null)
        {
            st.identifiers = GetStudyIdentifiers(identifier_rows);
        }

        HtmlNode? sponsor = detailsPage.Find("table", By.Id("section-b")).FirstOrDefault();
        IEnumerable<HtmlNode>? sponsor_rows = sponsor?.CssSelect("tbody tr");
        if (sponsor_rows is not null)
        {
            st.sponsors = GetStudySponsors(sponsor_rows);
        }

        HtmlNode? imp_details = detailsPage.Find("table", By.Id("section-d")).FirstOrDefault();
        IEnumerable<HtmlNode>? imp_tables = imp_details?.CssSelect("tbody");
        if (imp_tables is not null)
        {
            st.imps = GetStudyIMPs(imp_tables);
        }

        HtmlNode? study_details = detailsPage.Find("table", By.Id("section-e")).FirstOrDefault();
        IEnumerable<HtmlNode>? details_rows = study_details?.CssSelect("tbody tr");
        if (details_rows is not null)
        {
            st.features = GetStudyFeatures(details_rows, st, st.countries);
        }

        HtmlNode? population = detailsPage.Find("table", By.Id("section-f")).FirstOrDefault();
        IEnumerable<HtmlNode>? population_rows = population?.CssSelect("tbody tr");
        if (population_rows is not null)
        {
            st.population = GetStudyPopulation(population_rows);
        }

        return st;
    }


    public Euctr_Record ExtractResultDetails(Euctr_Record st, WebPage resultsPage)
    {
        var pdfLInk = resultsPage.Find("a", By.Id("downloadResultPdf")).FirstOrDefault();

        if (pdfLInk != null)
        {
            st.results_pdf_link = pdfLInk.Attributes["href"].Value;
        }

        HtmlNode? result_div = resultsPage.Find("div", By.Id("resultContent")).FirstOrDefault();
        var result_rows = result_div?.SelectNodes("table[1]/tr")?.ToArray();
        if (result_rows is not null)
        {
            foreach (var row in result_rows)
            {
                HtmlNode? first_cell = row.SelectSingleNode("td[1]");
                string? fc_content = first_cell.TrimmedContents();
                if (fc_content is not null)
                {
                    string? cell_content = first_cell.SelectSingleNode("following-sibling::td[1]").TrimmedContents();
                    if (fc_content == "Results version number")
                    {
                        st.results_version = cell_content;
                    }
                    else if (fc_content == "This version publication date")
                    {
                        st.results_revision_date = cell_content;
                    }
                    else if (fc_content == "First version publication date")
                    {
                        st.results_first_date = cell_content;
                    }
                    else if (fc_content == "Summary report(s)")
                    {
                        HtmlNode? following_cell = first_cell.SelectSingleNode("following-sibling::td[1]/a[1]");
                        if (following_cell is not null)
                        {
                            st.results_summary_link = following_cell.Attributes["href"].Value;
                            st.results_summary_name = following_cell.TrimmedContents();
                        }
                    } 
                }
            }
        }
        return st;
    }


    private List<DetailLine> GetStudyIdentifiers(IEnumerable<HtmlNode> identifier_rows)
    {
        List<DetailLine> study_identifiers = new List<DetailLine>();
        foreach (HtmlNode row in identifier_rows)
        {
            var row_class = row.Attributes["class"];
            if (row_class is { Value: "tricell" })
            {
                var cells = row.CssSelect("td").ToArray();
                string code = cells[0].InnerText;
                if (code != "A.6" && code != "A.7" && code != "A.8")
                {
                    DetailLine line = new DetailLine(code, HttpUtility.HtmlDecode(cells[1].InnerText));
                    List<item_value> values = new List<item_value>();
                    if (cells[2].CssSelect("table").Any())
                    {
                        HtmlNode? inner_table = cells[2].CssSelect("table").FirstOrDefault();
                        var inner_rows = inner_table?.CssSelect("tr").ToArray();
                        if (inner_rows?.Any() is true)
                        {
                            foreach (HtmlNode inner_row in inner_rows)
                            {
                                var inner_cell = inner_row.CssSelect("td").FirstOrDefault();
                                if (inner_cell is not null)
                                {
                                    string value = HttpUtility.HtmlDecode(inner_cell.InnerText).Trim();
                                    if (!string.IsNullOrEmpty(value)) values.Add(new item_value(value));
                                }
                            }
                        }
                    }
                    else
                    {
                        string value = HttpUtility.HtmlDecode(cells[2].InnerText).Trim();
                        if (!string.IsNullOrEmpty(value)) values.Add(new item_value(value));
                    }

                    if (values.Count > 0)
                    {
                        line.item_number = values.Count;
                        line.item_values = values;
                        study_identifiers.Add(line);
                    }
                }
            }
        }

        return study_identifiers;
    }


    private List<DetailLine> GetStudySponsors(IEnumerable<HtmlNode> sponsor_rows)
    {
        List<DetailLine> study_sponsors = new List<DetailLine>();
        foreach (HtmlNode row in sponsor_rows)
        {
            var row_class = row.Attributes["class"];
            if (row_class is not null && row_class.Value == "tricell")
            {
                var cells = row.CssSelect("td").ToArray();
                string code = cells[0].InnerText;
                if (!code.Contains("B.5"))
                {
                    DetailLine line = new DetailLine(code, HttpUtility.HtmlDecode(cells[1].InnerText));
                    List<item_value> values = new List<item_value>();
                    if (cells[2].CssSelect("table").Any())
                    {
                        HtmlNode? inner_table = cells[2].CssSelect("table").FirstOrDefault();
                        var inner_rows = inner_table?.CssSelect("tr").ToArray();
                        if (inner_rows?.Any() is true)
                        {
                            foreach (HtmlNode inner_row in inner_rows)
                            {
                                HtmlNode? inner_cell = inner_row.CssSelect("td").FirstOrDefault();
                                if (inner_cell is not null)
                                {
                                    string value = HttpUtility.HtmlDecode(inner_cell.InnerText).Trim();
                                    if (!string.IsNullOrEmpty(value)) values.Add(new item_value(value));
                                }
                            }
                        }
                    }
                    else
                    {
                        string value = HttpUtility.HtmlDecode(cells[2].InnerText).Trim();
                        if (!string.IsNullOrEmpty(value)) values.Add(new item_value(value));
                    }

                    if (values.Count > 0)
                    {
                        line.item_number = values.Count;
                        line.item_values = values;
                        study_sponsors.Add(line);
                    }
                }
            }
        }
        return study_sponsors;
    }


    private List<ImpLine> GetStudyIMPs(IEnumerable<HtmlNode> imp_tables)
    {
        List<ImpLine> study_imps = new List<ImpLine>();
        int imp_num = 0;
        foreach (HtmlNode tbody in imp_tables)
        {
            imp_num++;
            IEnumerable<HtmlNode>? imp_rows = tbody.CssSelect("tr");
            if (imp_rows is not null)
            {
                foreach (HtmlNode row in imp_rows)
                {
                    var row_class = row.Attributes["class"];
                    if (row_class is { Value: "tricell" })
                    {
                        var cells = row.CssSelect("td").ToArray();
                        string code = cells[0].InnerText;
                        
                        // just get various names (often duplicated).
                        
                        if (code is "D.2.1.1.1" or "D.3.1" or "D.3.8" or "D.3.9.1" or "D.3.9.3")
                        {
                            ImpLine line = new(imp_num, code, HttpUtility.HtmlDecode(cells[1].InnerText));
                            List<item_value> values = new List<item_value>();
                            if (cells[2].CssSelect("table").Any())
                            {
                                HtmlNode? inner_table = cells[2].CssSelect("table").FirstOrDefault();
                                var inner_rows = inner_table?.CssSelect("tr").ToArray();
                                if (inner_rows?.Any() is true)
                                {
                                    foreach (HtmlNode inner_row in inner_rows)
                                    {
                                        var inner_cell = inner_row.CssSelect("td").FirstOrDefault();
                                        if (inner_cell is not null)
                                        {
                                            string value = HttpUtility.HtmlDecode(inner_cell.InnerText).Trim();
                                            if (!string.IsNullOrEmpty(value)) values.Add(new item_value(value));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                string value = HttpUtility.HtmlDecode(cells[2].InnerText).Trim();
                                if (!string.IsNullOrEmpty(value)) values.Add(new item_value(value));
                            }

                            if (values.Count > 0)
                            {
                                line.item_number = values.Count;
                                line.item_values = values;
                                study_imps.Add(line);
                            }
                        }
                    }
                }
            }
        }
        return study_imps;
    }


    private List<DetailLine> GetStudyFeatures(IEnumerable<HtmlNode> details_rows, Euctr_Record st, List<Country>? summary_countries)
    {
        List<DetailLine> study_features = new List<DetailLine>();
        foreach (HtmlNode row in details_rows)
        {
            var row_class = row.Attributes["class"];
            if (row_class is { Value: "tricell" })
            {
                var cells = row.CssSelect("td").ToArray();
                string code = cells[0].InnerText;

                // condition under study and study objectives

                if (code.Contains("E.1.1") || code.Contains("E.2"))
                {
                    DetailLine line = new DetailLine(code, HttpUtility.HtmlDecode(cells[1].InnerText));
                    List<item_value> values = new List<item_value>();
                    if (cells[2].CssSelect("table").Any())
                    {
                        HtmlNode? inner_table = cells[2].CssSelect("table").FirstOrDefault();
                        var inner_rows = inner_table?.CssSelect("tr").ToArray();
                        if (inner_rows?.Any() is true)
                        {
                            foreach (HtmlNode inner_row in inner_rows)
                            {
                                HtmlNode? inner_cell = inner_row.CssSelect("td").FirstOrDefault();
                                if (inner_cell is not null)
                                {
                                    string value = HttpUtility.HtmlDecode(inner_cell.InnerText).Trim();
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        value = value.CompressSpaces()!;
                                        values.Add(new item_value(value));
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        string value = HttpUtility.HtmlDecode(cells[2].InnerText).Trim();
                        if (!string.IsNullOrEmpty(value))
                        {
                            value = value.CompressSpaces()!;
                            values.Add(new item_value(value.Replace("|", "\n")));
                        }
                    }

                    if (values.Count > 0)
                    {
                        line.item_number = values.Count;
                        line.item_values = values;
                        study_features.Add(line);
                    }
                }
                
                // E.3 and E.4 are inclusion - exclusion criteria
                
                if (code is "E.3" or "E.4")
                {
                    string? criteria = null;
                    if (cells[2].CssSelect("table").Any())
                    {
                        HtmlNode? inner_table = cells[2].CssSelect("table").FirstOrDefault();
                        var inner_rows = inner_table?.CssSelect("tr").ToArray();
                        if (inner_rows?.Any() is true)
                        {
                            HtmlNode inner_row = inner_rows[0];     // Just the English one required
                            {
                                HtmlNode? inner_cell = inner_row.CssSelect("td").FirstOrDefault();
                                if (inner_cell is not null)
                                {
                                    string? cell_html = inner_cell.InnerHtml.ReplaceHtmlTags();
                                    if (!string.IsNullOrEmpty(cell_html))
                                    {
                                        criteria = HttpUtility.HtmlDecode(cell_html).CompressSpaces();
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        string? cell_html = cells[2].InnerHtml.ReplaceHtmlTags();
                        if (!string.IsNullOrEmpty(cell_html))
                        {
                            criteria = HttpUtility.HtmlDecode(cell_html).CompressSpaces();
                        }
                    }

                    if (code == "E.3")
                    {
                        st.inclusion_criteria = criteria;
                    }
                    else
                    {
                        st.exclusion_criteria = criteria;
                    }

                }

                // study design features

                if (code.Contains("E.6") || code.Contains("E.7")
                    || code.Contains("E.8.1") || code.Contains("E.8.2"))
                {
                    DetailLine line = new DetailLine(code, HttpUtility.HtmlDecode(cells[1].InnerText));
                    List<item_value> values = new List<item_value>();
                    if (cells[2].CssSelect("table").Any())
                    {
                        HtmlNode? inner_table = cells[2].CssSelect("table").FirstOrDefault();
                        var inner_rows = inner_table?.CssSelect("tr").ToArray();
                        if (inner_rows?.Any() is true)
                        {
                            foreach (HtmlNode inner_row in inner_rows)
                            {
                                var inner_cell = inner_row.CssSelect("td").FirstOrDefault();
                                if (inner_cell is not null)
                                {
                                    string value = HttpUtility.HtmlDecode(inner_cell.InnerText).Trim();
                                    if (value.ToLower() == "yes") values.Add(new item_value(value));
                                }
                            }
                        }
                    }
                    else
                    {
                        string value = HttpUtility.HtmlDecode(cells[2].InnerText).Trim();
                        if (value.ToLower() == "yes") values.Add(new item_value(value));
                    }

                    if (values.Count > 0)
                    {
                        line.item_number = values.Count;
                        line.item_values = values;
                        study_features.Add(line);
                    }
                }

                if (code == "E.8.6.3")
                {
                    // May have a list of one or more countries in an internal table

                    HtmlNode? inner_table = cells[2].CssSelect("table").FirstOrDefault();
                    var inner_rows = inner_table?.CssSelect("tr").ToArray();
                    if (inner_rows?.Any() is true)
                    {
                        foreach (HtmlNode inner_row in inner_rows)
                        {
                            var inner_cell = inner_row.CssSelect("td").FirstOrDefault();
                            if (inner_cell is not null)
                            {
                                string value = HttpUtility.HtmlDecode(inner_cell.InnerText).Trim();
                                bool add_country = true;
                                if (summary_countries is not null)
                                {
                                    foreach (Country c in summary_countries)
                                    {
                                        if (c.name == value)
                                        {
                                            add_country = false;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    summary_countries = new List<Country>();
                                }

                                if (add_country)
                                {
                                    summary_countries.Add(new Country(value, null));
                                }
                            }
                        }
                    }
                }
            }
        }
        return study_features;
    }


    private List<DetailLine> GetStudyPopulation(IEnumerable<HtmlNode> population_rows)
    {
        List<DetailLine> study_population = new List<DetailLine>();
        foreach (HtmlNode row in population_rows)
        {
            var row_class = row.Attributes["class"];
            if (row_class is { Value: "tricell" })
            {
                var cells = row.CssSelect("td").ToArray();
                string code = cells[0].InnerText;
                if (code.Contains("F.1") || code.Contains("F.2"))
                {
                    DetailLine line = new DetailLine(code, HttpUtility.HtmlDecode(cells[1].InnerText));
                    List<item_value> values = new List<item_value>();
                    if (cells[2].CssSelect("table").Any())
                    {
                        HtmlNode? inner_table = cells[2].CssSelect("table").FirstOrDefault();
                        var inner_rows = inner_table?.CssSelect("tr").ToArray();
                        if (inner_rows?.Any() is true)
                        {
                            foreach (HtmlNode inner_row in inner_rows)
                            {
                                var inner_cell = inner_row.CssSelect("td").FirstOrDefault();
                                if (inner_cell is not null)
                                {
                                    string value = HttpUtility.HtmlDecode(inner_cell.InnerText).Trim();
                                    if (value.ToLower() == "yes") values.Add(new item_value(value));
                                }
                            }
                        }
                    }
                    else
                    {
                        string value = HttpUtility.HtmlDecode(cells[2].InnerText).Trim();
                        if (value.ToLower() == "yes") values.Add(new item_value(value));
                    }

                    if (values.Count > 0)
                    {
                        line.item_number = values.Count;
                        line.item_values = values;
                        study_population.Add(line);
                    }
                }
            }
        }
        return study_population;
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
