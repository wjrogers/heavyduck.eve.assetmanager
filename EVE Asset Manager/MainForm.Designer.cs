namespace HeavyDuck.EveAssetManager
{
    partial class MainForm
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
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.menu_file = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_options = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_options_keys = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_options_refresh = new System.Windows.Forms.ToolStripMenuItem();
            this.grid = new System.Windows.Forms.DataGridView();
            this.menuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menu_file,
            this.menu_options});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.menuStrip.Size = new System.Drawing.Size(389, 24);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip";
            // 
            // menu_file
            // 
            this.menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menu_file_exit});
            this.menu_file.Name = "menu_file";
            this.menu_file.Size = new System.Drawing.Size(35, 20);
            this.menu_file.Text = "&File";
            // 
            // menu_file_exit
            // 
            this.menu_file_exit.Name = "menu_file_exit";
            this.menu_file_exit.Size = new System.Drawing.Size(103, 22);
            this.menu_file_exit.Text = "E&xit";
            // 
            // menu_options
            // 
            this.menu_options.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menu_options_refresh,
            this.menu_options_keys});
            this.menu_options.Name = "menu_options";
            this.menu_options.Size = new System.Drawing.Size(56, 20);
            this.menu_options.Text = "&Options";
            // 
            // menu_options_keys
            // 
            this.menu_options_keys.Name = "menu_options_keys";
            this.menu_options_keys.Size = new System.Drawing.Size(169, 22);
            this.menu_options_keys.Text = "Manage API Keys";
            // 
            // menu_options_refresh
            // 
            this.menu_options_refresh.Name = "menu_options_refresh";
            this.menu_options_refresh.Size = new System.Drawing.Size(169, 22);
            this.menu_options_refresh.Text = "Refresh Assets";
            // 
            // grid
            // 
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(0, 24);
            this.grid.Name = "grid";
            this.grid.Size = new System.Drawing.Size(389, 311);
            this.grid.TabIndex = 1;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(389, 335);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "MainForm";
            this.Text = "EVE Asset Manager";
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem menu_file;
        private System.Windows.Forms.ToolStripMenuItem menu_file_exit;
        private System.Windows.Forms.ToolStripMenuItem menu_options;
        private System.Windows.Forms.ToolStripMenuItem menu_options_keys;
        private System.Windows.Forms.ToolStripMenuItem menu_options_refresh;
        private System.Windows.Forms.DataGridView grid;
    }
}