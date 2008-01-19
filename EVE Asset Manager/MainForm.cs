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
        private ToolStripTextBox m_filter;

        public MainForm()
        {
            InitializeComponent();

            // attach menu event handlers
            this.Load += new EventHandler(MainForm_Load);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // prep the asset grid
            GridHelper.Initialize(grid, true);
            GridHelper.AddColumn(grid, "typeName", "Name");
            GridHelper.AddColumn(grid, "groupName", "Group");
            GridHelper.AddColumn(grid, "characterName", "Owner");
            GridHelper.AddColumn(grid, "quantity", "Count");
            GridHelper.AddColumn(grid, "locationName", "Location");
            GridHelper.AddColumn(grid, "containerName", "Container");
            GridHelper.AddColumn(grid, "flagName", "Flag");
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.Columns["typeName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["characterName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["quantity"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns["containerName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Columns["flagName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            // set up the filter box
            m_filter = new ToolStripTextBox("filter_box");
            m_filter.Alignment = ToolStripItemAlignment.Right;
            m_filter.TextChanged += new EventHandler(m_filter_TextChanged);

            // set up the toolbar
            toolbar.Items.Add(new ToolStripButton("Refresh Assets", Properties.Resources.arrow_refresh, ToolStripItem_Click, "refresh"));
            toolbar.Items.Add(new ToolStripButton("Manage API Keys", Properties.Resources.key, ToolStripItem_Click, "manage_keys"));
            toolbar.Items.Add(m_filter);
            toolbar.Items.Add(new ToolStripLabel("Filter:", null, false, null, "filter_label"));
            toolbar.Items["filter_label"].Alignment = ToolStripItemAlignment.Right;
        }

        private void m_filter_TextChanged(object sender, EventArgs e)
        {
            if (m_assets != null)
            {
                if (string.IsNullOrEmpty(m_filter.Text))
                    m_assets.DefaultView.RowFilter = "";
                else
                    m_assets.DefaultView.RowFilter = string.Format("typeName LIKE '%{0}%' OR groupName LIKE '%{0}%' OR flagName LIKE '%{0}%'", m_filter.Text);
            }
        }

        private void ToolStripItem_Click(object sender, EventArgs e)
        {
            ToolStripItem item = sender as ToolStripItem;
            if (item == null) return;

            switch (item.Name)
            {
                case "refresh":
                    m_assets = RefreshAssets();
                    m_filter.Text = "";
                    grid.DataSource = m_assets;
                    break;
                case "manage_keys":
                    KeyManager.Show(this);
                    break;
                case "apply_filter":
                    break;
            }
        }

        private static DataTable RefreshAssets()
        {
            Dictionary<string, List<string>> assetFiles = new Dictionary<string, List<string>>();

            // make sure our character list is up to date
            Program.RefreshCharacters();

            // fetch the asset XML
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

            // init the database
            try
            {
                InitializeDB();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to initialize the asset database:\n\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            // parse the files
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

            // load the data
            try
            {
                return GetAssetTable();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to query the asset data:\n\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
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

        private static DataTable GetAssetTable()
        {
            StringBuilder sql;
            DataTable table = new DataTable("Assets");

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
                sql.Append("a.*, t.typeName, g.groupName, f.flagName, ct.typeName AS containerName, COALESCE(l.itemName, cl.itemName) AS locationName ");
                sql.Append("FROM ");
                sql.Append("assets a ");
                sql.Append("JOIN eve.invTypes t ON t.typeID = a.typeID ");
                sql.Append("JOIN eve.invGroups g ON g.groupID = t.groupID ");
                sql.Append("LEFT JOIN eve.invFlags f ON f.flagID = a.flag ");
                sql.Append("LEFT JOIN eve.eveNames l ON l.itemID = a.locationID ");
                sql.Append("LEFT JOIN assets c ON c.itemID = a.containerID ");
                sql.Append("LEFT JOIN eve.invTypes ct ON ct.typeID = c.typeID ");
                sql.Append("LEFT JOIN eve.eveNames cl ON cl.itemID = c.locationID ");

                // create adapter and fill our table
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(sql.ToString(), conn))
                    adapter.Fill(table);
            }

            // yay
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
    }
}