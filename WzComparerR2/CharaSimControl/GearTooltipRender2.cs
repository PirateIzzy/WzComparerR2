﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Resource = CharaSimResource.Resource;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;
using WzComparerR2.WzLib;
using WzComparerR2.AvatarCommon;

namespace WzComparerR2.CharaSimControl
{
    public class GearTooltipRender2 : TooltipRender
    {
        static GearTooltipRender2()
        {
            res = new Dictionary<string, TextureBrush>();
            res["t"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_top, WrapMode.Clamp);
            res["line"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_line, WrapMode.Tile);
            res["dotline"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_dotline, WrapMode.Clamp);
            res["b"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_bottom, WrapMode.Clamp);
            res["cover"] = new TextureBrush(Resource.UIToolTip_img_Item_Frame_cover, WrapMode.Clamp);
        }

        private static Dictionary<string, TextureBrush> res;

        public GearTooltipRender2()
        {
        }

        private CharacterStatus charStat;

        public Gear Gear { get; set; }
        private AvatarCanvasManager avatar;

        public override object TargetItem
        {
            get { return this.Gear; }
            set { this.Gear = value as Gear; }
        }

        public CharacterStatus CharacterStatus
        {
            get { return charStat; }
            set { charStat = value; }
        }

        public bool ShowSpeed { get; set; }
        public bool ShowLevelOrSealed { get; set; }
        public bool ShowMedalTag { get; set; } = true;
        public bool IsCombineProperties { get; set; } = true;
        public bool CompareMode { get; set; } = false;
        public int CosmeticHairColor { get; set; }
        public int CosmeticFaceColor { get; set; }

        private bool isPostNEXTClient;

        public TooltipRender SetItemRender { get; set; }

        public override Bitmap Render()
        {
            if (this.Gear == null)
            {
                return null;
            }

            int[] picH = new int[4];
            Bitmap left = RenderBase(out picH[0]);
            Bitmap add = RenderAddition(out picH[1]);
            int equipLevel = 0;
            this.Gear.Props.TryGetValue(GearPropType.reqLevel, out equipLevel);
            Bitmap genesis = RenderGenesisSkills(out int genesisHeight, equipLevel == 250);
            Bitmap set = RenderSetItem(out int setHeight);
            picH[2] = genesisHeight + setHeight;
            Bitmap levelOrSealed = null;
            if (this.ShowLevelOrSealed)
            {
                levelOrSealed = RenderLevelOrSealed(out picH[3]);
            }

            int width = 261;
            if (add != null) width += add.Width;
            if (set != null) width += set.Width;
            else if (genesis != null) width += genesis.Width; // ideally genesisWeapons always have setitem
            if (levelOrSealed != null) width += levelOrSealed.Width;
            int height = 0;
            for (int i = 0; i < picH.Length; i++)
            {
                height = Math.Max(height, picH[i]);
            }
            Bitmap tooltip = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(tooltip);

            //绘制主图
            width = 0;
            if (left != null)
            {
                //绘制背景
                g.DrawImage(res["t"].Image, width, 0);
                FillRect(g, res["line"], width, 13, picH[0] - 13);
                g.DrawImage(res["b"].Image, width, picH[0] - 13);

                //复制图像
                g.DrawImage(left, width, 0, new Rectangle(0, 0, left.Width, picH[0]), GraphicsUnit.Pixel);

                //cover
                g.DrawImage(res["cover"].Image, 3, 3);

                width += left.Width;
                left.Dispose();
            }

            //绘制addition
                if (add != null)
                {
                    //绘制背景
                g.DrawImage(res["t"].Image, width, 0);
                    FillRect(g, res["line"], width, 13, tooltip.Height - 13);
                    g.DrawImage(res["b"].Image, width, tooltip.Height - 13);

                    //复制原图
                g.DrawImage(add, width, 0, new Rectangle(0, 0, add.Width, picH[1]), GraphicsUnit.Pixel);

                width += add.Width;
                    add.Dispose();
                }

            //绘制setitem
            if (genesis != null || set != null)
            {
                int y = 0;
                int partWidth = 0;
                if (genesis != null)
                {
                    // draw background
                    g.DrawImage(res["t"].Image, width, 0);
                    FillRect(g, res["line"], width, 13, genesisHeight - 13);
                    g.DrawImage(res["b"].Image, width, genesisHeight - 13);

                    // copy text layer
                    g.DrawImage(genesis, width, 0, new Rectangle(0, 0, genesis.Width, genesisHeight), GraphicsUnit.Pixel);

                    y += genesisHeight;
                    partWidth = Math.Max(partWidth, genesis.Width);
                    genesis.Dispose();
                }

                //复制原图
                if (set != null)
                {
                    g.DrawImage(set, width, y, new Rectangle(0, 0, set.Width, setHeight), GraphicsUnit.Pixel);
                    partWidth = Math.Max(partWidth, set.Width);
                    set.Dispose();
                }

                width += partWidth;
            }

            //绘制levelOrSealed
            if (levelOrSealed != null)
            {
                //绘制背景
                g.DrawImage(res["t"].Image, width, 0);
                FillRect(g, res["line"], width, 13, picH[3] - 13);
                g.DrawImage(res["b"].Image, width, picH[3] - 13);

                //复制原图
                g.DrawImage(levelOrSealed, width, 0, new Rectangle(0, 0, levelOrSealed.Width, picH[3]), GraphicsUnit.Pixel);
                width += levelOrSealed.Width;
                levelOrSealed.Dispose();
            }

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, Gear.ItemID.ToString("d8"), true);
            }

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderBase(out int picH)
        {
            StringResult destinyWeapon;
            // 1212142 = Destiny Shining Rod
            isPostNEXTClient = StringLinker.StringEqp.TryGetValue(1212142, out destinyWeapon);
            Bitmap bitmap = new Bitmap(261, DefaultPicHeight);
            Graphics g = Graphics.FromImage(bitmap);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            StringFormat format = (StringFormat)StringFormat.GenericTypographic.Clone();
            var orange2FontColorTable = new Dictionary<string, Color>()
            {
                { "c", ((SolidBrush)GearGraphics.OrangeBrush2).Color },
            };
            var orange3FontColorTable = new Dictionary<string, Color>()
            {
                { "c", ((SolidBrush)GearGraphics.OrangeBrush3).Color },
            };
            var itemPropColorTable = new Dictionary<string, Color>()
            {
                { "$y", GearGraphics.gearCyanColor },
                { "$e", GearGraphics.ScrollEnhancementColor },
            };
            int value, value2; ;

            picH = 13;
            if (!Gear.GetBooleanValue(GearPropType.blockUpgradeStarforce))
            {
                DrawStar2(g, ref picH); //绘制星星
            }

            //绘制装备名称
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringEqp.TryGetValue(Gear.ItemID, out sr))
            {
                sr = new StringResult();
                sr.Name = "(null)";
            }
            string gearName = sr.Name;
            if (String.IsNullOrEmpty(gearName)) gearName = "(null)";
            bool isTranslateRequired = Translator.IsTranslateEnabled;
            if (isTranslateRequired)
            {
                string translatedGearName = Translator.MergeString(gearName, Translator.TranslateString(gearName, true), 0, false, true);
                if (translatedGearName != gearName)
                {
                    gearName = translatedGearName;
                }
            }
            switch (Gear.GetGender(Gear.ItemID))
            {
                case 0: gearName += " (Male)"; break;
                case 1: gearName += " (Female)"; break;
            }
            string nameAdd = Gear.ScrollUp > 0 ? ("+" + Gear.ScrollUp) : null;
            if (!string.IsNullOrEmpty(nameAdd))
            {
                gearName += " (" + nameAdd + ")";
            }

