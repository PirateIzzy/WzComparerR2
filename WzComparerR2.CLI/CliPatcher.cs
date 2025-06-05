using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WzComparerR2.Patcher;

namespace WzComparerR2.CLI
{
    internal class CliPatcher
    {
        public string GameRegion { get; set; }
        public int OldVersion { get; set; } = 0;
        public int NewVersion { get; set; } = 0;
        public int BaseVersion { get; set; } = 0;
        public bool OverrideMode { get; set; }
        private long availableDiskSpace;
        private bool isUpdating;
        private PatcherSession patcherSession;

        SortedDictionary<string, long> patchedFileSizes = new SortedDictionary<string, long>();
        List<string> patchedFileIndex = new List<string>();
        Dictionary<string, string> finishedFileIndex = new Dictionary<string, string>();
        public Encoding PatcherNoticeEncoding { get; set; }

        private static Dictionary<string, string> RegionUrlTemplate = new Dictionary<string, string>()
        {
            {"KMST", "http://maplestory.dn.nexoncdn.co.kr/PatchT/{1:d5}/{0:d5}to{1:d5}.patch"},
            {"KMST-MINOR", "http://maplestory.dn.nexoncdn.co.kr/PatchT/{0:d5}/Minor/{1:d2}to{2:d2}.patch"},
            {"KMS", "http://maplestory.dn.nexoncdn.co.kr/Patch/{1:d5}/{0:d5}to{1:d5}.patch"},
            {"KMS-MINOR", "http://maplestory.dn.nexoncdn.co.kr/Patch/{0:d5}/Minor/{1:d2}to{2:d2}.patch"},
            {"CMS", "http://mxd.clientdown.sdo.com/maplestory/patch/patchdir/{1:d5}/{0:d5}to{1:d5}.patch" },
            {"MSEA", "http://patch.maplesea.com/sea/patch/patchdir/{1:d5}/{0:d5}to{1:d5}.patch" },
            {"MSEA-ZIP", "http://download-maple.playpark.net/manual/MaplePatch{0:d3}to{1:d3}.zip" },
            {"TMS", "http://tw.cdnpatch.maplestory.beanfun.com/maplestory/patch/patchdir/{1:d5}/{0:d5}to{1:d5}.patch" }
        };

        public void TryGetPatch()
        {
            string url = "";
            if (string.IsNullOrEmpty(GameRegion) || !RegionUrlTemplate.TryGetValue(GameRegion, out string template))
            {
                throw new ArgumentException("Invalid game region.");
            }
            if (OldVersion < 0 || NewVersion < 0)
            {
                throw new ArgumentOutOfRangeException("Version numbers must be non-negative.");
            }
            if (GameRegion.Contains("MINOR"))
            {
                url = string.Format(template, BaseVersion, OldVersion, NewVersion);
            }
            else
            {
                url = string.Format(template, OldVersion, NewVersion);
            }
            DownloadingItem item = new DownloadingItem(url, null);
            try
            {
                Console.WriteLine("Finding patch file, please wait...");
                item.GetFileLength();
                if (item.FileLength > 0)
                {
                    Console.WriteLine($"Patch URL: {item.Url}");
                    Console.WriteLine($"File Length: {item.FileLength} bytes");
                    Console.WriteLine($"Modification Date: {item.LastModified}");
                    Console.WriteLine("Do you want to download this patch?");
                    Console.WriteLine("Press Y - Download");
                    Console.WriteLine("Press C - Copy Download URL to Clipboard");
                    Console.WriteLine("Press N - Do not download");
                    ConsoleKeyInfo cki = Console.ReadKey();
                    int responseType = ReadYNC(cki);
                    switch (responseType)
                    {
                        case 0:
#if NET6_0_OR_GREATER
                            Process.Start(new ProcessStartInfo
                            {
                                UseShellExecute = true,
                                FileName = url,
                            });
#else
                            Process.Start(url);
#endif
                            return;
                        case 1:
                            Clipboard.SetText(url);
                            return;
                        case 2:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving patch information: {ex.Message}");
            }
        }

        public void ApplyPatch(string patchFile, string gameDirectory, bool immediatePatch, bool verbose)
        {
            var session = new PatcherSession()
            {
                PatchFile = patchFile,
                MSFolder = gameDirectory,
                PrePatch = immediatePatch,
                DeadPatch = immediatePatch,
            };
            session.LoggingFileName = Path.Combine(session.MSFolder, $"wcpatcher_{DateTime.Now:yyyyMMdd_HHmmssfff}.log");
            session.PatchExecTask = Task.Run(() => this.ExecutePatchAsync(session, session.CancellationToken, verbose));
            this.patcherSession = session;
            Task.WaitAny(session.PatchExecTask);
        }

        private static int ReadYNC(ConsoleKeyInfo cki)
        {
            switch (cki.Key)
            {
                case ConsoleKey.Y:
                    return 0;
                case ConsoleKey.C:
                    return 1;
                case ConsoleKey.N:
                    return 2;
                default:
                    Console.WriteLine("Invalid Input, please try again.");
                    ConsoleKeyInfo retryCki = Console.ReadKey();
                    return ReadYNC(retryCki);
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

        private async Task ExecutePatchAsync(PatcherSession session, CancellationToken cancellationToken, bool verbose)
        {
            void AppendStateText(string text)
            {
                Console.Write(text);
                if (session.LoggingFileName != null)
                {
                    File.AppendAllText(session.LoggingFileName, text, Encoding.UTF8);
                }
            }
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
                if (verbose)
                {
                    Console.WriteLine(patcher.NoticeText);
                }
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
                if ((patchedAllFileSize > availableDiskSpace) && !this.OverrideMode)
                {
                    Console.WriteLine("Available disk space may be insufficient for finishing patching.");
                    Console.WriteLine("Proceed anyway?");
                    Console.WriteLine("To proceed, press Y.");
                    Console.WriteLine("Pressing any other keys will cancel the operation. ");
                    ConsoleKeyInfo cki = Console.ReadKey();
                    if (cki.Key != ConsoleKey.Y)
                    {
                        AppendStateText("The patch has been canceled.\r\n");
                        throw new OperationCanceledException("The patch has been canceled.");
                    }
                }
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
                Console.WriteLine("Patch Completed. Elapsed time: " + sw.Elapsed);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("The patch has been canceled.");
            }
            catch (UnauthorizedAccessException ex)
            {
                // File IO permission error
                Console.WriteLine(ex.ToString());
            }
            catch (Exception ex)
            {
                AppendStateText(ex.ToString());
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                session.State = PatcherTaskState.Complete;
                if (patcher != null)
                {
                    patcher.Close();
                    patcher = null;
                }
                GC.Collect();
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
                    //progressBarX1.Maximum = e.Part.NewFileLength;
                    session.TemporaryFileMapping.Add(e.Part.FileName, e.Part.TempFilePath);
                    break;
                case PatchingState.TempFileBuildProcessChanged:
                    //progressBarX1.Value = (int)e.CurrentFileLength;
                    //progressBarX1.Text = string.Format("{0:N0}/{1:N0}", e.CurrentFileLength, e.Part.NewFileLength);
                    break;
                case PatchingState.TempFileClosed:
                    logFunc("  Temporary file creation complete\r\n");
                    //progressBarX1.Value = 0;
                    //progressBarX1.Maximum = 0;
                    //progressBarX1.Text = string.Empty;

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
