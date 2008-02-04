namespace HeavyDuck.Eve.AssetManager
{
    partial class ReportOptionsDialog
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
            this.cancel_button = new System.Windows.Forms.Button();
            this.ok_button = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.title_box = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.path_box = new System.Windows.Forms.TextBox();
            this.browse_button = new System.Windows.Forms.Button();
            this.radio_all = new System.Windows.Forms.RadioButton();
            this.radio_saved = new System.Windows.Forms.RadioButton();
            this.radio_current = new System.Windows.Forms.RadioButton();
            this.query_combo = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // cancel_button
            // 
            this.cancel_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancel_button.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel_button.Location = new System.Drawing.Point(407, 137);
            this.cancel_button.Name = "cancel_button";
            this.cancel_button.Size = new System.Drawing.Size(75, 23);
            this.cancel_button.TabIndex = 7;
            this.cancel_button.Text = "Cancel";
            this.cancel_button.UseVisualStyleBackColor = true;
            // 
            // ok_button
            // 
            this.ok_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ok_button.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ok_button.Location = new System.Drawing.Point(326, 137);
            this.ok_button.Name = "ok_button";
            this.ok_button.Size = new System.Drawing.Size(75, 23);
            this.ok_button.TabIndex = 6;
            this.ok_button.Text = "Ok";
            this.ok_button.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(27, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Title";
            // 
            // title_box
            // 
            this.title_box.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.title_box.Location = new System.Drawing.Point(47, 12);
            this.title_box.Name = "title_box";
            this.title_box.Size = new System.Drawing.Size(404, 20);
            this.title_box.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Path";
            // 
            // path_box
            // 
            this.path_box.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.path_box.Location = new System.Drawing.Point(47, 38);
            this.path_box.Name = "path_box";
            this.path_box.Size = new System.Drawing.Size(404, 20);
            this.path_box.TabIndex = 3;
            // 
            // browse_button
            // 
            this.browse_button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browse_button.Location = new System.Drawing.Point(457, 36);
            this.browse_button.Name = "browse_button";
            this.browse_button.Size = new System.Drawing.Size(25, 23);
            this.browse_button.TabIndex = 4;
            this.browse_button.Text = "...";
            this.browse_button.UseVisualStyleBackColor = true;
            // 
            // radio_all
            // 
            this.radio_all.AutoSize = true;
            this.radio_all.Checked = true;
            this.radio_all.Location = new System.Drawing.Point(47, 64);
            this.radio_all.Name = "radio_all";
            this.radio_all.Size = new System.Drawing.Size(90, 17);
            this.radio_all.TabIndex = 8;
            this.radio_all.TabStop = true;
            this.radio_all.Text = "Use all assets";
            this.radio_all.UseVisualStyleBackColor = true;
            // 
            // radio_saved
            // 
            this.radio_saved.AutoSize = true;
            this.radio_saved.Location = new System.Drawing.Point(47, 87);
            this.radio_saved.Name = "radio_saved";
            this.radio_saved.Size = new System.Drawing.Size(117, 17);
            this.radio_saved.TabIndex = 9;
            this.radio_saved.Text = "Use a saved query:";
            this.radio_saved.UseVisualStyleBackColor = true;
            // 
            // radio_current
            // 
            this.radio_current.AutoSize = true;
            this.radio_current.Location = new System.Drawing.Point(47, 110);
            this.radio_current.Name = "radio_current";
            this.radio_current.Size = new System.Drawing.Size(148, 17);
            this.radio_current.TabIndex = 10;
            this.radio_current.Text = "Use current search results";
            this.radio_current.UseVisualStyleBackColor = true;
            // 
            // query_combo
            // 
            this.query_combo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.query_combo.Enabled = false;
            this.query_combo.FormattingEnabled = true;
            this.query_combo.Location = new System.Drawing.Point(170, 86);
            this.query_combo.Name = "query_combo";
            this.query_combo.Size = new System.Drawing.Size(140, 21);
            this.query_combo.TabIndex = 11;
            // 
            // ReportOptionsDialog
            // 
            this.AcceptButton = this.ok_button;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancel_button;
            this.ClientSize = new System.Drawing.Size(494, 172);
            this.ControlBox = false;
            this.Controls.Add(this.query_combo);
            this.Controls.Add(this.radio_current);
            this.Controls.Add(this.radio_saved);
            this.Controls.Add(this.radio_all);
            this.Controls.Add(this.browse_button);
            this.Controls.Add(this.path_box);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.title_box);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ok_button);
            this.Controls.Add(this.cancel_button);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "ReportOptionsDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Report Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cancel_button;
        private System.Windows.Forms.Button ok_button;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox title_box;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox path_box;
        private System.Windows.Forms.Button browse_button;
        private System.Windows.Forms.RadioButton radio_all;
        private System.Windows.Forms.RadioButton radio_saved;
        private System.Windows.Forms.RadioButton radio_current;
        private System.Windows.Forms.ComboBox query_combo;
    }
}