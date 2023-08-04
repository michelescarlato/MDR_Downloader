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
        public bool? hasResults { get; set; }
        //public ResultsSection? resultsSection { get; set; }
        public DocumentSection? documentSection { get; set; }
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
        public ArmsInterventionsModule? armsInterventionsModule { get; set; }
        public EligibilityModule? eligibilityModule { get; set; }
        public ReferencesModule? referencesModule { get; set; }
        public ContactsLocationsModule? contactsLocationsModule { get; set; }
        public OutcomesModule? outcomesModule { get; set; }
    }

    public class IdentificationModule
    {
        public string? nctId { get; set; }
        public OrgStudyIdInfo? orgStudyIdInfo { get; set; }
        public Organization? organization { get; set; }
        public string? briefTitle { get; set; }
        public SecondaryIdInfos[]? secondaryIdInfos { get; set; }
        public string? officialTitle { get; set; }
        public string? acronym { get; set; }
    }

    public class OrgStudyIdInfo
    {
        public string? id { get; set; }
    }

    public class Organization
    {
        public string? fullName { get; set; }
        public string? ctg_class { get; set; }
    }

    public class SecondaryIdInfos
    {
        public string? id { get; set; }
        public string? type { get; set; }
        public string? link { get; set; }
    }

    public class StatusModule
    {
        public string? statusVerifiedDate { get; set; }
        public string? overallStatus { get; set; }
        public ExpandedAccessInfo? expandedAccessInfo { get; set; }
        public StartDateStruct? startDateStruct { get; set; }
        public CompletionDateStruct? completionDateStruct { get; set; }
        public string? studyFirstSubmitDate { get; set; }
        public string? studyFirstSubmitQcDate { get; set; }
        public StudyFirstPostDateStruct? studyFirstPostDateStruct { get; set; }
        public string? lastUpdateSubmitDate { get; set; }
        public LastUpdatePostDateStruct? lastUpdatePostDateStruct { get; set; }
        public string? lastKnownStatus { get; set; }
        public PrimaryCompletionDateStruct? primaryCompletionDateStruct { get; set; }
        public string? resultsFirstSubmitDate { get; set; }
        public string? resultsFirstSubmitQcDate { get; set; }
        public ResultsFirstPostDateStruct? resultsFirstPostDateStruct { get; set; }
    }

    public class ExpandedAccessInfo
    {
        public bool hasExpandedAccess { get; set; }
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
        public LeadSponsor? leadSponsor { get; set; }
        public ResponsibleParty? responsibleParty { get; set; }
        public Collaborators[]? collaborators { get; set; }
    }

    public class LeadSponsor
    {
        public string? name { get; set; }
        public string? ctg_class { get; set; }
    }

    public class ResponsibleParty
    {
        public string? type { get; set; }
    }

    public class Collaborators
    {
        public string? name { get; set; }
        public string? ctg_class { get; set; }
    }

    public class OversightModule
    {
        public bool? oversightHasDmc { get; set; }
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
        public string[]? phases { get; set; }
        public DesignInfo? designInfo { get; set; }
        public EnrollmentInfo? enrollmentInfo { get; set; }
    }

    public class DesignInfo
    {
        public string? allocation { get; set; }
        public string? primaryPurpose { get; set; }
        public string? interventionModel { get; set; }
        public MaskingInfo? maskingInfo { get; set; }
        public string? timePerspective { get; set; }
    }

    public class MaskingInfo
    {
        public string? masking { get; set; }
    }

    public class EnrollmentInfo
    {
        public int? count { get; set; }
        public string? type { get; set; }
    }

    public class ArmsInterventionsModule
    {
        public Interventions[]? interventions { get; set; }
        public ArmGroups[]? armGroups { get; set; }
    }

    public class Interventions
    {
        public string? type { get; set; }
        public string? name { get; set; }
        public object[]? interventionMappedName { get; set; }
        public string? description { get; set; }
        public string[]? armGroupLabels { get; set; }
    }

    public class ArmGroups
    {
        public string? label { get; set; }
        public string? type { get; set; }
        public string? description { get; set; }
        public string[]? interventionNames { get; set; }
    }

    public class EligibilityModule
    {
        public string? eligibilityCriteria { get; set; }
        public bool healthyVolunteers { get; set; }
        public string? sex { get; set; }
        public string? minimumAge { get; set; }
        public string[]? stdAges { get; set; }
        public string? maximumAge { get; set; }
    }

    public class ReferencesModule
    {
        public References[]? references { get; set; }
        public SeeAlsoLinks[]? seeAlsoLinks { get; set; }
    }

    public class References
    {
        public string? pmid { get; set; }
        public string? type { get; set; }
        public string? citation { get; set; }
    }

    public class SeeAlsoLinks
    {
        public string? label { get; set; }
        public string? url { get; set; }
    }

    public class ContactsLocationsModule
    {
        public Locations[]? locations { get; set; }
        public Locations_nested[]? locations_nested { get; set; }
        public OverallOfficials[]? overallOfficials { get; set; }
    }

    public class Locations
    {
        public string? facility { get; set; }
        public string? city { get; set; }
        public string? country { get; set; }
        public GeoPoint? geoPoint { get; set; }
        public string? state { get; set; }
    }

    public class GeoPoint
    {
        public double? lat { get; set; }
        public double? lon { get; set; }
    }

    public class Locations_nested
    {
        public string? facility { get; set; }
        public string? city { get; set; }
        public string? country { get; set; }
        public GeoPoint1? geoPoint { get; set; }
        public string? state { get; set; }
    }

    public class GeoPoint1
    {
        public double? lat { get; set; }
        public double? lon { get; set; }
    }

    public class OverallOfficials
    {
        public string? name { get; set; }
        public string? affiliation { get; set; }
        public string? role { get; set; }
    }

    public class OutcomesModule
    {
        public PrimaryOutcomes[]? primaryOutcomes { get; set; }
    }

    public class PrimaryOutcomes
    {
        public string? measure { get; set; }
        public string? description { get; set; }
        public string? timeFrame { get; set; }
    }

    public class DerivedSection
    {
        //public MiscInfoModule? miscInfoModule { get; set; }
        public ConditionBrowseModule? conditionBrowseModule { get; set; }
        public InterventionBrowseModule? interventionBrowseModule { get; set; }
    }
