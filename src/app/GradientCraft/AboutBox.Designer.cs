
namespace AltCoD.GradientCraft
{
    partial class AboutBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutBox));
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            this.labelProductName = new System.Windows.Forms.Label();
            this.labelVersion = new System.Windows.Forms.Label();
            this.labelCopyright = new System.Windows.Forms.Label();
            this.labelCompanyName = new System.Windows.Forms.LinkLabel();
            this.textBoxDescription = new System.Windows.Forms.RichTextBox();
            this.paneBottom = new System.Windows.Forms.Panel();
            this.linkLicense = new System.Windows.Forms.LinkLabel();
            this.okButton = new System.Windows.Forms.Button();
            this.paneSysInfo = new System.Windows.Forms.Panel();
            this.btnNetHelp = new System.Windows.Forms.Button();
            this.txtInfoNetPath = new System.Windows.Forms.Label();
            this.txtInfoSystem = new System.Windows.Forms.Label();
            this.txtInfoDotNet = new System.Windows.Forms.Label();
            this.btnCopyEnv = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.paneBottom.SuspendLayout();
            this.paneSysInfo.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67F));
            this.tableLayoutPanel.Controls.Add(this.logoPictureBox, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.labelProductName, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.labelVersion, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.labelCopyright, 1, 2);
            this.tableLayoutPanel.Controls.Add(this.labelCompanyName, 1, 3);
            this.tableLayoutPanel.Controls.Add(this.textBoxDescription, 1, 4);
            this.tableLayoutPanel.Controls.Add(this.paneBottom, 1, 5);
            this.tableLayoutPanel.Controls.Add(this.paneSysInfo, 0, 6);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(9, 9);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 7;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 55F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(466, 357);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logoPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("logoPictureBox.Image")));
            this.logoPictureBox.Location = new System.Drawing.Point(3, 3);
            this.logoPictureBox.Name = "logoPictureBox";
            this.tableLayoutPanel.SetRowSpan(this.logoPictureBox, 6);
            this.logoPictureBox.Size = new System.Drawing.Size(147, 296);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.logoPictureBox.TabIndex = 12;
            this.logoPictureBox.TabStop = false;
            // 
            // labelProductName
            // 
            this.labelProductName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelProductName.Location = new System.Drawing.Point(159, 0);
            this.labelProductName.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
            this.labelProductName.MaximumSize = new System.Drawing.Size(0, 17);
            this.labelProductName.Name = "labelProductName";
            this.labelProductName.Size = new System.Drawing.Size(304, 17);
            this.labelProductName.TabIndex = 19;
            this.labelProductName.Text = "Product Name";
            this.labelProductName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelVersion
            // 
            this.labelVersion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelVersion.Location = new System.Drawing.Point(159, 20);
            this.labelVersion.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(304, 30);
            this.labelVersion.TabIndex = 0;
            this.labelVersion.Text = "Version";
            this.labelVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCopyright
            // 
            this.labelCopyright.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelCopyright.Location = new System.Drawing.Point(159, 50);
            this.labelCopyright.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
            this.labelCopyright.Name = "labelCopyright";
            this.labelCopyright.Size = new System.Drawing.Size(304, 40);
            this.labelCopyright.TabIndex = 21;
            this.labelCopyright.TabStop = true;
            this.labelCopyright.Text = "Copyright";
            this.labelCopyright.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCompanyName
            // 
            this.labelCompanyName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelCompanyName.Location = new System.Drawing.Point(159, 90);
            this.labelCompanyName.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
            this.labelCompanyName.MaximumSize = new System.Drawing.Size(0, 17);
            this.labelCompanyName.Name = "labelCompanyName";
            this.labelCompanyName.Size = new System.Drawing.Size(304, 17);
            this.labelCompanyName.TabIndex = 22;
            this.labelCompanyName.TabStop = true;
            this.labelCompanyName.Text = "Company Name";
            this.labelCompanyName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBoxDescription
            // 
            this.textBoxDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxDescription.Location = new System.Drawing.Point(159, 113);
            this.textBoxDescription.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.textBoxDescription.Name = "textBoxDescription";
            this.textBoxDescription.Size = new System.Drawing.Size(304, 151);
            this.textBoxDescription.TabIndex = 23;
            this.textBoxDescription.TabStop = false;
            this.textBoxDescription.Text = "Description";
            // 
            // paneBottom
            // 
            this.paneBottom.Controls.Add(this.linkLicense);
            this.paneBottom.Controls.Add(this.okButton);
            this.paneBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.paneBottom.Location = new System.Drawing.Point(156, 270);
            this.paneBottom.Name = "paneBottom";
            this.paneBottom.Size = new System.Drawing.Size(307, 29);
            this.paneBottom.TabIndex = 2;
            // 
            // linkLicense
            // 
            this.linkLicense.AutoSize = true;
            this.linkLicense.Location = new System.Drawing.Point(3, 6);
            this.linkLicense.Name = "linkLicense";
            this.linkLicense.Size = new System.Drawing.Size(40, 13);
            this.linkLicense.TabIndex = 1;
            this.linkLicense.TabStop = true;
            this.linkLicense.Text = "license";
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.okButton.Location = new System.Drawing.Point(232, 1);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 25);
            this.okButton.TabIndex = 24;
            this.okButton.Text = "&Close";
            // 
            // paneSysInfo
            // 
            this.paneSysInfo.BackColor = System.Drawing.SystemColors.Info;
            this.paneSysInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tableLayoutPanel.SetColumnSpan(this.paneSysInfo, 2);
            this.paneSysInfo.Controls.Add(this.btnNetHelp);
            this.paneSysInfo.Controls.Add(this.txtInfoNetPath);
            this.paneSysInfo.Controls.Add(this.txtInfoSystem);
            this.paneSysInfo.Controls.Add(this.txtInfoDotNet);
            this.paneSysInfo.Controls.Add(this.btnCopyEnv);
            this.paneSysInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.paneSysInfo.Location = new System.Drawing.Point(3, 305);
            this.paneSysInfo.Name = "paneSysInfo";
            this.paneSysInfo.Size = new System.Drawing.Size(460, 49);
            this.paneSysInfo.TabIndex = 24;
            // 
            // btnNetHelp
            // 
            this.btnNetHelp.BackColor = System.Drawing.Color.OrangeRed;
            this.btnNetHelp.ForeColor = System.Drawing.Color.Yellow;
            this.btnNetHelp.Location = new System.Drawing.Point(416, 17);
            this.btnNetHelp.Name = "btnNetHelp";
            this.btnNetHelp.Size = new System.Drawing.Size(42, 30);
            this.btnNetHelp.TabIndex = 4;
            this.btnNetHelp.Text = "!";
            this.btnNetHelp.UseVisualStyleBackColor = false;
            // 
            // txtInfoNetPath
            // 
            this.txtInfoNetPath.AutoSize = true;
            this.txtInfoNetPath.Location = new System.Drawing.Point(45, 32);
            this.txtInfoNetPath.Name = "txtInfoNetPath";
            this.txtInfoNetPath.Size = new System.Drawing.Size(72, 13);
            this.txtInfoNetPath.TabIndex = 3;
            this.txtInfoNetPath.Text = "{dot net path}";
            // 
            // txtInfoSystem
            // 
            this.txtInfoSystem.AutoSize = true;
            this.txtInfoSystem.Location = new System.Drawing.Point(45, 17);
            this.txtInfoSystem.Name = "txtInfoSystem";
            this.txtInfoSystem.Size = new System.Drawing.Size(67, 13);
            this.txtInfoSystem.TabIndex = 2;
            this.txtInfoSystem.Text = "{system info}";
            // 
            // txtInfoDotNet
            // 
            this.txtInfoDotNet.AutoSize = true;
            this.txtInfoDotNet.Location = new System.Drawing.Point(45, 2);
            this.txtInfoDotNet.Name = "txtInfoDotNet";
            this.txtInfoDotNet.Size = new System.Drawing.Size(68, 13);
            this.txtInfoDotNet.TabIndex = 1;
            this.txtInfoDotNet.Text = "{dot net info}";
            // 
            // btnCopyEnv
            // 
            this.btnCopyEnv.Location = new System.Drawing.Point(3, 12);
            this.btnCopyEnv.Name = "btnCopyEnv";
            this.btnCopyEnv.Size = new System.Drawing.Size(39, 23);
            this.btnCopyEnv.TabIndex = 0;
            this.btnCopyEnv.Text = "Copy";
            this.toolTip.SetToolTip(this.btnCopyEnv, "Copy the environment info into the Clipboard");
            this.btnCopyEnv.UseVisualStyleBackColor = true;
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.InitialDelay = 0;
            this.toolTip.ReshowDelay = 0;
            this.toolTip.UseAnimation = false;
            this.toolTip.UseFading = false;
            // 
            // AboutBox
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 375);
            this.Controls.Add(this.tableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutBox";
            this.Padding = new System.Windows.Forms.Padding(9);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "AboutBox1";
            this.tableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.paneBottom.ResumeLayout(false);
            this.paneBottom.PerformLayout();
            this.paneSysInfo.ResumeLayout(false);
            this.paneSysInfo.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label labelProductName;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.Label labelCopyright;
        private System.Windows.Forms.LinkLabel labelCompanyName;
        private System.Windows.Forms.RichTextBox textBoxDescription;
        private System.Windows.Forms.Panel paneBottom;
        private System.Windows.Forms.LinkLabel linkLicense;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Panel paneSysInfo;
        private System.Windows.Forms.Button btnCopyEnv;
        private System.Windows.Forms.Label txtInfoSystem;
        private System.Windows.Forms.Label txtInfoDotNet;
        private System.Windows.Forms.Label txtInfoNetPath;
        private System.Windows.Forms.Button btnNetHelp;
    }
}
