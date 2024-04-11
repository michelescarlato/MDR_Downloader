using HtmlAgilityPack;
using System.ComponentModel;
using System.Xml.Serialization;
namespace MDR_Downloader.euctr;

#pragma warning disable CS8981

public class Euctr_Record
{
    public string sd_sid { get; set; } = null!;
    public string? study_type { get; set; }
    public string? sponsors_id { get; set; }
    public string? sponsor_name { get; set; }
    
    public string? date_registration { get; set; }
    public DateTime? date_last_revised{ get; set; }
    public string? start_date { get; set; }
    public string? member_state { get; set; }
    public string? primary_objectives{ get; set; }
    public string? primary_endpoints{ get; set; }
    public string? trial_status { get; set; }
    public string? recruitment_status { get; set; }
    
    public string? scientific_title { get; set; }
    public string? public_title { get; set; }
    public string? acronym { get; set; }
    public string? scientific_acronym { get; set; }
    
    public string? target_size { get; set; }
    public string? results_actual_enrolment { get; set; }
    public string? minage { get; set; }
    public string? maxage { get; set; }
    public string? gender { get; set; }
    public string? inclusion_criteria { get; set; }
    public string? exclusion_criteria { get; set; }
    
    public string? medical_condition { get; set; }
    public string? population_age { get; set; }
    
    public string? search_url { get; set; } 
    public string? details_url { get; set; } 
    
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
    public List<Identifier>? identifiers { get; set; }
    public List<EMAFeature>? features { get; set; }
    public List<EMACondition>? conditions{ get; set; }
    public List<EMAImp>? imp_topics { get; set; }
    public List<EMAOrganisation>? organisations { get; set; }
    public List<MeddraTerm>? meddra_terms { get; set; }


    public Euctr_Record(string _sd_sid)
    {
        sd_sid = _sd_sid;
    }

    public Euctr_Record()
    { }
}

public class Study_Summary
{
    public string eudract_id { get; set; }
    public bool? do_download { get; set; }
    public HtmlNode? details_box { get; set; }

    public Study_Summary(string _eudract_id, HtmlNode? _details_box)
    {
        eudract_id = _eudract_id;
        details_box = _details_box;
    }
}


public class MeddraTerm
{
    public string? version { get; set; }
    public string? soc_term { get; set; }
    public string? code { get; set; }
    public string? term { get; set; }
    public string? level { get; set; }
}


public class EMACountry
{
    public string? country_name { get; set; }
    public string? status { get; set; }

    public EMACountry(string? _country_name, string? _status)
    {
        country_name = _country_name;
        status = _status;
    }
}


public class Identifier
{
    public int? identifier_type_id { get; set; }
    public string? identifier_type { get; set; }
    public string? identifier_value { get; set; }
    public int? identifier_org_id { get; set; }
    public string? identifier_org { get; set; }

    public Identifier(int? identifierTypeId, string? identifierType, 
                        string? identifierValue, int? identifierOrgId, string? identifierOrg)
    {
        identifier_type_id = identifierTypeId;
        identifier_type = identifierType;
        identifier_value = identifierValue;
        identifier_org_id = identifierOrgId;
        identifier_org = identifierOrg;
    }
}

public class EMAFeature
{
    public int? feature_id { get; set; }
    public string? feature_name { get; set; }
    public int? feature_value_id { get; set; }
    public string? feature_value_name { get; set; }

    public EMAFeature(int? featureId, string? featureName, int? featureValueId, 
                              string? featureValueName)
    {
        feature_id = featureId;
        feature_name = featureName;
        feature_value_id = featureValueId;
        feature_value_name = featureValueName;
    }
    
    //public string? study_design { get; set; }
    //public string? phaseField { get; set; }
}

public class EMACondition
{
    //public string? hc_freetext { get; set; }
    //public string[]? health_condition_keyword { get; set; }
    public string? condition_name { get; set; }
    public int? condition_ct_id { get; set; }
    public string? condition_ct { get; set; }
    public string? condition_ct_code { get; set; }

    public EMACondition(string? conditionName)
    {
        condition_name = conditionName;
    }
}

public class EMAImp
{
    //public string? i_freetext { get; set; }
    //public Intervention_code? intervention_code { get; set; }
    //public Intervention_keyword? intervention_keyword { get; set; }

    
    public int? imp_num { get; set; }
    public string? trade_name { get; set; }
    public string? product_name { get; set; }
    public string? inn { get; set; }
    public string? cas_number { get; set; }

    public EMAImp(int? _imp_num)
    {
        imp_num = _imp_num;
    }

    public EMAImp(int? _imp_num, string? _trade_name, string? _product_name, 
                  string? _inn, string? _cas_number)
    {
        imp_num = _imp_num;
        trade_name = _trade_name;
        product_name = _product_name;
        inn = _inn;
        cas_number = _cas_number;
    }

}

public class EMAOrganisation
{
    //public string? primary_sponsor { get; set; }
    //public string[]? secondary_sponsor { get; set; }
    //public string[]? source_support { get; set; }

    public int? org_role_id { get; set; }
    public string? org_role { get; set; }
    public string? org_name { get; set; }    
    
    public EMAOrganisation(int? orgRoleId, string? orgRole, string? orgName)
    {
         org_role_id = orgRoleId;
         org_role = orgRole;
         org_name = orgName;
    }
}




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





