namespace IDE
{
    partial class EditorTextBox
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.textBox = new System.Windows.Forms.RichTextBox();
            this.lineNumbers = new System.Windows.Forms.RichTextBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.textBox, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.lineNumbers, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(106, 25);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // textBox
            // 
            this.textBox.AcceptsTab = true;
            this.textBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox.DetectUrls = false;
            this.textBox.Location = new System.Drawing.Point(50, 0);
            this.textBox.Margin = new System.Windows.Forms.Padding(0);
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(56, 25);
            this.textBox.TabIndex = 0;
            this.textBox.Text = "";
            this.textBox.WordWrap = false;
            // 
            // lineNumbers
            // 
            this.lineNumbers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lineNumbers.Location = new System.Drawing.Point(0, 0);
            this.lineNumbers.Margin = new System.Windows.Forms.Padding(0);
            this.lineNumbers.Name = "lineNumbers";
            this.lineNumbers.ReadOnly = true;
            this.lineNumbers.Size = new System.Drawing.Size(50, 25);
            this.lineNumbers.TabIndex = 1;
            this.lineNumbers.Text = "";
            this.lineNumbers.WordWrap = false;
            // 
            // EditorTextBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "EditorTextBox";
            this.Size = new System.Drawing.Size(106, 25);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.RichTextBox textBox;
        private System.Windows.Forms.RichTextBox lineNumbers;
    }
}
