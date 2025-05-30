﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.WzLib;
using WzComparerR2.AvatarCommon;
using static WzComparerR2.CharaSimControl.RenderHelper;

namespace WzComparerR2.CharaSimControl
{
    public class MobTooltipRenderer : TooltipRender
    {

        public MobTooltipRenderer()
        {
        }

        public override object TargetItem
        {
            get { return this.MobInfo; }
            set { this.MobInfo = value as Mob; }
        }

        public Mob MobInfo { get; set; }
        private AvatarCanvasManager avatar { get; set; }

        public override Bitmap Render()
        {
            if (MobInfo == null)
            {
                return null;
            }

            Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);
            bool isTranslateRequired = Translator.IsTranslateEnabled;

            //预绘制
            List<TextBlock> titleBlocks = new List<TextBlock>();

            if (MobInfo.ID > -1)
            {
                string mobName = GetMobName(MobInfo.ID);
                var block = PrepareText(g, mobName ?? "(null)", GearGraphics.ItemNameFont2, Brushes.White, 0, 0);
                titleBlocks.Add(block);
                block = PrepareText(g, "ID:" + MobInfo.ID, GearGraphics.ItemDetailFont, Brushes.White, block.Size.Width + 6, 0);
                titleBlocks.Add(block);
            }

            List<TextBlock> locBlocks = new List<TextBlock>();
            int picY = 0;

            List<TextBlock> propBlocks = new List<TextBlock>();
            picY = 0;

            StringBuilder sbExt = new StringBuilder();
            if (MobInfo.Boss && MobInfo.PartyBonusMob)
            {
                sbExt.Append("[Mini-Boss] ");
            }
            if (MobInfo.Boss && !MobInfo.PartyBonusMob)
            {
                sbExt.Append("[Boss] ");
            }
            if (MobInfo.Undead)
            {
                sbExt.Append("[Undead] ");
            }
            if (MobInfo.FirstAttack)
            {
                sbExt.Append("[Auto-Aggressive] ");
            }
            if (!MobInfo.BodyAttack)
            {
                sbExt.Append("[No Touch Damage] ");
            }
            if (MobInfo.DamagedByMob)
            {
                sbExt.Append("[Vulnerable to Monsters] ");
            }
            if (MobInfo.ChangeableMob)
            {
                sbExt.Append("[Level Scaled] ");
            }
            if (MobInfo.AllyMob)
            {
                sbExt.Append("[Friendly] ");
            }
            if (MobInfo.Invincible)
            {
                sbExt.Append("[Invincible] ");
            }
            if (MobInfo.NotAttack)
            {
                sbExt.Append("[Non-Aggressive] ");//Monster can not attack or damage you. But you can damage it.
            }
            if (MobInfo.FixedDamage > 0)
            {
                sbExt.Append("[Fixed Damage: " + MobInfo.FixedDamage.ToString("N0") + "] ");
            }
            if (MobInfo.FixedBodyAttackDamageR > 0)
            {
                sbExt.Append("[Fixed Touch Damage: " + MobInfo.FixedBodyAttackDamageR + "%] ");
            }
            if (MobInfo.IgnoreDamage)
            {
                sbExt.Append("[Ignores Damage] ");
            }
            if (MobInfo.IgnoreMoveImpact)
            {
                sbExt.Append("[Immune to Rush] ");
            }
            if (MobInfo.IgnoreMovable)
            {
                sbExt.Append("[Immune to Stun/Bind] ");
            }
            if (MobInfo.NoDebuff)
            {
                sbExt.Append("[Immune to Debuffs] ");
            }
            if (MobInfo.OnlyNormalAttack)
            {
                sbExt.Append("[Damaged by Basic Attacks only] ");
            }
            if (MobInfo.OnlyHittedByCommonAttack)
            {
                sbExt.Append("[Hit by Basic Attacks only] ");
            }

            if (sbExt.Length > 1)
            {
                sbExt.Remove(sbExt.Length - 1, 1);
                propBlocks.Add(PrepareText(g, sbExt.ToString(), GearGraphics.ItemDetailFont, Brushes.GreenYellow, 0, picY));
                picY += 16;
            }

