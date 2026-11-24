using System;

namespace WzComparerR2.WzLib
{
    /// <summary>
    /// A node value that provides an image as PNG-encoded data.
    /// <para>
    /// Classic WZ images are <see cref="Wz_Png"/> (pixels live inside the WZ file's data stream); other
    /// sources (e.g. a Unity asset Canvas node) only need to implement this interface to reuse the
    /// existing image decoding and tooltip flow as-is.
    /// </para>
    /// </summary>
    public interface IWzPngSource
    {
        /// <summary>Gets the PNG-encoded bytes, or null when there is no image.</summary>
        byte[] GetPngData();
    }
}
