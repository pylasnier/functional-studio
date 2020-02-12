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
        public new Font Font
        {
            get => textBox.Font;
            set
            {
                textBox.Font = value;
                lineNumbers.Font = value;
            }
        }

        public EditorTextBox()
        {
            InitializeComponent();
        }
    }
}
