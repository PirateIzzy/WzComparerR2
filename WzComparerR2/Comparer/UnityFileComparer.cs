using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using WzComparerR2.UnityLib.Classic;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.Comparer
{
    /// <summary>
    /// Unity アセットノード（<see cref="WzComparerR2.WzLib.Wz_Node"/> 形式）専用のノード比較器。
    /// <para>
    /// Unity の画像／テキストアセットには従来の WZ のようなサイズやチェックサムがないため、実際の内容で差分を判定します:
    /// <see cref="UnityCanvas"/> は PNG の内容を、<see cref="UnityJsonAsset"/> は原文を比較し、
    /// 配列値（例: <c>origin</c> の 2 要素座標）は項目ごとに比較します。
    /// </para>
    /// </summary>
    public class UnityFileComparer : WzFileComparer
    {
        public override bool CompareData(object dataNew, object dataOld)
        {
            if (dataNew is UnityCanvas canvasNew && dataOld is UnityCanvas canvasOld)
            {
                return CompareCanvas(canvasNew, canvasOld);
            }

            if (dataNew is UnityJsonAsset jsonNew && dataOld is UnityJsonAsset jsonOld)
            {
                return string.Equals(jsonNew.Text, jsonOld.Text, StringComparison.Ordinal);
            }

            if (dataNew is UnityAudioClipInfo clipNew && dataOld is UnityAudioClipInfo clipOld)
            {
                return clipNew.Channels == clipOld.Channels
                    && clipNew.Frequency == clipOld.Frequency
                    && clipNew.Length == clipOld.Length
                    && clipNew.ResourceOffset == clipOld.ResourceOffset
                    && clipNew.ResourceSize == clipOld.ResourceSize;
            }

            if (dataNew is Array arrayNew && dataOld is Array arrayOld)
            {
                return CompareArray(arrayNew, arrayOld);
            }

            return base.CompareData(dataNew, dataOld);
        }

        private bool CompareArray(Array arrayNew, Array arrayOld)
        {
            if (arrayNew.Length != arrayOld.Length || arrayNew.Rank != arrayOld.Rank)
            {
                return false;
            }

            if (arrayNew.Rank == 1)
            {
                for (int i = 0; i < arrayNew.Length; i++)
                {
                    if (!this.CompareData(arrayNew.GetValue(i), arrayOld.GetValue(i)))
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        private bool CompareCanvas(UnityCanvas canvasNew, UnityCanvas canvasOld)
        {
            canvasNew.EnsureExternalPngInfo();
            canvasOld.EnsureExternalPngInfo();
            bool emptyNew = canvasNew.DataLength == 0;
            bool emptyOld = canvasOld.DataLength == 0;
            if (emptyNew || emptyOld)
            {
                return emptyNew && emptyOld;
            }

            switch (this.PngComparison)
            {
                case WzPngComparison.SizeOnly:
                    return canvasNew.Width == canvasOld.Width
                        && canvasNew.Height == canvasOld.Height;

                case WzPngComparison.Pixel:
                    return CompareCanvasPixels(canvasNew, canvasOld);

                case WzPngComparison.SizeAndDataLength:
                default:
                    return canvasNew.Width == canvasOld.Width
                        && canvasNew.Height == canvasOld.Height
                        && canvasNew.DataLength == canvasOld.DataLength;
            }
        }

        private bool CompareCanvasPixels(UnityCanvas canvasNew, UnityCanvas canvasOld)
        {
            try
            {
                using (Bitmap bmpNew = canvasNew.ExtractPng())
                using (Bitmap bmpOld = canvasOld.ExtractPng())
                {
                    if (bmpNew == null || bmpOld == null)
                    {
                        return bmpNew == null && bmpOld == null;
                    }
                    if (bmpNew.Width != bmpOld.Width || bmpNew.Height != bmpOld.Height)
                    {
                        return false;
                    }

                    byte[] pixelsNew = GetPixelBytes(bmpNew);
                    byte[] pixelsOld = GetPixelBytes(bmpOld);
                    if (pixelsNew == null || pixelsOld == null || pixelsNew.Length != pixelsOld.Length)
                    {
                        return false;
                    }
                    return BytesEqual(pixelsNew, pixelsOld);
                }
            }
            catch (Exception)
            {
                // デコードできない場合は PNG 内容の比較に切り替え、比較全体が中断しないようにします。
                byte[] dataNew = canvasNew.GetPng();
                byte[] dataOld = canvasOld.GetPng();
                return dataNew == null ? dataOld == null : BytesEqual(dataNew, dataOld);
            }
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static byte[] GetPixelBytes(Bitmap bitmap)
        {
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                byte[] pixels = new byte[Math.Abs(data.Stride) * data.Height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                return pixels;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}
