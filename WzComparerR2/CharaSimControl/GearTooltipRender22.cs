using System;
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
using DevComponents.DotNetBar;
using Newtonsoft.Json.Linq;

namespace WzComparerR2.CharaSimControl
{
    public class GearTooltipRender22 : TooltipRender
    {
        static GearTooltipRender22()
        {
            res = new Dictionary<string, TextureBrush>();
            res["top"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_fixed_top, WrapMode.Clamp);
            res["mid"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_fixed_mid, WrapMode.Tile);
            res["line"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_fixed_line, WrapMode.Clamp);
            res["btm"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Common_frame_fixed_btm, WrapMode.Clamp);

            res["category_w"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Equip_frame_common_category_w, WrapMode.Clamp);
            res["category_c"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Equip_frame_common_category_c, WrapMode.Tile);
            res["category_e"] = new TextureBrush(Resource.UIToolTipNew_img_Item_Equip_frame_common_category_e, WrapMode.Clamp);

        }

        private static Dictionary<string, TextureBrush> res;

        public GearTooltipRender22()
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
        public bool MaxStar25 { get; set; } = false;
        public bool MseaMode { get; set; }
        public bool IsCombineProperties { get; set; } = true;
        public bool ShowCashPurchasePrice { get; set; }
        public bool AutoTitleWrap { get; set; }
        public int CosmeticHairColor { get; set; }
        public int CosmeticFaceColor { get; set; }
        public bool CompareMode { get; set; } = false;
        private string titleLanguage = "";

        private bool isPostNEXTClient;
        private bool WillDrawMedal {  get; set; }
        private bool WillDrawChatBalloon { get; set; }
        private bool WillDrawNameTag { get; set; }
        private bool isMsnClient { get; set; }
        private Wz_Node MedalResNode { get; set; }
        private Wz_Node ChatBalloonResNode { get; set; }
        private Wz_Node NameTagResNode { get; set; }
        private Bitmap AvatarSample { get; set; }

        public TooltipRender SetItemRender { get; set; }
        private List<int> linePos;

        public override Bitmap Render()
        {
            if (this.Gear == null)
            {
                return null;
            }

            this.isMsnClient = StringLinker?.StringEqp?.ContainsKey(1006514) ?? false;
            int[] picH = new int[4];
            linePos = new List<int>();
            Bitmap left = RenderBase(out picH[0]);
            Bitmap set = RenderSetItem(out int setHeight);
            picH[2] = setHeight;
            Bitmap levelOrSealed = null;
            if (this.ShowLevelOrSealed)
            {
                levelOrSealed = RenderLevelOrSealed(out picH[3]);
            }

            int width = 324;
            if (set != null) width += set.Width;
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
                DrawBG(g, "", width, picH[0], 0);

                //复制图像
                g.DrawImage(left, width, 0, new Rectangle(0, 0, left.Width, picH[0]), GraphicsUnit.Pixel);

                width += left.Width;
                left.Dispose();
            }

