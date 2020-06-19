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
            get
            {
                return batchField;
            }
            set
            {
                batchField = value;
            }
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

        /// <remarks/>
        public FIXMLMktDataFullInstrmt Instrmt
        {
            get
            {
                return instrmtField;
            }
            set
            {
                instrmtField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Full")]
        public FIXMLMktDataFullFull[] Full
        {
            get
            {
                return fullField;
            }
            set
            {
                fullField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Attrb", IsNullable = false)]
        public FIXMLMktDataFullAttrb[] InstrmtExt
        {
            get
            {
                return instrmtExtField;
            }
            set
            {
                instrmtExtField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "date")]
        public System.DateTime BizDt
        {
            get
            {
                return bizDtField;
            }
            set
            {
                bizDtField = value;
            }
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

        private uint mMYField;

        private System.DateTime matDtField;

        private string exchField;

        private string fnlSettlCcyField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Evnt")]
        public FIXMLMktDataFullInstrmtEvnt[] Evnt
        {
            get
            {
                return evntField;
            }
            set
            {
                evntField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Sym
        {
            get
            {
                return symField;
            }
            set
            {
                symField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ID
        {
            get
            {
                return idField;
            }
            set
            {
                idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string CFI
        {
            get
            {
                return cFIField;
            }
            set
            {
                cFIField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SecTyp
        {
            get
            {
                return secTypField;
            }
            set
            {
                secTypField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Src
        {
            get
            {
                return srcField;
            }
            set
            {
                srcField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint MMY
        {
            get
            {
                return mMYField;
            }
            set
            {
                mMYField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "date")]
        public System.DateTime MatDt
        {
            get
            {
                return matDtField;
            }
            set
            {
                matDtField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Exch
        {
            get
            {
                return exchField;
            }
            set
            {
                exchField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FnlSettlCcy
        {
            get
            {
                return fnlSettlCcyField;
            }
            set
            {
                fnlSettlCcyField = value;
            }
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
            get
            {
                return eventTypField;
            }
            set
            {
                eventTypField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "date")]
        public System.DateTime Dt
        {
            get
            {
                return dtField;
            }
            set
            {
                dtField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint Txt
        {
            get
            {
                return txtField;
            }
            set
            {
                txtField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool TxtSpecified
        {
            get
            {
                return txtFieldSpecified;
            }
            set
            {
                txtFieldSpecified = value;
            }
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

        private byte szField;

        private bool szFieldSpecified;

        private byte pxTypField;

        private bool pxTypFieldSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Typ
        {
            get
            {
                return typField;
            }
            set
            {
                typField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal Px
        {
            get
            {
                return pxField;
            }
            set
            {
                pxField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool PxSpecified
        {
            get
            {
                return pxFieldSpecified;
            }
            set
            {
                pxFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Mkt
        {
            get
            {
                return mktField;
            }
            set
            {
                mktField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal DiscntFctr
        {
            get
            {
                return discntFctrField;
            }
            set
            {
                discntFctrField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool DiscntFctrSpecified
        {
            get
            {
                return discntFctrFieldSpecified;
            }
            set
            {
                discntFctrFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte OpenClsSettlFlag
        {
            get
            {
                return openClsSettlFlagField;
            }
            set
            {
                openClsSettlFlagField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool OpenClsSettlFlagSpecified
        {
            get
            {
                return openClsSettlFlagFieldSpecified;
            }
            set
            {
                openClsSettlFlagFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte Sz
        {
            get
            {
                return szField;
            }
            set
            {
                szField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool SzSpecified
        {
            get
            {
                return szFieldSpecified;
            }
            set
            {
                szFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte PxTyp
        {
            get
            {
                return pxTypField;
            }
            set
            {
                pxTypField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool PxTypSpecified
        {
            get
            {
                return pxTypFieldSpecified;
            }
            set
            {
                pxTypFieldSpecified = value;
            }
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
            get
            {
                return typField;
            }
            set
            {
                typField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Val
        {
            get
            {
                return valField;
            }
            set
            {
                valField = value;
            }
        }
    }
}
