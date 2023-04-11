#pragma warning disable CS8981
using System.Xml.Serialization;
using System.ComponentModel;
namespace MDR_Downloader.euctr;


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
[XmlRoot(Namespace = "", IsNullable = false)]
public class trials
{
    [XmlElement("trial")]
    public Trial[]? trials_list;
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Trial
{
    public Main? main { get; set; }
    
    [XmlArrayItem("contact", IsNullable = false)]
    public Contact[]? contacts { get; set; }

    [XmlArrayItem("country2", IsNullable = false)]
    public string[]? countries { get; set; }

    public Criteria? criteria { get; set; }

    public Health_condition_code? health_condition_code { get; set; }
    
    [XmlArrayItem("hc_keyword", IsNullable = false)]
    public string[]? health_condition_keyword { get; set; }

    public Intervention_code? intervention_code { get; set; }

    public Intervention_keyword? intervention_keyword { get; set; }
    
    [XmlArrayItem("prim_outcome", IsNullable = false)]
    public string[]? primary_outcome { get; set; }
    
    [XmlArrayItem("sec_outcome", IsNullable = false)]
    public string[]? secondary_outcome { get; set; }

    [XmlArrayItem("sponsor_name", IsNullable = false)]
    public string[]? secondary_sponsor { get; set; }
    
    [XmlArrayItem("secondary_id", IsNullable = false)]
    public Secondary_id[]? secondary_ids { get; set; }
    
    [XmlArrayItem("source_name", IsNullable = false)]
    public string[]? source_support { get; set; }

    public Ethics_reviews? ethics_reviews { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Main
{
    public string? trial_id { get; set; }
    public string? utrn { get; set; } 
    public string? reg_name { get; set; }
    public string? date_registration { get; set; }
    public string? primary_sponsor { get; set; }
    public string? public_title { get; set; }
    public string? acronym { get; set; }
    public string? scientific_title { get; set; }
    public string? scientific_acronym { get; set; }
    public string? date_enrolment { get; set; }
    public string? type_enrolment { get; set; }
    public string? target_size { get; set; }
    public string? recruitment_status { get; set; }
    public string? url { get; set; }
    public string? study_type { get; set; }
    public string? study_design { get; set; }
    public string? phase { get; set; }
    public string? hc_freetext { get; set; }
    public string? i_freetext { get; set; }
    public string? results_actual_enrolment { get; set; }
    public string? results_date_completed { get; set; }
    public string? results_url_link { get; set; }
    public string? results_summary { get; set; }
    public string? results_date_posted { get; set; }
    public string? results_date_first_publication { get; set; }
    public string? results_baseline_char { get; set; }
    public string? results_participant_flow { get; set; }
    public string? results_adverse_events { get; set; }
    public string? results_outcome_measures { get; set; }
    public string? results_url_protocol { get; set; }
    public string? results_IPD_plan { get; set; }
    public string? results_IPD_description { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Contact
{
    public string? type { get; set; }
    public string? firstname { get; set; }
    public string? middlename { get; set; }
    public string? lastname { get; set; }
    public string? address { get; set; }
    public string? city { get; set; }
    public string? country1 { get; set; }
    public string? zip { get; set; }
    public string? telephone { get; set; }
    public string? email { get; set; }
    public string? affiliation { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Criteria
{
    public string? inclusion_criteria { get; set; }
    public string? agemin { get; set; }
    public string? agemax { get; set; }
    public string? gender { get; set; }
    public string? exclusion_criteria { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Health_condition_code
{
    public string? hc_code { get; set; }
    public Health_condition_code? health_condition_code { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Intervention_code
{
    public string? i_code { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Intervention_keyword
{
    public string? i_keyword { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Secondary_id
{
    public string? sec_id { get; set; }
    public string? issuing_authority { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Ethics_reviews
{
    public Ethics_reviewsEthics_review? ethics_review { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Ethics_reviewsEthics_review
{
    public string? status { get; set; }
    public string? approval_date { get; set; }
    public string? contact_name { get; set; }
    public string? contact_address { get; set; }
    public string? contact_phone { get; set; }
    public string? contact_email { get; set; }
}

