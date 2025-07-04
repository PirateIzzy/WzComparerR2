﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Skill
    {
        public Skill()
        {
            this.level = 0;
            this.levelCommon = new List<Dictionary<string, string>>();
            this.common = new Dictionary<string, string>();
            this.PVPcommon = new Dictionary<string, string>();
            this.RelationSkill = null;
            this.ReqSkill = new Dictionary<int, int>();
            this.Action = new List<string>();
            this.Lt = new Dictionary<string, Wz_Vector>();
            this.Rb = new Dictionary<string, Wz_Vector>();
        }

        private int level;
        internal List<Dictionary<string, string>> levelCommon;
        internal Dictionary<string, string> common;

        public Dictionary<string, string> Common
        {
            get
            {
                if (PreBBSkill && this.level > 0 && this.level <= levelCommon.Count)
                    return levelCommon[this.level - 1];
                else
                    return common;
            }
        }

        public Dictionary<string, string> PVPcommon { get; private set; }
        public int SkillID { get; set; }
        public BitmapOrigin Icon { get; set; }
        public BitmapOrigin IconMouseOver { get; set; }
        public BitmapOrigin IconDisabled { get; set; }

        public HyperSkillType Hyper { get; set; }
        public bool HyperStat { get; set; }

        public int Level
        {
            get { return level; }
            set
            {
                bool canBreakLevel = this.CombatOrders || this.VSkill
                    || this.SkillID / 100000 == 4000; //fix for evan
                int maxLevel = canBreakLevel ? 100 : this.MaxLevel;
                level = Math.Max(0, Math.Min(value, maxLevel));
            }
        }

        public int ReqLevel { get; set; }
        public int ReqAmount { get; set; }
        public bool PreBBSkill { get; set; }
        public bool Invisible { get; set; }
        public bool CombatOrders { get; set; }
        public bool NotRemoved { get; set; }
        public bool VSkill { get; set; }
        public int VSkillValue { get; set; }
        public bool NotIncBuffDuration { get; set; }
        public bool NotCooltimeReset { get; set; }
        public bool NotCooltimeReduce { get; set; }
        public bool Origin { get; set; }
        public bool Ascent { get; set; }
        public bool TimeLimited { get; set; }
        public Tuple<int, int> RelationSkill { get; set; }
        public bool IsPetAutoBuff { get; set; }
        public bool IsSequenceOn { get; set; }
        public bool DisableNextLevelInfo { get; set; }
        public int MasterLevel { get; set; }
        public Dictionary<int, int> ReqSkill { get; private set; }
        public List<string> Action { get; private set; }
        public int AddAttackToolTipDescSkill { get; set; }
        public int AssistSkillLink { get; set; }
        public int VehicleID { get; set; }
        public Dictionary<string, Wz_Vector> Lt {  get; set; }
        public Dictionary<string, Wz_Vector> Rb { get; set; }

        public Point LT { get; set; }
        public Point RB { get; set; }

        public int MaxLevel
        {
            get
            {
                string v;
                if (this.PreBBSkill)
                    return levelCommon.Count;
                else if (common.TryGetValue("maxLevel", out v))
                    return Convert.ToInt32(v);
                return 0;
            }
        }

        public static Skill CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode, Wz_File wzf = null)
        {
            Skill skill = new Skill();
            int skillID;
            if (!Int32.TryParse(node?.Text, out skillID))
                return null;
            skill.SkillID = skillID;

            foreach (Wz_Node childNode in node.Nodes)
            {
                switch (childNode.Text)
                {
                    case "icon":
                        skill.Icon = BitmapOrigin.CreateFromNode(childNode, findNode, wzf);
                        break;
                    case "iconMouseOver":
                        skill.IconMouseOver = BitmapOrigin.CreateFromNode(childNode, findNode, wzf);
                        break;
                    case "iconDisabled":
                        skill.IconDisabled = BitmapOrigin.CreateFromNode(childNode, findNode, wzf);
                        break;
                    case "common":
                        foreach (Wz_Node commonNode in childNode.Nodes)
                        {
                            if (commonNode.Value != null && !(commonNode.Value is Wz_Vector))
                            {
                                skill.common[commonNode.Text] = commonNode.Value.ToString();
                            }
                            else if (commonNode.Value != null && commonNode.Value is Wz_Vector)
                            {
                                var match = Regex.Match(commonNode.Text, "^(lt|rb)([0-9]*)$");
                                if (match.Success)
                                {
                                    if (match.Groups[1].Value == "lt")
                                    {
                                        skill.Lt[match.Groups[2].Value] = commonNode.Value as Wz_Vector;
                                    }
                                    else
                                    {
                                        skill.Rb[match.Groups[2].Value] = commonNode.Value as Wz_Vector;
                                    }
                                }
                            }
                        }
                        break;
                    case "PVPcommon":
                        foreach (Wz_Node commonNode in childNode.Nodes)
                        {
                            if (commonNode.Value != null && !(commonNode.Value is Wz_Vector))
                            {
                                skill.PVPcommon[commonNode.Text] = commonNode.Value.ToString();
                            }
                        }
                        break;
                    case "level":
                        for (int i = 1; ; i++)
                        {
                            Wz_Node levelNode = childNode.FindNodeByPath(i.ToString());
                            if (levelNode == null)
                                break;
                            Dictionary<string, string> levelInfo = new Dictionary<string, string>();

                            foreach (Wz_Node commonNode in levelNode.Nodes)
                            {
                                if (commonNode.Value != null && !(commonNode.Value is Wz_Vector))
                                {
                                    levelInfo[commonNode.Text] = commonNode.Value.ToString();
                                }
                            }

                            skill.levelCommon.Add(levelInfo);
                        }
                        break;
                    case "hyper":
                        skill.Hyper = (HyperSkillType)childNode.GetValue<int>();
                        break;
                    case "hyperStat":
                        skill.HyperStat = childNode.GetValue<int>() != 0;
                        break;
                    case "invisible":
                        skill.Invisible = childNode.GetValue<int>() != 0;
                        break;
                    case "combatOrders":
                        skill.CombatOrders = childNode.GetValue<int>() != 0;
                        break;
                    case "notRemoved":
                        skill.NotRemoved = childNode.GetValue<int>() != 0;
                        break;
                    case "vSkill":
                        skill.VSkill = childNode.GetValue<int>() != 0;
                        skill.VSkillValue = childNode.GetValue<int>();
                        break;
                    case "notIncBuffDuration":
                        skill.NotIncBuffDuration = childNode.GetValue<int>() != 0;
                        break;
                    case "notCooltimeReset":
                        skill.NotCooltimeReset = childNode.GetValue<int>() != 0;
                        break;
                    case "notCooltimeReduce":
                        skill.NotCooltimeReduce = childNode.GetValue<int>() != 0;
                        break;
                    case "origin":
                        skill.Origin = childNode.GetValue<int>() != 0;
                        break;
                    case "ascent":
                        skill.Ascent = childNode.GetValue<int>() != 0;
                        break;
                    case "timeLimited":
                        skill.TimeLimited = childNode.GetValue<int>() != 0;
                        break;
                    case "relationSkill":
                        skill.RelationSkill = Tuple.Create(childNode.Nodes["skillID"].GetValueEx<int>(0), childNode.Nodes["periodMin"].GetValueEx<int>(0));
                        break;
                    case "isPetAutoBuff":
                        skill.IsPetAutoBuff = childNode.GetValue<int>() != 0;
                        break;
                    case "isSequenceOn":
                        skill.IsSequenceOn = childNode.GetValue<int>() != 0;
                        break;
                    case "disableNextLevelInfo":
                        skill.DisableNextLevelInfo = childNode.GetValue<int>() != 0;
                        break;
                    case "masterLevel":
                        skill.MasterLevel = childNode.GetValue<int>();
                        break;
                    case "reqLev":
                        skill.ReqLevel = childNode.GetValue<int>();
                        break;
                    case "req":
                        foreach (Wz_Node reqNode in childNode.Nodes)
                        {
                            if (reqNode.Text == "level")
                            {
                                skill.ReqLevel = reqNode.GetValue<int>();
                            }
                            else if (reqNode.Text == "reqAmount")
                            {
                                skill.ReqAmount = reqNode.GetValue<int>();
                            }
                            else
                            {
                                int reqSkill;
                                if (Int32.TryParse(reqNode.Text, out reqSkill))
                                {
                                    skill.ReqSkill[reqSkill] = reqNode.GetValue<int>();
                                }
                            }
                        }
                        break;
                    case "action":
                        for (int i = 0; ; i++)
                        {
                            Wz_Node idxNode = childNode.FindNodeByPath(i.ToString());
                            if (idxNode == null)
                                break;
                            skill.Action.Add(idxNode.GetValue<string>());
                        }
                        break;
                    case "addAttack":
                        Wz_Node toolTipDescNode = childNode.FindNodeByPath("toolTipDesc");
                        if (toolTipDescNode != null && toolTipDescNode.GetValue<int>() != 0)
                        {
                            skill.AddAttackToolTipDescSkill = childNode.FindNodeByPath("toolTipDescSkill").GetValue<int>();
                        }
                        break;
                    case "assistSkillLink":
                        skill.AssistSkillLink = childNode.FindNodeByPath("skill").GetValue<int>();
                        break;
                    case "vehicleID":
                        skill.VehicleID = childNode.GetValue<int>();
                        break;
                }
            }

            if ((skill.common.ContainsKey("forceCon") || (skill.levelCommon.Count > 0 && skill.levelCommon[0].ContainsKey("forceCon"))) && skill.Hyper == HyperSkillType.None)
            {
                Wz_Node forceNode = null;
                if (skill.SkillID / 10000 == 3001 || skill.SkillID / 10000 == 3100 || skill.SkillID / 10000 == 3110 || skill.SkillID / 10000 == 3111 || skill.SkillID / 10000 == 3112)
                {
                    forceNode = findNode.Invoke(string.Format("UI\\UIWindow2.img\\Skill\\main\\Force\\{0}", (Int32.Parse(skill.common["forceCon"]) - 1) / 30));
                }
                else if (skill.SkillID / 10000 / 1000 == 10)
                {
                    forceNode = findNode.Invoke(string.Format("UI\\UIWindow2.img\\SkillZero\\main\\Alpha\\{0}", skill.SkillID / 1000 % 10));
                }
                if (forceNode != null)
                {
                    BitmapOrigin force = BitmapOrigin.CreateFromNode(forceNode, findNode, wzf);
                    using (Graphics graphics = Graphics.FromImage(skill.Icon.Bitmap))
                    {
                        graphics.DrawImage(force.Bitmap, new Point(0, 0));
                    }
                    using (Graphics graphics = Graphics.FromImage(skill.IconMouseOver.Bitmap))
                    {
                        graphics.DrawImage(force.Bitmap, new Point(0, 0));
                    }
                    using (Graphics graphics = Graphics.FromImage(skill.IconDisabled.Bitmap))
                    {
                        graphics.DrawImage(force.Bitmap, new Point(0, 0));
                    }
                }
            }

            //判定技能声明版本
            skill.PreBBSkill = false;
            if (skill.levelCommon.Count > 0)
            {
                if (skill.common.Count <= 0 || skill.common.ContainsKey("maxLevel"))
                {
                    skill.PreBBSkill = true;
                }
            }

            return skill;
        }
    }
}
