using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Windows.Forms;

namespace HeavyDuck.Eve.AssetManager
{
    public partial class SearchClauseControl : UserControl
    {
        #region Static Initialization

        private static List<SearchField> m_fields;

        static SearchClauseControl()
        {
            SearchField field;

            m_fields = new List<SearchField>();

            // item name
            m_fields.Add(new SearchField("Name", "t.typeName", SearchField.SearchFieldType.String));

            // group name
            m_fields.Add(new SearchField("Group", "g.groupName", SearchField.SearchFieldType.String));

            // category name
            field = new SearchField("Category", "g.categoryID", SearchField.SearchFieldType.Enum);
            field.DataSource = GetFieldOptions("SELECT categoryName AS name, categoryID AS value FROM invCategories WHERE published = 1 ORDER BY categoryName");
            m_fields.Add(field);

            // flag
            m_fields.Add(new SearchField("Flag", "f.flagName", SearchField.SearchFieldType.String));

            // container
            m_fields.Add(new SearchField("Container", "ct.typeName", SearchField.SearchFieldType.String));

            // location
            m_fields.Add(new SearchField("Location", "COALESCE(l.itemName, cl.itemName)", SearchField.SearchFieldType.String));

            // owner
            m_fields.Add(new SearchField("Owner", "a.characterName", SearchField.SearchFieldType.String));
        }

