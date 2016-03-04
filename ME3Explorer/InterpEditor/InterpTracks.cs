using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Linq;

using ME3Explorer;
using ME3Explorer.Unreal;
using ME3Explorer.SequenceObjects;

using UMD.HCIL.Piccolo;
using UMD.HCIL.Piccolo.Nodes;
using UMD.HCIL.Piccolo.Event;
using UMD.HCIL.Piccolo.Util;
using UMD.HCIL.PiccoloX;
using UMD.HCIL.PiccoloX.Nodes;
using UMD.HCIL.PiccoloX.Components;

namespace ME3Explorer.InterpEditor
{
    public abstract class InterpTrack
    {
        private static Brush TrackListBrush = Brushes.DarkGray;
        private static Pen ChildLinePen = new Pen(Color.FromArgb(49, 49, 49));

        public struct InterpCurveVector //DistributionVectorConstantCurve
        {
            public struct InterpCurvePointVector
            {
                public float InVal;
                public float[] OutVal;
                public float[] ArriveTangent;
                public float[] LeaveTangent;
                public int InterpMode_Type;
                public int InterpMode_Value;

                public TreeNode ToTree(int index, PCCObject pcc, float time = -1)
                {
                    TreeNode root = new TreeNode(index + " : " + (time == -1 ? InVal : time));
                    root.Nodes.Add(new TreeNode("InVal : " + InVal));
                    TreeNode t = new TreeNode("OutVal");
                    t.Nodes.Add("X : " + OutVal[0]);
                    t.Nodes.Add("Y : " + OutVal[1]);
                    t.Nodes.Add("Z : " + OutVal[2]);
                    root.Nodes.Add(t);
                    t = new TreeNode("ArriveTangent");
                    t.Nodes.Add("X : " + ArriveTangent[0]);
                    t.Nodes.Add("Y : " + ArriveTangent[1]);
                    t.Nodes.Add("Z : " + ArriveTangent[2]);
                    root.Nodes.Add(t);
                    t = new TreeNode("LeaveTangent");
                    t.Nodes.Add("X : " + LeaveTangent[0]);
                    t.Nodes.Add("Y : " + LeaveTangent[1]);
                    t.Nodes.Add("Z : " + LeaveTangent[2]);
                    root.Nodes.Add(t);
                    root.Nodes.Add(new TreeNode("InterpMode : " + pcc.getNameEntry(InterpMode_Type) + ", " + pcc.getNameEntry(InterpMode_Value)));
                    return root;
                }
            }
            public List<InterpCurvePointVector> Points;

            public TreeNode ToTree(string name, PCCObject pcc, List<float> times = null)
            {
                TreeNode root = new TreeNode(name);
                TreeNode t = new TreeNode("Points");
                for (int i = 0; i < Points.Count; i++)
                {
                    if (times == null)
                        t.Nodes.Add(Points[i].ToTree(i, pcc));
                    else
                        t.Nodes.Add(Points[i].ToTree(i, pcc, times[i]));
                }
                root.Nodes.Add(t);
                return root;
            }
        }

        public struct InterpCurveFloat //DistributionFloatConstantCurve
        {
            public struct InterpCurvePointFloat
            {
                public float InVal;
                public float OutVal;
                public float ArriveTangent;
                public float LeaveTangent;
                public int InterpMode_Type;
                public int InterpMode_Value;

                public TreeNode ToTree(int index, PCCObject pcc)
                {
                    TreeNode root = new TreeNode(index + " : " + InVal);
                    root.Nodes.Add(new TreeNode("InVal : " + InVal));
                    root.Nodes.Add(new TreeNode("OutVal : " + OutVal));
                    root.Nodes.Add(new TreeNode("ArriveTangent : " + ArriveTangent));
                    root.Nodes.Add(new TreeNode("LeaveTangent : " + LeaveTangent));
                    root.Nodes.Add(new TreeNode("InterpMode : " + pcc.getNameEntry(InterpMode_Type) + ", " + pcc.getNameEntry(InterpMode_Value)));
                    return root;
                }
            }
            public List<InterpCurvePointFloat> Points;

            public TreeNode ToTree(string name, PCCObject pcc)
            {
                TreeNode root = new TreeNode(name);
                TreeNode t = new TreeNode("Points");
                for (int i = 0; i < Points.Count; i++)
                    t.Nodes.Add(Points[i].ToTree(i, pcc));
                root.Nodes.Add(t);
                return root;
            }
        }

        public TreeView propView;
        public TreeView keyPropView;
        public TalkFile talkfile;
        public PCCObject pcc;
        public int index;

        private SText title;
        public PPath listEntry;
        public PNode timelineEntry;
        public List<PPath> keys;
        public bool Visible
        {
            get
            {
                return listEntry.Visible;
            }
            set
            {
                listEntry.Visible = value;
                timelineEntry.Visible = value;
                listEntry.Pickable = value;
                timelineEntry.Pickable = value;
            }
        }

        public int m_eFindActorMode_Type;
        public int m_eFindActorMode_Value;
        public string TrackTitle
        {
            get
            {
                return title.Text;
            }
            set
            {
                title.Text = value;
            }
        }
        public bool bImportedTrack;

        public InterpTrack(int idx, PCCObject pccobj)
        {
            index = idx;
            pcc = pccobj;

            title = new SText("");
            listEntry = PPath.CreateRectangle(0, 0, Timeline.ListWidth, Timeline.TrackHeight);
            listEntry.Brush = TrackListBrush;
            listEntry.Pen = null;
            listEntry.MouseDown += listEntry_MouseDown;
            PPath p = PPath.CreateLine(9, 2, 9, 12);
            p.AddLine(9, 12, 31, 12);
            p.Brush = null;
            listEntry.AddChild(p);
            listEntry.AddChild(PPath.CreateLine(0, listEntry.Bounds.Bottom, Timeline.ListWidth, listEntry.Bounds.Bottom));
            title.TranslateBy(30, 3);
            listEntry.AddChild(title);
            timelineEntry = new PNode();
            //timelineEntry.Brush = Brushes.Green;
            LoadGenericData();
        }