/*
    public class MiscInfoModule
    {
        public string? versionHolder { get; set; }
        public ModelPredictions? modelPredictions { get; set; }
    }

    public class ModelPredictions
    {
        public BmiLimits? bmiLimits { get; set; }
    }

    public class BmiLimits
    {
        public double? minBmi { get; set; }
        public double? maxBmi { get; set; }
    }
*/
    public class ConditionBrowseModule
    {
        public Meshes[]? meshes { get; set; }
        //public Ancestors[]? ancestors { get; set; }
        //public BrowseLeaves[]? browseLeaves { get; set; }
        //public BrowseBranches[]? browseBranches { get; set; }
    }

    public class Meshes
    {
        public string? id { get; set; }
        public string? term { get; set; }
    }
/*
    public class Ancestors
    {
        public string? id { get; set; }
        public string? term { get; set; }
    }

    public class BrowseLeaves
    {
        public string? id { get; set; }
        public string? name { get; set; }
        public string? relevance { get; set; }
        public string? asFound { get; set; }
    }

    public class BrowseBranches
    {
        public string? abbrev { get; set; }
        public string? name { get; set; }
    }
*/
    public class InterventionBrowseModule
    {
        public Meshes1[]? meshes { get; set; }
        //public Ancestors1[]? ancestors { get; set; }
        //public BrowseLeaves1[]? browseLeaves { get; set; }
        //public BrowseBranches1[]? browseBranches { get; set; }
    }

    public class Meshes1
    {
        public string? id { get; set; }
        public string? term { get; set; }
    }
