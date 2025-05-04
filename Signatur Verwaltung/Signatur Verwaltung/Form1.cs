using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

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
        private static readonly string FileName = "settings.json";
        private static readonly string FilePath = Path.Combine(TempPath, FileName);

        private static DateTime? lastRemoteUpdate = null;
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
            listCheckTimer.Interval = 900000; // 15 Minuten 
            listCheckTimer.Tick += ListCheckTimer_Tick;
            listCheckTimer.Start();

            this.FormClosing += Form1_FormClosing;
        }

        private class LicenseResponse
        {
            public string clientId { get; set; }
            public string tenantId { get; set; }
            public string clientSecret { get; set; }
            public string licensedOrganisationName { get; set; }
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

        private async Task<bool> CheckInternetConnection()
        {
            clientId = Properties.Settings.Default.ClientID;
            tenantId = Properties.Settings.Default.TenantID;
            clientSecret = Properties.Settings.Default.ClientSecret;
            signatureChannelID = Properties.Settings.Default.SignatureChannelID;

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                var jsonSettings = new TempJsonSettingsManager();

                if (!string.IsNullOrWhiteSpace(jsonSettings.ClientID) &&
                    !string.IsNullOrWhiteSpace(jsonSettings.TenantID) &&
                    !string.IsNullOrWhiteSpace(jsonSettings.ClientSecret) &&
                    jsonSettings.SignatureChannelID >= 0)
                {
                    clientId = jsonSettings.ClientID;
                    tenantId = jsonSettings.TenantID;
                    clientSecret = jsonSettings.ClientSecret;
                    signatureChannelID = jsonSettings.SignatureChannelID;

                    if (await CanConnectToMicrosoft365(clientId, tenantId, clientSecret))
                    {
                        return true;
                    }

                    
                    Properties.Settings.Default.Save();
                    return false;
                }

                indeterminateToastNotification("Verbinden...", "", "Remote-Einrichtung");
                Task.Delay(4000).Wait();

                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        string url = "https://sm2license.vegilife.ch/?apiKey=LSDKJFGHSDLKJFGHLDSKFJGH09438UT2OREWHGJ";
                        string json = client.GetStringAsync(url).GetAwaiter().GetResult();
                        var remoteData = System.Text.Json.JsonSerializer.Deserialize<LicenseResponse>(json);

                        if (remoteData != null &&
                            !string.IsNullOrWhiteSpace(remoteData.clientId) &&
                            !string.IsNullOrWhiteSpace(remoteData.tenantId) &&
                            !string.IsNullOrWhiteSpace(remoteData.clientSecret))
                        {
                            updateIndeterminateToastNotification("Lizensieren...", "");
                            Task.Delay(1000).Wait();

                            updateIndeterminateToastNotification("Einstellungen installieren...", "");
                            Properties.Settings.Default.ClientID = remoteData.clientId;
                            Properties.Settings.Default.TenantID = remoteData.tenantId;
                            Properties.Settings.Default.ClientSecret = remoteData.clientSecret;
                            Properties.Settings.Default.LicenseName = remoteData.licensedOrganisationName;

                            Properties.Settings.Default.SetupCompleted = true;

                            Properties.Settings.Default.Save();
                            Task.Delay(3000).Wait();

                            updateIndeterminateToastNotification("Überprüfen...", "");
                            Task.Delay(2000).Wait();
                            ToastNotificationManagerCompat.History.Remove("ProcessToast", "SignatureUpdateProcess");

                            clientId = remoteData.clientId;
                            tenantId = remoteData.tenantId;
                            clientSecret = remoteData.clientSecret;

                            if (await CanConnectToMicrosoft365(clientId, tenantId, clientSecret))
                            {
                                Properties.Settings.Default.ShowProcessNotification = true;
                                return true;
                            }

                            errorToastNotification("Verbindung zu Microsoft 365 fehlgeschlagen", "Bitte überprüfen Sie die Lizenzdaten.");
                            Properties.Settings.Default.SetupCompleted = false;
                            Properties.Settings.Default.Save();
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    updateIndeterminateToastNotification("Fehlgeschlagen", "");
                    MessageBox.Show("Fehler bei der Remote-Einrichtung: " + ex.Message, "Fehler - Signatur Verwaltung", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Properties.Settings.Default.SetupCompleted = false;
                    Properties.Settings.Default.Save();
                    return false;
                }

                return false;
            }

            if (!await CanConnectToMicrosoft365(clientId, tenantId, clientSecret))
            {
                errorToastNotification("Verbindung zu Microsoft 365 fehlgeschlagen", "Bitte überprüfen Sie die Lizenzdaten.");
                Properties.Settings.Default.SetupCompleted = false;
                Properties.Settings.Default.Save();
                return false;
            }

            Properties.Settings.Default.SetupCompleted = true;
            Properties.Settings.Default.Save();
            return true;
        }






        private async void Initialize()
        {
            Properties.Settings.Default.ShowProcessNotification = false; //IMPORTANT FOR #SMI838 - UPDATE LOOP BUG

            if (!await CheckInternetConnection())
            {
                //errorToastNotification("Signaturenaktualisierung fehlgeschlagen", "Bitte stellen Sie sicher, dass Sie bei der nächsten Anmeldung mit dem Internet verbunden sind.");
                return;
            }
            var graphClient = GetAuthenticatedGraphClient();
            var site = await graphClient.Sites.GetByPath("sites/IT9", "vegilifeag966.sharepoint.com").Request().GetAsync();
            siteId = site.Id;

            await RegisterDeviceIfNotExists();
            UpdateSharePointListItem("idle", "local");

            var listItems = await graphClient.Sites[siteId]
                .Lists["Devices"]
                .Items
                .Request()
                .Expand("fields")
                .GetAsync();

            var currentDeviceName = Environment.MachineName;

            var matchingItem = listItems.CurrentPage.FirstOrDefault(item =>
                item.Fields != null &&
                item.Fields.AdditionalData != null &&
                item.Fields.AdditionalData.ContainsKey("DeviceName") &&
                item.Fields.AdditionalData["DeviceName"]?.ToString().Equals(currentDeviceName, StringComparison.OrdinalIgnoreCase) == true &&
                item.Fields.AdditionalData.TryGetValue("Status", out var statusObj) &&
                statusObj?.ToString().Equals("reset", StringComparison.OrdinalIgnoreCase) == true
            );

            if (matchingItem != null)
            {
                Properties.Settings.Default.ClientID = "";
                Properties.Settings.Default.ClientSecret = "";
                Properties.Settings.Default.TenantID = "";
                Properties.Settings.Default.SignatureChannelID = -1;
                Properties.Settings.Default.Save();

                try
                {
                    if (System.IO.File.Exists(FilePath))
                    {
                        System.IO.File.Delete(FilePath);
                        Trace.WriteLine($"Settings-Datei '{FilePath}' wurde gelöscht (wegen Reset).");
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Fehler beim Löschen der Settings-Datei '{FilePath}': {ex.Message}");
                }

                UpdateSharePointListItem("idle", "local");

                await Task.Delay(2000);
                Initialize(); 
                return;
            }
            await BackupAndUpdateSignatures();
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
                if (isRemoteUpdate == true)
                {
                    notifyIcon1.Text = "Signature Manager: Aktualisieren... - Von Ihrer Organisation angefordert.";
                    if (Properties.Settings.Default.ShowProcessNotification == true)
                    {
                        indeterminateToastNotification("Signature Manager: Vorbereiten... - Von Ihrer Organisation angefordert.", "", "Signaturen werden aktualisiert...");
                        lastRemoteUpdate = DateTime.Now;
                    }
                }
                else
                {
                    notifyIcon1.Text = "Signature Manager: Aktualisieren...";
                    if (Properties.Settings.Default.ShowProcessNotification == true)
                    {
                        indeterminateToastNotification("Vorbereiten...", "", "Signaturen werden aktualisiert...");
                    }
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

                    if (isRemoteUpdate == true)
                    {
                        var formattedDate = FormatDateTime(lastRemoteUpdate ?? DateTime.Now);
                        notifyIcon1.Text = $"Signature Manager: Auf dem neusten Stand - Letzte Aktualisierung: {formattedDate}";
                        if (Properties.Settings.Default.ShowProcessNotification == true)
                        {
                            updateIndeterminateToastNotification("Abgeschlossen - Von Ihrer Organisation angefordert.", "");
                        }
                    }
                    else
                    {
                        var formattedDate = FormatDateTime(lastRemoteUpdate ?? DateTime.Now);
                        notifyIcon1.Text = $"Signature Manager: Auf dem neusten Stand - Letzte Aktualisierung: {formattedDate}";
                        if (Properties.Settings.Default.ShowProcessNotification == true)
                        {
                            updateIndeterminateToastNotification("Abgeschlossen", "");
                            Properties.Settings.Default.ShowProcessNotification = false;
                        }
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
                        if (isRemoteUpdate == true)
                        {
                            notifyIcon1.Text = "Signature Manager: Aktualisieren... - Von Ihrer Organisation angefordert.";
                            if (Properties.Settings.Default.ShowProcessNotification == true)
                            {
                                updateIndeterminateToastNotification("Aktualisieren... - Von Ihrer Organisation angefordert.", displayFileName); // Update toast notification dynamically
                            }
                        }
                        else
                        {
                            notifyIcon1.Text = "Signature Manager: Aktualisieren...";
                            if (Properties.Settings.Default.ShowProcessNotification == true)
                            {
                                updateIndeterminateToastNotification("Aktualisieren...", displayFileName); // Update toast notification dynamically
                            }
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


        private void indeterminateToastNotification(string processState, string processTitle, string toastTitle)
        {
            var toastContent = new ToastContentBuilder()
            .SetToastDuration(ToastDuration.Long)
            .AddText(toastTitle)
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

                // Liste der Properties, die NICHT gespeichert werden sollen
                var ignoredSettings = new HashSet<string>
        {
            "SetupCompleted",
            "ShowProcessNotification",
        };

                var settingsDict = new Dictionary<string, object>();

                foreach (System.Configuration.SettingsProperty property in Properties.Settings.Default.Properties)
                {
                    string name = property.Name;

                    if (ignoredSettings.Contains(name))
                        continue;

                    var value = Properties.Settings.Default[name];
                    settingsDict[name] = value;
                }

                string json = System.Text.Json.JsonSerializer.Serialize(
                    settingsDict,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                );

                System.IO.File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Fehler beim Exportieren der Einstellungen: {ex.Message}");
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

        private string FormatDateTime(DateTime lastUpdateTime)
        {
            var now = DateTime.Now;
            var timeString = lastUpdateTime.ToString("HH:mm");

            if (lastUpdateTime.Date == now.Date)
            {
                return timeString; // Gleicher Tag
            }
            else if (lastUpdateTime.Date == now.AddDays(-1).Date)
            {
                return $"Gestern um {timeString}"; // Gestern
            }
            else if (lastUpdateTime.Date == now.AddDays(-2).Date)
            {
                return $"Vorgestern um {timeString}"; // Vorgestern
            }
            else
            {
                return lastUpdateTime.ToString("dd.MM.yyyy 'um' HH:mm"); // Weiter zurück
            }
        }

        private async Task<bool> CanConnectToMicrosoft365(string clientId, string tenantId, string clientSecret)
        {
            try
            {
                var scopes = new[] { "https://graph.microsoft.com/.default" };

                var confidentialClient = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithTenantId(tenantId)
                    .WithClientSecret(clientSecret)
                    .Build();

                var authProvider = new DelegateAuthenticationProvider(async (requestMessage) =>
                {
                    var result = await confidentialClient
                        .AcquireTokenForClient(scopes)
                        .ExecuteAsync();

                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                });

                var graphClient = new GraphServiceClient(authProvider);

                var result = await graphClient.Users.Request().Top(1).GetAsync();
                return result != null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Microsoft 365 Verbindung fehlgeschlagen: " + ex.Message);
                return false;
            }
        }
    }
}

