using System;
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
            LoadedCommoditiesByItemIdInteractive = new Dictionary<int, Dictionary<int, int>>();
            LoadedCommoditiesByItemIdHeroic = new Dictionary<int, Dictionary<int, int>>();
            LoadedMintableNFTItems = new List<int>();
            LoadedMintableSBTItems = new List<int>();
            LoadedMintableFTItems = new List<int>();
            LoadedPetEquipInfo = new Dictionary<int, List<int>>();
        }

        public static Dictionary<int, SetItem> LoadedSetItems { get; private set; }
        public static Dictionary<int, ExclusiveEquip> LoadedExclusiveEquips { get; private set; }
        public static Dictionary<int, Commodity> LoadedCommoditiesBySN { get; private set; }
        public static Dictionary<int, Commodity> LoadedCommoditiesByItemId { get; private set; }
        public static Dictionary<int, Dictionary<int, int>> LoadedCommoditiesByItemIdInteractive { get; private set; }
        public static Dictionary<int, Dictionary<int, int>> LoadedCommoditiesByItemIdHeroic { get; private set; }
        public static List<int> LoadedMintableNFTItems { get; private set; }
        public static List<int> LoadedMintableSBTItems { get; private set; }
        public static List<int> LoadedMintableFTItems { get; private set; }
        public static Dictionary<int, List<int>> LoadedPetEquipInfo { get; private set; }

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

        public static void LoadMsnMintableItemListIfEmpty(Wz_File sourceWzFile = null)
        {
            if (LoadedMintableNFTItems.Count == 0 || LoadedMintableSBTItems.Count == 0 || LoadedMintableFTItems.Count == 0)
            {
                LoadMsnMintableItemList(sourceWzFile);
            }
        }

        public static void LoadMsnMintableItemList(Wz_File sourceWzFile)
        {
            Wz_Node itemWz = PluginManager.FindWz(Wz_Type.Item, sourceWzFile);
            if (itemWz == null)
                return;
            Wz_Node mintableListNode = itemWz.FindNodeByPath("MintableList.img", true);
            if (mintableListNode == null)
                return;

            LoadedMintableNFTItems.Clear();
            LoadedMintableSBTItems.Clear();
            LoadedMintableFTItems.Clear();

            Wz_Node nftNode = mintableListNode.FindNodeByPath("NFT");
            if (nftNode != null)
            {
                foreach (var i in nftNode.Nodes)
                {
                    switch (i.Text)
                    {
                        case "SBT":
                            foreach (var j in i.Nodes)
                            {
                                if (int.TryParse(j.Text, out int sbtId))
                                    LoadedMintableSBTItems.Add(sbtId);
                            }
                            break;
                        default:
                            if (int.TryParse(i.Text, out int id))
                                LoadedMintableNFTItems.Add(id);
                            break;
                    }
                }
            }

            Wz_Node ftNode = mintableListNode.FindNodeByPath("FT");
            if (ftNode != null)
            {
                foreach (var i in ftNode.Nodes)
                {
                    if (int.TryParse(i.Text, out int id))
                        LoadedMintableFTItems.Add(id);
                }
            }
        }

        public static void LoadPetEquipInfoIfEmpty(Wz_File sourceWzFile = null)
        {
            if (LoadedPetEquipInfo.Count == 0)
            {
                LoadPetEquipInfo(sourceWzFile);
            }
        }

        public static void LoadPetEquipInfo(Wz_File sourceWzFile)
        {
            Wz_Node characterWz = PluginManager.FindWz(Wz_Type.Character, sourceWzFile);
            if (characterWz == null)
                return;
            Wz_Node petEquipNode = characterWz.FindNodeByPath("PetEquip", true);
            if (petEquipNode == null)
                return;

            LoadedPetEquipInfo.Clear();
            foreach (var i in petEquipNode.Nodes)
            {
                Wz_Image image = i.GetValue<Wz_Image>();
                if (image == null || !image.TryExtract())
                {
                    continue;
                }
                else
                {
                    if (Int32.TryParse(i.Text.Replace(".img", ""), out int petEquipId))
                    {
                        List<int> applicablePets = new List<int>();
                        foreach (var j in image.Node.Nodes)
                        {
                            if (Int32.TryParse(j.Text, out int petID))
                            {
                                applicablePets.Add(petID);
                            }
                        }
                        LoadedPetEquipInfo[petEquipId] = applicablePets;
                    }
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
                        bool isHeroicOnly = commodity.gameWorlds.Contains(45) && (!commodity.gameWorlds.Contains(1) || !commodity.gameWorlds.Contains(0));
                        // 45: Reboot
                        // 1: Scania (GMS)
                        // 0: Scania (KMS)
                        if (isHeroicOnly)
                        {
                            if (!LoadedCommoditiesByItemIdHeroic.ContainsKey(commodity.ItemId))
                            {
                                LoadedCommoditiesByItemIdHeroic[commodity.ItemId] = new Dictionary<int, int>();
                            }
                            LoadedCommoditiesByItemIdHeroic[commodity.ItemId][commodity.Count] = commodity.Price;
                        }
                        else
                        {
                            if (!LoadedCommoditiesByItemIdInteractive.ContainsKey(commodity.ItemId))
                            {
                                LoadedCommoditiesByItemIdInteractive[commodity.ItemId] = new Dictionary<int, int>();
                            }
                            if (commodity.Price > 1) LoadedCommoditiesByItemIdInteractive[commodity.ItemId][commodity.Count] = commodity.Price;
                        }
                    }
                }
            }
        }

        public static void ClearAll()
        {
            LoadedSetItems.Clear();
            LoadedExclusiveEquips.Clear();
            LoadedCommoditiesBySN.Clear();
            LoadedCommoditiesByItemId.Clear();
            LoadedCommoditiesByItemIdInteractive.Clear();
            LoadedCommoditiesByItemIdHeroic.Clear();
            LoadedMintableNFTItems.Clear();
            LoadedMintableSBTItems.Clear();
            LoadedMintableFTItems.Clear();
            LoadedPetEquipInfo.Clear();
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
