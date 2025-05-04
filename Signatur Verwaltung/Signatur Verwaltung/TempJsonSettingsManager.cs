using System;
using System.IO;
using System.Text.Json;

namespace Signatur_Verwaltung
{
    public class TempJsonSettingsManager
    {
        private class JsonAppSettings
        {
            public string ClientID { get; set; }
            public string TenantID { get; set; }
            public string ClientSecret { get; set; }
            public int SignatureChannelID { get; set; }
            public bool ShowProcessNotification { get; set; }
        }

        private readonly string settingsPath;
        private JsonAppSettings settings;

        public TempJsonSettingsManager()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "SignatureManager");
            Directory.CreateDirectory(tempPath);
            settingsPath = Path.Combine(tempPath, "settings.json");
            Load();
        }

        private void Load()
        {
            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath);
                settings = JsonSerializer.Deserialize<JsonAppSettings>(json);
            }
            else
            {
                settings = new JsonAppSettings();
            }
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }

        public string ClientID
        {
            get => settings.ClientID;
            set => settings.ClientID = value;
        }

        public string TenantID
        {
            get => settings.TenantID;
            set => settings.TenantID = value;
        }

        public string ClientSecret
        {
            get => settings.ClientSecret;
            set => settings.ClientSecret = value;
        }

        public int SignatureChannelID
        {
            get => settings.SignatureChannelID;
            set => settings.SignatureChannelID = value;
        }

        public bool ShowProcessNotification
        {
            get => settings.ShowProcessNotification;
            set => settings.ShowProcessNotification = value;
        }

        public void Reset()
        {
            settings = new JsonAppSettings();
            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }
        }
    }
}
