using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Parse;
using Utility;
using Timer = System.Windows.Forms.Timer;

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
                    CloseTabFile(edit);

                    break;
                }
            }
            
            UpdateUI();
        }


        private void CloseAll(object sender, FormClosingEventArgs e)    //Handles FormClosing
        {
            FileEdit[] copyEdits = new FileEdit[edits.Count];
            edits.CopyTo(copyEdits);
            foreach (FileEdit edit in copyEdits)
            {
                if (!CloseTabFile(edit))
                {
                    e.Cancel = true;
                    break;
                }
            }
        }

        //Subroutine to handle close dialog, placed in separate function as both CloseFile and CloseAll, also returns success or failure to cancel CloseAll
        private bool CloseTabFile(FileEdit edit)
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
            else
            {
                return false;
            }

            return true;
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
            
            private CancellationTokenSource tokenSource = new CancellationTokenSource();

            public FileEdit(TabControl tabControl)
            {
                Tab = new TabPage("Untitled");
                TextBox = new EditorTextBox { Font = new Font("Consolas", 9) };
                Saved = true;
                FilePath = "";

                Tab.Controls.Add(TextBox);
                TextBox.Dock = DockStyle.Fill;  //Necessary for textbox to scale with the window properly when resizing
                TextBox.TextChanged += TextChanged;
                tabControl.Resize += OnResize;

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
                tabControl.Resize += OnResize;

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

            private async void TextChanged(object sender, EventArgs e)
            {
                //UI feature: asterisk indicates changes aren't saved
                if (Saved)
                {
                    Saved = false;
                    Tab.Text += '*';
                }

                tokenSource.Cancel();
                tokenSource = new CancellationTokenSource();
                try
                {
                    await ExecuteAfterTime(Parse, 1000);
                }
                catch { }
            }

            private void OnResize(object sender, EventArgs e)
            {
                TextBox.UpdateLineNumbers();
            }

            private void Parse()
            {
                IntPtr empty = IntPtr.Zero;
                CHARFORMAT format;
                SendMessage(TextBox.TextHandle, WM_SETREDRAW, IntPtr.Zero, ref empty);

                int selectionStart = TextBox.SelectionStart;
                int selectionLength = TextBox.SelectionLength;

                TextBox.TextChanged -= TextChanged;

                TextBox.SelectAll();

                format = new CHARFORMAT();
                format.cbSize = Marshal.SizeOf(format);
                format.dwMask = CFM_UNDERLINETYPE;
                format.bUnderlineType = 0;
                SendMessage(TextBox.TextHandle, EM_SETCHARFORMAT, (IntPtr) SCF_SELECTION, ref format);

                var parserReturn = Translator.CallMe(TextBox.Text);

                if (!parserReturn.Success)
                {
                    foreach (ParserReturnErrorInfo error in parserReturn.Errors)
                    {
                        if (error.Error == ParserReturnError.BadIdentifier)
                        {
                            TextBox.SelectionStart = error.Index;
                            int i = 0;
                            while (!string.IsNullOrWhiteSpace(TextBox.Text[TextBox.SelectionStart + i].ToString())) i++;
                            TextBox.SelectionLength = i;

                            format = new CHARFORMAT();
                            format.cbSize = Marshal.SizeOf(format);
                            format.dwMask = CFM_UNDERLINETYPE;
                            format.bUnderlineType = WaveUnderlineStyle | RedUnderlineColour;
                            SendMessage(TextBox.TextHandle, EM_SETCHARFORMAT, (IntPtr) SCF_SELECTION, ref format);
                        }
                    }
                }

                TextBox.SelectionStart = selectionStart;
                TextBox.SelectionLength = selectionLength;

                TextBox.TextChanged += TextChanged;

                SendMessage(TextBox.TextHandle, WM_SETREDRAW, (IntPtr) 1, ref empty);
                TextBox.TextUpdate();
            }

            private async Task ExecuteAfterTime(Action action, int timeoutInMilliseconds)
            {
                await Task.Delay(timeoutInMilliseconds, tokenSource.Token);
                action();
            }
        }

        private const uint CFM_UNDERLINETYPE = 0x800000;
        private const int SCF_SELECTION = 1;
        private const int EM_SETCHARFORMAT = 0x0444;
        private const int WM_SETREDRAW = 0x000b;
        private const byte WaveUnderlineStyle = 8;
        private const byte RedUnderlineColour = 0x50;

        //http://geekswithblogs.net/pvidler/archive/2003/10/15/188.aspx
        [StructLayout(LayoutKind.Sequential)]
        private struct CHARFORMAT
        {
            public int cbSize;
            public uint dwMask;
            public uint dwEffects;
            public int yHeight;
            public int yOffset;
            public int crTextColor;
            public byte bCharSet;
            public byte bPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szFaceName;

            // CHARFORMAT2 from here onwards.
            public short wWeight;
            public short sSpacing;
            public int crBackColor;
            public int LCID;
            public uint dwReserved;
            public short sStyle;
            public short wKerning;
            public byte bUnderlineType;
            public byte bAnimation;
            public byte bRevAuthor;
        }

        [DllImport("User32.dll")]
        private static extern int SendMessage(IntPtr handle, int message, IntPtr wParam, ref CHARFORMAT lParam);
        [DllImport("User32.dll")]
        private static extern int SendMessage(IntPtr handle, int message, IntPtr wParam, ref IntPtr lParam);
    }
}
