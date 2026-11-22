using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using DevComponents.Editors;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using WzComparerR2.UnityLib.Classic;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public partial class FrmMonsterBookBrowser : DevComponents.DotNetBar.Office2007Form
    {
        public FrmMonsterBookBrowser()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font(new FontFamily("Microsoft Sans Serif"), 8f);
#endif
            // 複数行エディットの描画矩形を中央寄せにして、検索ボックスのテキストを縦方向中央に表示します。
            this.txtSearch.HandleCreated += (s, e) => this.CenterSearchText();
            this.txtSearch.SizeChanged += (s, e) => this.CenterSearchText();
            this.txtSearch.FontChanged += (s, e) => this.CenterSearchText();
            // 説明欄は編集中コントロールのため完全な透過はできません。親パネルの色を追従させて透過に見せます。
            this.pnlMobRow1.BackColorChanged += (s, e) => this.ApplyMobDescriptionBackColor();
            this.ApplyMobDescriptionBackColor();
            // 読み込み中のアニメーションは右エリアの矩形に合わせて手動配置します。
            this.tlpRightArea.SizeChanged += (s, e) => this.UpdateLoadingOverlayBounds();
            this.picLoading.BackColor = this.tlpRightArea.BackColor;
        }

        private void ApplyMobDescriptionBackColor()
        {
            this.richMobDescription.BackColorRichTextBox = this.pnlMobRow1.BackColor;
            this.richMobDescription.BackColor = this.pnlMobRow1.BackColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int EM_SETRECT = 0xB3;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref NativeRect lParam);

        private void CenterSearchText()
        {
            if (!this.txtSearch.IsHandleCreated)
            {
                return;
            }

            int textHeight = this.txtSearch.Font.Height;
            int top = Math.Max(0, (this.txtSearch.ClientSize.Height - textHeight) / 2);
            NativeRect rect = new NativeRect
            {
                Left = 0,
                Top = top,
                Right = this.txtSearch.ClientSize.Width,
                Bottom = top + textHeight,
            };
            SendMessage(this.txtSearch.Handle, EM_SETRECT, IntPtr.Zero, ref rect);
            this.txtSearch.Invalidate();
        }

        private readonly List<MobListItem> mobItems = new List<MobListItem>();
        private readonly List<MapListItem> mapItems = new List<MapListItem>();
        private readonly List<ItemListItem> itemListItems = new List<ItemListItem>();
        private readonly Dictionary<int, string> mobSearchTextCache = new Dictionary<int, string>();
        private static Dictionary<int, List<MobMapSpawnEntry>> mobToMapIndex;
        private static bool mobToMapIndexBuilding;
        private static readonly object mobToMapIndexLock = new object();
        private static readonly Dictionary<int, string> mapMarkCache = new Dictionary<int, string>();
        private int? mapListMobID;
        private static readonly Font MobStatsFont = new Font("Segoe UI", 10f);
        private static readonly Font MobElemAttrFont = new Font("Microsoft Sans Serif", 10f);
        private readonly Label lblMobElemAttr = new Label
        {
            AutoSize = true,
            Name = "lblMobElemAttr",
            TabIndex = 0,
        };
        private readonly List<Bitmap> mobAnimationFrames = new List<Bitmap>();
        private readonly List<int> mobAnimationDelays = new List<int>();
        private Timer mobFilterTimer;
        private Timer mobAnimationTimer;
        private Bitmap mobAnimationPlaceholder;
        private int mobAnimationFrameIndex;
        private bool isUpdatingAnimationTypes;
        private bool isLoadingMobList;
        private readonly List<Bitmap> loadingFrames = new List<Bitmap>();
        private readonly List<int> loadingDelays = new List<int>();
        private readonly Stopwatch loadingFrameWatch = new Stopwatch();
        private bool loadingFramesLoaded;
        private int loadingFrameIndex;
        private long loadingNextFrameMs;

        public StringLinker stringLinker { get; set; }

        public MainForm _mainForm { get; set; }

        /// <summary>現在選択中のモブ。列 1 の詳細表示はこのアイテムを参照します。</summary>
        private MobListItem SelectedMob { get; set; }
        /// <summary>モブの選択が変わったときに発生します（列 1 の内容更新用）。</summary>
        public event EventHandler MobSelected;

        /// <summary>
        /// MonsterBook アセット（例: Unity クライアントの <c>/MonsterBook</c>）の内容ノード。
        /// 子ノード名がモンスター ID です。
        /// </summary>
        public Wz_Node MonsterBookNode { get; set; }

        /// <summary>
        /// 説明文の取得元となる文字列テーブル（Unity の <c>String/&lt;region&gt;/MonsterBook</c>）。
        /// 子ノード名がモンスター ID で、ノード値が説明文です。WZ では <c>null</c>。
        /// </summary>
        public Wz_Node MonsterBookStringNode { get; set; }

        public void LoadMobList()
        {
            if (this.isLoadingMobList)
            {
                return;
            }

            this.isLoadingMobList = true;
            try
            {
                this.SetLoadingVisible(true);
                this.BuildMobList();
            }
            finally
            {
                this.SetLoadingVisible(false);
                this.isLoadingMobList = false;
            }
        }

        /// <summary>フォーム表示後に一覧構築を遅延実行し、ローディング表示を先に描画させます。</summary>
        public void BeginLoadMobList()
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke((Action)this.LoadMobList);
            }
        }

        private void SetLoadingVisible(bool visible)
        {
            // 同じセルに重ねているため、ローディング中は一覧パネル自体を隠します
            // （BringToFront では TableLayoutPanel がラベルをセル外へ再配置してしまう）。
            this.lblLoading.Visible = visible;
            this.itemPanelMobList.Visible = !visible;
            this.txtSearch.Visible = !visible;

            // 読み込み中に白く見えるコントロール（説明欄・モブ画像・各一覧）も隠します。
            this.richMobDescription.Visible = !visible;
            this.picMobAnimation.Visible = !visible;
            this.itemPanelMapList.Visible = !visible;
            this.itemPanelItemList.Visible = !visible;

            if (visible)
            {
                this.StartLoadingAnimation();
                // 構築で UI スレッドが止まる前に「読み込み中...」を描画させます。
                this.lblLoading.Update();
            }
            else
            {
                this.StopLoadingAnimation();
            }
        }

        /// <summary>
        /// 読み込み中のアニメーション（<c>Npc/0002006/smile1</c> を 200% 整数倍で拡大）を開始します。
        /// </summary>
        private void StartLoadingAnimation()
        {
            this.EnsureLoadingFrames();
            this.loadingFrameIndex = 0;
            if (this.loadingFrames.Count == 0)
            {
                // アニメーションを用意できないクライアントでは従来どおり「読み込み中...」のみ表示します。
                this.picLoading.Image = null;
                this.picLoading.Visible = false;
                this.loadingNextFrameMs = 0;
                this.loadingFrameWatch.Reset();
                return;
            }

            this.picLoading.Image = this.loadingFrames[0];
            this.loadingFrameWatch.Restart();
            this.loadingNextFrameMs = this.loadingDelays[0];

            this.UpdateLoadingOverlayBounds();
            this.picLoading.Visible = true;
            this.picLoading.BringToFront();
            this.picLoading.Refresh();
        }

        /// <summary>
        /// 読み込み中のアニメーションを右エリア全体に重ねます（TableLayoutPanel のセル配置を避けるため手動配置）。
        /// </summary>
        private void UpdateLoadingOverlayBounds()
        {
            if (this.picLoading == null || this.tlpRightArea == null
                || this.picLoading.IsDisposed || this.tlpRightArea.IsDisposed)
            {
                return;
            }

            Rectangle screenBounds = this.tlpRightArea.RectangleToScreen(this.tlpRightArea.ClientRectangle);
            this.picLoading.Bounds = this.RectangleToClient(screenBounds);
        }

        private void StopLoadingAnimation()
        {
            this.picLoading.Visible = false;
            this.picLoading.Image = null;
            this.loadingFrameIndex = 0;
            this.loadingNextFrameMs = 0;
            this.loadingFrameWatch.Reset();
        }

        /// <summary>
        /// 一覧構築で UI スレッドが止まっている間もフレームを進めます。
        /// （メッセージループを回さないため、再入の心配がありません。）
        /// </summary>
        private void PumpLoadingAnimation()
        {
            if (!this.picLoading.Visible || this.loadingFrames.Count <= 1)
            {
                return;
            }

            long now = this.loadingFrameWatch.ElapsedMilliseconds;
            if (now < this.loadingNextFrameMs)
            {
                return;
            }

            this.loadingFrameIndex = (this.loadingFrameIndex + 1) % this.loadingFrames.Count;
            this.loadingNextFrameMs = now + this.loadingDelays[this.loadingFrameIndex];
            this.picLoading.Image = this.loadingFrames[this.loadingFrameIndex];
            this.picLoading.Refresh();
        }

        /// <summary>
        /// <c>Npc/0002006/smile1</c> のフレームを 200% 整数倍で描画してキャッシュします（初回のみ）。
        /// </summary>
        private void EnsureLoadingFrames()
        {
            if (this.loadingFramesLoaded)
            {
                return;
            }
            this.loadingFramesLoaded = true;

            // Unity データセットは拡張子なしのパスで解決します。
            Wz_Node animationNode = PluginManager.FindWz(@"Npc\0002006.img\smile1")
                ?? PluginManager.FindWz(@"Npc\0002006\smile1");
            if (animationNode == null)
            {
                return;
            }

            List<BitmapOrigin> sources = new List<BitmapOrigin>();
            List<int> delays = new List<int>();
            Rectangle bounds = Rectangle.Empty;
            for (int i = 0; ; i++)
            {
                Wz_Node frameNode = animationNode.FindNodeByPath(i.ToString());
                if (frameNode == null)
                {
                    break;
                }

                BitmapOrigin origin = BitmapOrigin.CreateFromNode(frameNode, PluginManager.FindWz);
                if (origin.Bitmap == null)
                {
                    continue;
                }

                Rectangle frameBounds = new Rectangle(origin.OpOrigin, origin.Bitmap.Size);
                bounds = sources.Count == 0 ? frameBounds : Rectangle.Union(bounds, frameBounds);
                sources.Add(origin);
                delays.Add(Math.Max(1, frameNode.FindNodeByPath("delay").GetValueEx(120)));
            }

            if (sources.Count == 0)
            {
                return;
            }

            try
            {
                int canvasSize = Math.Max(1, (int)Math.Ceiling(Math.Max(bounds.Width, bounds.Height) * LoadingAnimationScale));
                foreach (BitmapOrigin source in sources)
                {
                    this.loadingFrames.Add(RenderMobAnimationFrame(
                        source.Bitmap, new Rectangle(source.OpOrigin, source.Bitmap.Size), bounds,
                        LoadingAnimationScale, InterpolationMode.NearestNeighbor, canvasSize));
                }
                this.loadingDelays.AddRange(delays);
            }
            finally
            {
                foreach (BitmapOrigin source in sources)
                {
                    source.Bitmap.Dispose();
                }
            }
        }

        private void DisposeLoadingFrames()
        {
            foreach (Bitmap frame in this.loadingFrames)
            {
                frame.Dispose();
            }
            this.loadingFrames.Clear();
            this.loadingDelays.Clear();
        }

        private void BuildMobList()
        {
            itemPanelMobList.BeginUpdate();
            itemPanelMobList.Items.Clear();
            this.mobItems.Clear();
            this.mobSearchTextCache.Clear();
            this.SelectedMob = null;
            this.ClearMobIntro();

            if (this.MonsterBookNode != null)
            {
                foreach (Wz_Node mobNode in this.MonsterBookNode.Nodes)
                {
                    this.PumpLoadingAnimation();

                    int mobID;
                    if (!int.TryParse(mobNode.Text, out mobID))
                    {
                        continue;
                    }

                    StringResult mobSr = null;
                    this.stringLinker?.StringMob.TryGetValue(mobID, out mobSr);
                    string mobName = string.IsNullOrEmpty(mobSr?.Name) ? "Unknown Mob" : mobSr.Name;
                    MobListItem item = new MobListItem(mobID, mobName);
                    item.SetIcon(LoadMobIcon(mobID));
                    this.mobItems.Add(item);
                    itemPanelMobList.Items.Add(item);
                }
            }

            itemPanelMobList.EndUpdate();
            this.ApplyMobFilter();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (this.mobFilterTimer != null)
            {
                this.mobFilterTimer.Stop();
                this.mobFilterTimer.Dispose();
                this.mobFilterTimer = null;
            }
            this.StopMobAnimation();
            this.StopLoadingAnimation();
            this.DisposeLoadingFrames();
            this.ClearMobIntro();
            base.OnFormClosed(e);
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (txtSearch.Text.IndexOf('\r') >= 0 || txtSearch.Text.IndexOf('\n') >= 0)
            {
                // 複数行エディットのため、貼り付けなどで改行が入らないようにします。
                int selectionStart = txtSearch.SelectionStart;
                txtSearch.Text = txtSearch.Text.Replace("\r", string.Empty).Replace("\n", string.Empty);
                txtSearch.SelectionStart = Math.Min(selectionStart, txtSearch.Text.Length);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                // 空になった場合は待機せず、元の一覧へ即座に戻します。
                this.mobFilterTimer?.Stop();
                this.ApplyMobFilter();
            }
            else
            {
                this.RestartMobFilterTimer();
            }
        }

        private void RestartMobFilterTimer()
        {
            if (this.mobFilterTimer == null)
            {
                this.mobFilterTimer = new Timer();
                this.mobFilterTimer.Interval = 1000;
                this.mobFilterTimer.Tick += this.MobFilterTimer_Tick;
            }
            this.mobFilterTimer.Stop();
            this.mobFilterTimer.Start();
        }

        private void MobFilterTimer_Tick(object sender, EventArgs e)
        {
            this.mobFilterTimer.Stop();
            this.ApplyMobFilter();
        }

        private void itemPanelMobList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            MobListItem item = this.itemPanelMobList.HitTest(e.X, e.Y) as MobListItem;
            if (item != null)
            {
                this.SelectMobItem(item);
            }
        }

        private void itemPanelMobList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            MobListItem item = this.itemPanelMobList.HitTest(e.X, e.Y) as MobListItem;
            if (item == null)
            {
                return;
            }

            if (this.SelectedMob != item)
            {
                this.SelectMobItem(item);
            }
            this.NavigateToMobNode(item);
        }

        private void SelectMobItem(MobListItem item)
        {
            this.SelectedMob = item;
            this.itemPanelMobList.SelectedItem = item;

            this.lblMobName.Text = item.MobName;
            this.lblMobID.Text = item.MobID.ToString();
            this.richMobDescription.Text = GetMobDescription(item.MobID);
            this.richMobDescription.SelectionStart = 0;
            this.richMobDescription.SelectionLength = 0;
            this.LoadMobStats(item.MobID);
            this.LoadAnimationTypes(item);
            this.LoadMapList(item);
            this.LoadItemList(item);

            this.MobSelected?.Invoke(this, EventArgs.Empty);
        }

        private void ClearMobIntro()
        {
            this.lblMobName.Text = string.Empty;
            this.lblMobID.Text = string.Empty;
            this.richMobDescription.Text = string.Empty;
            this.ClearMobStats();
            this.cmbAnimationType.Visible = false;
            this.mapListMobID = null;
            this.ClearMapList();
            this.ClearItemList();
            this.StopMobAnimation();

            this.isUpdatingAnimationTypes = true;
            try
            {
                this.cmbAnimationType.Items.Clear();
                this.cmbAnimationType.SelectedIndex = -1;
            }
            finally
            {
                this.isUpdatingAnimationTypes = false;
            }
            this.SetMobAnimationPlaceholder();
        }

        private void LoadAnimationTypes(MobListItem item)
        {
            this.isUpdatingAnimationTypes = true;
            try
            {
                this.cmbAnimationType.Items.Clear();

                int moveIndex = -1, standIndex = -1;
                foreach (string animationName in GetMobAnimationNames(item.MobID))
                {
                    int index = this.cmbAnimationType.Items.Add(new ComboItem(animationName) { Value = animationName });
                    if (moveIndex < 0 && string.Equals(animationName, "move", StringComparison.OrdinalIgnoreCase))
                    {
                        moveIndex = index;
                    }
                    if (standIndex < 0 && string.Equals(animationName, "stand", StringComparison.OrdinalIgnoreCase))
                    {
                        standIndex = index;
                    }
                }

                // move を既定にし、無いデータセットでは stand → 先頭の順にフォールバックします。
                int selectedIndex = moveIndex >= 0 ? moveIndex
                    : standIndex >= 0 ? standIndex
                    : this.cmbAnimationType.Items.Count > 0 ? 0 : -1;
                this.cmbAnimationType.SelectedIndex = selectedIndex;
                this.cmbAnimationType.Visible = this.cmbAnimationType.Items.Count > 0;
            }
            finally
            {
                this.isUpdatingAnimationTypes = false;
            }

            this.ShowMobAnimation(item, this.SelectedAnimationName);
        }

        private string SelectedAnimationName
        {
            get
            {
                return (this.cmbAnimationType.SelectedItem as ComboItem)?.Value as string
                    ?? this.cmbAnimationType.SelectedItem as string;
            }
        }

        private void cmbAnimationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingAnimationTypes)
            {
                return;
            }
            this.ShowMobAnimation(this.SelectedMob, this.SelectedAnimationName);
        }

        /// <summary>
        /// モブの基本性能と属性耐性を左列下部に表示します。
        /// </summary>
        private void LoadMobStats(int mobID)
        {
            Wz_Node mobNode = PluginManager.FindWz(string.Format(@"Mob\{0:D7}.img", mobID));
            Mob mob = mobNode == null ? null : Mob.CreateFromNode(mobNode, PluginManager.FindWz);
            if (mob == null)
            {
                this.ClearMobStats();
                return;
            }

            string[][] rows =
            {
                new[] { "HP", mob.MaxHP.ToString("N0") },
                new[] { "MP", mob.MaxMP.ToString("N0") },
                new[] { "Physical Damage", mob.PADamage.ToString("N0") },
                new[] { "Magic Damage", mob.MADamage.ToString("N0") },
                new[] { "Physical DEF Rate", mob.PDRate.ToString() + "%" },
                new[] { "Magic DEF Rate", mob.MDRate.ToString() + "%" },
                new[] { "Knockback", mob.Pushed.ToString("N0") },
                new[] { "EXP", mob.Exp.ToString("N0") },
            };
            this.BuildMobStats(rows, GetMobElemAttrText(mob.ElemAttr));
        }

        private void ClearMobStats()
        {
            this.tlpMobStats.SuspendLayout();
            this.tlpMobStats.Controls.Clear();
            this.tlpMobStats.RowStyles.Clear();
            this.tlpMobStats.RowCount = 0;
            this.tlpMobStats.ResumeLayout(true);
        }

        private void BuildMobStats(string[][] rows, string elemAttrText)
        {
            this.tlpMobStats.SuspendLayout();
            this.tlpMobStats.Controls.Clear();
            this.tlpMobStats.RowStyles.Clear();
            this.tlpMobStats.RowCount = 0;

            foreach (string[] row in rows)
            {
                int rowIndex = this.tlpMobStats.RowCount;
                this.tlpMobStats.RowCount++;
                this.tlpMobStats.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                Label nameLabel = new Label
                {
                    AutoSize = true,
                    Font = MobStatsFont,
                    Margin = new System.Windows.Forms.Padding(0, 0, 8, 0),
                    Text = row[0],
                };
                Label valueLabel = new Label
                {
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    Font = MobStatsFont,
                    Margin = new System.Windows.Forms.Padding(0),
                    TextAlign = ContentAlignment.MiddleRight,
                    Text = row[1],
                };
                this.tlpMobStats.Controls.Add(nameLabel, 0, rowIndex);
                this.tlpMobStats.Controls.Add(valueLabel, 1, rowIndex);
            }

            if (!string.IsNullOrEmpty(elemAttrText))
            {
                int rowIndex = this.tlpMobStats.RowCount;
                this.tlpMobStats.RowCount++;
                this.tlpMobStats.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                this.lblMobElemAttr.Font = MobElemAttrFont;
                this.lblMobElemAttr.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
                this.lblMobElemAttr.Text = elemAttrText;
                this.tlpMobStats.Controls.Add(this.lblMobElemAttr, 0, rowIndex);
                this.tlpMobStats.SetColumnSpan(this.lblMobElemAttr, 2);
            }

            this.tlpMobStats.ResumeLayout(true);
        }

        /// <summary>
        /// Shows the element resistances as "Fire strong, Ice immune" (same notation as the mob tooltip).
        /// </summary>
        private static string GetMobElemAttrText(MobElemAttr elemAttr)
        {
            if (elemAttr == null)
            {
                return string.Empty;
            }

            var elems = new[]
            {
                new { name = "Physical", attr = elemAttr.P },
                new { name = "Holy", attr = elemAttr.H },
                new { name = "Fire", attr = elemAttr.F },
                new { name = "Ice", attr = elemAttr.I },
                new { name = "Poison", attr = elemAttr.S },
                new { name = "Lightning", attr = elemAttr.L },
                new { name = "Dark", attr = elemAttr.D },
            };
            StringBuilder sb = new StringBuilder();
            foreach (var elem in elems)
            {
                if (elem.attr != ElemResistance.Normal)
                {
                    sb.Append($"{elem.name} {GetElemResistText(elem.attr)}, ");
                }
            }
            return sb.ToString().TrimEnd().TrimEnd(',');
        }

        private static string GetElemResistText(ElemResistance resist)
        {
            switch (resist)
            {
                case ElemResistance.Immune: return "immune";
                case ElemResistance.Resist: return "strong";
                case ElemResistance.Normal: return "neutral";
                case ElemResistance.Weak: return "weak";
                default: return "  ";
            }
        }

        /// <summary>
        /// <c>String/&lt;region&gt;/MonsterBook/&lt;id&gt;</c> の説明文を取得します。
        /// Unity データセットは文字列テーブル側のノード値、WZ は <c>episode</c> 子ノードが説明文です
        /// （Unity の MonsterBook アセット側 <c>episode</c> は話数の番号のみなので参照しません）。
        /// </summary>
        private string GetMobDescription(int mobID)
        {
            string text = this.MonsterBookStringNode?.FindNodeByPath(mobID.ToString())?.Value?.ToString();

            if (string.IsNullOrEmpty(text))
            {
                Wz_Node descriptionNode = PluginManager.FindWz(string.Format(@"String\MonsterBook.img\{0}", mobID));
                text = descriptionNode?.FindNodeByPath("episode")?.Value?.ToString()
                    ?? descriptionNode?.Value?.ToString();
            }

            if (string.IsNullOrEmpty(text))
            {
                // 単体の MonsterBook.img を開いている場合のフォールバック（数値だけの episode は無視します）。
                Wz_Node entryNode = this.MonsterBookNode?.FindNodeByPath(mobID.ToString());
                string entryText = entryNode?.FindNodeByPath("episode")?.Value?.ToString() ?? entryNode?.Value?.ToString();
                int ignored;
                if (!string.IsNullOrEmpty(entryText) && !int.TryParse(entryText, out ignored))
                {
                    text = entryText;
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                StringResult mobSr;
                if (this.stringLinker != null
                    && this.stringLinker.StringMonsterBook.TryGetValue(mobID, out mobSr))
                {
                    text = mobSr.Desc;
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            return text.Replace("\\r\\n", "\r\n").Replace("\\r", "\r").Replace("\\n", "\n");
        }

        private static IEnumerable<string> GetMobAnimationNames(int mobID)
        {
            Wz_Node mobNode = PluginManager.FindWz(string.Format(@"Mob\{0:D7}.img", mobID));
            if (mobNode == null)
            {
                yield break;
            }

            foreach (Wz_Node child in mobNode.Nodes)
            {
                if (string.Equals(child.Text, "info", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (IsMobAnimationNode(child))
                {
                    yield return child.Text;
                }
            }
        }

        private static bool IsMobAnimationNode(Wz_Node node)
        {
            Wz_Node firstFrame = node.FindNodeByPath("0");
            return firstFrame != null
                && (firstFrame.Value is Wz_Png || firstFrame.Value is IWzPngSource || firstFrame.Value is Wz_Uol);
        }

        private void ShowMobAnimation(MobListItem item, string animationName)
        {
            this.StopMobAnimation();

            if (item == null || string.IsNullOrEmpty(animationName))
            {
                this.SetMobAnimationPlaceholder();
                return;
            }

            Wz_Node mobNode = PluginManager.FindWz(string.Format(@"Mob\{0:D7}.img", item.MobID));
            Wz_Node animationNode = mobNode?.FindNodeByPath(animationName);
            if (animationNode == null || !this.BuildMobAnimation(animationNode))
            {
                this.SetMobAnimationPlaceholder();
                return;
            }

            this.mobAnimationFrameIndex = 0;
            this.mobAnimationTimer = new Timer();
            this.mobAnimationTimer.Tick += this.MobAnimationTimer_Tick;
            this.picMobAnimation.Image = this.mobAnimationFrames[0];
            this.mobAnimationTimer.Interval = this.mobAnimationDelays[0];
            if (this.mobAnimationFrames.Count > 1)
            {
                this.mobAnimationTimer.Start();
            }
        }

        private bool BuildMobAnimation(Wz_Node animationNode)
        {
            List<BitmapOrigin> sources = new List<BitmapOrigin>();
            List<int> delays = new List<int>();
            Rectangle bounds = Rectangle.Empty;

            for (int i = 0; ; i++)
            {
                Wz_Node frameNode = animationNode.FindNodeByPath(i.ToString());
                if (frameNode == null)
                {
                    break;
                }

                BitmapOrigin origin = BitmapOrigin.CreateFromNode(frameNode, PluginManager.FindWz);
                if (origin.Bitmap == null)
                {
                    continue;
                }

                Rectangle frameBounds = new Rectangle(origin.OpOrigin, origin.Bitmap.Size);
                bounds = sources.Count == 0 ? frameBounds : Rectangle.Union(bounds, frameBounds);
                sources.Add(origin);
                delays.Add(Math.Max(1, frameNode.FindNodeByPath("delay").GetValueEx(120)));
            }

            if (sources.Count == 0)
            {
                return false;
            }

            if (animationNode.FindNodeByPath("zigzag").GetValueEx(0) != 0)
            {
                // zigzag: 0 1 2 3 → 0 1 2 3 2 1 のように末尾へ往復分を追加します。
                for (int i = sources.Count - 2; i > 0; i--)
                {
                    sources.Add(sources[i]);
                    delays.Add(delays[i]);
                }
            }

            float scale = GetMobScaleFactor(Math.Max(bounds.Width, bounds.Height), MobAnimationSize, true, out InterpolationMode interpolationMode);
            try
            {
                foreach (BitmapOrigin source in sources)
                {
                    this.mobAnimationFrames.Add(RenderMobAnimationFrame(
                        source.Bitmap, new Rectangle(source.OpOrigin, source.Bitmap.Size), bounds,
                        scale, interpolationMode, MobAnimationSize));
                    this.mobAnimationDelays.Add(0);
                }
            }
            finally
            {
                foreach (BitmapOrigin source in sources)
                {
                    source.Bitmap.Dispose();
                }
            }

            for (int i = 0; i < delays.Count; i++)
            {
                this.mobAnimationDelays[i] = delays[i];
            }
            return true;
        }

        private void MobAnimationTimer_Tick(object sender, EventArgs e)
        {
            if (this.mobAnimationFrames.Count == 0)
            {
                return;
            }

            this.mobAnimationFrameIndex = (this.mobAnimationFrameIndex + 1) % this.mobAnimationFrames.Count;
            this.picMobAnimation.Image = this.mobAnimationFrames[this.mobAnimationFrameIndex];
            this.mobAnimationTimer.Interval = this.mobAnimationDelays[this.mobAnimationFrameIndex];
        }

        private void StopMobAnimation()
        {
            if (this.mobAnimationTimer != null)
            {
                this.mobAnimationTimer.Stop();
                this.mobAnimationTimer.Tick -= this.MobAnimationTimer_Tick;
                this.mobAnimationTimer.Dispose();
                this.mobAnimationTimer = null;
            }

            this.picMobAnimation.Image = null;
            foreach (Bitmap frame in this.mobAnimationFrames)
            {
                frame.Dispose();
            }
            this.mobAnimationFrames.Clear();
            this.mobAnimationDelays.Clear();
            this.mobAnimationFrameIndex = 0;

            this.mobAnimationPlaceholder?.Dispose();
            this.mobAnimationPlaceholder = null;
        }

        private void SetMobAnimationPlaceholder()
        {
            this.mobAnimationPlaceholder?.Dispose();
            this.mobAnimationPlaceholder = new Bitmap(MobAnimationSize, MobAnimationSize, PixelFormat.Format32bppArgb);
            this.picMobAnimation.Image = this.mobAnimationPlaceholder;
        }

        private void itemPanelMapList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            MapListItem item = this.itemPanelMapList.HitTest(e.X, e.Y) as MapListItem;
            if (item != null)
            {
                this.NavigateToMapNode(item);
            }
        }

        private void NavigateToMapNode(MapListItem item)
        {
            if (this._mainForm == null)
            {
                return;
            }

            // Map.wz の構成: Map/Map<先頭桁>/<9 桁 ID>.img
            Wz_Node mapNode = PluginManager.FindWz(string.Format(@"Map\Map\Map{0}\{1:D9}.img", item.MapID / 100000000, item.MapID));
            if (mapNode != null)
            {
                this._mainForm.RedirectToNode(mapNode);
            }
            else
            {
                ToastNotification.Show(this, "No matching map node was found.", null, 2000, eToastGlowColor.Red, eToastPosition.TopCenter);
            }
        }

        private void LoadMapList(MobListItem item)
        {
            this.mapListMobID = item?.MobID;
            this.ClearMapList();
            if (!this.mapListMobID.HasValue)
            {
                return;
            }

            // String/MonsterBook.img の <id>/map に出現マップ一覧があります（WZ / Unity 共通）。
            List<int> nodeMaps = this.GetMonsterBookMapIDs(this.mapListMobID.Value);
            if (nodeMaps != null)
            {
                if (nodeMaps.Count == 0)
                {
                    this.itemPanelMapList.Items.Add(new LabelItem { Text = "No maps found" });
                    return;
                }

                this.itemPanelMapList.BeginUpdate();
                foreach (int mapID in nodeMaps.OrderBy(id => id))
                {
                    MapListItem mapItem = new MapListItem(mapID, this.GetMapDisplayName(mapID));
                    mapItem.SetIcon(LoadMapIcon(TryGetMapMark(mapID)));
                    this.mapItems.Add(mapItem);
                    this.itemPanelMapList.Items.Add(mapItem);
                }
                this.itemPanelMapList.EndUpdate();
                return;
            }

            // 古いデータなど map ノードが無い場合は全マップを走査して求めます。
            this.PopulateMapList();
        }

        /// <summary>
        /// <c>MonsterBook.img/&lt;id&gt;/map</c> に列挙されたマップ ID を返します。
        /// <c>map</c> ノード自体が無い場合は <c>null</c>（出現マップ不明）を返します。
        /// </summary>
        private List<int> GetMonsterBookMapIDs(int mobID)
        {
            Wz_Node mapNode = this.MonsterBookNode?.FindNodeByPath(mobID.ToString())?.FindNodeByPath("map");
            if (mapNode == null)
            {
                return null;
            }

            List<int> result = new List<int>();
            foreach (Wz_Node child in mapNode.Nodes)
            {
                int mapID;
                string raw = child.Value?.ToString() ?? child.Text;
                if (int.TryParse(raw, out mapID) && mapID > 0 && !result.Contains(mapID))
                {
                    result.Add(mapID);
                }
            }
            return result;
        }

        /// <summary>マップ img の <c>info/mapMark</c>（MapHelper のマーカー名）を取得します。</summary>
        private static string TryGetMapMark(int mapID)
        {
            lock (mapMarkCache)
            {
                string cached;
                if (mapMarkCache.TryGetValue(mapID, out cached))
                {
                    return cached;
                }
            }

            string mapMark = null;
            Wz_Node mapNode = PluginManager.FindWz(string.Format(@"Map\Map\Map{0}\{1:D9}.img", mapID / 100000000, mapID));
            Wz_Image mapImage = mapNode?.GetValue<Wz_Image>();
            if (mapImage != null)
            {
                UnityAssetImage unityImage = mapImage as UnityAssetImage;
                bool wasExtracted = unityImage != null
                    ? unityImage.IsExtracted
                    : mapImage.Node != null && mapImage.Node.Nodes.Count > 0;
                if (mapImage.TryExtract())
                {
                    try
                    {
                        mapMark = mapImage.Node?.FindNodeByPath("info")?.FindNodeByPath("mapMark")?.Value?.ToString();
                    }
                    finally
                    {
                        // 走査で展開した img は解放し、全マップ分のメモリを保持しないようにします。
                        if (!wasExtracted)
                        {
                            mapImage.Unextract();
                        }
                    }
                }
            }

            lock (mapMarkCache)
            {
                mapMarkCache[mapID] = mapMark;
            }
            return mapMark;
        }

        private void ClearMapList()
        {
            this.itemPanelMapList.BeginUpdate();
            this.itemPanelMapList.Items.Clear();
            foreach (MapListItem mapItem in this.mapItems)
            {
                mapItem.SetIcon(null);
            }
            this.mapItems.Clear();
            this.itemPanelMapList.EndUpdate();
        }

        private void PopulateMapList()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.ClearMapList();
            if (!this.mapListMobID.HasValue)
            {
                return;
            }

            int mobID = this.mapListMobID.Value;
            Dictionary<int, List<MobMapSpawnEntry>> index;
            lock (mobToMapIndexLock)
            {
                index = mobToMapIndex;
            }

            if (index == null)
            {
                this.EnsureMobToMapIndex();
                this.itemPanelMapList.Items.Add(new LabelItem { Text = "Searching maps..." });
                return;
            }

            List<MobMapSpawnEntry> spawns;
            if (!index.TryGetValue(mobID, out spawns) || spawns.Count == 0)
            {
                this.itemPanelMapList.Items.Add(new LabelItem { Text = "No maps found" });
                return;
            }

            this.itemPanelMapList.BeginUpdate();
            foreach (MobMapSpawnEntry spawn in spawns.OrderBy(s => s.MapID))
            {
                string mapName = this.GetMapDisplayName(spawn.MapID);
                MapListItem item = new MapListItem(spawn.MapID, mapName);
                item.SetIcon(LoadMapIcon(spawn.MapMark));
                this.mapItems.Add(item);
                this.itemPanelMapList.Items.Add(item);
            }
            this.itemPanelMapList.EndUpdate();
        }

        private string GetMapDisplayName(int mapID)
        {
            string mapName;
            return this.TryGetMapName(mapID, out mapName)
                ? mapName
                : string.Format("Unknown Map ({0})", mapID);
        }

        private bool TryGetMapName(int mapID, out string mapName)
        {
            mapName = null;
            StringResult mapSr;
            if (this.stringLinker != null
                && this.stringLinker.StringMap.TryGetValue(mapID, out mapSr)
                && !string.IsNullOrEmpty(mapSr.Name))
            {
                mapName = mapSr.Name;
                return true;
            }
            return false;
        }

        private static Bitmap LoadMapIcon(string mapMark)
        {
            Bitmap placeholder = new Bitmap(MobIconSize, MobIconSize, PixelFormat.Format32bppArgb);
            if (string.IsNullOrEmpty(mapMark))
            {
                return placeholder;
            }
            placeholder.Dispose();

            Wz_Node markNode = PluginManager.FindWz(string.Format(@"Map\MapHelper.img\mark\{0}", mapMark));
            Bitmap source = BitmapOrigin.CreateFromNode(markNode, PluginManager.FindWz).Bitmap;
            try
            {
                return CreateScaledMobBitmap(source, MobIconSize);
            }
            finally
            {
                source?.Dispose();
            }
        }

        /// <summary>
        /// 全マップの life ノードを走査して「モブ ID → 出現マップ」索引を作ります。
        /// 1 回の構築に数秒かかるため、バックグラウンドで 1 度だけ実行してキャッシュします。
        /// </summary>
        private void EnsureMobToMapIndex()
        {
            lock (mobToMapIndexLock)
            {
                if (mobToMapIndex != null || mobToMapIndexBuilding)
                {
                    return;
                }
                mobToMapIndexBuilding = true;
            }

            Task.Run(() =>
            {
                Dictionary<int, List<MobMapSpawnEntry>> index = null;
                try
                {
                    index = BuildMobToMapIndex();
                }
                catch
                {
                    // 失敗した場合は索引を作らず、次の選択で再試行できるようにします。
                    index = null;
                }

                lock (mobToMapIndexLock)
                {
                    if (index != null)
                    {
                        mobToMapIndex = index;
                    }
                    mobToMapIndexBuilding = false;
                }

                try
                {
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke((Action)this.OnMobToMapIndexReady);
                    }
                }
                catch (InvalidOperationException)
                {
                    // フォームが閉じられた場合は無視します。
                }
            });
        }

        /// <summary>マップ索引が完成したら一覧を更新し、マップ名を含む検索も有効にします。</summary>
        private void OnMobToMapIndexReady()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.mobSearchTextCache.Clear();
            this.PopulateMapList();
            this.ApplyMobFilter();
        }

        private static Dictionary<int, List<MobMapSpawnEntry>> BuildMobToMapIndex()
        {
            Dictionary<int, List<MobMapSpawnEntry>> index = new Dictionary<int, List<MobMapSpawnEntry>>();
            Wz_Node mapWzRoot = PluginManager.FindWz(Wz_Type.Map);
            // Unity データセットは Map/Map/&lt;グループ&gt;/ ですが、WZ では Map/&lt;グループ&gt;/ と階層が異なります。
            Wz_Node mapsNode = mapWzRoot?.FindNodeByPath("Map") ?? mapWzRoot;
            if (mapsNode == null)
            {
                return index;
            }

            foreach (Wz_Node groupNode in mapsNode.Nodes)
            {
                foreach (Wz_Node mapNode in groupNode.Nodes)
                {
                    int mapID;
                    string nodeName = mapNode.Text ?? string.Empty;
                    if (nodeName.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                    {
                        nodeName = nodeName.Substring(0, nodeName.Length - 4);
                    }
                    if (!int.TryParse(nodeName, out mapID))
                    {
                        continue;
                    }

                    Wz_Image mapImage = mapNode.GetValue<Wz_Image>();
                    if (mapImage == null)
                    {
                        continue;
                    }

                    UnityAssetImage unityImage = mapImage as UnityAssetImage;
                    bool wasExtracted = unityImage?.IsExtracted ?? false;
                    if (!mapImage.TryExtract())
                    {
                        continue;
                    }

                    try
                    {
                        Wz_Node mapRoot = mapImage.Node;
                        Wz_Node lifeNode = mapRoot?.FindNodeByPath("life");
                        if (lifeNode == null)
                        {
                            continue;
                        }

                        string mapMark = mapRoot.FindNodeByPath("info")?.FindNodeByPath("mapMark")?.Value?.ToString();
                        foreach (Wz_Node life in lifeNode.Nodes)
                        {
                            if (life.FindNodeByPath("type")?.Value?.ToString() != "m")
                            {
                                continue;
                            }

                            int mobID;
                            if (!int.TryParse(life.FindNodeByPath("id")?.Value?.ToString(), out mobID))
                            {
                                continue;
                            }

                            List<MobMapSpawnEntry> list;
                            if (!index.TryGetValue(mobID, out list))
                            {
                                index[mobID] = list = new List<MobMapSpawnEntry>();
                            }
                            if (!list.Exists(s => s.MapID == mapID))
                            {
                                list.Add(new MobMapSpawnEntry(mapID, mapMark));
                            }
                        }
                    }
                    finally
                    {
                        // 走査で展開した img は解放し、全マップ分のメモリを保持しないようにします。
                        if (!wasExtracted)
                        {
                            mapImage.Unextract();
                        }
                    }
                }
            }

            return index;
        }

        private struct MobMapSpawnEntry
        {
            public MobMapSpawnEntry(int mapID, string mapMark)
            {
                this.MapID = mapID;
                this.MapMark = mapMark;
            }

            public int MapID;

            public string MapMark;
        }

        private void LoadItemList(MobListItem mob)
        {
            this.ClearItemList();
            if (mob == null || this.MonsterBookNode == null)
            {
                return;
            }

            Wz_Node rewardNode = this.MonsterBookNode.FindNodeByPath(mob.MobID.ToString())?.FindNodeByPath("reward");
            if (rewardNode == null || rewardNode.Nodes.Count == 0)
            {
                return;
            }

            this.itemPanelItemList.BeginUpdate();
            foreach (Wz_Node reward in rewardNode.Nodes)
            {
                int itemID;
                if (!int.TryParse(reward.Value?.ToString(), out itemID) || itemID <= 0)
                {
                    continue;
                }

                ItemListItem item = new ItemListItem(itemID, this.GetItemDisplayName(itemID));
                item.SetIcon(LoadItemIcon(itemID));
                this.itemListItems.Add(item);
                this.itemPanelItemList.Items.Add(item);
            }
            this.itemPanelItemList.EndUpdate();
        }

        private void ClearItemList()
        {
            this.itemPanelItemList.BeginUpdate();
            this.itemPanelItemList.Items.Clear();
            foreach (ItemListItem item in this.itemListItems)
            {
                item.SetIcon(null);
            }
            this.itemListItems.Clear();
            this.itemPanelItemList.EndUpdate();
        }

        private string GetItemDisplayName(int itemID)
        {
            string itemName;
            return this.TryGetItemName(itemID, out itemName)
                ? itemName
                : string.Format("Unknown Item ({0})", itemID);
        }

        private bool TryGetItemName(int itemID, out string itemName)
        {
            itemName = null;
            if (this.stringLinker == null)
            {
                return false;
            }

            StringResult itemSr = null;
            if (itemID < 2000000)
            {
                this.stringLinker.StringEqp.TryGetValue(itemID, out itemSr);
            }
            else
            {
                this.stringLinker.StringItem.TryGetValue(itemID, out itemSr);
            }
            if (string.IsNullOrEmpty(itemSr?.Name))
            {
                return false;
            }
            itemName = itemSr.Name;
            return true;
        }

        private void itemPanelItemList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            ItemListItem item = this.itemPanelItemList.HitTest(e.X, e.Y) as ItemListItem;
            if (item == null || this._mainForm == null)
            {
                return;
            }

            Wz_Node itemNode = FindItemImageNode(item.ItemID);
            if (itemNode != null)
            {
                this._mainForm.RedirectToNode(itemNode);
            }
            else
            {
                ToastNotification.Show(this, "No matching item node was found.", null, 2000, eToastGlowColor.Red, eToastPosition.TopCenter);
            }
        }

        /// <summary>
        /// アイテム ID から img ノードを解決します。
        /// 装備は <c>Character/&lt;分類&gt;/&lt;8 桁 ID&gt;.img</c>、それ以外は
        /// <c>Item/&lt;Type&gt;/&lt;4 桁&gt;.img/&lt;8 桁 ID&gt;</c> です。
        /// </summary>
        private static Wz_Node FindItemImageNode(int itemID)
        {
            Wz_Node node;
            if (itemID < 2000000)
            {
                // Unity データセットは <c>Character/&lt;8 桁 ID&gt;</c> を直接解決できます。
                node = PluginManager.FindWz(string.Format(@"Character\{0:D8}", itemID))
                    ?? FindGearImageNodeInCategories(itemID);
            }
            else
            {
                string itemType = Item.GetItemType(itemID).ToString();
                string path = (itemID / 10000 == 500)
                    ? string.Format(@"Item\{0}\{1:D7}.img", itemType, itemID)
                    : string.Format(@"Item\{0}\{1:D4}.img\{2:D8}", itemType, itemID / 10000, itemID);
                node = PluginManager.FindWz(path) ?? PluginManager.FindWz(string.Format(@"Item\{0}", itemID));
            }

            // ツリー上の img ノード自身は内容を持たないため、展開して内容ノードを返します。
            Wz_Image image = node?.GetValue<Wz_Image>();
            if (image != null)
            {
                return image.TryExtract() ? image.Node : null;
            }
            return node;
        }

        /// <summary>
        /// WZ の <c>Character/&lt;分類&gt;/&lt;8 桁 ID&gt;.img</c> を分類ディレクトリから探します。
        /// </summary>
        private static Wz_Node FindGearImageNodeInCategories(int itemID)
        {
            string idName = itemID.ToString("D8");
            Wz_Node characterNode = PluginManager.FindWz(Wz_Type.Character);
            foreach (Wz_Node category in characterNode?.Nodes ?? Enumerable.Empty<Wz_Node>())
            {
                if (category.Text.IndexOf("canvas", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }
                if (category.Text == idName || category.Text == idName + ".img")
                {
                    return category;
                }
                Wz_Node found = category.Nodes[idName + ".img"] ?? category.Nodes[idName];
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static Bitmap LoadItemIcon(int itemID)
        {
            Wz_Node imageNode = FindItemImageNode(itemID);
            Wz_Node infoNode = imageNode?.FindNodeByPath("info");
            Wz_Node iconNode = infoNode?.FindNodeByPath("iconRaw") ?? infoNode?.FindNodeByPath("icon");
            Bitmap source = BitmapOrigin.CreateFromNode(iconNode, PluginManager.FindWz).Bitmap;
            try
            {
                // アイテムアイコンは拡大せず、目標サイズ以下のときは等倍で表示します。
                return CreateScaledMobBitmap(source, MobIconSize, false);
            }
            finally
            {
                source?.Dispose();
            }
        }

        private void NavigateToMobNode(MobListItem item)
        {
            if (this._mainForm == null)
            {
                return;
            }

            Wz_Node mobNode = PluginManager.FindWz(string.Format(@"Mob\{0:D7}.img", item.MobID));
            if (mobNode != null)
            {
                this._mainForm.RedirectToNode(mobNode);
            }
            else
            {
                ToastNotification.Show(this, "No matching mob node was found.", null, 2000, eToastGlowColor.Red, eToastPosition.TopCenter);
            }
        }

        private void ApplyMobFilter()
        {
            string keyword = Translator.NormalizeForSearch(txtSearch.Text.Trim());

            itemPanelMobList.BeginUpdate();
            foreach (MobListItem item in this.mobItems)
            {
                item.Visible = string.IsNullOrEmpty(keyword) || IsMobMatch(item, keyword);
            }
            itemPanelMobList.EndUpdate();
        }

        private bool IsMobMatch(MobListItem item, string keyword)
        {
            string name = Translator.NormalizeForSearch(item.MobName);
            if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string paddedID = item.MobID.ToString("D7");
            if (paddedID.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                || item.MobID.ToString().IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            // ドロップアイテム名・出現マップ名でも検索できるようにします。
            return this.GetMobSearchText(item.MobID).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// モブに関連する検索用テキスト（ドロップアイテム名 + 出現マップ名）を正規化して返します。
        /// 結果はキャッシュし、一覧の再構築時やマップ索引の完成時に破棄します。
        /// </summary>
        private string GetMobSearchText(int mobID)
        {
            string cached;
            if (this.mobSearchTextCache.TryGetValue(mobID, out cached))
            {
                return cached;
            }

            StringBuilder sb = new StringBuilder();

            Wz_Node rewardNode = this.MonsterBookNode?.FindNodeByPath(mobID.ToString())?.FindNodeByPath("reward");
            if (rewardNode != null)
            {
                foreach (Wz_Node reward in rewardNode.Nodes)
                {
                    int itemID;
                    string itemName;
                    if (int.TryParse(reward.Value?.ToString(), out itemID) && this.TryGetItemName(itemID, out itemName))
                    {
                        sb.Append(itemName).Append('\n');
                    }
                }
            }

            Dictionary<int, List<MobMapSpawnEntry>> index;
            lock (mobToMapIndexLock)
            {
                index = mobToMapIndex;
            }
            List<int> nodeMaps = this.GetMonsterBookMapIDs(mobID);
            if (nodeMaps != null)
            {
                foreach (int mapID in nodeMaps)
                {
                    string mapName;
                    if (this.TryGetMapName(mapID, out mapName))
                    {
                        sb.Append(mapName).Append('\n');
                    }
                }
            }
            else if (index != null && index.TryGetValue(mobID, out List<MobMapSpawnEntry> spawns))
            {
                foreach (MobMapSpawnEntry spawn in spawns)
                {
                    string mapName;
                    if (this.TryGetMapName(spawn.MapID, out mapName))
                    {
                        sb.Append(mapName).Append('\n');
                    }
                }
            }

            string text = Translator.NormalizeForSearch(sb.ToString());
            this.mobSearchTextCache[mobID] = text;
            return text;
        }

        private const int MobIconSize = 48;
        private const int MobAnimationSize = 256;
        private const float LoadingAnimationScale = 2f;

        private static Bitmap LoadMobIcon(int mobID)
        {
            Wz_Node iconNode = PluginManager.FindWz(string.Format(@"Mob\{0:D7}.img\stand\0", mobID));
            Bitmap source = BitmapOrigin.CreateFromNode(iconNode, PluginManager.FindWz).Bitmap;
            try
            {
                return CreateScaledMobBitmap(source, MobIconSize);
            }
            finally
            {
                source?.Dispose();
            }
        }

        private static Bitmap CreateScaledMobBitmap(Bitmap source, int targetSize, bool allowIntegerUpscale = true)
        {
            Bitmap result = new Bitmap(targetSize, targetSize, PixelFormat.Format32bppArgb);
            if (source == null || source.Width <= 0 || source.Height <= 0)
            {
                return result;
            }

            Rectangle sourceBounds = new Rectangle(0, 0, source.Width, source.Height);
            float scale = GetMobScaleFactor(Math.Max(source.Width, source.Height), targetSize, allowIntegerUpscale, out InterpolationMode interpolationMode);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = interpolationMode;
                g.PixelOffsetMode = interpolationMode == InterpolationMode.NearestNeighbor
                    ? PixelOffsetMode.Half
                    : PixelOffsetMode.HighQuality;

                RectangleF dest = new RectangleF(
                    (targetSize - sourceBounds.Width * scale) / 2f,
                    (targetSize - sourceBounds.Height * scale) / 2f,
                    sourceBounds.Width * scale,
                    sourceBounds.Height * scale);
                if (interpolationMode == InterpolationMode.NearestNeighbor)
                {
                    g.DrawImage(source, new Rectangle(
                        (int)Math.Round(dest.X), (int)Math.Round(dest.Y),
                        Math.Max(1, (int)Math.Round(dest.Width)), Math.Max(1, (int)Math.Round(dest.Height))));
                }
                else
                {
                    g.DrawImage(source, dest);
                }
            }

            return result;
        }

        /// <summary>
        /// アイコン/アニメーション共通の拡大縮小率です。
        /// 半サイズ未満は整数倍、目標サイズ以下は等倍、超過分は目標サイズへ縮小します。
        /// <paramref name="allowIntegerUpscale"/> が false の場合は拡大しません（アイテムアイコン用）。
        /// </summary>
        private static float GetMobScaleFactor(int maxDimension, int targetSize, bool allowIntegerUpscale, out InterpolationMode interpolationMode)
        {
            interpolationMode = InterpolationMode.NearestNeighbor;
            if (maxDimension <= 0)
            {
                return 1f;
            }
            if (maxDimension < targetSize / 2)
            {
                return allowIntegerUpscale ? Math.Max(1, targetSize / maxDimension) : 1f;
            }
            if (maxDimension > targetSize)
            {
                interpolationMode = InterpolationMode.HighQualityBicubic;
                return (float)targetSize / maxDimension;
            }
            return 1f;
        }

        private static Bitmap RenderMobAnimationFrame(
            Bitmap source, Rectangle sourceBounds, Rectangle bounds,
            float scale, InterpolationMode interpolationMode, int targetSize)
        {
            Bitmap result = new Bitmap(targetSize, targetSize, PixelFormat.Format32bppArgb);
            if (source == null)
            {
                return result;
            }

            float offsetX = (targetSize - bounds.Width * scale) / 2f;
            float offsetY = (targetSize - bounds.Height * scale) / 2f;
            RectangleF dest = new RectangleF(
                offsetX + (sourceBounds.X - bounds.X) * scale,
                offsetY + (sourceBounds.Y - bounds.Y) * scale,
                sourceBounds.Width * scale,
                sourceBounds.Height * scale);

            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = interpolationMode;
                g.PixelOffsetMode = interpolationMode == InterpolationMode.NearestNeighbor
                    ? PixelOffsetMode.Half
                    : PixelOffsetMode.HighQuality;
                if (interpolationMode == InterpolationMode.NearestNeighbor)
                {
                    g.DrawImage(source, new Rectangle(
                        (int)Math.Round(dest.X), (int)Math.Round(dest.Y),
                        Math.Max(1, (int)Math.Round(dest.Width)), Math.Max(1, (int)Math.Round(dest.Height))));
                }
                else
                {
                    g.DrawImage(source, dest);
                }
            }

            return result;
        }

        /// <summary>左列モブ一覧の 1 行（アイコン + 名前 + ID）。</summary>
        private sealed class MobListItem : ButtonItem
        {
            public MobListItem(int mobID, string mobName)
            {
                this.MobID = mobID;
                this.MobName = mobName ?? string.Empty;

                this.ButtonStyle = eButtonStyle.ImageAndText;
                this.ImageFixedSize = new Size(MobIconSize, MobIconSize);
                this.AutoDisposeImages = true;
                this.EnableMarkup = true;
                this.Text = string.Format("{0}<br/><font color=\"#808080\">{1}</font>",
                    EscapeMarkup(this.MobName), mobID);
                this.Tooltip = string.Format("{0} ({1})", this.MobName, mobID);
                this.Tag = mobID;
            }

            public int MobID { get; private set; }

            public string MobName { get; private set; }

            public void SetIcon(Bitmap icon)
            {
                if (this.Image != null)
                {
                    this.Image.Dispose();
                    this.Image = null;
                }
                this.Image = icon;
            }
        }

        /// <summary>下段マップ一覧の 1 行（マーカーアイコン + マップ名 + ID）。</summary>
        private sealed class MapListItem : ButtonItem
        {
            public MapListItem(int mapID, string mapName)
            {
                this.MapID = mapID;
                this.MapName = mapName ?? string.Empty;

                this.ButtonStyle = eButtonStyle.ImageAndText;
                this.ImageFixedSize = new Size(MobIconSize, MobIconSize);
                this.AutoDisposeImages = true;
                this.EnableMarkup = true;
                this.Text = string.Format("{0}<br/><font color=\"#808080\">{1}</font>",
                    EscapeMarkup(this.MapName), mapID);
                this.Tooltip = string.Format("{0} ({1})", this.MapName, mapID);
                this.Tag = mapID;
            }

            public int MapID { get; private set; }

            public string MapName { get; private set; }

            public void SetIcon(Bitmap icon)
            {
                if (this.Image != null)
                {
                    this.Image.Dispose();
                    this.Image = null;
                }
                this.Image = icon;
            }
        }

        /// <summary>下段アイテム一覧の 1 行（iconRaw + アイテム名 + ID）。</summary>
        private sealed class ItemListItem : ButtonItem
        {
            public ItemListItem(int itemID, string itemName)
            {
                this.ItemID = itemID;
                this.ItemName = itemName ?? string.Empty;

                this.ButtonStyle = eButtonStyle.ImageAndText;
                this.ImageFixedSize = new Size(MobIconSize, MobIconSize);
                this.AutoDisposeImages = true;
                this.EnableMarkup = true;
                this.Text = string.Format("{0}<br/><font color=\"#808080\">{1}</font>",
                    EscapeMarkup(this.ItemName), itemID);
                this.Tooltip = string.Format("{0} ({1})", this.ItemName, itemID);
                this.Tag = itemID;
            }

            public int ItemID { get; private set; }

            public string ItemName { get; private set; }

            public void SetIcon(Bitmap icon)
            {
                if (this.Image != null)
                {
                    this.Image.Dispose();
                    this.Image = null;
                }
                this.Image = icon;
            }
        }

        private static string EscapeMarkup(string text)
        {
            return SecurityElement.Escape(text) ?? string.Empty;
        }
    }
}
