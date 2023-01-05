using System.Xml.Serialization;
using System.ComponentModel;

namespace MDR_Downloader.SourceSpecific.isrctn
{
    /*
    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    [XmlRootAttribute(Namespace = "http://www.67bricks.com/isrctn", IsNullable = false)]
    public partial class xxxallTrials
    {
        private allTrialsFullTrial[] fullTrialField;

        private ushort totalCountField;

        /// <remarks/>
        [XmlElementAttribute("fullTrial")]
        public allTrialsFullTrial[] fullTrial
        {
            get
            {
                return this.fullTrialField;
            }
            set
            {
                this.fullTrialField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public ushort totalCount
        {
            get
            {
                return this.totalCountField;
            }
            set
            {
                this.totalCountField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrial
    {

        private allTrialsFullTrialTrial trialField;

        private allTrialsFullTrialContact[] contactField;

        private allTrialsFullTrialSponsor sponsorField;

        private allTrialsFullTrialFunder[] funderField;

        /// <remarks/>
        public allTrialsFullTrialTrial trial
        {
            get
            {
                return this.trialField;
            }
            set
            {
                this.trialField = value;
            }
        }

        /// <remarks/>
        [XmlElementAttribute("contact")]
        public allTrialsFullTrialContact[] contact
        {
            get
            {
                return this.contactField;
            }
            set
            {
                this.contactField = value;
            }
        }

        /// <remarks/>
        public allTrialsFullTrialSponsor sponsor
        {
            get
            {
                return this.sponsorField;
            }
            set
            {
                this.sponsorField = value;
            }
        }

        /// <remarks/>
        [XmlElementAttribute("funder")]
        public allTrialsFullTrialFunder[] funder
        {
            get
            {
                return this.funderField;
            }
            set
            {
                this.funderField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrial
    {

        private allTrialsFullTrialTrialIsrctn isrctnField;

        private allTrialsFullTrialTrialTrialDescription trialDescriptionField;

        private allTrialsFullTrialTrialExternalRefs externalRefsField;

        private allTrialsFullTrialTrialTrialDesign trialDesignField;

        private allTrialsFullTrialTrialParticipants participantsField;

        private allTrialsFullTrialTrialConditions conditionsField;

        private allTrialsFullTrialTrialInterventions interventionsField;

        private allTrialsFullTrialTrialResults resultsField;

        private allTrialsFullTrialTrialOutput[] outputsField;

        private allTrialsFullTrialTrialParties partiesField;

        private allTrialsFullTrialTrialAttachedFile[] attachedFilesField;

        private System.DateTime lastUpdatedField;

        private byte versionField;

        /// <remarks/>
        public allTrialsFullTrialTrialIsrctn isrctn
        {
            get
            {
                return this.isrctnField;
            }
            set
            {
                this.isrctnField = value;
            }
        }

        /// <remarks/>
        public allTrialsFullTrialTrialTrialDescription trialDescription
        {
            get
            {
                return this.trialDescriptionField;
            }
            set
            {
                this.trialDescriptionField = value;
            }
        }

        /// <remarks/>
        public allTrialsFullTrialTrialExternalRefs externalRefs
        {
            get
            {
                return this.externalRefsField;
            }
            set
            {
                this.externalRefsField = value;
            }
        }

        /// <remarks/>
        public allTrialsFullTrialTrialTrialDesign trialDesign
        {
            get
            {
                return this.trialDesignField;
            }
            set
            {
                this.trialDesignField = value;
            }
        }

        /// <remarks/>
        public allTrialsFullTrialTrialParticipants participants
        {
            get
            {
                return this.participantsField;
            }
            set
            {
                this.participantsField = value;
            }
        }

        /// <remarks/>
        public allTrialsFullTrialTrialConditions conditions
        {
            get
            {
                return this.conditionsField;
            }
            set
            {
                this.conditionsField = value;
            }
        }

        /// <remarks/>
        public allTrialsFullTrialTrialInterventions interventions
        {
            get
            {
                return this.interventionsField;
            }
            set
            {
                this.interventionsField = value;
            }
        }

        /// <remarks/>
        public allTrialsFullTrialTrialResults results
        {
            get
            {
                return this.resultsField;
            }
            set
            {
                this.resultsField = value;
            }
        }

        /// <remarks/>
        [XmlArrayItemAttribute("output", IsNullable = false)]
        public allTrialsFullTrialTrialOutput[] outputs
        {
            get
            {
                return this.outputsField;
            }
            set
            {
                this.outputsField = value;
            }
        }

        /// <remarks/>
        public allTrialsFullTrialTrialParties parties
        {
            get
            {
                return this.partiesField;
            }
            set
            {
                this.partiesField = value;
            }
        }

        /// <remarks/>
        [XmlArrayItemAttribute("attachedFile", IsNullable = false)]
        public allTrialsFullTrialTrialAttachedFile[] attachedFiles
        {
            get
            {
                return this.attachedFilesField;
            }
            set
            {
                this.attachedFilesField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public System.DateTime lastUpdated
        {
            get
            {
                return this.lastUpdatedField;
            }
            set
            {
                this.lastUpdatedField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public byte version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialIsrctn
    {

        private System.DateTime dateAssignedField;

        private uint valueField;

        /// <remarks/>
        [XmlAttributeAttribute()]
        public System.DateTime dateAssigned
        {
            get
            {
                return this.dateAssignedField;
            }
            set
            {
                this.dateAssignedField = value;
            }
        }

        /// <remarks/>
        [XmlTextAttribute()]
        public uint Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialTrialDescription
    {

        private bool acknowledgmentField;

        private string titleField;

        private string scientificTitleField;

        private string acronymField;

        private string studyHypothesisField;

        private string plainEnglishSummaryField;

        private string primaryOutcomeField;

        private string secondaryOutcomeField;

        private string trialWebsiteField;

        private string ethicsApprovalField;

        /// <remarks/>
        public bool acknowledgment
        {
            get
            {
                return this.acknowledgmentField;
            }
            set
            {
                this.acknowledgmentField = value;
            }
        }

        /// <remarks/>
        public string title
        {
            get
            {
                return this.titleField;
            }
            set
            {
                this.titleField = value;
            }
        }

        /// <remarks/>
        public string scientificTitle
        {
            get
            {
                return this.scientificTitleField;
            }
            set
            {
                this.scientificTitleField = value;
            }
        }

        /// <remarks/>
        public string acronym
        {
            get
            {
                return this.acronymField;
            }
            set
            {
                this.acronymField = value;
            }
        }

        /// <remarks/>
        public string studyHypothesis
        {
            get
            {
                return this.studyHypothesisField;
            }
            set
            {
                this.studyHypothesisField = value;
            }
        }

        /// <remarks/>
        public string plainEnglishSummary
        {
            get
            {
                return this.plainEnglishSummaryField;
            }
            set
            {
                this.plainEnglishSummaryField = value;
            }
        }

        /// <remarks/>
        public string primaryOutcome
        {
            get
            {
                return this.primaryOutcomeField;
            }
            set
            {
                this.primaryOutcomeField = value;
            }
        }

        /// <remarks/>
        public string secondaryOutcome
        {
            get
            {
                return this.secondaryOutcomeField;
            }
            set
            {
                this.secondaryOutcomeField = value;
            }
        }

        /// <remarks/>
        public string trialWebsite
        {
            get
            {
                return this.trialWebsiteField;
            }
            set
            {
                this.trialWebsiteField = value;
            }
        }

        /// <remarks/>
        public string ethicsApproval
        {
            get
            {
                return this.ethicsApprovalField;
            }
            set
            {
                this.ethicsApprovalField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialExternalRefs
    {

        private string doiField;

        private string eudraCTNumberField;

        private string irasNumberField;

        private string clinicalTrialsGovNumberField;

        private string protocolSerialNumberField;

        /// <remarks/>
        public string doi
        {
            get
            {
                return this.doiField;
            }
            set
            {
                this.doiField = value;
            }
        }

        /// <remarks/>
        public string eudraCTNumber
        {
            get
            {
                return this.eudraCTNumberField;
            }
            set
            {
                this.eudraCTNumberField = value;
            }
        }

        /// <remarks/>
        public string irasNumber
        {
            get
            {
                return this.irasNumberField;
            }
            set
            {
                this.irasNumberField = value;
            }
        }

        /// <remarks/>
        public string clinicalTrialsGovNumber
        {
            get
            {
                return this.clinicalTrialsGovNumberField;
            }
            set
            {
                this.clinicalTrialsGovNumberField = value;
            }
        }

        /// <remarks/>
        public string protocolSerialNumber
        {
            get
            {
                return this.protocolSerialNumberField;
            }
            set
            {
                this.protocolSerialNumberField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialTrialDesign
    {

        private string studyDesignField;

        private string primaryStudyDesignField;

        private string secondaryStudyDesignField;

        private string trialSettingField;

        private string trialTypeField;

        private object overallStatusOverrideField;

        private object reasonAbandonedField;

        private System.DateTime overallStartDateField;

        private System.DateTime overallEndDateField;

        /// <remarks/>
        public string studyDesign
        {
            get
            {
                return this.studyDesignField;
            }
            set
            {
                this.studyDesignField = value;
            }
        }

        /// <remarks/>
        public string primaryStudyDesign
        {
            get
            {
                return this.primaryStudyDesignField;
            }
            set
            {
                this.primaryStudyDesignField = value;
            }
        }

        /// <remarks/>
        public string secondaryStudyDesign
        {
            get
            {
                return this.secondaryStudyDesignField;
            }
            set
            {
                this.secondaryStudyDesignField = value;
            }
        }

        /// <remarks/>
        public string trialSetting
        {
            get
            {
                return this.trialSettingField;
            }
            set
            {
                this.trialSettingField = value;
            }
        }

        /// <remarks/>
        public string trialType
        {
            get
            {
                return this.trialTypeField;
            }
            set
            {
                this.trialTypeField = value;
            }
        }

        /// <remarks/>
        public object overallStatusOverride
        {
            get
            {
                return this.overallStatusOverrideField;
            }
            set
            {
                this.overallStatusOverrideField = value;
            }
        }

        /// <remarks/>
        public object reasonAbandoned
        {
            get
            {
                return this.reasonAbandonedField;
            }
            set
            {
                this.reasonAbandonedField = value;
            }
        }

        /// <remarks/>
        public System.DateTime overallStartDate
        {
            get
            {
                return this.overallStartDateField;
            }
            set
            {
                this.overallStartDateField = value;
            }
        }

        /// <remarks/>
        public System.DateTime overallEndDate
        {
            get
            {
                return this.overallEndDateField;
            }
            set
            {
                this.overallEndDateField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialParticipants
    {

        private string[] recruitmentCountriesField;

        private allTrialsFullTrialTrialParticipantsTrialCentre[] trialCentresField;

        private string participantTypeField;

        private string inclusionField;

        private string ageRangeField;

        private string genderField;

        private ushort targetEnrolmentField;

        private string totalFinalEnrolmentField;

        private string totalTargetField;

        private string exclusionField;

        private string patientInfoSheetField;

        private System.DateTime recruitmentStartField;

        private System.DateTime recruitmentEndField;

        private object recruitmentStatusOverrideField;

        /// <remarks/>
        [XmlArrayItemAttribute("country", IsNullable = false)]
        public string[] recruitmentCountries
        {
            get
            {
                return this.recruitmentCountriesField;
            }
            set
            {
                this.recruitmentCountriesField = value;
            }
        }

        /// <remarks/>
        [XmlArrayItemAttribute("trialCentre", IsNullable = false)]
        public allTrialsFullTrialTrialParticipantsTrialCentre[] trialCentres
        {
            get
            {
                return this.trialCentresField;
            }
            set
            {
                this.trialCentresField = value;
            }
        }

        /// <remarks/>
        public string participantType
        {
            get
            {
                return this.participantTypeField;
            }
            set
            {
                this.participantTypeField = value;
            }
        }

        /// <remarks/>
        public string inclusion
        {
            get
            {
                return this.inclusionField;
            }
            set
            {
                this.inclusionField = value;
            }
        }

        /// <remarks/>
        public string ageRange
        {
            get
            {
                return this.ageRangeField;
            }
            set
            {
                this.ageRangeField = value;
            }
        }

        /// <remarks/>
        public string gender
        {
            get
            {
                return this.genderField;
            }
            set
            {
                this.genderField = value;
            }
        }

        /// <remarks/>
        public ushort targetEnrolment
        {
            get
            {
                return this.targetEnrolmentField;
            }
            set
            {
                this.targetEnrolmentField = value;
            }
        }

        /// <remarks/>
        public string totalFinalEnrolment
        {
            get
            {
                return this.totalFinalEnrolmentField;
            }
            set
            {
                this.totalFinalEnrolmentField = value;
            }
        }

        /// <remarks/>
        public string totalTarget
        {
            get
            {
                return this.totalTargetField;
            }
            set
            {
                this.totalTargetField = value;
            }
        }

        /// <remarks/>
        public string exclusion
        {
            get
            {
                return this.exclusionField;
            }
            set
            {
                this.exclusionField = value;
            }
        }

        /// <remarks/>
        public string patientInfoSheet
        {
            get
            {
                return this.patientInfoSheetField;
            }
            set
            {
                this.patientInfoSheetField = value;
            }
        }

        /// <remarks/>
        public System.DateTime recruitmentStart
        {
            get
            {
                return this.recruitmentStartField;
            }
            set
            {
                this.recruitmentStartField = value;
            }
        }

        /// <remarks/>
        public System.DateTime recruitmentEnd
        {
            get
            {
                return this.recruitmentEndField;
            }
            set
            {
                this.recruitmentEndField = value;
            }
        }

        /// <remarks/>
        public object recruitmentStatusOverride
        {
            get
            {
                return this.recruitmentStatusOverrideField;
            }
            set
            {
                this.recruitmentStatusOverrideField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialParticipantsTrialCentre
    {

        private string nameField;

        private string addressField;

        private string cityField;

        private object stateField;

        private string countryField;

        private string zipField;

        private string idField;

        /// <remarks/>
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        public string address
        {
            get
            {
                return this.addressField;
            }
            set
            {
                this.addressField = value;
            }
        }

        /// <remarks/>
        public string city
        {
            get
            {
                return this.cityField;
            }
            set
            {
                this.cityField = value;
            }
        }

        /// <remarks/>
        public object state
        {
            get
            {
                return this.stateField;
            }
            set
            {
                this.stateField = value;
            }
        }

        /// <remarks/>
        public string country
        {
            get
            {
                return this.countryField;
            }
            set
            {
                this.countryField = value;
            }
        }

        /// <remarks/>
        public string zip
        {
            get
            {
                return this.zipField;
            }
            set
            {
                this.zipField = value;
            }
        }

        /// <remarks/>
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialConditions
    {

        private allTrialsFullTrialTrialConditionsCondition conditionField;

        /// <remarks/>
        public allTrialsFullTrialTrialConditionsCondition condition
        {
            get
            {
                return this.conditionField;
            }
            set
            {
                this.conditionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialConditionsCondition
    {

        private string descriptionField;

        private string diseaseClass1Field;

        private object diseaseClass2Field;

        /// <remarks/>
        public string description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        public string diseaseClass1
        {
            get
            {
                return this.diseaseClass1Field;
            }
            set
            {
                this.diseaseClass1Field = value;
            }
        }

        /// <remarks/>
        public object diseaseClass2
        {
            get
            {
                return this.diseaseClass2Field;
            }
            set
            {
                this.diseaseClass2Field = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialInterventions
    {

        private allTrialsFullTrialTrialInterventionsIntervention interventionField;

        /// <remarks/>
        public allTrialsFullTrialTrialInterventionsIntervention intervention
        {
            get
            {
                return this.interventionField;
            }
            set
            {
                this.interventionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialInterventionsIntervention
    {

        private string descriptionField;

        private string interventionTypeField;

        private string phaseField;

        private string drugNamesField;

        /// <remarks/>
        public string description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        public string interventionType
        {
            get
            {
                return this.interventionTypeField;
            }
            set
            {
                this.interventionTypeField = value;
            }
        }

        /// <remarks/>
        public string phase
        {
            get
            {
                return this.phaseField;
            }
            set
            {
                this.phaseField = value;
            }
        }

        /// <remarks/>
        public string drugNames
        {
            get
            {
                return this.drugNamesField;
            }
            set
            {
                this.drugNamesField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialResults
    {

        private string publicationPlanField;

        private string ipdSharingStatementField;

        private System.DateTime intentToPublishField;

        private string[] dataPoliciesField;

        private object publicationDetailsField;

        private string publicationStageField;

        private bool biomedRelatedField;

        private object basicReportField;

        private object plainEnglishReportField;

        /// <remarks/>
        public string publicationPlan
        {
            get
            {
                return this.publicationPlanField;
            }
            set
            {
                this.publicationPlanField = value;
            }
        }

        /// <remarks/>
        public string ipdSharingStatement
        {
            get
            {
                return this.ipdSharingStatementField;
            }
            set
            {
                this.ipdSharingStatementField = value;
            }
        }

        /// <remarks/>
        public System.DateTime intentToPublish
        {
            get
            {
                return this.intentToPublishField;
            }
            set
            {
                this.intentToPublishField = value;
            }
        }

        /// <remarks/>
        [XmlArrayItemAttribute("dataPolicy", IsNullable = false)]
        public string[] dataPolicies
        {
            get
            {
                return this.dataPoliciesField;
            }
            set
            {
                this.dataPoliciesField = value;
            }
        }

        /// <remarks/>
        public object publicationDetails
        {
            get
            {
                return this.publicationDetailsField;
            }
            set
            {
                this.publicationDetailsField = value;
            }
        }

        /// <remarks/>
        public string publicationStage
        {
            get
            {
                return this.publicationStageField;
            }
            set
            {
                this.publicationStageField = value;
            }
        }

        /// <remarks/>
        public bool biomedRelated
        {
            get
            {
                return this.biomedRelatedField;
            }
            set
            {
                this.biomedRelatedField = value;
            }
        }

        /// <remarks/>
        public object basicReport
        {
            get
            {
                return this.basicReportField;
            }
            set
            {
                this.basicReportField = value;
            }
        }

        /// <remarks/>
        public object plainEnglishReport
        {
            get
            {
                return this.plainEnglishReportField;
            }
            set
            {
                this.plainEnglishReportField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialOutput
    {

        private allTrialsFullTrialTrialOutputLocalFile localFileField;

        private object descriptionField;

        private object productionNotesField;

        private string idField;

        private string outputTypeField;

        private string artefactTypeField;

        private string dateCreatedField;

        private System.DateTime dateUploadedField;

        private bool peerReviewedField;

        private bool patientFacingField;

        private string createdByField;

        /// <remarks/>
        public allTrialsFullTrialTrialOutputLocalFile localFile
        {
            get
            {
                return this.localFileField;
            }
            set
            {
                this.localFileField = value;
            }
        }

        /// <remarks/>
        public object description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        public object productionNotes
        {
            get
            {
                return this.productionNotesField;
            }
            set
            {
                this.productionNotesField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string outputType
        {
            get
            {
                return this.outputTypeField;
            }
            set
            {
                this.outputTypeField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string artefactType
        {
            get
            {
                return this.artefactTypeField;
            }
            set
            {
                this.artefactTypeField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string dateCreated
        {
            get
            {
                return this.dateCreatedField;
            }
            set
            {
                this.dateCreatedField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public System.DateTime dateUploaded
        {
            get
            {
                return this.dateUploadedField;
            }
            set
            {
                this.dateUploadedField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public bool peerReviewed
        {
            get
            {
                return this.peerReviewedField;
            }
            set
            {
                this.peerReviewedField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public bool patientFacing
        {
            get
            {
                return this.patientFacingField;
            }
            set
            {
                this.patientFacingField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string createdBy
        {
            get
            {
                return this.createdByField;
            }
            set
            {
                this.createdByField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialOutputLocalFile
    {

        private string fileIdField;

        private string originalFilenameField;

        private string downloadFilenameField;

        private string versionField;

        private string mimeTypeField;

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string fileId
        {
            get
            {
                return this.fileIdField;
            }
            set
            {
                this.fileIdField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string originalFilename
        {
            get
            {
                return this.originalFilenameField;
            }
            set
            {
                this.originalFilenameField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string downloadFilename
        {
            get
            {
                return this.downloadFilenameField;
            }
            set
            {
                this.downloadFilenameField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string mimeType
        {
            get
            {
                return this.mimeTypeField;
            }
            set
            {
                this.mimeTypeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialParties
    {

        private string[] funderIdField;

        private string[] contactIdField;

        private string sponsorIdField;

        /// <remarks/>
        [XmlElementAttribute("funderId")]
        public string[] funderId
        {
            get
            {
                return this.funderIdField;
            }
            set
            {
                this.funderIdField = value;
            }
        }

        /// <remarks/>
        [XmlElementAttribute("contactId")]
        public string[] contactId
        {
            get
            {
                return this.contactIdField;
            }
            set
            {
                this.contactIdField = value;
            }
        }

        /// <remarks/>
        public string sponsorId
        {
            get
            {
                return this.sponsorIdField;
            }
            set
            {
                this.sponsorIdField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialTrialAttachedFile
    {

        private object descriptionField;

        private string nameField;

        private string idField;

        private bool publicField;

        /// <remarks/>
        public object description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        public bool @public
        {
            get
            {
                return this.publicField;
            }
            set
            {
                this.publicField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialContact
    {

        private string titleField;

        private string forenameField;

        private string surnameField;

        private string orcidField;

        private string contactTypeField;

        private allTrialsFullTrialContactContactDetails contactDetailsField;

        private string privacyField;

        private string idField;

        /// <remarks/>
        public string title
        {
            get
            {
                return this.titleField;
            }
            set
            {
                this.titleField = value;
            }
        }

        /// <remarks/>
        public string forename
        {
            get
            {
                return this.forenameField;
            }
            set
            {
                this.forenameField = value;
            }
        }

        /// <remarks/>
        public string surname
        {
            get
            {
                return this.surnameField;
            }
            set
            {
                this.surnameField = value;
            }
        }

        /// <remarks/>
        public string orcid
        {
            get
            {
                return this.orcidField;
            }
            set
            {
                this.orcidField = value;
            }
        }

        /// <remarks/>
        public string contactType
        {
            get
            {
                return this.contactTypeField;
            }
            set
            {
                this.contactTypeField = value;
            }
        }

        /// <remarks/>
        public allTrialsFullTrialContactContactDetails contactDetails
        {
            get
            {
                return this.contactDetailsField;
            }
            set
            {
                this.contactDetailsField = value;
            }
        }

        /// <remarks/>
        public string privacy
        {
            get
            {
                return this.privacyField;
            }
            set
            {
                this.privacyField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialContactContactDetails
    {

        private string addressField;

        private string cityField;

        private object stateField;

        private string countryField;

        private string zipField;

        private string telephoneField;

        private string emailField;

        /// <remarks/>
        public string address
        {
            get
            {
                return this.addressField;
            }
            set
            {
                this.addressField = value;
            }
        }

        /// <remarks/>
        public string city
        {
            get
            {
                return this.cityField;
            }
            set
            {
                this.cityField = value;
            }
        }

        /// <remarks/>
        public object state
        {
            get
            {
                return this.stateField;
            }
            set
            {
                this.stateField = value;
            }
        }

        /// <remarks/>
        public string country
        {
            get
            {
                return this.countryField;
            }
            set
            {
                this.countryField = value;
            }
        }

        /// <remarks/>
        public string zip
        {
            get
            {
                return this.zipField;
            }
            set
            {
                this.zipField = value;
            }
        }

        /// <remarks/>
        public string telephone
        {
            get
            {
                return this.telephoneField;
            }
            set
            {
                this.telephoneField = value;
            }
        }

        /// <remarks/>
        public string email
        {
            get
            {
                return this.emailField;
            }
            set
            {
                this.emailField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialSponsor
    {
        private string? organisation { get; set; }
        private string? website { get; set; }
        private string? sponsorType { get; set; }
        private allTrialsFullTrialSponsorContactDetails? contactDetails { get; set; }
        private string? privacy { get; set; }
        private string? gridId { get; set; }
        private string? id { get; set; }
    }


    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialSponsorContactDetails
    {
        private string? address { get; set; }
        private string? city { get; set; }
        private string? state { get; set; }
        private string? country { get; set; }
        private string? zip { get; set; }
        private string? telephone { get; set; }
        private string? email { get; set; }
    }


    /// <remarks/>
    [System.SerializableAttribute()]
    [DesignerCategoryAttribute("code")]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class allTrialsFullTrialFunder
    {
        public string? name { get; set; }
        public string? fundRef { get; set; }

        [XmlAttributeAttribute()]
        public string? id { get; set; }
    }
    */



    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    [XmlRoot(Namespace = "http://www.67bricks.com/isrctn", IsNullable = false)]
    public class allTrials
    {
        [XmlElement("fullTrial")]
        public FullTrial[]? fullTrials { get; set; }

