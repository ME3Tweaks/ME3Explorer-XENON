﻿using AmaroK86.ImageFormat;
using KFreonLib;
using ME3Explorer.Unreal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Textures = KFreonLib.Textures;
using PCCObjects = KFreonLib.PCCObjects;
using Helpers = KFreonLib.Helpers;
using KFreonLib.GUI;
using KFreonLib.Debugging;
using KFreonLib.MEDirectories;
using TreeTexInfo = KFreonLib.Textures.TreeTexInfo;

namespace ME3Explorer
{
    public partial class Texplorer2 : Form
    {
        BackBone backbone;
        int WhichGame = 0;
        bool noWindow = false;
        myTreeNode t;
        static readonly object TreeLock = new object();
        MEDirectories MEExDirecs = new MEDirectories();

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

        string DLCPath
        {
            get
            {
                return MEExDirecs.DLCPath;
            }
        }

        string ThumbnailPath
        {
            get
            {
                return MEExDirecs.ThumbnailPath;
            }
        }
        string WindowTitle;
        public TreeDB Tree;
        int NumThreads = 4;
        int PropertiesWidth = 0;
        KFreonSearchForm Search;
        CancellationTokenSource cts = new CancellationTokenSource();
        List<int> ChangedTextures = new List<int>();
        bool ModMakerMode = false;
        string tocpath = "";
        bool DisableContext = false;
        bool TPFMode = false;
        TextUpdater StatusUpdater;
        ProgressBarChanger ProgBarUpdater;
        Gooey gooey;
        bool cancelling = true;

        

        public Texplorer2(bool nowindow = false, int which = -1)
        {
            if (!nowindow)
                InitializeComponent();
            UpgradeSettings();

            // KFreon: Set number of threads if necessary
            if (Properties.Settings.Default.NumThreads == 0)
            {
                Properties.Settings.Default.NumThreads = KFreonLib.Misc.Methods.SetNumThreads(false);
                Properties.Settings.Default.Save();
            }

            WhichGame = (which == -1) ? Properties.Settings.Default.TexplorerGameVersion : which;
            MEExDirecs.WhichGame = WhichGame;

            StatusUpdater = new TextUpdater(StatusLabel, StatusStrip);
            ProgBarUpdater = new ProgressBarChanger(StatusStrip, MainProgressBar);
            gooey = new Gooey(MainTreeView);
            SetupGUI();

            DebugOutput.StartDebugger("Texplorer 2.0");
            if (!nowindow)
            {
                ChangeButton.Text = "Modding ME" + WhichGame;
                WindowTitle = "Texplorer 2.0:  " + "ME" + WhichGame;
                this.Text = WindowTitle;
                DisappearPictureBox();
                ResetSearchBox();
            }
            backbone = new BackBone(() =>
            {
                Console.WriteLine("changing gui to false");
                gooey.ChangeState(false);
                return true;
            }, () =>
            {
                Console.WriteLine("Changeing gui to true");
                gooey.ChangeState(true);
                return true;
            });

            noWindow = nowindow;


            // KFreon: Get number of threads to use
            NumThreads = KFreonLib.Misc.Methods.SetNumThreads(false);

            // KFreon: Setup paths
            // KFreon: Check game states DONE HERE COS TREE NEEDS THESE THINGS
            CheckGameStates();
            

            // KFreon: Check path exists
            bool exists = true;

            // KFreon: Load tree only if necessary
            if (!nowindow && exists)
                backbone.AddToBackBone(b =>
                {
                    BeginLoadingTree();
                    return true;
                });
        }

        private void CheckGameStates()
        {
            List<string> messages = null;
            List<bool> states = KFreonLib.Misc.Methods.CheckGameState(MEExDirecs, true, out messages);

            for (int i = 0; i < states.Count; i++)
            {
                // KFreon: Actually set visual cues
                if (!noWindow)
                    ChangeGameIndicators(i + 1, states[i]);

                OutputBoxPrintLn(messages[i]);
            }
        }

        private void SetupGUI()
        {
            gooey.AddControl(CancelButton, "CancelB", new Action<bool>(state =>
            {
                CancelButton.Visible = !state;
                if (state)
                {
                    CancelButton.Text = "Cancel";
                    cts = new CancellationTokenSource();
                }
            }));

            gooey.AddControl(saveChangesToolStripMenuItem, "saveChanges", true);
            gooey.AddControl(treeIOToolStripMenuItem, "treeIO", true);
            gooey.AddControl(changePathsToolStripMenuItem, "changePaths", true);
            gooey.AddControl(searchToolStripMenuItem, "searchToolStrip", true);
            gooey.AddControl(startModMakerModeToolStripMenuItem, "ModMakerMode", true);
            gooey.AddControl(rebuildDatabaseToolStripMenuItem, "rebuild", true);
            gooey.AddControl(updateTOCsToolStripMenuItem, "updateTOCs", true);
            gooey.AddControl(SearchBox, "SearchBox", true);
            gooey.AddControl(ChangeButton, "ChangeButton", true);
            gooey.AddControl(regenerateThumbnailsToolStripMenuItem, "regenerate", true);
            gooey.AddControl(startTPFModeToolStripMenuItem, "TPFMode", true);
        }

        private void UpgradeSettings()
        {
            Properties.Settings.Default.Upgrade();
        }

        private bool SaveFile(List<string> Filenames, List<int> ExpIDs, Textures.ITexture2D tex2D, int j)
        {
            if (cts.IsCancellationRequested)
                return false;

            PCCObjects.IPCCObject PCC = null;
            string currentPCC = Filenames[j];

            // KFreon: Fix pathing
            string temppath = WhichGame == 1 ? Path.GetDirectoryName(pathBIOGame) : pathBIOGame;
            if (!currentPCC.Contains(temppath))
                currentPCC = Path.Combine(temppath, currentPCC);

            DebugOutput.PrintLn("Now saving pcc: " + currentPCC + "...");
            PCC = PCCObjects.Creation.CreatePCCObject(currentPCC, WhichGame);

            if (String.Compare(tex2D.texName, PCC.Exports[ExpIDs[j]].ObjectName, true) != 0 || (PCC.Exports[ExpIDs[j]].ClassName != "Texture2D" && PCC.Exports[ExpIDs[j]].ClassName != "LightMapTexture2D" && PCC.Exports[ExpIDs[j]].ClassName != "TextureFlipBook"))
                throw new InvalidDataException("Export object has wrong class or name");

            Textures.ITexture2D temptex2D = PCC.CreateTexture2D(ExpIDs[j], pathBIOGame);
            temptex2D.CopyImgList(tex2D, PCC);
            PCC.Exports[ExpIDs[j]].SetData(temptex2D.ToArray(PCC.Exports[ExpIDs[j]].DataOffset, PCC));
            PCC.saveToFile(currentPCC);
            PCC = null;
            return true;
        }

        private void saveChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ChangedTextures.Count == 0)
            {
                MessageBox.Show("You haven't made any changes yet.", "Emergency Induction Port...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DisableContext = true;
            ProgBarUpdater.ChangeProgressBar(0, ChangedTextures.Count);
            OutputBoxPrintLn("Saving " + ChangedTextures.Count + " Textures...");

            // KFreon: Run save function on backbone
            backbone.AddToBackBone(b =>
            {
                for (int i = 0; i < ChangedTextures.Count; i++)
                {
                    // KFreon: Check cancellation
                    if (cts.IsCancellationRequested)
                    {
                        StatusUpdater.UpdateText("Saving Cancelled!");
                        OutputBoxPrintLn("Saving Cancelled!");
                        return false;  // KFreon: return doesn't matter right now
                    }

                    // KFreon: Setup objects
                    TreeTexInfo tex = Tree.GetTex(ChangedTextures[i]);
                    StatusUpdater.UpdateText("Saving " + tex.TexName + " |  " + MainProgressBar.Value + 1 + " \\ " + MainProgressBar.Maximum);
                    PCCObjects.IPCCObject pcc = PCCObjects.Creation.CreatePCCObject(tex.Files[0], WhichGame);
                    Textures.ITexture2D tex2D = tex.Textures[0];

                    // KFreon: Save first file
                    PCCObjects.IExportEntry expEntry = pcc.Exports[tex2D.pccExpIdx];
                    expEntry.SetData(tex2D.ToArray(expEntry.DataOffset, pcc));
                    pcc.saveToFile(pcc.pccFileName);

                    this.Invoke(new Action(() => MainProgressBar.Increment(1)));
                    OutputBoxPrintLn("Initial saving complete for " + tex.TexName + ". Now saving remaining PCC's for this texture...");

                    // KFreon: Save rest of files
                    for (int j = 1; j < tex.Files.Count; j++)
                    {
                        if (!SaveFile(tex.Files, tex.ExpIDs, tex2D, j))
                            return false;
                    }
                }

                StatusUpdater.UpdateText("All textures saved!");
                OutputBoxPrintLn("All textures saved!");
                ChangedTextures.Clear();
                return true;
            });
        }

        private void ImportTree()
        {
            if (Tree == null)
            {
                MessageBox.Show("Game files not found. Unfortunately we seem to need them to use trees...");
                return;
            }
            else if (Tree.TexCount != 0)
                if (MessageBox.Show("This will replace your currently loaded tree but NOT on disk. Do you wish to proceed? Make REALLY sure you're loading the right game tree in...", "Sure you wanna do that Commander?", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                    return;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "ME Trees|*.bin";
                ofd.Title = "Select tree to import.";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TreeDB temptree = Tree.Clone();
                    int status;
                    if (!LoadTreeFromFile(ofd.FileName, out status, false))
                    {
                        Tree = temptree;
                        MessageBox.Show("Error occured while loading tree. Likely a corrupted or invalid tree.");
                        return;
                    }
                }
            }
        }

        private void ExportTree()
        {
            if (Tree == null || Tree.TexCount == 0)
            {
                MessageBox.Show("No tree loaded to export!");
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Select export location for your tree.";
                sfd.Filter = "ME Trees|*.bin";
                sfd.FileName = "ME" + WhichGame + "tree.bin";
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    StatusUpdater.UpdateText("Exporting Tree...");
                    ProgBarUpdater.ChangeProgressBar(0, 1);
                    Task.Run(() =>
                    {
                        Tree.WriteToFile(sfd.FileName, Path.GetDirectoryName(pathBIOGame));
                        DebugOutput.PrintLn("Tree exported successfully!");
                        StatusUpdater.UpdateText("Tree Exported!");
                        ProgBarUpdater.ChangeProgressBar(1, 1);
                    });
                }
            }
        }

