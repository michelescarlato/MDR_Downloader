using System.Xml.Serialization;
using System.ComponentModel;

namespace MDR_Downloader.isrctn;

public class Study
{
    public string? sd_sid { get; set; }
    public string? dateIdAssigned { get; set; }
    public string? lastUpdated { get; set; }
    public string? title { get; set; }
    public string? scientificTitle { get; set; }
    public string? acronym { get; set; }
    public string? doi { get; set; }
    public string? studyHypothesis { get; set; }
    public string? plainEnglishSummary { get; set; }
    public string? primaryOutcome { get; set; }
    public string? secondaryOutcome { get; set; }
    public string? trialWebsite { get; set; }
    public string? ethicsApproval { get; set; }
    public string? studyDesign { get; set; }
    public string? primaryStudyDesign { get; set; }
    public string? secondaryStudyDesign { get; set; }
    public string? trialSetting { get; set; }
    public string? trialType { get; set; }
    public string? overallStatusOverride { get; set; }
    public string? overallStartDate { get; set; }
    public string? overallEndDate { get; set; }
    public string? participantType { get; set; }
    public string? inclusion { get; set; }
    public string? ageRange { get; set; }
    public string? gender { get; set; }
    public int? targetEnrolment { get; set; }
    public string? totalFinalEnrolment { get; set; }
    public string? totalTarget { get; set; }
    public string? exclusion { get; set; }
    public string? patientInfoSheet { get; set; }
    public string? recruitmentStart { get; set; }
    public string? recruitmentEnd { get; set; }
    public string? recruitmentStatusOverride { get; set; }
    public string? conditionDescription { get; set; }
    public string? diseaseClass1 { get; set; }
    public string? diseaseClass2 { get; set; }
    public string? interventionDescription { get; set; }
    public string? interventionType { get; set; }
    public string? phase { get; set; }
    public string? drugNames { get; set; }
    public string? publicationPlan { get; set; }
    public string? ipdSharingStatement { get; set; }
    public string? intentToPublish { get; set; }
    public string? publicationDetails { get; set; }
    public string? publicationStage { get; set; }
    public bool? biomedRelated { get; set; }
    public string? basicReport { get; set; }
    public string? plainEnglishReport { get; set; }

    public List<Identifier>? identifiers { get; set; }
    public List<string>? recruitmentCountries { get; set; }
    public List<StudyCentre>? centres { get; set; }
    public List<StudyOutput>? outputs { get; set; }
    public List<StudyAttachedFile>? attachedFiles { get; set; }
    public List<StudyContact>? contacts { get; set; }
    public List<StudySponsor>? sponsors { get; set; }
    public List<StudyFunder>? funders { get; set; }
    public List<string>? dataPolicies { get; set; }

}


public class Identifier
{
    public int? identifier_type_id { get; set; }
    public string? identifier_type { get; set; }

    public string? identifier_value { get; set; }
    public int? identifier_org_id { get; set; }
    public string? identifier_org { get; set; }

    public Identifier()
    { }

    public Identifier(int? _identifier_type_id, string? _identifier_type, string? _identifier_value, 
                      int? _identifier_org_id, string? _identifier_org)
    {
        identifier_type_id = _identifier_type_id;
        identifier_type = _identifier_type;
        identifier_value = _identifier_value;
        identifier_org_id = _identifier_org_id;
        identifier_org = _identifier_org;
    }
}


public class StudyCentre
{
    public string? name { get; set; }
    public string? address { get; set; }
    public string? city { get; set; }
    public string? state { get; set; }
    public string? country { get; set; }

    public StudyCentre()
    {
    }

    public StudyCentre(string? _name, string? _address, string? _city,
              string? _state, string? _country)
    {
        name = _name;
        address = _address;
        city = _city;
        state = _state;
        country = _country;

    }
}

public class StudyOutput
{
    public string? description { get; set; }
    public string? productionNotes { get; set; }
    public string? outputType { get; set; }
    public string? artefactType { get; set; }
    public string? dateCreated { get; set; }
    public string? dateUploaded { get; set; }
    public bool? peerReviewed { get; set; }
    public bool? patientFacing { get; set; }
    public string? createdBy { get; set; }
    public string? externalLinkURL { get; set; }
    public string? fileId { get; set; }
    public string? localFileURL { get; set; }
    public bool? localFilePublic { get; set; }
    public string? originalFilename { get; set; }
    public string? downloadFilename { get; set; }
    public string? version { get; set; }
    public string? mimeType { get; set; }
    
    public StudyOutput()
    {
    }

