﻿using System;
using System.Collections.Generic;
using System.Linq;
using WzComparerR2.WzLib;

namespace WzComparerR2.Common
{
    public class StringLinker
    {
        public StringLinker()
        {
            stringEqp = new Dictionary<int, StringResult>();
            stringItem = new Dictionary<int, StringResult>();
            stringMap = new Dictionary<int, StringResult>();
            stringMob = new Dictionary<int, StringResult>();
            stringNpc = new Dictionary<int, StringResult>();
            stringSkill = new Dictionary<int, StringResult>();
            stringSkill2 = new Dictionary<string, StringResult>();
            stringSetItem = new Dictionary<int, StringResult>();
            stringQuest = new Dictionary<int, StringResult>();
            stringAchievement = new Dictionary<int, StringResult>();
        }

        public bool Update(Wz_Node stringNode, Wz_Node itemNode, Wz_Node etcNode, Wz_Node questNode)
        {
            if (stringNode == null || itemNode == null || etcNode == null)
                return false;

            return Load(stringNode, itemNode, etcNode, questNode, update: true);
        }

        public bool Load(Wz_File stringWz, Wz_File itemWz, Wz_File etcWz)
        {
            return Load(stringWz, itemWz, etcWz, null);
        }

        public bool Load(Wz_File stringWz, Wz_File itemWz, Wz_File etcWz, Wz_File questWz)
        {
            if (stringWz == null || stringWz.Node == null ||
                itemWz == null || itemWz.Node == null ||
                etcWz == null || etcWz.Node == null)
                return false;
            this.Clear();

            return Load(stringWz.Node, itemWz.Node, etcWz.Node, questWz.Node);
        }

