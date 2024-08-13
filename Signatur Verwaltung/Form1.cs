using System;
using System.Diagnostics; // For Debug.Print
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;

namespace Signatur_Verwaltung
{
    public partial class Form1 : Form
    {
        private static string clientId = Properties.Settings.Default.ClientID;
        private static string tenantId = Properties.Settings.Default.TenantID;
        private static string clientSecret = Properties.Settings.Default.ClientSecret;
        private static string errorCode = "";
        private System.Windows.Forms.Timer shutdownTimer;
        private bool wasOutlookRunning = false;

        public Form1()
        {
            InitializeComponent();
            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            int x = workingArea.Right - this.Width;
            int y = workingArea.Bottom - this.Height;
            this.Location = new Point(x, y);

            if (!CheckInternetConnection())
            {
                ShowToastNotification("Signaturenaktualisierung fehlgeschlagen", "Bitte stellen Sie sicher, dass Sie bei der nächsten Anmeldung mit dem Internet verbunden sind.\n\nFür weitere Informationen wenden Sie sich bitte an Ihren Systemadministrator.\n(Fehlercode: NOINET)");
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
                else
                {
                    System.Windows.Forms.Application.Exit();
                }
            }

            await BackupAndUpdateSignatures();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Timer stoppen, wenn die Einstellungen geöffnet werden
            StopShutdownTimer();
            Password passwordForm = new Password();
            passwordForm.ShowDialog();
        }

