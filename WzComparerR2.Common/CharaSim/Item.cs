﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Item : ItemBase
    {
        public Item()
        {
            this.Props = new Dictionary<ItemPropType, long>();
            this.Specs = new Dictionary<ItemSpecType, long>();
            this.CoreSpecs = new Dictionary<ItemCoreSpecType, Wz_Node>();
            this.AddTooltips = new List<int>();
            this.Recipes = new List<int>();
        }

        public int Level { get; set; }
        public int? DamageSkinID { get; set; }
        public string ConsumableFrom { get; set; }
        public string EndUseDate { get; set; }
        public string SamplePath { get; set; }

        public List<GearLevelInfo> Levels { get; internal set; }

        public Dictionary<ItemPropType, long> Props { get; private set; }
        public Dictionary<ItemSpecType, long> Specs { get; private set; }
        public Dictionary<ItemCoreSpecType, Wz_Node> CoreSpecs { get; private set; }
        public List<int> AddTooltips { get; internal set; } // Additional Tooltips
        public List<int> Recipes { get; private set; }
        public Bitmap AvatarBitmap { get; set; }

        public bool Cash
        {
            get { return GetBooleanValue(ItemPropType.cash); }
        }

        public bool TimeLimited
        {
            get { return GetBooleanValue(ItemPropType.timeLimited); }
        }

        public bool ShowCosmetic
        {
            get { return this.Specs.TryGetValue(ItemSpecType.cosmetic, out long value) && value > 0; }
        }

        public bool GetBooleanValue(ItemPropType type)
        {
            return this.Props.TryGetValue(type, out long value) && value != 0;
        }

        public static Item CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode)
        {
            Item item = new Item();
            int value;
            if (node == null
                || !Int32.TryParse(node.Text, out value)
                && !((value = node.Text.IndexOf(".img")) > -1 && Int32.TryParse(node.Text.Substring(0, value), out value)))
            {
                return null;
            }
            item.ItemID = value;

            // in msn the node could be UOL.
            if (node.Value is Wz_Uol)
            {
                if ((node = node.ResolveUol()) == null)
                {
                    return item;
                }
            }

            Wz_Node infoNode = node.FindNodeByPath("info");
            if (infoNode != null)
            {
                foreach (Wz_Node subNode in infoNode.Nodes)
                {
                    switch (subNode.Text)
                    {
                        case "icon":
                            if (subNode.Value is Wz_Uol || subNode.Value is Wz_Png)
                            {
                                item.Icon = BitmapOrigin.CreateFromNode(subNode, findNode);
                            }
                            break;

                        case "iconRaw":
                            if (subNode.Value is Wz_Uol || subNode.Value is Wz_Png)
                            {
                                item.IconRaw = BitmapOrigin.CreateFromNode(subNode, findNode);
                            }
                            break;

                        case "sample":
                            if (subNode.Value is Wz_Uol || subNode.Value is Wz_Png)
                            {
                                item.Sample = BitmapOrigin.CreateFromNode(subNode, findNode);
                            }
                            break;

                        case "lv":
                            item.Level = Convert.ToInt32(subNode.Value);
                            break;

                        case "damageSkinID":
                            item.DamageSkinID = Convert.ToInt32(subNode.Value);
                            break;

                        case "consumableFrom":
                            item.ConsumableFrom = Convert.ToString(subNode.Value);
                            break;

                        case "endUseDate":
                            item.EndUseDate = Convert.ToString(subNode.Value);
                            break;

                        case "samplePath":
                            item.SamplePath = Convert.ToString(subNode.Value);
                            break;

                        case "exp":
                            foreach (Wz_Node subNode2 in subNode.Nodes)
                            {
                                ItemPropType type2;
                                if (Enum.TryParse("exp_" + subNode2.Text, out type2))
                                {
                                    try
                                    {
                                        item.Props.Add(type2, Convert.ToInt32(subNode2.Value));
                                    }
                                    finally
                                    {
                                    }
                                }
                            }
                            break;

                        case "level": //可升级信息
                            Wz_Node levelInfo = subNode.Nodes["info"];
                            item.Levels = new List<GearLevelInfo>();
                            if (levelInfo != null)
                            {
                                for (int i = 1; ; i++)
                                {
                                    Wz_Node levelInfoNode = levelInfo.Nodes[i.ToString()];
                                    if (levelInfoNode != null)
                                    {
                                        GearLevelInfo info = GearLevelInfo.CreateFromNode(levelInfoNode);
                                        int lv;
                                        Int32.TryParse(levelInfoNode.Text, out lv);
                                        info.Level = lv;
                                        item.Levels.Add(info);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }

                            Wz_Node levelCase = subNode.Nodes["case"];
                            if (levelCase != null)
                            {
                                int probTotal = 0;
                                foreach (Wz_Node caseNode in levelCase.Nodes)
                                {
                                    int prob = caseNode.Nodes["prob"].GetValueEx(0);
                                    probTotal += prob;
                                    for (int i = 0; i < item.Levels.Count; i++)
                                    {
                                        GearLevelInfo info = item.Levels[i];
                                        Wz_Node caseLevel = caseNode.Nodes[info.Level.ToString()];
                                        if (caseLevel != null)
                                        {
                                            //desc
                                            Wz_Node caseHS = caseLevel.Nodes["hs"];
                                            if (caseHS != null)
                                            {
                                                info.HS = caseHS.GetValue<string>();
                                            }

                                            //随机技能
                                            Wz_Node caseSkill = caseLevel.Nodes["Skill"];
                                            if (caseSkill != null)
                                            {
                                                foreach (Wz_Node skillNode in caseSkill.Nodes)
                                                {
                                                    int id = skillNode.Nodes["id"].GetValueEx(-1);
                                                    int level = skillNode.Nodes["level"].GetValueEx(-1);
                                                    if (id >= 0 && level >= 0)
                                                    {
                                                        info.Skills[id] = level;
                                                    }
                                                }
                                            }

                                            //装备技能
                                            Wz_Node equipSkill = caseLevel.Nodes["EquipmentSkill"];
                                            if (equipSkill != null)
                                            {
                                                foreach (Wz_Node skillNode in equipSkill.Nodes)
                                                {
                                                    int id = skillNode.Nodes["id"].GetValueEx(-1);
                                                    int level = skillNode.Nodes["level"].GetValueEx(-1);
                                                    if (id >= 0 && level >= 0)
                                                    {
                                                        info.EquipmentSkills[id] = level;
                                                    }
                                                }
                                            }
                                            info.Prob = prob;
                                        }
                                    }
                                }

                                foreach (var info in item.Levels)
                                {
                                    info.ProbTotal = probTotal;
                                }
                            }
                            item.Props.Add(ItemPropType.level, 1);
                            break;

                        case "addTooltip":
                            if (subNode.Nodes.Count > 0)
                            {
                                foreach (Wz_Node tooltipNode in subNode.Nodes)
                                {
                                    item.AddTooltips.Add(Convert.ToInt32(tooltipNode.Value));
                                }
                            }
                            else
                            {
                                item.AddTooltips.Add(Convert.ToInt32(subNode.Value));
                            }
                            break;

                        default:
                            ItemPropType type;
                            if (Enum.TryParse(subNode.Text, out type))
                            {
                                try
                                {
                                    item.Props.Add(type, Convert.ToInt64(subNode.Value));
                                }
                                catch (Exception)
                                {
                                }
                            }
                            break;
                    }
                }
            }

            Wz_Node specNode = node.FindNodeByPath("spec");
            if (specNode != null)
            {
                foreach (Wz_Node subNode in specNode.Nodes)
                {
                    if (subNode.Text == "recipe")
                    {
                        if (subNode.Value == null && subNode.Nodes.Count > 0)
                        {
                            foreach (var recipeNode in subNode.Nodes)
                            {
                                item.Recipes.Add(recipeNode.GetValue<int>());
                            }
                        }
                        else
                        {
                            item.Recipes.Add(subNode.GetValue<int>());
                        }
                    }
                    else if(Enum.TryParse(subNode.Text, out ItemSpecType type))
                    {
                        try
                        {
                            item.Specs.Add(type, Convert.ToInt64(subNode.Value));
                        }
                        finally
                        {
                        }
                    }
                }
            }

            Wz_Node coreSpecNode = node.FindNodeByPath("corespec");
            if (coreSpecNode != null)
            {
                item.Props.Remove(ItemPropType.tradeBlock);
                foreach (Wz_Node subNode in coreSpecNode.Nodes)
                {
                    ItemCoreSpecType type;
                    if (Enum.TryParse(subNode.Text, out type))
                    {
                        item.CoreSpecs.Add(type, subNode);
                    }
                }
                if (item.CoreSpecs.ContainsKey(ItemCoreSpecType.Ctrl_addMission))
                {
                    List<ItemCoreSpecType> removeSpecs = item.CoreSpecs.Keys.Where(k => k != ItemCoreSpecType.Ctrl_addMission).ToList();
                    foreach (ItemCoreSpecType type in removeSpecs)
                    {
                        item.CoreSpecs.Remove(type);
                    }
                }
            }
            return item;
        }

    }
}
