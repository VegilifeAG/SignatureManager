using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using System.Configuration;
using System.Xml;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json;

namespace Signatur_Verwaltung
{
    public partial class Form1 : Form
    {
        private static string clientId;
        private static string tenantId;
        private static string clientSecret;
        private static int signatureChannelID;

        private System.Windows.Forms.Timer shutdownTimer;
        private bool wasOutlookRunning = false;
        private static readonly string TempPath = Path.Combine(Path.GetTempPath(), "SignatureManager");
        private static readonly string FileName = "SignatureManagerSettings.json";
        private static readonly string FilePath = Path.Combine(TempPath, FileName);

        public Form1()
        {
            InitializeComponent();
            LoadSettings();  // Lade Einstellungen in Variablen
            Initialize();
        }

        private void LoadSettings()
        {
            // Einstellungen in private Felder laden
            clientId = Properties.Settings.Default.ClientID;
            tenantId = Properties.Settings.Default.TenantID;
            clientSecret = Properties.Settings.Default.ClientSecret;
            signatureChannelID = Properties.Settings.Default.SignatureChannelID;
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
            ImportSettingsFromTempFile();
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
                var backupFolder = Path.Combine(System.IO.Path.GetTempPath(), "SignatureManager", "Backup");

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
                    ExportSettingsToTempFile();

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
                            string targetFolderName = signatureChannelID == 0 ? "Marcel Bourquin" :
                                                      signatureChannelID == 1 ? "Nadine Dahinden" :
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


        public static void ExportSettingsToTempFile()
        {
            try
            {
                if (!System.IO.Directory.Exists(TempPath))
                {
                    System.IO.Directory.CreateDirectory(TempPath);
                }

                var settingsDict = new Dictionary<string, object>();
                foreach (System.Configuration.SettingsProperty property in Properties.Settings.Default.Properties)
                {
                    string name = property.Name;
                    var value = Properties.Settings.Default[name];
                    settingsDict[name] = value;
                }

                string json = System.Text.Json.JsonSerializer.Serialize(settingsDict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(FilePath, json);

                //MessageBox.Show($"Die Einstellungen wurden erfolgreich in die Datei '{FilePath}' exportiert.", "Export Erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Fehler beim Exportieren der Einstellungen: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ImportSettingsFromTempFile()
        {
            try
            {
                if (!System.IO.File.Exists(FilePath))
                {
                    //MessageBox.Show($"Die Datei '{FilePath}' wurde nicht gefunden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string json = System.IO.File.ReadAllText(FilePath);
                var settingsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);

                foreach (var kvp in settingsDict)
                {
                    var property = Properties.Settings.Default.Properties[kvp.Key];
                    if (property != null)
                    {
                        // Konvertiere JsonElement in den richtigen Typ
                        object value = ConvertJsonElement(kvp.Value, property.PropertyType);
                        Properties.Settings.Default[kvp.Key] = value;
                    }
                }

                // Änderungen speichern
                Properties.Settings.Default.Save();

                // Anwenden der Änderungen
                ApplySettings();

                //MessageBox.Show("Die Einstellungen wurden erfolgreich wiederhergestellt und angewendet.", "Import Erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Fehler beim Importieren der Einstellungen: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static object ConvertJsonElement(System.Text.Json.JsonElement element, Type targetType)
        {
            if (targetType == typeof(int))
            {
                return element.GetInt32();
            }
            else if (targetType == typeof(double))
            {
                return element.GetDouble();
            }
            else if (targetType == typeof(bool))
            {
                return element.GetBoolean();
            }
            else if (targetType == typeof(string))
            {
                return element.GetString();
            }
            else
            {
                throw new NotSupportedException($"Der Typ '{targetType}' wird nicht unterstützt.");
            }
        }

        private static void ApplySettings()
        {
            // Beispiel: Wenn Einstellungen in Variablen gespeichert wurden, aktualisiere sie jetzt
            clientId = Properties.Settings.Default.ClientID;
            tenantId = Properties.Settings.Default.TenantID;
            clientSecret = Properties.Settings.Default.ClientSecret;
            signatureChannelID = Properties.Settings.Default.SignatureChannelID;

            // Hier können Sie weitere Aktualisierungen vornehmen, wenn dies erforderlich ist.
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
