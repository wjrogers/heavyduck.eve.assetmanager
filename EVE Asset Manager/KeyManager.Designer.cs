namespace HeavyDuck.Eve.AssetManager
{
    partial class KeyManager
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
            this.grid_keys = new HeavyDuck.Eve.AssetManager.DoubleBufferedDataGridView();
            this.refresh_button = new System.Windows.Forms.Button();
            this.grid_characters = new HeavyDuck.Eve.AssetManager.DoubleBufferedDataGridView();
            this.done_button = new System.Windows.Forms.Button();
            this.remove_button = new System.Windows.Forms.Button();
            this.add_button = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.api_link = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.grid_keys)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grid_characters)).BeginInit();
            this.SuspendLayout();
            // 
            // grid_keys
            // 
            this.grid_keys.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.grid_keys.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid_keys.Location = new System.Drawing.Point(12, 12);
            this.grid_keys.Name = "grid_keys";
            this.grid_keys.Size = new System.Drawing.Size(570, 130);
            this.grid_keys.TabIndex = 0;
            // 
            // refresh_button
            // 
            this.refresh_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.refresh_button.Location = new System.Drawing.Point(470, 148);
            this.refresh_button.Name = "refresh_button";
            this.refresh_button.Size = new System.Drawing.Size(112, 23);
            this.refresh_button.TabIndex = 3;
            this.refresh_button.Text = "Refresh Characters";
            this.refresh_button.UseVisualStyleBackColor = true;
            // 
            // grid_characters
            // 
            this.grid_characters.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.grid_characters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid_characters.Location = new System.Drawing.Point(12, 177);
            this.grid_characters.Name = "grid_characters";
            this.grid_characters.Size = new System.Drawing.Size(570, 150);
            this.grid_characters.TabIndex = 4;
            // 
            // done_button
            // 
            this.done_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.done_button.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.done_button.Location = new System.Drawing.Point(507, 333);
            this.done_button.Name = "done_button";
            this.done_button.Size = new System.Drawing.Size(75, 23);
            this.done_button.TabIndex = 5;
            this.done_button.Text = "Done";
            this.done_button.UseVisualStyleBackColor = true;
            // 
            // remove_button
            // 
            this.remove_button.Location = new System.Drawing.Point(93, 148);
            this.remove_button.Name = "remove_button";
            this.remove_button.Size = new System.Drawing.Size(75, 23);
            this.remove_button.TabIndex = 2;
            this.remove_button.Text = "Remove";
            this.remove_button.UseVisualStyleBackColor = true;
            // 
            // add_button
            // 
            this.add_button.Location = new System.Drawing.Point(12, 148);
            this.add_button.Name = "add_button";
            this.add_button.Size = new System.Drawing.Size(75, 23);
            this.add_button.TabIndex = 1;
            this.add_button.Text = "Add";
            this.add_button.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 332);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(338, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Note: the assets API requires a full API key. A limited key will not work.";
            // 
            // api_link
            // 
            this.api_link.AutoSize = true;
            this.api_link.Location = new System.Drawing.Point(13, 346);
            this.api_link.Name = "api_link";
            this.api_link.Size = new System.Drawing.Size(158, 13);
            this.api_link.TabIndex = 7;
            this.api_link.TabStop = true;
            this.api_link.Text = "http://www.eveonline.com/api/";
            // 
            // KeyManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(594, 368);
            this.ControlBox = false;
            this.Controls.Add(this.api_link);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.add_button);
            this.Controls.Add(this.remove_button);
            this.Controls.Add(this.done_button);
            this.Controls.Add(this.grid_characters);
            this.Controls.Add(this.refresh_button);
            this.Controls.Add(this.grid_keys);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "KeyManager";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "API Key Manager";
            ((System.ComponentModel.ISupportInitialize)(this.grid_keys)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grid_characters)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private HeavyDuck.Eve.AssetManager.DoubleBufferedDataGridView grid_keys;
        private System.Windows.Forms.Button refresh_button;
        private HeavyDuck.Eve.AssetManager.DoubleBufferedDataGridView grid_characters;
        private System.Windows.Forms.Button done_button;
        private System.Windows.Forms.Button remove_button;
        private System.Windows.Forms.Button add_button;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel api_link;
    }
}