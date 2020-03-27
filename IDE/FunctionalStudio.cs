using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using Parse;

namespace IDE
{
    public partial class FunctionalStudio : Form
    {
        private List<FileEdit> edits;
        private bool running;

        public FunctionalStudio()
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
                dialog.Filter = "Paskell files (*.ps)|*.ps";
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

        private void SaveFileAs(object sender, EventArgs e) //Handles saveAsToolStripMenuItem.Click
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

        private void SaveAll(object sender, EventArgs e)    //Handles toolStripButtonSaveAll.Click
        {
            foreach (FileEdit edit in edits)
            {
                //Equivalent to the SaveFile sequence, except for a break in the loop when SaveAs fails
                if (edit.FilePath != "")
                {
                    edit.SaveFile();
                }
                else
                {
                    if (!SaveTabFileAs(edit))
                    {
                        break;
                    }
                }
            }
        }

        //Subroutine to handle save-as file dialog, placed in a separate function as both SaveFile and SaveFileAs call the same sequence
        private bool SaveTabFileAs(FileEdit edit)
        {
            bool success = false;
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save file as";
                dialog.Filter = "Paskell files (*.ps)|*.ps";
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                Enabled = false;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        edit.SaveFileAs(dialog.FileName);
                        success = true;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        //Again, this exception catch should never occur, as the dialog will handle file path issues
                        MessageBox.Show("Invalid file path");
                    }
                }
                Enabled = true;
            }

            return success;
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
                //Attempts to close every open edit and breaks from loop if one fails
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

        private void StartProgram(object sender, EventArgs e)   //Handles startToolStripMenuItem.Click and toolStripSplitButtonStart.Click
        {
            PContext context;
            CompilerReturnState returnState;
            string sourceCode = "";

            running = true;
            UpdateUI();

            //Gets source code from active tab
            foreach (FileEdit edit in edits)
            {
                if (tabControl1.SelectedTab == edit.Tab)
                {
                    edit.TextBox.Enabled = false;
                    sourceCode = edit.TextBox.Text;
                    edit.HighlightErrors();

                    break;
                }
            }

            output.Text = "";

            returnState = Translator.Compile(sourceCode, out context);

            if (!returnState.Success)
            {
                //Any compile errors prevent execution and are printed in output
                while (returnState.Exceptions.Count > 0)
                {
                    PaskellCompileException exception = returnState.Exceptions.Dequeue();
                    output.Text += Environment.NewLine;
                    output.Text += $"{exception.ErrorMessage} on line {exception.Line + 1}, token {exception.Index + 1}";
                }
            }
            else
            {
                try
                {
                    //Similar pattern to that within compiler when defining expressions, ensure that there is one and only one "main" definition
                    PExpression[] results = context.Expressions.Where(x => x.Identifier == "main").ToArray();
                    if (results.Length != 1)
                    {
                        throw new PaskellRuntimeException("No unique definition for main", null);
                    }
                    else
                    {
                        PExpression main = results[0];
                        PExpression result = main.Evaluate();       //Run the code
                        output.Text += result.Value;
                    }
                }
                catch (PaskellRuntimeException f)
                {
                    output.Text += $"{f.ErrorMessage}";
                    if (f.PExpression != null)
                    {
                        output.Text += $" in expression {f.PExpression.Identifier}";
                    }
                }
            }

            running = false;
            UpdateUI();

            foreach (FileEdit edit in edits)
            {
                if (tabControl1.SelectedTab == edit.Tab)
                {
                    edit.TextBox.Enabled = true;

                    break;
                }
            }
        }

        private void CancelChangeTab(object sender, TabControlCancelEventArgs e)        //Handles tabControl1.Selecting
        {
            if (running)
            {
                e.Cancel = true;
            }
        }

        //This function should be called when at any point the state of the form changes such that the toolbar and menus need to have certain items enabled/disabled
        //Enabled/disable save and save as buttons depending on if files are open
        private void UpdateUI()
        {
            if (edits.Count > 0 && !running)
            {
                toolStripButtonNew.Enabled = true;
                toolStripButtonOpen.Enabled = true;
                newToolStripMenuItem.Enabled = true;
                openToolStripMenuItem.Enabled = true;
                toolStripButtonSave.Enabled = true;

                toolStripButtonSaveAll.Enabled = true;
                saveToolStripMenuItem.Enabled = true;
                saveAsToolStripMenuItem.Enabled = true;
                toolStripSplitButtonStart.Enabled = true;
                closeToolStripMenuItem.Enabled = true;
                startToolStripMenuItem.Enabled = true;
            }
            else if (running)
            {
                toolStripButtonNew.Enabled = false;
                toolStripButtonOpen.Enabled = false;
                newToolStripMenuItem.Enabled = false;
                openToolStripMenuItem.Enabled = false;

                toolStripButtonSave.Enabled = false;
                toolStripButtonSaveAll.Enabled = false;
                saveToolStripMenuItem.Enabled = false;
                saveAsToolStripMenuItem.Enabled = false;
                toolStripSplitButtonStart.Enabled = false;
                closeToolStripMenuItem.Enabled = false;
                startToolStripMenuItem.Enabled = false;
            }
            else
            {
                toolStripButtonNew.Enabled = true;
                toolStripButtonOpen.Enabled = true;
                newToolStripMenuItem.Enabled = true;
                openToolStripMenuItem.Enabled = true;

                toolStripButtonSave.Enabled = false;
                toolStripButtonSaveAll.Enabled = false;
                saveToolStripMenuItem.Enabled = false;
                saveAsToolStripMenuItem.Enabled = false;
                toolStripSplitButtonStart.Enabled = false;
                closeToolStripMenuItem.Enabled = false;
                startToolStripMenuItem.Enabled = false;
            }
        }

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

            //This overload is used when creating a new FileEdit instance but for a file which is being opened
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
                TextBox.UpdateLineNumbers();//----->//Necessary for the reason that the size of the EditorTextBox control changes after it is instantiated and updated
            }                                       //therefore it scales the internal components incorrectly, so needs to be reupdated otherwise it remains incorrectly
                                                    //scaled until the text is changed
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

            //Allows for safe removal of these objects when tabs in the control are closed
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

                //tokenSource.Cancel();
                //tokenSource = new CancellationTokenSource();
                //try
                //{
                //    await ExecuteAfterTime(Parse, 1000);
                //}
                //catch { }
            }

            private void OnResize(object sender, EventArgs e)
            {
                TextBox.UpdateLineNumbers();        //Called for the same reason as it is called in the constructor for opening a file
            }

            //Primary attempt of text highlighting using the EM_SETCHARFORMAT message as part of the windows controls rich edit tools
            public void HighlightErrors()
            {
                IntPtr empty = IntPtr.Zero;
                CHARFORMAT format;
                SendMessage(TextBox.TextHandle, WM_SETREDRAW, IntPtr.Zero, ref empty);      //Disables drawing of the textbox so the user doesn't see the text selection process

                int selectionStart = TextBox.SelectionStart;
                int selectionLength = TextBox.SelectionLength;

                TextBox.TextChanged -= TextChanged;         //This prevents all the app processing these changes as the file being edited

                TextBox.SelectAll();

                format = new CHARFORMAT();
                format.cbSize = Marshal.SizeOf(format);
                format.dwMask = CFM_UNDERLINETYPE;
                format.bUnderlineType = 0;
                SendMessage(TextBox.TextHandle, EM_SETCHARFORMAT, (IntPtr) SCF_SELECTION, ref format);      //Removes all underlining of text

                Queue<TokeniserReturnError> Errors = Translator.GetTokeniserErrors(TextBox.Text);
                while (Errors.Count > 0)
                {
                    TokeniserReturnError error = Errors.Dequeue();
                    TextBox.SelectionStart = error.Index;
                    int i = 0;
                    //This while loops runs to the end of a word (or the end of the file) to select it to underline
                    while (TextBox.SelectionStart + i < TextBox.Text.Length && !string.IsNullOrWhiteSpace(TextBox.Text[TextBox.SelectionStart + i].ToString())) i++;
                    TextBox.SelectionLength = i;

                    format = new CHARFORMAT();
                    format.cbSize = Marshal.SizeOf(format);
                    format.dwMask = CFM_UNDERLINETYPE;
                    format.bUnderlineType = WaveUnderlineStyle | RedUnderlineColour;
                    SendMessage(TextBox.TextHandle, EM_SETCHARFORMAT, (IntPtr) SCF_SELECTION, ref format);  //Underlines selected text with red wavy underline
                }

                //Putting everything back to normal again
                TextBox.SelectionStart = selectionStart;
                TextBox.SelectionLength = selectionLength;

                TextBox.TextChanged += TextChanged;

                SendMessage(TextBox.TextHandle, WM_SETREDRAW, (IntPtr) 1, ref empty);
                TextBox.TextUpdate();
            }

            //private async Task ExecuteAfterTime(Action action, int timeoutInMilliseconds)
            //{
            //    await Task.Delay(timeoutInMilliseconds, tokenSource.Token);
            //    action();
            //}
        }

        //These are all the values used in the HighlightErrors sub when using SendMessage to manipulate the textbox
        private const uint CFM_UNDERLINETYPE = 0x800000;
        private const int SCF_SELECTION = 1;
        private const int EM_SETCHARFORMAT = 0x0444;
        private const int WM_SETREDRAW = 0x000b;
        private const byte WaveUnderlineStyle = 8;
        private const byte RedUnderlineColour = 0x50;

        //http://geekswithblogs.net/pvidler/archive/2003/10/15/188.aspx
        //The following struct is the structure used to send information about the rich text editing to the textbox
        //We only use dwMask and bUnderlineType i.e. set mask to only consider underline information, then provide that underline information
        //cbSize is a perculiar necessity of implementing C++ library functions in C# with variable sized structures,
        //where the size of an instantiated structure, in this case CHARFORMAT, needs to be given back to itself using Marshal.SizeOf()
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
        private static extern int SendMessage(IntPtr handle, int message, IntPtr wParam, ref IntPtr lParam);        //Importing it again for the disable drawing purpose
    }
}
