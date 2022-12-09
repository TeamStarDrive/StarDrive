using System.ComponentModel;
using System.Windows.Forms;

namespace Ship_Game
{
    partial class ExceptionViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            components?.Dispose(ref components);
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExceptionViewer));
            this.tbError = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btClip = new System.Windows.Forms.Button();
            this.btClose = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.descrLabel = new System.Windows.Forms.Label();
            this.discordHelpline = new System.Windows.Forms.LinkLabel();
            this.githubIssues = new System.Windows.Forms.LinkLabel();
            this.panel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbError
            // 
            this.tbError.Font = new System.Drawing.Font("Consolas", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbError.Location = new System.Drawing.Point(3, 79);
            this.tbError.Multiline = true;
            this.tbError.Name = "tbError";
            this.tbError.ReadOnly = true;
            this.tbError.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbError.Size = new System.Drawing.Size(870, 562);
            this.tbError.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btClip);
            this.panel1.Controls.Add(this.btClose);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 643);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(876, 49);
            this.panel1.TabIndex = 1;
            // 
            // btClip
            // 
            this.btClip.Location = new System.Drawing.Point(123, 9);
            this.btClip.Name = "btClip";
            this.btClip.Size = new System.Drawing.Size(156, 28);
            this.btClip.TabIndex = 0;
            this.btClip.Text = "Copy Error to Clipboard";
            this.btClip.Click += new System.EventHandler(this.btClip_Click_1);
            // 
            // btClose
            // 
            this.btClose.Location = new System.Drawing.Point(12, 9);
            this.btClose.Name = "btClose";
            this.btClose.Size = new System.Drawing.Size(105, 28);
            this.btClose.TabIndex = 0;
            this.btClose.Text = "Close";
            this.btClose.UseVisualStyleBackColor = true;
            this.btClose.Click += new System.EventHandler(this.btClose_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.descrLabel);
            this.flowLayoutPanel1.Controls.Add(this.discordHelpline);
            this.flowLayoutPanel1.Controls.Add(this.githubIssues);
            this.flowLayoutPanel1.Controls.Add(this.tbError);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(876, 643);
            this.flowLayoutPanel1.TabIndex = 4;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // descrLabel
            // 
            this.descrLabel.AutoSize = true;
            this.descrLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(186)));
            this.descrLabel.Location = new System.Drawing.Point(3, 0);
            this.descrLabel.Name = "descrLabel";
            this.descrLabel.Padding = new System.Windows.Forms.Padding(6, 12, 6, 6);
            this.descrLabel.Size = new System.Drawing.Size(95, 36);
            this.descrLabel.TabIndex = 5;
            this.descrLabel.Text = "Description";
            // 
            // discordHelpline
            // 
            this.discordHelpline.AutoSize = true;
            this.discordHelpline.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.discordHelpline.Location = new System.Drawing.Point(3, 36);
            this.discordHelpline.Name = "discordHelpline";
            this.discordHelpline.Padding = new System.Windows.Forms.Padding(6, 0, 0, 2);
            this.discordHelpline.Size = new System.Drawing.Size(162, 20);
            this.discordHelpline.TabIndex = 6;
            this.discordHelpline.TabStop = true;
            this.discordHelpline.Text = "Open Discord Helpline";
            this.discordHelpline.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.discordHelpline_LinkClicked);
            // 
            // githubIssues
            // 
            this.githubIssues.AutoSize = true;
            this.githubIssues.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.githubIssues.Location = new System.Drawing.Point(3, 56);
            this.githubIssues.Name = "githubIssues";
            this.githubIssues.Padding = new System.Windows.Forms.Padding(6, 0, 0, 2);
            this.githubIssues.Size = new System.Drawing.Size(147, 20);
            this.githubIssues.TabIndex = 7;
            this.githubIssues.TabStop = true;
            this.githubIssues.Text = "Open GitHub Issues";
            this.githubIssues.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.githubIssues_LinkClicked);
            // 
            // ExceptionViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(876, 692);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExceptionViewer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Stardrive Blackbox - - ERROR!";
            this.panel1.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private TextBox tbError;
        private Panel panel1;
        private Button btClip;
        private Button btClose;
        private FlowLayoutPanel flowLayoutPanel1;
        private Label descrLabel;
        private LinkLabel discordHelpline;
        private LinkLabel githubIssues;
    }
}