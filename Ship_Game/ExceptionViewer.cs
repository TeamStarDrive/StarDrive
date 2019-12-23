using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Ship_Game
{
    public partial class ExceptionViewer : Form
    {
        public ExceptionViewer()
        {
            InitializeComponent();
        }

        public string Description
        {
            set => descrLabel.Text = value;
        }

        public string Error
        {
            get => tbError.Text;
            set
            {
                tbError.Text = value.Replace("\n", "\r\n");
                tbError.Select(0, 0);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate(); // give focus to the dialog
        }

        [STAThread]
        void btClip_Click(object sender, EventArgs e)
        {
            string all = tbError.Text + "\n\nUser Comment: " + tbComment.Text;
            Clipboard.SetText(all);
        }

        void btClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        void btOpenBugTracker_Click(object sender, EventArgs e)
        {
            const string discordTestingAndIssuesInvite = "https://discord.gg/9qCCgkR";
            Process.Start(discordTestingAndIssuesInvite);
        }

        public static void ShowExceptionDialog(string dialogText, bool autoReport)
        {
            var view = new ExceptionViewer();

            if (autoReport)
            {
                view.Description =
                    "This error was submitted automatically to our exception tracking system. \r\n" +
                    "If this error keeps reoccurring, you can add comments and create a new issue on BitBucket.";
            }
            else
            {
                view.Description =
                    "Automatic error reporting is disabled. \r\n" +
                    "If this error keep reoccurring, you can add comments and create a new issue on BitBucket.";
            }

            view.Error = dialogText;
            view.ShowDialog();
        }
    }
}