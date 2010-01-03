using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using HeavyDuck.Eve;
using HeavyDuck.Utilities.Forms;

namespace HeavyDuck.Eve.AssetManager
{
    internal static class Program
    {
        private const string CCP_DB_NAME = @"trinity_1.0_sqlite3.db";

        private static readonly string m_dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"HeavyDuck.Eve");

        private static string m_ccpDbPath;

        private static DataTable m_keys;
        private static DataTable m_characters;
        private static OptionsDialog m_options;
        private static IPriceProvider m_priceProvider = EveCentralHelper.Instance;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // initialize styles and crap
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // fix the fucking retarded default toolstrip look
            ToolStripManager.VisualStylesEnabled = false;

            // look for the static data db
#if DEBUG
            m_ccpDbPath = Path.Combine(@"C:\Temp", CCP_DB_NAME);
#else
            m_ccpDbPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), CCP_DB_NAME);
#endif
            if (!File.Exists(m_ccpDbPath))
            {
                MessageBox.Show("Could not find the CCP database file. Please download it from http://dl.eve-files.com/media/0712/trinity_1.0_sqlite3.db.zip!", "Database Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // prep the key table
            m_keys = new DataTable("Keys");
            m_keys.Columns.Add("userID", typeof(int));
            m_keys.Columns.Add("apiKey", typeof(string));
            m_keys.PrimaryKey = new DataColumn[] { m_keys.Columns["userID"] };

            // prep the characters table
            m_characters = new DataTable("Characters");
            m_characters.Columns.Add("userID", typeof(int));
            m_characters.Columns.Add("name", typeof(string));
            m_characters.Columns.Add("characterID", typeof(int));
            m_characters.Columns.Add("corporationName", typeof(string));
            m_characters.Columns.Add("corporationID", typeof(int));
            m_characters.Columns.Add("queryCorp", typeof(bool));
            m_characters.PrimaryKey = new DataColumn[] { m_characters.Columns["characterID"] };

            // make sure the data path exists
            try
            {
                if (!Directory.Exists(m_dataPath)) Directory.CreateDirectory(m_dataPath);
            }
            catch
            {
                // pass
            }

            // create options dialog
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Program), "Resources.options.xml"))
                m_options = OptionsDialog.FromStream(stream);

            // load options
            try
            {
                string optionsXml = DataStore.GetSetting("options");
                if (!string.IsNullOrEmpty(optionsXml))
                {
                    using (StringReader reader = new StringReader(optionsXml))
                        m_options.LoadValuesXml(reader);
                }
            }
            catch (Exception ex)
            {
                MainForm.ShowException(null, "Failed to load your options.", ex);
            }

            // initialize the eve types from the ccp database (if this fails, we got problems, so let it crash the app)
            EveTypes.Initialize(CcpDatabaseConnectionString);

            // load keys and characters from disk
            LoadDataTable(m_keys, "keys.xml", "Failed to load your saved API keys. You may need to enter them again.");
            LoadDataTable(m_characters, "characters.xml", "Failed to load your character list. You may need to reset your corp asset preferences.");

            // load price provider cache
            m_priceProvider.LoadCache();

            // start the UI
            Application.Run(new MainForm());

            // save our API keys and characters to disk
            SaveDataTable(m_keys, "keys.xml", "Failed to save your API keys. You may need to re-enter them next time.");
            SaveDataTable(m_characters, "characters.xml", "Failed to save your character list. You may need to reset your corp asset preferences next time.");

            // save options
            try
            {
                using (StringWriter writer = new StringWriter())
                {
                    m_options.SaveValuesXml(writer);
                    DataStore.SetSetting("options", writer.ToString());
                }
            }
            catch (Exception ex)
            {
                MainForm.ShowException(null, "Failed to save your options.", ex);
            }

            // save price provider cache
            m_priceProvider.SaveCache();
        }

        #region Public Properties

        public static DataTable ApiKeys
        {
            get { return m_keys; }
        }

        public static DataTable Characters
        {
            get { return m_characters; }
        }

        public static string DataPath
        {
            get { return m_dataPath; }
        }

        public static string CcpDatabasePath
        {
            get { return m_ccpDbPath; }
        }

        public static string CcpDatabaseConnectionString
        {
            get { return "Data Source=" + m_ccpDbPath; }
        }

        public static OptionsDialog OptionsDialog
        {
            get { return m_options; }
        }

        public static IPriceProvider PriceProvider
        {
            get { return m_priceProvider; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets an item's price, as determined by market data and current user options.
        /// </summary>
        public static decimal GetCompositePrice(EveItemType type, decimal? marketPrice)
        {
            if (type.Category.CategoryName == "Blueprint" && Program.OptionsDialog["Pricing.ZeroBlueprints"].ValueAsBoolean)
                return 0;
            else if (marketPrice.HasValue)
                return marketPrice.Value;
            else if (Program.OptionsDialog["Pricing.UseBasePrice"].ValueAsBoolean)
            {
                if (Program.OptionsDialog["Pricing.CorrectBasePrice"].ValueAsBoolean && type.Category.CategoryName == "Structure" && type.Group.GroupName != "Control Tower")
                    return (type.BasePrice * 0.9m) / type.PortionSize;
                else
                    return type.BasePrice / type.PortionSize;
            }
            else
                return 0m;
        }

        public static void RefreshCharacters()
        {
            string path;
            string apiKey;
            int userID;
            DataTable tempChars;
            CachedResult result;

            // this is where we're gonna put the characters while we query and read XML and stuff
            tempChars = m_characters.Clone();
            
            foreach (DataRow row in Program.ApiKeys.Rows)
            {
                // grab the account ID and key from the row
                userID = Convert.ToInt32(row["userID"]);
                apiKey = row["apiKey"].ToString();

                // query the API
                result = EveApiHelper.GetCharacters(userID, apiKey);
                if (result.State == CacheState.Uncached)
                {
                    MessageBox.Show("Failed to fetch characters for UserID " + userID.ToString() + result.Exception == null ? "" : "\n\n" + result.Exception.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }
                else
                {
                    path = result.Path;
                }

                // parse the XML
                using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read))
                {
                    XPathDocument doc = new XPathDocument(fs);
                    XPathNavigator nav = doc.CreateNavigator();
                    XPathNodeIterator iter;
                    DataRow charRow, existingRow;

                    iter = nav.Select("/eveapi/result/rowset/row");

                    while (iter.MoveNext())
                    {
                        // create the new row
                        charRow = tempChars.NewRow();
                        charRow["userID"] = userID;
                        charRow["name"] = iter.Current.SelectSingleNode("@name").Value;
                        charRow["characterID"] = iter.Current.SelectSingleNode("@characterID").ValueAsInt;
                        charRow["corporationName"] = iter.Current.SelectSingleNode("@corporationName").Value;
                        charRow["corporationID"] = iter.Current.SelectSingleNode("@corporationID").ValueAsInt;
                        charRow["queryCorp"] = true;

                        // try to find a matching row from the current characters table and keep that queryCorp value if we do
                        existingRow = m_characters.Rows.Find(charRow["characterID"]);
                        if (existingRow != null)
                            charRow["queryCorp"] = existingRow["queryCorp"];

                        // add the row to the temp table
                        tempChars.Rows.Add(charRow);
                    }
                }
            }

            // clear our character list and replace it
            m_characters.Rows.Clear();
            foreach (DataRow row in tempChars.Rows)
                m_characters.LoadDataRow(row.ItemArray, true);
        }

        #endregion

        #region Private Methods

        private static void LoadDataTable(DataTable table, string fileName, string errorText)
        {
            string path = Path.Combine(m_dataPath, fileName);

            try
            {
                if (File.Exists(path)) table.ReadXml(path);
            }
            catch (Exception ex)
            {
                MainForm.ShowException(null, errorText, ex);
            }
        }

        private static void SaveDataTable(DataTable table, string fileName, string errorText)
        {
            string path = Path.Combine(m_dataPath, fileName);

            try
            {
                table.WriteXml(path);
            }
            catch (Exception ex)
            {
                MainForm.ShowException(null, errorText, ex);
            }
        }

        #endregion
    }
}