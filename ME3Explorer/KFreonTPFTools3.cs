﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gibbed.IO;
using KFreonLib;
using KFreonLib.Textures;
using KFreonLib.GUI;
using KFreonLib.Debugging;
using KFreonLib.MEDirectories;

namespace ME3Explorer
{
    /// <summary>
    /// This is a tool to deal with TPF's and to install textures from TPF's and external images.
    /// </summary>
    public partial class KFreonTPFTools3 : Form
    {
        int WhichGame = 3;
        MEDirectories MEExDirecs = new MEDirectories();

        string DLCPath
        {
            get
            {
                return MEExDirecs.DLCPath;
            }
        }

        string pathBIOGame
        {
            get
            {
                return MEExDirecs.PathBIOGame;
            }
        }
        string pathCooked
        {
            get
            {
                return MEExDirecs.pathCooked;
            }
        }
        string ExecFolder
        {
            get
            {
                return MEExDirecs.ExecFolder;
            }
        }

        int numValid
        {
            get
            {
                if (LoadedTexes.Count == 0)
                    return 0;

                return LoadedTexes.Where(t => t.Valid).Count();
            }
        }

        int numImages
        {
            get
            {
                if (LoadedTexes.Count == 0)
                    return 0;

                return LoadedTexes.Where(t => !t.isDef).Count();
            }
        }

        string tttt = null;
        string TemporaryPath
        {
            get
            {
                if (tttt == null)
                    tttt = Path.Combine(ExecFolder, "TPFToolsTemp");
                return tttt;
            }
        }
        TreeDB Tree = null;
        private readonly Object BackBoneLock = new object();
        BackBone backbone;
        CancellationTokenSource cts = new CancellationTokenSource();
        List<TPFTexInfo> LoadedTexes = new List<TPFTexInfo>();
        List<SaltTPF.ZipReader> zippys = new List<SaltTPF.ZipReader>();
        List<object> FormControls = new List<object>();

        TextUpdater Overall;
        TextUpdater Current;
        ProgressBarChanger OverallProg;
        ProgressBarChanger CurrentProg;
        Gooey gooey;
        bool AttemptedAnalyse = false;
        bool PreventPCC = false;

        bool isAnalysed
        {
            get
            {
                return gooey.GetControlAffectedState("RunAutofix");
            }
        }

        static KFreonTPFTools3 CurrentInstance = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        public KFreonTPFTools3()
        {
            InitializeComponent();
            UpgradeSettings();

            // KFreon: Set number of threads if necessary
            if (Properties.Settings.Default.NumThreads == 0)
            {
                Properties.Settings.Default.NumThreads = KFreonLib.Misc.Methods.SetNumThreads(false);
                Properties.Settings.Default.Save();
            }

            Overall = new TextUpdater(OverallStatusLabel, BottomStrip);
            Current = new TextUpdater(CurrentStatusLabel, BottomStrip);
            OverallProg = new ProgressBarChanger(BottomStrip, OverallProgressBar);
            CurrentProg = new ProgressBarChanger(BottomStrip, CurrentProgressBar);

            CurrentInstance = this;

            backbone = new BackBone(() =>
            {
                gooey.ChangeState(false);
                DisableCancelButton(false);
                return true;
            },
                () =>
                {
                    DisableCancelButton(true);
                    gooey.ChangeState(true);
                    return true;
                }
            );

            // KFreon: Setup GUI
            Task.Run(() =>
            {
                // KFreon: Wait for controls to be created
                while (!MainTreeView.Parent.Created)
                    System.Threading.Thread.Sleep(50);

                this.Invoke(new Action(() =>
                {
                    // KFreon: Setup pathing and stuff
                    InitialiseGUI();
                    Initialise(false);
                    BeginTreeLoading();
                    ResetImageList();
                    DisappearDuplicatesBox(true);
                    ContextPanel.Height = 0;
                    MainTreeView.Height = 753 + 25;
                    PreviewTabPages.Height = 525;
                }));
            });
        }


        /// <summary>
        /// Sets up GUI elements 
        /// </summary>
        private void InitialiseGUI()
        {
            // KFreon: Make preview bigger
            PreviewBox.Dock = DockStyle.Fill;
            texmodPreviewBox.Dock = DockStyle.Fill;
            DisappearTextBox(true);
            EnableSecondProgressBar(false);
            FirstHalfInfoState(false);
            SecondHalfInfoState(false);

            // KFreon: Setup gooey
            DebugOutput.PrintLn("Gooeying things");
            gooey = new Gooey(PreviewTabPages);
            gooey.AddControl(LoadButton, "Load", true);
            gooey.AddControl(ExtractTOP, "ExtractTOP", true);
            gooey.AddControl(ClearAllFilesButton, "ClearAll", true);
            gooey.AddControl(RebuildTOP, "Rebuild", true);
            gooey.AddControl(RunAutofixButton, "RunAutofix", true);
            gooey.AddControl(MODtoTPFButton, "MODtoTPF", true);
            gooey.AddControl(AnalyseButton, "Analyse", true);
            gooey.AddControl(SaveModButton, "SaveMod", true);
            gooey.AddControl(InstallButton, "InstallB", true);
            gooey.AddControl(AutofixInstallButton, "AutoFixInstall", true);
            gooey.AddControl(extractInvalidToolStripMenuItem, "extractInvalid", true);
            gooey.AddControl(ChangePathsButton, "ChangePaths", true);
            gooey.AddControl(ChangeButton, "ChangeButton", true);

            gooey.ChangeState(false);
            

            // KFreon: Initialise MainListView



            OverallProgressBar.Value = 0;
            OverallStatusLabel.Text = "Ready.";
        }


        /// <summary>
        /// Upgrades settings to current build. Some weird thing necessary for keeping settings throughout rebuilds.
        /// </summary>
        private void UpgradeSettings()
        {
            Properties.Settings.Default.Upgrade();
        }


        /// <summary>
        /// Changes textbox and previewbox visibility.
        /// </summary>
        /// <param name="state">State to set to. If true, textbox disappears and previewbox is visible.</param>
        private void DisappearTextBox(bool state)
        {
            if (texmodPreviewBox.InvokeRequired)
                this.Invoke(new Action(() => DisappearTextBox(state)));
            else
            {
                texmodPreviewBox.Visible = !state;
                PreviewBox.Visible = state;
            }
        }


        /// <summary>
        /// Sets up some basic things.
        /// </summary>
        /// <param name="changeTree"></param>
        private void Initialise(bool changeTree)
        {
            // KFreon: Start debugger in separate thread
            DebugOutput.StartDebugger("TPF/DDS Tools 3.0");

            // KFreon: Get game version if necessary
            if (!changeTree)
                WhichGame = Properties.Settings.Default.TPFGameVersion;
            else
            {
                DebugOutput.PrintLn();
                DebugOutput.PrintLn("Changing Trees...");
            }

            MEExDirecs.WhichGame = WhichGame;

            // KFreon: Change window and button text
            ChangeButton.Text = "Modding ME" + WhichGame;
            this.Text = "TPF/DDS Tools 3.0:  " + "ME" + WhichGame;

            // KFreon: Change paths and clear tree if necessary
            if (changeTree)
            {
                if (Tree != null)
                    Tree.Clear(true);
            }
            MEExDirecs.SetupPathing(false);


            // KFreon: Actually set visual cues
            DoGameIndicatorChecks();

            // KFreon: Cleanup temporary files unless changing tree
            if (!changeTree)
                Cleanup();
        }


        /// <summary>
        /// Changes game indicator colours if necessary
        /// </summary>
        void DoGameIndicatorChecks()
        {
            List<string> messages = new List<string>();
            List<bool> states = KFreonLib.Misc.Methods.CheckGameState(MEExDirecs, false, out messages);
            for (int i = 0; i < 3; i++)
                ChangeIndicatorColours(i + 1, states[i], false);
        }


        /// <summary>
        /// Cleans up temporary files.
        /// </summary>
        private void Cleanup()
        {
            // KFreon: Clear temporary files
            for (int i = 0; i < 3; i++)
            {
                if (Directory.Exists(TemporaryPath))
                {
                    try
                    {
                        Directory.Delete(TemporaryPath, true);
                        DebugOutput.PrintLn("Successfully deleted existing temp files.");
                        break;
                    }
                    catch
                    {
                        DebugOutput.PrintLn("Failed to delete temp files. Waiting...");
                        System.Threading.Thread.Sleep(300);
                    }
                }
                else if (i != 0)
                {
                    DebugOutput.PrintLn("Successfully deleted existing temp files.");
                    break;
                }
            }
        }


        /// <summary>
        /// Begins tree loading MAYBE ASYNC?
        /// </summary>
        private void BeginTreeLoading()
        {
            // KFreon: Move to backbone if necessary
            if (!MainTreeView.InvokeRequired)
            {
                backbone.AddToBackBone(b =>
                {
                    BeginTreeLoading();
                    return true;
                });
                return;
            }
            string orig = OverallStatusLabel.Text;
            Overall.UpdateText("Attempting to load tree...");
            LoadTrees();
            Overall.UpdateText(orig);

            // KFreon: Change GUI stuff
            gooey.ModifyControl("ExtractTOP", false);
            gooey.ModifyControl("ClearAll", false);
            gooey.ModifyControl("Rebuild", false);
            gooey.ModifyControl("RunAutofix", false);
            gooey.ModifyControl("extractInvalid", false);
        }


        /// <summary>
        /// Sets colours of game-exists and tree-exists indicators.
        /// </summary>
        /// <param name="game">Game to change indicater of.</param>
        private void ChangeIndicatorColours(int game, bool state, bool isTree)
        {
            if (PreviewTabPages.InvokeRequired)
                PreviewTabPages.Invoke(new Action(() => ChangeIndicatorColours(game, state, isTree)));
            else
            {
                Color color = state ? Color.LightGreen : Color.Red;
            
                switch (game)
                {
                    case 1:
                        if (isTree)
                            OneTreeLabel.ForeColor = color;
                        else
                            OneLabel.ForeColor = color;
                        break;
                    case 2:
                        if (isTree)
                            TwoTreeLabel.ForeColor = color;
                        else
                            TwoLabel.ForeColor = color;
                        break;
                    case 3:
                        if (isTree)
                            ThreeTreeLabel.ForeColor = color;
                        else
                            ThreeLabel.ForeColor = color;
                        break;
                }
            }
        }


        /// <summary>
        /// Loads Tree.
        /// </summary>
        /// <returns>True if tree loaded properly.</returns>
        private bool LoadTrees()
        {
            // KFreon: Try to setup tree
            Task<TreeDB> current = null;
            for (int i = 1; i < 4; i++)
            {
                // KFreon: Start task for tree we want
                var y = i;
                Task<TreeDB> temptask = Task.Run(() =>
                {
                    TreeDB temptree = null;
                    string tempcooked = MEExDirecs.GetDifferentPathCooked(y);
                    string tempbio = MEExDirecs.GetDifferentPathBIOGame(y);
                    bool res = Texplorer2.SetupTree(ref temptree, tempcooked, y, null, tempbio);
                    bool temp = false;
                    if (res)
                    {
                        int status2;
                        temp = temptree.ReadFromFile(ExecFolder + "me" + y + "tree.bin", Path.GetDirectoryName(tempbio), ExecFolder + "ThumbnailCaches\\ME" + y + "ThumbnailCache\\", out status2);
                    }

                    ChangeIndicatorColours(y, temp, true);
                    DebugOutput.PrintLn(temp ? "ME" + y + " tree found." : "ME" + y + " tree not found.");
                    return temp ? temptree : null;
                });


                if (i == WhichGame)
                    current = temptask;
            }

            //KFreon: Wait for tree we want to finish loading.
            while (!current.IsCompleted)
            {
                System.Threading.Thread.Sleep(50);
                Application.DoEvents();
            }

            Tree = current.Result;
            return Tree != null;
        }


