using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IDE
{
    public partial class EditorTextBox : UserControl
    {
        private int lineCount = 1;

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

        public new event EventHandler TextChanged
        {
            add => textBox.TextChanged += value;
            remove => textBox.TextChanged -= value;
        }

        public EditorTextBox()
        {
            InitializeComponent();
            TextChanged += OnTextChanged;
            UpdateLineNumbers();
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            UpdateLineNumbers();
        }

        public void UpdateLineNumbers()
        {
            if (textBox.Lines.Length == 0)
            {
                lineCount = 1;
                lineNumbers.Text = "1\n";

                lineNumbers.SelectAll();
                lineNumbers.SelectionAlignment = HorizontalAlignment.Right;
                lineNumbers.DeselectAll();
            }
            else if (textBox.Lines.Length != lineCount)
            {
                lineCount = textBox.Lines.Length;
                lineNumbers.Text = "";
                for (int i = 1; i <= lineCount; i++)
                {
                    lineNumbers.Text += $"{i}\n";
                }

                lineNumbers.SelectAll();
                lineNumbers.SelectionAlignment = HorizontalAlignment.Right;
                lineNumbers.DeselectAll();
            }
        }
    }
}