/*
    public class Ancestors1
    {
        public string? id { get; set; }
        public string? term { get; set; }
    }

    public class BrowseLeaves1
    {
        public string? id { get; set; }
        public string? name { get; set; }
        public string? asFound { get; set; }
        public string? relevance { get; set; }
    }

    public class BrowseBranches1
    {
        public string? abbrev { get; set; }
        public string? name { get; set; }
    }
*/
/*
    public class ResultsSection
    {
        public ParticipantFlowModule? participantFlowModule { get; set; }
        public BaselineCharacteristicsModule? baselineCharacteristicsModule { get; set; }
        public OutcomeMeasuresModule? outcomeMeasuresModule { get; set; }
        public AdverseEventsModule? adverseEventsModule { get; set; }
        public MoreInfoModule? moreInfoModule { get; set; }
    }

    public class ParticipantFlowModule
    {
        public Groups[]? groups { get; set; }
        public Periods[]? periods { get; set; }
    }

    public class Groups
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
    }

    public class Periods
    {
        public string? title { get; set; }
        public Milestones[]? milestones { get; set; }
    }

    public class Milestones
    {
        public string? type { get; set; }
        public Achievements[]? achievements { get; set; }
    }

    public class Achievements
    {
        public string? groupId { get; set; }
        public string? numSubjects { get; set; }
        public string? comment { get; set; }
    }

    public class BaselineCharacteristicsModule
    {
        public string? populationDescription { get; set; }
        public Groups1[]?groups { get; set; }
        public Denoms[]? denoms { get; set; }
        public Measures[]? measures { get; set; }
    }

    public class Groups1
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
    }

    public class Denoms
    {
        public string? units { get; set; }
        public Counts[]? counts { get; set; }
    }

    public class Counts
    {
        public string? groupId { get; set; }
        public string? value { get; set; }
    }

    public class Measures
    {
        public string? title { get; set; }
        public string? paramType { get; set; }
        public string? dispersionType { get; set; }
        public string? unitOfMeasure { get; set; }
        public Classes[]? classes { get; set; }
    }

    public class Classes
    {
        public Categories[]? categories { get; set; }
        public string? title { get; set; }
    }

    public class Categories
    {
        public Measurements[]? measurements { get; set; }
        public string? title { get; set; }
    }

    public class Measurements
    {
        public string? groupId { get; set; }
        public string? value { get; set; }
        public string? spread { get; set; }
    }

    public class OutcomeMeasuresModule
    {
        public OutcomeMeasures[]? outcomeMeasures { get; set; }
    }

    public class OutcomeMeasures
    {
        public string? type { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public string? populationDescription { get; set; }
        public string? reportingStatus { get; set; }
        public string? paramType { get; set; }
        public string? unitOfMeasure { get; set; }
        public string? timeFrame { get; set; }
        public Groups2[]? groups { get; set; }
        public Denoms1[]? denoms { get; set; }
        public Classes1[]? classes { get; set; }
    }

    public class Groups2
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
    }

    public class Denoms1
    {
        public string? units { get; set; }
        public Counts1[]? counts { get; set; }
    }

    public class Counts1
    {
        public string? groupId { get; set; }
        public string? value { get; set; }
    }

    public class Classes1
    {
        public string? title { get; set; }
        public Categories1[]? categories { get; set; }
    }

    public class Categories1
    {
        public Measurements1[]? measurements { get; set; }
    }

    public class Measurements1
    {
        public string? groupId { get; set; }
        public string? value { get; set; }
    }

    public class AdverseEventsModule
    {
        public string? frequencyThreshold { get; set; }
        public string? timeFrame { get; set; }
        public string? description { get; set; }
        public EventGroups[]? eventGroups { get; set; }
        public SeriousEvents[]? seriousEvents { get; set; }
        public OtherEvents[]? otherEvents { get; set; }
    }

    public class EventGroups
    {
        public string? id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public int? seriousNumAffected { get; set; }
        public int? seriousNumAtRisk { get; set; }
        public int? otherNumAffected { get; set; }
        public int? otherNumAtRisk { get; set; }
    }

    public class SeriousEvents
    {
        public string? term { get; set; }
        public string? organSystem { get; set; }
        public string? assessmentType { get; set; }
        public string? notes { get; set; }
        public Stats[]? stats { get; set; }
    }

    public class Stats
    {
        public string? groupId { get; set; }
        public int? numAffected { get; set; }
        public int? numAtRisk { get; set; }
    }

    public class OtherEvents
    {
        public string? term { get; set; }
        public string? organSystem { get; set; }
        public string? assessmentType { get; set; }
        public string? notes { get; set; }
        public Stats1[]? stats { get; set; }
    }

    public class Stats1
    {
        public string? groupId { get; set; }
        public int? numAffected { get; set; }
        public int? numAtRisk { get; set; }
    }
*/
    public class MoreInfoModule
    {
        public CertainAgreement? certainAgreement { get; set; }
        public PointOfContact? pointOfContact { get; set; }
    }

    public class CertainAgreement
    {
        public bool? piSponsorEmployee { get; set; }
        public bool? restrictiveAgreement { get; set; }
    }

    public class PointOfContact
    {
        public string? title { get; set; }
        public string? organization { get; set; }
        public string? email { get; set; }
        public string? phone { get; set; }
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

