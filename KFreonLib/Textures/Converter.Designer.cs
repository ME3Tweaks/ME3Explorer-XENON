namespace KFreonLib.Textures
{
    partial class Converter
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Converter));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.MainSplitter = new System.Windows.Forms.SplitContainer();
            this.GenerateMipsCheckBox = new System.Windows.Forms.CheckBox();
            this.CancellationButton = new System.Windows.Forms.Button();
            this.ConvertButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.FormatBox = new System.Windows.Forms.ComboBox();
            this.CurrentDisplayBox = new System.Windows.Forms.RichTextBox();
            this.MainPictureBox = new System.Windows.Forms.PictureBox();
            this.LoadButton = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MainSplitter)).BeginInit();
            this.MainSplitter.Panel1.SuspendLayout();
            this.MainSplitter.Panel2.SuspendLayout();
            this.MainSplitter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LoadButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(576, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // MainSplitter
            // 
            this.MainSplitter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainSplitter.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.MainSplitter.Location = new System.Drawing.Point(0, 25);
            this.MainSplitter.Name = "MainSplitter";
            // 
            // MainSplitter.Panel1
            // 
            this.MainSplitter.Panel1.Controls.Add(this.GenerateMipsCheckBox);
            this.MainSplitter.Panel1.Controls.Add(this.CancellationButton);
            this.MainSplitter.Panel1.Controls.Add(this.ConvertButton);
            this.MainSplitter.Panel1.Controls.Add(this.label1);
            this.MainSplitter.Panel1.Controls.Add(this.FormatBox);
            this.MainSplitter.Panel1MinSize = 0;
            // 
            // MainSplitter.Panel2
            // 
            this.MainSplitter.Panel2.Controls.Add(this.CurrentDisplayBox);
            this.MainSplitter.Panel2.Controls.Add(this.MainPictureBox);
            this.MainSplitter.Panel2MinSize = 0;
            this.MainSplitter.Size = new System.Drawing.Size(576, 490);
            this.MainSplitter.SplitterDistance = 175;
            this.MainSplitter.SplitterWidth = 1;
            this.MainSplitter.TabIndex = 1;
            // 
            // GenerateMipsCheckBox
            // 
            this.GenerateMipsCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.GenerateMipsCheckBox.AutoSize = true;
            this.GenerateMipsCheckBox.Checked = true;
            this.GenerateMipsCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.GenerateMipsCheckBox.Location = new System.Drawing.Point(29, 56);
            this.GenerateMipsCheckBox.Name = "GenerateMipsCheckBox";
            this.GenerateMipsCheckBox.Size = new System.Drawing.Size(95, 17);
            this.GenerateMipsCheckBox.TabIndex = 8;
            this.GenerateMipsCheckBox.Text = "Generate Mips";
            this.GenerateMipsCheckBox.UseVisualStyleBackColor = true;
            // 
            // CancellationButton
            // 
            this.CancellationButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CancellationButton.Location = new System.Drawing.Point(95, 455);
            this.CancellationButton.Name = "CancellationButton";
            this.CancellationButton.Size = new System.Drawing.Size(75, 23);
            this.CancellationButton.TabIndex = 5;
            this.CancellationButton.Text = "Cancel";
            this.CancellationButton.UseVisualStyleBackColor = true;
            // 
            // ConvertButton
            // 
            this.ConvertButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ConvertButton.Location = new System.Drawing.Point(3, 455);
            this.ConvertButton.Name = "ConvertButton";
            this.ConvertButton.Size = new System.Drawing.Size(75, 23);
            this.ConvertButton.TabIndex = 4;
            this.ConvertButton.Text = "Convert!";
            this.ConvertButton.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(66, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Format";
            // 
            // FormatBox
            // 
            this.FormatBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.FormatBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.FormatBox.FormattingEnabled = true;
            this.FormatBox.Location = new System.Drawing.Point(29, 29);
            this.FormatBox.MaxDropDownItems = 20;
            this.FormatBox.Name = "FormatBox";
            this.FormatBox.Size = new System.Drawing.Size(121, 21);
            this.FormatBox.TabIndex = 0;
            // 
            // CurrentDisplayBox
            // 
            this.CurrentDisplayBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.CurrentDisplayBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.CurrentDisplayBox.Location = new System.Drawing.Point(0, 0);
            this.CurrentDisplayBox.Name = "CurrentDisplayBox";
            this.CurrentDisplayBox.ReadOnly = true;
            this.CurrentDisplayBox.Size = new System.Drawing.Size(400, 109);
            this.CurrentDisplayBox.TabIndex = 1;
            this.CurrentDisplayBox.Text = "";
            // 
            // MainPictureBox
            // 
            this.MainPictureBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.MainPictureBox.Location = new System.Drawing.Point(0, 90);
            this.MainPictureBox.Name = "MainPictureBox";
            this.MainPictureBox.Size = new System.Drawing.Size(400, 400);
            this.MainPictureBox.TabIndex = 0;
            this.MainPictureBox.TabStop = false;
            // 
            // LoadButton
            // 
            this.LoadButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.LoadButton.Image = ((System.Drawing.Image)(resources.GetObject("LoadButton.Image")));
            this.LoadButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.LoadButton.Name = "LoadButton";
            this.LoadButton.Size = new System.Drawing.Size(37, 22);
            this.LoadButton.Text = "Load";
            this.LoadButton.Click += new System.EventHandler(this.LoadButton_Click);
            // 
            // Converter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(576, 515);
            this.Controls.Add(this.MainSplitter);
            this.Controls.Add(this.toolStrip1);
            this.Name = "Converter";
            this.Text = "Converter";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.MainSplitter.Panel1.ResumeLayout(false);
            this.MainSplitter.Panel1.PerformLayout();
            this.MainSplitter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MainSplitter)).EndInit();
            this.MainSplitter.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton LoadButton;
        private System.Windows.Forms.SplitContainer MainSplitter;
        private System.Windows.Forms.CheckBox GenerateMipsCheckBox;
        private System.Windows.Forms.Button CancellationButton;
        private System.Windows.Forms.Button ConvertButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox FormatBox;
        private System.Windows.Forms.RichTextBox CurrentDisplayBox;
        private System.Windows.Forms.PictureBox MainPictureBox;

    }
}