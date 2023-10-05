namespace MDR_Downloader.ctg;

public class CTGRootobject
{
    public JSONStudy[]? studies { get; set; }
    public string? nextPageToken { get; set; }
    public int? totalCount { get; set; }
}

public class JSONStudy
{
    public ProtocolSection? protocolSection { get; set; }
    public DerivedSection? derivedSection { get; set; }
    public DocumentSection? documentSection { get; set; }
    public bool? hasResults { get; set; }
}

public class ProtocolSection
{
    public IdentificationModule? identificationModule { get; set; }
    public StatusModule? statusModule { get; set; }
    public SponsorCollaboratorsModule? sponsorCollaboratorsModule { get; set; }
    public OversightModule? oversightModule { get; set; }
    public DescriptionModule? descriptionModule { get; set; }
    public ConditionsModule? conditionsModule { get; set; }
    public DesignModule? designModule { get; set; }
    public EligibilityModule? eligibilityModule { get; set; }
    public ContactsLocationsModule? contactsLocationsModule { get; set; }    
    public ReferencesModule? referencesModule { get; set; }
    public IPDSharingStatementModule? ipdSharingStatementModule { get; set; }
}

public class IdentificationModule
{
    public string? nctId { get; set; }
    public string[]? nctIdAliases { get; set; }   
    public OrgStudyIdInfo? orgStudyIdInfo { get; set; }    
    public SecondaryIdInfos[]? secondaryIdInfos { get; set; }    
    public string? briefTitle { get; set; }
    public string? officialTitle { get; set; }
    public string? acronym { get; set; }
    public Organization? organization { get; set; }
}

public class OrgStudyIdInfo
{
    public string? id { get; set; }
    public string? type { get; set; }
    public string? link { get; set; }
}

public class SecondaryIdInfos
{
    public string? id { get; set; }
    public string? type { get; set; }
    public string? domain { get; set; }    
    public string? link { get; set; }
}


public class Organization
{
    public string? fullName { get; set; }
    public string? ctg_class { get; set; }
}

public class StatusModule
{
    public string? statusVerifiedDate { get; set; }
    public string? overallStatus { get; set; }
    public string? lastKnownStatus { get; set; }
    public string? whyStopped { get; set; }
    public ExpandedAccessInfo? expandedAccessInfo { get; set; }
    
    public StartDateStruct? startDateStruct { get; set; }
    public PrimaryCompletionDateStruct? primaryCompletionDateStruct { get; set; }
    public CompletionDateStruct? completionDateStruct { get; set; }
    public string? studyFirstSubmitDate { get; set; }
    public StudyFirstPostDateStruct? studyFirstPostDateStruct { get; set; }
    public string? resultsFirstSubmitDate { get; set; }
    public ResultsFirstPostDateStruct? resultsFirstPostDateStruct { get; set; }
    public string? lastUpdateSubmitDate { get; set; }
    public LastUpdatePostDateStruct? lastUpdatePostDateStruct { get; set; }
}

public class ExpandedAccessInfo
{
    public bool hasExpandedAccess { get; set; }
    public string? nctId { get; set; }
    public string? statusForNctId { get; set; }
}

public class StartDateStruct
{
    public string? date { get; set; }
    public string? type { get; set; }
}

public class CompletionDateStruct
{
    public string? date { get; set; }
    public string? type { get; set; }
}

public class StudyFirstPostDateStruct
{
    public string? date { get; set; }
    public string? type { get; set; }
}

public class LastUpdatePostDateStruct
{
    public string? date { get; set; }
    public string? type { get; set; }
}

public class PrimaryCompletionDateStruct
{
    public string? date { get; set; }
    public string? type { get; set; }
}

public class ResultsFirstPostDateStruct
{
    public string? date { get; set; }
    public string? type { get; set; }
}

public class SponsorCollaboratorsModule
{
    public ResponsibleParty? responsibleParty { get; set; }        
    public LeadSponsor? leadSponsor { get; set; }
    public Collaborator[]? collaborators { get; set; }
}

public class LeadSponsor
{
    public string? name { get; set; }
    public string? ctg_class { get; set; }
}

public class ResponsibleParty
{
    public string? type { get; set; }
    public string? investigatorFullName { get; set; }
    public string? investigatorTitle { get; set; }
    public string? investigatorAffiliation { get; set; }
    public string? oldNameTitle { get; set; }
    public string? oldOrganization { get; set; }

}

public class Collaborator
{
    public string? name { get; set; }
    public string? ctg_class { get; set; }
}

public class OversightModule
{
    public bool? oversightHasDmc { get; set; }
    public bool? isFdaRegulatedDrug { get; set; }
    public bool? isFdaRegulatedDevice { get; set; }
    public bool? isUnapprovedDevice { get; set; }
    public bool? isPpsd { get; set; }
}

