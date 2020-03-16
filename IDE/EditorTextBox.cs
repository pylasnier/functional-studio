using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

namespace IDE
{
    public partial class EditorTextBox : UserControl
    {
        public override Font Font
        {
            get => textBox.Font;
            set
            {
                textBox.Font = value;
                lineNumbers.Font = value;
            }
        }

        public new string Text
        {
            get => textBox.Text;
            set => textBox.Text = value;
        }

        public new bool Enabled
        {
            get => !textBox.ReadOnly;
            set => textBox.ReadOnly = !value;
        }

        public int SelectionStart
        {
            get => textBox.SelectionStart;
            set => textBox.SelectionStart = value;
        }

        public int SelectionLength
        {
            get => textBox.SelectionLength;
            set => textBox.SelectionLength = value;
        }

        public Font SelectionFont
        {
            get => textBox.SelectionFont;
            set => textBox.SelectionFont = value;
        }

        public void SelectAll() => textBox.SelectAll();

        public void TextUpdate() => textBox.Update();

        public IntPtr TextHandle => textBox.Handle;

        public int LineCount        //textbox.Lines.Length doesn't quite give me what I want for line numbers
        {
            get
            {
                int value = 1;
                bool lastLineEmpty = true;
                for (int i = 0; i < Text.Length; i++)
                {
                    if (Text[i] == '\n')
                    {
                        value++;
                        lastLineEmpty = true;
                    }
                    else if (lastLineEmpty)
                    {
                        lastLineEmpty = false;
                    }
                }

                if (lastLineEmpty)
                {
                    value--;
                }

                return value;
            }
        }

        public new event EventHandler TextChanged
        {
            add => textBox.TextChanged += value;
            remove => textBox.TextChanged -= value;
        }

        public new event MouseEventHandler MouseWheel
        {
            add
            {
                textBox.MouseWheel += value;
                lineNumbers.MouseWheel += value;
            }
            remove
            {
                textBox.MouseWheel -= value;
                lineNumbers.MouseWheel -= value;
            }
        }

        public new event KeyEventHandler KeyPress
        {
            add => textBox.KeyDown += value;
            remove => textBox.KeyDown -= value;
        }

        public EditorTextBox()
        {
            InitializeComponent();
            TextChanged += OnTextChanged;
            MouseWheel += OnMouseWheel;
            KeyPress += OnKeyDown;
            vScrollBar.Scroll += (sender, e) =>{ ScrollTextBox(); };
            UpdateLineNumbers();
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            UpdateLineNumbers();
            if (vScrollBar.Enabled && textBox.GetLineFromCharIndex(SelectionStart) - vScrollBar.Value / (LineCount + Height / Font.Height) > Height / Font.Height)
            {
                vScrollBar.Value = (textBox.GetLineFromCharIndex(SelectionStart) + Height / Font.Height) * (LineCount + Height / Font.Height);
            }
            else if (vScrollBar.Enabled && textBox.GetLineFromCharIndex(SelectionStart) - vScrollBar.Value / (LineCount + Height / Font.Height) < 0)
            {
                vScrollBar.Value = textBox.GetLineFromCharIndex(SelectionStart) * (LineCount + Height / Font.Height);
            }
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            MethodInfo methodInfo = typeof(VScrollBar).GetMethod("OnMouseWheel", BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo.Invoke(vScrollBar, new object[] { e });
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.PageUp)
            {
                vScrollBar.Value = Math.Max(vScrollBar.Minimum, vScrollBar.Value - vScrollBar.LargeChange);
                ScrollTextBox();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                vScrollBar.Value = Math.Min(vScrollBar.Maximum, vScrollBar.Value + vScrollBar.LargeChange);
                ScrollTextBox();
                e.Handled = true;
            }
        }

        private void ScrollTextBox()
        {
            container.Location = new Point(0,
                (int)(-vScrollBar.Value / (LineCount + Height / Font.Height) / Math.Max(1f, LineCount - 1f) * Math.Max(Font.Height, textBox.GetPositionFromCharIndex(Text.Length - 1).Y)));
        }

        //private void OnScroll(object sender, ScrollEventArgs e)
        //{
        //    panel.VerticalScroll.Value = e.NewValue;
        //}

        public void UpdateLineNumbers()
        {
            string newNumbers = "";
            for (int i = 0; i <= LineCount; i++)
            {
                newNumbers += $"{i + 1}\n";
            }

            lineNumbers.Text = newNumbers;

            container.Height = Font.Height * LineCount + Height;

            if (textBox.Lines.Length - 1 <= 0)
            {
                vScrollBar.Enabled = false;
                vScrollBar.Maximum = 0;
            }
            else
            {
                int scrollableLines = LineCount - (Text.Last() == '\n' ? 0 : 1); //Only so that it doesn't scroll to the last line unless there was a newline character

                vScrollBar.Maximum = (scrollableLines + Height / Font.Height) * (LineCount + Height / Font.Height);
                vScrollBar.SmallChange = Math.Min(3, scrollableLines) * (LineCount + Height / Font.Height);
                vScrollBar.LargeChange = Height / Font.Height * (LineCount + Height / Font.Height);
                vScrollBar.Enabled = true;
            }
        }
    }
}
