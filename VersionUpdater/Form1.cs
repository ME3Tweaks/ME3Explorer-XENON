using SharpSvn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VersionUpdater
{
    public partial class Form1 : Form
    {
        private string SvnSubfolder
        {
            get
            {
                string execpath = Application.ExecutablePath;
                for (int i = 0; i < 4; i++)
                    execpath = Path.GetDirectoryName(execpath);
                return execpath;
            }
        }

        int RevisionNumber
        {
            get
            {
                SvnClient svn = new SvnClient();
                SvnInfoEventArgs info;
                svn.GetInfo(SvnSubfolder, out info);
                this.Invoke(new Action(() =>
                {
                    richTextBox1.AppendText(SvnSubfolder + Environment.NewLine);
                    richTextBox1.AppendText(info.Revision.ToString());
                }));
                return (int)info.Revision;
            }
        }

        public Form1()
        {
            InitializeComponent();
            Task.Factory.StartNew(() => ThreadStart());
        }


        public void ThreadStart()
        {

            /*this.Invoke(new Action(() => richTextBox1.AppendText("Sleeping\n")));
            Application.DoEvents();*/
            System.Threading.Thread.Sleep(100);

            IncrementVersion();
            this.Invoke(new Action(() => richTextBox1.AppendText("Finished")));
            Application.Exit();
        }


        /// <summary>
        /// Increments version number in ME3Explorer\Properties\AssemblyInfo.cs
        /// </summary>
        public void IncrementVersion()
        {
            this.Invoke(new Action(() => richTextBox1.AppendText("Beginning...\n")));
            List<string> parts = new List<string>(Path.GetDirectoryName(Application.ExecutablePath).Split('\\'));
            parts.RemoveAt(parts.Count - 1);
            parts.RemoveAt(parts.Count - 1);
            parts.RemoveAt(parts.Count - 1);
            //parts.RemoveAt(parts.Count - 1);
            this.Invoke(new Action(() => richTextBox1.AppendText("Thing: " + String.Join("\\", parts.ToArray()) + Environment.NewLine)));
            List<string> parts2 = new List<string>(parts);
            parts.Add("ME3Explorer");
            parts2.Add("KFreonLib");
            string ExecPath = String.Join("\\", parts.ToArray());
            string ExecPath2 = String.Join("\\", parts2.ToArray());
            this.Invoke(new Action(() => richTextBox1.AppendText("exec paths: " + ExecPath + Environment.NewLine)));
            this.Invoke(new Action(() => richTextBox1.AppendText("exec paths: " + ExecPath2 + Environment.NewLine)));
            this.Invoke(new Action(() => richTextBox1.AppendText("Incrementing version number...\n")));

            int revision = RevisionNumber + 1;
            this.Invoke(new Action(() => richTextBox1.AppendText("Rev: " + revision + Environment.NewLine)));
            if (revision == 0)
            {
                this.Invoke(new Action(() => richTextBox1.AppendText("Failed to get local rev details.")));
                return;
            }

            // KFreon: Incrememnt build version
            string FilePath = ExecPath + "\\Properties\\AssemblyInfo.cs";
            string FilePath2 = ExecPath2 + "\\Properties\\AssemblyInfo.cs";
            this.Invoke(new Action(() => richTextBox1.AppendText("file paths: " + FilePath + Environment.NewLine)));
            this.Invoke(new Action(() => richTextBox1.AppendText("file paths: " + FilePath2 + Environment.NewLine)));


            // KFreon: Check if file exists
            if (!File.Exists(FilePath) || !File.Exists(FilePath2))
            {
                this.Invoke(new Action(() => richTextBox1.AppendText(" doesn't exist." + Environment.NewLine)));
                return;
            }

            this.Invoke(new Action(() => richTextBox1.AppendText("Reading current file...\n")));

            // KFreon: Read lines
            List<string> lines = null;
            List<string> lines2 = null;
            try
            {
                lines = new List<string>(File.ReadAllLines(FilePath));
                lines2 = new List<string>(File.ReadAllLines(FilePath2));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }


            // KFreon: Increment version
            for (int i = 0; i < 2; i++)
            {
                parts = new List<string>(lines[lines.Count - (2 - i)].Split('.'));
                int version = Convert.ToInt32(parts[parts.Count - 1].Split('"')[0]);
                parts[parts.Count - 2] = revision.ToString();
                parts2[parts2.Count - 2] = revision.ToString();
                version++;

                parts[parts.Count - 1] = version.ToString() + '"' + ")]";
                string newline = string.Join(".", parts.ToArray());
                lines[lines.Count - (2 - i)] = newline;
                lines2[lines2.Count - (2 - i)] = newline;
            }

            this.Invoke(new Action(() => richTextBox1.AppendText("Writing current version back...\n")));

            // KFreon: Write lines back to file
            try
            {
                File.WriteAllLines(FilePath, lines);
                File.WriteAllLines(FilePath2, lines2);
            }
            catch (Exception e)
            {
                this.Invoke(new Action(() => richTextBox1.AppendText("exception: " + e.Message + Environment.NewLine)));
            }
        }
    }
}