        /// <summary>
        /// Changes cancel button visibility. 
        /// </summary>
        /// <param name="state">Hides cancel button if true.</param>
        private void DisableCancelButton(bool state)
        {
            if (BottomStrip.InvokeRequired)
                this.Invoke(new Action(() => DisableCancelButton(state)));
            else
            {
                // KFreon: Deal with cancel
                CancelButton.Visible = !state;
                if (!state)
                {
                    CancelButton.Text = "Cancel";
                    CancelButton.Enabled = true;
                    cts = new CancellationTokenSource();
                }
            }
        }


        private void LoadButton_Click(object sender, EventArgs e)
        {
            BeginLoadingFiles();
        }


        private void BeginLoadingFiles(List<string> Files = null)
        {
            backbone.AddToBackBone(b =>
            {
                bool retval = LoadFiles(Files);
                if (retval)
                {
                    gooey.ModifyControl("ExtractTOP", true);
                    gooey.ModifyControl("Rebuild", true);
                    gooey.ModifyControl("ChangePaths", false);
                    gooey.ModifyControl("ClearAll", true);
                }
                return retval;
            });
        }


        private bool LoadFiles(List<string> files)
        {
            // KFreon: Select files to load if required
            List<string> Files = new List<string>();
            if (files == null)
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Select file/s to load";
                    ofd.Filter = "All Supported|*.dds;*.tpf;*.MEtpf;*.jpg;*.jpeg;*.png;*.bmp|DDS Images|*.dds|Texmod TPF's|*.tpf|Texplorer TPF's|*.MEtpf|Images|*.jpg;*.jpeg;*.png;*.bmp;*.dds|Standard Images|*.jpg;*.jpeg;*.png;*.bmp";
                    ofd.Multiselect = true;

                    System.Windows.Forms.DialogResult res = System.Windows.Forms.DialogResult.Abort;
                    this.Invoke(new Action(() => res = ofd.ShowDialog()));

                    if (res == System.Windows.Forms.DialogResult.OK)
                        Files.AddRange(ofd.FileNames.ToList());
                    else
                        return false;
                }
            }
            else
                Files = files;

            // KFreon: Change GUI
            this.Invoke(new Action(() =>
            {
                OverallProg.ChangeProgressBar(0, Files.Count);
                OverallStatusLabel.Text = "Loading file" + ((Files.Count == 1) ? "" : "s") + "...";
            }));

            foreach (string file in Files)
            {
                if (cts.IsCancellationRequested)
                    return false;

                switch (Path.GetExtension(file).ToLowerInvariant())
                {
                    case ".tpf":
                    case ".metpf":
                        LoadTPF(file);
                        break;
                    case ".dds":
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".bmp":
                        LoadExternal(file, false);
                        break;
                    case ".log":
                    case ".txt":
                    case ".def":
                        LoadExternal(file, true);
                        break;
                    default:
                        DebugOutput.PrintLn("File: " + file + " is unsupported.");
                        break;
                }

                OverallProg.IncrementBar();
            }

