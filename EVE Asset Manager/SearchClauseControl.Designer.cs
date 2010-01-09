namespace HeavyDuck.Eve.AssetManager
{
    partial class SearchClauseControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.field_combo = new System.Windows.Forms.ComboBox();
            this.remove_button = new System.Windows.Forms.Button();
            this.op_combo = new System.Windows.Forms.ComboBox();
            this.boolean_combo = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // field_combo
            // 
            this.field_combo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.field_combo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.field_combo.FormattingEnabled = true;
            this.field_combo.Location = new System.Drawing.Point(66, 0);
            this.field_combo.Name = "field_combo";
            this.field_combo.Size = new System.Drawing.Size(140, 21);
            this.field_combo.Sorted = true;
            this.field_combo.TabIndex = 1;
            this.field_combo.TabStop = false;
            // 
            // remove_button
            // 
            this.remove_button.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.remove_button.FlatAppearance.BorderSize = 0;
            this.remove_button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.remove_button.Image = global::HeavyDuck.Eve.AssetManager.Properties.Resources.minus_circle;
            this.remove_button.Location = new System.Drawing.Point(665, 1);
            this.remove_button.Name = "remove_button";
            this.remove_button.Size = new System.Drawing.Size(19, 19);
            this.remove_button.TabIndex = 3;
            this.remove_button.UseVisualStyleBackColor = true;
            // 
            // op_combo
            // 
            this.op_combo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.op_combo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.op_combo.FormattingEnabled = true;
            this.op_combo.Location = new System.Drawing.Point(212, 0);
            this.op_combo.Name = "op_combo";
            this.op_combo.Size = new System.Drawing.Size(110, 21);
            this.op_combo.TabIndex = 2;
            this.op_combo.TabStop = false;
            // 
            // boolean_combo
            // 
            this.boolean_combo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.boolean_combo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.boolean_combo.FormattingEnabled = true;
            this.boolean_combo.Location = new System.Drawing.Point(0, 0);
            this.boolean_combo.Name = "boolean_combo";
            this.boolean_combo.Size = new System.Drawing.Size(60, 21);
            this.boolean_combo.TabIndex = 0;
            this.boolean_combo.TabStop = false;
            // 
            // SearchClauseControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.boolean_combo);
            this.Controls.Add(this.op_combo);
            this.Controls.Add(this.remove_button);
            this.Controls.Add(this.field_combo);
            this.DoubleBuffered = true;
            this.Name = "SearchClauseControl";
            this.Size = new System.Drawing.Size(684, 21);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox field_combo;
        private System.Windows.Forms.Button remove_button;
        private System.Windows.Forms.ComboBox op_combo;
        private System.Windows.Forms.ComboBox boolean_combo;
    }
}
