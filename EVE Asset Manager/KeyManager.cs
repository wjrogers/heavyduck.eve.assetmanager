using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using HeavyDuck.Utilities.Forms;

namespace HeavyDuck.Eve.AssetManager
{
    public partial class KeyManager : Form
    {
        private static KeyManager the_instance = null;

        private KeyManager()
        {
            InitializeComponent();

            // set up key grid
            GridHelper.Initialize(grid_keys, true);
            GridHelper.AddColumn(grid_keys, "userID", "User ID");
            GridHelper.AddColumn(grid_keys, "apiKey", "Full API Key");
            grid_keys.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid_keys.Columns["userID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid_keys.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // set up character grid
            GridHelper.Initialize(grid_characters, false);
            GridHelper.AddColumn(grid_characters, "userID", "User ID");
            GridHelper.AddColumn(grid_characters, "name", "Name");
            GridHelper.AddColumn(grid_characters, "corporationName", "Corporation");
            GridHelper.AddColumn(grid_characters, new DataGridViewCheckBoxColumn(), "queryCorp", "Query Corp Assets?");
            grid_characters.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid_characters.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            grid_characters.Columns["userID"].ReadOnly = true;
            grid_characters.Columns["name"].ReadOnly = true;
            grid_characters.Columns["corporationName"].ReadOnly = true;
            grid_characters.Columns["corporationName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // bind data to the grids
            BindGrids();

            // no sorty-sort arrows
            GridHelper.DisableClickToSort(grid_keys, false);
            GridHelper.DisableClickToSort(grid_characters, false);

            // event handlers
            add_button.Click += new EventHandler(add_button_Click);
            remove_button.Click += new EventHandler(remove_button_Click);
            refresh_button.Click += new EventHandler(refresh_button_Click);
        }

        private void add_button_Click(object sender, EventArgs e)
        {
            NewKeyDialog dialog = new NewKeyDialog();

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    Program.ApiKeys.LoadDataRow(new object[] { dialog.UserID, dialog.ApiKey }, true);
                }
                catch
                {
                    MessageBox.Show("You entered an invalid user ID or API key.", "Invalid Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void remove_button_Click(object sender, EventArgs e)
        {
            List<DataRow> rows = new List<DataRow>(grid_keys.SelectedRows.Count);
            DataRowView view;

            // mark the rows we are going to kill
            foreach (DataGridViewRow selected in grid_keys.SelectedRows)
            {
                view = selected.DataBoundItem as DataRowView;
                if (view != null) rows.Add(view.Row);
            }

            // delete them...
            foreach (DataRow row in rows)
                row.Delete();

            // ... and accept the changes
            Program.ApiKeys.AcceptChanges();
        }

        private void refresh_button_Click(object sender, EventArgs e)
        {
            ProgressDialog dialog;

            try
            {
                // grids freak out if we modify their data sources in another thread
                grid_keys.DataSource = null;
                grid_characters.DataSource = null;

                // refresh using a user-friendly progress dialog
                dialog = new ProgressDialog();
                dialog.AddTask(Program.RefreshCharacters);
                dialog.Show();

                // re-attach grids
                BindGrids();
            }
            catch (Exception ex)
            {
                MainForm.ShowException(this, "Failed to refresh characters", ex);
            }
        }

        private void BindGrids()
        {
            grid_keys.DataSource = Program.ApiKeys;
            grid_characters.DataSource = Program.Characters;
        }

        public static new void Show(IWin32Window parent)
        {
            if (Monitor.TryEnter(typeof(KeyManager)))
            {
                if (the_instance == null) the_instance = new KeyManager();
                the_instance.ShowDialog(parent);

                Monitor.Exit(typeof(KeyManager));
            }
            else
            {
                throw new InvalidOperationException("The KeyManager is already open");
            }
        }
    }
}