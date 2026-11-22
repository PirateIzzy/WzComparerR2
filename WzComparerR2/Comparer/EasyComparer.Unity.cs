using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using WzComparerR2.UnityLib.Classic;
using WzComparerR2.UnityLib.Unity;
using WzComparerR2.UnityLib.Wzjs;
using WzComparerR2.WzLib;

namespace WzComparerR2.Comparer
{
    public partial class EasyComparer
    {
        /// <summary>
        /// Unity アセットデータセット同士の比較。両側をまず「WZ 風」のディレクトリツリーに整理し、
        /// （<see cref="ClassicWzBridge.CreateCatalogNode(ClassicWzSource, bool, bool)"/>）、
        /// さらに <see cref="Wz_Type"/> ごとにページ出力し、結果の形式は WZ 比較と一致します。
        /// </summary>
        /// <param name="labelNew">新しいバージョンの表示名（例: <c>冒険島懐旧服 1.1.2</c>）。</param>
        /// <param name="labelOld">古いバージョンの表示名。</param>
        public void EasyCompareUnity(
            string labelNew, ClassicWzSource sourceNew,
            string labelOld, ClassicWzSource sourceOld,
            string outputDir, StreamWriter index = null)
        {
            if (sourceNew == null)
            {
                throw new ArgumentNullException(nameof(sourceNew));
            }
            if (sourceOld == null)
            {
                throw new ArgumentNullException(nameof(sourceOld));
            }

            this.Comparer = new UnityFileComparer
            {
                PngComparison = this.Comparer.PngComparison,
                ResolvePngLink = false,
                IgnoreWzFile = true,
            };

            StateInfo = "Comparing...";
            StateDetail = "Loading the structure of the new version...";

            CreateStyleSheet(outputDir);

            UnityCompareSide sideNew = new UnityCompareSide(
                sourceNew, ClassicWzBridge.CreateCatalogNode(sourceNew, false, false));
            StateDetail = "Loading the structure of the old version...";
            UnityCompareSide sideOld = new UnityCompareSide(
                sourceOld, ClassicWzBridge.CreateCatalogNode(sourceOld, false, false));

            Dictionary<Wz_Type, Dictionary<string, Wz_Node>> indexNew = BuildUnityTypeIndex(sideNew.Root);
            Dictionary<Wz_Type, Dictionary<string, Wz_Node>> indexOld = BuildUnityTypeIndex(sideOld.Root);

            List<Wz_Type> typeList = indexNew.Keys.Union(indexOld.Keys)
                .OrderBy(type => type.ToString(), StringComparer.Ordinal)
                .ToList();

            // ツールチップ出力では Wz_File の代わりに、比較対象のデータセットを解決先として指定します。
            this.WzNewOld[0] = sideNew.Root;
            this.WzNewOld[1] = sideOld.Root;
            BeginUnityTooltipResolve(sideNew, sideOld);
            try
            {
                this.InitializeUnityTooltipSources();

                for (int typeIndex = 0; typeIndex < typeList.Count; typeIndex++)
                {
                    Wz_Type type = typeList[typeIndex];
                    bool selected;
                    if (this.selectedNodes != null
                        && this.selectedNodes.TryGetValue(type.ToString(), out selected)
                        && !selected)
                    {
                        continue;
                    }

                    // 型別ごとの走査は時間がかかるため、こまめに進捗を出す。
                    this.unityCompareCount = 0;
                    this.unityCompareNotify = 0;
                    StateDetail = string.Format("Comparing {0}... ({1}/{2})", type, typeIndex + 1, typeList.Count);

                    Dictionary<string, Wz_Node> dirsNew, dirsOld;
                    indexNew.TryGetValue(type, out dirsNew);
                    indexOld.TryGetValue(type, out dirsOld);

                    List<UnityCompareEntry> entries = new List<UnityCompareEntry>();
                    foreach (UnityDirectoryPair pair in MatchUnityDirectories(dirsNew, dirsOld))
                    {
                        if (pair.NodeNew != null && pair.NodeOld != null)
                        {
                            // 単一アセットのルート（MonsterBook など）にも対応するため、階層を判定して比較します。
                            CompareUnityNode(sideNew, pair.NodeNew, sideOld, pair.NodeOld, pair.Name, entries);
                        }
                        else if (pair.NodeNew != null)
                        {
                            AddUnityEntryTree(sideNew, pair.NodeNew, pair.Name, DifferenceType.Append, entries);
                        }
                        else
                        {
                            AddUnityEntryTree(sideOld, pair.NodeOld, pair.Name, DifferenceType.Remove, entries);
                        }
                    }

                    OutputUnityFile(labelNew, labelOld, type, entries, outputDir, index);
                }

                // Outputs tooltip images from the collected IDs (same file layout as the WZ comparison).
                StateDetail = "Outputting the changed parts to tooltip images...";
                this.SaveTooltips("String", outputDir);
                this.SaveTooltips("Quest", outputDir);
            }
            finally
            {
                EndUnityTooltipResolve();
                if (this.AnyTooltipOutput)
                {
                    // 比較用に読み込んだ補助データは破棄し、ノード閲覧時に開いているデータセットから読み直させます。
                    CharaSimLoader.ClearAll();
                }
            }

            StateInfo = "Comparison complete";
            StateDetail = "";
            GC.Collect();
        }

        private int unityCompareCount;
        private int unityCompareNotify;
        private int unityNodeTick;
        private string unityEntryPath;
        private Wz_Node unityContentNew;
        private Wz_Node unityContentOld;

        /// <summary>Shows the scan progress at fixed intervals (to limit the number of notifications to the UI thread).</summary>
        private void ReportUnityProgress(string path)
        {
            this.unityCompareCount++;
            if (this.unityCompareCount - this.unityCompareNotify < 16)
            {
                return;
            }
            this.unityCompareNotify = this.unityCompareCount;
            this.StateInfo = string.Format("Comparing... {0} ({1})", path, this.unityCompareCount);
        }

        /// <summary>
        /// Displays the node path being compared (same granularity as the WZ comparison). Values are thinned out because there can be many nodes.
        /// </summary>
        private void ReportUnityNodeProgress(Wz_Node nodeNew, Wz_Node nodeOld)
        {
            int now = Environment.TickCount;
            if (now - this.unityNodeTick < 100)
            {
                return;
            }
            this.unityNodeTick = now;

            string path = this.GetUnityComparedPath(nodeNew, this.unityContentNew);
            if (path == null)
            {
                path = this.GetUnityComparedPath(nodeOld, this.unityContentOld);
            }
            if (path == null)
            {
                return;
            }

            this.StateInfo = string.Format("Comparing... {0} ({1})", path, this.unityCompareCount + 1);
        }

