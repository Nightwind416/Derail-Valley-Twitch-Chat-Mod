using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using System.Xml;
using DV.UIFramework;
using UnityEngine;
using UnityModManagerNet;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace TwitchChat
{
    public static class Main
    {
        private static bool _isEnabled;
        public static bool _dispatcherModDetected;
        public static UnityModManager.ModEntry ModEntry { get; private set; } = null!;
        public static Settings Settings { get; set; } = null!;
        private static string logFilePath = string.Empty;
        public static string messageFilePath = string.Empty;
        public static string twitch_oauth_token = string.Empty;
        private static readonly string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "Settings.xml");
        public static HttpClient httpClient = new();

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
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            
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
        private static async Task InitializeTwitch()
        {
            await TwitchEventHandler.ConnectionStatus();
            await TwitchEventHandler.GetUserID();
            await TwitchEventHandler.JoinChannel();
            await TwitchEventHandler.SendMessage(message: "Test message from Derail Valley");
        }
        private static void OnUpdate(UnityModManager.ModEntry mod, float delta)
        {
            try
            {
                if (IsModEnabledAndWorldReadyForInteraction())
                {
                    // AttachNotification("TwitchChatMod is enabled and ready for interaction.", "null");
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

        
        public static bool LoadSettings()
        {
            UnityModManager.Logger.Log("[TwitchChat] Attempting to load settings from file.");
        
            // Check if the settings file exists
            if (!File.Exists(settingsFilePath))
            {
                UnityModManager.Logger.Log("[TwitchChat] Settings file not found.");
                return false;
            }
        
            // Deserialize the settings from the XML file
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            Settings settings;
            using (FileStream fs = new FileStream(settingsFilePath, FileMode.Open))
            {
                settings = (Settings)serializer.Deserialize(fs);
            }
        
            // Assign the deserialized values to the properties
            // var messageDuration = settings.messageDuration;
            // var welcomeMessage = settings.welcomeMessage;
            // var infoMessage = settings.infoMessage;
            // var commandMessage = settings.commandMessage;
            // var newFollowerMessage = settings.newFollowerMessage;
            // var newSubscriberMessage = settings.newSubscriberMessage;
        
            UnityModManager.Logger.Log("[TwitchChat] Settings loaded from file successfully.");
        
            return true;
        }

        public static void ReadSettings()
        {
            UnityModManager.Logger.Log("[TwitchChat] Attempting to read settings from file.");

            // Check if the settings file exists
            if (!File.Exists(settingsFilePath))
            {
                UnityModManager.Logger.Log("[TwitchChat] Settings file not found.");
                return;
            }
        
            // Deserialize the settings from the XML file
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            Settings settings;
            using (FileStream fs = new FileStream(settingsFilePath, FileMode.Open))
            {
                settings = (Settings)serializer.Deserialize(fs);
            }

            try
            {
                string json = File.ReadAllText(settingsFilePath);
                var deserializedSettings = JsonConvert.DeserializeObject<Settings>(json);

                // var read_messageDuration = deserializedSettings?.messageDuration;
                // var read_welcomeMessage = deserializedSettings?.welcomeMessage;
                // var read_infoMessage = deserializedSettings?.infoMessage;
                // var read_commandMessage = deserializedSettings?.commandMessage;
                // var read_newFollowerMessage = deserializedSettings?.newFollowerMessage;
                // var read_newSubscriberMessage = deserializedSettings?.newSubscriberMessage;
                // var read_dispatcherMessage = deserializedSettings?.dispatcherMessage;
                // var read_timedMessagesActive = deserializedSettings?.timedMessagesActive;

                // Log the settings
                UnityModManager.Logger.Log($"[TwitchChat] Standard Messages:");
                // UnityModManager.Logger.Log($"[TwitchChat] Message Duration: {read_messageDuration}");
                // UnityModManager.Logger.Log($"[TwitchChat] Welcome Message: {read_welcomeMessage}");
                // UnityModManager.Logger.Log($"[TwitchChat] Info Message: {read_infoMessage}");
                // UnityModManager.Logger.Log($"[TwitchChat] Command Message: {read_commandMessage}");
                // UnityModManager.Logger.Log($"[TwitchChat] New Follower Message: {read_newFollowerMessage}");
                // UnityModManager.Logger.Log($"[TwitchChat] New Subscriber Message: {read_newSubscriberMessage}");
                // UnityModManager.Logger.Log($"[TwitchChat] Dispatcher:");
                // UnityModManager.Logger.Log($"[TwitchChat] Dispatcher Message: {read_dispatcherMessage}");
                // UnityModManager.Logger.Log($"[TwitchChat] Timed Messages:");
                // UnityModManager.Logger.Log($"[TwitchChat] Timed Messages Active: {read_timedMessagesActive}");
            }
            catch (Exception ex)
            {
                UnityModManager.Logger.Log($"[TwitchChat] Error reading settings file: {ex.Message}");
            }
    
            UnityModManager.Logger.Log("[TwitchChat] Settings file read successfully.");
        
            return;
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

        public static async Task ConnectToTwitch()
        {
            UnityModManager.Logger.Log("[TwitchChat] Initializing Twitch connection...");

            try
            {
                var tokenResponse = await GetOAuthToken();
                if (tokenResponse != null)
                {
                    if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
                    {
                        if (tokenResponse.AccessToken != null)
                        {
                            twitch_oauth_token = tokenResponse.AccessToken;
                        }
                        UnityModManager.Logger.Log($"[TwitchChat] OAuth token obtained: {twitch_oauth_token}");
                    }
                    else
                    {
                        UnityModManager.Logger.Log("[TwitchChat] OAuth token is null or empty.");
                    }

                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");
                    httpClient.DefaultRequestHeaders.Add("Client-Id", Settings.client_id);
                    UnityModManager.Logger.Log("[TwitchChat] Connected to Twitch.");
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
        
        private static async Task<TwitchOathTokenClass?> GetOAuthToken()
        {
            UnityModManager.Logger.Log("[TwitchChat] Requesting OAuth token...");
            string tokenRequestUrl = "https://id.twitch.tv/oauth2/token";
            string tokenRequestContent = $"client_id={Settings.client_id}&client_secret={Settings.client_secret}&grant_type=client_credentials&scope=user:read:chat+user:bot+channel:bot";
            UnityModManager.Logger.Log($"[TwitchChat] Token request URL: {tokenRequestUrl}");
            UnityModManager.Logger.Log($"[TwitchChat] Token request content: {tokenRequestContent}");

            var content = new StringContent(tokenRequestContent, Encoding.UTF8, "application/x-www-form-urlencoded");

            try
            {
                var response = await httpClient.PostAsync(tokenRequestUrl, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                UnityModManager.Logger.Log($"[TwitchChat] Token response content: {responseContent}");

                var tokenResponse = JsonConvert.DeserializeObject<TwitchOathTokenClass>(responseContent);
                return tokenResponse;
            }
            catch (HttpRequestException httpEx)
            {
                UnityModManager.Logger.Log($"[TwitchChat] HTTP request error: {httpEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                UnityModManager.Logger.Log($"[TwitchChat] General error: {ex.Message}");
                return null;
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
        
        // private static async Task<TwitchOathTokenClass> GetOAuthToken()
        // {
        //     try
        //     {
        //         UnityModManager.Logger.Log("[TwitchChat] Requesting OAuth token...");
        //         string tokenRequestUrl = "https://id.twitch.tv/oauth2/token";
        //         var tokenRequestContent = new FormUrlEncodedContent(new[]
        //         {
        //             new KeyValuePair<string, string>("client_id", client_id),
        //             new KeyValuePair<string, string>("client_secret", client_secret),
        //             new KeyValuePair<string, string>("grant_type", "client_credentials"),
        //             new KeyValuePair<string, string>("scope", "user:read:chat user:bot channel:bot")
        //         });
        
        //         UnityModManager.Logger.Log($"[TwitchChat] Token request URL: {tokenRequestUrl}");
        //         UnityModManager.Logger.Log($"[TwitchChat] Token request content: {await tokenRequestContent.ReadAsStringAsync()}");
        
        //         var response = await httpClient.PostAsync(tokenRequestUrl, tokenRequestContent);
        //         UnityModManager.Logger.Log($"[TwitchChat] Token request status code: {response.StatusCode}");
        //         response.EnsureSuccessStatusCode();
        
        //         var responseContent = await response.Content.ReadAsStringAsync();
        //         UnityModManager.Logger.Log($"[TwitchChat] OAuth token response: {responseContent}");
        
        //         var tokenResponse = JsonConvert.DeserializeObject<TwitchOathTokenClass>(responseContent);

        //         // Extract the access_token using regex
        //         if (responseContent != null)
        //         {
        //             var match = Regex.Match(responseContent, "\"access_token\":\"([^\"]+)\"");
        //             if (match.Success)
        //             {
        //                 twitch_oauth_token = match.Groups[1].Value;
        //             }
        //         }
                
        //         return tokenResponse ?? new TwitchOathTokenClass();
        //     }
        //     catch (Exception ex)
        //     {
        //         UnityModManager.Logger.Log($"[TwitchChat] Exception while requesting OAuth token: {ex.Message}");
        //         return new TwitchOathTokenClass();
        //     }
        // }

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
                        UnityModManager.Logger.Log($"[TwitchChat] Skipped adding message: {messageKey} with timer: +{timerKey}");
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
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { content = message }), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync($"https://api.twitch.tv/helix/chat/messages?broadcaster_id={Settings.twitchChannel}", content);

                if (response.IsSuccessStatusCode)
                {
                    TwitchChatMessages($"[Sent Message] {message}");
                }
                else
                {
                    UnityModManager.Logger.Log($"[TwitchChat] Failed to send message: {response.ReasonPhrase}");
                }
            }
            else
            {
                UnityModManager.Logger.Log("[TwitchChat] Unable to send message.");
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

    public class TwitchOathTokenClass
    {
        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("scope")]
        public string[] Scope { get; set; } = Array.Empty<string>();

        [JsonProperty("token_type")]
        public string? TokenType { get; set; }
    }
}
