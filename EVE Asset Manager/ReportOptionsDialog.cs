using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HeavyDuck.Eve.AssetManager
{
    public partial class ReportOptionsDialog : Form
    {
        public ReportOptionsDialog()
        {
            InitializeComponent();

            // event handlers
            this.Load += new EventHandler(ReportOptionsDialog_Load);
            browse_button.Click += new EventHandler(browse_button_Click);
        }

        private void ReportOptionsDialog_Load(object sender, EventArgs e)
        {
            string title = ReportTitle;

            // initialize the report path
            if (!string.IsNullOrEmpty(title))
                ReportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), string.Format("{0}_{1:yyyyMMddHHmm}.html", title.Replace(" ", ""), DateTime.Now));
        }

        private void browse_button_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();

            // initialize dialog
            dialog.FileName = ReportPath;
            dialog.Filter = "HTML Files (*.html)|*.html";
            dialog.AddExtension = true;
            dialog.DefaultExt = "html";
            dialog.OverwritePrompt = true;

            // show it
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                ReportPath = dialog.FileName;
            }
        }

        public string ReportTitle
        {
            get { return title_box.Text; }
            set { title_box.Text = value; }
        }

        public string ReportPath
        {
            get { return path_box.Text; }
            set { path_box.Text = value; }
        }

        public bool UseCurrentFields
        {
            get { return use_current_check.Checked; }
            set { use_current_check.Checked = value; }
        }
    }
}
