﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    [SectionName("WcR2.CharaSim")]
    public sealed class CharaSimConfig : ConfigSectionBase<CharaSimConfig>
    {
        [ConfigurationProperty("selectedFontIndex")]
        public ConfigItem<int> SelectedFontIndex
        {
            get { return (ConfigItem<int>)this["selectedFontIndex"]; }
            set { this["selectedFontIndex"] = value; }
        }

        [ConfigurationProperty("autoQuickView")]
        public ConfigItem<bool> AutoQuickView
        {
            get { return (ConfigItem<bool>)this["autoQuickView"]; }
            set { this["autoQuickView"] = value; }
        }

        [ConfigurationProperty("skill")]
        public CharaSimSkillConfig Skill
        {
            get { return (CharaSimSkillConfig)this["skill"]; }
        }

        [ConfigurationProperty("gear")]
        public CharaSimGearConfig Gear
        {
            get { return (CharaSimGearConfig)this["gear"]; }
        }

        [ConfigurationProperty("item")]
        public CharaSimItemConfig Item
        {
            get { return (CharaSimItemConfig)this["item"]; }
        }

        [ConfigurationProperty("recipe")]
        public CharaSimRecipeConfig Recipe
        {
            get { return (CharaSimRecipeConfig)this["recipe"]; }
        }

        [ConfigurationProperty("map")]
        public CharaSimMapConfig Map
        {
            get { return (CharaSimMapConfig)this["map"]; }
        }

        [ConfigurationProperty("mob")]
        public CharaSimMobConfig Mob
        {
            get { return (CharaSimMobConfig)this["mob"]; }
        }

        [ConfigurationProperty("npc")]
        public CharaSimNpcConfig Npc
        {
            get { return (CharaSimNpcConfig)this["npc"]; }
        }

        [ConfigurationProperty("quest")]
        public CharaSimQuestConfig Quest
        {
            get { return (CharaSimQuestConfig)this["quest"]; }
        }

        [ConfigurationProperty("PreferredStringCopyMethod")]
        public ConfigItem<int> PreferredStringCopyMethod
        {
            get { return (ConfigItem<int>)this["PreferredStringCopyMethod"]; }
            set { this["PreferredStringCopyMethod"] = value; }
        }

        [ConfigurationProperty("CopyParsedSkillString")]
        public ConfigItem<bool> CopyParsedSkillString
        {
            get { return (ConfigItem<bool>)this["CopyParsedSkillString"]; }
            set { this["CopyParsedSkillString"] = value; }
        }

        [ConfigurationProperty("misc")]
        public CharaSimMiscConfig Misc
        {
            get { return (CharaSimMiscConfig)this["misc"]; }
        }
    }
}