            g.DrawImage(Resource.ToolTip_Equip_Dot_0, 9, picH + 5);//GMS Version blue dot in EQUIPMENT
            format.Alignment = StringAlignment.Near;//GMS Version
            TextRenderer.DrawText(g, gearName, GearGraphics.ItemNameFont2,
                //GearGraphics.GetGearNameBrush(Gear.diff, Gear.ScrollUp > 0, Gear.ItemID / 10000 == 180), 15, picH, format);//GMS Version
                //new Point(14, picH), Gear.Cash ? ((SolidBrush)GearGraphics.GearNameBrushB).Color : ((SolidBrush)GearGraphics.GetGearNameBrush(Gear.diff, Gear.ScrollUp > 0, Gear.ItemID / 10000 == 180)).Color, TextFormatFlags.Left | TextFormatFlags.NoPrefix); // Use this line and not above
                new Point(14, picH), ((SolidBrush)GearGraphics.GetGearNameBrush(Gear.diff, Gear.ScrollUp > 0, Gear.Cash, Gear.ItemID / 10000 == 180)).Color, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            picH += 23;

            //装备rank
            string rankStr = null;
            if (Gear.GetBooleanValue(GearPropType.specialGrade))
            {
                rankStr = ItemStringHelper.GetGearGradeString(GearGrade.Special);
            }
            else if (!Gear.Cash) //T98后C级物品依然显示
            {
                rankStr = ItemStringHelper.GetGearGradeString(Gear.Grade);
            }
            if (rankStr != null)
            {
                TextRenderer.DrawText(g, rankStr, GearGraphics.EquipDetailFont, new Point(261, picH), Color.White, TextFormatFlags.HorizontalCenter);
                picH += 15;
            }

            if (Gear.Props.TryGetValue(GearPropType.royalSpecial, out value) && value > 0)
            {
                switch (value)
                {
                    case 1:
                        TextRenderer.DrawText(g, "Special Label", GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.GearNameBrushA).Color, TextFormatFlags.HorizontalCenter);
                        break;
                    case 2:
                        TextRenderer.DrawText(g, "Red Label", GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.GearNameBrushH).Color, TextFormatFlags.HorizontalCenter);
                        break;
                    case 3:
                        TextRenderer.DrawText(g, "Black Label", GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.GearNameBrushF).Color, TextFormatFlags.HorizontalCenter);
                        break;
                }
                picH += 15;
            }
            else if (Gear.Props.TryGetValue(GearPropType.masterSpecial, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "Master Label", GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.BlueBrush).Color, TextFormatFlags.HorizontalCenter);
                picH += 15;
            }
            else if (Gear.Props.TryGetValue(GearPropType.BTSLabel, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "BTS Label", GearGraphics.EquipDetailFont, new Point(70, picH), Color.FromArgb(187, 102, 238), TextFormatFlags.HorizontalCenter);
                picH += 15;
            }
            else if (Gear.Props.TryGetValue(GearPropType.BLACKPINKLabel, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "BLACKPINK Label", GearGraphics.EquipDetailFont, new Point(261, picH), Color.FromArgb(255, 136, 170), TextFormatFlags.HorizontalCenter);
                picH += 15;
            }

            //额外属性
            var attrList = GetGearAttributeString();
            if (attrList.Count > 0)
            {
                var font = GearGraphics.EquipDetailFont;
                string attrStr = null;
                for (int i = 0; i < attrList.Count; i++)
                {
                    var newStr = (attrStr != null ? (attrStr + ", ") : null) + attrList[i];
                    //if (TextRenderer.MeasureText(g, newStr, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width > 261 - 7) pr - CharaSim: Suppress ExclusiveEquip line for same gear name
                    if (TextRenderer.MeasureText(g, newStr, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width > 261 - 13)
                    {
                        TextRenderer.DrawText(g, attrStr, GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.OrangeBrush2).Color, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                        picH += 15;
                        attrStr = attrList[i];
                    }
                    else
                    {
                        attrStr = newStr;
                    }
                }
                if (!string.IsNullOrEmpty(attrStr))
                {
                    TextRenderer.DrawText(g, attrStr, GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.OrangeBrush2).Color, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                    picH += 15;
                }
            }

            //装备限时
            if (Gear.TimeLimited)
            {
                DateTime time = DateTime.Now.AddDays(7d);
                string expireStr = "Usable Until: " + time.ToString(@"M\/d\/yyyy HH\:mm") + " UTC";
                TextRenderer.DrawText(g, expireStr, GearGraphics.EquipDetailFont, new Point(bitmap.Width, picH), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                picH += 15;
            }
            else if (Gear.GetBooleanValue(GearPropType.abilityTimeLimited))
            {
                DateTime time = DateTime.Now.AddDays(7d);
                string expireStr;
                if (!Gear.Cash)
                {
                    expireStr = "Effects Last Until: " + time.ToString(@"M\/d\/yyyy HH\:mm") + " UTC";
                }
                else
                {
                    expireStr = "Usable Until: " + time.ToString(@"M\/d\/yyyy HH\:mm") + " UTC";
                }
                TextRenderer.DrawText(g, expireStr, GearGraphics.EquipDetailFont, new Point(bitmap.Width, picH), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                picH += 15;
            }

            //分割线1号
            picH += 7;
            g.DrawImage(res["dotline"].Image, 0, picH);

            //绘制装备图标
            if (Gear.Grade > 0 && (int)Gear.Grade <= 4) //绘制外框
            {
                Image border = Resource.ResourceManager.GetObject("UIToolTip_img_Item_ItemIcon_" + (int)Gear.Grade) as Image;
                if (border != null)
                {
                    g.DrawImage(border, 13, picH + 11);
                }
            }
            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_base, 12, picH + 10); //绘制背景
            if (Gear.IconRaw.Bitmap != null) //绘制icon
            {
                var attr = new System.Drawing.Imaging.ImageAttributes();
                var matrix = new System.Drawing.Imaging.ColorMatrix(
                    new[] {
                        new float[] { 1, 0, 0, 0, 0 },
                        new float[] { 0, 1, 0, 0, 0 },
                        new float[] { 0, 0, 1, 0, 0 },
                        new float[] { 0, 0, 0, 0.5f, 0 },
                        new float[] { 0, 0, 0, 0, 1 },
                        });
                attr.SetColorMatrix(matrix);

                //绘制阴影
                var shade = Resource.UIToolTip_img_Item_ItemIcon_shade;
                g.DrawImage(shade,
                    new Rectangle(18 + 9, picH + 15 + 54, shade.Width, shade.Height),
                    0, 0, shade.Width, shade.Height,
                    GraphicsUnit.Pixel,
                    attr);
                //绘制图标
                g.DrawImage(GearGraphics.EnlargeBitmap(Gear.IconRaw.Bitmap),
                    18 + (1 - Gear.IconRaw.Origin.X) * 2,
                    picH + 15 + (33 - Gear.IconRaw.Origin.Y) * 2);

                attr.Dispose();
            }
            if (Gear.Cash && !(Gear.Props.TryGetValue(GearPropType.mintable, out value) && value != 0)) //绘制cash标识
            {
                Bitmap cashImg = null;
                Point cashOrigin = new Point(12, 12);

                if (Gear.Props.TryGetValue(GearPropType.royalSpecial, out value) && value > 0)
                {
                    string resKey = $"CashShop_img_CashItem_label_{value - 1}";
                    cashImg = Resource.ResourceManager.GetObject(resKey) as Bitmap;
                }
                else if (Gear.Props.TryGetValue(GearPropType.masterSpecial, out value) && value > 0)
                {
                    cashImg = Resource.CashShop_img_CashItem_label_3;
                }
                else if (Gear.Props.TryGetValue(GearPropType.BTSLabel, out value) && value > 0)
                {
                    cashImg = Resource.CashShop_img_CashItem_label_10;
                    cashOrigin = new Point(cashImg.Width, cashImg.Height);
                }
                else if (Gear.Props.TryGetValue(GearPropType.BLACKPINKLabel, out value) && value > 0)
                {
                    cashImg = Resource.CashShop_img_CashItem_label_11;
                    cashOrigin = new Point(cashImg.Width, cashImg.Height);
                }
                else if (Gear.Props.TryGetValue(GearPropType.illusionGrade, out value) && value > 0)
                {
                    switch (value)
                    {
                        case 1:
                            cashImg = Resource.CashShop_img_CashItem_label_12;
                            cashOrigin = new Point(cashImg.Width, cashImg.Height);
                            break;
                        case 2:
                            cashImg = Resource.CashShop_img_CashItem_label_13;
                            cashOrigin = new Point(cashImg.Width, cashImg.Height);
                            break;
                        case 3:
                            cashImg = Resource.CashShop_img_CashItem_label_14;
                            cashOrigin = new Point(cashImg.Width, cashImg.Height);
                            break;
                    }
                }
                if (cashImg == null) //default cashImg
                {
                    cashImg = Resource.CashItem_0;
                }

                g.DrawImage(GearGraphics.EnlargeBitmap(cashImg),
                    18 + 68 - cashOrigin.X * 2 - 2,
                    picH + 15 + 68 - cashOrigin.Y * 2 - 2);
            }
            //检查星岩
            bool hasSocket = Gear.GetBooleanValue(GearPropType.nActivatedSocket);
            if (hasSocket)
            {
                Bitmap socketBmp = GetAlienStoneIcon();
                if (socketBmp != null)
                {
                    g.DrawImage(GearGraphics.EnlargeBitmap(socketBmp),
                        18 + 2,
                        picH + 15 + 3);
                }
            }

            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_old, 14 - 2 + 5, picH + 9 + 5);
            g.DrawImage(Resource.UIToolTip_img_Item_ItemIcon_cover, 16, picH + 14); //绘制左上角cover

            //绘制攻击力变化
            format.Alignment = StringAlignment.Far;
            TextRenderer.DrawText(g, "Attack Power Increase", GearGraphics.EquipDetailFont, new Point(248 - TextRenderer.MeasureText(g, "Attack Power Increase", GearGraphics.EquipDetailFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width, picH + 10), ((SolidBrush)GearGraphics.GrayBrush2).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_incline_0, 249 - 19, picH + 27); //暂时画个0

            //绘制属性需求
            DrawGearReq(g, 97, picH + 59);
            picH += 94;

            //绘制属性变化
            DrawPropDiffEx(g, 12, picH);
            picH += 20;

            //绘制职业需求
            DrawJobReq(g, ref picH);

            if (Gear.type == GearType.android && Gear.Props.TryGetValue(GearPropType.android, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "Appearance:", GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 15;

                Wz_Node android = PluginBase.PluginManager.FindWz(string.Format("Etc/Android/{0:D4}.img", value));
                Wz_Node costume = android?.Nodes["costume"];
                Wz_Node basic = android?.Nodes["basic"];

                BitmapOrigin appearance;
                int morphID = android?.Nodes["info"]?.Nodes["morphID"]?.GetValueEx<int>(0) ?? 0;
                if (Gear.ToolTIpPreview.Bitmap != null)
                {
                    appearance = Gear.ToolTIpPreview;
                    g.DrawImage(appearance.Bitmap, (bitmap.Width - appearance.Bitmap.Width) / 2 + 13, picH);
                    picH += appearance.Bitmap.Height;
                }
                else
                {
                    if (morphID != 0)
                    {
                        appearance = BitmapOrigin.CreateFromNode(PluginBase.PluginManager.FindWz(string.Format("Morph/{0:D4}.img/stand/0", morphID)), PluginBase.PluginManager.FindWz);
                    }
                    else
                    {
                        if (this.avatar == null)
                        {
                            this.avatar = new AvatarCanvasManager();
                        }

                        var skin = costume?.Nodes["skin"]?.Nodes["0"].GetValueEx<int>(2015);
                        var hair = costume?.Nodes["hair"]?.Nodes["0"].GetValueEx<int>(30000);
                        var face = costume?.Nodes["face"]?.Nodes["0"].GetValueEx<int>(20000);

                        this.avatar.AddBodyFromSkin4((int)skin);
                        this.avatar.AddGears([(int)hair, (int)face]);

                        if (basic != null)
                        {
                            foreach (var node in basic.Nodes)
                            {
                                var gearID = node.GetValueEx<int>(0);
                                this.avatar.AddGear(gearID);
                            }
                        }

                        appearance = this.avatar.GetBitmapOrigin();

                        this.avatar.ClearCanvas();
                    }

                    var imgrect = new Rectangle(Math.Max(appearance.Origin.X - 50, 0),
                        Math.Max(appearance.Origin.Y - 100, 0),
                        Math.Min(appearance.Bitmap.Width, appearance.Origin.X + 50) - Math.Max(appearance.Origin.X - 50, 0),
                        Math.Min(appearance.Origin.Y, 100));

                    g.DrawImage(appearance.Bitmap, 88 - Math.Min(appearance.Origin.X, 50), picH + Math.Max(80 - appearance.Origin.Y, 0), imgrect, GraphicsUnit.Pixel);
                    Gear.AndroidBitmap = appearance.Bitmap;

                    picH += 100;
                }
                //BitmapOrigin appearance = BitmapOrigin.CreateFromNode(PluginBase.PluginManager.FindWz(morphID != 0 ? string.Format("Morph/{0:D4}.img/stand/0", morphID) : "Npc/0010300.img/stand/0"), PluginBase.PluginManager.FindWz);

                //appearance.Bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);

                List<string> randomParts = new List<string>();
                if (costume?.Nodes["face"]?.Nodes["1"] != null)
                {
                    randomParts.Add("Face");
                }
                if (costume?.Nodes["hair"]?.Nodes["1"] != null)
                {
                    randomParts.Add("Hair");
                }
                if (costume?.Nodes["skin"]?.Nodes["1"] != null)
                {
                    randomParts.Add("Skin");
                }
                if (randomParts.Count > 0)
                {
                    GearGraphics.DrawString(g, $"#c{string.Join(", ", randomParts)} The image is an example. The android's appearance is determined upon equipping it for the first time.#", GearGraphics.EquipDetailFont, orange2FontColorTable, 13, 244, ref picH, 15);
                }
            }

            //分割线2号
            g.DrawImage(res["dotline"].Image, 0, picH);
            picH += 8;

            bool hasPart2 = Gear.Cash;
            format.Alignment = StringAlignment.Center;

            //绘制属性
            if (Gear.Props.TryGetValue(GearPropType.superiorEqp, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "Superior", GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.HorizontalCenter);
                picH += 15;
            }
            if (Gear.Props.TryGetValue(GearPropType.limitBreak, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "Damage Cap", GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.HorizontalCenter);
                picH += 16;
            }

            //绘制装备升级
            if (Gear.Props.TryGetValue(GearPropType.level, out value) && !Gear.FixLevel)
            {
                bool max = (Gear.Levels != null && value >= Gear.Levels.Count);
                TextRenderer.DrawText(g, "Growth Level: " + (max ? "MAX" : value.ToString()), GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                picH += 15;
                string expString = Gear.Levels != null && Gear.Levels.First().Point != 0 ? ": 0/" + Gear.Levels.First().Point : ": 0%";
                TextRenderer.DrawText(g, "Growth EXP" + (max ? ": MAX" : expString), GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
            }
            else if (Gear.ItemID / 1000 == 1712)
            {
                TextRenderer.DrawText(g, "Growth Level: 1", GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                picH += 15;
                TextRenderer.DrawText(g, "Item EXP: 1 / 12 ( 8% )", GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                picH += 15;
            }
            else if (Gear.ItemID / 1000 == 1713)
            {
                TextRenderer.DrawText(g, "Growth Level: 1", GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
                TextRenderer.DrawText(g, "Item EXP : 1 / 29 ( 3% )", GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
            }
            else if (Gear.ItemID / 1000 == 1714)
            {
                TextRenderer.DrawText(g, "Growth Level : 1", GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
                TextRenderer.DrawText(g, "Item EXP : 1 / 29 ( 3% )", GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
                foreach (var prop in new[] {"EXP Obtained : +10%", "Mesos Obtained : +5%", "Item Drop Rate : +5%" })
                {
                    TextRenderer.DrawText(g, prop, GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 15;
                }
            }

            if (Gear.Props.TryGetValue(GearPropType.@sealed, out value))
            {
                bool max = (Gear.Seals != null && value >= Gear.Seals.Count);
                TextRenderer.DrawText(g, "Unseal Stage : " + (max ? "MAX" : value.ToString()), GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                picH += 15;
                TextRenderer.DrawText(g, "Unseal EXP : " + (max ? "MAX" : "0%"), GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                picH += 15;
            }

            //绘制耐久度
            if (Gear.Props.TryGetValue(GearPropType.durability, out value))
            {
                TextRenderer.DrawText(g, "Durability: " + "100%", GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                picH += 15;
            }

            //装备类型 - blank typeStr are for One-handed Weapon and Two-handed Weapon respectively
            bool isWeapon = Gear.IsWeapon(Gear.type);
            string typeStr = ItemStringHelper.GetGearTypeString(Gear.type);
            if (!string.IsNullOrEmpty(typeStr) && (int)Gear.type / 10 != 171)
            {
                if (isWeapon)
                {
                    typeStr = "Type: " + typeStr;
                }
                else
                {
                    typeStr = "Type: " + typeStr;
                }

                if (!Gear.Cash && (Gear.IsLeftWeapon(Gear.type) || Gear.type == GearType.katara))
                {
                    typeStr += "";
                }
                else if (!Gear.Cash && Gear.IsDoubleHandWeapon(Gear.type))
                {
                    typeStr += "";
                }
                TextRenderer.DrawText(g, typeStr, GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                picH += 15;
                hasPart2 = true;
            }

            if (!Gear.Props.TryGetValue(GearPropType.attackSpeed, out value)
                && (Gear.IsWeapon(Gear.type) || Gear.type == GearType.katara)) //找不到攻速的武器
            {
                value = 6; //给予默认速度
            }
            //  if (gear.Props.TryGetValue(GearPropType.attackSpeed, out value) && value > 0)
            if (!Gear.Cash && value > 0)
            {
                bool isValidSpeed = (2 <= value && value <= 9);
                string speedStr = string.Format("Attack Speed: {0}{1}{2}",
                    ItemStringHelper.GetAttackSpeedString(value),
                    isValidSpeed ? $" (Stage {10 - value})" : null,
                    ShowSpeed ? $" ({value})" : null
                );

                TextRenderer.DrawText(g, speedStr, GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 15;
                hasPart2 = true;
            }
            //机器人等级
            if (Gear.Props.TryGetValue(GearPropType.grade, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "Rank: " + value, GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                picH += 15;
                hasPart2 = true;
            }
            //Regular Cosmetic
            if (Gear.type == GearType.body || Gear.type == GearType.head || Gear.type == GearType.face || Gear.type == GearType.face2 || Gear.type == GearType.hair || Gear.type == GearType.hair2 || Gear.type == GearType.hair3)
            {
                if (this.avatar == null)
                {
                    this.avatar = new AvatarCanvasManager();
                }
                if (Gear.type == GearType.body) this.avatar.AddBodyFromSkin4(Gear.ItemID);
                if (Gear.type == GearType.head) this.avatar.AddBodyFromSkin4(Gear.ItemID - 10000);
                else this.avatar.AddBodyFromSkin4(2015);
                if (Gear.type == GearType.face || Gear.type == GearType.face2 || Gear.type == GearType.hair || Gear.type == GearType.hair2 || Gear.type == GearType.hair3) this.avatar.AddHairOrFace(Gear.ItemID);

                this.avatar.AddGears([1042194, 1062153]);

                var appearance = this.avatar.GetBitmapOrigin();
                if (appearance.Bitmap != null)
                {
                    var imgrect = new Rectangle(Math.Max(appearance.Origin.X - 50, 0),
                        Math.Max(appearance.Origin.Y - 100, 0),
                        Math.Min(appearance.Bitmap.Width, appearance.Origin.X + 50) - Math.Max(appearance.Origin.X - 50, 0),
                        Math.Min(appearance.Origin.Y, 100));
                    g.DrawImage(appearance.Bitmap, 88 - Math.Min(appearance.Origin.X, 50), picH + Math.Max(80 - appearance.Origin.Y, 0), imgrect, GraphicsUnit.Pixel);
                    Gear.AndroidBitmap = appearance.Bitmap;
                    picH += appearance.Bitmap.Height;
                    picH += 2;

                    Gear.AndroidBitmap = appearance.Bitmap;
                    picH += 30;
                }

                this.avatar.ClearCanvas();
            }
            //MSN Cosmetic
            if ((Gear.type == GearType.face_n || Gear.type == GearType.hair_n) && Gear.Props.TryGetValue(GearPropType.cosmetic, out value) && value > 0)
            {
                string colorName = "";
                if (Gear.type == GearType.hair_n) colorName = AvatarCanvas.HairColor[this.CosmeticHairColor];
                else if (Gear.type == GearType.face_n) colorName = AvatarCanvas.HairColor[this.CosmeticFaceColor];
                GearGraphics.DrawString(g, $"Color: #c{colorName}#", GearGraphics.EquipDetailFont, orange2FontColorTable, 13, 244, ref picH, 15);
                TextRenderer.DrawText(g, "Appearance: ", GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                if (this.avatar == null)
                {
                    this.avatar = new AvatarCanvasManager();
                }

                this.avatar.SetCosmeticColor(this.CosmeticHairColor, this.CosmeticFaceColor);

                if (value < 1000)
                {
                    this.avatar.AddBodyFromSkin3((int)value);
                }
                else
                {
                    this.avatar.AddBodyFromSkin4(2015);
                    this.avatar.AddHairOrFace((int)value);
                }

                this.avatar.AddGears([1042194, 1062153]);

                var appearance = this.avatar.GetBitmapOrigin();
                if (appearance.Bitmap != null)
                {
                    var imgrect = new Rectangle(Math.Max(appearance.Origin.X - 50, 0),
                        Math.Max(appearance.Origin.Y - 100, 0),
                        Math.Min(appearance.Bitmap.Width, appearance.Origin.X + 50) - Math.Max(appearance.Origin.X - 50, 0),
                        Math.Min(appearance.Origin.Y, 100));
                    g.DrawImage(appearance.Bitmap, 88 - Math.Min(appearance.Origin.X, 50), picH + Math.Max(80 - appearance.Origin.Y, 0), imgrect, GraphicsUnit.Pixel);
                    Gear.AndroidBitmap = appearance.Bitmap;
                    picH += appearance.Bitmap.Height;
                    picH += 2;

                    Gear.AndroidBitmap = appearance.Bitmap;
                    picH += 30;
                }

                this.avatar.ClearCanvas();
            }

            //一般属性
            List<GearPropType> props = new List<GearPropType>();
            foreach (KeyValuePair<GearPropType, int> p in Gear.PropsV5) //5转过滤
            {
                if ((int)p.Key < 100 && p.Value != 0)
                    props.Add(p.Key);
            }
            foreach (KeyValuePair<GearPropType, int> p in Gear.AbilityTimeLimited)
            {
                if ((int)p.Key < 100 && p.Value != 0 && !props.Contains(p.Key))
                    props.Add(p.Key);
            }
            props.Sort();
            //bool epic = Gear.Props.TryGetValue(GearPropType.epicItem, out value) && value > 0;
            foreach (GearPropType type in props)
            {
                //var font = (epic && Gear.IsEpicPropType(type)) ? GearGraphics.EpicGearDetailFont : GearGraphics.EquipDetailFont;
                //g.DrawString(ItemStringHelper.GetGearPropString(type, Gear.Props[type]), font, Brushes.White, 11, picH);
                //picH += 16;

                //绘制属性变化
                Gear.StandardProps.TryGetValue(type, out value); //standard value
                if (value > 0 || Gear.Props[type] > 0)
                {
                    var propStr = ItemStringHelper.GetGearPropDiffString(type, Gear.Props[type], value);
                    GearGraphics.DrawString(g, propStr, GearGraphics.EquipDetailFont, 12, 256, ref picH, 15);//changes the vertical distance of stats in tooltip, such as STR, DEX, INT etc.
                    hasPart2 = true;
                }
            }

            //戒指特殊潜能 (Ring Special Potential)
            int ringOpt, ringOptLv;
            if (Gear.Props.TryGetValue(GearPropType.ringOptionSkill, out ringOpt)
                && Gear.Props.TryGetValue(GearPropType.ringOptionSkillLv, out ringOptLv))
            {
                var opt = Potential.LoadFromWz(ringOpt, ringOptLv, PluginBase.PluginManager.FindWz);
                if (opt != null)
                {
                    TextRenderer.DrawText(g, opt.ConvertSummary(), GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    picH += 16;
                    hasPart2 = true;
                }
            }

            bool hasReduce = Gear.Props.TryGetValue(GearPropType.reduceReq, out value);
            if (hasReduce && value > 0)
            {
                TextRenderer.DrawText(g, ItemStringHelper.GetGearPropString(GearPropType.reduceReq, value), GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                picH += 15;
                hasPart2 = true;
            }

            bool hasTuc = Gear.HasTuc && Gear.Props.TryGetValue(GearPropType.tuc, out value);
            if (Gear.GetBooleanValue(GearPropType.exceptUpgrade))
            {
                TextRenderer.DrawText(g, "Unable to enhance", GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                picH += 15;
            }
            else if (Gear.GetBooleanValue(GearPropType.blockUpgradeStarforce))
            {
                g.DrawString("Cannot Star Force Enhance", GearGraphics.EquipDetailFont, GearGraphics.BlockRedBrush, 11, picH);
                picH += 15;
            }
            else if (hasTuc && !Gear.GetBooleanValue(GearPropType.blockUpgradeStarforce))
            {
                var colorTable = new Dictionary<string, Color>
                {
                    { "c", GearGraphics.OrangeBrush3Color }
                };
                GearGraphics.DrawString(g, "Remaining Enhancements: " + value + (Gear.Cash ? "" : "\n#c(Available Recoveries: 0)#"), GearGraphics.EquipDetailFont, colorTable, 13, 244, ref picH, 15);
                hasPart2 = true;
            }

            if (!Gear.CanPotential && !Gear.Cash)
            {
                TextRenderer.DrawText(g, "This item cannot gain Potential.", GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 15;
            }
            if (Gear.Props.TryGetValue(GearPropType.fixedPotential, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "Cannot Set Bonus Potential", GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 15;
            }

            //星星锤子
            if (hasTuc && Gear.Hammer > -1 && Gear.GetMaxStar(isPostNEXTClient) > 0 && !Gear.GetBooleanValue(GearPropType.blockUpgradeStarforce))
            {
                if (Gear.Hammer == 2)
                {
                    TextRenderer.DrawText(g, "", GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    picH += 15;
                }
                if (Gear.Props.TryGetValue(GearPropType.superiorEqp, out value) && value > 0) //极真
                {
                    GearGraphics.DrawPlainText(g, ItemStringHelper.GetGearPropString(GearPropType.superiorEqp, value), GearGraphics.EquipDetailFont, ((SolidBrush)GearGraphics.GreenBrush2).Color, 12, 256, ref picH, 15);//GMS - Superior green line change 
                }
            }
            if (Gear.Props.TryGetValue(GearPropType.CuttableCount, out value) && value > 0) //可使用剪刀
            {
                g.DrawString(ItemStringHelper.GetGearPropString(GearPropType.CuttableCount, value), GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush3, 11, picH);
                picH += 15;
                hasPart2 = true;
            }

            if (Gear.Props.TryGetValue(GearPropType.limitBreak, out value) && value > 0) //突破上限
            {
                TextRenderer.DrawText(g, ItemStringHelper.GetGearPropString(GearPropType.limitBreak, value), GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding);
                picH += 15;
                hasPart2 = true;
            }

            if (Gear.GetBooleanValue(GearPropType.blockUpgradeExtraOption))
            {
                g.DrawString("Cannot Set/Reset Bonus Stats", GearGraphics.EquipDetailFont, GearGraphics.BlockRedBrush, 11, picH);
                picH += 15;
            }

            if (hasTuc && Gear.Hammer > -1 && !Gear.GetBooleanValue(GearPropType.blockUpgradeStarforce))
            {
                TextRenderer.DrawText(g, "Hammers Applied", GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.DrawString(": " + Gear.Hammer.ToString() + (Gear.Hammer == 2 ? "(MAX)" : null), GearGraphics.EquipDetailFont, Brushes.White, 93, picH + 1);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
                picH += 15;
                hasPart2 = true;
            }

            picH += 3; //original value is '8'. 3 is perfect.

            /*if (hasTuc && Gear.PlatinumHammer > -1 && !Gear.GetBooleanValue(GearPropType.blockUpgradeStarforce))
            {
                g.DrawString("白金锤强化次数:" + Gear.PlatinumHammer, GearGraphics.ItemDetailFont, Brushes.White, 11, picH);// CMS 'Platinum Hammer'
                picH += 16;
                hasPart2 = true;
            }*/

            if (Gear.type == GearType.shovel || Gear.type == GearType.pickaxe)
            {
                string skillName = null;
                switch (Gear.type)
                {
                    case GearType.shovel: skillName = "Herbalism"; break;
                    case GearType.pickaxe: skillName = "Mining"; break;
                }
                if (Gear.Props.TryGetValue(GearPropType.gatherTool_incSkillLevel, out value) && value > 0)
                {
                    TextRenderer.DrawText(g, skillName + " Skill: +" + value, GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 15;
                    hasPart2 = true;
                }
                if (Gear.Props.TryGetValue(GearPropType.gatherTool_incSpeed, out value) && value > 0)
                {
                    TextRenderer.DrawText(g, skillName + " Speed: +" + value + "%", GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 15;
                    hasPart2 = true;
                }
                if (Gear.Props.TryGetValue(GearPropType.gatherTool_incNum, out value) && value > 0)
                {
                    TextRenderer.DrawText(g, "Up to " + value + " items can be acquired", GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 15;
                    hasPart2 = true;
                }
                if (Gear.Props.TryGetValue(GearPropType.gatherTool_reqSkillLevel, out value) && value > 0)
                {
                    TextRenderer.DrawText(g, "Requires " + value + " " + skillName + " Skill ", GearGraphics.EquipDetailFont, new Point(12, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 15;
                    hasPart2 = true;
                }
            }

            picH += 5;

            //绘制浮动属性
            if ((Gear.VariableStat != null && Gear.VariableStat.Count > 0))
            {
                if (hasPart2) //分割线...
                {
                    picH -= 1;
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }

                int reqLvl;
                Gear.Props.TryGetValue(GearPropType.reqLevel, out reqLvl);
                TextRenderer.DrawText(g, "Stats increase with Character Level (Up to Lv. " + reqLvl + ")", GearGraphics.EquipDetailFont, new Point(261, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.HorizontalCenter);
                picH += 20;

                int reduceLvl;
                Gear.Props.TryGetValue(GearPropType.reduceReq, out reduceLvl);

                int curLevel = charStat == null ? reqLvl : Math.Min(charStat.Level, reqLvl);

                foreach (var kv in Gear.VariableStat)
                {
                    int dLevel = curLevel - reqLvl + reduceLvl;
                    //int addVal = (int)Math.Floor(kv.Value * dLevel);
                    //这里有一个计算上的错误 换方式执行
                    int addVal = (int)Math.Floor(new decimal(kv.Value) * dLevel);
                    string text = ItemStringHelper.GetGearPropString(kv.Key, addVal, 1);
                    text += string.Format(" ({0:f1} x {1})", kv.Value, dLevel);
                    TextRenderer.DrawText(g, text, GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    picH += 20;
                }

                if (hasReduce)
                {
                    TextRenderer.DrawText(g, "When upgrading or enhancing, it will be viewed as  " + reqLvl, GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.GrayBrush2).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);//text is cut-off in GMS as well, once it is fixed, add >> Lv. << after 'as'.
                    picH += 16;
                }
            }

            //绘制潜能 (Drawing Potential)
            int optionCount = 0;
            foreach (Potential potential in Gear.Options)
            {
                if (potential != null)
                {
                    optionCount++;
                }
            }

            if (optionCount > 0)
            {
                //分割线3号
                if (hasPart2)
                {
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }
                g.DrawImage(GetAdditionalOptionIcon(Gear.Grade), 9, picH - 2);
                TextRenderer.DrawText(g, "Potential", GearGraphics.EquipDetailFont, new Point(27, picH), ((SolidBrush)GearGraphics.GetPotentialTextBrush(Gear.Grade)).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                picH += 15;
                foreach (Potential potential in Gear.Options)
                {
                    if (potential != null)
                    {
                        GearGraphics.DrawString(g, potential.ConvertSummary(), GearGraphics.EquipDetailFont2, 12, 244, ref picH, 15);
                        //TextRenderer.DrawText(g, potential.ConvertSummary(), GearGraphics.EquipDetailFont2, new Point(11, picH), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                        //picH += 15;
                    }
                }
            }

            if (hasSocket)
            {
                g.DrawLine(Pens.White, 6, picH, 254, picH);
                picH += 8;
                GearGraphics.DrawString(g, ItemStringHelper.GetGearPropString(GearPropType.nActivatedSocket, 1),
                    GearGraphics.EquipDetailFont, 12, 244, ref picH, 16);
                picH += 3;
            }

            //绘制附加潜能
            int adOptionCount = 0;
            foreach (Potential potential in Gear.AdditionalOptions)
            {
                if (potential != null)
                {
                    adOptionCount++;
                }
            }
            if (adOptionCount > 0)
            {
                //分割线4号
                if (hasPart2)
                {
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }
                g.DrawImage(GetAdditionalOptionIcon(Gear.AdditionGrade), 9, picH - 2);
                TextRenderer.DrawText(g, "Bonus Potential", GearGraphics.EquipDetailFont, new Point(27, picH), ((SolidBrush)GearGraphics.GetPotentialTextBrush(Gear.AdditionGrade)).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                picH += 15;

                foreach (Potential potential in Gear.AdditionalOptions)
                {
                    if (potential != null)
                    {
                        GearGraphics.DrawString(g, "+ " + potential.ConvertSummary(), GearGraphics.EquipDetailFont2, 12, 244, ref picH, 15);
                        //TextRenderer.DrawText(g, "+ " + potential.ConvertSummary(), GearGraphics.EquipDetailFont2, new Point(11, picH), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                        //picH += 15;
                    }
                }
                //picH += 5;
            }

            if (Gear.Props.TryGetValue(GearPropType.Etuc, out value) && value > 0)
            {
                //分割线5号
                if (hasPart2)
                {
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }
                TextRenderer.DrawText(g, ItemStringHelper.GetGearPropString(GearPropType.Etuc, value), GearGraphics.EquipDetailFont, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 39;
            }

            //绘制desc
            List<string> desc = new List<string>();
            GearPropType[] descTypes = new GearPropType[]{
                GearPropType.tradeAvailable,
                GearPropType.accountShareTag,
                GearPropType.jokerToSetItem,
                GearPropType.plusToSetItem,
                GearPropType.colorvar,
            };
            foreach (GearPropType type in descTypes)
            {
                if (Gear.Props.TryGetValue(type, out value) && value != 0)
                {
                    //desc.Add(" " + ItemStringHelper.GetGearPropString(type, value)); (Korean edit code)
                    desc.Add(ItemStringHelper.GetGearPropString(type, value));
                }
            }

            if (!string.IsNullOrEmpty(Gear.EpicHs) && sr[Gear.EpicHs] != null)
            {
                switch (Translator.DefaultPreferredLayout)
                {
                    case 1:
                        desc.Add(Translator.TranslateString(sr[Gear.EpicHs]).Replace("#", " #"));
                        desc.Add(sr[Gear.EpicHs].Replace("#", " #"));
                        break;
                    case 2:
                        desc.Add(sr[Gear.EpicHs].Replace("#", " #"));
                        desc.Add(Translator.TranslateString(sr[Gear.EpicHs]).Replace("#", " #"));
                        break;
                    case 3:
                        desc.Add(Translator.TranslateString(sr[Gear.EpicHs]).Replace("#", " #"));
                        break;
                    default:
                        desc.Add(sr[Gear.EpicHs].Replace("#", " #"));
                        break;
                }
            }

            //绘制倾向
            if (Gear.State == GearState.itemList)
            {
                StringBuilder incline = new StringBuilder();
                GearPropType[] inclineTypes = new GearPropType[]{
                    GearPropType.charismaEXP,
                    GearPropType.insightEXP,
                    GearPropType.willEXP,
                    GearPropType.craftEXP,
                    GearPropType.senseEXP,
                    GearPropType.charmEXP };

                string[] inclineString = new string[]{
                    " Ambition"," Insight"," Willpower"," Diligence"," Empathy"," Charm"};

                for (int i = 0; i < inclineTypes.Length; i++)
                {
                    bool success = Gear.Props.TryGetValue(inclineTypes[i], out value);

                    if (inclineTypes[i] == GearPropType.charmEXP && Gear.Cash)
                    {
                        success = true;
                        switch (Gear.type)
                        {
                            case GearType.cashWeapon: value = 60; break;
                            /*case GearType.shield:
                            case GearType.katara: value = 60; break;*/
                            case GearType.cap: value = 50; break;
                            case GearType.cape: value = 30; break;
                            case GearType.longcoat: value = 60; break;
                            case GearType.coat: value = 30; break;
                            case GearType.pants: value = 30; break;
                            case GearType.shoes: value = 40; break;
                            case GearType.glove: value = 40; break;
                            case GearType.earrings: value = 40; break;
                            case GearType.faceAccessory: value = 40; break;
                            case GearType.eyeAccessory: value = 40; break;
                            default: success = false; break;
                        }

                        if (Gear.Props.TryGetValue(GearPropType.cashForceCharmExp, out value2))
                        {
                            success = true;
                            value = value2;
                        }
                    }

                    if (success && value > 0)
                    {
                        if (incline.Length > 0)
                        {
                            incline.Append(", ");
                        }
                        incline.Append(value + inclineString[i]); // GMS lists it inverse to CMS, i.e. 90 Ambition instead of Ambition 90
                    }
                }

                if (incline.Length > 0)
                {
                    desc.Add($"#cGrants {incline} EXP when first equipped (up to the daily maximum, excluded if max reached).");
                }

                if (Gear.Cash && (!Gear.Props.TryGetValue(GearPropType.noMoveToLocker, out value) || value == 0) && (!Gear.Props.TryGetValue(GearPropType.tradeBlock, out value) || value == 0) && (!Gear.Props.TryGetValue(GearPropType.accountSharable, out value) || value == 0))
                {
                    desc.Add("#cThis item cannot be traded once it has been used.#");
                }

                if (Gear.type != GearType.pickaxe && Gear.type != GearType.shovel && PluginBase.PluginManager.FindWz(string.Format("Effect/ItemEff.img/{0}/effect", Gear.ItemID)) != null)
                {
                    desc.Add("This item does not show in some situations, such as in the #cCharacter Info window.#");
                }

                if (Gear.Props.TryGetValue(GearPropType.noPetEquipStatMoveItem, out value) && value != 0)
                {
                    desc.Add("");
                    desc.Add("You cannot use the Pet Equipment Stat Transfer Scroll on this item.");
                }

                if (desc.Count >= 1 && desc.Last() == "")
                {
                    desc.RemoveAt(desc.Count - 1);
                }
            }

            //判断是否绘制徽章
            Wz_Node medalResNode = null;
            Wz_Node chatBalloonResNode = null;
            Wz_Node nameTagResNode = null;
            bool willDrawMedalTag = this.Gear.Sample.Bitmap == null
                && this.Gear.Props.TryGetValue(GearPropType.medalTag, out value)
                && this.TryGetMedalResource(value, 0, out medalResNode);
            bool willDrawChatBalloon = this.Gear.Props.TryGetValue(GearPropType.chatBalloon, out value)
                && this.TryGetMedalResource(value, 1, out chatBalloonResNode);
            bool willDrawNameTag = this.Gear.Props.TryGetValue(GearPropType.nameTag, out value)
                && this.TryGetMedalResource(value, 2, out nameTagResNode);

            //判断是否绘制技能desc
            string levelDesc = null;
            if (Gear.FixLevel && Gear.Props.TryGetValue(GearPropType.level, out value))
            {
                var levelInfo = Gear.Levels.FirstOrDefault(info => info.Level == value);
                if (levelInfo != null && levelInfo.Prob == levelInfo.ProbTotal && !string.IsNullOrEmpty(levelInfo.HS))
                {
                    levelDesc = sr[levelInfo.HS];
                }
            }

            if (!string.IsNullOrEmpty(sr.Desc) || !string.IsNullOrEmpty(levelDesc) || desc.Count > 0 || Gear.Sample.Bitmap != null || willDrawMedalTag || willDrawChatBalloon || willDrawNameTag)
            {
                //分割线4号
                if (hasPart2)
                {
                    g.DrawImage(res["dotline"].Image, 0, picH);
                    picH += 8;
                }
                if (willDrawChatBalloon)
                {
                    GearGraphics.DrawChatBalloon(g, chatBalloonResNode, "MAPLESTORY", bitmap.Width - 10, ref picH);
                    picH += 4;
                }
                else if (willDrawNameTag)
                {
                    GearGraphics.DrawNameTag(g, nameTagResNode, "MAPLESTORY", bitmap.Width - 10, ref picH);
                    picH += 4;
                }
                else if (Gear.Sample.Bitmap != null)
                {
                    g.DrawImage(Gear.Sample.Bitmap, (bitmap.Width - Gear.Sample.Bitmap.Width) / 2, picH);
                    picH += Gear.Sample.Bitmap.Height;
                    picH += 4;
                }
                else if (medalResNode != null)
                {
                    //GearGraphics.DrawNameTag(g, medalResNode, sr.Name, bitmap.Width, ref picH);2 juni
                    GearGraphics.DrawNameTag(g, medalResNode, sr.Name.Replace("Medal of Honor", ""), bitmap.Width, ref picH);
                    picH += 4;
                }
                if (!string.IsNullOrEmpty(sr.Desc))
                {
                    if (isTranslateRequired)
                    {
                        string translatedDesc = Translator.MergeString(sr.Desc.Replace("#", " #"), Translator.TranslateString(sr.Desc).Replace("#", " #"), 2);
                        GearGraphics.DrawString(g, translatedDesc, GearGraphics.EquipDetailFont2, orange2FontColorTable, 10, 243, ref picH, 15);
                    }
                    else
                    {

                        GearGraphics.DrawString(g, sr.Desc.Replace("#", " #"), GearGraphics.EquipDetailFont2, orange2FontColorTable, 10, 243, ref picH, 15);
                    }
                }
                if (!string.IsNullOrEmpty(levelDesc))
                {
                    GearGraphics.DrawString(g, " " + levelDesc, GearGraphics.EquipDetailFont2, orange2FontColorTable, 10, 243, ref picH, 15);
                }
                foreach (string str in desc)
                {
                    GearGraphics.DrawString(g, str, GearGraphics.EquipDetailFont, orange2FontColorTable, 10, 243, ref picH, 15);
                }
                picH += 5;
            }

            foreach (KeyValuePair<int, ExclusiveEquip> kv in CharaSimLoader.LoadedExclusiveEquips)
            {
                if (kv.Value.Items.Contains(Gear.ItemID))
                {
                    /*if (hasPart2)
                    {
                        g.DrawImage(res["dotline"].Image, 0, picH);
                        picH += 8;
                    }*/ //pr - Suppress ExclusiveEquip line for same gear name

                    string exclusiveEquip;
                    if (!string.IsNullOrEmpty(kv.Value.Info))
                    {
                        exclusiveEquip = "#cCannot equip multiple " + kv.Value.Info + " items.#";
                    }
                    else
                    {
                        List<string> itemNames = new List<string>();
                        foreach (int itemID in kv.Value.Items)
                        {
                            StringResult sr2;
                            if (this.StringLinker == null || !this.StringLinker.StringEqp.TryGetValue(itemID, out sr2))
                            {
                                sr2 = new StringResult();
                                sr2.Name = "(null)";
                            }
                            if (!itemNames.Contains(sr2.Name))
                            {
                                itemNames.Add(sr2.Name);
                            }
                        }
                        if (itemNames.Count == 1)
                        {
                            break;
                        }
                        //exclusiveEquip = "#c" + string.Join(", ", itemNames.ToArray()); //pr - Suppress ExclusiveEquip line for same gear name


                        /*char lastCharacter = itemNames.Last().Last();
                        if (lastCharacter >= 44032 && lastCharacter <= 55203 && (lastCharacter - 44032) % 28 == 0)
                            exclusiveEquip = "#cCannot equip multiple " + string.Join(", ", itemNames.ToArray()) + " items.#";
                        else*/ //Only for Korean branch
                        exclusiveEquip = "#cCannot equip multiple " + string.Join(", ", itemNames.ToArray()) + " items.#";
                    }

                    if (hasPart2)
                    {
                        g.DrawImage(res["dotline"].Image, 0, picH);
                        picH += 8;
                    }
                    GearGraphics.DrawString(g, exclusiveEquip, GearGraphics.EquipDetailFont2, orange2FontColorTable, 13, 244, ref picH, 15);
                    picH += 5;
                    break;
                }
            }

            picH += 2;
            format.Dispose();
            g.Dispose();
            return bitmap;
        }

        private Bitmap RenderAddition(out int picHeight)
        {
            Bitmap addBitmap = null;
            picHeight = 0;
            if (Gear.Additions.Count > 0 && !Gear.AdditionHideDesc)
            {
                addBitmap = new Bitmap(261, DefaultPicHeight);
                Graphics g = Graphics.FromImage(addBitmap);
                StringBuilder sb = new StringBuilder();
                foreach (Addition addition in Gear.Additions)
                {
                    string conString = addition.GetConString(), propString = addition.GetPropString();
                    if (!string.IsNullOrEmpty(conString) || !string.IsNullOrEmpty(propString))
                    {
                        sb.Append("- ");
                        if (!string.IsNullOrEmpty(conString))
                            sb.AppendLine(conString);
                        if (!string.IsNullOrEmpty(propString))
                            sb.AppendLine(propString);
                        sb.AppendLine();
                    }
                }
                if (sb.Length > 0)
                {
                    picHeight = 10;
                    GearGraphics.DrawString(g, sb.ToString(), GearGraphics.EquipDetailFont, 12, 250, ref picHeight, 15);
                }
                g.Dispose();
            }
            return addBitmap;
        }

        private Bitmap RenderSetItem(out int picHeight)
        {
            Bitmap setBitmap = null;
            int setID;
            picHeight = 0;
            if (Gear.Props.TryGetValue(GearPropType.setItemID, out setID))
            {
                SetItem setItem;
                if (CompareMode)
                {
                    setItem = CharaSimLoader.LoadSetItem(setID, this.SourceWzFile);
                    if (setItem == null) return null;
                }
                else if (!CharaSimLoader.LoadedSetItems.TryGetValue(setID, out setItem))
                    return null;

                TooltipRender renderer = this.SetItemRender;
                if (renderer == null)
                {
                    var defaultRenderer = new SetItemTooltipRender();
                    defaultRenderer.StringLinker = this.StringLinker;
                    defaultRenderer.ShowObjectID = false;
                    renderer = defaultRenderer;
                }

                renderer.TargetItem = setItem;
                setBitmap = renderer.Render();
                if (setBitmap != null)
                    picHeight = setBitmap.Height;
            }
            return setBitmap;
        }

        private Bitmap RenderLevelOrSealed(out int picHeight)
        {
            Bitmap levelOrSealed = null;
            Graphics g = null;
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            picHeight = 0;
            if (Gear.Levels != null)
            {
                if (levelOrSealed == null)
                {
                    levelOrSealed = new Bitmap(261, DefaultPicHeight);
                    g = Graphics.FromImage(levelOrSealed);
                }
                picHeight += 13;
                TextRenderer.DrawText(g, "Growth Stats", GearGraphics.EquipDetailFont, new Point(261, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.HorizontalCenter);
                picHeight += 15;
                if (Gear.FixLevel)
                {
                    TextRenderer.DrawText(g, "[Fixed level at acquisition]", GearGraphics.EquipDetailFont, new Point(261, picHeight), ((SolidBrush)GearGraphics.OrangeBrush).Color, TextFormatFlags.HorizontalCenter);
                    picHeight += 16;
                }

                for (int i = 0; i < Gear.Levels.Count; i++)
                {
                    var info = Gear.Levels[i];
                    TextRenderer.DrawText(g, "Level " + info.Level + (i >= Gear.Levels.Count - 1 ? " (MAX)" : null), GearGraphics.EquipDetailFont, new Point(12, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    picHeight += 15;
                    foreach (var kv in info.BonusProps)
                    {
                        GearLevelInfo.Range range = kv.Value;

                        string propString = ItemStringHelper.GetGearPropString(kv.Key, kv.Value.Min);
                        if (propString != null)
                        {
                            if (range.Max != range.Min)
                            {
                                propString += " ~ " + kv.Value.Max + (propString.EndsWith("%") ? "%" : null);
                            }
                            TextRenderer.DrawText(g, propString, GearGraphics.EquipDetailFont, new Point(12, picHeight), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                            picHeight += 15;
                        }
                    }
                    if (info.Skills.Count > 0)
                    {
                        string title = string.Format("{2:P2} ({0}/{1}) chance to obtain a skill:", info.Prob, info.ProbTotal, info.Prob * 1.0 / info.ProbTotal);
                        TextRenderer.DrawText(g, title, GearGraphics.EquipDetailFont, new Point(12, picHeight), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                        picHeight += 15;
                        foreach (var kv in info.Skills)
                        {
                            StringResult sr = null;
                            if (this.StringLinker != null)
                            {
                                this.StringLinker.StringSkill.TryGetValue(kv.Key, out sr);
                            }
                            string text = string.Format("+{2} {0}", sr == null ? null : sr.Name, kv.Key, kv.Value);
                            TextRenderer.DrawText(g, text, GearGraphics.EquipDetailFont, new Point(12, picHeight), ((SolidBrush)GearGraphics.OrangeBrush).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                            picHeight += 15;
                        }
                    }
                    if (info.EquipmentSkills.Count > 0)
                    {
                        string title;
                        if (info.Prob < info.ProbTotal)
                        {
                            title = string.Format("Have {2:P2}({0}/{1}) chance to obtain skill:", info.Prob, info.ProbTotal, info.Prob * 1.0 / info.ProbTotal);
                        }
                        else
                        {
                            title = "Skill obtained when equipped:";
                        }
                        TextRenderer.DrawText(g, title, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                        picHeight += 15;
                        foreach (var kv in info.EquipmentSkills)
                        {
                            StringResult sr = null;
                            if (this.StringLinker != null)
                            {
                                this.StringLinker.StringSkill.TryGetValue(kv.Key, out sr);
                            }
                            string text = string.Format("Lv{2} {0}", sr == null ? null : sr.Name, kv.Key, kv.Value);
                            TextRenderer.DrawText(g, text, GearGraphics.EquipDetailFont, new Point(10, picHeight), ((SolidBrush)GearGraphics.OrangeBrush).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                            picHeight += 15;
                        }
                    }
                    if (info.Exp > 0)
                    {
                        TextRenderer.DrawText(g, "Required EXP : " + info.Exp + "%", GearGraphics.EquipDetailFont, new Point(12, picHeight), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                        picHeight += 15;
                    }
                    if (info.Point > 0 && info.DecPoint > 0)
                    {
                        TextRenderer.DrawText(g, "Required Anti-magic : " + info.Point + " (-" + info.DecPoint + " daily)", GearGraphics.EquipDetailFont, new Point(12, picHeight), Color.White, TextFormatFlags.NoPadding);
                        picHeight += 15;
                    }

                    picHeight += 2;
                }
            }

            if (Gear.Seals != null)
            {
                if (levelOrSealed == null)
                {
                    levelOrSealed = new Bitmap(261, DefaultPicHeight);
                    g = Graphics.FromImage(levelOrSealed);
                }
                picHeight += 13;
                TextRenderer.DrawText(g, "Unsealed Stats", GearGraphics.EquipDetailFont, new Point(261, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.HorizontalCenter);
                picHeight += 16;
                for (int i = 0; i < Gear.Seals.Count; i++)
                {
                    var info = Gear.Seals[i];

                    TextRenderer.DrawText(g, "Level" + info.Level + (i >= Gear.Seals.Count - 1 ? "(MAX)" : null), GearGraphics.EquipDetailFont, new Point(10, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    picHeight += 16;
                    var props = this.IsCombineProperties ? Gear.CombineProperties(info.BonusProps) : info.BonusProps;
                    foreach (var kv in props)
                    {
                        string propString = ItemStringHelper.GetGearPropString(kv.Key, kv.Value);
                        TextRenderer.DrawText(g, propString, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                        picHeight += 16;
                    }
                    if (info.HasIcon)
                    {
                        Bitmap icon = info.Icon.Bitmap ?? info.IconRaw.Bitmap;
                        if (icon != null)
                        {
                            TextRenderer.DrawText(g, "Icon : ", GearGraphics.EquipDetailFont, new Point(10, picHeight + icon.Height / 2 - 6), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                            g.DrawImage(icon, 52, picHeight);
                            picHeight += icon.Height;
                        }
                    }
                    if (info.Exp > 0)
                    {
                        TextRenderer.DrawText(g, "Required EXP : " + info.Exp + "%", GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                        picHeight += 16;
                    }
                    picHeight += 2;
                }
            }


            format.Dispose();
            if (g != null)
            {
                g.Dispose();
                picHeight += 13;
            }
            return levelOrSealed;
        }

        private Bitmap RenderGenesisSkills(out int picHeight, bool isDestinyWeapon=false)
        {
            Bitmap genesisBitmap = null;
            picHeight = 0;
            if (Gear.IsGenesisWeapon)
            {
                genesisBitmap = new Bitmap(261, DefaultPicHeight);
                Graphics g = Graphics.FromImage(genesisBitmap);
                picHeight = 13;

                int destinySkill = 1241 * (Gear.IsDestinyWeapon ? 1 : 0);

                foreach (var skillID in new[] { 80002632, 80002633 })
                {
                    string skillName;
                    if (this.StringLinker?.StringSkill.TryGetValue(skillID, out var sr) ?? false && sr.Name != null)
                    {
                        skillName = sr.Name;
                    }
                    else
                    {
                        skillName = skillID.ToString();
                    }
                    g.DrawString($"{skillName} available", GearGraphics.ItemDetailFont, GearGraphics.GreenBrush2, 10, picHeight);
                    picHeight += 16;
                }
                picHeight += 9;
                g.Dispose();
            }
            return genesisBitmap;
        }

        private void FillRect(Graphics g, TextureBrush brush, int x, int y0, int y1)
        {
            brush.ResetTransform();
            brush.TranslateTransform(x, y0);
            g.FillRectangle(brush, x, y0, brush.Image.Width, y1 - y0);
        }

        private List<string> GetGearAttributeString()
        {
            int value;
            List<string> tags = new List<string>();

            if (Gear.Props.TryGetValue(GearPropType.only, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.only, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.tradeBlock, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.tradeBlock, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.onlyEquip, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.onlyEquip, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.mintable, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.mintable, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.abilityTimeLimited, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.abilityTimeLimited, value));
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.notExtend, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.equipTradeBlock, out value) && value != 0)
            {
                if (Gear.State == GearState.itemList)
                {
                    tags.Add(ItemStringHelper.GetGearPropString(GearPropType.equipTradeBlock, value));
                }
                else
                {
                    string tradeBlock = ItemStringHelper.GetGearPropString(GearPropType.tradeBlock, 1);
                    if (!tags.Contains(tradeBlock))
                        tags.Add(tradeBlock);
                }
            }
            if (Gear.Props.TryGetValue(GearPropType.accountSharable, out value) && value != 0)
            {
                int value2;
                if (Gear.Props.TryGetValue(GearPropType.sharableOnce, out value2) && value2 != 0)
                {
                    //tags.Add(ItemStringHelper.GetGearPropString(GearPropType.sharableOnce, value2));
                    tags.AddRange(ItemStringHelper.GetGearPropString(GearPropType.sharableOnce, value2).Split('\n'));
                }
                else
                {
                    tags.Add(ItemStringHelper.GetGearPropString(GearPropType.accountSharable, value));
                }
            }
            if (Gear.Props.TryGetValue(GearPropType.blockGoldHammer, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.blockGoldHammer, value));
            }
            //if (Gear.Props.TryGetValue(GearPropType.noPotential, out value) && value != 0)
            //{
            //    tags.Add(ItemStringHelper.GetGearPropString(GearPropType.noPotential, value));
            //}
            if ((Gear.Props.TryGetValue(GearPropType.fixedPotential, out value) && value != 0) || (Gear.Props.TryGetValue(GearPropType.fixedGrade, out value) && value != 0))
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.fixedPotential, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.notExtend, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.notExtend, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.cantRepair, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.cantRepair, value));
            }
            if (Gear.Props.TryGetValue(GearPropType.noLookChange, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.noLookChange, value));
            }
            if ((Gear.ItemID / 10000 >= 161 && Gear.ItemID / 10000 <= 165) || (Gear.ItemID / 10000 >= 194 && Gear.ItemID / 10000 <= 197))
            {
                tags.Add("Cannot use Fusion Anvil");//Unable to use anvil > change when GMS adds this line to mechanic, dragon gears
            }

            return tags;
        }

        private Bitmap GetAlienStoneIcon()
        {
            if (Gear.AlienStoneSlot == null)
            {
                return Resource.ToolTip_Equip_AlienStone_Empty;
            }
            else
            {
                switch (Gear.AlienStoneSlot.Grade)
                {
                    case AlienStoneGrade.Normal:
                        return Resource.ToolTip_Equip_AlienStone_Normal;
                    case AlienStoneGrade.Rare:
                        return Resource.ToolTip_Equip_AlienStone_Rare;
                    case AlienStoneGrade.Epic:
                        return Resource.ToolTip_Equip_AlienStone_Epic;
                    case AlienStoneGrade.Unique:
                        return Resource.ToolTip_Equip_AlienStone_Unique;
                    case AlienStoneGrade.Legendary:
                        return Resource.ToolTip_Equip_AlienStone_Legendary;
                    default:
                        return null;
                }
            }
        }

        private void DrawGearReq(Graphics g, int x, int y)
        {
            int value;
            bool can;
            NumberType type;
            Size size;
            //需求等级
            this.Gear.Props.TryGetValue(GearPropType.reqLevel, out value);
            int reduceReq = 0;
            {
                this.Gear.Props.TryGetValue(GearPropType.reduceReq, out reduceReq);
            }
            int value2 = Math.Max(0, value - reduceReq);
            can = this.charStat == null || this.charStat.Level >= value2;
            type = GetReqType(can, value2);
            g.DrawImage(FindReqImage(type, "reqLEV", out size), x, y);
            //DrawReqNum(g, value.ToString().PadLeft(3), (type == NumberType.Can ? NumberType.YellowNumber : type), x + 54, y, StringAlignment.Near);
            int levX = DrawReqNum(g, value2.ToString().PadLeft(3), (type == NumberType.Can ? NumberType.YellowNumber : type), x + 54, y, StringAlignment.Near);
            if (reduceReq != 0)
            {
                DrawReqNum(g, $"({value.ToString()}-{reduceReq.ToString()})", NumberType.Can, levX + 2, y, StringAlignment.Near);
                DrawReqNum(g, $"({value.ToString()}-{reduceReq.ToString()}", NumberType.YellowNumber, levX + 2, y, StringAlignment.Near);
                DrawReqNum(g, $"({value.ToString()}", NumberType.Can, levX + 2, y, StringAlignment.Near);
            }

            //需求人气
            this.Gear.Props.TryGetValue(GearPropType.reqPOP, out value);
            can = this.charStat == null || this.charStat.Pop >= value;
            type = GetReqType(can, value);
            if (value > 0)
            {
                g.DrawImage(FindReqImage(type, "reqPOP", out size), x + 80, y);
                DrawReqNum(g, value.ToString("D3"), type, x + 80 + 54, y, StringAlignment.Near);
            }

            y += 15;

            //需求力量
            this.Gear.Props.TryGetValue(GearPropType.reqSTR, out value);
            can = this.charStat == null || this.charStat.Strength.GetSum() >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqSTR", out size), x, y);
            DrawReqNum(g, value.ToString("D3"), type, x + 54, y, StringAlignment.Near);


            //需求运气
            this.Gear.Props.TryGetValue(GearPropType.reqLUK, out value);
            can = this.charStat == null || this.charStat.Luck.GetSum() >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqLUK", out size), x + 80, y);
            DrawReqNum(g, value.ToString("D3"), type, x + 80 + 54, y, StringAlignment.Near);

            y += 9;

            //需求敏捷
            this.Gear.Props.TryGetValue(GearPropType.reqDEX, out value);
            can = this.charStat == null || this.charStat.Dexterity.GetSum() >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqDEX", out size), x, y);
            DrawReqNum(g, value.ToString("D3"), type, x + 54, y, StringAlignment.Near);

            //需求智力
            this.Gear.Props.TryGetValue(GearPropType.reqINT, out value);
            can = this.charStat == null || this.charStat.Intelligence.GetSum() >= value;
            type = GetReqType(can, value);
            g.DrawImage(FindReqImage(type, "reqINT", out size), x + 80, y);
            DrawReqNum(g, value.ToString("D3"), type, x + 80 + 54, y, StringAlignment.Near);
        }

        private void DrawPropDiffEx(Graphics g, int x, int y)
        {
            int value;
            string numValue;
            //Defense tooltip icon
            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_icon_pdd, x, y);
            x += 62;
            DrawReqNum(g, "0", NumberType.LookAhead, x - 5, y + 6, StringAlignment.Far);

            //Boss DMG tooltip icon
            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_icon_bdr, x, y);
            x += 62;
            this.Gear.Props.TryGetValue(GearPropType.bdR, out value);
            numValue = (value > 0 ? "+ " : null) + value + " % ";
            DrawReqNum(g, numValue, NumberType.LookAhead, x - 5 + 3, y + 6, StringAlignment.Far);
            //DrawReqNum(g, numValue, NumberType.LookAhead, x - 1, y + 6, StringAlignment.Far);

            //Ignored Monster DEF tooltip icon
            g.DrawImage(Resource.UIToolTip_img_Item_Equip_Summary_icon_igpddr, x, y);
            x += 62;
            this.Gear.Props.TryGetValue(GearPropType.imdR, out value);
            numValue = (value > 0 ? "+ " : null) + value + " % ";
            DrawReqNum(g, numValue, NumberType.LookAhead, x - 5 - 1, y + 6, StringAlignment.Far);
            //DrawReqNum(g, numValue, NumberType.LookAhead, x - 5, y + 6, StringAlignment.Far);
        }

        private void DrawJobReq(Graphics g, ref int picH)
        {
            int value;
            string extraReq = ItemStringHelper.GetExtraJobReqString(Gear.type);
            if (extraReq == null && Gear.Props.TryGetValue(GearPropType.reqSpecJob, out value))
            {
                extraReq = ItemStringHelper.GetExtraJobReqString(value);
            }
            if (extraReq == null && Gear.ReqSpecJobs.Count > 0)
            {
                int[] specJobsList1 = new[] { 2, 12, 22, 32, 172 };
                if (new HashSet<int>(specJobsList1).SetEquals(Gear.ReqSpecJobs))
                {
                    extraReq = ItemStringHelper.GetExtraJobReqString(specJobsList1);
                }
                else
                {
                    extraReq = ItemStringHelper.GetExtraJobReqString(Gear.ReqSpecJobs);
                }
            }

            Image jobImage = null;
            int extraReqWidth = 261;
            if (extraReq == null)
            {
                jobImage = Resource.UIToolTip_img_Item_Equip_Job_normal;
            }
            else
            {
                // measure jobReq desc
                // Actually we use GearGraphics.DrawPlainText to render extraReq, the meatured lines may not accurate.
                using var extraReqFmt = new StringFormat();
                extraReqFmt.Alignment = StringAlignment.Center;
                g.MeasureString(extraReq, GearGraphics.ItemDetailFont, new SizeF(extraReqWidth, short.MaxValue), extraReqFmt, out _, out var lines);
                jobImage = lines == 1 ? Resource.UIToolTip_img_Item_Equip_Job_expand : Resource.UIToolTip_img_Item_Equip_Job_expand2;
            }
            //g.DrawImage(jobImage, 10, picH);
            g.DrawImage(jobImage, 12, picH);

            int reqJob;
            Gear.Props.TryGetValue(GearPropType.reqJob, out reqJob);
            int[] origin = new int[] { 16, 7, 58, 7, 97, 7, 137, 7, 168, 10, 200, 10 };//翻译改动
            int[] origin2 = new int[] { 10, 6, 44, 6, 79, 6, 126, 6, 166, 6, 201, 6 };
            for (int i = 0; i <= 5; i++)
            {
                bool enable;
                if (i == 0)
                {
                    enable = reqJob <= 0;
                    if (reqJob == 0) reqJob = 0b11111;
                    if (reqJob == -1) reqJob = 0b00000;
                }
                else
                {
                    enable = (reqJob & (1 << (i - 1))) != 0;
                }
                if (enable)
                {
                    enable = this.charStat == null || Character.CheckJobReq(this.charStat.Job, i);
                    Image jobImage2 = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_Job_" + (enable ? "enable" : "disable") + "_" + i.ToString()) as Image;
                    if (jobImage != null)
                    {
                        if (enable)
                            g.DrawImage(jobImage2, 10 + origin[i * 2], picH + origin[i * 2 + 1]);
                        else
                            g.DrawImage(jobImage2, 10 + origin2[i * 2], picH + origin2[i * 2 + 1]);
                    }
                }
            }
            if (extraReq != null)
            {
                // ignore yaxis.
                int tempY = picH + 24;
                GearGraphics.DrawPlainText(g, extraReq, GearGraphics.EquipDetailFont, GearGraphics.OrangeBrush3Color,
                    4, 259, ref tempY, 16, Text.TextAlignment.Center);
            }
            picH += jobImage.Height + 9;
        }

        private Image FindReqImage(NumberType type, string req, out Size size)
        {
            Image image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + req) as Image;
            if (image != null)
                size = image.Size;
            else
                size = Size.Empty;
            return image;
        }

        private void DrawStar(Graphics g, ref int picH)
        {
            if (Gear.Star > 0)
            {
                int totalWidth = Gear.Star * 10 + (Gear.Star / 5 - 1) * 6;
                int dx = 130 - totalWidth / 2;
                for (int i = 0; i < Gear.Star; i++)
                {
                    g.DrawImage(Resource.UIToolTip_img_Item_Equip_Star_Star, dx, picH);
                    dx += 10;
                    if (i > 0 && i % 5 == 4)
                    {
                        dx += 6;
                    }
                }
                picH += 18;
            }
        }

        private void DrawStar2(Graphics g, ref int picH)
        {
            //int maxStar = Gear.GetMaxStar();
            int maxStar = Math.Max(Gear.GetMaxStar(isPostNEXTClient), Gear.Star);
            if (maxStar > 0)
            {
                for (int i = 0; i < maxStar; i += 15)
                {
                    int starLine = Math.Min(maxStar - i, 15);
                    int totalWidth = starLine * 10 + (starLine / 5 - 1) * 6;
                    int dx = 130 - totalWidth / 2;
                    for (int j = 0; j < starLine; j++)
                    {
                        g.DrawImage((i + j < Gear.Star) ?
                            Resource.UIToolTip_img_Item_Equip_Star_Star : Resource.UIToolTip_img_Item_Equip_Star_Star0,
                            dx, picH);
                        dx += 10;
                        if (j > 0 && j % 5 == 4)
                        {
                            dx += 6;
                        }
                    }
                    picH += 18;
                }
                picH -= 1;
            }
        }

        private NumberType GetReqType(bool can, int reqValue)
        {
            if (reqValue <= 0)
                return NumberType.Disabled;
            if (can)
                return NumberType.Can;
            else
                return NumberType.Cannot;
        }

        private int DrawReqNum(Graphics g, string numString, NumberType type, int x, int y, StringAlignment align)
        {
            if (g == null || numString == null || align == StringAlignment.Center)
                return x;
            int spaceWidth = type == NumberType.LookAhead ? 3 : 6;
            bool near = align == StringAlignment.Near;

            for (int i = 0; i < numString.Length; i++)
            {
                char c = near ? numString[i] : numString[numString.Length - i - 1];
                Image image = null;
                Point origin = Point.Empty;
                switch (c)
                {
                    case ' ':
                        break;
                    case '+':
                        image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + "plus") as Image;
                        break;
                    case '-':
                        image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + "minus") as Image;
                        origin.Y = 2;
                        break;
                    case '%':
                        image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + "percent") as Image;
                        break;
                    case '(':
                        image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + "leftParenthesis") as Image;
                        break;
                    case ')':
                        image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + "rightParenthesis") as Image;
                        break;
                    default:
                        if ('0' <= c && c <= '9')
                        {
                            image = Resource.ResourceManager.GetObject("UIToolTip_img_Item_Equip_" + type.ToString() + "_" + c) as Image;
                            if (c == '1' && type == NumberType.LookAhead)
                            {
                                origin.X = 1;
                            }
                        }
                        break;
                }

                if (image != null)
                {
                    if (near)
                    {
                        g.DrawImage(image, x + origin.X, y + origin.Y);
                        x += image.Width + origin.X + 1;
                    }
                    else
                    {
                        x -= image.Width + origin.X;
                        g.DrawImage(image, x + origin.X, y + origin.Y);
                        x -= 1;
                    }
                }
                else //空格补位
                {
                    x += spaceWidth * (near ? 1 : -1);
                }
            }
            return x;
        }

        private Image GetAdditionalOptionIcon(GearGrade grade)
        {
            switch (grade)
            {
                default:
                case GearGrade.B: return Resource.AdditionalOptionTooltip_rare;
                case GearGrade.A: return Resource.AdditionalOptionTooltip_epic;
                case GearGrade.S: return Resource.AdditionalOptionTooltip_unique;
                case GearGrade.SS: return Resource.AdditionalOptionTooltip_legendary;
            }
        }

        private bool TryGetMedalResource(int medalTag, int type, out Wz_Node resNode)
        {
            switch (type)
            {
                case 0:
                    resNode = PluginBase.PluginManager.FindWz("UI/NameTag.img/medal/" + medalTag);
                    break;
                case 1:
                    resNode = PluginBase.PluginManager.FindWz("UI/ChatBalloon.img/" + medalTag);
                    break;
                case 2:
                    resNode = PluginBase.PluginManager.FindWz("UI/NameTag.img/" + medalTag);
                    break;
                default:
                    resNode = null;
                    break;
            }
            return resNode != null;
        }

        private enum NumberType
        {
            Can,
            Cannot,
            Disabled,
            LookAhead,
            YellowNumber,
        }
    }
}