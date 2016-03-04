﻿using KFreonLib.Debugging;
using KFreonLib.GUI;
using ME3Explorer.Unreal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ME3Explorer
{
    public partial class TexplorerFirstTimeSetup : Form
    {
        public class DLCInfo
        {
            public string Name = null;
            public string Path = null;

            public string SizeString
            {
                get
                {
                    if (Size == 0)
                        return "";

                    string siz = Size.ToString();

                    return siz.Substring(0, (siz.Length <= 4 ? siz.Length : 4));
                }
            }

            private long sfarSize = 0;
            private double size = 0;
            public double Size { 
                get
                {
                    if (!isBaseGame == true && WhichGame == 3)
                    {
                        if (sfarSize == 0)
                            sfarSize = (new FileInfo(sfar)).Length;

                        if (!isExtracted)
                        {
                            if (BackupRequested == true && !isExtracted)
                                size = sfarSize * 2.5;
                            else if (BackupRequested != true)
                                size = sfarSize * 1.5;
                        }

                        return size / Math.Pow(1024, 3);
                    }
                    else
                        return 0;
                }
            }
            public bool? isBaseGame
            {
                get
                {
                    bool? temp = null;
                    if (Name.Contains("BaseGame"))
                        temp = true;
                    else
                        temp = false;
                    return temp;
                }
            }
            public bool? UseExtracted = null;
            public bool? BackupRequested = null;
            public string BackupFileName = null;
            public int WhichGame = -1;
            public bool isBackupPresent
            {
                get
                {
                    return BackupFileName != null;
                }
            }

            public List<string> ExtractedFiles 
            {
                get
                {
                    return KFreonLib.Misc.Methods.EnumerateGameFiles(WhichGame, Files);
                }
            }
            public bool isExtracted
            {
                get
                {
                    UseExtracted = ExtractedFiles.Count != 0 ? UseExtracted : null;
                    return ExtractedFiles.Count != 0;
                }
            }

            public List<string> Files = new List<string>();

            public DLCInfo()
            {

            }

            public DLCInfo(string name, string path, int game)
            {
                Name = name;
                Path = path;
                WhichGame = game;

                GetFiles();

                foreach (string file in Files)
                    if (file.Contains(".backup"))
                        BackupFileName = file;


                if (isBackupPresent && isExtracted)
                {
                    if (UseExtracted == null)
                        UseExtracted = true;
                } 
                else if (isBackupPresent)
                    UseExtracted = true;
                else if (isExtracted)
                    UseExtracted = true;
                else
                {
                    if (BackupRequested == null)
                        BackupRequested = true;
                } 
            }

            public void GetFiles()
            {
                Files.AddRange(KFreonLib.Misc.Methods.EnumerateGameFiles(WhichGame, Path, predicate: new Predicate<string>(target =>
                    {
                        string test = target.ToLowerInvariant();
                        return test.EndsWith(".sfar") || test.EndsWith(".backup") || test.EndsWith(".pcc") || test.EndsWith(".u") || test.EndsWith(".upk") || test.EndsWith(".sfm");
                    })));
            }

            public string sfar
            {
                get
                {
                    return Files.FindLast(t => t.Contains("Default.sfar"));
                }
            }
        }




        public List<string> FilesToAddToTree = null;
        List<DLCInfo> DLCs = new List<DLCInfo>();
        TextUpdater StatusUpdater;
        ProgressBarChanger ProgressUpdater;
        CancellationTokenSource cts = new CancellationTokenSource();
        bool DoingStuff = false;


        public TexplorerFirstTimeSetup(int game, string DLCPath, string CookedPath)
        {
            InitializeComponent();

            // KFreon: Hide GUI stuff
            BackupCheckBox.Visible = false;
            BackupPresentLabel.Visible = false;
            ExtractedLabel.Visible = false;
            ExtractedListBox.Visible = false;


            StatusProgLabel.Text = "Loading...";

            StatusUpdater = new TextUpdater(StatusProgLabel, toolStrip1);
            ProgressUpdater = new ProgressBarChanger(toolStrip1, StatusProgBar);

            SetupStuff(game, DLCPath, CookedPath);

            foreach (DLCInfo dlc in DLCs)
            {
                MainListView.Items.Add(dlc.Name, true);
            }

            StatusUpdater.UpdateText("Ready. Loaded " + (DLCs.Count - 1) + " DLC's. Disk space to be used: ~" + DLCs.Select(d => d.Size).Sum() + " GB");
        }

        private void SetupStuff(int game, string DLCPath, string CookedPath)
        {
            List<string> things = KFreonLib.Misc.Methods.GetInstalledDLC(DLCPath);

            // KFreon: Add basegame
            DLCInfo basegame = new DLCInfo("BaseGame", CookedPath, game);
            DLCs.Add(basegame);

            if (things.Count == 0)
            {
                DebugOutput.PrintLn("No DLC Detected.");
                StatusUpdater.UpdateText("No DLC Detected!");
            }
            else
                foreach (string folder in things)
                {
                    string name = KFreonLib.Misc.Methods.GetDLCNameFromPath(folder);
                    if (name != null)
                        DLCs.Add(new DLCInfo(name, folder, game));
                }
        }

        private void MainListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = MainListView.SelectedIndex;
            if (index < 0)
                return;

            DLCInfo dlc = DLCs[index];

            if (dlc.isBaseGame == true)
            {
                MainListView.SetItemChecked(0, true);

                // KFreon: Hide everything
                BackupCheckBox.Visible = false;
                ExtractedLabel.Visible = false;
                ExtractedListBox.Visible = false;
                BackupPresentLabel.Visible = false;
                DiskSpaceLabel.Text = "";
                return;
            }

            ExtractedListBox.Visible = dlc.isExtracted;
            ExtractedLabel.Visible = dlc.isExtracted;

            if (dlc.isBackupPresent)
            {
                BackupPresentLabel.Text = "Backup present at: " + dlc.BackupFileName;
                BackupPresentLabel.Visible = true;
            }

            if (dlc.isExtracted)
            {
                ExtractedListBox.Items.Clear();
                ExtractedListBox.Items.AddRange(dlc.ExtractedFiles.ToArray());
            }


            BackupCheckBox.Visible = true;
            if (dlc.isBackupPresent && dlc.isExtracted)
            {
                BackupCheckBox.Text = "Use extracted Files?";
                BackupCheckBox.Checked = dlc.UseExtracted == true;
                BackupCheckBox.Enabled = true;
            }
            else if (dlc.isBackupPresent)
            {
                BackupCheckBox.Text = "Backup already present.";
                BackupCheckBox.Checked = false;
                BackupCheckBox.Enabled = false;
            }
            else if (dlc.isExtracted)
            {
                BackupCheckBox.Text = "No backup. Must use extracted.";
                BackupCheckBox.Checked = false;
                BackupCheckBox.Enabled = false;
            }
            else
            {
                BackupCheckBox.Text = "Backup?";
                BackupCheckBox.Checked = dlc.BackupRequested == true;
                BackupCheckBox.Enabled = true;
            }

            DiskSpaceLabel.Text = dlc.SizeString + " GB";
            StatusUpdater.UpdateText("Ready. Loaded " + DLCs.Count + " DLC's. Disk space to be used: ~" + DLCs.Select(d => d.Size).Sum() + " GB");
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            FilesToAddToTree = null;
            this.Close();
        }

        private void BackupCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            int index = MainListView.SelectedIndex;
            if (index < 0)
                return;

            DLCInfo dlc = DLCs[index];
            if (BackupCheckBox.Text.Contains("extracted"))
                dlc.UseExtracted = BackupCheckBox.Checked;
            else
                dlc.BackupRequested = BackupCheckBox.Checked;

            DiskSpaceLabel.Text = dlc.SizeString + " GB";
            StatusUpdater.UpdateText("Ready. Loaded " + DLCs.Count + " DLC's. Disk space to be used: ~" + DLCs.Select(d => d.Size).Sum() + " GB");
        }

        private async void ContinueButton_Click(object sender, EventArgs e)
        {
            FilesToAddToTree = new List<string>();
            BitConverter.IsLittleEndian = true;
            ContinueButton.Enabled = false;
            DoingStuff = true;
            await Task.Run(() => DoStuff());
            DoingStuff = false;
            this.Close();
        }

        private void DoStuff()
        {
            Predicate<string> adder = t => !t.EndsWith("backup") && !t.EndsWith(".tfc") && !t.EndsWith(".sfar");

            ProgressUpdater.ChangeProgressBar(0, MainListView.Items.Count);
            for (int i = 0; i < MainListView.Items.Count; i++)
            {
                if (cts.IsCancellationRequested)
                {
                    DebugOutput.PrintLn("Extraction cancelled!");
                    return;
                }
                bool ischecked = false;
                this.Invoke(new Action(() => ischecked = MainListView.GetItemChecked(i)));
                if (ischecked)
                {
                    DLCInfo dlc = DLCs[i];
                    if (dlc.isBaseGame == false)
                    {
                        if (dlc.isBackupPresent && dlc.UseExtracted == false)
                        {
                            // KFreon: Delete extracted and restore from backup
                            StatusUpdater.UpdateText("Deleting old files and restoring backup for: " + dlc.Name);
                            File.Delete(dlc.sfar);
                            dlc.ExtractedFiles.ForEach(t => File.Delete(t));
                            File.Copy(dlc.BackupFileName, dlc.sfar);
                        }
                        else if (dlc.BackupRequested == true)
                        {
                            // KFreon: Backup sfar
                            StatusUpdater.UpdateText("Backing up: " + dlc.Name);
                            File.Copy(dlc.sfar, Path.ChangeExtension(dlc.sfar, ".backup")); 
                        }
                        else if (dlc.UseExtracted == true)
                        {
                            FilesToAddToTree.AddRange(dlc.Files.Where(a => adder(a)));
                            ProgressUpdater.IncrementBar();
                            continue;
                        }

                        // KFreon: Extract
                        StatusUpdater.UpdateText("Extracting DLC: " + dlc.Name);

                        if (File.Exists(dlc.sfar))
                        {
                            DLCExtractHelper(dlc.sfar);
                            dlc.GetFiles();
                        }
                        else
                            DebugOutput.PrintLn("DLC: " + dlc.sfar + "  failed.");
                    }
                    FilesToAddToTree.AddRange(dlc.Files.Where(a => adder(a)));
                }
                ProgressUpdater.IncrementBar();
            }
        }

        public static List<string> DLCExtractHelper(string file)
        {
            List<string> ExtractedFiles = new List<string>();
            string[] dlcname = file.Split('\\');
            DebugOutput.PrintLn("Temp extracting DLC: " + dlcname[dlcname.Length - 3]);
            DLCPackage dlc = new DLCPackage(file);
            List<string> dlcpath = new List<string>(dlc.MyFileName.Split('\\'));
            dlcpath.RemoveRange(dlcpath.Count - 5, 5);
            string dlcExtractionPath = String.Join("\\", dlcpath.ToArray());

            List<int> Indicies = new List<int>();
            for (int i = 0; i < dlc.Files.Count(); i++)
            {
                DLCPackage.FileEntryStruct entry = dlc.Files[i];
                if (Path.GetExtension(entry.FileName).ToLower() == ".pcc" || Path.GetExtension(entry.FileName).ToLower() == ".tfc")
                {
                    DebugOutput.PrintLn("Extracting: " + dlc.Files[i].FileName);
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(dlcExtractionPath + dlc.Files[i].FileName));
                        using (FileStream fs = new FileStream(dlcExtractionPath + dlc.Files[i].FileName, FileMode.CreateNew))
                            dlc.DecompressEntry(i).WriteTo(fs);

                        Indicies.Add(i);
                    }
                    catch (Exception e)
                    {
                        DebugOutput.PrintLn("File " + dlcExtractionPath + entry.FileName + " already exists.  Extra: " + e.Message);
                        Console.WriteLine(e.Message);
                    }
                    ExtractedFiles.Add(dlcExtractionPath + entry.FileName);
                }
            }
            dlc.DeleteEntry(Indicies);
            return ExtractedFiles;
        }

        private void FormClosingEvent(object sender, FormClosingEventArgs e)
        {
            if (DoingStuff)
            {
                cts.Cancel();
                e.Cancel = true;
            }
        }
    }
}
