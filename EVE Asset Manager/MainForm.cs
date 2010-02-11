using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using HeavyDuck.Eve;
using HeavyDuck.Utilities.Forms;

namespace HeavyDuck.Eve.AssetManager
{
    public partial class MainForm : Form
    {
        private readonly Dictionary<string, CacheResult> m_assetsCachedUntil = new Dictionary<string, CacheResult>();

        private DataTable m_assets;
        private List<WhereClause> m_clauses;
        private ToolStripLabel m_countLabel;
        private List<SearchClauseControl> m_searchControls;
        private System.Threading.Timer m_timerCacheStatus;
        private int m_gridCurrentRowIndex = -1;

        private DoubleBufferedDataGridView grid;

        public MainForm()
        {
            InitializeComponent();

            // member initialization
            m_searchControls = new List<SearchClauseControl>();

            // update the title with the version
            this.Text += " " + AboutForm.GetVersionString(false);

            // create the timer and start it
            m_timerCacheStatus = new System.Threading.Timer(TimerCacheStatusCallback);
            m_timerCacheStatus.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            // attach event handlers
            this.Load += new EventHandler(MainForm_Load);
            this.Shown += new EventHandler(MainForm_Shown);
            this.KeyDown += new KeyEventHandler(MainForm_KeyDown);
            this.FormClosed += new FormClosedEventHandler(MainForm_FormClosed);
            menu_file_import.Click += new EventHandler(menu_file_import_Click);
            menu_file_export.Click += new EventHandler(menu_file_export_Click);
            menu_file_exit.Click += new EventHandler(menu_file_exit_Click);
            menu_reports_category.Click += new EventHandler(menu_reports_category_Click);
            menu_reports_location.Click += new EventHandler(menu_reports_location_Click);
            menu_reports_loadouts.Click += new EventHandler(menu_reports_loadouts_Click);
            menu_reports_material.Click += new EventHandler(menu_reports_material_Click);
            menu_reports_pos.Click += new EventHandler(menu_reports_pos_Click);
            menu_options_refresh.Click += new EventHandler(menu_options_refresh_Click);
            menu_options_keys.Click += new EventHandler(menu_options_keys_Click);
            menu_options_options.Click += new EventHandler(menu_options_options_Click);
            menu_help_about.Click += new EventHandler(menu_help_about_Click);
        }

        #region Event Handlers

