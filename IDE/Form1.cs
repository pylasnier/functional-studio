using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IDE
{
    public partial class Form1 : Form
    {
        private List<FileEdit> edits;

        public Form1()
        {
            InitializeComponent();
            edits = new List<FileEdit>();
        }

        private void NewFile(object sender, EventArgs e)
        {
            var edit = new FileEdit(tabControl1);

            tabControl1.SelectTab(edit.Tab);
            edit.TextBox.Select();
            edits.Add(edit);
        }

        private void OpenFile(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Open file";
                dialog.Multiselect = false;
                dialog.Filter = "Functional Studio files (*.func)|*.func";
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                Enabled = false;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var edit = new FileEdit(tabControl1, dialog.FileName);

                        tabControl1.SelectTab(edit.Tab);
                        edit.TextBox.Select();
                        edits.Add(edit);
                    }
                    catch (FileNotFoundException)
                    {
                        MessageBox.Show("File not found.");
                    }
                }

                Enabled = true;
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (edits.Count > 0)
            {
                toolStripButtonSave.Enabled = true;
                toolStripButtonSaveAs.Enabled = true;
                saveToolStripMenuItem.Enabled = true;
                saveAsToolStripMenuItem.Enabled = true;
            }
            else
            {
                toolStripButtonSave.Enabled = false;
                toolStripButtonSaveAs.Enabled = false;
                saveToolStripMenuItem.Enabled = false;
                saveAsToolStripMenuItem.Enabled = false;
            }
        }

        private void SaveFile(object sender, EventArgs e)
        {
            foreach (FileEdit edit in edits)
            {
                if (tabControl1.SelectedTab == edit.Tab)
                {
                    if(edit.FilePath != "")
                    {
                        edit.SaveFile();
                    }
                    else
                    {
                        using (var dialog = new SaveFileDialog())
                        {
                            dialog.Title = "Save file as";
                            dialog.Filter = "Functional Studio files (*.func)|*.func";
                            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                            Enabled = false;
                            if (dialog.ShowDialog() == DialogResult.OK)
                            {
                                try
                                {
                                    edit.SaveFileAs(dialog.FileName);
                                }
                                catch (DirectoryNotFoundException)
                                {
                                    MessageBox.Show("Invalid file path");
                                }
                            }
                        }
                    }

                    break;
                }
            }

            
        }
        private void SaveFileAs(object sender, EventArgs e)
        {

        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private class FileEdit
        {
            public readonly TabPage Tab;
            public readonly RichTextBox TextBox;
            public bool Saved;
            public string FilePath { get; private set; }

            public FileEdit(TabControl tabControl)
            {
                Tab = new TabPage("Untitled");
                TextBox = new RichTextBox();
                Saved = true;
                FilePath = "";

                Tab.Controls.Add(TextBox);
                TextBox.Dock = DockStyle.Fill;
                TextBox.WordWrap = false;
                TextBox.Font = new Font("Consolas", 9);
                TextBox.AcceptsTab = true;
                TextBox.TextChanged += FileChanged;

                tabControl.TabPages.Add(Tab);
            }

            public FileEdit(TabControl tabControl, string filePath)
            {
                string text;

                try
                {
                    text = File.ReadAllText(filePath);
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine("File path not found");
                    throw e;
                }
                
                Tab = new TabPage(Path.GetFileName(filePath));
                TextBox = new RichTextBox {Text = text};
                Saved = true;
                FilePath = filePath;

                Tab.Controls.Add(TextBox);
                TextBox.Dock = DockStyle.Fill;
                TextBox.WordWrap = false;
                TextBox.Font = new Font("Consolas", 9);
                TextBox.AcceptsTab = true;
                TextBox.TextChanged += FileChanged;

                tabControl.TabPages.Add(Tab);
            }

            public void SaveFile()
            {
                if (!Saved)
                {
                    try
                    {
                        File.WriteAllText(FilePath, TextBox.Text);
                        Saved = true;
                        Tab.Text = Tab.Text.Substring(0, Tab.Text.Length - 1);

                    }
                    catch (DirectoryNotFoundException e)
                    {
                        Console.WriteLine("Invalid file path");
                        throw e;
                    }
                }
            }

            public void SaveFileAs(string filePath)
            {
                FilePath = filePath;
                Saved = false;
                SaveFile();
            }

            private void FileChanged(object sender, EventArgs e)
            {
                if (Saved)
                {
                    Saved = false;
                    Tab.Text += '*';
                }
            }
        }
    }
}
