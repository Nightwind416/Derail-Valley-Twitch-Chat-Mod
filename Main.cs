using System;
// using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
// using System.Xml;
using DV.UIFramework;
using UnityEngine;
using UnityModManagerNet;
// using System.Xml.Serialization;
using System.Reflection;

namespace TwitchChat
{
    public static class Main
    {
        private static bool _isEnabled;
        public static bool _dispatcherModDetected;
        public static UnityModManager.ModEntry ModEntry { get; private set; } = null!;
        // public static Settings Settings { get; set; } = null!;
        public static HttpClient httpClient = new();
        public static string settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "Settings.xml");
        private static string debugLog = string.Empty;
        private static string messageLog = string.Empty;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
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
            Settings.Instance = UnityModManager.ModSettings.Load<Settings>(modEntry);
            
            // Register the mod's GUI methods
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            // Initialize the log files
            InitializeLogFiles();

            // Connect to WebSocket
            // _ = TwitchEventHandler.ConnectToWebSocket();    

            ModEntry.Logger.Log("[Load] Load method completed successfully.");
            LogEntry(methodName, "Load method completed successfullyy.");
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Settings.Instance.DrawButtons();
            Settings.Instance.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Settings.Instance.Save(modEntry);
        }

        private static bool OnToggle(UnityModManager.ModEntry _, bool isEnabled)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            if (isEnabled)
            {
                _isEnabled = true;
                // TimedMessages();
                AttachNotification("TwitchChatMod Notifications Enabled.", "null");
                LogEntry(methodName, "Mod Enabled!");
                ModEntry.Enabled = true;
            }
            else
            {
                _isEnabled = false;
                AttachNotification("TwitchChatMod Notifications Disabled.", "null");
                LogEntry(methodName, "Mod Disabled!");
                ModEntry.Enabled = false;
            }

            return true;
        }

        private static void OnUpdate(UnityModManager.ModEntry mod, float delta)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (_isEnabled)
                {
                    Settings.Instance.Update();
                }
            }
            catch (Exception e)
            {
                LogEntry(methodName, $"Exception: {e}");
            }
        }

        // public static bool ApplySettingsFromFile()
        // {
        //     string methodName = MethodBase.GetCurrentMethod().Name;
        //     LogEntry(methodName, "Attempting to apply settings from file.");
        
        //     // Check if the settings file exists
        //     if (!File.Exists(settingsFile))
        //     {
        //         LogEntry(methodName, "Settings file not found.");
        //         return false;
        //     }
        
        //     try
        //     {
        //         // Deserialize the settings from the XML file
        //         var serializer = new XmlSerializer(typeof(Settings));
        //         Settings applySettings;
        //         using (var fs = new FileStream(settingsFile, FileMode.Open))
        //         {
        //             applySettings = (Settings)serializer.Deserialize(fs);
        //         }
        
        //         // Access static members using the class name
        //         Settings.twitchUsername = applySettings.twitchUsername;
        //         Settings.twitchChannel = applySettings.twitchChannel;
        //         Settings.client_id = applySettings.client_id;
        //         Settings.client_secret = applySettings.client_secret;
        //         Settings.manual_token = applySettings.manual_token;
        
        //     }
        //     catch (Exception ex)
        //     {
        //         LogEntry(methodName, $"Error applying settings from file: {ex.Message}");
        //         return false;
        //     }
        
        //     return true;
        // }

        // public static void ReadSettingsFromFile()
        // {
        //     string methodName = MethodBase.GetCurrentMethod().Name;
        //     LogEntry(methodName, "Attempting to read settings from file.");
        
        //     // Check if the settings file exists
        //     if (!File.Exists(settingsFile))
        //     {
        //         LogEntry(methodName, "Settings file not found.");
        //         return;
        //     }
        
        //     try
        //     {
        //         // Deserialize the settings from the XML file
        //         XmlSerializer settingsSerializer = new(typeof(Settings));
        //         Settings readSettings;
        //         using (FileStream settingsFileStream = new(settingsFile, FileMode.Open))
        //         {
        //             readSettings = (Settings)settingsSerializer.Deserialize(settingsFileStream);
        //         }
        
        //         // Log the settings from the file
        //         LogEntry(methodName, $"Settings File read successfully:");
        //         LogEntry(methodName, $"Twitch Username: {readSettings.twitchUsername}");
        //         LogEntry(methodName, $"Twitch Channel: {readSettings.twitchChannel}");
        //         LogEntry(methodName, $"Client ID: {readSettings.client_id}");
        //         LogEntry(methodName, $"Client Secret: {readSettings.client_secret}");
        //         LogEntry(methodName, $"Manual Token: {readSettings.manual_token}");
        //     }
        //     catch (Exception ex)
        //     {
        //         LogEntry(methodName, $"Error reading settings file: {ex.Message}");
        //     }
        // }

        // public static void PrintCurrentSettings()
        // {
        //     string methodName = MethodBase.GetCurrentMethod().Name;
        //     LogEntry(methodName, $"Current settings:");
        //     LogEntry(methodName, $"Twitch Username: {Settings.twitchUsername}");
        //     LogEntry(methodName, $"Twitch Channel: {Settings.twitchChannel}");
        //     LogEntry(methodName, $"Client ID: {Settings.client_id}");
        //     LogEntry(methodName, $"Client Secret: {Settings.client_secret}");
        //     LogEntry(methodName, $"Manual Token: {Settings.manual_token}");
        // }

        private static void InitializeLogFiles()
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "Logs");
            string date = DateTime.Now.ToString("yyyy-MM-dd");

            // Create the directory if it does not exist
            if (!Directory.Exists(logDirectory))
            {
                try
                {
                    Directory.CreateDirectory(logDirectory);
                    ModEntry.Logger.Log($"Created directory: {logDirectory}");
                }
                catch (Exception ex)
                {
                    ModEntry.Logger.Log($"Failed to create directory: {logDirectory}. Exception: {ex.Message}");
                    return;
                }
            }

            debugLog = Path.Combine(logDirectory, $"Log - {date}.txt");
            messageLog = Path.Combine(logDirectory, $"Messages - {date}.txt");

            ModEntry.Logger.Log($"Initialized log file: {debugLog}");
            ModEntry.Logger.Log($"Initialized message file: {messageLog}");

            // Open each of the logs and write a new header (not using logEntry because it will write to the log file)
            using StreamWriter logHeader = new(debugLog, true);
            logHeader.WriteLine($"Log file initialized on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            using StreamWriter messageHeader = new(messageLog, true);
            messageHeader.WriteLine($"Message file initialized on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            ModEntry.Logger.Log("Log files initialized.");
            
        }
        
        public static async Task GetOathToken()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            LogEntry(methodName, "Initializing Twitch connection...");
        
            try
            {
                string clientId = WebSocketClient.client_id;
                string redirectUri = "https://localhost:3000/";
                string scope = "chat:edit chat:read channel:bot channel:manage:broadcast channel:moderate channel:read:subscriptions user:read:chat user:read:subscriptions user:write:chat user:read:email";
                string state = Guid.NewGuid().ToString();
        
                string authorizationUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={clientId}&redirect_uri={redirectUri}&scope={Uri.EscapeDataString(scope)}&state={state}";
                LogEntry(methodName, $"Authorization URL: {authorizationUrl}");
        
                // Open the authorization URL in the default web browser
                await Task.Run(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authorizationUrl,
                    UseShellExecute = true
                }));
        
                LogEntry(methodName, "Opened Twitch authorization URL in the default web browser.");
        
                // Start a local HTTP listener to capture the response
                HttpListener listener = new();
                listener.Prefixes.Add(redirectUri);
                listener.Start();
                LogEntry(methodName, "Waiting for Twitch authorization response...");

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
                    LogEntry(methodName, $"Received response: {responseUrl}");
                }
                catch (OperationCanceledException)
                {
                    LogEntry(methodName, "Authorization response timed out.");
                }
                finally
                {
                    listener.Stop();
                }
            }
            catch (Exception ex)
            {
                LogEntry(methodName, $"Failed to connect to Twitch: {ex.Message}");
            }
        }

        // public static void DisconnectFromTwitch()
        // {
        //     string methodName = MethodBase.GetCurrentMethod().Name;
        //     if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
        //     {
        //         httpClient.DefaultRequestHeaders.Remove("Authorization");
        //         LogEntry(methodName, "Disconnected from Twitch.");
        //     }
        //     else
        //     {
        //         LogEntry(methodName, "Already disconnected from Twitch.");
        //         return;
        //     }
        // }

        private static void AttachNotification(string displayed_text, string object_name)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            // Find the object_name GameObject in the scene
            GameObject found_object = GameObject.Find(object_name);
        
            // Find NotificationManager in the scene
            NotificationManager notificationManager = UnityEngine.Object.FindObjectOfType<NotificationManager>();
        
            if (notificationManager == null)
            {
                LogEntry(methodName, "NotificationManager not found in the scene.");
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
                LogEntry(methodName, $"Error showing notification and text: {displayed_text}\nException: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

        // private static void TimedMessages()
        // {
        //     string methodName = MethodBase.GetCurrentMethod().Name;
        //     try
        //     {
        //         // Check if the file exists
        //         if (!File.Exists(settingsFile))
        //         {
        //             LogEntry(methodName, $"Settings file not found: {settingsFile}");
        //             return;
        //         }

        //         // Load the XML document
        //         var xmlDoc = new XmlDocument();
        //         xmlDoc.Load(settingsFile);

        //         // Extract messages and their timers
        //         var messages = new Dictionary<string, int>();

        //         for (int i = 1; i <= 10; i++)
        //         {
        //             string activeKey = $"timedMessage{i}Toggle";
        //             string messageKey = $"timedMessage{i}";
        //             string timerKey = $"{messageKey}Timer";
                
        //             var activeNode = xmlDoc.SelectSingleNode($"//Settings/{activeKey}");
        //             var messageNode = xmlDoc.SelectSingleNode($"//Settings/{messageKey}");
        //             var timerNode = xmlDoc.SelectSingleNode($"//Settings/{timerKey}");
                
        //             bool isActive = activeNode != null && bool.TryParse(activeNode.InnerText, out bool active) && active;
                
        //             if (isActive && messageNode != null && timerNode != null && int.TryParse(timerNode.InnerText, out int timerValue) && timerValue > 0)
        //             {
        //                 messages.Add(messageNode.InnerText, timerValue);
        //                 LogEntry(methodName, $"Added message: {messageNode.InnerText} with timer: {timerValue}");
        //             }
        //             else
        //             {
        //                 LogEntry(methodName, $"Skipped adding message: {messageKey} with timer: +{timerKey}");
        //             }
        //         }

        //         // Create and start timers for each message
        //         foreach (var message in messages)
        //         {
        //             System.Timers.Timer message_timer = new(message.Value * 1000); // Convert seconds to milliseconds
        //             message_timer.Elapsed += (source, e) => SendMessageToTwitch(message.Key);
        //             message_timer.AutoReset = true;
        //             message_timer.Enabled = true;
        //             LogEntry(methodName, $"Timer set for message: {message.Key} with interval: {message.Value * 1000} ms");
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         LogEntry(methodName, $"An error occurred: {ex.Message}");
        //     }
        // }

        // private static async void SendMessageToTwitch(string message)
        // {
        //     string methodName = MethodBase.GetCurrentMethod().Name;

        //     if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
        //     {
        //         var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { content = message }), Encoding.UTF8, "application/json");
        //         var response = await httpClient.PostAsync($"https://api.twitch.tv/helix/chat/messages?broadcaster_id={Settings.twitchChannel}", content);

        //         if (response.IsSuccessStatusCode)
        //         {
        //             LogEntry(methodName, $"{message}");
        //         }
        //         else
        //         {
        //             LogEntry(methodName, $"Failed to send message: {response.ReasonPhrase}");
        //         }
        //     }
        //     else
        //     {
        //         LogEntry(methodName, "Unable to send message.");
        //     }
        // }

        public static void LogEntry(string source, string message)
        {
            string selected_log = (source == "ReceivedMessage" || source == "SentMessage") ? messageLog : debugLog;
        
            if (string.IsNullOrEmpty(selected_log))
            {
                ModEntry.Logger.Log($"[{source}] Log file path is not initialized.");
                return;
            }
        
            int retryCount = 3;
            int delay = 1000; // 1 second
        
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    using StreamWriter writer = new(selected_log, true);
                    string logMessage = selected_log == debugLog ? $"[{source}] {message}" : message;
                    writer.WriteLine($"{DateTime.Now:HH:mm}: {logMessage}");
                    return;
                }
                catch (IOException ex) when (i < retryCount - 1)
                {
                    ModEntry.Logger.Log($"[{source}] Failed to write to log file: {selected_log}. Exception: {ex.Message}. Retrying in {delay}ms...");
                    Thread.Sleep(delay);
                }
                catch (Exception ex)
                {
                    ModEntry.Logger.Log($"[{source}] Failed to write to log file: {selected_log}. Exception: {ex.Message}");
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