        private string GetUnityComparedPath(Wz_Node node, Wz_Node contentRoot)
        {
            if (node == null || contentRoot == null || this.unityEntryPath == null)
            {
                return null;
            }
            string relative = GetUnityRelativePath(node, contentRoot);
            return string.IsNullOrEmpty(relative)
                ? this.unityEntryPath
                : CombineUnityPath(this.unityEntryPath, relative);
        }

        #region ノード走査と差分判定

        /// <summary>ディレクトリ名で両側の型ごとのトップディレクトリを対応付けます。</summary>
        private static IEnumerable<UnityDirectoryPair> MatchUnityDirectories(
            Dictionary<string, Wz_Node> dirsNew, Dictionary<string, Wz_Node> dirsOld)
        {
            if (dirsNew != null)
            {
                foreach (KeyValuePair<string, Wz_Node> item in dirsNew.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                {
                    Wz_Node nodeOld = null;
                    if (dirsOld != null)
                    {
                        dirsOld.TryGetValue(item.Key, out nodeOld);
                    }
                    yield return new UnityDirectoryPair(item.Key, item.Value, nodeOld);
                }
            }

            if (dirsOld != null)
            {
                foreach (KeyValuePair<string, Wz_Node> item in dirsOld.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                {
                    if (dirsNew != null && dirsNew.ContainsKey(item.Key))
                    {
                        continue;
                    }
                    yield return new UnityDirectoryPair(item.Key, null, item.Value);
                }
            }
        }

        /// <summary>
        /// <paramref name="root"/> のトップディレクトリを <see cref="Wz_Type"/> ごとに分類します。
        /// 従来の WZ に対応する型が無いディレクトリ（<c>MonsterBook</c> など）は <see cref="Wz_Type.Base"/> として扱います。
        /// </summary>
        private static Dictionary<Wz_Type, Dictionary<string, Wz_Node>> BuildUnityTypeIndex(Wz_Node root)
        {
            Dictionary<Wz_Type, Dictionary<string, Wz_Node>> index =
                new Dictionary<Wz_Type, Dictionary<string, Wz_Node>>();
            foreach (Wz_Node child in root.Nodes)
            {
                Wz_Type type = ClassicWzTypeMap.GetWzTypeOrBase(child.Text);

                Dictionary<string, Wz_Node> dirs;
                if (!index.TryGetValue(type, out dirs))
                {
                    dirs = new Dictionary<string, Wz_Node>(StringComparer.Ordinal);
                    index[type] = dirs;
                }
                dirs[child.Text] = child;
            }
            return index;
        }

        private void CompareUnityDirectory(
            UnityCompareSide sideNew, Wz_Node dirNew,
            UnityCompareSide sideOld, Wz_Node dirOld,
            string prefix, List<UnityCompareEntry> entries)
        {
            Dictionary<string, Wz_Node> oldChildren = new Dictionary<string, Wz_Node>(StringComparer.Ordinal);
            foreach (Wz_Node child in dirOld.Nodes)
            {
                oldChildren[child.Text] = child;
            }

            HashSet<string> newNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (Wz_Node childNew in SortUnityNodes(dirNew.Nodes))
            {
                newNames.Add(childNew.Text);
                string path = CombineUnityPath(prefix, childNew.Text);

                Wz_Node childOld;
                if (!oldChildren.TryGetValue(childNew.Text, out childOld))
                {
                    AddUnityEntryTree(sideNew, childNew, path, DifferenceType.Append, entries);
                    continue;
                }
                CompareUnityNode(sideNew, childNew, sideOld, childOld, path, entries);
            }

            foreach (Wz_Node childOld in SortUnityNodes(dirOld.Nodes))
            {
                if (newNames.Contains(childOld.Text))
                {
                    continue;
                }
                AddUnityEntryTree(sideOld, childOld, CombineUnityPath(prefix, childOld.Text), DifferenceType.Remove, entries);
            }
        }

        private void CompareUnityNode(
            UnityCompareSide sideNew, Wz_Node nodeNew,
            UnityCompareSide sideOld, Wz_Node nodeOld,
            string path, List<UnityCompareEntry> entries)
        {
            bool isDirNew = nodeNew.Value == null && nodeNew.Nodes.Count > 0;
            bool isDirOld = nodeOld.Value == null && nodeOld.Nodes.Count > 0;

            if (isDirNew && isDirOld)
            {
                CompareUnityDirectory(sideNew, nodeNew, sideOld, nodeOld, path, entries);
                return;
            }
            if (isDirNew)
            {
                AddUnityEntryTree(sideNew, nodeNew, path, DifferenceType.Append, entries);
                return;
            }
            if (isDirOld)
            {
                AddUnityEntryTree(sideOld, nodeOld, path, DifferenceType.Remove, entries);
                return;
            }

            string displayPath = GetUnityDisplayPath(path, nodeNew);
            UnityEntryChange change;
            this.unityEntryPath = displayPath;
            try
            {
                change = CompareUnityEntry(sideNew, nodeNew, sideOld, nodeOld);
            }
            catch (Exception ex)
            {
                // Continue the whole comparison even when some assets cannot be parsed.
                this.FailToExportNodes[displayPath] = ex.Message;
                change = UnityEntryChange.Value;
            }
            finally
            {
                this.unityEntryPath = null;
                this.unityContentNew = null;
                this.unityContentOld = null;
            }

            ReportUnityProgress(displayPath);
            if (change == UnityEntryChange.None)
            {
                return;
            }

            entries.Add(new UnityCompareEntry
            {
                DifferenceType = DifferenceType.Changed,
                SideNew = sideNew,
                SideOld = sideOld,
                NodeNew = nodeNew,
                NodeOld = nodeOld,
                DisplayPath = displayPath,
                ValueOnly = change == UnityEntryChange.Value,
            });
        }

        /// <summary>サブツリー全体の葉ノードを差分一覧に追加します（片側にしか存在しないアセット用）。</summary>
        private void AddUnityEntryTree(
            UnityCompareSide side, Wz_Node node, string path,
            DifferenceType type, List<UnityCompareEntry> entries)
        {
            if (node.Value == null && node.Nodes.Count > 0)
            {
                foreach (Wz_Node child in SortUnityNodes(node.Nodes))
                {
                    AddUnityEntryTree(side, child, CombineUnityPath(path, child.Text), type, entries);
                }
                return;
            }

            ReportUnityProgress(path);
            entries.Add(new UnityCompareEntry
            {
                DifferenceType = type,
                SideNew = type == DifferenceType.Remove ? null : side,
                SideOld = type == DifferenceType.Remove ? side : null,
                NodeNew = type == DifferenceType.Remove ? null : node,
                NodeOld = type == DifferenceType.Remove ? node : null,
                DisplayPath = GetUnityDisplayPath(path, node),
            });
        }

        /// <summary>単一アセットの内容に差分があるか判定します。</summary>
        private UnityEntryChange CompareUnityEntry(
            UnityCompareSide sideNew, Wz_Node nodeNew,
            UnityCompareSide sideOld, Wz_Node nodeOld)
        {
            // アセットの生データが同一なら、展開せずに「変更なし」と判定します。
            // 似たバージョン同士の比較ではほとんどのアセットが該当し、JSON／WZJS の解析を丸ごと省略できます。
            if (UnityEntryPayloadEquals(sideNew, nodeNew, sideOld, nodeOld))
            {
                return UnityEntryChange.None;
            }

            Wz_Image imageNew, imageOld;
            bool ownedNew, ownedOld;
            Wz_Node contentNew = BuildUnityContent(sideNew, nodeNew, out imageNew, out ownedNew);
            Wz_Node contentOld = BuildUnityContent(sideOld, nodeOld, out imageOld, out ownedOld);
            try
            {
                bool hasContentNew = HasUnityContent(contentNew, imageNew, nodeNew);
                bool hasContentOld = HasUnityContent(contentOld, imageOld, nodeOld);
                if (!hasContentNew && !hasContentOld)
                {
                    // sprite canvas など子ノードを持たないアセット: 値を直接比較します。
                    return this.Comparer.CompareData(nodeNew.Value, nodeOld.Value)
                        ? UnityEntryChange.None
                        : UnityEntryChange.Value;
                }
                if (hasContentNew != hasContentOld || contentNew == null || contentOld == null)
                {
                    return UnityEntryChange.Value;
                }

                return this.CompareUnityContent(contentNew, contentOld)
                    ? UnityEntryChange.Content
                    : UnityEntryChange.None;
            }
            finally
            {
                if (ownedNew && imageNew != null)
                {
                    imageNew.Unextract();
                }
                if (ownedOld && imageOld != null)
                {
                    imageOld.Unextract();
                }
            }
        }

        /// <summary>
        /// 両側のアセットの生データ（bundle 内のシリアライズ済みバイト列）が同一かどうかを判定します。
        /// 内容の展開が必要なアセットのみ対象で、同一なら展開後のノードも同一とみなせます。
        /// </summary>
        private static bool UnityEntryPayloadEquals(
            UnityCompareSide sideNew, Wz_Node nodeNew,
            UnityCompareSide sideOld, Wz_Node nodeOld)
        {
            if (nodeNew == null || nodeOld == null
                || !IsUnityContentAsset(nodeNew.Value) || !IsUnityContentAsset(nodeOld.Value))
            {
                return false;
            }
            string keyNew = sideNew.GetImageKey(nodeNew);
            string keyOld = sideOld.GetImageKey(nodeOld);
            if (keyNew == null || keyOld == null)
            {
                return false;
            }
            byte[] payloadNew = ReadUnityEntryPayload(sideNew.Source, keyNew);
            if (payloadNew == null)
            {
                return false;
            }
            byte[] payloadOld = ReadUnityEntryPayload(sideOld.Source, keyOld);
            if (payloadOld == null || payloadNew.Length != payloadOld.Length)
            {
                return false;
            }
            int length = payloadNew.Length;
            int offset = 0;
            for (; offset + sizeof(long) <= length; offset += sizeof(long))
            {
                if (BitConverter.ToInt64(payloadNew, offset) != BitConverter.ToInt64(payloadOld, offset))
                {
                    return false;
                }
            }
            for (; offset < length; offset++)
            {
                if (payloadNew[offset] != payloadOld[offset])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 生データ（bundle 内のシリアライズ済みバイト列）だけで内容が確定するアセットかどうか。
        /// sprite（<see cref="UnityCanvas"/>）は画素データを別アセットから解決するため対象外です。
        /// </summary>
        private static bool IsUnityContentAsset(object value)
        {
            return value is Wz_Image || value is UnityJsonAsset;
        }

        private static byte[] ReadUnityEntryPayload(ClassicWzSource source, string imageKey)
        {
            if (source == null || !source.Catalog.Entries.TryGetValue(imageKey, out var location))
            {
                return null;
            }
            try
            {
                return source.ReadEntry(location);
            }
            catch (Exception)
            {
                // 読み出せないアセットは通常の比較にまわします。
                return null;
            }
        }

        private static bool HasUnityContent(Wz_Node content, Wz_Image image, Wz_Node entry)
        {
            if (entry.Value is Wz_Image)
            {
                // img の展開に失敗した場合（content が null）は内容なしとみなし、値の比較で処理します。
                return content != null;
            }
            return content != null && content.Nodes.Count > 0;
        }

        /// <summary>
        /// Content comparison: stop as soon as the first difference is found.
        /// Temporarily subscribes to the comparison hook so the node path being compared can be displayed.
        /// </summary>
        private bool CompareUnityContent(Wz_Node contentNew, Wz_Node contentOld)
        {
            this.unityContentNew = contentNew;
            this.unityContentOld = contentOld;
            this.Comparer.ComparingNode += this.ReportUnityNodeProgress;
            try
            {
                foreach (CompareDifference diff in this.Comparer.Compare(contentNew, contentOld))
                {
                    return true;
                }
                return false;
            }
            finally
            {
                this.Comparer.ComparingNode -= this.ReportUnityNodeProgress;
            }
        }

        /// <summary>
        /// アセット内容のノードコンテナを取得します: img（WZJS／サウンド）はそのまま展開し、テキストアセットはノードとして解析し、
        /// それ以外のアセットは空のコンテナを返します。
        /// </summary>
        private static Wz_Node BuildUnityContent(
            UnityCompareSide side, Wz_Node entry, out Wz_Image image, out bool ownedByUs)
        {
            image = entry.Value as Wz_Image;
            ownedByUs = false;
            if (image != null)
            {
                // Do not dispose images that are already expanded (in use by the tree view, etc.).
                UnityAssetImage unityImage = image as UnityAssetImage;
                ownedByUs = unityImage == null || !unityImage.IsExtracted;
                return image.TryExtract() ? image.Node : null;
            }

            Wz_Node holder = new Wz_Node(entry.Text);
            if (entry.Value != null)
            {
                string imageKey = side.GetImageKey(entry);
                if (imageKey != null)
                {
                    try
                    {
                        ClassicWzBridge.AppendEntryContents(holder, side.Source, imageKey);
                    }
                    catch (Exception)
                    {
                        // 解析できないアセットは内容なしとみなし、差分は値の比較で判定します。
                        holder.Nodes.Clear();
                    }
                }
            }
            return holder;
        }

        private static IEnumerable<Wz_Node> SortUnityNodes(IEnumerable<Wz_Node> nodes)
        {
            return nodes.OrderBy(node => node.Text, new UnityNodeNameComparer());
        }

        private sealed class UnityNodeNameComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return ClassicCatalog.CompareNodeNames(x, y);
            }
        }

        /// <summary>ディレクトリパスとノード名から WZ 形式のパスを組み立てます（img には <c>.img</c> を付加）。</summary>
        private static string GetUnityDisplayPath(string path, Wz_Node node)
        {
            return node != null && node.Value is Wz_Image ? path + ".img" : path;
        }

        private static string CombineUnityPath(string prefix, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return prefix;
            }
            return string.IsNullOrEmpty(prefix) ? name : prefix + "\\" + name;
        }

        #endregion

        #region HTML 出力

        private void OutputUnityFile(
            string labelNew, string labelOld, Wz_Type type,
            List<UnityCompareEntry> entries, string outputDir, StreamWriter index)
        {
            string htmlFilePath = Path.Combine(outputDir, type.ToString() + ".html");
            for (int i = 1; File.Exists(htmlFilePath); i++)
            {
                htmlFilePath = Path.Combine(outputDir, string.Format("{0}_{1}.html", type, i));
            }
            string srcDirPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(htmlFilePath) + "_files");
            if (this.OutputPng && !Directory.Exists(srcDirPath))
            {
                Directory.CreateDirectory(srcDirPath);
            }

            string fileName = Path.GetFileName(htmlFilePath);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(htmlFilePath);
            int changed = 0, added = 0, removed = 0;
            foreach (UnityCompareEntry entry in entries)
            {
                switch (entry.DifferenceType)
                {
                    case DifferenceType.Changed: changed++; break;
                    case DifferenceType.Append: added++; break;
                    case DifferenceType.Remove: removed++; break;
                }
            }

            StreamWriter sw = null;
            StateInfo = type + " report is being generated...";
            StateDetail = "Creating the output file...";
            try
            {
                sw = new StreamWriter(new FileStream(htmlFilePath, FileMode.Create, FileAccess.Write), Encoding.UTF8);
                sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
                sw.WriteLine("<html>");
                sw.WriteLine("<head>");
                sw.WriteLine("<meta http-equiv=\"content-type\" content=\"text/html;charset=utf-8\">");
                sw.WriteLine("<title>{0} {2} → {1}</title>", type, EncodeUnityHtml(labelOld), EncodeUnityHtml(labelNew));
                sw.WriteLine("<link type=\"text/css\" rel=\"stylesheet\" href=\"style.css\" />");
                sw.WriteLine("</head>");
                sw.WriteLine("<body>");
                sw.WriteLine("<p class=\"wzf\">");
                sw.WriteLine("<table>");
                sw.WriteLine("<tr><th>&nbsp;</th><th>Client</th><th>Version</th></tr>");
                sw.WriteLine("<tr><td>New Version</td><td colspan=\"2\">{0}</td></tr>", EncodeUnityHtml(labelNew));
                sw.WriteLine("<tr><td>Old Version</td><td colspan=\"2\">{0}</td></tr>", EncodeUnityHtml(labelOld));
                sw.WriteLine("<tr><td>Current Time</td><td colspan='2'>{0:yyyy-MM-dd HH:mm:ss.fff}</td></tr>", DateTime.Now);
                sw.WriteLine("<tr><td>Options</td><td colspan='2'>{0}</td></tr>", string.Join("<br/>", new[] {
                    this.OutputPng ? "- Output PNG Files" : null,
                    this.OutputAddedImg ? "- Added Files" : null,
                    this.OutputRemovedImg ? "- Removed Files" : null,
                    "- Compare " + this.Comparer.PngComparison,
                }.Where(option => option != null)));
                sw.WriteLine("</table>");
                sw.WriteLine("</p>");

                // 目次
                string[] menus = new string[3];
                int[] menuCount = new int[3];
                foreach (UnityCompareEntry entry in entries)
                {
                    int idx = GetUnityDiffIndex(entry.DifferenceType);
                    if (idx < 0)
                    {
                        continue;
                    }
                    menus[idx] += string.Format(
                        "<tr><td><a name=\"m_{0}_{1}\" href=\"#a_{0}_{1}\">{2}</a></td></tr>\r\n",
                        idx, menuCount[idx], EncodeUnityHtml(entry.DisplayPath));
                    menuCount[idx]++;
                }

                string[] diffStr = { "Modified", "Added", "Removed" };
                for (int i = 0; i < menus.Length; i++)
                {
                    sw.WriteLine("<table class=\"lst{0}\">", i);
                    sw.WriteLine("<tr><th><a name=\"m_{0}\">{1}:{2}</a></th></tr>", i, diffStr[i], menuCount[i]);
                    if (menus[i] != null)
                    {
                        sw.Write(menus[i]);
                    }
                    sw.WriteLine("</table>");
                    menus[i] = null;
                }

                // 詳細
                int[] count = new int[3];
                foreach (UnityCompareEntry entry in entries)
                {
                    int idx = GetUnityDiffIndex(entry.DifferenceType);
                    if (idx < 0)
                    {
                        continue;
                    }

                    OutputUnityEntry(sw, entry, idx, count[idx], count[idx] + 1, menuCount[idx], diffStr[idx],
                        labelNew, labelOld, this.OutputPng ? srcDirPath : outputDir);
                    count[idx]++;
                }

                sw.WriteLine("</body>");
                sw.WriteLine("</html>");
            }
            finally
            {
                if (sw != null)
                {
                    sw.Flush();
                    sw.Close();
                }
            }

            if (index != null)
            {
                index.WriteLine("<tr><td><a href=\"{0}.html\">{0}</a></td><td>{1}</td><td>{2}</td><td><a href=\"{0}.html#m_0\">{3}</a></td><td><a href=\"{0}.html#m_1\">{4}</a></td><td><a href=\"{0}.html#m_2\">{5}</a></td></tr>",
                    fileNameNoExt,
                    EncodeUnityHtml(labelNew),
                    EncodeUnityHtml(labelOld),
                    changed,
                    added,
                    removed
                    );
                index.Flush();
            }

            StateDetail = fileName + " created.";
        }

