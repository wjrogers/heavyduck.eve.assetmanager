using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace HeavyDuck.Eve.AssetManager
{
    public partial class AboutForm : Form
    {
        public const string HOMEPAGE = @"http://wiki.heavyduck.com/EveAssetManager";

        public AboutForm()
        {
            InitializeComponent();

            this.Load += new EventHandler(AboutForm_Load);
            link_label.LinkClicked += new LinkLabelLinkClickedEventHandler(link_label_LinkClicked);
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            version_label.Text = "Version " + GetVersionString(true);
            copyright_label.Text = "Copyright © William J Rogers 2008-2010";
            link_label.Text = HOMEPAGE;
        }

        private void link_label_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(HOMEPAGE);
        }

        public static string GetVersionString(bool includeBuild)
        {
            string[] versionBits = Application.ProductVersion.Split('.');

            if (includeBuild)
                return string.Join(".", versionBits, 0, 3) + " (build " + versionBits[3] + ")";
            else
                return string.Join(".", versionBits, 0, 3);
        }
    }
}
