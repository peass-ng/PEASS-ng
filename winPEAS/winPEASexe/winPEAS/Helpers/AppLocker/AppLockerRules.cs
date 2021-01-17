using System.Collections.Generic;

namespace winPEAS.Helpers.AppLocker
{
    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class AppLockerPolicy
    {

        private AppLockerPolicyRuleCollection[] ruleCollectionField;

        private byte versionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("RuleCollection")]
        public AppLockerPolicyRuleCollection[] RuleCollection
        {
            get
            {
                return this.ruleCollectionField;
            }
            set
            {
                this.ruleCollectionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte Version
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
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class AppLockerPolicyRuleCollection
    {

        private List<AppLockerPolicyRuleCollectionFileHashRule> fileHashRuleField;

        private List<AppLockerPolicyRuleCollectionFilePathRule> filePathRuleField;

        private List<AppLockerPolicyRuleCollectionFilePublisherRule> filePublisherRuleField;

        private string typeField;

        private string enforcementModeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("FileHashRule")]
        public List<AppLockerPolicyRuleCollectionFileHashRule> FileHashRule
        {
            get
            {
                return this.fileHashRuleField;
            }
            set
            {
                this.fileHashRuleField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("FilePathRule")]
        public List<AppLockerPolicyRuleCollectionFilePathRule> FilePathRule
        {
            get
            {
                return this.filePathRuleField;
            }
            set
            {
                this.filePathRuleField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("FilePublisherRule")]
        public List<AppLockerPolicyRuleCollectionFilePublisherRule> FilePublisherRule
        {
            get
            {
                return this.filePublisherRuleField;
            }
            set
            {
                this.filePublisherRuleField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string EnforcementMode
        {
            get
            {
                return this.enforcementModeField;
            }
            set
            {
                this.enforcementModeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class AppLockerPolicyRuleCollectionFileHashRule
    {

        private AppLockerPolicyRuleCollectionFileHashRuleFileHashCondition[] conditionsField;

        private string idField;

        private string nameField;

        private string descriptionField;

        private string userOrGroupSidField;

        private string actionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("FileHashCondition", IsNullable = false)]
        public AppLockerPolicyRuleCollectionFileHashRuleFileHashCondition[] Conditions
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Id
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Description
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string UserOrGroupSid
        {
            get
            {
                return this.userOrGroupSidField;
            }
            set
            {
                this.userOrGroupSidField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class AppLockerPolicyRuleCollectionFileHashRuleFileHashCondition
    {

        private AppLockerPolicyRuleCollectionFileHashRuleFileHashConditionFileHash fileHashField;

        /// <remarks/>
        public AppLockerPolicyRuleCollectionFileHashRuleFileHashConditionFileHash FileHash
        {
            get
            {
                return this.fileHashField;
            }
            set
            {
                this.fileHashField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class AppLockerPolicyRuleCollectionFileHashRuleFileHashConditionFileHash
    {

        private string typeField;

        private string dataField;

        private string sourceFileNameField;

        private uint sourceFileLengthField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Data
        {
            get
            {
                return this.dataField;
            }
            set
            {
                this.dataField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SourceFileName
        {
            get
            {
                return this.sourceFileNameField;
            }
            set
            {
                this.sourceFileNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint SourceFileLength
        {
            get
            {
                return this.sourceFileLengthField;
            }
            set
            {
                this.sourceFileLengthField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class AppLockerPolicyRuleCollectionFilePathRule
    {

        private AppLockerPolicyRuleCollectionFilePathRuleFilePathCondition[] conditionsField;

        private string idField;

        private string nameField;

        private string descriptionField;

        private string userOrGroupSidField;

        private string actionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("FilePathCondition", IsNullable = false)]
        public AppLockerPolicyRuleCollectionFilePathRuleFilePathCondition[] Conditions
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Id
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Description
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string UserOrGroupSid
        {
            get
            {
                return this.userOrGroupSidField;
            }
            set
            {
                this.userOrGroupSidField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class AppLockerPolicyRuleCollectionFilePathRuleFilePathCondition
    {

        private string pathField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Path
        {
            get
            {
                return this.pathField;
            }
            set
            {
                this.pathField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class AppLockerPolicyRuleCollectionFilePublisherRule
    {

        private AppLockerPolicyRuleCollectionFilePublisherRuleFilePublisherCondition[] conditionsField;

        private string idField;

        private string nameField;

        private string descriptionField;

        private string userOrGroupSidField;

        private string actionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("FilePublisherCondition", IsNullable = false)]
        public AppLockerPolicyRuleCollectionFilePublisherRuleFilePublisherCondition[] Conditions
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Id
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Description
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string UserOrGroupSid
        {
            get
            {
                return this.userOrGroupSidField;
            }
            set
            {
                this.userOrGroupSidField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Action
        {
            get
            {
                return this.actionField;
            }
            set
            {
                this.actionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class AppLockerPolicyRuleCollectionFilePublisherRuleFilePublisherCondition
    {

        private AppLockerPolicyRuleCollectionFilePublisherRuleFilePublisherConditionBinaryVersionRange binaryVersionRangeField;

        private string publisherNameField;

        private string productNameField;

        private string binaryNameField;

        /// <remarks/>
        public AppLockerPolicyRuleCollectionFilePublisherRuleFilePublisherConditionBinaryVersionRange BinaryVersionRange
        {
            get
            {
                return this.binaryVersionRangeField;
            }
            set
            {
                this.binaryVersionRangeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string PublisherName
        {
            get
            {
                return this.publisherNameField;
            }
            set
            {
                this.publisherNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ProductName
        {
            get
            {
                return this.productNameField;
            }
            set
            {
                this.productNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string BinaryName
        {
            get
            {
                return this.binaryNameField;
            }
            set
            {
                this.binaryNameField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class AppLockerPolicyRuleCollectionFilePublisherRuleFilePublisherConditionBinaryVersionRange
    {

        private string lowSectionField;

        private string highSectionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string LowSection
        {
            get
            {
                return this.lowSectionField;
            }
            set
            {
                this.lowSectionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string HighSection
        {
            get
            {
                return this.highSectionField;
            }
            set
            {
                this.highSectionField = value;
            }
        }
    }
}
