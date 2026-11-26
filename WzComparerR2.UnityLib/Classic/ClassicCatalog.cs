using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WzComparerR2.UnityLib.Images;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>catalog 內一個 bundle 的識別資料。</summary>
    public sealed class ClassicBundleMeta
    {
        public ClassicBundleMeta(string name, long size, long lastWriteTicks)
        {
            this.Name = name;
            this.Size = size;
            this.LastWriteTicks = lastWriteTicks;
        }

        /// <summary>bundle 檔名（去副檔名）。</summary>
        public string Name { get; private set; }

        public long Size { get; private set; }

        public long LastWriteTicks { get; private set; }
    }

    /// <summary>索引目錄的下一層。</summary>
    public sealed class ClassicIndexChild
    {
        internal ClassicIndexChild(string name, string path, bool isImage, int imageCount)
        {
            this.Name = name;
            this.Path = path;
            this.IsImage = isImage;
            this.ImageCount = imageCount;
        }

        public string Name { get; private set; }
        public string Path { get; private set; }
        public bool IsImage { get; private set; }
        public int ImageCount { get; private set; }
    }

    /// <summary>
    /// 資產索引：bundle → 邏輯路徑 → 物件位置。
    /// <para>
    /// 路徑協定：container 路徑
    /// <c>Assets/WzAssets/Json/Character/00002000.wzjson</c> → 索引鍵
    /// <c>Character/00002000</c>（去前綴、去副檔名）。樹上前幾段是「目錄」，
    /// 命中索引鍵的那個節點是「img」，img 之後的段落由 WZJS 樹導航
    /// （如 <c>Character/00020116/angry/0/face</c>）。
    /// </para>
    /// <para>
    /// 索引在 <see cref="Build"/> 時重建（與參考實作同樣掃描每個 bundle 的
    /// AssetBundle container）；磁碟快取尚未實作。
    /// </para>
    /// </summary>
    public sealed class ClassicCatalog
    {
        public const int CurrentVersion = 7;

        private const string JsonPrefix = "assets/wzassets/json/";
        private const string SoundPrefix = "assets/wzdata/sound/";
        private static readonly string[] SoundSuffixes = new[] { ".mp3", ".wav" };

        private ClassicCatalog(string rootPath)
        {
            this.RootPath = rootPath;
            this.Bundles = new List<ClassicBundleMeta>();
            this.Entries = new SortedDictionary<string, UnityEntryLocation>(StringComparer.Ordinal);
            this.Image = new UnityImageIndex();
            this.spritesetEntryKeys = new HashSet<string>(StringComparer.Ordinal);
            this.atlasEntryKeys = new HashSet<string>(StringComparer.Ordinal);
            this.SpritesetOrigins = new Dictionary<string, UnitySpriteOrigin>(StringComparer.Ordinal);
            this.SoundEntries = new SortedDictionary<string, UnityEntryLocation>(StringComparer.Ordinal);
            this.SoundKeysByImage = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        }

        public string RootPath { get; private set; }

        public List<ClassicBundleMeta> Bundles { get; private set; }

        /// <summary>邏輯路徑（如 <c>Character/00002000</c>）→ 物件位置。</summary>
        public SortedDictionary<string, UnityEntryLocation> Entries { get; private set; }

        public UnityImageIndex Image { get; private set; }

        /// <summary>
        /// サウンドパス（例: <c>Sound/Bgm00/FloralLife</c>）→ AudioClip の位置。
        /// これらの項目は <see cref="Entries"/> には入れません: WZ のサウンドは img 配下のノードであり、
        /// <see cref="SoundKeysByImage"/> から img 内容を構成します。
        /// </summary>
        public SortedDictionary<string, UnityEntryLocation> SoundEntries { get; private set; }

        /// <summary>サウンド img（例: <c>Sound/Bgm00</c>）→ その配下のサウンドパス。</summary>
        public SortedDictionary<string, List<string>> SoundKeysByImage { get; private set; }

        /// <summary><see cref="RebuildSpritesetEntries"/> で追加された索引キー（ロケール切り替え時にまとめて取り消せます）。</summary>
        private readonly HashSet<string> spritesetEntryKeys;

        /// <summary><see cref="AddAtlasEntries"/> で追加された索引キー（ロケールに依存しないので再構築は 1 回だけ）。</summary>
        private readonly HashSet<string> atlasEntryKeys;

        /// <summary>
        /// spriteset スプライトの邏輯路徑 → WZ の <c>origin</c>。
        /// sprite の m_Pivot から求めた値で、アニメーションの基準点合わせに使います。
        /// </summary>
        public Dictionary<string, UnitySpriteOrigin> SpritesetOrigins { get; private set; }

        /// <summary>掃描目錄下所有 .bundle，用每個 AssetBundle 物件的 m_Container 建索引。</summary>
        /// <param name="rootDirectory">
        /// bundle のあるディレクトリ。bundle ディレクトリ自体、StreamingAssets/aa、クライアントフォルダーの
        /// いずれを指定しても自動的に解決します。
        /// </param>
        public static ClassicCatalog Build(string rootDirectory)
        {
            if (rootDirectory == null)
            {
                throw new ArgumentNullException(nameof(rootDirectory));
            }
            if (!Directory.Exists(rootDirectory))
            {
                throw new DirectoryNotFoundException("The dataset directory does not exist: " + rootDirectory);
            }

            string bundleDirectory = ResolveBundleDirectory(rootDirectory);

            List<string> files = new List<string>();
            foreach (string path in Directory.GetFiles(bundleDirectory))
            {
                if (string.Equals(Path.GetExtension(path), ".bundle", StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(path);
                }
            }
            files.Sort(StringComparer.Ordinal);

            ClassicCatalog catalog = new ClassicCatalog(bundleDirectory);
            foreach (string path in files)
            {
                FileInfo info = new FileInfo(path);
                catalog.Bundles.Add(new ClassicBundleMeta(
                    Path.GetFileNameWithoutExtension(path), info.Length, info.LastWriteTimeUtc.Ticks));
            }

            // 1st pass: 全 bundle 横断の pathID → 位置索引。spriteset の .asset は別 bundle
            // （atlas_* など）の Sprite / Texture2D を参照するため、外部参照の解決に必要です。
            Dictionary<long, UnityEntryLocation> objectLocations =
                new Dictionary<long, UnityEntryLocation>(65536);
            for (int bundleIndex = 0; bundleIndex < files.Count; bundleIndex++)
            {
                using (UnityFsBundle bundle = UnityFsBundle.Open(files[bundleIndex]))
                {
                    catalog.CollectObjectLocations(bundle, bundleIndex, objectLocations);
                }
            }

            for (int bundleIndex = 0; bundleIndex < files.Count; bundleIndex++)
            {
                using (UnityFsBundle bundle = UnityFsBundle.Open(files[bundleIndex]))
                {
                    catalog.IndexBundle(
                        bundle, bundleIndex, catalog.Bundles[bundleIndex].Name, objectLocations);
                }
            }
            catalog.GroupSoundEntries();
            return catalog;
        }

        /// <summary>
        /// 全 bundle の SerializedFile を走査し、pathID → <see cref="UnityEntryLocation"/> を集めます
        /// （最初に見つかったものを優先）。
        /// </summary>
        private void CollectObjectLocations(
            UnityFsBundle bundle, int bundleIndex, Dictionary<long, UnityEntryLocation> objectLocations)
        {
            for (int nodeIndex = 0; nodeIndex < bundle.Nodes.Count; nodeIndex++)
            {
                if (!bundle.Nodes[nodeIndex].IsSerializedFile)
                {
                    continue;
                }
                UnitySerializedFile serializedFile;
                try
                {
                    int metadataLength = UnitySerializedFile.GetMetadataLength(
                        bundle.ReadNodeRange(nodeIndex, 0, UnitySerializedFile.HeaderSize));
                    serializedFile = UnitySerializedFile.Parse(
                        bundle.ReadNodeRange(nodeIndex, 0, metadataLength));
                }
                catch (Exception ex) when (ex is UnityException || ex is ArgumentException || ex is OverflowException)
                {
                    continue;
                }

                foreach (UnityObjectInfo obj in serializedFile.Objects)
                {
                    if (obj.PathId == 0 || objectLocations.ContainsKey(obj.PathId))
                    {
                        continue;
                    }
                    long absolute = serializedFile.DataOffset + obj.ByteStart;
                    if (absolute < serializedFile.DataOffset)
                    {
                        continue;
                    }
                    objectLocations[obj.PathId] = new UnityEntryLocation(
                        bundleIndex, nodeIndex, absolute, obj.ByteSize, obj.ClassId);
                }
            }
        }

        /// <summary>任意の <see cref="UnityEntryLocation"/> からオブジェクトの bytes を読みます（別 bundle は都度開きます）。</summary>
        internal byte[] ReadEntryBytes(UnityEntryLocation location)
        {
            string bundlePath = Path.Combine(this.RootPath, this.Bundles[location.BundleIndex].Name + ".bundle");
            using (UnityFsBundle bundle = UnityFsBundle.Open(bundlePath))
            {
                return bundle.ReadNodeRange(
                    location.NodeIndex, location.AbsoluteOffset, (int)location.ByteSize);
            }
        }

        /// <summary>
        /// .bundle が実際に入っているディレクトリを特定します。
        /// Addressables 1.x のローカル配置は <c>&lt;aa&gt;/StandaloneWindows64</c>、
        /// 2.x のリモートカタログ配置は <c>&lt;aa&gt;/w</c> になるため、
        /// クライアント直下・<see cref="System.IO.Path"/> の <c>StreamingAssets/aa</c>・
        /// その配下のいずれを選んでも開けるようにします。
        /// </summary>
        public static string ResolveBundleDirectory(string rootDirectory)
        {
            if (rootDirectory == null)
            {
                throw new ArgumentNullException(nameof(rootDirectory));
            }
            if (HasBundles(rootDirectory))
            {
                return rootDirectory;
            }

            List<string> candidates = new List<string>();
            // クライアント直下・StreamingAssets・aa のいずれを選んでも開けるようにします。
            List<string> aaDirectories = new List<string>();
            aaDirectories.Add(rootDirectory);
            aaDirectories.Add(Path.Combine(rootDirectory, "StreamingAssets", "aa"));
            aaDirectories.Add(Path.Combine(rootDirectory, "aa"));
            aaDirectories.Add(Path.Combine(rootDirectory, "StreamingAssets"));

            foreach (string aaDirectory in aaDirectories)
            {
                // Addressables の 2 つの配置（リモートカタログ w / ローカル StandaloneWindows64）。
                candidates.Add(Path.Combine(aaDirectory, "w"));
                string buildTarget = ReadBuildTarget(Path.Combine(aaDirectory, "settings.json"));
                if (buildTarget != null)
                {
                    candidates.Add(Path.Combine(aaDirectory, buildTarget));
                }
                candidates.Add(Path.Combine(aaDirectory, "StandaloneWindows64"));

                // aa 直下のサブディレクトリも順に確認します。
                candidates.AddRange(SafeGetDirectories(aaDirectory));
                candidates.Add(aaDirectory);
            }

            // クライアント直下のサブディレクトリも確認します。
            candidates.AddRange(SafeGetDirectories(rootDirectory));

            foreach (string candidate in candidates)
            {
                if (Directory.Exists(candidate) && HasBundles(candidate))
                {
                    return candidate;
                }
            }

            throw new ClassicAssetException(
                "No AssetBundle (.bundle) was found under: " + rootDirectory + "\r\n"
                + "Select the StreamingAssets/aa folder of the client, or a subfolder that contains the bundles.");
        }

        private static bool HasBundles(string directory)
        {
            return Directory.Exists(directory)
                && Directory.GetFiles(directory, "*.bundle").Length > 0;
        }

        private static IEnumerable<string> SafeGetDirectories(string directory)
        {
            try
            {
                return Directory.Exists(directory)
                    ? Directory.GetDirectories(directory)
                    : Enumerable.Empty<string>();
            }
            catch (IOException)
            {
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>aa/settings.json からビルドターゲット名（例: <c>StandaloneWindows64</c>）を読みます。</summary>
        private static string ReadBuildTarget(string settingsPath)
        {
            try
            {
                if (!File.Exists(settingsPath))
                {
                    return null;
                }
                string text = File.ReadAllText(settingsPath);
                Match match = Regex.Match(text, "\"m_BuildTarget\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.CultureInvariant);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        /// <summary>
        /// サウンドのエントリーを WZ の img 形式に整理します: <c>Sound/&lt;img&gt;/&lt;サブディレクトリ…&gt;/&lt;名前&gt;</c>
        /// <see cref="SoundEntries"/> に格納し、<see cref="Entries"/> には img 自体だけを残します
        /// （<c>Sound/&lt;img&gt;</c>）にまとめ、ツリー上で「Sound &gt; Bgm00 &gt; FloralLife」と表示されるようにし、すべてのサウンドがルート直下に並ばないようにします。
        /// </summary>
        private void GroupSoundEntries()
        {
            List<KeyValuePair<string, UnityEntryLocation>> sounds = new List<KeyValuePair<string, UnityEntryLocation>>();
            foreach (KeyValuePair<string, UnityEntryLocation> entry in this.Entries)
            {
                if (entry.Value.ClassId == UnityClassIds.AudioClip)
                {
                    sounds.Add(entry);
                }
            }

            foreach (KeyValuePair<string, UnityEntryLocation> entry in sounds)
            {
                int firstSlash = entry.Key.IndexOf('/');
                int secondSlash = firstSlash < 0 ? -1 : entry.Key.IndexOf('/', firstSlash + 1);
                if (secondSlash <= firstSlash + 1 || secondSlash == entry.Key.Length - 1)
                {
                    continue; // 形状が合わない場合（例: Sound/<name>）: そのまま維持
                }

                string imageKey = entry.Key.Substring(0, secondSlash);
                List<string> keys;
                if (!this.SoundKeysByImage.TryGetValue(imageKey, out keys))
                {
                    keys = new List<string>();
                    this.SoundKeysByImage[imageKey] = keys;
                }
                keys.Add(entry.Key);
                this.SoundEntries[entry.Key] = entry.Value;
                this.Entries.Remove(entry.Key);
            }

            foreach (KeyValuePair<string, List<string>> group in this.SoundKeysByImage)
            {
                group.Value.Sort(StringComparer.Ordinal);
                if (!this.Entries.ContainsKey(group.Key))
                {
                    this.Entries[group.Key] = this.SoundEntries[group.Value[0]];
                }
            }
        }

        /// <summary>その索引キーがサウンド img（例: <c>Sound/Bgm00</c>）かどうか。</summary>
        public bool IsSoundImage(string key)
        {
            return key != null && this.SoundKeysByImage.ContainsKey(key);
        }

        /// <summary>サウンド img 配下のサウンドパス（例: <c>Sound/Bgm00/FloralLife</c>）。ソート済み。</summary>
        public IReadOnlyList<string> GetSoundKeys(string imageKey)
        {
            List<string> keys;
            if (imageKey != null && this.SoundKeysByImage.TryGetValue(imageKey, out keys))
            {
                return keys;
            }
            return new string[0];
        }

        /// <summary>
        /// spriteset 索引を再構築します: <paramref name="locale"/> と <c>Common</c> で提供されるパスのみを収録します。
        /// <para>
        /// spriteset（<c>SpriteSet/&lt;region&gt;/UI/…</c>）は WZJS/JSON アセットではないため、本来は索引に現れず、
        /// <c>UI/UIWindow</c>、<c>UI/Login</c> のように spriteset だけで提供されるディレクトリがツリーから丸ごと消えていました。
        /// 従来の WZ と同じく、ここでは現在の地域に実際に存在するノードだけを索引します: 他地域だけに存在するアセット
        /// （例: TW/KR のみ提供される <c>UI/Login/GameLogo</c>）はツリーに現れません。
        /// </para>
        /// <para>Value はレンダリング時に <see cref="UnityCanvas"/> がロケールに応じて解決します。</para>
        /// </summary>
        public void RebuildSpritesetEntries(string locale)
        {
            foreach (string key in this.spritesetEntryKeys)
            {
                this.Entries.Remove(key);
                this.SpritesetOrigins.Remove(key);
            }
            this.spritesetEntryKeys.Clear();

            if (!string.IsNullOrEmpty(locale))
            {
                this.AddSpritesetEntries(locale);
            }
            this.AddSpritesetEntries("Common");
            this.AddAtlasEntries();
        }

        /// <summary>
        /// SpriteAtlas（<c>Assets/WzAssets/Atlas/&lt;name&gt;.spriteatlasv2</c>）を <c>UI/&lt;atlas 名&gt;.img</c> として索引します。
        /// 現在のロケールの spriteset に無い sprite もここから辿れます（atlas はロケール固定なので再構築は 1 回だけ）。
        /// </summary>
        private void AddAtlasEntries()
        {
            foreach (KeyValuePair<string, Dictionary<string, UnitySetSpriteEntry>> asset in this.Image.AtlasAssets)
            {
                foreach (KeyValuePair<string, UnitySetSpriteEntry> sprite in asset.Value)
                {
                    string path = asset.Key + "/" + sprite.Key;
                    if (this.Entries.ContainsKey(path) && !this.atlasEntryKeys.Contains(path))
                    {
                        continue; // WZJS / JSON / spriteset 側の定義を優先
                    }
                    this.Entries[path] = sprite.Value.SpriteLocation;
                    this.SpritesetOrigins[path] = sprite.Value.Origin;
                    this.atlasEntryKeys.Add(path);
                }
            }
        }

        private void AddSpritesetEntries(string region)
        {
            foreach (KeyValuePair<string, Dictionary<string, UnitySetSpriteEntry>> asset in this.Image.SetAssets)
            {
                int slash = asset.Key.IndexOf('/');
                if (slash <= 0 || slash == asset.Key.Length - 1)
                {
                    continue;
                }
                if (!string.Equals(asset.Key.Substring(0, slash), region, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string logicalPath = asset.Key.Substring(slash + 1);
                foreach (KeyValuePair<string, UnitySetSpriteEntry> sprite in asset.Value)
                {
                    string path = string.IsNullOrEmpty(sprite.Key)
                        ? logicalPath
                        : logicalPath + "/" + sprite.Key;
                    if (this.Entries.ContainsKey(path) && !this.spritesetEntryKeys.Contains(path))
                    {
                        continue; // WZJS / JSON アセットを優先
                    }
                    this.Entries[path] = sprite.Value.SpriteLocation;
                    this.SpritesetOrigins[path] = sprite.Value.Origin;
                    this.spritesetEntryKeys.Add(path);
                }
            }
        }

        /// <summary>頂層（或某目錄節點下）的下一層。</summary>
        public List<ClassicIndexChild> ListIndexChildren(string prefix)
        {
            SortedDictionary<string, ClassicIndexChild> output =
                new SortedDictionary<string, ClassicIndexChild>(StringComparer.Ordinal);
            string pfx = string.IsNullOrEmpty(prefix) ? string.Empty : prefix + "/";
            foreach (string key in this.Entries.Keys)
            {
                if (!key.StartsWith(pfx, StringComparison.Ordinal))
                {
                    continue;
                }
                string rest = key.Substring(pfx.Length);
                int slash = rest.IndexOf('/');
                string segment = slash < 0 ? rest : rest.Substring(0, slash);
                string childPath = pfx + segment;

                ClassicIndexChild child;
                if (!output.TryGetValue(childPath, out child))
                {
                    child = new ClassicIndexChild(segment, childPath, false, 0);
                    output[childPath] = child;
                }
                bool isImage = slash < 0;
                output[childPath] = new ClassicIndexChild(
                    segment, childPath, child.IsImage || isImage, child.ImageCount + 1);
            }
            List<ClassicIndexChild> result = new List<ClassicIndexChild>(output.Values);
            result.Sort((a, b) => CompareNodeNames(a.Name, b.Name));
            return result;
        }

        /// <summary>把 path 拆成 (img_key, img 內部路徑)；找不到任何 img 前綴回 false。</summary>
        public bool TrySplitImagePrefix(string path, out string imageKey, out string inner)
        {
            imageKey = null;
            inner = null;
            if (path == null)
            {
                return false;
            }
            string current = path;
            while (true)
            {
                if (this.Entries.ContainsKey(current))
                {
                    imageKey = current;
                    string rest = path.Substring(current.Length);
                    if (rest.StartsWith("/", StringComparison.Ordinal))
                    {
                        rest = rest.Substring(1);
                    }
                    inner = rest;
                    return true;
                }
                int slash = current.LastIndexOf('/');
                if (slash < 0)
                {
                    return false;
                }
                current = current.Substring(0, slash);
            }
        }

        /// <summary>
        /// アイテム id から <c>Item</c> ディレクトリ配下の候補パスを優先順に並べます。パスの末尾は
        /// img、それ以降のセグメントは img 内のノードです（参考実装の item_path_candidates と一致）。
        /// </summary>
        public List<string> GetItemPathCandidates(int itemId)
        {
            List<string> candidates = new List<string>();
            if (itemId < 0)
            {
                return candidates;
            }
            string id = itemId.ToString("D8", CultureInfo.InvariantCulture);
            string group = (itemId / 10000).ToString("D4", CultureInfo.InvariantCulture);
            switch (itemId / 1000000)
            {
                case 2:
                    candidates.Add("Item/Consume/" + group + "/" + id);
                    break;
                case 3:
                    candidates.Add("Item/Install/" + group + "/" + id);
                    break;
                case 4:
                    candidates.Add("Item/Etc/" + group + "/" + id);
                    break;
                case 5:
                    // ペットは 1 id 1 img、それ以外の 5 で始まるアイテムは Cash 区分の img に入ります。
                    string pet = "Item/Pet/" + itemId.ToString(CultureInfo.InvariantCulture);
                    if (this.Entries.ContainsKey(pet))
                    {
                        candidates.Add(pet);
                    }
                    candidates.Add("Item/Cash/" + group + "/" + id);
                    break;
                case 9:
                    // セットアイテムやインベントリ拡張など: Special img 内のノードは 7 桁 id です。
                    candidates.Add("Item/Special/0"
                        + (itemId / 10000).ToString(CultureInfo.InvariantCulture)
                        + "/" + itemId.ToString("D7", CultureInfo.InvariantCulture));
                    break;
            }
            return candidates;
        }

        /// <summary>
        /// 装備 id から <c>Character</c> ディレクトリ配下の候補パスを優先順に並べます。
        /// </summary>
        public List<string> GetEquipPathCandidates(int itemId)
        {
            List<string> candidates = new List<string>();
            if (itemId / 1000000 != 1)
            {
                return candidates;
            }
            string directory = GetEquipDirectory(itemId / 10000);
            if (directory != null)
            {
                candidates.Add("Character/" + directory + "/"
                    + itemId.ToString("D8", CultureInfo.InvariantCulture));
            }
            return candidates;
        }

        /// <summary>
        /// モブ id から <c>Mob</c> ディレクトリ配下の候補パスを優先順に並べます
        /// （従来の WZ の <c>Mob/&lt;id&gt;.img</c> や特殊分類に対応）。
        /// </summary>
        public List<string> GetMobPathCandidates(int mobId)
        {
            List<string> candidates = new List<string>();
            if (mobId <= 0)
            {
                return candidates;
            }
            string id = mobId.ToString("D7", CultureInfo.InvariantCulture);
            candidates.Add("Mob/" + id);
            foreach (string group in new[]
            {
                "AbyssExpeditionMob", "MExplorerMob", "QuestCountGroup", "RoguelikeMob",
            })
            {
                candidates.Add("Mob/" + group + "/" + id);
            }
            candidates.Add("Mob/RoguelikeMob/Redmoon/" + id);
            return candidates;
        }

        /// <summary>NPC id から <c>Npc</c> ディレクトリ配下のパスを導きます。</summary>
        public List<string> GetNpcPathCandidates(int npcId)
        {
            List<string> candidates = new List<string>();
            if (npcId <= 0)
            {
                return candidates;
            }
            candidates.Add("Npc/" + npcId.ToString("D7", CultureInfo.InvariantCulture));
            return candidates;
        }

        /// <summary>
        /// スキル id から <c>Skill</c> ディレクトリ配下のパスを導きます:
        /// img は職業（<c>Skill/&lt;id/10000&gt;</c>）で、スキル自体は img 内の
        /// <c>skill/&lt;id&gt;</c>。
        /// </summary>
        public List<string> GetSkillPathCandidates(int skillId)
        {
            List<string> candidates = new List<string>();
            if (skillId <= 0)
            {
                return candidates;
            }
            candidates.Add("Skill/" + (skillId / 10000).ToString(CultureInfo.InvariantCulture)
                + "/skill/" + skillId.ToString(CultureInfo.InvariantCulture));
            return candidates;
        }

        /// <summary>
        /// マップ id から <c>Map</c> ディレクトリ配下のパスを導きます:
        /// <c>Map/Map/Map&lt;先頭桁&gt;/&lt;9 桁 id&gt;</c>（WZ の <c>Map/Map/MapN/&lt;id&gt;.img</c> に対応）。
        /// </summary>
        public List<string> GetMapPathCandidates(int mapId)
        {
            List<string> candidates = new List<string>();
            if (mapId <= 0)
            {
                return candidates;
            }
            string id = mapId.ToString("D9", CultureInfo.InvariantCulture);
            candidates.Add("Map/Map/Map" + id[0] + "/" + id);
            return candidates;
        }

        /// <summary>
        /// クエスト id から <c>Quest</c> ディレクトリ配下のパスを導きます:
        /// <c>Quest/QuestInfo/&lt;id&gt;</c>（WZ の <c>Quest/QuestInfo.img/&lt;id&gt;</c> に対応）。
        /// </summary>
        public List<string> GetQuestPathCandidates(int questId)
        {
            List<string> candidates = new List<string>();
            if (questId <= 0)
            {
                return candidates;
            }
            candidates.Add("Quest/QuestInfo/" + questId.ToString(CultureInfo.InvariantCulture));
            return candidates;
        }

        /// <summary>
        /// 指定ディレクトリ（1 階層下のサブディレクトリを含む）内で「ノード名の数値が <paramref name="id"/> と等しい」img 索引キーを探し、
        /// ゼロ埋めの違いは無視します。見つからない場合は null。WZ の分類ディレクトリとアセットツリーのディレクトリが一致しない場合に使用します。
        /// </summary>
        public string FindImageKeyByNumericName(string directory, int id)
        {
            if (string.IsNullOrEmpty(directory) || id < 0)
            {
                return null;
            }
            string prefix = directory + "/";
            foreach (string key in this.Entries.Keys)
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }
                string rest = key.Substring(prefix.Length);
                int slash = rest.LastIndexOf('/');
                if (slash >= 0 && rest.IndexOf('/') != slash)
                {
                    continue; // 3 階層以上は分類 img とみなさない
                }
                string leaf = slash < 0 ? rest : rest.Substring(slash + 1);
                long value;
                if (TryParseNumericName(leaf, out value) && value == id)
                {
                    return key;
                }
            }
            return null;
        }

        private static string GetEquipDirectory(int group)
        {
            switch (group)
            {
                case 100:
                    return "Cap";
                case 101:
                case 102:
                case 103:
                case 112:
                case 113:
                case 114:
                case 115:
                    return "Accessory";
                case 104:
                    return "Coat";
                case 105:
                    return "Longcoat";
                case 106:
                    return "Pants";
                case 107:
                    return "Shoes";
                case 108:
                    return "Glove";
                case 109:
                    return "Shield";
                case 110:
                    return "Cape";
                case 111:
                    return "Ring";
                case 180:
                    return "PetEquip";
                default:
                    return group >= 121 && group <= 179 ? "Weapon" : null;
            }
        }

        /// <summary>
        /// 節點名比較：純數字名依數值排最前，其餘不分大小寫依字母。
        /// </summary>
        public static int CompareNodeNames(string left, string right)
        {
            long leftNumber;
            long rightNumber;
            bool leftIsNumber = TryParseNumericName(left, out leftNumber);
            bool rightIsNumber = TryParseNumericName(right, out rightNumber);
            if (leftIsNumber && rightIsNumber)
            {
                int compare = leftNumber.CompareTo(rightNumber);
                return compare != 0 ? compare : string.CompareOrdinal(left, right);
            }
            if (leftIsNumber)
            {
                return -1;
            }
            if (rightIsNumber)
            {
                return 1;
            }
            int insensitive = string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
            return insensitive != 0 ? insensitive : string.CompareOrdinal(left, right);
        }

        /// <summary>把 container 路徑正規化成索引鍵；不屬於 json / 聲音資產的回 null。</summary>
        public static string NormalizeContainerPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            string lower = path.ToLowerInvariant();
            if (lower.StartsWith(JsonPrefix, StringComparison.Ordinal))
            {
                string stripped = path.Substring(JsonPrefix.Length);
                if (lower.EndsWith(".wzjson", StringComparison.Ordinal))
                {
                    return stripped.Substring(0, stripped.Length - ".wzjson".Length);
                }
                if (lower.EndsWith(".json", StringComparison.Ordinal))
                {
                    return stripped.Substring(0, stripped.Length - ".json".Length);
                }
                return null;
            }
            if (lower.StartsWith(SoundPrefix, StringComparison.Ordinal))
            {
                foreach (string suffix in SoundSuffixes)
                {
                    if (lower.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        string stripped = path.Substring(SoundPrefix.Length);
                        return "Sound/" + stripped.Substring(0, stripped.Length - suffix.Length);
                    }
                }
            }
            return null;
        }

        private static bool TryParseNumericName(string name, out long value)
        {
            value = 0;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] < '0' || name[i] > '9')
                {
                    return false;
                }
            }
            return long.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }

        private void IndexBundle(
            UnityFsBundle bundle, int bundleIndex, string bundleStem,
            Dictionary<long, UnityEntryLocation> objectLocations)
        {
            IReadOnlyList<UnityFsBundle.BundleNode> nodes = bundle.Nodes;
            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                if (!nodes[nodeIndex].IsSerializedFile)
                {
                    continue;
                }

                byte[] head;
                int metadataLength;
                try
                {
                    head = bundle.ReadNodeRange(nodeIndex, 0, UnitySerializedFile.HeaderSize);
                    metadataLength = UnitySerializedFile.GetMetadataLength(head);
                }
                catch (Exception ex) when (ex is UnityException || ex is ArgumentException || ex is OverflowException)
                {
                    continue;
                }

                UnitySerializedFile serializedFile;
                try
                {
                    head = bundle.ReadNodeRange(nodeIndex, 0, metadataLength);
                    serializedFile = UnitySerializedFile.Parse(head);
                }
                catch (Exception ex) when (ex is UnityException || ex is ArgumentException || ex is OverflowException)
                {
                    continue;
                }

                Dictionary<long, UnityObjectInfo> objectsByPathId =
                    new Dictionary<long, UnityObjectInfo>(serializedFile.Objects.Count);
                foreach (UnityObjectInfo obj in serializedFile.Objects)
                {
                    objectsByPathId[obj.PathId] = obj;
                }

                foreach (UnityObjectInfo assetBundleObject in serializedFile.ObjectsByClass(UnityClassIds.AssetBundle))
                {
                    long objectOffset = serializedFile.DataOffset + assetBundleObject.ByteStart;
                    if (objectOffset < serializedFile.DataOffset || assetBundleObject.ByteSize > int.MaxValue)
                    {
                        throw new UnityException(string.Format(
                            "Invalid AssetBundle object offset: bundle={0}, node={1}", bundleStem, nodeIndex));
                    }

                    UnityAssetBundleInfo assetBundle;
                    try
                    {
                        byte[] data = bundle.ReadNodeRange(
                            nodeIndex, objectOffset, (int)assetBundleObject.ByteSize);
                        assetBundle = UnityObjectParsers.ParseAssetBundle(data, serializedFile.IsLittleEndian);
                    }
                    catch (UnityException ex)
                    {
                        throw new UnityException(string.Format(
                            "Failed to parse the AssetBundle object: bundle={0}, node={1}: {2}",
                            bundleStem, nodeIndex, ex.Message), ex);
                    }

                    this.Image.CollectFromContainer(
                        bundle, bundleIndex, bundleStem, nodeIndex,
                        serializedFile, assetBundle.Container, objectsByPathId,
                        objectLocations, this.ReadEntryBytes);

                    foreach (UnityContainerEntry containerEntry in assetBundle.Container)
                    {
                        string key = NormalizeContainerPath(containerEntry.Path);
                        if (key == null)
                        {
                            continue;
                        }
                        if (containerEntry.Asset.FileId != 0)
                        {
                            continue; // 外部參照不在此 bundle，略過
                        }
                        UnityObjectInfo obj;
                        if (!objectsByPathId.TryGetValue(containerEntry.Asset.PathId, out obj))
                        {
                            continue;
                        }
                        long absolute = serializedFile.DataOffset + obj.ByteStart;
                        if (absolute < serializedFile.DataOffset)
                        {
                            continue;
                        }
                        this.Entries[key] = new UnityEntryLocation(
                            bundleIndex, nodeIndex, absolute, obj.ByteSize, obj.ClassId);
                    }
                }
            }
        }
    }
}

