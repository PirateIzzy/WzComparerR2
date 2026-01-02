using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    public class CharaSimMiscConfig : ConfigurationElement
    {
        [ConfigurationProperty("enable22AniStyle", DefaultValue = false)]
        public bool Enable22AniStyle
        {
            get { return (bool)this["enable22AniStyle"]; }
            set { this["enable22AniStyle"] = value; }
        }

        [ConfigurationProperty("mseaMode", DefaultValue = false)]
        public bool MseaMode
        {
            get { return (bool)this["mseaMode"]; }
            set { this["mseaMode"] = value; }
        }
        
        [ConfigurationProperty("enableWorldArchive", DefaultValue = true)]
        public bool EnableWorldArchive
        {
            get { return (bool)this["enableWorldArchive"]; }
            set { this["enableWorldArchive"] = value; }
        }
    }
}