    public StudyOutput(string? _description, string? _productionNotes, 
                       string? _outputType, string? _artefactType, string? _dateCreated,
                       string? _dateUploaded, bool? _peerReviewed, bool? _patientFacing, 
                       string? _createdBy, string? _externalLinkURL, string? _fileId,
                       string? _originalFilename, string? _downloadFilename, 
                       string? _version, string? _mimeType)
    {
        description = _description;
        productionNotes = _productionNotes;
        outputType = _outputType;
        artefactType = _artefactType;
        dateCreated = _dateCreated;
        dateUploaded = _dateUploaded;
        peerReviewed = _peerReviewed;
        patientFacing = _patientFacing;
        createdBy = _createdBy;
        externalLinkURL = _externalLinkURL;
        fileId = _fileId;
        originalFilename = _originalFilename;
        downloadFilename = _downloadFilename;
        version = _version;
        mimeType = _mimeType;
    }

}


public class StudyAttachedFile
{
    public string? description { get; set; }
    public string? name { get; set; }
    public string? id { get; set; }
    public bool? @public { get; set; }
    
    public StudyAttachedFile()
    {
    }

    public StudyAttachedFile(string? _description, string? _name, string? _id, bool? _public)
    {
        description = _description;
        name = _name;
        id = _id;
        @public = _public;
    }
}


public class StudyContact
{
    public string? forename { get; set; }
    public string? surname { get; set; }
    public string? orcid { get; set; }
    public string? contactType { get; set; }
    public string? address { get; set; }
    public string? city { get; set; }
    public string? country { get; set; }
    public string? email { get; set; }
    
    public StudyContact()
    {
    }

    public StudyContact(string? _forename, string? _surname, string? _orcid, 
                        string? _contactType, string? _address, 
                        string? _city, string? _country, string? _email)
    {
        forename = _forename;
        surname = _surname;
        orcid = _orcid;
        contactType = _contactType;
        address = _address;
        city = _city;
        country = _country;
        email = _email;
    }
}


public class StudySponsor
{
    public string? organisation { get; set; }
    public string? website { get; set; }
    public string? sponsorType { get; set; }
    public string? gridId { get; set; }    
    public string? address { get; set; }
    public string? city { get; set; }
    public string? country { get; set; }

    public StudySponsor()
    {
    }

    public StudySponsor(string? _organisation, string? _website, 
        string? _sponsorType, string? _gridId, 
        string? _city, string? _country)
    {
        organisation = _organisation;
        website = _website;
        sponsorType = _sponsorType;
        gridId = _gridId;
        city = _city;
        country = _country;
    }
}

public class StudyFunder
{
    public string? name { get; set; }
    public string? fundRef { get; set; }

    public StudyFunder()
    {
    }

    public StudyFunder(string? _name, string? _fundRef)
    {
        name = _name;
        fundRef = _fundRef;
    }
}