        private bool CheckInternetConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("sharepoint.microsoft.com");
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
            // Attempt to locate OUTLOOK.EXE
            string[] possiblePaths = {
                @"C:\Program Files\Microsoft Office\root\Office16\OUTLOOK.EXE",
                @"C:\Program Files (x86)\Microsoft Office\root\Office16\OUTLOOK.EXE",
                @"C:\Program Files\Microsoft Office\Office15\OUTLOOK.EXE",
                @"C:\Program Files (x86)\Microsoft Office\Office15\OUTLOOK.EXE",
                // Add other possible paths here
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

                Debug.WriteLine($"Site ID: {siteId}");
                Debug.WriteLine($"Drive ID: {driveId}");

                // Zugriff auf den Root-Ordner
                Debug.WriteLine("Listing all items in the root folder...");
                var rootItems = await graphClient.Sites[siteId].Drives[driveId].Root.Children.Request().GetAsync();

                string signaturesFolderId = null;

                foreach (var item in rootItems)
                {
                    Debug.WriteLine($"Item name: {item.Name}, Item ID: {item.Id}");
                    if (item.Name == "General" && item.Folder != null)
                    {
                        // Zugriff auf den General-Ordner
                        var generalItems = await graphClient.Sites[siteId].Drives[driveId].Items[item.Id].Children.Request().GetAsync();

                        foreach (var generalItem in generalItems)
                        {
                            Debug.WriteLine($"Item name: {generalItem.Name}, Item ID: {generalItem.Id}");
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
                    Debug.WriteLine($"Signatures folder ID: {signaturesFolderId}");
                    await NavigateToLiveSyncFolder(graphClient, siteId, driveId, signaturesFolderId, userDownloadFolder);
                    StartShutdownTimer();
                }
                else
                {
                    MessageBox.Show("Signatures folder not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Debug.WriteLine("Signatures folder not found.");
                }
            }
            catch (MsalServiceException msalEx)
            {
                Debug.WriteLine($"Authentication failed: {msalEx.Message}");
                Debug.WriteLine($"Error Code: {msalEx.ErrorCode}");
                Debug.WriteLine($"Correlation ID: {msalEx.CorrelationId}");
                Debug.WriteLine($"Exception: {msalEx}");
                MessageBox.Show($"Authentication failed: {msalEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (ServiceException graphEx)
            {
                Debug.WriteLine($"Graph API error: {graphEx.Message}");
                Debug.WriteLine($"Error Code: {graphEx.Error.Code}");
                Debug.WriteLine($"Exception: {graphEx}");
                MessageBox.Show($"Graph API error: {graphEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
                Debug.WriteLine($"Exception: {ex}");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Start Outlook if it was previously running
            if (wasOutlookRunning)
            {
                StartOutlook();
            }
        }

        private async Task NavigateToLiveSyncFolder(GraphServiceClient graphClient, string siteId, string driveId, string signaturesFolderId, string userDownloadFolder)
        {
            var items = await graphClient.Sites[siteId].Drives[driveId].Items[signaturesFolderId].Children.Request().GetAsync();

            foreach (var item in items)
            {
                Debug.WriteLine($"Item name: {item.Name}, Item ID: {item.Id}");
                if (item.Name == "LiveSync" && item.Folder != null)
                {
                    Debug.WriteLine("Listing items in LiveSync folder...");
                    await NavigateToWindowsFolder(graphClient, siteId, driveId, item.Id, userDownloadFolder);
                    break;
                }
            }
        }

        private async Task NavigateToWindowsFolder(GraphServiceClient graphClient, string siteId, string driveId, string liveSyncFolderId, string userDownloadFolder)
        {
            var items = await graphClient.Sites[siteId].Drives[driveId].Items[liveSyncFolderId].Children.Request().GetAsync();

            foreach (var item in items)
            {
                Debug.WriteLine($"Item name: {item.Name}, Item ID: {item.Id}");
                if (item.Name == "Windows" && item.Folder != null)
                {
                    Debug.WriteLine("Listing items in Windows folder...");
                    await NavigateToMarcelBourquinFolder(graphClient, siteId, driveId, item.Id, userDownloadFolder);
                    break;
                }
            }
        }

        private async Task NavigateToMarcelBourquinFolder(GraphServiceClient graphClient, string siteId, string driveId, string windowsFolderId, string userDownloadFolder)
        {
            Debug.WriteLine(Properties.Settings.Default.SignatureChannelID);
            string Item = "";
            if (Properties.Settings.Default.SignatureChannelID == 0)
            {
                Item = "Marcel Bourquin";
            }
            else if (Properties.Settings.Default.SignatureChannelID == 1)
            {
                Item = "Nadine Dahinden";
            }

            var items = await graphClient.Sites[siteId].Drives[driveId].Items[windowsFolderId].Children.Request().GetAsync();

            foreach (var item in items)
            {
                Debug.WriteLine($"Item name: {item.Name}, Item ID: {item.Id}");
                if (item.Name == Item && item.Folder != null)
                {
                    Debug.WriteLine("Listing items in " + Item + " folder...");
                    await DownloadFilesFromFolder(graphClient, siteId, driveId, item.Id, userDownloadFolder);
                    break;
                }
            }
        }

        private async Task DownloadFilesFromFolder(GraphServiceClient graphClient, string siteId, string driveId, string folderId, string userDownloadFolder)
        {
            var items = await graphClient.Sites[siteId].Drives[driveId].Items[folderId].Children.Request().GetAsync();

            foreach (var item in items)
            {
                Debug.WriteLine($"Item name: {item.Name}, Item ID: {item.Id}");

                if (item.Folder != null)
                {
                    // Unterordner erstellen
                    var subFolderPath = Path.Combine(userDownloadFolder, item.Name);
                    if (!System.IO.Directory.Exists(subFolderPath))
                    {
                        System.IO.Directory.CreateDirectory(subFolderPath);
                    }

                    // Rekursiv weiter in den Ordner gehen
                    await DownloadFilesFromFolder(graphClient, siteId, driveId, item.Id, subFolderPath);
                }
                else if (item.File != null)
                {
                    var fileName = ExtractFileName(item.Name);

                    // Datei herunterladen
                    if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".thmx", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        SetStatusLabel($"Signaturen werden aktualisiert... ({fileName})");
                    }

                    var fileContent = await graphClient.Sites[siteId].Drives[driveId].Items[item.Id].Content.Request().GetAsync();
                    var filePath = Path.Combine(userDownloadFolder, item.Name);

                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await fileContent.CopyToAsync(fileStream);
                    }

                    Debug.WriteLine($"Downloaded file: {filePath}");
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

        private void SetStatusLabel(string text)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.Invoke(new Action(() => statusLabel.Text = text));
            }
            else
            {
                statusLabel.Text = text;
            }
        }

        private void StartShutdownTimer()
        {
            shutdownTimer = new System.Windows.Forms.Timer();
            shutdownTimer.Interval = 5000; // 5 Sekunden
            shutdownTimer.Tick += (sender, e) =>
            {
                shutdownTimer.Stop();
                shutdownTimer.Dispose();
                Debug.WriteLine("Shutting down application...");
                System.Windows.Forms.Application.Exit();
            };
            shutdownTimer.Start();
        }

        private void StopShutdownTimer()
        {
            if (shutdownTimer != null)
            {
                shutdownTimer.Stop();
                shutdownTimer.Dispose();
                shutdownTimer = null;
                Debug.WriteLine("Shutdown timer stopped.");
            }
        }

        static string GetOperatingSystemInfo()
        {
            var os = Environment.OSVersion;
            return $"{os.VersionString}";
        }

        static string GetWindowsUpdateInfo()
        {
            string updateInfo = "Unknown";

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        object buildLabEx = key.GetValue("BuildLabEx");
                        if (buildLabEx != null)
                        {
                            string buildLabExStr = buildLabEx.ToString();
                            updateInfo = buildLabExStr.Split('.')[0];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving Windows Update info: {ex.Message}");
            }

            return updateInfo;
        }

        static string GetProcessorInfo()
        {
            string processorInfo = "Unknown";

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        processorInfo = obj["Name"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving processor info: {ex.Message}");
            }

            return processorInfo;
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

        // TOAST NOTIFICATION //
        private void ShowToastNotification(string title, string message)
        {
            string criticalSoundPath = "file:///C:\\Windows\\Media\\Windows Foreground.wav";
            string emailAddress = "support@yourcompany.com";
            string subject = Uri.EscapeDataString("Fehler bei der Signaturenaktualisierung");
            string body = Uri.EscapeDataString($"Bitte geben Sie eine Beschreibung des Problems an.\n\nFehlercode: NOINET");
            string mailtoUri = $"mailto:{emailAddress}?subject={subject}&body={body}";

            new ToastContentBuilder()
                .AddArgument("action", "viewConversation")
                .AddArgument("conversationId", 9813)
                .AddText(title)
                .AddText(message)
                .AddAudio(new Uri(criticalSoundPath))
                .AddButton(new ToastButton()
                    .SetContent("Fehler Melden")
                    .AddArgument("action", "mailto")
                    .SetBackgroundActivation())
                .SetToastDuration(ToastDuration.Long)
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTimeOffset.MaxValue;
                });
        }

        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            ToastArguments args = ToastArguments.Parse(e.Argument);
            if (args["action"] == "mailto")
            {
                string emailAddress = "admin@vegilife.ch";
                string subject = Uri.EscapeDataString("Fehler bei der Signaturenaktualisierung");
                string body = Uri.EscapeDataString($"Feherlauszug von SX.SignatureManager.Core:\n\nSystem: {GetOperatingSystemInfo()}\nUpdate: {GetWindowsUpdateInfo()}\nChip: {GetProcessorInfo()}\nBenutzer: {Environment.UserName}\nFehlercode: " + errorCode + " - DDCE");
                string mailtoUri = $"mailto:{emailAddress}?subject={subject}&body={body}";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(mailtoUri) { UseShellExecute = true });
                Debug.Print("Fehler Melden button clicked - Mailto link opened");
                System.Windows.Forms.Application.Exit();
            }
        }
    }
}
