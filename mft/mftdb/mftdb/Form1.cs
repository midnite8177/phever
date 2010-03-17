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
        DBControl drive;
        public Form1()
        {            
            try
            {
                InitializeComponent();
                drive = new DBControl('c');
            }
            catch(Exception e)
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(@"c:\debug.log"))
                {
                    writer.WriteLine(e.ToString());
                }
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {                        
            if (e.KeyCode == Keys.Enter)
            {
                int index = 0;
                listView1.Items.Clear();
                foreach (var path in drive.Query(textBox1.Text, true))
                {
                    //var ext  =System.IO.Path.GetExtension(path);
                    //if(!imageList1.Images.ContainsKey(ext)) {
                    //    var icon = Icon.ExtractAssociatedIcon(path);
                    //    imageList1.Images.Add(ext,icon);
                    //}

                    listView1.Items.Add(path);
                    index++;
                    if (index > 100) break;
                }
            }
        }
    }
}
