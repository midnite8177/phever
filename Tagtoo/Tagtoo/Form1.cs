using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Tagtoo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //CChangeJournal.SelfTest();
            MFT.SelfTest();
        }

        private void fileSystemWatcher1_Changed(object sender, System.IO.FileSystemEventArgs e)
        {

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}