        [XmlAttribute()]
        public uint totalCount { get; set; }
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
        public DateTime? lastUpdated { get; set; }

        [XmlAttribute()]
        public byte? version { get; set; }

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
        public DateTime? dateAssigned { get; set; }

        [XmlText()]
        public uint? value { get; set; }
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
        public object? overallStatusOverride { get; set; }
        public object? reasonAbandoned { get; set; }
        public DateTime? overallStartDate { get; set; }
        public DateTime? overallEndDate { get; set; }
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
        public uint? targetEnrolment { get; set; }
        public string? totalFinalEnrolment { get; set; }
        public string? totalTarget { get; set; }
        public string? exclusion { get; set; }
        public string? patientInfoSheet { get; set; }
        public DateTime? recruitmentStart { get; set; }
        public DateTime? recruitmentEnd { get; set; }
        public object? recruitmentStatusOverride { get; set; }
    }


    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public class Centre
    {
        public string? name { get; set; }
        public string? address { get; set; }
        public string? city { get; set; }
        public object? state { get; set; }
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
        public object? diseaseClass2 { get; set; }
    }

    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public partial class Interventions
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
        public DateTime? intentToPublish { get; set; }

        [XmlArrayItem("dataPolicy", IsNullable = false)]
        public string[]? dataPolicies { get; set; }

