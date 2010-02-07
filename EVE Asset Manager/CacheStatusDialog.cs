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
    public partial class CacheStatusDialog : Form
    {
        private DataTable m_cacheStatus;

        public CacheStatusDialog(DataTable cacheStatus)
        {
            InitializeComponent();

            m_cacheStatus = cacheStatus;
            this.Load += new EventHandler(CacheStatusDialog_Load);
        }

        private void CacheStatusDialog_Load(object sender, EventArgs e)
        {
            GridHelper.Initialize(grid, true);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            GridHelper.AddColumn(grid, "name", "Character/Corporation");
            GridHelper.AddColumn(grid, "cachedUntil", "Cached Until");
            grid.Columns["cachedUntil"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["cachedUntil"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns["cachedUntil"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
            grid.Columns["cachedUntil"].DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            grid.CellFormatting += new DataGridViewCellFormattingEventHandler(grid_CellFormatting);
            grid.DataSource = m_cacheStatus;
        }

        private void grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (grid.Columns[e.ColumnIndex].Name == "cachedUntil")
            {
                DateTime? cachedUntil = e.Value as DateTime?;

                if (cachedUntil.HasValue && cachedUntil.Value < DateTime.Now)
                    e.CellStyle.ForeColor = Color.Maroon;
            }
        }
    }
}
