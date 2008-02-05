using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using HeavyDuck.Utilities.Forms;

namespace HeavyDuck.Eve.AssetManager
{
    public partial class SearchManager : Form
    {
        DataTable m_searches;

        public SearchManager(DataTable searches)
        {
            InitializeComponent();

            // our table of saved searches
            m_searches = searches;

            // event handlers
            this.Load += new EventHandler(SearchManager_Load);
            rename_button.Click += new EventHandler(rename_button_Click);
            delete_button.Click += new EventHandler(delete_button_Click);
        }

        private void SearchManager_Load(object sender, EventArgs e)
        {
            // set up the list
            list.DisplayMember = "name";
            list.DataSource = m_searches;
        }

        private void rename_button_Click(object sender, EventArgs e)
        {
            DataRowView view = list.SelectedItem as DataRowView;
            if (view == null) return;
            string name = view["name"].ToString();

            if (InputDialog.ShowDialog(this, "Rename Query", "Enter a new name:", ref name) == DialogResult.OK)
            {
                try
                {
                    view["name"] = name;
                    view.EndEdit();
                }
                catch (ConstraintException)
                {
                    MessageBox.Show(this, "There is already a saved query with that name.", "Duplicate Name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void delete_button_Click(object sender, EventArgs e)
        {
            DataRowView view = list.SelectedItem as DataRowView;
            if (view == null) return;
            string name = view["name"].ToString();

            if (MessageBox.Show(this, "Really delete saved query '" + name + "'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                view.Delete();
            }
        }
    }
}
