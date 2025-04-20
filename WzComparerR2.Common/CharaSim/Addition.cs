using System;
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
                    sb.Append("보스 공격 시, ");
                    {
                        string v1;
                        if (this.Props.TryGetValue("prob", out v1))
                            sb.Append(v1 + "%의 확률로 ");
                        sb.Append(Props["damage"] + "%의 데미지 추가");
                    }
                    return sb.ToString();
                case AdditionType.critical:
                    sb = new StringBuilder();
                    {
                        string val;
                        if (this.Props.TryGetValue("prob", out val))
                        {
                            sb.AppendFormat("크리티컬 확률 {0}%\r\n", val);
                        }
                        if (this.Props.TryGetValue("damage", out val))
                        {
                            sb.AppendFormat("크리티컬 데미지 {0}%증가\r\n", val);
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
                                case 'I': elem = "얼음"; break;
                                case 'F': elem = "불"; break;
                                case 'L': elem = "전기"; break;
                                default: elem = v1[0].ToString(); break;
                            }
                            return elem + "속성 효과 " + v1.Substring(1) + "% 강화";
                        }
                    }
                    break;
                case AdditionType.hpmpchange:
                    sb = new StringBuilder();
                    sb.Append("每10秒恢复");
                    {
                        string v1;
                        if (this.Props.TryGetValue("hpChangePerTime", out v1))
                        {
                            sb.Append("HP " + v1);
                        }
                    }
                    return sb.ToString();
                case AdditionType.mobcategory:
                    return ItemStringHelper.GetMobCategoryName(Convert.ToInt32(this.Props["category"])) + " 몬스터 공격 시, " + this.Props["damage"] + "% 추가 데미지";
                case AdditionType.mobdie:
                    sb = new StringBuilder();
                    {
                        string v1;
                        if (this.Props.TryGetValue("hpIncOnMobDie", out v1))
                        {
                            sb.AppendLine("몬스터 사망 시 HP " + v1 + " 회복");
                        }
                        if (this.Props.TryGetValue("hpIncRatioOnMobDie", out v1))
                        {
                            sb.AppendLine("몬스터 사망 시 " + Props["hpRatioProp"] + "%의 확률로 데미지의 " + v1 + "%의 HP 회복 ( 단 최대 HP의 10%를 넘을 수 없다. )");
                        }
                        if (this.Props.TryGetValue("mpIncOnMobDie", out v1))
                        {
                            sb.AppendLine("몬스터 사망 시 MP " + v1 + " 회복");
                        }
                        if (this.Props.TryGetValue("mpIncRatioOnMobDie", out v1))
                        {
                            sb.AppendLine("몬스터 사망 시 " + Props["hpRatioProp"] + "%의 확률로 데미지의 " + v1 + "%의 MP 회복 ( 단 최대 MP의 10%를 넘을 수 없다. )");
                        }
                    }
                    if (sb.Length > 0)
                    {
                        sb.Append("일부 지역에서는 기능이 제한될 수 있다.");
                        return sb.ToString();
                    }
                    break;
                case AdditionType.skill:
                    switch (Convert.ToInt32(this.Props["id"]))
                    {
                        //case 90000000: return "확률적으로 즉사 효과 추가";
                        case 90001001: return "확률적으로 기절 효과 추가";
                        case 90001002: return "확률적으로 슬로우 효과 추가";
                        case 90001003: return "확률적으로 독 효과 추가";
                        case 90001004: return "확률적으로 암흑 효과 추가";
                        case 90001005: return "확률적으로 봉인 효과 추가";
                        case 90001006: return "확률적으로 결빙 효과 추가";
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
                    return "직업이 " + string.Join(" 또는 ", reqJobs) + "일 때";
                case GearPropType.reqLevel:
                    return "레벨 " + this.ConValue[0] + " 이상일 때";
                case GearPropType.reqCraft:
                    int lastExp;
                    //return "손재주 경험치가 " + this.ConValue[0] + "(" + getPersonalityLevel(this.ConValue[0], out lastExp) + "레벨 " + lastExp + "점) 이상일 때";
                    return "";
                case GearPropType.reqWeekDay:
                    string[] weekdays = new string[this.ConValue.Count];
                    for (int i = 0; i < this.ConValue.Count; i++)
                    {
                        weekdays[i] = GetWeekDayString(this.ConValue[i]);
                    }
                    return string.Join(", ", weekdays) + "일 때";
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
                case 0: return "일요일";
                case 1: return "월요일";
                case 2: return "화요일";
                case 3: return "수요일";
                case 4: return "목요일";
                case 5: return "금요일";
                case 6: return "토요일";
                default: return "周" + weekDay; //这怎么可能...
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