            this.Invoke(new Action(() =>
            {
                OverallProgressBar.Value = OverallProgressBar.Maximum;
                OverallStatusLabel.Text = "File" + ((Files.Count > 1) ? "s" : "") + " loaded! Ready.";

                RedrawTreeView();
            }));
            GC.Collect();
            return true;
        }

        public void RedrawTreeView()
        {
            if (MainTreeView.InvokeRequired)
                this.Invoke(new Action(() =>
                {
                    MainTreeView.BeginUpdate();
                    RedrawTreeView();
                    MainTreeView.EndUpdate();
                }));
            else
            {
                MainTreeView.BeginUpdate();
                List<myTreeNode> nodes = new List<myTreeNode>();
                ResetImageList();
                for (int i = 0; i < LoadedTexes.Count; i++)
                {
                    TPFTexInfo curr = LoadedTexes[i];

                    // KFreon: If analysed, add visual cues
                    string text = curr.FormatTexDetails(isAnalysed);
                    myTreeNode node = new myTreeNode(text);
                    node.TexInd = i;

                    // KFreon: Make tree dups obvious
                    if (curr.TreeDuplicates.Count != 0)
                    {
                        Font tempfont = node.NodeFont ?? MainTreeView.Font;
                        node.NodeFont = new Font(tempfont, FontStyle.Italic);
                    }

                    if (!curr.isDef)
                    {
                        // KFreon: Deal with File dups
                        if (curr.FileDuplicates.Count != 0)
                            for (int j = 0; j < curr.FileDuplicates.Count; j++)
                            {
                                TPFTexInfo trr = curr.FileDuplicates[j];
                                text = trr.FormatTexDetails(isAnalysed);
                                trr.ThumbInd = -1;
                                myTreeNode temp = new myTreeNode(text);
                                temp.TexInds.Add(i);
                                temp.TexInds.Add(j);
                                node.Nodes.Add(temp);
                                curr.FileDuplicates[j] = trr;
                            }

                        try
                        {
                            Bitmap bmp = new Bitmap(curr.Thumbnail);
                            curr.ThumbInd = MainTreeViewImageList.Images.Count;
                            MainTreeViewImageList.Images.Add(bmp);
                        }
                        catch
                        {
                            curr.ThumbInd = 2;
                        }
                    }
                    else
                        curr.ThumbInd = 1;

                    nodes.Add(node);
                    LoadedTexes[i] = curr;

                }

                MainTreeView.Nodes.Clear();
                MainTreeView.Nodes.AddRange(nodes.ToArray());
                SetTreeImages(MainTreeView);
                MainTreeView.EndUpdate();
            }
        }


        private void ResetImageList()
        {
            MainTreeViewImageList.Images.Clear();
            MainTreeViewImageList.Images.Add(Image.FromFile(Path.Combine(ExecFolder, "TPFTools.ico")));
            MainTreeViewImageList.Images.Add(Image.FromFile(Path.Combine(ExecFolder, "TextDoc.jpg")));
            MainTreeViewImageList.Images.Add(Image.FromFile(Path.Combine(ExecFolder, "Placeholder.ico")));
        }

        private void SetTreeImagesInternal(myTreeNode treeNode)
        {
            int index = 0;
            if (treeNode.TexInd != -1)
            {
                TPFTexInfo tex = LoadedTexes[treeNode.TexInd];
                index = tex.ThumbInd;
            }
            else
                index = 0;

            treeNode.ImageIndex = index;
            treeNode.SelectedImageIndex = index;

            // Print each node recursively.
            foreach (myTreeNode tn in treeNode.Nodes)
                SetTreeImagesInternal(tn);
        }

        private void SetTreeImages(TreeView treeview)
        {
            for (int i = 0; i < treeview.Nodes.Count; i++)
                SetTreeImagesInternal(treeview.Nodes[i] as myTreeNode);
        }

        private void LoadTPF(string file)
        {
            EnableSecondProgressBar(true);

            // KFreon: Open TPF and set some properties
            SaltTPF.ZipReader zippy = new SaltTPF.ZipReader(file);
            zippy.Description = "TPF Details\n\nFilename:  \n" + zippy._filename + "\n\nComment:  \n" + zippy.EOFStrct.Comment + "\nNumber of stored files:  " + zippy.Entries.Count;
            zippy.Scanned = false;
            int zippyInd = zippys.Count;
            zippys.Add(zippy);
            int numEntries = zippy.Entries.Count;

            // KFreon: Setup nodes and GUI elements
            this.Invoke(new Action(() =>
            {
                CurrentProg.ChangeProgressBar(0, numEntries);
                CurrentStatusLabel.Text = "Processing file: " + Path.GetFileName(file);
            }));
            DebugOutput.PrintLn("Loading file: " + Path.GetFileName(file));

            // KFreon: Get hash info from TPF
            string alltext = "";
            try
            {
                byte[] data = zippy.Entries[numEntries - 1].Extract(true);
                char[] chars = new char[data.Length];
                for (int i = 0; i < data.Length; i++)
                    chars[i] = (char)data[i];
                alltext = new string(chars);
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occurred during extraction: " + e.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // KFreon: Get individual hashes without duplicate lines
            List<string> parts = alltext.Replace("\r", "").Split('\n').ToList();
            parts.RemoveAll(s => s == "\0");
            List<string> tempparts = new List<string>();
            foreach (string part in parts)
                if (!tempparts.Contains(part))
                    tempparts.Add(part);
            parts = tempparts;


            // KFreon: Thread TPF loading
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 1;//Properties.Settings.Default.NumThreads;
            DebugOutput.PrintLn("Reading TPF using " + po.MaxDegreeOfParallelism + " threads.");
            List<TPFTexInfo> temptexes = new List<TPFTexInfo>();
            for (int i = 0; i < numEntries; i++)
            {
                temptexes.Add(new TPFTexInfo());
            }

            Parallel.For(0, numEntries, po, i =>
            {
                // KFreon: Add TPF entries to TotalTexes list
                TPFTexInfo tmpTex = new TPFTexInfo(zippy.Entries[i].Filename, i, null, zippy, WhichGame);

                // KFreon: Find and set hash
                foreach (string line in parts)
                    if (line.ToLowerInvariant().Contains(tmpTex.FileName.ToLowerInvariant()))
                    {
                        tmpTex.Hash = KFreonLib.Textures.Methods.FormatTexmodHashAsUint(line);
                        tmpTex.OriginalHash = tmpTex.Hash;
                        tmpTex.FileName = line.Split('|')[1].Replace("\r", "");
                        break;
                    }

                // KFreon: If hash gen failed, notify
                if (!tmpTex.isDef && tmpTex.Hash == 0)
                    DebugOutput.PrintLn("Failure to get hash for entry " + i + " in " + file);

                // KFreon: Get details
                if (!tmpTex.isDef)
                    tmpTex.EnumerateDetails();

                temptexes[i] = tmpTex;
                
                CurrentProg.IncrementBar();
            });

            // KFreon: Load textures into list and treeview
            LoadedTexes.AddRange(temptexes);
            EnableSecondProgressBar(false);
        }

        public int LoadExternal(string file, bool isDef)
        {
            EnableSecondProgressBar(false);

            TPFTexInfo tmpTex = new TPFTexInfo(Path.GetFileName(file), -1, Path.GetDirectoryName(file), null, WhichGame);

            // KFreon: Get hash
            if (!isDef)
            {
                string hash = "";

                // KFreon: Check if hash in filename
                if (file.Contains("0x"))
                    hash = file.Substring(file.IndexOf("0x"), 10);
                else  // KFreon: If not in filename, look in all non TPF .defs
                    foreach (TPFTexInfo tex in LoadedTexes)
                    {
                        if (tex.isDef && tex.isExternal)
                        {
                            using (StreamReader sr = new StreamReader(Path.Combine(tex.FilePath, tex.FileName)))
                                while (!sr.EndOfStream)
                                {
                                    string line = sr.ReadLine();
                                    if (line.Contains(file + '|'))
                                    {
                                        int start = line.IndexOf('|');
                                        hash = line.Substring(start + 1, line.Length - (start + 1));
                                        break;
                                    }
                                }
                            if (hash != "")
                                break;
                        }
                    }


                // KFreon: Convert hash to uint
                if (hash != "")
                    tmpTex.Hash = KFreonLib.Textures.Methods.FormatTexmodHashAsUint(hash);

                tmpTex.OriginalHash = tmpTex.Hash;
            }


            // KFreon: Get details
            if (!tmpTex.isDef)
                tmpTex.EnumerateDetails();

            // KFreon: Add node and its index to current node
            myTreeNode temp = new myTreeNode(Path.GetFileName(file));
            temp.TexInd = LoadedTexes.Count;
            this.Invoke(new Action(() =>
            {
                MainTreeView.Nodes.Add(temp);
                OverallProg.IncrementBar();
            }));

            LoadedTexes.Add(tmpTex);

            return temp.TexInd;
        }

        private void EnableSecondProgressBar(bool state)
        {
            if (BottomStrip.InvokeRequired)
                this.Invoke(new Action(() => EnableSecondProgressBar(state)));
            else
            {
                CurrentProgressBar.Visible = state;
                CurrentStatusLabel.Visible = state;
                OverallLabel.Visible = state;
                toolStripSeparator2.Visible = state;
                toolStripSeparator3.Visible = state;
                CurrentLabel.Visible = state;
            }
        }

        private void MainTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TPFTexInfo tex;
            GetSelectedTex(out tex);
            DisplayInfo(tex);


            // KFreon: Preview image
            Task.Run(() =>
            {
                try
                {
                    PreviewObject(tex);
                }
                catch (Exception ex)
                {
                    DebugOutput.PrintLn("Preview failed: " + ex.Message);
                }
            });
        }

        private void ClearPreview()
        {
            try
            {
                if (PreviewBox.InvokeRequired)
                    this.Invoke(new Action(() => ClearPreview()));
                else if (PreviewBox.Image != null)
                {
                    PreviewBox.Image.Dispose();
                    PreviewBox.Image = null;
                }
            }
            catch { }
        }

        private void PreviewObject(TPFTexInfo tex)
        {
            // KFreon: Clear old image
            ClearPreview();

            // KFreon: Get data
            byte[] data = tex.Extract(null, true);
            if (data == null)
                return;

            // KFreon: Load new one
            if (tex.isDef)
            {
                DisappearTextBox(false);
                try
                {
                    string message = Encoding.UTF8.GetString(data);
                    this.Invoke(new Action(() => texmodPreviewBox.Text = message));
                }
                catch (Exception e)
                {
                    DebugOutput.PrintLn("Unable to get text from data: " + e.Message);
                }
            }
            else
            {
                //KFreonLib.Textures.Methods.GetImage(tex.Format, data);
                Bitmap img = null;
                using (ImageEngine.KFreonImage kfimg = new ImageEngine.KFreonImage(data))
                    img = kfimg.ToBitmap();
                if (img == null)
                    return;

                try
                {
                    this.Invoke(new Action(() => PreviewBox.Image = KFreonLib.Textures.Creation.GenerateThumbImage(img, 512)));
                }
                catch { }

                DisappearTextBox(true);
                img.Dispose();
            }
        }

        private void DisplayInfo(TPFTexInfo tex)
        {
            string message = "";
            FirstHalfInfoState(true);

            // KFreon: Display top info
            if (tex.isExternal)
                message = "External file\n\nPath: " + tex.FilePath + "\\" + tex.FileName;
            else
                message = tex.zippy.Description;
            GeneralInfoRTB.Text = message;

            // KFreon: Disappear stuff if unnecessary
            SecondHalfInfoState(!AnalyseButton.Enabled && !CancelButton.Visible && !tex.isDef);
            if (tex.isDef)
            {
                FirstHalfInfoState(false);
                return;
            }
            GotoInvalidButton.Visible = !AnalyseButton.Enabled && LoadedTexes.Where(ter => !ter.isDef && !ter.Valid).Count() > 1;
            GotoDupButton.Visible = tex.TreeDuplicates.Count != 0;
            PromoteButton.Visible = tex.ThumbInd == -1;
            InstallSingleButton.Visible = !AnalyseButton.Enabled && !tex.isDef;

            // KFreon: Tree details
            TreeMipsEntry.Text = (tex.ExpectedMips == 0 ? "¯\\_(ツ)_/¯" : tex.ExpectedMips.ToString());
            TreeFormatEntry.Text = (tex.ExpectedFormat == "") ? "¯\\_(ツ)_/¯" : tex.ExpectedFormat;

            // KFreon: Display main info
            TPFFormatEntry.Text = tex.Format;
            HashTextBox.Text = (tex.Hash == 0) ? "Unknown" : KFreonLib.Textures.Methods.FormatTexmodHashAsString(tex.Hash);
            TPFMipsEntry.Text = tex.NumMips.ToString();
            ImageSizeEntry.Text = tex.Width + "x" + tex.Height;

            // KFreon: Set resethash button visibility
            if (!CancelButton.Visible)
                ResetHashButton.Visible = (tex.isDef) ? false : (tex.Hash != tex.OriginalHash);

            // KFreon: Deal with duplicates
            if (tex.TreeDuplicates.Count != 0)
            {
                DisappearDuplicatesBox(false);
                DuplicatesTextBox.Text = "TREE DUPLICATES:" + Environment.NewLine;
                for (int i = 0; i < tex.TreeDuplicates.Count; i++)
                    DuplicatesTextBox.Text += LoadedTexes[tex.TreeDuplicates[i]].TexName + Environment.NewLine;
            }
            else
                DisappearDuplicatesBox(true);


            // KFreon: Do PCC stuff
            PreventPCC = true;

            // KFreon: Show pccs
            if (tex.TexName != null)
            {
                PCCsCheckListBox.Items.Clear();
                int count = 0;
                foreach (string file in tex.OriginalFiles)
                {
                    string displaystring = tex.DisplayString(file);
                    string filedisp = tex.GetFileFromDisplay(file);
                    PCCsCheckListBox.Items.Add(displaystring);
                    bool isChecked = true;
                    if (!tex.Files.Contains(filedisp))
                        isChecked = false;
                    PCCsCheckListBox.Items[count++].Checked = isChecked;
                }
                /*PCCsCheckListBox.SuspendLayout();
                PCCsCheckListBox.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.ColumnContent);
                PCCsCheckListBox.ResumeLayout();*/
            }
            PCCsCheckListBox.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            PreventPCC = false;
        }

        private void CheckSelectAllPCCsList(bool SelectAll, bool FromButton)
        {
            bool state = true;

            // KFreon: Check all unless all already checked, then uncheck all
            if (PCCsCheckListBox.CheckedIndices.Count == PCCsCheckListBox.Items.Count)
                state = false;

            this.Invoke(new Action(() => PCCSelectAllButton.Text = (!state ? "Deselect All" : "Select All")));

            if (SelectAll)
                for (int i = 0; i < PCCsCheckListBox.Items.Count; i++)
                    PCCsCheckListBox.Items[i].Checked = state;

            if (FromButton)
                PCCsCheckBox_IndexChanged(null, null);
        }

        private void UpdateSelectedTexPCCList(TPFTexInfo tex, int index, bool SelectAll = false)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(() => UpdateSelectedTexPCCList(tex, index, SelectAll)));
            else
            {
                CheckSelectAllPCCsList(SelectAll, false);

                List<string> newfiles = new List<string>();
                List<int> newexpids = new List<int>();
                for (int i = 0; i < PCCsCheckListBox.Items.Count; i++)
                {

                    if (PCCsCheckListBox.Items[i].Checked)
                    {
                        newfiles.Add(tex.OriginalFiles[i]);
                        newexpids.Add(tex.OriginalExpIDs[i]);
                    }
                }
                tex.Files = new List<string>(newfiles);
                tex.ExpIDs = new List<int>(newexpids);
                LoadedTexes[index] = tex;
            }
        }

        private void ShowContextPanel(bool state)
        {
            /*if (!ContextPanel.InvokeRequired)
                Task.Run(new Action(() => ShowContextPanel(state)));*/
            if (ContextPanel.InvokeRequired)
                this.Invoke(new Action(() => ShowContextPanel(state)));
            else
            {
                /*Transitions.TransitionType_CriticalDamping trans = new Transitions.TransitionType_CriticalDamping(300);
                Transitions.Transition.run(ContextPanel, "Height", (state ? 25 : 0), trans);
                Transitions.Transition.run(MainTreeView, "Height", (state ? MainSplitter.Panel1.Height - 25 : MainSplitter.Panel1.Height), trans);*/
                ContextPanel.Height = state ? 25 : 0;
                MainTreeView.Height = state ? MainSplitter.Panel1.Height - 25 : MainSplitter.Panel1.Height;
            }
        }

        private void DisappearDuplicatesBox(bool state)
        {
            if (DuplicatesTextBox.InvokeRequired)
                this.Invoke(new Action(() => DisappearDuplicatesBox(state)));
            else
                DuplicatesTextBox.Visible = !state;
        }

        private void FirstHalfInfoState(bool state)
        {
            if (MainTreeView.InvokeRequired)
                FirstHalfInfoState(state);
            else
            {
                TPFFormatLabel.Visible = state;
                TPFFormatEntry.Visible = state;
                TPFMipsEntry.Visible = state;
                TPFMipsLabel.Visible = state;
                HashLabel.Visible = state;
                HashTextBox.Visible = state;
                ImageSizeEntry.Visible = state;
                ImageSizeLabel.Visible = state;
            }
        }

        private void SecondHalfInfoState(bool state)
        {
            if (MainTreeView.InvokeRequired)
                this.Invoke(new Action(() => SecondHalfInfoState(state)));
            else
            {
                TreeFormatEntry.Visible = state;
                TreeFormatLabel.Visible = state;
                TreeMipsEntry.Visible = state;
                TreeMipsLabel.Visible = state;
            }
        }

        private void HashBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !CancelButton.Visible && gooey.GetControlAffectedState("Analyse"))
            {
                TPFTexInfo tex;
                int index = GetSelectedTex(out tex);
                DebugOutput.PrintLn("Updating hash for texture: " + Path.GetFileName(tex.FileName) + " to: " + HashTextBox.Text);
                UpdateHashAndReplace(tex, index, HashTextBox.Text);
                e.Handled = true;
                e.SuppressKeyPress = true;
                DisplayInfo(tex);
            }
        }

        public void UpdateHashAndReplace(TPFTexInfo tex, int index, string hash)
        {
            if ((tex = UpdateHash(tex, hash)).Files != null)
                LoadedTexes[index] = tex;
        }

        public void UpdateHashAndReplace(int index, string hash)
        {
            TPFTexInfo tex = LoadedTexes[index];
            UpdateHashAndReplace(tex, index, hash);
        }

        public void UpdateHashAndReplace(int index, uint hash, bool SetOrigToo)
        {
            TPFTexInfo tex = LoadedTexes[index];
            UpdateHash(tex, hash);
            if (SetOrigToo)
                tex.OriginalHash = hash;
            LoadedTexes[index] = tex;
        }

        private TPFTexInfo UpdateHash(TPFTexInfo tex, string hash)
        {
            // KFreon: Check new hash
            uint newhash = 0;
            try
            {
                newhash = KFreonLib.Textures.Methods.FormatTexmodHashAsUint(hash);
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Specified hash invalid: " + e.Message);
                MessageBox.Show("Hash specified is invalid. Got enough characters? Remember you need 0x at the start.");
                return null;
            }

            tex.Hash = newhash;
            return tex;
        }

        private TPFTexInfo UpdateHash(TPFTexInfo tex, uint hash)
        {
            tex.Hash = hash;
            return tex;
        }

        private int GetSelectedTex(out TPFTexInfo tex)
        {
            tex = null;
            myTreeNode node = MainTreeView.SelectedNode as myTreeNode;
            if (node == null)
                return -1;

            if (node.TexInd >= LoadedTexes.Count)
                return 0;

            if (node.TexInds.Count != 0)
                tex = LoadedTexes[node.TexInds[0]].FileDuplicates[node.TexInds[1]];
            else
                tex = LoadedTexes[node.TexInd];
            return node.TexInd;
        }

        private int GetParentTex(out TPFTexInfo tex)
        {
            myTreeNode node = MainTreeView.SelectedNode as myTreeNode;
            myTreeNode parent = (node.Parent as myTreeNode);
            tex = LoadedTexes[parent.TexInd];
            return parent.TexInd;
        }

        private void ResetHashButton_Click(object sender, EventArgs e)
        {
            /*if (MessageBox.Show("Are you sure you want to reset this texture's hash to its originally computed value?", "Phew, heading back into good territory.", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {*/
            TPFTexInfo tex;
            int index = GetSelectedTex(out tex);
            tex.Hash = tex.OriginalHash;
            LoadedTexes[index] = tex;
            DisplayInfo(tex);
            //}
        }

        private void AnalyseButton_Click(object sender, EventArgs e)
        {
            if (!CheckTree())
            {
                AttemptedAnalyse = true;
                return;
            }

            // KFreon: If no textures loaded, ask
            if (LoadedTexes.Count == 0)
            {
                BeginLoadingFiles();
                backbone.AddToBackBone(result =>
                {
                    if (!result)
                    {
                        this.Invoke(new Action(() =>
                        {
                            EnableSecondProgressBar(false);
                            OverallStatusLabel.Text = "Ready.";
                            OverallProgressBar.Value = 0;
                        }));
                        return false;
                    }
                    else
                    {
                        AnalyseVsTree();
                        return true;
                    }
                });
            }
            else  // KFreon: Just run analysis
            {
                backbone.AddToBackBone(result =>
                {
                    AnalyseVsTree();
                    return true;
                });
            }
        }

        private void AnalyseVsTree()
        {
            // KFreon: Change GUI
            gooey.ModifyControl("Rebuild", true);
            gooey.ModifyControl("RunAutofix", true);
            gooey.ModifyControl("extractInvalid", true);
            gooey.ModifyControl("Analyse", false);


            DebugOutput.PrintLn("Matching hashes...");
            Overall.UpdateText("Matching hashes...");
            OverallProg.ChangeProgressBar(0);

            MatchHashes();
            FindTreeDuplicates();


            // KFreon: Change GUI
            gooey.ModifyControl("Load", true);
            gooey.ModifyControl("MODtoTPF", true);
            gooey.ModifyControl("extractInvalid", true);
            gooey.ModifyControl("RunAutofix", true);

            RedrawTreeView();

            OverallProg.ChangeProgressBar(1, 1);
            Overall.UpdateText("Matching complete! " + numValid + " valid out of " + numImages + " images.");


            gooey.ModifyControl("ExtractTOP", true);
            gooey.ModifyControl("ClearAll", true);
            gooey.ModifyControl("Rebuild", true);
            
        }

        private bool CheckTextures()
        {
            bool retval = true;

            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = Properties.Settings.Default.NumThreads;

            DebugOutput.PrintLn("Checking textures and finding tree duplicates...");

            for (int i = 0; i < LoadedTexes.Count; i++)
            {
                TPFTexInfo tex = LoadedTexes[i];
                if (cts.IsCancellationRequested)
                    return false;


                // KFreon: Found only
                if (!tex.found || tex.isDef)
                    continue;
                else
                    LoadedTexes[i] = tex;
                OverallProg.IncrementBar();
            }
            if (cts.IsCancellationRequested)
                retval = false;
            return retval;
        }

        private void MatchHashes()
        {
            DebugOutput.PrintLn("Tree Texes: " + Tree.TexCount);
            DebugOutput.PrintLn("Checking for File Duplicates...");

            // KFreon: Deal with duplicates
            RemoveFileDuplicates();

            // KFreon: Redraw treeview
            //RedrawTreeView();

            this.Invoke(new Action(() =>
            {
                OverallProg.ChangeProgressBar(0, LoadedTexes.Where(tex => tex.found).Count());
                Overall.UpdateText("Searching tree for matches. Please Wait...");
            }));
        }

        private void ShowPCCContextPanel(bool state)
        {
            if (!PCCContextPanel.InvokeRequired)
                Task.Run(new Action(() => ShowPCCContextPanel(state)));
            else
            {
                bool tempstate = state;
                if (PCCsCheckListBox.Items.Count == 0)
                    tempstate = false;
                Transitions.TransitionType_CriticalDamping trans = new Transitions.TransitionType_CriticalDamping(300);
                //Transitions.Transition.run(PreviewTabPages, "Height", ((tempstate) ? 495 : 525), trans);

            }
        }

        private void FindTreeDuplicates()
        {
            List<TPFTexInfo> temptexes = new List<TPFTexInfo>(LoadedTexes);

            int offset = 0;
            // KFreon: For each loaded texture, find its duplicates in the tree and add them as seperate textures
            for (int i = 0; i < temptexes.Count; i++)
            {
                if (temptexes[i].isDef)
                    continue;

                TPFTexInfo curr = temptexes[i];//.Clone();
                if (curr.Hash == 0)
                    return;

                int tempoffset = 0;

                // KFreon: Search tree for duplicates
                for (int j = 0; j < Tree.TexCount; j++)
                {
                    TreeTexInfo treetex = Tree.GetTex(j);
                    if (curr.Hash == treetex.Hash)
                    {
                        // KFreon: If texture already set up, then treetex is a duplicate
                        if (curr.Files.Count != 0)
                        {
                            // KFreon: Clone current for duplicate
                            TPFTexInfo temp = curr.Clone();

                            // KFreon: Clear texture lists
                            temp.Files.Clear();
                            temp.ExpIDs.Clear();
                            temp.OriginalFiles.Clear();
                            temp.OriginalExpIDs.Clear();

                            // KFreon: Change duplicate specific entries
                            temp.UpdateTex(j, treetex);
                            temp.TreeDuplicates.Add(offset + ++tempoffset + i);
                            curr.TreeDuplicates.Add(curr.TreeDuplicates.Count + offset + i);
                            temp.TreeDuplicates.Sort();
                            curr.TreeDuplicates.Sort();

                            // KFreon: File duplicates
                            if (curr.FileDuplicates.Count != 0)
                            {
                                for (int k = 0; k < curr.FileDuplicates.Count; k++)
                                {
                                    TPFTexInfo currTex = curr.FileDuplicates[k];
                                    TPFTexInfo tempTex = temp.FileDuplicates[k];

                                    currTex.TreeDuplicates = new List<int>(curr.TreeDuplicates);
                                    tempTex.TreeDuplicates = new List<int>(temp.TreeDuplicates);

                                    curr.FileDuplicates[k] = currTex;
                                    temp.FileDuplicates[k] = tempTex;
                                }
                            }

                            LoadedTexes.Insert(offset + i, temp);
                        }
                        else
                        {
                            // KFreon: Update current details
                            curr.UpdateTex(j, treetex);
                        }
                    }
                }
                LoadedTexes[offset + tempoffset + i] = curr;
                offset += tempoffset;
            }
        }

        private void RemoveFileDuplicates()
        {
            List<TPFTexInfo> duplicates = new List<TPFTexInfo>();
            int currentPos = 0;
            int stepperPos = 1;
            while (true)
            {
                // KFreon: Break when finished
                if (currentPos >= LoadedTexes.Count - 1)
                    break;


                TPFTexInfo curr = LoadedTexes[currentPos];
                TPFTexInfo step = LoadedTexes[stepperPos];

                // KFreon: Ignore currentPos if .def
                if (!curr.isDef)
                {
                    // KFreon: Ignore stepperPos if .def
                    if (!step.isDef)
                    {
                        // KFreon: Check if textures are identical
                        if (curr.Hash == step.Hash && curr.FileName == step.FileName)
                        {
                            // KFreon: Add to duplicate list and remove from overall texes
                            duplicates.Add(step);
                            LoadedTexes.RemoveAt(stepperPos);
                        }
                    }

                    // KFreon: Advance stepper and position (if applicable)
                    if (stepperPos < LoadedTexes.Count - 1)
                        stepperPos++;
                    else
                    {
                        // KFreon: Add duplicates to current tex
                        curr.FileDuplicates.AddRange(duplicates);
                        LoadedTexes[currentPos] = curr;
                        duplicates.Clear();
                        currentPos++;
                        stepperPos = currentPos + 1;
                    }
                }
                else
                {
                    // KFreon: Ignore current texture cos its a .def
                    currentPos++;
                    stepperPos = currentPos + 1;
                }
            }
        }

        private void MainTreeView_DragEnter(object sender, DragEventArgs e)
        {
            if (CancelButton.Visible || !AnalyseButton.Enabled)
                return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private void MainTreeView_DragDrop(object sender, DragEventArgs e)
        {
            List<string> DroppedFiles = new List<string>((string[])e.Data.GetData(DataFormats.FileDrop, false));
            List<string> ValidDrops = new List<string>();
            List<string> Invalids = new List<string>();

            // KFreon: Check valid files
            foreach (string file in DroppedFiles)
                switch (Path.GetExtension(file).ToLowerInvariant())
                {
                    case ".tpf":
                    case ".metpf":
                    case ".dds":
                    case ".def":
                    case ".txt":
                    case ".log":
                        ValidDrops.Add(file);
                        break;
                    default:
                        Invalids.Add(file);
                        break;
                }

            // KFreon: Notify if some are invalid
            if (Invalids.Count > 0)
                MessageBox.Show("The following files are not TPFTools things. .dds, .def/.log/.txt, .tpf/.metpf ONLY" + Environment.NewLine + String.Join(Environment.NewLine, Invalids.ToArray()), "You have failed. We will find another.");

            BeginLoadingFiles(ValidDrops);
        }

        private void CloseFilesButton_Click(object sender, EventArgs e)
        {
            if (!CancelButton.Visible)
            {
                DebugOutput.PrintLn("Terminating remote access...");

                ImageEngine.ImageEngine.Shutdown();

                MainTreeView.Nodes.Clear();
                LoadedTexes.Clear();
                GeneralInfoRTB.Text = "";
                HashTextBox.Clear();
                PCCsCheckListBox.Items.Clear();
                zippys.Clear();
                AttemptedAnalyse = false;

                DebugOutput.PrintLn("Deleting runtimes...");

                // KFreon: Reset picturebox
                if (PreviewBox.Image != null)
                {
                    PreviewBox.Image.Dispose();
                    PreviewBox.Image = null;
                    PreviewBox.Refresh();
                }
                DebugOutput.PrintLn("Removing archives... (legion rules!)");

                InitialiseGUI();
                gooey.ModifyControl("ExtractTOP", false);
                gooey.ModifyControl("ClearAll", false);
                gooey.ModifyControl("Rebuild", false);
                gooey.ModifyControl("RunAutofix", false);
                gooey.ModifyControl("extractInvalid", false);
                gooey.ChangeState(true);

                Cleanup();
                DisappearDuplicatesBox(true);
                ShowPCCContextPanel(false);
                ShowContextPanel(false);

                GC.Collect();
            }
        }

        private void extractAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string outputPath = "";
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    outputPath = fbd.SelectedPath;
                else
                    return;
            }
            Extractor(outputPath);
        }

        private void Extractor(string ExtractPath)
        {
            Extractor(ExtractPath, null, t => true);
        }

        private void Extractor(string ExtractPath, TPFTexInfo tex, Predicate<TPFTexInfo> predicate)
        {
            // KFreon: Move to backbone if necessary
            if (!MainTreeView.InvokeRequired)
            {
                backbone.AddToBackBone(b =>
                {
                    Extractor(ExtractPath, tex, predicate);
                    return true;
                });
                return;
            }

            DebugOutput.PrintLn("Extracting textures to somewhere like: " + ExtractPath);
            OverallProg.ChangeProgressBar(0, 1);

            // KFreon: Extract single texture
            if (tex != null && tex.Files != null)
            {
                DebugOutput.PrintLn("Extracting single texture: " + tex.TexName);
                tex.Extract(ExtractPath);
            }
            else  
            {
                // KFreon: Extract many based on predicate
                List<TPFTexInfo> filtered = new List<TPFTexInfo>(LoadedTexes.Where(texn => predicate(texn)));
                OverallProg.ChangeProgressBar(0, filtered.Count);

                foreach (TPFTexInfo texn in filtered)
                {
                    texn.Extract(ExtractPath);
                    OverallProg.IncrementBar();
                }
            }

            OverallProg.ChangeProgressBar(1, 1);
        }

        private void extractInvalidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string outputPath = "";
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    outputPath = fbd.SelectedPath;
                else
                    return;
            }
            Overall.UpdateText("Extracting " + LoadedTexes.Where(r => !r.Valid && !r.isDef).Count() + " invalid textures...");
            Extractor(outputPath, null, t => !t.Valid && !t.isDef);
            Overall.UpdateText("All invalids extracted!");
        }

        private void MainTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (!CancelButton.Visible)
            {
                if (MainTreeView.Nodes.Count == 0)
                    return;

                if (e.KeyCode == Keys.Delete)
                {
                    if (!DeleteEntry())
                        return;
                }
                else if (e.KeyCode == Keys.Up)
                {
                    int index = MainTreeView.Nodes.IndexOf(MainTreeView.SelectedNode);
                    if (index > 0)
                        MainTreeView.SelectedNode = MainTreeView.Nodes[index - 1];
                }
                else if (e.KeyCode == Keys.Down)
                {
                    int index = MainTreeView.Nodes.IndexOf(MainTreeView.SelectedNode);
                    if (index < MainTreeView.Nodes.Count)
                        MainTreeView.SelectedNode = MainTreeView.Nodes[index + 1];
                }
            }
            e.Handled = true;
        }

        private bool DeleteEntry(int ind = -1)
        {
            if (LoadedTexes.Count == 1)
            {
                CloseFilesButton_Click(null, null);
                return false;
            }

            TPFTexInfo tex;
            int index = ind == -1 ? GetSelectedTex(out tex) : ind;

            // KFreon: Wipe out nodes to stop preview glitch
            MainTreeView.Nodes.Clear();
            ClearPreview();
            FirstHalfInfoState(false);

            // KFreon: Remove from lists
            MainTreeView.SuspendLayout();
            LoadedTexes.RemoveAt(index);

            RedrawTreeView();
            MainTreeView.ResumeLayout();
            return true;
        }

        private void MainTreeView_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (LoadedTexes.Count == 0)
                return;


            // KFreon: Use given font, or default
            Font font = e.Node.NodeFont ?? (sender as TreeView).Font;

            string text = e.Node.Text;
            myTreeNode nod = (e.Node as myTreeNode);
            TPFTexInfo curr = nod.TexInd == -1 ? LoadedTexes[nod.TexInds[0]].FileDuplicates[nod.TexInds[1]] : LoadedTexes[nod.TexInd];


            TextRenderer.DrawText(e.Graphics, text, font, e.Node.Bounds, Color.Black, TextFormatFlags.VerticalCenter);
        }


        private void MainTreeView_FocusLeave(object sender, EventArgs e)
        {
            ShowContextPanel(false);
        }

        private void MainTreeView_Click(object sender, EventArgs e)
        {
            if (CancelButton.Visible)
                return;

            ShowContextPanel(true);
        }

        private void PCCsCheckBox_IndexChanged(object sender, ItemCheckedEventArgs e)
        {
            if (!CancelButton.Visible && !PreventPCC)
            {
                ShowPCCContextPanel(true);
                TPFTexInfo tex;
                int index = GetSelectedTex(out tex);
                UpdateSelectedTexPCCList(tex, index);
            }
        }

        private void SaveModButton_Click(object sender, EventArgs e)
        {
            if (!CheckTree())
                return;

            // KFreon: If no loaded textures, run analysis code which asks for loading, then analyses
            if (LoadedTexes.Count == 0 || LoadedTexes[0].TexName == null)
            {
                AnalyseButton_Click(null, null);
                backbone.AddToBackBone(result =>
                {
                    return SaveModInternal(result);
                });
            }
            else  // KFreon: Everything is fine, just save stuff
            {
                backbone.AddToBackBone(result =>
                {
                    return SaveModInternal(true);
                });
            }
        }

        private bool SaveModInternal(bool result)
        {
            string savePath = "";
            if (result && (savePath = GetSavePath("Select destination for .mod", "ME3Ex .mod|*.mod")) != "")
            {
                if (SaveValidToMod(savePath))
                {
                    this.Invoke(new Action(() =>
                    {
                        OverallStatusLabel.Text = ".mod saved! Ready.";
                        OverallProgressBar.Value = OverallProgressBar.Maximum;
                    }));
                    return true;
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        OverallStatusLabel.Text = "Saving .mod failed or cancelled!";
                        OverallProgressBar.Value = OverallProgressBar.Maximum;
                    }));
                    return false;
                }
            }
            else
            {
                this.Invoke(new Action(() =>
                {
                    EnableSecondProgressBar(false);
                    OverallStatusLabel.Text = "Ready.";
                    OverallProgressBar.Value = 0;
                }));
                return false;
            }
        }

        private string GetSavePath(string title, string extension)
        {
            string retval = "";
            if (MainTreeView.InvokeRequired)
                this.Invoke(new Action(() => retval = GetSavePath(title, extension)));
            else
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = title;
                    sfd.Filter = extension;
                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        retval = sfd.FileName;
                }
            return retval;
        }

        private bool SaveValidToMod(string saveFile)
        {
            // KFreon: Return if no valid ones
            if (numValid == 0)
            {
                this.Invoke(new Action(() => MessageBox.Show("No valid textures to save!", "Looks like you need a Quarian", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                return false;
            }

            // KFreon: Update GUI
            this.Invoke(new Action(() =>
            {
                OverallStatusLabel.Text = "Saving .mod...";
                OverallProg.ChangeProgressBar(0, numValid);
            }));

            // KFreon: Get valid textures (defs are not valid)
            List<TPFTexInfo> temp = new List<TPFTexInfo>(LoadedTexes.Where(tex => tex.Valid));

            using (FileStream fs = new FileStream(saveFile, FileMode.Create, FileAccess.Write))
            {
                KFreonLib.Scripting.ModMaker.WriteModHeader(fs, temp.Count);
                foreach (TPFTexInfo tex in temp)
                {
                    KFreonLib.Scripting.ModMaker.ModJob job = tex.CreateModJob(ExecFolder, pathBIOGame);
                    job.WriteJobToFile(fs);
                    OverallProg.IncrementBar();
                    if (cts.IsCancellationRequested)
                        return false;
                }
            }
            return true;
        }

        private void InstallButton_Click(object sender, EventArgs e)
        {
            if (!CheckTree())
                return;

            // KFreon: If no loaded textures, run analysis code which asks for loading, then analyses
            if (LoadedTexes.Count == 0 || LoadedTexes[0].TexName == null)
            {
                AnalyseButton_Click(null, null);
                backbone.AddToBackBone(result =>
                {
                    bool res = InstallValid(result);
                    if (!res)
                    {
                        Overall.UpdateText("Install Failed!");
                        OverallProg.ChangeProgressBar(1, 1);
                    }
                    return res;
                });
            }
            else  // KFreon: Everything is fine, just save stuff
            {
                backbone.AddToBackBone(result =>
                {
                    bool res = InstallValid(true);
                    if (!res)
                    {
                        Overall.UpdateText("Install Failed!");
                        OverallProg.ChangeProgressBar(1, 1);
                    }
                    return res;
                });
            }
        }


        private bool InstallTextures(List<TPFTexInfo> textures, bool result)
        {
            // KFreon: Cancel if analysis failed
            if (!result)
            {
                this.Invoke(new Action(() =>
                {
                    EnableSecondProgressBar(false);
                    OverallStatusLabel.Text = "Ready.";
                    OverallProgressBar.Value = 0;
                }));
                return false;
            }

            // KFreon: Get valids only
            List<TPFTexInfo> validtexes = new List<TPFTexInfo>();
            validtexes = new List<TPFTexInfo>(textures.Where(tex => tex.Valid));

            int valids = validtexes.Count;
            if (valids == 0)
                return true;

            // KFreon: Setup modified DLC list
            List<string> modifiedDLC = new List<string>();

            OverallProg.ChangeProgressBar(0, validtexes.Count + 1);
            int count = 1;
            DebugOutput.PrintLn("Textures loaded = " + validtexes.Count);
            DebugOutput.PrintLn("Num valid: " + valids);

            // KFreon: Install textures
            Texplorer2 texplorer = new Texplorer2(true, WhichGame);
            foreach (TPFTexInfo tex in validtexes)
            {
                Overall.UpdateText("Installing mod:  " + tex.TexName + " | " + count + "/" + valids + " mods completed.");
                DebugOutput.PrintLn("Installing mod:  " + tex.TexName + " | " + count++ + "/" + valids + " mods completed.");
                OverallProg.IncrementBar();

                texplorer.InstallTexture(tex.TexName, tex.Files, tex.ExpIDs, tex.Extract(null, true));

                // KFreon: Add modified DLC to list
                if (WhichGame == 3)
                {
                    foreach (string file in tex.Files)
                    {
                        string dlcname = KFreonLib.Misc.Methods.GetDLCNameFromPath(file);
                        if (dlcname != "" && dlcname != null && !modifiedDLC.Contains(dlcname))
                            modifiedDLC.Add(dlcname);
                    }
                }

                // KFreon: Check cancellation
                if (cts.IsCancellationRequested)
                    return false;
            }

            // KFreon: Update TOC's
            this.Invoke(new Action(() =>
            {
                OverallProgressBar.Value = OverallProgressBar.Maximum - 1;
                OverallStatusLabel.Text = "Checking TOC.bin...";
            }));
            DebugOutput.PrintLn("Updating Basegame...");
            Texplorer2.UpdateTOCs(pathBIOGame, WhichGame, DLCPath, modifiedDLC);
            this.Invoke(new Action(() =>
            {
                OverallStatusLabel.Text = "All mods installed!";
                OverallProgressBar.Value = OverallProgressBar.Maximum;
            }));

            return true;
        }


        private bool InstallValid(bool result)
        {
            return InstallTextures(LoadedTexes, result);
        }

        private bool CheckTree()
        {
            // KFreon: If no tree, try again
            if (Tree == null || Tree.TexCount == 0)
                if (!LoadTrees())
                {
                    MessageBox.Show("No ME" + WhichGame + " Tree found! Use Texplorer to generate one.", "Lets not get ahead of ourselves", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            return true;
        }

        private void ChangeButton_Click(object sender, EventArgs e)
        {
            // KFreon: Change trees
            int whichgame = -1;
            switch (WhichGame)
            {
                case 1:
                    whichgame = 2;
                    break;
                case 2:
                    whichgame = 3;
                    break;
                case 3:
                    whichgame = 1;
                    break;
            }
            ChangeTrees(whichgame);

            System.Threading.Thread.Sleep(400);
            if (Tree != null)
                backbone.AddToBackBone(b =>
                {
                    // KFreon: If analysed OR previous analysis failed, undo analysis and do it again
                    if (isAnalysed || AttemptedAnalyse)
                    {
                        for (int i = 0; i < LoadedTexes.Count; i++)
                        {
                            TPFTexInfo tex = LoadedTexes[i];
                            tex.UndoAnalysis(whichgame);
                            LoadedTexes[i] = tex;
                        }

                        // KFreon: Wait for tree changing task to begin, then analyse again.
                        AnalyseButton_Click(null, null);
                    }
                    return true;
                });

        }

        private void ChangeTrees(int whichgame)
        {
            WhichGame = whichgame;
            MEExDirecs.WhichGame = whichgame;
            Properties.Settings.Default.TPFGameVersion = WhichGame;
            Properties.Settings.Default.Save();
            Initialise(true);
            BeginTreeLoading();
        }

        private void ExtractButton_Click(object sender, EventArgs e)
        {
            TPFTexInfo tex;
            int index = GetSelectedTex(out tex);

            string outputPath = "";
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Where to extract to?";
                sfd.Filter = "DirectX Images|*.dds";
                sfd.FileName = tex.TexName + "_" + KFreonLib.Textures.Methods.FormatTexmodHashAsString(tex.Hash) + ".dds";

                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    outputPath = sfd.FileName;
                else
                    return;
            }
            Extractor(outputPath, tex, null);
            Overall.UpdateText("Extraction complete!");
        }

        private void ConvertButton_Click(object sender, EventArgs e)
        {

        }

        private void GotoInvalid_Click(object sender, EventArgs e)
        {
            TPFTexInfo tex;
            int index = GetSelectedTex(out tex);
            if (index == -1)
                index = GetParentTex(out tex);

            // KFreon: Must go to next invalid and wrap back to the top
            if (index == MainTreeView.Nodes.Count - 1)
            {
                index = -1;
                tex = LoadedTexes[0];
            }

            if (index < MainTreeView.Nodes.Count - 1)
            {
                for (int i = index + 1; i < MainTreeView.Nodes.Count; i++)
                {
                    myTreeNode node = (myTreeNode)MainTreeView.Nodes[i];
                    TPFTexInfo texture = LoadedTexes[node.TexInd];
                    if (!tex.isDef && !texture.Valid)
                    {
                        MainTreeView.SelectedNode = node;
                        break;
                    }
                }
            }
        }

        private void ExportPCCListButton_Click(object sender, EventArgs e)
        {
            TPFTexInfo tex;
            int index = GetSelectedTex(out tex);
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Select export location for PCC list";
                sfd.Filter = "Text Files|*.txt";
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    using (StreamWriter sw = new StreamWriter(sfd.FileName))
                        foreach (string line in tex.Files)
                            sw.WriteLine(line);
                else
                    return;
            }
            Overall.UpdateText("PCC list exported!");
        }

        private void GotoDupButton_Click(object sender, EventArgs e)
        {
            TPFTexInfo tex;
            int index = GetSelectedTex(out tex);

            List<int> indicies = new List<int>(tex.TreeDuplicates.Where(ind => ind > index));
            if (indicies.Count == 0)
                indicies = tex.TreeDuplicates;
            myTreeNode node = (myTreeNode)MainTreeView.Nodes[indicies[0]];
            MainTreeView.SelectedNode = node;
        }

        private void PromoteDupButton_Click(object sender, EventArgs e)
        {
            TPFTexInfo selected;
            TPFTexInfo parent;
            int parentIndex = GetParentTex(out parent);
            int selectedInd = GetSelectedTex(out selected);

            parent.FileDuplicates.Remove(selected);
            selected.FileDuplicates = new List<TPFTexInfo>(parent.FileDuplicates);
            selected.FileDuplicates.Add(parent);

            LoadedTexes[parentIndex] = selected;
            RedrawTreeView();
        }

        private void PCCSelectAllButton_Click(object sender, EventArgs e)
        {
            CheckSelectAllPCCsList(true, true);
        }

        private void TabControl_TabChanged(object sender, EventArgs e)
        {
            bool state = PreviewTabPages.SelectedTab.Text == "PCC's";
            ShowPCCContextPanel(state);
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            if (CancelButton.Visible)
                return;

            if (HelpButton.Text == "Help")
            {
                ShowContextPanel(true);
                System.Threading.Thread.Sleep(400);
                SetupToolTip(PreviewTabPages);
                SetupToolTip(MainTreeView);
                SetupToolTip(DuplicatesTextBox);
                SetupToolTip(GeneralInfoRTB);
                SetupToolTip(DetailsSplitter.Panel2);
                SetupToolTip(ContextPanel);

                HelpButton.Text = "Further help online";
            }
            else
            {
                Process.Start("http://me3explorer.freeforums.org/tutorial-tpf-dds-tools-3-0-t1428.html");
                HelpButton.Text = "Help";
            }
        }

        public void SetupToolTip(Control control)
        {
            ToolTip newtip = new ToolTip();
            //newtip.IsBalloon = true;
            newtip.Show(PrimaryToolTip.GetToolTip(control), control, 10, 10, 100000);
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            if (CancelButton.Visible && MessageBox.Show("Background Tasks are running. Are you sure you want to close?", "Reeeally sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
            {
                cts.Cancel();
                e.Cancel = true;

                Task.Run(() =>
                {
                    while (!OverallStatusLabel.Text.ToLowerInvariant().Contains("failed"))
                        System.Threading.Thread.Sleep(100);

                    DebugOutput.PrintLn("-----Execution of TPF/DDS Tools closing...-----");
                    CurrentInstance = null;
                    Properties.Settings.Default.Save();
                    this.Close();
                });
            }

            DebugOutput.PrintLn("-----Execution of TPF/DDS Tools closing...-----");
            CurrentInstance = null;
            Properties.Settings.Default.Save();
        }

        private void RebuildTOP_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want a TPF that is compatible with Texmod? NOTE: Both are compatible with TPFTools.", "Either works", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No)
                RepackWithTexplorer();
            else
                RepackWithTexmod();
        }

        private bool RepackWithTexplorer()
        {
            string path = "";
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Select location to save TPF";
                sfd.Filter = "Texplorer TPF|*.metpf";
                sfd.FileName = "";
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    path = sfd.FileName;
                else
                    return false;
            }

            // KFreon: Extract files to central location and build .log
            ExtractAndBuildLog(Path.Combine(TemporaryPath, "TPF_REBUILD"));

            List<string> files = new List<string>(Directory.GetFiles(TemporaryPath));
            SaltTPF.ZipWriter.Repack(path, files);
            return true;
        }

        private void ExtractAndBuildLog(string extractPath)
        {
            // KFreon: Create path if necessary
            if (!Directory.Exists(extractPath))
                Directory.CreateDirectory(extractPath);

            using (FileStream fs = new FileStream(extractPath + "\\MEResults.log", FileMode.Create))
            {
                foreach (TPFTexInfo tex in LoadedTexes)
                {
                    // KFreon: Ignore textures with no hash
                    if (tex.Hash == 0)
                        continue;

                    // KFreon: Write hashes to log
                    string hash = KFreonLib.Textures.Methods.FormatTexmodHashAsString(tex.Hash);
                    fs.WriteString("0x" + hash.ToUpper() + "|" + tex.FileName + "\n");
                }
                Extractor(extractPath, null, t => t.Hash != 0 && !t.isDef);
            }
        }

        private void RepackWithTexmod()
        {
            bool success = false;

            // KFreon: Extract all valid textures and build .def
            ExtractAndBuildLog(Path.Combine(TemporaryPath, "\\TexmodRebuild"));

            ProcessStartInfo pc = null;

            // KFreon: Begin voodoo computer control chanting
            string texmodLocFile = Path.Combine(ExecFolder, "texmodLocation.txt");
            string builder = Path.Combine(ExecFolder, "Texmod_Builder.exe");
            string logloc = '\"' + Path.Combine(TemporaryPath, "TexmodRebuild\\MEresults.log") + '\"';
            if (File.Exists(texmodLocFile))
            {
                using (StreamReader sr = new StreamReader(texmodLocFile))
                {
                    string texmodpath = sr.ReadLine();
                    pc = new ProcessStartInfo(texmodpath);
                    Process.Start(pc);
                    pc = new ProcessStartInfo(builder, logloc);
                }
            }
            else
            {
                this.Invoke(new Action(() =>
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Title = "Select Texmod.exe";
                        ofd.Filter = "Texmod|texmod.exe";
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            File.WriteAllText(texmodLocFile, ofd.FileName);
                            pc = new ProcessStartInfo(ofd.FileName);
                            Process.Start(pc);
                            System.Threading.Thread.Sleep(500);
                            pc = new ProcessStartInfo(builder, logloc);
                        }
                    }
                }));
            }

            // KFreon: If texmod found
            if (pc != null)
            {
                System.Threading.Thread.Sleep(2000);

                try
                {
                    Process.Start(pc).WaitForExit();
                    success = true;
                }
                catch (Exception e)
                {
                    DebugOutput.PrintLn("Weird stuff happening with ExecFolder. Let me know what this is ->  " + ExecFolder);
                    DebugOutput.PrintLn("Also: " + e.Message);
                }
            }

            Overall.UpdateText(success ? "Build complete." : "Build failed.");
        }

        private void MODtoTPFButton_Click(object sender, EventArgs e)
        {
            string filename = "";
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select .mod to convert to .tpf";
                ofd.Filter = "ME3Explorer Mods|*.mod";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    return;

                filename = ofd.FileName;
            }

            Overall.UpdateText("Loading .mod from: " + filename);
            backbone.AddToBackBone(b =>
            {
                LoadMOD(filename);
                return true;
            });
        }

        private void LoadMOD(string filename)
        {
            ModMaker modmaker = new ModMaker();
            string basetemppath = Path.Combine(TemporaryPath, "MOD CONVERSION");

            // KFreon: Setup temp folder
            for (int i = 0; i < 3; i++)
                try
                {
                    if (Directory.Exists(basetemppath))
                        Directory.Delete(basetemppath);
                    else
                        break;
                }
                catch
                {
                    System.Threading.Thread.Sleep(100);
                }
            Directory.CreateDirectory(basetemppath);

            // KFreon: Load mods
            Overall.UpdateText("Loading Mod: " + filename);
            int nummods;
            modmaker.LoadMods(new string[] { filename }, out nummods, true);
            Overall.UpdateText("Formatting/Updating .mods...");
            modmaker.FormatJobs(true, true);


            List<TreeTexInfo> TreeTexes = Tree.GetTreeAsList();
            int count = 1;
            OverallProg.ChangeProgressBar(0, nummods);
            foreach (KFreonLib.Scripting.ModMaker.ModJob job in KFreonLib.Scripting.ModMaker.JobList)
            {
                if (cts.IsCancellationRequested)
                    break;

                if (job.JobType != "TEXTURE")
                    DebugOutput.PrintLn("Job:" + job.Name + " isn't a texture job. Ignoring...");
                else
                {
                    Overall.UpdateText("Converting job: " + count++ + " of " + nummods);

                    // KFreon: Change Trees if necessary
                    if (WhichGame != job.WhichGame)
                        ChangeTrees(job.WhichGame);   // wait here?

                    uint inthash = KFreonLib.Textures.Methods.FindHashByName(job.Texname, job.PCCs, job.ExpIDs, TreeTexes);
                    string hash = KFreonLib.Textures.Methods.FormatTexmodHashAsString(inthash);

                    if (hash == "0")
                        DebugOutput.PrintLn("Unable to find hash for " + job.Texname + ". Continuing...");
                    else
                    {
                        string newname = "MASSEFFECT" + WhichGame + ".EXE_" + hash + ".dds";
                        string newpath = Path.Combine(basetemppath, newname);
                        File.WriteAllBytes(newpath, job.data);
                        LoadExternal(newpath, false);
                    }
                }
            }

            if (!cts.IsCancellationRequested)
                Overall.UpdateText("Loaded " + (count - 1) + " jobs out of " + KFreonLib.Scripting.ModMaker.JobList.Count + " from Modmaker.");
            else
                Overall.UpdateText("Mod loading cancelled!");
            OverallProg.ChangeProgressBar(1, 1);
            RedrawTreeView();
        }

        private void ChangePathsButton_Click(object sender, EventArgs e)
        {
            using (KFreonLib.Helpers.PathChanger changer = new KFreonLib.Helpers.PathChanger(MEExDirecs.GetDifferentPathBIOGame(1), MEExDirecs.GetDifferentPathBIOGame(2), MEExDirecs.GetDifferentPathBIOGame(3)))
            {
                if (changer.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    return;

                // KFreon: Set and save settings
                MEExDirecs.SetPaths(changer.PathME1, changer.PathME2, changer.PathME3);

                // KFreon: Check game paths again for indicators
                DoGameIndicatorChecks();
            }
        }

        public static async Task<KFreonTPFTools3> GetCurrentInstance()
        {
            if (CurrentInstance == null)
            {
                CurrentInstance = new KFreonTPFTools3();
                CurrentInstance.Show();

                await Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(400);
                    while (!CurrentInstance.LoadButton.Enabled)
                        System.Threading.Thread.Sleep(50);
                });
            }

            return CurrentInstance;
        }

        private void RunAutofixButton_Click(object sender, EventArgs e)
        {
            List<TPFTexInfo> invalids = LoadedTexes.Where(t => t.found && !t.Valid && !t.isDef).ToList();

            backbone.AddToBackBone(a => Autofix(invalids));
        }

        private bool Autofix(List<TPFTexInfo> texes)
        {
            bool retval = false;

            OverallProg.ChangeProgressBar(0, texes.Count);
            
            foreach (TPFTexInfo tex in texes)
            {
                Overall.UpdateText("Fixing: " + tex.TexName);
                DebugOutput.PrintLn("Fixing: " + tex.TexName + Environment.NewLine + "     FORMAT -> Current: " + tex.Format + "  Expected: " + tex.ExpectedFormat + Environment.NewLine + "     MIPS -> Current: " + tex.NumMips + "  Expected: " + tex.ExpectedMips);
                byte[] arr = tex.Extract(null, true);
                ImageEngine.KFreonImage img = new ImageEngine.KFreonImage(arr);
                bool formatsuccess = img.ChangeFormat(tex.ExpectedFormat);
                if (!formatsuccess)
                {
                    DebugOutput.PrintLn("Autofix failed on image: " + tex.TexName + ". Reason: " + ImageEngine.ImageEngine.GetILError());
                    tex.AutofixSuccess = false;
                }

                string path = tex.Autofixedpath(TemporaryPath);
                bool success = img.ConvertAndSave(path, tex.ExpectedMips > 1);

                if (!success)
                {
                    DebugOutput.PrintLn("Autofix failed on image: " + tex.TexName + ". Reason: " + ImageEngine.ImageEngine.GetILError());
                    tex.AutofixSuccess = false;
                }

                if (!retval && success)
                    retval = true;
                else if (retval && !success)
                    retval = false;

                tex.FilePath = Path.GetDirectoryName(tex.Autofixedpath(TemporaryPath));
                tex.EnumerateDetails();

                RedrawTreeView();
            }
            Overall.UpdateText("Autofix complete." + (!retval ? "Some errors occured." : ""));
            OverallProg.ChangeProgressBar(1, 1);
            return retval;
        }

        private void AutofixInstallButton_Click(object sender, EventArgs e)
        {

        }

        private async void ReplaceButton_Click(object sender, EventArgs e)
        {
            TPFTexInfo tex;
            int index = GetSelectedTex(out tex);
            if (index < 0)
                return;

            string replacingPath = null;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {

                ofd.Title = "Select image to replace with";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    replacingPath = ofd.FileName;
                else
                    return;
            }
            Overall.UpdateText("Replacing texture...");
            OverallProg.ChangeProgressBar(0, 1);
            DebugOutput.PrintLn("Replacing data of texture: " + (isAnalysed ? tex.TexName : tex.FileName) + " with: " + replacingPath);

            await Task.Run(() =>
            {
                tex.FileName = Path.GetFileName(replacingPath);
                tex.FilePath = Path.GetDirectoryName(replacingPath);
                DebugOutput.PrintLn("Getting new details...");
                tex.Thumbnail = new KFreonLib.Helpers.LiquidEngine.MemoryTributary();
                tex.EnumerateDetails();

                /*try
                {
                    DebugOutput.PrintLn("Updating to new thumbnail");
                     MainTreeViewImageList.Images[tex.ThumbInd] = new Bitmap(tex.Thumbnail);
                }
                catch
                {
                    myTreeNode node = MainTreeView.SelectedNode as myTreeNode;
                    node.ImageIndex = 2;
                }*/
                LoadedTexes[index] = tex;
            });
            OverallProg.ChangeProgressBar(1, 1);
            Overall.UpdateText("Texture replaced!");
            RedrawTreeView();
        }

        private async void InstallSingleButton_Click(object sender, EventArgs e)
        {
            TPFTexInfo tex;
            int index = GetSelectedTex(out tex);
            bool res = await Task<bool>.Run(() => InstallTextures(new List<TPFTexInfo>() { tex }, true));
            if (!res)
            {
                Overall.UpdateText("Install Failed!");
                OverallProg.ChangeProgressBar(1, 1);
            }
        }

        private List<string> ComparePCCs(string file1, string file2)
        {
            List<string> diffs = new List<string>();
            diffs.Add(@"PCC1 = R:\Games\Origin Games\Mass Effect 3\BIOGame\DLC\DLC_EXP_Pack002\CookedPCConsole\BioA_Omg004_950_GenOffice.pcc");
            diffs.Add(@"PCC2 = R:\Games\Origin Games\Mass Effect 3 commit test\BIOGame\DLC\DLC_EXP_Pack002\CookedPCConsole\BioA_Omg004_950_GenOffice.pcc");
            KFreonLib.PCCObjects.IPCCObject pcc1 = KFreonLib.PCCObjects.Creation.CreatePCCObject(file1, 3);
            KFreonLib.PCCObjects.IPCCObject pcc2 = KFreonLib.PCCObjects.Creation.CreatePCCObject(file2, 3);
            


            if (pcc1.expDataBegOffset != pcc2.expDataBegOffset)
                diffs.Add("expDataBegOffset => PCC1: " + pcc1.expDataBegOffset + "   PCC2: " + pcc2.expDataBegOffset);

            if (pcc1.expDataEndOffset != pcc2.expDataEndOffset)
                diffs.Add("expDataEndOffset => PCC1: " + pcc1.expDataEndOffset + "   PCC2: " + pcc2.expDataEndOffset);

            if (pcc1.ExportCount != pcc2.ExportCount)
                diffs.Add("ExportCount => PCC1: " + pcc1.ExportCount + "   PCC2: " + pcc2.ExportCount);

            if (pcc1.ExportOffset != pcc2.ExportOffset)
                diffs.Add("ExportOffset => PCC1: " + pcc1.ExportOffset + "   PCC2: " + pcc2.ExportOffset);

            if (pcc1.flags != pcc2.flags)
                diffs.Add("Flags => PCC1: " + pcc1.flags + "   PCC2: " + pcc2.flags);
            
            if (pcc1.ImportCount != pcc2.ImportCount)
                diffs.Add("ImportCount => PCC1: " + pcc1.ImportCount + "   PCC2: " + pcc2.ImportCount);

            if (pcc1.ImportOffset != pcc2.ImportOffset)
                diffs.Add("ImportOffset => PCC2: " + pcc1.ImportOffset + "   PCC2: " + pcc2.ImportOffset);

            if (pcc1.NameCount != pcc2.NameCount)
                diffs.Add("NameCount => PCC1: " + pcc1.NameCount + "   PCC2: " + pcc2.NameCount);
 
            if (pcc1.NameOffset != pcc2.NameOffset)
                diffs.Add("NameOffset => PCC1: " + pcc1.NameOffset + "   PCC2: " + pcc2.NameOffset);

            if (pcc1.nameSize != pcc2.nameSize)
                diffs.Add("NameSize => PCC1: " + pcc1.nameSize + "   PCC2: " + pcc2.nameSize);

            if (pcc1.NumChunks != pcc2.NumChunks)
                diffs.Add("NumChuncks => PCC1: " + pcc1.NumChunks + "   PCC2: " + pcc2.NumChunks);


            for (int i = 0; i < pcc1.Imports.Count; i++)
            {
                KFreonLib.PCCObjects.ME3ImportEntry entry1 = (KFreonLib.PCCObjects.ME3ImportEntry) pcc1.Imports[i];
                KFreonLib.PCCObjects.ME3ImportEntry entry2 = (KFreonLib.PCCObjects.ME3ImportEntry) pcc2.Imports[i];

                if (entry1.ClassName != entry2.ClassName)
                    diffs.Add("Imports - ClassName => PCC1: " + entry1.ClassName + "   PCC2: " + entry2.ClassName);

                if (!entry1.data.SequenceEqual(entry2.data))
                    diffs.Add("Imports - data => Not equal");

                if (entry1.idxLink != entry2.idxLink)
                    diffs.Add("Imports - idxLink => PCC1: " + entry1.idxLink + "   PCC2: " + entry2.idxLink);

                if (entry1.idxObjectName != entry2.idxObjectName)
                    diffs.Add("Imports - idxObjectName => PCC1: " + entry1.idxObjectName + "   PCC2: " + entry2.idxObjectName);

                if (entry1.link != entry2.link)
                    diffs.Add("Imports - link => PCC1: " + entry1.link + "   PCC2: " + entry2.link);

                if (entry1.ObjectFlags != entry2.ObjectFlags)
                    diffs.Add("Imports - ObjectFlags => PCC1: " + entry1.ObjectFlags + "   PCC2: " + entry2.ObjectFlags);

                if (entry1.ObjectName != entry2.ObjectName)
                    diffs.Add("Imports - ObjectName => PCC1: " + entry1.ObjectName + "   PCC2: " + entry2.ObjectName);

                if (entry1.PackageFullName != entry2.PackageFullName)
                    diffs.Add("Imports - PackageFullName => PCC1: " + entry1.PackageFullName + "   PCC2: " + entry2.PackageFullName);

                if (entry1.PackageFile != entry2.PackageFile)
                    diffs.Add("Imports - PackageFile => PCC1: " + entry1.PackageFile + "   PCC2: " + entry2.PackageFile);

                if (entry1.PackageName != entry2.PackageName)
                    diffs.Add("Imports - PackageName => PCC1: " + entry1.PackageName + "   PCC2: " + entry2.PackageName);

            }

            if (!pcc1.header.SequenceEqual(pcc2.header))
                diffs.Add("Header => Not equal");

            for (int i = 0; i < pcc1.Exports.Count; i++)
            {
                KFreonLib.PCCObjects.ME3ExportEntry entry1 = (KFreonLib.PCCObjects.ME3ExportEntry)pcc1.Exports[i];
                KFreonLib.PCCObjects.ME3ExportEntry entry2 = (KFreonLib.PCCObjects.ME3ExportEntry)pcc2.Exports[i];

                try
                {
                    if (entry1.ArchtypeName != entry2.ArchtypeName)
                        diffs.Add("Exports - ArchtypeName => PCC1: " + entry1.ArchtypeName + "   PCC2: " + entry2.ArchtypeName);
                }
                catch{}

                try
                {
                    if (entry1.ClassName != entry2.ClassName)
                        diffs.Add("Exports - ClassName => PCC1: " + entry1.ClassName + "   PCC2: " + entry2.ClassName);
                }
                catch { }

                try
                {
                    if (entry1.ClassNameID != entry2.ClassNameID)
                        diffs.Add("Exports - ClassNameID => PCC1: " + entry1.ClassNameID + "   PCC2: " + entry2.ClassNameID);
                }
                catch { }

                try
                {
                    if (entry1.ClassParent != entry2.ClassParent)
                        diffs.Add("Exports - ClassParent => PCC1: " + entry1.ClassParent + "   PCC2: " + entry2.ClassParent);
                }
                catch { }

                try
                {
                    if (!entry1.Data.SequenceEqual(entry2.Data))
                        diffs.Add("Exports - Data => Not equal  " + entry1.ClassName + "  " + entry1.ObjectName);
                }
                catch { }

                try
                {
                    if (entry1.DataOffset != entry2.DataOffset)
                        diffs.Add("Exports - DataOffset => PCC1: " + entry1.DataOffset + "   PCC2: " + entry2.DataOffset);
                }
                catch { }

                try
                {
                    if (entry1.DataOffsetTmp != entry2.DataOffsetTmp)
                        diffs.Add("Exports - DataOffsetTmp => PCC1: " + entry1.DataOffsetTmp + "   PCC2: " + entry2.DataOffsetTmp);
                }
                catch { }

                try
                {
                    if (entry1.DataSize != entry2.DataSize)
                        diffs.Add("Exports - DataSize => PCC1: " + entry1.DataSize + "   PCC2: " + entry2.DataSize);
                }
                catch { }

                try
                {
                    if (entry1.flagint != entry2.flagint)
                        diffs.Add("Exports - flagint => PCC1: " + entry1.flagint + "   PCC2: " + entry2.flagint);
                }
                catch { }

                try
                {
                    if (entry1.hasChanged != entry2.hasChanged)
                        diffs.Add("Exports - hasChanged => PCC1: " + entry1.hasChanged + "   PCC2: " + entry2.hasChanged);
                }
                catch { }

                try
                {
                    if (entry1.idxArchtypeName != entry2.idxArchtypeName)
                        diffs.Add("Exports - idxArchtypeName => PCC1: " + entry1.idxArchtypeName + "   PCC2: " + entry2.idxArchtypeName);
                }
                catch { }

                try
                {
                    if (entry1.idxClassName != entry2.idxClassName)
                        diffs.Add("Exports - idxClassName => PCC1: " + entry1.idxClassName + "   PCC2: " + entry2.idxClassName);
                }
                catch { }

                try
                {
                    if (entry1.idxClassParent != entry2.idxClassParent)
                        diffs.Add("Exports - idxClassParent => PCC1: " + entry1.idxClassParent + "   PCC2: " + entry2.idxClassParent);
                }
                catch { }

                try
                {
                    if (entry1.idxLink != entry2.idxLink)
                        diffs.Add("Exports - idxLink => PCC1: " + entry1.idxLink + "   PCC2: " + entry2.idxLink);
                }
                catch { }

                try
                {
                    if (entry1.idxObjectName != entry2.idxObjectName)
                        diffs.Add("Exports - idxObjectName => PCC1: " + entry1.idxObjectName + "   PCC2: " + entry2.idxObjectName);
                }
                catch { }

                try
                {
                    if (entry1.idxPackageName != entry2.idxPackageName)
                        diffs.Add("Exports - idxPackageName => PCC1: " + entry1.idxPackageName + "   PCC2: " + entry2.idxPackageName);
                }
                catch { }

                try
                {
                    if (entry1.indexValue != entry2.indexValue)
                        diffs.Add("Exports - indexValue => PCC1: " + entry1.indexValue + "   PCC2: " + entry2.indexValue);
                }
                catch { }

                try
                {
                    if (!entry1.info.SequenceEqual(entry2.info))
                        diffs.Add("Exports - info => not equal");
                }
                catch { }

                try
                {
                    if (entry1.InfoOffset != entry2.InfoOffset)
                        diffs.Add("Exports - InfoOffset => PCC1: " + entry1.InfoOffset + "   PCC2: " + entry2.InfoOffset);
                }
                catch { }

                try
                {
                    if (entry1.Link != entry2.Link)
                        diffs.Add("Exports - Link => PCC1: " + entry1.Link + "   PCC2: " + entry2.Link);
                }
                catch { }

                try
                {
                    if (entry1.ObjectFlags != entry2.ObjectFlags)
                        diffs.Add("Exports - ObjectFlags => PCC1: " + entry1.ObjectFlags + "   PCC2: " + entry2.ObjectFlags);
                }
                catch { }

                try
                {
                    if (entry1.ObjectName != entry2.ObjectName)
                        diffs.Add("Exports - ObjectName => PCC1: " + entry1.ObjectName + "   PCC2: " + entry2.ObjectName);
                }
                catch { }

                try
                {
                    if (entry1.offset != entry2.offset)
                        diffs.Add("Exports - offset => PCC1: " + entry1.offset + "   PCC2: " + entry2.offset);
                }
                catch { }

                try
                {
                    if (entry1.Package != entry2.Package)
                        diffs.Add("Exports - Package => PCC1: " + entry1.Package + "   PCC2: " + entry2.Package);
                }
                catch { }

                try
                {
                    if (entry1.PackageFullName != entry2.PackageFullName)
                        diffs.Add("Exports - PackageFullName => PCC1: " + entry1.PackageFullName + "   PCC2: " + entry2.PackageFullName);
                }
                catch { }

                try
                {

                    if (entry1.PackageName != entry2.PackageName)
                        diffs.Add("Exports - PackageName => PCC1: " + entry1.PackageName + "   PCC2: " + entry2.PackageName);
                }
                catch { }
            }
            //pcc1.Names;
            //pcc1.listsStream;

            KFreonLib.Textures.ME3SaltTexture2D tex2D1 = (KFreonLib.Textures.ME3SaltTexture2D)KFreonLib.Textures.Creation.CreateTexture2D(file1, 1376, 3, pathBIOGame);
            KFreonLib.Textures.ME3SaltTexture2D tex2D2 = (KFreonLib.Textures.ME3SaltTexture2D)KFreonLib.Textures.Creation.CreateTexture2D(file2, 1376, 3, pathBIOGame);

            try
            {
                if (tex2D1.allFiles.SequenceEqual(tex2D2.allFiles))
                    diffs.Add("Tex2D - allFiles => Not equal");
            }
            catch { }

            try
            {
                List<string> dif = new List<string>();
                for (int i = 0; i < tex2D2.allPccs.Count; i++) 
                    if (tex2D1.allPccs[i] != tex2D2.allPccs[i])
                        diffs.Add("Tex2D - allPCCs => PCC1: " + tex2D1.allPccs[i] + "   PCC2: " + tex2D2.allPccs[i]);
            }
            catch { }

            try
            {
                if (tex2D1.arcName != tex2D2.arcName)
                    diffs.Add("Tex2D - arcName => PCC1: " + tex2D1.arcName + "   PCC2: " + tex2D2.arcName);
            }
            catch { }

            try
            {
                if (tex2D1.Class != tex2D2.Class)
                    diffs.Add("Tex2D - arcName => PCC1: " + tex2D1.arcName + "   PCC2: " + tex2D2.arcName);
            }
            catch { }

            try
            {
                if (tex2D1.expIDs.SequenceEqual(tex2D2.expIDs))
                    diffs.Add("Tex2D - expIDs => Not equal");
            }
            catch { }

            try
            {
                if (tex2D1.exportOffset != tex2D2.exportOffset)
                    diffs.Add("Tex2D - exportOffset => PCC1: " + tex2D1.exportOffset + "   PCC2: " + tex2D2.exportOffset);
            }
            catch { }

            try
            {
                if (tex2D1.FullArcPath != tex2D2.FullArcPath)
                    diffs.Add("Tex2D - FullArcPath => PCC1: " + tex2D1.FullArcPath + "   PCC2: " + tex2D2.FullArcPath);
            }
            catch { }

            try
            {
                if (tex2D1.Hash != tex2D2.Hash)
                    diffs.Add("Tex2D - Hash => PCC1: " + tex2D1.Hash + "   PCC2: " + tex2D2.Hash);
            }
            catch { }

            try
            {
                if (!tex2D1.headerData.SequenceEqual(tex2D2.headerData))
                    diffs.Add("Tex2D - HeaderData => not equal");
            }
            catch { }

            try
            {
                if (tex2D1.imageData.SequenceEqual(tex2D2.imageData))
                    diffs.Add("Tex2D - ImageData => Not equal");
            }
            catch { }

            //tex2D1.imgList;

            try
            {
                if (tex2D1.LODGroup != tex2D2.LODGroup)
                    diffs.Add("Tex2D - LODGroup => PCC1: " + tex2D1.LODGroup + "   PCC2: " + tex2D2.LODGroup);
            }
            catch { }
            try
            {
                if (tex2D1.Mips != tex2D2.Mips)
                    diffs.Add("Tex2D - Mips => PCC1: " + tex2D1.Mips + "   PCC2: " + tex2D2.Mips);
            }
            catch { }

            try
            {
                if (tex2D1.pccExpIdx != tex2D2.pccExpIdx)
                    diffs.Add("Tex2D - pccExpIdx => PCC1: " + tex2D1.pccExpIdx + "   PCC2: " + tex2D2.pccExpIdx);
            }
            catch { }

            try
            {
                if (tex2D1.pccOffset != tex2D2.pccOffset)
                    diffs.Add("Tex2D - pccOffset => PCC1: " + tex2D1.pccOffset + "   PCC2: " + tex2D2.pccOffset);
            }
            catch { }

            //tex2D1.properties;
            try
            {
                if (tex2D1.texFormat != tex2D2.texFormat)
                    diffs.Add("Tex2D - texFormat => PCC1: " + tex2D1.texFormat + "   PCC2: " + tex2D2.texFormat);
            }
            catch { }
            try
            {
                if (tex2D1.texName != tex2D2.texName)
                    diffs.Add("Tex2D - texName => PCC1: " + tex2D1.texName + "   PCC2: " + tex2D2.texName);
            }
            catch { }
            

            return diffs;
        }

        private void MainListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                // KFreon: Get selected items in a better format
                List<int> inds = new List<int>();
                for (int i = 0; i < MainListView.SelectedIndices.Count; i++)
                    inds.Add(MainListView.SelectedIndices[i]);

                // KFreon: Get list in descending order (to avoid having to recalculate indicies)
                inds.Sort();
                inds.Reverse();

                // KFreon: Delete items from both lists
                inds.ForEach(index =>
                {
                    DeleteEntry(index);
                    MainListView.Items.RemoveAt(index);
                });

                DrawListView();
            }
        }

        private void MainTabPages_TabChanged(object sender, TabControlEventArgs e)
        {
            if (e.TabPage.Text == "Delete Page")
            {
                // KFreon: Populate items
                DrawListView();
            }
        }

        private void DrawListView()
        {
            MainListView.Items.Clear();
            for (int i = 0; i < LoadedTexes.Count; i++)
            {
                TPFTexInfo tex = LoadedTexes[i];
                MainListView.Items.Add(String.IsNullOrEmpty(tex.TexName) ? tex.FileName : tex.TexName, tex.ThumbInd);
            }
        }

        private void CopyClipBoardButton_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            if (PCCsCheckListBox.SelectedItems.Count <= 0)
                return;

            for (int i = 0; i < PCCsCheckListBox.SelectedItems.Count; i++)
            {
                string str = PCCsCheckListBox.SelectedItems[i].Text;
                sb.AppendLine(str);
            }

            Clipboard.SetText(sb.ToString());
        }
    }
}
