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
using System.ComponentModel;
using System.IO;
using Microsoft.Graph;
using OfficeOpenXml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Signatur_Verwaltung
{
    public partial class Form1 : Form
    {
        private static string clientId;
        private static string tenantId;
        private static string clientSecret;
        private static string siteId;
        private static int signatureChannelID;
        private static bool isRemoteUpdate;

        private System.Windows.Forms.Timer shutdownTimer;
        private bool wasOutlookRunning = false;
        private static readonly string TempPath = Path.Combine(Path.GetTempPath(), "SignatureManager");
        private static readonly string FileName = "SignatureManagerSettings.json";
        private static readonly string FilePath = Path.Combine(TempPath, FileName);

        private static DateTime? lastRemoteUpdate = null; // Zeitpunkt des letzten Remote-Updates
        private List<DateTime> recentRemoteUpdates = new List<DateTime>();
        private bool isThirtyMinuteBlockActive = false;
        private DateTime thirtyMinuteBlockStartTime;

        private System.Windows.Forms.Timer listCheckTimer;

        public Form1()
        {
            InitializeComponent();
            LoadSettings();  // Lade Einstellungen in Variablen
            Initialize();

            // Timer initialisieren
            listCheckTimer = new System.Windows.Forms.Timer();
            listCheckTimer.Interval = 60000; // 60 Sekunden
            listCheckTimer.Tick += ListCheckTimer_Tick;
            listCheckTimer.Start();

            this.FormClosing += Form1_FormClosing;
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            await UpdateSharePointListItem(
                status: "offline",              
                updateRequestor: "local"         
            );
        }

        private void LoadSettings()
        {
            // Einstellungen in private Felder laden
            clientId = Properties.Settings.Default.ClientID;
            tenantId = Properties.Settings.Default.TenantID;
            clientSecret = Properties.Settings.Default.ClientSecret;
            signatureChannelID = Properties.Settings.Default.SignatureChannelID;
        }

        private static readonly SemaphoreSlim listCheckSemaphore = new SemaphoreSlim(1, 1);

        private async void ListCheckTimer_Tick(object sender, EventArgs e)
        {
            if (!await listCheckSemaphore.WaitAsync(0))
            {
                Trace.WriteLine("ListCheckTimer_Tick is already running.");
                return;
            }

            try
            {
                await CheckSharePointListForUpdates(); // Prüft nur auf Remote-Anweisungen
            }
            finally
            {
                listCheckSemaphore.Release();
            }
        }

        private async void Initialize()
        {
            if (!CheckInternetConnection())
            {
                errorToastNotification("Signaturenaktualisierung fehlgeschlagen", "Bitte stellen Sie sicher, dass Sie bei der nächsten Anmeldung mit dem Internet verbunden sind.");
                return;
            }

            ImportSettingsFromTempFile();

            var graphClient = GetAuthenticatedGraphClient();
            var site = await graphClient.Sites.GetByPath("sites/IT9", "vegilifeag966.sharepoint.com").Request().GetAsync();
            siteId = site.Id;

            await RegisterDeviceIfNotExists();
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



        private async Task BackupAndUpdateSignatures()
        {
            try
            {
                if (isRemoteUpdate)
                {
                    // Entferne Zeitstempel, die älter als 5 Minuten sind
                    recentRemoteUpdates = recentRemoteUpdates.Where(ts => (DateTime.Now - ts).TotalMinutes <= 5).ToList();

                    // Füge den aktuellen Zeitstempel hinzu
                    recentRemoteUpdates.Add(DateTime.Now);

                    // Überprüfen, ob 5 Updates in den letzten 5 Minuten aufgetreten sind
                    if (recentRemoteUpdates.Count >= 5)
                    {
                        // Überprüfen, ob bereits eine 30-Minuten-Sperre aktiv ist
                        if (isThirtyMinuteBlockActive)
                        {
                            // Wenn die 30-Minuten-Sperre aktiv ist und noch nicht abgelaufen, blockieren
                            if ((DateTime.Now - thirtyMinuteBlockStartTime).TotalMinutes < 30)
                            {
                                Trace.WriteLine("Remote update blocked. 30-minute cooldown is active.");
                                UpdateSharePointListItem("max-execution", "local");
                                return;
                            }
                            else
                            {
                                // Wenn die 30-Minuten-Sperre abgelaufen ist, zurücksetzen
                                isThirtyMinuteBlockActive = false;
                                recentRemoteUpdates.Clear();
                            }
                        }
                        else
                        {
                            // Aktiviere die 30-Minuten-Sperre
                            isThirtyMinuteBlockActive = true;
                            thirtyMinuteBlockStartTime = DateTime.Now;
                            Trace.WriteLine("Remote update blocked. 30-minute cooldown started.");
                            UpdateSharePointListItem("max-execution", "local");
                            return;
                        }
                    }
                }

                // Aktualisierung der Zeit des letzten Remote-Updates, falls ein Remote-Update ausgeführt wird
                if (isRemoteUpdate == true) {
                    indeterminateToastNotification("Initialisieren... - Von Ihrer Organisation angefordert.", "");
                    lastRemoteUpdate = DateTime.Now;
                } else {
                    indeterminateToastNotification("Initialisieren...", "");
                }
                var graphClient = GetAuthenticatedGraphClient();
                var userDownloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Signatures");
                var backupFolder = Path.Combine(System.IO.Path.GetTempPath(), "SignatureManager", "Backup");

                // Überprüfen, ob das temporäre Verzeichnis existiert, und bei Bedarf erstellen
                if (!System.IO.Directory.Exists(backupFolder))
                {
                    Trace.WriteLine($"Backup folder does not exist. Creating directory: {backupFolder}");
                    System.IO.Directory.CreateDirectory(backupFolder);
                }

                // Backup the current Signatures folder
                if (System.IO.Directory.Exists(userDownloadFolder))
                {
                    Trace.WriteLine($"Backing up signatures from: {userDownloadFolder} to {backupFolder}");
                    if (System.IO.Directory.Exists(backupFolder))
                    {
                        System.IO.Directory.Delete(backupFolder, true);
                    }
                    System.IO.Directory.Move(userDownloadFolder, backupFolder);
                }

                // Sicherstellen, dass das Benutzer-Download-Verzeichnis existiert
                if (!System.IO.Directory.Exists(userDownloadFolder))
                {
                    Trace.WriteLine($"User signatures folder does not exist. Creating directory: {userDownloadFolder}");
                    System.IO.Directory.CreateDirectory(userDownloadFolder);
                }

                Trace.WriteLine("Getting site and drive information...");
                var drive = await graphClient.Sites[siteId].Drive.Request().GetAsync();
                var driveId = drive.Id;

                // Suche gezielt nach dem 'Signatures'-Ordner
                var searchResult = await graphClient.Sites[siteId].Drives[driveId]
                    .Root
                    .Search("Signatures")
                    .Request()
                    .GetAsync();

                var signatureFolder = searchResult.FirstOrDefault(item => item.Folder != null);
                if (signatureFolder != null)
                {
                    await UpdateSharePointListItem(
                        status: "updating",                  // Neuer Status
                        updateRequestor: isRemoteUpdate ? "remote" : "local"          // Anfragender
                    );

                    var signaturesFolderId = signatureFolder.Id;
                    Trace.WriteLine($"Found Signatures folder: {signatureFolder.Name} (ID: {signaturesFolderId})");

                    // Navigiere und lade den Inhalt des Ordners herunter
                    await NavigateAndDownload(graphClient, siteId, driveId, signaturesFolderId, userDownloadFolder);
                    updateIndeterminateToastNotification("Abgeschlossen", "");

                    if (isRemoteUpdate == true) {
                        updateIndeterminateToastNotification("Abgeschlossen - Von Ihrer Organisation angefordert.", "");
                    } else {
                        updateIndeterminateToastNotification("Abgeschlossen", "");
                    }

                    await Task.Delay(2000);
                    ToastNotificationManagerCompat.History.Remove("ProcessToast", "SignatureUpdateProcess");
                    ExportSettingsToTempFile();
                    await UpdateSharePointListItem(
                        status: "idle",                  // Neuer Status
                        updateRequestor: "local" // Anfragender
                    );
                    isRemoteUpdate = false;
                }
                else
                {
                    MessageBox.Show("Signatures folder not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Trace.WriteLine("Signatures folder not found.");
                }
            }
            catch (Exception ex)
            {
                // Fehlernachricht und Stack-Trace ausgeben
                Trace.WriteLine($"An error occurred: {ex.Message}");
                Trace.WriteLine($"StackTrace: {ex.StackTrace}");

                // Zeige detaillierte Fehlermeldung an
                MessageBox.Show($"An error occurred: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async Task NavigateAndDownload(GraphServiceClient graphClient, string siteId, string driveId, string signaturesFolderId, string userDownloadFolder)
        {
            string currentDeviceName = Environment.MachineName; // Aktueller PC-Name

            // Zielordner ermitteln (remote oder lokal)
            string targetUser = await GetTargetFolder(graphClient, currentDeviceName, signatureChannelID);
            Trace.WriteLine($"Using target folder: {targetUser}");

            // Suche und Herunterladen der Signaturen basierend auf dem Zielordner
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
                            var userItems = await graphClient.Sites[siteId].Drives[driveId].Items[windowsItem.Id].Children.Request().GetAsync();
                            foreach (var userItem in userItems)
                            {
                                if (userItem.Name == targetUser && userItem.Folder != null)
                                {
                                    Trace.WriteLine($"Folder for user '{targetUser}' found in SharePoint.");
                                    var items = await graphClient.Sites[siteId].Drives[driveId].Items[userItem.Id].Children.Request().GetAsync();
                                    await ProcessItems(graphClient, siteId, driveId, items, userDownloadFolder);
                                    return; // Fertig, Ordner gefunden und verarbeitet
                                }
                            }

                            Trace.WriteLine($"Folder for user '{targetUser}' does not exist in SharePoint.");
                        }
                    }
                }
            }

            Trace.WriteLine($"No matching folder found for user '{targetUser}'.");
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
                        !fileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) &&
                        !fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        if (isRemoteUpdate == true) {
                            updateIndeterminateToastNotification("Aktualisieren... - Von Ihrer Organisation angefordert.", displayFileName); // Update toast notification dynamically
                        } else {
                            updateIndeterminateToastNotification("Aktualisieren...", displayFileName); // Update toast notification dynamically
                        }
                    }

                    try
                    {
                        var fileContent = await graphClient.Sites[siteId].Drives[driveId].Items[item.Id].Content.Request().GetAsync();
                        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            await fileContent.CopyToAsync(fileStream);
                        }
                        //Trace.WriteLine($"Downloaded file: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Failed to download {fileName}: {ex.Message}");
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

                Trace.WriteLine("Authenticated successfully.");
                return new GraphServiceClient(authProvider);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Authentication failed: {ex.Message}");
                Trace.WriteLine($"Exception: {ex}");
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
            .AddText("Signaturen werden aktualisiert...")
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
                Group = "SignatureUpdateProcess",
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

            // Aktualisiere die Toast-Benachrichtigung mit den neuen Daten
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

        private async Task userAccountControlAuthAsync(Func<Task> successProcess)
        {
            try
            {
                var procInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = "powershell.exe",
                    Verb = "runas",
                    Arguments = "-Command \"Write-Output 'UAC Authentifizierung erfolgreich'\"",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                var process = System.Diagnostics.Process.Start(procInfo);
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    await successProcess();
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
            userAccountControlAuth(() =>
            {
                Settings settingsForm = new Settings();
                settingsForm.ShowDialog();
            });
        }

        private void signaturenAktualisierenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BackupAndUpdateSignatures();
        }

        private async void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await userAccountControlAuthAsync(async () =>
            {
                try
                {
                    string currentDeviceName = Environment.MachineName;
                    var graphClient = GetAuthenticatedGraphClient();
                    var site = await graphClient.Sites.GetByPath("sites/IT9", "vegilifeag966.sharepoint.com").Request().GetAsync();

                    await UpdateSharePointListItem(
                        status: "offline",
                        updateRequestor: "local"
                    );

                    Trace.WriteLine($"Device '{currentDeviceName}' status updated to 'offline' before exiting.");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error updating status to 'offline' before exiting: {ex.Message}");
                }
                finally
                {
                    System.Windows.Forms.Application.Exit();
                }
            });
        }


        private async Task UpdateSharePointListItem(string status, string updateRequestor)
        {
            try
            {
                var graphClient = GetAuthenticatedGraphClient();

                // Alle Listeneinträge abrufen
                Trace.WriteLine("Fetching all items from SharePoint list...");
                var listItems = await graphClient.Sites[siteId]
                    .Lists["Devices"]
                    .Items
                    .Request()
                    .Expand("fields") // WICHTIG: Felder explizit anfordern
                    .GetAsync();

                // Manuell nach dem DeviceName filtern
                Trace.WriteLine("Filtering items locally...");
                var existingItem = listItems.CurrentPage.FirstOrDefault(item =>
                    item.Fields != null &&
                    item.Fields.AdditionalData != null &&
                    item.Fields.AdditionalData.ContainsKey("DeviceName") && // Ersetze "DeviceName" mit dem internen Namen
                    item.Fields.AdditionalData["DeviceName"] != null &&
                    item.Fields.AdditionalData["DeviceName"].ToString().Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase));

                if (existingItem != null)
                {
                    // Eintrag aktualisieren
                    Trace.WriteLine($"Updating existing item for device '{Environment.MachineName}'...");
                    var itemId = existingItem.Id;
                    var fields = new FieldValueSet
                    {
                        AdditionalData = new Dictionary<string, object>
                {
                    { "AppVersion", Assembly.GetExecutingAssembly().GetName().Version },
                    { "Status", status }, // Ersetze "Status" mit dem internen Namen der Spalte
                    { "LastUpdate", DateTime.Now.ToString("dd.MM.yyyy - HH:mm") }, // Aktualisiere das Datum
                    { "UpdateRequestor", updateRequestor } // Ersetze "UpdateRequestor" mit dem internen Namen
                }
                    };

                    await graphClient.Sites[siteId]
                        .Lists["Devices"]
                        .Items[itemId]
                        .Fields
                        .Request()
                        .UpdateAsync(fields);

                    Trace.WriteLine($"Updated device '{Environment.MachineName}' with status '{status}' in SharePoint list.");
                }
                else
                {
                    // Kein Eintrag gefunden
                    Trace.WriteLine($"Device '{Environment.MachineName}' not found in SharePoint list. No updates made.");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error interacting with SharePoint list: {ex.Message}");
            }
        }


        private async Task CheckSharePointListForUpdates()
        {
            try
            {
                var graphClient = GetAuthenticatedGraphClient();

                // Alle Listeneinträge abrufen
                Trace.WriteLine("Fetching all items from SharePoint list...");
                var listItems = await graphClient.Sites[siteId]
                    .Lists["Devices"]
                    .Items
                    .Request()
                    .Expand("fields") // Felder explizit anfordern
                    .GetAsync();

                // Überprüfung: Status = "requested" und UpdateRequestor = "remote"
                var matchingItems = listItems.CurrentPage.Where(item =>
                    item.Fields != null &&
                    item.Fields.AdditionalData != null &&
                    item.Fields.AdditionalData.TryGetValue("Status", out var status) &&
                    status != null &&
                    status.ToString().Equals("requested", StringComparison.OrdinalIgnoreCase) &&
                    item.Fields.AdditionalData.TryGetValue("UpdateRequestor", out var requestor) &&
                    requestor != null &&
                    requestor.ToString().Equals("remote", StringComparison.OrdinalIgnoreCase));

                if (matchingItems.Any())
                {
                    foreach (var matchingItem in matchingItems)
                    {
                        var deviceName = matchingItem.Fields.AdditionalData.ContainsKey("DeviceName")
                            ? matchingItem.Fields.AdditionalData["DeviceName"].ToString()
                            : "Unknown Device";

                        Trace.WriteLine($"Update request detected for device: {deviceName}");

                        // Nur für Remote-Updates das Flag setzen und die Toast-Benachrichtigung anpassen
                        isRemoteUpdate = true;
                        await BackupAndUpdateSignatures();
                        isRemoteUpdate = false; // Direkt nach dem Update zurücksetzen
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error interacting with SharePoint list: {ex.Message}");
            }
        }

        private async Task RegisterDeviceIfNotExists()
        {
            try
            {
                var graphClient = GetAuthenticatedGraphClient();
                string listName = "Devices"; // Direkt den Listenname setzen
                string deviceName = Environment.MachineName;

                // Alle Listeneinträge abrufen
                Trace.WriteLine("Fetching all items from SharePoint list...");
                var listItems = await graphClient.Sites[siteId]
                    .Lists[listName]
                    .Items
                    .Request()
                    .Expand("fields") // Felder explizit anfordern
                    .GetAsync();

                // Liste aller verfügbaren Felder abrufen
                Trace.WriteLine("Fetching SharePoint list fields...");
                var listFields = await graphClient.Sites[siteId]
                    .Lists[listName]
                    .Columns
                    .Request()
                    .GetAsync();

                // Internen Namen für 'AppPlattform', 'DeviceName' und 'AppVersion' validieren
                string appPlattformFieldName = listFields.FirstOrDefault(f => f.DisplayName == "AppPlattform")?.Name ?? "AppPlattform";
                string deviceNameFieldName = listFields.FirstOrDefault(f => f.DisplayName == "DeviceName")?.Name ?? "DeviceName";
                string appVersionFieldName = listFields.FirstOrDefault(f => f.DisplayName == "AppVersion")?.Name ?? "AppVersion";

                // Prüfen, ob der aktuelle PC bereits registriert ist
                Trace.WriteLine("Filtering items locally...");
                var existingItem = listItems.CurrentPage.FirstOrDefault(item =>
                    item.Fields != null &&
                    item.Fields.AdditionalData != null &&
                    item.Fields.AdditionalData.ContainsKey(deviceNameFieldName) &&
                    item.Fields.AdditionalData[deviceNameFieldName] != null &&
                    item.Fields.AdditionalData[deviceNameFieldName].ToString().Equals(deviceName, StringComparison.OrdinalIgnoreCase));

                if (existingItem != null)
                {
                    Trace.WriteLine($"Device '{deviceName}' is already registered in the SharePoint list.");
                    return;
                }

                // Wenn der PC nicht gefunden wurde, neuen Eintrag erstellen
                Trace.WriteLine($"Device '{deviceName}' not found in the SharePoint list. Creating a new entry...");

                var newItem = new ListItem
                {
                    Fields = new FieldValueSet
                    {
                        AdditionalData = new Dictionary<string, object>
                {
                    { appPlattformFieldName, "Windows" },
                    { deviceNameFieldName, deviceName },
                    { appVersionFieldName, Assembly.GetExecutingAssembly().GetName().Version },
                    { "Status", "registered" }, // Optional: Standard-Status
                    { "LastUpdate", DateTime.Now.ToString("dd.MM.yyyy - HH:mm") },
                    { "UpdateRequestor", "local" } // Optional
                }
                    }
                };

                await graphClient.Sites[siteId]
                    .Lists[listName]
                    .Items
                    .Request()
                    .AddAsync(newItem);

                Trace.WriteLine($"Device '{deviceName}' has been successfully registered in the SharePoint list.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error while registering device '{Environment.MachineName}': {ex.Message}");
            }
        }


        private async Task<string> GetTargetFolder(GraphServiceClient graphClient, string currentDeviceName, int signatureChannelID)
        {
            try
            {
                string listName = "Devices"; // Name der SharePoint-Liste

                // Alle Einträge aus der Liste abrufen
                var listItems = await graphClient.Sites[siteId]
                    .Lists[listName]
                    .Items
                    .Request()
                    .Expand("fields")
                    .GetAsync();

                // Eintrag für das aktuelle Gerät suchen
                var existingItem = listItems.CurrentPage.FirstOrDefault(item =>
                    item.Fields != null &&
                    item.Fields.AdditionalData != null &&
                    item.Fields.AdditionalData.ContainsKey("DeviceName") &&
                    item.Fields.AdditionalData["DeviceName"] != null &&
                    item.Fields.AdditionalData["DeviceName"].ToString().Equals(currentDeviceName, StringComparison.OrdinalIgnoreCase));

                if (existingItem != null)
                {
                    // Benutzer aus dem Eintrag abrufen
                    if (existingItem.Fields.AdditionalData.ContainsKey("User"))
                    {
                        string assignedUser = existingItem.Fields.AdditionalData["User"].ToString();
                        Trace.WriteLine($"User field found: {assignedUser}");

                        // Benutzernamen zurückgeben
                        return assignedUser;
                    }
                    else
                    {
                        Trace.WriteLine($"No 'User' field found for device '{currentDeviceName}'. Falling back to local setting.");
                    }
                }
                else
                {
                    Trace.WriteLine($"Device '{currentDeviceName}' not found in SharePoint list. Falling back to local setting.");
                }

                // Lokale Einstellung basierend auf signatureChannelID
                string localUser = signatureChannelID switch
                {
                    0 => "Marcel Bourquin",
                    1 => "Debora Staub",
                    _ => "Yannick Wiss"
                };
                Trace.WriteLine($"Using local setting based on signatureChannelID: {localUser}");
                return localUser;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error fetching target folder from SharePoint list: {ex.Message}");
                // Lokale Einstellung im Fehlerfall
                string fallbackUser = signatureChannelID switch
                {
                    0 => "Marcel Bourquin",
                    1 => "Debora Staub",
                    _ => "Yannick Wiss"
                };
                Trace.WriteLine($"Using fallback local setting: {fallbackUser}");
                return fallbackUser;
            }
        }
    }
}

