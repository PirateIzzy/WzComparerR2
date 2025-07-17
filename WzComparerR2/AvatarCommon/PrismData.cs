﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.AvatarCommon
{
    public class PrismData
    {
        public PrismData()
        {
            this.Type = 0;
            this.Hue = 0;
            this.Saturation = 100;
            this.Brightness = 100;
        }

        public PrismData(int type, int hue, int saturation, int brightness)
        {
            this.Type = type;
            this.Hue = hue;
            this.Saturation = saturation;
            this.Brightness = brightness;
        }

        public int Type;
        public int Hue;
        public int Saturation;
        public int Brightness;

        public bool Valid
        {
            get { return this.Hue != 0 || this.Saturation != 100 || this.Brightness != 100; }
        }

        public void Clear()
        {
            this.Type = 0;
            this.Hue = 0;
            this.Saturation = 100;
            this.Brightness = 100;
        }

        public void Set(int type, int hue, int saturation, int brightness)
        {
            this.Type = type;
            this.Hue = hue;
            this.Saturation = saturation;
            this.Brightness = brightness;
        }

        public string GetColorType()
        {
            if (!this.Valid) return null;

            switch (this.Type)
            {
                case 0:
                    return "全色系";
                case 1:
                    return "赤色系";
                case 2:
                    return "黄色系";
                case 3:
                    return "緑色系";
                case 4:
                    return "ターコイズ色系";
                case 5:
                    return "青色系";
                case 6:
                    return "紫色系";
                default:
                    return null;
            }
        }
    }
}
