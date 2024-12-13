using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using DV.UIFramework;
using UnityEngine;
using UnityModManagerNet;

namespace TwitchChat
{
    public static class Main
    {
        private static bool _isEnabled;
        public static bool _dispatcherModDetected;
        public static UnityModManager.ModEntry ModEntry { get; private set; } = null!;
        public static XmlSettings Settings { get; private set; } = null!;
        private static string logFilePath = string.Empty;
        private static string messageFilePath = string.Empty;
        private static string twitchUsername = string.Empty;
        private static string twitchToken = string.Empty;
        private static string twitchChannel = string.Empty;
        private static float messageDuration = 20;
        private static string welcomeMessage = string.Empty;
        private static string infoMessage = string.Empty;
        private static string commandMessage = string.Empty;
        private static string newFollowerMessage = string.Empty;
        private static string newSubscriberMessage = string.Empty;
        private static string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "Settings.xml");
        private static HttpClient httpClient = new();

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;

            modEntry.Logger.Log("[TwitchChat] Load method started.");

            // Initialize UnityMainThreadDispatcher
            GameObject dispatcherObject = new("UnityMainThreadDispatcher");
            dispatcherObject.AddComponent<UnityMainThreadDispatcher>();
            UnityEngine.Object.DontDestroyOnLoad(dispatcherObject);
            modEntry.Logger.Log("[TwitchChat] UnityMainThreadDispatcher initialized.");

            // Check for RemoteDispatch Mod
            var remoteDispatchMod = UnityModManager.modEntries.FirstOrDefault(mod => mod.Info.Id == "RemoteDispatch");
            if (remoteDispatchMod != null)
            {
                modEntry.Logger.Log("[TwitchChat] RemoteDispatch Mod detected.");
                _dispatcherModDetected = true;
            }
            else
            {
                modEntry.Logger.Log("[TwitchChat] RemoteDispatch Mod not detected.");
            }

            // Register the mod's toggle and update methods
            ModEntry.OnToggle = OnToggle;
            ModEntry.OnUpdate = OnUpdate;
            
            // Load the mod's settings
            Settings = UnityModManager.ModSettings.Load<XmlSettings>(modEntry);
            
            // Register the mod's GUI methods
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            // Initialize the log files
            InitializeLogFiles();
            
            // Load the settings
            LoadSettings();