        private void instructionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CancelButton.Visible)
                return;

            if (instructionsToolStripMenuItem.Text == "Instructions")
            {
                // KFreon: Show tooltips
                ShowContextPanel(true);
                System.Threading.Thread.Sleep(300);
                SetupToolTip(MainTreeView);
                SetupToolTip(MainListView);
                SetupToolTip(DetailsHideButton);
                SetupToolTip(ContextPanel);
                SetupToolTip(tabControl1);
                SetupToolTip(ChangeButton);
                SetupToolTip(OutputBox);

                instructionsToolStripMenuItem.Text = "Further instructions online";
            }
            else
            {
                Process.Start("http://me3explorer.freeforums.org/tutorial-texplorer-2-0-t1426.html");
                instructionsToolStripMenuItem.Text = "Instructions";
            }


        }

        public void SetupToolTip(Control control)
        {
            ToolTip newtip = new ToolTip();
            //newtip.IsBalloon = true;
            newtip.Show(PrimaryToolTip.GetToolTip(control), control, 10, 10, 100000);
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Search.Show();
        }

        private void startModMakerModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!ModMakerMode && MessageBox.Show("This mode will send all modifications to ModMaker. Changes are made to your files in this mode. Proceed?", "Pick up your hammer", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
            {
                ModMakerMode = true;
                startModMakerModeToolStripMenuItem.Text = "Stop Modmaker Mode";
            }
            else if (ModMakerMode)
            {
                ModMakerMode = false;
                startModMakerModeToolStripMenuItem.Text = "Start Modmaker Mode";
            }
        }

        private void rebuildDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will delete your current tree and re-scan your gamefiles. Are you sure?", "Reconstruct facial profile?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
            {
                // KFreon: Check game state first in case of update
                CheckGameStates();

                // KFreon: Remove tree
                if (File.Exists(ExecFolder + "me" + WhichGame + "tree.bin"))
                    File.Delete(ExecFolder + "me" + WhichGame + "tree.bin");
                OutputBoxPrintLn(Environment.NewLine + "Rebuilding ME" + WhichGame + " tree...");

                // KFreon: Clear everything and rebuild tree
                ClearDisplays();
                Tree = null;
                BeginLoadingTree(true, true);
            }
        }

        private void updateTOCsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (WhichGame == 3)
            {
                List<string> dlcs = Directory.EnumerateFiles(DLCPath, "*.sfar", SearchOption.AllDirectories) as List<string>;
                using (Helpers.SelectionForm sf = new Helpers.SelectionForm(dlcs, "Select DLC's to update.", "DLC TOC Update Selector", true))
                {
                    sf.ShowDialog();
                    if (sf.SelectedInds.Count == 0)
                        return;
                    else
                    {
                        dlcs.Clear();
                        foreach (string item in sf.SelectedItems)
                            dlcs.Add(item);
                        Task.Run(() => UpdateTOCs(pathBIOGame, WhichGame, DLCPath, dlcs));
                    }
                }
            }
            else
                Task.Run(() => UpdateTOCs(pathBIOGame, WhichGame, DLCPath));
        }

        private async void ChangeButton_Click(object sender, EventArgs e)
        {
            OutputBoxPrintLn(Environment.NewLine);
            switch (WhichGame)
            {
                case 1:
                    WhichGame = 2;
                    ChangeButton.Text = "Modding ME2";
                    break;
                case 2:
                    WhichGame = 3;
                    ChangeButton.Text = "Modding ME3";
                    break;
                case 3:
                    WhichGame = 1;
                    ChangeButton.Text = "Modding ME1";
                    break;
            }

            MEExDirecs.WhichGame = WhichGame;
            DebugOutput.PrintLn();
            DebugOutput.PrintLn("Changing Trees...");
            WindowTitle = "Texplorer 2.0:  " + "ME" + WhichGame;
            this.Text = WindowTitle;
            Properties.Settings.Default.TexplorerGameVersion = WhichGame;
            Properties.Settings.Default.Save();
            ClearDisplays();
            Tree = null;

            if (!noWindow)
            {
                NoRenderButton.Visible = (WhichGame == 1);
                LowResButton.Visible = (WhichGame == 1);
            }

            // KFreon: Setup pathing
            await Task.Run(() => MEExDirecs.SetupPathing(true));
            BeginLoadingTree();
        }

        private void ClearDisplays()
        {
            if (MainListView.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    Tree.Clear();
                    MainListView.Items.Clear();
                    PCCsCheckedListBox.Items.Clear();
                    PropertiesRTB.Clear();
                }));
            }
            else
            {
                if (Tree != null)
                    Tree.Clear();
                MainListView.Items.Clear();
                PCCsCheckedListBox.Items.Clear();
                PropertiesRTB.Clear();
            }
        }

        private void ChangeTreeIndicators(int game, bool state)
        {
            if (MainTreeView.InvokeRequired)
                MainTreeView.Invoke(new Action(() => ChangeTreeIndicators(game, state)));
            else
            {
                Color color = state ? Color.LightGreen : Color.Red;
                switch (game)
                {
                    case 1:
                        Tree1Label.ForeColor = color;
                        break;
                    case 2:
                        Tree2Label.ForeColor = color;
                        break;
                    case 3:
                        Tree3Label.ForeColor = color;
                        break;
                }
            }
        }

        private void ChangeGameIndicators(int game, bool state)
        {
            if (MainTreeView.InvokeRequired)
                MainTreeView.Invoke(new Action(() => ChangeGameIndicators(game, state)));
            else
            {
                Color color = state ? Color.LightGreen : Color.Red;
                switch (game)
                {
                    case 1:
                        Game1Label.ForeColor = color;
                        break;
                    case 2:
                        Game2Label.ForeColor = color;
                        break;
                    case 3:
                        Game3Label.ForeColor = color;
                        break;
                }
            }
        }

        private void BeginLoadingTree(bool NoUser = false, bool ForceRebuild = false)
        {
            // KFreon: Move to backbone if necessary
            if (!MainListView.InvokeRequired)
            {
                backbone.AddToBackBone(b =>
                {
                    BeginLoadingTree(NoUser, ForceRebuild);
                    return true;
                });
                return;
            }

            // KFreon: Wait for GUI controls to be created
            while (!ChangeButton.Parent.Created)
                System.Threading.Thread.Sleep(100);


            this.Invoke(new Action(() =>
            {
                // KFreon: Close search list
                TabSearchSplitter.SplitterDistance = 0;

                // KFreon: Close context panel
                ContextPanel.Height = 0;
            }));

            // KFreon: Loop over all trees
            for (int i = 1; i < 4; i++)
            {
                // KFreon: Partly check tree we want
                if (i == WhichGame)
                {
                    if (!Texplorer2.SetupTree(ref Tree, pathCooked, WhichGame, MainTreeView, pathBIOGame))
                        continue;
                }
                else
                {
                    // KFreon: Start tasks for trees we don't care about. Could keep these, but won't so trees can be loaded dynamically.
                    var y = i;
                    Task.Run(() =>
                    {
                        TreeDB temptree = null;
                        MEExDirecs.SetupPathing(false);
                        string tempbio = MEExDirecs.GetDifferentPathBIOGame(y);
                        bool res = Texplorer2.SetupTree(ref temptree, MEExDirecs.GetDifferentPathCooked(y), y, null, tempbio);
                        bool temp = false;
                        if (res)
                        {
                            int status2;
                            temp = temptree.ReadFromFile(ExecFolder + "me" + y + "tree.bin", Path.GetDirectoryName(tempbio), ExecFolder + "ThumbnailCaches\\ME" + y + "ThumbnailCache\\", out status2);
                        }

                        DebugOutput.PrintLn(temp ? "Found ME" + y + " tree." : "ME" + y + " tree not found.");
                        ChangeTreeIndicators(y, temp);
                        return temp;
                    });
                }
            }

            bool dostuff = ForceRebuild;
            bool treefound = false;

            if (Tree == null)
                MessageBox.Show("ME" + WhichGame + " not found!", "Forgetting something?", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else if (!dostuff && Tree != null)
            {
                int status = 0;
                if (!LoadTreeFromFile(ExecFolder + "me" + WhichGame + "tree.bin", out status, true))
                {
                    // KFreon: Load failed, but user said rebuild
                    if (status == 1 || (!NoUser && MessageBox.Show("No ME" + WhichGame + " tree found. Do you want to build one?", "Damn Kai Leng...", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes))
                        dostuff = true;
                    else if (status == 2)   // KFreon: User cancelled rebuild
                    {
                        OutputBoxPrintLn("Tree corrupt. Load cancelled!");
                        ChangeButton.Invoke(new Action(() => ChangeButton.Enabled = true));
                        return;
                    }
                }
                else
                    treefound = true;
            }

            // KFreon: Don't want else if here cos do stuff can change inside the !dostuff statement. Also this performs the ManageDLC stuff if tree not found
            if (Tree == null)
            {
                // KFreon: Placeholder
                OutputBoxPrintLn("ME" + WhichGame + " game files not found. Tree not loaded.");
                StatusUpdater.UpdateText("ME" + WhichGame + " not found.");
            }
            else if (!treefound)
            {
                if (dostuff)
                    treefound = FirstTimeSetup();
                else
                {
                    OutputBoxPrintLn("Tree not found, and tree scan cancelled!");
                    StatusUpdater.UpdateText("Ready.");
                }
            }
            else
                treefound = true;

            ChangeTreeIndicators(WhichGame, treefound && Tree != null);

            gooey.ModifyControl("ModMakerMode", treefound);
            gooey.ModifyControl("updateTOCs", treefound);
            gooey.ModifyControl("SearchBox", treefound);
            gooey.ModifyControl("regenerate", treefound);
            gooey.ModifyControl("TPFMode", treefound);
            gooey.ModifyControl("saveChanges", treefound);
            gooey.ModifyControl("searchToolStrip", treefound);

            if (treefound)
            {
                if (!cts.IsCancellationRequested)
                    SetupSearch();
                else
                {
                    StatusUpdater.UpdateText("Operation cancelled!");
                    OutputBoxPrintLn("Operation cancelled!");
                }
            }
            Console.WriteLine("Finished function");
        }


        private bool LoadTreeFromFile(string filename, out int status, bool AllowQuestions)
        {
            status = 0;
            OutputBoxPrintLn("Trying to load tree from:  " + filename);
            StatusUpdater.UpdateText("Trying to load tree...");

            if (!Tree.ReadFromFile(filename, Path.GetDirectoryName(pathBIOGame), ThumbnailPath, out status, AllowQuestions ? this : null))
                return false;

            OutputBoxPrintLn("Tree loaded.");
            ConstructTree();
            OutputBoxPrintLn("Tree constructed. Ready.");
            StatusUpdater.UpdateText("Ready.");
            return true;
        }


        private bool DateCheckFileList(List<string> files, bool Basegame, long year = 2013, int month = 6)
        {
            bool modified = false;
            string print = "";
            foreach (string file in files)
            {
                DateTime dt = File.GetLastWriteTime(file);
                if (Basegame)
                {
                    if (dt.Year < year)
                        continue;
                    else if (dt.Year == year && dt.Month < month)
                        continue;
                }
                else
                {
                    // KFreon: Year here is actually ticks
                    if (year > 3000 && dt.Ticks <= year)
                        continue;
                    else if (year < 3000 && dt.Year <= year)
                        continue;
                }

                string message = "Modified File:  " + file + "  - Modified at: " + dt.ToLongTimeString() + ' ' + dt.ToLongDateString();
                print += message + Environment.NewLine;
                modified = true;
            }
            DebugOutput.PrintLn(print);
            return modified;
        }


        private void BeginTreeScan()
        {
            // KFreon: Create/clear thumbnail folder
            if (!Directory.Exists(ThumbnailPath))
            {
                Directory.CreateDirectory(ThumbnailPath);
                DebugOutput.PrintLn("Thumbnail cache created at: " + ThumbnailPath);
            }
            else
            {
                DebugOutput.PrintLn("Attempting to delete old thumbnails...");
                StatusUpdater.UpdateText("Attempting to delete old thumbnails...");
                try
                {
                    foreach (string file in Directory.GetFiles(ThumbnailPath))
                        File.Delete(file);
                    DebugOutput.PrintLn("Thumbnails successfully cleared.");
                }
                catch (Exception e)
                {
                    DebugOutput.PrintLn("ERROR removing thumbnails: " + e.Message);
                }
            }

            double StarTime = Environment.TickCount;
            ScanPCCList(true);

            if (cts.IsCancellationRequested)
            {
                OutputBoxPrintLn("Scan Cancelled!");
                StatusUpdater.UpdateText("Scan cancelled!");
            }
            else
            {
                //StatusUpdater.UpdateText("Ensuring file order is correct. This will take time...");
                Tree.WriteToFile(ExecFolder + "me" + WhichGame + "tree.bin", Path.GetDirectoryName(pathBIOGame));
                Console.WriteLine("TEXCOUNT: " + Tree.TexCount);
                this.Invoke(new Action(() =>
                {
                    ConstructTree();
                    MainProgressBar.Value = MainProgressBar.Maximum;
                }));
                StatusUpdater.UpdateText("Ready.  Treescan time: " + TimeSpan.FromMilliseconds(Environment.TickCount - StarTime).Duration());


            }
            ChangeTreeIndicators(WhichGame, !cts.IsCancellationRequested);
        }


        private void ScanPCCList(bool isTree, List<string> pccs = null)
        {
            ProgBarUpdater.ChangeProgressBar(0, isTree ? Tree.numPCCs : pccs.Count);

            OutputBoxPrintLn("Scanning Files and generating thumbnails...");
            StatusUpdater.UpdateText("Scanned: 0/" + (isTree ? Tree.numPCCs : pccs.Count));

            // KFreon: Begin parallel file scan
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = NumThreads;
            int count = 0;
            object countlock = new object();


            //DeepScanPCC(@"R:\\Games\\Origin Games\\Mass Effect 3\\BIOGame\\DLC\\DLC_CON_END\\CookedPCConsole\\BioD_End002_730MemorialGreen_LOC_INT.pcc");
            
            Parallel.For(0, isTree ? Tree.numPCCs : pccs.Count, po, (b, loopstate) =>
            {
                if (cts.IsCancellationRequested)
                    loopstate.Stop();
                string file = isTree ? Tree.GetPCC(b) : pccs[b];
                DebugOutput.PrintLn("Scanning: " + file);
                DeepScanPCC(file);
                lock (countlock)
                    count++;
                if (count % 10 == 0)
                    this.Invoke(new Action(() =>
                    {
                        MainProgressBar.Increment(10);
                        StatusLabel.Text = "Scanning: " + count + " / " + (isTree ? Tree.numPCCs : pccs.Count);
                    }));
            });
        }



        private void DeepScanPCC(string filename)
        {
            PCCObjects.IPCCObject temppcc = PCCObjects.Creation.CreatePCCObject(filename, WhichGame);

            // KFreon:  Search the exports list for viable texture types
            for (int i = 0; i < temppcc.Exports.Count; i++)
            {
                //TreeTexInfo tex = KFreonFormsLib.Miscextra.GenerateTexStruct(temppcc, i, WhichGame, pathBIOGame, ExecFolder, allpccs);
                bool result;
                TreeTexInfo tex = new TreeTexInfo(temppcc, i, WhichGame, pathBIOGame, ExecFolder, out result);

                if (result)
                    Tree.AddTex(tex, (WhichGame == 1) ? temppcc.Exports[i].Package : "", filename);
            }
        }


        public static bool SetupTree(ref TreeDB Tree, string pathCooked, int WhichGame, TreeView MainTreeView, string pathBIOGame)
        {
            if (Tree == null)
            {
                List<string> allPccFiles = new List<string>();
                string path = pathCooked;

                if (!Directory.Exists(path))
                {
                    DebugOutput.PrintLn("Couldn't find main game files for ME" + WhichGame + ".");
                    return false;
                }
                else
                {
                    DebugOutput.PrintLn("Found main gamefiles for ME" + WhichGame + " at  " + pathBIOGame);
                }

                allPccFiles.AddRange(Directory.GetFiles(path, "*", SearchOption.AllDirectories).Where(s =>
                {
                    if (WhichGame == 1)
                    {
                        return s.EndsWith(".upk", true, null) || s.EndsWith(".u", true, null) || s.EndsWith(".sfm", true, null);
                    }
                    else
                        return s.EndsWith(".pcc");
                }));
//                Tree = new TreeDB(ref MainTreeView, WhichGame, pathBIOGame);
                Tree = new TreeDB(allPccFiles, ref MainTreeView, WhichGame, pathBIOGame);
            }
            return true;
        }


        private bool FirstTimeSetup()
        {
            using (TexplorerFirstTimeSetup fts = new TexplorerFirstTimeSetup(WhichGame, DLCPath, pathCooked))
            {
                fts.ShowDialog();
                if (fts.FilesToAddToTree == null)
                {
                    StatusUpdater.UpdateText("First Time Setup Cancelled!");
                    DebugOutput.PrintLn("First time setup cancelled.");
                    return false;
                }
                else
                {
                    Tree.Clear(true);
                    Tree.AddPCCs(fts.FilesToAddToTree);
                }
            }

            BeginTreeScan();


            OutputBoxPrintLn("Performing first time setup. Don't worry, this only has to be done once.");
            return true;
        }


        private bool CheckDLCExtracted()
        {
            List<string> retval = new List<string>();
            List<string> check = new List<string>(Directory.GetFiles(pathBIOGame + "\\DLC", "*.*", SearchOption.AllDirectories).Where(b => b.EndsWith(".tfc") || b.EndsWith(".pcc")));
            List<string> modified = new List<string>();
            string print = "";
            foreach (string str in check)
            {
                modified.Add(str);
                print += "Modified DLC File: " + str + Environment.NewLine;
            }
            DebugOutput.PrintLn(print);

            if (modified.Count != 0)
            {
                if (MessageBox.Show("Some DLC files appear to be extracted. If these have been modified, scanning anyway may break the universe. NOTE: If you use content mods like ThaneMOD and MEHEM, refer to their instructions." + Environment.NewLine + "Do you want to use these old files?", "DLC already extracted", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                {
                    foreach (string file in modified)
                        File.Delete(file);
                    return false;
                }
                else
                    return true;
            }
            return false;
        }

        // KFreon: Extracts .tfc from DLC's and calculates number of files
        /*public bool EnumerateDLC()
        {
            List<string> files = new List<string>();
            DebugOutput.PrintLn("Detecting and investigating DLC...");
            switch (WhichGame)
            {
                case 1:
                    if (Directory.Exists(DLCPath))
                        files.AddRange(Directory.EnumerateFiles(DLCPath, "*", SearchOption.AllDirectories).Where(s => s.EndsWith(".upk", true, null) || s.EndsWith(".u", true, null) || s.EndsWith(".sfm", true, null)));
                    else
                    {
                        OutputBoxPrintLn("No DLC found. Continuing...");
                        return false;
                    }
                    break;
                case 2:
                    if (Directory.Exists(DLCPath))
                        files.AddRange(Directory.GetFiles(DLCPath, "*.pcc", SearchOption.AllDirectories));
                    else
                    {
                        OutputBoxPrintLn("No DLC found. Continuing...");
                        return false;
                    }
                    break;
                case 3:
                    if (Directory.Exists(DLCPath))
                        files.AddRange(Directory.GetFiles(DLCPath, "Default.sfar", SearchOption.AllDirectories));
                    else
                    {
                        OutputBoxPrintLn("No DLC found. Continuing...");
                        return false;
                    }
                    break;
            }

            List<string> tempfiles = new List<string>();

            // KFreon: Get DLC name
            foreach (String file in files)
            {
                string[] parts = file.Remove(0, DLCPath.Length).Split('\\');
                string tempfile = parts.First(s => s != "");
                if (!tempfiles.Contains(tempfile))
                    tempfiles.Add(tempfile);
            }

            // KFreon: Allow selection of DLC's
            using (Helpers.SelectionForm sf = new Helpers.SelectionForm(tempfiles, "Select DLC to use." + ((WhichGame == 3) ? " Selected DLC will be extracted if necessary." : ""), "Select DLC", true))
            {
                sf.ShowDialog();
                if (sf.SelectedItems.Count != 0)
                {
                    List<string> newfiles = new List<string>();
                    foreach (string name in sf.SelectedItems)
                    {
                        foreach (string file in files)
                            if (file.Contains(name) && !newfiles.Contains(file))
                                newfiles.Add(file);
                    }
                    files = newfiles;
                }
                else
                {
                    OutputBoxPrintLn("No DLC selected, so ignoring DLC.");
                    return false;
                }
            }


            // KFreon: ME3 DLC extraction
            if (WhichGame == 3)
            {
                ProgBarUpdater.ChangeProgressBar(0, files.Count);

                // KFreon: Check if DLC is already extracted
                if (!CheckDLCExtracted())
                {
                    // KFreon: Extract pccs and tfc from DLC
                    OutputBoxPrintLn("Extracting and shrinking DLC");
                    foreach (string file in files)
                    {
                        OutputBoxPrintLn("Extracting and shrinking DLC: " + file);
                        DLCExtractHelper(file);
                        ProgBarUpdater.IncrementBar();
                        if (cts.IsCancellationRequested)
                            return false;
                    }
                    List<string> date = new List<string>();
                    date.Add(DateTime.Now.Ticks.ToString());
                    File.WriteAllLines(pathBIOGame + "\\DLC\\" + "TexplorerDLCDate.txt", date.ToArray());
                }

                // KFreon: Add extracted files to list
                List<string> newfiles = new List<string>();
                foreach (string dlc in files)
                    newfiles.AddRange(Directory.EnumerateFiles(Path.GetDirectoryName(dlc), "*.pcc", SearchOption.AllDirectories));  //.Where(s => s.EndsWith(".pcc") || s.EndsWith(".tfc")));

                files = newfiles;
            }

            if (!Tree.AddPCCs(files))
            {
                MessageBox.Show("Adding DLC to Tree failed. PCCs is null.");
                return false;
            }
            Tree.numDLCFiles = files.Count;
            return true;
        }*/


        

        private void DisappearPictureBox()
        {
            MainListView.Dock = DockStyle.Fill;
            MainListView.Visible = true;
        }

        private void ConstructTree()
        {
            DebugOutput.PrintLn("Number of texes = " + Tree.TexCount);
            OutputBoxPrintLn("Constructing tree...");
            StatusUpdater.UpdateText("Constructing Tree...");
            if (!noWindow)
            {
                this.Invoke(new Action(() =>
                {
                    MainTreeView.Nodes.Clear();
                    MainTreeView.BeginUpdate();
                }));
            }
            t = new myTreeNode("All Texture Files", "All Texture Files");

            // KFreon:  Loop over all textures and add them to a node
            for (int i = 0; i < Tree.TexCount; i++)
            {
                TreeTexInfo currentTex = Tree.GetTex(i);
                string[] packs = currentTex.FullPackage.Split('.');
                myTreeNode node = null;
                myTreeNode prevNode = t;
                int NodeCount = Tree.NodeCount;

                for (int j = 0; j < packs.Length; j++)
                {
                    node = null;
                    string tempPack = String.Join(".", packs, 0, j + 1);
                    for (int k = 0; k < NodeCount; k++)
                    {
                        myTreeNode current = Tree.GetNode(k);
                        if (current.Name == tempPack)
                        {
                            node = current;
                            break;
                        }
                    }


                    if (node == null)
                    {
                        node = new myTreeNode(packs[j], tempPack);
                        prevNode.Nodes.Add(node);
                        Tree.AddNode(node);
                    }
                    prevNode = node;
                }
                string texname = currentTex.TexName;
                node.TexInds.Add(i);
                Textures.ITexture2D tex2D = Textures.Creation.CreateTexture2D(texname, currentTex.Files, currentTex.ExpIDs, WhichGame, pathBIOGame, currentTex.Hash);
                tex2D.Mips = currentTex.NumMips;
                currentTex.Textures.Add(tex2D);
                currentTex.ParentNode = node;
                if (!Tree.ReplaceTex(i, currentTex))
                    MessageBox.Show("Replace in tree failed! Index: " + i + ", thisTex: " + currentTex.TexName);

            }

            if (!noWindow)
            {
                this.Invoke(new Action(() =>
                {
                    MainTreeView.Sort();
                    MainTreeView.Nodes.Add(t);
                    MainTreeView.EndUpdate();
                }));
            }
            DebugOutput.PrintLn("Tree constructed!");
        }


        private void DrawPCCList(int index)
        {
            TreeTexInfo tex = Tree.GetTex(index);
            PCCsCheckedListBox.Items.Clear();
            PCCsCheckedListBox.Items.Add("Select All", (tex.Files.Count == tex.OriginalFiles.Count ? true : false));
            for (int i = 0; i < tex.OriginalFiles.Count; i++)
            {
                bool isChecked = false;
                for (int j = 0; j < tex.Files.Count; j++)
                {
                    if (tex.OriginalFiles[i] == tex.Files[j])
                    {
                        isChecked = true;
                        break;
                    }
                }
                PCCsCheckedListBox.Items.Add(tex.OriginalFiles[i] + "      @" + tex.OriginalExpIDs[i], isChecked);
            }
        }


        private void UpdatePCCList(bool SelectAll, TreeTexInfo current)
        {
            if (SelectAll)
                for (int i = 0; i < PCCsCheckedListBox.Items.Count; i++)
                    PCCsCheckedListBox.SetItemChecked(i, PCCsCheckedListBox.GetItemChecked(0));
            else
                PCCsCheckedListBox.SetItemChecked(0, false);
            List<string> newlist = new List<string>();
            foreach (string item in PCCsCheckedListBox.CheckedItems)
                newlist.Add(item.Split('@')[0].Trim());
            current.Files = new List<string>(newlist);
            Tree.ReplaceTex(GetSelectedTexInd(), current);
        }


        public void InstallTexture(string texname, List<string> pccs, List<int> IDs, byte[] imgdata)
        {
            string fulpath = pccs[0];
            string temppath = (WhichGame == 1) ? Path.GetDirectoryName(pathBIOGame) : pathBIOGame;
            if (!fulpath.Contains(temppath))
                fulpath = Path.Combine(temppath, fulpath);

            PCCObjects.IPCCObject pcc = PCCObjects.Creation.CreatePCCObject(fulpath, WhichGame);

            if ((pcc.Exports[IDs[0]].ClassName != "Texture2D" && pcc.Exports[IDs[0]].ClassName != "LightMapTexture2D" && pcc.Exports[IDs[0]].ClassName != "TextureFlipBook") || String.Compare(pcc.Exports[IDs[0]].ObjectName, texname, true) != 0)
                throw new InvalidDataException("Export is not correct class or name!");

            //Load the texture from the pcc
            Textures.ITexture2D tex2D = null;
            tex2D = pcc.CreateTexture2D(IDs[0], pathBIOGame);
            tex2D.allPccs = pccs;
            tex2D.expIDs = IDs;
            int noImg = tex2D.imgList.Count;

            DebugOutput.PrintLn("Now replacing textures in texture: " + tex2D.texName, true);

            ImageFile im = null;
            try
            {
                im = new DDS("", imgdata);
            }
            catch
            {
                Console.WriteLine("");
            }

            //The texture is a single image, therefore use replace function
            if (noImg == 1)
            {
                string imgSize = tex2D.imgList[0].imgSize.width.ToString() + "x" + tex2D.imgList[0].imgSize.height.ToString();
                try
                {
                    tex2D.replaceImage(imgSize, im, pathBIOGame);
                }
                catch
                {
                    // KFreon:  If replace fails, it's single image thus use the singleimageupscale function
                    tex2D.singleImageUpscale(im, pathBIOGame);
                }
            }
            //If the texture has multiple images, then check the input texture for MIPMAPS
            else
            {
                bool hasMips = true;
                ImageFile imgFile = im;
                try { ImageMipMapHandler imgMipMap = new ImageMipMapHandler("", imgdata); }
                catch { hasMips = false; }
                if (!hasMips)
                {
                    //string fileformat = Path.GetExtension(texFile);
                    //imgFile = new DDS(texFile);
                    string imgSize = imgFile.imgSize.width.ToString() + "x" + imgFile.imgSize.height.ToString();
                    try
                    {
                        //Try replacing the image. If it doesn't exist then it'll throw and error and you'll need to upscale the image
                        tex2D.replaceImage(imgSize, imgFile, pathBIOGame);
                    }
                    catch { tex2D.addBiggerImage(imgFile, pathBIOGame); }
                }
                else
                {
                    try
                    {
                        tex2D.OneImageToRuleThemAll(imgFile, pathBIOGame, imgdata);
                    }
                    catch (Exception e)
                    {
                        if (e.Message.Contains("Format"))
                        {
                            MessageBox.Show(texname + " is in the wrong format." + Environment.NewLine + Environment.NewLine + e.Message);
                            return;
                        }
                    }
                }
            }

            DebugOutput.PrintLn("Replacement complete. Now saving pcc: " + pcc.pccFileName, true);

            //tex2D.DumpTexture(@"R:\commit.txt");

            PCCObjects.IExportEntry expEntry = pcc.Exports[IDs[0]];
            expEntry.SetData(tex2D.ToArray(expEntry.DataOffset, pcc));
            expEntry.hasChanged = true;
            pcc.Exports[IDs[0]] = expEntry;

            pcc.saveToFile(pcc.pccFileName);

            /*File.Delete(loc + "tex.dds");
            File.Delete(loc + "data.bin");*/

            int modCount = tex2D.allPccs.Count;

            // KFreon: Elapsed time stuff
            int start = Environment.TickCount;

            // KFreon: Start threading of save function (max 10 threads)
            //ParallelOptions po = new ParallelOptions();
            //po.MaxDegreeOfParallelism = 2;
            if (modCount > 1)
                //Parallel.For(1, modCount, po, (item, loopstate) =>
                for (int item = 1; item < modCount; item++)
                {
                    if (!SaveFile(pccs, IDs, tex2D, item))
                        break;
                    /*loopstate.Stop();
            });*/
                }

            // KFreon: More timer stuff
            TimeSpan ts = TimeSpan.FromMilliseconds(Environment.TickCount - start);
            Console.WriteLine(ts.Duration().ToString());
            GC.Collect();
            DebugOutput.Print("All PCC updates finished. ");
        }

        public static void UpdateTOCs(string pathBIOGame, int WhichGame, string DLCPath, List<string> modifiedDLC = null)
        {
            DebugOutput.PrintLn("Updating Basegame...");
            AutoTOC.AutoTOC toc = new AutoTOC.AutoTOC();

            // KFreon: Format filenames
            List<string> FileNames = toc.GetFiles(pathBIOGame + "\\");
            List<string> tet = new List<string>(pathBIOGame.Split('\\'));
            tet.RemoveAt(tet.Count - 1);
            string remov = String.Join("\\", tet.ToArray());
            for (int i = 0; i < FileNames.Count; i++)
                FileNames[i] = FileNames[i].Substring(remov.Length + 1);

            // KFreon: Format basepath
            string[] ts = pathBIOGame.Split('\\');
            tet.Clear();
            tet.AddRange(ts);
            tet.RemoveAt(tet.Count - 1);
            string basepath = String.Join("\\", tet.ToArray()) + '\\';
            string tocfile = pathBIOGame + "\\PCConsoleTOC.bin";
            Console.WriteLine("BasePath: " + basepath);
            Console.WriteLine("Tocfile: " + tocfile);
            toc.CreateTOC(basepath, tocfile, FileNames.ToArray());

            // KFreon: Update pcconsole.bin - not updated by WV's code.
            //tocUpdater(pathBIOGame + "\\PCConsoleTOC.bin");
            DebugOutput.PrintLn("Basegame updated.");
            

            // KFreon: Update DLC TOC's
            if (WhichGame == 3)
            {
                List<string> files = new List<string>(Directory.EnumerateFiles(DLCPath, "Default.sfar", SearchOption.AllDirectories));
                DebugOutput.PrintLn("Updating DLC...");
                if (modifiedDLC != null)
                {
                    
                    files.Clear();
                    foreach (string line in modifiedDLC)
                        files.Add(pathBIOGame + "\\DLC\\" + line + "\\CookedPCConsole\\Default.sfar");
                }

                foreach (string file in files)
                {
                    if (file != "")
                    {
                        DLCPackage dlc = new DLCPackage(file);
                        List<TOCBinFile.Entry> entries = dlc.UpdateTOCbin(true);
                        using (StreamWriter sw = new StreamWriter(@"R:\commit.txt"))
                        {
                            foreach (TOCBinFile.Entry entry in entries)
                            {
                                sw.WriteLine(entry.name);
                                sw.WriteLine(entry.size);
                                sw.WriteLine(entry.offset);
                                sw.WriteLine();
                            }
                        }
                    }
                }
                DebugOutput.PrintLn("DLC Updated.");
            }
        }

        internal void AddToTree(string path, int exportID)
        {
            MessageBox.Show("THIS DOESN'T WORK YET! LET ME KNOW PLEASE.");
            return;
            BeginLoadingTree(true);
            PCCObjects.IPCCObject pcc = PCCObjects.Creation.CreatePCCObject(path, WhichGame);

            //TreeTexInfo tex = KFreonFormsLib.Miscextra.GenerateTexStruct(pcc, exportID, WhichGame, pathBIOGame, ExecFolder, Tree.GetPCCsAsList());
            bool Success;
            TreeTexInfo tex = new TreeTexInfo(pcc, exportID, WhichGame, pathBIOGame, ExecFolder, out Success);
            Tree.AddTex(tex, "", "");  // not right - should be blind add?
            if (File.Exists(Tree.TreePath))
                File.Delete(Tree.TreePath);
            Tree.WriteToFile(Tree.TreePath, Path.GetDirectoryName(pathBIOGame));
        }

        private void OutputBoxPrintLn(string message)
        {
            if (noWindow)
            {
                DebugOutput.PrintLn(message);
                return;
            }

            if (!OutputBox.Parent.Created)
            {
                Task.Run(() =>
                {
                    try
                    {
                        while (!OutputBox.Parent.Created)
                            System.Threading.Thread.Sleep(100);
                        OutputBoxPrintLn(message);
                    }
                    catch
                    {
                        return;
                    }
                });
                return;
            }

            DebugOutput.PrintLn(message);
            if (OutputBox.InvokeRequired)
                OutputBox.Invoke(new Action(() =>
                {
                    OutputBox.AppendText(message + Environment.NewLine);
                    OutputBox.ScrollToCaret();
                }));
            else
            {
                OutputBox.AppendText(message + Environment.NewLine);
                OutputBox.ScrollToCaret();
            }
        }


        private void UpdateTexDetails(int index, out TreeTexInfo tex, out Textures.ITexture2D tex2D)
        {
            tex = Tree.GetTex(index);
            tex2D = tex.Textures[0];

            //if (tex2D.imgList.Count == 0)
            if (!tex2D.hasChanged)
            {
                //Textures.ITexture2D newtex2D = KFreonFormsLib.Textures.Creation.CreateTexture2D(KFreonFormsLib.PCCObjects.Creation.CreatePCCObject(tex2D.allPccs[0], WhichGame), tex2D.expIDs[0], WhichGame, pathBIOGame, tex2D.Hash);
                tex2D = Textures.Creation.CreateTexture2D(tex2D, WhichGame, pathBIOGame);

                tex.Textures = new List<Textures.ITexture2D>();
                tex.Textures.Add(tex2D);
                Tree.ReplaceTex(index, tex);
            }
        }


        private void MainTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // KFreon: Populate MainListView and change icons etc in MainTreeView
            MainListView.BeginUpdate();
            MainListView.Items.Clear();
            myTreeNode locTree = e.Node as myTreeNode;
            int i = 0;
            //foreach (Textures.ITexture2D tex in locTree.textures)
            for (int j = 0; j < locTree.TexInds.Count; j++)
            {
                TreeTexInfo tex = Tree.GetTex(locTree.TexInds[j]);
                ListViewItem it = new ListViewItem("", 0);
                it.Text = tex.TexName;
                it.Name = i.ToString();
                tex.ListViewIndex = MainListView.Items.Count;
                //Tree.ReplaceTex(locTree.TexInds[j], tex);
                MainListView.Items.Add(it);
                i++;
            }
            MainListView.EndUpdate();

            // KFreon: Populate properties tab
            PropertiesRTB.Text = "Tree Node:  " + e.Node.Name + Environment.NewLine;
            int line1length = PropertiesRTB.TextLength;

            // KFreon: Count number of textures in node
            int count = locTree.NodeTextureCount();
            if (locTree.TexCount == 0)
                locTree.TexCount = count;
            PropertiesRTB.Text += "Contains: " + count;

            PCCsCheckedListBox.Items.Clear();

            // KFreon: Load thumbnails
            ListViewImageList.Images.Add(Image.FromFile(ExecFolder + "Placeholder.ico"));  // This is the old image seen in the list
            UpdateThumbnailDisplays(e.Node as myTreeNode);
        }


        /// <summary>
        /// Loads thumbnails from disk, generated during treescan.
        /// </summary>
        /// <param name="nod">Node to load textures into.</param>
        private void UpdateThumbnailDisplays(myTreeNode nod)
        {
            if (!MainListView.InvokeRequired)
            {
                Task.Run(new Action(() => UpdateThumbnailDisplays(nod)));
                return;
            }
            myTreeNode SelectedNode = nod;
            List<Image> thumbs = new List<Image>();
            List<int> thuminds = new List<int>();

            for (int i = 0; i < SelectedNode.TexInds.Count; i++)
            {
                TreeTexInfo tex = Tree.GetTex(SelectedNode.TexInds[i]);
                int ind = i + 1;

                // KFreon: If no thumbnail, try again
                if (tex.ThumbnailPath == null)
                {
                    ind = 0;
                    continue;
                }
                else if (tex.ThumbnailPath.Contains("placeholder.ico") && !tex.TriedThumbUpdate)
                {
                    using (Bitmap img = tex.GetImage(pathBIOGame))
                    {

                        PCCObjects.IPCCObject pcc = PCCObjects.Creation.CreatePCCObject(tex.Files[0], WhichGame);
                        Textures.ITexture2D tex2D = pcc.CreateTexture2D(tex.ExpIDs[0], pathBIOGame);
                        if (img != null)
                        {
                            string savepath = Path.Combine(ThumbnailPath, tex.ThumbName);
                            if (File.Exists(savepath))
                            {
                                ind = i + 1;
                                tex.ThumbnailPath = savepath;
                            }
                            else
                            {
                                string thumbpath = null;
                                try
                                {
                                    thumbpath = Textures.Creation.GenerateThumbnail(img, savepath, ExecFolder);
                                    tex.ThumbnailPath = thumbpath;
                                    ind = i + 1;
                                }
                                catch
                                {
                                    ind = 0;
                                    // IGNORE
                                }
                            }
                            tex.TriedThumbUpdate = true;
                            Tree.ReplaceTex(SelectedNode.TexInds[i], tex);
                        }
                    }
                }
                Image thumb;
                try
                {
                    thumb = Textures.Methods.FixThumb(Image.FromFile(tex.ThumbnailPath), 128);
                    thumbs.Add(thumb);
                }
                catch
                {
                    DebugOutput.PrintLn("Thumbnail: " + tex.ThumbnailPath + "  not found.");
                    ind = 0;
                }
                thuminds.Add(ind);
            }

            // KFreon: Set images to show in ListView
            if (!MainListView.IsDisposed)
            {
                this.Invoke(new Action(() =>
                {
                    ListViewImageList.Images.Clear();
                    ListViewImageList.Images.Add(new Bitmap(Path.Combine(ExecFolder, "placeholder.ico")));
                    ListViewImageList.Images.AddRange(thumbs.ToArray());


                    for (int i = 0; i < MainListView.Items.Count; i++)
                    {
                        try
                        {
                            MainListView.Items[i].ImageIndex = thuminds[i];
                        }
                        catch(Exception e)
                        {
                            DebugOutput.PrintLn("Error setting thumbnails in display: " + e.Message);
                        }
                    }

                    MainListView.Refresh();
                }));
            }
        }

        private void MainTreeView_AfterExpand(object sender, TreeViewEventArgs e)
        {
            e.Node.ImageIndex = 1;
        }

        private void MainTreeView_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            e.Node.ImageIndex = 0;
        }

        private void ShowContextPanel(bool state)
        {
            Transitions.ITransitionType trans = new Transitions.TransitionType_CriticalDamping(400);
            Transitions.Transition.run(ContextPanel, "Height", (!state) ? 0 : 30, trans);
        }

        private void MainListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            // KFreon: Indexing check
            if (MainListView.SelectedIndices.Count <= 0 || MainListView.SelectedIndices[0] < 0)
            {
                // KFreon: Disappear context menu
                //ShowContextPanel(false);
                return;
            }

            // KFreon: Show context menu
            //ShowContextPanel(true);

            // KFreon: Populate properties pane
            int index = GetSelectedTexInd();   // Note: SelectedIndicies only has 1 element (multiselect = false)
            TreeTexInfo tex = Tree.GetTex(index);
            Textures.ITexture2D tex2D = tex.Textures[0];

            // KFreon: Update Texture information if required
            UpdateTexDetails(index, out tex, out tex2D);

            Textures.IImageInfo info = tex2D.GenerateImageInfo();

            // KFreon: Display texture properties
            DisplayTextureProperties(tex2D, info);
            DrawPCCList(index);
        }

        private void DisplayTextureProperties(Textures.ITexture2D tex2D, Textures.IImageInfo info)
        {
            List<string> message = new List<string>();
            message.Add("Texture Name:  " + tex2D.texName);
            message.Add("Format:  " + (tex2D.texFormat.ToLower().Contains("g8") ? tex2D.texFormat + @"/L8" : tex2D.texFormat));
            message.Add("Width:  " + info.imgSize.width + ",  Height:  " + info.imgSize.height);
            message.Add("LODGroup:  " + (tex2D.hasChanged ? "TEXTUREGROUP_Shadowmap" : ((String.IsNullOrEmpty(tex2D.LODGroup) ? "None (Uses World)" : tex2D.LODGroup))));
            message.Add("Texmod Hash:  " + Textures.Methods.FormatTexmodHashAsString(tex2D.Hash));

            if (WhichGame != 1)
                message.Add("Texture Cache File:  " + (String.IsNullOrEmpty(tex2D.arcName) ? "PCC Stored" : tex2D.arcName + ".tfc"));

            PropertiesRTB.Text = String.Join(Environment.NewLine, message);
        }

        private void PCCsCheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = PCCsCheckedListBox.SelectedIndex;
            bool SelectAll = false;
            if (index < 0)
                return;

            int texInd = GetSelectedTexInd();
            TreeTexInfo temp = Tree.GetTex(texInd);
            if (index == 0)
                SelectAll = true;
            else if ((PCCsCheckedListBox.CheckedIndices.Count == temp.Files.Count) && !PCCsCheckedListBox.CheckedIndices.Contains(0))    // KFreon: If all items checked, check "Select All" box
            {
                PCCsCheckedListBox.SetItemChecked(0, true);
                SelectAll = true;
            }

            Console.WriteLine(PCCsCheckedListBox.CheckedIndices.Count);
            UpdatePCCList(SelectAll, temp);
        }


        private async void Form_Closing(object sender, FormClosingEventArgs e)
        {
            bool cleanup = false;
            bool unsaved = false;

            // KFreon: Cancelling is a multithread fix for closing the form when I want to
            if (cancelling)
            {
                // KFreon: Background processes
                if (CancelButton.Visible)  // Only visible when backbone is doing stuff
                {
                    CancelButton_Click(null, null);
                    cleanup = cts.IsCancellationRequested;

                    // KFreon: Don't cancel
                    if (!cleanup)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                // KFreon: Unsaved changes
                if (ChangedTextures.Count > 0)
                {
                    if (MessageBox.Show("There are unsaved changes. Are you sure you want to exit?", "We fight or we die...", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                    else
                    {
                        cleanup = true;
                        unsaved = true;
                    }
                }
            }

            e.Cancel = cancelling;
            this.ControlBox = !cancelling;

            if (cancelling)
                await Task.Run(() => CloseForm(cleanup, unsaved));
            else
            {
                DebugOutput.PrintLn("-----Execution of Texplorer closing...-----");
                Properties.Settings.Default.Save();
            }
        }

        private void CloseForm(bool cleanup, bool unsaved)
        {
            cancelling = false;
            // KFreon: Wait for stuff, if necessary
            if (cleanup)
            {
                if (!unsaved)
                    while (!StatusLabel.Text.ToLowerInvariant().Contains("cancelled"))
                        System.Threading.Thread.Sleep(100);

                // KFreon: Regenerate modified thumbnails if necessary
                for (int i = 0; i < ChangedTextures.Count; i++)
                {
                    TreeTexInfo tex = Tree.GetTex(ChangedTextures[i]);
                    RegenerateThumbnail(tex, ChangedTextures[i], true);
                }


                // KFreon: Reset Search
                if (Search != null)
                {
                    Search.Close();
                    Search.Dispose();
                }
            }
            this.Invoke(new Action(() => this.Close()));
        }

        public void SetupSearch()
        {
            Search = new KFreonSearchForm(this);
            Search.LoadSearch();
        }

        public void ResetSearchAfterClose()
        {
            Task.Run(() =>
            {
                while (Search.IsHandleCreated)
                    System.Threading.Thread.Sleep(50);

                this.Invoke(new Action(() => SetupSearch()));
            });
        }

        // KFreon:  This method extends the original by dealing correctly with dupicate named textures.
        public void GoToTex(string name, string nodeName, int selectedIndex)
        {
            // Clear previous search
            MainListView.SelectedItems.Clear();
            foreach (myTreeNode node in Tree.GetNodesAsList())
                node.BackColor = Color.White;

            // KFreon: Search in tree for specified texture
            List<TreeTexInfo> found = new List<TreeTexInfo>(Tree.GetTreeAsList().Where(tex => (name.ToLowerInvariant() == tex.TexName.ToLowerInvariant() && nodeName.ToLowerInvariant() == tex.ParentNode.Text.ToLowerInvariant())));

            // KFreon: Pick one of the textures found based on which is selected in the search box
            TreeTexInfo treetex = found[selectedIndex];

            // KFreon: Set selected node and set its colour
            MainTreeView.SelectedNode = treetex.ParentNode;
            treetex.ParentNode.BackColor = Color.Green;

            // KFreon: Select item in ListView and ensure it's visible and focused
            MainListView.Items[treetex.ListViewIndex].Selected = true;
            MainListView.EnsureVisible(treetex.ListViewIndex);
            MainListView.Focus();
        }

        private void DetailsHideButton_Click(object sender, EventArgs e)
        {
            Transitions.ITransitionType transition = new Transitions.TransitionType_CriticalDamping(700);

            if (DetailsHideButton.Text == ">>")
            {
                PropertiesWidth = splitContainer3.Width - splitContainer3.Panel2.Width;
                TabSearchSplitter.Visible = false;
                SearchBox.Visible = false;
                SearchCountLabel.Visible = false;
                Transitions.Transition.run(splitContainer3, "SplitterDistance", splitContainer3.Width - 40, transition);
                DetailsHideButton.Text = "<<";
            }
            else
            {
                Transitions.Transition.run(splitContainer3, "SplitterDistance", PropertiesWidth, transition);
                DetailsHideButton.Text = ">>";
                TabSearchSplitter.Visible = true;
                SearchBox.Visible = true;
                SearchCountLabel.Visible = true;
            }
        }

        private void Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (Tree.TexCount != 0)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    Task.Run(() =>
                    {
                        if (SearchBox.Text != "")
                        {
                            Transitions.Transition.run(TabSearchSplitter, "SplitterDistance", TabSearchSplitter.Height / 2, new Transitions.TransitionType_CriticalDamping(400));
                            Search.SearchAllv5(SearchBox.Text, SearchListBox, "new");
                        }
                    });
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.Control && e.KeyCode == Keys.Back)
                {
                    if (SearchBox.Text != "")
                    {
                        string text = SearchBox.Text;
                        int length = text.LastIndexOfAny(new char[] { '_', ' ', '-', '\\' });
                        SearchBox.Text = text.Substring(0, length + 1);
                        SearchBox.SelectionStart = SearchBox.Text.Length;
                        SearchBox.SelectionLength = 0;
                    }
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            if (SearchBox.Text == "")
            {
                Transitions.Transition.run(TabSearchSplitter, "SplitterDistance", 0, new Transitions.TransitionType_CriticalDamping(400));
                SearchCountLabel.Text = "";
                int dest = splitContainer4.Width - (SearchBox.Location.X - TabSearchSplitter.Location.X) - 4;
                Transitions.Transition.run(SearchBox, "Width", dest, new Transitions.TransitionType_CriticalDamping(200));
            }
            else
            {
                if (SearchBox.ForeColor != Color.Gray && Tree != null && Tree.TexCount != 0)
                {
                    if (SearchBox.Text[0] != '\\')
                    {
                        Transitions.Transition.run(TabSearchSplitter, "SplitterDistance", TabSearchSplitter.Height / 2, new Transitions.TransitionType_CriticalDamping(400));
                        Search.SearchAllv5(SearchBox.Text, SearchListBox, "new");
                        SearchCountLabel.Text = SearchListBox.Items.Count.ToString();
                        int dest = splitContainer4.Width - 4 - (SearchCountLabel.Width) - (SearchBox.Location.X - TabSearchSplitter.Location.X);
                        Transitions.Transition.run(SearchBox, "Width", dest, new Transitions.TransitionType_CriticalDamping(200));
                    }
                }
            }
        }

        private void SearchListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SearchList_Click(SearchListBox);
        }

        public void SearchList_Click(ListBox box)
        {
            if (box.SelectedIndex == -1)
                return;

            string fullName = box.SelectedItem.ToString();
            string texName = fullName.Remove(fullName.IndexOf(" ("));
            int lastIndex = fullName.LastIndexOf("(");
            string nodeName = fullName.Remove(0, lastIndex >= 0 ? lastIndex : 0).Replace("(", "").Replace(")", "");

            // KFreon: Get all entries in box
            int index = -1;
            int count = 0;
            for (int i = 0; i < box.Items.Count; i++)
            {
                string name = box.Items[i].ToString();
                if (name.ToLowerInvariant() == fullName.ToLowerInvariant())
                {
                    index = i == box.SelectedIndex ? count : index;
                    count++;
                }
            }

            GoToTex(texName, nodeName, index);
        }

        public List<TreeTexInfo> SearchLoad()
        {
            return Tree.GetTreeAsList();
        }

        private void SearchBox_Enter(object sender, EventArgs e)
        {
            if (SearchBox.ForeColor == Color.Gray)
            {
                SearchBox.Clear();
                SearchBox.ForeColor = Color.Black;
            }
        }

        private void ResetSearchBox()
        {
            SearchBox.ForeColor = Color.Gray;
            SearchBox.Text = "Type to search...";
        }

        private void SearchBox_Leave(object sender, EventArgs e)
        {
            if (SearchBox.Text == "")
                ResetSearchBox();
        }

        private void Change_Changed(object sender, EventArgs e)
        {
            // KFreon: Recalculate position
            SearchCountLabel.Left = TabSearchSplitter.Width - (SearchCountLabel.Width + 4);
            Search.Reset();
            Search.LoadSearch();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Background tasks are running. Are you sure you want to cancel them?", "I don't know about this Shepard...", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.Yes)
            {
                CancelButton.Text = "Cancelling...";
                CancelButton.Enabled = false;
                cts.Cancel();
            }
        }

        private void MainListView_DoubleClick(object sender, EventArgs e)
        {
            if (MainListView.SelectedItems.Count <= 0)
                return;

            TreeTexInfo tex = Tree.GetTex(GetSelectedTexInd());
            Bitmap img = tex.GetImage(pathBIOGame, 512);

            // KFreon: Show image
            if (img != null)
            {
                if (MainPictureBox.Image != null)
                    MainPictureBox.Image.Dispose();
                MainPictureBox.Image = img;
                MainPictureBox.Refresh();
                MainListView.Visible = false;
            }
            else
                MessageBox.Show("Unknown DDS image. Contact KFreon.");
        }

        private int GetSelectedTexInd()
        {
            return ((myTreeNode)MainTreeView.SelectedNode).TexInds[MainListView.SelectedIndices[0]];
        }

        private void MainPictureBox_Click(object sender, EventArgs e)
        {
            MainListView.Visible = true;
        }

        private void MainListView_FocusLeave(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                // KFreon: Disappear context menu
                System.Threading.Thread.Sleep(300);
                ShowContextPanel(false);
            });

        }

        private void PicturePanel_Click(object sender, MouseEventArgs e)
        {
            MainPictureBox_Click(null, null);
        }

        private string ExternalImageSelector(Textures.ITexture2D tex2D)
        {
            string path = "";
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select the image to add";
                ofd.Filter = "Image file|*.dds|All files|*.*";
                //                ofd.Filter = "Image file|*" + tex2D.getFileFormat() + "|All files|*.*";

                if (ofd.ShowDialog() != DialogResult.OK)
                    return "";
                else
                    path = ofd.FileName;
            }

            // KFreon: Check replacing texture
            string selectedformat;
            if (!Textures.Methods.CheckTextureFormat(File.ReadAllBytes(path), tex2D.texFormat, out selectedformat))
            {
                MessageBox.Show("Invalid format. Selected image is: " + selectedformat.ToUpperInvariant() + "  Required: " + tex2D.texFormat.ToUpperInvariant(), "Mission Failure", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return "";
            }

            // KFreon: Check replacing mips
            int numMips = 0;
            bool CorrectMips = Textures.Methods.CheckTextureMips(path, tex2D.Mips, out numMips);
            if (!CorrectMips)
            {
                MessageBox.Show("Mipmap error. Requires: " + tex2D.Mips + ".  Currently: " + numMips, "Mission Failure", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return "";
            }
            return path;
        }

        private void UpdateModifiedTex(Textures.ITexture2D tex2D, TreeTexInfo tex, int ind)
        {
            if (!tex2D.hasChanged)
                tex2D.hasChanged = true;
            tex.Textures[0] = tex2D;

            // KFreon: Change texture in tree
            if (!ChangedTextures.Contains(ind))
                ChangedTextures.Add(ind);
            Tree.ReplaceTex(ind, tex);
        }

        private void AddBiggerButton_Click(object sender, EventArgs e)
        {
            try
            {
                string path = "";
                int ind = GetSelectedTexInd();
                TreeTexInfo tex = Tree.GetTex(ind);
                Textures.ITexture2D tex2D = tex.Textures[0];
                tex2D.Mips = tex.NumMips;

                path = ExternalImageSelector(tex2D);
                if (path == "")
                    return;

                ProgBarUpdater.ChangeProgressBar(0);


                ImageFile im = Textures.Creation.LoadAKImageFile(null, path);
                byte[] imgData = File.ReadAllBytes(path);

                // add function
                // KFreon:  If single image (no mips), use a different function.
                if (tex2D.imgList.Count <= 1)
                    tex2D.singleImageUpscale(im, pathBIOGame);
                else
                    tex2D.OneImageToRuleThemAll(im, pathBIOGame, imgData);

                //--------- update the pcc with the new replaced infos ----------
                //First check that the tex2D hasn't already been modded (i.e. it hasn't been replaced and then
                //upscaled
                //if (!t.textures[Convert.ToInt32(listItem.Name)].hasChanged)

                UpdateModifiedTex(tex2D, tex, ind);

                if (WhichGame == 3)
                {
                    // KFreon:  Update TOC.bin using custom path if necessary
                    if (tex2D.arcName == null)
                    {
                        string tempCache = "";
                        for (int i = 10; i >= 0; i--)
                        {
                            tempCache = pathBIOGame + "\\CookedPCConsole\\CustTextures" + i + ".tfc";
                            if (File.Exists(tempCache))
                                break;
                        }
                        tocUpdater(tempCache);
                    }
                    else
                        tocUpdater(tex2D.GetTexArchive(pathBIOGame));
                }
                StatusUpdater.UpdateText("Replacement Complete!");
                OutputBoxPrintLn("Texture: " + tex.TexName + " Replaced.");
                this.Invoke(new Action(() => MainProgressBar.Value = MainProgressBar.Maximum));


                // KFreon: Regenerate thumbnail
                RegenerateThumbnail(tex, ind, false);
                UpdateThumbnailDisplays(MainTreeView.SelectedNode as myTreeNode);

                DisplayTextureProperties(tex2D, tex2D.GenerateImageInfo());

                // KFreon: If modmaking load textures
                if (ModMakerMode)
                {
                    AddModJob(tex2D, path);
                    StatusUpdater.UpdateText("Replacement Complete and job added to Modmaker!");
                }

                if (TPFMode)
                {
                    AddTPFToolsJob(path, tex.Hash);
                    StatusUpdater.UpdateText("Replacement Complete and job added to TPFTools!");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("An error occurred while replacing texture:\n" + exc, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (ModMakerMode)
                    MessageBox.Show("The textures have also not been added to the ModMaker Joblist");
            }
        }

        private async void AddTPFToolsJob(string replacingPath, uint hash)
        {
            KFreonTPFTools3 tpftools = await KFreonTPFTools3.GetCurrentInstance();
            int texind = tpftools.LoadExternal(replacingPath, false);
            tpftools.UpdateHashAndReplace(texind, hash, true);
            tpftools.RedrawTreeView();
        }

        /// <summary>
        /// Will update PCConsoleTOC.bin for a particular filename s
        /// </summary>
        /// <param name="s">The filename of the file to be updated</param>
        private bool tocUpdater(string s)
        {
            FileInfo finfo = new FileInfo(s);
            if (!finfo.Exists)
                throw new FileNotFoundException("File: " + s + " not found");
            if (String.IsNullOrEmpty(tocpath))
                tocpath = Path.Combine(pathBIOGame, "PCConsoleTOC.bin");
            TOCeditor tc = new TOCeditor();
            if (!File.Exists(tocpath))
            {
                OpenFileDialog tocopen = new OpenFileDialog();
                tocopen.Title = "Select the PCConsoleTOC.bin file to use";
                tocopen.Filter = "TOC.bin|PCConsoleTOC.bin";
                if (tocopen.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    MessageBox.Show("Since you have elected not to select your TOC.bin file, you'll get harrassed every time the program wants to update it and the game will crash unless you run the TOCbinUpdater tool", "I hope you're happy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                tocpath = tocopen.FileName;
            }

            // KFreon: If normal TOC update fails, try with WV's
            if (!tc.UpdateFile(Path.GetFileName(s), (uint)finfo.Length, tocpath))
                return false;
            else
                return true;
        }

        private void regenerateThumbnailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will delete and recreate all thumbnails for ME" + WhichGame + ". This will take some time. Are you sure?", "Consensus?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
            {
                int texCount = Tree.TexCount;
                ProgBarUpdater.ChangeProgressBar(0, texCount);
                OutputBoxPrintLn("Regenerating ME" + WhichGame + " thumbnails...");
                StatusUpdater.UpdateText("Regenerating thumbnails...  0 of " + texCount);

                backbone.AddToBackBone(b =>
                {
                    // KFreon: Find all textures inside each file
                    Dictionary<string, List<int>> files = new Dictionary<string, List<int>>();
                    int count = 0;
                    foreach (TreeTexInfo treetex in Tree.GetTreeAsList())
                    {
                        if (!files.ContainsKey(treetex.Files[0]))
                        {
                            List<int> texinds = new List<int>();
                            texinds.Add(count);
                            files.Add(treetex.Files[0], texinds);
                        }
                        else
                        {
                            List<int> texinds = files[treetex.Files[0]];
                            texinds.Add(count);
                            files[treetex.Files[0]] = texinds;
                        }
                        count++;
                    }

                    // KFreon: Delete old thumbnails
                    if (Directory.Exists(ThumbnailPath))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            try
                            {
                                Directory.Delete(ThumbnailPath, true);
                                System.Threading.Thread.Sleep(100);
                                DebugOutput.PrintLn("Successfully deleted old thumbnails.");
                                break;
                            }
                            catch
                            {
                                DebugOutput.PrintLn("Failed to delete old thumbnails. Sleeping for a bit before trying again.");
                                System.Threading.Thread.Sleep(500);
                            }
                        }
                    }

                    // KFreon: Create directory
                    Directory.CreateDirectory(ThumbnailPath);

                    // KFreon: Generate thumbnails for all textures in file
                    ParallelOptions po = new ParallelOptions();
                    po.MaxDegreeOfParallelism = NumThreads;
                    count = 0;
                    object locker = new object();



                    int curnt = 0;
                    foreach (string file in files.Keys)
                        curnt += files[file].Count;





                    Parallel.ForEach(files.Keys, po, file =>
                    {
                        PCCObjects.IPCCObject pcc = KFreonLib.PCCObjects.Creation.CreatePCCObject(file, WhichGame);

                        List<int> texinds = files[file];

                        // KFreon: Generate thumbnails for each texture inside this file
                        for (int i = 0; i < texinds.Count; i++)
                        {
                            TreeTexInfo tex = Tree.GetTex(texinds[i]);

                            for (int j = 0; j < 3; j++)
                            {
                                try
                                {
                                    Textures.ITexture2D tex2D = KFreonLib.Textures.Creation.CreateTexture2D(tex.Textures[0], WhichGame, pathBIOGame);
                                    tex.ThumbnailPath = KFreonLib.Textures.Creation.GenerateThumbnail(tex2D.GetImage(), tex.ThumbnailPath ?? Path.Combine(ThumbnailPath, tex.ThumbName), ExecFolder);
                                    DebugOutput.PrintLn("Generated thumbnail at: " + tex.ThumbnailPath);
                                    ProgBarUpdater.IncrementBar();

                                    // KFreon: Update status
                                    lock (locker)
                                    {
                                        count++;
                                        if (count % 10 == 0)
                                            StatusUpdater.UpdateText("Regenerating thumbnails...  " + count + " of " + texCount);
                                    }
                                    break;
                                }
                                catch
                                {
                                    DebugOutput.PrintLn("Failed to generate thumbnail from: " + tex.TexName + ". Sleeping before trying again.");
                                    System.Threading.Thread.Sleep(100);
                                }
                            }
                        }
                    });
                    ProgBarUpdater.ChangeProgressBar(1, 1);
                    StatusUpdater.UpdateText("Thumbnails Regenerated.");
                    OutputBoxPrintLn("Thumbnails Regenerated");
                    if (!Tree.AdvancedFeatures && MessageBox.Show("Your current tree doesn't have advanced features enabled. Do you want to save these features to your current tree?", "You probably want to do this", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                    {
                        for (int i = 0; i < 3; i++)
                            try
                            {
                                Tree.WriteToFile(Tree.TreePath, Path.GetDirectoryName(pathBIOGame));
                                DebugOutput.PrintLn("Tree saved with advanced features");
                                break;
                            }
                            catch
                            {
                                DebugOutput.PrintLn("Tree in use. Sleeping for a bit.");
                                System.Threading.Thread.Sleep(200);
                            }
                    }
                    return true;
                });
            }
        }

        private void RegenerateThumbnail(TreeTexInfo tex, int index, bool FromFile)
        {
            // KFreon: Try to delete old thumbnail
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    if (File.Exists(tex.ThumbnailPath))
                    {
                        File.Delete(tex.ThumbnailPath);
                        DebugOutput.PrintLn("Deleted old thumbnail successfully at: " + tex.ThumbnailPath);
                    }
                    break;
                }
                catch
                {
                    this.Invoke(new Action(() => ListViewImageList.Images.Clear()));
                    System.Threading.Thread.Sleep(200);
                }
            }


            // KFreon: Update tex details if necessary
            Textures.ITexture2D tex2D = null;
            UpdateTexDetails(index, out tex, out tex2D);

            string thumbpath = tex.ThumbnailPath ?? Path.Combine(ThumbnailPath, tex.ThumbName);

            if (FromFile)
                Textures.Creation.GenerateThumbnail(tex.Files[0], WhichGame, tex.ExpIDs[0], pathBIOGame, thumbpath, ExecFolder);
            else
                Textures.Creation.GenerateThumbnail(tex2D.GetImage(), thumbpath, ExecFolder);
            Tree.ReplaceTex(index, tex);
        }

        private void RegenerateButton_Click(object sender, EventArgs e)
        {
            if (MainListView.SelectedIndices.Count != 0 && MainListView.SelectedIndices[0] >= 0)
            {
                MainProgressBar.Value = 0;
                int ind = GetSelectedTexInd();
                RegenerateThumbnail(Tree.GetTex(ind), ind, true);
                UpdateThumbnailDisplays(MainTreeView.SelectedNode as myTreeNode);
                StatusUpdater.UpdateText("Thumbnail Regenerated!");
                MainProgressBar.Value = MainProgressBar.Maximum;
            }
        }

        private void ReplaceButton_Click(object sender, EventArgs e)
        {
            if (MainListView.SelectedIndices.Count != 0 && MainListView.SelectedIndices[0] >= 0)
            {
                // KFreon: Select mip to replace
                List<string> names = new List<string>();
                int index = GetSelectedTexInd();
                TreeTexInfo tex = Tree.GetTex(index);
                Textures.ITexture2D tex2D = tex.Textures[0];
                for (int i = 0; i < tex2D.imgList.Count; i++)
                {
                    bool pccstored = tex2D.imgList[i].CompareStorage("pccSto");
                    names.Add("Image: " + tex2D.imgList[i].imgSize + " stored inside " + (pccstored ? "PCC file" : "Archive file") + " at offset " + ((pccstored) ? (tex2D.imgList[i].offset + tex2D.pccOffset).ToString() : tex2D.imgList[i].offset.ToString()));
                }

                string selectedImage = "";
                using (Helpers.SelectionForm sf = new Helpers.SelectionForm(names, "Select ONE image to replace.", "Replace Image Selector", false))
                {
                    sf.ShowDialog();
                    if (sf.SelectedInds.Count == 0)
                        return;
                    else if (sf.SelectedInds.Count == 1)
                        selectedImage = sf.SelectedItems[0];
                    else
                    {
                        MessageBox.Show("You must select ONLY ONE image to replace.");
                        return;
                    }
                }
                string imgsize = selectedImage.Split(' ')[1];
                string replacingfile = "";
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Select Image to replace with";
                    ofd.Filter = "Image File|*.dds";

                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        replacingfile = ofd.FileName;
                    else
                        return;
                }

                byte[] imgData = File.ReadAllBytes(replacingfile);
                ImageMipMapHandler mip = new ImageMipMapHandler("", imgData);
                string[] sizes = imgsize.Split('x');
                ImageFile im = mip.imageList.First(img => img.imgSize.width.ToString() == sizes[0] && img.imgSize.height.ToString() == sizes[1]);

                DebugOutput.PrintLn("Replacing image in " + tex.TexName + " at " + imgsize + " with " + replacingfile);

                tex2D.replaceImage(imgsize, im, pathBIOGame);
                UpdateModifiedTex(tex2D, tex, index);
                DebugOutput.PrintLn("Image replaced.");

                StatusUpdater.UpdateText("Image replaced!");
                MainProgressBar.Value = MainProgressBar.Maximum;

                DisplayTextureProperties(tex2D, tex2D.GenerateImageInfo());

                RegenerateThumbnail(tex, index, false);

                if (ModMakerMode)
                {
                    AddModJob(tex2D, replacingfile);
                    StatusUpdater.UpdateText("Replacement complete and job added to modmaker!");
                }

                if (TPFMode)
                {
                    AddTPFToolsJob(replacingfile, tex.Hash);
                    StatusUpdater.UpdateText("Replacement Complete and job added to TPFTools!");
                }
            }
        }

        private async void AddModJob(Textures.ITexture2D tex2D, string replacingfile)
        {
            ModMaker modmaker = await ModMaker.GetCurrentInstance();
            KFreonLib.Scripting.ModMaker.AddJob(tex2D, replacingfile, WhichGame, pathBIOGame);
            modmaker.ExternalRefresh();
        }

        private void ExtractButton_Click(object sender, EventArgs e)
        {
            if (MainListView.SelectedIndices.Count != 0 && MainListView.SelectedIndices[0] >= 0)
            {
                ProgBarUpdater.ChangeProgressBar(0, 1);

                // KFreon: Select mip to replace
                List<string> names = new List<string>();
                int index = GetSelectedTexInd();
                TreeTexInfo tex = Tree.GetTex(index);
                Textures.ITexture2D tex2D = tex.Textures[0];
                for (int i = 0; i < tex2D.imgList.Count; i++)
                {
                    bool pccstored = tex2D.imgList[i].CompareStorage("pccSto");

                    // KFreon: Ignore null entries
                    int offset = (int)((pccstored) ? tex2D.imgList[i].offset + tex2D.pccOffset : tex2D.imgList[i].offset);
                    if (offset != -1)
                        names.Add("Image: " + tex2D.imgList[i].imgSize + " stored inside " + (pccstored ? "PCC file" : "Archive file") + " at offset " + offset.ToString());
                }

                string selectedImage = "";
                using (Helpers.SelectionForm sf = new Helpers.SelectionForm(names, "Select ONE image to extract.", "Extract Image Selector", false))
                {
                    sf.ShowDialog();
                    if (sf.SelectedInds.Count == 0)
                        return;
                    else if (sf.SelectedInds.Count == 1)
                        selectedImage = sf.SelectedItems[0];
                    else
                    {
                        MessageBox.Show("You must select ONLY ONE image to extract.");
                        return;
                    }
                }
                string imgsize = selectedImage.Split(' ')[1];
                string savepath = "";
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Select destination for file. Hash will be appended to filename.";
                    sfd.FileName = tex.TexName + Textures.Methods.FormatTexmodHashAsString(tex.Hash) + ".dds";
                    sfd.Filter = "DDS Files|*.dds";
                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        savepath = sfd.FileName;
                    else
                        return;
                }

                // KFreon: Make sure hash is there
                if (!savepath.Contains(Textures.Methods.FormatTexmodHashAsString(tex.Hash)))
                    savepath = Path.Combine(Path.GetDirectoryName(savepath), Path.GetFileNameWithoutExtension(savepath) + "_" + Textures.Methods.FormatTexmodHashAsString(tex.Hash) + Path.GetExtension(savepath));

                // KFreon: Save file
                File.WriteAllBytes(savepath, tex2D.extractImage(imgsize, false));

                StatusUpdater.UpdateText("Image extracted and saved!");
                ProgBarUpdater.ChangeProgressBar(1, 1);
            }
        }

        private void UpscaleButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("If anyone uses this, let KFreon know and I'll look at adding it.");
        }

        private void NoRenderButton_Click(object sender, EventArgs e)
        {
            if (WhichGame == 1 && MainListView.SelectedIndices.Count != 0 && MainListView.SelectedIndices[0] >= 0)
            {
                MainProgressBar.Value = 0;
                int index = GetSelectedTexInd();
                TreeTexInfo tex = Tree.GetTex(index);
                Textures.ITexture2D tex2D = tex.Textures[0];

                tex2D.NoRenderFix = true;
                UpdateModifiedTex(tex2D, tex, index);
                StatusUpdater.UpdateText("No Render fix applied!");
                OutputBoxPrintLn("No render fix applied to " + tex.TexName);
                MainProgressBar.Value = MainProgressBar.Maximum;
            }
        }

        private void LowResButton_Click(object sender, EventArgs e)
        {
            if (WhichGame == 1 && MainListView.SelectedIndices.Count != 0 && MainListView.SelectedIndices[0] >= 0)
            {
                MainProgressBar.Value = 0;
                int index = GetSelectedTexInd();
                TreeTexInfo tex = Tree.GetTex(index);
                Textures.ITexture2D tex2D = tex.Textures[0];

                // KFreon: Single Image check
                if (tex2D.imgList.Count <= 1)
                {
                    StatusUpdater.UpdateText("Don't need to fix a single level texture. Don't even worry about it");
                    OutputBoxPrintLn("Texture: " + tex.TexName + " is single level. No fix required or applied.");
                    return;
                }

                tex2D.LowResFix();
                UpdateModifiedTex(tex2D, tex, index);

                StatusUpdater.UpdateText("Low res patch applied!");
                OutputBoxPrintLn("Low res patch applied to " + tex.TexName);
                MainProgressBar.Value = MainProgressBar.Maximum;
            }
        }

        private void startTPFModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!TPFMode && MessageBox.Show("This mode will capture all modifications and send them to an instance of TPFTools." + Environment.NewLine + "NOTE: No modifications will be made to your files in this mode. Proceed?", "How bout we go that way?", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
            {
                startTPFModeToolStripMenuItem.Text = "Stop TPF Mode";
                TPFMode = true;
            }
            else if (TPFMode)
            {
                startTPFModeToolStripMenuItem.Text = "Start TPF Mode";
                TPFMode = false;
            }
        }

        private void PCCBoxContext_Click(object sender, EventArgs e)
        {
            string savepath = "";
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Select destination for PCC list";
                sfd.Filter = "Text Files|*.txt";

                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    savepath = sfd.FileName;
                else
                    return;
            }

            TreeTexInfo tex = Tree.GetTex(GetSelectedTexInd());
            using (StreamWriter sw = new StreamWriter(savepath))
                foreach (string pccname in tex.Files)
                    sw.WriteLine(pccname);

            MessageBox.Show("PCC list exported!");
        }

        public void removeTopTexture(string texname, List<string> pccs, List<int> IDs)
        {
            // KFreon: WHY does this function exist...
        }

        private async void MainListView_MouseDown(object sender, MouseEventArgs e)
        {
            await Task.Delay(50);
            if (!DisableContext && MainListView.SelectedIndices.Count != 0 && MainListView.SelectedIndices[0] != -1)
                ShowContextPanel(true);
            else
                ShowContextPanel(false);
        }

        private void treeIOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Helpers.KFreonMessageBox box = new Helpers.KFreonMessageBox("Tree Input/Output GUI", "Do you want to import or export a tree?", "Import", MessageBoxIcon.Information, "Export", "Cancel"))
            {
                DialogResult dr = box.ShowDialog();
                if (dr == System.Windows.Forms.DialogResult.Abort)
                    ExportTree();
                else if (dr == System.Windows.Forms.DialogResult.OK)
                    ImportTree();
            }
        }

        private void changePathsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // KFreon: Populate BIOGame entries
            List<string> biogames = new List<string>();
            for (int i = 1; i < 4; i++)
            {
                if (i == WhichGame)
                    biogames.Add(pathBIOGame);
                else
                    biogames.Add("");
            }

            // KFreon: Display PathChanger
            using (Helpers.PathChanger changer = new Helpers.PathChanger(biogames[0], biogames[1], biogames[2]))
            {
                if (changer.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    return;

                #region Change Paths and Save
                // KFreon: Change paths
                MEExDirecs.SetPaths(changer.PathME1, changer.PathME2, changer.PathME3);

                // KFreon: Get paths again
                CheckGameStates();
                #endregion
            }
        }

        private void AddDLCToTree(List<string> pccs)
        {
            Tree.AddPCCs(pccs);
            ScanPCCList(false, pccs);
            Tree.WriteToFile(Tree.TreePath, Path.GetDirectoryName(pathBIOGame));
        }

        private void addDLCToTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> pccs = new List<string>();

            // KFreon: Scan DLC PCC's


            AddDLCToTree(pccs);
        }
    }
}
