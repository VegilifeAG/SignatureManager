using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms;

namespace Signatur_Verwaltung
{
    public partial class CheckForUpdates : Form
    {
        public CheckForUpdates()
        {
            InitializeComponent();

            HeaderLabel.Visible = false;
            progressBar1.Visible = false;
            UpdatesAvailableLabel.Visible = false;
            NoUpdatesAvailableLabel.Visible = false;

            MessageBox.Show("Update-Überprüfung gestartet.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //CheckForUpdatesManually();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

        }
    }
}