            modEntry.Logger.Log("[TwitchChat] Load method completed successfully.");
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
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
                ConnectToTwitch();
                TimedMessages();
                AttachNotification("TwitchChatMod Notifications Enabled.", "null");
                UnityModManager.Logger.Log("[TwitchChat] Mod Enabled!");
                ModEntry.Enabled = true;
            }
            else
            {
                _isEnabled = false;
                DisconnectFromTwitch();
                AttachNotification("TwitchChatMod Notifications Disabled.", "null");
                UnityModManager.Logger.Log("[TwitchChat] Mod Disabled!");
                ModEntry.Enabled = false;
            }

            return true;
        }
        private static void OnUpdate(UnityModManager.ModEntry mod, float delta)
        {
            try
            {
                if (IsModEnabledAndWorldReadyForInteraction())
                {
                    AttachNotification("TwitchChatMod is enabled and ready for interaction.", "null");
                }
            } catch (Exception e) {
                ModEntry.Logger.Log(e.ToString());
            }
        }
        private static bool IsModEnabledAndWorldReadyForInteraction()
        {
            if (!_isEnabled) {
                return false;
            }
            if (UnityEngine.Object.FindObjectOfType<NotificationManager>() == null)
            {
                return false;
            }
            return true;
        }

        private static bool LoadSettings()
        {
            UnityModManager.Logger.Log("[TwitchChat] Attempting to load settings.");

            // Load the settings
            XmlSettings settings = new();
            CredentialsSettings credentials = new();
            CredentialsTokenSettings credentialsToken = new();
            StandardMessagesSettings messages = new();

            // Add debug logs to check if settings are loaded correctly
            UnityModManager.Logger.Log($"[TwitchChat] Loaded Settings: {settings}");
            UnityModManager.Logger.Log($"[TwitchChat] Loaded CredentialsSettings: {credentials}");
            UnityModManager.Logger.Log($"[TwitchChat] Loaded StandardMessagesSettings: {messages}");

            // Log detailed settings values
            UnityModManager.Logger.Log($"[TwitchChat] Settings - Message Duration: {settings.messageDuration}");
            UnityModManager.Logger.Log($"[TwitchChat] CredentialsSettings - Username: {credentials.twitchUsername}");
            UnityModManager.Logger.Log($"[TwitchChat] CredentialsSettings - Token: {credentialsToken.twitchToken}");
            UnityModManager.Logger.Log($"[TwitchChat] CredentialsSettings - Channel: {credentials.twitchChannel}");
            UnityModManager.Logger.Log($"[TwitchChat] StandardMessagesSettings - Welcome Message: {messages.welcomeMessage}");
            UnityModManager.Logger.Log($"[TwitchChat] StandardMessagesSettings - Info Message: {messages.infoMessage}");
            UnityModManager.Logger.Log($"[TwitchChat] StandardMessagesSettings - Command Message: {messages.commandMessage}");
            UnityModManager.Logger.Log($"[TwitchChat] StandardMessagesSettings - New Follower Message: {messages.newFollowerMessage}");
            UnityModManager.Logger.Log($"[TwitchChat] StandardMessagesSettings - New Subscriber Message: {messages.newSubscriberMessage}");
        
            messageDuration = settings.messageDuration;
            twitchUsername = credentials.twitchUsername;
            twitchToken = credentialsToken.twitchToken;
            twitchChannel = credentials.twitchChannel;
            welcomeMessage = messages.welcomeMessage;
            infoMessage = messages.infoMessage;
            commandMessage = messages.commandMessage;
            newFollowerMessage = messages.newFollowerMessage;
            newSubscriberMessage = messages.newSubscriberMessage;
        
            UnityModManager.Logger.Log($"[TwitchChat] Username: {twitchUsername}");
            UnityModManager.Logger.Log($"[TwitchChat] Token: {twitchToken}");
            UnityModManager.Logger.Log($"[TwitchChat] Channel: {twitchChannel}");
            UnityModManager.Logger.Log($"[TwitchChat] Message Duration: {messageDuration}");
            UnityModManager.Logger.Log($"[TwitchChat] Welcome Message: {welcomeMessage}");
            UnityModManager.Logger.Log($"[TwitchChat] Info Message: {infoMessage}");
            UnityModManager.Logger.Log($"[TwitchChat] Command Message: {commandMessage}");
            UnityModManager.Logger.Log($"[TwitchChat] New Follower Message: {newFollowerMessage}");
            UnityModManager.Logger.Log($"[TwitchChat] New Subscriber Message: {newSubscriberMessage}");

            UnityModManager.Logger.Log("[TwitchChat] Settings loaded successfully.");
        
            return true;
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
                    UnityModManager.Logger.Log($"[TwitchChat] Created directory: {logDirectory}");
                }
                catch (Exception ex)
                {
                    UnityModManager.Logger.Log($"[TwitchChat] Failed to create directory: {logDirectory}. Exception: {ex.Message}");
                    return;
                }
            }

            logFilePath = Path.Combine(logDirectory, $"Log - {date}.txt");
            messageFilePath = Path.Combine(logDirectory, $"Messages - {date}.txt");

            UnityModManager.Logger.Log($"[TwitchChat] Initialized log file: {logFilePath}");
            UnityModManager.Logger.Log($"[TwitchChat] Initialized message file: {messageFilePath}");
        }

        public static async void ConnectToTwitch()
        {
            if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                UnityModManager.Logger.Log("[TwitchChat] Already connected to Twitch.");
                return;
            }
            else
            {
                UnityModManager.Logger.Log("[TwitchChat] Initializing Twitch connection...");

                try
                {
                    var tokenResponse = await GetOAuthToken();
                    if (tokenResponse != null)
                    {
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");
                        httpClient.DefaultRequestHeaders.Add("Client-Id", twitchUsername);

                        UnityModManager.Logger.Log("[TwitchChat] Connecting to Twitch IRC...");
                        ConnectToIrc();
                    }
                    else
                    {
                        UnityModManager.Logger.Log("[TwitchChat] Failed to obtain OAuth token.");
                    }
                }
                catch (Exception ex)
                {
                    UnityModManager.Logger.Log($"[TwitchChat] Failed to connect to Twitch: {ex.Message}");
                }
            }
        }

        public static void DisconnectFromTwitch()
        {
            if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                httpClient.DefaultRequestHeaders.Remove("Authorization");
                UnityModManager.Logger.Log("[TwitchChat] Disconnected from Twitch.");
            }
            else
            {
                UnityModManager.Logger.Log("[TwitchChat] Already disconnected from Twitch.");
                return;
            }
        }

        private static async Task<TokenResponse?> GetOAuthToken()
        {
            var requestBody = new Dictionary<string, string>
            {
                { "client_id", twitchUsername },
                { "client_secret", twitchToken },
                { "grant_type", "client_credentials" }
            };

            var requestContent = new FormUrlEncodedContent(requestBody);
            var response = await httpClient.PostAsync("https://id.twitch.tv/oauth2/token", requestContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TokenResponse>(responseContent);
            }

            return null;
        }

        private static void ConnectToIrc()
        {
            // Implement IRC connection logic here
            // For example, use a library like IrcDotNet to connect to irc://irc.chat.twitch.tv:6667
        }

        private static void AttachNotification(string displayed_text, string object_name)
        {
            // Find the object_name GameObject in the scene
            GameObject found_object = GameObject.Find(object_name);
        
            // Find NotificationManager in the scene
            NotificationManager notificationManager = UnityEngine.Object.FindObjectOfType<NotificationManager>();
        
            if (notificationManager == null)
            {
                UnityModManager.Logger.Log("[TwitchChat] NotificationManager not found in the scene.");
                return;
            }
        
            try
            {
                // Display a notification, attached to the found_object if it's not null
                var notification = notificationManager.ShowNotification(
                    displayed_text,             // Text?
                    null,                       // Localization parameters?
                    messageDuration,           // Duration?
                    false,                      // Clear existing notifications?
                    found_object?.transform,    // Attach to GameObject if not null
                    false,                      // Localize?
                    false                       // Target UI?
                );
            }
            catch (Exception ex)
            {
                UnityModManager.Logger.Log($"[TwitchChat] Error showing notification and text: {displayed_text}\nException: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

        private static void TimedMessages()
        {
            try
            {
                // Check if the file exists
                if (!File.Exists(settingsFilePath))
                {
                    UnityModManager.Logger.Log($"[TwitchChat] Settings file not found: {settingsFilePath}");
                    return;
                }

                // Load the XML document
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(settingsFilePath);

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
                        UnityModManager.Logger.Log($"[TwitchChat] Added message: {messageNode.InnerText} with timer: {timerValue}");
                    }
                    else
                    {
                        UnityModManager.Logger.Log($"[TwitchChat] Skipped adding message: {messageKey} with timer: {timerKey}");
                    }
                }

                // Create and start timers for each message
                foreach (var message in messages)
                {
                    Timer timer = new(message.Value * 1000); // Convert seconds to milliseconds
                    timer.Elapsed += (source, e) => SendMessageToTwitch(message.Key);
                    timer.AutoReset = true;
                    timer.Enabled = true;
                    UnityModManager.Logger.Log($"[TwitchChat] Timer set for message: {message.Key} with interval: {message.Value * 1000} ms");
                }
            }
            catch (Exception ex)
            {
                UnityModManager.Logger.Log($"An error occurred: {ex.Message}");
            }
        }

        private static async void SendMessageToTwitch(string message)
        {
            if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                var content = new StringContent(JsonSerializer.Serialize(new { content = message }), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"https://api.twitch.tv/helix/chat/messages?broadcaster_id={twitchChannel}", content);

                if (response.IsSuccessStatusCode)
                {
                    TwitchChatMessages($"[Timed Message] {message}");
                }
                else
                {
                    UnityModManager.Logger.Log($"[TwitchChat] Failed to send message: {response.ReasonPhrase}");
                }
            }
            else
            {
                UnityModManager.Logger.Log("[TwitchChat] Unable to send message, Twitch client is not connected.");
            }
        }

        private static void TwitchChatLog(string message)
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                UnityModManager.Logger.Log("[TwitchChat] Log file path is not initialized.");
                return;
            }

            try
            {
                using (StreamWriter writer = new(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now:HH:mm}: {message}");
                }
            }
            catch (Exception ex)
            {
                UnityModManager.Logger.Log($"[TwitchChat] Failed to write to log file: {logFilePath}. Exception: {ex.Message}");
            }
        }

        private static void TwitchChatMessages(string message)
        {
            if (string.IsNullOrEmpty(messageFilePath))
            {
                UnityModManager.Logger.Log("[TwitchChat] Message file path is not initialized.");
                return;
            }

            int retryCount = 3;
            int delay = 1000; // 1 second

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    using (StreamWriter writer = new(messageFilePath, true))
                    {
                        writer.WriteLine($"{DateTime.Now:HH:mm}: {message}");
                    }
                    return; // Exit the method if writing is successful
                }
                catch (IOException ex) when (i < retryCount - 1)
                {
                    UnityModManager.Logger.Log($"[TwitchChat] Failed to write to message file: {messageFilePath}. Exception: {ex.Message}. Retrying in {delay}ms...");
                    System.Threading.Thread.Sleep(delay);
                }
                catch (Exception ex)
                {
                    UnityModManager.Logger.Log($"[TwitchChat] Failed to write to message file: {messageFilePath}. Exception: {ex.Message}");
                    return;
                }
            }
        }
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
}