        private void OutputUnityEntry(
            StreamWriter sw, UnityCompareEntry entry, int idx, int anchorIndex,
            int entryIndex, int entryTotal, string diffLabel,
            string labelNew, string labelOld, string outputDir)
        {
            string anchorName = string.Format("a_{0}_{1}", idx, anchorIndex);
            string menuAnchorName = string.Format("m_{0}_{1}", idx, anchorIndex);

            StateDetail = string.Format("Comparing {0}...", entry.DisplayPath);
            List<UnityCompareRow> rows = BuildUnityRows(entry, outputDir);
            StateDetail = string.Format("{0} change(s) in total", rows.Count);

            int changed = 0, added = 0, removed = 0;
            foreach (UnityCompareRow row in rows)
            {
                switch (row.DifferenceType)
                {
                    case DifferenceType.Changed: changed++; break;
                    case DifferenceType.Append: added++; break;
                    case DifferenceType.Remove: removed++; break;
                }
            }

            StringBuilder sb = new StringBuilder();
            foreach (UnityCompareRow row in rows)
            {
                int rowIndex = GetUnityDiffIndex(row.DifferenceType);
                StateInfo = string.Format("{0}/{1} {2}: {3}", entryIndex, entryTotal, diffLabel,
                    row.Path ?? entry.DisplayPath);
                this.CollectUnityTooltipIDs(row.Path ?? entry.DisplayPath);
                sb.AppendFormat("<tr class=\"r{0}\">", rowIndex);
                sb.AppendFormat("<td>{0}</td>", row.Path == null ? " " : EncodeUnityHtml(row.Path));
                sb.AppendFormat("<td>{0}</td>", OutputNodeValue(row.Path, row.NodeOld, 1, outputDir) ?? " ");
                sb.AppendFormat("<td>{0}</td>", OutputNodeValue(row.Path, row.NodeNew, 0, outputDir) ?? " ");
                sb.AppendLine("</tr>");
            }

            sw.WriteLine("<table class=\"img\">");
            sw.WriteLine("<tr><th colspan=\"3\"><a name=\"{1}\">{0}</a>: Modified: {2}; Added: {3}; Removed: {4}</th></tr>",
                EncodeUnityHtml(entry.DisplayPath), anchorName, changed, added, removed);
            sw.WriteLine("<tr><th>Path</th><th>Old Version ({0})</th><th>New Version ({1})</th></tr>",
                EncodeUnityHtml(labelOld), EncodeUnityHtml(labelNew));
            sw.Write(sb.ToString());
            sw.WriteLine("<tr><td colspan=\"3\"><a href=\"#{1}\">{0}</a></td></tr>", "Back to top", menuAnchorName);
            sw.WriteLine("</table>");
        }

