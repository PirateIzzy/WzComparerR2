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
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font(new FontFamily("Microsoft Sans Serif"), 8f);
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
            typedParts = Enum.GetValues(typeof(Wz_Type)).Cast<Wz_Type>().ToDictionary(type => type, type => new List<PatchPartContext>());
        }

        public Encoding PatcherNoticeEncoding { get; set; }

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
                    switch (MessageBoxEx.Show(string.Format("Size: {0:N0} bytes. Last updated: {1:yyyy-MM-dd HH:mm:ss}\r\nDo you want to download the file now?", item.FileLength, item.LastModified), "Patcher", MessageBoxButtons.YesNo))
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
                patcher.OpenDecompress(cancellationToken);
                AppendStateText("Completed\r\n");
                //if (session.PrePatch)
                {
                    AppendStateText("Preparing the patch... \r\n");
                    long decompressedSize = patcher.PrePatch(cancellationToken);
                    if (patcher.IsKMST1125Format.Value)
                    {
                        AppendStateText("Patch size: KMST1125\r\n");
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
                        this.txtNotice.Text = patcher.NoticeText;
                        foreach (PatchPartContext part in patcher.PatchParts)
                        {
                            this.advTreePatchFiles.Nodes.Add(CreateFileNode(part));
                            advTreePatchFiles.Nodes[advTreePatchFiles.Nodes.Count - 1].Enabled = session.PrePatch;
                            if (session.PrePatch && part.Type == 1)
                            {
                                advTreePatchFiles.Nodes[advTreePatchFiles.Nodes.Count - 1].Checked = File.Exists(Path.Combine(session.MSFolder, part.FileName));
                            }
                        }
                        //this.advTreePatchFiles.Enabled = true;
                        this.advTreePatchFiles.EndUpdate();
                    });
                }
                if (session.PrePatch)
                {
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
                        node.Enabled = false;
                    }
                    patcher.PatchParts.Sort((part1, part2) => part1.Offset.CompareTo(part2.Offset));
                }
                AppendStateText("Patching...\r\n");
                var sw = Stopwatch.StartNew();
                patcher.Patch(session.MSFolder, cancellationToken);
                sw.Stop();
                if (this.sw != null)
                {
                    this.sw.WriteLine("</table>");
                    this.sw.WriteLine("</p>");

                    //html结束
                    this.sw.WriteLine("</body>");
                    this.sw.WriteLine("</html>");

                    try
                    {
                        if (this.sw != null)
                        {
                            this.sw.Flush();
                            this.sw.Close();
                        }
                    }
                    catch
                    {
                    }
                }
                AppendStateText("Completed\r\n");
                session.State = PatcherTaskState.Complete;
                MessageBoxEx.Show(this, "Patch Completed " + sw.Elapsed, "Patcher");
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
                session.State = PatcherTaskState.Complete;
                try
                {
                    if (sw != null)
                    {
                        sw.Flush();
                        sw.Close();
                    }
                }
                catch
                {
                }
                try
                {
                    if (htmlFile != null)
                    {
                        htmlFile.Flush();
                        htmlFile.Close();
                    }
                }
                catch
                {
                }
                htmlFilePath = null;
                foreach (List<PatchPartContext> parts in typedParts.Values)
                {
                    parts.Clear();
                }
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
                    progressBarX1.Maximum = (int)e.Part.OldFileLength;
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

                    typedParts[e.Part.WzType].Add(e.Part);

                    if (!string.IsNullOrEmpty(session.CompareFolder)
                        //&& e.Part.Type == 1
                        && Path.GetExtension(e.Part.FileName).Equals(".wz", StringComparison.OrdinalIgnoreCase)
                        && !Path.GetFileName(e.Part.FileName).Equals("list.wz", StringComparison.OrdinalIgnoreCase)
                        && typedParts[e.Part.WzType].Count == ((WzPatcher)sender).PatchParts.Where(part => part.Type != 2 && part.WzType == e.Part.WzType).Count())
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
                            comparer.PatchingStateChanged += (o, e) => this.patcher_PatchingStateChanged(o, e, session, logFunc);
                            //wznew.Load(e.Part.TempFilePath, false);
                            //wzold.Load(e.Part.OldFilePath, false);
                            //comparer.EasyCompareWzFiles(wznew.wz_files[0], wzold.wz_files[0], session.CompareFolder);
                            string tempDir = e.Part.TempFilePath;
                            while (Path.GetDirectoryName(tempDir) != session.MSFolder)
                            {
                                tempDir = Path.GetDirectoryName(tempDir);
                            }
                            string newWzFilePath = Path.Combine(tempDir, "Data", e.Part.WzType.ToString(), e.Part.WzType + ".wz");
                            string oldWzFilePath = Path.Combine(session.MSFolder, "Data", e.Part.WzType.ToString(), e.Part.WzType + ".wz");
                            bool isNewKMST1125WzFormat = wznew.IsKMST1125WzFormat(newWzFilePath, oldWzFilePath); // TODO: check if deleted
                            bool isOldKMST1125WzFormat = wzold.IsKMST1125WzFormat(oldWzFilePath);
                            if (isNewKMST1125WzFormat)
                            {
                                wznew.LoadKMST1125DataWz(newWzFilePath, oldWzFilePath);
                            }
                            else
                            {
                                foreach (PatchPartContext part in typedParts[e.Part.WzType])
                                {
                                    if (part.Type != 2)
                                    {
                                        wznew.Load(part.TempFilePath, false);
                                    }
                                }
                            }
                            if (isOldKMST1125WzFormat)
                            {
                                wzold.LoadKMST1125DataWz(oldWzFilePath);
                            }
                            else
                            {
                                foreach (PatchPartContext part in ((WzPatcher)sender).PatchParts.Where(part => part.WzType == e.Part.WzType))
                                {
                                    if (part.Type != 0 && File.Exists(Path.Combine(session.MSFolder, part.FileName)))
                                    {
                                        wzold.Load(Path.Combine(session.MSFolder, part.FileName), false);
                                    }
                                }
                            }
                            if (sw == null)
                            {
                                htmlFilePath = Path.Combine(session.CompareFolder, "index.html");

                                htmlFile = new FileStream(htmlFilePath, FileMode.Create, FileAccess.Write);
                                sw = new StreamWriter(htmlFile, Encoding.UTF8);
                                sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
                                sw.WriteLine("<html>");
                                sw.WriteLine("<head>");
                                sw.WriteLine("<meta http-equiv=\"content-type\" content=\"text/html;charset=utf-8\">");
                                sw.WriteLine("<title>Index {0}←{1}</title>", wznew.wz_files.Where(wz_file => wz_file != null).First().Header.WzVersion, wzold.wz_files.Where(wz_file => wz_file != null).First().Header.WzVersion);
                                sw.WriteLine("<link type=\"text/css\" rel=\"stylesheet\" href=\"style.css\" />");
                                sw.WriteLine("</head>");
                                sw.WriteLine("<body>");
                                //输出概况
                                sw.WriteLine("<p class=\"wzf\">");
                                sw.WriteLine("<table>");
                                sw.WriteLine("<tr><th>Filename</th><th>Size New Version</th><th>Size Old Version</th><th>Modified</th><th>Added</th><th>Removed</th></tr>");
                            }
                            if (isNewKMST1125WzFormat && isOldKMST1125WzFormat)
                            {
                                comparer.EasyCompareWzFiles(wznew.wz_files[0], wzold.wz_files[0], session.CompareFolder, sw);
                            }
                            else if (!isNewKMST1125WzFormat && !isOldKMST1125WzFormat)
                            {
                                comparer.EasyCompareWzStructures(wznew, wzold, session.CompareFolder, sw);
                            }
                            else if (isNewKMST1125WzFormat && !isOldKMST1125WzFormat)
                            {
                                comparer.EasyCompareWzStructuresToWzFiles(wznew.wz_files[0], wzold, session.CompareFolder, sw);
                            }
                            else
                            {
                                // TODO
                            }
                        }
                        catch (Exception ex)
                        {
                            logFunc(ex.ToString());
                        }
                        finally
                        {
                            wznew.Clear();
                            wzold.Clear();
                            GC.Collect();
                        }

                        if (session.DeadPatch && typedParts[e.Part.WzType].Count == ((WzPatcher)sender).PatchParts.Where(part => part.WzType == e.Part.WzType).Count())
                        {
                            foreach (PatchPartContext part in typedParts[e.Part.WzType].Where(part => part.Type == 1))
                            {
                                ((WzPatcher)sender).SafeMove(part.TempFilePath, part.OldFilePath);
                            }
                            logFunc("  Applying files...\r\n");
                        }
                    }

                    if (string.IsNullOrEmpty(session.CompareFolder) && session.DeadPatch && e.Part.Type == 1 && sender is WzPatcher patcher)
                    {
                        if (patcher.IsKMST1125Format.Value)
                        {
                            // TODO: we should build the file dependency tree to make sure all old files could be overridden safely.
                            logFunc("  (Immediate Patch) Delayed applying files...\r\n");
                        }
                        else
                        {
                            patcher.SafeMove(e.Part.TempFilePath, e.Part.OldFilePath);
                            logFunc("  (Immediate Patch) Applying files...\r\n");
                        }
                    }
                    break;
                case PatchingState.CompareStarted:
                    progressBarX1.Maximum = e.Part.NewFileLength;
                    break;
                case PatchingState.CompareProcessChanged:
                    progressBarX1.Value = (int)e.CurrentFileLength;
                    progressBarX1.Text = string.Format("{0:N0}/{1:N0}", e.CurrentFileLength, e.Part.NewFileLength);
                    break;
                case PatchingState.CompareFinished:
                    progressBarX1.Value = 0;
                    progressBarX1.Maximum = 0;
                    progressBarX1.Text = string.Empty;
                    break;
                case PatchingState.PrepareVerifyOldChecksumBegin:
                    logFunc($"Checking the pre-patch checksum: {e.Part.FileName}");
                    break;
                case PatchingState.PrepareVerifyOldChecksumEnd:
                    logFunc(" Completed\r\n");
                    break;
                case PatchingState.ApplyFile:
                    logFunc($"Applying files: {e.Part.FileName}\r\n");
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
    }
}