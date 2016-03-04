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

namespace ResIL
{
    public partial class Converter : Form
    {
        //string format = null;
        string OrigPath = null;
        //string SavePath = null;
        KFreonImage image;


        public Converter(string format = null, string originalPath = null)
        {
            InitializeComponent();

            // KFreon: Set labels visibility
            OldFormatLabel.Text = format ?? "";
            OldPathBox.Text = originalPath ?? "";
            HeightLabel.Text = "";
            WidthLabel.Text = "";

            // KFreon: Set format dropdown
            foreach (string formats in ImageEngine.ImageEngine.ValidFormats)
                FormatBox.Items.Add(formats);

            // KFreon: Load image
            if (originalPath != null)
                LoadImage(originalPath);
            SavingButton.Visible = format != null;
        }

        private void SavingButton_Click(object sender, EventArgs e)
        {
            try
            {
                image.ChangeFormat(FormatBox.SelectedItem.ToString());
                image.Save(SavePathBox.Text, MipsBox.Checked);
                MessageBox.Show("Image saved!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select source image";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    return;

                OrigPath = ofd.FileName;
            }

            LoadImage(OrigPath);
        }

        private void LoadImage(string filepath)
        {
            OldPathBox.Text = filepath;
            Task.Run(() =>
            {
                image = new KFreonImage(OrigPath);
                List<string> details = FormatImageDetails(image);
                this.Invoke(new Action(() =>
                    {
                        OldFormatLabel.Text = image.Format;
                        DetailsBox.Text = String.Join(Environment.NewLine, details);
                        HeightLabel.Text = image.Height.ToString();
                        WidthLabel.Text = image.Width.ToString();
                    }));
                DrawPreviewer();
            });
        }

        private List<string> FormatImageDetails(KFreonImage img)
        {
            List<string> details = new List<string>();
            ResIL.Unmanaged.ImageInfo info = img.info;

            details.Add("Bits Per Pixel: " + info.BitsPerPixel);
            details.Add("Bytes Per Pixel: " + info.BytesPerPixel);
            details.Add("Channels: " + info.Channels);
            details.Add("Cube Flags: " + info.CubeFlags);
            details.Add("Data Type: " + info.DataType);
            details.Add("Depth: " + info.Depth);
            details.Add("Duration: " + info.Duration);
            details.Add("DXTC Format: " + (img.Format == "V8U8" ? "V8U8" : info.DxtcFormat.ToString()));
            details.Add("Face Count: " + info.FaceCount);
            details.Add("Format: " + info.Format);
            details.Add("HasDXTC: " + info.HasDXTC);
            details.Add("Has Palette: " + info.HasPalette);
            details.Add("Height: " + info.Height);
            details.Add("Image Count: " + info.ImageCount);
            details.Add("Is Cube Map?: " + info.IsCubeMap);
            details.Add("Is Sphere Map?: " + info.IsSphereMap);
            details.Add("Layer Count: " + info.LayerCount);
            details.Add("MipMap Count: " + info.MipMapCount);
            details.Add("OffsetX: " + info.OffsetX);
            details.Add("OffsetY: " + info.OffsetY);
            details.Add("Origin: " + info.Origin);
            details.Add("Palette Base Type: " + info.PaletteBaseType);
            details.Add("Palette Bytes Per Pixel: " + info.PaletteBytesPerPixel);
            details.Add("Palette Column Count: " + info.PaletteColumnCount);
            details.Add("Palette Type: " + info.PaletteType);
            details.Add("Plane Size: " + info.PlaneSize);
            details.Add("Size of Data: " + info.SizeOfData);
            details.Add("Width: " + info.Width);

            return details;
        }

        private void DrawPreviewer()
        {
            // KFreon: Get image of correct size
            int determiningDimension = Previewer.Width > Previewer.Height ? Previewer.Height : Previewer.Width;
            Bitmap bmp = image.ToBitmap(determiningDimension, determiningDimension);

            // KFreon: Show preview
            Previewer.Invoke(new Action(() =>
            {
                if (Previewer.Image != null)
                    Previewer.Image.Dispose();

                Previewer.Image = bmp;
            }));
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(OrigPath))
                return;

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Select save path";
                sfd.Filter = GetFilterString(ImageEngine.ImageEngine.GetExtensionFromFormat(FormatBox.SelectedItem != null ? FormatBox.SelectedItem.ToString() : image.Format));

                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    return;

                //SavePath = sfd.FileName;
                SavePathBox.Text = sfd.FileName;
            }

            SavingButton.Visible = !String.IsNullOrEmpty(SavePathBox.Text) && FormatBox.SelectedItem != null;
        }

        private string GetFilterString(string ext)
        {
            string retval = "";

            switch (ext.ToLowerInvariant())
            {
                case ".dds":
                    retval = "DirectX Images|*.dds";
                    break;
                case ".jpg":
                    retval = "JPEG Images|*.jpg";
                    break;
                case ".bmp":
                    retval = "Bitmap Images|*.bmp";
                    break;
                case ".png":
                    retval = "Portable Network Graphics|*.png";
                    break;
                case ".gif":
                    retval = "Graphics Interchange Format|*.gif";
                    break;
            }

            return retval;
        }

        private void FormatBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SavingButton.Visible = !String.IsNullOrEmpty(SavePathBox.Text);

            string form = FormatBox.SelectedItem.ToString();
            string ext = ImageEngine.ImageEngine.GetExtensionFromFormat(form);

            string temp = Path.ChangeExtension(SavePathBox.Text, ext);
            SavePathBox.Text = temp;
        }

        private void Form_ResizeEnd(object sender, EventArgs e)
        {
            if (image != null)
                Task.Run(() => DrawPreviewer());
        }
    }
}
