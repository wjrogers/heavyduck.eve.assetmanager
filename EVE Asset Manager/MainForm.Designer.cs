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
            this.reset_button = new System.Windows.Forms.Button();
            this.search_button = new System.Windows.Forms.Button();
            this.search_location = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.search_owner = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.search_group = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.search_name = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.search_panel.SuspendLayout();
            this.SuspendLayout();
            // 
            // grid
            // 
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(0, 167);
            this.grid.Name = "grid";
            this.grid.Size = new System.Drawing.Size(772, 389);
            this.grid.TabIndex = 1;
            // 
            // toolbar
            // 
            this.toolbar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolbar.Location = new System.Drawing.Point(0, 0);
            this.toolbar.Name = "toolbar";
            this.toolbar.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.toolbar.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.toolbar.Size = new System.Drawing.Size(772, 25);
            this.toolbar.TabIndex = 2;
            // 
            // search_panel
            // 
            this.search_panel.Controls.Add(this.reset_button);
            this.search_panel.Controls.Add(this.search_button);
            this.search_panel.Controls.Add(this.search_location);
            this.search_panel.Controls.Add(this.label4);
            this.search_panel.Controls.Add(this.search_owner);
            this.search_panel.Controls.Add(this.label3);
            this.search_panel.Controls.Add(this.search_group);
            this.search_panel.Controls.Add(this.label2);
            this.search_panel.Controls.Add(this.search_name);
            this.search_panel.Controls.Add(this.label1);
            this.search_panel.Dock = System.Windows.Forms.DockStyle.Top;
            this.search_panel.Location = new System.Drawing.Point(0, 25);
            this.search_panel.Name = "search_panel";
            this.search_panel.Size = new System.Drawing.Size(772, 142);
            this.search_panel.TabIndex = 3;
            // 
            // reset_button
            // 
            this.reset_button.Location = new System.Drawing.Point(157, 113);
            this.reset_button.Name = "reset_button";
            this.reset_button.Size = new System.Drawing.Size(75, 23);
            this.reset_button.TabIndex = 9;
            this.reset_button.Text = "Reset";
            this.reset_button.UseVisualStyleBackColor = true;
            // 
            // search_button
            // 
            this.search_button.Location = new System.Drawing.Point(76, 113);
            this.search_button.Name = "search_button";
            this.search_button.Size = new System.Drawing.Size(75, 23);
            this.search_button.TabIndex = 8;
            this.search_button.Text = "Query";
            this.search_button.UseVisualStyleBackColor = true;
            // 
            // search_location
            // 
            this.search_location.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.search_location.Location = new System.Drawing.Point(76, 87);
            this.search_location.Name = "search_location";
            this.search_location.Size = new System.Drawing.Size(684, 20);
            this.search_location.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 90);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Location";
            // 
            // search_owner
            // 
            this.search_owner.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.search_owner.Location = new System.Drawing.Point(76, 61);
            this.search_owner.Name = "search_owner";
            this.search_owner.Size = new System.Drawing.Size(684, 20);
            this.search_owner.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 64);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Owner";
            // 
            // search_group
            // 
            this.search_group.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.search_group.Location = new System.Drawing.Point(76, 35);
            this.search_group.Name = "search_group";
            this.search_group.Size = new System.Drawing.Size(684, 20);
            this.search_group.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 38);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(36, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Group";
            // 
            // search_name
            // 
            this.search_name.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.search_name.Location = new System.Drawing.Point(76, 9);
            this.search_name.Name = "search_name";
            this.search_name.Size = new System.Drawing.Size(684, 20);
            this.search_name.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Item Name";
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
            this.Name = "MainForm";
            this.Text = "EVE Asset Manager";
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.search_panel.ResumeLayout(false);
            this.search_panel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.ToolStrip toolbar;
        private System.Windows.Forms.Panel search_panel;
        private System.Windows.Forms.Button reset_button;
        private System.Windows.Forms.Button search_button;
        private System.Windows.Forms.TextBox search_location;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox search_owner;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox search_group;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox search_name;
        private System.Windows.Forms.Label label1;
    }
}