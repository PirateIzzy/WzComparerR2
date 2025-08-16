using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Achievement
    {
        public Achievement()
        {
            this.ID = -1;
            this.PriorIDs = new List<int>();
            this.Missions = new List<string>();
            this.Rewards = new List<AchievementReward>();
        }

        private string _mainCategory { get; set; }
        private string _subCategory { get; set; }
        public int ID { get; set; }
        public int Score { get; set; }
        public string MainCategory
        {
            get { return GetMainCategoryStr(); }
        }
        public string SubCategory
        {
            get { return GetSubCategoryStr(); }
        }
        public string Difficulty { get; set; }
        public string UiForm { get; set; }
        public string Block { get; set; }
        public string PriorCondition { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public List<int> PriorIDs { get; set; }
        public List<string> Missions { get; set; }
        public List<AchievementReward> Rewards { get; set; }

        public bool ShowMissions
        {
            get
            {
                return (this.UiForm == "mission" || this.UiForm == "all")
                    && this.Missions.Count > 0;
            }
        }
        public bool HasRewards
        {
            get { return this.Rewards.Count > 0; }
        }
        public bool Hide
        {
            get { return this.Block == "hide"; }
        }

        public static Achievement CreateFromNode(
            Wz_Node node,
            GlobalFindNodeFunction findNode,
            GlobalFindNodeFunction2 findNode2,
            Wz_File wzf = null
        )
        {
            if (node == null)
                return null;

            Match m = Regex.Match(node.Text, @"^(\d+)\.img$");
            if (!(m.Success && Int32.TryParse(m.Result("$1"), out int achievementID)))
            {
                return null;
            }

            Achievement achievement = new Achievement();
            achievement.ID = achievementID;
            Wz_Node infoNode = node.FindNodeByPath("info").ResolveUol();
            if (infoNode != null)
            {
                foreach (var propNode in infoNode.Nodes)
                {
                    switch (propNode.Text)
                    {
                        case "score":
                            achievement.Score = propNode.GetValueEx<int>(0);
                            break;
                        case "mainCategory":
                            achievement._mainCategory = propNode.GetValueEx<string>(null);
                            break;
                        case "subCategory":
                            achievement._subCategory = propNode.GetValueEx<string>(null);
                            break;
                        case "difficulty":
                            achievement.Difficulty = propNode.GetValueEx<string>("normal");
                            break;
                        case "prior":
                            var prior = propNode.FindNodeByPath("achievement_id");
                            if (prior != null)
                                achievement.PriorIDs.Add(prior.GetValueEx<int>(-1));
                            else
                            {
                                var valueNode = propNode.FindNodeByPath("values");
                                foreach (
                                    var value in valueNode?.Nodes
                                        ?? new Wz_Node.WzNodeCollection(null)
                                )
                                {
                                    prior = value.FindNodeByPath("achievement_id");
                                    var priorID = prior.GetValueEx<int>(-1);
                                    if (priorID > -1)
                                    {
                                        achievement.PriorIDs.Add(prior.GetValueEx<int>(priorID));
                                    }
                                }
                            }
                            achievement.PriorCondition = propNode
                                .FindNodeByPath("condition")
                                .GetValueEx<string>(null);
                            break;
                        case "uiType":
                            achievement.UiForm = propNode
                                .FindNodeByPath("uiForm")
                                .GetValueEx<string>("basic");
                            break;
                        case "block":
                            achievement.Block = propNode.GetValueEx<string>("none");
                            break;
                        case "period":
                            achievement.Start = propNode
                                .FindNodeByPath("start")
                                .GetValueEx<string>(null);
                            achievement.End = propNode
                                .FindNodeByPath("end")
                                .GetValueEx<string>(null);
                            break;
                    }
                }
            }

            Wz_Node missionNode = node.FindNodeByPath("mission").ResolveUol();
            if (missionNode != null)
            {
                foreach (var mission in missionNode.Nodes)
                {
                    var missionName = mission.FindNodeByPath("name").GetValueEx<string>(null);
                    if (!string.IsNullOrEmpty(missionName))
                    {
                        achievement.Missions.Add(missionName);
                    }
                }
            }

            Wz_Node rewardNode = node.FindNodeByPath("reward").ResolveUol();
            if (rewardNode != null)
            {
                foreach (var reward in rewardNode.Nodes)
                {
                    var id = reward.FindNodeByPath("id").GetValueEx<int>(-1);
                    var desc = reward.FindNodeByPath("desc").GetValueEx<string>(null);
                    if (id >= 0 && !string.IsNullOrEmpty(desc))
                    {
                        achievement.Rewards.Add(new AchievementReward() { ID = id, Desc = desc });
                    }
                }
            }

            return achievement;
        }

        private string GetMainCategoryStr()
        {
            // Etc/Achievement/AchievementInfo.img/Category
            switch (this._mainCategory)
            {
                case "general":
                    return "General";
                case "growth":
                    return "Level Up";
                case "job":
                    return "Job";
                case "item":
                    return "Item";
                case "adventure":
                    return "Adventure";
                case "battle":
                    return "Battle";
                case "social":
                    return "Social";
                case "event":
                    return "Event";
                case "memory":
                    return "Memory";

                default:
                    return this._mainCategory;
            }
        }

        private string GetSubCategoryStr()
        {
            // Etc/Achievement/AchievementInfo.img/Category
            switch (this._mainCategory)
            {
                case "general":
                case "event":
                case "memory":
                    return null;
            }

            switch (this._subCategory)
            {
                case "level":
                    return "Level";
                case "stat":
                    return "Stats";
                case "personality":
                    return "Traits";
                case "makingSkill":
                    return "Profession";
                case "union":
                    return "Legion";

                case "story":
                    return "Story";
                case "jobChange":
                    return "Job Advancement";
                case "skill":
                    return "Skills";
                case "vMatrix":
                    return "V Matrix";
                case "linkSkill":
                    return "Link Skill";

                case "collection":
                    return "Collect";
                case "enchantment":
                    return "Boost";
                case "equip":
                    return "Equipped";

                case "exploration":
                    return "Exploration";
                case "quest":
                    return "Quest";
                case "cooperation":
                    return "Cooperation";
                case "special":
                    return "Special";

                case "field":
                    return "Field";
                case "boss":
                    return "Boss";
                case "loot":
                    return "Drop Item";

                case "party":
                    return "Party";
                case "guild":
                    return "Guild";
                case "trade":
                    return "Trade";
                case "etc":
                    return "Other";

                case "progress":
                    return "Ongoing Events";
                case "complete":
                    return "Past Events";

                default:
                    return this._subCategory;
            }
        }

        public struct AchievementReward
        {
            public int ID;
            public string Desc;
        }
    }
}
