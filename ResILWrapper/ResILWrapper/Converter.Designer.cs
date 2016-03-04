namespace ResIL
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
            this.Previewer = new System.Windows.Forms.PictureBox();
            this.LoadButton = new System.Windows.Forms.Button();
            this.MainSplitter = new System.Windows.Forms.SplitContainer();
            this.MipsBox = new System.Windows.Forms.CheckBox();
            this.OldPathBox = new System.Windows.Forms.RichTextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.OldFormatLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.BrowseButton = new System.Windows.Forms.Button();
            this.SavePathBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.FormatBox = new System.Windows.Forms.ComboBox();
            this.SavingButton = new System.Windows.Forms.Button();
            this.DetailsBox = new System.Windows.Forms.RichTextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.WidthLabel = new System.Windows.Forms.Label();
            this.HeightLabel = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.Previewer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MainSplitter)).BeginInit();
            this.MainSplitter.Panel1.SuspendLayout();
            this.MainSplitter.Panel2.SuspendLayout();
            this.MainSplitter.SuspendLayout();
            this.SuspendLayout();
            // 
            // Previewer
            // 
            this.Previewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Previewer.Location = new System.Drawing.Point(0, 0);
            this.Previewer.Name = "Previewer";
            this.Previewer.Size = new System.Drawing.Size(365, 488);
            this.Previewer.TabIndex = 0;
            this.Previewer.TabStop = false;
            // 
            // LoadButton
            // 
            this.LoadButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LoadButton.Location = new System.Drawing.Point(4, 495);
            this.LoadButton.Name = "LoadButton";
            this.LoadButton.Size = new System.Drawing.Size(75, 23);
            this.LoadButton.TabIndex = 1;
            this.LoadButton.Text = "Load";
            this.LoadButton.UseVisualStyleBackColor = true;
            this.LoadButton.Click += new System.EventHandler(this.LoadButton_Click);
            // 
            // MainSplitter
            // 
            this.MainSplitter.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainSplitter.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.MainSplitter.Location = new System.Drawing.Point(1, 1);
            this.MainSplitter.Name = "MainSplitter";
            // 
            // MainSplitter.Panel1
            // 
            this.MainSplitter.Panel1.Controls.Add(this.label5);
            this.MainSplitter.Panel1.Controls.Add(this.DetailsBox);
            this.MainSplitter.Panel1.Controls.Add(this.MipsBox);
            this.MainSplitter.Panel1.Controls.Add(this.OldPathBox);
            this.MainSplitter.Panel1.Controls.Add(this.label4);
            this.MainSplitter.Panel1.Controls.Add(this.label3);
            this.MainSplitter.Panel1.Controls.Add(this.OldFormatLabel);
            this.MainSplitter.Panel1.Controls.Add(this.label2);
            this.MainSplitter.Panel1.Controls.Add(this.BrowseButton);
            this.MainSplitter.Panel1.Controls.Add(this.SavePathBox);
            this.MainSplitter.Panel1.Controls.Add(this.label1);
            this.MainSplitter.Panel1.Controls.Add(this.FormatBox);
            // 
            // MainSplitter.Panel2
            // 
            this.MainSplitter.Panel2.Controls.Add(this.Previewer);
            this.MainSplitter.Size = new System.Drawing.Size(750, 488);
            this.MainSplitter.SplitterDistance = 384;
            this.MainSplitter.SplitterWidth = 1;
            this.MainSplitter.TabIndex = 2;
            // 
            // MipsBox
            // 
            this.MipsBox.AutoSize = true;
            this.MipsBox.Checked = true;
            this.MipsBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.MipsBox.Location = new System.Drawing.Point(17, 63);
            this.MipsBox.Name = "MipsBox";
            this.MipsBox.Size = new System.Drawing.Size(101, 17);
            this.MipsBox.TabIndex = 10;
            this.MipsBox.Text = "Generate Mips?";
            this.MipsBox.UseVisualStyleBackColor = true;
            // 
            // OldPathBox
            // 
            this.OldPathBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.OldPathBox.Location = new System.Drawing.Point(14, 99);
            this.OldPathBox.Name = "OldPathBox";
            this.OldPathBox.ReadOnly = true;
            this.OldPathBox.Size = new System.Drawing.Size(365, 51);
            this.OldPathBox.TabIndex = 9;
            this.OldPathBox.Text = "";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(14, 83);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Image Path";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(14, 153);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(102, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Save destination";
            // 
            // OldFormatLabel
            // 
            this.OldFormatLabel.AutoSize = true;
            this.OldFormatLabel.Location = new System.Drawing.Point(49, 27);
            this.OldFormatLabel.Name = "OldFormatLabel";
            this.OldFormatLabel.Size = new System.Drawing.Size(35, 13);
            this.OldFormatLabel.TabIndex = 6;
            this.OldFormatLabel.Text = "label3";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(49, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(68, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Old Format";
            // 
            // BrowseButton
            // 
            this.BrowseButton.Location = new System.Drawing.Point(304, 167);
            this.BrowseButton.Name = "BrowseButton";
            this.BrowseButton.Size = new System.Drawing.Size(75, 23);
            this.BrowseButton.TabIndex = 4;
            this.BrowseButton.Text = "Browse";
            this.BrowseButton.UseVisualStyleBackColor = true;
            this.BrowseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // SavePathBox
            // 
            this.SavePathBox.Location = new System.Drawing.Point(3, 169);
            this.SavePathBox.Name = "SavePathBox";
            this.SavePathBox.Size = new System.Drawing.Size(295, 20);
            this.SavePathBox.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(194, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "New Format";
            // 
            // FormatBox
            // 
            this.FormatBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.FormatBox.FormattingEnabled = true;
            this.FormatBox.Location = new System.Drawing.Point(197, 24);
            this.FormatBox.Name = "FormatBox";
            this.FormatBox.Size = new System.Drawing.Size(121, 21);
            this.FormatBox.TabIndex = 0;
            this.FormatBox.SelectedIndexChanged += new System.EventHandler(this.FormatBox_SelectedIndexChanged);
            // 
            // SavingButton
            // 
            this.SavingButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.SavingButton.Location = new System.Drawing.Point(319, 495);
            this.SavingButton.Name = "SavingButton";
            this.SavingButton.Size = new System.Drawing.Size(75, 23);
            this.SavingButton.TabIndex = 3;
            this.SavingButton.Text = "Save";
            this.SavingButton.UseVisualStyleBackColor = true;
            this.SavingButton.Click += new System.EventHandler(this.SavingButton_Click);
            // 
            // DetailsBox
            // 
            this.DetailsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DetailsBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.DetailsBox.Location = new System.Drawing.Point(0, 208);
            this.DetailsBox.Name = "DetailsBox";
            this.DetailsBox.ReadOnly = true;
            this.DetailsBox.Size = new System.Drawing.Size(379, 280);
            this.DetailsBox.TabIndex = 11;
            this.DetailsBox.Text = "";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(11, 192);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(57, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Raw info";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(453, 503);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(43, 15);
            this.label6.TabIndex = 13;
            this.label6.Text = "Width";
            // 
            // WidthLabel
            // 
            this.WidthLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.WidthLabel.AutoSize = true;
            this.WidthLabel.Location = new System.Drawing.Point(502, 505);
            this.WidthLabel.Name = "WidthLabel";
            this.WidthLabel.Size = new System.Drawing.Size(54, 13);
            this.WidthLabel.TabIndex = 14;
            this.WidthLabel.Text = "widthlabel";
            // 
            // HeightLabel
            // 
            this.HeightLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.HeightLabel.AutoSize = true;
            this.HeightLabel.Location = new System.Drawing.Point(693, 503);
            this.HeightLabel.Name = "HeightLabel";
            this.HeightLabel.Size = new System.Drawing.Size(58, 13);
            this.HeightLabel.TabIndex = 15;
            this.HeightLabel.Text = "heightlabel";
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(638, 501);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(49, 15);
            this.label9.TabIndex = 16;
            this.label9.Text = "Height";
            // 
            // Converter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(749, 528);
            this.Controls.Add(this.WidthLabel);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.SavingButton);
            this.Controls.Add(this.HeightLabel);
            this.Controls.Add(this.MainSplitter);
            this.Controls.Add(this.LoadButton);
            this.Controls.Add(this.label6);
            this.Name = "Converter";
            this.Text = "Image Converter";
            this.ResizeEnd += new System.EventHandler(this.Form_ResizeEnd);
            ((System.ComponentModel.ISupportInitialize)(this.Previewer)).EndInit();
            this.MainSplitter.Panel1.ResumeLayout(false);
            this.MainSplitter.Panel1.PerformLayout();
            this.MainSplitter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MainSplitter)).EndInit();
            this.MainSplitter.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox Previewer;
        private System.Windows.Forms.Button LoadButton;
        private System.Windows.Forms.SplitContainer MainSplitter;
        private System.Windows.Forms.Button BrowseButton;
        private System.Windows.Forms.TextBox SavePathBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox FormatBox;
        private System.Windows.Forms.Label OldFormatLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button SavingButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RichTextBox OldPathBox;
        private System.Windows.Forms.CheckBox MipsBox;
        private System.Windows.Forms.RichTextBox DetailsBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label HeightLabel;
        private System.Windows.Forms.Label WidthLabel;
        private System.Windows.Forms.Label label6;
    }
}