[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
[XmlRoot(Namespace = "http://www.67bricks.com/isrctn", IsNullable = false)]
public class allTrials
{
    [XmlElement("fullTrial")]
    public FullTrial[]? fullTrials { get; set; }

    [XmlAttribute()]
    public int totalCount { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class FullTrial
{
    public Trial? trial { get; set; }

    [XmlElement("contact")]
    public Contact[]? contact { get; set; }

    [XmlElement("sponsor")]
    public Sponsor[]? sponsor { get; set; }

    [XmlElement("funder")]
    public Funder[]? funder { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Trial
{
    [XmlAttribute()]
    public string? lastUpdated { get; set; }

    [XmlAttribute()]
    public int version { get; set; }

    public Isrctn? isrctn { get; set; }
    public Description? trialDescription { get; set; }
    public ExternalRefs? externalRefs { get; set; }
    public Design? trialDesign { get; set; }
    public Participants? participants { get; set; }
    public Conditions? conditions { get; set; }
    public Interventions? interventions { get; set; }
    public Results? results { get; set; }

    [XmlArrayItem("output", IsNullable = false)]
    public Output[]? outputs { get; set; }

    public Parties? parties { get; set; }

    [XmlArrayItem("attachedFile", IsNullable = false)]
    public AttachedFile[]? attachedFiles { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Isrctn
{
    [XmlAttribute()]
    public string dateAssigned { get; set; }

    [XmlText()]
    public int value { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Description
{
    public bool? acknowledgment { get; set; }
    public string? title { get; set; }
    public string? scientificTitle { get; set; }
    public string? acronym { get; set; }
    public string? studyHypothesis { get; set; }
    public string? plainEnglishSummary { get; set; }
    public string? primaryOutcome { get; set; }
    public string? secondaryOutcome { get; set; }
    public string? trialWebsite { get; set; }
    public string? ethicsApproval { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class ExternalRefs
{
    public string? doi { get; set; }
    public string? eudraCTNumber { get; set; }
    public string? irasNumber { get; set; }
    public string? clinicalTrialsGovNumber { get; set; }
    public string? protocolSerialNumber { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Design
{
    public string? studyDesign { get; set; }
    public string? primaryStudyDesign { get; set; }
    public string? secondaryStudyDesign { get; set; }
    public string? trialSetting { get; set; }
    public string? trialType { get; set; }
    public string? overallStatusOverride { get; set; }
    public string? reasonAbandoned { get; set; }
    public string? overallStartDate { get; set; }
    public string? overallEndDate { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Participants
{
    [XmlArrayItem("country", IsNullable = false)]
    public string[]? recruitmentCountries { get; set; }

    [XmlArrayItem("trialCentre", IsNullable = false)]
    public Centre[]? trialCentres { get; set; }

    public string? participantType { get; set; }
    public string? inclusion { get; set; }
    public string? ageRange { get; set; }
    public string? gender { get; set; }
    public int? targetEnrolment { get; set; }
    public string? totalFinalEnrolment { get; set; }
    public string? totalTarget { get; set; }
    public string? exclusion { get; set; }
    public string? patientInfoSheet { get; set; }
    public string? recruitmentStart { get; set; }
    public string? recruitmentEnd { get; set; }
    public string? recruitmentStatusOverride { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Centre
{
    public string? name { get; set; }
    public string? address { get; set; }
    public string? city { get; set; }
    public string? state { get; set; }
    public string? country { get; set; }
    public string? zip { get; set; }
    public string? id { get; set; }
}

[SerializableAttribute()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Conditions
{
    public Condition? condition { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Condition
{
    public string? description { get; set; }
    public string? diseaseClass1 { get; set; }
    public string? diseaseClass2 { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Interventions
{
    public Intervention? intervention { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Intervention
{
    public string? description { get; set; }
    public string? interventionType { get; set; }
    public string? phase { get; set; }
    public string? drugNames { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Results
{
    public string? publicationPlan { get; set; }
    public string? ipdSharingStatement { get; set; }
    public string? intentToPublish { get; set; }

    [XmlArrayItem("dataPolicy", IsNullable = false)]
    public string[]? dataPolicies { get; set; }

    public string? publicationDetails { get; set; }
    public string? publicationStage { get; set; }
    public bool? biomedRelated { get; set; }
    public string? basicReport { get; set; }
    public string? plainEnglishReport { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Output
{
    public string? description { get; set; }
    public string? productionNotes { get; set; }

    public LocalFile? localFile { get; set; }
    public ExternalLink? externalLink { get; set; }

    [XmlAttribute()]
    public string id { get; set; }

    [XmlAttribute()]
    public string outputType { get; set; }

    [XmlAttribute()]
    public string artefactType { get; set; }

    [XmlAttribute()]
    public string dateCreated { get; set; }

    [XmlAttribute()]
    public string dateUploaded { get; set; }

    [XmlAttribute()]
    public bool peerReviewed { get; set; }

    [XmlAttribute()]
    public bool patientFacing { get; set; }

    [XmlAttribute()]
    public string createdBy { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class LocalFile
{
    [XmlAttribute()]
    public string fileId { get; set; }

    [XmlAttribute()]
    public string originalFilename { get; set; }

    [XmlAttribute()]
    public string downloadFilename { get; set; }

    [XmlAttribute()]
    public string version { get; set; }

    [XmlAttribute()]
    public string mimeType { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class ExternalLink
{
    [XmlAttribute()]
    public string url { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Parties
{
    [XmlElement("funderId")]
    public string[]? funderId { get; set; }

    [XmlElement("contactId")]
    public string[]? contactId { get; set; }

    [XmlElement("sponsorId")]
    public string[]? sponsorId { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class AttachedFile
{
    public string? description { get; set; }
    public string? name { get; set; }
    public string? id { get; set; }
    public bool? @public { get; set; }
}

[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Contact
{
    [XmlAttribute()]
    public string? id { get; set; }

    public string? title { get; set; }
    public string? forename { get; set; }
    public string? surname { get; set; }
    public string? orcid { get; set; }
    public string? contactType { get; set; }
    public ContactDetails? contactDetails { get; set; }
    public string? privacy { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class ContactDetails
{
    public string? address { get; set; }
    public string? city { get; set; }
    public string? state { get; set; }
    public string? country { get; set; }
    public string? zip { get; set; }
    public string? telephone { get; set; }
    public string? email { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Sponsor
{
    [XmlAttribute()]
    public string? id { get; set; }

    public string? organisation { get; set; }
    public string? website { get; set; }
    public string? sponsorType { get; set; }
    public SponsorContactDetails? contactDetails { get; set; }
    public string? privacy { get; set; }
    public string? gridId { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class SponsorContactDetails
{
    public string? address { get; set; }
    public string? city { get; set; }
    public string? state { get; set; }
    public string? country { get; set; }
    public string? zip { get; set; }
    public string? telephone { get; set; }
    public string? email { get; set; }
}


[Serializable()]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
public class Funder
{
    [XmlAttribute()]
    public string? id { get; set; }

    public string? name { get; set; }
    public string? fundRef { get; set; }
}


