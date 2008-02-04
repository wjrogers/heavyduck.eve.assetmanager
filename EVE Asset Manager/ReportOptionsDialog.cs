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
            DataTable searches;
            string title = ReportTitle;

            // initialize the report path
            if (!string.IsNullOrEmpty(title))
                ReportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), string.Format("EVE_{0}_{1:yyyyMMddHHmm}.html", title.Replace(" ", ""), DateTime.Now));

            // prep the saved search combo
            try
            {
                searches = DataStore.GetSavedSearches();
                if (searches.Rows.Count > 0)
                {
                    query_combo.DisplayMember = "name";
                    query_combo.ValueMember = "id";
                    query_combo.DataSource = searches;
                    query_combo.SelectedIndex = 0;
                    radio_saved.Enabled = true;
                }
                else
                {
                    radio_saved.Enabled = false;
                }
            }
            catch
            {
                radio_saved.Enabled = false;
            }
            radio_saved.CheckedChanged += new EventHandler(radio_saved_CheckedChanged);
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

        private void radio_saved_CheckedChanged(object sender, EventArgs e)
        {
            query_combo.Enabled = radio_saved.Checked;
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

        public int SavedSearchID
        {
            get { return Convert.ToInt32(query_combo.SelectedValue); }
            set { query_combo.SelectedValue = value; }
        }

        public AssetSourceType AssetSource
        {
            get
            {
                if (radio_current.Checked)
                    return AssetSourceType.Current;
                else if (radio_saved.Checked)
                    return AssetSourceType.SavedSearch;
                else
                    return AssetSourceType.All;
            }
            set
            {
                switch (value)
                {
                    case AssetSourceType.Current:
                        radio_current.Checked = true;
                        break;
                    case AssetSourceType.SavedSearch:
                        radio_saved.Checked = true;
                        break;
                    default:
                        radio_all.Checked = true;
                        break;
                }
            }
        }

        public enum AssetSourceType
        {
            All,
            SavedSearch,
            Current
        }
    }
}