        public bool Load(Wz_Node stringNode, Wz_Node itemNode, Wz_Node etcNode, Wz_Node questNode, bool update = false)
        {
            int id;
            foreach (Wz_Node node in stringNode.Nodes ?? new Wz_Node.WzNodeCollection(null))
            {
                Wz_Image image = node.Value as Wz_Image;
                if (image == null)
                    continue;
                switch (node.Text)
                {
                    case "Pet.img":
                    case "Cash.img":
                    case "Ins.img":
                    case "Consume.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            Wz_Node test_tree = TryLocateUolNode(tree);
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringItem[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name;
                                strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                strResult.AutoDesc = GetDefaultString(linkNode, "autodesc") ?? strResult.AutoDesc;
                                if (tree.FullPath == test_tree.FullPath)
                                {
                                    if (tree.FullPath == test_tree.FullPath)
                                    {
                                        strResult.FullPath = tree.FullPath;
                                    }
                                    else
                                    {
                                        strResult.FullPath = tree.FullPath + " -> " + test_tree.FullPath;
                                    }
                                }
                                else
                                {
                                    strResult.FullPath = tree.FullPath + " -> " + test_tree.FullPath;
                                }

                                AddAllValue(strResult, linkNode);
                                stringItem[id] = strResult;
                            }
                        }
                        break;
                    case "Etc.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree0 in image.Node.Nodes)
                        {
                            foreach (Wz_Node tree in tree0.Nodes)
                            {
                                Wz_Node test_tree = TryLocateUolNode(tree);
                                if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                                {
                                    StringResult strResult = null;
                                    if (update)
                                    {
                                        try { strResult = stringItem[id]; }
                                        catch { }
                                    }
                                    if (strResult == null) strResult = new StringResult();

                                    strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name;
                                    strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                    if (tree.FullPath == test_tree.FullPath)
                                    {
                                        if (tree.FullPath == test_tree.FullPath)
                                        {
                                            strResult.FullPath = tree.FullPath;
                                        }
                                        else
                                        {
                                            strResult.FullPath = tree.FullPath + " -> " + test_tree.FullPath;
                                        }
                                    }
                                    else
                                    {
                                        strResult.FullPath = tree.FullPath + " -> " + test_tree.FullPath;
                                    }

                                    AddAllValue(strResult, linkNode);
                                    stringItem[id] = strResult;
                                }
                            }
                        }
                        break;
                    case "Mob.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            Wz_Node test_tree = TryLocateUolNode(tree);
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringMob[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name;
                                if (tree.FullPath == test_tree.FullPath)
                                {
                                    strResult.FullPath = tree.FullPath;
                                }
                                else
                                {
                                    strResult.FullPath = tree.FullPath + " -> " + test_tree.FullPath;
                                }

                                AddAllValue(strResult, linkNode);
                                stringMob[id] = strResult;
                            }
                        }
                        break;
                    case "Npc.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            Wz_Node test_tree = TryLocateUolNode(tree);
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringNpc[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name;
                                strResult.Desc = GetDefaultString(linkNode, "func") ?? strResult.Desc;
                                if (tree.FullPath == test_tree.FullPath)
                                {
                                    strResult.FullPath = tree.FullPath;
                                }
                                else
                                {
                                    strResult.FullPath = tree.FullPath + " -> " + test_tree.FullPath;
                                }

                                AddAllValue(strResult, linkNode);
                                stringNpc[id] = strResult;
                            }
                        }
                        break;
                    case "Map.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree0 in image.Node.Nodes)
                        {
                            foreach (Wz_Node tree in tree0.Nodes)
                            {
                                Wz_Node test_tree = TryLocateUolNode(tree);
                                if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                                {
                                    StringResult strResult = null;
                                    if (update)
                                    {
                                        try { strResult = stringMap[id]; }
                                        catch { }
                                    }
                                    if (strResult == null) strResult = new StringResult();

                                    var streetName = GetDefaultString(linkNode, "streetName");
                                    var mapName = GetDefaultString(linkNode, "mapName");
                                    strResult.Name = string.Format("{0} : {1}",
                                        streetName,
                                        mapName) ?? strResult.Name;
                                    strResult.StreetName = streetName ?? strResult.StreetName;
                                    strResult.MapName = mapName ?? strResult.MapName;
                                    strResult.Desc = GetDefaultString(linkNode, "mapDesc") ?? strResult.Desc;
                                    if (tree.FullPath == test_tree.FullPath)
                                    {
                                        strResult.FullPath = tree.FullPath;
                                    }
                                    else
                                    {
                                        strResult.FullPath = tree.FullPath + " -> " + test_tree.FullPath;
                                    }

                                    AddAllValue(strResult, linkNode);
                                    stringMap[id] = strResult;
                                }
                            }
                        }
                        break;
                    case "Skill.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            Wz_Node test_tree = TryLocateUolNode(tree);
                            if (tree.ResolveUol() is not Wz_Node linkNode)
                            {
                                continue;
                            }
                            StringResult strResult = null;
                            if (update)
                            {
                                try
                                {
                                    if (tree.Text.Length >= 7 && Int32.TryParse(tree.Text, out id))
                                    {
                                        strResult = stringSkill[id];
                                    }
                                    strResult = stringSkill2[tree.Text];
                                }
                                catch { }
                            }
                            if (strResult == null) strResult = new StringResultSkill();

                            strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name;//?? GetDefaultString(tree, "bookName");
                            strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                            strResult.Pdesc = GetDefaultString(linkNode, "pdesc") ?? strResult.Pdesc;

                            var h = GetDefaultString(linkNode, "h");
                            if (update && h != null)
                            {
                                strResult.SkillH.Clear();
                            }
                            strResult.SkillH.Add(h);

                            h = GetDefaultString(linkNode, "ph");
                            if (update && h != null)
                            {
                                strResult.SkillpH.Clear();
                                strResult.SkillpH.Add(h);
                            }
                            else if (!update) strResult.SkillpH.Add(h);

                            h = GetDefaultString(linkNode, "hch");
                            if (update && h != null)
                            {
                                strResult.SkillhcH.Clear();
                                strResult.SkillhcH.Add(h);
                            }
                            else if (!update) strResult.SkillhcH.Add(h);

                            if (strResult.SkillH.Count > 0 && strResult.SkillH.Last() == null)
                            {
                                strResult.SkillH.RemoveAt(strResult.SkillH.Count - 1);
                                bool cleared = false;

                                for (int i = 1; ; i++)
                                {
                                    string hi = GetDefaultString(linkNode, "h" + i);
                                    if (string.IsNullOrEmpty(hi))
                                        break;
                                    else if (update && !cleared)
                                    {
                                        strResult.SkillH.Clear();
                                        cleared = true;
                                    }
                                    strResult.SkillH.Add(hi);
                                }
                            }
                            strResult.SkillH.TrimExcess();
                            strResult.SkillpH.TrimExcess();
                            if (tree.FullPath == test_tree.FullPath)
                            {
                                strResult.FullPath = tree.FullPath;
                            }
                            else
                            {
                                strResult.FullPath = tree.FullPath + " -> " + test_tree.FullPath;
                            }

