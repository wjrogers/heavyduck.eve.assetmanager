namespace HeavyDuck.Eve.AssetManager
{
    partial class SearchManager
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
            this.rename_button = new System.Windows.Forms.Button();
            this.delete_button = new System.Windows.Forms.Button();
            this.done_button = new System.Windows.Forms.Button();
            this.list = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // rename_button
            // 
            this.rename_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.rename_button.Location = new System.Drawing.Point(12, 231);
            this.rename_button.Name = "rename_button";
            this.rename_button.Size = new System.Drawing.Size(75, 23);
            this.rename_button.TabIndex = 1;
            this.rename_button.Text = "Rename";
            this.rename_button.UseVisualStyleBackColor = true;
            // 
            // delete_button
            // 
            this.delete_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.delete_button.Location = new System.Drawing.Point(93, 231);
            this.delete_button.Name = "delete_button";
            this.delete_button.Size = new System.Drawing.Size(75, 23);
            this.delete_button.TabIndex = 2;
            this.delete_button.Text = "Delete";
            this.delete_button.UseVisualStyleBackColor = true;
            // 
            // done_button
            // 
            this.done_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.done_button.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.done_button.Location = new System.Drawing.Point(305, 231);
            this.done_button.Name = "done_button";
            this.done_button.Size = new System.Drawing.Size(75, 23);
            this.done_button.TabIndex = 3;
            this.done_button.Text = "Done";
            this.done_button.UseVisualStyleBackColor = true;
            // 
            // list
            // 
            this.list.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.list.FormattingEnabled = true;
            this.list.IntegralHeight = false;
            this.list.Location = new System.Drawing.Point(12, 12);
            this.list.Name = "list";
            this.list.Size = new System.Drawing.Size(368, 213);
            this.list.TabIndex = 4;
            // 
            // SearchManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.done_button;
            this.ClientSize = new System.Drawing.Size(392, 266);
            this.ControlBox = false;
            this.Controls.Add(this.list);
            this.Controls.Add(this.done_button);
            this.Controls.Add(this.delete_button);
            this.Controls.Add(this.rename_button);
            this.Name = "SearchManager";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manage Saved Queries";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button rename_button;
        private System.Windows.Forms.Button delete_button;
        private System.Windows.Forms.Button done_button;
        private System.Windows.Forms.ListBox list;
    }
}