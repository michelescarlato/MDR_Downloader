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
            // log
            return null;  // main details are missing, seems very unlikely but...
        }
        string? search_url = mn.url;
        if (search_url is null)
        {
            // log
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
        
        // t now has sd_sid (Eudract Id), sponsors id and start date, 
        // sponsor name, medical condition as text and possibly as a Meddra list,
        // age and gender as text strings, list of countries (protocols) with
        // associated statuses, details_url and results url, if one exists.
        
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
        
        // do common title sort out here!
        
        ed.target_size = mn.target_size;
        ed.results_actual_enrolment = mn.results_actual_enrolment;
        
        // Add criteria and gender, age information (though age mostly null at present)
        // needs ton be added later as a function of the identified populations.

        Criteria? crits = t.criteria;
        if (crits is not null)
        {
            ed.inclusion_criteria = crits.inclusion_criteria;
            if (!string.IsNullOrEmpty(crits.inclusion_criteria))
            {
                int age_pos = crits.inclusion_criteria.IndexOf("Are the trial subjects under 18",
                    0, StringComparison.Ordinal);
                ed.inclusion_criteria = crits.inclusion_criteria[..age_pos];
                string age_string = crits.inclusion_criteria[age_pos..];
                
                /*
                 * Are the trial subjects under 18? no
Number of subjects for this age range: 
F.1.2 Adults (18-64 years) yes   --- begins with, ends with 'yes'  (as the first test)
F.1.2.1 Number of subjects for this age range 
F.1.3 Elderly (>=65 years) no
F.1.3.1 Number of subjects for this age range 
Seem to be always F.1.2 -space and F.1.3 space
*/
                
                 */
            }
            ed.exclusion_criteria = crits.exclusion_criteria;  // first part only
            
            ed.minage = crits.agemin; // usually null at present
            ed.maxage = crits.agemax; // usually null at present
            if (crits.agemin is null && crits.agemax is null)
            {
                // need to use the structured textual description in 'inclusion criteria'
                
            }
            
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

    public string? member_state { get; set; }
    public string? primary_objectives{ get; set; }
    public string? primary_endpoints{ get; set; }
    public string? trial_status { get; set; }
    public string? recruitment_status { get; set; }

    public string? minage { get; set; }
    public string? maxage { get; set; }
    
    public string? medical_condition { get; set; }
    public string? population_age { get; set; }
    
    public string? results_url { get; set; }
    public string? results_version { get; set; }
    public string? results_date_posted { get; set; }
    public string? results_revision_date { get; set; }
    public string? results_summary_link { get; set; }
    public string? results_summary_name { get; set; }
    public string? results_pdf_link { get; set; }
    public string? results_url_protocol { get; set; }    
    
    public string? results_IPD_plan { get; set; }
    public string? results_IPD_description { get; set; }
    
    public List<EMACountry>? countries { get; set; }
    public List<EMAIdentifier>? identifiers { get; set; }
    public List<EMAFeature>? features { get; set; }
    public List<EMACondition>? conditions{ get; set; }
    public List<EMAImp>? imp_topics { get; set; }
    public List<EMAOrganisation>? organisations { get; set; }
    
    */