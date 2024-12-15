using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;
using DV.UIFramework;
using UnityEngine;
using UnityModManagerNet;
using System.Xml.Serialization;
using System.Net;

namespace TwitchChat
{
    public static class Main
    {
        private static bool _isEnabled;
        public static bool _dispatcherModDetected;
        public static UnityModManager.ModEntry ModEntry { get; private set; } = null!;
        public static Settings Settings { get; set; } = null!;
        public static HttpClient httpClient = new();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;

            ModEntry.Logger.Log("[Load] Load method started.");

            // Initialize UnityMainThreadDispatcher
            GameObject dispatcherObject = new("UnityMainThreadDispatcher");
            dispatcherObject.AddComponent<UnityMainThreadDispatcher>();
            UnityEngine.Object.DontDestroyOnLoad(dispatcherObject);
            ModEntry.Logger.Log("[Load] UnityMainThreadDispatcher initialized.");

            // Check for RemoteDispatch Mod
            var remoteDispatchMod = UnityModManager.modEntries.FirstOrDefault(mod => mod.Info.Id == "RemoteDispatch");
            if (remoteDispatchMod != null)
            {
                ModEntry.Logger.Log("[Load] RemoteDispatch Mod detected.");
                _dispatcherModDetected = true;
            }
            else
            {
                ModEntry.Logger.Log("[Load] RemoteDispatch Mod not detected.");
            }

            // Register the mod's toggle and update methods
            ModEntry.OnToggle = OnToggle;
            ModEntry.OnUpdate = OnUpdate;
            
            // Load the mod's settings
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            
            // Register the mod's GUI methods
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            // Load the settings
            ReadSettingsFromFile();
            ApplySettingsFromFile();   

            // Initialize the log files
            InitializeLogFiles();

            // Connect to WebSocket
            // _ = TwitchEventHandler.ConnectToWebSocket();    

            ModEntry.Logger.Log("[Load] Load method completed successfully.");
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.DrawButtons();
            Settings.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }

        private static bool OnToggle(UnityModManager.ModEntry _, bool isEnabled)
        {
            if (isEnabled)
            {
                _isEnabled = true;
                // TimedMessages();
                // TwitchEventHandler.ConnectToWebSocket().Wait();
                AttachNotification("TwitchChatMod Notifications Enabled.", "null");
                ModEntry.Logger.Log("[OnToggle] Mod Enabled!");
                ModEntry.Enabled = true;
            }
            else
            {
                _isEnabled = false;
                DisconnectFromTwitch();
                // TwitchEventHandler.DisconnectFromWebSocket().Wait();
                AttachNotification("TwitchChatMod Notifications Disabled.", "null");
                ModEntry.Logger.Log("[OnToggle] Mod Disabled!");
                ModEntry.Enabled = false;
            }

            return true;
        }

        private static void OnUpdate(UnityModManager.ModEntry mod, float delta)
        {
            try
            {
                if (_isEnabled)
                {
                    Settings.Update();
                }
            }
            catch (Exception e)
            {
                ModEntry.Logger.Log($"[OnUpdate] Exception: {e}");
            }
        }
        // private static bool IsModEnabledAndWorldReadyForInteraction()
        // {
        //     if (!_isEnabled) {
        //         return false;
        //     }
        //     if (UnityEngine.Object.FindObjectOfType<NotificationManager>() == null)
        //     {
        //         return false;
        //     }
        //     return true;
        // }

        
        public static bool ApplySettingsFromFile()
        {
            ModEntry.Logger.Log("[ApplySettings] Attempting to apply settings from file.");
        
            // Check if the settings file exists
            if (!File.Exists(Settings.settingsFilePath))
            {
                ModEntry.Logger.Log("[ApplySettings] Settings file not found.");
                return false;
            }
        
            try
            {
                // Deserialize the settings from the XML file
                var serializer = new XmlSerializer(typeof(Settings));
                Settings applySettings;
                using (var fs = new FileStream(Settings.settingsFilePath, FileMode.Open))
                {
                    applySettings = (Settings)serializer.Deserialize(fs);
                }
        
                // Access static members using the class name
                Settings.twitchUsername = applySettings.twitchUsername;
                Settings.twitchChannel = applySettings.twitchChannel;
                Settings.client_id = applySettings.client_id;
                Settings.client_secret = applySettings.client_secret;
                Settings.manual_token = applySettings.manual_token;
        
            }
            catch (Exception ex)
            {
                ModEntry.Logger.Log($"[ApplySettings] Error applying settings from file: {ex.Message}");
                return false;
            }
        
            return true;
        }

