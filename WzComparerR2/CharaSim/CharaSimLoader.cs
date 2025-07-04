﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;

namespace WzComparerR2.CharaSim
{
    public static class CharaSimLoader
    {
        static CharaSimLoader()
        {
            LoadedSetItems = new Dictionary<int, SetItem>();
            LoadedExclusiveEquips = new Dictionary<int, ExclusiveEquip>();
            LoadedCommoditiesBySN = new Dictionary<int, Commodity>();
            LoadedCommoditiesByItemId = new Dictionary<int, Commodity>();
        }

        public static Dictionary<int, SetItem> LoadedSetItems { get; private set; }
        public static Dictionary<int, ExclusiveEquip> LoadedExclusiveEquips { get; private set; }
        public static Dictionary<int, Commodity> LoadedCommoditiesBySN { get; private set; }
        public static Dictionary<int, Commodity> LoadedCommoditiesByItemId { get; private set; }

        public static void LoadSetItemsIfEmpty()
        {
            if (LoadedSetItems.Count == 0)
            {
                LoadSetItems();
            }
        }

        public static void LoadSetItems()
        {
            //搜索setItemInfo.img
            Wz_Node etcWz = PluginManager.FindWz(Wz_Type.Etc, true);
            if (etcWz == null)
                return;
            Wz_Node setItemNode = etcWz.FindNodeByPath("SetItemInfo.img", true);
            if (setItemNode == null)
                return;

            //搜索ItemOption.img
            Wz_Node itemWz = PluginManager.FindWz(Wz_Type.Item, true);
            if (itemWz == null)
                return;
            Wz_Node optionNode = itemWz.FindNodeByPath("ItemOption.img", true);
            if (optionNode == null)
                return;

            LoadedSetItems.Clear();
            foreach (Wz_Node node in setItemNode.Nodes)
            {
                int setItemIndex;
                if (Int32.TryParse(node.Text, out setItemIndex))
                {
                    SetItem setItem = SetItem.CreateFromNode(node, optionNode);
                    if (setItem != null)
                        LoadedSetItems[setItemIndex] = setItem;
                }
            }
        }

        public static SetItem LoadSetItem(int setID, Wz_File sourceWzFile)
        {
            //搜索setItemInfo.img
            Wz_Node etcWz = PluginManager.FindWz(Wz_Type.Etc, sourceWzFile);
            if (etcWz == null)
                return null;
            Wz_Node setItemNode = etcWz.FindNodeByPath("SetItemInfo.img", true);
            if (setItemNode == null)
                return null;

            //搜索ItemOption.img
            Wz_Node itemWz = PluginManager.FindWz(Wz_Type.Item, sourceWzFile);
            if (itemWz == null)
                return null;
            Wz_Node optionNode = itemWz.FindNodeByPath("ItemOption.img", true);
            if (optionNode == null)
                return null;

            foreach (Wz_Node node in setItemNode.Nodes)
            {
                int setItemIndex;
                if (Int32.TryParse(node.Text, out setItemIndex) && setItemIndex == setID)
                {
                    SetItem setItem = SetItem.CreateFromNode(node, optionNode);
                    if (setItem != null)
                        return setItem;
                    else
                        return null;
                }
            }
            return null;
        }

        public static void LoadExclusiveEquipsIfEmpty()
        {
            if (LoadedExclusiveEquips.Count == 0)
            {
                LoadExclusiveEquips();
            }
        }

        public static void LoadExclusiveEquips()
        {
            Wz_Node exclusiveNode = PluginManager.FindWz("Etc/ExclusiveEquip.img");
            if (exclusiveNode == null)
                return;

            LoadedExclusiveEquips.Clear();
            foreach (Wz_Node node in exclusiveNode.Nodes)
            {
                int exclusiveEquipIndex;
                if (Int32.TryParse(node.Text, out exclusiveEquipIndex))
                {
                    ExclusiveEquip exclusiveEquip = ExclusiveEquip.CreateFromNode(node);
                    if (exclusiveEquip != null)
                        LoadedExclusiveEquips[exclusiveEquipIndex] = exclusiveEquip;
                }
            }
        }

        public static void LoadCommoditiesIfEmpty()
        {
            if (LoadedCommoditiesBySN.Count == 0 && LoadedCommoditiesByItemId.Count == 0)
            {
                LoadCommodities();
            }
        }

        public static void LoadCommodities()
        {
            Wz_Node commodityNode = PluginManager.FindWz("Etc/Commodity.img");
            if (commodityNode == null)
                return;

            LoadedCommoditiesBySN.Clear();
            LoadedCommoditiesByItemId.Clear();
            foreach (Wz_Node node in commodityNode.Nodes)
            {
                int commodityIndex;
                if (Int32.TryParse(node.Text, out commodityIndex))
                {
                    Commodity commodity = Commodity.CreateFromNode(node);
                    if (commodity != null)
                    {
                        LoadedCommoditiesBySN[commodity.SN] = commodity;
                        if (commodity.ItemId / 10000 == 910)
                            LoadedCommoditiesByItemId[commodity.ItemId] = commodity;
                    }
                }
            }
        }

        public static void ClearAll()
        {
            LoadedSetItems.Clear();
            LoadedExclusiveEquips.Clear();
        }

        public static int GetActionDelay(string actionName, Wz_Node wzNode = null)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                return 0;
            }
            Wz_Node actionNode = wzNode == null ? PluginManager.FindWz("Character/00002000.img/" + actionName) :
                PluginManager.FindWz("Character/00002000.img/" + actionName, wzNode.GetNodeWzFile());
            if (actionNode == null)
            {
                return 0;
            }

            int delay = 0;
            foreach (Wz_Node frameNode in actionNode.Nodes)
            {
                Wz_Node delayNode = frameNode.Nodes["delay"];
                if (delayNode != null)
                {
                    delay += Math.Abs(delayNode.GetValue<int>());
                }
            }

            return delay;
        }
    }
}
