using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.XPath;
using System.Windows.Forms;
using HeavyDuck.Utilities.Forms;
using HeavyDuck.Eve;

namespace HeavyDuck.EveAssetManager
{
    public partial class KeyManager : Form
    {
        private static KeyManager the_instance = null;

        private DataTable m_keys;
        private DataTable m_characters;

        private KeyManager()
        {
            InitializeComponent();

            // initialize grids
            GridHelper.Initialize(grid_keys, false);
            GridHelper.Initialize(grid_characters, true);

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

            // add columns to key table
            GridHelper.AddColumn(grid_keys, "userID", "User ID");
            GridHelper.AddColumn(grid_keys, "apiKey", "Full API Key");
            grid_keys.AllowUserToAddRows = true;
            grid_keys.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid_keys.Columns["userID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid_keys.DataSource = m_keys;

            // add columns to characters table
            GridHelper.AddColumn(grid_characters, "userID", "User ID");
            GridHelper.AddColumn(grid_characters, "name", "Name");
            GridHelper.AddColumn(grid_characters, "characterID", "Character ID");
            GridHelper.AddColumn(grid_characters, "corporationName", "Corporation");
            GridHelper.AddColumn(grid_characters, "corporationID", "Corp ID");
            grid_characters.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid_characters.DataSource = m_characters;

            // no sorty-sort arrows
            GridHelper.DisableClickToSort(grid_keys, false);
            GridHelper.DisableClickToSort(grid_characters, false);

            // event handlers
            this.Load += new EventHandler(KeyManager_Load);
            add_button.Click += new EventHandler(add_button_Click);
            refresh_button.Click += new EventHandler(refresh_button_Click);
        }

        private void KeyManager_Load(object sender, EventArgs e)
        {
        }

        private void add_button_Click(object sender, EventArgs e)
        {
            m_keys.LoadDataRow(new object[] { 175621, @"dTTsX4o7JP6kxdHmZtiHUdTNeFy20jDTuJiroAi2a3XcoitXrspbXyslRUsrsGHe" }, true);
        }

        private void refresh_button_Click(object sender, EventArgs e)
        {
            string path;
            string apiKey;
            int userID;
            DataTable tempChars;

            // this is where we're gonna put the characters while we query and read XML and stuff
            tempChars = m_characters.Clone();
            
            foreach (DataRow row in m_keys.Rows)
            {
                // grab the account ID and key from the row
                userID = Convert.ToInt32(row["userID"]);
                apiKey = row["apiKey"].ToString();

                // query the API
                try
                {
                    path = EveApiHelper.GetCharacters(175621, @"dTTsX4o7JP6kxdHmZtiHUdTNeFy20jDTuJiroAi2a3XcoitXrspbXyslRUsrsGHe");
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
            m_characters.Rows.Clear();
            foreach (DataRow row in tempChars.Rows)
                m_characters.LoadDataRow(row.ItemArray, true);
        }

        public static new void Show(IWin32Window parent)
        {
            if (Monitor.TryEnter(typeof(KeyManager)))
            {
                if (the_instance == null) the_instance = new KeyManager();
                the_instance.ShowDialog(parent);

                Monitor.Exit(typeof(KeyManager));
            }
            else
            {
                throw new InvalidOperationException("The KeyManager is already open");
            }
        }
    }
}