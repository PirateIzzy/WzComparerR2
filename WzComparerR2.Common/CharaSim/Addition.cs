﻿using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Addition
    {
        public Addition()
        {
            Props = new Dictionary<string, string>();
            ConValue = new List<int>();
        }

        public AdditionType Type { get; set; }
        public GearPropType ConType { get; set; }
        public List<int> ConValue { get; private set; }
        public Dictionary<string, string> Props { get; private set; }

        public string GetPropString()
        {
            StringBuilder sb;
            switch (this.Type)
            {
                case AdditionType.boss:
                    sb = new StringBuilder();
                    //sb.Append("When attacking Bosses, "); NECESSARY FOR GMS VERSION
                    {
                        string v1;
                        if (this.Props.TryGetValue("prob", out v1))
                            sb.Append("Has a " + v1 + "% chance ");
                        sb.Append("to deal " + Props["damage"] + "% extra damage on boss monsters.");
                    }
                    return sb.ToString();
                case AdditionType.critical:
                    sb = new StringBuilder();
                    {
                        string val;
                        if (this.Props.TryGetValue("prob", out val))
                        {
                            sb.AppendFormat("Critical Rate: +{0}%\r\n", val);
                        }
                        if (this.Props.TryGetValue("damage", out val))
                        {
                            sb.AppendFormat("Critical Damage: +{0}%\r\n", val);
                        }
                        if (sb.Length > 2)
                        {
                            sb.Remove(sb.Length - 2, 2);
                        }
                    }
                    return sb.ToString();
                case AdditionType.elemboost:
                    {
                        string v1, elem;
                        if (this.Props.TryGetValue("elemVol", out v1))
                        {
                            switch (v1[0])
                            {
                                case 'I': elem = "Ice"; break;
                                case 'F': elem = "Fire"; break;
                                case 'L': elem = "Lightning"; break;
                                default: elem = v1[0].ToString(); break;
                            }
                            return elem + " Attribute: +" + v1.Substring(1) + "%";
                        }
                    }
                    break;
                case AdditionType.hpmpchange:
                    sb = new StringBuilder();
                    sb.Append("Recover");
                    {
                        string v1;
                        if (this.Props.TryGetValue("hpChangePerTime", out v1))
                        {
                            sb.Append("HP per 10 seconds " + v1);
                        }
                    }
                    return sb.ToString();
                case AdditionType.mobcategory:
                    return "When attacking " + ItemStringHelper.GetMobCategoryName(Convert.ToInt32(this.Props["category"])) + " enemies, deals " + this.Props["damage"] + "% extra damage.";
                case AdditionType.mobdie:
                    sb = new StringBuilder();
                    {
                        string v1;
                        if (this.Props.TryGetValue("hpIncOnMobDie", out v1))
                        {
                            sb.AppendLine("When you kill a monster, to recover " + v1 + " HP");
                        }
                        if (this.Props.TryGetValue("hpIncRatioOnMobDie", out v1))
                        {
                            sb.AppendLine("When you kill a monster, has a " + Props["hpRatioProp"] + "% chance to recover " + v1 + "% of damage to as HP (Cannot recover more than 10% of Max HP.)");
                        }
                        if (this.Props.TryGetValue("mpIncOnMobDie", out v1))
                        {
                            sb.AppendLine("When you kill a monster, to recover " + v1 + " MP");
                        }
                        if (this.Props.TryGetValue("mpIncRatioOnMobDie", out v1))
                        {
                            sb.AppendLine("When you kill a monster, has a " + Props["hpRatioProp"] + "% chance to recover " + v1 + "% of damage to as MP (Cannot recover more than 10% of Max MP.)");
                        }
                    }
                    if (sb.Length > 0)
                    {
                        sb.Append("Function may be limited in some locations.");
                        return sb.ToString();
                    }
                    break;
                case AdditionType.skill:
                    switch (Convert.ToInt32(this.Props["id"]))
                    {
                        case 90000000: return "Has a chance to add: Instant Death effect";
                        case 90001001: return "Has a chance to add: Knock Down effect";
                        case 90001002: return "Has a chance to add: Slow effect";
                        case 90001003: return "Has a chance to add: Poison effect";
                        case 90001004: return "Has a chance to add: Darkness effect";
                        case 90001005: return "Has a chance to add: Seal effect";
                        case 90001006: return "Has a chance to add: Freeze effect";
                    }
                    break;
                case AdditionType.statinc:
                    sb = new StringBuilder();
                    {
                        List<GearPropType> props = new List<GearPropType>();
                        foreach (var kv in Props)
                        {
                            try
                            {
                                GearPropType propType = (GearPropType)Enum.Parse(typeof(GearPropType), kv.Key);
                                props.Add(propType);
                            }
                            catch
                            {
                            }
                        }
                        props.Sort();
                        foreach (GearPropType type in props)
                        {
                            var text = ItemStringHelper.GetGearPropString(type, Convert.ToInt32(Props[Enum.GetName(typeof(GearPropType), type)]));
                            if (!string.IsNullOrEmpty(text))
                            {
                                sb.AppendLine(text);
                            }
                        }
                    }
                    if (sb.Length > 0)
                    {
                        return sb.ToString();
                    }
                    break;
                default: return null;
            }
            return null;
        }

        public string GetConString()
        {
            switch (this.ConType)
            {
                case GearPropType.reqJob:
                    string[] reqJobs = new string[this.ConValue.Count];
                    for (int i = 0; i < reqJobs.Length; i++)
                    {
                        reqJobs[i] = ItemStringHelper.GetJobName(this.ConValue[i]) ?? this.ConValue[i].ToString();
                    }
                    return "When your job is " + string.Join(" or ", reqJobs) + ",";
                case GearPropType.reqLevel:
                    return "When your level is " + this.ConValue[0] + " or higher,";
                case GearPropType.reqCraft:
                    int lastExp;
					return "When Diligence EXP is " + this.ConValue[0] + " (Lv. " + getPersonalityLevel(this.ConValue[0], out lastExp) + " " + lastExp + " Points) or higher";
                case GearPropType.reqWeekDay:
                    string[] weekdays = new string[this.ConValue.Count];
                    for (int i = 0; i < this.ConValue.Count; i++)
                    {
                        weekdays[i] = GetWeekDayString(this.ConValue[i]);
                    }
                    return string.Join(", ", weekdays);
                default:
                    return null;
            }
        }

        private int getPersonalityLevel(int totalExp, out int lastExp)
        {
            int curExp = 0;
            for (int level = 0; ; level++)
            {
                if (level == 0)
                {
                    curExp = 20;
                }
                else if (level < 10)
                {
                    curExp = (int)Math.Round(curExp * 1.3, MidpointRounding.AwayFromZero);
                }
                else if (level < 20)
                {
                    curExp = (int)Math.Round(curExp * 1.1, MidpointRounding.AwayFromZero);
                }
                else if (level < 30)
                {
                    curExp = (int)Math.Round(curExp * 1.03, MidpointRounding.AwayFromZero);
                }
                else if (level < 70)
                {
                    curExp = (int)Math.Round(curExp * 1.015, MidpointRounding.AwayFromZero);
                }
                else if (level < 100)
                {
                    curExp = (int)Math.Round(curExp * 1.003, MidpointRounding.AwayFromZero);
                }
                else
                {
                    lastExp = 0;
                    return 100;
                }
                if (totalExp - curExp <= 0)
                {
                    lastExp = totalExp;
                    return level;
                }
                else
                {
                    totalExp -= curExp;
                }
            }
        }

        private static string GetWeekDayString(int weekDay)
        {
            switch (weekDay)
            {
                case 0: return "Sunday";
                case 1: return "Monday";
                case 2: return "Tuesday";
                case 3: return "Wednesday";
                case 4: return "Thursday";
                case 5: return "Friday";
                case 6: return "Saturday";
                default: return "Week" + weekDay; //这怎么可能...
            }
        }

        public static Addition CreateFromNode(Wz_Node node)
        {
            if (node == null)
                return null;
            foreach (AdditionType type in Enum.GetValues(typeof(AdditionType)))
            {
                if (type.ToString() == node.Text)
                {
                    Addition addition = new Addition();
                    addition.Type = type;
                    Action<Wz_Node> addInt32 = n => addition.ConValue.Add(n.GetValue<int>());
                    Action<Wz_Node> addWeekDay = n =>
                    {
                        try
                        {
                            DayOfWeek weekday = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), n.GetValue<string>(), true);
                            addition.ConValue.Add((int)weekday);
                        }
                        catch { }
                    };

                    foreach (Wz_Node subNode in node.Nodes)
                    {
                        if (subNode.Text == "con")
                        {
                            Action<Wz_Node> addValueFunc = addInt32;
                            foreach (Wz_Node conNode in subNode.Nodes)
                            {
                                switch (conNode.Text)
                                {
                                    case "job":
                                        addition.ConType = GearPropType.reqJob;
                                        break;
                                    //case "lv": //已不被官方识别了
                                    case "level":
                                        addition.ConType = GearPropType.reqLevel;
                                        break;
                                    case "craft":
                                        addition.ConType = GearPropType.reqCraft;
                                        break;
                                    case "weekDay":
                                        addition.ConType = GearPropType.reqWeekDay;
                                        addValueFunc = addWeekDay; //改变解析方法
                                        break;
                                    default: //不识别的东西
                                        addition.ConType = (GearPropType)0;
                                        continue;
                                }

                                if (conNode.Nodes.Count > 0)
                                {
                                    foreach (Wz_Node conValNode in conNode.Nodes)
                                    {
                                        addValueFunc(conValNode);
                                    }
                                }
                                else
                                {
                                    addValueFunc(conNode);
                                }
                            }
                        }
                        else
                        {
                            addition.Props.Add(subNode.Text, Convert.ToString(subNode.Value));
                        }
                    }
                    return addition;
                }
            }
            return null;
        }
    }
}
