using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace HeavyDuck.Eve.AssetManager
{
    public partial class NewKeyDialog : Form
    {
        public NewKeyDialog()
        {
            InitializeComponent();
        }

        public int UserID
        {
            get { return Convert.ToInt32(id_box.Text.Trim()); }
        }

        public string ApiKey
        {
            get { return key_box.Text.Trim(); }
        }
    }
}