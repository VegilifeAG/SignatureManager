using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.ApplicationModel.VoiceCommands;
using Windows.UI.Notifications;

namespace Signatur_Verwaltung
{
    public partial class Form1 : Form
    {
        private static string clientId = Properties.Settings.Default.ClientID;
        private static string tenantId = Properties.Settings.Default.TenantID;
        private static string clientSecret = Properties.Settings.Default.ClientSecret;
        private System.Windows.Forms.Timer shutdownTimer;
        private bool wasOutlookRunning = false;

        public Form1()
        {
            InitializeComponent();
            Initialize();
        }

        private async void Initialize()
        {
            if (!CheckInternetConnection())
            {
                errorToastNotification("Signaturenaktualisierung fehlgeschlagen", "Bitte stellen Sie sicher, dass Sie bei der nächsten Anmeldung mit dem Internet verbunden sind.");
                return;
            }

            if (IsOutlookRunning())
            {
                wasOutlookRunning = true;
                var result = MessageBox.Show("Outlook muss zum Aktualisieren der Signaturen geschlossen werden. Möchten Sie Outlook jetzt schließen?", "Warnung - Signatur Verwaltung", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                {
                    CloseOutlook();
                }
            }

            await BackupAndUpdateSignatures();
        }


        private bool CheckInternetConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("microsoft.com");
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool IsOutlookRunning()
        {
            return System.Diagnostics.Process.GetProcessesByName("OUTLOOK").Any();
        }

        private void CloseOutlook()
        {
            var outlookProcesses = System.Diagnostics.Process.GetProcessesByName("OUTLOOK");
            foreach (var process in outlookProcesses)
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        private void StartOutlook()
        {
            string[] possiblePaths = {
                @"C:\Program Files\Microsoft Office\root\Office16\OUTLOOK.EXE",
                @"C:\Program Files (x86)\Microsoft Office\root\Office16\OUTLOOK.EXE",
                @"C:\Program Files\Microsoft Office\Office15\OUTLOOK.EXE",
                @"C:\Program Files (x86)\Microsoft Office\Office15\OUTLOOK.EXE",
            };

            foreach (var path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    System.Diagnostics.Process.Start(path);
                    return;
                }
            }

            MessageBox.Show("OUTLOOK.EXE konnte nicht gefunden werden. Bitte starten Sie Outlook manuell.", "Fehler - Signatur Verwaltung", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async Task BackupAndUpdateSignatures()
        {
            try
            {
                // Startmeldung anzeigen
                indeterminateToastNotification("Initialisieren...", "");

                var graphClient = GetAuthenticatedGraphClient();
                var userDownloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Signatures");
                var backupFolder = Path.Combine(System.IO.Path.GetTempPath(), "SignaturesBackup");

                // Backup the current Signatures folder
                if (System.IO.Directory.Exists(userDownloadFolder))
                {
                    if (System.IO.Directory.Exists(backupFolder))
                    {
                        System.IO.Directory.Delete(backupFolder, true);
                    }
                    System.IO.Directory.Move(userDownloadFolder, backupFolder);
                }

                System.IO.Directory.CreateDirectory(userDownloadFolder);

                Debug.WriteLine("Getting site and drive information...");
                var site = await graphClient.Sites.GetByPath("sites/IT9", "vegilifeag966.sharepoint.com").Request().GetAsync();
                var siteId = site.Id;

                var drive = await graphClient.Sites[siteId].Drive.Request().GetAsync();
                var driveId = drive.Id;

                var rootItems = await graphClient.Sites[siteId].Drives[driveId].Root.Children.Request().GetAsync();
                string signaturesFolderId = null;

                foreach (var item in rootItems)
                {
                    if (item.Name == "General" && item.Folder != null)
                    {
                        var generalItems = await graphClient.Sites[siteId].Drives[driveId].Items[item.Id].Children.Request().GetAsync();

                        foreach (var generalItem in generalItems)
                        {
                            if (generalItem.Name == "Signatures" && generalItem.Folder != null)
                            {
                                signaturesFolderId = generalItem.Id;
                                break;
                            }
                        }
                        if (signaturesFolderId != null)
                        {
                            break;
                        }
                    }
                }

                if (signaturesFolderId != null)
                {
                    await NavigateAndDownload(graphClient, siteId, driveId, signaturesFolderId, userDownloadFolder);
                    updateIndeterminateToastNotification("Abgeschlossen", "");
                    string currentVersion = GetCurrentVersion();
                    //await CheckForUpdates(graphClient, currentVersion);
                }
                else
                {
                    MessageBox.Show("Signatures folder not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Debug.WriteLine("Signatures folder not found.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (wasOutlookRunning)
            {
                StartOutlook();
            }
        }


        private async Task NavigateAndDownload(GraphServiceClient graphClient, string siteId, string driveId, string signaturesFolderId, string userDownloadFolder)
        {
            var liveSyncItems = await graphClient.Sites[siteId].Drives[driveId].Items[signaturesFolderId].Children.Request().GetAsync();
            foreach (var liveSyncItem in liveSyncItems)
            {
                if (liveSyncItem.Name == ".LiveSync" && liveSyncItem.Folder != null)
                {
                    var windowsItems = await graphClient.Sites[siteId].Drives[driveId].Items[liveSyncItem.Id].Children.Request().GetAsync();
                    foreach (var windowsItem in windowsItems)
                    {
                        if (windowsItem.Name == "Windows" && windowsItem.Folder != null)
                        {
                            string targetFolderName = Properties.Settings.Default.SignatureChannelID == 0 ? "Marcel Bourquin" :
                                                      Properties.Settings.Default.SignatureChannelID == 1 ? "Nadine Dahinden" :
                                                      "Yannick Wiss";

                            var userItems = await graphClient.Sites[siteId].Drives[driveId].Items[windowsItem.Id].Children.Request().GetAsync();
                            foreach (var userItem in userItems)
                            {
                                if (userItem.Name == targetFolderName && userItem.Folder != null)
                                {
                                    var items = await graphClient.Sites[siteId].Drives[driveId].Items[userItem.Id].Children.Request().GetAsync();
                                    await ProcessItems(graphClient, siteId, driveId, items, userDownloadFolder);
                                }
                            }
                        }
                    }
                }
            }
        }

        private async Task ProcessItems(GraphServiceClient graphClient, string siteId, string driveId, IEnumerable<DriveItem> items, string userDownloadFolder)
        {
            foreach (var item in items)
            {
                if (item.Folder != null)
                {
                    // Create the directory for the subfolder
                    var subFolderPath = Path.Combine(userDownloadFolder, item.Name);
                    if (!System.IO.Directory.Exists(subFolderPath))
                    {
                        System.IO.Directory.CreateDirectory(subFolderPath);
                    }

                    // Recursively process the subfolder's contents
                    var subItems = await graphClient.Sites[siteId].Drives[driveId].Items[item.Id].Children.Request().GetAsync();
                    await ProcessItems(graphClient, siteId, driveId, subItems, subFolderPath);
                }
                else if (item.File != null)
                {
                    // Download the file
                    var fileName = item.Name;
                    var displayFileName = ExtractFileName(item.Name);
                    var filePath = Path.Combine(userDownloadFolder, fileName);

                    if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                        !fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
                        !fileName.EndsWith(".thmx", StringComparison.OrdinalIgnoreCase) &&
                        !fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        updateIndeterminateToastNotification("Aktualisieren...", displayFileName); // Update toast notification dynamically
                    }

                    try
                    {
                        var fileContent = await graphClient.Sites[siteId].Drives[driveId].Items[item.Id].Content.Request().GetAsync();
                        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            await fileContent.CopyToAsync(fileStream);
                        }
                        //Debug.WriteLine($"Downloaded file: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to download {fileName}: {ex.Message}");
                        errorToastNotification("Fehler beim Herunterladen der Datei", $"Datei: {fileName}\nFehler: {ex.Message}");
                    }
                }
            }
        }


        private static string ExtractFileName(string itemName)
        {
            int startIndex = itemName.IndexOf('(');
            if (startIndex > 0)
            {
                return itemName.Substring(0, startIndex).Trim();
            }
            return itemName;
        }


        private static GraphServiceClient GetAuthenticatedGraphClient()
        {
            try
            {
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

                Debug.WriteLine("Authenticated successfully.");
                return new GraphServiceClient(authProvider);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Authentication failed: {ex.Message}");
                Debug.WriteLine($"Exception: {ex}");
                throw;
            }
        }

        private void errorToastNotification(string title, string message)
        {
            var notifier = ToastNotificationManagerCompat.CreateToastNotifier();
            Bitmap image = Properties.Resources.Error_png;
            string tempPath = Path.Combine(Path.GetTempPath(), "Error.png");
            image.Save(tempPath);
            Uri imageUri = new Uri(tempPath, UriKind.Absolute);

            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .AddAppLogoOverride(imageUri)
                .SetToastDuration(ToastDuration.Short);

            var content = builder.GetToastContent();
            var toast = new ToastNotification(content.GetXml())
            {
                Tag = "ErrorToast",
                Group = "NoEthernetAvailable"
            };
            notifier.Show(toast);
        }


        private void indeterminateToastNotification(string processState, string processTitle)
        {
            var toastContent = new ToastContentBuilder()
            .SetToastDuration(ToastDuration.Long)
            .AddText("Signaturen werden aktualsiert...")
            .AddVisualChild(new AdaptiveProgressBar()
            {
                Title = new BindableString("processTitle"),
                Value = AdaptiveProgressBarValue.Indeterminate,
                Status = new BindableString("processState")
            })
            .GetToastContent();

            var toast = new ToastNotification(toastContent.GetXml())
            {
                Tag = "ProcessToast",
                Group = "SignatureUpdateProcess"
            };

            var data = new NotificationData();
            data.Values["processState"] = processState;
            data.Values["processTitle"] = processTitle;

            toast.Data = data;

            ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        private void updateIndeterminateToastNotification(string processState, string processTitle)
        {
            var data = new NotificationData();
            data.Values["processState"] = processState;
            data.Values["processTitle"] = processTitle;

            ToastNotificationManagerCompat.CreateToastNotifier().Update(data, "ProcessToast", "SignatureUpdateProcess");
        }

        public void userAccountControlAuth(Action successProcess)
        {
            try
            {
                ProcessStartInfo procInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = "powershell.exe",
                    Verb = "runas",
                    Arguments = "-Command \"Write-Output 'UAC Authentifizierung erfolgreich'\"",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                System.Diagnostics.Process proc = System.Diagnostics.Process.Start(procInfo);
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    successProcess();
                }
                else
                {
                    MessageBox.Show("Die erforderlichen Administratorrechte wurden nicht erteilt.", "Zugriff verweigert", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Die erforderlichen Administratorrechte wurden nicht erteilt.", "Zugriff verweigert", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }







        private async Task CheckForUpdates(GraphServiceClient graphClient, string currentVersion)
        {
            MessageBox.Show($"Derzeit installierte Version: {currentVersion}", "Prüfen auf Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);

            try
            {
                // Pfadangabe auf SharePoint
                string sitePath = "sites/IT9";
                string siteId = "vegilifeag966.sharepoint.com";

                // Die Id des Ordners 'LiveSync' im Pfad 'IT9/General/Software/LiveSync'
                var site = await graphClient.Sites.GetByPath(sitePath, siteId).Request().GetAsync();
                var drive = await graphClient.Sites[site.Id].Drive.Request().GetAsync();
                var rootItem = await graphClient.Sites[site.Id].Drives[drive.Id].Root.ItemWithPath("General/Software/.LiveSync").Request().GetAsync();
                var updatesFolderId = rootItem.Id;

                // Get the items (files) in the updates directory
                var updateFiles = await graphClient.Sites[site.Id].Drives[drive.Id].Items[updatesFolderId].Children.Request().GetAsync();

                string latestVersion = currentVersion;
                string latestFileName = null;

                foreach (var file in updateFiles)
                {
                    if (file.File != null)
                    {
                        // Extract version number from file name, e.g., "MyApp_v1.1.zip" -> "1.1"
                        string fileVersion = ExtractVersionFromFileName(file.Name);

                        // Compare with the current version
                        if (IsNewerVersion(fileVersion, latestVersion))
                        {
                            latestVersion = fileVersion;
                            latestFileName = file.Name;
                        }
                    }
                }

                // If a newer version is found
                if (!string.IsNullOrEmpty(latestFileName) && IsNewerVersion(latestVersion, currentVersion))
                {
                    var result = MessageBox.Show(
                        $"Derzeit installierte Version: {currentVersion}\nVerfügbare Version: {latestVersion}\n\nMöchten Sie jetzt aktualisieren?",
                        "Update verfügbar",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        await DownloadAndUpdate(graphClient, site.Id, drive.Id, updatesFolderId, latestFileName);
                    }
                }
                else
                {
                    MessageBox.Show($"Sie haben bereits die neueste Version installiert.\nDerzeit installierte Version: {currentVersion}", "Keine Updates verfügbar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Überprüfen auf Updates: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                errorToastNotification("Fehler beim Überprüfen auf Updates", $"Ein Fehler ist aufgetreten: {ex.Message}");
            }
        }

        private string ExtractVersionFromFileName(string fileName)
        {
            // Example: Extract "1.1" from "MyApp_v1.1.zip"
            var match = Regex.Match(fileName, @"v(\d+\.\d+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        private bool IsNewerVersion(string fileVersion, string currentVersion)
        {
            if (string.IsNullOrEmpty(fileVersion) || string.IsNullOrEmpty(currentVersion))
                return false;

            Version newVersion = new Version(fileVersion);
            Version installedVersion = new Version(currentVersion);

            return newVersion > installedVersion;
        }

        private async Task DownloadAndUpdate(GraphServiceClient graphClient, string siteId, string driveId, string updatesFolderId, string latestFileName)
        {
            try
            {
                // Download the file
                var filePath = Path.Combine(Path.GetTempPath(), latestFileName);
                var fileContent = await graphClient.Sites[siteId].Drives[driveId].Items[updatesFolderId].ItemWithPath(latestFileName).Content.Request().GetAsync();
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await fileContent.CopyToAsync(fileStream);
                }

                MessageBox.Show($"Update erfolgreich heruntergeladen: {filePath}", "Update erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Implement your update logic here, e.g., extract and replace files, restart application, etc.
                MessageBox.Show($"Die neue Version {latestFileName} wurde erfolgreich heruntergeladen und wird jetzt installiert.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Migrate settings after update
                MigrateSettings();

                // Example: Run the installer (assuming it's a self-contained installer)
                System.Diagnostics.Process.Start(filePath);
                System.Windows.Forms.Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Herunterladen der Update-Datei {latestFileName}: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                errorToastNotification("Fehler beim Update-Download", $"Datei: {latestFileName}\nFehler: {ex.Message}");
            }
        }

        private void SaveCurrentSettings()
        {
            Properties.Settings.Default.Save();
        }

        private void MigrateSettings()
        {
            if (!Properties.Settings.Default.HasUpgraded)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.HasUpgraded = true;
                Properties.Settings.Default.Save();
            }
        }

        private string GetCurrentVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var versionAttribute = assembly.GetCustomAttribute<System.Reflection.AssemblyFileVersionAttribute>();
            return versionAttribute?.Version;
        }

















        private void einstellungenToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            userAccountControlAuth(() => {
                Settings settingsForm = new Settings();
                settingsForm.ShowDialog();
            });
        }

        private void signaturenAktualisierenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BackupAndUpdateSignatures();
        }

        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            userAccountControlAuth(() => {
                System.Windows.Forms.Application.Exit();
            });
        }
    }
}
