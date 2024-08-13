using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Signatur_Verwaltung
{
    public partial class Password : Form
    {
        public Password()
        {
            InitializeComponent();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            string correctPassword = "Vegilife.2015";

            if (textBox1.Text == correctPassword)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Benutzername oder Passwort inkorrekt!", "Fehler - Signatur Verwaltung", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox1.Clear();
                textBox1.Focus();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            if (this.DialogResult == DialogResult.OK)
            {
                Settings settingsForm = new Settings();
                settingsForm.ShowDialog();
            }
        }
    }
}
