using System;

namespace WzComparerR2.WzLib
{
    /// <summary>
    /// PNG エンコードデータで画像を提供するノード値。
    /// <para>
    /// 従来の WZ の画像は <see cref="Wz_Png"/>（ピクセルは Wz ファイルのデータストリーム内）で、
    /// その他のソース（例: Unity アセットの Canvas ノード）はこのインターフェイスを実装するだけで、
    /// 既存の画像デコードと tooltip の流れをそのまま利用できます。
    /// </para>
    /// </summary>
    public interface IWzPngSource
    {
        /// <summary>PNG エンコードされたバイト列を取得します。画像がない場合は null。</summary>
        byte[] GetPngData();
    }
}