            if (MobInfo.RemoveAfter > 0)
            {
                propBlocks.Add(PrepareText(g, "[Disappears after " + MobInfo.RemoveAfter + " seconds]", GearGraphics.ItemDetailFont, Brushes.GreenYellow, 0, picY));
                picY += 16;
            }

            propBlocks.Add(PrepareText(g, "Type: " + GetMobCategoryName(MobInfo.Category), GearGraphics.ItemDetailFont, Brushes.White, 0, picY));
            propBlocks.Add(PrepareText(g, "Level: " + MobInfo.Level, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            string hpNum = !string.IsNullOrEmpty(MobInfo.FinalMaxHP) ? this.AddCommaSeparators(MobInfo.FinalMaxHP) : MobInfo.MaxHP.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            propBlocks.Add(PrepareText(g, "HP: " + hpNum, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            string mpNum = !string.IsNullOrEmpty(MobInfo.FinalMaxMP) ? this.AddCommaSeparators(MobInfo.FinalMaxMP) : MobInfo.MaxMP.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            propBlocks.Add(PrepareText(g, "MP: " + mpNum, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            if (MobInfo.HPRecovery > 0)
            {
                propBlocks.Add(PrepareText(g, "HP Recovery: " + MobInfo.HPRecovery.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            }
            if (MobInfo.MPRecovery > 0)
            {
                propBlocks.Add(PrepareText(g, "MP Recovery: " + MobInfo.MPRecovery.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            }
            propBlocks.Add(PrepareText(g, "Physical Damage: " + MobInfo.PADamage.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "Magic Damage: " + MobInfo.MADamage.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            //propBlocks.Add(PrepareText(g, "Physical Defense: " + MobInfo.PDDamage.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            //propBlocks.Add(PrepareText(g, "Magic Defense: " + MobInfo.MDDamage.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "Physical DEF Rate: " + MobInfo.PDRate + "%", GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "Magic DEF Rate: " + MobInfo.MDRate + "%", GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            //propBlocks.Add(PrepareText(g, "Accuracy: " + MobInfo.Acc, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16)); //no longer used
            //propBlocks.Add(PrepareText(g, "Avoidability: " + MobInfo.Eva, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16)); //no longer used
            propBlocks.Add(PrepareText(g, "Knockback: " + MobInfo.Pushed.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "EXP: " + MobInfo.Exp.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            if (MobInfo.CharismaEXP > 0)
            {
                propBlocks.Add(PrepareText(g, "Ambition EXP: " + MobInfo.CharismaEXP, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            }
            if (MobInfo.SenseEXP > 0)
            {
                propBlocks.Add(PrepareText(g, "Empathy EXP: " + MobInfo.SenseEXP, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            }
            if (MobInfo.InsightEXP > 0)
            {
                propBlocks.Add(PrepareText(g, "Insight EXP: " + MobInfo.InsightEXP, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            }
            if (MobInfo.WillEXP > 0)
            {
                propBlocks.Add(PrepareText(g, "Willpower EXP: " + MobInfo.WillEXP, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            }
            if (MobInfo.CraftEXP > 0)
            {
                propBlocks.Add(PrepareText(g, "Diligence EXP: " + MobInfo.CraftEXP, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            }
            if (MobInfo.CharmEXP > 0)
            {
                propBlocks.Add(PrepareText(g, "Charm EXP: " + MobInfo.CharmEXP, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            }
            if (MobInfo.WP > 0)
            {
                propBlocks.Add(PrepareText(g, "Weapon Points (for Zero): " + MobInfo.WP, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            }
            //propBlocks.Add(PrepareText(g, GetElemAttrString(MobInfo.ElemAttr), GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            if (GetElemAttrString(MobInfo.ElemAttr) != "")
            {
                propBlocks.Add(PrepareText(g, "Elements: " + GetElemAttrString(MobInfo.ElemAttr), GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            }
            if (MobInfo?.ID != null)
            {
                var locNode = PluginBase.PluginManager.FindWz("Etc\\MobLocation.img\\" + MobInfo.ID.ToString());
                if (locNode != null)
                {
                    propBlocks.Add(PrepareText(g, "Location:", GearGraphics.ItemDetailFont, GearGraphics.LocationBrush, 0, picY += 30));
                    foreach (var locMapNode in locNode.Nodes)
                    {
                        int mapID = locMapNode.GetValueEx<int>(-1);
                        string mapName = null;
                        if (mapID >= 0)
                        {
                            mapName = GetMapName(mapID);
                        }
                        string mobLoc = string.Format("{0}({1})", mapName ?? "null", mapID);

                        propBlocks.Add(PrepareText(g, mobLoc, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
                    }
                }
            }

            picY += 28;

            if (MobInfo.Revive.Count > 0)
            {
                Dictionary<int, int> reviveCounts = new Dictionary<int, int>();
                foreach (var reviveID in MobInfo.Revive)
                {
                    int count = 0;
                    reviveCounts.TryGetValue(reviveID, out count);
                    reviveCounts[reviveID] = count + 1;
                }

                StringBuilder sb = new StringBuilder();
                //sb.Append("Summons after death: ");
                sb.Append("Revives into: ");
                int rowCount = 0;
                foreach (var kv in reviveCounts)
                {
                    if (rowCount++ > 0)
                    {
                        sb.AppendLine().Append("       ");
                    }
                    string mobName = GetMobName(kv.Key);
                    sb.AppendFormat("{0} ({1:D7})", mobName, kv.Key);
                    if (kv.Value > 1)
                    {
                        sb.Append(" * " + kv.Value);
                    }
                }

                propBlocks.Add(PrepareText(g, sb.ToString(), GearGraphics.ItemDetailFont, Brushes.GreenYellow, 0, picY));
            }
            g.Dispose();
            bmp.Dispose();

            //计算大小
            Rectangle titleRect = Measure(titleBlocks);
            Rectangle imgRect = Rectangle.Empty;
            Rectangle textRect = Measure(propBlocks);
            Rectangle locRect = Measure(locBlocks);
            Bitmap mobImg = MobInfo.Default.Bitmap;
            if (MobInfo.IsAvatarLook)
            {
                if (this.avatar == null)
                {
                    this.avatar = new AvatarCanvasManager();
                }

                foreach (var node in MobInfo.AvatarLook.Nodes)
                {
                    switch (node.Text)
                    {
                        case "skin":
                            var skin = node.GetValueEx<int>(0);
                            this.avatar.AddBodyFromSkin3(skin);
                            break;

                        case "ear":
                            var type = node.GetValueEx<int>(0);
                            this.avatar.SetEarType(type);
                            break;

                        default:
                            var gearID = node.GetValueEx<int>(0);
                            this.avatar.AddGear(gearID);
                            break;
                    }
                }

                var img = this.avatar.GetBitmapOrigin();
                if (img.Bitmap != null)
                {
                    MobInfo.Default = img;
                    mobImg = img.Bitmap;
                }

                this.avatar.ClearCanvas();
            }
            if (mobImg != null)
            {
                if (mobImg.Width > 250 || mobImg.Height > 300) //进行缩放
                {
                    double scale = Math.Min((double)250 / mobImg.Width, (double)300 / mobImg.Height);
                    imgRect = new Rectangle(0, 0, (int)(mobImg.Width * scale), (int)(mobImg.Height * scale));
                }
                else
                {
                    imgRect = new Rectangle(0, 0, mobImg.Width, mobImg.Height);
                }
            }


            //布局 
            //水平排列
            int width = 0;
            if (!imgRect.IsEmpty)
            {
                textRect.X = imgRect.Width + 4;
            }
            locRect.X = textRect.X + textRect.Width + 4;
            width = Math.Max(titleRect.Width, Math.Max(imgRect.Right, Math.Max(textRect.Right, locRect.Right)));
            titleRect.X = (width - titleRect.Width) / 2;

            //垂直居中
            int height = Math.Max(imgRect.Height, Math.Max(textRect.Height, locRect.Height));
            imgRect.Y = (height - imgRect.Height) / 2;
            textRect.Y = (height - textRect.Height) / 2;
            if (!titleRect.IsEmpty)
            {
                height += titleRect.Height + 4;
                imgRect.Y += titleRect.Bottom + 4;
                textRect.Y += titleRect.Bottom + 4;
            }
            locRect.Y = textRect.Y;

            //绘制
            bmp = new Bitmap(width + 20, height + 20);
            titleRect.Offset(10, 10);
            imgRect.Offset(10, 10);
            textRect.Offset(10, 10);
            locRect.Offset(10, 10);
            g = Graphics.FromImage(bmp);
            //绘制背景
            GearGraphics.DrawNewTooltipBack(g, 0, 0, bmp.Width, bmp.Height);
            //绘制标题
            foreach (var item in titleBlocks)
            {
                DrawText(g, item, titleRect.Location);
            }
            //绘制图像
            if (mobImg != null && !imgRect.IsEmpty)
            {
                g.DrawImage(mobImg, imgRect);
            }
            //绘制文本
            foreach (var item in propBlocks)
            {
                DrawText(g, item, textRect.Location);
            }
            foreach (var item in locBlocks)
            {
                DrawText(g, item, locRect.Location);
            }
            g.Dispose();
            return bmp;
        }

        private string GetMobName(int mobID)
        {
            bool isTranslateRequired = Translator.IsTranslateEnabled;
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringMob.TryGetValue(mobID, out sr))
            {
                return null;
            }
            if (isTranslateRequired)
            {
                return Translator.MergeString(sr.Name, Translator.TranslateString(sr.Name, true), 0, false, true);
            }
            else
            {
                return sr.Name;
            }
        }

        private string GetElemAttrString(MobElemAttr elemAttr)
        {
            StringBuilder sb1 = new StringBuilder();
            var elems = new[]
            {
                new {name = "Physical", attr = elemAttr.P },
                new {name = "Holy", attr = elemAttr.H },
                new {name = "Fire", attr = elemAttr.F },
                new {name = "Ice", attr = elemAttr.I },
                new {name = "Poison", attr = elemAttr.S },
                new {name = "Lightning", attr = elemAttr.L },
                new {name = "Dark", attr = elemAttr.D },
            };
            foreach (var item in elems)
            {
                if (item.attr != ElemResistance.Normal)
                {
                    sb1.Append($"{item.name} {GetElemAttrResistString(item.attr)}, ");
                }
            }
            return sb1.ToString().TrimEnd().TrimEnd(',');
        }

        public static string GetMobCategoryName(int category)
        {
            switch (category)
            {
                case 1: return "Mammal";
                case 2: return "Plant";
                case 3: return "Fish";
                case 4: return "Reptile";
                case 5: return "Spirit";
                case 6: return "Devil";
                case 7: return "Undead";
                case 8: return "Enchanted";
                default: return "None";
            }
        }

        private string GetElemAttrResistString(ElemResistance resist)
        {
            string e = null;
            switch (resist)
            {
                case ElemResistance.Immune: e = "immune"; break;
                case ElemResistance.Resist: e = "strong"; break;
                case ElemResistance.Normal: e = "neutral"; break;
                case ElemResistance.Weak: e = "weak"; break;
            }
            return e ?? "  ";
        }

        private string AddCommaSeparators(string number)
        {
            return Regex.Replace(number, @"^(\d+?)(\d{3})+$", m =>
            {
                var sb = new StringBuilder();
                sb.Append(m.Result("$1"));
                foreach (Capture cap in m.Groups[2].Captures)
                {
                    sb.Append(",");
                    sb.Append(cap.ToString());
                }
                return sb.ToString();
            });
        }

        private string GetMapName(int mapID)
        {
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringMap.TryGetValue(mapID, out sr))
            {
                return null;
            }
            return sr.Name;
        }
    }
}
