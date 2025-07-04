﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Drawing;

namespace WzComparerR2.Config
{
    [SectionName("WcR2.ImageHandler")]
    public sealed class ImageHandlerConfig : ConfigSectionBase<ImageHandlerConfig>
    {
        public ImageHandlerConfig()
        {
            BackgroundColor = Color.White;
            BackgroundType = ImageBackgroundType.Transparent;
            MinMixedAlpha = 0;
            MinDelay = 30;
            OverlayRectColor = Color.Cyan;
            OverlayRectAlpha = 60;
        }

        [ConfigurationProperty("autoSavePictureFolder")]
        public ConfigItem<string> AutoSavePictureFolder
        {
            get { return (ConfigItem<string>)this["autoSavePictureFolder"]; }
            set { this["autoSavePictureFolder"] = value; }
        }

        [ConfigurationProperty("autoSaveEnabled")]
        public ConfigItem<bool> AutoSaveEnabled
        {
            get { return (ConfigItem<bool>)this["autoSaveEnabled"]; }
            set { this["autoSaveEnabled"] = value; }
        }

        [ConfigurationProperty("savePngFramesEnabled")]
        public ConfigItem<bool> SavePngFramesEnabled
        {
            get { return (ConfigItem<bool>)this["savePngFramesEnabled"]; }
            set { this["savePngFramesEnabled"] = value; }
        }

        [ConfigurationProperty("gifEncoder")]
        public ConfigItem<int> GifEncoder
        {
            get { return (ConfigItem<int>)this["gifEncoder"]; }
            set { this["gifEncoder"] = value; }
        }

        [ConfigurationProperty("backgroundType")]
        public ConfigItem<ImageBackgroundType> BackgroundType
        {
            get { return (ConfigItem<ImageBackgroundType>)this["backgroundType"]; }
            set { this["backgroundType"] = value; }
        }

        [ConfigurationProperty("backgroundColor")]
        public ConfigItem<Color> BackgroundColor
        {
            get { return (ConfigItem<Color>)this["backgroundColor"]; }
            set { this["backgroundColor"] = value; }
        }

        [ConfigurationProperty("minMixedAlpha")]
        public ConfigItem<int> MinMixedAlpha
        {
            get { return (ConfigItem<int>)this["minMixedAlpha"]; }
            set { this["minMixedAlpha"] = value; }
        }

        [ConfigurationProperty("minDelay")]
        public ConfigItem<int> MinDelay
        {
            get { return (ConfigItem<int>)this["minDelay"]; }
            set { this["minDelay"] = value; }
        }

        [ConfigurationProperty("mosaicInfo")]
        public MosaicInfo MosaicInfo
        {
            get { return (MosaicInfo)this["mosaicInfo"]; }
            set { this["mosaicInfo"] = value; }
        }

        [ConfigurationProperty("imageNameMethod")]
        public ConfigItem<ImageNameMethod> ImageNameMethod
        {
            get { return (ConfigItem<ImageNameMethod>)this["imageNameMethod"]; }
            set { this["imageNameMethod"] = value; }
        }

        [ConfigurationProperty("paletteOptimized")]
        public ConfigItem<bool> PaletteOptimized
        {
            get { return (ConfigItem<bool>)this["paletteOptimized"]; }
            set { this["paletteOptimized"] = value; }
        }

        [ConfigurationProperty("overlayRectColor")]
        public ConfigItem<Color> OverlayRectColor
        {
            get { return (ConfigItem<Color>)this["overlayRectColor"]; }
            set { this["overlayRectColor"] = value; }
        }

        [ConfigurationProperty("overlayRectAlpha")]
        public ConfigItem<int> OverlayRectAlpha
        {
            get { return (ConfigItem<int>)this["overlayRectAlpha"]; }
            set { this["overlayRectAlpha"] = value; }
        }

        [ConfigurationProperty("ffmpegBinPath")]
        public ConfigItem<string> FFmpegBinPath
        {
            get { return (ConfigItem<string>)this["ffmpegBinPath"]; }
            set { this["ffmpegBinPath"] = value; }
        }

        [ConfigurationProperty("ffmpegArgument")]
        public ConfigItem<string> FFmpegArgument
        {
            get { return (ConfigItem<string>)this["ffmpegArgument"]; }
            set { this["ffmpegArgument"] = value; }
        }

        [ConfigurationProperty("ffmpegOutputFileExtension")]
        public ConfigItem<string> FFmpegOutputFileExtension
        {
            get { return (ConfigItem<string>)this["ffmpegOutputFileExtension"]; }
            set { this["ffmpegOutputFileExtension"] = value; }
        }
    }

    public enum ImageBackgroundType
    {
        Transparent = 0,
        Color = 1,
        Mosaic = 2,
    }

    public enum ImageNameMethod
    {
        Default = 0,
        PathToImage = 1,
        PathToWz = 2
    }
}
