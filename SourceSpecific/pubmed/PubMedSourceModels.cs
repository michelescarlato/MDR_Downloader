using System.ComponentModel;
using System.Xml.Serialization;

namespace MDR_Downloader.pubmed;

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
[XmlRoot(Namespace = "", IsNullable = false)]
public class PubmedArticleSet
{
    [XmlElement("PubmedArticle")]
    public PubmedArticle[]? PubmedArticles { get; set; }

}

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class PubmedArticle
{
    public Citation? MedlineCitation { get; set; }
    public PubmedData? PubmedData { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Citation
{
    [XmlAttribute]
    public string Status { get; set; } = null!;

    [XmlAttribute]
    public string Owner { get; set; } = null!;

    [XmlAttribute]
    public int VersionID { get; set; }

    public PMID PMID { get; set; } = null!;
    public CitationDate? DateCompleted { get; set; }
    public CitationDate? DateRevised { get; set; }
    public CitationArticle? Article { get; set; }
    public MedlineJournalInfo? MedlineJournalInfo { get; set; }

    public Chemical[]? ChemicalList { get; set; }
    public CommentsCorrections[]? CommentsCorrectionsList { get; set; }
    [XmlElement("OtherID")]
    public OtherID[]? OtherIDs { get; set; }
    public MeshHeading[]? MeshHeadingList { get; set; }
    public SupplMeshName[]? SupplMeshList { get; set; }
    public KeywordList? KeywordList { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class PMID
{
    [XmlAttribute]
    public int Version { get; set; }

    [XmlText]
    public int Value { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class CitationDate
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class CitationArticle
{
    [XmlAttribute]
    public string PubModel { get; set; } = null!;

    public Journal? Journal { get; set; }
    public string? ArticleTitle { get; set; }
    public string? VernacularTitle { get; set; }
    public Pagination? Pagination { get; set; }

    [XmlElement("ELocationID")]
    public ELocationID[]? ELocationIDs { get; set; }

    public AuthorList? AuthorList { get; set; }
    public PublicationType[]? PublicationTypeList { get; set; }

    [XmlElement("ArticleDate")]
    public ArticleDate[]? ArticleDates { get; set; }

    [XmlElement("Language")]
    public string[]? Languages { get; set; }

    public DataBankList? DataBankList { get; set; }
    public GrantList? GrantList { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Journal
{
    public string? Title { get; set; }
    public string? ISOAbbreviation { get; set; }   
    public JournalIssue? JournalIssue { get; set; }
    
    [XmlElement("ISSN", IsNullable = false)]
    public JournalISSN[]? ISSN { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class JournalISSN
{
    [XmlAttribute]
    public string IssnType { get; set; } = null!;

    [XmlText]
    public string Value { get; set; } = null!;
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class JournalIssue
{
    [XmlAttribute]
    public string CitedMedium { get; set; } = null!;

    public string? Volume { get; set; }
    public string? Issue { get; set; }
    public IssuePubDate? PubDate { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class IssuePubDate
{
    public int? Year { get; set; }
    public string? Month { get; set; }
    public int? Day { get; set; }

    public string? MedlineDate { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Pagination
{
    public string? MedlinePgn { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class ELocationID
{
    [XmlAttribute]
    public string EIdType { get; set; } = null!;

    [XmlAttribute] 
    public string ValidYN { get; set; } = null!;

    [XmlText]
    public string Value { get; set; } = null!;
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class AuthorList
{
    [XmlAttribute]
    public string CompleteYN { get; set; } = null!;   
    
    [XmlElement("Author", IsNullable = false)]
    public Author[]? Author { get; set; }
}



[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Author
{
    public string? CollectiveName { get; set; }
    public string? LastName { get; set; }
    public string? ForeName { get; set; }
    public string? Suffix { get; set; }
    public string? Initials { get; set; }
    public Identifier? Identifier { get; set; }

    [XmlElement("AffiliationInfo")]
    public AuthorAffiliationInfo[]? Affiliations { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Identifier
{
    [XmlAttribute]
    public string Source { get; set; } = null!;

    [XmlText]
    public string Value { get; set; } = null!;
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class AuthorAffiliationInfo
{
    public string? Affiliation { get; set; }
    public Identifier? Identifier { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class DataBankList
{
    [XmlElement("DataBank")]
    public DataBank[]? DataBanks { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class DataBank
{
    public string? DataBankName { get; set; }

    [XmlArrayItem("AccessionNumber", IsNullable = false)]
    public string[]? AccessionNumberList { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class GrantList
{
    [XmlAttribute]
    public string CompleteYN { get; set; } = null!;

    [XmlElement("Grant", IsNullable = false)]
    public Grant[]? Grant { get; set; }

}

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Grant
{
    public string? GrantID { get; set; }
    public string? Acronym { get; set; }
    public string? Agency { get; set; }
    public string? Country { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class PublicationType
{
    [XmlAttribute]
    public string UI { get; set; } = null!;

    [XmlText]
    public string Value { get; set; } = null!;
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class ArticleDate
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }

    [XmlAttribute]
    public string DateType { get; set; } = null!;
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class MedlineJournalInfo
{
    public string? Country { get; set; }
    public string? MedlineTA { get; set; }
    public string? NlmUniqueID { get; set; }
    public string? ISSNLinking { get; set; }
}

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class OtherID
{
    [XmlAttribute]
    public string Source { get; set; } = null!;
    
    [XmlText]
    public string Value { get; set; } = null!;
}

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Chemical
{
    public NameOfSubstance? NameOfSubstance { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class NameOfSubstance
{
    [XmlAttribute]
    public string UI { get; set; } = null!;

    [XmlText]
    public string Value { get; set; } = null!;
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class SupplMeshName
{
    [XmlAttribute]
    public string Type { get; set; } = null!;

    [XmlAttribute]
    public string UI { get; set; } = null!;

    [XmlText]
    public string Value { get; set; } = null!;
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class CommentsCorrections
{
    [XmlAttribute]
    public string RefType { get; set; } = null!;

    public string? RefSource { get; set; }
    public CCPMID? PMID { get; set; }
    public string? Note { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class CCPMID
{
    [XmlAttribute]
    public int Version { get; set; }

    [XmlText]
    public int Value { get; set; } 
}

[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class MeshHeading
{
    public DescriptorName? DescriptorName { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class DescriptorName
{
    [XmlAttribute]
    public string UI { get; set; } = null!;
    
    [XmlAttribute]
    public string MajorTopicYN { get; set; } = null!;
    
    [XmlAttribute]
    public string Type { get; set; } = null!;
    
    [XmlText]
    public string Value { get; set; } = null!;
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class KeywordList
{
    [XmlAttribute]
    public string Owner { get; set; } = null!;

    [XmlElement("Keyword")]
    public Keyword[]? Keyword { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Keyword
{
    [XmlAttribute]
    public string MajorTopicYN { get; set; } = null!;

    [XmlText]
    public string Value { get; set; } = null!;
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class PubmedData
{
    [XmlArrayItem("PubMedPubDate", IsNullable = false)]
    public PubMedPubDate[]? History { get; set; }

    public string? PublicationStatus { get; set; }

    [XmlArrayItem("ArticleId", IsNullable = false)]
    public PubmedDataArticleId[]? ArticleIdList { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class PubMedPubDate
{
    [XmlAttribute]
    public string PubStatus { get; set; } = null!;
    
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }
}


[Serializable]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class PubmedDataArticleId
{
    [XmlAttribute] 
    public string IdType { get; set; } = null!;

    [XmlText]
    public string Value { get; set; } = null!;
}



//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.7.3081.0.
// 
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.3081.0")]
[Serializable]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
[XmlRoot(Namespace = "", IsNullable = false)]

public class eSearchResult
{
    public int Count { get; set; } 
    public int RetMax { get; set; } 
    public int RetStart { get; set; } 
    public int QueryKey { get; set; } 
    public string? WebEnv { get; set; } 
    public object? TranslationSet { get; set; } 
    public string? QueryTranslation { get; set; } 
    public eSearchResultTranslationStack? TranslationStack { get; set; } 

    [XmlArrayItem("Id", IsNullable = false)]
    public int[]? IdList { get; set; } 
}


[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.3081.0")]
[Serializable]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class eSearchResultTranslationStack
{
    public eSearchResultTranslationStackTermSet? TermSet { get; set; } 
    public string? OP { get; set; } 
}


[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.7.3081.0")]
[Serializable]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class eSearchResultTranslationStackTermSet
{
    public string? Term { get; set; } 
    public string? Field { get; set; } 
    public int? Count { get; set; } 
    public string? Explode { get; set; } 
}

public class ePostResult
{
    public int QueryKey { get; set; }
    public string? WebEnv { get; set; }
}

