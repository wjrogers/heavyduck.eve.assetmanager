using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using HeavyDuck.Eve;

namespace HeavyDuck.Eve.AssetManager
{
    internal static class Program
    {
        private static readonly string m_keysPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"HeavyDuck.Eve\keys.xml");

        private static DataTable m_keys;
        private static DataTable m_characters;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // initialize styles and crap
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // prep the key table
            m_keys = new DataTable("Keys");
            m_keys.Columns.Add("userID", typeof(int));
            m_keys.Columns.Add("apiKey", typeof(string));
            m_keys.PrimaryKey = new DataColumn[] { m_keys.Columns["userID"] };

            // make sure the keys path exists
            try
            {
                string dirPath = Path.GetDirectoryName(m_keysPath);
                if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
            }
            catch
            {
                // pass
            }

            // load keys from disk
            try
            {
                if (File.Exists(m_keysPath)) m_keys.ReadXml(m_keysPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load your saved API keys. You may need to enter them again.\n\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // prep the characters table
            m_characters = new DataTable("Characters");
            m_characters.Columns.Add("userID", typeof(int));
            m_characters.Columns.Add("name", typeof(string));
            m_characters.Columns.Add("characterID", typeof(int));
            m_characters.Columns.Add("corporationName", typeof(string));
            m_characters.Columns.Add("corporationID", typeof(int));

            // start the UI
            Application.Run(new MainForm());

            // save our API keys to disk
            try
            {
                m_keys.WriteXml(m_keysPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save your API keys. You may need to re-enter them next time.\n\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static DataTable ApiKeys
        {
            get { return m_keys; }
        }

        public static DataTable Characters
        {
            get { return m_characters; }
        }

        public static void RefreshCharacters()
        {
            string path;
            string apiKey;
            int userID;
            DataTable tempChars;

            // this is where we're gonna put the characters while we query and read XML and stuff
            tempChars = Program.Characters.Clone();
            
            foreach (DataRow row in Program.ApiKeys.Rows)
            {
                // grab the account ID and key from the row
                userID = Convert.ToInt32(row["userID"]);
                apiKey = row["apiKey"].ToString();

                // query the API
                try
                {
                    path = EveApiHelper.GetCharacters(userID, apiKey);
                }
                catch (EveApiException ex)
                {
                    MessageBox.Show(ex.ToString(), "EVE API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // parse the XML
                using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read))
                {
                    XPathDocument doc = new XPathDocument(fs);
                    XPathNavigator nav = doc.CreateNavigator();
                    XPathNodeIterator iter;
                    DataRow charRow;

                    iter = nav.Select("/eveapi/result/rowset/row");

                    while (iter.MoveNext())
                    {
                        charRow = tempChars.NewRow();
                        charRow["userID"] = userID;
                        charRow["name"] = iter.Current.SelectSingleNode("@name").Value;
                        charRow["characterID"] = iter.Current.SelectSingleNode("@characterID").ValueAsInt;
                        charRow["corporationName"] = iter.Current.SelectSingleNode("@corporationName").Value;
                        charRow["corporationID"] = iter.Current.SelectSingleNode("@corporationID").ValueAsInt;

                        tempChars.Rows.Add(charRow);
                    }
                }
            }

            // clear our character list and replace it
            Program.Characters.Rows.Clear();
            foreach (DataRow row in tempChars.Rows)
                Program.Characters.LoadDataRow(row.ItemArray, true);
        }
    }
}