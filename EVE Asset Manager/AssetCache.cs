using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace HeavyDuck.Eve.AssetManager
{
    internal static class AssetCache
    {
        private const int MAX_FIX_ID_RUNS = 20;

        private static readonly string m_localCachePath = Path.Combine(Program.DataPath, "assets.db");
        private static readonly string m_connectionString = "Data Source=" + m_localCachePath;

        static AssetCache()
        {
            // initialize the database when this class is first accessed
            InitializeDB(false);
        }

        /// <summary>
        /// Initializes the local asset cache by creating the asset table.
        /// </summary>
        /// <param name="deleteExisting">If true, any existing local cache will be deleted first.</param>
        public static void InitializeDB(bool deleteExisting)
        {
            SQLiteConnection conn = null;
            SQLiteCommand cmd = null;
            StringBuilder sql;

            // delete any existing file
            if (deleteExisting && File.Exists(m_localCachePath)) File.Delete(m_localCachePath);
           
            // let's connect
            try
            {
                // connect to our brand new database
                conn = new SQLiteConnection(m_connectionString);
                conn.Open();

                // let's build up a create table statement
                sql = new StringBuilder();
                sql.Append("CREATE TABLE IF NOT EXISTS assets (");
                sql.Append("itemID INTEGER PRIMARY KEY,");
                sql.Append("characterName STRING,");
                sql.Append("locationID INTEGER,");
                sql.Append("typeID INTEGER,");
                sql.Append("quantity INTEGER,");
                sql.Append("flag INTEGER,");
                sql.Append("singleton BOOL,");
                sql.Append("containerID INTEGER");
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

        // gets the number of assets in the local cache
        public static int GetAssetCount()
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

            return total;
        }

        public static DataTable GetAssetTable(IList<WhereClause> clauses)
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
                sql.Append("a.*, t.typeName, g.groupName, cat.categoryName, f.flagName, ct.typeName || ' #' || c.itemID AS containerName, c.typeID AS containerTypeID, cg.groupName AS containerGroup, ccat.categoryName AS containerCategory, l.itemName AS locationName ");
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

                // add where stuff
                if (clauses != null && clauses.Count > 0)
                {
                    BooleanOp firstOp = clauses[0].BooleanOp;
                    StringBuilder sb = new StringBuilder();
                    bool parens = false;

                    // add the first clause
                    sb.Append(clauses[0].Clause);

                    // loop through the rest
                    for (int i = 1; i < clauses.Count; ++i)
                    {
                        if (clauses[i].BooleanOp != firstOp && !parens)
                        {
                            sb.AppendFormat(" {0} ({1}", firstOp.ToString().ToUpper(), clauses[i].Clause);
                            parens = true;
                        }
                        else if (clauses[i].BooleanOp == firstOp && parens)
                        {
                            sb.AppendFormat(") {0} {1}", firstOp.ToString().ToUpper(), clauses[i].Clause);
                            parens = false;
                        }
                        else
                        {
                            sb.AppendFormat(" {0} {1}", clauses[i].BooleanOp.ToString().ToUpper(), clauses[i].Clause);
                        }
                    }
                    if (parens) sb.Append(")");

                    // add to the sql string
                    sql.Append("WHERE ");
                    sql.Append(sb.ToString());
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

        public static void ParseAssets(string filePath, string characterName)
        {
            SQLiteConnection conn = null;
            SQLiteTransaction trans = null;

            try
            {
                // create and open the connection
                conn = new SQLiteConnection(m_connectionString);
                conn.Open();

                // start the transaction
                trans = conn.BeginTransaction();

                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    // create the insertion command
                    cmd.CommandText = "INSERT INTO assets (itemID, characterName, locationID, typeID, quantity, flag, singleton, containerID) VALUES (@itemID, @characterName, @locationID, @typeID, @quantity, @flag, @singleton, @containerID)";
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
                }

                // back-fill location IDs from container locationIDs
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    int affected = int.MaxValue;
                    int runs = 0;

                    // prepare command text
                    cmd.CommandText = "UPDATE assets SET locationID = (SELECT locationID FROM assets a WHERE a.itemID = assets.containerID) WHERE locationID IS NULL";

                    // keep running this query until it has no effect (all locationIDs are populated) or we hit the limit
                    while (affected > 0 && runs < MAX_FIX_ID_RUNS)
                    {
                        affected = cmd.ExecuteNonQuery();
                        runs++;
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
    }

    internal enum BooleanOp
    {
        And,
        Or
    }

    internal class WhereClause
    {
        private string m_clause;
        private string m_parameterName;
        private object m_parameterValue;
        private BooleanOp m_booleanOp;

        public WhereClause(string clause, BooleanOp booleanOp, string parameterName, object parameterValue)
        {
            m_clause = clause;
            m_booleanOp = booleanOp;
            m_parameterName = parameterName;
            m_parameterValue = parameterValue;
        }

        public string Clause
        {
            get { return m_clause; }
        }

        public BooleanOp BooleanOp
        {
            get { return m_booleanOp; }
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