        /// <summary>単一アセットの詳細テーブル用の行を生成します。</summary>
        private List<UnityCompareRow> BuildUnityRows(UnityCompareEntry entry, string outputDir)
        {
            List<UnityCompareRow> rows = new List<UnityCompareRow>();
            if (entry.DifferenceType == DifferenceType.Changed && entry.ValueOnly)
            {
                rows.Add(new UnityCompareRow
                {
                    DifferenceType = DifferenceType.Changed,
                    Path = entry.DisplayPath,
                    NodeNew = entry.NodeNew,
                    NodeOld = entry.NodeOld,
                });
                return rows;
            }

            Wz_Image imageNew = null, imageOld = null;
            bool ownedNew = false, ownedOld = false;
            Wz_Node contentNew = null, contentOld = null;
            try
            {
                if (entry.SideNew != null && entry.NodeNew != null)
                {
                    contentNew = BuildUnityContent(entry.SideNew, entry.NodeNew, out imageNew, out ownedNew);
                }
                if (entry.SideOld != null && entry.NodeOld != null)
                {
                    contentOld = BuildUnityContent(entry.SideOld, entry.NodeOld, out imageOld, out ownedOld);
                }

                if (entry.DifferenceType == DifferenceType.Changed)
                {
                    if (contentNew != null && contentOld != null)
                    {
                        foreach (CompareDifference diff in this.Comparer.Compare(contentNew, contentOld))
                        {
                            string pathNew = diff.NodeNew == null ? null
                                : CombineUnityPath(entry.DisplayPath, GetUnityRelativePath(diff.NodeNew, contentNew));
                            string pathOld = diff.NodeOld == null ? null
                                : CombineUnityPath(entry.DisplayPath, GetUnityRelativePath(diff.NodeOld, contentOld));
                            rows.Add(new UnityCompareRow
                            {
                                DifferenceType = diff.DifferenceType,
                                Path = pathNew ?? pathOld ?? entry.DisplayPath,
                                NodeNew = diff.NodeNew,
                                NodeOld = diff.NodeOld,
                            });
                        }
                    }
                }
                else if (entry.DifferenceType == DifferenceType.Append && contentNew != null)
                {
                    AddUnityRowsOfTree(contentNew, entry.DisplayPath, DifferenceType.Append, rows);
                }
                else if (entry.DifferenceType == DifferenceType.Remove && contentOld != null)
                {
                    AddUnityRowsOfTree(contentOld, entry.DisplayPath, DifferenceType.Remove, rows);
                }
            }
            finally
            {
                if (ownedNew && imageNew != null)
                {
                    imageNew.Unextract();
                }
                if (ownedOld && imageOld != null)
                {
                    imageOld.Unextract();
                }
            }

            if (rows.Count == 0)
            {
                rows.Add(new UnityCompareRow
                {
                    DifferenceType = entry.DifferenceType,
                    Path = entry.DisplayPath,
                    NodeNew = entry.NodeNew,
                    NodeOld = entry.NodeOld,
                });
            }
            return rows;
        }

