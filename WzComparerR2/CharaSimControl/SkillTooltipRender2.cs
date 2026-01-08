using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.WzLib;
using Resource = CharaSimResource.Resource;

namespace WzComparerR2.CharaSimControl
{
    public class SkillTooltipRender2 : TooltipRender
    {
        public SkillTooltipRender2()
        {
        }

        public Skill Skill { get; set; }

        public override object TargetItem
        {
            get { return this.Skill; }
            set { this.Skill = value as Skill; }
        }

        public bool ShowProperties { get; set; } = true;
        public bool ShowDelay { get; set; }
        public bool ShowArea { get; set; }
        public bool ShowReqSkill { get; set; } = true;
        public bool DisplayCooltimeMSAsSec { get; set; } = true;
        public bool DisplayPermyriadAsPercent { get; set; } = true;
        public bool IgnoreEvalError { get; set; } = false;
        public bool IsWideMode { get; set; } = true;
        public bool Enable22AniStyle { get; set; }
        public Dictionary<string, List<string>> DiffSkillTags { get; set; } = new Dictionary<string, List<string>>();
        public Wz_Node wzNode { get; set; } = null;

        public TooltipRender LinkRidingGearRender { get; set; }
        public string ParsedHdesc { get; set; }

        public override Bitmap Render()
        {
            return Render(false);
        }
        public Bitmap Render(bool doHighlight)
        {
            if (this.Skill == null)
            {
                return null;
            }

            CanvasRegion region = this.IsWideMode ? (this.Enable22AniStyle ? CanvasRegion._22AniWide : CanvasRegion.Wide) : (this.Enable22AniStyle ? CanvasRegion._22AniOriginal : CanvasRegion.Original);

            int picHeight;
            List<int> splitterH;
            Bitmap originBmp = RenderSkill(region, out picHeight, out splitterH, doHighlight);
            Bitmap ridingGearBmp = null;

            int vehicleID = Skill.VehicleID;
            if (vehicleID == 0)
            {
                vehicleID = PluginBase.PluginManager.FindWz(string.Format(@"Skill\RidingSkillInfo.img\{0:D7}\vehicleID", Skill.SkillID)).GetValueEx<int>(0);
            }
            if (vehicleID != 0)
            {
                Wz_Node imgNode = PluginBase.PluginManager.FindWz(string.Format(@"Character\TamingMob\{0:D8}.img", vehicleID));
                if (imgNode != null)
                {
                    Gear gear = Gear.CreateFromNode(imgNode, path => PluginBase.PluginManager.FindWz(path));
                    if (gear != null)
                    {
                        ridingGearBmp = RenderLinkRidingGear(gear);
                    }
                }
            }

            Size totalSize = new Size(originBmp.Width, picHeight);
            Point ridingGearOrigin = Point.Empty;

            if (ridingGearBmp != null)
            {
                totalSize.Width += ridingGearBmp.Width;
                totalSize.Height = Math.Max(picHeight, ridingGearBmp.Height);
                ridingGearOrigin.X = originBmp.Width;
            }

            Point hexaSkillDescOrigin = Point.Empty;
            Bitmap hexaSkillDescBmp = RenderHexaDesc(region);
            if ((Skill.Origin || Skill.Ascent) && !Skill.Invisible)
            {
                totalSize.Width += hexaSkillDescBmp.Width;
                totalSize.Height = Math.Max(picHeight, hexaSkillDescBmp.Height);
                hexaSkillDescOrigin.X = originBmp.Width;
            }

            Bitmap tooltip = new Bitmap(totalSize.Width, totalSize.Height);
            Graphics g = Graphics.FromImage(tooltip);

            //绘制背景区域
            GearGraphics.DrawNewTooltipBack(g, 0, 0, originBmp.Width, picHeight);
            if (splitterH != null && splitterH.Count > 0)
            {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                foreach (var y in splitterH)
                {
                    DrawV6SkillDotline(g, region.SplitterX1, region.SplitterX2, y);
                }
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            }

            //复制图像
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, originBmp.Width, picHeight), GraphicsUnit.Pixel);

