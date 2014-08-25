namespace Ship_Game
{
    partial class ExceptionViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExceptionViewer));
            this.tbError = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btClip = new System.Windows.Forms.Button();
            this.btOpenBugTracker = new System.Windows.Forms.Button();
            this.btClose = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tbComment = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbError
            // 
            this.tbError.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbError.Font = new System.Drawing.Font("Lucida Console", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbError.Location = new System.Drawing.Point(0, 0);
            this.tbError.Multiline = true;
            this.tbError.Name = "tbError";
            this.tbError.ReadOnly = true;
            this.tbError.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbError.Size = new System.Drawing.Size(589, 486);
            this.tbError.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btClip);
            this.panel1.Controls.Add(this.btClose);
            this.panel1.Controls.Add(this.btOpenBugTracker);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 586);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(589, 49);
            this.panel1.TabIndex = 1;
            // 
            // btClip
            // 
            this.btClip.Location = new System.Drawing.Point(412, 9);
            this.btClip.Name = "btClip";
            this.btClip.Size = new System.Drawing.Size(146, 28);
            this.btClip.TabIndex = 1;
            this.btClip.Text = "Copy all to Clipboard";
            this.btClip.UseVisualStyleBackColor = true;
            this.btClip.Click += new System.EventHandler(this.btClip_Click);
            // 
            // btOpenBugTracker
            // 
            this.btOpenBugTracker.Location = new System.Drawing.Point(154, 9);
            this.btOpenBugTracker.Name = "btOpenBugTracker";
            this.btOpenBugTracker.Size = new System.Drawing.Size(203, 28);
            this.btOpenBugTracker.TabIndex = 2;
            this.btOpenBugTracker.Text = "Open Bugtracker in Browser";
            this.btOpenBugTracker.UseVisualStyleBackColor = true;
            this.btOpenBugTracker.Click += new System.EventHandler(this.btOpenBugTracker_Click);
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
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tbComment);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupBox1.Location = new System.Drawing.Point(0, 486);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(589, 100);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Comment the exception, if you can (what have you done?):";
            // 
            // tbComment
            // 
            this.tbComment.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbComment.Font = new System.Drawing.Font("Lucida Console", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbComment.Location = new System.Drawing.Point(3, 16);
            this.tbComment.Multiline = true;
            this.tbComment.Name = "tbComment";
            this.tbComment.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbComment.Size = new System.Drawing.Size(583, 81);
            this.tbComment.TabIndex = 0;
            // 
            // ExceptionViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(589, 635);
            this.Controls.Add(this.tbError);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimizeBox = false;
            this.Name = "ExceptionViewer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Stardrive Blackbox - - ERROR!";
            this.panel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbError;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btClip;
        private System.Windows.Forms.Button btClose;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox tbComment;
        private System.Windows.Forms.Button btOpenBugTracker;
    }
}