        private static void AddUnityRowsOfTree(
            Wz_Node node, string path, DifferenceType type, List<UnityCompareRow> rows)
        {
            foreach (Wz_Node child in SortUnityNodes(node.Nodes))
            {
                string childPath = CombineUnityPath(path, child.Text);
                rows.Add(new UnityCompareRow
                {
                    DifferenceType = type,
                    Path = childPath,
                    NodeNew = type == DifferenceType.Append ? child : null,
                    NodeOld = type == DifferenceType.Remove ? child : null,
                });
                if (child.Nodes.Count > 0)
                {
                    AddUnityRowsOfTree(child, childPath, type, rows);
                }
            }
        }

        private static string GetUnityRelativePath(Wz_Node node, Wz_Node root)
        {
            if (node == root)
            {
                return null;
            }

            List<string> segments = new List<string>();
            for (Wz_Node current = node; current != null && current != root; current = current.ParentNode)
            {
                segments.Add(current.Text);
            }
            segments.Reverse();
            return string.Join("\\", segments.ToArray());
        }

        private static int GetUnityDiffIndex(DifferenceType type)
        {
            switch (type)
            {
                case DifferenceType.Changed:
                    return 0;
                case DifferenceType.Append:
                    return 1;
                case DifferenceType.Remove:
                    return 2;
                default:
                    return -1;
            }
        }

