﻿using System;
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

using Parse;

namespace IDE
{
    public partial class Form1 : Form
    {
        private List<FileEdit> edits;

        public Form1()
        {
            //Though in practice Visual Studio discourages editing the InitializeComponent function, I did so to assign custom event handlers, e.g. NewFile and OpenFile
            
            InitializeComponent();
            edits = new List<FileEdit>();
        }

        private void NewFile(object sender, EventArgs e)    //Handles newToolStripMenuItem.Click and toolStripSplitButtonNew.Click
        {
            var edit = new FileEdit(tabControl1);

            //User-friendliness, auto-focuses text editor in tab when opening a new file
            tabControl1.SelectTab(edit.Tab);
            edit.TextBox.Select();
            edits.Add(edit);

            UpdateUI();
        }

        private void OpenFile(object sender, EventArgs e)   //Handles openToolStripMenuItem.Click and toolStripButtonOpen.Click
        {
            //In-built file opening dialog
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Open file";
                dialog.Multiselect = false;
                dialog.Filter = "Functional Studio files (*.ps)|*.ps";
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                Enabled = false;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var edit = new FileEdit(tabControl1, dialog.FileName);

                        //User-friendliness, auto-focuses text editor in tab when opening a new file
                        tabControl1.SelectTab(edit.Tab);
                        edit.TextBox.Select();
                        edits.Add(edit);

                        UpdateUI();
                    }
                    catch (FileNotFoundException)
                    {
                        //In theory this exception catch should never occur, as the dialog handles all file path issues. It's here just in case
                        MessageBox.Show("File not found.");
                    }
                }

                Enabled = true;
            }
        }

        private void SaveFile(object sender, EventArgs e)   //Handles saveToolStripMenuItem.Click and toolStripButtonSave.Click
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
                        SaveTabFileAs(edit);
                    }

                    break;
                }
            }

            UpdateUI();
        }
        private void SaveFileAs(object sender, EventArgs e) //Handles saveAsToolStripMenuItem.Click and toolStripButtonSaveAs.Click
        {
            foreach (FileEdit edit in edits)
            {
                if (tabControl1.SelectedTab == edit.Tab)
                {
                    SaveTabFileAs(edit);

                    break;
                }
            }

            UpdateUI();
        }

        //Subroutine to handle save-as file dialog, placed in a separate function as both SaveFile and SaveFileAs call the same sequence
        private void SaveTabFileAs(FileEdit edit)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save file as";
                dialog.Filter = "Functional Studio files (*.ps)|*.ps";
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
                        //Again, this exception catch should never occur, as the dialog will handle file path issues
                        MessageBox.Show("Invalid file path");
                    }
                }
                Enabled = true;
            }
        }

        private void CloseFile(object sender, EventArgs e)      //Handles closeToolStripMenuItem.Click
        {
            foreach (FileEdit edit in edits)
            {
                if (tabControl1.SelectedTab == edit.Tab)
                {
                    var result = DialogResult.No;

                    if (!edit.Saved)
                    {
                        string displayName = edit.FilePath;
                        if (displayName == "")
                        {
                            displayName = "Untitled";
                        }
                        result = MessageBox.Show($"Save file \"{displayName}\"?", "Save file?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    }

                    if (result != DialogResult.Cancel)
                    {
                        if (result == DialogResult.Yes)
                        {
                            SaveFile(this, EventArgs.Empty);        //Using this rather than edit.SaveFile in case it is a new file and hasn't been saved before
                        }

                        edits.Remove(edit);
                        edit.Dispose();
                    }

                    break;
                }
            }
            
            UpdateUI();
        }

        private void BuildProgram(object sender, EventArgs e)   //Handles buildProgramToolStripMenuItem.Click
        {
            foreach (FileEdit edit in edits)
            {
                if (tabControl1.SelectedTab == edit.Tab)
                {
                    Translator.CallMe(edit.TextBox.Text);

                    break;
                }
            }
        }

        //This function should be called when at any point the state of the form changes such that the toolbar and menus need to have certain items enabled/disabled
        //Enabled/disable save and save as buttons depending on if files are open
        private void UpdateUI()
        {
            if (edits.Count > 0)
            {
                toolStripButtonSave.Enabled = true;
                toolStripButtonSaveAs.Enabled = true;
                saveToolStripMenuItem.Enabled = true;
                saveAsToolStripMenuItem.Enabled = true;
                closeToolStripMenuItem.Enabled = true;
            }
            else
            {
                toolStripButtonSave.Enabled = false;
                toolStripButtonSaveAs.Enabled = false;
                saveToolStripMenuItem.Enabled = false;
                saveAsToolStripMenuItem.Enabled = false;
                closeToolStripMenuItem.Enabled = false;
            }
        }

        //Allows for ctrl shortcuts. Directly maps to button features e.g. save
        //private void CheckShortcuts(object sender, KeyEventArgs e)
        //{
        //    if (e.Control)
        //    {
        //        switch (e.KeyCode)
        //        {
        //            case Keys.N:
        //                NewFile(this, EventArgs.Empty);
        //                break;

        //            case Keys.S:
        //                SaveFile(this, EventArgs.Empty);
        //                break;

        //            case Keys.O:
        //                OpenFile(this, EventArgs.Empty);
        //                break;

        //            case Keys.W:
        //                CloseFile(this, EventArgs.Empty);
        //                break;

        //            default:
        //                break;
        //        }
        //    }
        //}

        //^^ As is turns out, Visual Studio already has a solution for this as part of drop down menu properties

        //Classifies file-editing tab object, including handling nuance of unsaved files (titled 'Untitled'), saving and saving as, changing file path and tab title
        //when necessary i.e. once saved as a new file, title needs to change accordingly.
        private class FileEdit : IDisposable
        {
            public readonly TabPage Tab;
            public readonly EditorTextBox TextBox;
            public bool Saved;
            public string FilePath { get; private set; }

            public FileEdit(TabControl tabControl)
            {
                Tab = new TabPage("Untitled");
                TextBox = new EditorTextBox { Font = new Font("Consolas", 9) };
                Saved = true;
                FilePath = "";

                Tab.Controls.Add(TextBox);
                TextBox.Dock = DockStyle.Fill;  //Necessary for textbox to scale with the window properly when resizing
                TextBox.TextChanged += TextChanged;

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
                TextBox = new EditorTextBox {Text = text, Font = new Font("Consolas", 9)};
                Saved = true;
                FilePath = filePath;

                Tab.Controls.Add(TextBox);
                TextBox.Dock = DockStyle.Fill;
                TextBox.TextChanged += TextChanged;
                TextBox.UpdateLineNumbers();

                tabControl.TabPages.Add(Tab);
                TextBox.UpdateLineNumbers();
            }

            public void SaveFile()
            {
                if (!Saved)
                {
                    try
                    {
                        File.WriteAllText(FilePath, TextBox.Text);
                        Saved = true;
                        Tab.Text = Path.GetFileName(FilePath);

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


            public void Dispose()
            {
                TextBox.Dispose();
                Tab.Dispose();
            }

            private void TextChanged(object sender, EventArgs e)
            {
                //UI feature: asterisk indicates changes aren't saved
                if (Saved)
                {
                    Saved = false;
                    Tab.Text += '*';
                }
            }
        }
    }
}