            //绘制setitem
            if (set != null)
            {
                int y = 0;
                int partWidth = 0;
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
                GearGraphics.DrawNewTooltipBack(g, width, 0, levelOrSealed.Width, picH[3]);

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

        private void DrawBG(Graphics g, string tag, int startX, int endY, int target)
        {
            int startY = 30;

            g.DrawImage(res[$"top{tag}"].Image, startX, 0);
            for (int i = 0; i < linePos.Count; i += 2)
            {
                if (linePos[i] == target)
                {
                    FillRect(g, res[$"mid{tag}"], startX, startY, linePos[i + 1]);
                    g.DrawImage(res[$"line{tag}"].Image, startX, linePos[i + 1]);
                    startY = linePos[i + 1] + 3;
                }
            }
            FillRect(g, res[$"mid{tag}"], startX, startY, endY - 13);
            g.DrawImage(res[$"btm{tag}"].Image, startX, endY - 13);
        }

        private void AddLines(int target, int spacing, ref int picH, bool condition = true)
        {
            if (condition)
            {
                linePos.Add(target);
                linePos.Add(picH);
                picH += spacing;
            }
        }

        private Bitmap RenderBase(out int picH)
        {
            int width = 324;
            Bitmap bitmap = new Bitmap(width, DefaultPicHeight);
            Graphics g = Graphics.FromImage(bitmap);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            var equip22ColorTable = new Dictionary<string, Color>()
            {
                { "c", ((SolidBrush)GearGraphics.Equip22BrushEmphasis).Color },
                { "$y", GearGraphics.gearCyanColor },
                { "$r", ((SolidBrush)GearGraphics.Equip22BrushRed).Color },
                { "$e", ((SolidBrush)GearGraphics.Equip22BrushEmphasisBright).Color },
                { "$b", ((SolidBrush)GearGraphics.Equip22BrushBonusStat).Color },
                { "$s", ((SolidBrush)GearGraphics.Equip22BrushScroll).Color },
                { "$g", ((SolidBrush)GearGraphics.Equip22BrushGray).Color },
                { "$d", ((SolidBrush)GearGraphics.Equip22BrushDarkGray).Color },
                { "$z", ((SolidBrush)GearGraphics.GreenBrush2).Color },
            };
            var itemPotentialColorTable = new Dictionary<string, Color>()
            {
                { "$n", ((SolidBrush)GearGraphics.Equip22BrushDarkGray).Color },
                { "$r", ((SolidBrush)GearGraphics.Equip22BrushRare).Color },
                { "$e", ((SolidBrush)GearGraphics.Equip22BrushEpic).Color },
                { "$u", ((SolidBrush)GearGraphics.Equip22BrushEmphasis).Color },
                { "$l", ((SolidBrush)GearGraphics.Equip22BrushLegendary).Color },
            };
            int value, value2;

            picH = 10;

            // 스타포스 별
            int maxStar = Math.Max(Gear.GetMaxStar(isPostNEXTClient), Gear.Star);
            if (maxStar >= 25 && Gear.IsGenesisWeapon)
            {
                maxStar = 22;
            }
            if (!Gear.GetBooleanValue(GearPropType.blockUpgradeStarforce))
            {
                DrawStar(g, maxStar, ref picH);
            }

            // 강화 정보
            // removed at kms 402(2)
            //DrawEnchantBox(g, Gear.ScrollUp, (int)Gear.Grade, (int)Gear.AdditionGrade, ref picH);

            // 아이템 이름
            StringResult sr;
            if (StringLinker == null || !StringLinker.StringEqp.TryGetValue(Gear.ItemID, out sr))
            {
                sr = new StringResult();
                sr.Name = "(null)";
            }
            string gearName = sr.Name;
            if (Translator.DefaultDesiredCurrency != "none")
            {
                if (Translator.DefaultDetectCurrency == "auto")
                {
                    titleLanguage = Translator.GetLanguage(gearName);
                }
                else
                {
                    titleLanguage = Translator.ConvertCurrencyToLang(Translator.DefaultDetectCurrency);
                }
            }
            int gender = Gear.GetGender(Gear.ItemID);
            switch (gender)
            {
                case 0: gearName += " (M)"; break;
                case 1: gearName += " (F)"; break;
            }

            TextRenderer.DrawText(g, gearName, GearGraphics.ItemNameFont,
                new Point(width, picH), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            picH += 20;

            // 스페셜 아이템
            if (Gear.GetBooleanValue(GearPropType.specialGrade))
            {
                TextRenderer.DrawText(g, "Special Item", GearGraphics.EquipMDMoris9Font, new Point(width, picH), Color.White, TextFormatFlags.HorizontalCenter);
                picH += 16;
            }
            else if (Gear.Props.TryGetValue(GearPropType.royalSpecial, out value) && value > 0)
            {
                switch (value)
                {
                    case 1:
                        TextRenderer.DrawText(g, "Special Label", GearGraphics.EquipMDMoris9Font, new Point(width, picH), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.HorizontalCenter);
                        break;
                    case 2:
                        TextRenderer.DrawText(g, "Red Label", GearGraphics.EquipMDMoris9Font, new Point(width, picH), ((SolidBrush)GearGraphics.Equip22BrushEmphasis).Color, TextFormatFlags.HorizontalCenter);
                        break;
                    case 3:
                        TextRenderer.DrawText(g, "Black Label", GearGraphics.EquipMDMoris9Font, new Point(width, picH), ((SolidBrush)GearGraphics.Equip22BrushEmphasis).Color, TextFormatFlags.HorizontalCenter);
                        break;
                }
                picH += 16;
            }
            else if (Gear.Props.TryGetValue(GearPropType.masterSpecial, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "Master Label", GearGraphics.EquipMDMoris9Font, new Point(width, picH), ((SolidBrush)GearGraphics.BlueBrush).Color, TextFormatFlags.HorizontalCenter);
                picH += 16;
            }
            else if (Gear.Props.TryGetValue(GearPropType.BTSLabel, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "BTS Label", GearGraphics.EquipMDMoris9Font, new Point(width, picH), Color.FromArgb(182, 110, 238), TextFormatFlags.HorizontalCenter);
                picH += 16;
            }
            else if (Gear.Props.TryGetValue(GearPropType.BLACKPINKLabel, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "BLACKPINK Label", GearGraphics.EquipMDMoris9Font, new Point(width, picH), Color.FromArgb(242, 140, 160), TextFormatFlags.HorizontalCenter);
                picH += 16;
            }
            else if (Gear.Props.TryGetValue(GearPropType.limitedLabel, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "LIMITED Label", GearGraphics.EquipMDMoris9Font, new Point(width, picH), Color.FromArgb(248, 196, 129), TextFormatFlags.HorizontalCenter);
                picH += 16;
                if (!string.IsNullOrEmpty(Gear.LabelGradeTooltip))
                {
                    var limitedLabelText = Regex.Replace(Gear.LabelGradeTooltip, "%d", "0");
                    TextRenderer.DrawText(g, limitedLabelText, GearGraphics.EquipMDMoris9Font, new Point(width, picH), Color.FromArgb(248, 196, 129), TextFormatFlags.HorizontalCenter);
                    picH += 16;
                }
            }
            else if (Gear.Props.TryGetValue(GearPropType.magicLayerWz2, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "Magic Layer", GearGraphics.EquipMDMoris9Font, new Point(width, picH), Color.FromArgb(82, 180, 189), TextFormatFlags.HorizontalCenter);
                picH += 16;
            }

            // 기타 속성
            //额外属性
            var topAttrList = GetGearTopAttributeString();
            if (topAttrList.Count > 0)
            {
                foreach (var text in topAttrList)
                {
                    GearGraphics.DrawString(g, $"#$r{text}#", GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 306, ref picH, 16, alignment: Text.TextAlignment.Center);
                }
            }
            picH -= 1;

            // ----------------------------------------------------------------------
            AddLines(0, -5, ref picH);

            // 아이템 아이콘 이미지
            if (Gear.IconRaw.Bitmap != null) //绘制icon
            {
                /*
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
                */

                //绘制阴影
                /*
                var shade = Resource.UIToolTipNew_img_Item_Common_ItemIcon_shade;
                g.DrawImage(shade,
                    new Rectangle(15, picH + 10, shade.Width, shade.Height),
                    0, 0, shade.Width, shade.Height,
                    GraphicsUnit.Pixel);
                */
                //绘制图标
                g.DrawImage(Resource.UIToolTipNew_img_Item_Common_ItemIcon_base_custom, 15, picH + 10);
                g.DrawImage(GearGraphics.EnlargeBitmap(Gear.IconRaw.Bitmap),
                    21 + (1 - Gear.IconRaw.Origin.X) * 2,
                    picH + 16 + (33 - Gear.IconRaw.Origin.Y) * 2);

                //attr.Dispose();
            }
            else g.DrawImage(Resource.UIToolTipNew_img_Item_Common_ItemIcon_base, 15, picH + 10);

            // 캐시 라벨 아이콘
            if (Gear.Cash && !((Gear.Props.TryGetValue(GearPropType.mintable, out value) && value != 0) || CharaSimLoader.LoadedMintableNFTItems.Contains(Gear.ItemID) || CharaSimLoader.LoadedMintableSBTItems.Contains(Gear.ItemID) || CharaSimLoader.LoadedMintableFTItems.Contains(Gear.ItemID)))
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
                else if (Gear.Props.TryGetValue(GearPropType.limitedLabel, out value) && value > 0)
                {
                    cashImg = Resource.CashShop_img_CashItem_label_15;
                    cashOrigin = new Point(12, 12);
                }
                else if (Gear.Props.TryGetValue(GearPropType.magicLayerWz2, out value) && value > 0)
                {
                    cashImg = Resource.CashShop_img_CashItem_label_100;
                    cashOrigin = new Point(12, 12);
                }
                if (cashImg == null) //default cashImg
                {
                    cashImg = Resource.CashItem_0;
                }

                g.DrawImage(GearGraphics.EnlargeBitmap(cashImg),
                    21 + 68 - cashOrigin.X * 2 - 2,
                    picH + 16 + 68 - cashOrigin.Y * 2 - 2);
            }

            /*
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
            */

            // 전투력 증가량
            TextRenderer.DrawText(g, "Combat Power Increase", GearGraphics.EquipMDMoris9Font, new Point(309 - TextRenderer.MeasureText(g, "Combat Power Increase", GearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width, picH + 12), ((SolidBrush)GearGraphics.Equip22BrushDarkGray).Color, TextFormatFlags.NoPadding);
            g.DrawImage(Resource.UIToolTipNew_img_Item_Equip_imgFont_atkPow_plus_0, 293, picH + 38); //暂时画个
            picH += 78;

            // 장비 분류
            DrawCategory(g, picH);
            picH += 18;

            // 착용 직업
            string reqJobString = ItemStringHelper.GetExtraJobReqString(Gear.type);
            if (reqJobString == null && Gear.Props.TryGetValue(GearPropType.reqSpecJob, out value))
            {
                reqJobString = ItemStringHelper.GetExtraJobReqString(value);
            }
            if (reqJobString == null && Gear.ReqSpecJobs.Count > 0)
            {
                // apply req order fix for CMS only
                int[] specJobsList1 = new[] { 2, 22, 12, 32, 172 };
                if (new HashSet<int>(specJobsList1).SetEquals(Gear.ReqSpecJobs))
                {
                    reqJobString = JoinStringWithNewline(g, ", ", ItemStringHelper.GetExtraJobReqStringList(specJobsList1), 210);
                }
                else
                {
                    reqJobString = JoinStringWithNewline(g, ", ", ItemStringHelper.GetExtraJobReqStringList(Gear.ReqSpecJobs, isMsnClient), 210);
                }
            }
            if (reqJobString == null)
            {
                List<string> reqJobList = new List<string>();
                Gear.Props.TryGetValue(GearPropType.reqJob, out int reqJob);
                switch (reqJob)
                {
                    case -1:
                        reqJobString = "Beginner";
                        break;
                    case 0:
                        reqJobString = "Shared";
                        break;
                    default:
                        for (int i = 0; i < 5; i++)
                        {
                            if ((reqJob & (1 << i)) != 0)
                            {
                                switch (i)
                                {
                                    case 0:
                                        reqJobList.Add("Warrior");
                                        break;
                                    case 1:
                                        reqJobList.Add("Magician");
                                        break;
                                    case 2:
                                        reqJobList.Add("Bowman");
                                        break;
                                    case 3:
                                        reqJobList.Add("Thief");
                                        break;
                                    case 4:
                                        reqJobList.Add("Pirate");
                                        break;
                                }
                            }
                        }
                        break;
                }

                if (reqJobList.Count > 0)
                {
                    reqJobString = string.Join(", ", reqJobList);
                }
            }
            TextRenderer.DrawText(g, "Required Job", GearGraphics.EquipMDMoris9Font, new Point(15, picH), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.NoPadding);
            GearGraphics.DrawString(g, (string.IsNullOrEmpty(reqJobString) ? "Shared" : reqJobString).Replace("Can be equipped by ", "").Replace(" only", "").Trim(), GearGraphics.EquipMDMoris9Font, equip22ColorTable, 100, 308, ref picH, 16);

            // 요구 레벨
            this.Gear.Props.TryGetValue(GearPropType.reqLevel, out value2);
            int reduceReq = 0;
            {
                this.Gear.Props.TryGetValue(GearPropType.reduceReq, out reduceReq);
            }
            int finalReqLevel = Math.Max(0, value2 - reduceReq);
            bool moveX = false;
            if (finalReqLevel > 0)
            {
                TextRenderer.DrawText(g, "Required Level", GearGraphics.EquipMDMoris9Font, new Point(15, picH), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.NoPadding);
                if (reduceReq > 0)
                {
                    GearGraphics.DrawString(g, $"Lv. {finalReqLevel} #$g({value2} #$b- {reduceReq}#)#", GearGraphics.EquipMDMoris9Font, equip22ColorTable, 100, 244, ref picH, 16);
                }
                else
                {
                    GearGraphics.DrawString(g, $"Lv. {finalReqLevel}", GearGraphics.EquipMDMoris9Font, equip22ColorTable, 100, 244, ref picH, 16);
                }
                moveX = true;
            }

            // 착용 성별
            if (gender < 2)
            {
                TextRenderer.DrawText(g, "Gender", GearGraphics.EquipMDMoris9Font, new Point(moveX ? 232: 15, picH - (moveX ? 16 : 0)), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, gender == 0 ? "M" : "F", GearGraphics.EquipMDMoris9Font, new Point(moveX ? 300: 100, picH - (moveX ? 16 : 0)), Color.White, TextFormatFlags.NoPadding);
                if (!moveX) picH += 16;
            }

            // ----------------------------------------------------------------------
            bool secondLineNeeded = true;
            bool hasThirdContents = false;
            bool hasOptionPart = false;
            bool hasDescPart = false;

            picH -= 1;
            AddLines(0, 7, ref picH);

            // 안드로이드
            if (Gear.type == GearType.android && Gear.Props.TryGetValue(GearPropType.android, out value) && value > 0)
            {
                hasThirdContents = true;
                hasOptionPart = true;

                picH -= 2;
                TextRenderer.DrawText(g, "Appearance:", GearGraphics.EquipMDMoris9Font, new Point(15, picH + 2), Color.White, TextFormatFlags.NoPadding);

                Wz_Node android = PluginBase.PluginManager.FindWz(string.Format("Etc/Android/{0:D4}.img", value)) ?? PluginBase.PluginManager.FindWz("Etc/Android/0001.img");
                Wz_Node costume = android?.Nodes["costume"];
                Wz_Node basic = android?.Nodes["basic"];

                BitmapOrigin appearance;
                int morphID = android?.Nodes["info"]?.Nodes["morphID"]?.GetValueEx<int>(0) ?? 0;
                if (Gear.ToolTipPreview.Bitmap != null)
                {
                    appearance = Gear.ToolTipPreview;
                    Gear.AndroidBitmap = appearance.Bitmap;
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

                    g.DrawImage(appearance.Bitmap, 135 - Math.Min(appearance.Origin.X, 50), picH + Math.Max(80 - appearance.Origin.Y, 0), imgrect, GraphicsUnit.Pixel);
                    Gear.AndroidBitmap = appearance.Bitmap;

                    picH += 102;
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
                    GearGraphics.DrawString(g, $"#c{string.Join(", ", randomParts)} The image is an example. The android's appearance is determined upon equipping it for the first time.#", GearGraphics.EquipMDMoris9Font, null, 15, 305, ref picH, 16, strictlyAlignLeft: 1);
                }
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
                GearGraphics.DrawString(g, $"Color: #c{colorName}#", GearGraphics.EquipMDMoris9Font, null, 15, 305, ref picH, 16, strictlyAlignLeft: 1);
                TextRenderer.DrawText(g, "Appearance: ", GearGraphics.EquipMDMoris9Font, new Point(13, picH), Color.White, TextFormatFlags.NoPadding);
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
            // 안드로이드 등급
            if (Gear.Props.TryGetValue(GearPropType.grade, out value) && value > 0)
            {
                hasThirdContents = true;
                hasOptionPart = true;

                // 안드로이드 등급
                if (Gear.Props.TryGetValue(GearPropType.grade, out value) && value > 0)
                {
                    hasThirdContents = true;
                    hasOptionPart = true;

                    picH += 4;
                    TextRenderer.DrawText(g, "Rank: " + value, GearGraphics.EquipMDMoris9Font, new Point(15, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 12;
                }
            }

            // 세트 아이템
            {
                List<string> setList = new List<string>();
                if (Gear.Props.TryGetValue(GearPropType.setItemID, out int setID))
                {
                    SetItem setItem;
                    if (CompareMode)
                    {
                        setItem = CharaSimLoader.LoadSetItem(setID, this.SourceWzFile);
                        if (setItem != null)
                            setList.Add(setItem.SetItemName);
                    }
                    else if (CharaSimLoader.LoadedSetItems.TryGetValue(setID, out setItem))
                        setList.Add(setItem.SetItemName);
                } 
                if (Gear.Props.TryGetValue(GearPropType.jokerToSetItem, out value) && value > 0) setList.Add("Lucky Item");

                var text = string.Join(", ", setList);
                if (!string.IsNullOrEmpty(text))
                {
                    hasThirdContents = true;
                    hasOptionPart = true;

                    g.DrawImage(Resource.UIToolTipNew_img_Item_Equip_textIcon_set_guide, 16, picH - 2);
                    GearGraphics.DrawString(g, $"#$g{text}#", GearGraphics.EquipMDMoris9Font, equip22ColorTable, 100, 308, ref picH, 16, alignment: Text.TextAlignment.Left);
                }
            }

            // 사용 가능 스킬
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
            {
                List<string> skillNames = new List<string>();

                if (Gear.IsGenesisWeapon)
                {
                    int destinySkill = 1241 * (Gear.IsDestinyWeapon ? 1 : 0);

                    foreach (var skillID in new[] { 80002632, 80002633 })
                    {
                        string skillName;
                        if (this.StringLinker?.StringSkill.TryGetValue(skillID + destinySkill, out var sr2) ?? false && sr2.Name != null)
                        {
                            skillName = sr2.Name;
                        }
                        else
                        {
                            skillName = (skillID + destinySkill).ToString();
                        }
                        skillNames.Add(skillName);
                    }
                }
                if (!string.IsNullOrEmpty(levelDesc))
                {
                    skillNames.Add(levelDesc);
                }

                var text = string.Join(", ", skillNames);
                if (!string.IsNullOrEmpty(text))
                {
                    hasThirdContents = true;
                    hasOptionPart = true;

                    TextRenderer.DrawText(g, "Usable Skill", GearGraphics.EquipMDMoris9Font, new Point(15, picH), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.NoPadding);
                    GearGraphics.DrawString(g, $"#$g{text}#".Replace("#c", ""), GearGraphics.EquipMDMoris9Font, equip22ColorTable, 100, 308, ref picH, 16, alignment: Text.TextAlignment.Left);
                }
            }

            // Growth Level
            //绘制装备升级
            if (Gear.Props.TryGetValue(GearPropType.level, out value) && !Gear.FixLevel)
            {
                hasThirdContents = true;
                hasOptionPart = true;

                bool max = (Gear.Levels != null && value >= Gear.Levels.Count);
                string expString = Gear.Levels != null && Gear.Levels.First().Point != 0 ? ": 0/" + Gear.Levels.First().Point : ": 0%";
                string text = $"#$gLv : {(max ? "MAX" : value.ToString())}  EXP {(max ? ": MAX" : expString)}#";
                TextRenderer.DrawText(g, "Growth Level", GearGraphics.EquipMDMoris9Font, new Point(15, picH), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.NoPadding);
                GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, equip22ColorTable, 100, 308, ref picH, 16, alignment: Text.TextAlignment.Left);
            }
            else if ((GearType)Gear.type == GearType.arcaneSymbol)
            {
                hasThirdContents = true;
                hasOptionPart = true;

                string text = $"#$gLv : 1  EXP : 1 / 12 ( 8% )#";
                TextRenderer.DrawText(g, "Growth Level", GearGraphics.EquipMDMoris9Font, new Point(15, picH), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.NoPadding);
                GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, equip22ColorTable, 100, 308, ref picH, 16, alignment: Text.TextAlignment.Left);
            }
            else if ((GearType)Gear.type == GearType.authenticSymbol)
            {
                hasThirdContents = true;
                hasOptionPart = true;

                string text = $"#$gLv : 1  EXP : 1 / 29 ( 3% )#";
                TextRenderer.DrawText(g, "Growth Level", GearGraphics.EquipMDMoris9Font, new Point(15, picH), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.NoPadding);
                GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, equip22ColorTable, 100, 308, ref picH, 16, alignment: Text.TextAlignment.Left);
            }
            else if ((GearType)Gear.type == GearType.grandAuthenticSymbol)
            {
                hasThirdContents = true;
                hasOptionPart = true;

                string text = $"#$gLv : 1  EXP : 1 / 29 ( 3% )#";
                TextRenderer.DrawText(g, "Growth Level", GearGraphics.EquipMDMoris9Font, new Point(15, picH), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.NoPadding);
                GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, equip22ColorTable, 100, 308, ref picH, 16, alignment: Text.TextAlignment.Left);
            }

            // 공격 속도
            if (!Gear.Props.TryGetValue(GearPropType.attackSpeed, out value)
                && (Gear.IsWeapon(Gear.type) || Gear.type == GearType.katara)) //找不到攻速的武器
            {
                value = 6; //给予默认速度
            }
            if (!Gear.Cash && value > 0)
            {
                if (2 <= value && value <= 9) // check valid speed
                {
                    hasThirdContents = true;
                    hasOptionPart = true;

                    TextRenderer.DrawText(g, "Attack Speed", GearGraphics.EquipMDMoris9Font, new Point(15, picH), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.NoPadding);
                    GearGraphics.DrawString(g, $"#$gStage {10 - value}#", GearGraphics.EquipMDMoris9Font, equip22ColorTable, 100, 305, ref picH, 16, alignment: Text.TextAlignment.Left);
                }
            }
            // 등급
            if (Gear.type != GearType.android && Gear.Props.TryGetValue(GearPropType.grade, out value) && value > 0)
            {
                AddLines(0, 7, ref picH, condition: secondLineNeeded);
                secondLineNeeded = false;
                hasThirdContents = true;
                hasOptionPart = true;

                TextRenderer.DrawText(g, "Grade : " + value, GearGraphics.EquipMDMoris9Font, new Point(15, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 16;
            }

            // 내구도
            if (Gear.Props.TryGetValue(GearPropType.durability, out value))
            {
                hasThirdContents = true;
                hasOptionPart = true;

                TextRenderer.DrawText(g, "Durability : 100%", GearGraphics.EquipMDMoris9Font, new Point(15, picH), ((SolidBrush)GearGraphics.Equip22BrushLegendary).Color, TextFormatFlags.NoPadding);
                picH += 16;
            }

            // 채집 도구
            if (Gear.type == GearType.shovel || Gear.type == GearType.pickaxe)
            {
                string skillName = null;
                switch (Gear.type)
                {
                    case GearType.shovel: skillName = "Mining"; break;
                    case GearType.pickaxe: skillName = "Herbalism"; break;
                }
                if (Gear.Props.TryGetValue(GearPropType.gatherTool_incSkillLevel, out value) && value > 0)
                {
                    hasThirdContents = true;
                    hasOptionPart = true;

                    TextRenderer.DrawText(g, skillName + " Skill Level : +" + value, GearGraphics.EquipMDMoris9Font, new Point(15, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 16;
                }
                if (Gear.Props.TryGetValue(GearPropType.gatherTool_incSpeed, out value) && value > 0)
                {
                    hasThirdContents = true;
                    hasOptionPart = true;

                    TextRenderer.DrawText(g, skillName + " Speed: +" + value + "%", GearGraphics.EquipMDMoris9Font, new Point(15, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 16;
                }
                if (Gear.Props.TryGetValue(GearPropType.gatherTool_incNum, out value) && value > 0)
                {
                    hasThirdContents = true;
                    hasOptionPart = true;

                    TextRenderer.DrawText(g, "Can get up to " + value + "item(s)", GearGraphics.EquipMDMoris9Font, new Point(15, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 16;
                }
                if (Gear.Props.TryGetValue(GearPropType.gatherTool_reqSkillLevel, out value) && value > 0)
                {
                    hasThirdContents = true;
                    hasOptionPart = true;

                    TextRenderer.DrawText(g, "Requires " + value + " " + skillName + " Skill ", GearGraphics.EquipMDMoris9Font, new Point(15, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 16;
                }
            }

            // 기간 한정 능력치
            if (Gear.Props.TryGetValue(GearPropType.abilityTimeLimited, out value) && value != 0)
            {
                hasThirdContents = true;
                hasOptionPart = true;

                DateTime time = DateTime.Now.AddDays(7d);
                var text = $"#$e{ItemStringHelper.GetGearPropString22(GearPropType.abilityTimeLimited, value)[0]}: {time.ToString("M/d/yyyy HH:mm")} UTC" +
                    $"{ItemStringHelper.GetGearPropString22(GearPropType.notExtend, value)[0]}#";

                GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 310, ref picH, 16);
            }

            // 장비 옵션
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
            foreach (GearPropType type in props)
            {
                Gear.StandardProps.TryGetValue(type, out value); //standard value
                if (value > 0 || Gear.Props[type] > 0)
                {
                    var propStr = ItemStringHelper.GetGearPropDiffString22(type, Gear.Props[type], value);

                    if (DrawProps(g, propStr, 0, picH, equip22ColorTable))
                    {
                        hasThirdContents = true;
                        hasOptionPart = true;
                        picH += 16;
                    }
                }
            }
            // 그랜드 어센틱심볼 기본 옵션
            if ((GearType)Gear.type == GearType.grandAuthenticSymbol)
            {
                foreach (var prop in new[] { "EXP Obtained  +10%", "Mesos Obtained  +5%", "Item Drop Rate  +5%" })
                {
                    if (DrawProps(g, prop.Split(':'), 5, picH, equip22ColorTable))
                    {
                        hasThirdContents = true;
                        hasOptionPart = true;
                        picH += 16;
                    }
                }
            }
            if (hasOptionPart)
                picH += 4;

            // 추가 능력치
            if (Gear.Additions.Count > 0 && !Gear.AdditionHideDesc)
            {
                List<string> texts = new List<string>();
                foreach (Addition addition in Gear.Additions)
                {
                    string conString = addition.GetConString(), propString = addition.GetPropString();
                    bool a = !string.IsNullOrEmpty(conString);
                    bool b = !string.IsNullOrEmpty(propString);
                    var text = "- ";
                    if (a)
                    {
                        text += conString;
                        if (b)
                        {
                            text += "\n";
                        }
                    }
                    if (b)
                    {
                        text += propString;
                    }

                    if (a || b)
                    {
                        texts.Add(text);
                    }
                }
                if (texts.Count > 0)
                {
                    hasThirdContents = true;
                    hasDescPart = true;

                    GearGraphics.DrawString(g, string.Join("\n\n", texts), GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 305, ref picH, 16, strictlyAlignLeft: 1);
                }
            }
            // 레벨당 능력치
            //绘制浮动属性
            if ((Gear.VariableStat != null && Gear.VariableStat.Count > 0))
            {
                int reqLvl;
                Gear.Props.TryGetValue(GearPropType.reqLevel, out reqLvl);
                TextRenderer.DrawText(g, $"Stats increase with Character Level (Up to Lv. " + reqLvl + ")", GearGraphics.EquipMDMoris9Font, new Point(15, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 16;

                int reduceLvl;
                Gear.Props.TryGetValue(GearPropType.reduceReq, out reduceLvl);

                int curLevel = charStat == null ? reqLvl : Math.Min(charStat.Level, reqLvl);

                foreach (var kv in Gear.VariableStat)
                {
                    hasThirdContents = true;
                    hasDescPart = true;

                    int dLevel = curLevel - reqLvl + reduceLvl;
                    //int addVal = (int)Math.Floor(kv.Value * dLevel);
                    //这里有一个计算上的错误 换方式执行
                    int addVal = (int)Math.Floor(new decimal(kv.Value) * dLevel);
                    string[] texts = ItemStringHelper.GetGearPropString22(kv.Key, addVal, 1);
                    string text = "- " + string.Join(" ", texts);
                    text += string.Format(" ({0:f1} x {1})", kv.Value, dLevel);
                    TextRenderer.DrawText(g, text, GearGraphics.EquipMDMoris9Font, new Point(15, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 16;
                }
                /*if (hasReduce)
                {
                    TextRenderer.DrawText(g, "업그레이드 및 강화 시, " + reqLvl + "Lv 무기로 취급", GearGraphics.EquipDetailFont, new Point(12, picH), ((SolidBrush)GearGraphics.GrayBrush2).Color, TextFormatFlags.NoPadding);
                    picH += 16;
                }*/
            }
            // 반지스킬
            if (Gear.Props.TryGetValue(GearPropType.limitBreak, out value) && value > 0) //突破上限
            {
                hasThirdContents = true;

                TextRenderer.DrawText(g, "Damage Cap", GearGraphics.EquipMDMoris9Font, new Point(15, picH), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding);
                GearGraphics.DrawString(g, $"#$z{value.ToString("N0")}#", GearGraphics.EquipMDMoris9Font, equip22ColorTable, 100, 280, ref picH, 16, alignment: Text.TextAlignment.Left);
            }
            int ringOpt, ringOptLv;
            if (Gear.Props.TryGetValue(GearPropType.ringOptionSkill, out ringOpt)
                && Gear.Props.TryGetValue(GearPropType.ringOptionSkillLv, out ringOptLv))
            {
                var opt = Potential.LoadFromWz(ringOpt, ringOptLv, PluginBase.PluginManager.FindWz);
                if (opt != null)
                {
                    hasThirdContents = true;
                    hasDescPart = true;

                    TextRenderer.DrawText(g, opt.ConvertSummary(), GearGraphics.EquipMDMoris9Font, new Point(15, picH), Color.White, TextFormatFlags.NoPadding);
                    picH += 16;
                }
            }
            // 샘플 미리보기
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

            if (Gear.Sample.Bitmap != null || willDrawMedalTag || willDrawChatBalloon || willDrawNameTag)
            {
                // picH -= 2;
                hasThirdContents = true;
                hasDescPart = true;

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
                    g.DrawImage(Gear.Sample.Bitmap, (bitmap.Width - 10 - Gear.Sample.Bitmap.Width) / 2, picH);
                    picH += Gear.Sample.Bitmap.Height;
                    picH += 4;
                }
                else if (medalResNode != null)
                {
                    GearGraphics.DrawNameTag(g, medalResNode, sr.Name.Replace("Medal of Honor", ""), bitmap.Width - 10, ref picH);
                    picH += 4;
                }
            }
            // 장비 설명
            if (!string.IsNullOrEmpty(sr.Desc))
            {
                hasThirdContents = true;
                hasDescPart = true;

                GearGraphics.DrawString(g, sr.Desc.Replace("#", " #").Trim(), GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 305, ref picH, 16, strictlyAlignLeft: 1);
            }
            // 값이 있는 설명
            if (!string.IsNullOrEmpty(Gear.EpicHs) && sr[Gear.EpicHs] != null)
            {
                var text = sr[Gear.EpicHs].Replace("#", " #").Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    hasThirdContents = true;
                    hasDescPart = true;

                    GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, new Dictionary<string, Color>() { { "c", Color.White } }, 15, 305, ref picH, 16, strictlyAlignLeft: 0);
                }
            }
            // 슈페리얼 장비 강화 설명
            if (Gear.HasTuc && maxStar > 0 && !Gear.GetBooleanValue(GearPropType.blockUpgradeStarforce))
            {
                if (Gear.Props.TryGetValue(GearPropType.superiorEqp, out value) && value > 0) //极真
                {
                    var text = ItemStringHelper.GetGearPropString22(GearPropType.superiorEqp, value)[0];
                    if (!string.IsNullOrEmpty (text))
                    {
                        hasThirdContents = true;
                        hasDescPart = true;

                        GearGraphics.DrawPlainText(g, text, GearGraphics.EquipMDMoris9Font, Color.White, 15, 305, ref picH, 16);
                    }
                }
            }

            // 펫장비 능력치 이전 주문서
            if (Gear.Props.TryGetValue(GearPropType.noPetEquipStatMoveItem, out value) && value != 0)
            {
                hasThirdContents = true;
                hasDescPart = true;

                GearGraphics.DrawString(g, "You cannot use the Pet Equipment Stat\nTransfer Scroll on this item.", GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 305, ref picH, 16, strictlyAlignLeft: 1);
            }
            // 캐시 이펙트
            if (Gear.Cash && Gear.type != GearType.pickaxe && !Gear.IsCashWeapon(Gear.type) && Gear.type != GearType.shovel && PluginBase.PluginManager.FindWz(string.Format("Effect/ItemEff.img/{0}/effect", Gear.ItemID)) != null)
            {
                hasThirdContents = true;
                hasDescPart = true;

                GearGraphics.DrawString(g, "#cThis item does not show in some situations, such as in the #cCharacter Info window.#", GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 305, ref picH, 16, strictlyAlignLeft: 1);
            }
            // 캐시 성향
            if (Gear.State == GearState.itemList)
            {
                List<string> texts = new List<string>();
                GearPropType[] inclineTypes = new GearPropType[]{
                    GearPropType.charismaEXP,
                    GearPropType.insightEXP,
                    GearPropType.willEXP,
                    GearPropType.craftEXP,
                    GearPropType.senseEXP,
                    GearPropType.charmEXP };

                string[] inclineString = new string[]{
                    "Ambition","Insight","Willpower","Diligence","Empathy","Charm"};

                for (int i = 0; i < inclineTypes.Length; i++)
                {
                    bool success = false;
                    if (inclineTypes[i] == GearPropType.charmEXP && Gear.Cash)
                    {
                        success = true;
                        switch (Gear.type)
                        {
                            case GearType.cashWeapon: value = 60; break;
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
                        texts.Add($"{inclineString[i]} +{value}");
                    }
                }

                if (texts.Count > 0 && Gear.Cash)
                {
                    hasThirdContents = true;
                    hasDescPart = true;

                    foreach (var text in texts)
                    {
                        TextRenderer.DrawText(g, $"{text} One Time when Equipped", GearGraphics.EquipMDMoris9Font, new Point(15, picH), Color.White, TextFormatFlags.NoPadding);
                        picH += 16;
                    }
                    TextRenderer.DrawText(g, $"Up to the Daily Maximum", GearGraphics.EquipMDMoris9Font, new Point(15, picH), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.NoPadding);
                    picH += 16;
                }
            }
            if (hasDescPart)
                picH += 4;

            // ----------------------------------------------------------------------
            bool thirdLineNeeded = hasThirdContents;

            // 강화 정보
            int enhance_starForce = 0;
            int enhance_scroll = 0;
            int enhance_bonusStat = 0;
            int enhance_potential = 0;
            int enhance_addiPotential = 0;
            int tuc = 0;
            bool enhanceable = Gear.GetBooleanValue(GearPropType.setExtraOption);
            Gear.Props.TryGetValue(GearPropType.tuc, out tuc);
            if (!Gear.Cash && Gear.IsEnhanceable(Gear.type) || enhanceable)
            {
                hasThirdContents = true;

                string text = "";
                bool fixedPotential = false;

                if (Gear.HasTuc)
                {
                    enhance_scroll = 1;
                }
                if (Gear.GetBooleanValue(GearPropType.exceptUpgrade))
                {
                    enhance_scroll = 0;
                }
                if (Gear.ScrollUp > 0)
                {
                    enhance_scroll = 2;
                }

                if (maxStar > 0)
                {
                    if (Gear.Props.TryGetValue(GearPropType.superiorEqp, out value) && value > 0)
                    {
                        enhance_starForce = 2;
                    }
                    else enhance_starForce = 1;
                }
                if (Gear.GetBooleanValue(GearPropType.blockUpgradeStarforce))
                {
                    enhance_starForce = 0;
                }

                if ((Gear.CanEnhanceBonusStat(Gear.type) && !Gear.GetBooleanValue(GearPropType.blockUpgradeExtraOption)) || Gear.GetBooleanValue(GearPropType.setExtraOption))
                {
                    enhance_bonusStat = 1;
                }

                if (Gear.CanPotential)
                {
                    enhance_potential = 1;
                    enhance_addiPotential = 1;
                }
                if (Gear.Props.TryGetValue(GearPropType.fixedPotential, out value) && value > 0)
                {
                    enhance_addiPotential = 0;
                    fixedPotential = true;
                }
                if (Gear.Props.TryGetValue(GearPropType.noPotential, out value) && value > 0)
                {
                    enhance_potential = 0;
                    enhance_addiPotential = 0;
                }
                if (Gear.IsDestinyWeapon)
                {
                    enhance_potential = 11;
                    enhance_addiPotential = 11;
                }
                else if (Gear.IsGenesisWeapon)
                {
                    enhance_potential = 10;
                    enhance_addiPotential = 10;
                }

                var cantEnhanceList = new List<string>();
                if (enhance_starForce == 0)
                    cantEnhanceList.Add("Star Force");
                if (enhance_scroll == 0)
                    cantEnhanceList.Add("Scroll");
                if (enhance_bonusStat == 0)
                    cantEnhanceList.Add("Bonus Stats");
                if (cantEnhanceList.Count > 0)
                {
                    GearGraphics.DrawString(g, $"{string.Join(", ", cantEnhanceList)} Can't Enhance", GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 305, ref picH, 16);
                }

                switch (enhance_starForce)
                {
                    case 0:
                        //text = $"#$d스타포스 강화 : 강화 불가#";
                        break;
                    case 1:
                        //text = $"#$d스타포스 : 없음# (최대 {maxStar}성)";
                        break;
                    case 2:
                        //text = $"#$d스타포스 (슈페리얼) : 없음# (최대 {maxStar}성)";
                        break;
                }
                if (!string.IsNullOrEmpty(text))
                    GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 305, ref picH, 16);

                switch (enhance_scroll)
                {
                    case 0:
                        //text = $"#$d주문서 강화 : 강화 불가#";
                        break;
                    case 1:
                        text = $"#$dScroll Enhancement None\n({tuc} Remaining, 0 Restore(s) Available)#";
                        break;
                    case 2:
                        text = $"Scroll Enhancement {Gear.ScrollUp} time(s)\n({tuc} Remaining, 0 Restore(s) Available)";
                        break;
                }
                if (!string.IsNullOrEmpty(text))
                    GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 305, ref picH, 16);

                /*
                switch (enhance_bonusStat)
                {
                    case 0:
                        text = $"#$d추가옵션 : 강화 불가#";
                        break;
                    case 1:
                        text = $"#$d추가옵션 : 없음";
                        break;
                }
                if (!string.IsNullOrEmpty(text))
                    GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 305, ref picH, 16);
                */

                GearGraphics.DrawString(g, "#$dCheck the enhancement details by using the Interact/Harvest key.#", GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 305, ref picH, 16);
                picH += 4;


                // ----------------------------------------------------------------------
                thirdLineNeeded = hasThirdContents;

                picH -= 5;
                AddLines(0, 7, ref picH);
                thirdLineNeeded = false;

                switch (enhance_potential)
                {
                    case 0:
                        text = $"#${GetPotentialColorTag(GearGrade.C)}Potential : Can't Enhance#";
                        g.DrawImage(GetPotentialGradeIcon(GearGrade.C), 15, picH);
                        GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, itemPotentialColorTable, 30, 305, ref picH, 16);
                        break;
                    case 1:
                        text = $"#${GetPotentialColorTag(Gear.Grade)}Potential : {GetPotentialString((int)Gear.Grade)}#{(fixedPotential ? " (No Additional Enhancements)": "")}";
                        g.DrawImage(GetPotentialGradeIcon(Gear.Grade), 15, picH);
                        GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, itemPotentialColorTable, 30, 305, ref picH, 16);

                        //绘制潜能
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
                            foreach (Potential potential in Gear.Options)
                            {
                                if (potential != null)
                                {
                                    g.DrawImage(GetPotentialGradeIcon(Gear.Grade, false), 15, picH);
                                    TextRenderer.DrawText(g, potential.ConvertSummary(), GearGraphics.EquipMDMoris9Font, new Point(30, picH), Color.White, TextFormatFlags.NoPadding);
                                    picH += 16;
                                }
                            }
                            picH += 4;
                        }
                        break;
                    case 10:
                        text = $"#${GetPotentialColorTag(GearGrade.S)}Potential : {GetPotentialString((int)GearGrade.S)}#";
                        g.DrawImage(GetPotentialGradeIcon(GearGrade.S), 15, picH);
                        GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, itemPotentialColorTable, 30, 305, ref picH, 16);
                        break;
                    case 11:
                        text = $"#${GetPotentialColorTag(GearGrade.C)}Potential : Genesis Weapon Stat Upgrade Transfer#";
                        g.DrawImage(GetPotentialGradeIcon(GearGrade.C), 15, picH);
                        GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, itemPotentialColorTable, 30, 305, ref picH, 16);
                        break;
                }

                switch (enhance_addiPotential)
                {
                    case 0:
                        text = $"#${GetPotentialColorTag(GearGrade.C)}Bonus Potential : Can't Enhance#";
                        g.DrawImage(GetPotentialGradeIcon(GearGrade.C), 15, picH);
                        GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, itemPotentialColorTable, 30, 305, ref picH, 16);
                        break;
                    case 1:
                        text = $"#${GetPotentialColorTag(Gear.Grade)}Bonus Potential : {GetPotentialString((int)Gear.Grade)}#";
                        g.DrawImage(GetPotentialGradeIcon(Gear.Grade), 15, picH);
                        GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, itemPotentialColorTable, 30, 305, ref picH, 16);

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
                            foreach (Potential potential in Gear.AdditionalOptions)
                            {
                                if (potential != null)
                                {
                                    g.DrawImage(GetPotentialGradeIcon(Gear.Grade, false), 15, picH);
                                    TextRenderer.DrawText(g, potential.ConvertSummary(), GearGraphics.EquipMDMoris9Font, new Point(30, picH), Color.White, TextFormatFlags.NoPadding);
                                    picH += 16;
                                }
                            }
                        }
                        break;
                    case 10:
                        text = $"#${GetPotentialColorTag(GearGrade.A)}Bonus Potential : {GetPotentialString((int)GearGrade.A)}#";
                        g.DrawImage(GetPotentialGradeIcon(GearGrade.A), 15, picH);
                        GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, itemPotentialColorTable, 30, 305, ref picH, 16);
                        break;
                    case 11:
                        text = $"#${GetPotentialColorTag(GearGrade.C)}Bonus Potential : Genesis Weapon Stat Upgrade Transfer#";
                        g.DrawImage(GetPotentialGradeIcon(GearGrade.C), 15, picH);
                        GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, itemPotentialColorTable, 30, 305, ref picH, 16);
                        break;
                }
                picH += 4;
            }
            else if (Gear.type == GearType.petEquip)
            {
                hasThirdContents = true;

                GearGraphics.DrawString(g, $"#$dScroll Enhancement None#\n({tuc} Remaining, 0 Restore(s) Available)", GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 305, ref picH, 16);
                picH += 4;
            }

            // 익셉셔널
            if (Gear.Props.TryGetValue(GearPropType.Etuc, out value) && value > 0)
            {
                AddLines(0, 6, ref picH, condition: thirdLineNeeded);
                thirdLineNeeded = false;

                var text = ItemStringHelper.GetGearPropString22(GearPropType.Etuc, value, 0)[0];
                g.DrawImage(GetPotentialGradeIcon(GearGrade.C), 15, picH);
                GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, equip22ColorTable, 30, 305, ref picH, 16);
                picH += 4;
            }