        public void LoadGenericData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "TrackTitle")
                    TrackTitle = p.Value.StringValue;
                if (pcc.getNameEntry(p.Name) == "bImportedTrack")
                    bImportedTrack = p.Value.IntValue != 0;
                if (pcc.getNameEntry(p.Name) == "m_eFindActorMode")
                    GetByteVal(p.raw, out m_eFindActorMode_Type, out m_eFindActorMode_Value);
            }
        }

        public virtual void GetKeyFrames() { }

        protected PPath GenerateKeyFrame(float time)
        {
            PPath p = PPath.CreatePolygon(new PointF[] { new PointF(-7, 7), new PointF(0, 0), new PointF(7, 7) });
            //p.Pickable = false;
            p.Pen = null;
            p.Brush = new SolidBrush(Color.FromArgb(100, 0, 0));
            p.Tag = time;
            p.MouseDown += p_MouseDown;
            return p;
        }

        public virtual void DrawKeyFrames()
        {
            foreach (PPath k in keys)
            {
                timelineEntry.AddChild(k);
                k.TranslateBy((float)k.Tag * 60 + 60, 13);
            }
            timelineEntry.Height = Timeline.TrackHeight;
            if (timelineEntry.ChildrenCount != 0)
                timelineEntry.Width = (float)timelineEntry[timelineEntry.ChildrenCount - 1].OffsetX + 10;
        }

        protected virtual void p_MouseDown(object sender, PInputEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                ContextMenuStrip menu = new ContextMenuStrip();
                ToolStripMenuItem setTime = new ToolStripMenuItem("SetTime");
                setTime.Click += setTime_Click;
                ToolStripMenuItem deleteKey = new ToolStripMenuItem("DeleteKey");
                deleteKey.Click += deleteKey_Click;
                menu.Items.AddRange(new ToolStripItem[] { setTime, deleteKey });
                menu.Show(Cursor.Position);
            }
        }

        void deleteKey_Click(object sender, EventArgs e)
        {

        }

        void setTime_Click(object sender, EventArgs e)
        {

        }

        void listEntry_MouseDown(object sender, PInputEventArgs e)
        {
            e.Handled = true;
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                ContextMenuStrip menu = new ContextMenuStrip();
                ToolStripMenuItem openInPCCEd = new ToolStripMenuItem("Open in PCCEditor2");
                openInPCCEd.Click += openInPCCEd_Click;
                menu.Items.AddRange(new ToolStripItem[] { openInPCCEd });
                menu.Show(Cursor.Position);
			}
			Interpreter2.Interpreter2 ip = new Interpreter2.Interpreter2();
			ip.pcc = pcc;
			ip.Index = index;
			ip.InitInterpreter(talkfile);
			//ip.Show();
			propView.Nodes.Add(ip.Scan());
            //ToTree();
			ip.Dispose();
        }

        void openInPCCEd_Click(object sender, EventArgs e)
        {
            PCCEditor2 p = new PCCEditor2();
            //p.MdiParent = Form.MdiParent;
            p.WindowState = FormWindowState.Maximized;
            p.Show();
            p.pcc = new PCCObject(pcc.pccFileName);
            p.SetView(2);
            p.RefreshView();
            p.InitStuff();
            p.listBox1.SelectedIndex = index;
        }

        public virtual void ToTree()
        {
            propView.Nodes.Clear();
            TreeNode t = new TreeNode("Track Title : \"" + TrackTitle + "\"");
            t.Name = "TrackTitle";
            propView.Nodes.Add(t);
        }

        #region helper methods

        public static void GetByteVal(byte[] raw, out int t, out int v)
        {

            t = BitConverter.ToInt32(raw, 24);
            v = BitConverter.ToInt32(raw, 32);
        }

        public static InterpCurveVector GetCurveVector(PropertyReader.Property p, PCCObject pcc)
        {
            InterpCurveVector vec = new InterpCurveVector();
            vec.Points = new List<InterpCurveVector.InterpCurvePointVector>();
            int pos = 60;
            int count = BitConverter.ToInt32(p.raw, 56);
            for (int j = 0; j < count; j++)
            {
                List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                InterpCurveVector.InterpCurvePointVector point = new InterpCurveVector.InterpCurvePointVector();
                for (int i = 0; i < p2.Count(); i++)
                {
                    if (pcc.getNameEntry(p2[i].Name) == "InVal")
                        point.InVal = BitConverter.ToSingle(p2[i].raw, 24);
                    else if (pcc.getNameEntry(p2[i].Name) == "OutVal")
                    {
                        point.OutVal = new float[3];
                        point.OutVal[0] = BitConverter.ToSingle(p2[i].raw, 32);
                        point.OutVal[1] = BitConverter.ToSingle(p2[i].raw, 36);
                        point.OutVal[2] = BitConverter.ToSingle(p2[i].raw, 40);

                    }
                    else if (pcc.getNameEntry(p2[i].Name) == "ArriveTangent")
                    {
                        point.ArriveTangent = new float[3];
                        point.ArriveTangent[0] = BitConverter.ToSingle(p2[i].raw, 32);
                        point.ArriveTangent[1] = BitConverter.ToSingle(p2[i].raw, 36);
                        point.ArriveTangent[2] = BitConverter.ToSingle(p2[i].raw, 40);

                    }
                    else if (pcc.getNameEntry(p2[i].Name) == "LeaveTangent")
                    {
                        point.LeaveTangent = new float[3];
                        point.LeaveTangent[0] = BitConverter.ToSingle(p2[i].raw, 32);
                        point.LeaveTangent[1] = BitConverter.ToSingle(p2[i].raw, 36);
                        point.LeaveTangent[2] = BitConverter.ToSingle(p2[i].raw, 40);

                    }
                    else if (pcc.getNameEntry(p2[i].Name) == "InterpMode")
                        GetByteVal(p2[i].raw, out point.InterpMode_Type, out point.InterpMode_Value);
                    pos += p2[i].raw.Length;
                }
                vec.Points.Add(point);
            }
            return vec;
        }

        public static InterpCurveFloat GetCurveFloat(PropertyReader.Property p, PCCObject pcc)
        {
            InterpCurveFloat CurveFloat = new InterpCurveFloat();
            CurveFloat.Points = new List<InterpCurveFloat.InterpCurvePointFloat>();
            int pos = 60;
            int count = BitConverter.ToInt32(p.raw, 56);
            for (int j = 0; j < count; j++)
            {
                List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                InterpCurveFloat.InterpCurvePointFloat point = new InterpCurveFloat.InterpCurvePointFloat();
                for (int i = 0; i < p2.Count(); i++)
                {
                    if (pcc.getNameEntry(p2[i].Name) == "InVal")
                        point.InVal = BitConverter.ToSingle(p2[i].raw, 24);
                    else if (pcc.getNameEntry(p2[i].Name) == "OutVal")
                    {
                        point.OutVal = BitConverter.ToSingle(p2[i].raw, 24);

                    }
                    else if (pcc.getNameEntry(p2[i].Name) == "ArriveTangent")
                    {
                        point.ArriveTangent = BitConverter.ToSingle(p2[i].raw, 24);

                    }
                    else if (pcc.getNameEntry(p2[i].Name) == "LeaveTangent")
                    {
                        point.LeaveTangent = BitConverter.ToSingle(p2[i].raw, 24);

                    }
                    else if (pcc.getNameEntry(p2[i].Name) == "InterpMode")
                        GetByteVal(p2[i].raw, out point.InterpMode_Type, out point.InterpMode_Value);
                    pos += p2[i].raw.Length;
                }
                CurveFloat.Points.Add(point);
            }
            return CurveFloat;
        }

        #endregion
    }

    public abstract class BioInterpTrack : InterpTrack
    {

        public struct TrackKey
        {
            public int KeyName;
            public float fTime;

            public TreeNode ToTree(int index, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + " : " + fTime);
                root.Nodes.Add(new TreeNode("KeyName : " + pcc.getNameEntry(KeyName)));
                root.Nodes.Add(new TreeNode("fTime : " + fTime));
                return root;
            }
        }

        public List<TrackKey> m_aTrackKeys;

        public BioInterpTrack(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            GetKeyFrames();

        }
        public void LoadData()
        {   //default values
            m_aTrackKeys = new List<TrackKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aTrackKeys")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        TrackKey key = new TrackKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "KeyName")
                                key.KeyName = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "fTime")
                                key.fTime = BitConverter.ToSingle(p2[i].raw, 24);
                            pos += p2[i].raw.Length;
                        }
                        m_aTrackKeys.Add(key);
                    }
                }
            }
        }

        public override void GetKeyFrames()
        {
            keys = new List<PPath>();
            if (m_aTrackKeys != null)
                foreach (TrackKey k in m_aTrackKeys)
                    keys.Add(GenerateKeyFrame(k.fTime));
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aTrackKeys");
            for (int i = 0; i < m_aTrackKeys.Count; i++)
                t.Nodes.Add(m_aTrackKeys[i].ToTree(i, pcc));
            propView.Nodes.Add(t);
        }
    }

    public abstract class SFXGameActorInterpTrack : BioInterpTrack
    {
        public int m_nmFindActor;
        public int m_eFindActorMode_Type;
        public int m_eFindActorMode_Value;

        public SFXGameActorInterpTrack(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
        }

        public void LoadData()
        {   //default values
            m_nmFindActor = -1;
            m_eFindActorMode_Type = -1;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_nmFindActor")
                    m_nmFindActor = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "m_eFindActorMode")
                    GetByteVal(p.raw, out m_eFindActorMode_Type, out m_eFindActorMode_Value);
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            if (m_nmFindActor != -1)
                propView.Nodes.Add("m_nmFindActor : " + pcc.getNameEntry(m_nmFindActor));
            if (m_eFindActorMode_Type != -1)
                propView.Nodes.Add("m_eFindActorMode : " + pcc.getNameEntry(m_eFindActorMode_Type) + ", " + pcc.getNameEntry(m_eFindActorMode_Value));
        }
    }

    public abstract class SFXInterpTrackMovieBase : BioInterpTrack
    {
        public struct MovieKey
        {
            public int PlaceHolder;
            public int m_eState_Type;
            public int m_eState_Value;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + " : " + time);
                root.Nodes.Add(new TreeNode("PlaceHolder : " + PlaceHolder));
                root.Nodes.Add(new TreeNode("m_eState : " + pcc.getNameEntry(m_eState_Type) + ", " + pcc.getNameEntry(m_eState_Value)));
                return root;
            }
        }

        public List<MovieKey> m_aMovieKeyData;

        public SFXInterpTrackMovieBase(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
        }

        public void LoadData()
        {   //default values
            m_aMovieKeyData = new List<MovieKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aMovieKeyData")
                {
                    m_aMovieKeyData = new List<MovieKey>();
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        MovieKey key = new MovieKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "PlaceHolder")
                                key.PlaceHolder = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "m_eState")
                                GetByteVal(p2[i].raw, out key.m_eState_Type, out key.m_eState_Value);
                            pos += p2[i].raw.Length;
                        }
                        m_aMovieKeyData.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aMovieKeyData");
            for (int i = 0; i < m_aMovieKeyData.Count; i++)
                t.Nodes.Add(m_aMovieKeyData[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
        }
    }

    public abstract class SFXInterpTrackToggleBase : BioInterpTrack
    {
        public struct ToggleKey
        {
            public bool m_bToggle;
            public bool m_bEnable;

            public TreeNode ToTree(int index, float time)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("m_bToggle : " + m_bToggle));
                root.Nodes.Add(new TreeNode("m_bEnable : " + m_bEnable));
                return root;
            }
        }

        public List<ToggleKey> m_aToggleKeyData;
        public List<int> m_aTarget;
        public int m_TargetActor;

        public SFXInterpTrackToggleBase(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
        }

        public void LoadData()
        {   //default values
            m_aToggleKeyData = new List<ToggleKey>();
            m_aTarget = new List<int>();
            m_TargetActor = 0;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aTarget")
                {
                    int count2 = BitConverter.ToInt32(p.raw, 24);
                    for (int k = 0; k < count2; k++)
                    {
                        m_aTarget.Add(BitConverter.ToInt32(p.raw, 28 + k * 4));
                    }
                }
                else if (pcc.getNameEntry(p.Name) == "m_TargetActor")
                    m_TargetActor = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "m_aToggleKeyData")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        ToggleKey key = new ToggleKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "m_bToggle")
                                key.m_bToggle = p2[i].Value.IntValue != 0;
                            if (pcc.getNameEntry(p2[i].Name) == "m_bEnable")
                                key.m_bEnable = p2[i].Value.IntValue != 0;
                            pos += p2[i].raw.Length;
                        }
                        m_aToggleKeyData.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aToggleKeyData");
            for (int i = 0; i < m_aToggleKeyData.Count; i++)
                t.Nodes.Add(m_aToggleKeyData[i].ToTree(i, m_aTrackKeys[i].fTime));
            propView.Nodes.Add(t);
            t = new TreeNode("m_aTarget");
            for (int i = 0; i < m_aTarget.Count; i++)
                t.Nodes.Add(m_aTarget[i].ToString());
            propView.Nodes.Add(t);
            propView.Nodes.Add(new TreeNode("m_TargetActor : " + m_TargetActor));
        }
    }

    public abstract class InterpTrackFloatBase : InterpTrack
    {
        public InterpCurveFloat FloatTrack;

        public InterpTrackFloatBase(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            GetKeyFrames();
        }

        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "FloatTrack")
                    FloatTrack = GetCurveFloat(p, pcc);
            }
        }

        public override void GetKeyFrames()
        {
            keys = new List<PPath>();
            if (FloatTrack.Points != null)
                foreach (InterpCurveFloat.InterpCurvePointFloat e in FloatTrack.Points)
                    keys.Add(GenerateKeyFrame(e.InVal));
        }

        public override void ToTree()
        {
            propView.Nodes.Clear();
            propView.Nodes.Add(new TreeNode("Track Title : \"" + TrackTitle + "\""));
            propView.Nodes.Add(FloatTrack.ToTree("Float Track", pcc));
        }
    }

    public abstract class InterpTrackVectorBase : InterpTrack
    {
        public InterpCurveVector VectorTrack;

        public InterpTrackVectorBase(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            GetKeyFrames();
        }
        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "VectorTrack")
                    VectorTrack = GetCurveVector(p, pcc);
            }
        }

        public override void GetKeyFrames()
        {
            keys = new List<PPath>();
            if (VectorTrack.Points != null)
                foreach (InterpCurveVector.InterpCurvePointVector e in VectorTrack.Points)
                    keys.Add(GenerateKeyFrame(e.InVal));
        }

        public override void ToTree()
        {
            propView.Nodes.Clear();
            propView.Nodes.Add(new TreeNode("Track Title : \"" + TrackTitle + "\""));
            propView.Nodes.Add(VectorTrack.ToTree("Vector Track", pcc));
        }
    }

    public class BioInterpTrackMove : InterpTrackMove
    {
        public int FacingController;

        public BioInterpTrackMove(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Bio Movement";
        }

        public void LoadData()
        {   //default values
            FacingController = -1;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "FacingController")
                    FacingController = p.Value.IntValue;
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            propView.Nodes.Add(new TreeNode("FacingController : " + pcc.getNameEntry(FacingController)));
        }
    }

    public class BioScalarParameterTrack : InterpTrackFloatBase
    {
        public float InterpValue;
        public int PropertyName;
        public int m_pParentEffect;

        public BioScalarParameterTrack(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Scalar Param";
        }

        public void LoadData()
        {   //default values
            m_pParentEffect = 0;
            InterpValue = 0;
            PropertyName = -1;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "InterpValue")
                    InterpValue = BitConverter.ToSingle(p.raw, 24);
                else if (pcc.getNameEntry(p.Name) == "PropertyName")
                    PropertyName = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "m_pParentEffect")
                    m_pParentEffect = p.Value.IntValue;
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            propView.Nodes.Add(new TreeNode("PropertyName : " + PropertyName));
            propView.Nodes.Add(new TreeNode("InterpValue : " + InterpValue));
            propView.Nodes.Add(new TreeNode("m_pParentEffect : " + m_pParentEffect));
        }
    }

    public class BioEvtSysTrackInterrupt : BioInterpTrack
    {
        public struct InterruptKey
        {
            public bool bShowInterrupt;

            public TreeNode ToTree(int index, float time)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("bShowInterrupt : " + bShowInterrupt));
                return root;
            }
        }

        public List<InterruptKey> m_aInterruptData;

        public BioEvtSysTrackInterrupt(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Interrupt";

        }

        public void LoadData()
        {   //default values
            m_aInterruptData = new List<InterruptKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aInterruptData")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        InterruptKey key = new InterruptKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "bShowInterrupt")
                                key.bShowInterrupt = p2[i].Value.IntValue != 0;
                            pos += p2[i].raw.Length;
                        }
                        m_aInterruptData.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aInterruptData");
            for (int i = 0; i < m_aInterruptData.Count; i++)
                t.Nodes.Add(m_aInterruptData[i].ToTree(i, m_aTrackKeys[i].fTime));
            propView.Nodes.Add(t);
        }
    }

    public class BioEvtSysTrackSubtitles : BioInterpTrack
    {
        public struct SubtitleKey
        {
            public int nStrRefID;
            public float fLength;
            public bool bShowAtTop;
            public bool bUseOnlyAsReplyWheelHint;

            public TreeNode ToTree(int index, float time, PCCObject pcc, TalkFile tlk)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("nStrRefID : " + tlk.findDataById(nStrRefID)));
                root.Nodes.Add(new TreeNode("fLength : " + fLength));
                root.Nodes.Add(new TreeNode("bShowAtTop : " + bShowAtTop));
                root.Nodes.Add(new TreeNode("bUseOnlyAsReplyWheelHint : " + bUseOnlyAsReplyWheelHint));
                return root;
            }
        }

        public List<SubtitleKey> m_aSubtitleData;

        public BioEvtSysTrackSubtitles(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Subtitles";
            GetKeyFrames();
        }

        public void LoadData()
        {   //default values
            m_aSubtitleData = new List<SubtitleKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aSubtitleData")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        SubtitleKey key = new SubtitleKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "nStrRefID")
                                key.nStrRefID = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "fLength")
                                key.fLength = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "bShowAtTop")
                                key.bShowAtTop = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bUSeOnlyAsReplyWheelHint")
                                key.bUseOnlyAsReplyWheelHint = p2[i].Value.IntValue != 0;
                            pos += p2[i].raw.Length;
                        }
                        m_aSubtitleData.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aSubtitleData");
            for (int i = 0; i < m_aSubtitleData.Count; i++)
                t.Nodes.Add(m_aSubtitleData[i].ToTree(i, m_aTrackKeys[i].fTime, pcc, talkfile));
            propView.Nodes.Add(t);
        }
    }

    public class BioEvtSysTrackSwitchCamera : BioInterpTrack
    {
        public struct CameraSwitchKey
        {
            public int nmStageSpecificCam;
            public bool bForceCrossingLineOfAction;
            public bool bUseForNextCamera;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("nmStageSpecificCam : " + pcc.getNameEntry(nmStageSpecificCam)));
                root.Nodes.Add(new TreeNode("bForceCrossingLineOfAction : " + bForceCrossingLineOfAction));
                root.Nodes.Add(new TreeNode("bUseForNextCamera : " + bUseForNextCamera));
                return root;
            }
        }

        public List<CameraSwitchKey> m_aCameras;

        public BioEvtSysTrackSwitchCamera(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Switch Camera";

        }

        public void LoadData()
        {   //default values
            m_aCameras = new List<CameraSwitchKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aCameras")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        CameraSwitchKey key = new CameraSwitchKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "nmStageSpecificCam")
                                key.nmStageSpecificCam = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "bForceCrossingLineOfAction")
                                key.bForceCrossingLineOfAction = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bUseForNextCamera")
                                key.bUseForNextCamera = p2[i].Value.IntValue != 0;
                            pos += p2[i].raw.Length;
                        }
                        m_aCameras.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aCameras");
            for (int i = 0; i < m_aCameras.Count; i++)
                t.Nodes.Add(m_aCameras[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
        }
    }

    public class BioEvtSysTrackVOElements : BioInterpTrack
    {
        public int m_nStrRefID;
        public float m_fJCutOffset;

        public BioEvtSysTrackVOElements(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "VO Elements";
        }

        public void LoadData()
        {
            m_nStrRefID = 0;
            m_fJCutOffset = 0;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_nStrRefID")
                    m_nStrRefID = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "m_fJCutOffset")
                    m_fJCutOffset = BitConverter.ToSingle(p.raw, 24);
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            propView.Nodes.Add(new TreeNode("m_nStrRefID : " + talkfile.findDataById(m_nStrRefID)));
            propView.Nodes.Add(new TreeNode("m_fJCutOffset : " + m_fJCutOffset));
        }
    }

    public class BioInterpTrackRotationMode : BioInterpTrack
    {
        public struct RotationModeKey
        {
            public int FindActorTag; //name
            public float InterpTime;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("nmStageSpecificCam : " + pcc.getNameEntry(FindActorTag)));
                root.Nodes.Add(new TreeNode("bForceCrossingLineOfAction : " + InterpTime));
                return root;
            }
        }

        public List<RotationModeKey> EventTrack;

        public BioInterpTrackRotationMode(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Rotation Mode";

        }

        public void LoadData()
        {   //default values
            EventTrack = new List<RotationModeKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "EventTrack")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        RotationModeKey key = new RotationModeKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "FindActorTag")
                                key.FindActorTag = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "InterpTime")
                                key.InterpTime = BitConverter.ToSingle(p2[i].raw, 24);
                            pos += p2[i].raw.Length;
                        }
                        EventTrack.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("EventTrack");
            for (int i = 0; i < EventTrack.Count; i++)
                t.Nodes.Add(EventTrack[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
        }
    }

    public class BioEvtSysTrackGesture : SFXGameActorInterpTrack
    {
        public struct Gesture
        {
            public List<int> aChainedGestures;
            public int nmPoseSet;
            public int nmPoseAnim;
            public int nmGestureSet;
            public int nmGestureAnim;
            public int nmTransitionSet;
            public int nmTransitionAnim;
            public float fPlayRate;
            public float fStartOffset;
            public float fEndOffset;
            public float fStartBlendDuration;
            public float fWeight;
            public float fTransBlendTime;
            public bool bInvalidData;
            public bool bOneShotAnim;
            public bool bChainToPrevious;
            public bool bPlayUntilNext;
            public bool bTerminateAllGestures;
            public bool bUseDynAnimSets;
            public bool bSnapToPose;
            public int ePoseFilter_Type;
            public int ePoseFilter_Value;
            public int ePose_Type;
            public int ePose_Value;
            public int eGestureFilter_Type;
            public int eGestureFilter_Value;
            public int eGesture_Type;
            public int eGesture_Value;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                TreeNode t = new TreeNode("aChainedGestures");
                for (int i = 0; i < aChainedGestures.Count; i++)
                    t.Nodes.Add(aChainedGestures[i].ToString());
                root.Nodes.Add(t);
                root.Nodes.Add(new TreeNode("nmPoseSet : " + pcc.getNameEntry(nmPoseSet)));
                root.Nodes.Add(new TreeNode("nmPoseAnim : " + pcc.getNameEntry(nmPoseAnim)));
                root.Nodes.Add(new TreeNode("nmGestureSet : " + pcc.getNameEntry(nmGestureSet)));
                root.Nodes.Add(new TreeNode("nmGestureAnim : " + pcc.getNameEntry(nmGestureAnim)));
                root.Nodes.Add(new TreeNode("nmTransitionSet : " + pcc.getNameEntry(nmTransitionSet)));
                root.Nodes.Add(new TreeNode("nmTransitionAnim : " + pcc.getNameEntry(nmTransitionAnim)));
                root.Nodes.Add(new TreeNode("fPlayRate : " + fPlayRate));
                root.Nodes.Add(new TreeNode("fStartOffset : " + fStartOffset));
                root.Nodes.Add(new TreeNode("fEndOffset : " + fEndOffset));
                root.Nodes.Add(new TreeNode("fStartBlendDuration : " + fStartBlendDuration));
                root.Nodes.Add(new TreeNode("fWeight : " + fWeight));
                root.Nodes.Add(new TreeNode("fTransBlendTime : " + fTransBlendTime));
                root.Nodes.Add(new TreeNode("bInvalidData : " + bInvalidData));
                root.Nodes.Add(new TreeNode("bOneShotAnim : " + bOneShotAnim));
                root.Nodes.Add(new TreeNode("bChainToPrevious : " + bChainToPrevious));
                root.Nodes.Add(new TreeNode("bPlayUntilNext : " + bPlayUntilNext));
                root.Nodes.Add(new TreeNode("bTerminateAllGestures : " + bTerminateAllGestures));
                root.Nodes.Add(new TreeNode("bUseDynAnimSets : " + bUseDynAnimSets));
                root.Nodes.Add(new TreeNode("bSnapToPose : " + bSnapToPose));
                root.Nodes.Add(new TreeNode("ePoseFilter : " + pcc.getNameEntry(ePoseFilter_Type) + ", " + pcc.getNameEntry(ePoseFilter_Value)));
                root.Nodes.Add(new TreeNode("ePose : " + pcc.getNameEntry(ePose_Type) + ", " + pcc.getNameEntry(ePose_Value)));
                root.Nodes.Add(new TreeNode("eGestureFilter : " + pcc.getNameEntry(eGestureFilter_Type) + ", " + pcc.getNameEntry(eGestureFilter_Value)));
                root.Nodes.Add(new TreeNode("eGesture : " + pcc.getNameEntry(eGesture_Type) + ", " + pcc.getNameEntry(eGesture_Value)));
                return root;
            }
        }

        public List<Gesture> m_aGestures;
        public int nmStartingPoseSet = -1;
        public int nmStartingPoseAnim = -1;
        public float m_fStartPoseOffset;
        public bool m_bUseDynamicAnimsets;
        public int ePoseFilter_Type = -1;
        public int ePoseFilter_Value;
        public int eStartingPose_Type = -1;
        public int eStartingPose_Value;

        public BioEvtSysTrackGesture(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Gesture";
        }

        public void LoadData()
        {   //default values
            m_aGestures = new List<Gesture>();
            nmStartingPoseSet = -1;
            nmStartingPoseAnim = -1;
            m_fStartPoseOffset = 0;
            //m_bUseDynamicAnimsets;
            ePoseFilter_Type = -1;
            eStartingPose_Type = -1;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "nmStartingPoseSet")
                    nmStartingPoseSet = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "nmStartingPoseAnim")
                    nmStartingPoseAnim = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "m_fStartPoseOffset")
                    m_fStartPoseOffset = BitConverter.ToSingle(p.raw, 24);
                else if (pcc.getNameEntry(p.Name) == "m_bUseDynamicAnimsets")
                    m_bUseDynamicAnimsets = p.Value.IntValue != 0;
                else if (pcc.getNameEntry(p.Name) == "ePoseFilter")
                    GetByteVal(p.raw, out ePoseFilter_Type, out ePoseFilter_Value);
                else if (pcc.getNameEntry(p.Name) == "eStartingPose")
                    GetByteVal(p.raw, out eStartingPose_Type, out eStartingPose_Value);
                else if (pcc.getNameEntry(p.Name) == "m_aGesturesKeys")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        Gesture key = new Gesture();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "aChainedGestures")
                            {
                                int count2 = BitConverter.ToInt32(p2[i].raw, 24);
                                key.aChainedGestures = new List<int>();
                                for (int k = 0; k < count2; k++)
                                {
                                    key.aChainedGestures.Add(BitConverter.ToInt32(p2[i].raw, 28 + k * 4));
                                }
                            }
                            else if (pcc.getNameEntry(p2[i].Name) == "nmPoseSet")
                                key.nmPoseSet = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "nmPoseAnim")
                                key.nmPoseAnim = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "nmGestureSet")
                                key.nmGestureSet = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "nmGestureAnim")
                                key.nmGestureAnim = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "nmTransitionSet")
                                key.nmTransitionSet = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "nmTransitionAnim")
                                key.nmTransitionAnim = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "fPlayRate")
                                key.fPlayRate = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "fStartOffset")
                                key.fStartOffset = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "fEndOffset")
                                key.fEndOffset = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "fStartBlendDuration")
                                key.fStartBlendDuration = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "fWeight")
                                key.fWeight = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "fTransBlendTime")
                                key.fTransBlendTime = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "bInvalidData")
                                key.bInvalidData = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bOneShotAnim")
                                key.bOneShotAnim = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bChainToPrevious")
                                key.bChainToPrevious = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bPlayUntilNext")
                                key.bPlayUntilNext = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bTerminateAllGestures")
                                key.bTerminateAllGestures = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bUseDynAnimSets")
                                key.bUseDynAnimSets = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bSnapToPose")
                                key.bSnapToPose = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "ePoseFilter")
                                GetByteVal(p2[i].raw, out key.ePoseFilter_Type, out key.ePoseFilter_Value);
                            pos += p2[i].raw.Length;
                        }
                        m_aGestures.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aGestures");
            for (int i = 0; i < m_aGestures.Count; i++)
                t.Nodes.Add(m_aGestures[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
            if (nmStartingPoseSet != -1)
                propView.Nodes.Add(new TreeNode("nmStartingPoseSet : " + pcc.getNameEntry(nmStartingPoseSet)));
            if (nmStartingPoseAnim != -1)
                propView.Nodes.Add(new TreeNode("nmStartingPoseAnim : " + pcc.getNameEntry(nmStartingPoseAnim)));
            propView.Nodes.Add(new TreeNode("m_fStartPoseOffset : " + m_fStartPoseOffset));
            propView.Nodes.Add(new TreeNode("m_bUseDynamicAnimsets : " + m_bUseDynamicAnimsets));
            if (ePoseFilter_Type != -1)
                propView.Nodes.Add(new TreeNode("ePoseFilter : " + pcc.getNameEntry(ePoseFilter_Type) + ", " + pcc.getNameEntry(ePoseFilter_Value)));
            if (eStartingPose_Type != -1)
                propView.Nodes.Add(new TreeNode("eStartingPose : " + pcc.getNameEntry(eStartingPose_Type) + ", " + pcc.getNameEntry(eStartingPose_Value)));
        }
    }

    public class BioEvtSysTrackLighting : SFXGameActorInterpTrack
    {
        public struct LightingKey
        {
            public int TargetBoneName;  //name
            public float KeyLight_Scale_Red;
            public float KeyLight_Scale_Green;
            public float KeyLight_Scale_Blue;
            public float FillLight_Scale_Red;
            public float FillLight_Scale_Green;
            public float FillLight_Scale_Blue;
            public int RimLightColor;
            public float RimLightScale;
            public float RimLightYaw;
            public float RimLightPitch;
            public float BouncedLightingIntensity;
            public int LightRig; //object
            public float LightRigOrientation;
            public bool bLockEnvironment;
            public bool bTriggerFullUpdate;
            public bool bUseForNextCamera;
            public bool bCastShadows;
            public int RimLightControl_Type;
            public int RimLightControl_Value;
            public int LightingType_Type;
            public int LightingType_Value;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("TargetBoneName : " + pcc.getNameEntry(TargetBoneName)));
                root.Nodes.Add(new TreeNode("KeyLight_Scale_Red : " + KeyLight_Scale_Red));
                root.Nodes.Add(new TreeNode("KeyLight_Scale_Green : " + KeyLight_Scale_Green));
                root.Nodes.Add(new TreeNode("KeyLight_Scale_Blue : " + KeyLight_Scale_Blue));
                root.Nodes.Add(new TreeNode("FillLight_Scale_Red : " + FillLight_Scale_Red));
                root.Nodes.Add(new TreeNode("FillLight_Scale_Green : " + FillLight_Scale_Green));
                root.Nodes.Add(new TreeNode("FillLight_Scale_Blue : " + FillLight_Scale_Blue));
                root.Nodes.Add(new TreeNode("RimLightColor : " + RimLightColor));
                root.Nodes.Add(new TreeNode("RimLightScale : " + RimLightScale));
                root.Nodes.Add(new TreeNode("RimLightYaw : " + RimLightYaw));
                root.Nodes.Add(new TreeNode("RimLightPitch : " + RimLightPitch));
                root.Nodes.Add(new TreeNode("BouncedLightingIntensity : " + BouncedLightingIntensity));
                root.Nodes.Add(new TreeNode("LightRig : " + LightRig));
                root.Nodes.Add(new TreeNode("LightRigOrientation : " + LightRigOrientation));
                root.Nodes.Add(new TreeNode("bLockEnvironment : " + bLockEnvironment));
                root.Nodes.Add(new TreeNode("bTriggerFullUpdate : " + bTriggerFullUpdate));
                root.Nodes.Add(new TreeNode("bUseForNextCamera : " + bUseForNextCamera));
                root.Nodes.Add(new TreeNode("bCastShadows : " + bCastShadows));
                root.Nodes.Add(new TreeNode("RimLightControl : " + pcc.getNameEntry(RimLightControl_Type) + "," + pcc.getNameEntry(RimLightControl_Value)));
                root.Nodes.Add(new TreeNode("LightingType : " + pcc.getNameEntry(LightingType_Type) + "," + pcc.getNameEntry(LightingType_Value)));
                return root;
            }
        }

        public List<LightingKey> m_aLightingKeys;

        public BioEvtSysTrackLighting(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Lighting";
            GetKeyFrames();

        }

        public void LoadData()
        {   //default values
            m_aLightingKeys = new List<LightingKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aLightingKeys")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        LightingKey key = new LightingKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "TargetBoneName")
                                key.TargetBoneName = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "KeyLight_Scale_Red")
                                key.KeyLight_Scale_Red = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "KeyLight_Scale_Green")
                                key.KeyLight_Scale_Green = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "KeyLight_Scale_Blue")
                                key.KeyLight_Scale_Blue = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "FillLight_Scale_Red")
                                key.FillLight_Scale_Red = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "FillLight_Scale_Green")
                                key.FillLight_Scale_Green = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "FillLight_Scale_Blue")
                                key.FillLight_Scale_Blue = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "RimLightColor")
                                key.RimLightColor = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "RimLightScale")
                                key.RimLightScale = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "RimLightYaw")
                                key.RimLightYaw = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "RimLightPitch")
                                key.RimLightPitch = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "BouncedLightingIntensity")
                                key.BouncedLightingIntensity = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "LightRig")
                                key.LightRig = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "LightRigOrientation")
                                key.LightRigOrientation = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "bLockEnvironment")
                                key.bLockEnvironment = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bTriggerFullUpdate")
                                key.bTriggerFullUpdate = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bUseForNextCamera")
                                key.bUseForNextCamera = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bCastShadows")
                                key.bCastShadows = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "RimLightControl")
                                GetByteVal(p2[i].raw, out key.RimLightControl_Type, out key.RimLightControl_Value);
                            else if (pcc.getNameEntry(p2[i].Name) == "LightingType")
                                GetByteVal(p2[i].raw, out key.LightingType_Type, out key.LightingType_Value);
                            pos += p2[i].raw.Length;
                        }
                        m_aLightingKeys.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aLightingKeys");
            for (int i = 0; i < m_aLightingKeys.Count; i++)
                t.Nodes.Add(m_aLightingKeys[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
        }
    }

    public class BioEvtSysTrackLookAt : SFXGameActorInterpTrack
    {
        public struct LookAtKey
        {
            public int nmFindActor;
            public bool bEnabled;
            public bool bInstantTransition;
            public bool bLockedToTarget;
            public int eFindActorMode_Type;
            public int eFindActorMode_Value;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("nmFindActor : " + pcc.getNameEntry(nmFindActor)));
                root.Nodes.Add(new TreeNode("bEnabled : " + bEnabled));
                root.Nodes.Add(new TreeNode("bInstantTransition : " + bInstantTransition));
                root.Nodes.Add(new TreeNode("bLockedToTarget : " + bLockedToTarget));
                root.Nodes.Add(new TreeNode("eFindActorMode : " + pcc.getNameEntry(eFindActorMode_Type) + ", " + pcc.getNameEntry(eFindActorMode_Value)));
                return root;
            }
        }

        public List<LookAtKey> m_aLookAtKeys;

        public BioEvtSysTrackLookAt(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "LookAt";
        }

        public void LoadData()
        {   //default values
            m_aLookAtKeys = new List<LookAtKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aLookAtKeys")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        LookAtKey key = new LookAtKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "nmFindActor")
                                key.nmFindActor = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "bEnabled")
                                key.bEnabled = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bInstantTransition")
                                key.bInstantTransition = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bLockedToTarget")
                                key.bLockedToTarget = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "eFindActorMode")
                                GetByteVal(p2[i].raw, out key.eFindActorMode_Type, out key.eFindActorMode_Value);
                            pos += p2[i].raw.Length;
                        }
                        m_aLookAtKeys.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aLookAtKeys");
            for (int i = 0; i < m_aLookAtKeys.Count; i++)
                t.Nodes.Add(m_aLookAtKeys[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
        }
    }

    public class BioEvtSysTrackProp : SFXGameActorInterpTrack
    {
        public struct PropKey
        {
            public int pWeaponClass; //object
            public int nmProp; //name
            public int nmAction; //name
            public int pPropMesh; //object
            public int pActionPartSys; //object
            public int pActionClientEffect; //object
            public bool bEquip;
            public bool bForceGenericWeapon;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("pWeaponClass : " + pWeaponClass));
                root.Nodes.Add(new TreeNode("nmProp : " + pcc.getNameEntry(nmProp)));
                root.Nodes.Add(new TreeNode("nmAction : " + pcc.getNameEntry(nmAction)));
                root.Nodes.Add(new TreeNode("pPropMesh : " + pPropMesh));
                root.Nodes.Add(new TreeNode("pActionPartSys : " + pActionPartSys));
                root.Nodes.Add(new TreeNode("pActionClientEffect : " + pActionClientEffect));
                root.Nodes.Add(new TreeNode("bEquip : " + bEquip));
                root.Nodes.Add(new TreeNode("bForceGenericWeapon : " + bForceGenericWeapon));
                return root;
            }
        }

        public List<PropKey> m_aPropKeys;

        public BioEvtSysTrackProp(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Prop";
        }

        public void LoadData()
        {   //defultt values
            m_aPropKeys = new List<PropKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aPropKeys")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        PropKey key = new PropKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "pWeaponClass")
                                key.pWeaponClass = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "nmProp")
                                key.nmProp = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "nmAction")
                                key.nmAction = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "pPropMesh")
                                key.pPropMesh = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "pActionPartSys")
                                key.pActionPartSys = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "pActionClientEffect")
                                key.pActionClientEffect = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "bEquip")
                                key.bEquip = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bForceGenericWeapon")
                                key.bForceGenericWeapon = p2[i].Value.IntValue != 0;
                            pos += p2[i].raw.Length;
                        }
                        m_aPropKeys.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aPropKeys");
            for (int i = 0; i < m_aPropKeys.Count; i++)
                t.Nodes.Add(m_aPropKeys[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
        }
    }

    public class BioEvtSysTrackSetFacing : SFXGameActorInterpTrack
    {
        public struct FacingKey
        {
            public int nmStageNode;
            public float fOrientation;
            public bool bApplyOrientation;
            public int eCurrentStageNode_Type;
            public int eCurrentStageNode_Value;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("nmStageNode : " + pcc.getNameEntry(nmStageNode)));
                root.Nodes.Add(new TreeNode("fOrientation : " + fOrientation));
                root.Nodes.Add(new TreeNode("bApplyOrientation : " + bApplyOrientation));
                root.Nodes.Add(new TreeNode("eCurrentStageNode : " + pcc.getNameEntry(eCurrentStageNode_Type) + ", " + pcc.getNameEntry(eCurrentStageNode_Type)));
                return root;
            }
        }

        public List<FacingKey> m_aFacingKeys;

        public BioEvtSysTrackSetFacing(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "SetFacing";
        }

        public void LoadData()
        {   //default values
            m_aFacingKeys = new List<FacingKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aFacingKeys")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        FacingKey key = new FacingKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "nmStageNode")
                                key.nmStageNode = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "fOrientation")
                                key.fOrientation = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "bApplyOrientation")
                                key.bApplyOrientation = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "eCurrentStageNode")
                                GetByteVal(p2[i].raw, out key.eCurrentStageNode_Type, out key.eCurrentStageNode_Value);
                            pos += p2[i].raw.Length;
                        }
                        m_aFacingKeys.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aFacingKeys");
            for (int i = 0; i < m_aFacingKeys.Count; i++)
                t.Nodes.Add(m_aFacingKeys[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
        }
    }

    public class SFXGameInterpTrackProcFoley : SFXGameActorInterpTrack
    {
        public struct ProcFoleyStartStopKey
        {
            public float m_fMaxThreshold;
            public float m_fSmoothingFactor;
            public bool m_bStart;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("m_fMaxThreshold : " + m_fMaxThreshold));
                root.Nodes.Add(new TreeNode("m_fSmoothingFactor : " + m_fSmoothingFactor));
                root.Nodes.Add(new TreeNode("m_bStart : " + m_bStart));
                return root;
            }
        }

        public List<ProcFoleyStartStopKey> m_aProcFoleyStartStopKeys;
        public int m_TrackFoleySound; //object

        public SFXGameInterpTrackProcFoley(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "ProcFoley";
        }

        public void LoadData()
        {   //default
            m_aProcFoleyStartStopKeys = new List<ProcFoleyStartStopKey>();
            m_TrackFoleySound = 0;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_TrackFoleySound")
                    m_TrackFoleySound = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "m_aProcFoleyStartStopKeys")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        ProcFoleyStartStopKey key = new ProcFoleyStartStopKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "m_fMaxThreshold")
                                key.m_fMaxThreshold = BitConverter.ToSingle(p2[i].raw, 24);
                            if (pcc.getNameEntry(p2[i].Name) == "m_fSmoothingFactor")
                                key.m_fSmoothingFactor = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "m_bStart")
                                key.m_bStart = p2[i].Value.IntValue != 0;
                            pos += p2[i].raw.Length;
                        }
                        m_aProcFoleyStartStopKeys.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aProcFoleyStartStopKeys");
            for (int i = 0; i < m_aProcFoleyStartStopKeys.Count; i++)
                t.Nodes.Add(m_aProcFoleyStartStopKeys[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
            propView.Nodes.Add(new TreeNode("m_TrackFoleySound : " + m_TrackFoleySound));
        }
    }

    public class SFXInterpTrackPlayFaceOnlyVO : SFXGameActorInterpTrack
    {
        public struct FOVOKey
        {
            public int pConversation; //object
            public int nLineStrRef;
            public int srActorNameOverride;
            public bool bForceHideSubtitles;
            public bool bPlaySoundOnly;
            public bool bDisableDelayUntilPreload;
            public bool bAllowInConversation;
            public bool bSubtitleHasPriority;

            public TreeNode ToTree(int index, float time, TalkFile tlk, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("pConversation : " + pConversation));
                root.Nodes.Add(new TreeNode("nLineStrRef : " + tlk.findDataById(nLineStrRef)));
                root.Nodes.Add(new TreeNode("srActorNameOverride : " + srActorNameOverride));
                root.Nodes.Add(new TreeNode("bForceHideSubtitles : " + bForceHideSubtitles));
                root.Nodes.Add(new TreeNode("bPlaySoundOnly : " + bPlaySoundOnly));
                root.Nodes.Add(new TreeNode("bDisableDelayUntilPreload : " + bDisableDelayUntilPreload));
                root.Nodes.Add(new TreeNode("bAllowInConversation : " + bAllowInConversation));
                root.Nodes.Add(new TreeNode("bSubtitleHasPriority : " + bSubtitleHasPriority));
                return root;
            }
        }

        public List<FOVOKey> m_aFOVOKeys;

        public SFXInterpTrackPlayFaceOnlyVO(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "PlayFaceOnlyVO";
        }

        public void LoadData()
        {   //default values
            m_aFOVOKeys = new List<FOVOKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aFOVOKeys")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        FOVOKey key = new FOVOKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "pConversation")
                                key.pConversation = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "nLineStrRef")
                                key.nLineStrRef = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "srActorNameOverride")
                                key.srActorNameOverride = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "bForceHideSubtitles")
                                key.bForceHideSubtitles = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bPlaySoundOnly")
                                key.bPlaySoundOnly = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bDisableDelayUntilPreload")
                                key.bDisableDelayUntilPreload = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bAllowInConversation")
                                key.bAllowInConversation = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bSubtitleHasPriority")
                                key.bSubtitleHasPriority = p2[i].Value.IntValue != 0;
                            pos += p2[i].raw.Length;
                        }
                        m_aFOVOKeys.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aFOVOKeys");
            for (int i = 0; i < m_aFOVOKeys.Count; i++)
                t.Nodes.Add(m_aFOVOKeys[i].ToTree(i, m_aTrackKeys[i].fTime, talkfile, pcc));
            propView.Nodes.Add(t);
        }
    }

    public class SFXInterpTrackAttachCrustEffect : BioInterpTrack
    {
        public struct CrustEffectKey
        {
            public float m_fLifeTime;
            public bool m_bAttach;

            public TreeNode ToTree(int index, float time)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("m_fLifeTime : " + m_fLifeTime));
                root.Nodes.Add(new TreeNode("m_bAttach : " + m_bAttach));
                return root;
            }
        }

        public List<CrustEffectKey> m_aCrustEffectKeyData;
        public List<int> m_aTarget;
        public int oEffect;

        public SFXInterpTrackAttachCrustEffect(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Attach Crust Effect";
        }

        public void LoadData()
        {   //default values
            m_aCrustEffectKeyData = new List<CrustEffectKey>();
            m_aTarget = new List<int>();
            oEffect = 0;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "oEffect")
                    oEffect = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "m_aCrustEffectKeyData")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        CrustEffectKey key = new CrustEffectKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "m_fLifeTime")
                                key.m_fLifeTime = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "m_bAttach")
                                key.m_bAttach = p2[i].Value.IntValue != 0;
                            pos += p2[i].raw.Length;
                        }
                        m_aCrustEffectKeyData.Add(key);
                    }
                }
                else if (pcc.getNameEntry(p.Name) == "m_aTarget")
                {
                    int count2 = BitConverter.ToInt32(p.raw, 24);
                    for (int k = 0; k < count2; k++)
                    {
                        m_aTarget.Add(BitConverter.ToInt32(p.raw, 28 + k * 4));
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aCrustEffectKeyData");
            for (int i = 0; i < m_aCrustEffectKeyData.Count; i++)
                t.Nodes.Add(m_aCrustEffectKeyData[i].ToTree(i, m_aTrackKeys[i].fTime));
            propView.Nodes.Add(t);
            t = new TreeNode("m_aTarget");
            for (int i = 0; i < m_aTarget.Count; i++)
                t.Nodes.Add(m_aTarget[i].ToString());
            propView.Nodes.Add(t);
            propView.Nodes.Add(new TreeNode("oEffect : " + oEffect));
        }
    }

    public class SFXInterpTrackAttachToActor : BioInterpTrack
    {
        public List<int> m_aTarget;
        public float[]  RelativeOffset;          
        public float[]  RelativeRotation;        
        public int      BoneName;                
        public bool     bDetach;             
        public bool     bHardAttach;         
        public bool     bUseRelativeOffset;  
        public bool     bUseRelativeRotation;

        public SFXInterpTrackAttachToActor(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Attach To Actor";
        }

        public void LoadData()
        {   //default values
            m_aTarget = new List<int>();
            RelativeOffset = new float[3];
            RelativeRotation = new float[3];
            BoneName = -1;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aTarget")
                {
                    int count2 = BitConverter.ToInt32(p.raw, 24);
                    for (int k = 0; k < count2; k++)
                    {
                        m_aTarget.Add(BitConverter.ToInt32(p.raw, 28 + k * 4));
                    }
                }
                else if (pcc.getNameEntry(p.Name) == "RelativeOffset")
                {
                    RelativeOffset[0] = BitConverter.ToSingle(p.raw, 32);
                    RelativeOffset[1] = BitConverter.ToSingle(p.raw, 36);
                    RelativeOffset[2] = BitConverter.ToSingle(p.raw, 40);

                }
                else if (pcc.getNameEntry(p.Name) == "RelativeRotation")
                {
                    RelativeRotation[0] = BitConverter.ToSingle(p.raw, 32);
                    RelativeRotation[1] = BitConverter.ToSingle(p.raw, 36);
                    RelativeRotation[2] = BitConverter.ToSingle(p.raw, 40);

                }
                else if (pcc.getNameEntry(p.Name) == "BoneName")
                    BoneName = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "bDetach")
                    bDetach = p.Value.IntValue != 0;
                else if (pcc.getNameEntry(p.Name) == "bHardAttach")
                    bHardAttach = p.Value.IntValue != 0;
                else if (pcc.getNameEntry(p.Name) == "bUseRelativeOffset")
                    bUseRelativeOffset = p.Value.IntValue != 0;
                else if (pcc.getNameEntry(p.Name) == "bDetach")
                    bUseRelativeRotation = p.Value.IntValue != 0;
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aTarget");
            for (int i = 0; i < m_aTarget.Count; i++)
                t.Nodes.Add(m_aTarget[i].ToString());
            propView.Nodes.Add(t);
            t = new TreeNode("RelativeOffset");
            t.Nodes.Add("X : " + RelativeOffset[0]);
            t.Nodes.Add("Y : " + RelativeOffset[1]);
            t.Nodes.Add("Z : " + RelativeOffset[2]);
            propView.Nodes.Add(t);
            t = new TreeNode("RelativeRotation");
            t.Nodes.Add("" + RelativeRotation[0]);
            t.Nodes.Add("" + RelativeRotation[1]);
            t.Nodes.Add("" + RelativeRotation[2]);
            propView.Nodes.Add(t);
            propView.Nodes.Add(new TreeNode("BoneName : " + BoneName));
            propView.Nodes.Add(new TreeNode("bDetach : " + bDetach));
            propView.Nodes.Add(new TreeNode("bHardAttach : " + bHardAttach));
            propView.Nodes.Add(new TreeNode("bUseRelativeOffset : " + bUseRelativeOffset));
            propView.Nodes.Add(new TreeNode("bUseRelativeRotation : " + bUseRelativeRotation));
        }
    }

    public class SFXInterpTrackAttachVFXToObject : BioInterpTrack
    {
        public List<int> m_aAttachToTarget;
        public float[] m_vOffset;
        public int m_nmSocketOrBone;
        public int m_oEffect;

        public SFXInterpTrackAttachVFXToObject(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Attach VFX To Object";
        }

        public void LoadData()
        {   //default values
            m_aAttachToTarget = new List<int>();
            m_vOffset = new float[3];
            m_nmSocketOrBone = -1;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aAttachToTarget")
                {
                    int count2 = BitConverter.ToInt32(p.raw, 24);
                    for (int k = 0; k < count2; k++)
                    {
                        m_aAttachToTarget.Add(BitConverter.ToInt32(p.raw, 28 + k * 4));
                    }
                }
                else if (pcc.getNameEntry(p.Name) == "m_vOffset")
                {
                    m_vOffset[0] = BitConverter.ToSingle(p.raw, 32);
                    m_vOffset[1] = BitConverter.ToSingle(p.raw, 36);
                    m_vOffset[2] = BitConverter.ToSingle(p.raw, 40);

                }
                else if (pcc.getNameEntry(p.Name) == "m_nmSocketOrBone")
                    m_nmSocketOrBone = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "m_oEffect")
                    m_oEffect = p.Value.IntValue;
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aAttachToTarget");
            for (int i = 0; i < m_aAttachToTarget.Count; i++)
                t.Nodes.Add(m_aAttachToTarget[i].ToString());
            propView.Nodes.Add(t);
            t = new TreeNode("m_vOffset");
            t.Nodes.Add("" + m_vOffset[0]);
            t.Nodes.Add("" + m_vOffset[1]);
            t.Nodes.Add("" + m_vOffset[2]);
            propView.Nodes.Add(t);
            propView.Nodes.Add(new TreeNode("m_nmSocketOrBone : " + m_nmSocketOrBone));
            propView.Nodes.Add(new TreeNode("m_oEffect : " + m_oEffect));
        }
    }

    public class SFXInterpTrackBlackScreen : BioInterpTrack
    {
        public struct BlackScreenKey
        {
            public int PlaceHolder;
            public int BlackScreenState_Type;
            public int BlackScreenState_Value;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("PlaceHolder : " + PlaceHolder));
                root.Nodes.Add(new TreeNode("BlackScreenState : " + pcc.getNameEntry(BlackScreenState_Type) + ", " + pcc.getNameEntry(BlackScreenState_Value)));
                return root;
            }
        }

        public List<BlackScreenKey> m_aBlackScreenKeyData;
        public int m_BlackScreenSeq;

        public SFXInterpTrackBlackScreen(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Black Screen";
        }

        public void LoadData()
        {   //default values
            m_aBlackScreenKeyData = new List<BlackScreenKey>();
            m_BlackScreenSeq = 0;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_BlackScreenSeq")
                    m_BlackScreenSeq = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "m_aBlackScreenKeyData")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        BlackScreenKey key = new BlackScreenKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "PlaceHolder")
                                key.PlaceHolder = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "BlackScreenState")
                                GetByteVal(p2[i].raw, out key.BlackScreenState_Type, out key.BlackScreenState_Value);
                            pos += p2[i].raw.Length;
                        }
                        m_aBlackScreenKeyData.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aBlackScreenKeyData");
            for (int i = 0; i < m_aBlackScreenKeyData.Count; i++)
                t.Nodes.Add(m_aBlackScreenKeyData[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
            propView.Nodes.Add(new TreeNode("m_BlackScreenSeq : " + m_BlackScreenSeq));
        }
    }

    public class SFXInterpTrackDestroy : BioInterpTrack
    {
        public List<int> m_aTarget;

        public SFXInterpTrackDestroy(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Destroy";
        }

        public void LoadData()
        {   //default values
            m_aTarget = new List<int>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aTarget")
                {
                    int count2 = BitConverter.ToInt32(p.raw, 24);
                    for (int k = 0; k < count2; k++)
                    {
                        m_aTarget.Add(BitConverter.ToInt32(p.raw, 28 + k * 4));
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aTarget");
            for (int i = 0; i < m_aTarget.Count; i++)
                t.Nodes.Add(m_aTarget[i].ToString());
            propView.Nodes.Add(t);
        }
    }

    public class SFXInterpTrackForceLightEnvUpdate : BioInterpTrack
    {
        public int m_SeqForceUpdateLight;

        public SFXInterpTrackForceLightEnvUpdate(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "ForceLightEnvUpdate";
        }

        public void LoadData()
        {   //default values
            m_SeqForceUpdateLight = 0;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_SeqForceUpdateLight")
                    m_SeqForceUpdateLight = p.Value.IntValue;
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            propView.Nodes.Add(new TreeNode("m_SeqForceUpdateLight : " + m_SeqForceUpdateLight));
        }
    }

    public class SFXInterpTrackLightEnvQuality : BioInterpTrack
    {
        public struct LightEnvKey
        {
            public int PlaceHolder;
            public int Quality_Type;
            public int Quality_Value;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("PlaceHolder : " + PlaceHolder));
                root.Nodes.Add(new TreeNode("Quality : " + pcc.getNameEntry(Quality_Type) + ", " + pcc.getNameEntry(Quality_Value)));
                return root;
            }
        }

        public List<LightEnvKey> m_aLightEnvKeyData;
        public int m_LightEnvSeq;

        public SFXInterpTrackLightEnvQuality(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Light Env Quality";
        }

        public void LoadData()
        {   //default values
            m_aLightEnvKeyData = new List<LightEnvKey>();
            m_LightEnvSeq = 0;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_LightEnvSeq")
                    m_LightEnvSeq = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "m_aLightEnvKeyData")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        LightEnvKey key = new LightEnvKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "PlaceHolder")
                                key.PlaceHolder = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "Quality")
                                GetByteVal(p2[i].raw, out key.Quality_Type, out key.Quality_Value);
                            pos += p2[i].raw.Length;
                        }
                        m_aLightEnvKeyData.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aLightEnvKeyData");
            for (int i = 0; i < m_aLightEnvKeyData.Count; i++)
                t.Nodes.Add(m_aLightEnvKeyData[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
            propView.Nodes.Add(new TreeNode("m_LightEnvSeq : " + m_LightEnvSeq));
        }
    }

    public class SFXInterpTrackMovieBink : SFXInterpTrackMovieBase
    {
        public string m_sMovieName;
        //public float m_fAutoResizeBuffer;
        public int m_SoundEvent; //object
        //public bool m_bIgnoreShrinking;
        //public bool m_bIgnoreGrowing;

        public SFXInterpTrackMovieBink(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Movie Bink";
        }

        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_sMovieName")
                    m_sMovieName = p.Value.StringValue;
                else if (pcc.getNameEntry(p.Name) == "m_SoundEvent")
                    m_SoundEvent = p.Value.IntValue;
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            propView.Nodes.Add(new TreeNode("m_sMovieName : \"" + m_sMovieName + "\""));
            propView.Nodes.Add(new TreeNode("m_SoundEvent : " + m_SoundEvent));
        }
    }

    public class SFXInterpTrackMovieTexture : SFXInterpTrackMovieBase
    {
        public int m_oTextureMovie;

        public SFXInterpTrackMovieTexture(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Movie Texture";
        }

        public void LoadData()
        {   //default values
            m_oTextureMovie = 0;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_oTextureMovie")
                    m_oTextureMovie = p.Value.IntValue;
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            propView.Nodes.Add(new TreeNode("m_oTextureMovie : " + m_oTextureMovie));
        }
    }

    public class SFXInterpTrackSetPlayerNearClipPlane : BioInterpTrack
    {
        public struct NearClipKey
        {
            public float m_fValue;
            public bool m_bUseDefaultValue;

            public TreeNode ToTree(int index, float time)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("m_fValue : " + m_fValue));
                root.Nodes.Add(new TreeNode("m_bUseDefaultValue : " + m_bUseDefaultValue));
                return root;
            }
        }

        public List<NearClipKey> m_aNearClipKeyData;

        public SFXInterpTrackSetPlayerNearClipPlane(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "SetPlayerNearClipPlane";
        }

        public void LoadData()
        {   //default values
            m_aNearClipKeyData = new List<NearClipKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aNearClipKeyData")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        NearClipKey key = new NearClipKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "m_fValue")
                                key.m_fValue = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "m_bUseDefaultValue")
                                key.m_bUseDefaultValue = p2[i].Value.IntValue != 0;
                            pos += p2[i].raw.Length;
                        }
                        m_aNearClipKeyData.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aNearClipKeyData");
            for (int i = 0; i < m_aNearClipKeyData.Count; i++)
                t.Nodes.Add(m_aNearClipKeyData[i].ToTree(i, m_aTrackKeys[i].fTime));
            propView.Nodes.Add(t);
        }
    }

    public class SFXInterpTrackSetWeaponInstant  : BioInterpTrack
    {
        public struct WeaponClassKey
        {
            public int cWeapon;

            public TreeNode ToTree(int index, float time)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("cWeapon : " + cWeapon));
                return root;
            }
        }

        public List<WeaponClassKey> m_aWeaponClassKeyData;
        public int m_PawnRefTag;
        public int m_Pawn;

        public SFXInterpTrackSetWeaponInstant(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "SetWeaponInstant";
        }

        public void LoadData()
        {   //default values
            m_aWeaponClassKeyData = new List<WeaponClassKey>();
            m_PawnRefTag = 0;
            m_Pawn = 0;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_PawnRefTag")
                    m_PawnRefTag = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "m_Pawn")
                    m_Pawn = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "m_aWeaponClassKeyData")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        WeaponClassKey key = new WeaponClassKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "cWeapon")
                                key.cWeapon = p2[i].Value.IntValue;
                            pos += p2[i].raw.Length;
                        }
                        m_aWeaponClassKeyData.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aWeaponClassKeyData");
            for (int i = 0; i < m_aWeaponClassKeyData.Count; i++)
                t.Nodes.Add(m_aWeaponClassKeyData[i].ToTree(i, m_aTrackKeys[i].fTime));
            propView.Nodes.Add(t);
            propView.Nodes.Add(new TreeNode("m_PawnRefTag : " + m_PawnRefTag));
            propView.Nodes.Add(new TreeNode("m_Pawn : " + m_Pawn));
        }
    }

    public class SFXInterpTrackToggleAffectedByHitEffects : SFXInterpTrackToggleBase
    {
        public SFXInterpTrackToggleAffectedByHitEffects(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            if (TrackTitle == "")
                TrackTitle = "ToggleAffectedByHitEffects";
        }
    }

    public class SFXInterpTrackToggleHidden : SFXInterpTrackToggleBase
    {
        public SFXInterpTrackToggleHidden(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            if (TrackTitle == "")
                TrackTitle = "Toggle Hidden";
        }
    }

    public class SFXInterpTrackToggleLightEnvironment : SFXInterpTrackToggleBase
    {
        public int m_LightEnvSeq;

        public SFXInterpTrackToggleLightEnvironment(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "ToggleLightEnvironment";
        }

        public void LoadData()
        {
            m_LightEnvSeq = 0;

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_LightEnvSeq")
                    m_LightEnvSeq = p.Value.IntValue;
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            propView.Nodes.Add(new TreeNode("m_LightEnvSeq : " + m_LightEnvSeq));
        }
    }

    public class SFXGameInterpTrackWwiseMicLock : BioInterpTrack
    {
        public struct MicLockKey
        {
            public int m_nmFindActor;
            public bool m_bLock;
            public int m_eFindActorMode_Type;
            public int m_eFindActorMode_Value;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("m_nmFindActor : " + pcc.getNameEntry(m_nmFindActor)));
                root.Nodes.Add(new TreeNode("m_bLock : " + m_bLock));
                root.Nodes.Add(new TreeNode("m_eFindActorMode : " + pcc.getNameEntry(m_eFindActorMode_Type) + ", " + pcc.getNameEntry(m_eFindActorMode_Value)));
                return root;
            }
        }

        public List<MicLockKey> m_aMicLockKeys;
        public bool m_bUnlockAtEnd;

        public SFXGameInterpTrackWwiseMicLock(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
        }

        public void LoadData()
        {   //default values
            m_aMicLockKeys = new List<MicLockKey>();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_bUnlockAtEnd")
                    m_bUnlockAtEnd = p.Value.IntValue != 0;
                else if (pcc.getNameEntry(p.Name) == "m_aMicLockKeys")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        MicLockKey key = new MicLockKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "m_nmFindActor")
                                key.m_nmFindActor = p2[i].Value.IntValue;
                            if (pcc.getNameEntry(p2[i].Name) == "m_bLock")
                                key.m_bLock = p2[i].Value.IntValue != 0;
                            if (pcc.getNameEntry(p2[i].Name) == "m_eFindActorMode")
                                GetByteVal(p2[i].raw, out  key.m_eFindActorMode_Type, out key.m_eFindActorMode_Value);
                            pos += p2[i].raw.Length;
                        }
                        m_aMicLockKeys.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aMicLockKeys");
            for (int i = 0; i < m_aMicLockKeys.Count; i++)
                t.Nodes.Add(m_aMicLockKeys[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
            propView.Nodes.Add(new TreeNode("m_bUnlockAtEnd : " + m_bUnlockAtEnd));
        }
    }

    public class InterpTrackEvent : InterpTrack
    {
        public struct EventTrackKey
        {
            public int EventName; //name
            public float Time;

            public TreeNode ToTree(int index, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + Time);
                root.Nodes.Add(new TreeNode("EventName : " + pcc.getNameEntry(EventName)));
                root.Nodes.Add(new TreeNode("Time : " + Time));
                return root;
            }
        }

        public List<EventTrackKey> EventTrack;
        public bool bFireEventsWhenForwards;
        public bool bFireEventsWhenBackwards;
        public bool bFireEventsWhenJumpingForwards;

        public InterpTrackEvent(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Event";
            GetKeyFrames();
        }

        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "bFireEventsWhenForwards")
                    bFireEventsWhenForwards = p.Value.IntValue != 0;
                else if (pcc.getNameEntry(p.Name) == "bFireEventsWhenBackwards")
                    bFireEventsWhenBackwards = p.Value.IntValue != 0;
                else if (pcc.getNameEntry(p.Name) == "bFireEventsWhenJumpingForwards")
                    bFireEventsWhenJumpingForwards = p.Value.IntValue != 0;
                else if (pcc.getNameEntry(p.Name) == "EventTrack")
                {
                    EventTrack = new List<EventTrackKey>();
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        EventTrackKey key = new EventTrackKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "EventName")
                                key.EventName = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "Time")
                                key.Time = BitConverter.ToSingle(p2[i].raw, 24);
                            pos += p2[i].raw.Length;
                        }
                        EventTrack.Add(key);
                    }
                }
            }
        }

        public override void GetKeyFrames()
        {
            keys = new List<PPath>();
            if (EventTrack != null)
                foreach (EventTrackKey e in EventTrack)
                    keys.Add(GenerateKeyFrame(e.Time));
        }

        public override void ToTree()
        {
            propView.Nodes.Clear();
            propView.Nodes.Add(new TreeNode("Track Title : \"" + TrackTitle + "\""));
            TreeNode t = new TreeNode("EventTrack");
            for (int i = 0; i < EventTrack.Count; i++)
                t.Nodes.Add(EventTrack[i].ToTree(i, pcc));
            propView.Nodes.Add(t);
            propView.Nodes.Add(new TreeNode("bFireEventsWhenForwards : " + bFireEventsWhenForwards));
            propView.Nodes.Add(new TreeNode("bFireEventsWhenBackwards : " + bFireEventsWhenBackwards));
            propView.Nodes.Add(new TreeNode("bFireEventsWhenJumpingForwards : " + bFireEventsWhenJumpingForwards));
        }
    }

    public class InterpTrackFaceFX : InterpTrack
    {
        public struct FaceFXTrackKey
        {
            public string FaceFXGroupName;
            public string FaceFXSeqName;
            public float StartTime;

            public TreeNode ToTree(int index)
            {
                TreeNode root = new TreeNode(index + ": " + StartTime);
                root.Nodes.Add(new TreeNode("FaceFXGroupName : \"" + FaceFXGroupName + "\""));
                root.Nodes.Add(new TreeNode("FaceFXSeqName : \"" + FaceFXSeqName + "\""));
                root.Nodes.Add(new TreeNode("StartTime : " + StartTime));
                return root;
            }
        }
        public struct FaceFXSoundCueKey
        {
            public int FaceFXSoundCue; //object

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                root.Nodes.Add(new TreeNode("FaceFXSoundCue : " + FaceFXSoundCue));
                return root;
            }
        }
        public struct Override_Asset
        {
            public int fxAsset; //object

            public TreeNode ToTree()
            {
                TreeNode root = new TreeNode("OverrideAsset");
                root.Nodes.Add(new TreeNode("fxAsset : " + fxAsset));
                return root;
            }
        }

        public List<FaceFXTrackKey> FaceFXSeqs;
        public List<FaceFXSoundCueKey> FaceFXSoundCueKeys;
        public Override_Asset OverrideAsset;

        public InterpTrackFaceFX(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "FaceFX";
            GetKeyFrames();
        }

        public void LoadData()
        {
			FaceFXSeqs = new List<FaceFXTrackKey>();
			FaceFXSoundCueKeys = new List<FaceFXSoundCueKey>();
			OverrideAsset = new Override_Asset();

            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "FaceFXSeqs")
                {
                    FaceFXSeqs = new List<FaceFXTrackKey>();
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        FaceFXTrackKey key = new FaceFXTrackKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "FaceFXGroupName")
                                key.FaceFXGroupName = p2[i].Value.StringValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "FaceFXSeqName")
                                key.FaceFXSeqName = p2[i].Value.StringValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "StartTime")
                                key.StartTime = BitConverter.ToSingle(p2[i].raw, 24);
                            pos += p2[i].raw.Length;
                        }
                        FaceFXSeqs.Add(key);
                    }
                }
                if (pcc.getNameEntry(p.Name) == "FaceFXSoundCueKey")
                {
                    FaceFXSoundCueKeys = new List<FaceFXSoundCueKey>();
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        FaceFXSoundCueKey key = new FaceFXSoundCueKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "FaceFXSoundCue")
                                key.FaceFXSoundCue = p2[i].Value.IntValue;
                            pos += p2[i].raw.Length;
                        }
                        FaceFXSoundCueKeys.Add(key);
                    }
                }
                if (pcc.getNameEntry(p.Name) == "OverrideAsset")
                {
                    OverrideAsset = new Override_Asset();
                    OverrideAsset.fxAsset = BitConverter.ToInt32(p.raw, 56);
                }
            }
        }

        public override void GetKeyFrames()
        {
            keys = new List<PPath>();
            if (FaceFXSeqs != null)
                foreach (FaceFXTrackKey s in FaceFXSeqs)
                    keys.Add(GenerateKeyFrame(s.StartTime));
        }

        public override void ToTree()
        {
            propView.Nodes.Clear();
            propView.Nodes.Add(new TreeNode("Track Title : \"" + TrackTitle + "\""));
            TreeNode t = new TreeNode("FaceFXSeqs");
            for (int i = 0; i < FaceFXSeqs.Count; i++)
                t.Nodes.Add(FaceFXSeqs[i].ToTree(i));
            propView.Nodes.Add(t);
            t = new TreeNode("FaceFXSoundCueKeys");
            for (int i = 0; i < FaceFXSoundCueKeys.Count; i++)
                t.Nodes.Add(FaceFXSoundCueKeys[i].ToTree(i, FaceFXSeqs[i].StartTime, pcc));
            propView.Nodes.Add(t);
            propView.Nodes.Add(OverrideAsset.ToTree());
        }
    }

    public class InterpTrackAnimControl : InterpTrack
    {
        public struct AnimControlTrackKey
        {
            public int AnimSeqName; //name
            public float StartTime;
            public float AnimStartOffset;
            public float AnimEndOffset;
            public float AnimPlayRate;
            public bool bLooping;
            public bool bReverse;

            public TreeNode ToTree(int index, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + StartTime + " : AnimControlTrackKey");
                root.Nodes.Add(new TreeNode("AnimSeqName : " + pcc.getNameEntry(AnimSeqName)));
                root.Nodes.Add(new TreeNode("StartTime : " + StartTime));
                root.Nodes.Add(new TreeNode("AnimStartOffset : " + AnimStartOffset));
                root.Nodes.Add(new TreeNode("AnimEndOffset : " + AnimEndOffset));
                root.Nodes.Add(new TreeNode("AnimPlayRate : " + AnimPlayRate));
                root.Nodes.Add(new TreeNode("bLooping : " + bLooping));
                root.Nodes.Add(new TreeNode("bReverse : " + bReverse));
                return root;
            }
        }

        public List<AnimControlTrackKey> AnimSeqs;

        public InterpTrackAnimControl(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "AnimControl";
            GetKeyFrames();
        }

        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "AnimSeqs")
                {
                    AnimSeqs = new List<AnimControlTrackKey>();
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        AnimControlTrackKey key = new AnimControlTrackKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "AnimSeqName")
                                key.AnimSeqName = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "StartTime")
                                key.StartTime = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "AnimStartOffset")
                                key.AnimStartOffset = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "AnimEndOffset")
                                key.AnimEndOffset = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "AnimPlayRate")
                                key.AnimPlayRate = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "bLooping")
                                key.bLooping = p2[i].Value.IntValue != 0;
                            else if (pcc.getNameEntry(p2[i].Name) == "bReverse")
                                key.bReverse = p2[i].Value.IntValue != 0;
                            pos += p2[i].raw.Length;
                        }
                        AnimSeqs.Add(key);
                    }
                }
            }
        }

        public override void GetKeyFrames()
        {
            keys = new List<PPath>();
            if (AnimSeqs != null)
                foreach (AnimControlTrackKey a in AnimSeqs)
                    keys.Add(GenerateKeyFrame(a.StartTime));
        }

        public override void ToTree()
        {
            propView.Nodes.Clear();
            propView.Nodes.Add(new TreeNode("Track Title : \"" + TrackTitle + "\""));
            TreeNode t = new TreeNode("AnimSeqs");
            for (int i = 0; i < AnimSeqs.Count; i++)
                t.Nodes.Add(AnimSeqs[i].ToTree(i, pcc));
            propView.Nodes.Add(t);
        }
    }

    public class InterpTrackMove : InterpTrack
    {
        public struct InterpLookupTrack
        {
            public struct Point
            {
                public int GroupName; //name
                public float Time;

                public TreeNode ToTree(int index, PCCObject pcc)
                {
                    TreeNode root = new TreeNode(index + ": " + Time);
                    root.Nodes.Add(new TreeNode("GroupName : " + pcc.getNameEntry(GroupName)));
                    root.Nodes.Add(new TreeNode("Time : " + Time));
                    return root;
                }
            }
            public List<Point> Points;

            public TreeNode ToTree(PCCObject pcc)
            {
                TreeNode root = new TreeNode("LookupTrack");
                TreeNode t = new TreeNode("Points");
                for (int i = 0; i < Points.Count; i++)
                    t.Nodes.Add(Points[i].ToTree(i, pcc));
                root.Nodes.Add(t);
                return root;
            }
        }

        public InterpCurveVector PosTrack;
        public InterpCurveVector EulerTrack;
        public InterpLookupTrack LookupTrack;
        public bool bUseQuatInterpolation = false;

        public InterpTrackMove(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Movement";
            GetKeyFrames();
        }

        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "bUseQuatInterpolation")
                    bUseQuatInterpolation = p.Value.IntValue != 0;
                if (pcc.getNameEntry(p.Name) == "EulerTrack")
                    EulerTrack = GetCurveVector(p, pcc);
                if (pcc.getNameEntry(p.Name) == "PosTrack")
                    PosTrack = GetCurveVector(p, pcc);
                if (pcc.getNameEntry(p.Name) == "LookupTrack")
                {
                    LookupTrack = new InterpLookupTrack();
                    LookupTrack.Points = new List<InterpLookupTrack.Point>();
                    int pos = 60;
                    int count = BitConverter.ToInt32(p.raw, 56);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        InterpLookupTrack.Point point = new InterpLookupTrack.Point();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "Time")
                                point.Time = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "GroupName")
                                point.GroupName = p2[i].Value.IntValue;
                            pos += p2[i].raw.Length;
                        }
                        LookupTrack.Points.Add(point);
                    }
                }
            }
        }

        public override void GetKeyFrames()
        {
            keys = new List<PPath>();
            if (LookupTrack.Points != null)
                foreach (InterpLookupTrack.Point p in LookupTrack.Points)
                    keys.Add(GenerateKeyFrame(p.Time));
            else if (PosTrack.Points != null)
                foreach (InterpCurveVector.InterpCurvePointVector p in PosTrack.Points)
                    keys.Add(GenerateKeyFrame(p.InVal));
            else if (EulerTrack.Points != null)
                foreach (InterpCurveVector.InterpCurvePointVector p in EulerTrack.Points)
                    keys.Add(GenerateKeyFrame(p.InVal));

        }

        public override void ToTree()
        {
            propView.Nodes.Clear();
            propView.Nodes.Add(new TreeNode("Track Title : \"" + TrackTitle + "\""));
            List<float> times = new List<float>();
            foreach (InterpLookupTrack.Point p in LookupTrack.Points)
                times.Add(p.Time);
            propView.Nodes.Add(PosTrack.ToTree("PosTrack", pcc, times));
            propView.Nodes.Add(EulerTrack.ToTree("EulerTrack", pcc, times));
            propView.Nodes.Add(LookupTrack.ToTree(pcc));
        }
    }

    public class InterpTrackVisibility : InterpTrack
    {
        public struct VisibilityTrackKey
        {
            public int Time;
            public int Action_Type;
            public int Action_Value;
            public int ActiveCondition_Type;
            public int ActiveCondition_Value;

            public TreeNode ToTree(int index, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + Time);
                root.Nodes.Add(new TreeNode("Time : " + Time));
                root.Nodes.Add(new TreeNode("Action : " + pcc.getNameEntry(Action_Type) + ", " + pcc.getNameEntry(Action_Value)));
                root.Nodes.Add(new TreeNode("ActiveCondition : " + pcc.getNameEntry(ActiveCondition_Type) + ", " + pcc.getNameEntry(ActiveCondition_Value)));
                return root;
            }
        }

        public List<VisibilityTrackKey> VisibilityTrack;
        public bool bFireEventsWhenForwards = true;
        public bool bFireEventsWhenBackwards = true;
        public bool bFireEventsWhenJumpingForwards = true;

        public InterpTrackVisibility(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Visibility";
            GetKeyFrames();
        }

        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "bFireEventsWhenForwards")
                    bFireEventsWhenForwards = p.Value.IntValue != 0;
                else if (pcc.getNameEntry(p.Name) == "bFireEventsWhenBackwards")
                    bFireEventsWhenBackwards = p.Value.IntValue != 0;
                else if (pcc.getNameEntry(p.Name) == "bFireEventsWhenJumpingForwards")
                    bFireEventsWhenJumpingForwards = p.Value.IntValue != 0;
                else if (pcc.getNameEntry(p.Name) == "VisibilityTrack")
                {
                    VisibilityTrack = new List<VisibilityTrackKey>();
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        VisibilityTrackKey key = new VisibilityTrackKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "Time")
                                key.Time = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "Action")
                                GetByteVal(p2[i].raw, out key.Action_Type, out key.Action_Value);
                            else if (pcc.getNameEntry(p2[i].Name) == "ActiveCondition")
                                GetByteVal(p2[i].raw, out key.ActiveCondition_Type, out key.ActiveCondition_Type);
                            pos += p2[i].raw.Length;
                        }
                        VisibilityTrack.Add(key);
                    }
                }
            }
        }

        public override void GetKeyFrames()
        {
            keys = new List<PPath>();
            if (VisibilityTrack != null)
                foreach (VisibilityTrackKey k in VisibilityTrack)
                    keys.Add(GenerateKeyFrame(k.Time));
        }

        public override void ToTree()
        {
            propView.Nodes.Clear();
            propView.Nodes.Add(new TreeNode("Track Title : \"" + TrackTitle + "\""));
            TreeNode t = new TreeNode("VisibilityTrack");
            for (int i = 0; i < VisibilityTrack.Count; i++)
                t.Nodes.Add(VisibilityTrack[i].ToTree(i, pcc));
            propView.Nodes.Add(t);
            propView.Nodes.Add(new TreeNode("bFireEventsWhenForwards : " + bFireEventsWhenForwards));
            propView.Nodes.Add(new TreeNode("bFireEventsWhenBackwards : " + bFireEventsWhenBackwards));
            propView.Nodes.Add(new TreeNode("bFireEventsWhenJumpingForwards : " + bFireEventsWhenJumpingForwards));
        }
    }

    public class InterpTrackToggle : InterpTrack
    {
        public struct ToggleTrackKey
        {
            public float Time;
            public int ToggleAction_Type;
            public int ToggleAction_Value;

            public TreeNode ToTree(int index, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + Time);
                root.Nodes.Add(new TreeNode("Time : " + Time));
                root.Nodes.Add(new TreeNode("ToggleAction : " + pcc.getNameEntry(ToggleAction_Type) + ", " + pcc.getNameEntry(ToggleAction_Value)));
                return root;
            }
        }

        public List<ToggleTrackKey> ToggleTrack;

        public InterpTrackToggle(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Toggle";
            GetKeyFrames();
        }

        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "ToggleTrack")
                {
                    ToggleTrack = new List<ToggleTrackKey>();
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        ToggleTrackKey key = new ToggleTrackKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "Time")
                                key.Time = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "ToggleAction")
                                GetByteVal(p2[i].raw, out key.ToggleAction_Type, out key.ToggleAction_Value);
                            pos += p2[i].raw.Length;
                        }
                        ToggleTrack.Add(key);
                    }
                }
            }
        }

        public override void GetKeyFrames()
        {
            keys = new List<PPath>();
            if (ToggleTrack != null)
                foreach (ToggleTrackKey t in ToggleTrack)
                    keys.Add(GenerateKeyFrame(t.Time));
        }

        public override void ToTree()
        {
            propView.Nodes.Clear();
            propView.Nodes.Add(new TreeNode("Track Title : \"" + TrackTitle + "\""));
            TreeNode t = new TreeNode("ToggleTrack");
            for (int i = 0; i < ToggleTrack.Count; i++)
                t.Nodes.Add(ToggleTrack[i].ToTree(i, pcc));
            propView.Nodes.Add(t);
        }
    }

    public class InterpTrackWwiseEvent : InterpTrack
    {
        public struct WwiseEvent
        {
            public float Time;
            public int Event; //object

            public TreeNode ToTree(int index)
            {
                TreeNode root = new TreeNode(index + ": " + Time);
                root.Nodes.Add(new TreeNode("Time : " + Time));
                root.Nodes.Add(new TreeNode("Event : " + Event));
                return root;
            }
        }

        public List<WwiseEvent> WwiseEvents = new List<WwiseEvent>();

        public InterpTrackWwiseEvent(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "WwiseEvent";
            GetKeyFrames();
        }

        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "WwiseEvents")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        WwiseEvent key = new WwiseEvent();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "Time")
                                key.Time = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "Event")
                                key.Event = p2[i].Value.IntValue;
                            pos += p2[i].raw.Length;
                        }
                        WwiseEvents.Add(key);
                    }
                }
            }
        }

        public override void GetKeyFrames()
        {
            keys = new List<PPath>();
            if (WwiseEvents != null)
                foreach (WwiseEvent e in WwiseEvents)
                    keys.Add(GenerateKeyFrame(e.Time));
        }

        public override void ToTree()
        {
            propView.Nodes.Clear();
            propView.Nodes.Add(new TreeNode("Track Title : \"" + TrackTitle + "\""));
            TreeNode t = new TreeNode("WwiseEvents");
            for (int i = 0; i < WwiseEvents.Count; i++)
                t.Nodes.Add(WwiseEvents[i].ToTree(i));
            propView.Nodes.Add(t);
        }
    }

    public class InterpTrackWwiseSoundEffect : InterpTrackWwiseEvent
    {
        public InterpTrackWwiseSoundEffect(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            if (TrackTitle == "" || TrackTitle == "WwiseEvent")
                TrackTitle = "WwiseSoundEffect";
            GetKeyFrames();
        }
    }

    public class InterpTrackVectorProp : InterpTrackVectorBase
    {
        public int PropertyName; //name

        public InterpTrackVectorProp(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "" || TrackTitle == "Generic Vector Track")
                TrackTitle = "Vector Property";
            GetKeyFrames();
        }
        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "PropertyName")
                    PropertyName = p.Value.IntValue;
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            propView.Nodes.Add(new TreeNode("PropertyName : " + PropertyName));
        }
    }

    public class InterpTrackVectorMaterialParam : InterpTrackVectorBase
    {
        public struct MeshMaterialRef
        {
            public int MeshComp; //object
            public int MaterialIndex;

            public TreeNode ToTree(int index)
            {
                TreeNode root = new TreeNode(index.ToString());
                root.Nodes.Add(new TreeNode("MeshComp : " + MeshComp));
                root.Nodes.Add(new TreeNode("MaterialIndex : " + MaterialIndex));
                return root;
            }
        }

        public List<MeshMaterialRef> AffectedMaterialRefs = new List<MeshMaterialRef>();
        public int ParamName = -1; //name

        public InterpTrackVectorMaterialParam(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "" || TrackTitle == "Generic Vector Track")
                TrackTitle = "VectorMaterialParam";
            GetKeyFrames();
        }
        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "ParamName")
                    ParamName = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "AffectedMaterialRefs")
                {
                    AffectedMaterialRefs = new List<MeshMaterialRef>();
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        MeshMaterialRef key = new MeshMaterialRef();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "MeshComp")
                                key.MeshComp = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "MaterialIndex")
                                key.MaterialIndex = p2[i].Value.IntValue;
                            pos += p2[i].raw.Length;
                        }
                        AffectedMaterialRefs.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            if (ParamName != -1)
                propView.Nodes.Add(new TreeNode("ParamName : " + ParamName));
            TreeNode t = new TreeNode("AffectedMaterialRefs");
            for (int i = 0; i < AffectedMaterialRefs.Count; i++)
                t.Nodes.Add(AffectedMaterialRefs[i].ToTree(i));
            propView.Nodes.Add(t);
        }
    }

    public class InterpTrackColorProp : InterpTrackVectorBase
    {
        public int PropertyName = -1; //name

        public InterpTrackColorProp(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "" || TrackTitle == "Generic Vector Track")
                TrackTitle = "ColorProperty";
            GetKeyFrames();
        }
        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "PropertyName")
                    PropertyName = p.Value.IntValue;
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            if (PropertyName != -1)
                propView.Nodes.Add(new TreeNode("PropertyName : " + PropertyName));
        }
    }

    public class InterpTrackFloatProp : InterpTrackFloatBase
    {

        public int PropertyName; //name

        public InterpTrackFloatProp(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "" || TrackTitle == "Generic Float Track")
                TrackTitle = "Float Property";
            GetKeyFrames();
        }

        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "PropertyName")
                    PropertyName = p.Value.IntValue;
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            if (PropertyName != -1)
                propView.Nodes.Add(new TreeNode("PropertyName : " + PropertyName));
        }
    }

    public class InterpTrackFloatMaterialParam : InterpTrackFloatBase
    {
        public int ParamName; //name

        public InterpTrackFloatMaterialParam(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "" || TrackTitle == "Generic Float Track")
                TrackTitle = "Float Material Param";
            GetKeyFrames();
        }

        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "ParamName")
                    ParamName = p.Value.IntValue;
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            if (ParamName != -1)
                propView.Nodes.Add(new TreeNode("ParamName : " + ParamName));
        }
    }

    public class SFXInterpTrackClientEffect : InterpTrack
    {
        public struct ToggleKey
        {
            public float Time;
            public int ToggleAction_Type;
            public int ToggleAction_Value;

            public TreeNode ToTree(int index, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + Time);
                root.Nodes.Add(new TreeNode("Time : " + Time));
                root.Nodes.Add(new TreeNode("ToggleAction : " + pcc.getNameEntry(ToggleAction_Type) + ", " + pcc.getNameEntry(ToggleAction_Value)));
                return root;
            }
        }

        public List<ToggleKey> ToggleTrack;
        public int m_pEffect;

        public SFXInterpTrackClientEffect(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Client Effect";
            GetKeyFrames();
        }

        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_pEffect")
                    m_pEffect = p.Value.IntValue;
                else if (pcc.getNameEntry(p.Name) == "ToggleTrack")
                {
                    ToggleTrack = new List<ToggleKey>();
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        ToggleKey key = new ToggleKey();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "Time")
                                key.Time = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "ToggleAction")
                                GetByteVal(p2[i].raw, out key.ToggleAction_Type, out key.ToggleAction_Value);
                            pos += p2[i].raw.Length;
                        }
                        ToggleTrack.Add(key);
                    }
                }
            }
        }

        public override void GetKeyFrames()
        {
            keys = new List<PPath>();
            if (ToggleTrack != null)
                foreach (ToggleKey k in ToggleTrack)
                    keys.Add(GenerateKeyFrame(k.Time));
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("ToggleTrack");
            for (int i = 0; i < ToggleTrack.Count; i++)
                t.Nodes.Add(ToggleTrack[i].ToTree(i, pcc));
            propView.Nodes.Add(t);
            if (m_pEffect != -1)
                propView.Nodes.Add(new TreeNode("m_pEffect : " + m_pEffect));
        }
    }
    //Director
    public class BioEvtsysTrackDOF : BioInterpTrack
    {
        public struct DOFDatum
        {
            public float[] vFocusPosition;
            public float fFalloffExponent;
            public float fBlurKernelSize;
            public float fMaxNearBlurAmount;
            public float fMaxFarBlurAmount;
            public int cModulateBlurColor;
            public float fFocusInnerRadius;
            public float fFocusDistance;
            public float fInterpolateSeconds;
            public bool bEnableDOF;

            public TreeNode ToTree(int index, float time, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + time);
                TreeNode t = new TreeNode("vFocusPosition");
                t.Nodes.Add("" + vFocusPosition[0]);
                t.Nodes.Add("" + vFocusPosition[1]);
                t.Nodes.Add("" + vFocusPosition[2]);
                root.Nodes.Add(t);
                root.Nodes.Add(new TreeNode("fFalloffExponent : " + fFalloffExponent));
                root.Nodes.Add(new TreeNode("fBlurKernelSize : " + fBlurKernelSize));
                root.Nodes.Add(new TreeNode("fMaxNearBlurAmount : " + fMaxNearBlurAmount));
                root.Nodes.Add(new TreeNode("fMaxFarBlurAmount : " + fMaxFarBlurAmount));
                root.Nodes.Add(new TreeNode("cModulateBlurColor : " + cModulateBlurColor));
                root.Nodes.Add(new TreeNode("fFocusInnerRadius : " + fFocusInnerRadius));
                root.Nodes.Add(new TreeNode("fFocusDistance : " + fFocusDistance));
                root.Nodes.Add(new TreeNode("fInterpolateSeconds : " + fInterpolateSeconds));
                root.Nodes.Add(new TreeNode("bEnableDOF : " + bEnableDOF));
                return root;
            }
        }

        public List<DOFDatum> m_aDOFData;

        public BioEvtsysTrackDOF(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "DOF";
            GetKeyFrames();
        }

        public void LoadData()
        {
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "m_aDOFData")
                {
                    m_aDOFData = new List<DOFDatum>();
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        DOFDatum key = new DOFDatum();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "vFocusPosition")
                            {
                                key.vFocusPosition = new float[3];
                                key.vFocusPosition[0] = BitConverter.ToSingle(p2[i].raw, 32);
                                key.vFocusPosition[1] = BitConverter.ToSingle(p2[i].raw, 36);
                                key.vFocusPosition[2] = BitConverter.ToSingle(p2[i].raw, 40);

                            }
                            else if (pcc.getNameEntry(p2[i].Name) == "fFalloffExponent")
                                key.fFalloffExponent = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "fBlurKernelSize")
                                key.fBlurKernelSize = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "fMaxNearBlurAmount")
                                key.fMaxNearBlurAmount = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "fMaxFarBlurAmount")
                                key.fMaxFarBlurAmount = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "cModulateBlurColor")
                                key.cModulateBlurColor = BitConverter.ToInt32(p2[i].raw, 28);
                            else if (pcc.getNameEntry(p2[i].Name) == "fFocusInnerRadius")
                                key.fFocusInnerRadius = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "fFocusDistance")
                                key.fFocusDistance = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "fInterpolateSeconds")
                                key.fInterpolateSeconds = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "bEnableDOF")
                                key.bEnableDOF = p2[i].Value.IntValue != 0;
                            pos += p2[i].raw.Length;
                        }
                        m_aDOFData.Add(key);
                    }
                }
            }
        }

        public override void ToTree()
        {
            base.ToTree();
            TreeNode t = new TreeNode("m_aDOFData");
            for (int i = 0; i < m_aDOFData.Count; i++)
                t.Nodes.Add(m_aDOFData[i].ToTree(i, m_aTrackKeys[i].fTime, pcc));
            propView.Nodes.Add(t);
        }
    }

    public class InterpTrackDirector : InterpTrack
    {
        public struct DirectorTrackCut
        {
            public int TargetCamGroup; //name
            public float Time;
            public float TransitionTime;
            public bool bSkipCameraReset;

            public TreeNode ToTree(int index, PCCObject pcc)
            {
                TreeNode root = new TreeNode(index + ": " + Time);
                root.Nodes.Add(new TreeNode("TargetCamGroup : " + pcc.getNameEntry(TargetCamGroup)));
                root.Nodes.Add(new TreeNode("Time : " + Time));
                root.Nodes.Add(new TreeNode("TransitionTime : " + TransitionTime));
                root.Nodes.Add(new TreeNode("bSkipCameraReset : " + bSkipCameraReset));
                return root;
            }
        }

        public List<DirectorTrackCut> CutTrack;

        public InterpTrackDirector(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            LoadData();
            if (TrackTitle == "")
                TrackTitle = "Director";
            GetKeyFrames();
        }


        public void LoadData()
        {
            CutTrack = new List<DirectorTrackCut>();
            byte[] buff = pcc.Exports[index].Data;
            BitConverter.IsLittleEndian = true;
            List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
            foreach (PropertyReader.Property p in props)
            {
                if (pcc.getNameEntry(p.Name) == "CutTrack")
                {
                    int pos = 28;
                    int count = BitConverter.ToInt32(p.raw, 24);
                    for (int j = 0; j < count; j++)
                    {
                        List<PropertyReader.Property> p2 = PropertyReader.ReadProp(pcc, p.raw, pos);
                        DirectorTrackCut key = new DirectorTrackCut();
                        for (int i = 0; i < p2.Count(); i++)
                        {
                            if (pcc.getNameEntry(p2[i].Name) == "TargetCamGroup")
                                key.TargetCamGroup = p2[i].Value.IntValue;
                            else if (pcc.getNameEntry(p2[i].Name) == "Time")
                                key.Time = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "TransitionTime")
                                key.TransitionTime = BitConverter.ToSingle(p2[i].raw, 24);
                            else if (pcc.getNameEntry(p2[i].Name) == "bSkipCameraReset")
                                key.bSkipCameraReset = p2[i].Value.IntValue != 0;
                            pos += p2[i].raw.Length;
                        }
                        CutTrack.Add(key);
                    }
                }
            }
        }

        public override void GetKeyFrames()
        {
            keys = new List<PPath>();
            if (CutTrack != null)
                foreach (DirectorTrackCut c in CutTrack)
                    keys.Add(GenerateKeyFrame(c.Time));
        }

        public override void ToTree()
        {
            propView.Nodes.Clear();
            propView.Nodes.Add(new TreeNode("Track Title : \"" + TrackTitle + "\""));
            TreeNode t = new TreeNode("CutTrack");
            for (int i = 0; i < CutTrack.Count; i++)
                t.Nodes.Add(CutTrack[i].ToTree(i, pcc));
            propView.Nodes.Add(t);
        }
    }

    public class InterpTrackFade : InterpTrackFloatBase
    {

        public InterpTrackFade(int idx, PCCObject pccobj)
            : base(idx, pccobj)
        {
            //LoadData();
            if (TrackTitle == "" || TrackTitle == "Generic Float Track")
                TrackTitle = "Fade";
            GetKeyFrames();
        }

        //public void LoadData()
        //{
        //    byte[] buff = pcc.Exports[index].Data;
        //    BitConverter.IsLittleEndian = true;
        //    List<PropertyReader.Property> props = PropertyReader.getPropList(pcc, pcc.Exports[index].Data);
        //    foreach (PropertyReader.Property p in props)
        //    {
        //    }
        //}
    }

}