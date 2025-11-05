using DevComponents.DotNetBar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public partial class FrmSkillTooltipExport : DevComponents.DotNetBar.Office2007Form
    {

        public FrmSkillTooltipExport(bool isMsea = false)
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font(new FontFamily("Microsoft Sans Serif"), 9f);
#endif
            this.isMsea = isMsea;
            this.clbJobName.Items.Add("Other", false);
            jobNameToCode = isMsea ? jobNameToCodeSea : jobNameToCodeGms;
            foreach (var i in jobNameToCode.Keys)
            {
                this.clbJobName.Items.Add(i, false);
            }
        }

        public string ExportFolderPath { get; private set; }
        public List<int> SelectedJobCodes { get; private set; }
        public Wz_Node skillNode { get; set; }
        private bool isMsea;
        private bool sorted = false;

        private static Dictionary<string, int[]> jobNameToCode;

        private static Dictionary<string, int[]> jobNameToCodeGms = new Dictionary<string, int[]>()
        {
            { "Hero", new int[] { 100, 110, 111, 112, 114 } },
            { "Paladin", new int[] { 100, 120, 121, 122, 124 } },
            { "Dark Knight", new int[] { 100, 130, 131, 132, 134 } },
            { "Arch Mage (Fire,Poison)", new int[] { 200, 210, 211, 212, 214 } },
            { "Arch Mage (Ice,Lightning)", new int[] { 200, 220, 221, 222, 224 } },
            { "Bishop", new int[] { 200, 230, 231, 232, 234 } },
            { "Bowmaster", new int[] { 300, 310, 311, 312, 314 } },
            { "Marksman", new int[] { 300, 320, 321, 322, 324 } },
            { "Pathfinder", new int[] { 301, 330, 331, 332, 334 } },
            { "Night Lord", new int[] { 400, 410, 411, 412, 414 } },
            { "Shadower", new int[] { 400, 420, 421, 422, 424 } },
            { "Dual Blade", new int[] { 400, 430, 431, 432, 433, 434, 436 } },
            { "Buccaneer", new int[] { 500, 510, 511, 512, 514 } },
            { "Corsair", new int[] { 500, 520, 521, 522, 524 } },
            { "Cannon Master", new int[] { 501, 530, 531, 532, 534 } },
            { "Jett", new int[] { 508, 570, 571, 572, 574 } },
            { "Dawn Warrior", new int[] { 1000, 1100, 1110, 1111, 1112, 1114 } },
            { "Blaze Wizard", new int[] { 1000, 1200, 1210, 1211, 1212, 1214 } },
            { "Wind Archer", new int[] { 1000, 1300, 1310, 1311, 1312, 1314 } },
            { "Night Walker", new int[] { 1000, 1400, 1410, 1411, 1412, 1414 } },
            { "Thunder Breaker", new int[] { 1000, 1500, 1510, 1511, 1512, 1514 } },
            { "Aran", new int[] { 2000, 2100, 2110, 2111, 2112, 2114 } },
            { "Evan", new int[] { 2001, 2200, 2210, 2211, 2212, 2213, 2214, 2215, 2216, 2217, 2218, 2219, 2220 } },
            { "Mercedes", new int[] { 2002, 2300, 2310, 2311, 2312, 2314 } },
            { "Phantom", new int[] { 2003, 2400, 2410, 2411, 2412, 2414 } },
            { "Luminous", new int[] { 2004, 2700, 2710, 2711, 2712, 2714 } },
            { "Shade", new int[] { 2005, 2500, 2510, 2511, 2512, 2514 } },
            { "Demon Slayer", new int[] { 3001, 3100, 3110, 3111, 3112, 3114 } },
            { "Demon Avenger", new int[] { 3001, 3101, 3120, 3121, 3122, 3124 } },
            { "Blaster", new int[] { 3000, 3700, 3710, 3711, 3712, 3714 } },
            { "Battle Mage", new int[] { 3000, 3200, 3210, 3211, 3212, 3214 } },
            { "Wild Hunter", new int[] { 3000, 3300, 3310, 3311, 3312, 3314 } },
            { "Mechanic", new int[] { 3000, 3500, 3510, 3511, 3512, 3514 } },
            { "Xenon", new int[] { 3002, 3600, 3610, 3611, 3612, 3614 } },
            { "Hayato", new int[] { 4001, 4100, 4110, 4111, 4112, 4114 } },
            { "Kanna", new int[] { 4002, 4200, 4210, 4211, 4212, 4214 } },
            { "Mihile", new int[] { 5000, 5100, 5110, 5111, 5112, 5114 } },
            { "Kaiser", new int[] { 6000, 6100, 6110, 6111, 6112, 6114 } },
            { "Kain", new int[] { 6003, 6300, 6310, 6311, 6312, 6314 } },
            { "Cadena", new int[] { 6002, 6400, 6410, 6411, 6412, 6414 } },
            { "Angelic Buster", new int[] { 6001, 6500, 6510, 6511, 6512, 6514 } },
            // { "Ability", new int[] { 7000 } },
            // { "Legion", new int[] { 7100 } },
            // { "Monster Life", new int[] { 7200 } },
            // { "Guild", new int[] { 9100 } },
            // { "Professional", new int[] { 9200, 9201, 9202, 9203, 9204 } },
            { "Zero", new int[] { 10000, 10100, 10110, 10111, 10112, 10114 } },
            { "Beast Tamer", new int[] { 11000, 11200, 11210, 11211, 11212 } },
            { "Tanjiro Kamado", new int[] { 12000, 12005, 12100 } },
            { "Pink Bean", new int[] { 13000, 13100 } },
            { "Yeti", new int[] { 13001, 13500 } },
            { "Kinesis", new int[] { 14000, 14200, 14210, 14211, 14212, 14214 } },
            { "Adele", new int[] { 15002, 15100, 15110, 15111, 15112, 15114 } },
            { "Illium", new int[] { 15000, 15200, 15210, 15211, 15212, 15214 } },
            { "Khali", new int[] { 15003, 15400, 15410, 15411, 15412, 15414 } },
            { "Ark", new int[] { 15001, 15500, 15510, 15511, 15512, 15514 } },
            { "Ren", new int[] { 16002, 16100, 16110, 16111, 16112, 16114 } },
            { "Lara", new int[] { 16001, 16200, 16210, 16211, 16212, 16214 } },
            { "Hoyoung", new int[] { 16000, 16400, 16410, 16411, 16412, 16414 } },
            { "Mo Xuan", new int[] { 17000, 17500, 17510, 17511, 17512, 17514 } },
            { "Lynn", new int[] { 17001, 17200, 17210, 17211, 17212, 17214 } },
            { "Erel Light", new int[] { 18001, 18100, 18110, 18111, 18112, 18114 } },
            { "Sia Astelle", new int[] { 18000, 18200, 18210, 18211, 18212, 18214 } },
            { "Iel", new int[] { 18002, 18300, 18310, 18311, 18312, 18314 } },
            { "5th (Other)", new int[] { 40000, 40001, 40002, 40003, 40004, 40005 } },
            { "6th (Other)", new int[] { 50000, 50006, 50007 } },
        };

        private static Dictionary<string, int[]> jobNameToCodeSea = new Dictionary<string, int[]>()
        {
            { "Hero", new int[] { 100, 110, 111, 112, 114 } },
            { "Paladin", new int[] { 100, 120, 121, 122, 124 } },
            { "Dark Knight", new int[] { 100, 130, 131, 132, 134 } },
            { "Arch Mage (Fire,Poison)", new int[] { 200, 210, 211, 212, 214 } },
            { "Arch Mage (Ice,Lightning)", new int[] { 200, 220, 221, 222, 224 } },
            { "Bishop", new int[] { 200, 230, 231, 232, 234 } },
            { "Bowmaster", new int[] { 300, 310, 311, 312, 314 } },
            { "Crossbow Master", new int[] { 300, 320, 321, 322, 324 } },
            { "Pathfinder", new int[] { 301, 330, 331, 332, 334 } },
            { "Night Lord", new int[] { 400, 410, 411, 412, 414 } },
            { "Shadower", new int[] { 400, 420, 421, 422, 424 } },
            { "Dual Blade", new int[] { 400, 430, 431, 432, 433, 434, 436 } },
            { "Viper", new int[] { 500, 510, 511, 512, 514 } },
            { "Captain", new int[] { 500, 520, 521, 522, 524 } },
            { "Cannon Master", new int[] { 501, 530, 531, 532, 534 } },
            { "Zen", new int[] { 508, 570, 571, 572, 574 } },
            { "Soul Master", new int[] { 1000, 1100, 1110, 1111, 1112, 1114 } },
            { "Flame Wizard", new int[] { 1000, 1200, 1210, 1211, 1212, 1214 } },
            { "Wind Breaker", new int[] { 1000, 1300, 1310, 1311, 1312, 1314 } },
            { "Night Walker", new int[] { 1000, 1400, 1410, 1411, 1412, 1414 } },
            { "Striker", new int[] { 1000, 1500, 1510, 1511, 1512, 1514 } },
            { "Aran", new int[] { 2000, 2100, 2110, 2111, 2112, 2114 } },
            { "Evan", new int[] { 2001, 2200, 2210, 2211, 2212, 2213, 2214, 2215, 2216, 2217, 2218, 2219, 2220 } },
            { "Mercedes", new int[] { 2002, 2300, 2310, 2311, 2312, 2314 } },
            { "Phantom", new int[] { 2003, 2400, 2410, 2411, 2412, 2414 } },
            { "Luminous", new int[] { 2004, 2700, 2710, 2711, 2712, 2714 } },
            { "Eunwol", new int[] { 2005, 2500, 2510, 2511, 2512, 2514 } },
            { "Demon Slayer", new int[] { 3001, 3100, 3110, 3111, 3112, 3114 } },
            { "Demon Avenger", new int[] { 3001, 3101, 3120, 3121, 3122, 3124 } },
            { "Blaster", new int[] { 3000, 3700, 3710, 3711, 3712, 3714 } },
            { "Battle Mage", new int[] { 3000, 3200, 3210, 3211, 3212, 3214 } },
            { "Wild Hunter", new int[] { 3000, 3300, 3310, 3311, 3312, 3314 } },
            { "Mechanic", new int[] { 3000, 3500, 3510, 3511, 3512, 3514 } },
            { "Xenon", new int[] { 3002, 3600, 3610, 3611, 3612, 3614 } },
            { "Hayato", new int[] { 4001, 4100, 4110, 4111, 4112, 4114 } },
            { "Kanna", new int[] { 4002, 4200, 4210, 4211, 4212, 4214 } },
            { "Mihile", new int[] { 5000, 5100, 5110, 5111, 5112, 5114 } },
            { "Kaiser", new int[] { 6000, 6100, 6110, 6111, 6112, 6114 } },
            { "Kaine", new int[] { 6003, 6300, 6310, 6311, 6312, 6314 } },
            { "Cadena", new int[] { 6002, 6400, 6410, 6411, 6412, 6414 } },
            { "Angelic Buster", new int[] { 6001, 6500, 6510, 6511, 6512, 6514 } },
            // { "Ability", new int[] { 7000 } },
            // { "Union", new int[] { 7100 } },
            // { "Monster Life", new int[] { 7200 } },
            // { "Guild", new int[] { 9100 } },
            // { "Professional", new int[] { 9200, 9201, 9202, 9203, 9204 } },
            { "Zero", new int[] { 10000, 10100, 10110, 10111, 10112, 10114 } },
            { "Beast Tamer", new int[] { 11000, 11200, 11210, 11211, 11212 } },
            { "Tanjiro Kamado", new int[] { 12000, 12005, 12100 } },
            { "Pink Bean", new int[] { 13000, 13100 } },
            { "Yeti", new int[] { 13001, 13500 } },
            { "Kinesis", new int[] { 14000, 14200, 14210, 14211, 14212, 14214 } },
            { "Adele", new int[] { 15002, 15100, 15110, 15111, 15112, 15114 } },
            { "Illium", new int[] { 15000, 15200, 15210, 15211, 15212, 15214 } },
            { "Khali", new int[] { 15003, 15400, 15410, 15411, 15412, 15414 } },
            { "Ark", new int[] { 15001, 15500, 15510, 15511, 15512, 15514 } },
            { "Len", new int[] { 16002, 16100, 16110, 16111, 16112, 16114 } },
            { "Lara", new int[] { 16001, 16200, 16210, 16211, 16212, 16214 } },
            { "Ho Young", new int[] { 16000, 16400, 16410, 16411, 16412, 16414 } },
            { "Mo Xuan", new int[] { 17000, 17500, 17510, 17511, 17512, 17514 } },
            { "Lynn", new int[] { 17001, 17200, 17210, 17211, 17212, 17214 } },
            { "Erel Light", new int[] { 18001, 18100, 18110, 18111, 18112, 18114 } },
            { "Sia Astelle", new int[] { 18000, 18200, 18210, 18211, 18212, 18214 } },
            { "Iel", new int[] { 18002, 18300, 18310, 18311, 18312, 18314 } },
            { "5th (Other)", new int[] { 40000, 40001, 40002, 40003, 40004, 40005 } },
            { "6th (Other)", new int[] { 50000, 50006, 50007 } },
        };
        private static HashSet<int> AllClassesCode()
        {
            HashSet<int> hsClassCode = new HashSet<int>() { };
            foreach (var i in jobNameToCode.Values)
            {
                foreach (var j in i)
                {
                    hsClassCode.Add(j);
                }
            }
            return hsClassCode;
        }

        private void btnSort_Click(object sender, EventArgs e)
        {
            this.sorted = !this.sorted;
            this.btnSort.Text = this.sorted ? "Alphabetical" : "Default Order";
            this.clbJobName.Items.Clear();
            this.clbJobName.Items.Add("Other", this.clbJobName.CheckedItems.Contains("Other"));
            var jobNameToCodeSorted = jobNameToCode.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
            var sourceDict = this.sorted ? jobNameToCodeSorted : jobNameToCode;
            foreach (var i in sourceDict.Keys)
            {
                this.clbJobName.Items.Add(i, this.clbJobName.CheckedItems.Contains(i));
            }
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            int checkedCount = this.clbJobName.CheckedItems.Contains("Other") ? this.clbJobName.CheckedItems.Count - 1 : this.clbJobName.CheckedItems.Count;
            bool checkedStatus = (checkedCount < this.clbJobName.Items.Count - 1);
            for (int i = 1; i < this.clbJobName.Items.Count; i++)
            {
                this.clbJobName.SetItemChecked(i, checkedStatus);
            }
        }

        private void btnReverseSelect_Click(object sender, EventArgs e)
        {
            for (int i = 1; i < this.clbJobName.Items.Count; i++)
            {
                this.clbJobName.SetItemChecked(i, !this.clbJobName.GetItemChecked(i));
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (this.clbJobName.CheckedItems.Count == 0)
            {
                MessageBoxEx.Show("Please select at least one job to export.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "Select a destination folder to save tooltips.";;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                bool allSelected = this.clbJobName.CheckedItems.Count == this.clbJobName.Items.Count;

                List<int> skillImg = new List<int>() { };
                foreach (Wz_Node node in skillNode.Nodes)
                {
                    Wz_Image currentImg = node.GetValue<Wz_Image>();
                    if (currentImg != null && Int32.TryParse(currentImg.Name.Replace(".img", ""), out int jobCode))
                    {
                        skillImg.Add(jobCode);
                    }
                }
                List<int> selectedJob = selectedJob = skillImg.Intersect(this.clbJobName.CheckedItems.Cast<string>().SelectMany(name => jobNameToCode.ContainsKey(name) ? jobNameToCode[name] : new int[] { }).ToList()).ToList();
                if (this.clbJobName.CheckedItems.Contains("Other"))
                {
                    selectedJob.AddRange(skillImg.Except(AllClassesCode()));
                }
                ExportFolderPath = dlg.SelectedPath;
                SelectedJobCodes = allSelected ? skillImg : selectedJob;
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}
