using System;
using System.Collections.Generic;
using WzComparerR2.UnityLib.Images;
using WzComparerR2.UnityLib.Unity;
using WzComparerR2.UnityLib.Wzjs;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// 圖片查詢與渲染：WZJS 的 Canvas / UOL 節點 → PNG。
    /// <para>
    /// spritesheet 由 WZSS（舊版）或 SpriteSheet <c>.asset</c>（新版）索引 + atlas 頁
    /// Texture2D 組成；spriteset 則由 sprite 清單 MonoBehaviour + Sprite 物件組成。
    /// 只有目標 sprite 所在的 atlas 頁會被解碼。
    /// </para>
    /// </summary>
    public sealed class UnityImageRenderer
    {
        private const int ImageCacheCapacity = 64;
        private const long ImageCacheBytes = 64L * 1024 * 1024;
        private const long PagePixelCacheBytes = 128L * 1024 * 1024;

        private readonly ClassicCatalog catalog;
        private readonly IReadOnlyList<UnityFsBundle> bundles;
        private readonly Func<string, WzjsNode> treeProvider;
        private readonly object syncRoot = new object();
        private readonly Dictionary<string, byte[]> imageCache = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        private readonly List<string> imageCacheOrder = new List<string>();
        private long imageCacheBytes;
        private readonly Dictionary<string, byte[]> pagePixelCache = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        private readonly List<string> pagePixelCacheOrder = new List<string>();
        private long pagePixelCacheBytes;
        private const int SheetCacheCapacity = 48;
        private readonly Dictionary<string, object> sheetCache = new Dictionary<string, object>(StringComparer.Ordinal);
        private readonly List<string> sheetCacheOrder = new List<string>();

        /// <summary>兄弟 img のシートを横断検索するときの上限（巨大ディレクトリでの索引構築を避ける）。</summary>
        private const int MaxCrossSheetSiblings = 256;
        private readonly object crossSheetLock = new object();
        private readonly Dictionary<string, Dictionary<string, string>> crossSheetIndex =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        public UnityImageRenderer(
            ClassicCatalog catalog, IReadOnlyList<UnityFsBundle> bundles,
            Func<string, WzjsNode> treeProvider, string locale)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }
            if (bundles == null)
            {
                throw new ArgumentNullException(nameof(bundles));
            }
            if (treeProvider == null)
            {
                throw new ArgumentNullException(nameof(treeProvider));
            }
            this.catalog = catalog;
            this.bundles = bundles;
            this.treeProvider = treeProvider;
            this.Locale = locale;
        }

        public string Locale { get; set; }

        /// <summary>清空渲染快取（切換語系後應呼叫）。</summary>
        public void ClearCache()
        {
            lock (this.syncRoot)
            {
                this.imageCache.Clear();
                this.imageCacheOrder.Clear();
                this.imageCacheBytes = 0;
            }
            lock (this.pagePixelCache)
            {
                this.pagePixelCache.Clear();
                this.pagePixelCacheOrder.Clear();
                this.pagePixelCacheBytes = 0;
            }
            lock (this.sheetCache)
            {
                this.sheetCache.Clear();
                this.sheetCacheOrder.Clear();
            }
            lock (this.crossSheetLock)
            {
                this.crossSheetIndex.Clear();
            }
        }

        /// <summary>
        /// spritesheet 索引データを解析します（キャッシュがあればそれを使用）。1 つの spritesheet には通常大量の sprite が含まれるため、
        /// canvas ごとに再解析するとバッチ処理が非常に遅くなります。
        /// </summary>
        private T GetParsedSheet<T>(UnityEntryLocation location, Func<byte[], T> parser) where T : class
        {
            string key = string.Format("{0}:{1}:{2}:{3}",
                location.BundleIndex, location.NodeIndex, location.AbsoluteOffset, location.ByteSize);
            lock (this.sheetCache)
            {
                object cached;
                if (this.sheetCache.TryGetValue(key, out cached))
                {
                    this.sheetCacheOrder.Remove(key);
                    this.sheetCacheOrder.Add(key);
                    return cached as T;
                }
            }

            byte[] payload = this.ReadEntry(location);
            UnityMonoBehaviourInfo mono = UnityObjectParsers.ParseMonoBehaviour(payload, true);
            if (mono.HeaderLength > payload.Length)
            {
                throw new UnityException("The SpriteSheet MonoBehaviour header is out of range.");
            }
            T parsed = parser(Slice(payload, mono.HeaderLength));

            lock (this.sheetCache)
            {
                this.sheetCache[key] = parsed;
                this.sheetCacheOrder.Add(key);
                while (this.sheetCacheOrder.Count > SheetCacheCapacity)
                {
                    string evict = this.sheetCacheOrder[0];
                    this.sheetCacheOrder.RemoveAt(0);
                    this.sheetCache.Remove(evict);
                }
            }
            return parsed;
        }

        /// <summary>
        /// atlas ページの RGBA ピクセルをデコードします（キャッシュがあればそれを使用）。同じページは複数の canvas で共有されるため、
        /// キャッシュがないと canvas ごとにページ全体を再デコードするため、バッチ処理（例: 比較ツール）が非常に遅くなります。
        /// </summary>
        private byte[] GetAtlasPixels(UnityEntryLocation location, UnityTexture2DInfo texture, byte[] data)
        {
            string key = string.Format("{0}:{1}:{2}:{3}",
                location.BundleIndex, location.NodeIndex, location.AbsoluteOffset, location.ByteSize);
            lock (this.pagePixelCache)
            {
                byte[] cached;
                if (this.pagePixelCache.TryGetValue(key, out cached))
                {
                    this.pagePixelCacheOrder.Remove(key);
                    this.pagePixelCacheOrder.Add(key);
                    return cached;
                }
            }

            UnityFsBundle bundle = this.bundles[location.BundleIndex];
            byte[] rgba = UnityTexture.FetchPixels(bundle, texture, data);
            if (rgba == null || rgba.Length > PagePixelCacheBytes)
            {
                return rgba;
            }

            lock (this.pagePixelCache)
            {
                byte[] existing;
                if (this.pagePixelCache.TryGetValue(key, out existing))
                {
                    return existing;
                }
                while (this.pagePixelCacheOrder.Count > 0
                    && this.pagePixelCacheBytes + rgba.Length > PagePixelCacheBytes)
                {
                    string evict = this.pagePixelCacheOrder[0];
                    this.pagePixelCacheOrder.RemoveAt(0);
                    byte[] removed;
                    if (this.pagePixelCache.TryGetValue(evict, out removed))
                    {
                        this.pagePixelCacheBytes -= removed.Length;
                        this.pagePixelCache.Remove(evict);
                    }
                }
                this.pagePixelCache[key] = rgba;
                this.pagePixelCacheOrder.Add(key);
                this.pagePixelCacheBytes += rgba.Length;
            }
            return rgba;
        }

        /// <summary>讀取 <see cref="UnityEntryLocation"/> 指向的物件 bytes。</summary>
        public byte[] ReadEntry(UnityEntryLocation location)
        {
            this.ValidateLocation(location);
            UnityFsBundle bundle = this.bundles[location.BundleIndex];
            return bundle.ReadNodeRange(location.NodeIndex, location.AbsoluteOffset, (int)location.ByteSize);
        }

        /// <summary>取 canvas 或 uol 節點的圖片（PNG bytes）。查無圖時回 null。</summary>
        public byte[] GetImage(string path)
        {
            byte[] cached = this.TryGetCachedImage(path);
            if (cached != null)
            {
                return cached;
            }
            byte[] image = this.GetImageUncached(path);
            if (image != null)
            {
                return this.CacheImage(path, image);
            }
            return null;
        }

        /// <summary>
        /// 精確 frame 無法由 WZSS 節點鍵解出時，提供完整 atlas 頁作為預覽。
        /// 僅接受已驗證的 Canvas 或 UOL 內部節點；不讓 img 根或一般 SubTree 誤顯示 atlas。
        /// </summary>
        public byte[] GetCanvasAtlasFallback(string path)
        {
            string imageKey;
            string inner;
            if (!this.catalog.TrySplitImagePrefix(path, out imageKey, out inner) || inner.Length == 0)
            {
                return null;
            }
            WzjsNode node = this.treeProvider(imageKey).Find(inner);
            if (node == null || !node.IsImageCapable)
            {
                return null;
            }

            List<string> candidates = new List<string>();
            candidates.Add(imageKey + "/" + inner);
            int slash = imageKey.LastIndexOf('/');
            if (slash >= 0)
            {
                candidates.Add(imageKey.Substring(0, slash) + "/" + inner);
            }
            candidates.Add(imageKey);

            foreach (string candidate in candidates)
            {
                UnitySheetEntry entry = this.GetSheet(candidate);
                if (entry == null || entry.AtlasPages.Count == 0)
                {
                    continue;
                }
                return this.RenderAtlasPage(entry.AtlasPages[0].Value);
            }
            return null;
        }

        private byte[] GetImageUncached(string path)
        {
            // spriteset 素材（TW/UI/...、Common/UI/...）：不在 catalog.Entries，
            // SetAssets のキーで「地域プレフィックス + 前方一致」を行います。ロケール優先、次に Common。
            foreach (SetAssetLookup lookup in this.GetSetAssetLookups(path))
            {
                Dictionary<string, UnitySetSpriteEntry> asset;
                asset = this.catalog.Image.FindSpriteAsset(lookup.Key);
                if (asset == null)
                {
                    continue;
                }
                UnitySetSpriteEntry spriteEntry = PickSetSprite(asset, lookup.Inner);
                if (spriteEntry != null)
                {
                    return this.RenderSetSprite(spriteEntry);
                }
            }

            string imageKey;
            string inner;
            if (!this.catalog.TrySplitImagePrefix(path, out imageKey, out inner))
            {
                return null;
            }

            // 圖片只屬於 Canvas 與 UOL（UOL 在 WZSS 節點鍵池裡也有自己的槽）。
            // 在查索引前先以 WZJS 樹明確限縮。
            if (inner.Length > 0)
            {
                WzjsNode node = this.treeProvider(imageKey).Find(inner);
                if (node == null || !node.IsImageCapable)
                {
                    return null;
                }
            }

            List<string> candidates = new List<string>();
            if (inner.Length > 0)
            {
                candidates.Add(imageKey + "/" + inner);
                int slash = imageKey.LastIndexOf('/');
                if (slash >= 0)
                {
                    candidates.Add(imageKey.Substring(0, slash) + "/" + inner);
                }
            }
            candidates.Add(imageKey);

            foreach (string candidate in candidates)
            {
                Dictionary<string, UnitySheetEntry> variants;
                if (!this.catalog.Image.SheetsByPath.TryGetValue(candidate, out variants))
                {
                    continue;
                }
                UnitySheetEntry entry = UnityImageIndex.SelectLocalized(variants, this.Locale);
                if (entry == null)
                {
                    continue;
                }
                // 地域バリエーション（例: Map/Back/blueCave2 の CN）にはその地域で変更された sprite のみが含まれるため、
                // それ以外のノードは COMMON の索引にフォールバックします。
                UnitySheetEntry fallback = null;
                UnitySheetEntry common;
                if (variants.TryGetValue("COMMON", out common) && !ReferenceEquals(common, entry))
                {
                    fallback = common;
                }
                bool perImage = string.Equals(candidate, imageKey, StringComparison.Ordinal);
                byte[] sheetImage = this.RenderSheet(entry, fallback, candidate, imageKey, inner, perImage);
                if (sheetImage != null)
                {
                    return sheetImage;
                }
                // この img のシートに sprite が無い場合は他の候補・兄弟シートを試します。
            }

            Dictionary<string, UnitySetSpriteEntry> setAsset = this.catalog.Image.FindSpriteAsset(imageKey);
            if (setAsset != null)
            {
                UnitySetSpriteEntry spriteEntry = PickSetSprite(setAsset, inner);
                if (spriteEntry != null)
                {
                    return this.RenderSetSprite(spriteEntry);
                }
            }

            // Classic World のクライアントは img の一部の sprite を別 img の atlas にしか持たないことがあります
            // （例: Character/PetEquip/01802000.img/5000036/angry/0 は 01802006 のシートにのみ存在）。
            // 自 img のシートで見つからない場合は、同じ親ディレクトリの兄弟シートから解決します。
            return this.TryRenderFromSiblingSheet(imageKey, inner);
        }

        /// <summary>
        /// 兄弟 img の spritesheet から sprite を解決します（見つからない場合は null）。
        /// </summary>
        private byte[] TryRenderFromSiblingSheet(string imageKey, string inner)
        {
            if (string.IsNullOrEmpty(imageKey) || string.IsNullOrEmpty(inner))
            {
                return null;
            }
            int slash = imageKey.LastIndexOf('/');
            if (slash <= 0)
            {
                return null;
            }
            string sheetKey;
            if (!this.GetSiblingSheetIndex(imageKey.Substring(0, slash)).TryGetValue(inner, out sheetKey)
                || string.Equals(sheetKey, imageKey, StringComparison.Ordinal))
            {
                return null;
            }
            Dictionary<string, UnitySheetEntry> variants;
            if (!this.catalog.Image.SheetsByPath.TryGetValue(sheetKey, out variants))
            {
                return null;
            }
            UnitySheetEntry entry = UnityImageIndex.SelectLocalized(variants, this.Locale);
            if (entry == null)
            {
                return null;
            }
            try
            {
                return this.RenderSheetEntry(entry, sheetKey, imageKey, inner, perImage: true);
            }
            catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException)
            {
                // 壊れたシートは他の兄弟シートの探索を妨げません。
                return null;
            }
        }

        /// <summary>
        /// 親ディレクトリ直下の兄弟 img シートを横断する「sprite 名 → シート key」索引を返します（初回のみ構築）。
        /// </summary>
        private Dictionary<string, string> GetSiblingSheetIndex(string parentPath)
        {
            lock (this.crossSheetLock)
            {
                Dictionary<string, string> index;
                if (this.crossSheetIndex.TryGetValue(parentPath, out index))
                {
                    return index;
                }

                index = new Dictionary<string, string>(StringComparer.Ordinal);
                string prefix = parentPath + "/";
                List<string> sheetKeys = new List<string>();
                foreach (string key in this.catalog.Image.SheetsByPath.Keys)
                {
                    if (key.Length <= prefix.Length || !key.StartsWith(prefix, StringComparison.Ordinal)
                        || key.IndexOf('/', prefix.Length) >= 0)
                    {
                        continue; // 直下の子のみ
                    }
                    sheetKeys.Add(key);
                }

                if (sheetKeys.Count <= MaxCrossSheetSiblings)
                {
                    sheetKeys.Sort(StringComparer.Ordinal);
                    foreach (string sheetKey in sheetKeys)
                    {
                        UnitySheetEntry entry = UnityImageIndex.SelectLocalized(
                            this.catalog.Image.SheetsByPath[sheetKey], this.Locale);
                        if (entry == null || entry.IndexLocation == null || !entry.IndexIsAsset)
                        {
                            continue;
                        }
                        SpriteSheetAsset asset;
                        try
                        {
                            asset = this.GetParsedSheet(entry.IndexLocation, SpriteSheetAsset.Parse);
                        }
                        catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException)
                        {
                            continue;
                        }
                        if (asset == null)
                        {
                            continue;
                        }
                        foreach (string name in asset.EnumerateNames())
                        {
                            if (!index.ContainsKey(name))
                            {
                                index[name] = sheetKey;
                            }
                        }
                    }
                }

                this.crossSheetIndex[parentPath] = index;
                return index;
            }
        }

        /// <summary>spriteset の 1 回分の検索条件: (asset 索引キー, sprite 名)。</summary>
        private struct SetAssetLookup
        {
            public SetAssetLookup(string key, string inner)
            {
                this.Key = key;
                this.Inner = inner;
            }

            public string Key { get; private set; }
            public string Inner { get; private set; }
        }

        /// <summary>
        /// spriteset の候補索引キーを列挙します: まずパス自体を試し（<c>Common/…</c>、<c>TW/…</c> プレフィックス込み）、
        /// 続いて「現在のロケール → Common」の順でプレフィックスを補います。地域プレフィックスは別ロケールへフォールバックせず、誤った地域の画像表示を防ぎます。
        /// </summary>
        private List<SetAssetLookup> GetSetAssetLookups(string path)
        {
            List<SetAssetLookup> lookups = new List<SetAssetLookup>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            CollectSetAssetLookups(lookups, seen, path);

            string localePrefix = FindSetPrefix(this.Locale);
            if (localePrefix != null)
            {
                CollectSetAssetLookups(lookups, seen, localePrefix + "/" + path);
            }
            string commonPrefix = FindSetPrefix("Common");
            if (commonPrefix != null)
            {
                CollectSetAssetLookups(lookups, seen, commonPrefix + "/" + path);
            }
            return lookups;
        }

        private string FindSetPrefix(string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                return null;
            }
            foreach (string prefix in this.catalog.Image.SetPrefixes)
            {
                if (string.Equals(prefix, locale, StringComparison.OrdinalIgnoreCase))
                {
                    return prefix;
                }
            }
            return null;
        }

        private void CollectSetAssetLookups(
            List<SetAssetLookup> lookups, HashSet<string> seen, string candidatePath)
        {
            string key;
            string inner;
            if (!TrySplitSetAssetPrefix(this.catalog, candidatePath, out key, out inner))
            {
                return;
            }
            if (seen.Add(key + "\n" + inner))
            {
                lookups.Add(new SetAssetLookup(key, inner));
            }
        }

        private UnitySheetEntry GetSheet(string key)
        {
            Dictionary<string, UnitySheetEntry> variants;
            if (!this.catalog.Image.SheetsByPath.TryGetValue(key, out variants))
            {
                return null;
            }
            return UnityImageIndex.SelectLocalized(variants, this.Locale);
        }

        /// <summary>spritesheet → PNG（frame 切圖；映射未知時只有 img 根可輸出整頁）。</summary>
        private byte[] RenderSheet(
            UnitySheetEntry entry, UnitySheetEntry fallback, string sheetKey,
            string imageKey, string inner, bool perImage)
        {
            byte[] image = this.RenderSheetEntry(entry, sheetKey, imageKey, inner, perImage);
            if (image == null && fallback != null && inner.Length > 0)
            {
                // 地域バリエーションにはその地域で変更された sprite のみが含まれるため、不足するノードは COMMON バリエーションから画像を取得します。
                image = this.RenderSheetEntry(fallback, sheetKey, imageKey, inner, perImage);
            }
            // アトラスにこのノードの sprite がない場合は null を返します（データセット側に画像がないだけで、エラーではありません）。
            return image;
        }

        /// <summary>
        /// ノードパスのフレーム候補。WZJS ツリーには <c>…/CN/0</c> のような地域マーカーのサブツリーが現れることがあり、
        /// ただしアトラスのノードキーには地域セグメントが含まれません。また一部のノードは単一フレームのディレクトリです（<c>…/3</c> ↔ <c>…/3/0</c>）。
        /// </summary>
        private static List<string> GetFrameInnerCandidates(string inner)
        {
            List<string> candidates = new List<string>(4);
            if (inner.Length == 0)
            {
                candidates.Add(inner);
                return candidates;
            }

            candidates.Add(inner);
            candidates.Add(inner + "/0");
            if (inner.EndsWith("/0", StringComparison.Ordinal))
            {
                candidates.Add(inner.Substring(0, inner.Length - 2));
            }

            // 地域セグメント（CN/TW/KR/JP/COMMON）を除いたバリエーションも追加します。
            int count = candidates.Count;
            for (int i = 0; i < count; i++)
            {
                string stripped = StripRegionSegments(candidates[i]);
                if (stripped != null && !candidates.Contains(stripped))
                {
                    candidates.Add(stripped);
                }
            }
            return candidates;
        }

        private static string StripRegionSegments(string inner)
        {
            string[] segments = inner.Split('/');
            List<string> kept = new List<string>(segments.Length);
            bool changed = false;
            foreach (string segment in segments)
            {
                if (IsRegionSegment(segment))
                {
                    changed = true;
                    continue;
                }
                kept.Add(segment);
            }
            if (!changed || kept.Count == 0)
            {
                return null;
            }
            return string.Join("/", kept);
        }

        private static bool IsRegionSegment(string segment)
        {
            return string.Equals(segment, "CN", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segment, "TW", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segment, "KR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segment, "JP", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segment, "COMMON", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>単一のアトラスバリエーションで PNG を出力します。対応する frame がない場合は null。</summary>
        private byte[] RenderSheetEntry(
            UnitySheetEntry entry, string sheetKey, string imageKey, string inner, bool perImage)
        {
            if (entry.IndexLocation == null)
            {
                throw new UnityException("The spritesheet has no image index.");
            }

            WzssSprite? sprite;
            if (entry.IndexIsAsset)
            {
                SpriteSheetAsset asset = this.GetParsedSheet(entry.IndexLocation, SpriteSheetAsset.Parse);
                if (!string.Equals(asset.Path, sheetKey, StringComparison.Ordinal))
                {
                    throw new UnityException(string.Format(
                        "The SpriteSheet asset path does not match: {0} != {1}", asset.Path, sheetKey));
                }
                WzssSprite found = default(WzssSprite);
                bool foundSprite = false;
                if (inner.Length > 0)
                {
                    foreach (string candidateInner in GetFrameInnerCandidates(inner))
                    {
                        if (asset.TryGetSpriteForNode(candidateInner, out found))
                        {
                            foundSprite = true;
                            break;
                        }
                    }
                }
                if (foundSprite)
                {
                    sprite = found;
                }
                else if (!perImage && inner.Length > 0 && asset.TryGetFirstSprite(out found))
                {
                    sprite = found;
                }
                else
                {
                    sprite = null;
                }
            }
            else
            {
                WzssIndex wzss = this.GetParsedSheet(entry.IndexLocation, WzssIndex.Parse);
                // 舊版 WZSS 用節點鍵定位，少數 UOL 需要反推。
                int frame = -1;
                bool hasFrame = false;
                if (inner.Length > 0)
                {
                    foreach (string candidateInner in GetFrameInnerCandidates(inner))
                    {
                        if (wzss.TryGetFrameIndexForNode(candidateInner, out frame))
                        {
                            hasFrame = true;
                            break;
                        }
                    }
                }
                if (!hasFrame)
                {
                    if (perImage && inner.Length > 0)
                    {
                        int inferred;
                        if (TryInferFrameFromUol(this.treeProvider(imageKey), inner, wzss.UolLinks, out inferred))
                        {
                            frame = inferred;
                            hasFrame = true;
                        }
                    }
                    else if (wzss.Rects.Count > 0)
                    {
                        frame = 0;
                        hasFrame = true;
                    }
                }
                WzssSprite wzssSprite;
                sprite = hasFrame && wzss.TryGetSprite(frame, out wzssSprite) ? wzssSprite : (WzssSprite?)null;
            }

            // 只解碼目標 sprite 所在的 atlas 頁。
            int? wantedPage = sprite.HasValue ? sprite.Value.Page : (int?)null;
            List<AtlasPage> pages = new List<AtlasPage>();
            foreach (KeyValuePair<int, UnityEntryLocation> page in entry.AtlasPages)
            {
                bool needed = wantedPage.HasValue
                    ? page.Key == wantedPage.Value
                    // 沒有 frame 時只有 img 根會用到第一頁。
                    : inner.Length == 0 && pages.Count == 0;
                if (!needed)
                {
                    continue;
                }
                byte[] data = this.ReadEntry(page.Value);
                UnityTexture2DInfo texture = UnityObjectParsers.ParseTexture2D(data, true);
                byte[] rgba = this.GetAtlasPixels(page.Value, texture, data);
                pages.Add(new AtlasPage(page.Key, texture.Width, texture.Height, rgba));
            }

            if (sprite.HasValue)
            {
                return WzssIndex.CutSprite(pages, sprite.Value).ToPng();
            }
            if (inner.Length == 0)
            {
                if (pages.Count == 0)
                {
                    throw new UnityException("The spritesheet has no atlas page.");
                }
                AtlasPage page = pages[0];
                // キャッシュ内のピクセルは直接反転できないため、コピーしてから処理します。
                byte[] pixels = (byte[])page.Rgba.Clone();
                UnityTexture.FlipVertical(pixels, page.Width, page.Height);
                return UnityTexture.ToPng(page.Width, page.Height, pixels);
            }
            return null;
        }

        private byte[] RenderAtlasPage(UnityEntryLocation location)
        {
            byte[] data = this.ReadEntry(location);
            UnityTexture2DInfo texture = UnityObjectParsers.ParseTexture2D(data, true);
            byte[] rgba = (byte[])this.GetAtlasPixels(location, texture, data).Clone();
            UnityTexture.FlipVertical(rgba, texture.Width, texture.Height);
            return UnityTexture.ToPng(texture.Width, texture.Height, rgba);
        }

        /// <summary>spriteset sprite → PNG（m_Rect、または atlas 詰めの切り出し矩形で切圖）。</summary>
        private byte[] RenderSetSprite(UnitySetSpriteEntry entry)
        {
            byte[] spriteData = this.ReadEntry(entry.SpriteLocation);
            UnitySpriteInfo sprite = UnityObjectParsers.ParseSprite(spriteData, true);
            UnityEntryLocation location = entry.TextureLocation;
            byte[] data = this.ReadEntry(location);
            UnityTexture2DInfo texture = UnityObjectParsers.ParseTexture2D(data, true);

            int x;
            int y;
            int width;
            int height;
            SpriteRectToPixels(entry.CropRect ?? sprite.Rect, out x, out y, out width, out height);
            byte[] rgba = this.GetAtlasPixels(location, texture, data);
            byte[] cropped = UnityTexture.CropRgba(texture.Width, texture.Height, rgba, x, y, width, height);
            UnityTexture.FlipVertical(cropped, width, height);
            return UnityTexture.ToPng(width, height, cropped);
        }

        private void ValidateLocation(UnityEntryLocation location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }
            if (location.BundleIndex < 0 || location.BundleIndex >= this.bundles.Count)
            {
                throw new UnityException(string.Format(
                    "bundle index {0} is out of range (total {1})", location.BundleIndex, this.bundles.Count));
            }
            UnityFsBundle bundle = this.bundles[location.BundleIndex];
            if (location.NodeIndex < 0 || location.NodeIndex >= bundle.Nodes.Count)
            {
                throw new UnityException(string.Format(
                    "node index {0} is out of range (total {1})", location.NodeIndex, bundle.Nodes.Count));
            }
            UnityFsBundle.BundleNode node = bundle.Nodes[location.NodeIndex];
            long end = location.AbsoluteOffset + location.ByteSize;
            if (location.AbsoluteOffset < 0 || end > node.Size)
            {
                throw new UnityException(string.Format(
                    "The object range {0}..{1} exceeds the {2} bytes of the node.",
                    location.AbsoluteOffset, end, node.Size));
            }
        }

        private byte[] TryGetCachedImage(string path)
        {
            lock (this.syncRoot)
            {
                byte[] image;
                if (!this.imageCache.TryGetValue(path, out image))
                {
                    return null;
                }
                this.imageCacheOrder.Remove(path);
                this.imageCacheOrder.Add(path);
                return image;
            }
        }

        private byte[] CacheImage(string path, byte[] image)
        {
            if (image.Length > ImageCacheBytes)
            {
                return image;
            }
            lock (this.syncRoot)
            {
                while (this.imageCacheOrder.Count > 0
                    && (this.imageCacheOrder.Count >= ImageCacheCapacity
                        || this.imageCacheBytes + image.Length > ImageCacheBytes))
                {
                    string evict = this.imageCacheOrder[0];
                    this.imageCacheOrder.RemoveAt(0);
                    byte[] removed;
                    if (this.imageCache.TryGetValue(evict, out removed))
                    {
                        this.imageCache.Remove(evict);
                        this.imageCacheBytes -= removed.Length;
                    }
                }
                byte[] previous;
                if (this.imageCache.TryGetValue(path, out previous))
                {
                    this.imageCacheBytes -= previous.Length;
                    this.imageCacheOrder.Remove(path);
                }
                this.imageCache[path] = image;
                this.imageCacheBytes += image.Length;
                this.imageCacheOrder.Add(path);
                return image;
            }
        }

        private static byte[] Slice(byte[] data, int offset)
        {
            if (offset < 0 || offset > data.Length)
            {
                throw new UnityException("The payload offset is out of range.");
            }
            byte[] result = new byte[data.Length - offset];
            Buffer.BlockCopy(data, offset, result, 0, result.Length);
            return result;
        }

        private static bool TrySplitSetAssetPrefix(
            ClassicCatalog catalog, string path, out string key, out string inner)
        {
            key = null;
            inner = null;
            if (path == null)
            {
                return false;
            }
            string current = path;
            while (true)
            {
                if (catalog.Image.FindSpriteAsset(current) != null)
                {
                    key = current;
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

        private static UnitySetSpriteEntry PickSetSprite(
            Dictionary<string, UnitySetSpriteEntry> asset, string inner)
        {
            if (string.IsNullOrEmpty(inner))
            {
                foreach (UnitySetSpriteEntry entry in asset.Values)
                {
                    return entry;
                }
                return null;
            }
            UnitySetSpriteEntry found;
            if (asset.TryGetValue(inner, out found))
            {
                return found;
            }
            string suffix = "/" + inner;
            foreach (KeyValuePair<string, UnitySetSpriteEntry> item in asset)
            {
                if (item.Key.EndsWith(suffix, StringComparison.Ordinal))
                {
                    return item.Value;
                }
            }
            int slash = inner.LastIndexOf('/');
            string last = slash < 0 ? inner : inner.Substring(slash + 1);
            if (asset.TryGetValue(last, out found))
            {
                return found;
            }
            foreach (KeyValuePair<string, UnitySetSpriteEntry> item in asset)
            {
                int itemSlash = item.Key.LastIndexOf('/');
                string itemLast = itemSlash < 0 ? item.Key : item.Key.Substring(itemSlash + 1);
                if (string.Equals(itemLast, last, StringComparison.Ordinal))
                {
                    return item.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// WZSS 的直接 Canvas 鍵是遊戲自訂 hash，rect 也以該 hash 排序，不能用
        /// WZJSON DFS 順序硬配。若同一條原始 UOL 在 WZSS 中一致指向單一 frame，
        /// 則可從 WZJSON 解出其目標 Canvas，得到可驗證的精確映射。
        /// </summary>
        private static bool TryInferFrameFromUol(
            WzjsNode root, string target, IReadOnlyList<WzssUolLink> links, out int frame)
        {
            frame = -1;
            Dictionary<string, SortedSet<int?>> framesByUol =
                new Dictionary<string, SortedSet<int?>>(StringComparer.Ordinal);
            foreach (WzssUolLink link in links)
            {
                SortedSet<int?> frames;
                if (!framesByUol.TryGetValue(link.Uol, out frames))
                {
                    frames = new SortedSet<int?>();
                    framesByUol[link.Uol] = frames;
                }
                frames.Add(link.FrameIndex);
            }

            SortedSet<int> candidates = new SortedSet<int>();
            // WZJSON 的根本身是資產；inner 從根的 children 開始，不含根名稱。
            foreach (WzjsNode child in root.Children)
            {
                WalkUol(child, string.Empty, target, framesByUol, candidates);
            }
            if (candidates.Count != 1)
            {
                return false;
            }
            foreach (int candidate in candidates)
            {
                frame = candidate;
            }
            return true;
        }

        private static void WalkUol(
            WzjsNode node, string path, string target,
            Dictionary<string, SortedSet<int?>> framesByUol, SortedSet<int> candidates)
        {
            string here = path.Length == 0 ? node.Name : path + "/" + node.Name;
            if (node.Value.Kind == WzjsValueKind.Uol)
            {
                string uol = node.Value.Text;
                if (string.Equals(ResolveUolTarget(here, uol), target, StringComparison.Ordinal))
                {
                    SortedSet<int?> frames;
                    if (framesByUol.TryGetValue(uol, out frames) && frames.Count == 1)
                    {
                        foreach (int? frame in frames)
                        {
                            if (frame.HasValue)
                            {
                                candidates.Add(frame.Value);
                            }
                        }
                    }
                }
            }
            foreach (WzjsNode child in node.Children)
            {
                WalkUol(child, here, target, framesByUol, candidates);
            }
        }

        /// <summary>UOL 是相對於該 UOL property 的父節點。</summary>
        internal static string ResolveUolTarget(string source, string uol)
        {
            List<string> segments = new List<string>();
            foreach (string part in source.Split('/'))
            {
                if (part.Length > 0)
                {
                    segments.Add(part);
                }
            }
            if (segments.Count == 0)
            {
                return null;
            }
            segments.RemoveAt(segments.Count - 1);
            foreach (string part in uol.Split('/'))
            {
                if (part.Length == 0 || part == ".")
                {
                    continue;
                }
                if (part == "..")
                {
                    if (segments.Count == 0)
                    {
                        return null;
                    }
                    segments.RemoveAt(segments.Count - 1);
                }
                else
                {
                    segments.Add(part);
                }
            }
            return string.Join("/", segments);
        }

        internal static void SpriteRectToPixels(UnityRectF rect, out int x, out int y, out int width, out int height)
        {
            x = ExactPixel(rect.X, "x", true);
            y = ExactPixel(rect.Y, "y", true);
            width = ExactPixel(rect.Width, "width", false);
            height = ExactPixel(rect.Height, "height", false);
        }

        private static int ExactPixel(float value, string label, bool allowZero)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0 || (!allowZero && value == 0))
            {
                throw new UnityException(string.Format("Invalid sprite rect {0}: {1}", label, value));
            }
            float rounded = (float)Math.Round(value);
            if (Math.Abs(value - rounded) > 0.001 || rounded > int.MaxValue)
            {
                throw new UnityException(string.Format(
                    "Sprite rect {0} is not made of valid integer pixels: {1}", label, value));
            }
            return (int)rounded;
        }
    }
}