        private static string EncodeUnityHtml(string text)
        {
            return string.IsNullOrEmpty(text) ? " " : WebUtility.HtmlEncode(text);
        }

        #endregion

        #region ツールチップ出力

        // Unity データセットは Wz_File を持たないため、ツールチップ出力中の解決先（PluginManager.FindWz）を
        // 比較対象のデータセットへ固定します。
        private static UnityCompareSide unityResolveSideNew;
        private static UnityCompareSide unityResolveSideOld;
        private static int unityResolveSlot = -1;
        private static bool unityResolveHandlerInstalled;

        /// <summary>
        /// ツールチップを出力するか（いずれかの種類が有効か）。
        /// </summary>
        private bool AnyTooltipOutput
        {
            get
            {
                return this.OutputSkillTooltip || this.OutputItemTooltip || this.OutputGearTooltip
                    || this.OutputMapTooltip || this.OutputMobTooltip || this.OutputNpcTooltip
                    || this.OutputCashTooltip || this.OutputQuestTooltip || this.OutputAchvTooltip;
            }
        }

        /// <summary>
        /// 解決先を比較対象 <paramref name="i"/>（0 = 新、1 = 旧）に切り替えます。Unity 比較以外では何もしません。
        /// </summary>
        private void SetUnityResolveSlot(int i)
        {
            if (unityResolveSideNew == null && unityResolveSideOld == null)
            {
                return;
            }
            unityResolveSlot = i;
        }

        private static void BeginUnityTooltipResolve(UnityCompareSide sideNew, UnityCompareSide sideOld)
        {
            unityResolveSideNew = sideNew;
            unityResolveSideOld = sideOld;
            unityResolveSlot = -1;
            if (!unityResolveHandlerInstalled)
            {
                // WZ 比較用のハンドラーより後に登録されるため、Unity 側の解決結果が優先されます。
                PluginManager.WzFileFinding += UnityResolveWzFinding;
                unityResolveHandlerInstalled = true;
            }
        }

        private static void EndUnityTooltipResolve()
        {
            unityResolveSlot = -1;
            unityResolveSideNew = null;
            unityResolveSideOld = null;
        }

        /// <summary>
        /// Unity クライアント比較中に解決先として優先するデータセット。比較していない場合は null。
        /// WZ 比較用の <see cref="PluginManager.FindWz(string)"/> を通らない検索（ツールチップ側の
        /// 外部リゾルバーなど）も、このデータセットだけを参照します。
        /// </summary>
        public static ClassicWzSource PreferredUnitySource
        {
            get
            {
                UnityCompareSide side = unityResolveSlot == 0 ? unityResolveSideNew
                    : unityResolveSlot == 1 ? unityResolveSideOld
                    : null;
                return side?.Source;
            }
        }

        /// <summary>
        /// 比較対象のデータセットからノードを解決します。別バージョンの Unity クライアントや WZ の内容が
        /// 混ざらないよう、常に結果を上書きします。
        /// </summary>
        private static void UnityResolveWzFinding(object sender, FindWzEventArgs e)
        {
            UnityCompareSide side = unityResolveSlot == 0 ? unityResolveSideNew
                : unityResolveSlot == 1 ? unityResolveSideOld
                : null;
            if (side == null)
            {
                return;
            }

            e.WzFile = null;
            e.WzNode = null;
            if (!string.IsNullOrEmpty(e.FullPath))
            {
                e.WzNode = FindUnitySideNode(side, e.FullPath);
            }
            else if (e.WzType != Wz_Type.Unknown)
            {
                e.WzNode = FindUnitySideTypeNode(side, e.WzType);
            }
        }

        private static Wz_Node FindUnitySideNode(UnityCompareSide side, string wzPath)
        {
            try
            {
                return side.Source.FindNode(wzPath);
            }
            catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException || ex is WzjsException)
            {
                return null;
            }
        }

        private static ClassicWzSource GetUnityCompareSource(int i)
        {
            UnityCompareSide side = i == 0 ? unityResolveSideNew : unityResolveSideOld;
            return side?.Source;
        }

