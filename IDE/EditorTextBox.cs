using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IDE
{
    public partial class EditorTextBox : UserControl
    {
        private int actualSmallChange;

        public override Font Font
        {
            get => textBox.Font;
            set
            {
                textBox.Font = value;
                lineNumbers.Font = value;
            }
        }

        public new string Text { get; set; }

        public string LineNumbersText { get; set; }

        public int SelectionStart { get; set; }

        public int SelectionLength { get; set; }

        public int EditorLines => textBox.Height / textBox.Font.Height;

        public int TotalLines => Text.GetLines().Length;

        public int ScrollbarScalar => EditorLines + TotalLines;

        public new event EventHandler TextChanged
        {
            add => textBox.TextChanged += value;
            remove => textBox.TextChanged -= value;
        }

        public EditorTextBox()
        {
            InitializeComponent();
            TextChanged += OnTextChanged;
            vScrollBar.Scroll += OnScroll;
            UpdateText();
        }

        private void OnTextChanged(object sender, EventArgs e) => UpdateText();

        //private void OnScroll(object sender, EventArgs e)
        //{
        //    //                                         1 = SbVert scroll bar type enum (scroll bar vertical)
        //    int pos = GetScrollPos(textBox.Handle, 1);
        //    //                   4 = SB_THUMBPOSITION scroll bar command enum (scroll bar thumb position)
        //    uint wParam = ((uint) 4 | ((uint) pos << 16));
        //    //                                     0x0115 = WM_VSCROLL message enum
        //    SendMessage(lineNumbers.Handle, 0x0115, new IntPtr(wParam), new IntPtr(0));
        //}

        private void OnScroll(object sender, ScrollEventArgs e) => EditorScroll();

        private const int WM_VSCROLL = 0x115;

        //private void EditorScroll(int oldValue, int newValue, ScrollOrientation scrollOrientation)
        //{
        //    int currentSelectionStart = textBox.SelectionStart;
        //    int currentSelectionLength = textBox.SelectionLength;
        //    textBox.SelectionLength = 0;
        //    bool caretBelow = newValue > oldValue;

        //    switch (scrollOrientation)
        //    {
        //        case ScrollOrientation.VerticalScroll:
        //            SendMessage(textBox.Handle, WM_VSCROLL, (IntPtr) )
        //            break;

        //        case ScrollOrientation.HorizontalScroll:
        //            break;
        //    }

        //    textBox.ScrollToCaret();
        //    //textBox.SelectionStart = currentSelectionStart;
        //    //textBox.SelectionLength = currentSelectionLength;
        //}

        //private void EditorScroll(int oldValue, int newValue, ScrollOrientation scrollOrientation)
        //{
        //    var scrollInfo = new SCROLLINFO();
        //    scrollInfo.cbSize = Marshal.SizeOf(scrollInfo);
        //    scrollInfo.fMask = (uint) ScrollMaskInfo.SIF_MYMASK;
        //    scrollInfo.nMin = vScrollBar.Minimum;
        //    scrollInfo.nMax = vScrollBar.Maximum;
        //    scrollInfo.nPos = newValue;

        //    SetScrollInfo(textBox.Handle, WM_VSCROLL, ref scrollInfo, false);
        //}


        //public void UpdateLineNumbers()
        //{
        //    if (textBox.Lines.Length != lineCount)
        //    {
        //        lineCount = 1;
        //        lineNumbers.Text = "1\n";

        //        if (textBox.Lines.Length > 1)
        //        {
        //            lineCount = textBox.Lines.Length;
        //            for (int i = 2; i <= lineCount; i++)
        //            {
        //                lineNumbers.Text += $"{i}\n";
        //            }
        //        }

        //        lineNumbers.SelectAll();
        //        lineNumbers.SelectionAlignment = HorizontalAlignment.Right;
        //        lineNumbers.DeselectAll();
                
        //        vScrollBar.Maximum = (EditorLines + lineCount - 1) * (lineCount - 1);
        //        vScrollBar.Minimum = 0;
        //        vScrollBar.SmallChange = 3 * (EditorLines + lineCount - 1);
        //        if (lineCount == 1)
        //        {
        //            vScrollBar.Enabled = false;
        //        }
        //        else
        //        {
        //            vScrollBar.Enabled = true;
        //            vScrollBar.LargeChange = (lineCount - 1) * EditorLines;
        //        }
        //    }
        //}

        public void ResetText()
        {
            UpdateText();
            vScrollBar.Value = 0;
            EditorScroll();
        }

        private void UpdateText()
        {
            string newLineNumbers = "";
            for (int i = 0; i <= TotalLines; i++)
            {
                newLineNumbers += $"{i + 1}\n";
            }

            LineNumbersText = newLineNumbers;

            if (TotalLines == 0)
            {
                vScrollBar.Maximum = 0;
                vScrollBar.Minimum = 0;
                vScrollBar.Enabled = false;
                actualSmallChange = 0;
            }
            else
            {
                vScrollBar.Maximum = ScrollbarScalar * (TotalLines + EditorLines);
                vScrollBar.Minimum = 0;
                actualSmallChange = Math.Min(3, TotalLines);
                vScrollBar.SmallChange = actualSmallChange * ScrollbarScalar;
                vScrollBar.LargeChange = EditorLines * (TotalLines + EditorLines);
                vScrollBar.Enabled = true;
            }

            string changedText = "";

            SelectionStart = textBox.SelectionStart;
            SelectionLength = textBox.SelectionLength;
        }

        private void EditorScroll()
        {
            string[] lines = new string[EditorLines];
            Array.Copy(Text.GetLines(), vScrollBar.Value / ScrollbarScalar, lines, 0,
                Math.Min(EditorLines, Text.GetLines().Length - vScrollBar.Value / ScrollbarScalar));

            string newText = "";
            foreach (string line in lines)
            {
                newText += line + "\n";
            }

            textBox.Text = newText;

            lines = new string[EditorLines];
            Array.Copy(LineNumbersText.GetLines(), vScrollBar.Value / ScrollbarScalar, lines, 0,
                Math.Min(EditorLines, LineNumbersText.GetLines().Length - vScrollBar.Value / ScrollbarScalar));

            newText = "";
            foreach (string line in lines)
            {
                newText += line + "\n";
            }

            lineNumbers.Text = newText;
        }

        [DllImport("User32.dll")]
        public static extern int GetScrollPos(IntPtr hWnd, int nBar);

        [DllImport("User32.dll")]
        public static extern int SetScrollInfo(IntPtr handle, int scrollType, ref SCROLLINFO scrollInfo, bool redrawScrollBar);

        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct SCROLLINFO
        {
            public int cbSize;      //Size of the SCROLLINFO struct (must be given when passed)
            public uint fMask;      //Specifies which parameters to set
            public int nMin;        //Scrollbar minimum value
            public int nMax;        //Scrollbar maximum value
            public uint nPage;      //Page size in device units (won't be using this)
            public int nPos;        //Scrollbar scroll position
            public int nTrackPos;   //Position while track bar being moved (won't be using this)
        }

        enum ScrollMaskInfo : uint
        {
            SIF_RANGE = 0x1,
            SIF_PAGE = 0x2,
            SIF_POS = 0x4,
            SIF_DISABLENOSCROLL = 0x8,
            SIF_TRACKPOS = 0x10,
            SIF_ALL = (SIF_RANGE | SIF_PAGE | SIF_POS | SIF_TRACKPOS),
            SIF_MYMASK = (SIF_RANGE | SIF_POS)
        }
    }

    public static class LinesExtensionMethod
    {
        public static string[] GetLines(this string text)
        {
            string[] returnValue;

            if (text == null || text == "")
            {
                returnValue = new string[0];
            }

            else
            {
                List<string> lines = new List<string>();
                string line = "";

                foreach (char character in text)
                {
                    if (character == '\n')
                    {
                        lines.Add(line);
                        line = "";
                    }
                    else
                    {
                        line += character;
                    }
                }

                if (line != "")
                {
                    lines.Add(line);
                }

                returnValue = lines.ToArray();
            }

            return returnValue;
        }
    }
}