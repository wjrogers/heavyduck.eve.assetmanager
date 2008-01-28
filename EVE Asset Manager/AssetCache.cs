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
        public static void InitializeDB()
        {
            SQLiteConnection conn = null;
            SQLiteCommand cmd = null;
            StringBuilder sql;

            // delete any existing file
            if (File.Exists(Program.LocalCachePath)) File.Delete(Program.LocalCachePath);
           
            // let's connect
            try
            {
                // connect to our brand new database
                conn = new SQLiteConnection(Program.ConnectionString);
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

        public static DataTable GetAssetTable(IEnumerable<WhereClause> clauses)
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

        public static void ParseAssets(string filePath, string characterName)
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