        public static void ReadSettingsFromFile()
        {
            ModEntry.Logger.Log("[ReadSettings] Attempting to read settings from file.");
        
            // Check if the settings file exists
            if (!File.Exists(Settings.settingsFilePath))
            {
                ModEntry.Logger.Log("[ReadSettings] Settings file not found.");
                return;
            }
        
            try
            {
                // Deserialize the settings from the XML file
                XmlSerializer settingsSerializer = new(typeof(Settings));
                Settings readSettings;
                using (FileStream settingsFileStream = new(Settings.settingsFilePath, FileMode.Open))
                {
                    readSettings = (Settings)settingsSerializer.Deserialize(settingsFileStream);
                }
        
                // Log the settings from the file
                ModEntry.Logger.Log($"[ReadSettings] Settings File read successfully:");
                ModEntry.Logger.Log($"[ReadSettings] Twitch Username: {readSettings.twitchUsername}");
                ModEntry.Logger.Log($"[ReadSettings] Twitch Channel: {readSettings.twitchChannel}");
                ModEntry.Logger.Log($"[ReadSettings] Client ID: {readSettings.client_id}");
                ModEntry.Logger.Log($"[ReadSettings] Client Secret: {readSettings.client_secret}");
                ModEntry.Logger.Log($"[ReadSettings] Manual Token: {readSettings.manual_token}");
            }
            catch (Exception ex)
            {
                ModEntry.Logger.Log($"[ReadSettings] Error reading settings file: {ex.Message}");
            }
        }

        public static void PrintCurrentSettings()
        {
            ModEntry.Logger.Log($"[PrintSettings] Current settings:");
            ModEntry.Logger.Log($"[PrintSettings] Twitch Username: {Settings.twitchUsername}");
            ModEntry.Logger.Log($"[PrintSettings] Twitch Channel: {Settings.twitchChannel}");
            ModEntry.Logger.Log($"[PrintSettings] Client ID: {Settings.client_id}");
            ModEntry.Logger.Log($"[PrintSettings] Client Secret: {Settings.client_secret}");
            ModEntry.Logger.Log($"[PrintSettings] Manual Token: {Settings.manual_token}");
        }

