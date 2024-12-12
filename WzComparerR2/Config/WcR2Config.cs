﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Drawing;
using WzComparerR2.Patcher;

namespace WzComparerR2.Config
{
    [SectionName("WcR2")]
    public sealed class WcR2Config : ConfigSectionBase<WcR2Config>
    {
        public WcR2Config()
        {
            this.MainStyle = DevComponents.DotNetBar.eStyle.Metro;
            this.MainStyleColor = Color.DimGray;
            this.SortWzOnOpened = true;
            this.AutoDetectExtFiles = true;
            this.WzVersionVerifyMode = WzLib.WzVersionVerifyMode.Fast;
            this.PreferredLayout = 0;
            this.DesiredLanguage = "en";
            this.MozhiBackend = "https://mozhi.aryak.me";
            this.DetectCurrency = "auto";
            this.DesiredCurrency = "none";
        }

        /// <summary>
        /// 获取最近打开的文档列表。
        /// </summary>
        [ConfigurationProperty("recentDocuments")]
        [ConfigurationCollection(typeof(ConfigArrayList<string>.ItemElement))]
        public ConfigArrayList<string> RecentDocuments
        {
            get { return (ConfigArrayList<string>)this["recentDocuments"]; }
        }

        /// <summary>
        /// 获取或设置主窗体界面样式。
        /// </summary>
        [ConfigurationProperty("mainStyle")]
        public ConfigItem<DevComponents.DotNetBar.eStyle> MainStyle
        {
            get { return (ConfigItem<DevComponents.DotNetBar.eStyle>)this["mainStyle"]; }
            set { this["mainStyle"] = value; }
        }

        /// <summary>
        /// 获取或设置主窗体界面主题色。
        /// </summary>
        [ConfigurationProperty("mainStyleColor")]
        public ConfigItem<Color> MainStyleColor
        {
            get { return (ConfigItem<Color>)this["mainStyleColor"]; }
            set { this["mainStyleColor"] = value; }
        }

        /// <summary>
        /// Mozhi Backend Configuration
        /// </summary>
        [ConfigurationProperty("MozhiBackend")]
        [ConfigurationCollection(typeof(ConfigArrayList<string>.ItemElement))]
        public ConfigItem<string> MozhiBackend
        {
            get { return (ConfigItem<string>)this["MozhiBackend"]; }
            set { this["MozhiBackend"] = value; }
        }

        /// <summary>
        /// Desired Language Configuration
        /// </summary>
        [ConfigurationProperty("DesiredLanguage")]
        [ConfigurationCollection(typeof(ConfigArrayList<string>.ItemElement))]
        public ConfigItem<string> DesiredLanguage
        {
            get { return (ConfigItem<string>)this["DesiredLanguage"]; }
            set { this["DesiredLanguage"] = value; }
        }

        /// <summary>
        /// Preferred Translate Engine Configuration
        /// </summary>
        [ConfigurationProperty("PreferredTranslateEngine")]
        public ConfigItem<int> PreferredTranslateEngine
        {
            get { return (ConfigItem<int>)this["PreferredTranslateEngine"]; }
            set { this["PreferredTranslateEngine"] = value; }
        }

        /// <summary>
        /// Preferred Layout Configuration
        /// </summary>
        [ConfigurationProperty("PreferredLayout")]
        public ConfigItem<int> PreferredLayout
        {
            get { return (ConfigItem<int>)this["PreferredLayout"]; }
            set { this["PreferredLayout"] = value; }
        }

        /// <summary>
        /// Detect Currency Configuration
        /// </summary>
        [ConfigurationProperty("DetectCurrency")]
        [ConfigurationCollection(typeof(ConfigArrayList<string>.ItemElement))]
        public ConfigItem<string> DetectCurrency
        {
            get { return (ConfigItem<string>)this["DetectCurrency"]; }
            set { this["DetectCurrency"] = value; }
        }

