
namespace AltCoD.GradientCraft
{
    partial class SourceCodeWnd
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnCopy = new System.Windows.Forms.Button();
            this.editSource = new System.Windows.Forms.RichTextBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnInfo = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.btnExplain = new System.Windows.Forms.Button();
            this.radCopyText = new System.Windows.Forms.RadioButton();
            this.radCopyRTF = new System.Windows.Forms.RadioButton();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnClose
            // 
            this.btnClose.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnClose.AutoSize = true;
            this.btnClose.Location = new System.Drawing.Point(264, 362);
            this.btnClose.Margin = new System.Windows.Forms.Padding(3, 10, 3, 3);
            this.btnClose.MinimumSize = new System.Drawing.Size(35, 25);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(65, 35);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // btnCopy
            // 
            this.btnCopy.Location = new System.Drawing.Point(3, 78);
            this.btnCopy.Margin = new System.Windows.Forms.Padding(3, 30, 3, 3);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(50, 30);
            this.btnCopy.TabIndex = 1;
            this.btnCopy.Text = "Copy";
            this.toolTip.SetToolTip(this.btnCopy, "Copy the source code sample to the Clipboard (text or rtf content)");
            this.btnCopy.UseVisualStyleBackColor = true;
            // 
            // editSource
            // 
            this.editSource.BackColor = System.Drawing.SystemColors.Window;
            this.editSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editSource.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.editSource.Location = new System.Drawing.Point(6, 6);
            this.editSource.Name = "editSource";
            this.editSource.ReadOnly = true;
            this.editSource.Size = new System.Drawing.Size(582, 343);
            this.editSource.TabIndex = 2;
            this.editSource.Text = "";
            this.editSource.WordWrap = false;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel1.Controls.Add(this.editSource, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnClose, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(3);
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(654, 405);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnExplain);
            this.flowLayoutPanel1.Controls.Add(this.btnCopy);
            this.flowLayoutPanel1.Controls.Add(this.radCopyText);
            this.flowLayoutPanel1.Controls.Add(this.radCopyRTF);
            this.flowLayoutPanel1.Controls.Add(this.btnInfo);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(594, 6);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(54, 242);
            this.flowLayoutPanel1.TabIndex = 3;
            // 
            // btnInfo
            // 
            this.btnInfo.Location = new System.Drawing.Point(3, 187);
            this.btnInfo.Margin = new System.Windows.Forms.Padding(3, 30, 3, 3);
            this.btnInfo.Name = "btnInfo";
            this.btnInfo.Size = new System.Drawing.Size(50, 30);
            this.btnInfo.TabIndex = 2;
            this.btnInfo.Text = "?";
            this.toolTip.SetToolTip(this.btnInfo, "What is this fucking source code ?");
            this.btnInfo.UseVisualStyleBackColor = true;
            // 
            // btnExplain
            // 
            this.btnExplain.Location = new System.Drawing.Point(3, 15);
            this.btnExplain.Margin = new System.Windows.Forms.Padding(3, 15, 3, 3);
            this.btnExplain.Name = "btnExplain";
            this.btnExplain.Size = new System.Drawing.Size(50, 30);
            this.btnExplain.TabIndex = 3;
            this.btnExplain.Text = "Explain";
            this.toolTip.SetToolTip(this.btnExplain, "What may this scrap do ?");
            this.btnExplain.UseVisualStyleBackColor = true;
            // 
            // radCopyText
            // 
            this.radCopyText.AutoSize = true;
            this.radCopyText.Location = new System.Drawing.Point(3, 114);
            this.radCopyText.Name = "radCopyText";
            this.radCopyText.Size = new System.Drawing.Size(46, 17);
            this.radCopyText.TabIndex = 4;
            this.radCopyText.TabStop = true;
            this.radCopyText.Text = "Text";
            this.toolTip.SetToolTip(this.radCopyText, "Only the raw text content of the source code will be copied to clipboard");
            this.radCopyText.UseVisualStyleBackColor = true;
            // 
            // radCopyRTF
            // 
            this.radCopyRTF.AutoSize = true;
            this.radCopyRTF.Location = new System.Drawing.Point(3, 137);
            this.radCopyRTF.Name = "radCopyRTF";
            this.radCopyRTF.Size = new System.Drawing.Size(46, 17);
            this.radCopyRTF.TabIndex = 5;
            this.radCopyRTF.TabStop = true;
            this.radCopyRTF.Text = "RTF";
            this.toolTip.SetToolTip(this.radCopyRTF, "The content will be copied with the RTF code and tags");
            this.radCopyRTF.UseVisualStyleBackColor = true;
            // 
            // SourceCodeWnd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(654, 405);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "SourceCodeWnd";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Source code sample";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.RichTextBox editSource;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button btnInfo;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Button btnExplain;
        private System.Windows.Forms.RadioButton radCopyText;
        private System.Windows.Forms.RadioButton radCopyRTF;
    }
}