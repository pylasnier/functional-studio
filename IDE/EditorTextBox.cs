using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;

namespace IDE
{
    public partial class EditorTextBox : UserControl
    {
        //Most of these overloads are to make interaction with the actual textBox component of this control as close as possible to a real textbox
        //They also interact with the lineNumbers component in places to make properties consistent between the two controls
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

        //This is used in particular for when code is running, where the editor needs to be disabled
        //I prefered instead changing the ReadOnly property of the textbox rather than Enabled, as the colour is nicer,
        //and text can still be selected without allowing changes.
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
        //End of overloads
        
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

                return value;       //Resulting value is as many lines up to the end position, minus 1 if the last line doesn't contain any text i.e. is only a newline
            }
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
            //These just refocus the editor window by scrolling up/down to where the caret is
            //The distinction between up or down is so it only scrolls as far as it has to, which depends on direction, or if not at all
            if (vScrollBar.Enabled && textBox.GetLineFromCharIndex(SelectionStart) - vScrollBar.Value / (LineCount + Height / Font.Height) > Height / Font.Height)
            {
                vScrollBar.Value = (textBox.GetLineFromCharIndex(SelectionStart) - Height / Font.Height) * (LineCount + Height / Font.Height);
                ScrollTextBox();
            }
            else if (vScrollBar.Enabled && textBox.GetLineFromCharIndex(SelectionStart) - vScrollBar.Value / (LineCount + Height / Font.Height) < 0)
            {
                vScrollBar.Value = textBox.GetLineFromCharIndex(SelectionStart) * (LineCount + Height / Font.Height);
                ScrollTextBox();
            }
        }

        //Effectively passes on mousewheel events from the textbox to the scrollbar
        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            MethodInfo methodInfo = typeof(VScrollBar).GetMethod("OnMouseWheel", BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo.Invoke(vScrollBar, new object[] { e });
        }

        //Makes pageup and pagedown scroll, and forces textbox to update when caret is moved using arrow keys
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
            else if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                OnTextChanged(sender, EventArgs.Empty);
            }
        }

        //The strange combinations of divisions is because the integer font height and the height of many lines of a give font don't line up
        //and so the actual height must be used instead. This is on top of the scrollbar value having a multiplier which makes the thumb size appropriate
        private void ScrollTextBox()
        {
            container.Location = new Point(0,
                (int)(-vScrollBar.Value / (LineCount + Height / Font.Height) / Math.Max(1f, LineCount - 1f) * Math.Max(Font.Height, textBox.GetPositionFromCharIndex(Text.Length - 1).Y)));
        }

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
                //A large consideration for the scrollbar was needed, as the ratio of the size of the thumb to the size of the scrollbar is actually given
                //by the ratio of the LargeChange property to the difference between the Maximum and Minimum properties. Also a strange behaviour of the
                //scrollbar is that the value of it can never make it to Maximum, only to Maximum - LargeChange, similar to how it visually behaves.
                //The following complicated adjustments to these properties are to compensate for these behaviours, and to achieve a thumb whose size ratio
                //matches the size ratio of the visible window to the whole text file

                int scrollableLines = LineCount - (Text.Last() == '\n' ? 0 : 1); //Only so that it doesn't scroll to the last line unless there was a newline character

                vScrollBar.Maximum = (scrollableLines + Height / Font.Height) * (LineCount + Height / Font.Height);
                vScrollBar.SmallChange = Math.Min(3, scrollableLines) * (LineCount + Height / Font.Height);
                vScrollBar.LargeChange = Height / Font.Height * (LineCount + Height / Font.Height);
                vScrollBar.Enabled = true;
            }
        }
    }
}
