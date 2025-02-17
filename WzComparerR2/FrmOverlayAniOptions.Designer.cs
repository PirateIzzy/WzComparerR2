namespace WzComparerR2
{
    partial class FrmOverlayAniOptions // base code from FrmGifClipOptions
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonOK = new DevComponents.DotNetBar.ButtonX();
            this.buttonCancel = new DevComponents.DotNetBar.ButtonX();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.labelX2 = new DevComponents.DotNetBar.LabelX();
            this.labelX3 = new DevComponents.DotNetBar.LabelX();
            this.labelX4 = new DevComponents.DotNetBar.LabelX();
            this.labelX5 = new DevComponents.DotNetBar.LabelX();
            this.labelX6 = new DevComponents.DotNetBar.LabelX();
            this.labelX7 = new DevComponents.DotNetBar.LabelX();
            this.txtDelayOffset = new DevComponents.Editors.IntegerInput();
            this.txtMoveX = new DevComponents.Editors.IntegerInput();
            this.txtMoveY = new DevComponents.Editors.IntegerInput();
            this.txtFrameStart = new DevComponents.Editors.IntegerInput();
            this.txtFrameEnd = new DevComponents.Editors.IntegerInput();
            this.txtPngDelay = new DevComponents.Editors.IntegerInput();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtDelayOffset)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMoveX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMoveY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFrameStart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFrameEnd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPngDelay)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.buttonOK.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonOK.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(76, 4);
            this.buttonOK.Margin = new System.Windows.Forms.Padding(40, 4, 5, 4);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(101, 31);
            this.buttonOK.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.buttonOK.Symbol = "";
            this.buttonOK.SymbolSize = 1F;
            this.buttonOK.TabIndex = 0;
            this.buttonOK.Text = "OK";
            // 
            // buttonCancel
            // 
            this.buttonCancel.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonCancel.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(260, 4);
            this.buttonCancel.Margin = new System.Windows.Forms.Padding(5, 4, 40, 4);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(101, 31);
            this.buttonCancel.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 5;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 43F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 43F));
            this.tableLayoutPanel1.Controls.Add(this.labelX1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelX2, 3, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelX3, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelX4, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelX5, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelX6, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelX7, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.txtDelayOffset, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtMoveX, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.txtMoveY, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.txtFrameStart, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.txtFrameEnd, 4, 3);
            this.tableLayoutPanel1.Controls.Add(this.txtPngDelay, 2, 4);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(10, 11);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(438, 178);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // labelX1
            // 
            this.labelX1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Location = new System.Drawing.Point(5, 4);
            this.labelX1.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.labelX1.Name = "labelX1";
            this.tableLayoutPanel1.SetRowSpan(this.labelX1, 5);
            this.labelX1.Size = new System.Drawing.Size(62, 170);
            this.labelX1.TabIndex = 0;
            this.labelX1.Text = "Settings";
            // 
            // labelX2
            // 
            this.labelX2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX2.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX2.Location = new System.Drawing.Point(325, 109);
            this.labelX2.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.labelX2.Name = "labelX2";
            this.labelX2.Size = new System.Drawing.Size(18, 27);
            this.labelX2.TabIndex = 12;
            this.labelX2.Text = "-";
            // 
            // labelX3
            // 
            this.labelX3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX3.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX3.Location = new System.Drawing.Point(77, 4);
            this.labelX3.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.labelX3.Name = "labelX3";
            this.labelX3.Size = new System.Drawing.Size(150, 27);
            this.labelX3.TabIndex = 8;
            this.labelX3.Text = "Start Delay (ms)";
            // 
            // labelX4
            // 
            this.labelX4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX4.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX4.Location = new System.Drawing.Point(77, 39);
            this.labelX4.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.labelX4.Name = "labelX4";
            this.labelX4.Size = new System.Drawing.Size(150, 27);
            this.labelX4.TabIndex = 9;
            this.labelX4.Text = "X Coordinate (px)";
            // 
            // labelX5
            // 
            this.labelX5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX5.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX5.Location = new System.Drawing.Point(77, 74);
            this.labelX5.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.labelX5.Name = "labelX5";
            this.labelX5.Size = new System.Drawing.Size(150, 27);
            this.labelX5.TabIndex = 10;
            this.labelX5.Text = "Y Coordinate (px)";
            // 
            // labelX6
            // 
            this.labelX6.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX6.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX6.Location = new System.Drawing.Point(77, 109);
            this.labelX6.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.labelX6.Name = "labelX6";
            this.labelX6.Size = new System.Drawing.Size(150, 27);
            this.labelX6.TabIndex = 11;
            this.labelX6.Text = "Select Frame";
            // 
            // labelX7
            // 
            this.labelX7.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.labelX7.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX7.Location = new System.Drawing.Point(77, 144);
            this.labelX7.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.labelX7.Name = "labelX7";
            this.labelX7.Size = new System.Drawing.Size(150, 30);
            this.labelX7.TabIndex = 12;
            this.labelX7.Text = "PNG Delay (ms)";
            // 
            // txtDelayOffset
            // 
            this.txtDelayOffset.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtDelayOffset.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtDelayOffset.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtDelayOffset.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.tableLayoutPanel1.SetColumnSpan(this.txtDelayOffset, 3);
            this.txtDelayOffset.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtDelayOffset.Increment = 30;
            this.txtDelayOffset.Location = new System.Drawing.Point(237, 4);
            this.txtDelayOffset.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.txtDelayOffset.MaxValue = 65530;
            this.txtDelayOffset.MinValue = 0;
            this.txtDelayOffset.Name = "txtDelayOffset";
            this.txtDelayOffset.ShowUpDown = true;
            this.txtDelayOffset.Size = new System.Drawing.Size(196, 22);
            this.txtDelayOffset.TabIndex = 0;
            // 
            // txtMoveX
            // 
            this.txtMoveX.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtMoveX.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtMoveX.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtMoveX.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.tableLayoutPanel1.SetColumnSpan(this.txtMoveX, 3);
            this.txtMoveX.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtMoveX.Location = new System.Drawing.Point(237, 39);
            this.txtMoveX.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.txtMoveX.MaxValue = 8192;
            this.txtMoveX.MinValue = -8192;
            this.txtMoveX.Name = "txtMoveX";
            this.txtMoveX.ShowUpDown = true;
            this.txtMoveX.Size = new System.Drawing.Size(196, 22);
            this.txtMoveX.TabIndex = 1;
            // 
            // txtMoveY
            // 
            this.txtMoveY.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtMoveY.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtMoveY.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtMoveY.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.tableLayoutPanel1.SetColumnSpan(this.txtMoveY, 3);
            this.txtMoveY.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtMoveY.Location = new System.Drawing.Point(237, 74);
            this.txtMoveY.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.txtMoveY.MaxValue = 8192;
            this.txtMoveY.MinValue = -8192;
            this.txtMoveY.Name = "txtMoveY";
            this.txtMoveY.ShowUpDown = true;
            this.txtMoveY.Size = new System.Drawing.Size(196, 22);
            this.txtMoveY.TabIndex = 2;
            // 
            // txtFrameStart
            // 
            this.txtFrameStart.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtFrameStart.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtFrameStart.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtFrameStart.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.txtFrameStart.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtFrameStart.Location = new System.Drawing.Point(237, 109);
            this.txtFrameStart.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.txtFrameStart.MaxValue = 8192;
            this.txtFrameStart.MinValue = 0;
            this.txtFrameStart.Name = "txtFrameStart";
            this.txtFrameStart.ShowUpDown = true;
            this.txtFrameStart.Size = new System.Drawing.Size(78, 22);
            this.txtFrameStart.TabIndex = 3;
            // 
            // txtFrameEnd
            // 
            this.txtFrameEnd.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtFrameEnd.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtFrameEnd.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtFrameEnd.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.txtFrameEnd.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtFrameEnd.Location = new System.Drawing.Point(353, 109);
            this.txtFrameEnd.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.txtFrameEnd.MaxValue = 8192;
            this.txtFrameEnd.MinValue = 0;
            this.txtFrameEnd.Name = "txtFrameEnd";
            this.txtFrameEnd.ShowUpDown = true;
            this.txtFrameEnd.TabIndex = 4;
            // 
            // txtPngDelay
            // 
            this.txtPngDelay.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtPngDelay.BackgroundStyle.Class = "DateTimeInputBackground";
            this.txtPngDelay.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtPngDelay.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.tableLayoutPanel1.SetColumnSpan(this.txtPngDelay, 3);
            this.txtPngDelay.Enabled = false;
            this.txtPngDelay.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtPngDelay.Location = new System.Drawing.Point(237, 144);
            this.txtPngDelay.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.txtPngDelay.MaxValue = 65530;
            this.txtPngDelay.MinValue = 0;
            this.txtPngDelay.Name = "txtPngDelay";
            this.txtPngDelay.ShowUpDown = true;
            this.txtPngDelay.Size = new System.Drawing.Size(196, 22);
            this.txtPngDelay.TabIndex = 5;
            this.txtPngDelay.Value = 120;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.buttonCancel, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.buttonOK, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(10, 189);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(438, 40);
            this.tableLayoutPanel2.TabIndex = 3;
            // 
            // FrmOverlayAniOptions
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(458, 240);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.tableLayoutPanel2);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmOverlayAniOptions";
            this.Padding = new System.Windows.Forms.Padding(10, 11, 10, 11);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Animation Overlay Settings";
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.txtDelayOffset)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMoveX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMoveY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFrameStart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFrameEnd)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPngDelay)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DevComponents.DotNetBar.ButtonX buttonOK;
        private DevComponents.DotNetBar.ButtonX buttonCancel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private DevComponents.DotNetBar.LabelX labelX4;
        private DevComponents.Editors.IntegerInput txtDelayOffset;
        private DevComponents.Editors.IntegerInput txtMoveX;
        private DevComponents.Editors.IntegerInput txtMoveY;
        private DevComponents.Editors.IntegerInput txtFrameStart;
        private DevComponents.Editors.IntegerInput txtFrameEnd;
        private DevComponents.Editors.IntegerInput txtPngDelay;
        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.LabelX labelX3;
        private DevComponents.DotNetBar.LabelX labelX5;
        private DevComponents.DotNetBar.LabelX labelX6;
        private DevComponents.DotNetBar.LabelX labelX2;
        private DevComponents.DotNetBar.LabelX labelX7;
    }
}