            // 소울
            if (!Gear.Cash && Gear.IsWeapon(Gear.type))
            {
                AddLines(0, 6, ref picH, condition: thirdLineNeeded);
                thirdLineNeeded = false;

                g.DrawImage(Resource.UIToolTipNew_img_Item_Equip_textIcon_soulWeapon_normal, 15, picH - 2);
                TextRenderer.DrawText(g, "Soul : Requires Soul Weapon Conversion", GearGraphics.EquipMDMoris9Font, new Point(29, picH), Color.White, TextFormatFlags.NoPadding);
                picH += 20;
            }

            // 기타 속성
            //额外属性
            var attrList = GetGearAttributeString();
            if (attrList.Count > 0)
            {
                if (thirdLineNeeded)
                {
                    picH += 4;
                    thirdLineNeeded = false;
                }

                foreach (var text in attrList)
                {
                    GearGraphics.DrawString(g, text, GearGraphics.EquipMDMoris9Font, equip22ColorTable, 15, 305, ref picH, 16);
                }
            }
            /*
            if (Gear.Props.TryGetValue(GearPropType.limitBreak, out value) && value > 0)
            {
                TextRenderer.DrawText(g, "突破上限武器", GearGraphics.EquipDetailFont, new Point(width, picH), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.HorizontalCenter);
                picH += 16;
            }
            */

