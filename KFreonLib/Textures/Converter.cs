using ImageEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KFreonLib.Textures
{
    public partial class Converter : Form
    {
        KFreonImage LoadedImage;
        public Converter()
        {
            InitializeComponent();

            foreach (string format in ImageEngine.ImageEngine.ValidFormats)
                FormatBox.Items.Add(format);

            this.Height = 170;
            this.Width = 180;
        }

        private async void LoadButton_Click(object sender, EventArgs e)
        {
            string filename = null;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    return;
                filename = ofd.FileName;
            }
            LoadedImage = new KFreonImage(filename);

            string message = "File loaded: " + filename + Environment.NewLine + Environment.NewLine;
            message += "Format: " + LoadedImage.Format + Environment.NewLine;
            message += "Mips: " + LoadedImage.Mips + Environment.NewLine;

            Bitmap bmp = LoadedImage.ToBitmap(LoadedImage.Width, LoadedImage.Height);

            Transitions.Transition.run(this, "Width", 584, new Transitions.TransitionType_Deceleration(500));
            Transitions.Transition.run(this, "Height", 546, new Transitions.TransitionType_Deceleration(500));

            await Task.Run(() =>
            {
                System.Threading.Thread.Sleep(500);
            });

            CurrentDisplayBox.Text = message;
            MainPictureBox.Image = bmp;
        }

        private void ConvertButton_Click(object sender, EventArgs e)
        {

        }

        private void CancellationButton_Click(object sender, EventArgs e)
        {
            if (LoadedImage != null)
                LoadedImage.Dispose();
            ImageEngine.ImageEngine.Shutdown();
            this.Close();
        }
    }
}
