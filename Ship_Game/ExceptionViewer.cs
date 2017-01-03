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
            set
            {
                descrLabel.Text = value.Replace("\n", "\r\n");
            }
        }

        public string Error
        {
            set
            {
                tbError.Text = value.Replace("\n", "\r\n");
                tbError.Select(0, 0);
            }
        }

        private void btClip_Click(object sender, EventArgs e)
        {
            string all = tbError.Text + "\n\nUser Comment: " + tbComment.Text;
            Clipboard.SetText(all);
        }

        private void btClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btOpenBugTracker_Click(object sender, EventArgs e)
        {
            if(!ExceptionTracker.Kudos)
            Process.Start(ExceptionTracker.BugtrackerURL);
            else
            {
                ExceptionTracker.Kudos = false;
                Process.Start(ExceptionTracker.KudosURL);
            }
        }

        public static void ShowExceptionDialog(Exception ex)
        {
            var view = new ExceptionViewer();

            if (GlobalStats.AutoErrorReport)
            {
                view.Description =
                    "This error was submitted automatically to our exception tracking system. \r\n" +
                    "If this error keeps reocurring, you can add comments and create a new issue on BitBucket.";
            }
            else
            {
                view.Description = 
                    "Automatic error reporting is disabled. \r\n" +
                    "If this error keep reocurring, you can add comments and create a new issue on BitBucket.";
            }

            view.Error = Log.CurryExceptionMessage(ex) + "\r\n" + Log.CleanStackTrace(ex);
            view.ShowDialog();
        }
    }
}
