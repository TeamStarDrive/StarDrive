using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


///Added by Crimsoned

namespace Ship_Game
{
    public partial class ExceptionViewer: Form
    {
        public ExceptionViewer()
        {
            InitializeComponent();
        }
        public DialogResult ShowDialog(string ExceptionMessage)
        {
            tbError.Text = ExceptionMessage;
            tbError.Select(0, 0);
            return ShowDialog();
        }

        private void btClip_Click(object sender, EventArgs e)
        {
            string all = tbError.Text +Environment.NewLine+Environment.NewLine+"User Comment: "+tbComment.Text;
            System.Windows.Forms.Clipboard.SetText(all);

        }

        private void btClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btOpenBugTracker_Click(object sender, EventArgs e)
        {
            Process.Start(ExceptionTracker.BugtrackerURL);
        }

  

    }
}