            /*
            if (Gear.Props.TryGetValue(GearPropType.@sealed, out value))
            {
                bool max = (Gear.Seals != null && value >= Gear.Seals.Count);
                TextRenderer.DrawText(g, "封印解除阶段 : " + (max ? "MAX" : value.ToString()), GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
                TextRenderer.DrawText(g, "封印解除经验值 : " + (max ? "MAX" : "0%"), GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.OrangeBrush3).Color, TextFormatFlags.NoPadding);
                picH += 15;
            }

            if (Gear.Props.TryGetValue(GearPropType.limitBreak, out value) && value > 0) //突破上限
            {
                TextRenderer.DrawText(g, ItemStringHelper.GetGearPropString(GearPropType.limitBreak, value), GearGraphics.EquipDetailFont, new Point(13, picH), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding);
                picH += 15;
                hasPart2 = true;
            }
            */

            if (Gear.Cash && ShowCashPurchasePrice)
            {
                if (CharaSimLoader.LoadedCommoditiesByItemIdInteractive.ContainsKey(Gear.ItemID))
                {
                    int price = CharaSimLoader.LoadedCommoditiesByItemIdInteractive[Gear.ItemID].Values.ToList()[0];
                    if (price > 0)
                    {
                        picH += 16;
                        string approxPrice = "";
                        if (Translator.DefaultDesiredCurrency != "none")
                        {
                            approxPrice = $" ({Translator.GetConvertedCurrency(price, titleLanguage)})";
                        }
                        GearGraphics.DrawString(g, "- Price: " + price + "NX" + approxPrice, GearGraphics.EquipDetailFont, 13, 244, ref picH, 16);
                    }
                }
            }

