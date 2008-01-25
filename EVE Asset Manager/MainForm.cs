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

        public MainForm()
        {
            InitializeComponent();

            // member initialization
            m_searchControls = new List<SearchClauseControl>();

            // update the title with the version
            this.Text += " " + AboutForm.GetVersionString(false);

            // attach event handlers
            this.Load += new EventHandler(MainForm_Load);
            this.KeyUp += new KeyEventHandler(MainForm_KeyUp);
            menu_file_import.Click += new EventHandler(menu_file_import_Click);
            menu_file_exit.Click += new EventHandler(menu_file_exit_Click);
            menu_reports_loadouts.Click += new EventHandler(menu_reports_loadouts_Click);
            menu_options_refresh.Click += new EventHandler(menu_options_refresh_Click);
            menu_options_keys.Click += new EventHandler(menu_options_keys_Click);
            menu_help_about.Click += new EventHandler(menu_help_about_Click);
        }

        #region Event Handlers

        private void MainForm_Load(object sender, EventArgs e)
        {
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
            GridHelper.AddColumn(grid, "itemID", "ID");
            grid.Columns["quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // the label for counting assets
            m_countLabel = new ToolStripLabel();
            m_countLabel.Name = "count_label";
            m_countLabel.Alignment = ToolStripItemAlignment.Right;
            m_countLabel.Text = "0 assets";

            // set up the toolbar
            toolbar.Items.Add(new ToolStripButton("Query", Properties.Resources.magnifier, ToolStripItem_Click, "query"));
            toolbar.Items.Add(new ToolStripButton("Add Field", Properties.Resources.add, ToolStripItem_Click, "add_field"));
            toolbar.Items.Add(new ToolStripButton("Reset Fields", Properties.Resources.page_white, ToolStripItem_Click, "reset_fields"));
            toolbar.Items.Add(m_countLabel);

            // toolbar tooltips
            toolbar.Items["query"].ToolTipText = "Search your assets using the criteria in the fields below";
            toolbar.Items["add_field"].ToolTipText = "Add another search field";
            toolbar.Items["reset_fields"].ToolTipText = "Reset all the search fields";

            // initialize the UI with stuff
            InitializeSearchControls();
            UpdateAssetCount();
        }


        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            // auto-hook enter on a search control to mean query
            if (e.KeyCode == Keys.Enter && this.ActiveControl is SearchClauseControl)
            {
                e.Handled = true;
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
                    // create the database only if it does not already exist!
                    if (!File.Exists(Program.LocalDatabasePath)) Program.InitializeDB();

                    // process the file
                    ParseAssets(dialog.FileName, "Unknown");

                    // update the count
                    UpdateAssetCount();
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void menu_file_exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void menu_reports_loadouts_Click(object sender, EventArgs e)
        {
            List<WhereClause> clauses;
            DataTable assets;
            DataColumn flagNameColumn, slotOrderColumn, classColumn;
            ProgressDialog dialog;

            try
            {
                dialog = new ProgressDialog();
                dialog.AddTask(delegate(IProgressDialog p)
                {
                    p.Update(0, 3);

                    // create our clauses and get assets
                    p.Update("Querying assets...");
                    clauses = new List<WhereClause>();
                    clauses.Add(new WhereClause("containerCategory = 'Ship'", null, null));
                    assets = GetAssetTable(clauses);
                    p.Advance();

                    // stealthily modify the assets table so we can sort the slots in the order we want
                    p.Update("Preparing data...");
                    flagNameColumn = assets.Columns["flagName"];
                    slotOrderColumn = assets.Columns.Add("slotOrder", typeof(string));
                    classColumn = assets.Columns.Add("rowClass", typeof(string));
                    foreach (DataRow row in assets.Rows)
                    {
                        string flag = row[flagNameColumn].ToString().ToLower();
                        if (flag.StartsWith("hislot"))
                        {
                            row[slotOrderColumn] = "1" + flag;
                            row[classColumn] = "hislot";
                        }
                        else if (flag.StartsWith("medslot"))
                        {
                            row[slotOrderColumn] = "2" + flag;
                            row[classColumn] = "medslot";
                        }
                        else if (flag.StartsWith("loslot"))
                        {
                            row[slotOrderColumn] = "3" + flag;
                            row[classColumn] = "loslot";
                        }
                        else if (flag.StartsWith("rigslot"))
                        {
                            row[slotOrderColumn] = "4" + flag;
                            row[classColumn] = "rigslot";
                        }
                        else if (flag == "dronebay")
                        {
                            row[slotOrderColumn] = "5" + flag;
                            row[classColumn] = flag;
                        }
                        else
                        {
                            row[slotOrderColumn] = flag;
                            row[classColumn] = flag;
                        }
                    }
                    p.Advance();

                    // generate the report through this ridiculously long function call
                    p.Update("Generating report...");
                    Reporter.GenerateHtmlReport(assets, @"C:\Temp\loadouts.html", "Ship Loadouts", "containerName", "characterName, locationName, containerName, slotOrder, typeName", "rowClass", delegate(object value, DataRowView row)
                    {
                        return string.Format("{0}'s {1} in {2}", row["characterName"], value, row["locationName"]);
                    }, "flagName", "quantity", "typeName", "groupName");
                    p.Advance();
                });
                dialog.Show();

            }
            catch (Exception ex)
            {
                ShowException("Failed to generate report:", ex);
            }
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

        private void BuildWhereClause()
        {
            List<WhereClause> clauses = new List<WhereClause>();

            foreach (SearchClauseControl control in m_searchControls)
            {
                SearchClauseControl.SearchField field = control.SelectedField;
                SearchClauseControl.ComparisonOp op = control.SelectedComparisonOp;
                string parameterName = field.GetParameterName();
                string format;
                object value = control.Value;

                // sanity check
                if (value == null) continue;

                // contruct the basic format of the clause
                switch (op)
                {
                    case SearchClauseControl.ComparisonOp.Equals:
                        format = "{0} = {1}";
                        break;
                    case SearchClauseControl.ComparisonOp.NotEquals:
                        format = "{0} <> {1}";
                        break;
                    case SearchClauseControl.ComparisonOp.Like:
                        format = "{0} LIKE '%' || {1} || '%'";
                        break;
                    case SearchClauseControl.ComparisonOp.NotLike:
                        format = "{0} NOT LIKE '%' || {1} || '%'";
                        break;
                    default:
                        throw new InvalidOperationException("Don't know how to construct where clause for ComparisonOp" + op);
                }

                // fill it
                clauses.Add(new WhereClause(string.Format(format, field.DbField, parameterName), parameterName, value));
            }

            m_clauses = clauses;
        }

        private void InitializeSearchControls()
        {
            // remove any in there
            foreach (SearchClauseControl control in m_searchControls)
            {
                RemoveSearchControl(control);
            }
            m_searchControls.Clear();

            // add three
            AddSearchControl("Name");

            // update the panel
            UpdateSearchPanel();
        }

        private void AddSearchControl()
        {
            AddSearchControl(new SearchClauseControl());
        }

        private void AddSearchControl(string fieldName)
        {
            AddSearchControl(new SearchClauseControl(fieldName));
        }

        private void AddSearchControl(SearchClauseControl control)
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

        private void UpdateAssetCount()
        {
            int total = 0;

            // count the total number of assets in the local db
            using (SQLiteConnection conn = new SQLiteConnection(Program.ConnectionString))
            {
                conn.Open();

                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "SELECT count(*) FROM assets";
                        total = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    catch
                    {
                        // pass
                    }
                }
            }

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
            BuildWhereClause();

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

            // clear the assets and set our dialog value/max
            m_assets = null;
            dialog.Update(0, 5);

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
                try
                {
                    if (!assetFiles.ContainsKey(characterName))
                        assetFiles[characterName] = EveApiHelper.GetCharacterAssetList(userID, apiKey, characterID);
                    else
                        System.Diagnostics.Debug.WriteLine("Odd, got two records for the same character name... " + characterName);
                }
                catch (Exception ex)
                {
                    ShowException("Error retrieving assets:", ex);
                }

                // fetch corporation assets?
                try
                {
                    if (queryCorp && !assetFiles.ContainsKey(corporationName))
                        assetFiles[corporationName] = EveApiHelper.GetCorporationAssetList(userID, apiKey, characterID, corporationID);
                }
                catch (Exception ex)
                {
                    // we don't care about errors due to inadequate permissions, but we will switch off corp data for this character
                    if (ex is EveApiException && ((EveApiException)ex).ErrorCode == 209)
                    {
                        System.Diagnostics.Debug.WriteLine(characterName + " is not a Director or CEO of " + corporationName + ".");
                        row["queryCorp"] = false;
                    }
                    else
                    {
                        ShowException("Error retrieving corp assets:", ex);
                    }
                }
            }
            dialog.Advance();

            // init the database
            dialog.Update("Initializing local asset database...");
            try
            {
                Program.InitializeDB();
            }
            catch (Exception ex)
            {
                ShowException("Failed to initialize the asset database:", ex);
                return;
            }
            dialog.Advance();

            // parse the files
            dialog.Update("Parsing asset XML...");
            foreach (string characterName in assetFiles.Keys)
            {
                string assetFile = assetFiles[characterName];

                try
                {
                    ParseAssets(assetFile, characterName);
                }
                catch (Exception ex)
                {
                    ShowException("Error parsing assets:", ex);
                }
            }
            dialog.Advance();
        }

        private void UpdateAssetTable(IProgressDialog dialog)
        {
            // update dialog
            dialog.Update("Querying asset database...", 0, 1);

            // yay
            m_assets = GetAssetTable(m_clauses);
            m_assets.DefaultView.Sort = "typeName ASC";
            dialog.Advance();
        }

        private DataTable GetAssetTable(IEnumerable<WhereClause> clauses)
        {
            StringBuilder sql;
            DataTable table = new DataTable("Assets");

            // connect to our lovely database
            using (SQLiteConnection conn = new SQLiteConnection(Program.ConnectionString))
            {
                conn.Open();

                // attach the eve database
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "ATTACH DATABASE @dbpath AS eve";
                    cmd.Parameters.AddWithValue("@dbpath", Program.CcpDatabasePath);
                    cmd.ExecuteNonQuery();
                }

                // build our select statement
                sql = new StringBuilder();
                sql.Append("SELECT ");
                sql.Append("a.*, t.typeName, g.groupName, cat.categoryName, f.flagName, ct.typeName || ' #' || c.itemID AS containerName, cg.groupName AS containerGroup, ccat.categoryName AS containerCategory, COALESCE(l.itemName, cl.itemName) AS locationName ");
                sql.Append("FROM ");
                sql.Append("assets a ");
                sql.Append("JOIN eve.invTypes t ON t.typeID = a.typeID ");
                sql.Append("JOIN eve.invGroups g ON g.groupID = t.groupID ");
                sql.Append("JOIN eve.invCategories cat ON cat.categoryID = g.categoryID ");
                sql.Append("LEFT JOIN eve.invFlags f ON f.flagID = a.flag ");
                sql.Append("LEFT JOIN eve.eveNames l ON l.itemID = a.locationID ");
                sql.Append("LEFT JOIN assets c ON c.itemID = a.containerID ");
                sql.Append("LEFT JOIN eve.invTypes ct ON ct.typeID = c.typeID ");
                sql.Append("LEFT JOIN eve.invGroups cg ON cg.groupID = ct.groupID ");
                sql.Append("LEFT JOIN eve.invCategories ccat ON ccat.categoryID = cg.categoryID ");
                sql.Append("LEFT JOIN eve.eveNames cl ON cl.itemID = c.locationID ");

                // add where stuff
                if (clauses != null)
                {
                    List<string> whereParts = new List<string>();

                    foreach (WhereClause clause in clauses)
                        whereParts.Add(clause.Clause);

                    if (whereParts.Count > 0)
                    {
                        sql.Append("WHERE ");
                        sql.Append(string.Join(" AND ", whereParts.ToArray()));
                    }
                }

                // start the command we will use
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    // set the command text to our laboriously built sql string
                    cmd.CommandText = sql.ToString();

                    // add parameters for the user-entered where clauses
                    if (clauses != null)
                    {
                        foreach (WhereClause clause in clauses)
                        {
                            if (!string.IsNullOrEmpty(clause.ParameterName))
                                cmd.Parameters.AddWithValue(clause.ParameterName, clause.ParameterValue);
                        }
                    }

                    // create adapter and fill our table
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                        adapter.Fill(table);
                }
            }

            return table;
        }

        private static void ParseAssets(string filePath, string characterName)
        {
            SQLiteConnection conn = null;
            SQLiteCommand cmd = null;
            SQLiteTransaction trans = null;

            try
            {
                // create and open the connection
                conn = new SQLiteConnection(Program.ConnectionString);
                conn.Open();

                // start the transaction
                trans = conn.BeginTransaction();

                // create the insertion command
                cmd = new SQLiteCommand("INSERT INTO assets (itemID, characterName, locationID, typeID, quantity, flag, singleton, containerID) VALUES (@itemID, @characterName, @locationID, @typeID, @quantity, @flag, @singleton, @containerID)", conn);
                cmd.Parameters.Add("@itemID", DbType.Int64);
                cmd.Parameters.AddWithValue("@characterName", characterName);
                cmd.Parameters.Add("@locationID", DbType.Int64);
                cmd.Parameters.Add("@typeID", DbType.Int32);
                cmd.Parameters.Add("@quantity", DbType.Int32);
                cmd.Parameters.Add("@flag", DbType.Int32);
                cmd.Parameters.Add("@singleton", DbType.Boolean);
                cmd.Parameters.Add("@containerID", DbType.Int64);

                // parse the asset XML (recursive madness here)
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    XPathDocument doc = new XPathDocument(fs);
                    XPathNavigator nav = doc.CreateNavigator();
                    XPathNodeIterator iter = nav.Select("/eveapi/result/rowset/row");

                    while (iter.MoveNext())
                    {
                        ProcessNode(iter.Current, cmd, null);
                    }
                }

                // finish the transaction
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (trans != null) trans.Dispose();
                if (conn != null) conn.Dispose();
            }
        }

        private static void ProcessNode(XPathNavigator node, SQLiteCommand insertCmd, Int64? containerID)
        {
            XPathNodeIterator contentIter;
            XPathNavigator tempNode;
            long itemID;

            // read the values
            itemID = node.SelectSingleNode("@itemID").ValueAsLong;
            insertCmd.Parameters["@itemID"].Value = itemID;
            tempNode = node.SelectSingleNode("@locationID");
            insertCmd.Parameters["@locationID"].Value = (tempNode == null ? (object)DBNull.Value : tempNode.ValueAsLong);
            insertCmd.Parameters["@typeID"].Value = node.SelectSingleNode("@typeID").ValueAsInt;
            insertCmd.Parameters["@quantity"].Value = node.SelectSingleNode("@quantity").ValueAsInt;
            insertCmd.Parameters["@flag"].Value = node.SelectSingleNode("@flag").ValueAsInt;
            insertCmd.Parameters["@singleton"].Value = node.SelectSingleNode("@singleton").ValueAsBoolean;
            insertCmd.Parameters["@containerID"].Value = containerID.HasValue ? containerID.Value : (object)DBNull.Value;

            // insert the row
            try
            {
                insertCmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                if (ex.Message.Contains("constraint violation"))
                    System.Diagnostics.Debug.WriteLine("skipping row, duplicate itemID " + itemID.ToString());
                else
                    throw;
            }

            // process child nodes
            contentIter = node.Select("rowset/row");
            while (contentIter.MoveNext())
            {
                ProcessNode(contentIter.Current, insertCmd, itemID);
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

        private class WhereClause
        {
            private string m_clause;
            private string m_parameterName;
            private object m_parameterValue;

            public WhereClause(string clause, string parameterName, object parameterValue)
            {
                m_clause = clause;
                m_parameterName = parameterName;
                m_parameterValue = parameterValue;
            }

            public string Clause
            {
                get { return m_clause; }
            }

            public string ParameterName
            {
                get { return m_parameterName; }
            }

            public object ParameterValue
            {
                get { return m_parameterValue; }
            }
        }
    }
}