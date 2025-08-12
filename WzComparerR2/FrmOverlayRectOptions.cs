﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevComponents.Editors;
using Microsoft.Xna.Framework;
using WzComparerR2.Controls;
using WzComparerR2.Config;
using DevComponents.DotNetBar.Controls;

namespace WzComparerR2
{
    public partial class FrmOverlayRectOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmOverlayRectOptions() : this(0, 0, null, false)
        {

        }

        public FrmOverlayRectOptions(int s, int e, ImageHandlerConfig config, bool enableAutoArea)
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new System.Drawing.Font(new System.Drawing.FontFamily("Microsoft Sans Serif"), 8f);
#endif
            this.txtLeft.Value = 0;
            this.txtRight.Value = 0;
            this.txtTop.Value = 0;
            this.txtBottom.Value = 0;
            this.txtRadius.Value = 0;
            this.txtStart.Value = 0;
            this.txtStart.MaxValue = e;
            this.txtEnd.Value = e;
            this.txtEnd.MaxValue = e;
            this.txtSpeedX.Value = 0;
            this.txtSpeedY.Value = 0;
            this.txtGoX.Value = 0;
            this.txtGoY.Value = 0;
            this.txtAlphaDst.Value = 0;
            this.txtAlphaStart.Value = 0;
            this.txtAlphaEnd.Value = e;
            this.txtAlphaEnd.MaxValue = e;

            this.colorPickerButton1.SelectedColor = config.OverlayRectColor;
            this.txtAlpha.Value = config.OverlayRectAlpha;

            this.txtStart.ValueChanged += TxtStart_ValueChanged;
            this.txtEnd.ValueChanged += TxtEnd_ValueChanged;
            this.chkAutoArea.CheckedChanged += ChkAutoArea_CheckedChanged;
            this.chkIsCircle.CheckedChanged += this.ChkIsCircle_CheckedChanged;
            this.chkAlphaGradation.CheckedChanged += ChkAlphaGradation_CheckedChanged;

            if (!enableAutoArea)
            {
                this.chkAutoArea.Checked = false;
                this.chkAutoArea.Enabled = false;
            }
        }

        public OverlayOptions GetValues(ImageHandlerConfig config)
        {
            var s = this.txtStart.ValueObject as int? ?? 0;
            var e = this.txtEnd.ValueObject as int? ?? 0;
            var a = this.txtAlpha.ValueObject as int? ?? 60;
            var a_s = this.txtAlphaStart.ValueObject as int? ?? 0;
            var a_e = this.txtAlphaEnd.ValueObject as int? ?? (s <= e ? e : s);
            if (a_s < s) a_s = s;
            if (a_e > e) a_e = e;

            var ret = new OverlayOptions()
            {
                AniStart = s,
                AniEnd = s <= e ? e : s,

                SpeedX = this.txtSpeedX.ValueObject as int? ?? 0,
                SpeedY = this.txtSpeedY.ValueObject as int? ?? 0,
                GoX = this.txtGoX.ValueObject as int? ?? 0,
                GoY = this.txtGoY.ValueObject as int? ?? 0,

                RectAutoArea = this.chkAutoArea.Checked,
                RectLT = new Point(this.txtLeft.ValueObject as int? ?? 0, this.txtTop.ValueObject as int? ?? 0),
                RectRB = new Point(this.txtRight.ValueObject as int? ?? 0, this.txtBottom.ValueObject as int? ?? 0),

                RectType = this.chkIsCircle.Checked ? 1 : 0,
                RectRadius = this.txtRadius.ValueObject as int? ?? 0,
                RectAlpha = a,

                RectGradation = this.chkAlphaGradation.Checked,
                RectAlphaDst = this.txtAlphaDst.ValueObject as int? ?? a,
                RectAlphaStart = a_s,
                RectAlphaEnd = a_e
            };

            config.OverlayRectColor = this.colorPickerButton1.SelectedColor;
            config.OverlayRectAlpha = ret.RectAlpha;

            return ret;
        }

        private void TxtStart_ValueChanged(object sender, EventArgs e)
        {
            int value = (sender as IntegerInput).ValueObject as int? ?? 0;

            this.txtEnd.MinValue = value;
        }

        private void TxtEnd_ValueChanged(object sender, EventArgs e)
        {
            int value = (sender as IntegerInput).ValueObject as int? ?? 0;

            this.txtStart.MaxValue = value;
        }

        private void ChkAutoArea_CheckedChanged(object sender, EventArgs e)
        {
            if ((sender as CheckBoxX).Checked)
            {
                this.chkIsCircle.Checked = false;
                this.txtRight.Enabled = false;
                this.txtBottom.Enabled = false;
                this.chkAlphaGradation.Checked = false;
                this.chkIsCircle.Enabled = false;
                this.chkAlphaGradation.Enabled = false;
                this.labelX3.Text = "X,Y Location";
            }
            else
            {
                this.txtRight.Enabled = true;
                this.txtBottom.Enabled = true;
                this.chkIsCircle.Enabled = true;
                this.chkAlphaGradation.Enabled = true;
                this.labelX3.Text = "LT";
            }
        }

        private void ChkIsCircle_CheckedChanged(object sender, System.EventArgs e)
        {
            if ((sender as CheckBoxX).Checked)
            {
                this.txtRadius.Enabled = true;
                this.txtRight.Enabled = false;
                this.txtBottom.Enabled = false;
                this.labelX3.Text = "X,Y Location";
            }
            else
            {
                this.txtRadius.Enabled = false;
                this.txtRight.Enabled = true;
                this.txtBottom.Enabled = true;
                this.labelX3.Text = "LT";
            }
        }

        private void ChkAlphaGradation_CheckedChanged(object sender, EventArgs e)
        {
            bool value = (sender as CheckBoxX).Checked;

            if (value)
            {
                this.txtAlphaStart.Value = this.txtStart.Value;
                this.txtAlphaEnd.Value = this.txtEnd.Value;
            }
            this.txtAlphaDst.Enabled = value;
            this.txtAlphaStart.Enabled = value;
            this.txtAlphaEnd.Enabled = value;
        }
    }
}