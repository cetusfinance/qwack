namespace Qwack.Transport.CmeXml
{
    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class FIXML
    {

        private FIXMLMktDataFull[] batchField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("MktDataFull", IsNullable = false)]
        public FIXMLMktDataFull[] Batch
        {
            get => batchField;
            set => batchField = value;
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FIXMLMktDataFull
    {

        private FIXMLMktDataFullInstrmt instrmtField;

        private FIXMLMktDataFullFull[] fullField;

        private FIXMLMktDataFullAttrb[] instrmtExtField;

        private System.DateTime bizDtField;

        private FIXMLMktDataFullUndly undlyField;

        /// <remarks/>
        public FIXMLMktDataFullInstrmt Instrmt
        {
            get => instrmtField;
            set => instrmtField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Full")]
        public FIXMLMktDataFullFull[] Full
        {
            get => fullField;
            set => fullField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Undly")]
        public FIXMLMktDataFullUndly Undly
        {
            get => undlyField;
            set => undlyField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Attrb", IsNullable = false)]
        public FIXMLMktDataFullAttrb[] InstrmtExt
        {
            get => instrmtExtField;
            set => instrmtExtField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "date")]
        public System.DateTime BizDt
        {
            get => bizDtField;
            set => bizDtField = value;
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FIXMLMktDataFullInstrmt
    {

        private FIXMLMktDataFullInstrmtEvnt[] evntField;

        private string symField;

        private string idField;

        private string cFIField;

        private string secTypField;

        private string srcField;

        private string mMYField;

        private System.DateTime matDtField;

        private string exchField;

        private string fnlSettlCcyField;

        private decimal strkPxField;

        private bool putCallField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Evnt")]
        public FIXMLMktDataFullInstrmtEvnt[] Evnt
        {
            get => evntField;
            set => evntField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Sym
        {
            get => symField;
            set => symField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ID
        {
            get => idField;
            set => idField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string CFI
        {
            get => cFIField;
            set => cFIField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SecTyp
        {
            get => secTypField;
            set => secTypField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Src
        {
            get => srcField;
            set => srcField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string MMY
        {
            get => mMYField;
            set => mMYField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "date")]
        public System.DateTime MatDt
        {
            get => matDtField;
            set => matDtField = value;
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal StrkPx
        {
            get => strkPxField;
            set => strkPxField = value;
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool PutCall
        {
            get => putCallField;
            set => putCallField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Exch
        {
            get => exchField;
            set => exchField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FnlSettlCcy
        {
            get => fnlSettlCcyField;
            set => fnlSettlCcyField = value;
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FIXMLMktDataFullUndly
    {
        private string symField;

        private string idField;

        private string cFIField; 

        private string secTypField;

        private string srcField;

        private string mMYField;

        private System.DateTime matDtField;

        private string exchField;

        private string fnlSettlCcyField;

        private decimal strkPxField;

        private bool putCallField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Sym
        {
            get => symField;
            set => symField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ID
        {
            get => idField;
            set => idField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string CFI
        {
            get => cFIField;
            set => cFIField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SecTyp
        {
            get => secTypField;
            set => secTypField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Src
        {
            get => srcField;
            set => srcField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string MMY
        {
            get => mMYField;
            set => mMYField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "date")]
        public System.DateTime MatDt
        {
            get => matDtField;
            set => matDtField = value;
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal StrkPx
        {
            get => strkPxField;
            set => strkPxField = value;
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool PutCall
        {
            get => putCallField;
            set => putCallField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Exch
        {
            get => exchField;
            set => exchField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FnlSettlCcy
        {
            get => fnlSettlCcyField;
            set => fnlSettlCcyField = value;
        }
    }



    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FIXMLMktDataFullInstrmtEvnt
    {

        private byte eventTypField;

        private System.DateTime dtField;

        private uint txtField;

        private bool txtFieldSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte EventTyp
        {
            get => eventTypField;
            set => eventTypField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "date")]
        public System.DateTime Dt
        {
            get => dtField;
            set => dtField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint Txt
        {
            get => txtField;
            set => txtField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool TxtSpecified
        {
            get => txtFieldSpecified;
            set => txtFieldSpecified = value;
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FIXMLMktDataFullFull
    {

        private string typField;

        private decimal pxField;

        private bool pxFieldSpecified;

        private string mktField;

        private decimal discntFctrField;

        private bool discntFctrFieldSpecified;

        private byte openClsSettlFlagField;

        private bool openClsSettlFlagFieldSpecified;

        private uint szField;

        private bool szFieldSpecified;

        private byte pxTypField;

        private bool pxTypFieldSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Typ
        {
            get => typField;
            set => typField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal Px
        {
            get => pxField;
            set => pxField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool PxSpecified
        {
            get => pxFieldSpecified;
            set => pxFieldSpecified = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Mkt
        {
            get => mktField;
            set => mktField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal DiscntFctr
        {
            get => discntFctrField;
            set => discntFctrField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool DiscntFctrSpecified
        {
            get => discntFctrFieldSpecified;
            set => discntFctrFieldSpecified = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte OpenClsSettlFlag
        {
            get => openClsSettlFlagField;
            set => openClsSettlFlagField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool OpenClsSettlFlagSpecified
        {
            get => openClsSettlFlagFieldSpecified;
            set => openClsSettlFlagFieldSpecified = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint Sz
        {
            get => szField;
            set => szField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool SzSpecified
        {
            get => szFieldSpecified;
            set => szFieldSpecified = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte PxTyp
        {
            get => pxTypField;
            set => pxTypField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool PxTypSpecified
        {
            get => pxTypFieldSpecified;
            set => pxTypFieldSpecified = value;
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class FIXMLMktDataFullAttrb
    {

        private byte typField;

        private string valField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte Typ
        {
            get => typField;
            set => typField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Val
        {
            get => valField;
            set => valField = value;
        }
    }
}
