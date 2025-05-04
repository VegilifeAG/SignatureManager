using System;
using System.Reflection;

namespace Signatur_Verwaltung
{
    public partial class Settings : Form
    {
        private TempJsonSettingsManager settingsManager;

        private static bool internetConnection = false;
        private static bool graphConnection = false;
        private static bool graphAuth = false;
        private static bool sharepointConnection = false;

        public Settings()
        {
            InitializeComponent();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            label6.Text = $"Version: {version}";
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            settingsManager = new TempJsonSettingsManager();

            clientIDBox.Text = Properties.Settings.Default.ClientID;
            tenantIDBox.Text = Properties.Settings.Default.TenantID;
            clientSecretBox.Text = Properties.Settings.Default.ClientSecret;

            label5.Text = "Lizensiert für " + Properties.Settings.Default.LicenseName;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.ClientID = clientIDBox.Text;
            Properties.Settings.Default.TenantID = tenantIDBox.Text;
            Properties.Settings.Default.ClientSecret = clientSecretBox.Text;
            Properties.Settings.Default.SignatureChannelID = signatureChannelComboBox.SelectedIndex;
            Properties.Settings.Default.Save();

            settingsManager.ClientID = clientIDBox.Text;
            settingsManager.TenantID = tenantIDBox.Text;
            settingsManager.ClientSecret = clientSecretBox.Text;
            settingsManager.SignatureChannelID = signatureChannelComboBox.SelectedIndex;

            settingsManager.Save();
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.ClientID = null;
            Properties.Settings.Default.TenantID = null;
            Properties.Settings.Default.ClientSecret = null;
            Properties.Settings.Default.SignatureChannelID = -1;

            clientIDBox.Text = Properties.Settings.Default.ClientID;
            tenantIDBox.Text = Properties.Settings.Default.TenantID;
            clientSecretBox.Text = Properties.Settings.Default.ClientSecret;
            signatureChannelComboBox.SelectedIndex = Properties.Settings.Default.SignatureChannelID;
        }

       
        private void button2_Click(object sender, EventArgs e)
        {
            CheckForUpdates checkForUpdatesForm = new CheckForUpdates();
            checkForUpdatesForm.ShowDialog();
        }
    }
}
