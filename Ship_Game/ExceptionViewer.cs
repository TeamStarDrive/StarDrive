using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Ship_Game;

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

    [STAThread] // Clipboard.SetText requires STAThread
    void btClip_Click_1(object sender, EventArgs e)
    {
        try
        {
            Clipboard.SetText(tbError.Text);
        }
        catch {}
    }

    void btClose_Click(object sender, EventArgs e)
    {
        Close();
    }

    void githubIssues_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start("https://github.com/TeamStarDrive/StarDrive/issues");
    }

    void discordHelpline_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start("https://discord.gg/dfvnfH4");
    }

    public static void ShowExceptionDialog(string dialogText, bool autoReport)
    {
        var view = new ExceptionViewer();

        if (autoReport)
        {
            view.Description =
                "This error was submitted automatically to our exception tracking system. \r\n" +
                "If this error keeps reoccurring, you can create a new issue on GitHub.";
        }
        else
        {
            view.Description =
                "Automatic error reporting is disabled. \r\n" +
                "If this error keep reoccurring, you can create a new issue on GitHub.";
        }

        view.Error = dialogText;
        // bugfix: ensure default cursor is always enabled, since we override it with software cursor
        view.Cursor = Cursors.Default;
        view.ShowDialog();
    }
}