using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace HeavyDuck.Eve.AssetManager
{
    // provides methods to facilitate storing and retrieving configuration settings and data
    internal static class DataStore
    {
        private static readonly string m_dbPath = Path.Combine(Program.DataPath, "settings.db");

        static DataStore()
        {
            SQLiteConnection conn = null;
            SQLiteTransaction trans = null;
            Dictionary<string, string> tables;
            List<string> existingTables;

            // define all the tables that should be in the datastore
            tables = new Dictionary<string, string>();
            tables["settings"] = "CREATE TABLE settings (name TEXT PRIMARY KEY, value TEXT)";
            tables["saved_searches"] = "CREATE TABLE saved_searches (id INTEGER PRIMARY KEY, name TEXT UNIQUE)";
            tables["saved_search_parameters"] = "CREATE TABLE saved_search_parameters (id INTEGER PRIMARY KEY, search_id INTEGER REFERENCES saved_searches (id), booleanOp TEXT, fieldName TEXT, comparisonOp TEXT, value TEXT)";

            // create the ones that are missing
            try
            {
                conn = GetOpenConnection();
                trans = conn.BeginTransaction();

                // get the list of current tables
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    existingTables = new List<string>();
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingTables.Add(reader.GetString(0));
                        }
                    }
                }

                // loop through the list
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    foreach (string tableName in tables.Keys)
                    {
                        if (!existingTables.Contains(tableName))
                        {
                            cmd.CommandText = tables[tableName];
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // commit changes
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

        public static string GetSetting(string name)
        {
            SQLiteConnection conn = null;
            SQLiteCommand cmd = null;

            try
            {
                conn = GetOpenConnection();
                cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT value FROM settings WHERE name = @name";
                cmd.Parameters.AddWithValue("@name", name);

                return cmd.ExecuteScalar().ToString();
            }
            catch
            {
                return null;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (conn != null) conn.Dispose();
            }
        }

        public static void SetSetting(string name, string value)
        {
            using (SQLiteConnection conn = GetOpenConnection())
            {
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT OR REPLACE INTO settings (name, value) VALUES (@name, @value)";
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@value", value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static DataTable GetSavedSearches()
        {
            DataTable data;

            data = GetTable("SELECT * FROM saved_searches ORDER BY name");
            data.PrimaryKey = new DataColumn[] { data.Columns["id"] };
            data.Constraints.Add("name_unique", data.Columns["name"], false);

            return data;
        }

        public static DataTable GetSavedSearchParameters(long searchID)
        {
            DataTable data = new DataTable();

            using (SQLiteConnection conn = GetOpenConnection())
            {
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM saved_search_parameters WHERE search_id = @searchID";
                    cmd.Parameters.AddWithValue("@searchID", searchID);

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                        adapter.Fill(data);
                }
            }

            return data;
        }

        public static void SaveSearch(string name, IEnumerable<SearchClauseControl> controls)
        {
            SQLiteConnection conn = null;
            SQLiteTransaction trans = null;
            object id;

            try
            {
                conn = GetOpenConnection();
                trans = conn.BeginTransaction();

                // check whether there is already a name
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT id FROM saved_searches WHERE name = @name";
                    cmd.Parameters.AddWithValue("@name", name);

                    id = cmd.ExecuteScalar();
                }

                // handle deleting existing parameters or inserting a new search row
                if (id != null && !(id is DBNull))
                {
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {

                        // delete the parameters
                        cmd.CommandText = "DELETE FROM saved_search_parameters WHERE search_id = @id";
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // insert a new search
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO saved_searches (name) VALUES (@name)";
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.ExecuteNonQuery();
                    }

                    // get the id of the row we just inserted
                    id = GetScalar("SELECT last_insert_rowid()", conn);
                }

                // insert the parameters
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO saved_search_parameters (search_id, booleanOp, fieldName, comparisonOp, value) VALUES (@id, @booleanOp, @fieldName, @comparisonOp, @value)";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.Add("@booleanOp", DbType.String);
                    cmd.Parameters.Add("@fieldName", DbType.String);
                    cmd.Parameters.Add("@comparisonOp", DbType.String);
                    cmd.Parameters.Add("@value", DbType.String);

                    foreach (SearchClauseControl control in controls)
                    {
                        object value = control.Value;

                        if (value == null) continue;

                        cmd.Parameters["@booleanOp"].Value = control.SelectedBooleanOp;
                        cmd.Parameters["@fieldName"].Value = control.SelectedField.Name;
                        cmd.Parameters["@comparisonOp"].Value = control.SelectedComparisonOp;
                        cmd.Parameters["@value"].Value = control.ValueText;
                        cmd.ExecuteNonQuery();
                    }
                }

                // finally!
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

        public static void UpdateSearches(DataTable searches)
        {
            if (searches == null) throw new ArgumentNullException("searches");

            SQLiteConnection conn = null;
            SQLiteTransaction trans = null;

            try
            {
                conn = GetOpenConnection();
                trans = conn.BeginTransaction();

                using (SQLiteCommand updateCmd = conn.CreateCommand(), deleteCmd = conn.CreateCommand())
                {
                    // define the SQL statements
                    updateCmd.CommandText = "UPDATE saved_searches SET name = @name WHERE id = @id";
                    updateCmd.Parameters.Add("@id", DbType.Int64);
                    updateCmd.Parameters.Add("@name", DbType.String);
                    deleteCmd.CommandText = "DELETE FROM saved_search_parameters WHERE search_id = @id; DELETE FROM saved_searches WHERE id = @id";
                    deleteCmd.Parameters.Add("@id", DbType.Int64);

                    // loop? I don't know
                    foreach (DataRow row in searches.Rows)
                    {
                        switch (row.RowState)
                        {
                            case DataRowState.Deleted:
                                deleteCmd.Parameters["@id"].Value = row["id", DataRowVersion.Original];
                                deleteCmd.ExecuteNonQuery();
                                break;
                            case DataRowState.Modified:
                                updateCmd.Parameters["@id"].Value = row["id"];
                                updateCmd.Parameters["@name"].Value = row["name"];
                                updateCmd.ExecuteNonQuery();
                                break;
                        }
                    }
                }

                // we made it!
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

        private static SQLiteConnection GetOpenConnection()
        {
            SQLiteConnection conn;

            conn = new SQLiteConnection("Data Source=" + m_dbPath);
            conn.Open();

            return conn;
        }

        private static DataTable GetTable(string sql)
        {
            DataTable data = new DataTable();

            using (SQLiteConnection conn = GetOpenConnection())
            {
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(sql, conn))
                {
                    adapter.Fill(data);
                }
            }

            return data;
        }

        private static object GetScalar(string sql, SQLiteConnection conn)
        {
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteScalar();
            }
        }
    }
}