                            AddAllValue(strResult, linkNode);
                            if (tree.Text.Length >= 7 && Int32.TryParse(tree.Text, out id))
                            {
                                stringSkill[id] = strResult;
                            }
                            stringSkill2[tree.Text] = strResult;
                        }
                        break;
                    case "Eqp.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree0 in image.Node.Nodes)
                        {
                            foreach (Wz_Node tree1 in tree0.Nodes)
                            {
                                foreach (Wz_Node tree in tree1.Nodes)
                                {
                                    Wz_Node test_tree = TryLocateUolNode(tree);
                                    if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                                    {
                                        StringResult strResult = null;
                                        if (update)
                                        {
                                            try { strResult = stringEqp[id]; }
                                            catch { }
                                        }
                                        if (strResult == null) strResult = new StringResult();

                                        strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name;
                                        strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                        if (tree.FullPath == test_tree.FullPath)
                                        {
                                            strResult.FullPath = tree.FullPath;
                                        }
                                        else
                                        {
                                            strResult.FullPath = tree.FullPath + " -> " + test_tree.FullPath;
                                        }

                                        AddAllValue(strResult, linkNode);
                                        stringEqp[id] = strResult;
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            foreach (Wz_Node node in itemNode.FindNodeByPath("Special")?.Nodes ?? new Wz_Node.WzNodeCollection(null))
            {
                Wz_Image image = node.Value as Wz_Image;
                if (image == null)
                    continue;
                switch (node.Text)
                {
                    case "0910.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            Wz_Node test_tree = TryLocateUolNode(tree);
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringItem[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name;
                                strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                if (tree.FullPath == test_tree.FullPath)
                                {
                                    strResult.FullPath = tree.FullPath;
                                }
                                else
                                {
                                    strResult.FullPath = tree.FullPath + " -> " + test_tree.FullPath;
                                }

                                AddAllValue(strResult, linkNode);
                                stringItem[id] = strResult;
                            }
                        }
                        break;
                }
            }

            foreach (Wz_Node node in etcNode.Nodes ?? new Wz_Node.WzNodeCollection(null))
            {
                Wz_Image image = node.Value as Wz_Image;
                if (image == null)
                    continue;
                switch (node.Text)
                {
                    case "SetItemInfo.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {

                            Wz_Node test_tree = TryLocateUolNode(tree);
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringSetItem[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "setItemName") ?? strResult.Name;
                                if (tree.FullPath == test_tree.FullPath)
                                {
                                    strResult.FullPath = tree.FullPath;
                                }
                                else
                                {
                                    strResult.FullPath = tree.FullPath + " -> " + test_tree.FullPath;
                                }

                                AddAllValue(strResult, linkNode);
                                stringSetItem[id] = strResult;
                            }
                        }
                        break;
                }
            }

            var achievementNode = etcNode.FindNodeByPath("Achievement\\AchievementData");
            foreach (Wz_Node node in achievementNode.Nodes ?? new Wz_Node.WzNodeCollection(null))
            {
                Wz_Image image = node.Value as Wz_Image;
                if (image == null || !image.TryExtract())
                    continue;
                Wz_Node tree = image.Node;
                Wz_Node infoNode = tree.FindNodeByPath("info");
                if (Int32.TryParse(tree.Text.Replace(".img", ""), out id) && infoNode.ResolveUol() is Wz_Node linkNode && linkNode != null)
                {
                    StringResult strResult = null;
                    if (update)
                    {
                        try { strResult = stringAchievement[id]; }
                        catch { }
                    }
                    if (strResult == null) strResult = new StringResult();

                    strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name;
                    strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                    strResult.FullPath = "AchievementData\\" + tree.FullPath;

                    //AddAllValue(strResult, linkNode);
                    stringAchievement[id] = strResult;
                }
            }

            Wz_Node qDataNode = questNode?.FindNodeByPath("QuestData");
            Wz_Node qInfoNode = null;
            bool newQuestDir = true;
            if (qDataNode == null)
            {
                qDataNode = questNode?.FindNodeByPath("QuestInfo.img");
                Wz_Image image = qDataNode.Value as Wz_Image;
                if (image != null && image.TryExtract())
                {
                    qDataNode = image.Node;
                    newQuestDir = false;
                }
            }
            foreach (Wz_Node node in qDataNode?.Nodes ?? new Wz_Node.WzNodeCollection(null))
            {
                Wz_Node tree = node;
                if (node.Value is Wz_Image image)
                {
                    if (image == null)
                        continue;

                    if (!image.TryExtract()) continue;
                    tree = image.Node;
                }
                qInfoNode = newQuestDir ? tree.FindNodeByPath("QuestInfo") : tree;
                if (Int32.TryParse(tree.Text.Replace(".img", ""), out id) && qInfoNode.ResolveUol() is Wz_Node linkNode && linkNode != null)
                {
                    StringResult strResult = null;
                    if (update)
                    {
                        try { strResult = stringQuest[id]; }
                        catch { }
                    }
                    if (strResult == null) strResult = new StringResult();

                    strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name;
                    strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                    strResult.FullPath = (newQuestDir ? "QuestData\\" : "") + tree.FullPath;

                    AddAllValue(strResult, linkNode);
                    stringQuest[id] = strResult;
                }
            }

            return this.HasValues;
        }

