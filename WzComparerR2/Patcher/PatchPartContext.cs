using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib;

namespace WzComparerR2.Patcher
{
    public class PatchPartContext
    {
        public PatchPartContext(string fileName, long offset, int type)
        {
            this.Offset = offset;
            this.FileName = fileName;
            this.Type = type;

            Match m = Regex.Match(fileName, @"^([A-Za-z]+)\d*(?:\.wz)?$");
            if (m.Success)
            {
                fileName = m.Result("$1");
            }
            m = Regex.Match(fileName, @"^Data\\([A-Za-z]+)\\.*(?:\.wz)$");
            if (m.Success)
            {
                fileName = m.Result("$1");
            }

            try
            {
                this.WzType = (Wz_Type)Enum.Parse(typeof(Wz_Type), fileName, true);
            }
            catch
            {
                this.WzType = Wz_Type.Unknown;
            }
        }

        private readonly HashSet<string> dependencyFiles = new HashSet<string>();

        public long Offset { get; set; }
        public string FileName { get; private set; }
        public int Type { get; private set; }
        public Wz_Type WzType { get; private set; }
        public int? OldFileLength { get; set; }
        public int NewFileLength { get; set; }
        public uint? OldChecksum { get; set; }
        public uint? OldChecksumActual { get; set; }
        public uint NewChecksum { get; set; }
        public string TempFilePath { get; set; }
        public string OldFilePath { get; set; }
        public int Action0 { get; set; }
        public int Action1 { get; set; }
        public int Action2 { get; set; }
        public ISet<string> DependencyFiles { get; private set; } = new HashSet<string>();
    }
}
