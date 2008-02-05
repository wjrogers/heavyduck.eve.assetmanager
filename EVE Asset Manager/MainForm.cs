using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using HeavyDuck.Eve;
using HeavyDuck.Utilities.Forms;

namespace HeavyDuck.Eve.AssetManager
{
    public partial class MainForm : Form
    {
        private DataTable m_assets;
        private List<WhereClause> m_clauses;
        private ToolStripLabel m_countLabel;
        private List<SearchClauseControl> m_searchControls;

        private DoubleBufferedDataGridView grid;

        public MainForm()
        {
            InitializeComponent();

            // member initialization
            m_searchControls = new List<SearchClauseControl>();

            // update the title with the version
            this.Text += " " + AboutForm.GetVersionString(false);

            // attach event handlers
            this.Load += new EventHandler(MainForm_Load);
            this.KeyDown += new KeyEventHandler(MainForm_KeyDown);
            this.KeyUp += new KeyEventHandler(MainForm_KeyUp);
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
            menu_help_about.Click += new EventHandler(menu_help_about_Click);
        }

        #region Event Handlers

        private void MainForm_Load(object sender, EventArgs e)
        {
            // create the grid
            grid = new DoubleBufferedDataGridView();
            grid.Dock = DockStyle.Fill;
            this.Controls.Add(grid);
            grid.BringToFront();

            // prep the asset grid
            GridHelper.Initialize(grid, true);
            GridHelper.AddColumn(grid, "typeName", "Name");
            GridHelper.AddColumn(grid, "groupName", "Group");
            GridHelper.AddColumn(grid, "categoryName", "Category");
            GridHelper.AddColumn(grid, "characterName", "Owner");
            GridHelper.AddColumn(grid, "quantity", "Count");
            GridHelper.AddColumn(grid, "locationName", "Location");
            GridHelper.AddColumn(grid, "containerName", "Container");
            GridHelper.AddColumn(grid, "flagName", "Flag");
            GridHelper.AddColumn(grid, "basePrice", "Base Price");
            GridHelper.AddColumn(grid, "metaLevel", "Meta Level");
            GridHelper.AddColumn(grid, "itemID", "ID");
            grid.Columns["quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["quantity"].DefaultCellStyle.Format = "#,##0";
            grid.Columns["basePrice"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["basePrice"].DefaultCellStyle.Format = "#,##0";
            grid.Columns["metaLevel"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["itemID"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // the label for counting assets
            m_countLabel = new ToolStripLabel();
            m_countLabel.Name = "count_label";
            m_countLabel.Alignment = ToolStripItemAlignment.Right;
            m_countLabel.Text = "0 assets";

            // set up the toolbar
            toolbar.Items.Add(new ToolStripButton("Query", Properties.Resources.magnifier, ToolStripItem_Click, "query"));
            toolbar.Items.Add(new ToolStripButton("Add Field", Properties.Resources.add, ToolStripItem_Click, "add_field"));
            toolbar.Items.Add(new ToolStripButton("Reset Fields", Properties.Resources.page_white, ToolStripItem_Click, "reset_fields"));
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(new ToolStripButton("Save Query", Properties.Resources.disk, ToolStripItem_Click, "save_query"));
            toolbar.Items.Add(new ToolStripDropDownButton("Load Query", Properties.Resources.folder, null, "load_query"));
            toolbar.Items.Add(m_countLabel);

            // toolbar tooltips
            toolbar.Items["query"].ToolTipText = "Search your assets using the criteria in the fields below";
            toolbar.Items["add_field"].ToolTipText = "Add another search field";
            toolbar.Items["reset_fields"].ToolTipText = "Reset all the search fields";
            toolbar.Items["save_query"].ToolTipText = "Save the current search fields for later";

            // initialize the UI with stuff
            InitializeSearchControls();
            UpdateAssetCount();
            UpdateSavedSearches();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // prevent the bonk sound when hitting enter on a search control
            if (e.KeyCode == Keys.Enter && this.ActiveControl is SearchClauseControl)
            {
                e.SuppressKeyPress = true;
            }
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            // auto-hook enter on a search control to mean query
            if (e.KeyCode == Keys.Enter && this.ActiveControl is SearchClauseControl)
            {
                e.SuppressKeyPress = true;
                this.BeginInvoke((MethodInvoker)RunQuery);
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

            // re-run the query
            RunQuery();
        }

        private void menu_options_keys_Click(object sender, EventArgs e)
        {
            KeyManager.Show(this);
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
                dialog = new ProgressDialog();
                dialog.AddTask(delegate(IProgressDialog p)
                {
                    p.Update(0, 2);

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
                    p.Advance();

                    // generate report
                    p.Update("Generating report...");
                    reportMethod(assets, options.ReportTitle, options.ReportPath);
                    p.Advance();

                });
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

            // open the report
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

            // update our internal representation of the search boxes
            m_clauses = GetWhereClauses(m_searchControls);

            // run the query on the asset database again
            try
            {
                dialog = new ProgressDialog();
                dialog.AddTask(UpdateAssetTable);
                dialog.Show();

                grid.DataSource = m_assets.DefaultView;
                grid.AutoResizeColumns();
                UpdateAssetCount();
            }
            catch (ProgressException ex)
            {
                ShowException(ex);
            }
        }

        private void RefreshAssets(IProgressDialog dialog)
        {
            Dictionary<string, string> assetFiles = new Dictionary<string, string>();
            List<string> outdatedNames = new List<string>();
            CachedResult result;

            // clear the assets and set our dialog value/max
            m_assets = null;
            dialog.Update(0, 4);

            // make sure our character list is up to date
            dialog.Update("Refreshing character list...");
            Program.RefreshCharacters();
            dialog.Advance();

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
            }
            dialog.Advance();

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
            dialog.Update("Querying asset database...", 0, 1);

            // yay
            m_assets = AssetCache.GetAssetTable(m_clauses);
            m_assets.DefaultView.Sort = "typeName ASC";
            dialog.Advance();
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
                        for (int i = 0; i < data.Columns.Count; ++i)
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
                writer.Write(value.ToString());
            }
        }

        private void ShowException(Exception ex)
        {
            ShowException(null, ex);
        }

        private void ShowException(string intro, Exception ex)
        {
            if (ex == null) throw new ArgumentNullException("ex");
            string message = string.IsNullOrEmpty(intro) ? ex.ToString() : intro + "\n\n" + ex.ToString();
            MessageBox.Show(this, message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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