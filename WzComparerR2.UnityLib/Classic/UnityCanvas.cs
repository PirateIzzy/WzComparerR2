using System;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// 一個 WZ-like 的 Canvas 節點值：代表「某個 Unity 資產路徑上的圖片」，
    /// PNG は初回使用時にデコードされます（従来の WZ の <c>Wz_Png</c> に相当）。
    /// <para>
    /// <see cref="Wz_Png"/> を継承することで、WZ 型しか扱わない既存コード（MapRender、プレビューツールなど）
    /// そのまま利用できます: 画像は <see cref="Wz_Png.PngDataProvider"/> が提供し、
    /// <see cref="Wz_Png.ExtractPng(int)"/> は WZ データストリームを読まず、この PNG をデコードします。
    /// </para>
    /// </summary>
    public sealed class UnityCanvas : Wz_Png, IWzPngSource
    {
        private readonly ClassicWzSource source;
        private readonly object syncRoot = new object();
        private byte[] png;
        private long pngGeneration = -1;

        public UnityCanvas(ClassicWzSource source, string path)
            : base(0, 0, 0, Wz_TextureFormat.ARGB8888, 0, 0, 0, 0, null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            this.source = source;
            this.Path = path;
            this.PngDataProvider = this.GetPng;
        }

        /// <summary>邏輯路徑，如 <c>Character/00002000/stand1/0/body</c>。</summary>
        public string Path { get; private set; }

        /// <summary>取得（並快取）PNG bytes；查無對應圖片時回 null。</summary>
        public byte[] GetPng()
        {
            long generation = this.source.LocaleGeneration;
            byte[] cached = this.png;
            if (cached != null && this.pngGeneration == generation)
            {
                return cached;
            }
            lock (this.syncRoot)
            {
                if (this.png == null || this.pngGeneration != generation)
                {
                    // ロケール切り替え後は同じ Canvas が別地域の画像に対応する場合があるため、再デコードが必要です。
                    this.png = this.source.GetImage(this.Path);
                    this.pngGeneration = generation;
                }
                return this.png;
            }
        }

        byte[] IWzPngSource.GetPngData()
        {
            return this.GetPng();
        }

        public override string ToString()
        {
            return "canvas " + this.Path;
        }
    }
}