            picH += 9;
            g.Dispose();
            return bitmap;
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
                    var defaultRenderer = new SetItemTooltipRender22();
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
                    TextRenderer.DrawText(g, "[Fixed level at acquisition]", GearGraphics.EquipDetailFont, new Point(261, picHeight), ((SolidBrush)GearGraphics.Equip22BrushEmphasis).Color, TextFormatFlags.HorizontalCenter);
                    picHeight += 16;
                }

                for (int i = 0; i < Gear.Levels.Count; i++)
                {
                    var info = Gear.Levels[i];
                    TextRenderer.DrawText(g, "Level " + info.Level + (i >= Gear.Levels.Count - 1 ? "(MAX)" : null), GearGraphics.EquipDetailFont, new Point(10, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding);
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
                            TextRenderer.DrawText(g, propString, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                            picHeight += 15;
                        }
                    }
                    if (info.Skills.Count > 0)
                    {
                        string title = string.Format("{2:P2} ({0}/{1}) chance to obtain a skill:", info.Prob, info.ProbTotal, info.Prob * 1.0 / info.ProbTotal);
                        TextRenderer.DrawText(g, title, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                        picHeight += 15;
                        foreach (var kv in info.Skills)
                        {
                            StringResult sr = null;
                            if (this.StringLinker != null)
                            {
                                this.StringLinker.StringSkill.TryGetValue(kv.Key, out sr);
                            }
                            string text = string.Format("{0} +{2} Level", sr == null ? null : sr.Name, kv.Key, kv.Value);
                            TextRenderer.DrawText(g, text, GearGraphics.EquipDetailFont, new Point(10, picHeight), ((SolidBrush)GearGraphics.Equip22BrushEmphasis).Color, TextFormatFlags.NoPadding);
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
                        TextRenderer.DrawText(g, title, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                        picHeight += 15;
                        foreach (var kv in info.EquipmentSkills)
                        {
                            StringResult sr = null;
                            if (this.StringLinker != null)
                            {
                                this.StringLinker.StringSkill.TryGetValue(kv.Key, out sr);
                            }
                            string text = string.Format("+{2} {0}", sr == null ? null : sr.Name, kv.Key, kv.Value);
                            TextRenderer.DrawText(g, text, GearGraphics.EquipDetailFont, new Point(10, picHeight), ((SolidBrush)GearGraphics.Equip22BrushEmphasis).Color, TextFormatFlags.NoPadding);
                            picHeight += 15;
                        }
                    }
                    if (info.Exp > 0)
                    {
                        TextRenderer.DrawText(g, "Required EXP : " + info.Exp + "%", GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                        picHeight += 15;
                    }
                    if (info.Point > 0 && info.DecPoint > 0)
                    {
                        TextRenderer.DrawText(g, "Required Anti-magic : " + info.Point + " (-" + info.DecPoint + " daily)", GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
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

                    TextRenderer.DrawText(g, "Level" + info.Level + (i >= Gear.Seals.Count - 1 ? "(MAX)" : null), GearGraphics.EquipDetailFont, new Point(10, picHeight), ((SolidBrush)GearGraphics.GreenBrush2).Color, TextFormatFlags.NoPadding);
                    picHeight += 16;
                    var props = this.IsCombineProperties ? Gear.CombineProperties(info.BonusProps) : info.BonusProps;
                    foreach (var kv in props)
                    {
                        string propString = ItemStringHelper.GetGearPropString(kv.Key, kv.Value);
                        TextRenderer.DrawText(g, propString, GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
                        picHeight += 16;
                    }
                    if (info.HasIcon)
                    {
                        Bitmap icon = info.Icon.Bitmap ?? info.IconRaw.Bitmap;
                        if (icon != null)
                        {
                            TextRenderer.DrawText(g, "Icon : ", GearGraphics.EquipDetailFont, new Point(10, picHeight + icon.Height / 2 - 6), Color.White, TextFormatFlags.NoPadding);
                            g.DrawImage(icon, 52, picHeight);
                            picHeight += icon.Height;
                        }
                    }
                    if (info.Exp > 0)
                    {
                        TextRenderer.DrawText(g, "Required EXP : " + info.Exp + "%", GearGraphics.EquipDetailFont, new Point(10, picHeight), Color.White, TextFormatFlags.NoPadding);
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

        private void FillRect(Graphics g, TextureBrush brush, int x, int y0, int y1)
        {
            brush.ResetTransform();
            brush.TranslateTransform(x, y0);
            g.FillRectangle(brush, x, y0, brush.Image.Width, y1 - y0);
        }

        private List<string> GetGearTopAttributeString()
        {
            int value;
            List<string> tags = new List<string>();
            List<string> tempTags = new List<string>();

            // 교환
            if (Gear.Props.TryGetValue(GearPropType.tradeBlock, out value) && value != 0)
            {
                tempTags.Add(ItemStringHelper.GetGearPropString22(GearPropType.tradeBlock, value)[0]);
            }
            if (Gear.Props.TryGetValue(GearPropType.equipTradeBlock, out value) && value != 0)
            {
                if (Gear.State == GearState.itemList)
                {
                    tempTags.Add(ItemStringHelper.GetGearPropString22(GearPropType.equipTradeBlock, value)[0]);
                }
                else
                {
                    string tradeBlock = ItemStringHelper.GetGearPropString22(GearPropType.tradeBlock, 1)[0];
                    if (!tempTags.Contains(tradeBlock))
                        tempTags.Add(tradeBlock);
                }
            }
            {
                var text = string.Join(" ", tempTags);
                // 가위 가능 횟수
                if (Gear.Props.TryGetValue(GearPropType.CuttableCount, out value) && value > 0)
                {
                    text += ItemStringHelper.GetGearPropString22(GearPropType.CuttableCount, value)[0];
                }
                tags.Add(text);
            }
            tempTags.Clear();

            // 계정 내 교환
            if (Gear.Props.TryGetValue(GearPropType.accountSharable, out value) && value != 0)
            {
                int value2;
                if (Gear.Props.TryGetValue(GearPropType.sharableOnce, out value2) && value2 != 0)
                {
                    tags.Add(ItemStringHelper.GetGearPropString22(GearPropType.sharableOnce, value2)[0]);
                }
                else
                {
                    tags.Add(ItemStringHelper.GetGearPropString22(GearPropType.accountSharable, value)[0]);
                }
            }

            if (Gear.State == GearState.itemList && Gear.Cash && (!Gear.Props.TryGetValue(GearPropType.noMoveToLocker, out value) || value == 0) && (!Gear.Props.TryGetValue(GearPropType.tradeBlock, out value) || value == 0) && (!Gear.Props.TryGetValue(GearPropType.accountSharable, out value) || value == 0) && (!Gear.Props.TryGetValue(GearPropType.noTradeLimitCash, out value) || value <= 0))
            {
                tags.Add("#$rThis item cannot be traded once it has been used.#");
            }

            // 기간제
            if (Gear.Props.TryGetValue(GearPropType.timeLimited, out value) && value != 0)
            {
                DateTime time = DateTime.Now.AddDays(7d);
                var text = $"#$r{ItemStringHelper.GetGearPropString22(GearPropType.timeLimited, value)[0]}: {time.ToString("M/d/yyyy HH:mm")} UTC" +
                    $"{ItemStringHelper.GetGearPropString22(GearPropType.notExtend, value)[0]}#";
                tags.Add(text);
            }

            // 민팅
            if (Gear.Props.TryGetValue(GearPropType.mintable, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString22(GearPropType.mintable, value)[0]);
            }
            else if (CharaSimLoader.LoadedMintableNFTItems.Contains(Gear.ItemID) || CharaSimLoader.LoadedMintableSBTItems.Contains(Gear.ItemID) || CharaSimLoader.LoadedMintableFTItems.Contains(Gear.ItemID))
            {
                tags.Add(ItemStringHelper.GetGearPropString22(GearPropType.mintable, 1)[0]);
            }

            return tags.Where(text => !string.IsNullOrEmpty(text)).ToList();
        }

        private List<string> GetGearAttributeString()
        {
            int value;
            List<string> tags = new List<string>();
            List<string> tempTags = new List<string>();


            // 카르마의 가위
            if (Gear.Props.TryGetValue(GearPropType.tradeAvailable, out value) && value > 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString22(GearPropType.tradeAvailable, value)[0]);
            }

            // 쉐어 네임 택
            if (Gear.Props.TryGetValue(GearPropType.accountShareTag, out value) && value > 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString22(GearPropType.accountShareTag, value)[0]);
            }

            if (Gear.Props.TryGetValue(GearPropType.colorvar, out value) && value > 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString(GearPropType.colorvar, value));
            }

            // 재발급 불가
            if (Gear.Props.TryGetValue(GearPropType.reissueBan, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString22(GearPropType.reissueBan, value, 0)[0]);
            }

            // 모루
            if (Gear.Props.TryGetValue(GearPropType.noLookChange, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString22(GearPropType.noLookChange, value)[0]);
            }
            if ((Gear.ItemID / 10000 >= 161 && Gear.ItemID / 10000 <= 165) || (Gear.ItemID / 10000 >= 194 && Gear.ItemID / 10000 <= 197))
            {
                tags.Add("#$rCannot use Fusion Anvil#");
            }

            // 중복 소지/장착
            if (Gear.Props.TryGetValue(GearPropType.only, out value) && value != 0)
            {
                tempTags.Add(ItemStringHelper.GetGearPropString22(GearPropType.only, value, 0)[0]);
            }
            if (Gear.Props.TryGetValue(GearPropType.onlyEquip, out value) && value != 0)
            {
                tempTags.Add(ItemStringHelper.GetGearPropString22(GearPropType.onlyEquip, value)[0]);
            }
            {
                // 중복 착용 금지
                foreach (KeyValuePair<int, ExclusiveEquip> kv in CharaSimLoader.LoadedExclusiveEquips)
                {
                    if (kv.Value.Items.Contains(Gear.ItemID))
                    {
                        string exclusiveEquip;
                        if (!string.IsNullOrEmpty(kv.Value.Info))
                        {
                            var itemGroup = kv.Value.Info;
                            var delStr = "s cannot be stacked.";
                            if (kv.Value.Info.Contains(delStr))
                            {
                                itemGroup = itemGroup.Replace(delStr, "");
                            }
                            else
                            {
                                itemGroup += " Type Item";
                            }
                            exclusiveEquip = $"#$rCan't Equip Duplicate# ({itemGroup}) #$rItems#";
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

                            exclusiveEquip = $"#$rCan't Equip Duplicate# ({string.Join(", ", itemNames)} #$rItems#";
                        }
                        if (!string.IsNullOrEmpty(exclusiveEquip))
                        {
                            tempTags.Add(exclusiveEquip);
                        }
                        break;
                    }
                }

                tags.Add(string.Join("#$r,# ", tempTags));
            }
            tempTags.Clear();

            // 프리즘 불가
            if (Gear.Props.TryGetValue(GearPropType.noPrism, out value) && value != 0)
            {
                tags.Add(ItemStringHelper.GetGearPropString22(GearPropType.noPrism, value)[0]);
            }

            return tags.Where(text => !string.IsNullOrEmpty(text)).ToList();
        }

        private void DrawStar(Graphics g, int maxStar, ref int picH)
        {
            if (maxStar > 0)
            {
                for (int i = 0; i < maxStar; i += 15)
                {
                    int starLine = Math.Min(maxStar - i, 15);
                    int totalWidth = starLine * 11 + (starLine / 5 - 1) * 10;
                    int dx = 161 - totalWidth / 2;
                    if ((maxStar - i) % 5 != 0 && maxStar - i < 15) dx -= 5;
                    for (int j = 0; j < starLine; j++)
                    {
                        g.DrawImage((i + j < Gear.Star) ?
                            Resource.UIToolTipNew_img_Item_Equip_textIcon_starForce_star : Resource.UIToolTipNew_img_Item_Equip_textIcon_starForce_empty,
                            dx, picH);
                        dx += 11;
                        if (j > 0 && j % 5 == 4)
                        {
                            dx += 10;
                        }
                    }
                    picH += 18;
                }
                picH += 2;
            }
        }

        private void DrawEnchantBox(Graphics g, int tuc, int pot1, int pot2, ref int picH)
        {
            if ((GearType)Gear.Type == GearType.petEquip)
            {
                return;
            }    

            if (tuc == 0 && pot1 == 0 && pot2 == 0)
                return;

            var font = GearGraphics.EquipMDMoris9Font;
            int startX = 14;
            int inteval = 74;
            int offset;
            int pos;
            string text;

            for (int i = 0; i < 4; i++)
            {
                g.DrawImage(Resource.UIToolTipNew_img_Item_Equip_frame_common_box, startX + inteval * i, picH);
            }

            picH += 5;

            // scroll
            text = tuc == 0 ? "-" : $"{tuc} 회";
            offset = (TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width + 15) / 2;
            pos = startX + inteval * 0 + 36 - offset;
            g.DrawImage(Resource.UIToolTipNew_img_Item_Equip_textIcon_scroll_normal, pos, picH);
            TextRenderer.DrawText(g, text, font, new Point(pos + 15, picH), tuc == 0 ? ((SolidBrush)GearGraphics.Equip22BrushDarkGray).Color : ((SolidBrush)GearGraphics.Equip22BrushScroll).Color, TextFormatFlags.NoPadding);

            // bonus stat
            text = $"-";
            offset = (TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width + 17) / 2;
            pos = startX + inteval * 1 + 36 - offset;
            g.DrawImage(Resource.UIToolTipNew_img_Item_Equip_textIcon_bonusStat_normal, pos, picH);
            TextRenderer.DrawText(g, text, font, new Point(pos + 17, picH), ((SolidBrush)GearGraphics.Equip22BrushDarkGray).Color, TextFormatFlags.NoPadding);

            // potential
            text = GetPotentialString(pot1);
            offset = (TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width + 14) / 2;
            pos = startX + inteval * 2 + 36 - offset;
            g.DrawImage(Resource.UIToolTipNew_img_Item_Equip_textIcon_potential_normal, pos, picH);
            TextRenderer.DrawText(g, text, font, new Point(pos + 14, picH), GetPotentialColor(pot1), TextFormatFlags.NoPadding);

            // addi potential
            text = GetPotentialString(pot2);
            offset = (TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width + 16) / 2;
            pos = startX + inteval * 3 + 36 - offset;
            g.DrawImage(Resource.UIToolTipNew_img_Item_Equip_textIcon_additionalPotential_normal, pos, picH);
            TextRenderer.DrawText(g, text, font, new Point(pos + 16, picH), GetPotentialColor(pot2), TextFormatFlags.NoPadding);

            picH += 25;
        }

        private string GetPotentialString(int grade)
        {
            switch(grade)
            {
                case 0:
                    return "None";
                case 1:
                    return "Rare";
                case 2:
                    return "Epic";
                case 3:
                    return "Unique";
                case 4:
                    return "Legendary";
                default:
                    return "-";
            }
        }

        private Color GetPotentialColor(int grade)
        {
            switch (grade)
            {
                case 1:
                    return ((SolidBrush)GearGraphics.Equip22BrushRare).Color;
                case 2:
                    return ((SolidBrush)GearGraphics.Equip22BrushEpic).Color;
                case 3:
                    return ((SolidBrush)GearGraphics.Equip22BrushEmphasis).Color;
                case 4:
                    return ((SolidBrush)GearGraphics.Equip22BrushLegendary).Color;
                default:
                    return ((SolidBrush)GearGraphics.Equip22BrushDarkGray).Color;
            }
        }

        private String GetPotentialColorTag(GearGrade grade)
        {
            switch (grade)
            {
                default:
                case GearGrade.C: return "n";
                case GearGrade.B: return "r";
                case GearGrade.A: return "e";
                case GearGrade.S: return "u";
                case GearGrade.SS: return "l";
            }
        }

        private Image GetPotentialGradeIcon(GearGrade grade, bool isTitle = true)
        {
            switch (grade)
            {
                default:
                case GearGrade.C: return Resource.UIToolTipNew_img_Item_Equip_textIcon_potential_title_normal;
                case GearGrade.B: return isTitle ? Resource.UIToolTipNew_img_Item_Equip_textIcon_potential_title_rare : Resource.UIToolTipNew_img_Item_Equip_textIcon_potential_detail_rare;
                case GearGrade.A: return isTitle ? Resource.UIToolTipNew_img_Item_Equip_textIcon_potential_title_epic : Resource.UIToolTipNew_img_Item_Equip_textIcon_potential_detail_epic;
                case GearGrade.S: return isTitle ? Resource.UIToolTipNew_img_Item_Equip_textIcon_potential_title_unique : Resource.UIToolTipNew_img_Item_Equip_textIcon_potential_detail_unique;
                case GearGrade.SS: return isTitle ? Resource.UIToolTipNew_img_Item_Equip_textIcon_potential_title_legendary : Resource.UIToolTipNew_img_Item_Equip_textIcon_potential_detail_legendary;
            }
        }

        private void DrawCategory(Graphics g, int picH)
        {
            var max_rows = 2;
            var line_height = 17;
            List<List<string>> all_categories = new List<List<string>>();
            for (int i = 0; i < max_rows; i++)
            {
                all_categories.Add(new List<string>());
            }

            if (Gear.IsWeapon(Gear.type) || Gear.IsCashWeapon(Gear.type))
            {
                all_categories[0].Add("Weapon");
                if (!Gear.Cash && (Gear.IsLeftWeapon(Gear.type) || Gear.type == GearType.katara))
                {
                    all_categories[0].Add("One-handed");
                }
                else if (!Gear.Cash && Gear.IsDoubleHandWeapon(Gear.type))
                {
                    all_categories[0].Add("Two-handed");
                }
            }
            else if (Gear.IsSubWeapon(Gear.type) || Gear.type == GearType.shield)
            {
                all_categories[0].Add("Sub Weapon");
            }
            else if (Gear.IsEmblem(Gear.type))
            {
                all_categories[0].Add("Emblem / Power Source");
            }
            else if (Gear.IsArmor(Gear.type))
            {
                all_categories[0].Add("Armor");
            }
            else if (Gear.IsAccessory(Gear.type))
            {
                all_categories[0].Add("Accessory");
            }
            else if (Gear.IsMechanicGear(Gear.type))
            {
                all_categories[0].Add("Mechanic Equipment");
            }
            else if (Gear.IsDragonGear(Gear.type))
            {
                all_categories[0].Add("Dragon Equipment");
            }

            var text = ItemStringHelper.GetGearTypeString(Gear.type);
            if (!string.IsNullOrEmpty(text))
            {
                all_categories[1].Add(text);
            }

            if (all_categories[0].Count + all_categories[1].Count <= 0) return;
            if (all_categories[1].Count > 0) picH -= line_height; // has second row
            var font = GearGraphics.EquipMDMoris9Font;
            var ww = res["category_w"].Image.Width;
            var ew = res["category_e"].Image.Width;
            var ch = res["category_c"].Image.Height;

            foreach (var categories in all_categories)
            {
                var sp = 309;
                for (int i = categories.Count - 1; i >= 0; i--)
                {
                    var length = TextRenderer.MeasureText(g, categories[i], font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;

                    g.DrawImage(res["category_w"].Image, sp - ew - length - ww, picH);
                    g.FillRectangle(res["category_c"], sp - ew - length, picH, length, ch);
                    TextRenderer.DrawText(g, categories[i], font, new Point(sp - ew - length, picH), ((SolidBrush)GearGraphics.Equip22BrushGray).Color, TextFormatFlags.NoPadding);
                    g.DrawImage(res["category_e"].Image, sp - ew, picH);

                    sp -= (3 + ew + length + ww);
                }
                picH += line_height;
            }
        }

        private bool DrawProps(Graphics g, string[] propStr, int dx, int y, Dictionary<string, Color> colorTable)
        {
            if (!string.IsNullOrEmpty(propStr[0]))
            {
                var propLength = TextRenderer.MeasureText(g, propStr[0], GearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
                var moveX = propLength + 12;

                GearGraphics.DrawString(g, propStr[0], GearGraphics.EquipMDMoris9Font, colorTable, 15, 305, ref y, 0);
                if (!string.IsNullOrEmpty(propStr[1]))
                {
                    var propLength2 = TextRenderer.MeasureText(g, propStr[1], GearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
                    var moveX2 = propLength2 + 12;
                    GearGraphics.DrawString(g, propStr[1], GearGraphics.EquipMDMoris9Font, colorTable, 15 + moveX, 305, ref y, 0, alignment: Text.TextAlignment.Left);

                    if (!string.IsNullOrEmpty(propStr[2]))
                    {
                        if (Gear.ScrollUp > 0)
                        {
                            propStr[2] = propStr[2].Replace("#$e", "#$s");
                        }
                        GearGraphics.DrawString(g, propStr[2], GearGraphics.EquipMDMoris9Font, colorTable, 15 + moveX + moveX2, 310, ref y, 0);
                    }
                }
                return true;
            }
            return false;
        }

        public static string JoinStringWithNewline(Graphics g, string separator, List<string> texts, int width)
        {
            if (texts == null || texts.Count <= 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            int count = 1;
            int width_total = TextRenderer.MeasureText(g, texts[0] + separator, GearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            int width_add = 0;
            sb.Append(texts[0]);

            while (count < texts.Count)
            {
                sb.Append(separator);

                width_add = TextRenderer.MeasureText(g, texts[count] + separator, GearGraphics.EquipMDMoris9Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
                width_total += width_add;
                if (width_total > width)
                {
                    width_total = width_add;
                    sb.Append("\n");
                }

                sb.Append(texts[count++]);
            }

            return sb.ToString();
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
