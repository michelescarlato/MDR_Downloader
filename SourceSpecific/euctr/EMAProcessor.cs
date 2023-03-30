using HtmlAgilityPack;
using MDR_Downloader.Helpers;
using ScrapySharp.Extensions;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System.Web;
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
            return null;  // main details are missing, seems very unlikely but...
        }
        string? search_url = mn.url;
        if (search_url is null)
        {
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
        if (ed is null)
        {
            _loggingHelper.LogError($"Unable to use results box to get basic information on study");
            return null;
        }

        ed.date_registration = mn.date_registration;
        ed.start_date = mn.date_enrolment;
        if (mn.study_type is not null && mn.study_type.StartsWith("Interventional clinical trial"))
        {
            ed.study_type = "Interventional"; // They all are at the moment!
        }

        ed.scientific_title = mn.scientific_title;
        ed.public_title = mn.public_title;
        ed.scientific_acronym = mn.scientific_acronym;
        ed.acronym = mn.acronym;
        
        ed.target_size = mn.target_size;
        ed.results_actual_enrolment = mn.results_actual_enrolment;


        
        // Add criteria and gender, ager information (though age mostly null at present)
        // needs ton be added later as a function of the identified populations.

        Criteria? crits = t.criteria;
        if (crits is not null)
        {
            ed.inclusion_criteria = crits.inclusion_criteria;
            ed.exclusion_criteria = crits.exclusion_criteria;
            ed.minage = crits.agemin; // usually null at present
            ed.maxage = crits.agemax; // usually null at present
            if (!string.IsNullOrEmpty(crits.gender))
            {
                if (crits.gender.Contains("Female: yes") && crits.gender.Contains("Male: yes"))
                {
                    ed.gender = "All";
                }
                else if (crits.gender.Contains("includes_women"))
                {
                    ed.gender = "Female";
                }
                else if (crits.gender.Contains("includes_men"))
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

        return ed;
    }
}


/*

    public string? sd_sid { get; set; }
    public string? study_type { get; set; }
    public string? brief_description{ get; set; }
    public string? recruitment_status { get; set; }
    public string? url { get; set; }
    public string? results_url_link { get; set; }
    public string? reg_name { get; set; }
    public string? date_registration { get; set; }
    public string? date_enrolment { get; set; }
    public string? type_enrolment { get; set; }
    public string? target_size { get; set; }
    public string? results_actual_enrolment { get; set; }
    public string? results_date_completed { get; set; }
    public string? results_date_posted { get; set; }
    public string? results_url_protocol { get; set; }
    public string? results_IPD_plan { get; set; }
    public string? results_IPD_description { get; set; }
    
    public List<EMATitle>? titles { get; set; }
    public List<EMACountry>? countries { get; set; }
    public List<EMAIdentifier>? identifiers { get; set; }
    public List<EMAFeature>? features { get; set; }
    public List<EMACondition>? conditions{ get; set; }
    public List<EMAImp>? imp_topics { get; set; }
    public List<EMAOrganisation>? organisations { get; set; }
    public List<EMAPerson>? people { get; set; }
    
    */