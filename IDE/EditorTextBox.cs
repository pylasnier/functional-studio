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

        public int LineCount        //textbox.Lines.Length doesn't quite give me what I want
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
            string newNumbers = "";
            for (int i = 0; i <= LineCount; i++)
            {
                newNumbers += $"{i + 1}\n";
            }

            lineNumbers.Text = newNumbers;
            
            container.Height = textBox.Font.Height * (LineCount + 2);

            if (LineCount == 0)
            {
                vScrollBar.Enabled = false;
                vScrollBar.Maximum = 0;
            }
            else
            {
                
            }
        }
    }
}