public class DescriptionModule
{
    public string? briefSummary { get; set; }
    public string? detailedDescription { get; set; }
}

public class ConditionsModule
{
    public string[]? conditions { get; set; }
    public string[]? keywords { get; set; }
}

public class DesignModule
{
    public string? studyType { get; set; }
    public bool? patientRegistry { get; set; }      
    public string[]? phases { get; set; }        
    public DesignInfo? designInfo { get; set; }
    public EnrollmentInfo? enrollmentInfo { get; set; }
    public Biospec? bioSpec { get; set; }
}

public class DesignInfo
{
    public string? allocation { get; set; }
    public string? interventionModel { get; set; }    
    public string? interventionModelDescription { get; set; }    
    public string? primaryPurpose { get; set; }
    public string? observationalModel { get; set; }
    public string? timePerspective { get; set; }
    public MaskingInfo? maskingInfo { get; set; }
}

public class MaskingInfo
{
    public string? masking { get; set; }
    public string? maskingDescription { get; set; }
    public string[]? whoMasked { get; set; }
    public int? numDesignWhoMaskeds  { get; set; }
}

public class EnrollmentInfo
{
    public int? count { get; set; }
    public string? type { get; set; }
}

public class Biospec
{
    public string? retention { get; set; }
    public string? description { get; set; }
}

public class EligibilityModule
{
    public string? eligibilityCriteria { get; set; }
    public bool healthyVolunteers { get; set; }
    public string? sex { get; set; }
    public bool genderBased { get; set; }
    public string? genderDescription { get; set; }
    public string? minimumAge { get; set; }
    public string[]? stdAges { get; set; }
    public string? maximumAge { get; set; }
    public string? studyPopulation { get; set; }
    public string? samplingMethod { get; set; }
}

public class ContactsLocationsModule
{
    public Centralcontact[]? centralContacts { get; set; }
    public OverallOfficial[]? overallOfficials { get; set; }
    public Location[]? locations { get; set; }
}

public class Centralcontact
{
    public string? name { get; set; }
    public string? role { get; set; }
    public string? email { get; set; }
}

public class OverallOfficial
{
    public string? name { get; set; }
    public string? affiliation { get; set; }
    public string? role { get; set; }
}

public class Location
{
    public string? facility { get; set; }
    public string? city { get; set; }
    public string? country { get; set; }
    public GeoPoint? geoPoint { get; set; }
    public string? state { get; set; }
    public string? status { get; set; } 
}

public class GeoPoint
{
    public double? lat { get; set; }
    public double? lon { get; set; }
}

public class ReferencesModule
{
    public References[]? references { get; set; }
    public SeeAlsoLinks[]? seeAlsoLinks { get; set; }
    public AvailIpd[]? availIpds { get; set; }
}

public class References
{
    public string? pmid { get; set; }
    public string? type { get; set; }
    public string? citation { get; set; }
    public Retraction[]? retractions { get; set; }
}

public class Retraction
{
    public string? pmid { get; set; }
    public string? source { get; set; }
}

public class SeeAlsoLinks
{
    public string? label { get; set; }
    public string? url { get; set; }
}

public class AvailIpd
{
    public string? id { get; set; }
    public string? type { get; set; }
    public string? url { get; set; }
    public string? comment { get; set; }
}

public class IPDSharingStatementModule
{
    public string? ipdSharing { get; set; }
    public string? description { get; set; }
    public string[]? infoTypes { get; set; }
    public string? timeFrame { get; set; }
    public string? accessCriteria { get; set; }
    public string? url { get; set; }
}

public class DocumentSection
{
    public LargeDocumentModule? largeDocumentModule { get; set; }
}

public class LargeDocumentModule
{
    public LargeDocs[]? largeDocs { get; set; }
}

public class LargeDocs
{
    public string? typeAbbrev { get; set; }
    public bool? hasProtocol { get; set; }
    public bool? hasSap { get; set; }
    public bool? hasIcf { get; set; }
    public string? label { get; set; }
    public string? date { get; set; }
    public string? uploadDate { get; set; }
    public string? filename { get; set; }
    public int? size { get; set; }
}

public class DerivedSection
{
    public ConditionBrowseModule? conditionBrowseModule { get; set; }
    public InterventionBrowseModule? interventionBrowseModule { get; set; }
}

public class ConditionBrowseModule
{
    public Mesh[]? meshes { get; set; }
}

public class InterventionBrowseModule
{
    public Mesh[]? meshes { get; set; }
}

public class Mesh
{
    public string? id { get; set; }
    public string? term { get; set; }
}
    
