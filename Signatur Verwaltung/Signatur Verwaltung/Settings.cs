using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace Signatur_Verwaltung
{
    public partial class Settings : Form
    {
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
            clientIDBox.Text = Properties.Settings.Default.ClientID;
            tenantIDBox.Text = Properties.Settings.Default.TenantID;
            clientSecretBox.Text = Properties.Settings.Default.ClientSecret;
            signatureChannelComboBox.SelectedIndex = Properties.Settings.Default.SignatureChannelID;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.ClientID = clientIDBox.Text;
            Properties.Settings.Default.TenantID = tenantIDBox.Text;
            Properties.Settings.Default.ClientSecret = clientSecretBox.Text;
            Properties.Settings.Default.SignatureChannelID = signatureChannelComboBox.SelectedIndex;
            Properties.Settings.Default.Save();
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

        private async void checkConnectionButton_Click(object sender, EventArgs e)
        {
            /*CheckInternetConnection();
            if (internetConnection)
            {
                await CheckMicrosoftGraphConnection();
                await CheckSharePointConnection();
                FinalConnectionCheck();
            }*/

            MessageBox.Show("Diese Funktion steht zurzeit nicht zur verfügung.", "Information - Signatur Verwaltung", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CheckInternetConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("sharepoint.microsoft.com");
                    if (reply.Status == IPStatus.Success)
                    {
                        internetConnection = true;
                        Debug.Print("Internetverbindung erfolgreich.");
                    }
                    else
                    {
                        internetConnection = false;
                        Debug.Print("Internetverbindung fehlgeschlagen.");
                    }
                }
            }
            catch
            {
                internetConnection = false;
                MessageBox.Show("Verbindung zum Internet fehlgeschlagen.", "Fehler - Signatur Verwaltung", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CheckMicrosoftGraphConnection()
        {
            try
            {
                var graphClient = GetAuthenticatedGraphClient();
                var me = await graphClient.Me.Request().GetAsync();
                graphConnection = true;
                Debug.Print($"Microsoft Graph Verbindung erfolgreich: {me.DisplayName}");
            }
            catch (Exception ex)
            {
                graphConnection = false;
                MessageBox.Show($"Verbindung zu Microsoft Graph fehlgeschlagen.\n\n{ex.Message}", "Fehler - Signatur Verwaltung", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static GraphServiceClient GetAuthenticatedGraphClient()
        {
            try
            {
                var clientId = Properties.Settings.Default.ClientID;
                var tenantId = Properties.Settings.Default.TenantID;
                var clientSecret = Properties.Settings.Default.ClientSecret;

                var confidentialClientApplication = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithTenantId(tenantId)
                    .WithClientSecret(clientSecret)
                    .Build();

                var authProvider = new DelegateAuthenticationProvider(async (requestMessage) =>
                {
                    var result = await confidentialClientApplication
                        .AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" })
                        .ExecuteAsync();
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                });

                graphAuth = true;
                Debug.Print("Microsoft Graph Authentifizierung erfolgreich.");
                return new GraphServiceClient(authProvider);
            }
            catch (Exception ex)
            {
                graphAuth = false;
                MessageBox.Show($"Authentifizierung in Microsoft Graph fehlgeschlagen.\n\n{ex.Message}", "Fehler - Signatur Verwaltung", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private async Task CheckSharePointConnection()
        {
            try
            {
                var graphClient = GetAuthenticatedGraphClient();
                var site = await graphClient.Sites.GetByPath("sites/IT9", "vegilifeag966.sharepoint.com").Request().GetAsync();
                sharepointConnection = true;
                Debug.Print($"SharePoint Verbindung erfolgreich: {site.DisplayName}");
            }
            catch (Exception ex)
            {
                sharepointConnection = false;
                MessageBox.Show($"Verbindung zu Microsoft SharePoint fehlgeschlagen.\n\n{ex.Message}", "Fehler - Signatur Verwaltung", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FinalConnectionCheck()
        {
            if (internetConnection && graphConnection && graphAuth && sharepointConnection)
            {
                MessageBox.Show("Verbindung zu den Diensten erfolgreich!", "Information - Signatur Verwaltung", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Eine oder mehrere Verbindungen sind fehlgeschlagen.", "Fehler - Signatur Verwaltung", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }
    }
}
