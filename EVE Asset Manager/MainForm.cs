using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using HeavyDuck.Eve;
using HeavyDuck.Utilities.Forms;

namespace HeavyDuck.EveAssetManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            // attach menu event handlers
            this.Load += new EventHandler(MainForm_Load);
            menu_file_exit.Click += new EventHandler(menu_file_exit_Click);
            menu_options_keys.Click += new EventHandler(menu_options_keys_Click);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // prep the asset grid
            GridHelper.Initialize(grid, true);
            GridHelper.AddColumn(grid, "typeName", "Name");
            GridHelper.AddColumn(grid, "quantity", "Count");
        }

        private void menu_file_exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void menu_options_keys_Click(object sender, EventArgs e)
        {
            KeyManager.Show(this);
        }
    }
}