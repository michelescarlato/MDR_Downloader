using HtmlAgilityPack;
using System.Xml.Serialization;

namespace MDR_Downloader.euctr;


public class Euctr_Record
{
    public string sd_sid { get; set; } = null!;
    public string? sponsor_id { get; set; }
    public string? sponsor_name { get; set; }
    public string? start_date { get; set; }
    public string? inclusion_criteria { get; set; }
    public string? exclusion_criteria { get; set; }
    public string? trial_status { get; set; }
    public string? medical_condition { get; set; }
    public string? population_age { get; set; }
    public string? gender { get; set; }
    public string? details_url { get; set; } 
    public string? results_url { get; set; }
    public string? results_version { get; set; }
    public string? results_first_date { get; set; }
    public string? results_revision_date { get; set; }
    public string? results_summary_link { get; set; }
    public string? results_summary_name { get; set; }
    public string? results_pdf_link { get; set; }
    public string? entered_in_db { get; set; }

    public List<MeddraTerm>? meddra_terms { get; set; }
    public List<DetailLine>? identifiers { get; set; }
    public List<DetailLine>? sponsors { get; set; }
    public List<ImpLine>? imps { get; set; }
    public List<DetailLine>? features { get; set; }
    public List<DetailLine>? population { get; set; }
    public List<Country>? countries { get; set; }

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


public class DetailLine
{
    public string? item_code { get; set; }
    public string? item_name { get; set; }
    public int? item_number { get; set; }
    
    public DetailLine(string? _item_code, string? _item_name)
    {
        item_code = _item_code;
        item_name = _item_name;
    }

    public DetailLine()
    { }

    [XmlArray("values")]
    [XmlArrayItem("value")]
    public List<item_value>? item_values { get; set; }
}

public class ImpLine
{
    public int? imp_number { get; set; }
    public string? item_code { get; set; }
    public string? item_name { get; set; }
    public int? item_number { get; set; }
    
    [XmlArray("values")]
    [XmlArrayItem("value")]
    public List<item_value>? item_values { get; set; }
    
    public ImpLine(int? _imp_number, string? _item_code, 
                   string? _item_name)
    {
        imp_number = _imp_number;
        item_code = _item_code;
        item_name = _item_name;
    }

    public ImpLine()
    { }
}

public class item_value
{
    [XmlText]
    public string? value { get; set; }

    public item_value(string? _value)
    {
        value = _value;
    }

    public item_value()
    { }
}

public class Country
{
    public string? name { get; set; }
    public string? status { get; set; }

    public Country()
    { }

    public Country(string? _name, string? _status)
    {
        name = _name;
        status = _status;
    }
}
