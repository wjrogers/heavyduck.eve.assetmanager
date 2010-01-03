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
        private static readonly string m_outpostDatabasePath = Path.Combine(Program.DataPath, "outposts.db");
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
            bool gotOutposts;

            // try to grab some data on the outposts out there in never-never land
            try
            {
                // query the api and parse to a sqlite db
                UpdateOutpostDatabase();

                // we can use the outposts table now
                gotOutposts = true;
            }
            catch
            {
                // nope, we screwed up, can't use that table
                gotOutposts = false;
            }

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

                // attach the outposts database
                if (gotOutposts)
                {
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "ATTACH DATABASE @dbpath AS op";
                        cmd.Parameters.AddWithValue("@dbpath", m_outpostDatabasePath);
                        cmd.ExecuteNonQuery();
                    }
                }

                // build our select statement, select clause
                sql = new StringBuilder();
                sql.Append("SELECT ");
                sql.Append("a.*, t.typeName, t.basePrice, t.portionSize, g.groupName, cat.categoryName, f.flagName, ct.typeName AS containerName, c.typeID AS containerTypeID, cg.groupName AS containerGroup, ccat.categoryName AS containerCategory, mlattr.valueInt AS metaLevel ");
                if (gotOutposts)
                    sql.Append(", COALESCE(l.itemName, o.stationName) AS locationName ");
                else
                    sql.Append(", l.itemName AS locationName ");

                // from clause
                sql.Append("FROM ");
                sql.Append("assets a ");
                sql.Append("JOIN eve.invTypes t ON t.typeID = a.typeID ");
                sql.Append("JOIN eve.invGroups g ON g.groupID = t.groupID ");
                sql.Append("JOIN eve.invCategories cat ON cat.categoryID = g.categoryID ");
                sql.Append("LEFT JOIN eve.invFlags f ON f.flagID = a.flag ");
                sql.Append("LEFT JOIN eve.eveNames l ON l.itemID = a.locationID ");
                if (gotOutposts)
                    sql.Append("LEFT JOIN op.outposts o ON o.stationID = a.locationID ");
                sql.Append("LEFT JOIN dgmTypeAttributes mlattr ON mlattr.typeID = a.typeID AND mlattr.attributeID = 633 ");

                // these joins add in the info on the object's container
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

            // add pricing columns
            LoadMarketPrices(table, table.Columns["typeID"], "quantity");

            return table;
        }

        public static void LoadMarketPrices(DataTable data, DataColumn columnTypeID, string quantityExpression)
        {
            DataColumn columnMarketPriceUnit = data.Columns.Add("_marketPriceUnit", typeof(decimal));
            if (!string.IsNullOrEmpty(quantityExpression))
                data.Columns.Add("_marketPriceTotal", typeof(decimal), quantityExpression + " * _marketPriceUnit");

            // set them?
            try
            {
                Dictionary<int, List<DataRow>> index_rows = new Dictionary<int, List<DataRow>>();
                Dictionary<int, decimal> index_prices;

                // discover all typeIDs, index rows by them
                foreach (DataRow row in data.Rows)
                {
                    List<DataRow> list;
                    int typeID = Convert.ToInt32(row[columnTypeID]);

                    if (!index_rows.TryGetValue(typeID, out list))
                    {
                        list = new List<DataRow>();
                        index_rows[typeID] = list;
                    }

                    list.Add(row);
                }

                // get prices
                index_prices = Program.PriceProvider.GetPrices(index_rows.Keys, PriceStat.Median);

                // assign them to the rows
                foreach (KeyValuePair<int, decimal> price in index_prices)
                {
                    foreach (DataRow row in index_rows[price.Key])
                        row[columnMarketPriceUnit] = price.Value;
                }

                // accept changes
                data.AcceptChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
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
                    cmd.Parameters.Add("@typeID", DbType.Int64);
                    cmd.Parameters.Add("@quantity", DbType.Int64);
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
                            ProcessNode(iter.Current, cmd, null, null);
                        }
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

        private static void ProcessNode(XPathNavigator node, SQLiteCommand insertCmd, long? locationID, long? containerID)
        {
            XPathNodeIterator contentIter;
            XPathNavigator tempNode;
            long itemID, typeID;

            // look for a locationID attribute
            typeID = node.SelectSingleNode("@typeID").ValueAsLong;
            tempNode = node.SelectSingleNode("@locationID");
            if (tempNode != null)
            {
                // correct office location IDs?
                if (typeID == 27 && tempNode.Value.StartsWith("66"))
                    locationID = tempNode.ValueAsLong - 6000001L;
                else if (typeID == 27 && tempNode.Value.StartsWith("67"))
                    locationID = tempNode.ValueAsLong - 6000000L;
                else
                    locationID = tempNode.ValueAsLong;

            }

            // read the values
            itemID = node.SelectSingleNode("@itemID").ValueAsLong;
            insertCmd.Parameters["@itemID"].Value = itemID;
            insertCmd.Parameters["@locationID"].Value = locationID;
            insertCmd.Parameters["@typeID"].Value = typeID;
            insertCmd.Parameters["@quantity"].Value = node.SelectSingleNode("@quantity").ValueAsLong;
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
                ProcessNode(contentIter.Current, insertCmd, locationID, itemID);
            }
        }

        private static void UpdateOutpostDatabase()
        {
            Dictionary<string, string> parameters;
            SQLiteConnection conn = null;
            SQLiteTransaction trans = null;
            StringBuilder sql;
            CachedResult result;
            string xmlPath;

            // create our single uninteresting parameter
            parameters = new Dictionary<string, string>();
            parameters["version"] = "2";

            // query the api
            result = EveApiHelper.QueryApi("/eve/ConquerableStationList.xml.aspx", parameters);
            if (result.State == CacheState.Uncached)
                throw new ApplicationException("Failed to retrieve outpost list", result.Exception);
            else
                xmlPath = result.Path;

            // attempt to see whether we need to do anything at all
            FileInfo xmlInfo = new FileInfo(xmlPath);
            FileInfo outpostInfo = new FileInfo(m_outpostDatabasePath);
            if (outpostInfo.Exists && outpostInfo.LastWriteTime >= xmlInfo.LastWriteTime) return;

            // connect to the outpost db
            try
            {
                conn = new SQLiteConnection("Data Source=" + m_outpostDatabasePath);
                conn.Open();
                trans = conn.BeginTransaction();

                // we need to create the table if it does not exist
                sql = new StringBuilder();
                sql.Append("CREATE TABLE IF NOT EXISTS outposts (");
                sql.Append("stationID INTEGER PRIMARY KEY,");
                sql.Append("stationName TEXT,");
                sql.Append("stationTypeID INTEGER,");
                sql.Append("solarSystemID INTEGER,");
                sql.Append("corporationID INTEGER,");
                sql.Append("corporationName TEXT,");
                sql.Append("current BOOL");
                sql.Append(")");

                // rah rah go table
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    // create it if it's not there
                    cmd.CommandText = sql.ToString();
                    cmd.ExecuteNonQuery();

                    // set all the current flags to false
                    cmd.CommandText = "UPDATE outposts SET current = 0";
                    cmd.ExecuteNonQuery();
                }

                // parse it
                using (FileStream input = File.Open(xmlPath, FileMode.Open, FileAccess.Read))
                {
                    XPathDocument doc = new XPathDocument(input);
                    XPathNavigator nav = doc.CreateNavigator();
                    XPathNodeIterator iter = nav.Select("/eveapi/result/rowset[@name='outposts']/row");

                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        // create the command we will use to insert rows
                        cmd.CommandText = "INSERT OR REPLACE INTO outposts (stationID, stationName, stationTypeID, solarSystemID, corporationID, corporationName, current) VALUES (@stationID, @stationName, @stationTypeID, @solarSystemID, @corporationID, @corporationName, 1)";
                        cmd.Parameters.Add("@stationID", DbType.Int64);
                        cmd.Parameters.Add("@stationName", DbType.String);
                        cmd.Parameters.Add("@stationTypeID", DbType.Int64);
                        cmd.Parameters.Add("@solarSystemID", DbType.Int64);
                        cmd.Parameters.Add("@corporationID", DbType.Int64);
                        cmd.Parameters.Add("@corporationName", DbType.String);

                        // loop through all the nodes
                        while (iter.MoveNext())
                        {
                            // set the parameters
                            cmd.Parameters["@stationID"].Value = iter.Current.SelectSingleNode("@stationID").ValueAsLong;
                            cmd.Parameters["@stationName"].Value = iter.Current.SelectSingleNode("@stationName").Value;
                            cmd.Parameters["@stationTypeID"].Value = iter.Current.SelectSingleNode("@stationTypeID").ValueAsLong;
                            cmd.Parameters["@solarSystemID"].Value = iter.Current.SelectSingleNode("@solarSystemID").ValueAsLong;
                            cmd.Parameters["@corporationID"].Value = iter.Current.SelectSingleNode("@corporationID").ValueAsLong;
                            cmd.Parameters["@corporationName"].Value = iter.Current.SelectSingleNode("@corporationName").Value;

                            // execute the damn thing
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // commit transaction
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