        private static void InitializeLogFiles()
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "ChatLogs");
            string date = DateTime.Now.ToString("yyyy-MM-dd");

            // Create the directory if it does not exist
            if (!Directory.Exists(logDirectory))
            {
                try
                {
                    Directory.CreateDirectory(logDirectory);
                    ModEntry.Logger.Log($"[InitLogs] Created directory: {logDirectory}");
                }
                catch (Exception ex)
                {
                    ModEntry.Logger.Log($"[InitLogs] Failed to create directory: {logDirectory}. Exception: {ex.Message}");
                    return;
                }
            }

            Settings.logFilePath = Path.Combine(logDirectory, $"Log - {date}.txt");
            Settings.messageFilePath = Path.Combine(logDirectory, $"Messages - {date}.txt");

            ModEntry.Logger.Log($"[InitLogs] Initialized log file: {Settings.logFilePath}");
            ModEntry.Logger.Log($"[InitLogs] Initialized message file: {Settings.messageFilePath}");
        }
        
        public static async Task ConnectToTwitch()
        {
            ModEntry.Logger.Log("[Connect] Initializing Twitch connection...");
        
            try
            {
                string clientId = Settings.client_id;
                string redirectUri = "https://localhost:3000/";
                string scope = "chat:edit chat:read channel:bot channel:manage:broadcast channel:moderate channel:read:subscriptions user:read:chat user:read:subscriptions user:write:chat";
                string state = Guid.NewGuid().ToString();
        
                string authorizationUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={clientId}&redirect_uri={redirectUri}&scope={Uri.EscapeDataString(scope)}&state={state}";
                ModEntry.Logger.Log($"[Connect] Authorization URL: {authorizationUrl}");
        
                // Open the authorization URL in the default web browser
                await Task.Run(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authorizationUrl,
                    UseShellExecute = true
                }));
        
                ModEntry.Logger.Log("[Connect] Opened Twitch authorization URL in the default web browser.");
        
                // Start a local HTTP listener to capture the response
                HttpListener listener = new();
                listener.Prefixes.Add(redirectUri);
                listener.Start();
                ModEntry.Logger.Log("[Connect] Waiting for Twitch authorization response...");

                CancellationTokenSource cts = new();
                cts.CancelAfter(TimeSpan.FromSeconds(60));
        
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync().WithCancellation(cts.Token);
                    string responseString = "<html><body>You can close this window now.</body></html>";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Close();

                    string responseUrl = context.Request.Url.ToString();
                    ModEntry.Logger.Log($"[Connect] Received response: {responseUrl}");
                }
                catch (OperationCanceledException)
                {
                    ModEntry.Logger.Log("[Connect] Authorization response timed out.");
                }
                finally
                {
                    listener.Stop();
                }
            }
            catch (Exception ex)
            {
                ModEntry.Logger.Log($"[Connect] Failed to connect to Twitch: {ex.Message}");
            }
        }

        public static void DisconnectFromTwitch()
        {
            if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                httpClient.DefaultRequestHeaders.Remove("Authorization");
                ModEntry.Logger.Log("[Connect] Disconnected from Twitch.");
            }
            else
            {
                ModEntry.Logger.Log("[Connect] Already disconnected from Twitch.");
                return;
            }
        }

        private static void AttachNotification(string displayed_text, string object_name)
        {
            // Find the object_name GameObject in the scene
            GameObject found_object = GameObject.Find(object_name);
        
            // Find NotificationManager in the scene
            NotificationManager notificationManager = UnityEngine.Object.FindObjectOfType<NotificationManager>();
        
            if (notificationManager == null)
            {
                ModEntry.Logger.Log("[AttachNotification] NotificationManager not found in the scene.");
                return;
            }
        
            try
            {
                // Set default duration to 15 if no value is set
                int duration = 15;

                // Display a notification, attached to the found_object if it's not null
                var notification = notificationManager.ShowNotification(
                    displayed_text,             // Text?
                    null,                       // Localization parameters?
                    duration,                   // Duration?
                    false,                      // Clear existing notifications?
                    found_object?.transform,    // Attach to GameObject if not null
                    false,                      // Localize?
                    false                       // Target UI?
                );
            }
            catch (Exception ex)
            {
                ModEntry.Logger.Log($"[AttachNotification] Error showing notification and text: {displayed_text}\nException: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

        private static void TimedMessages()
        {
            try
            {
                // Check if the file exists
                if (!File.Exists(Settings.settingsFilePath))
                {
                    ModEntry.Logger.Log($"[TimedMessages] Settings file not found: {Settings.settingsFilePath}");
                    return;
                }

                // Load the XML document
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(Settings.settingsFilePath);

                // Extract messages and their timers
                var messages = new Dictionary<string, int>();

                for (int i = 1; i <= 10; i++)
                {
                    string activeKey = $"timedMessage{i}Toggle";
                    string messageKey = $"timedMessage{i}";
                    string timerKey = $"{messageKey}Timer";
                
                    var activeNode = xmlDoc.SelectSingleNode($"//Settings/{activeKey}");
                    var messageNode = xmlDoc.SelectSingleNode($"//Settings/{messageKey}");
                    var timerNode = xmlDoc.SelectSingleNode($"//Settings/{timerKey}");
                
                    bool isActive = activeNode != null && bool.TryParse(activeNode.InnerText, out bool active) && active;
                
                    if (isActive && messageNode != null && timerNode != null && int.TryParse(timerNode.InnerText, out int timerValue) && timerValue > 0)
                    {
                        messages.Add(messageNode.InnerText, timerValue);
                        ModEntry.Logger.Log($"[TimedMessages] Added message: {messageNode.InnerText} with timer: {timerValue}");
                    }
                    else
                    {
                        ModEntry.Logger.Log($"[TimedMessages] Skipped adding message: {messageKey} with timer: +{timerKey}");
                    }
                }

                // Create and start timers for each message
                foreach (var message in messages)
                {
                    System.Timers.Timer message_timer = new(message.Value * 1000); // Convert seconds to milliseconds
                    message_timer.Elapsed += (source, e) => SendMessageToTwitch(message.Key);
                    message_timer.AutoReset = true;
                    message_timer.Enabled = true;
                    ModEntry.Logger.Log($"[TimedMessages] Timer set for message: {message.Key} with interval: {message.Value * 1000} ms");
                }
            }
            catch (Exception ex)
            {
                ModEntry.Logger.Log($"[TimedMessages] An error occurred: {ex.Message}");
            }
        }

        private static async void SendMessageToTwitch(string message)
        {
            if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { content = message }), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"https://api.twitch.tv/helix/chat/messages?broadcaster_id={Settings.twitchChannel}", content);

                if (response.IsSuccessStatusCode)
                {
                    TwitchChatMessages($"[Sent Message] {message}");
                }
                else
                {
                    ModEntry.Logger.Log($"[SendMessage] Failed to send message: {response.ReasonPhrase}");
                }
            }
            else
            {
                ModEntry.Logger.Log("[SendMessage] Unable to send message.");
            }
        }

        private static void TwitchChatLog(string message)
        {
            if (string.IsNullOrEmpty(Settings.logFilePath))
            {
                ModEntry.Logger.Log("[TwitchLog] Log file path is not initialized.");
                return;
            }

            try
            {
                using StreamWriter writer = new(Settings.logFilePath, true);
                writer.WriteLine($"{DateTime.Now:HH:mm}: {message}");
            }
            catch (Exception ex)
            {
                ModEntry.Logger.Log($"[TwitchLog] Failed to write to log file: {Settings.logFilePath}. Exception: {ex.Message}");
            }
        }

        public static void TwitchChatMessages(string message)
        {
            if (string.IsNullOrEmpty(Settings.messageFilePath))
            {
                ModEntry.Logger.Log("[MessageLog] Message file path is not initialized.");
                return;
            }

            int retryCount = 3;
            int delay = 1000; // 1 second

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    using StreamWriter writer = new(Settings.messageFilePath, true);
                    writer.WriteLine($"{DateTime.Now:HH:mm}: {message}");
                    return; // Exit the method if writing is successful
                }
                catch (IOException ex) when (i < retryCount - 1)
                {
                    ModEntry.Logger.Log($"[MessageLog] Failed to write to message file: {Settings.messageFilePath}. Exception: {ex.Message}. Retrying in {delay}ms...");
                    Thread.Sleep(delay);
                }
                catch (Exception ex)
                {
                    ModEntry.Logger.Log($"[MessageLog] Failed to write to message file: {Settings.messageFilePath}. Exception: {ex.Message}");
                    return;
                }
            }
        }
    }
}

public static class TaskExtensions
{
    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
        {
            if (task != await Task.WhenAny(task, tcs.Task))
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
        return await task;
    }
}

