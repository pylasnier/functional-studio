using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Controls;

namespace IDE
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void NewFile(object sender, EventArgs e)
        {
            CreateEditorTab("Untitled");

            if(tabControl1.TabCount > 0)
            {
                tabControl1.Enabled = true;
            }

            tabControl1.SelectedTab.Controls[0].Select();
        }

        private void CreateEditorTab(string title, string text = "")
        {
            var newTab = new TabPage(title);
            var newRichTextBox = new System.Windows.Forms.RichTextBox();

            newRichTextBox.Dock = DockStyle.Fill;
            newRichTextBox.WordWrap = false;
            newRichTextBox.Font = new Font("Consolas", 9);
            newRichTextBox.Text = text;
            newRichTextBox.AcceptsTab = true;
            newRichTextBox.TextChanged += FileChanged;

            newTab.Controls.Add(newRichTextBox);
            tabControl1.TabPages.Add(newTab);
            tabControl1.SelectTab(newTab);
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void FileChanged(object sender, EventArgs e)
        {
            //if(e.GetType() == typeof(TextChangedEventArgs))
            //{
            //    var tce = (TextChangedEventArgs) e;
                
            //}

        }

        private class FileEdit
        {
            readonly TabPage Tab;
            readonly System.Windows.Forms.RichTextBox TextBox;
            public bool Saved { get; }
            public string File { get; }

            public FileEdit(TabPage tab)
            {
                Tab = tab
            }
        }
    }
}
