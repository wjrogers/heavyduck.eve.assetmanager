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
        private static readonly string m_dbPath = Path.Combine(Program.DataPath, "assets.db");
        private static readonly string m_connectionString = "Data Source=" + m_dbPath;

        private DataTable m_assets;
        private List<WhereClause> m_clauses;
        private ToolStripLabel m_count_label;

        public MainForm()
        {
            InitializeComponent();

            // update the title with the version
            this.Text += " " + Application.ProductVersion;

            // attach menu event handlers
            this.Load += new EventHandler(MainForm_Load);
            search_button.Click += new EventHandler(search_button_Click);
            reset_button.Click += new EventHandler(reset_button_Click);
        }

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
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.Columns["typeName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["categoryName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["characterName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["quantity"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns["containerName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["flagName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            // the label for counting assets
            m_count_label = new ToolStripLabel();
            m_count_label.Name = "count_label";
            m_count_label.Alignment = ToolStripItemAlignment.Right;
            m_count_label.Text = "0 assets";

            // set up the toolbar
            toolbar.Items.Add(new ToolStripButton("Refresh Assets", Properties.Resources.arrow_refresh, ToolStripItem_Click, "refresh"));
            toolbar.Items.Add(new ToolStripButton("Manage API Keys", Properties.Resources.key, ToolStripItem_Click, "manage_keys"));
            toolbar.Items.Add(m_count_label);

            // initialize the count label
            UpdateAssetCount();
        }

        private void ToolStripItem_Click(object sender, EventArgs e)
        {
            ProgressDialog dialog;
            ToolStripItem item = sender as ToolStripItem;
            if (item == null) return;

            switch (item.Name)
            {
                case "refresh":
                    // refresh our asset data from EVE
                    try
                    {
                        dialog = new ProgressDialog();
                        dialog.AddTask(RefreshAssets);
                        dialog.Show();
                    }
                    catch (ProgressException ex)
                    {
                        MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // re-run the query
                    RunQuery();

                    break;
                case "manage_keys":
                    KeyManager.Show(this);
                    break;
                case "apply_filter":
                    break;
            }
        }

        private void reset_button_Click(object sender, EventArgs e)
        {
            // clear all the search boxes
            search_name.Text = "";
            search_group.Text = "";
            search_location.Text = "";
            search_owner.Text = "";
        }

        private void search_button_Click(object sender, EventArgs e)
        {
            RunQuery();
        }

        private void BuildWhereClause()
        {
            List<WhereClause> clauses = new List<WhereClause>();

            if (!string.IsNullOrEmpty(search_name.Text))
                clauses.Add(new WhereClause("t.typeName LIKE '%' || @searchName || '%'", "@searchName", search_name.Text));
            if (!string.IsNullOrEmpty(search_group.Text))
                clauses.Add(new WhereClause("g.groupName LIKE '%' || @searchGroup || '%'", "@searchGroup", search_group.Text));
            if (!string.IsNullOrEmpty(search_location.Text))
                clauses.Add(new WhereClause("COALESCE(l.itemName, cl.itemName) LIKE '%' || @searchLocation || '%'", "@searchLocation", search_location.Text));
            if (!string.IsNullOrEmpty(search_owner.Text))
                clauses.Add(new WhereClause("a.characterName LIKE '%' || @searchOwner || '%'", "@searchOwner", search_owner.Text));

            m_clauses = clauses;
        }

        private void UpdateAssetCount()
        {
            int total = 0;

            // count the total number of assets in the local db
            using (SQLiteConnection conn = new SQLiteConnection(m_connectionString))
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
                m_count_label.Text = string.Format("showing {0:#,##0} assets (of {1:#,##0})", m_assets.Rows.Count, total);
            else
                m_count_label.Text = string.Format("{0:#,##0} assets cached", total);
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
                dialog.AddTask(GetAssetTable);
                dialog.Show();

                grid.DataSource = m_assets;
                UpdateAssetCount();
            }
            catch (ProgressException ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshAssets(IProgressDialog dialog)
        {
            Dictionary<string, List<string>> assetFiles = new Dictionary<string, List<string>>();

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
                string apiKey = Program.ApiKeys.Rows.Find(userID)["apiKey"].ToString();
                string characterName = row["name"].ToString();

                try
                {
                    if (!assetFiles.ContainsKey(characterName)) assetFiles[characterName] = new List<string>();
                    assetFiles[characterName].Add(EveApiHelper.GetCharacterAssetList(userID, apiKey, characterID));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error retrieving assets:\n\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            dialog.Advance();

            // init the database
            dialog.Update("Initializing local asset database...");
            try
            {
                InitializeDB();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to initialize the asset database:\n\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            dialog.Advance();

            // parse the files
            dialog.Update("Parsing asset XML...");
            foreach (string characterName in assetFiles.Keys)
            {
                foreach (string assetFile in assetFiles[characterName])
                {
                    try
                    {
                        ParseAssets(assetFile, characterName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error parsing assets:\n\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            dialog.Advance();
        }

        private static void InitializeDB()
        {
            SQLiteConnection conn = null;
            SQLiteCommand cmd = null;
            StringBuilder sql;

            // delete any existing file
            if (File.Exists(m_dbPath)) File.Delete(m_dbPath);
           
            // let's connect
            try
            {
                // connect to our brand new database
                conn = new SQLiteConnection(m_connectionString);
                conn.Open();

                // let's build up a create table statement
                sql = new StringBuilder();
                sql.Append("CREATE TABLE assets (");
                sql.Append("itemID INT PRIMARY KEY,");
                sql.Append("characterName STRING,");
                sql.Append("locationID INT,");
                sql.Append("typeID INT,");
                sql.Append("quantity INT,");
                sql.Append("flag INT,");
                sql.Append("singleton BOOL,");
                sql.Append("containerID INT");
                sql.Append(")");

                // create our command and create the table
                cmd = new SQLiteCommand(sql.ToString(), conn);
                cmd.ExecuteNonQuery();
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (conn != null) conn.Dispose();
            }
        }

        private void GetAssetTable(IProgressDialog dialog)
        {
            StringBuilder sql;
            DataTable table = new DataTable("Assets");

            // update dialog
            dialog.Update("Querying asset database...", 0, 1);

            // connect to our lovely database
            using (SQLiteConnection conn = new SQLiteConnection(m_connectionString))
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
                sql.Append("a.*, t.typeName, g.groupName, cat.categoryName, f.flagName, ct.typeName AS containerName, COALESCE(l.itemName, cl.itemName) AS locationName ");
                sql.Append("FROM ");
                sql.Append("assets a ");
                sql.Append("JOIN eve.invTypes t ON t.typeID = a.typeID ");
                sql.Append("JOIN eve.invGroups g ON g.groupID = t.groupID ");
                sql.Append("JOIN eve.invCategories cat ON cat.categoryID = g.categoryID ");
                sql.Append("LEFT JOIN eve.invFlags f ON f.flagID = a.flag ");
                sql.Append("LEFT JOIN eve.eveNames l ON l.itemID = a.locationID ");
                sql.Append("LEFT JOIN assets c ON c.itemID = a.containerID ");
                sql.Append("LEFT JOIN eve.invTypes ct ON ct.typeID = c.typeID ");
                sql.Append("LEFT JOIN eve.eveNames cl ON cl.itemID = c.locationID ");

                // add where stuff
                if (m_clauses != null && m_clauses.Count > 0)
                {
                    List<string> clauses = new List<string>(m_clauses.Count);

                    foreach (WhereClause clause in m_clauses)
                        clauses.Add(clause.Clause);

                    sql.Append("WHERE ");
                    sql.Append(string.Join(" AND ", clauses.ToArray()));
                }

                // start the command we will use
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    // set the command text to our laboriously built sql string
                    cmd.CommandText = sql.ToString();

                    // add parameters for the user-entered where clauses
                    if (m_clauses != null)
                    {
                        foreach (WhereClause clause in m_clauses)
                            cmd.Parameters.AddWithValue(clause.ParameterName, clause.ParameterValue);
                    }

                    // create adapter and fill our table
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                        adapter.Fill(table);
                }
            }

            // yay
            m_assets = table;
            dialog.Advance();
        }

        private static void ParseAssets(string filePath, string characterName)
        {
            SQLiteConnection conn = null;
            SQLiteCommand cmd = null;
            SQLiteTransaction trans = null;

            try
            {
                // create and open the connection
                conn = new SQLiteConnection(m_connectionString);
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
            insertCmd.ExecuteNonQuery();

            // process child nodes
            contentIter = node.Select("rowset/row");
            while (contentIter.MoveNext())
            {
                ProcessNode(contentIter.Current, insertCmd, itemID);
            }
        }

        private class WhereClause
        {
            private string m_clause;
            private string m_parameterName;
            private string m_parameterValue;

            public WhereClause(string clause, string parameterName, string parameterValue)
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

            public string ParameterValue
            {
                get { return m_parameterValue; }
            }
        }
    }
}