        /// <summary>
        /// Desired Currency Configuration
        /// </summary>
        [ConfigurationProperty("DesiredCurrency")]
        [ConfigurationCollection(typeof(ConfigArrayList<string>.ItemElement))]
        public ConfigItem<string> DesiredCurrency
        {
            get { return (ConfigItem<string>)this["DesiredCurrency"]; }
            set { this["DesiredCurrency"] = value; }
        }

        /// <summary>
        /// NXSecretKey Configuration
        /// </summary>
        [ConfigurationProperty("nxSecretKey")]
        [ConfigurationCollection(typeof(ConfigArrayList<string>.ItemElement))]
        public ConfigItem<string> NxSecretKey
        {
            get { return (ConfigItem<string>)this["nxSecretKey"]; }
            set { this["nxSecretKey"] = value; }
        }

        /// <summary>
        /// 获取或设置Wz对比报告默认输出文件夹。
        /// </summary>
        [ConfigurationProperty("comparerOutputFolder")]
        public ConfigItem<string> ComparerOutputFolder
        {
            get { return (ConfigItem<string>)this["comparerOutputFolder"]; }
            set { this["comparerOutputFolder"] = value; }
        }

        /// <summary>
        /// 获取或设置一个值，指示Wz文件加载后是否自动排序。
        /// </summary>
        [ConfigurationProperty("sortWzOnOpened")]
        public ConfigItem<bool> SortWzOnOpened
        {
            get { return (ConfigItem<bool>)this["sortWzOnOpened"]; }
            set { this["sortWzOnOpened"] = value; }
        }

        /// <summary>
        /// 获取或设置一个值，指示Wz文件加载后是否自动排序。
        /// </summary>
        [ConfigurationProperty("sortWzByImgID")]
        public ConfigItem<bool> SortWzByImgID
        {
            get { return (ConfigItem<bool>)this["sortWzByImgID"]; }
            set { this["sortWzByImgID"] = value; }
        }

        /// <summary>
        /// 获取或设置一个值，指示Wz加载中对于ansi字符串的编码。
        /// </summary>
        [ConfigurationProperty("wzEncoding")]
        public ConfigItem<int> WzEncoding
        {
            get { return (ConfigItem<int>)this["wzEncoding"]; }
            set { this["wzEncoding"] = value; }
        }

        /// <summary>
        /// 获取或设置一个值，指示加载Base.wz时是否自动检测扩展wz文件（如Map2、Mob2）。
        /// </summary>
        [ConfigurationProperty("autoDetectExtFiles")]
        public ConfigItem<bool> AutoDetectExtFiles
        {
            get { return (ConfigItem<bool>)this["autoDetectExtFiles"]; }
            set { this["autoDetectExtFiles"] = value; }
        }

        /// <summary>
        /// 获取或设置一个值，指示读取wz是否跳过img检测。
        /// </summary>
        [ConfigurationProperty("imgCheckDisabled")]
        public ConfigItem<bool> ImgCheckDisabled
        {
            get { return (ConfigItem<bool>)this["imgCheckDisabled"]; }
            set { this["imgCheckDisabled"] = value; }
        }

        /// <summary>
        /// 获取或设置一个值，指示读取wz是否跳过img检测。
        /// </summary>
        [ConfigurationProperty("wzVersionVerifyMode")]
        public ConfigItem<WzLib.WzVersionVerifyMode> WzVersionVerifyMode
        {
            get { return (ConfigItem<WzLib.WzVersionVerifyMode>)this["wzVersionVerifyMode"]; }
            set { this["wzVersionVerifyMode"] = value; }
        }

        [ConfigurationProperty("patcherSettings")]
        [ConfigurationCollection(typeof(PatcherSetting), CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap)]
        public PatcherSettingCollection PatcherSettings
        {
            get { return (PatcherSettingCollection)this["patcherSettings"]; }
        }
    }
}