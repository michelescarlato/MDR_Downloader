using HtmlAgilityPack;
using MDR_Downloader.Helpers;
using ScrapySharp.Extensions;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MDR_Downloader.euctr
{
    public class EUCTR_Processor
    {
        public int GetListLength(WebPage homePage)
        {
            // gets the numbers of records found for the current search
            HtmlNode? total_link = homePage.Find("a", By.Id("ui-id-1")).FirstOrDefault();
            if (total_link is not null)
            {
                string results = total_link.TrimmedContents();
                int left_bracket_pos = results.IndexOf("(");
                int right_bracket_pos = results.IndexOf(")");
                string results_value = results.Substring(left_bracket_pos + 1, right_bracket_pos - left_bracket_pos - 1);
                results_value = results_value.Replace(",", "");
                if (Int32.TryParse(results_value, out int result_count))
                {
                    return result_count;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public List<EUCTR_Summmary> GetStudySuummaries(WebPage homePage)
        {
            List<EUCTR_Summmary> summaries = new();
            
            var pageContent = homePage.Find("div", By.Class("results"));
            HtmlNode[] studyBoxes = pageContent.CssSelect(".result").ToArray();
            foreach (HtmlNode box in studyBoxes)
            {
                // Ids and start date - three td elements in a fixed order in first row.
                HtmlNode[] studyDetails = box.Elements("tr").ToArray();
                HtmlNode[] idDetails = studyDetails[0].CssSelect("td").ToArray();

                string euctr_id = idDetails[0].InnerValue();
                string sponsor_id = idDetails[1].InnerValue();
                string start_date = idDetails[2].InnerValue();

                EUCTR_Summmary summary = new (euctr_id, sponsor_id, start_date);

                // Get other details in the box on the search page.

                // sponsor name in second row - also extracted later from the details page
                summary.sponsor_name = studyDetails[1].InnerValue();
                if (summary.sponsor_name.Contains("[...]"))
                {
                    summary.sponsor_name = summary.sponsor_name.Replace("[...]", "");
                }

                // medical conditiona as a text description
                summary.medical_condition = studyDetails[3].InnerValue();

                // Disease (MedDRA details) - five td elements in a fixed order, 
                // if they are there at all appear in a nested table, class = 'meddra'.

                List<MeddraTerm> meddra_terms = new();
                HtmlNode? meddraTable = studyDetails[4].CssSelect(".meddra").FirstOrDefault();
                if (meddraTable is not null)
                {
                    // this table has at 2 least rows, the first with the headers (so row 0 can be ignored)
                    HtmlNode[] disDetails = meddraTable.Descendants("tr").ToArray();
                    for (int k = 1; k < disDetails.Length; k++)
                    {
                        MeddraTerm stm = new();
                        HtmlNode[] meddraDetails = disDetails[k].Elements("td").ToArray();
                        stm.version = meddraDetails[0].InnerText?.Trim() ?? "";
                        stm.soc_term = meddraDetails[1].InnerText?.Trim() ?? "";
                        stm.code = meddraDetails[2].InnerText?.Trim() ?? "";
                        stm.term = meddraDetails[3].InnerText?.Trim() ?? "";
                        stm.level = meddraDetails[4].InnerText?.Trim() ?? "";
                        meddra_terms.Add(stm);
                    }

                    summary.meddra_terms = meddra_terms;
                }

                // population age and gender - 2 td elements in a fixed order
                HtmlNode[] popDetails = studyDetails[5].CssSelect("td").ToArray();
                summary.population_age = popDetails[0].InnerValue();
                summary.gender = popDetails[1].InnerValue();

                // Protocol links 
                // These are often multiple but - for the time being at least
                // we take the first and use that to obtain further details
                HtmlNode? link = studyDetails[6].CssSelect("a").FirstOrDefault();
                if (link is not null)
                {
                    summary.details_url = "https://www.clinicaltrialsregister.eu" + link.Attributes["href"].Value;
                }

                HtmlNode? status_node = studyDetails[6].CssSelect("span.status").FirstOrDefault();
                if (status_node is not null)
                {
                    string status = status_node.InnerText ?? "";
                    // remove surrounding brackets
                    if (status != "" && status.StartsWith("(") && status.Length > 2)
                    {
                        status = status.Substring(1, status.Length - 2);
                    }
                    summary.trial_status = status;
                }

                // Results link, if any
                HtmlNode? resultLink = studyDetails[7].CssSelect("a").FirstOrDefault();
                if (resultLink is not null)
                {
                    summary.results_url = "https://www.clinicaltrialsregister.eu" + resultLink.Attributes["href"].Value;

                    // if results link present and Status not completed make status "Completed"
                    // (some entries may not have been updated)
                    if (summary.trial_status != "Completed") summary.trial_status = "Completed";
                }

                summaries.Add(summary);
            }

            return summaries;       
        }

  
        public EUCTR_Record ExtractProtocolDetails(EUCTR_Record st, WebPage detailsPage)
        {
            var summary = detailsPage.Find("table", By.Class("summary")).FirstOrDefault();

            // if no summary probably missing page - seems to occur for one record
            if (summary is null) return st;

            // get the date added to system from the 6th row of the summary table
            var summary_rows = summary.CssSelect("tbody tr").ToArray();
            if (summary_rows is not null)
            {
                foreach (HtmlNode row in summary_rows)
                {
                    var cells = row.CssSelect("td").ToArray();
                    if (cells[0].InnerText.Substring(0, 8) == "Date on ")
                    {
                        st.entered_in_db = cells[1].InnerText;
                        break;
                    }
                }
            }

            var identifiers = detailsPage.Find("table", By.Id("section-a")).FirstOrDefault();
            var identifier_rows = identifiers.CssSelect("tbody tr").ToArray();
            if (identifier_rows is not null)
            {
                st.identifiers = GetStudyIdentifiers(identifier_rows);
            }

            var sponsor = detailsPage.Find("table", By.Id("section-b")).FirstOrDefault();
            var sponsor_rows = sponsor.CssSelect("tbody tr").ToArray();
            if (sponsor_rows is not null)
            {
                st.sponsors = GetStudySponsors(sponsor_rows);
            }

            var imp_details = detailsPage.Find("table", By.Id("section-d")).FirstOrDefault();
            var imp_tables = imp_details.CssSelect("tbody").ToArray();
            if (imp_tables is not null)
            {
                st.imps = GetStudyIMPs(imp_tables);
            }

            var study_details = detailsPage.Find("table", By.Id("section-e")).FirstOrDefault();
            var details_rows = study_details.CssSelect("tbody tr").ToArray();
            if (details_rows is not null)
            {
                st.features = GetStudyFeatures(details_rows);
            }

            var population = detailsPage.Find("table", By.Id("section-f")).FirstOrDefault();
            var population_rows = population.CssSelect("tbody tr").ToArray();
            if (population_rows is not null)
            {
                st.population = GetStudyPopulation(population_rows);
            }

            return st;
        }


        public EUCTR_Record ExtractResultDetails(EUCTR_Record st, WebPage resultsPage)
        {

            var pdfLInk = resultsPage.Find("a", By.Id("downloadResultPdf")).FirstOrDefault();

            if (pdfLInk is not null)
            {
                st.results_pdf_link = pdfLInk.Attributes["href"].Value;
            }

            var result_div = resultsPage.Find("div", By.Id("resultContent")).FirstOrDefault();

            if (result_div is not null)
            {
                var result_rows = result_div.SelectNodes("table[1]/tr")?.ToArray();
                if (result_rows is not null)
                {
                    for (int i = 0; i < result_rows.Length; i++)
                    {
                        var first_cell = result_rows[i].SelectSingleNode("td[1]");
                        string first_cell_content = first_cell.TrimmedContents();

                        if (first_cell_content == "Results version number")
                        {
                            st.results_version = first_cell.SelectSingleNode("following-sibling::td[1]").TrimmedContents();
                        }
                        else if (first_cell_content == "This version publication date")
                        {
                            st.results_revision_date = first_cell.SelectSingleNode("following-sibling::td[1]").TrimmedContents();
                        }
                        else if (first_cell_content == "First version publication date")
                        {
                            st.results_first_date = first_cell.SelectSingleNode("following-sibling::td[1]").TrimmedContents();
                        }
                        else if (first_cell_content == "Summary report(s)")
                        {
                            st.results_summary_link = first_cell.SelectSingleNode("following-sibling::td[1]/a[1]").Attributes["href"].Value;
                            st.results_summary_name = first_cell.SelectSingleNode("following-sibling::td[1]/a[1]").TrimmedContents();
                        }
                    }
                }

            }

            return st;
        }


        private List<DetailLine> GetStudyIdentifiers(HtmlNode[] identifier_rows)
        {
            List<DetailLine> study_identifiers = new();

            foreach (HtmlNode row in identifier_rows)
            {
                var row_class = row.Attributes["class"];
                if (row_class is not null && row_class.Value == "tricell")
                {
                    var cells = row.CssSelect("td").ToArray();
                    string code = cells[0].InnerText;
                    if (code != "A.6" && code != "A.7" && code != "A.8")
                    {
                        DetailLine line = new();
                        List<item_value> values = new();
                        line.item_code = code;
                        line.item_name = HttpUtility.HtmlDecode(cells[1].InnerText);
                        if (cells[2].CssSelect("table").Any())
                        {
                            var inner_table = cells[2].CssSelect("table");
                            var inner_rows = inner_table.CssSelect("tr").ToArray();
                            if (inner_rows.Length > 0)
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
                            line.item_number = 1;
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


        private List<DetailLine> GetStudySponsors(HtmlNode[] sponsor_rows)
        {
            List<DetailLine> study_sponsors = new();
            
            foreach (HtmlNode row in sponsor_rows)
            {
                var row_class = row.Attributes["class"];
                if (row_class is not null && row_class.Value == "tricell")
                {
                    var cells = row.CssSelect("td").ToArray();
                    string code = cells[0].InnerText;
                    if (!code.Contains("B.5"))
                    {
                        DetailLine line = new();
                        List<item_value> values = new();
                        line.item_code = code;
                        line.item_name = HttpUtility.HtmlDecode(cells[1].InnerText);
                        if (cells[2].CssSelect("table").Any())
                        {
                            var inner_table = cells[2].CssSelect("table");
                            var inner_rows = inner_table.CssSelect("tr").ToArray();
                            line.item_number = inner_rows.Length;
                            if (inner_rows.Length > 0)
                            {
                                foreach (HtmlNode inner_row in inner_rows)
                                {
                                    HtmlNode? inner_cell = inner_row.CssSelect("td").FirstOrDefault();
                                    if (inner_cell is not null)
                                    {
                                        string? value = HttpUtility.HtmlDecode(inner_cell.InnerText).Trim();
                                        if (!string.IsNullOrEmpty(value)) values.Add(new item_value(value));
                                    }
                                }
                            }
                        }
                        else
                        {
                            line.item_number = 1;
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


        private List<ImpLine> GetStudyIMPs(HtmlNode[] imp_tables)
        {
            List<ImpLine> study_imps = new();

            int imp_num = 0;
            foreach (HtmlNode tbody in imp_tables)
            {
                imp_num++;
                var imp_rows = tbody.CssSelect("tr").ToArray();
                if (imp_rows is not null)
                {
                    foreach (HtmlNode row in imp_rows)
                    {
                        var row_class = row.Attributes["class"];
                        if (row_class is not null && row_class.Value == "tricell")
                        {
                            var cells = row.CssSelect("td").ToArray();
                            string code = cells[0].InnerText;
                            // just get various names (ofgen duplicated)
                            if (code == "D.2.1.1.1" || code == "D.3.1" || code == "D.3.8"
                                || code == "D.3.9.1" || code == "D.3.9.3")
                            {
                                ImpLine line = new();
                                line.imp_number = imp_num;
                                List<item_value> values = new();
                                line.item_code = code;
                                line.item_name = HttpUtility.HtmlDecode(cells[1].InnerText);
                                if (cells[2].CssSelect("table").Any())
                                {
                                    var inner_table = cells[2].CssSelect("table");
                                    var inner_rows = inner_table.CssSelect("tr").ToArray();
                                    line.item_number = inner_rows.Length;
                                    if (inner_rows.Length > 0)
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
                                    line.item_number = 1;
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


        private List<DetailLine> GetStudyFeatures(HtmlNode[] details_rows)
        {
            List<DetailLine> study_features = new();
            
            foreach (HtmlNode row in details_rows)
            {
                var row_class = row.Attributes["class"];
                if (row_class is not null && row_class.Value == "tricell")
                {
                    var cells = row.CssSelect("td").ToArray();
                    string code = cells[0].InnerText;
                    if (code.Contains("E.1.1") || code.Contains("E.2"))
                    {
                        DetailLine line = new();
                        List<item_value> values = new();
                        line.item_code = code;
                        line.item_name = HttpUtility.HtmlDecode(cells[1].InnerText);
                        if (cells[2].CssSelect("table").Any())
                        {
                            var inner_table = cells[2].CssSelect("table");
                            var inner_rows = inner_table.CssSelect("tr").ToArray();
                            line.item_number = inner_rows.Length;
                            if (inner_rows.Length > 0)
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
                            line.item_number = 1;
                            string value = HttpUtility.HtmlDecode(cells[2].InnerText).Trim();
                            if (!string.IsNullOrEmpty(value)) values.Add(new item_value(value.Replace("|", "<br/>")));
                        }

                        if (values.Count > 0)
                        {
                            line.item_number = values.Count;
                            line.item_values = values;
                            study_features.Add(line);
                        }
                    }


                    if (code.Contains("E.6") || code.Contains("E.7")
                        || code.Contains("E.8.1") || code.Contains("E.8.2"))
                    {
                        DetailLine line = new();
                        List<item_value> values = new();
                        line.item_code = code;
                        line.item_name = HttpUtility.HtmlDecode(cells[1].InnerText);
                        if (cells[2].CssSelect("table").Any())
                        {
                            var inner_table = cells[2].CssSelect("table");
                            var inner_rows = inner_table.CssSelect("tr").ToArray();
                            line.item_number = inner_rows.Length;
                            if (inner_rows.Length > 0)
                            {
                                foreach (HtmlNode inner_row in inner_rows)
                                {
                                    HtmlNode? inner_cell = inner_row.CssSelect("td").FirstOrDefault();
                                    if (inner_cell is not null)
                                    {
                                        string? value = HttpUtility.HtmlDecode(inner_cell.InnerText).Trim();
                                        if (value.ToLower() == "yes") values.Add(new item_value(value));
                                    }
                                }
                            }
                        }
                        else
                        {
                            line.item_number = 1;
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
                }
            }

            return study_features;
        }


        private List<DetailLine> GetStudyPopulation(HtmlNode[] population_rows)
        {
            List<DetailLine> study_population = new();
            
            foreach (HtmlNode row in population_rows)
            {
                var row_class = row.Attributes["class"];
                if (row_class is not null && row_class.Value == "tricell")
                {
                    var cells = row.CssSelect("td").ToArray();
                    string code = cells[0].InnerText;
                    if (code.Contains("F.1") || code.Contains("F.2"))
                    {
                        DetailLine line = new();
                        List<item_value> values = new();
                        line.item_code = code;
                        line.item_name = HttpUtility.HtmlDecode(cells[1].InnerText);
                        if (cells[2].CssSelect("table").Any())
                        {
                            var inner_table = cells[2].CssSelect("table");
                            var inner_rows = inner_table.CssSelect("tr").ToArray();
                            line.item_number = inner_rows.Length;
                            if (inner_rows.Length > 0)
                            {
                                foreach (HtmlNode inner_row in inner_rows)
                                {
                                    HtmlNode? inner_cell = inner_row.CssSelect("td").FirstOrDefault();
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
                            line.item_number = 1;
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


        /*
        private string InnerValue(HtmlNode node)
        {
            string allInner = node.InnerText?.Replace("\n", "")?.Replace("\r", "")?.Trim() ?? "";
            string label = node.CssSelect(".label").FirstOrDefault()?.InnerText?.Trim() ?? "";
            return allInner.Substring(label.Length)?.Trim() ?? "";
        }

        private string TrimmedContents(HtmlNode node)
        {
            return node.InnerText?.Replace("\n", "")?.Replace("\r", "")?.Trim() ?? "";
        }

        private string TrimmedLabel(HtmlNode node)
        {
            return node.CssSelect(".label").FirstOrDefault()?.InnerText?.Trim() ?? "";
        }
        */
        
    }
}