        private static DataTable GetFieldOptions(string sql)
        {
            DataTable table = new DataTable();

            using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + Program.CcpDatabasePath))
            {
                conn.Open();

                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(sql, conn))
                    adapter.Fill(table);
            }

            return table;
        }

        #endregion

        private Control m_edit_control = null;
        private DataTable m_op_table;

        public event EventHandler RemoveClicked;

        public SearchClauseControl() : this("Name") { }

        public SearchClauseControl(string initialField)
        {
            InitializeComponent();

            // event handlers
            remove_button.Click += new EventHandler(remove_button_Click);
            field_combo.SelectedIndexChanged += new EventHandler(field_combo_SelectedIndexChanged);

            // setup the op table
            m_op_table = new DataTable("Operators");
            m_op_table.Columns.Add("name", typeof(string));
            m_op_table.Columns.Add("operator", typeof(ComparisonOp));

            // initialize op drop-down
            op_combo.DisplayMember = "name";
            op_combo.ValueMember = "operator";
            op_combo.DataSource = m_op_table;

            // initialize field drop-down
            foreach (SearchField field in m_fields)
                field_combo.Items.Add(field);
            int index = field_combo.FindString(initialField);
            if (index > -1)
                field_combo.SelectedIndex = index;
            else
                field_combo.SelectedIndex = 0;
        }

        private void field_combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            SearchField field = (SearchField)field_combo.SelectedItem;

            // get rid of the existing edit control
            if (m_edit_control != null)
            {
                this.Controls.Remove(m_edit_control);
                m_edit_control.Dispose();
            }

            // create the new edit control
            m_edit_control = field.GetControl();
            m_edit_control.Location = new Point(op_combo.Right + 6, 0);
            m_edit_control.Height = this.Height;
            m_edit_control.Width = remove_button.Left - m_edit_control.Left - 6;
            m_edit_control.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(m_edit_control);

            // fill the op table
            m_op_table.BeginLoadData();
            m_op_table.Rows.Clear();
            switch (field.FieldType)
            {
                case SearchField.SearchFieldType.String:
                    m_op_table.LoadDataRow(new object[] { "Like", ComparisonOp.Like }, true);
                    m_op_table.LoadDataRow(new object[] { "Not Like", ComparisonOp.NotLike }, true);
                    m_op_table.LoadDataRow(new object[] { "Equals", ComparisonOp.Equals }, true);
                    m_op_table.LoadDataRow(new object[] { "Not Equals", ComparisonOp.NotEquals }, true);
                    break;
                case SearchField.SearchFieldType.Enum:
                    m_op_table.LoadDataRow(new object[] { "Equals", ComparisonOp.Equals }, true);
                    m_op_table.LoadDataRow(new object[] { "Not Equals", ComparisonOp.NotEquals }, true);
                    break;
                case SearchField.SearchFieldType.Number:
                    m_op_table.LoadDataRow(new object[] { "Equals", ComparisonOp.Equals }, true);
                    m_op_table.LoadDataRow(new object[] { "Not Equals", ComparisonOp.NotEquals }, true);
                    break;
            }
            m_op_table.EndLoadData();
            op_combo.SelectedIndex = 0;
        }

        private void remove_button_Click(object sender, EventArgs e)
        {
            EventHandler handler = this.RemoveClicked;

            // raise the event
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// Gets the selected comparison operator.
        /// </summary>
        public ComparisonOp SelectedComparisonOp
        {
            get { return (ComparisonOp)op_combo.SelectedValue; }
            set { op_combo.SelectedValue = value; }
        }

        /// <summary>
        /// Gets the value selected in the field combo.
        /// </summary>
        public SearchField SelectedField
        {
            get { return (SearchField)field_combo.SelectedItem; }
        }

        /// <summary>
        /// Gets the current value of the search field.
        /// </summary>
        public object Value
        {
            get
            {
                object value;
                
                // extract from the edit control if we can
                if (m_edit_control is TextBox)
                    value = ((TextBox)m_edit_control).Text;
                else if (m_edit_control is ComboBox)
                    value = ((ComboBox)m_edit_control).SelectedValue;
                else
                    value = null;

                // do a check for empty strings
                if (value is string && ((string)value).Trim() == "")
                    value = null;

                // now return it
                return value;
            }
        }

        /// <summary>
        /// Gets or sets the visibility of the remove button.
        /// </summary>
        public bool RemoveButtonVisible
        {
            get { return remove_button.Visible; }
            set { remove_button.Visible = value; }
        }

        public enum BooleanOp
        {
            And,
            Or
        }

        public enum ComparisonOp
        {
            Like,
            NotLike,
            Equals,
            NotEquals
        }

        public class SearchField
        {
            private static Dictionary<string, int> m_nameCounter = new Dictionary<string, int>();
            private static System.Text.RegularExpressions.Regex m_nameSanitizer = new System.Text.RegularExpressions.Regex(@"\W+");

            private string m_name;
            private string m_dbField;
            private SearchFieldType m_fieldType;
            private object m_dataSource = null;

            public enum SearchFieldType
            {
                String,
                Enum,
                Number
            }

            public SearchField(string name, string dbField, SearchFieldType fieldType)
            {
                m_name = name;
                m_dbField = dbField;
                m_fieldType = fieldType;
            }

            public string Name
            {
                get { return m_name; }
            }

            public string DbField
            {
                get { return m_dbField; }
            }

            public SearchFieldType FieldType
            {
                get { return m_fieldType; }
            }

            public object DataSource
            {
                get { return m_dataSource; }
                set { m_dataSource = value; }
            }

            public Control GetControl()
            {
                switch (m_fieldType)
                {
                    case SearchFieldType.Enum:
                        ComboBox combo = new ComboBox();

                        combo.DropDownStyle = ComboBoxStyle.DropDownList;
                        if (m_dataSource is DataTable)
                        {
                            combo.DisplayMember = "name";
                            combo.ValueMember = "value";
                        }
                        combo.DataSource = m_dataSource;

                        return combo;
                    case SearchFieldType.String:
                        return new TextBox();
                    case SearchFieldType.Number:
                        return new TextBox();
                    default:
                        throw new InvalidOperationException("Don't know how to make a control for SearchFieldType " + m_fieldType);
                }
            }

            public string GetParameterName()
            {
                string sanitizedName;
                
                // make the name safe for use as a database parameter
                sanitizedName = m_nameSanitizer.Replace(m_name, "");

                // count stuff
                if (!m_nameCounter.ContainsKey(sanitizedName)) m_nameCounter[sanitizedName] = 0;
                m_nameCounter[sanitizedName] += 1;
                return "@" + sanitizedName + m_nameCounter[sanitizedName].ToString();
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}