        private void MainForm_Load(object sender, EventArgs e)
        {
            const string numberFormat = "#,##0";

            // create the grid
            grid = new DoubleBufferedDataGridView();
            grid.Dock = DockStyle.Fill;
            this.Controls.Add(grid);
            grid.BringToFront();

            // prep the asset grid
            GridHelper.Initialize(grid, true);
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            GridHelper.AddColumn(grid, "typeName", "Name");
            GridHelper.AddColumn(grid, "groupName", "Group");
            GridHelper.AddColumn(grid, "categoryName", "Category");
            GridHelper.AddColumn(grid, "characterName", "Owner");
            GridHelper.AddColumn(grid, "quantity", "Count");
            GridHelper.AddColumn(grid, "totalVolume", "Volume");
            GridHelper.AddColumn(grid, "locationName", "Location");
            GridHelper.AddColumn(grid, "flagName", "Flag");
            GridHelper.AddColumn(grid, "containerName", "Container");
            GridHelper.AddColumn(grid, "containerID", "Container ID");
            GridHelper.AddColumn(grid, "_marketPriceTotal", "Market Value");
            GridHelper.AddColumn(grid, "basePrice", "Base Price");
            GridHelper.AddColumn(grid, "metaLevel", "Meta Level");
            GridHelper.AddColumn(grid, "itemID", "ID");
            grid.Columns["quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["quantity"].DefaultCellStyle.Format = numberFormat;
            grid.Columns["basePrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["basePrice"].DefaultCellStyle.Format = numberFormat;
            grid.Columns["totalVolume"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["totalVolume"].DefaultCellStyle.Format = numberFormat;
            grid.Columns["metaLevel"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["containerID"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["itemID"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["_marketPriceTotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["_marketPriceTotal"].DefaultCellStyle.Format = numberFormat;
            grid.Columns["typeName"].Frozen = true;
            grid.CellFormatting += new DataGridViewCellFormattingEventHandler(grid_CellFormatting);
            grid.CurrentCellChanged += new EventHandler(grid_CurrentCellChanged);

            // the label for counting assets
            m_countLabel = new ToolStripLabel();
            m_countLabel.Name = "count_label";
            m_countLabel.Alignment = ToolStripItemAlignment.Right;
            m_countLabel.Text = "0 assets";

            // set up the toolbar
            toolbar.Items.Add(new ToolStripButton("Query", Appearance.Icons.Search, ToolStripItem_Click, "query"));
            toolbar.Items.Add(new ToolStripButton("Add Field", Appearance.Icons.Add, ToolStripItem_Click, "add_field"));
            toolbar.Items.Add(new ToolStripButton("Reset Fields", Appearance.Icons.New, ToolStripItem_Click, "reset_fields"));
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(new ToolStripButton("Save Query", Appearance.Icons.Save, ToolStripItem_Click, "save_query"));
            toolbar.Items.Add(new ToolStripDropDownButton("Load Query", Appearance.Icons.Open, null, "load_query"));
            toolbar.Items.Add(m_countLabel);

            // toolbar tooltips
            toolbar.Items["query"].ToolTipText = "Search your assets using the criteria in the fields below";
            toolbar.Items["add_field"].ToolTipText = "Add another search field";
            toolbar.Items["reset_fields"].ToolTipText = "Reset all the search fields";
            toolbar.Items["save_query"].ToolTipText = "Save the current search fields for later";

            // statusbar items
            statusbar.ShowItemToolTips = true;
            statusbar.Items.Add(new ToolStripLabel("", null, false, null, "firstExpiry"));
            statusbar.Items["firstExpiry"].ToolTipText = "Click for details";
            statusbar.Items["firstExpiry"].Click += new EventHandler(firstExpiry_Click);

            // initialize the UI with stuff
            InitializeSearchControls();
            UpdateAssetCount();
            UpdateSavedSearches();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            // refresh assets at startup if that option is set
            if (Program.OptionsDialog["General.StartupRefresh"].ValueAsBoolean)
                menu_options_refresh_Click(this, EventArgs.Empty);
            else
                BeginCheckAssets();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // auto-hook enter on a search control to mean query
            if (e.KeyCode == Keys.Enter && this.ActiveControl is SearchClauseControl)
            {
                e.SuppressKeyPress = true;
                this.BeginInvoke((MethodInvoker)RunQuery);
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // proactively kill the timer to reduce the chance of an exception
            m_timerCacheStatus.Dispose();
        }

        private void grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            DataGridViewCell currentCell = grid.CurrentCell;

            if (e.ColumnIndex == 0 && currentCell.ColumnIndex > 0 && currentCell.RowIndex == e.RowIndex)
                e.CellStyle.BackColor = Color.LightCyan;
        }

        private void grid_CurrentCellChanged(object sender, EventArgs e)
        {
            DataGridViewCell currentCell = grid.CurrentCell;

            // repaint the "old" current cell row's first cell
            if (m_gridCurrentRowIndex >= 0)
                grid.InvalidateCell(0, m_gridCurrentRowIndex);

            // repaint the new one and remember its index for next time
            if (currentCell != null)
            {
                grid.InvalidateCell(0, currentCell.RowIndex);
                m_gridCurrentRowIndex = currentCell.RowIndex;
            }
            else
            {
                m_gridCurrentRowIndex = -1;
            }
        }

        private void ToolStripItem_Click(object sender, EventArgs e)
        {
            ToolStripItem item = sender as ToolStripItem;
            if (item == null) return;

            switch (item.Name)
            {
                case "query":
                    RunQuery();
                    break;
                case "add_field":
                    AddSearchControl();
                    UpdateSearchPanel();
                    break;
                case "reset_fields":
                    InitializeSearchControls();
                    break;
                case "save_query":
                    SaveSearch();
                    break;
                case "manage_queries":
                    ManageSavedSearches();
                    break;
            }
        }

        private void ToolStripSearch_Click(object sender, EventArgs e)
        {
            DataTable fields;
            SearchClauseControl control;
            ToolStripItem item = sender as ToolStripItem;
            if (item == null) return;

            // we stuck the id in the item's tag
            long id = (long)item.Tag;

            try
            {
                // get the table
                fields = DataStore.GetSavedSearchParameters(id);

                // clear the list, then add in each search parameter
                ClearSearchControls();
                foreach (DataRow row in fields.Rows)
                {
                    // read the values from the row
                    string fieldName = row["fieldName"].ToString();
                    BooleanOp booleanOp = (BooleanOp)Enum.Parse(typeof(BooleanOp), row["booleanOp"].ToString());
                    SearchClauseControl.ComparisonOp comparisonOp = (SearchClauseControl.ComparisonOp)Enum.Parse(typeof(SearchClauseControl.ComparisonOp), row["comparisonOp"].ToString());
                    string value = row["value"].ToString();

                    // create the control
                    control = AddSearchControl(fieldName);
                    control.SelectedBooleanOp = booleanOp;
                    control.SelectedComparisonOp = comparisonOp;
                    control.Value = value;
                }

                // run the query
                RunQuery();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                UpdateSearchPanel();
            }
        }

        private void searchControl_RemoveClicked(object sender, EventArgs e)
        {
            SearchClauseControl control = sender as SearchClauseControl;

            RemoveSearchControl(control);
            m_searchControls.Remove(control);
            UpdateSearchPanel();
        }

        private void firstExpiry_Click(object sender, EventArgs e)
        {
            DataTable cacheStatus;

            lock (m_assetsCachedUntil)
            {
                // create the table for displaying the cache status
                cacheStatus = new DataTable("Cache Status");
                cacheStatus.Columns.Add("name", typeof(string));
                cacheStatus.Columns.Add("cachedUntil", typeof(DateTime));
                cacheStatus.BeginLoadData();

                // copy the asset cache status into it
                foreach (KeyValuePair<string, CacheResult> entry in m_assetsCachedUntil)
                    cacheStatus.LoadDataRow(new object[] { entry.Key, entry.Value.CachedUntil }, false);

                // finish up
                cacheStatus.DefaultView.Sort = "name ASC";
                cacheStatus.AcceptChanges();
                cacheStatus.EndLoadData();
            }

            // display it
            new CacheStatusDialog(cacheStatus).ShowDialog(this);
        }

        #endregion

        #region Event Handlers - Menus

        private void menu_file_import_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog;

            try
            {
                dialog = new OpenFileDialog();
                dialog.CheckFileExists = true;
                dialog.Filter = "XML Files (*.xml)|*.xml";
                dialog.Multiselect = false;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    // process the file
                    AssetCache.ParseAssets(dialog.FileName, "Unknown");

                    // update the count
                    UpdateAssetCount();
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void menu_file_export_Click(object sender, EventArgs e)
        {
            GenerateReport("Assets", "CSV Files (*.csv)|*.csv", "csv", ExportCsv);
        }

        private void menu_file_exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void menu_reports_category_Click(object sender, EventArgs e)
        {
            GenerateReport("Assets by Category", Reporter.GenerateAssetsByCategoryReport);
        }

        private void menu_reports_location_Click(object sender, EventArgs e)
        {
            GenerateReport("Assets by Location", Reporter.GenerateAssetsByLocationReport);
        }

        private void menu_reports_loadouts_Click(object sender, EventArgs e)
        {
            GenerateReport("Ship Loadouts", Reporter.GenerateLoadoutReport);
        }

        private void menu_reports_material_Click(object sender, EventArgs e)
        {
            GenerateReport("Materials", Reporter.GenerateMaterialReport);
        }

        private void menu_reports_pos_Click(object sender, EventArgs e)
        {
            GenerateReport("POS Fuel", Reporter.GeneratePosReport);
        }

        private void menu_options_refresh_Click(object sender, EventArgs e)
        {
            ProgressDialog dialog;

            // refresh our asset data from EVE
            try
            {
                dialog = new ProgressDialog();
                dialog.AddTask(RefreshAssets);
                dialog.Show();
            }
            catch (ProgressException ex)
            {
                ShowException(ex);
                return;
            }
            finally
            {
                // give the user feedback on the new state of the cache, successful or not
                BeginCheckAssets();
            }

            // re-run the query
            RunQuery();
        }

        private void menu_options_keys_Click(object sender, EventArgs e)
        {
            KeyManager.Show(this);
        }

        private void menu_options_options_Click(object sender, EventArgs e)
        {
            DialogResult result = Program.OptionsDialog.Show(this);

            // update the API helper's base URI
            if (result == DialogResult.OK)
            {
                Program.SetProxyUri(Program.OptionsDialog["General.EveApiProxy"].ValueAsString);
            }

            // if the user changed the data dump path, validate the new one and reload some stuff
            if (result == DialogResult.OK && Program.OptionsDialog["General.DataDumpPath"].ValueAsString != Program.CcpDatabasePath)
            {
                string candidate = Program.OptionsDialog["General.DataDumpPath"].ValueAsString;

                // first, validate it
                if (!Program.ValidateDataDump(candidate))
                {
                    MessageBox.Show(this, "The data dump file you selected is not valid. The next time you start EVE Asset Manager, you will be prompted to select a valid data dump.", "Invalid Data Dump", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // change the path (this will trigger an EveTypes reload)
                Program.CcpDatabasePath = candidate;
            }
        }

        private void menu_help_about_Click(object sender, EventArgs e)
        {
            AboutForm form = new AboutForm();

            form.ShowDialog(this);
        }

        #endregion

        #region Private Methods - Query Fields

        private List<WhereClause> GetWhereClauses(IEnumerable<SearchClauseControl> searchControls)
        {
            List<WhereClause> clauses = new List<WhereClause>();

            foreach (SearchClauseControl control in searchControls)
            {
                SearchClauseControl.SearchField field = control.SelectedField;
                SearchClauseControl.ComparisonOp op = control.SelectedComparisonOp;
                BooleanOp booleanOp = control.SelectedBooleanOp;
                string parameterName = field.GetParameterName();
                string format;
                object value = control.Value;

                // sanity check
                if (value == null) continue;

                // contruct the basic format of the clause
                format = GetComparisonOpFormat(op);

                // fill it
                clauses.Add(new WhereClause(string.Format(format, field.DbField, parameterName), booleanOp, parameterName, value));
            }

            return clauses;
        }

        private static string GetComparisonOpFormat(SearchClauseControl.ComparisonOp op)
        {
            switch (op)
            {
                case SearchClauseControl.ComparisonOp.Equals:
                    return "{0} = {1}";
                case SearchClauseControl.ComparisonOp.NotEquals:
                    return "{0} <> {1}";
                case SearchClauseControl.ComparisonOp.Like:
                    return "{0} LIKE '%' || {1} || '%'";
                case SearchClauseControl.ComparisonOp.NotLike:
                    return "{0} NOT LIKE '%' || {1} || '%'";
                case SearchClauseControl.ComparisonOp.LessThan:
                    return "{0} < {1}";
                case SearchClauseControl.ComparisonOp.LessThanOrEqual:
                    return "{0} <= {1}";
                case SearchClauseControl.ComparisonOp.GreaterThan:
                    return "{0} > {1}";
                case SearchClauseControl.ComparisonOp.GreaterThanOrEqual:
                    return "{0} >= {1}";
                default:
                    throw new InvalidOperationException("Don't know how to construct where clause for ComparisonOp" + op);
            }
        }

        private void InitializeSearchControls()
        {
            // clear it out
            ClearSearchControls();

            // add three
            AddSearchControl("Name");

            // update the panel
            UpdateSearchPanel();
        }

        private void ClearSearchControls()
        {
            // remove any in there
            foreach (SearchClauseControl control in m_searchControls)
            {
                RemoveSearchControl(control);
            }
            m_searchControls.Clear();
        }

        private SearchClauseControl AddSearchControl()
        {
            return AddSearchControl(new SearchClauseControl());
        }

        private SearchClauseControl AddSearchControl(string fieldName)
        {
            return AddSearchControl(new SearchClauseControl(fieldName));
        }

        private SearchClauseControl AddSearchControl(SearchClauseControl control)
        {
            // initialize the control
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            control.Location = new Point(12, 6 + m_searchControls.Count * 27);
            control.Width = search_panel.Width - 24;

            // add to the panel
            m_searchControls.Add(control);
            search_panel.Controls.Add(control);

            // add the click handler
            control.RemoveClicked += new EventHandler(searchControl_RemoveClicked);

            // send it back
            return control;
        }

        private void RemoveSearchControl(SearchClauseControl control)
        {
            search_panel.Controls.Remove(control);
            control.RemoveClicked -= searchControl_RemoveClicked;
            control.Dispose();
        }

        private void UpdateSearchPanel()
        {
            // make all the proper widgets visible (or not) and in the correct locations
            if (m_searchControls.Count > 0)
            {
                m_searchControls[0].RemoveButtonVisible = m_searchControls.Count > 1;
                m_searchControls[0].Location = new Point(12, 6);
            }
            for (int i = 1; i < m_searchControls.Count; ++i)
            {
                m_searchControls[i].RemoveButtonVisible = m_searchControls.Count > 1;
                m_searchControls[i].Location = new Point(12, 6 + i * 27);
            }

            // size the panel correctly
            search_panel.Height = 6 + m_searchControls.Count * 27;
        }

        private void SaveSearch()
        {
            List<SearchClauseControl> controls;
            string name = "";

            if (InputDialog.ShowDialog(this, "Search Name", "Enter a name for the saved search:", ref name) == DialogResult.OK)
            {
                // get the list of controls
                controls = new List<SearchClauseControl>();
                foreach (Control c in search_panel.Controls)
                {
                    if (c is SearchClauseControl) controls.Add((SearchClauseControl)c);
                }

                // save them
                try
                {
                    DataStore.SaveSearch(name, controls);
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }

                // refresh the menu
                UpdateSavedSearches();
            }
        }

        private void UpdateSavedSearches()
        {
            DataTable searches;
            ToolStripDropDownButton button;
            ContextMenuStrip menu;
            ToolStripMenuItem item;

            // grab the table of saved searches from our datastore
            try
            {
                searches = DataStore.GetSavedSearches();
            }
            catch (Exception ex)
            {
                ShowException(ex);
                return;
            }

            // build the menu
            button = (ToolStripDropDownButton)toolbar.Items["load_query"];
            menu = new ContextMenuStrip();
            foreach (DataRow row in searches.Rows)
            {
                string name = row["name"].ToString();
                long id = Convert.ToInt64(row["id"]);

                item = new ToolStripMenuItem(name, Properties.Resources.magnifier);
                item.Click += new EventHandler(ToolStripSearch_Click);
                item.Tag = id;

                menu.Items.Add(item);
            }
            if (menu.Items.Count > 0)
            {
                menu.Items.Add("-");
                menu.Items.Add(new ToolStripMenuItem("Manage Saved Queries...", null, ToolStripItem_Click, "manage_queries"));
            }
            button.DropDown = menu;
        }

        private void ManageSavedSearches()
        {
            DataTable searches, changes;
            SearchManager manager;

            // fetch the current list and create a manager form
            try
            {
                searches = DataStore.GetSavedSearches();
                manager = new SearchManager(searches);
            }
            catch (Exception ex)
            {
                ShowException(ex);
                return;
            }

            // show the form
            manager.ShowDialog(this);

            // let's see if the user changed anything
            changes = searches.GetChanges();
            if (changes == null || changes.Rows.Count < 1) return;

            // save the changes
            try
            {
                DataStore.UpdateSearches(changes);
                UpdateSavedSearches();
            }
            catch (Exception ex)
            {
                ShowException("Failed to save your changes:", ex);
            }
        }

        private List<WhereClause> GetWhereClausesForSavedSearch(long searchID)
        {
            DataTable fields;
            List<WhereClause> clauses;

            // load from the databaser
            fields = DataStore.GetSavedSearchParameters(searchID);
            clauses = new List<WhereClause>(fields.Rows.Count);

            // read in each row
            foreach (DataRow row in fields.Rows)
            {
                // read the values from the row
                string fieldName = row["fieldName"].ToString();
                BooleanOp booleanOp = (BooleanOp)Enum.Parse(typeof(BooleanOp), row["booleanOp"].ToString());
                SearchClauseControl.ComparisonOp comparisonOp = (SearchClauseControl.ComparisonOp)Enum.Parse(typeof(SearchClauseControl.ComparisonOp), row["comparisonOp"].ToString());
                string value = row["value"].ToString();

                // figure out a few more things
                SearchClauseControl.SearchField field = SearchClauseControl.GetField(fieldName);
                string format = GetComparisonOpFormat(comparisonOp);
                string parameterName = field.GetParameterName();

                // add it to the list
                clauses.Add(new WhereClause(string.Format(format, field.DbField, parameterName), booleanOp, parameterName, value));
            }

            // return the list
            return clauses;
        }

        #endregion

        #region Private Methods

        private void GenerateReport(string defaultTitle, GenerateReportDelegate reportMethod)
        {
            GenerateReport(defaultTitle, "HTML Files (*.html)|*.html", "html", reportMethod);
        }

        private void GenerateReport(string defaultTitle, string fileFilter, string defaultExt, GenerateReportDelegate reportMethod)
        {
            List<WhereClause> clauses;
            DataTable assets;
            ProgressDialog dialog;
            ReportOptionsDialog options;
            ReportOptionsDialog.AssetSourceType sourceType;
            EventHandler<ProgressEventArgs> handler = null;
            int savedSearchID = -1;

            // ask the user some stuff
            options = new ReportOptionsDialog(fileFilter, defaultExt);
            options.ReportTitle = defaultTitle;
            if (options.ShowDialog(this) == DialogResult.Cancel) return;
            sourceType = options.AssetSource;
            if (sourceType == ReportOptionsDialog.AssetSourceType.SavedSearch)
                savedSearchID = options.SavedSearchID;

            // create clauses
            clauses = new List<WhereClause>();

            try
            {
                // create dialog
                dialog = new ProgressDialog();

                // create the handler for price query updates
                handler = delegate(object sender, ProgressEventArgs e)
                {
                    if (e.Max < 1) return;
                    dialog.Update("Updating market prices...", e.Progress, e.Max);
                };
                Program.PriceProvider.UpdateProgress += handler;

                // add the progress task
                dialog.AddTask(delegate(IProgressDialog p)
                {
                    // create our clauses and get assets
                    p.Update("Querying assets...");
                    switch (sourceType)
                    {
                        case ReportOptionsDialog.AssetSourceType.All:
                            assets = AssetCache.GetAssetTable(clauses);
                            break;
                        case ReportOptionsDialog.AssetSourceType.Current:
                            if (m_assets == null) throw new ApplicationException("There are no current search results to use.");
                            assets = m_assets;
                            break;
                        case ReportOptionsDialog.AssetSourceType.SavedSearch:
                            clauses.AddRange(GetWhereClausesForSavedSearch(savedSearchID));
                            assets = AssetCache.GetAssetTable(clauses);
                            break;
                        default:
                            throw new ApplicationException("Don't know how to get assets for source type " + sourceType.ToString());
                    }

                    // generate report
                    p.Update("Generating report...");
                    reportMethod(assets, options.ReportTitle, options.ReportPath);
                });

                // run it!
                dialog.Show();
            }
            catch (Exception ex)
            {
                // let the user know we failed
                ShowException("Failed to generate report:", ex);

                // try to get rid of the partially-written file
                try { if (File.Exists(options.ReportPath)) File.Delete(options.ReportPath); }
                catch { /* pass */ }

                // get outta here, she's gonna blow!
                return;
            }
            finally
            {
                if (handler != null)
                    Program.PriceProvider.UpdateProgress -= handler;
            }

            // open the report
            if (Program.OptionsDialog["Reports.OpenReport"].ValueAsBoolean)
                System.Diagnostics.Process.Start(options.ReportPath);
        }

        private void UpdateAssetCount()
        {
            int total = AssetCache.GetAssetCount();

            // update the label
            if (m_assets != null)
                m_countLabel.Text = string.Format("showing {0:#,##0} assets (of {1:#,##0})", m_assets.Rows.Count, total);
            else
                m_countLabel.Text = string.Format("{0:#,##0} assets cached", total);
        }

        private void RunQuery()
        {
            ProgressDialog dialog;
            EventHandler<ProgressEventArgs> handler = null;

            // update our internal representation of the search boxes
            m_clauses = GetWhereClauses(m_searchControls);

            // run the query on the asset database again
            try
            {
                // create dialog
                dialog = new ProgressDialog();

                // create the handler for price query updates
                handler = delegate(object sender, ProgressEventArgs e)
                {
                    if (e.Max < 1) return;
                    dialog.Update("Updating market prices...", e.Progress, e.Max);
                };
                Program.PriceProvider.UpdateProgress += handler;

                // initialize the dialog and display it
                dialog.Update(0, 1);
                dialog.AddTask(UpdateAssetTable);
                dialog.Show();

                // display the fresh asset data in the grid
                grid.DataSource = m_assets.DefaultView;
                grid.AutoResizeColumns();
                UpdateAssetCount();
            }
            catch (ProgressException ex)
            {
                ShowException(ex);
            }
            finally
            {
                if (handler != null)
                    Program.PriceProvider.UpdateProgress -= handler;
            }
        }

        private void RefreshAssets(IProgressDialog dialog)
        {
            Dictionary<string, string> assetFiles = new Dictionary<string, string>();
            List<string> outdatedNames = new List<string>();
            CacheResult result;

            // clear the assets
            m_assets = null;

            // make sure our character list is up to date
            dialog.Update("Refreshing character list...");
            Program.RefreshCharacters();
            dialog.Update(1, 3 + Program.Characters.Rows.Count);

            // fetch the asset XML
            dialog.Update("Querying API for asset lists...");
            foreach (DataRow row in Program.Characters.Rows)
            {
                int userID = Convert.ToInt32(row["userID"]);
                int characterID = Convert.ToInt32(row["characterID"]);
                int corporationID = Convert.ToInt32(row["corporationID"]);
                string apiKey = Program.ApiKeys.Rows.Find(userID)["apiKey"].ToString();
                string characterName = row["name"].ToString();
                string corporationName = row["corporationName"].ToString();
                bool queryCorp = Convert.ToBoolean(row["queryCorp"]);

                // fetch character assets
                if (!assetFiles.ContainsKey(characterName))
                {
                    result = EveApiHelper.GetCharacterAssetList(userID, apiKey, characterID);
                    switch (result.State)
                    {
                        case CacheState.Cached:
                            assetFiles[characterName] = result.Path;
                            break;
                        case CacheState.CachedOutOfDate:
                            assetFiles[characterName] = result.Path;
                            outdatedNames.Add(characterName);
                            break;
                        default:
                            throw new ApplicationException("Failed to retrieve asset data for " + characterName, result.Exception);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Odd, got two records for the same character name... " + characterName);
                }

                // fetch corporation assets?
                if (queryCorp && !assetFiles.ContainsKey(corporationName))
                {
                    // attempt the query
                    result = EveApiHelper.GetCorporationAssetList(userID, apiKey, characterID, corporationID);

                    // check whether we got an eve error about not being a director
                    if (result.Exception != null && result.Exception is EveApiException && ((EveApiException)result.Exception).ErrorCode == 209)
                    {
                        System.Diagnostics.Debug.WriteLine(characterName + " is not a Director or CEO of " + corporationName + ".");
                        row["queryCorp"] = false;
                    }
                    else
                    {
                        switch (result.State)
                        {
                            case CacheState.Cached:
                                assetFiles[corporationName] = result.Path;
                                break;
                            case CacheState.CachedOutOfDate:
                                assetFiles[corporationName] = result.Path;
                                outdatedNames.Add(corporationName);
                                break;
                            default:
                                throw new ApplicationException("Failed to retrieve asset data for " + corporationName, result.Exception);
                        }
                    }
                }

                // progress
                dialog.Advance();
            }

            // inform the user about any files that could not be refreshed
            if (outdatedNames.Count > 0)
            {
                StringBuilder message = new StringBuilder();

                // prepare the semi-friendly message
                message.Append("An error occurred while refreshing assets for the characters and/or\ncorporations listed below. Cached data will be used instead. Your assets might\nbe out of date.\n");
                foreach (string name in outdatedNames)
                {
                    message.Append("\n");
                    message.Append(name);
                }

                // prepare the code to be invoked
                MethodInvoker code = delegate()
                {
                    MessageBox.Show(this, message.ToString(), "Using Cached Assets", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                };

                // invoke it
                if (this.InvokeRequired)
                    this.Invoke(code);
                else
                    code();
            }

            // init the database
            dialog.Update("Initializing local asset database...");
            AssetCache.InitializeDB(true);
            dialog.Advance();

            // parse the files
            dialog.Update("Parsing asset XML...");
            foreach (string characterName in assetFiles.Keys)
            {
                string assetFile = assetFiles[characterName];
                AssetCache.ParseAssets(assetFile, characterName);
            }
            dialog.Advance();
        }

        private void UpdateAssetTable(IProgressDialog dialog)
        {
            // update dialog
            dialog.Update("Querying asset database...");

            // yay
            m_assets = AssetCache.GetAssetTable(m_clauses);
            m_assets.DefaultView.Sort = "typeName ASC";
        }

        private void TimerCacheStatusCallback(object state)
        {
            // if we re-enter this method, just return
            if (!Monitor.TryEnter(m_timerCacheStatus, TimeSpan.Zero))
                return;

            // update the display (this just updates the style really so it's highlighted if it's past)
            try
            {
                this.BeginInvoke(new MethodInvoker(DisplayCachedUntil));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            finally
            {
                Monitor.Exit(m_timerCacheStatus);
            }
        }

        /// <summary>
        /// Begins an asynchronous check of the cached asset XML.
        /// </summary>
        private void BeginCheckAssets()
        {
            MethodInvoker d;

            d = new MethodInvoker(CheckAssets);
            d.BeginInvoke(CheckAssetsCallback, d);
        }

        /// <summary>
        /// Checks the cache status of assets for all known characters and corporations.
        /// </summary>
        private void CheckAssets()
        {
            lock (m_assetsCachedUntil)
            {
                // clear all existing state
                m_assetsCachedUntil.Clear();

                // TODO: refactor access to Characters table to improve thread safety
                foreach (DataRow row in Program.Characters.Rows)
                {
                    int userID = Convert.ToInt32(row["userID"]);
                    int characterID = Convert.ToInt32(row["characterID"]);
                    int corporationID = Convert.ToInt32(row["corporationID"]);
                    string apiKey = Program.ApiKeys.Rows.Find(userID)["apiKey"].ToString();
                    string characterName = row["name"].ToString();
                    string corporationName = row["corporationName"].ToString();
                    bool queryCorp = Convert.ToBoolean(row["queryCorp"]);

                    // check character assets
                    m_assetsCachedUntil[characterName] = EveApiHelper.CheckCharacterAssetList(userID, apiKey, characterID);

                    // check corp assets unless it has been disabled
                    if (queryCorp)
                        m_assetsCachedUntil[corporationName] = EveApiHelper.CheckCorporationAssetList(userID, apiKey, characterID, corporationID);
                }
            }
        }

        /// <summary>
        /// Asynchronous callback handler for CheckAssets.
        /// </summary>
        private void CheckAssetsCallback(IAsyncResult result)
        {
            MethodInvoker d;

            // end invoke
            try
            {
                // UI thread
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new AsyncCallback(CheckAssetsCallback), result);
                    return;
                }

                // call EndInvoke
                d = (MethodInvoker)result.AsyncState;
                d.EndInvoke(result);

                // display the updated expiration stuff
                DisplayCachedUntil();
            }
            catch (Exception ex)
            {
                ShowException(null, "Error checking asset cache status", ex);
            }
        }

        /// <summary>
        /// Displays the earliest asset cache expiration date in the status bar.
        /// </summary>
        private void DisplayCachedUntil()
        {
            ToolStripLabel label = statusbar.Items["firstExpiry"] as ToolStripLabel;

            // sanity check
            if (label == null) return;

            // display the earliest cachedUntil
            lock (m_assetsCachedUntil)
            {
                KeyValuePair<string, CacheResult>? first = null;

                // find it
                foreach (KeyValuePair<string, CacheResult> pair in m_assetsCachedUntil)
                {
                    if (pair.Value.State != CacheState.Uncached
                        && (!first.HasValue || pair.Value.CachedUntil < first.Value.Value.CachedUntil))
                        first = pair;
                }

                // display it and style it
                if (first.HasValue && DateTime.Now > first.Value.Value.CachedUntil)
                {
                    label.Text = string.Format("asset cache expired {0:d MMMM} at {0:HH:mm} local time", first.Value.Value.CachedUntil);
                    label.ForeColor = Color.Maroon;
                    label.Font = new Font(statusbar.Font, FontStyle.Bold);
                }
                else if (first.HasValue)
                {
                    label.Text = string.Format("assets cached until {0:d MMMM} at {0:HH:mm} local time", first.Value.Value.CachedUntil);
                    label.ForeColor = statusbar.ForeColor;
                    label.Font = statusbar.Font;
                }
                else
                {
                    label.Text = "";
                }
            }
        }

        private void ExportCsv(DataTable data, string title, string outputPath)
        {
            // argument sanity checks
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrEmpty(outputPath)) throw new ArgumentNullException(outputPath);
            if (data.Columns.Count < 1) return;

            using (FileStream fs = File.Open(outputPath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    // write the column headers
                    writer.Write(data.Columns[0].ColumnName);
                    for (int i = 1; i < data.Columns.Count; ++i)
                    {
                        writer.Write(',');
                        writer.Write(data.Columns[i].ColumnName);
                    }
                    writer.WriteLine();

                    // write data
                    foreach (DataRow row in data.Rows)
                    {
                        WriteCsvValue(writer, row[data.Columns[0]]);
                        for (int i = 1; i < data.Columns.Count; ++i)
                        {
                            writer.Write(',');
                            WriteCsvValue(writer, row[data.Columns[i]]);
                        }
                        writer.WriteLine();
                    }

                    // make sure it all goes down
                    writer.Flush();
                }
            }
        }

        private static void WriteCsvValue(TextWriter writer, object value)
        {
            if (value is string)
            {
                writer.Write('"');
                writer.Write(value.ToString().Replace("\"", "\"\""));
                writer.Write('"');
            }
            else if (value != null)
            {
                writer.Write(Convert.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private void ShowException(Exception ex)
        {
            ShowException(this, null, ex);
        }

        private void ShowException(string intro, Exception ex)
        {
            ShowException(this, intro, ex);
        }

        internal static void ShowException(IWin32Window owner, string intro, Exception ex)
        {
            if (ex == null) throw new ArgumentNullException("ex");
            string message = string.IsNullOrEmpty(intro) ? ex.ToString() : intro + "\n\n" + ex.ToString();
            MessageBox.Show(owner, message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion
    }

    internal class DoubleBufferedDataGridView : DataGridView
    {
        public DoubleBufferedDataGridView() : base()
        {
            this.DoubleBuffered = true;
        }
    }
}
