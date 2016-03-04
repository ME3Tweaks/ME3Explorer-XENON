﻿namespace ME1Explorer
{
    partial class MainWindow
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pCCEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveGameEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveGameOperatorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openDebugWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolsToolStripMenuItem,
            this.optionToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(883, 24);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pCCEditorToolStripMenuItem,
            this.saveGameEditorToolStripMenuItem,
            this.saveGameOperatorToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // pCCEditorToolStripMenuItem
            // 
            this.pCCEditorToolStripMenuItem.Name = "pCCEditorToolStripMenuItem";
            this.pCCEditorToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.pCCEditorToolStripMenuItem.Text = "Package Editor";
            this.pCCEditorToolStripMenuItem.Click += new System.EventHandler(this.pCCEditorToolStripMenuItem_Click);
            // 
            // saveGameEditorToolStripMenuItem
            // 
            this.saveGameEditorToolStripMenuItem.Name = "saveGameEditorToolStripMenuItem";
            this.saveGameEditorToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.saveGameEditorToolStripMenuItem.Text = "Save Game Editor";
            this.saveGameEditorToolStripMenuItem.Click += new System.EventHandler(this.saveGameEditorToolStripMenuItem_Click);
            // 
            // saveGameOperatorToolStripMenuItem
            // 
            this.saveGameOperatorToolStripMenuItem.Name = "saveGameOperatorToolStripMenuItem";
            this.saveGameOperatorToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.saveGameOperatorToolStripMenuItem.Text = "Save Game Operator";
            this.saveGameOperatorToolStripMenuItem.Click += new System.EventHandler(this.saveGameOperatorToolStripMenuItem_Click);
            // 
            // optionToolStripMenuItem
            // 
            this.optionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openDebugWindowToolStripMenuItem});
            this.optionToolStripMenuItem.Name = "optionToolStripMenuItem";
            this.optionToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.optionToolStripMenuItem.Text = "Option";
            // 
            // openDebugWindowToolStripMenuItem
            // 
            this.openDebugWindowToolStripMenuItem.Name = "openDebugWindowToolStripMenuItem";
            this.openDebugWindowToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.openDebugWindowToolStripMenuItem.Text = "Open Debug Window";
            this.openDebugWindowToolStripMenuItem.Click += new System.EventHandler(this.openDebugWindowToolStripMenuItem_Click);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(883, 453);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainWindow";
            this.Text = "ME1 Explorer by Warranty Voider";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form_Closing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pCCEditorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveGameEditorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openDebugWindowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveGameOperatorToolStripMenuItem;
    }
}



