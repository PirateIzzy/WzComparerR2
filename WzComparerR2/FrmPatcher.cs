using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevComponents.AdvTree;
using DevComponents.DotNetBar;
using DevComponents.Editors;
using WzComparerR2.Comparer;
using WzComparerR2.Config;
using WzComparerR2.Patcher;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public partial class FrmPatcher : DevComponents.DotNetBar.Office2007Form
    {
        public FrmPatcher()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(FrmPatcher_FormClosing);
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
#endif
            panelEx1.AutoScroll = true;

            var settings = WcR2Config.Default.PatcherSettings;
            if (settings.Count <= 0)
            {
                settings.Add(new PatcherSetting("KMST", "http://maplestory.dn.nexoncdn.co.kr/PatchT/{1:d5}/{0:d5}to{1:d5}.patch", 2));
                settings.Add(new PatcherSetting("KMST-Minor", "http://maplestory.dn.nexoncdn.co.kr/PatchT/{0:d5}/Minor/{1:d2}to{2:d2}.patch", 3));
                settings.Add(new PatcherSetting("KMS", "http://maplestory.dn.nexoncdn.co.kr/Patch/{1:d5}/{0:d5}to{1:d5}.patch", 2));
                settings.Add(new PatcherSetting("KMS-Minor", "http://maplestory.dn.nexoncdn.co.kr/Patch/{0:d5}/Minor/{1:d2}to{2:d2}.patch", 3));
                settings.Add(new PatcherSetting("JMS", "http://webdown2.nexon.co.jp/maple/patch/patchdir/{1:d5}/{0:d5}to{1:d5}.patch", 2));
                settings.Add(new PatcherSetting("GMS", "http://download2.nexon.net/Game/MapleStory/patch/patchdir/{1:d5}/CustomPatch{0}to{1}.exe", 2));
                settings.Add(new PatcherSetting("TMS", "http://tw.cdnpatch.maplestory.beanfun.com/maplestory/patch/patchdir/{1:d5}/{0:d5}to{1:d5}.patch", 2));
                settings.Add(new PatcherSetting("MSEA", "http://patch.maplesea.com/sea/patch/patchdir/{1:d5}/{0:d5}to{1:d5}.patch", 2));
                settings.Add(new PatcherSetting("CMS", "http://mxd.clientdown.sdo.com/maplestory/patch/patchdir/{1:d5}/{0:d5}to{1:d5}.patch", 2));
            }

            foreach (PatcherSetting p in settings)
            {
                this.MigrateSetting(p);
                comboBoxEx1.Items.Add(p);
            }
            if (comboBoxEx1.Items.Count > 0)
                comboBoxEx1.SelectedIndex = 0;

            foreach (WzPngComparison comp in Enum.GetValues(typeof(WzPngComparison)))
            {
                cmbComparePng.Items.Add(comp);
            }
            cmbComparePng.SelectedItem = WzPngComparison.SizeAndDataLength;
            SortedDictionary<string, long> patchedFileSizes = new SortedDictionary<string, long>();
            List<string> patchedFileIndex = new List<string>();
            Dictionary<string, string> finishedFileIndex = new Dictionary<string, string>();

            typedParts = Enum.GetValues(typeof(Wz_Type)).Cast<Wz_Type>().ToDictionary(type => type, type => new List<PatchPartContext>());

            // Disable until the bug is fixed
            this.chkCompare.Enabled = false;
            this.cmbComparePng.Enabled = false;
            this.chkOutputPng.Enabled = false;
            this.chkResolvePngLink.Enabled = false;
            this.chkOutputAddedImg.Enabled = false;
            this.chkOutputRemovedImg.Enabled = false;
            this.chkEnableDarkMode.Enabled = false;
        }

        SortedDictionary<string, long> patchedFileSizes = new SortedDictionary<string, long>();
        List<string> patchedFileIndex = new List<string>();
        Dictionary<string, string> finishedFileIndex = new Dictionary<string, string>();
        public Encoding PatcherNoticeEncoding { get; set; }

        long availableDiskSpace;
        private bool isUpdating;
        private PatcherSession patcherSession;

        private PatcherSetting SelectedPatcherSetting => comboBoxEx1.SelectedItem as PatcherSetting;

        private void MigrateSetting(PatcherSetting patcherSetting)
        {
            if (patcherSetting.MaxVersion == 0 && patcherSetting.Versions == null)
            {
                patcherSetting.MaxVersion = 2;
                patcherSetting.Versions = new[] { patcherSetting.Version0 ?? 0, patcherSetting.Version1 ?? 0 };
                patcherSetting.Version0 = null;
                patcherSetting.Version1 = null;
            }
            if (patcherSetting.Versions != null && patcherSetting.Versions.Length < patcherSetting.MaxVersion)
            {
                var newVersions = new int[patcherSetting.MaxVersion];
                Array.Copy(patcherSetting.Versions, newVersions, patcherSetting.Versions.Length);
                patcherSetting.Versions = newVersions;
            }
        }

        private void ApplySetting(PatcherSetting p)
        {
            if (isUpdating)
            {
                return;
            }
            isUpdating = true;
            try
            {
                if (this.flowLayoutPanel1.Controls.Count < p.MaxVersion)
                {
                    var inputTemplate = this.integerInput1;
                    var preAddedControls = Enumerable.Range(0, p.MaxVersion - this.flowLayoutPanel1.Controls.Count)
                        .Select(_ =>
                        {
                            var input = new IntegerInput()
                            {
                                AllowEmptyState = inputTemplate.AllowEmptyState,
                                Size = inputTemplate.Size,
                                Value = 0,
                                MinValue = inputTemplate.MinValue,
                                MaxValue = inputTemplate.MaxValue,
                                DisplayFormat = inputTemplate.DisplayFormat,
                                ShowUpDown = inputTemplate.ShowUpDown,
                            };
                            input.BackgroundStyle.ApplyStyle(inputTemplate.BackgroundStyle);
                            input.ValueChanged += this.integerInput_ValueChanged;
                            return input;
                        }).ToArray();
                    this.flowLayoutPanel1.Controls.AddRange(preAddedControls);
                }
                for (int i = 0; i < this.flowLayoutPanel1.Controls.Count; i++)
                {
                    var input = (IntegerInput)this.flowLayoutPanel1.Controls[i];
                    if (i < p.MaxVersion)
                    {
                        input.Show();
                        input.Value = (p.Versions != null && i < p.Versions.Length) ? p.Versions[i] : 0;
                    }
                    else
                    {
                        input.Hide();
                        input.Value = 0;
                    }
                }
                this.txtUrl.Text = p.Url;
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void combineUrl()
        {
            if (this.SelectedPatcherSetting is var p)
            {
                txtUrl.Text = p.Url;
            }
        }

        private void comboBoxEx1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.SelectedPatcherSetting is var p)
            {
                this.ApplySetting(p);
            }
        }

        private void integerInput_ValueChanged(object sender, EventArgs e)
        {
            if (this.SelectedPatcherSetting is var p && sender is IntegerInput input)
            {
                var i = this.flowLayoutPanel1.Controls.IndexOf(input);
                if (i > -1 && i < p.MaxVersion)
                {
                    if (p.Versions == null)
                    {
                        p.Versions = new int[p.MaxVersion];
                    }
                    p.Versions[i] = input.Value;
                }
                this.ApplySetting(p);
            }
        }

        private void buttonXCheck_Click(object sender, EventArgs e)
        {
            DownloadingItem item = new DownloadingItem(txtUrl.Text, null);
            try
            {
                item.GetFileLength();
                if (item.FileLength > 0)
                {
                    switch (MessageBoxEx.Show(string.Format("Size: {0:N0} bytes. Last updated: {1:yyyy-MM-dd HH:mm:ss}\r\nDo you want to download the file now?\r\n\r\nYes - Download\r\nNo - Copy URL to Clipboard\r\nCancel - Do not download", item.FileLength, item.LastModified), "Patcher", MessageBoxButtons.YesNoCancel))
                    {
                        case DialogResult.Yes:
#if NET6_0_OR_GREATER
                            Process.Start(new ProcessStartInfo
                            {
                                UseShellExecute = true,
                                FileName = txtUrl.Text,
                            });
#else
                            Process.Start(txtUrl.Text);
#endif
                            return;

                        case DialogResult.No:
                            Clipboard.SetText(txtUrl.Text);
                            return;

                        case DialogResult.Cancel:
                            return;
                    }
                }
                else
                {
                    MessageBoxEx.Show("This file does not exist.");
                }
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show("Error: " + ex.Message);
            }
        }

        private void FrmPatcher_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.patcherSession != null && !this.patcherSession.IsCompleted)
            {
                this.patcherSession.Cancel();
            }
            ConfigManager.Reload();
            WcR2Config.Default.PatcherSettings.Clear();
            foreach (PatcherSetting item in comboBoxEx1.Items)
            {
                WcR2Config.Default.PatcherSettings.Add(item);
            }
            ConfigManager.Save();
        }

        private void NewFile(BinaryReader reader, string fileName, string patchDir)
        {
            string tmpFile = Path.Combine(patchDir, fileName);
            string dir = Path.GetDirectoryName(tmpFile);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private void buttonXOpen1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select Patch File";
            dlg.Filter = "Patch File (*.patch;*.exe)|*.patch;*.exe";
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtPatchFile.Text = dlg.FileName;
            }
        }

        private void buttonXOpen2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "Please select your MapleStory folder.";
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtMSFolder.Text = dlg.SelectedPath;
            }
        }

        private void buttonXPatch_Click(object sender, EventArgs e)
        {
            if (!File.Exists(txtMSFolder.Text + "//MapleStory.exe") && !File.Exists(txtMSFolder.Text + "//MapleStoryT.exe"))
            {
                DialogResult PatcherPromptResult = MessageBoxEx.Show("The selected folder is not a valid MapleStory folder.\r\nProceed anyway?", "Warning", MessageBoxButtons.YesNo);
                if (PatcherPromptResult == System.Windows.Forms.DialogResult.No)
                {
                    return;
                }
            }
            if (this.patcherSession != null)
            {
                if (this.patcherSession.State == PatcherTaskState.WaitForContinue)
                {
                    this.patcherSession.Continue();
                    return;
                }
                else if (!this.patcherSession.PatchExecTask.IsCompleted)
                {
                    MessageBoxEx.Show("The patch is already in progress.");
                    return;
                }
            }
            string compareFolder = null;
            if (chkCompare.Checked)
            {
                FolderBrowserDialog dlg = new FolderBrowserDialog();
                dlg.Description = "Please select the destination folder for the comparison results.";
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                compareFolder = dlg.SelectedPath;
            }

            var session = new PatcherSession()
            {
                PatchFile = txtPatchFile.Text,
                MSFolder = txtMSFolder.Text,
                PrePatch = chkPrePatch.Checked,
                DeadPatch = chkDeadPatch.Checked,
                CompareFolder = compareFolder,
            };
            session.LoggingFileName = Path.Combine(session.MSFolder, $"wcpatcher_{DateTime.Now:yyyyMMdd_HHmmssfff}.log");
            session.PatchExecTask = Task.Run(() => this.ExecutePatchAsync(session, session.CancellationToken));
            this.patcherSession = session;
        }

        string htmlFilePath;
        FileStream htmlFile;
        StreamWriter sw;
        Dictionary<Wz_Type, List<PatchPartContext>> typedParts;

        private async Task ExecutePatchAsync(PatcherSession session, CancellationToken cancellationToken)
        {
            void AppendStateText(string text)
            {
                this.Invoke(new Action<string>(t => this.txtPatchState.AppendText(t)), text);
                if (session.LoggingFileName != null)
                {
                    File.AppendAllText(session.LoggingFileName, text, Encoding.UTF8);
                }
            }

            this.Invoke(() =>
            {
                this.advTreePatchFiles.Nodes.Clear();
                this.txtNotice.Clear();
                this.txtPatchState.Clear();
                this.panelEx2.Visible = true;
                this.expandablePanel2.Height = 340;
            });

            WzPatcher patcher = null;
            session.State = PatcherTaskState.Prepatch;

            try
            {
                patcher = new WzPatcher(session.PatchFile);
                patcher.NoticeEncoding = this.PatcherNoticeEncoding ?? Encoding.Default;
                patcher.PatchingStateChanged += (o, e) => this.patcher_PatchingStateChanged(o, e, session, AppendStateText);
                AppendStateText($"Patch file name: {session.PatchFile}\r\n");
                AppendStateText("Analyzing the patch...");
                long patchedAllFileSize = 0;
                long decompressedSize = patcher.PrePatch(cancellationToken);
                availableDiskSpace = RemainingDiskSpace(session.MSFolder);
                this.Invoke(() =>
                {
                    this.txtNotice.Text = patcher.NoticeText;
                    foreach (PatchPartContext part in patcher.PatchParts)
                    {
                        this.advTreePatchFiles.Nodes.Add(CreateFileNode(part));
                    }
                });
                patcher.OpenDecompress(cancellationToken);
                StringBuilder diskSpaceMessage = new StringBuilder();
                patchedFileSizes.Add("ZOther", 0);
                foreach (PatchPartContext part in patcher.PatchParts)
                {
                    switch (part.Type)
                    {
                        case 0:
                        case 1:
                            patchedAllFileSize += part.NewFileLength;
                            break;
                        case 2:
                            patchedAllFileSize -= part.NewFileLength;
                            break;
                    }
                    if (patcher.IsKMST1125Format.Value)
                    {
                        string[] patchedFileDirectory = part.FileName.Split('\\');
                        if (part.Type == 1 && (patchedFileDirectory[0] == "Data" || patchedFileDirectory[0] == "NxOverlay")) patchedFileIndex.Add(part.FileName);
                        if (patchedFileDirectory[0] == "Data")
                        {
                            if (!patchedFileSizes.ContainsKey(patchedFileDirectory[1])) patchedFileSizes.Add(patchedFileDirectory[1], 0);
                            switch (part.Type)
                            {
                                case 0:
                                case 1:
                                    patchedFileSizes[patchedFileDirectory[1]] += part.NewFileLength;
                                    break;
                                case 2:
                                    patchedFileSizes[patchedFileDirectory[1]] -= part.NewFileLength;
                                    break;
                            }
                        }
                        else
                        {
                            switch (part.Type)
                            {
                                case 0:
                                case 1:
                                    patchedFileSizes["ZOther"] += part.NewFileLength;
                                    break;
                                case 2:
                                    patchedFileSizes["ZOther"] -= part.NewFileLength;
                                    break;
                            }
                        }
                    }
                }
                foreach (string key in patchedFileSizes.Keys)
                {
                    switch (key)
                    {
                        case "ZOther":
                            diskSpaceMessage.AppendLine(string.Format("Required space for other files: {0}", GetBothByteAndGBValue(patchedFileSizes[key])));
                            break;
                        default:
                            diskSpaceMessage.AppendLine(string.Format("Required space for [{0}]: {1}", key, GetBothByteAndGBValue(patchedFileSizes[key])));
                            break;
                    }
                }
                patchedFileIndex.Sort();
                diskSpaceMessage.AppendLine(string.Format("Total required disk space: {0}", GetBothByteAndGBValue(patchedAllFileSize)));
                diskSpaceMessage.AppendLine(string.Format("Available disk space: {0}", GetBothByteAndGBValue(availableDiskSpace)));
                AppendStateText(diskSpaceMessage.ToString());
                AppendStateText("Completed\r\n");
                if (patchedAllFileSize > availableDiskSpace)
                {
                    DialogResult PatcherPromptResult = MessageBoxEx.Show(this, diskSpaceMessage.ToString() + "\r\nAvailable disk space may be insufficient for finishing patching.\r\nProceed anyway?", "Warning", MessageBoxButtons.YesNo);
                    if (PatcherPromptResult == DialogResult.No)
                    {
                        throw new OperationCanceledException("The patch has been canceled.");
                    }
                }
                if (session.PrePatch)
                {
                    AppendStateText("Preparing the patch... \r\n");
                    if (patcher.IsKMST1125Format.Value)
                    {
                        AppendStateText("Patch type: KMST1125\r\n");
                        if (patcher.OldFileHash != null)
                        {
                            AppendStateText($"Number of files to check checksum pre-patch: {patcher.OldFileHash.Count}\r\n");
                        }
                    }
                    AppendStateText(string.Format("Patch size: {0:N0} bytes...\r\n", decompressedSize));
                    AppendStateText(string.Format("Number of files to patch: {0}...\r\n", patcher.PatchParts.Count));

                    this.Invoke(() =>
                    {
                        this.advTreePatchFiles.BeginUpdate();
                        this.advTreePatchFiles.Enabled = true;
                        this.advTreePatchFiles.EndUpdate();
                    });

                    AppendStateText("After selecting a patch file, you may click on the Patch button to start.\r\n");
                    session.State = PatcherTaskState.WaitForContinue;
                    await session.WaitForContinueAsync();
                    this.Invoke(() =>
                    {
                        this.advTreePatchFiles.Enabled = false;
                    });
                    session.State = PatcherTaskState.Patching;
                    patcher.PatchParts.Clear();
                    foreach (Node node in this.advTreePatchFiles.Nodes)
                    {
                        if (node.Checked && node.Tag is PatchPartContext part)
                        {
                            patcher.PatchParts.Add(part);
                        }
                    }
                }
                if (patcher.IsKMST1125Format.Value && session.DeadPatch)
                {
                    AppendStateText("Immediate Patch Execution Plan\r\n");
                    session.deadPatchExecutionPlan = new();
                    session.deadPatchExecutionPlan.Build(patcher.PatchParts);
                    foreach (var part in patcher.PatchParts)
                    {
                        if (session.deadPatchExecutionPlan.Check(part.FileName, out var filesCanInstantUpdate))
                        {
                            AppendStateText($"+ Execute file [{part.FileName}]\r\n");
                            foreach (var fileName in filesCanInstantUpdate)
                            {
                                AppendStateText($"  - Apply file [{fileName}]\r\n");
                            }
                        }
                        else
                        {
                            AppendStateText($"- Execute file [{part.FileName}], pending application\r\n");
                        }
                    }
                    // disable force validation
                    patcher.ThrowOnValidationFailed = false;
                }
                AppendStateText("Applying patch\r\n");
                var sw = Stopwatch.StartNew();
                patcher.Patch(session.MSFolder, cancellationToken);
                sw.Stop();
                AppendStateText("Completed\r\n");
                session.State = PatcherTaskState.Complete;
                MessageBoxEx.Show(this, "Patch Completed. Elapsed time: " + sw.Elapsed, "Patcher");
            }
            catch (OperationCanceledException)
            {
                MessageBoxEx.Show(this.Owner, "The patch has been canceled.", "Patcher");
            }
            catch (UnauthorizedAccessException ex)
            {
                // File IO permission error
                MessageBoxEx.Show(this, ex.ToString(), "Patcher");
            }
            catch (Exception ex)
            {
                AppendStateText(ex.ToString());
                MessageBoxEx.Show(this, ex.ToString(), "Patcher");
            }
            finally
            {
                patchedFileSizes.Clear();
                patchedFileIndex.Clear();
                finishedFileIndex.Clear();
                session.State = PatcherTaskState.Complete;
                if (patcher != null)
                {
                    patcher.Close();
                    patcher = null;
                }
                GC.Collect();
                panelEx2.Visible = false;
                expandablePanel2.Height = 157;
            }
        }

        private void patcher_PatchingStateChanged(object sender, PatchingEventArgs e, PatcherSession session, Action<string> logFunc)
        {
            switch (e.State)
            {
                case PatchingState.PatchStart:
                    logFunc("[" + e.Part.FileName + "] Patching\r\n");
                    break;
                case PatchingState.VerifyOldChecksumBegin:
                    logFunc("  Checking pre-patch checksum...");
                    break;
                case PatchingState.VerifyOldChecksumEnd:
                    logFunc("  Completed\r\n");
                    break;
                case PatchingState.VerifyNewChecksumBegin:
                    logFunc("  Checking post-patch checksum...");
                    break;
                case PatchingState.VerifyNewChecksumEnd:
                    logFunc("  Completed\r\n");
                    break;
                case PatchingState.TempFileCreated:
                    logFunc("  Creating temporary files...\r\n");
                    progressBarX1.Maximum = e.Part.NewFileLength;
                    session.TemporaryFileMapping.Add(e.Part.FileName, e.Part.TempFilePath);
                    break;
                case PatchingState.TempFileBuildProcessChanged:
                    progressBarX1.Value = (int)e.CurrentFileLength;
                    progressBarX1.Text = string.Format("{0:N0}/{1:N0}", e.CurrentFileLength, e.Part.NewFileLength);
                    break;
                case PatchingState.TempFileClosed:
                    logFunc("  Temporary file creation complete\r\n");
                    progressBarX1.Value = 0;
                    progressBarX1.Maximum = 0;
                    progressBarX1.Text = string.Empty;

                    if (!string.IsNullOrEmpty(session.CompareFolder)
                        && e.Part.Type == 1
                        && Path.GetExtension(e.Part.FileName).Equals(".wz", StringComparison.OrdinalIgnoreCase)
                        && !Path.GetFileName(e.Part.FileName).Equals("list.wz", StringComparison.OrdinalIgnoreCase))
                    {
                        Wz_Structure wznew = new Wz_Structure();
                        Wz_Structure wzold = new Wz_Structure();
                        try
                        {
                            logFunc("  Comparing files...\r\n");
                            EasyComparer comparer = new EasyComparer();
                            comparer.OutputPng = chkOutputPng.Checked;
                            comparer.OutputAddedImg = chkOutputAddedImg.Checked;
                            comparer.OutputRemovedImg = chkOutputRemovedImg.Checked;
                            comparer.EnableDarkMode = chkEnableDarkMode.Checked;
                            comparer.Comparer.PngComparison = (WzPngComparison)cmbComparePng.SelectedItem;
                            comparer.Comparer.ResolvePngLink = chkResolvePngLink.Checked;
                            wznew.Load(e.Part.TempFilePath, false);
                            wzold.Load(e.Part.OldFilePath, false);
                            comparer.EasyCompareWzFiles(wznew.wz_files[0], wzold.wz_files[0], session.CompareFolder);
                        }
                        catch (Exception ex)
                        {
                            txtPatchState.AppendText(ex.ToString());
                        }
                        finally
                        {
                            wznew.Clear();
                            wzold.Clear();
                            GC.Collect();
                        }
                    }

                    if (session.DeadPatch && e.Part.Type == 1 && sender is WzPatcher patcher)
                    {
                        if (patcher.IsKMST1125Format.Value)
                        {
                            if (session.deadPatchExecutionPlan?.Check(e.Part.FileName, out var filesCanInstantUpdate) ?? false)
                            {
                                long currentUsedDiskSpace = availableDiskSpace - RemainingDiskSpace(session.MSFolder);
                                logFunc(string.Format("  (Immediate Patch) Used disk space: {0}\r\n", GetBothByteAndGBValue(currentUsedDiskSpace)));
                                foreach (string fileName in filesCanInstantUpdate)
                                {
                                    if (session.TemporaryFileMapping.TryGetValue(fileName, out var temporaryFileName))
                                    {
                                        logFunc($"  (Immediate Patch) Applying file [{fileName}]...\r\n");
                                        patcher.SafeMove(temporaryFileName, Path.Combine(session.MSFolder, fileName));
                                    }
                                }
                            }
                            else
                            {
                                logFunc("  (Immediate Patch) Delayed applying files...\r\n");
                            }
                        }
                        else
                        {
                            logFunc("  (Immediate Patch) Applying files...\r\n");
                            patcher.SafeMove(e.Part.TempFilePath, e.Part.OldFilePath);
                        }
                    }
                    break;
                case PatchingState.PrepareVerifyOldChecksumBegin:
                    logFunc($"Checking the pre-patch checksum: {e.Part.FileName}");
                    break;
                case PatchingState.PrepareVerifyOldChecksumEnd:
                    if (e.Part.OldChecksum != e.Part.OldChecksumActual)
                    {
                        logFunc(" Mismatch\r\n");
                    }
                    else
                    {
                        logFunc(" Completed\r\n");
                    }
                    break;
                case PatchingState.ApplyFile:
                    logFunc($"Applying files: {e.Part.FileName}\r\n");
                    break;
                case PatchingState.FileSkipped:
                    logFunc("  Skipped file: " + e.Part.FileName + "\r\n");
                    break;
            }
        }

        private Node CreateFileNode(PatchPartContext part)
        {
            Node node = new Node(part.FileName) { CheckBoxVisible = true, Checked = true };
            ElementStyle style = new ElementStyle();
            style.TextAlignment = eStyleTextAlignment.Far;
            switch (part.Type)
            {
                case 0: node.Cells.Add(new Cell("Added", style)); break;
                case 1: node.Cells.Add(new Cell("Modified", style)); break;
                case 2: node.Cells.Add(new Cell("Removed", style)); break;
                default: node.Cells.Add(new Cell(part.Type.ToString(), style)); break;
            }
            node.Cells.Add(new Cell(part.NewFileLength.ToString("n0"), style));
            node.Cells.Add(new Cell(part.NewChecksum.ToString("x8"), style));
            node.Cells.Add(new Cell(part.OldChecksum?.ToString("x8"), style));
            if (part.Type == 1)
            {
                string text = string.Format("{0}|{1}|{2}|{3}", part.Action0, part.Action1, part.Action2, part.DependencyFiles.Count);
                node.Cells.Add(new Cell(text, style));
            }
            node.Tag = part;
            return node;
        }

        private void buttonXOpen3_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select Patch File";
            dlg.Filter = "Patch File (*.patch;*.exe)|*.patch;*.exe";
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtPatchFile2.Text = dlg.FileName;
            }
        }

        private void buttonXOpen4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "Please select your MapleStory folder.";
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtMSFolder2.Text = dlg.SelectedPath;
            }
        }

        private void buttonXCreate_Click(object sender, EventArgs e)
        {
            MessageBoxEx.Show(@"> This feature is incomplete and requires more testing...
> Since this is still incomplete, only select the patch file. The .exe patch is not supported right now.
> It will not check the client version, so please check it yourself before you start.
> File block filtering or file missing prompts are not available for the time being.
> It will generate a large file due to no optimization. However, it is guaranteed that the file is complete.", "Notice");

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Patch File (*.patch)|*.patch";
            dlg.Title = "Save Patch File";
            dlg.CheckFileExists = false;
            dlg.InitialDirectory = Path.GetDirectoryName(txtPatchFile2.Text);
            dlg.FileName = Path.GetFileNameWithoutExtension(txtPatchFile2.Text) + "_reverse.patch";

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    ReversePatcherBuilder builder = new ReversePatcherBuilder();
                    builder.msDir = txtMSFolder2.Text;
                    builder.patchFileName = txtPatchFile2.Text;
                    builder.outputFileName = dlg.FileName;
                    builder.Build();
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void FrmPatcher_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.patcherSession != null && this.patcherSession.State != PatcherTaskState.NotStarted && this.patcherSession.State != PatcherTaskState.Complete)
            {
                DialogResult result = MessageBoxEx.Show(this, "The game is currently being patched, and terminating it may corrupt the game files.\r\nWould you like to terminate it anyway?", "Patcher", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    e.Cancel = false;
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        private long RemainingDiskSpace(string path)
        {
            string diskDrive = path.Substring(0, 2);
            try
            {
                DriveInfo dinfo = new DriveInfo(diskDrive);
                return dinfo.AvailableFreeSpace;
            }
            catch
            {
                return 0;
            }
        }

        private string GetBothByteAndGBValue(long size)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double targetbytes = size;
            int order = 0;

            while (targetbytes >= 1024 && order < sizes.Length)
            {
                order++;
                targetbytes /= 1024;
            }

            if (size <= 1024)
            {
                return $"{size:N0} bytes";
            }
            else
            {
                return $"{size:N0} bytes ({targetbytes:0.##} {sizes[order]})";
            }
        }

        class PatcherSession
        {
            public PatcherSession()
            {
                this.cancellationTokenSource = new CancellationTokenSource();
            }

            public string PatchFile;
            public string MSFolder;
            public string CompareFolder;
            public bool PrePatch;
            public bool DeadPatch;

            public Task PatchExecTask;
            public string LoggingFileName;
            public PatcherTaskState State;

            public DeadPatchExecutionPlan deadPatchExecutionPlan;
            public Dictionary<string, string> TemporaryFileMapping = new();

            public CancellationToken CancellationToken => this.cancellationTokenSource.Token;
            private CancellationTokenSource cancellationTokenSource;
            private TaskCompletionSource<bool> tcsWaiting;

            public bool IsCompleted => this.PatchExecTask?.IsCompleted ?? true;

            public void Cancel()
            {
                this.cancellationTokenSource.Cancel();
            }

            public async Task WaitForContinueAsync()
            {
                var tcs = new TaskCompletionSource<bool>();
                this.tcsWaiting = tcs;
                this.cancellationTokenSource.Token.Register(() => tcs.TrySetCanceled());
                await tcs.Task;
            }

            public void Continue()
            {
                if (this.tcsWaiting != null)
                {
                    this.tcsWaiting.SetResult(true);
                }
            }
        }

        enum PatcherTaskState
        {
            NotStarted = 0,
            Prepatch = 1,
            WaitForContinue = 2,
            Patching = 3,
            Complete = 4,
        }

        class DeadPatchExecutionPlan
        {
            public DeadPatchExecutionPlan()
            {
                this.FileUpdateDependencies = new Dictionary<string, List<string>>();
            }

            public Dictionary<string, List<string>> FileUpdateDependencies { get; private set; }

            public void Build(IEnumerable<PatchPartContext> orderedParts)
            {
                /*
                 *  for examle:
                 *    fileName   | type | dependencies               
                 *    -----------|------|---------------     
                 *    Mob_000.wz | 1    | Mob_000.wz   (self update)
                 *    Mob_001.wz | 1    | Mob_001.wz, Mob_002.wz  (merge data)
                 *    Mob_002.wz | 1    | Mob_001.wz, Mob_002.wz  (merge data)
                 *    Mob_003.wz | 1    | Mob_001.wz, Mob_002.wz  (balance size from other file)
                 *                                                 
                 *  fileLastDependecy:                             
                 *    key        | value                           
                 *    -----------|----------------                 
                 *    Mob_000.wz | Mob_000.wz
                 *    Mob_001.wz | Mob_003.wz
                 *    Mob_002.wz | Mob_003.wz
                 *    Mob_003.wz | Mob_003.wz
                 *    
                 *  FileUpdateDependencies:
                 *    key        | value
                 *    -----------|----------------
                 *    Mob_000.wz | Mob000.wz
                 *    Mob_003.wz | Mob001.wz, Mob002.wz, Mob003.wz
                 */

                // find the last dependency
                Dictionary<string, string> fileLastDependecy = new();
                foreach (var part in orderedParts)
                {
                    if (part.Type == 0)
                    {
                        fileLastDependecy[part.FileName] = part.FileName;
                    }
                    else if (part.Type == 1)
                    {
                        fileLastDependecy[part.FileName] = part.FileName;
                        foreach (var dep in part.DependencyFiles)
                        {
                            fileLastDependecy[dep] = part.FileName;
                        }
                    }
                }

                // reverse key and value
                this.FileUpdateDependencies.Clear();
                foreach (var grp in fileLastDependecy.GroupBy(kv => kv.Value, kv => kv.Key))
                {
                    this.FileUpdateDependencies.Add(grp.Key, grp.ToList());
                }
            }

            public bool Check(string fileName, out IReadOnlyList<string> filesCanInstantUpdate)
            {
                if (this.FileUpdateDependencies.TryGetValue(fileName, out var value) && value != null && value.Count > 0)
                {
                    filesCanInstantUpdate = value;
                    return true;
                }

                filesCanInstantUpdate = null;
                return false;
            }
        }
    }
}