            //左上角
            if (!Enable22AniStyle) g.DrawImage(Resource.UIToolTip_img_Skill_Frame_cover, 3, 3);

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, Skill.SkillID.ToString("d7"), true);
            }

            if (ridingGearBmp != null)
            {
                g.DrawImage(ridingGearBmp, ridingGearOrigin.X, ridingGearOrigin.Y,
                    new Rectangle(Point.Empty, ridingGearBmp.Size), GraphicsUnit.Pixel);
            }

            if (hexaSkillDescBmp != null)
            {
                g.DrawImage(hexaSkillDescBmp, hexaSkillDescOrigin.X, hexaSkillDescOrigin.Y,
                    new Rectangle(Point.Empty, hexaSkillDescBmp.Size), GraphicsUnit.Pixel);

            }

            if (originBmp != null)
                originBmp.Dispose();
            if (ridingGearBmp != null)
                ridingGearBmp.Dispose();
            if (hexaSkillDescBmp != null)
                hexaSkillDescBmp.Dispose();

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderSkill(CanvasRegion region, out int picH, out List<int> splitterH, bool doHighlight = false)
        {
            Bitmap bitmap = new Bitmap(region.Width, DefaultPicHeight);
            Graphics g = Graphics.FromImage(bitmap);
            StringFormat format = (StringFormat)StringFormat.GenericDefault.Clone();
            var v6SkillSummaryFontColorTable = new Dictionary<string, Color>()
            {
                { "c", GearGraphics.OrangeBrushColor },
                { "$g", GearGraphics.gearCyanColor },
            };

            picH = 0;
            splitterH = new List<int>();
            string skillIDstr = Skill.SkillID.ToString().PadLeft(7, '0');

            //获取文字
            if (StringLinker == null || !(StringLinker.StringSkill.TryGetValue(Skill.SkillID, out StringResult _sr) && _sr is StringResultSkill sr))
            {
                sr = new StringResultSkill();
                sr.Name = "(null)";
            }

            // Roguelike Check
            if (Skill.IsRoguelikeSkill)
            {
                if (StringLinker == null || !(StringLinker.StringRoguelikeSkill.TryGetValue(Skill.SkillID, out StringResult _sr2) && _sr2 is StringResultSkill sr2))
                {
                    sr2 = new StringResultSkill();
                    sr2.Name = "(null)";
                }
                sr = sr2;
            }
            else if (Skill.IsGuildCastleResearch)
            {
                switch (Skill.GuildCastleResearchType)
                {
                    case 0:
                        if (StringLinker == null || !(StringLinker.StringGuildCastleGuildResearch.TryGetValue(Skill.SkillID, out StringResult _sr2) && _sr2 is StringResultSkill sr2))
                        {
                            sr2 = new StringResultSkill();
                            sr2.Name = "(null)";
                        }
                        sr = sr2;
                        break;
                    case 1:
                        if (StringLinker == null || !(StringLinker.StringGuildCastlePersonalResearch.TryGetValue(Skill.SkillID, out _sr2) && _sr2 is StringResultSkill sr3))
                        {
                            sr3 = new StringResultSkill();
                            sr3.Name = "(null)";
                        }
                        sr = sr3;
                        break;
                }
            }

            bool isTranslateRequired = Translator.IsTranslateEnabled;
            bool isNewLineRequired = false;
            string translatedSkillName = "";
            if (isTranslateRequired)
            {
                translatedSkillName = Translator.TryCheckStringIndex(Skill.SkillID, "ms_skill", out string t) ? t : Translator.TranslateString(sr.Name, true);
                SizeF titleSize;
                titleSize = TextRenderer.MeasureText(g, translatedSkillName + " (" + sr.Name + ")", GearGraphics.ItemNameFont2, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix);
                if (titleSize.Width > (int)(0.96 * region.Width))
                {
                    isNewLineRequired = true;
                }
            }

            // for 6th job skills

            //绘制技能名称
            if (isTranslateRequired)
            {
                string mergedSkillName;
                if (isNewLineRequired)
                {
                    mergedSkillName = Translator.MergeString(sr.Name, translatedSkillName, 1, false, true);
                }
                else
                {
                    mergedSkillName = Translator.MergeString(sr.Name, translatedSkillName, 0, false, true);
                }
                if (!Skill.Origin && !Skill.Ascent)
                {
                    g.DrawImage(Resource.ToolTip_Equip_Dot_0, 9, picH + 15);//GMS Version blue dot in SKILLS
                    format.Alignment = StringAlignment.Near;
                    TextRenderer.DrawText(g, mergedSkillName, GearGraphics.ItemNameFont2, new Point(13, 10), Color.White, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
                }
                if (Skill.Origin || Skill.Ascent)
                {
                    if (Skill.Origin) g.DrawImage(Resource.UIWindow2_img_Skill_skillTypeIcon_origin, 20, 11);
                    else if (Skill.Ascent) g.DrawImage(Resource.UIWindow2_img_Skill_skillTypeIcon_ascent, 20, 11);
                    g.DrawImage(Resource.ToolTip_Equip_Dot_0, 92, picH + 15);//GMS Version blue dot in SKILLS
                    format.Alignment = StringAlignment.Near;
                    TextRenderer.DrawText(g, mergedSkillName, GearGraphics.ItemNameFont2, new Point(96, 10), Color.White, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
                }
                if (translatedSkillName.Contains(Environment.NewLine))
                {
                    picH += 30;
                }
            }
            else
            {
                if (!Skill.Origin && !Skill.Ascent)
                {
                    g.DrawImage(Resource.ToolTip_Equip_Dot_0, 9, picH + 15);//GMS Version blue dot in SKILLS
                    format.Alignment = StringAlignment.Near;
                    TextRenderer.DrawText(g, sr.Name, GearGraphics.ItemNameFont2, new Point(13, 10), Color.White, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
                }
                if (Skill.Origin || Skill.Ascent)
                {
                    if (Skill.Origin) g.DrawImage(Resource.UIWindow2_img_Skill_skillTypeIcon_origin, 20, 11);
                    else if (Skill.Ascent) g.DrawImage(Resource.UIWindow2_img_Skill_skillTypeIcon_ascent, 20, 11);
                    g.DrawImage(Resource.ToolTip_Equip_Dot_0, 92, picH + 15);//GMS Version blue dot in SKILLS
                    format.Alignment = StringAlignment.Near;
                    TextRenderer.DrawText(g, sr.Name, GearGraphics.ItemNameFont2, new Point(96, 10), Color.White, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
                }
            }


            //绘制图标
            if (Skill.Icon.Bitmap != null)
            {
                picH = 33;
                g.DrawImage(Resource.UIToolTip_img_Skill_Frame_iconBackgrnd, 13, picH - 2);
                g.DrawImage(GearGraphics.EnlargeBitmap(Skill.Icon.Bitmap),
                15 + (1 - Skill.Icon.Origin.X) * 2,
                picH + (33 - Skill.Icon.Bitmap.Height) * 2);
            }



            //绘制desc
            picH = 35;
            if (Skill.HyperStat)
                GearGraphics.DrawString(g, "[Master Level: " + Skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            else if (!Skill.PreBBSkill)
                GearGraphics.DrawString(g, "[Master Level: " + Skill.MaxLevel + "]", GearGraphics.ItemDetailFont2, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);

            if (sr.Desc != null)
            if (sr.Desc != null)
            {
                Dictionary<string, string> skillCommon = Skill.Common;
                if (Skill.PerJobAttackInfo.Count > 0)
                {
                    var perJobInfo = Skill.PerJobAttackInfo.Values.ToList()[Skill.PerJobIndex];
                    foreach (var i in perJobInfo.Keys)
                    {
                        skillCommon[i] = perJobInfo[i];
                    }
                }
                string hdesc = SummaryParser.GetSkillSummary(sr.Desc, Skill.Level, skillCommon, SummaryParams.Default);
                if (Skill.IsRoguelikeSkill)
                {
                    hdesc = hdesc.Replace("<style color=\"Orange\">", "#c").Replace("</>", "#");
                }
                if (Skill.IsGuildCastleResearch)
                {
                    StringBuilder gcrSb = new StringBuilder();
                    gcrSb.AppendLine(hdesc);
                    List<string> OrRequirements = new List<string>();
                    if (Skill.GuildCastleResearchRequirements.Count > 0)
                    {
                        gcrSb.AppendLine();
                        gcrSb.AppendLine(StringLinker.StringGuildCastleResearchTooltip["requirementTitle"].Desc);
                        foreach (var i in Skill.GuildCastleResearchRequirements)
                        {
                            StringResultSkill subSr = new StringResultSkill();
                            switch (Skill.GuildCastleResearchType)
                            {
                                case 0:
                                    if (StringLinker == null || !(StringLinker.StringGuildCastleGuildResearch.TryGetValue(i.Key, out StringResult _sr2) && _sr2 is StringResultSkill sr2))
                                    {
                                        sr2 = new StringResultSkill();
                                        sr2.Name = "(null)";
                                    }
                                    subSr = sr2;
                                    break;
                                case 1:
                                    if (StringLinker == null || !(StringLinker.StringGuildCastlePersonalResearch.TryGetValue(i.Key, out _sr2) && _sr2 is StringResultSkill sr3))
                                    {
                                        sr3 = new StringResultSkill();
                                        sr3.Name = "(null)";
                                    }
                                    subSr = sr3;
                                    break;
                            }
                            if (Skill.GuildCastleResearchReqCondition != "OR")
                            {
                                gcrSb.AppendLine(StringLinker.StringGuildCastleResearchTooltip["requirementElem"].Desc.Replace("#requirementElem", subSr.Name).Replace("#level", i.Value.ToString()));
                            }
                            else
                            {
                                OrRequirements.Add(StringLinker.StringGuildCastleResearchTooltip["requirementElem"].Desc.Replace("#requirementElem", subSr.Name).Replace("#level", i.Value.ToString()));
                            }
                        }
                        if (OrRequirements.Count > 0)
                        {
                            gcrSb.AppendLine(string.Join(" or\r\n", OrRequirements));
                        }
                    }
                    hdesc = gcrSb.ToString();
                }
                if (isTranslateRequired)
                {
                    string mergedDescString = Translator.MergeString(hdesc, Translator.TranslateString(hdesc), 2);
                    //mergedDescString += "\r\n" + hdescAppend.ToString();
                    GearGraphics.DrawString(g, mergedDescString, GearGraphics.ItemDetailFont, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
                }
                else
                {
                    //hdesc += "\r\n" + hdescAppend.ToString();
                    GearGraphics.DrawString(g, hdesc, GearGraphics.ItemDetailFont, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
                }
            }
            if (Skill.TimeLimited)
            {
                DateTime time = DateTime.Now.AddDays(7d);
                string expireStr = "Expiration Date: " + time.ToString("M\\/d\\/yyyy HH:mm");
                //GearGraphics.DrawString(g, "#c" + expireStr + "#", GearGraphics.ItemDetailFont2, 86, 485, ref picH, 26);//original values, 92, 274, 16. '400' is GMS sync
                GearGraphics.DrawString(g, "#c" + expireStr + "#", GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.RelationSkill != null)
            {
                StringResult sr2 = null;
                if (StringLinker == null || !StringLinker.StringSkill.TryGetValue(Skill.RelationSkill.Item1, out sr2))
                {
                    sr2 = new StringResultSkill();
                    sr2.Name = "(null)";
                }
                DateTime time = DateTime.Now.AddMinutes(Skill.RelationSkill.Item2);
                string expireStr = " Expiration Date: " + time.ToString(@"M\/d\/yyyy HH\:mm") + " UTC"; ;//Change when Permanent Thunder Horse is given to players.
                //GearGraphics.DrawString(g, "#c" + sr2.Name + expireStr + "#", GearGraphics.ItemDetailFont2, Skill.Icon.Bitmap == null ? 10 : 86, 485, ref picH, 16);
                GearGraphics.DrawString(g, "#c" + sr2.Name + expireStr + "#", GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.IsSequenceOn)
            {
                string colortag = "#c";
                if (doHighlight && DiffSkillTags.ContainsKey(skillIDstr) && DiffSkillTags[skillIDstr].Contains("isSequenceOn"))
                {
                    colortag = "#$g";
                }
                GearGraphics.DrawString(g, colortag + "Can add Skill Sequence#", GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.IsPetAutoBuff)
            {
                string colortag = "#c";
                if (doHighlight && DiffSkillTags.ContainsKey(skillIDstr) && DiffSkillTags[skillIDstr].Contains("isPetAutoBuff"))
                {
                    colortag = "#$g";
                }
                GearGraphics.DrawString(g, colortag + "Can add Auto Buff Skill#", GearGraphics.ItemDetailFont2, v6SkillSummaryFontColorTable, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if ((Skill.SkillID / 10000 / 1000 == 10 || Skill.SkillID / 10000 / 1000 == 11) && Skill.ReqLevel > 0)
            {
                GearGraphics.DrawString(g, "#c(Level Required: " + Skill.ReqLevel.ToString() + " or above)#", GearGraphics.ItemDetailFont2, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.ReqAmount > 0)
            {
                GearGraphics.DrawString(g, "#c" + ItemStringHelper.GetSkillReqAmount(Skill.SkillID, Skill.ReqAmount) + "#", GearGraphics.ItemDetailFont2, Skill.Icon.Bitmap == null ? region.LevelDescLeft : region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            picH += 13;
            /*if (Skill.ReqLevel > 0)
            {
                GearGraphics.DrawString(g, "#c[要求等级：" + Skill.ReqLevel.ToString() + "]#", GearGraphics.ItemDetailFont, region.SkillDescLeft, region.TextRight, ref picH, 16);
            }
            if (Skill.ReqAmount > 0)
            {
                GearGraphics.DrawString(g, "#c" + ItemStringHelper.GetSkillReqAmount(Skill.SkillID, Skill.ReqAmount) + "#", GearGraphics.ItemDetailFont, region.SkillDescLeft, region.TextRight, ref picH, 16);
            }*/
            picH += 13;

            //delay rendering v6 splitter
            picH = Math.Max(picH, 114);
            splitterH.Add(picH);
            picH += this.Enable22AniStyle ? 16 : 15;

            var skillSummaryOptions = new SkillSummaryOptions
            {
                ConvertCooltimeMS = this.DisplayCooltimeMSAsSec,
                ConvertPerM = this.DisplayPermyriadAsPercent,
                IgnoreEvalError = this.IgnoreEvalError,
                EndColorOnNewLine = true,
            };

            if (Skill.Level > 0)
            {

                // 스킬 변경점에 초록색 칠하기
                if (doHighlight)
                {

                    if (Skill.SkillID / 100000 == 4000)
                    {
                        if (Skill.VSkillValue == 2) Skill.Level = 60;
                        if (Skill.VSkillValue == 1) Skill.Level = 30;
                    }
                }
                string hStr = SummaryParser.GetSkillSummary(Skill, Skill.Level, sr, SummaryParams.Default, skillSummaryOptions, doHighlight, skillIDstr, this.DiffSkillTags);

                if (Skill.IsGuildCastleResearch && Skill.VariableProps.Contains("ResearchTimeCost"))
                {
                    string extraHstr = StringLinker.StringGuildCastleResearchTooltip["ResearchTimeCost"].Desc.Replace("#ResearchTimeCost", $"#ResearchTimeCost_{Skill.Level}");
                    hStr += "\r\n";
                    hStr += SummaryParser.GetSkillSummary(extraHstr, Skill.Level, Skill.Common, SummaryParams.Default, skillSummaryOptions);
                }

                GearGraphics.DrawString(g, "[Current Level " + Skill.Level + "]", GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
                if (Skill.SkillID / 10000 / 1000 == 10 && Skill.Level == 1 && Skill.ReqLevel > 0)
                {
                    //GearGraphics.DrawPlainText(g, "(Level Required: " + Skill.ReqLevel.ToString() + " or above)", GearGraphics.ItemDetailFont2, GearGraphics.skillYellowColor, 10, 485, ref picH, 16); *Related to Zero skills
                    //GearGraphics.DrawPlainText(g, "[필요 레벨: " + Skill.ReqLevel.ToString() + "레벨 이상]", GearGraphics.ItemDetailFont2, GearGraphics.skillYellowColor, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
                if (hStr != null)
                {
                    //GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont2, region.LevelDescLeft, region.TextRight, ref picH, 16);
                    ParsedHdesc = hStr;
                    if (isTranslateRequired)
                    {
                        string mergedhStr = Translator.MergeString(hStr, Translator.TranslateString(hStr), 2);
                        GearGraphics.DrawString(g, mergedhStr, GearGraphics.ItemDetailFont, v6SkillSummaryFontColorTable, region.LevelDescLeft, region.TextRight, ref picH, 16);
                    }
                    else
                    {
                        GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont, v6SkillSummaryFontColorTable, region.LevelDescLeft, region.TextRight, ref picH, 16);
                    }
                }
            }

            if (Skill.Level < Skill.MaxLevel && !Skill.DisableNextLevelInfo)
            {
                string hStr = SummaryParser.GetSkillSummary(Skill, Skill.Level + 1, sr, SummaryParams.Default, new SkillSummaryOptions
                {
                    ConvertCooltimeMS = this.DisplayCooltimeMSAsSec,
                    ConvertPerM = this.DisplayPermyriadAsPercent,
                    IgnoreEvalError = this.IgnoreEvalError,
                });
                if (Skill.IsGuildCastleResearch && Skill.VariableProps.Contains("ResearchTimeCost"))
                {
                    string extraHstr = StringLinker.StringGuildCastleResearchTooltip["ResearchTimeCost"].Desc.Replace("#ResearchTimeCost", $"#ResearchTimeCost_{Skill.Level + 1}");
                    hStr += "\r\n";
                    hStr += SummaryParser.GetSkillSummary(extraHstr, Skill.Level + 1, Skill.Common, SummaryParams.Default, skillSummaryOptions);
                }
                GearGraphics.DrawString(g, "[Next Level " + (Skill.Level + 1) + "]", GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
                if (Skill.SkillID / 10000 / 1000 == 10 && (Skill.Level + 1) == 1 && Skill.ReqLevel > 0)
                {
                    GearGraphics.DrawPlainText(g, "[Level Required: " + Skill.ReqLevel.ToString() + " or above]", GearGraphics.ItemDetailFont2, GearGraphics.skillYellowColor, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
                if (hStr != null)
                {
                    if (isTranslateRequired)
                    {
                        string mergedhStr = Translator.MergeString(hStr, Translator.TranslateString(hStr), 2);
                        GearGraphics.DrawString(g, mergedhStr, GearGraphics.ItemDetailFont, v6SkillSummaryFontColorTable, region.LevelDescLeft, region.TextRight, ref picH, 16);
                    }
                    else
                    {

                        GearGraphics.DrawString(g, hStr, GearGraphics.ItemDetailFont, v6SkillSummaryFontColorTable, region.LevelDescLeft, region.TextRight, ref picH, 16);
                    }
                }
            }
            picH += 3;

            if (Skill.AddAttackToolTipDescSkill != 0)
            {
                //delay rendering v6 splitter
                splitterH.Add(picH);
                picH += 15;
                GearGraphics.DrawPlainText(g, "[Combo Skill]", GearGraphics.ItemDetailFont, Color.FromArgb(119, 204, 255), region.LevelDescLeft, region.TextRight, ref picH, 16);
                picH += 4;
                BitmapOrigin icon = new BitmapOrigin();
                Wz_Node skillNode = PluginBase.PluginManager.FindWz(string.Format(@"Skill\{0}.img\skill\{1}", Skill.AddAttackToolTipDescSkill / 10000, Skill.AddAttackToolTipDescSkill));
                if (skillNode != null)
                {
                    Skill skill = Skill.CreateFromNode(skillNode, PluginBase.PluginManager.FindWz, PluginBase.PluginManager.FindWz);
                    icon = skill.Icon;
                }
                if (icon.Bitmap != null)
                {
                    g.DrawImage(icon.Bitmap, 13 - icon.Origin.X, picH + 32 - icon.Origin.Y);
                }
                string skillName;
                if ((this.StringLinker != null && this.StringLinker.StringSkill.TryGetValue(Skill.AddAttackToolTipDescSkill, out var _sr2)) && _sr2 is StringResultSkill sr2)
                {
                    skillName = sr2.Name;
                }
                else
                {
                    skillName = Skill.AddAttackToolTipDescSkill.ToString();
                }
                picH += 10;
                GearGraphics.DrawString(g, skillName, GearGraphics.ItemDetailFont, region.LinkedSkillNameLeft, region.TextRight, ref picH, 16);
                picH += 6;
                picH += 13;
            }

            if (Skill.AssistSkillLink != 0)
            {
                //delay rendering v6 splitter
                splitterH.Add(picH);
                picH += 15;
                GearGraphics.DrawPlainText(g, "[Assist Skill]", GearGraphics.ItemDetailFont, GearGraphics.SkillSummaryOrangeTextColor, region.LevelDescLeft, region.TextRight, ref picH, 16);
                picH += 4;
                BitmapOrigin icon = new BitmapOrigin();
                Wz_Node skillNode = PluginBase.PluginManager.FindWz(string.Format(@"Skill\{0}.img\skill\{1}", Skill.AssistSkillLink / 10000, Skill.AssistSkillLink));
                if (skillNode != null)
                {
                    Skill skill = Skill.CreateFromNode(skillNode, PluginBase.PluginManager.FindWz, PluginBase.PluginManager.FindWz);
                    icon = skill.Icon;
                }
                if (icon.Bitmap != null)
                {
                    g.DrawImage(icon.Bitmap, 13 - icon.Origin.X, picH + 32 - icon.Origin.Y);
                }
                string skillName;
                if ((this.StringLinker != null && this.StringLinker.StringSkill.TryGetValue(Skill.AssistSkillLink, out _sr)) && _sr is StringResultSkill sr2)
                {
                    skillName = sr2.Name;
                }
                else
                {
                    skillName = Skill.AssistSkillLink.ToString();
                }
                picH += 10;
                GearGraphics.DrawString(g, skillName, GearGraphics.ItemDetailFont, region.LinkedSkillNameLeft, region.TextRight, ref picH, 16);
                picH += 6;
                picH += 13;
            }

            List<string> skillDescEx = new List<string>();
            if (ShowProperties)
            {
                List<string> attr = new List<string>();
                if (Skill.ReqLevel > 0)
                 {
                    attr.Add("[Lv. " + Skill.ReqLevel + " required]");
                }
                if (Skill.IsRoguelikeSkill)
                {
                    attr.Add("Roguelike Skill: " + (Skill.IsRedmoon ? "Red Moon Forest" : "Pharaoh's Treasure"));
                }
                if (Skill.IsGuildCastleResearch)
                {
                    switch (Skill.GuildCastleResearchType)
                    {
                        case 0: attr.Add("Guild Castle Research: Common Research"); break;
                        case 1: attr.Add("Guild Castle Research: Personal Research"); break;
                    }
                }
                if (Skill.Invisible)
                {
                    attr.Add("[Hidden Skill]");
                }
                if (Skill.Hyper != HyperSkillType.None)
                {
                    attr.Add("[Hyper Skill: " + Skill.Hyper + "]");
                }
                if (Skill.CombatOrders)
                {
                    if (doHighlight && DiffSkillTags.ContainsKey(skillIDstr) && DiffSkillTags[skillIDstr].Contains("combatOrders"))
                    {
                        attr.Add("#g[Compatible with Combat Orders]#");
                    }
                    else
                    {
                        attr.Add("[Compatible with Combat Orders]");
                    }
                }
                if (Skill.NotRemoved)
                {
                    attr.Add("[Undispellable]");
                }
                if (Skill.MasterLevel > 0 && Skill.MasterLevel < Skill.MaxLevel)
                {
                    attr.Add("[Mastery Book required to upgrade beyond Lv. " + Skill.MasterLevel + "]");
                }
                if (Skill.NotIncBuffDuration)
                {
                    attr.Add("[Unaffected by buff duration increases]");
                }
                if (Skill.NotCooltimeReset)
                {
                    attr.Add("[Unaffected by cooldown reset effects]");
                }
                if (Skill.NotCooltimeReduce)
                {
                    attr.Add("[Unaffected by cooldown reduction effects]");
                }

                if (attr.Count > 0)
                {
                    skillDescEx.Add("#c" + string.Join("\n", attr.ToArray()) + "#");
                }
            }

            if (ShowDelay && Skill.Action.Count > 0)
            {
                foreach (string action in Skill.Action)
                {
                    string colortag = "";
                    if (doHighlight && DiffSkillTags[Skill.SkillID.ToString()].Contains(action))
                    {
                        colortag = "#$g";
                    }
                    skillDescEx.Add("#c[Delay] " + colortag + action + ": " + CharaSimLoader.GetActionDelay(action, this.wzNode) + " ms#");
                }
            }

            if (ShowArea && Skill.Lt.Count > 0)
            {
                foreach (var kv in Skill.Lt)
                {
                    if (!Skill.Rb.ContainsKey(kv.Key))
                    {
                        continue;
                    }
                    string colortag = "";
                    if (doHighlight && DiffSkillTags.ContainsKey(skillIDstr) && (DiffSkillTags[skillIDstr].Contains("lt" + kv.Key) || DiffSkillTags[skillIDstr].Contains("rb" + kv.Key)))
                    {
                        colortag = "#$g";
                    }
                    skillDescEx.Add("#c[Range" + kv.Key + "(px)] " + colortag + "Left: " + kv.Value.X + ", Right: " + Skill.Rb[kv.Key].X + ", Top: " + kv.Value.Y + ", Bottom: " + Skill.Rb[kv.Key].Y + "" +
                        ", Area: " + Math.Abs(Skill.Rb[kv.Key].X - kv.Value.X) + " x " + Math.Abs(kv.Value.Y - Skill.Rb[kv.Key].Y) + "#");
                }
            }

            if (ShowReqSkill && Skill.ReqSkill.Count > 0)
            {
                foreach (var kv in Skill.ReqSkill)
                {
                    string skillName;
                    if ((this.StringLinker != null && this.StringLinker.StringSkill.TryGetValue(kv.Key, out var _sr2)) && _sr2 is StringResultSkill sr2)
                    {
                        skillName = sr2.Name;
                    }
                    else
                    {
                        skillName = kv.Key.ToString();
                    }
                    skillDescEx.Add("#c[Lv. " + kv.Value + " " + skillName + " required]#");
                }
            }

            if (Skill.PerJobAttackInfo.Count > 0)
            {
                int jobID = Skill.PerJobAttackInfo.Keys.ToList()[Skill.PerJobIndex];
                skillDescEx.Add($"#c[Applicable Job] {ItemStringHelper.GetJobName(Skill.SkillID / 100000000 == 5 ? jobID + 2 : jobID) ?? ItemStringHelper.GetJobName(Skill.SkillID / 100000000 == 5 ? jobID + 3 : jobID) ?? ItemStringHelper.GetJobName(jobID)}({jobID})#");
            }

            if (Skill.LT.X != 0)
            {

                skillDescEx.Add("#c[Range Coordinates] Left Top: (" + Skill.LT.X + "," + Skill.LT.Y + ")" + " / " +
                                            "Right Bottom: (" + Skill.RB.X + "," + Skill.RB.Y + ")");
                int LT = Math.Abs(Skill.LT.X) + Skill.RB.X;
                int RB = Math.Abs(Skill.LT.Y) + Skill.RB.Y;
                skillDescEx.Add("#c[Range] " + LT + " x " + RB);

            }

            if (skillDescEx.Count > 0)
            {
                //delay rendering v6 splitter
                splitterH.Add(picH);
                picH += 9;
                foreach (var descEx in skillDescEx)
                {
                    GearGraphics.DrawString(g, descEx, GearGraphics.ItemDetailFont, region.LevelDescLeft, region.TextRight, ref picH, 16);
                }
                picH += 3;
            }

            picH += 6;

            format.Dispose();
            g.Dispose();
            return bitmap;
        }

        private void DrawV6SkillDotline(Graphics g, int x1, int x2, int y)
        {
            // here's a trick that we won't draw left and right part because it looks the same as background border.
            var picCenter = Enable22AniStyle ? Resource.UIToolTipNew_img_Skill_Frame_dotline_c : Resource.UIToolTip_img_Skill_Frame_dotline_c;
            using (var brush = new TextureBrush(picCenter))
            {
                brush.TranslateTransform(x1, y);
                g.FillRectangle(brush, new Rectangle(x1, y, x2 - x1, picCenter.Height));
            }
        }

        private Bitmap RenderLinkRidingGear(Gear gear)
        {
            TooltipRender renderer = this.LinkRidingGearRender;
            if (renderer == null)
            {
                if (this.Enable22AniStyle)
                {
                    GearTooltipRender22 defaultRenderer = new GearTooltipRender22();
                    defaultRenderer.StringLinker = this.StringLinker;
                    defaultRenderer.ShowObjectID = false;
                    renderer = defaultRenderer;
                }
                else
                {
                    GearTooltipRender2 defaultRenderer = new GearTooltipRender2();
                    defaultRenderer.StringLinker = this.StringLinker;
                    defaultRenderer.ShowObjectID = false;
                    renderer = defaultRenderer;
                }
            }

            renderer.TargetItem = gear;
            return renderer.Render();
        }



        private Bitmap RenderHexaDesc(CanvasRegion region)
        {
            Bitmap bitmap = new Bitmap(1, 1);
            if (Skill.Origin)
            {
                bitmap = new Bitmap(430, 125);
            }
            else if (Skill.Ascent)
            {
                bitmap = new Bitmap(430, 360);
            }
            Graphics g = Graphics.FromImage(bitmap);
            int picH = 13;
            if (Skill.Origin && !Skill.Invisible)
            {
                string originSkillDesc = "The Origin Skill absolutely stuns all enemies (including enemies immune to stun).\r\nThe resistance duration of the absolute stun is not shared with other stun statuses.";
                string originSkillH = "When the attack lands, the enemy is completely stunned for 10 sec.";
                GearGraphics.DrawNewTooltipBack(g, 0, 0, bitmap.Width, 125);
                GearGraphics.DrawPlainText(g, originSkillDesc, GearGraphics.ItemDetailFont, Color.FromArgb(175, 173, 255), region.LevelDescLeft, region.TextRight, ref picH, 16);
                picH += 16;
                DrawV6SkillDotline(g, region.SplitterX1, region.SplitterX2, picH);
                picH += 10;
                GearGraphics.DrawPlainText(g, originSkillH, GearGraphics.ItemDetailFont, Color.FromArgb(175, 173, 255), region.LevelDescLeft, region.TextRight, ref picH, 16);
            }
            else if (Skill.Ascent && !Skill.Invisible)
            {
                string ascentSkillDesc = "Ascent skills can be used without a cooldown a set number of times during boss battles. However, they can only be used when a boss enemy with the highest max HP is present.\nHitting an enemy with an Ascent skill will not activate additional attacks or combat effects.\nAscent skills target bosses with the highest max HP first, and are unaffected by attack ignore and attack reflection effects.\nThe Ignore Defense and Boss Damage Increase effects from the Lv. 10, Lv. 20, and Lv. 30 Ascent skills are added to increases from basic effects.\n\nThe damage of Ascent skills is unaffected by stat changes from the following sources:\n- Equipment: Hat\n- Equipment: Ring\n- Conditional passive skill effects\n- Effects from active skills\n- Enemy attributes\n- Enemy attack patterns and debuffs\n- Usable and Cash items with less than 30 min. duration";
                string ascentSkillH = "Can be used in a boss battle 3 times\nHas a cooldown of 240 sec. when used in other areas";
                GearGraphics.DrawNewTooltipBack(g, 0, 0, bitmap.Width, 360);
                GearGraphics.DrawPlainText(g, ascentSkillDesc, GearGraphics.ItemDetailFont, Color.FromArgb(175, 173, 255), region.LevelDescLeft, region.TextRight, ref picH, 16);
                picH += 16;
                DrawV6SkillDotline(g, region.SplitterX1, region.SplitterX2, picH);
                picH += 16;
                GearGraphics.DrawPlainText(g, ascentSkillH, GearGraphics.ItemDetailFont, Color.FromArgb(175, 173, 255), region.LevelDescLeft, region.TextRight, ref picH, 16);
            }
            g.Dispose();
            return bitmap;
        }

        private class CanvasRegion
        {
            public int Width { get; private set; }
            public int TitleCenterX { get; private set; }
            public int SplitterX1 { get; private set; }
            public int SplitterX2 { get; private set; }
            public int SkillDescLeft { get; private set; }
            public int LinkedSkillNameLeft { get; private set; }
            public int LevelDescLeft { get; private set; }
            public int TextRight { get; private set; }

            public static CanvasRegion Original { get; } = new CanvasRegion()
            {
                Width = 290,
                TitleCenterX = 144,
                SplitterX1 = 4,
                SplitterX2 = 284,
                SkillDescLeft = 90,
                LinkedSkillNameLeft = 46,
                LevelDescLeft = 8,
                TextRight = 272,
            };

            public static CanvasRegion Wide { get; } = new CanvasRegion()
            {
                Width = 430,
                TitleCenterX = 253,
                SplitterX1 = 4,
                SplitterX2 = 424,
                SkillDescLeft = 88,
                LinkedSkillNameLeft = 46,
                LevelDescLeft = 11,
                TextRight = 411,
            };

            public static CanvasRegion _22AniOriginal { get; } = new CanvasRegion()
            {
                Width = 290,
                TitleCenterX = 144,
                SplitterX1 = 12,
                SplitterX2 = 276,
                SkillDescLeft = 90,
                LinkedSkillNameLeft = 46,
                LevelDescLeft = 8,
                TextRight = 272,
            };

            public static CanvasRegion _22AniWide { get; } = new CanvasRegion()
            {
                Width = 430,
                TitleCenterX = 215,
                SplitterX1 = 12,
                SplitterX2 = 416,
                SkillDescLeft = 92,
                LinkedSkillNameLeft = 49,
                LevelDescLeft = 13,
                TextRight = 411,
            };
        }
    }
}