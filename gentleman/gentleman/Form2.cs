using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace gentleman
{
    public partial class Form2 : Form
    {
        private Form1 ParentForm = null;
        
        public Form2(Form1 pForm)
        {
            InitializeComponent();            
            ParentForm = pForm;
            ShowFolderOption();
        }

        private void ShowFolderOption()
        {
            foreach (var item in ParentForm.LoadFolderOption())
            {
                checkedListBox1.Items.Add(item.Key, item.Value);
            }
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            var result = folderBrowserDialog1.ShowDialog();
            checkedListBox1.Items.Add(folderBrowserDialog1.SelectedPath, true);            
        }

        private void ButtonRemove_Click(object sender, EventArgs e)
        {
            checkedListBox1.Items.Remove(checkedListBox1.SelectedItem);
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            Dictionary<string, bool> FolderOption = new Dictionary<string, bool>();
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                FolderOption[checkedListBox1.Items[i].ToString()] = checkedListBox1.GetItemChecked(i);                
            }
            ParentForm.UpdateFolderOption(FolderOption);
            this.Close();
        }

        private void ButtonCancal_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