        private static Wz_Node FindUnitySideTypeNode(UnityCompareSide side, Wz_Type type)
        {
            Wz_Node root = side?.Root;
            if (root == null)
            {
                return null;
            }
            if (type == Wz_Type.Base)
            {
                // Unity データセットに Base.wz はありません。
                return root;
            }
            foreach (Wz_Node child in root.Nodes)
            {
                if (string.Equals(child.Text, type.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
            return null;
        }

        /// <summary>
        /// Unity 比較中なら、比較対象の文字列テーブル（String/&lt;region&gt;/&lt;category&gt;）で
        /// <paramref name="linker"/> を初期化します。
        /// </summary>
        private bool TryLoadUnityStringLinker(int i, StringLinker linker)
        {
            ClassicWzSource source = GetUnityCompareSource(i);
            if (source == null)
            {
                return false;
            }
            try
            {
                ClassicStringLinkerTables tables = source.StringTables;
                if (tables == null || !tables.HasValues)
                {
                    return false;
                }
                linker.Clear();
                linker.Load(tables.StringNode, tables.ItemNode, tables.EtcNode, tables.QuestNode);
                return true;
            }
            catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException || ex is WzjsException)
            {
                return false;
            }
        }

        /// <summary>
        /// ツールチップ生成に使う補助データ（セットアイテム、ポイントアイテムなど）を比較対象から読み込みます。
        /// </summary>
        private void InitializeUnityTooltipSources()
        {
            if (!this.AnyTooltipOutput)
            {
                return;
            }

            StateDetail = "Loading the auxiliary data for tooltips...";
            CharaSimLoader.ClearAll();
            SetUnityResolveSlot(0);
            CharaSimLoader.LoadSetItemsIfEmpty();
            CharaSimLoader.LoadAstraSubWeaponsIfEmpty();
            CharaSimLoader.LoadExclusiveEquipsIfEmpty();
            CharaSimLoader.LoadMsnMintableItemListIfEmpty();
            CharaSimLoader.LoadGmsTucBlacklistItemsIfEmpty();
            if (this.LocatePetEquip)
            {
                CharaSimLoader.LoadPetEquipInfoIfEmpty();
            }

            if (this.OutputItemTooltip || this.OutputGearTooltip || this.OutputCashTooltip)
            {
                SetUnityResolveSlot(1);
                CharaSimLoader.LoadCommodities(null, slotIdx: 1);
                SetUnityResolveSlot(0);
                CharaSimLoader.LoadCommodities(null, slotIdx: 0);
                this.CompareCommodities();
            }
        }

        /// <summary>
        /// 差分パス（例: <c>Item\Consume\0200\02000000.img\info\price</c>）から、ツールチップを出力する
        /// アイテムやマップの id を収集します。WZ 比較の <c>Get*ID</c> に相当します。
        /// </summary>
        private void CollectUnityTooltipIDs(string path)
        {
            if (string.IsNullOrEmpty(path) || !this.AnyTooltipOutput)
            {
                return;
            }
            string[] segments = path.Replace('/', '\\').Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                return;
            }

            switch (segments[0])
            {
                case "Item":
                    this.CollectUnityItemPath(segments);
                    break;
                case "Character":
                    this.CollectUnityGearPath(segments);
                    break;
                case "Mob":
                    this.CollectUnitySimpleID(segments, this.OutputMobTooltip, this.OutputMobTooltipIDs, 7);
                    break;
                case "Npc":
                    this.CollectUnitySimpleID(segments, this.OutputNpcTooltip, this.OutputNpcTooltipIDs, 7);
                    break;
                case "Map":
                    if (segments.Length >= 4
                        && string.Equals(segments[1], "Map", StringComparison.Ordinal)
                        && segments[2].StartsWith("Map", StringComparison.Ordinal))
                    {
                        this.AddUnityMapID(segments[3]);
                    }
                    break;
                case "Skill":
                    this.CollectUnitySkillPath(segments);
                    break;
                case "Quest":
                    if (this.OutputQuestTooltip && segments.Length >= 3)
                    {
                        this.AddUnityQuestID(segments[2]);
                    }
                    break;
                case "String":
                    this.CollectUnityStringPath(segments);
                    break;
            }
        }

        /// <summary><c>Item\&lt;区分&gt;\&lt;分類&gt;\&lt;id&gt;...</c> からアイテム id を求めます。</summary>
        private void CollectUnityItemPath(string[] segments)
        {
            string category = segments[1];
            string id = FindUnityNumericID(segments, 2, 7);
            if (id == null)
            {
                return;
            }

            switch (category)
            {
                case "Special":
                    // WZ の Item\Special.img\<分類>.img\<id>（ポイントアイテムの構成）に相当します。
                    this.AddUnityCashID(id.TrimStart('0'));
                    break;
                case "Consume":
                case "Install":
                case "Etc":
                case "Cash":
                case "Pet":
                    this.AddUnityItemID(id);
                    break;
            }
        }

        /// <summary><c>Character\&lt;分類&gt;\&lt;id&gt;...</c> から装備 id を求めます。</summary>
        private void CollectUnityGearPath(string[] segments)
        {
            if (!this.OutputGearTooltip || segments.Length < 3)
            {
                return;
            }
            if (Array.IndexOf(UnityEquipDirectories, segments[1]) < 0)
            {
                return;
            }
            string id = NormalizeUnityID(segments[2], 8);
            if (id == null)
            {
                return;
            }
            if (!this.OutputGearTooltipIDs.Contains(id))
            {
                this.OutputGearTooltipIDs.Add(id);
            }
            AddUnityDiffTag(GetUnityDiffTags(this.DiffGearTags, id), segments[segments.Length - 1]);
        }

        /// <summary><c>Mob\&lt;id&gt;...</c> などの 1 階層 id から id を求めます。</summary>
        private void CollectUnitySimpleID(string[] segments, bool enabled, HashSet<string> ids, int digitCount)
        {
            if (!enabled || segments.Length < 2)
            {
                return;
            }
            string id = FindUnityNumericID(segments, 1, digitCount);
            if (id == null)
            {
                return;
            }
            if (!ids.Contains(id))
            {
                ids.Add(id);
            }
        }

        /// <summary><c>Skill\&lt;職業&gt;\skill\&lt;id&gt;...</c> からスキル id を求めます。</summary>
        private void CollectUnitySkillPath(string[] segments)
        {
            if (!this.OutputSkillTooltip)
            {
                return;
            }
            string id = FindUnityNumericID(segments, 1, 7);
            if (id == null)
            {
                return;
            }
            // WZ 比較と同じく、ツールチップ側はゼロ埋めなしの id で扱います。
            this.AddUnitySkillID(id.TrimStart('0'), segments[segments.Length - 1]);
        }

        /// <summary>
        /// <c>String\&lt;region&gt;\&lt;category.img&gt;\&lt;id&gt;...</c> から、名前だけが変更された
        /// アイテムなどの id を求めます。
        /// </summary>
        private void CollectUnityStringPath(string[] segments)
        {
            if (segments.Length < 4)
            {
                return;
            }
            string category = StripUnityImgSuffix(segments[2]);
            // 表ごとに id の桁数が違います（スキルは 3〜7 桁、マップは 9 桁）。
            int minLength = category == "Skill" ? 3 : category == "Map" ? 9 : 7;
            string id = FindUnityNumericID(segments, 3, minLength);
            if (id == null)
            {
                return;
            }
            string tag = segments[segments.Length - 1];

            switch (category)
            {
                case "Item":
                    // Classic では Item / Eqp / Consume などが 1 つの表なので、id の範囲で振り分けます。
                    string paddedID = id.PadLeft(8, '0');
                    if (paddedID.StartsWith("01", StringComparison.Ordinal))
                    {
                        this.AddUnityGearID(id, tag);
                    }
                    else if (paddedID.StartsWith("09", StringComparison.Ordinal))
                    {
                        // 09xxxxxx はポイントアイテム（WZ の Item\Special.img）。
                        this.AddUnityCashID(id.TrimStart('0'));
                    }
                    else
                    {
                        this.AddUnityItemID(id);
                    }
                    break;
                case "Eqp":
                case "Gear":
                    this.AddUnityGearID(id, tag);
                    break;
                case "Consume":
                case "Ins":
                case "Etc":
                case "Cash":
                case "Pet":
                    this.AddUnityItemID(id);
                    break;
                case "Skill":
                    this.AddUnitySkillID(id.TrimStart('0'), tag);
                    break;
                case "Map":
                    this.AddUnityMapID(id);
                    break;
                case "Mob":
                case "MonsterBook":
                    if (this.OutputMobTooltip && !this.OutputMobTooltipIDs.Contains(id))
                    {
                        this.OutputMobTooltipIDs.Add(id);
                    }
                    break;
                case "Npc":
                    if (this.OutputNpcTooltip && !this.OutputNpcTooltipIDs.Contains(id))
                    {
                        this.OutputNpcTooltipIDs.Add(id);
                    }
                    break;
            }
        }

        private void AddUnityItemID(string id)
        {
            if (!this.OutputItemTooltip || id == null)
            {
                return;
            }
            // ペットは 7 桁、それ以外のアイテムは 8 桁の id で扱います（WZ 比較と同じ）。
            if (!id.StartsWith("500", StringComparison.Ordinal))
            {
                id = id.PadLeft(8, '0');
            }
            if (!this.OutputItemTooltipIDs.Contains(id))
            {
                this.OutputItemTooltipIDs.Add(id);
                this.DiffItemTags[id] = new List<string>();
            }
            if (this.OutputChairAndRidingMount)
            {
                // 椅子はアバター例（ChairExamples）として出力します。
                if (id.StartsWith("0301", StringComparison.Ordinal))
                {
                    this.OutputChairIDs.Add(id);
                }
                this.DiffItemIDs.Add(id);
            }
        }

        private void AddUnityGearID(string id, string tag)
        {
            if (!this.OutputGearTooltip || id == null)
            {
                return;
            }
            if (!this.OutputGearTooltipIDs.Contains(id))
            {
                this.OutputGearTooltipIDs.Add(id);
            }
            // ポイントアイテムの価格比較など、id だけが先に登録される場合があります。
            AddUnityDiffTag(GetUnityDiffTags(this.DiffGearTags, id), tag);
        }

        private void AddUnityCashID(string id)
        {
            if (!this.OutputCashTooltip || string.IsNullOrEmpty(id))
            {
                return;
            }
            if (!this.OutputCashTooltipIDs.Contains(id))
            {
                this.OutputCashTooltipIDs.Add(id);
                this.DiffCashTags[id] = new List<string>();
            }
        }

        private void AddUnityMapID(string id)
        {
            if (!this.OutputMapTooltip)
            {
                return;
            }
            string mapID = NormalizeUnityID(id, 9);
            if (mapID == null || this.OutputMapTooltipIDs.Contains(mapID))
            {
                return;
            }
            this.OutputMapTooltipIDs.Add(mapID);
            this.DiffMapTags[mapID] = new List<string>();
        }

        private void AddUnityQuestID(string id)
        {
            string questID = NormalizeUnityID(id, 1);
            if (questID == null || this.OutputQuestTooltipIDs.Contains(questID))
            {
                return;
            }
            this.OutputQuestTooltipIDs.Add(questID);
        }

        private void AddUnitySkillID(string id, string tag)
        {
            if (!this.OutputSkillTooltip || string.IsNullOrEmpty(id))
            {
                return;
            }
            if (!this.OutputSkillTooltipIDs.Contains(id))
            {
                this.OutputSkillTooltipIDs.Add(id);
            }
            AddUnityDiffTag(GetUnityDiffTags(this.DiffSkillTags, id), tag);
        }

        /// <summary>id に対応するタグ一覧を取得します（未登録なら作成）。</summary>
        private static List<string> GetUnityDiffTags(Dictionary<string, List<string>> tags, string id)
        {
            List<string> list;
            if (!tags.TryGetValue(id, out list))
            {
                list = new List<string>();
                tags[id] = list;
            }
            return list;
        }

        private static void AddUnityDiffTag(List<string> tags, string tag)
        {
            if (tags == null || string.IsNullOrEmpty(tag) || tags.Contains(tag))
            {
                return;
            }
            tags.Add(tag);
        }

        private static string FindUnityNumericID(string[] segments, int startIndex, int minLength)
        {
            for (int i = startIndex; i < segments.Length; i++)
            {
                string id = NormalizeUnityID(segments[i], minLength);
                if (id != null)
                {
                    return id;
                }
            }
            return null;
        }

        /// <summary>ノード名が <paramref name="minLength"/> 桁以上の数字なら、その文字列を返します。</summary>
        private static string NormalizeUnityID(string name, int minLength)
        {
            string text = StripUnityImgSuffix(name);
            if (string.IsNullOrEmpty(text) || text.Length < Math.Max(minLength, 1))
            {
                return null;
            }
            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]))
                {
                    return null;
                }
            }
            return text;
        }

        private static string StripUnityImgSuffix(string name)
        {
            if (name != null && name.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(0, name.Length - 4);
            }
            return name;
        }

        /// <summary>装備ツールチップの対象になる <c>Character</c> 配下の分類名。</summary>
        private static readonly string[] UnityEquipDirectories =
        {
            "Accessory", "Cap", "Coat", "Longcoat", "Pants", "Shoes", "Glove", "Shield", "Cape", "Ring",
            "Totem", "Weapon", "Mechanic", "Android", "Bits", "ArcaneForce", "AuthenticForce", "Jewel",
            "NT_Beauty", "EquipBag", "PetEquip", "Dragon", "TamingMob", "Face", "Hair",
        };

        #endregion

        #region 内部型

        private enum UnityEntryChange
        {
            None = 0,
            Content,
            Value,
        }

        private sealed class UnityCompareSide
        {
            public UnityCompareSide(ClassicWzSource source, Wz_Node root)
            {
                this.Source = source;
                this.Root = root;
            }

            public ClassicWzSource Source { get; private set; }
            public Wz_Node Root { get; private set; }

            /// <summary>WZ 風ディレクトリツリー内の位置から catalog のアセット索引キーを復元します。</summary>
            public string GetImageKey(Wz_Node node)
            {
                List<string> segments = new List<string>();
                for (Wz_Node current = node; current != null && current != this.Root; current = current.ParentNode)
                {
                    segments.Add(current.Text);
                }
                if (segments.Count == 0)
                {
                    return null;
                }
                segments.Reverse();
                return string.Join("/", segments.ToArray());
            }
        }

        private sealed class UnityDirectoryPair
        {
            public UnityDirectoryPair(string name, Wz_Node nodeNew, Wz_Node nodeOld)
            {
                this.Name = name;
                this.NodeNew = nodeNew;
                this.NodeOld = nodeOld;
            }

            public string Name { get; private set; }
            public Wz_Node NodeNew { get; private set; }
            public Wz_Node NodeOld { get; private set; }
        }

        private sealed class UnityCompareEntry
        {
            public DifferenceType DifferenceType { get; set; }
            public UnityCompareSide SideNew { get; set; }
            public UnityCompareSide SideOld { get; set; }
            public Wz_Node NodeNew { get; set; }
            public Wz_Node NodeOld { get; set; }
            public string DisplayPath { get; set; }
            public bool ValueOnly { get; set; }
        }

        private sealed class UnityCompareRow
        {
            public DifferenceType DifferenceType { get; set; }
            public string Path { get; set; }
            public Wz_Node NodeNew { get; set; }
            public Wz_Node NodeOld { get; set; }
        }

        #endregion
    }
}
