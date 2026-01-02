using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
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
        public int MaxWidth { get; set; }
        public bool ShowAllSubMobAtOnce { get; set; }
        public bool MseaMode {get; set;}
        public bool EnableWorldArchive { get; set; }
        public bool EnableMonsterBook { get; set; } = false; // Disable Monster Book Feature
        private AvatarCanvasManager avatar { get; set; }
        private WorldArchiveTooltipRender WorldArchiveRender { get; set; }

        public override Bitmap Render()
        {
            if (MobInfo == null)
            {
                return null;
            }

            Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            Bitmap subMobBmpTooltip = null;
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
            if (MobInfo.IsQuestCountGroupMob)
            {
                if (MobInfo.LvOptimum)
                {
                    sbExt.Append($"[Quest Mob Set: Within 20 levels above or below your character's Level] ");
                }
                else if (MobInfo.ChangeableMob)
                {
                    sbExt.Append($"[Quest Mob Set: Elite Monsters] ");
                }
                else if (MobInfo.Filters != 0)
                {
                    switch (MobInfo.Filters)
                    {
                        case 1: sbExt.Append($"[Quest Mob Set: Star Force Monster] "); break;
                        case 2: sbExt.Append($"[Quest Mob Set: Arcane River Monster] "); break;
                        case 5: sbExt.Append($"[Quest Mob Set: Star Force Elite Monster] "); break;
                        case -10: sbExt.Append(MseaMode ? $"[Quest Mob Set: Arcane Force/Authentic Force Region Monsters] ": $"[Quest Mob Set: Arcane Power/Sacred Power Region Monsters] "); break;
                        default: sbExt.Append($"[Quest Mob Set: Conditional] "); break;
                    }
                }
                else
                {
                    sbExt.Append($"[Quest Mob Set: {MobInfo.QuestCountGroupMobID.Count} Monsters] ");
                }
                if (sbExt.Length > 1)
                {
                    sbExt.Remove(sbExt.Length - 1, 1);
                    propBlocks.Add(PrepareText(g, sbExt.ToString(), GearGraphics.ItemDetailFont, Brushes.GreenYellow, 0, picY));
                    picY += 16;
                }
                if (!ShowAllSubMobAtOnce && !MobInfo.LvOptimum && !MobInfo.ChangeableMob && !(MobInfo.Filters != 0))
                {
                    propBlocks.Add(PrepareText(g, "Press [-] / [+] to switch Monsters.", GearGraphics.ItemDetailFont, Brushes.White, 0, picY));
                }
                Bitmap[] subMobBmps = new Bitmap[MobInfo.QuestCountGroupMobID.Count];
                MobTooltipRenderer subRenderer = new MobTooltipRenderer();
                subRenderer.StringLinker = this.StringLinker;
                subRenderer.ShowObjectID = this.ShowObjectID;
                for (int i = 0; i < MobInfo.QuestCountGroupMobID.Count; i++)
                {
                    try
                    {
                        Mob subMobInfo = Mob.CreateFromNode(PluginManager.FindWz(string.Format(@"Mob\{0:D7}.img", MobInfo.QuestCountGroupMobID[i]), this.SourceWzFile), PluginManager.FindWz);
                        subRenderer.MobInfo = subMobInfo;
                        subRenderer.EnableWorldArchive = false;
                        subMobBmps[i] = subRenderer.Render();
                    }
                    catch
                    {
                        continue;
                    }
                }
                if (ShowAllSubMobAtOnce)
                {
                    Size fullBitmapSize = new Size(0, 0);
                    int maxHeightPerLine = 0;
                    int currentItem = 0;
                    int currentWidth = 0;
                    foreach (Bitmap i in subMobBmps)
                    {
                        if (i != null)
                        {
                            currentWidth += i.Width;
                            maxHeightPerLine = Math.Max(maxHeightPerLine, i.Height);
                            if (currentWidth > this.MaxWidth)
                            {
                                currentWidth -= i.Width;
                                fullBitmapSize.Width = Math.Max(fullBitmapSize.Width, currentWidth);
                                fullBitmapSize.Height += maxHeightPerLine;
                                currentWidth = 0;
                                maxHeightPerLine = 0;
                            }
                            currentItem++;
                            if (currentItem == subMobBmps.Count())
                            {
                                fullBitmapSize.Width = Math.Max(fullBitmapSize.Width, currentWidth);
                                fullBitmapSize.Height += maxHeightPerLine;
                                maxHeightPerLine = 0;
                                currentItem = 0;
                            }
                        }
                    }
                    maxHeightPerLine = 0;
                    currentItem = 1;
                    subMobBmpTooltip = new Bitmap(fullBitmapSize.Width, fullBitmapSize.Height);
                    using (Graphics subG = Graphics.FromImage(subMobBmpTooltip))
                    {
                        int subH = 0;
                        int subW = 0;
                        foreach (Bitmap i in subMobBmps)
                        {
                            if (i != null)
                            {
                                if (subW + i.Width > this.MaxWidth)
                                {
                                    subH += maxHeightPerLine;
                                    subW = 0;
                                    maxHeightPerLine = 0;
                                }
                                using (Graphics subG2 = Graphics.FromImage(i))
                                {
                                    GearGraphics.DrawGearDetailNumber(subG2, 3, 3, currentItem.ToString(), true);
                                }
                                subG.DrawImage(i, subW, subH, new Rectangle(0, 0, i.Width, i.Height), GraphicsUnit.Pixel);
                                subW += i.Width;
                                maxHeightPerLine = Math.Max(maxHeightPerLine, i.Height);
                                currentItem++;
                            }
                        }
                    }
                }
                else if (subMobBmps.Count() > 0)
                {
                    if (subMobBmps[MobInfo.MobGroupIndex] != null)
                    {
                        using (Graphics subG = Graphics.FromImage(subMobBmps[MobInfo.MobGroupIndex]))
                        {
                            var labelFont = new Font("Arial", 12f, GraphicsUnit.Pixel);
                            int picH = 2;
                            GearGraphics.DrawPlainText(subG, $"{MobInfo.MobGroupIndex + 1} / {subMobBmps.Count()}", labelFont, Color.FromArgb(255, 255, 255), 2, 130, ref picH, 13);
                        }
                        subMobBmpTooltip = subMobBmps[MobInfo.MobGroupIndex];
                    }
                }
            }
            else
            {
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
            }
            g.Dispose();
            bmp.Dispose();

            //计算大小
            Rectangle titleRect = Measure(titleBlocks);
            Rectangle imgRect = Rectangle.Empty;
            Rectangle textRect = Measure(propBlocks);
            Rectangle locRect = Measure(locBlocks);
            Bitmap mobImg = MobInfo.Default.Bitmap;
            Bitmap mobIcon = GetMobIcon(MobInfo.ID);
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
                    if (MobInfo.Default.Bitmap != null)
                    {
                        MobInfo.Default.Bitmap.Dispose();
                    }
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
            if (mobIcon != null)
            {
                if (textRect.Y < titleRect.Y - (mobIcon.Height - titleRect.Height) / 2 + mobIcon.Height)
                {
                    int heightDelta = titleRect.Y - (mobIcon.Height - titleRect.Height) / 2 + mobIcon.Height - textRect.Y;
                    height += heightDelta + 4;
                    textRect.Y += heightDelta + 4;
                }
            }
            locRect.Y = textRect.Y;

            //绘制
            Bitmap baseBmp = new Bitmap(width + 20, height + 20);
            titleRect.Offset(10, 10);
            imgRect.Offset(10, 10);
            textRect.Offset(10, 10);
            using (g = Graphics.FromImage(baseBmp))
            {
                //绘制背景
                GearGraphics.DrawNewTooltipBack(g, 0, 0, baseBmp.Width, baseBmp.Height);
                //绘制标题
                foreach (var item in titleBlocks)
                {
                    DrawText(g, item, titleRect.Location);
                }
                //Attempt Draw Mob Icon
                if (mobIcon != null)
                {
                    g.DrawImage(mobIcon, titleRect.Location.X - mobIcon.Width - 4, titleRect.Y - (mobIcon.Height - titleRect.Height) / 2, new Rectangle(0, 0, mobIcon.Width, mobIcon.Height), GraphicsUnit.Pixel);
                }                //绘制图像
                if (mobImg != null && !imgRect.IsEmpty)
                {
                    g.DrawImage(mobImg, imgRect);
                }
                //绘制文本
                foreach (var item in propBlocks)
                {
                    DrawText(g, item, textRect.Location);
                }
            }
            string monsterBookDesc = EnableMonsterBook ? GetMobDesc(MobInfo.ID) : null;
            string worldArchiveDesc = EnableWorldArchive ? GetWorldArchiveDesc(MobInfo.ID) : null;
            if (!string.IsNullOrEmpty(worldArchiveDesc) || !string.IsNullOrEmpty(monsterBookDesc) && EnableWorldArchive)
            {
                WorldArchiveRender = new WorldArchiveTooltipRender();
                WorldArchiveRender.WorldArchiveMessage = worldArchiveDesc;
                WorldArchiveRender.MonsterBookMessage = monsterBookDesc;
                WorldArchiveRender.MobID = MobInfo.ID;
                Bitmap waBitmap = WorldArchiveRender.Render();
                Bitmap appendWaBitmap = new Bitmap(baseBmp.Width + waBitmap.Width, Math.Max(baseBmp.Height, waBitmap.Height));
                using (g = Graphics.FromImage(appendWaBitmap))
                {
                    g.DrawImage(baseBmp, 0, 0, new Rectangle(0, 0, baseBmp.Width, baseBmp.Height), GraphicsUnit.Pixel);
                    g.DrawImage(waBitmap, baseBmp.Width, 0, new Rectangle(0, 0, waBitmap.Width, waBitmap.Height), GraphicsUnit.Pixel);
                }
                baseBmp = appendWaBitmap;
            }
            if (subMobBmpTooltip != null)
            {
                bmp = new Bitmap(Math.Max(baseBmp.Width, subMobBmpTooltip.Width), baseBmp.Height + subMobBmpTooltip.Height);
                using (g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(baseBmp, 0, 0, new Rectangle(0, 0, baseBmp.Width, baseBmp.Height), GraphicsUnit.Pixel);
                    g.DrawImage(subMobBmpTooltip, 0, baseBmp.Height, new Rectangle(0, 0, subMobBmpTooltip.Width, subMobBmpTooltip.Height), GraphicsUnit.Pixel);
                }
            }
            else
            {
                bmp = baseBmp;
            }
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

        private string GetMobDesc(int mobID)
        {
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringMonsterBook.TryGetValue(mobID, out sr))
            {
                return null;
            }
            else
            {
                return sr.Desc;
            }
        }

        private Bitmap GetMobIcon(int mobID)
        {
            BitmapOrigin mobIconOrigin = BitmapOrigin.CreateFromNode(PluginManager.FindWz($@"UI\UIWindow2.img\MobGage\Mob\{mobID.ToString()}", this.SourceWzFile), PluginManager.FindWz);
            return mobIconOrigin.Bitmap;
        }

        private string GetWorldArchiveDesc(int mobID)
        {
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringWorldArchiveMob.TryGetValue(mobID, out sr))
            {
                return null;
            }
            return sr.Desc;
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
