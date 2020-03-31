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

        private int ScrollMax { get => vScrollBar.Maximum - vScrollBar.LargeChange + 1; }
        private int ScrollMin { get => vScrollBar.Minimum; }

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
            ScrollToLine(textBox.GetLineFromCharIndex(SelectionStart));
        }

        //Effectively passes on mousewheel events from the textbox to the scrollbar
        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            if (vScrollBar.Enabled)
            {
                MethodInfo methodInfo = typeof(VScrollBar).GetMethod("OnMouseWheel", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo.Invoke(vScrollBar, new object[] { e });
            }
        }

        //Makes pageup and pagedown scroll, and forces textbox to update when caret is moved using arrow keys
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.PageUp)
            {
                vScrollBar.Value = Math.Max(ScrollMin, vScrollBar.Value - vScrollBar.LargeChange);
                ScrollTextBox();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                vScrollBar.Value = Math.Min(ScrollMax, vScrollBar.Value + vScrollBar.LargeChange);
                ScrollTextBox();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                int line;

                switch (e.KeyCode)
                {
                    case Keys.Up:
                        line = textBox.GetLineFromCharIndex(SelectionStart) - 1;
                        break;

                    case Keys.Down:
                        line = textBox.GetLineFromCharIndex(SelectionStart) + 1;
                        break;

                    case Keys.Left:
                        if (textBox.SelectionStart > 0)
                        {
                            line = textBox.GetLineFromCharIndex(SelectionStart - 1);
                        }
                        else
                        {
                            line = textBox.GetLineFromCharIndex(SelectionStart);
                        }
                        break;

                    case Keys.Right:
                        if (textBox.SelectionStart < textBox.TextLength - 1)
                        {
                            line = textBox.GetLineFromCharIndex(SelectionStart + 1);
                        }
                        else
                        {
                            line = textBox.GetLineFromCharIndex(SelectionStart);
                        }
                        break;

                    default:    //Never reached
                        line = textBox.GetLineFromCharIndex(SelectionStart);
                        break;
                }

                ScrollToLine(Math.Min(ScrollMax, Math.Max(ScrollMin, line)));
            }
        }

        private void ScrollTextBox()
        {
            container.Location = new Point(0, -vScrollBar.Value * (container.Height - Height) / ScrollMax);
        }

        private void ScrollToLine(int line)
        {
            //These just refocus the editor window by scrolling up/down to where the caret is
            //The distinction between up or down is so it only scrolls as far as it has to, which depends on direction, or if not at all
            if (line - vScrollBar.Value >= Height / Font.Height)
            {
                vScrollBar.Value = line - Height / Font.Height;
                ScrollTextBox();
            }
            else if (line - vScrollBar.Value <= 0)
            {
                vScrollBar.Value = line;
                ScrollTextBox();
            }
        }

        public void UpdateLineNumbers()
        {
            string newNumbers = "";
            for (int i = 0; i < textBox.Lines.Length; i++)
            {
                newNumbers += $"{i + 1}\n";
            }
            if (Text.Length == 0 || Text.Last() != '\n')
            {
                newNumbers += $"{textBox.Lines.Length + 1}";
            }

            lineNumbers.Text = newNumbers;

            if (textBox.Lines.Length <= 1)
            {
                container.Height = Height;

                vScrollBar.Enabled = false;
            }
            else
            {
                container.Height = textBox.GetPositionFromCharIndex(Text.Length - (Text.Last() == '\n' ? 0 : 1)).Y + Height;

                //Value uses line count minus two, because we only want to be able to scroll past all but one of the lines (so the last line cannot be scrolled past)
                //Therefore that makes the theoretical maximum the line count minus 1. However, the actual maximum scrollable value is Maximum - LargeChange + 1,
                //so Maximum must be set to actual maximum plus LargeChange minus one, therefore the line count minus 2 plus LargeChange, set afterwards
                vScrollBar.Maximum = textBox.Lines.Length - 2 + Height / Font.Height;
                vScrollBar.SmallChange = Math.Min(3, textBox.Lines.Length);
                vScrollBar.LargeChange = Height / Font.Height;
                vScrollBar.Enabled = true;
            }
        }
    }
}
