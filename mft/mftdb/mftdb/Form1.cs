using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;

using System.Text;
using System.Windows.Forms;

namespace mftdb
{
    public partial class Form1 : Form
    {        
        public Form1()
        {
            try
            {
                InitializeComponent();
                DBControl c = new DBControl('c');
            }
            catch(Exception e)
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(@"c:\debug.log"))
                {
                    writer.WriteLine(e.ToString());
                }
            }
        }
    }
}
