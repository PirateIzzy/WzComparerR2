﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    public class CharaSimGearConfig : ConfigurationElement
    {
        [ConfigurationProperty("showID", DefaultValue = true)]
        public bool ShowID
        {
            get { return (bool)this["showID"]; }
            set { this["showID"] = value; }
        }

        [ConfigurationProperty("showWeaponSpeed", DefaultValue = false)]
        public bool ShowWeaponSpeed
        {
            get { return (bool)this["showWeaponSpeed"]; }
            set { this["showWeaponSpeed"] = value; }
        }

        [ConfigurationProperty("showLevelOrSealed", DefaultValue = true)]
        public bool ShowLevelOrSealed
        {
            get { return (bool)this["showLevelOrSealed"]; }
            set { this["showLevelOrSealed"] = value; }
        }

        [ConfigurationProperty("showMedalTag", DefaultValue = false)]
        public bool ShowMedalTag
        {
            get { return (bool)this["showMedalTag"]; }
            set { this["showMedalTag"] = value; }
        }

        [ConfigurationProperty("maxStar25", DefaultValue = false)]
        public bool MaxStar25
        {
            get { return (bool)this["maxStar25"]; }
            set { this["maxStar25"] = value; }
        }
    }
}
