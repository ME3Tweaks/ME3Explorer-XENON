using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ME3Explorer.Unreal;
using ME3Explorer.Unreal.Classes;
using ME3Explorer.SequenceObjects;
using ME3Explorer.TOCUpdater;

using UMD.HCIL.Piccolo;
using UMD.HCIL.Piccolo.Nodes;
using UMD.HCIL.Piccolo.Event;
using UMD.HCIL.Piccolo.Util;
using KFreonLib.MEDirectories;


namespace ME3Explorer.InterpEditor
{
    public partial class InterpEditor : Form
    {
        public PCCObject pcc;
        public TalkFile talkfile;
        public string CurrentFile;
        public List<int> objects;

        public InterpEditor()
        {
            SText.fontcollection = LoadFont("KismetFont.ttf", 8);
            InitializeComponent();
            timeline.Scrollbar = vScrollBar1;
            timeline.GroupList.ScrollbarH = hScrollBar1;
            timeline.GroupList.tree1 = treeView1;
            timeline.GroupList.tree2 = treeView2;
            BitConverter.IsLittleEndian = true;
            objects = new List<int>();
            talkfile = new TalkFile();
            talkfile.LoadTlkData(ME3Directory.cookedPath + "BIOGame_INT.tlk");
        }

        private void openPCCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "PCC Files(*.pcc)|*.pcc";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadPCC(d.FileName);
            }
        }

        public void LoadPCC(string fileName)
        {
            objects.Clear();
            pcc = new PCCObject(fileName);
            CurrentFile = fileName;
            for (int i = 0; i < pcc.Exports.Count; i++)
                if (pcc.Exports[i].ClassName == "SeqAct_Interp")
                    objects.Add(i);
            RefreshCombo();
        }

        public void RefreshCombo()
        {
            if (objects == null)
                return;
            toolStripComboBox1.Items.Clear();
            foreach (int i in objects)
                toolStripComboBox1.Items.Add("#" + i + " : " + pcc.Exports[i].ObjectName);
            if (toolStripComboBox1.Items.Count != 0)
                toolStripComboBox1.SelectedIndex = 0;
        }

        public void loadInterpData(int index)
        {
            timeline.GroupList.LoadInterpData(index, pcc);
            timeline.GroupList.OnCameraChanged(timeline.Camera);
            timeline.GroupList.Talkfile = talkfile;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            int n = toolStripComboBox1.SelectedIndex;
            if (n == -1)
                return;
            SAction interp = new SAction(objects[n], 0, 0, pcc);
            interp.Layout(0,0);
            int dataIndex = interp.Varlinks[0].Links[0];
            loadInterpData(dataIndex);
        }
        public static PrivateFontCollection LoadFont(string file, int fontSize)
        {
            PrivateFontCollection fontCollection = new PrivateFontCollection();
            fontCollection.AddFontFile(file);
            if (fontCollection.Families.Length < 0)
            {
                throw new InvalidOperationException("No font familiy found when loading font");
            }
            return fontCollection;
        }

        //private void openInPCCEditor2ToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    int l = CurrentObjects[listBox1.SelectedIndex];
        //    if (l == -1)
        //        return;
        //    PCCEditor2 p = new PCCEditor2();
        //    p.MdiParent = this.MdiParent;
        //    p.WindowState = FormWindowState.Maximized;
        //    p.Show();
        //    p.pcc = new PCCObject(CurrentFile);
        //    p.SetView(2);
        //    p.RefreshView();
        //    p.InitStuff();
        //    p.listBox1.SelectedIndex = l;
        //}
    }
}
