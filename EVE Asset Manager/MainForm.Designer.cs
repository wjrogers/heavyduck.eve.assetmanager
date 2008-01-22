namespace HeavyDuck.Eve.AssetManager
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.grid = new System.Windows.Forms.DataGridView();
            this.toolbar = new System.Windows.Forms.ToolStrip();
            this.search_panel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            // 
            // grid
            // 
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(0, 167);
            this.grid.Name = "grid";
            this.grid.Size = new System.Drawing.Size(772, 389);
            this.grid.TabIndex = 2;
            // 
            // toolbar
            // 
            this.toolbar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolbar.Location = new System.Drawing.Point(0, 0);
            this.toolbar.Name = "toolbar";
            this.toolbar.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.toolbar.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.toolbar.Size = new System.Drawing.Size(772, 25);
            this.toolbar.TabIndex = 0;
            // 
            // search_panel
            // 
            this.search_panel.Dock = System.Windows.Forms.DockStyle.Top;
            this.search_panel.Location = new System.Drawing.Point(0, 25);
            this.search_panel.Name = "search_panel";
            this.search_panel.Size = new System.Drawing.Size(772, 142);
            this.search_panel.TabIndex = 1;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(772, 556);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.search_panel);
            this.Controls.Add(this.toolbar);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.Text = "EVE Asset Manager";
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.ToolStrip toolbar;
        private System.Windows.Forms.Panel search_panel;
    }
}