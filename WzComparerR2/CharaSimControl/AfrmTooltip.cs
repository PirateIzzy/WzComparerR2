﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;
using WzComparerR2.Controls;
using SharpDX.XAudio2;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace WzComparerR2.CharaSimControl
{
    public class AfrmTooltip : AlphaForm
    {
        public AfrmTooltip()
        {
            this.menu = new ContextMenuStrip();
            this.menu.Items.Add(new ToolStripMenuItem("Copy Tooltip Image to Clipboard", null, tsmiCopy_Click));
            this.menu.Items.Add(new ToolStripMenuItem("Save Tooltip Image", null, tsmiSave_Click));
            this.menu.Items.Add(new ToolStripMenuItem("Save Avatar Image", null, tsmiAvatarSave_Click));
            this.menu.Items.Add(new ToolStripSeparator());
            this.menu.Items.Add(new ToolStripMenuItem("Copy String to Clipboard", null, tsmiCopyText_Click));
            this.menu.Items.Add(new ToolStripMenuItem("Copy Translated String to Clipboard", null, tsmiCopyTranslate_Click));
            this.menu.Items.Add(new ToolStripSeparator());
            this.menu.Items.Add(new ToolStripMenuItem("Close (Esc)", null, tsmiClose_Click));
            this.ContextMenuStrip = this.menu;

            this.Size = new Size(1, 1);
            this.HideOnHover = true;
            this.GearRender = new GearTooltipRender2();
            this.GearRender22 = new GearTooltipRender22();
            this.ItemRender = new ItemTooltipRender2();
            this.SkillRender = new SkillTooltipRender2();
            this.RecipeRender = new RecipeTooltipRender();
            this.MapRender = new MapTooltipRenderer();
            this.MobRender = new MobTooltipRenderer();
            this.NpcRender = new NpcTooltipRenderer();
            this.HelpRender = new HelpTooltipRender();
            this.SetItemRender = new SetItemTooltipRender();
            this.SetItemRender22 = new SetItemTooltipRender22();
            this.SizeChanged += AfrmTooltip_SizeChanged;

            this.MouseClick += AfrmTooltip_MouseClick;
        }

        private object item;

        private ContextMenuStrip menu;
        private bool showMenu;
        private bool showID;

        private Bitmap AvatarBitmap;
        private FrmWaiting WaitingForm = new FrmWaiting();
        private static readonly SemaphoreSlim TranslateSemaphore = new SemaphoreSlim(1, 1);


        public Object TargetItem
        {
            get { return item; }
            set { item = value; }
        }

        public StringLinker StringLinker { get; set; }
        public Character Character { get; set; }

        public GearTooltipRender2 GearRender { get; private set; }
        public GearTooltipRender22 GearRender22 { get; private set; }
        public ItemTooltipRender2 ItemRender { get; private set; }
        public SkillTooltipRender2 SkillRender { get; private set; }
        public RecipeTooltipRender RecipeRender { get; private set; }
        public MapTooltipRenderer MapRender { get; private set; }
        public MobTooltipRenderer MobRender { get; private set; }
        public NpcTooltipRenderer NpcRender { get; private set; }
        public HelpTooltipRender HelpRender { get; private set; }
        public SetItemTooltipRender SetItemRender { get; private set; }
        public SetItemTooltipRender22 SetItemRender22 { get; private set; }

        public string ImageFileName { get; set; }
        public string NodeName { get; set; }
        public string Desc { get; set; }
        public string Pdesc { get; set; }
        public string AutoDesc { get; set; }
        public string Hdesc { get; set; }
        public string DescLeftAlign { get; set; }
        public int NodeID { get; set; }
        public int PreferredStringCopyMethod { get; set; }
        public bool CopyParsedSkillString { get; set; }
        public bool Enable22AniStyle { get; set; }
        public bool ShowID
        {
            get { return this.showID; }
            set
            {
                this.showID = value;
                this.GearRender.ShowObjectID = value;
                this.GearRender22.ShowObjectID = value;
                this.MapRender.ShowObjectID = value;
                this.ItemRender.ShowObjectID = value;
                this.SkillRender.ShowObjectID = value;
                this.RecipeRender.ShowObjectID = value;
            }
        }

        public bool ShowMenu
        {
            get { return showMenu; }
            set { showMenu = value; }
        }

        public async override void Refresh()
        {
            this.PreRender();
            if (Translator.IsTranslateEnabled)
            {
                TranslateSemaphore.Wait();
                Thread.Sleep(10);
                TranslateSemaphore.Release();
            }
            if (this.Bitmap != null)
            {
                this.SetBitmap(Bitmap);
                this.CaptionRectangle = new Rectangle(0, 0, Bitmap.Width, Bitmap.Height);
                base.Refresh();
            }
        }

        public async void PreRender()
        {
            AvatarBitmap = null;
            if (this.item == null)
                return;

            TooltipRender renderer;
            if (item is Item)
            {
                renderer = ItemRender;
                ItemRender.Item = this.item as Item;
            }
            else if (item is Gear)
            {
                if (Enable22AniStyle)
                {
                    renderer = GearRender22;
                    GearRender22.Gear = this.TargetItem as Gear;
                }
                else
                {
                    renderer = GearRender;
                    GearRender.Gear = this.TargetItem as Gear;
                }

                if (false)
                {
                    Gear g = GearRender.Gear;
                    if (this.StringLinker.StringEqp.ContainsKey(g.ItemID))
                    {
                        this.StringLinker.StringEqp[g.ItemID].Name = "暴君之高卡文黑锅";
                        this.StringLinker.StringEqp[g.ItemID].Desc = @"""#c这个锅 我背了！#"" ————gaokawen";
                    }
                    g.Star = 25;
                    g.Grade = GearGrade.SS;
                    g.AdditionGrade = GearGrade.B;
                    g.Props[GearPropType.reqLevel] = 250;
                    g.Props[GearPropType.reqSTR] = 6;
                    g.Props[GearPropType.reqDEX] = 6;
                    g.Props[GearPropType.reqINT] = 6;
                    g.Props[GearPropType.reqLUK] = 6;
                    g.Props[GearPropType.reqPOP] = 666;
                    g.Props[GearPropType.level] = 1;
                    g.Props[GearPropType.reqJob] = 0;
                    g.Props[GearPropType.incPAD] = 6;
                    g.Props[GearPropType.incMAD] = 6;
                    g.Props[GearPropType.incPDD] = 666;
                    g.Props[GearPropType.incMDD] = 666;
                    g.Props[GearPropType.tuc] = 66;
                    g.Props[GearPropType.superiorEqp] = 1;
                    g.Props[GearPropType.tradeAvailable] = 2;
                    //g.Props[GearPropType.charismaEXP] = 88;
                    //g.Props[GearPropType.willEXP] = 88;
                    //g.Props[GearPropType.charmEXP] = 88;
                    g.Props[GearPropType.nActivatedSocket] = 1;
                    //g.Props[GearPropType.setItemID] = 135;
                    //g.Options[0] = Potential.LoadFromWz(60001, 3);
                    //g.Options[1] = Potential.LoadFromWz(60001, 3);
                    //g.Options[2] = Potential.LoadFromWz(60001, 3);
                    //g.AdditionalOptions[0] = Potential.LoadFromWz(32086, 10);
                    //g.AdditionalOptions[1] = Potential.LoadFromWz(32086, 10);
                    //g.AdditionalOptions[2] = Potential.LoadFromWz(32086, 10);
                }
            }
            else if (item is Skill)
            {
                renderer = SkillRender;
                SkillRender.Skill = this.item as Skill;
            }
            else if (item is Recipe)
            {
                renderer = RecipeRender;
                RecipeRender.Recipe = this.item as Recipe;
            }
            else if (item is Map)
            {
                renderer = MapRender;
                MapRender.Map = this.item as Map;
            }
            else if (item is Mob)
            {
                renderer = MobRender;
                MobRender.MobInfo = this.item as Mob;
            }
            else if (item is Npc)
            {
                renderer = NpcRender;
                NpcRender.NpcInfo = this.item as Npc;
            }
            else if (item is TooltipHelp)
            {
                renderer = HelpRender;
                HelpRender.Pair = this.item as TooltipHelp;
            }
            else if (item is SetItem)
            {
                if (Enable22AniStyle)
                {
                    if ((item as SetItem).ItemIDs.Parts.Any(p => p.Value.ItemIDs.Any(i => i.Key / 1000000 == 5)))
                    {
                        renderer = SetItemRender;
                        SetItemRender.Enable22AniStyle = true;
                        SetItemRender.SetItem = this.item as SetItem;
                    }
                    else
                    {
                        renderer = SetItemRender22;
                        SetItemRender22.SetItem = this.item as SetItem;
                    }
                }
                else
                {
                    renderer = SetItemRender;
                    SetItemRender.SetItem = this.item as SetItem;
                }
            }
            else
            {
                this.Bitmap = null;
                renderer = null;
                return;
            }
            renderer.StringLinker = StringLinker;
            if (Translator.IsTranslateEnabled)
            {
                Translator.WaitingForGlossaryTableRelease();
                WaitingForm.UpdateMessage("Translating...");
                WaitingForm.Show();
                await Task.Run(() =>
                {
                    TranslateSemaphore.Wait();
                    this.Bitmap = renderer.Render();
                    TranslateSemaphore.Release();
                });
                WaitingForm.Hide();
            }
            else
            {
                this.Bitmap = renderer.Render();
            }
            if (item is Item) AvatarBitmap = (this.TargetItem as Item).AvatarBitmap;
            if (item is Gear) AvatarBitmap = (this.TargetItem as Gear).AndroidBitmap;
            if (item is Npc) AvatarBitmap = (this.TargetItem as Npc).AvatarBitmap;
        }

        void AfrmTooltip_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && this.showMenu)
            {
                this.menu.Show(this, e.Location);
            }
        }

        void tsmiCopy_Click(object sender, EventArgs e)
        {
            if (this.Bitmap != null)
            {
                using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
                {
                    var dataObj = new DataObject();
                    dataObj.SetData(DataFormats.Bitmap, this.Bitmap);
                    Byte[] dibData = ConvertToDib(this.Bitmap);
                    stream.Write(dibData, 0, dibData.Length);
                    dataObj.SetData(DataFormats.Dib, stream);
                    Clipboard.SetDataObject(dataObj, true);
                }
            }
        }

        void tsmiCopyText_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            if (this.PreferredStringCopyMethod == 2) sb.AppendLine(this.NodeID.ToString());
            if (!String.IsNullOrEmpty(this.NodeName)) sb.AppendLine(this.NodeName);
            if (String.IsNullOrEmpty(this.Desc)) this.Desc = "";
            if (String.IsNullOrEmpty(this.Pdesc)) this.Pdesc = "";
            if (String.IsNullOrEmpty(this.AutoDesc)) this.AutoDesc = "";
            if (String.IsNullOrEmpty(this.Hdesc)) this.Hdesc = "";
            if (String.IsNullOrEmpty(this.DescLeftAlign)) this.DescLeftAlign = "";
            if (this.CopyParsedSkillString && item is Skill) this.Hdesc = this.SkillRender.ParsedHdesc;
            switch (this.PreferredStringCopyMethod)
            {
                default:
                case 0:
                    if (!String.IsNullOrEmpty(this.Desc)) sb.AppendLine(this.Desc);
                    if (!String.IsNullOrEmpty(this.Pdesc)) sb.AppendLine(this.Pdesc);
                    if (!String.IsNullOrEmpty(this.AutoDesc)) sb.AppendLine(this.AutoDesc);
                    if (!String.IsNullOrEmpty(this.Hdesc)) sb.AppendLine(this.Hdesc);
                    if (!String.IsNullOrEmpty(this.DescLeftAlign)) sb.AppendLine(this.DescLeftAlign);
                    break;
                case 1:
                    if ((this.Desc + this.Pdesc + this.AutoDesc).Contains("\\n"))
                    {
                        foreach (string i in (this.Desc + this.Pdesc + this.AutoDesc).Split(new string[] { "\\n" }, StringSplitOptions.None))
                        {
                            sb.AppendLine(i.Replace("\\r", "").Replace("#c", "").Replace("#", ""));
                        }
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(this.Desc)) sb.AppendLine(this.Desc);
                        if (!String.IsNullOrEmpty(this.Pdesc)) sb.AppendLine(this.Pdesc);
                        if (!String.IsNullOrEmpty(this.AutoDesc)) sb.AppendLine(this.AutoDesc);
                    }
                    if (this.Hdesc.Contains("\\n"))
                    {
                        foreach (string i in this.Hdesc.Split(new string[] { "\\n" }, StringSplitOptions.None))
                        {
                            if (this.CopyParsedSkillString)
                            {
                                sb.AppendLine(i.Replace("\\r", "").Replace("#c", "").Replace("#", ""));
                            }
                            else
                            {
                                sb.AppendLine(i.Replace("\\r", ""));
                            }
                        }
                    }
                    else
                    {
                        if (this.CopyParsedSkillString)
                        {
                            sb.AppendLine(this.Hdesc.Replace("#c", "").Replace("#", ""));
                        }
                        else
                        {
                            sb.AppendLine(this.Hdesc);
                        }
                    }
                    break;
                case 2:
                    if (!String.IsNullOrEmpty(this.Desc)) sb.AppendLine(this.Desc.Replace("\\r", "").Replace("\\n", "<br />").Replace("#c", "<span class=\"darkorange-text\">").Replace("#", "</span>"));
                    if (!String.IsNullOrEmpty(this.Pdesc)) sb.AppendLine(this.Pdesc.Replace("\\r", "").Replace("\\n", "<br />").Replace("#c", "<span class=\"darkorange-text\">").Replace("#", "</span>"));
                    if (!String.IsNullOrEmpty(this.AutoDesc)) sb.AppendLine(this.AutoDesc.Replace("\\r", "").Replace("\\n", "<br />").Replace("#c", "<span class=\"darkorange-text\">").Replace("#", "</span>"));
                    if (this.CopyParsedSkillString)
                    {
                        if (!String.IsNullOrEmpty(this.Hdesc)) sb.AppendLine(this.Hdesc.Replace("\\r", "").Replace("\\n", "<br />").Replace("#c", "<span class=\"darkorange-text\">").Replace("#", "</span>"));
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(this.Hdesc)) sb.AppendLine(this.Hdesc.Replace("\\r", "").Replace("\\n", "<br />"));
                    }
                    if (!String.IsNullOrEmpty(this.DescLeftAlign)) sb.AppendLine(this.DescLeftAlign.Replace("\\r", "").Replace("\\n", "<br />").Replace("#c", "<span class=\"darkorange-text\">").Replace("#", "</span>"));
                    break;
            }
            Clipboard.SetText(sb.ToString());
            sb.Clear();
        }

        async void tsmiCopyTranslate_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            if (this.PreferredStringCopyMethod == 2) sb.AppendLine(this.NodeID.ToString());
            if (!String.IsNullOrEmpty(this.NodeName)) sb.AppendLine(this.NodeName);
            if (String.IsNullOrEmpty(this.Desc)) this.Desc = "";
            if (String.IsNullOrEmpty(this.Pdesc)) this.Pdesc = "";
            if (String.IsNullOrEmpty(this.AutoDesc)) this.AutoDesc = "";
            if (String.IsNullOrEmpty(this.Hdesc)) this.Hdesc = "";
            if (String.IsNullOrEmpty(this.DescLeftAlign)) this.DescLeftAlign = "";
            if (this.CopyParsedSkillString && item is Skill) this.Hdesc = this.SkillRender.ParsedHdesc;
            if (!String.IsNullOrEmpty(this.NodeName)) sb.AppendLine("<name>" + this.NodeName + "</name>");
            if (!String.IsNullOrEmpty(this.Desc)) sb.AppendLine("<desc>" + this.Desc + "</desc>");
            if (!String.IsNullOrEmpty(this.Pdesc)) sb.AppendLine("<pdesc>" + this.Pdesc + "</pdesc>");
            if (!String.IsNullOrEmpty(this.AutoDesc)) sb.AppendLine("<autodesc>" + this.AutoDesc + "</autodesc>");
            if (!String.IsNullOrEmpty(this.Hdesc)) sb.AppendLine("<hdesc>" + this.Hdesc + "</hdesc>");
            if (!String.IsNullOrEmpty(this.DescLeftAlign)) sb.AppendLine("<descleftalign>" + this.DescLeftAlign + "</descleftalign>");
            string translatedResult = "";
            Translator.WaitingForGlossaryTableRelease();
            WaitingForm.UpdateMessage("Translating...");
            try
            {
                WaitingForm.Show();
                await Task.Run(() => { translatedResult = Translator.AfrmTooltipTranslateBeforeCopy(sb.ToString()); });
                Clipboard.SetText(translatedResult);
                WaitingForm.Hide();
                sb.Clear();
            }
            finally
            {
                if (WaitingForm.InvokeRequired)
                {
                    WaitingForm.Invoke(new Action(() => WaitingForm.Hide()));
                }
                else
                {
                    WaitingForm.Hide();
                }
            }
        }

        void tsmiClose_Click(object sender, EventArgs e)
        {
            if (this.Bitmap != null)
            {
                this.Close();
            }
        }

        private Byte[] ConvertToDib(Image image) // https://stackoverflow.com/a/46424800
        {
            Byte[] bm32bData;
            Int32 width = image.Width;
            Int32 height = image.Height;
            // Ensure image is 32bppARGB by painting it on a new 32bppARGB image.
            using (Bitmap bm32b = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics gr = Graphics.FromImage(bm32b))
                    gr.DrawImage(image, new Rectangle(0, 0, bm32b.Width, bm32b.Height));
                // Bitmap format has its lines reversed.
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
                Int32 stride;
                bm32bData = GetImageData(bm32b, out stride);
            }
            // BITMAPINFOHEADER struct for DIB.
            Int32 hdrSize = 0x28;
            Byte[] fullImage = new Byte[hdrSize + 12 + bm32bData.Length];
            //Int32 biSize;
            WriteIntToByteArray(fullImage, 0x00, 4, true, (UInt32)hdrSize);
            //Int32 biWidth;
            WriteIntToByteArray(fullImage, 0x04, 4, true, (UInt32)width);
            //Int32 biHeight;
            WriteIntToByteArray(fullImage, 0x08, 4, true, (UInt32)height);
            //Int16 biPlanes;
            WriteIntToByteArray(fullImage, 0x0C, 2, true, 1);
            //Int16 biBitCount;
            WriteIntToByteArray(fullImage, 0x0E, 2, true, 32);
            //BITMAPCOMPRESSION biCompression = BITMAPCOMPRESSION.BITFIELDS;
            WriteIntToByteArray(fullImage, 0x10, 4, true, 3);
            //Int32 biSizeImage;
            WriteIntToByteArray(fullImage, 0x14, 4, true, (UInt32)bm32bData.Length);
            // These are all 0. Since .net clears new arrays, don't bother writing them.
            //Int32 biXPelsPerMeter = 0;
            //Int32 biYPelsPerMeter = 0;
            //Int32 biClrUsed = 0;
            //Int32 biClrImportant = 0;

            // The aforementioned "BITFIELDS": colour masks applied to the Int32 pixel value to get the R, G and B values.
            WriteIntToByteArray(fullImage, hdrSize + 0, 4, true, 0x00FF0000);
            WriteIntToByteArray(fullImage, hdrSize + 4, 4, true, 0x0000FF00);
            WriteIntToByteArray(fullImage, hdrSize + 8, 4, true, 0x000000FF);
            Array.Copy(bm32bData, 0, fullImage, hdrSize + 12, bm32bData.Length);
            return fullImage;
        }

        private Byte[] GetImageData(Bitmap sourceImage, out Int32 stride) // https://stackoverflow.com/a/43706643
        {
            System.Drawing.Imaging.BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, sourceImage.PixelFormat);
            stride = sourceData.Stride;
            Byte[] data = new Byte[stride * sourceImage.Height];
            System.Runtime.InteropServices.Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            sourceImage.UnlockBits(sourceData);
            return data;
        }

        private void WriteIntToByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian, UInt32 value) // https://stackoverflow.com/a/46424800
        {
            Int32 lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a " + bytes + "-byte value at offset " + startIndex + ".");
            for (Int32 index = 0; index < bytes; index++)
            {
                Int32 offs = startIndex + (littleEndian ? index : lastByte - index);
                data[offs] = (Byte)(value >> (8 * index) & 0xFF);
            }
        }

        void tsmiSave_Click(object sender, EventArgs e)
        {
            if (this.Bitmap != null && this.item != null)
            {
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Filter = "PNG (*.png)|*.png|*.*|*.*";
                    dlg.FileName = this.ImageFileName;

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        this.Bitmap.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
        }

        void tsmiAvatarSave_Click(object sender, EventArgs e)
        {
            if (this.AvatarBitmap != null && this.item != null)
            {
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Filter = "PNG (*.png)|*.png|*.*|*.*";
                    dlg.FileName = this.ImageFileName.Replace("eqp", "avatar");

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        this.AvatarBitmap.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
        }


        void AfrmTooltip_SizeChanged(object sender, EventArgs e)
        {
            if (this.Bitmap != null)
                this.SetClientSizeCore(this.Bitmap.Width, this.Bitmap.Height);
        }
    }
}
