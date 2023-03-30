using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MDR_Downloader.Helpers;
using ScrapySharp.Extensions;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System.Web;

namespace MDR_Downloader.euctr;

public class EUCTR_Processor
{
    private readonly ILoggingHelper _loggingHelper;
    
    public EUCTR_Processor(ILoggingHelper loggingHelper)
    {
        _loggingHelper = loggingHelper;
    }
    
    public int GetListLength(WebPage homePage)
    {
        // gets the numbers of records found for the current search.

        HtmlNode? total_link = homePage.Find("a", By.Id("ui-id-1")).FirstOrDefault();
        string? results = total_link?.TrimmedContents();
        if (results is null)
        {
            _loggingHelper.LogError("Unable to obtain number of records on home page");
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
            _loggingHelper.LogError("Unable to find results box on page");
            return null;
        }
        List<HtmlNode> studyBoxes = pageContent.CssSelect(".result").ToList();
        if (studyBoxes.Count == 0)
        {
            _loggingHelper.LogError("Unable to obtain list of summary boxes on page");
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
                    st.date_registration = cells[1].InnerText;
                    break;
                }
            }
        }
        
        // Assume all studies are interventional - they always have been!
        
        st.study_type = "Interventional";
        
        // Use each section of the page to obtain the relevant data points
        
        HtmlNode? identifiers = detailsPage.Find("table", By.Id("section-a")).FirstOrDefault();
        IEnumerable<HtmlNode>? identifier_rows = identifiers?.CssSelect("tbody tr");
        if (identifier_rows is not null)
        {
            GetStudyIdentifiers(st, identifier_rows); // Gets identifiers, also populates titles, member state.
        }

        HtmlNode? sponsor = detailsPage.Find("table", By.Id("section-b")).FirstOrDefault();
        IEnumerable<HtmlNode>? sponsor_rows = sponsor?.CssSelect("tbody tr");
        if (sponsor_rows is not null)
        {
            GetStudySponsors(st, sponsor_rows);
        }

        HtmlNode? imp_details = detailsPage.Find("table", By.Id("section-d")).FirstOrDefault();
        IEnumerable<HtmlNode>? imp_tables = imp_details?.CssSelect("tbody");
        if (imp_tables is not null)
        {
            GetStudyIMPs(st, imp_tables);
        }

        HtmlNode? study_details = detailsPage.Find("table", By.Id("section-e")).FirstOrDefault();
        IEnumerable<HtmlNode>? details_rows = study_details?.CssSelect("tbody tr");
        if (details_rows is not null)
        {
            GetStudyFeatures(st, details_rows);
        }

        HtmlNode? population = detailsPage.Find("table", By.Id("section-f")).FirstOrDefault();
        IEnumerable<HtmlNode>? population_rows = population?.CssSelect("tbody tr");
        if (population_rows is not null)
        {
           GetStudyPopulation(st, population_rows);
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
                        st.results_date_posted = cell_content;
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


    private void GetStudyIdentifiers(Euctr_Record st, IEnumerable<HtmlNode> identifier_rows)
    {
        // add in initial identifiers representing EUDRACT number and sponsor id

        List<EMAIdentifier> ids = new() 
            { new EMAIdentifier(11, "Trial Registry ID", st.sd_sid, 100123, "EU Clinical Trials Register") };
        if (!string.IsNullOrEmpty(st.sponsors_id))
        {
            string sp_name = !string.IsNullOrEmpty(st.sponsor_name)
                ? st.sponsor_name
                : "No organisation name provided in source data";
            ids.Add(new EMAIdentifier(14, "Sponsor ID", st.sponsors_id, null, sp_name));
        }
        
        // get others from web page
        
        foreach (HtmlNode row in identifier_rows)
        {
            var row_class = row.Attributes["class"];
            if (row_class is { Value: "tricell" })
            {
                var cells = row.CssSelect("td").ToArray();
                string code = cells[0].InnerText;
                if (code == "A.1" || code.Contains("A.3") || code.Contains("A.5"))
                {
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
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        ProcessIdentifier(code, value);
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
                            ProcessIdentifier(code, value);
                        }
                    }
                }
            }
        }

        void ProcessIdentifier(string code, string value)
        {
            if (code == "A.1")
            {
                st.member_state = value;
            }
            else if (code.Contains("A.3"))  // titles
            {
                // N.B. If more than one found only the first (usually English) one is used.
                
                if (code == "A.3" && string.IsNullOrEmpty(st.scientific_title))
                {
                    st.scientific_title = value;
                }
                else if (code == "A.3.1" && string.IsNullOrEmpty(st.public_title))
                {
                    st.public_title = value;
                }
                else if (code == "A.3.2"&& string.IsNullOrEmpty(st.acronym))
                {
                    st.acronym = value;
                }
            }
            else if (code.Contains("A.5"))  // identifiers
            {
                if (code == "A.5.1")  // ISRCTN
                {
                    ids.Add(new EMAIdentifier(11, "Trial Registry ID", code, 100126, "ISRCTN"));
                }
                else  if (code == "A.5.2")  // NCT
                {
                    ids.Add(new EMAIdentifier(11, "Trial Registry ID", code, 100120, "ClinicalTrials.gov"));
                }
                else  if (code == "A.5.3")  // UTRN
                {
                    ids.Add(new EMAIdentifier(11, "Trial Registry ID", code, 100115, 
                        "International Clinical Trials Registry Platform"));
                }
            }
        }

        st.identifiers = ids;
    }

    
    private void GetStudySponsors(Euctr_Record st, IEnumerable<HtmlNode> sponsor_rows)
    {
        List<EMAOrganisation> organisations = new();
        foreach (HtmlNode row in sponsor_rows)
        {
            var row_class = row.Attributes["class"];
            if (row_class is not null && row_class.Value == "tricell")
            {
                var cells = row.CssSelect("td").ToArray();
                string code = cells[0].InnerText;
                if (code is "B.1.1" or "B.4.1")
                {
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
                       
                                        ProcessSponsor(code, value);
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
                   
                            ProcessSponsor(code, value);
                        }
                    }
                }
            }
        }

        void ProcessSponsor(string code, string value)
        {
            if (code == "B.1.1")
            {
                organisations.Add(new EMAOrganisation(54, "Trial Sponsor", value));
            }
            else if (code == "B.4.1")
            {
                organisations.Add(new EMAOrganisation(58, "Study Funder", value));
            }
        }

        st.organisations = organisations;
    }


    private void GetStudyIMPs(Euctr_Record st, IEnumerable<HtmlNode> imp_tables)
    {
        List<EMAImp> imps = new();
        EMAImp? current_imp = new(0);
        foreach (HtmlNode tbody in imp_tables)
        {
            IEnumerable<HtmlNode>? imp_rows = tbody.CssSelect("tr");
            if (imp_rows is not null)
            {
                foreach (HtmlNode row in imp_rows)
                {
                    string? row_class = row.Attributes["class"].ToString();
                    if (!string.IsNullOrEmpty(row_class) && row_class is "tricell" or "cellBlue")
                    {
                        // for tricell the three cells are code, item name, item value
                        // for cellBlue there is only 1 cell,and that holds a code / heading
                        
                        var cells = row.CssSelect("td").ToArray();
                        string code = cells[0].InnerText;
                        if (row_class == "cellBlue")
                        {
                            if (Regex.Match(code, @"D\.IMP:\s\d{1,2}").Success)
                            {
                                int current_imp_num = int.Parse(Regex.Match(code, @"\d{1,2}").Value);
                                AddNewImp(current_imp_num);
                            }
                        }
                        else
                        {
                            // Get various names.

                            if (code is "D.2.1.1.1" or "D.3.1" or "D.3.8" or "D.3.9.1")
                            {
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
                                                if (current_imp is not null && !string.IsNullOrEmpty(value))
                                                {
                                                    ProcessImpDetails(code, value);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    string value = HttpUtility.HtmlDecode(cells[2].InnerText).Trim();
                                    if (current_imp is not null &&  !string.IsNullOrEmpty(value))
                                    {
                                        ProcessImpDetails(code, value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void AddNewImp(int imp_number)
        {
            current_imp = new EMAImp(imp_number);
            imps.Add(current_imp);
        }
        
        void ProcessImpDetails(string code, string value)
        {
            if (current_imp is not null && code == "D.2.1.1.1")  // Trade name
            {
                current_imp.trade_name = value;
            }
            else if (current_imp is not null && code == "D.3.1") // Product Name
            {
                current_imp.product_name = value;
            }
            else if (current_imp is not null && code == "D.3.8") // INN
            {
                current_imp.inn = value;
            }
            else if (current_imp is not null && code == "D.3.9.1") // CAS number
            {
                current_imp.cas_number = value;
            }
        }

        if (imps.Count > 1)
        {
            st.imp_topics = imps.Where(i => i.imp_num > 0).ToList();
        }
    }


    private void GetStudyFeatures(Euctr_Record st, IEnumerable<HtmlNode> details_rows)
    {
        List<EMACondition> conditions = new();
        List<EMAFeature> features = new();
        
        foreach (HtmlNode row in details_rows)
        {
            var row_class = row.Attributes["class"];
            if (row_class is { Value: "tricell" })
            {
                var cells = row.CssSelect("td").ToArray();
                string code = cells[0].InnerText;

                // condition under study and study objectives

                if (code is "E.1.1" or "E.2.1" or "E.3" or "E.4" or "E.5.1"
                           || code.Contains("E.7") || code.Contains("E.8"))
                {
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
                                        ProcessFeature(code, value);
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
                            value = value.Replace("|", "\n");
                            ProcessFeature(code, value);
                        }
                    }
                }
            }
        }
        
        void ProcessFeature(string code, string value)
        {
            if (code == "E.3")
            {
                st.inclusion_criteria = value;
            }
            else if (code == "E.4")
            {
                st.exclusion_criteria = value;
            }
            else if (code == "E.1.1")  // condition under study
            {
                conditions.Add(new EMACondition(value));
            }
            else if (code == "E.2.1")  // primary objectives
            {
                st.primary_objectives = value;
            }
            else if (code == "E.5.1")  // primary end-points
            {
                st.primary_endpoints = value;
            }
            else if (code.Contains("E.7") || code.Contains("E.8"))
            {
                Tuple<int, string, int, string> new_feature = code switch
                {
                    "E.7.1" => new Tuple<int, string, int, string>(20, "phase", 110, "Phase 1"),
                    "E.7.2" => new Tuple<int, string, int, string>(20, "phase", 120, "Phase 2"),
                    "E.7.3" => new Tuple<int, string, int, string>(20, "phase", 130, "Phase 3"),
                    "E.7.4" => new Tuple<int, string, int, string>(20, "phase", 135, "Phase 4"),
                    "E.8.1.1" => new Tuple<int, string, int, string>(22, "allocation type", 205, "Randomised"),
                    "E.8.1.2" => new Tuple<int, string, int, string>(24, "masking", 500, "None (Open Label)"),
                    "E.8.1.3" => new Tuple<int, string, int, string>(24, "masking", 505, "Single"),
                    "E.8.1.4" => new Tuple<int, string, int, string>(24, "masking", 510, "Double"),
                    "E.8.1.5" => new Tuple<int, string, int, string>(23, "intervention model", 305,
                        "Parallel assignment"),
                    "E.8.1.6" => new Tuple<int, string, int, string>(23, "intervention model", 310,
                        "Crossover assignment"),
                    _ => new Tuple<int, string, int, string>(0, "", 0, ""),
                };

                if (new_feature.Item1 != 0)
                {
                    features.Add(new EMAFeature(new_feature.Item1, new_feature.Item2,
                        new_feature.Item3, new_feature.Item4));
                }
            }
            else if (code == "E.8.6.3")  // countries
            {
                bool add_country = true;
                if (st.countries is not null)
                {
                    foreach (EMACountry c in st.countries)
                    {
                        if (c.country_name == value)
                        {
                            add_country = false;
                            break;
                        }
                    }
                }
                else
                {
                    st.countries = new List<EMACountry>();
                }

                if (add_country)
                {
                    st.countries.Add(new EMACountry(value, null));
                }
            }

            st.conditions = conditions;
            st.features = features;
        }
    }
    
 
    private void GetStudyPopulation(Euctr_Record st, IEnumerable<HtmlNode> population_rows)
    {
        Dictionary<string, bool> pop_groups = new()
        {
            { "includes_under18", false },
            { "includes_in_utero", false },
            { "includes_preterm", false },
            { "includes_newborns", false },
            { "includes_infants", false },
            { "includes_children", false },
            { "includes_ados", false },
            { "includes_adults", false },
            { "includes_elderly", false },
            { "includes_women", false },
            { "includes_men", false },
        };

        foreach (HtmlNode row in population_rows)
        {
            var row_class = row.Attributes["class"];
            if (row_class is { Value: "tricell" })
            {
                var cells = row.CssSelect("td").ToArray();
                string code = cells[0].InnerText;
                if (code.Contains("F.1") || code.Contains("F.2"))
                {
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
                                    if (value.ToLower() == "yes")
                                    {
                                        UpdatePopDictionary(code);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        string value = HttpUtility.HtmlDecode(cells[2].InnerText).Trim();
                        if (value.ToLower() == "yes")
                        {
                            UpdatePopDictionary(code);
                        }
                    }
                }
            }
        }
        
        // get gender eligibility information

        if (pop_groups["includes_men"] && pop_groups["includes_women"])
        {
            st.gender = "All";
        }
        else if (pop_groups["includes_women"])
        {
            st.gender = "Female";
        }
        else if (pop_groups["includes_men"])
        {
            st.gender = "Male";
        }
        else
        {
            st.gender = "Not provided";
        }
        
        if (!pop_groups["includes_under18"])
        {
            // No children or adolescents included. If 'elderly' are included no age maximum is presumed.

            if (pop_groups["includes_adults"] && pop_groups["includes_elderly"])
            {
                st.minage = "18";
            }
            else if (pop_groups["includes_adults"])
            {
                st.minage = "18";
                st.maxage = "64";
            }
            else if (pop_groups["includes_elderly"])
            {
                st.minage = "65";
            }
        }
        else
        {
            // Some under 18s included. First discount the situation where under-18s,
            // adults and elderly are all included corresponds to no age restrictions

            if (pop_groups["includes_under18"] && pop_groups["includes_adults"] && pop_groups["includes_elderly"])
            {
                // Leave min and max ages blank
            }
            else
            {
                // First try and obtain a minimum age. Start with the youngest included and work up.

                if (pop_groups["includes_in_utero"] || pop_groups["includes_preterm"] ||
                    pop_groups["includes_newborns"])
                {
                    st.minage = "0 (days)";
                }
                else if (pop_groups["includes_infants"])
                {
                    st.minage = "28 (days)";
                }
                else if (pop_groups["includes_children"])
                {
                    st.minage = "2";
                }
                else if (pop_groups["includes_ados"])
                {
                    st.minage = "12";
                }

                // Then try and obtain a maximum age. Start with the oldest included and work down.

                if (pop_groups["includes_adults"])
                {
                    st.maxage = "64";
                }
                else if (pop_groups["includes_ados"])
                {
                    st.maxage = "17";
                }
                else if (pop_groups["includes_children"])
                {
                    st.maxage = "11";
                }
                else if (pop_groups["includes_infants"])
                {
                    st.maxage = "23 (months)";
                }
                else if (pop_groups["includes_newborns"])
                {
                    st.maxage = "27 (days)";
                }
                else if (pop_groups["includes_in_utero"] || pop_groups["includes_preterm"])
                {
                    st.maxage = "0 (days)";
                }
            }
        }
        
        // local function used to indicate which population flags are true.
        
        void UpdatePopDictionary(string code)
        {
            string group_type = code switch
            {
                "F.1.1" => "includes_under18",
                "F.1.1.1" => "includes_in_utero",
                "F.1.1.2" => "includes_preterm",
                "F.1.1.3" => "includes_newborns",
                "F.1.1.4" => "includes_infants",
                "F.1.1.5" => "includes_children",
                "F.1.1.6" => "includes_ados",
                "F.1.2" => "includes_adults",
                "F.1.3" => "includes_elderly",
                "F.2.1" => "includes_women",
                "F.2.2" => "includes_men",
                _ => ""
            };
            if (group_type != "")
            {
                pop_groups[group_type] = true;
            }
        }
    }
}
