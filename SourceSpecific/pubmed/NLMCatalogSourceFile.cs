using System.ComponentModel;
using System.Xml.Serialization;
#pragma warning disable CS8618
namespace MDR_Downloader.pubmed;

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
[XmlRoot(Namespace = "", IsNullable = false)]
public class NLMCatalogRecordSet
{
    [XmlElement("NLMCatalogRecord")]
    public NLMRecord[] NLMCatalogRecord { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class NLMRecord
{
    public string NlmUniqueID { get; set; }
    public NLMDate DateCreated { get; set; }
    public NLMDate DateRevised { get; set; }
    public NLMDate DateAuthorized { get; set; }
    public NLMDate DateCompleted { get; set; }
    public NLMDate DateRevisedMajor { get; set; }
    public TitleMain? TitleMain { get; set; }
    public string MedlineTA { get; set; }

    [XmlElement("TitleAlternate")]
    public TitleAlternate[] TitleAlternate { get; set; }

    [XmlElement("TitleRelated")]
    public TitleRelated[] TitleRelated { get; set; }

    public RecordAuthorList AuthorList { get; set; }
    public ResourceInfo ResourceInfo { get; set; }

    [XmlArrayItem("PublicationType", IsNullable = false)]
    public string[] PublicationTypeList { get; set; }
    public PublicationInfo? PublicationInfo { get; set; }

    [XmlElement("Language")]
    public Language[] Language { get; set; }
    public PhysicalDescription PhysicalDescription { get; set; }

    [XmlArrayItem("IndexingSource", IsNullable = false)]
    public IndexingSource[] IndexingSourceList { get; set; }

    [XmlElement("GeneralNote")]
    public GeneralNote[] GeneralNote { get; set; }

    [XmlArrayItem("MeshHeading", IsNullable = false)]
    public MeshHeadingEntry[] MeshHeadingList { get; set; }
    public Classification Classification { get; set; }

    [XmlArrayItem("ELocation", IsNullable = false)]
    public ELocation[] ELocationList { get; set; }
    public LCCN LCCN { get; set; }

    [XmlElement("ISSN")]
    public ISSN[]? ISSN { get; set; }
    public ISSNLinking ISSNLinking { get; set; }
    public RecordOtherID OtherID { get; set; }

    [XmlAttribute()]
    public string Owner { get; set; }

    [XmlAttribute()]
    public string Status { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class NLMDate
{
    public string Year { get; set; }
    public string Month { get; set; }
    public string Day { get; set; }
}

/*
[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class DateCreated
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class DateRevised
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class DateAuthorized
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class DateCompleted
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class DateRevisedMajor
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
}
*/

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class TitleMain
{
    public TitleMainTitle Title { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class TitleMainTitle
{
    [XmlAttribute()]
    public int Sort { get; set; }

    [XmlText()]
    public string Value { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class TitleAlternate
{
    public AlternateTitle Title { get; set; }
    public string OtherInformation { get; set; }

    [XmlAttribute()]
    public string Owner { get; set; }

    [XmlAttribute()]
    public string TitleType { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class AlternateTitle
{
    [XmlAttribute()]
    public string Sort { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class TitleRelated
{
    public TitleRelatedTitle Title { get; set; }

    [XmlElement("RecordID")]
    public TitleRelatedRecordID[] RecordID { get; set; }
    public TitleRelatedISSN ISSN { get; set; }

    [XmlAttribute()]
    public string Owner { get; set; }

    [XmlAttribute()]
    public string TitleType { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class TitleRelatedTitle
{
    [XmlAttribute()]
    public string Sort { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class TitleRelatedRecordID
{
    [XmlAttribute()]
    public string Source { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class TitleRelatedISSN
{
    [XmlAttribute()]
    public string IssnType { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class RecordAuthorList
{
    [XmlElement("Author")]
    public AuthorListAuthor[] Author { get; set; }

    [XmlAttribute()]
    public string CompleteYN { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class AuthorListAuthor
{
    public string CollectiveName { get; set; }

    public AuthorListAuthorRole Role { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class AuthorListAuthorRole
{
    [XmlAttribute()]
    public string CodedYN { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class ResourceInfo
{
    public string TypeOfResource { get; set; }
    public string Issuance { get; set; }

    [XmlElement("ResourceUnit")]
    public string[] ResourceUnit { get; set; }
    public ResourceInfoResource Resource { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class ResourceInfoResource
{
    public string ContentType { get; set; }
    public string MediaType { get; set; }
    public string CarrierType { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class PublicationInfo
{
    public string Country { get; set; }
    public PublicationInfoPlaceCode PlaceCode { get; set; }
    public PublicationInfoImprint? Imprint { get; set; }
    public string PublicationFirstYear { get; set; }
    public string PublicationEndYear { get; set; }
    public string DatesOfSerialPublication { get; set; }
    public PublicationInfoFrequency Frequency { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class PublicationInfoPlaceCode
{
    [XmlAttribute()]
    public string Authority { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class PublicationInfoImprint
{
    public string Place { get; set; }
    public string Entity { get; set; }
    public string DateIssued { get; set; }
    public string ImprintFull { get; set; }

    [XmlAttribute()]
    public string ImprintType { get; set; }

    [XmlAttribute()]
    public string FunctionType { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class PublicationInfoFrequency
{
    [XmlAttribute()]
    public string FrequencyType { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Language
{
    [XmlAttribute()]
    public string LangType { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class PhysicalDescription
{
    public string Extent { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class IndexingSource
{
    public IndexingSourceIndexingSourceName IndexingSourceName { get; set; }
    public string Coverage { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class IndexingSourceIndexingSourceName
{
    [XmlAttribute()]
    public string IndexingTreatment { get; set; }

    [XmlAttribute()]
    public string IndexingStatus { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class GeneralNote
{
    [XmlAttribute()]
    public string Owner { get; set; }

    [XmlAttribute()]
    public string NoteType { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class MeshHeadingEntry
{
    public MeshHeadingDescriptorName DescriptorName { get; set; }
    public MeshHeadingQualifierName QualifierName { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class MeshHeadingDescriptorName
{
    [XmlAttribute()]
    public string MajorTopicYN { get; set; }

    [XmlText()]
    public string Value { get; set; }

}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class MeshHeadingQualifierName
{

    [XmlAttribute()]
    public string MajorTopicYN { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class Classification
{
    [XmlAttribute()]
    public string NLMCallNumberYN { get; set; }

    [XmlAttribute()]
    public string Authority { get; set; }

    [XmlAttribute()]
    public string CallNumberType { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class ELocation
{
    public ELocationELocationID ELocationID { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class ELocationELocationID
{
    [XmlAttribute()]
    public string EIdType { get; set; }

    [XmlAttribute()]
    public string ValidYN { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class LCCN
{
    [XmlAttribute()]
    public string ValidYN { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class ISSN
{
    [XmlAttribute()]
    public string ValidYN { get; set; }

    [XmlAttribute()]
    public string IssnType { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class ISSNLinking
{
    [XmlAttribute()]
    public string ValidYN { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class RecordOtherID
{
    [XmlAttribute()]
    public string prefix { get; set; }

    [XmlAttribute()]
    public string source { get; set; }

    [XmlText()]
    public string value { get; set; }
}


/*
[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
[XmlRoot(Namespace = "", IsNullable = false)]
public partial class NLMCatalogRecordSet
{
    [XmlElement("NLMCatalogRecord")]
    public NLMCatalogRecordSetNLMCatalogRecord[] NLMCatalogRecord { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecord
{
    public ulong NlmUniqueID { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordDateCreated DateCreated { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordDateRevised DateRevised { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordDateAuthorized DateAuthorized { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordDateCompleted DateCompleted { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordDateRevisedMajor DateRevisedMajor { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordTitleMain TitleMain { get; set; }

    public string MedlineTA { get; set; }

    [XmlElement("TitleAlternate")]
    public NLMCatalogRecordSetNLMCatalogRecordTitleAlternate[] TitleAlternate { get; set; }

    [XmlElement("TitleRelated")]
    public NLMCatalogRecordSetNLMCatalogRecordTitleRelated[] TitleRelated { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordAuthorList AuthorList { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordResourceInfo ResourceInfo { get; set; }

    [XmlArrayItemAttribute("PublicationType", IsNullable = false)]
    public string[] PublicationTypeList { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordPublicationInfo PublicationInfo { get; set; }

    [XmlElement("Language")]
    public NLMCatalogRecordSetNLMCatalogRecordLanguage[] Language { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordPhysicalDescription PhysicalDescription { get; set; }

    [XmlArrayItemAttribute("IndexingSource", IsNullable = false)]
    public NLMCatalogRecordSetNLMCatalogRecordIndexingSource[] IndexingSourceList { get; set; }

    [XmlElement("GeneralNote")]
    public NLMCatalogRecordSetNLMCatalogRecordGeneralNote[] GeneralNote { get; set; }

    [XmlArrayItemAttribute("MeshHeading", IsNullable = false)]
    public NLMCatalogRecordSetNLMCatalogRecordMeshHeading[] MeshHeadingList { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordClassification Classification { get; set; }
   
    [XmlArrayItemAttribute("ELocation", IsNullable = false)]
    public NLMCatalogRecordSetNLMCatalogRecordELocation[] ELocationList { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordLCCN LCCN { get; set; }

    [XmlElement("ISSN")]
    public NLMCatalogRecordSetNLMCatalogRecordISSN[] ISSN { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordISSNLinking ISSNLinking { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordOtherID OtherID { get; set; }

    [XmlAttribute()]
    public string Owner { get; set; }

    [XmlAttribute()]
    public string Status { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordDateCreated
{
    public ushort Year { get; set; }

    public byte Month { get; set; }

    public byte Day { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordDateRevised
{
    public ushort Year { get; set; }

    public byte Month { get; set; }

    public byte Day { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordDateAuthorized
{
    public ushort Year { get; set; }

    public byte Month { get; set; }

    public byte Day { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordDateCompleted
{
    public ushort Year { get; set; }

    public byte Month { get; set; }

    public byte Day { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordDateRevisedMajor
{
    public ushort Year { get; set; }

    public byte Month { get; set; }

    public byte Day { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordTitleMain
{
    public NLMCatalogRecordSetNLMCatalogRecordTitleMainTitle Title { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordTitleMainTitle
{
    [XmlAttribute()]
    public byte Sort { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordTitleAlternate
{
    public NLMCatalogRecordSetNLMCatalogRecordTitleAlternateTitle Title { get; set; }

    public string OtherInformation { get; set; }

    [XmlAttribute()]
    public string Owner { get; set; }

    [XmlAttribute()]
    public string TitleType { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordTitleAlternateTitle
{
    [XmlAttribute()]
    public string Sort { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordTitleRelated
{
    public NLMCatalogRecordSetNLMCatalogRecordTitleRelatedTitle Title { get; set; }

    [XmlElement("RecordID")]
    public NLMCatalogRecordSetNLMCatalogRecordTitleRelatedRecordID[] RecordID { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordTitleRelatedISSN ISSN { get; set; }

    [XmlAttribute()]
    public string Owner { get; set; }

    [XmlAttribute()]
    public string TitleType { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordTitleRelatedTitle
{
    [XmlAttribute()]
    public string Sort { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordTitleRelatedRecordID
{
    [XmlAttribute()]
    public string Source { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordTitleRelatedISSN
{
    [XmlAttribute()]
    public string IssnType { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordAuthorList
{
    [XmlElement("Author")]
    public NLMCatalogRecordSetNLMCatalogRecordAuthorListAuthor[] Author { get; set; }

    [XmlAttribute()]
    public string CompleteYN { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordAuthorListAuthor
{
    public string CollectiveName { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordAuthorListAuthorRole Role { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordAuthorListAuthorRole
{
    [XmlAttribute()]
    public string CodedYN { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordResourceInfo
{
    public string TypeOfResource { get; set; }

    public string Issuance { get; set; }

    [XmlElement("ResourceUnit")]
    public string[] ResourceUnit { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordResourceInfoResource Resource { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordResourceInfoResource
{
    public string ContentType { get; set; }

    public string MediaType { get; set; }

    public string CarrierType { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordPublicationInfo
{
    public string Country { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordPublicationInfoPlaceCode PlaceCode { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordPublicationInfoImprint Imprint { get; set; }

    public ushort PublicationFirstYear { get; set; }

    public ushort PublicationEndYear { get; set; }

    public string DatesOfSerialPublication { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordPublicationInfoFrequency Frequency { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordPublicationInfoPlaceCode
{
    [XmlAttribute()]
    public string Authority { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordPublicationInfoImprint
{
    public string Place { get; set; }

    public string Entity { get; set; }

    public string DateIssued { get; set; }

    public string ImprintFull { get; set; }

    [XmlAttribute()]
    public string ImprintType { get; set; }

    [XmlAttribute()]
    public string FunctionType { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordPublicationInfoFrequency
{
    [XmlAttribute()]
    public string FrequencyType { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordLanguage
{
    [XmlAttribute()]
    public string LangType { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordPhysicalDescription
{
    public string Extent { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordIndexingSource
{
    public NLMCatalogRecordSetNLMCatalogRecordIndexingSourceIndexingSourceName IndexingSourceName { get; set; }

    public string Coverage { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordIndexingSourceIndexingSourceName
{
    [XmlAttribute()]
    public string IndexingTreatment { get; set; }

    [XmlAttribute()]
    public string IndexingStatus { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordGeneralNote
{
    [XmlAttribute()]
    public string Owner { get; set; }


    [XmlAttribute()]
    public string NoteType { get; set; }


    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordMeshHeading
{
    public NLMCatalogRecordSetNLMCatalogRecordMeshHeadingDescriptorName DescriptorName { get; set; }

    public NLMCatalogRecordSetNLMCatalogRecordMeshHeadingQualifierName QualifierName { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordMeshHeadingDescriptorName
{
    [XmlAttribute()]
    public string MajorTopicYN { get; set; }

    [XmlText()]
    public string Value { get; set; }

}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordMeshHeadingQualifierName
{

    [XmlAttribute()]
    public string MajorTopicYN { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordClassification
{
    [XmlAttribute()]
    public string NLMCallNumberYN { get; set; }

    [XmlAttribute()]
    public string Authority { get; set; }


    [XmlAttribute()]
    public string CallNumberType { get; set; }


    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordELocation
{
    public NLMCatalogRecordSetNLMCatalogRecordELocationELocationID ELocationID { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordELocationELocationID
{
    [XmlAttribute()]
    public string EIdType { get; set; }

    [XmlAttribute()]
    public string ValidYN { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordLCCN
{
    [XmlAttribute()]
    public string ValidYN { get; set; }

    [XmlText()]
    public uint Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordISSN
{
    [XmlAttribute()]
    public string ValidYN { get; set; }

    [XmlAttribute()]
    public string IssnType { get; set; }

    [XmlText()]
    public string Value { get; set; } 
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordISSNLinking
{
    [XmlAttribute()]
    public string ValidYN { get; set; }

    [XmlText()]
    public string Value { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class NLMCatalogRecordSetNLMCatalogRecordOtherID
{
    [XmlAttribute()]
    public string prefix { get ; set; } 

    [XmlAttribute()]
    public string source { get; set; }
   
    [XmlText()]
    public int value { get; set; }
}
*/
