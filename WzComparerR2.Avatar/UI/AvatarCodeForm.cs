﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;

namespace WzComparerR2.Avatar.UI
{
    public partial class AvatarCodeForm : DevComponents.DotNetBar.OfficeForm
    {
        public AvatarCodeForm()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font(new FontFamily("Microsoft Sans Serif"), 8f);
#endif
        }

        public string CodeText
        {
            get { return textBoxX1.Text; }
            set { textBoxX1.Text = value; }
        }

        public int LoadType
        {
            get
            {
                if (this.checkBoxX1.Checked)
                {
                    return 0;
                }
                else if (this.checkBoxX2.Checked)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        private void textBoxX1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                textBoxX1.SelectAll();
            }
        }
    }
}