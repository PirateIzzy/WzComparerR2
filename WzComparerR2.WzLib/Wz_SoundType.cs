using System;

namespace WzComparerR2.WzLib
{
    public enum Wz_SoundType
    {
        Unknown = 0,
        Mp3,
        Pcm,
        Binary,

        /// <summary>Ogg Vorbis（Unity データセットの FSB5 を再パッケージした結果）。</summary>
        Ogg
    }
}
