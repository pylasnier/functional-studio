using System.Windows.Forms;

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
            this.container = new System.Windows.Forms.TableLayoutPanel();
            this.textBox = new System.Windows.Forms.RichTextBox();
            this.lineNumbers = new System.Windows.Forms.Label();
            this.vScrollBar = new System.Windows.Forms.VScrollBar();
            this.container.SuspendLayout();
            this.SuspendLayout();
            // 
            // container
            // 
            this.container.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.container.BackColor = System.Drawing.SystemColors.Window;
            this.container.ColumnCount = 2;
            this.container.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.container.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.container.Controls.Add(this.textBox, 1, 0);
            this.container.Controls.Add(this.lineNumbers, 0, 0);
            this.container.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.container.Location = new System.Drawing.Point(0, 0);
            this.container.Name = "container";
            this.container.RowCount = 1;
            this.container.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.container.Size = new System.Drawing.Size(418, 66);
            this.container.TabIndex = 0;
            // 
            // textBox
            // 
            this.textBox.AcceptsTab = true;
            this.textBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox.DetectUrls = false;
            this.textBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox.Location = new System.Drawing.Point(52, 3);
            this.textBox.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.textBox.Name = "textBox";
            this.textBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.textBox.Size = new System.Drawing.Size(366, 63);
            this.textBox.TabIndex = 0;
            this.textBox.Text = "";
            this.textBox.WordWrap = false;
            // 
            // lineNumbers
            // 
            this.lineNumbers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lineNumbers.Location = new System.Drawing.Point(0, 3);
            this.lineNumbers.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
            this.lineNumbers.Name = "lineNumbers";
            this.lineNumbers.Size = new System.Drawing.Size(49, 63);
            this.lineNumbers.TabIndex = 1;
            this.lineNumbers.Text = "1";
            this.lineNumbers.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // vScrollBar
            // 
            this.vScrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.vScrollBar.Location = new System.Drawing.Point(421, 0);
            this.vScrollBar.Name = "vScrollBar";
            this.vScrollBar.Size = new System.Drawing.Size(17, 189);
            this.vScrollBar.TabIndex = 2;
            // 
            // EditorTextBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.container);
            this.Controls.Add(this.vScrollBar);
            this.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "EditorTextBox";
            this.Size = new System.Drawing.Size(439, 189);
            this.container.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel container;
        private System.Windows.Forms.RichTextBox textBox;
        private Label lineNumbers;
        private VScrollBar vScrollBar;
    }
}
