using System;
using System.Collections.Generic;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Unity データセットのサウンド img（<c>Sound/&lt;img&gt;</c>）を WZ の
    /// <c>Sound/&lt;img&gt;.img</c> 形式で表現します: 内容はネストしたディレクトリ + <see cref="Wz_Sound"/> の葉ノードで、
    /// 従来の WZ の配置と一致します。
    /// <para>
    /// サウンドデータ（FSB5）は初回使用時に Ogg Vorbis へ再パッケージし、
    /// そのため <see cref="Wz_Sound.ExtractSound"/>、再生、エクスポートでそのまま使用できます。
    /// </para>
    /// </summary>
    public sealed class UnitySoundImage : UnityAssetImage
    {
        public UnitySoundImage(ClassicWzSource source, string imageKey)
            : base(source, imageKey)
        {
        }

        /// <summary>WZ 形式のパスで同じデータセット内のノードを検索します（<c>.img</c> 拡張子は任意）。</summary>
        public Wz_Node FindNode(string wzPath)
        {
            return this.Source.FindNode(wzPath);
        }

        protected override void PopulateContent()
        {
            IReadOnlyList<string> soundKeys = this.Source.Catalog.GetSoundKeys(this.Name);
            string prefixLength = this.Name + "/";
            foreach (string soundKey in soundKeys)
            {
                if (!soundKey.StartsWith(prefixLength, StringComparison.Ordinal))
                {
                    continue;
                }

                string[] segments = soundKey.Substring(prefixLength.Length).Split('/');
                Wz_Node parent = this.Node;
                for (int i = 0; i < segments.Length - 1; i++)
                {
                    parent = GetOrAddFolder(parent, segments[i]);
                }

                string name = segments[segments.Length - 1];
                if (parent.Nodes[name] == null)
                {
                    Wz_Node soundNode = new Wz_Node(name);
                    soundNode.Value = this.Source.CreateSound(soundKey);
                    parent.Nodes.Add(soundNode);
                }
            }
        }

        private static Wz_Node GetOrAddFolder(Wz_Node parent, string name)
        {
            Wz_Node folder = parent.Nodes[name];
            if (folder == null)
            {
                folder = new Wz_Node(name);
                parent.Nodes.Add(folder);
            }
            return folder;
        }
    }
}
