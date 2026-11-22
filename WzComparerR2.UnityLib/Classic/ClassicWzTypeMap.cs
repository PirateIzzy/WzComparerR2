using System;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Classic データセットのディレクトリ → <see cref="Wz_Type"/> の対応。
    /// <para>
    /// Unity クライアントには WZ ファイルがありませんが、既存機能の多くは <c>Wz_File.Type</c> だけでノード種別を判断します
    /// （アイテム／装備／マップ／クエストの tooltip など）。ここではアセットパスの 1 セグメント目のディレクトリを
    /// 従来の WZ の型を返すことで、それらの機能が Classic ノードも扱えるようにします。
    /// </para>
    /// <para>
    /// 対応関係: <c>Character/*</c> → Character、<c>Item/*</c> → Item、<c>Mob/*</c> → Mob、
    /// <c>Npc/*</c> → Npc、<c>Map/*</c> → Map、<c>Skill/*</c> → Skill、<c>Quest/*</c> → Quest、
    /// <c>String/*</c> → String、<c>Sound/*</c> → Sound…という対応です。
    /// </para>
    /// </summary>
    public static class ClassicWzTypeMap
    {
        /// <summary>
        /// アセットパス（例: <c>Character/Cap/01002000</c> または <c>Character\Cap\01002000</c>）
        /// または単一のディレクトリ名から対応する <see cref="Wz_Type"/> を取得します。対応できない場合は <see cref="Wz_Type.Unknown"/> を返します。
        /// </summary>
        public static Wz_Type GetWzType(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return Wz_Type.Unknown;
            }

            int slash = assetPath.IndexOfAny(new[] { '/', '\\' });
            string root = slash < 0 ? assetPath : assetPath.Substring(0, slash);
            switch (root.ToLowerInvariant())
            {
                case "character":
                    return Wz_Type.Character;
                case "datamap":
                    return Wz_Type.DataMap;
                case "effect":
                    return Wz_Type.Effect;
                case "etc":
                    return Wz_Type.Etc;
                case "item":
                    return Wz_Type.Item;
                case "language":
                    return Wz_Type.Language;
                case "map":
                    return Wz_Type.Map;
                case "mob":
                    return Wz_Type.Mob;
                case "morph":
                    return Wz_Type.Morph;
                case "npc":
                    return Wz_Type.Npc;
                case "quest":
                    return Wz_Type.Quest;
                case "reactor":
                    return Wz_Type.Reactor;
                case "skill":
                    return Wz_Type.Skill;
                case "sound":
                    return Wz_Type.Sound;
                case "string":
                    return Wz_Type.String;
                case "tamingmob":
                    return Wz_Type.TamingMob;
                case "ui":
                    return Wz_Type.UI;
                default:
                    return Wz_Type.Unknown;
            }
        }

        /// <summary>
        /// <see cref="GetWzType(string)"/> の null 許容版: 対応できない場合は null を返します（外部型解決用）。
        /// </summary>
        public static Wz_Type? TryGetWzType(string assetPath)
        {
            Wz_Type type = GetWzType(assetPath);
            return type == Wz_Type.Unknown ? (Wz_Type?)null : type;
        }

        /// <summary>
        /// <see cref="GetWzType(string)"/> と同じですが、従来の WZ に対応する型が無いディレクトリ
        /// （例: <c>CashShopPreviewNew</c>、<c>Feature</c>、<c>GlobalMarketPreview</c>、<c>MapLogin</c>、
        /// <c>MonsterBook</c>、<c>StandardPDD</c>）を <see cref="Wz_Type.Base"/> として扱います。
        /// ルートディレクトリを型ごとにまとめる用途（比較ページの分類など）で使用します。
        /// </summary>
        public static Wz_Type GetWzTypeOrBase(string assetPath)
        {
            Wz_Type type = GetWzType(assetPath);
            return type == Wz_Type.Unknown ? Wz_Type.Base : type;
        }
    }
}