        public void Clear()
        {
            stringEqp.Clear();
            stringItem.Clear();
            stringMob.Clear();
            stringMap.Clear();
            stringNpc.Clear();
            stringSkill.Clear();
            stringSkill2.Clear();
            stringSetItem.Clear();
            stringQuest.Clear();
            stringAchievement.Clear();
        }

        public bool HasValues
        {
            get
            {
                return (stringEqp.Count + stringItem.Count + stringMap.Count +
                    stringMob.Count + stringNpc.Count + stringSkill.Count + stringSetItem.Count + stringQuest.Count + stringAchievement.Count > 0);
            }
        }

        private Dictionary<int, StringResult> stringEqp;
        private Dictionary<int, StringResult> stringItem;
        private Dictionary<int, StringResult> stringMap;
        private Dictionary<int, StringResult> stringMob;
        private Dictionary<int, StringResult> stringNpc;
        private Dictionary<int, StringResult> stringSkill;
        private Dictionary<string, StringResult> stringSkill2;
        private Dictionary<int, StringResult> stringSetItem;
        private Dictionary<int, StringResult> stringQuest;
        private Dictionary<int, StringResult> stringAchievement;

        private string GetDefaultString(Wz_Node node, string searchNodeText)
        {
            node = node.FindNodeByPath(searchNodeText);
            return node == null ? null : Convert.ToString(node.Value);
        }

        private void AddAllValue(StringResult sr, Wz_Node node)
        {
            foreach (Wz_Node child in node.Nodes)
            {
                if (child.Value != null)
                {
                    sr[child.Text] = child.GetValue<string>();
                }
            }
        }

        private Wz_Node TryLocateUolNode(Wz_Node node)
        {
            if (node.Value is Wz_Uol)
            {
                Wz_Uol uol = node.Value as Wz_Uol;
                Wz_Node uolNode = uol.HandleUol(node);
                if (uolNode != null)
                {
                    return uolNode;
                }
                else
                {
                    return node;
                }
            }
            else
            {
                return node;
            }
        }


        public Dictionary<int, StringResult> StringEqp
        {
            get { return stringEqp; }
        }

        public Dictionary<int, StringResult> StringItem
        {
            get { return stringItem; }
        }

        public Dictionary<int, StringResult> StringMap
        {
            get { return stringMap; }
        }

        public Dictionary<int, StringResult> StringMob
        {
            get { return stringMob; }
        }

        public Dictionary<int, StringResult> StringNpc
        {
            get { return stringNpc; }
        }

        public Dictionary<int, StringResult> StringSkill
        {
            get { return stringSkill; }
        }

        public Dictionary<string, StringResult> StringSkill2
        {
            get { return stringSkill2; }
        }

        public Dictionary<int, StringResult> StringSetItem
        {
            get { return stringSetItem; }
        }

        public Dictionary<int, StringResult> StringQuest
        {
            get { return stringQuest; }
        }

        public Dictionary<int, StringResult> StringAchievement
        {
            get { return stringAchievement; }
        }
    }
}