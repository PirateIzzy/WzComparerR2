using System;
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
                    return "All Color Schemes";
                case 1:
                    return "Red Color Schemes";
                case 2:
                    return "Yellow Color Schemes";
                case 3:
                    return "Green Color Schemes";
                case 4:
                    return "Turquoise Color Schemes";
                case 5:
                    return "Blue Color Schemes";
                case 6:
                    return "Purple Color Schemes";
                default:
                    return null;
            }
        }
    }
}