        public object? publicationDetails { get; set; }
        public string? publicationStage { get; set; }
        public bool? biomedRelated { get; set; }
        public object? basicReport { get; set; }
        public object? plainEnglishReport { get; set; }
    }


    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public class Output
    {
        public LocalFile? localFile { get; set; }
        public object? description { get; set; }
        public object? productionNotes { get; set; }

        [XmlAttribute()]
        public string? id { get; set; }

        [XmlAttribute()]
        public string? outputType { get; set; }

        [XmlAttribute()]
        public string? artefactType { get; set; }

        [XmlAttribute()]
        public string? dateCreated { get; set; }

        [XmlAttribute()]
        public DateTime? dateUploaded { get; set; }

        [XmlAttribute()]
        public bool? peerReviewed { get; set; }

        [XmlAttribute()]
        public bool? patientFacing { get; set; }

        [XmlAttribute()]
        public string? createdBy { get; set; }
    }


    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public class LocalFile
    {
        [XmlAttribute()]
        public string? fileId { get; set; }

        [XmlAttribute()]
        public string? originalFilename { get; set; }

        [XmlAttribute()]
        public string? downloadFilename { get; set; }

        [XmlAttribute()]
        public string? version { get; set; }

        [XmlAttribute()]
        public string? mimeType { get; set; }
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
        public string? sponsorId { get; set; }
    }


    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "http://www.67bricks.com/isrctn")]
    public class AttachedFile
    {
        public object? description { get; set; }
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
        public object? state { get; set; }
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

}
