using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace HeavyDuck.EveAssetManager
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
    }
}