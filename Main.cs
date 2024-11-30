using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Reflection;
using UnityModManagerNet;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using UnityEngine;
using DV.UIFramework;
using Newtonsoft.Json.Linq;

namespace TwitchChat
{
    public static class Main
    {
        private static TwitchClient client = new TwitchClient();
        private static string? logFilePath;
        private static string? messageFilePath;
        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            modEntry.Logger.Log("[TwitchChat] Load method started.");
            
            // Initialize UnityMainThreadDispatcher
            GameObject dispatcherObject = new GameObject("UnityMainThreadDispatcher");
            dispatcherObject.AddComponent<UnityMainThreadDispatcher>();
            UnityEngine.Object.DontDestroyOnLoad(dispatcherObject);
            modEntry.Logger.Log("[TwitchChat] UnityMainThreadDispatcher initialized.");

            try
            {

                // Other plugin startup logic
                modEntry.OnToggle = OnToggle;
                modEntry.Logger.Log("[TwitchChat] OnToggle method assigned.");
            }
            catch (Exception ex)
            {
                modEntry.Logger.LogException($"[TwitchChat] Failed to load {modEntry.Info.DisplayName}:", ex);
                return false;
            }

            modEntry.Logger.Log("[TwitchChat] Load method completed successfully.");
            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                string displayed_text = "TwitchChatMod Notifications Enabled.";
                string object_name = "null";
                AttachNotification(displayed_text, object_name);
                ConnectToTwitch();
                InitializeLogFile();
                UnityModManager.Logger.Log("[TwitchChat] Mod Enabled!");
            }
            else
            {
                UnityModManager.Logger.Log("[TwitchChat] Mod Disabled!");
            }

            return true;
        }

        private static void ConnectToTwitch()
        {
            if (client.IsConnected)
            {
                UnityModManager.Logger.Log("[TwitchChat] Already connected to Twitch.");
                return;
            }
        
            UnityModManager.Logger.Log("[TwitchChat] Initializing Twitch connection...");
        
            // Define the path to the credentials file
            string credentialsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "twitch_credentials.json");
        
            try
            {
                // Check if the file exists
                if (!File.Exists(credentialsFilePath))
                {
                    UnityModManager.Logger.Log($"[TwitchChat] Credentials file not found: {credentialsFilePath}");
                    return;
                }
        
                // Read credentials from the JSON file
                var credentialsJson = File.ReadAllText(credentialsFilePath);
                var credentialsData = JObject.Parse(credentialsJson);
                
                // Log the contents of the credentialsData object               
                string username = credentialsData["username"]?.ToString() ?? string.Empty;
                string token = credentialsData["token"]?.ToString() ?? string.Empty;
                string channel = credentialsData["channel"]?.ToString() ?? string.Empty;

                UnityModManager.Logger.Log($"[TwitchChat] Username: {username}");
                UnityModManager.Logger.Log($"[TwitchChat] Token: {token}");
                UnityModManager.Logger.Log($"[TwitchChat] Channel: {channel}");
        
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token))
                {
                    UnityModManager.Logger.Log("[TwitchChat] Invalid credentials.");
                    return;
                }
        
                ConnectionCredentials credentials = new ConnectionCredentials(username, token);
                client.Initialize(credentials, channel);
                
                client.OnLog += Client_OnLog;
                client.OnJoinedChannel += Client_OnJoinedChannel;
                client.OnMessageReceived += Client_OnMessageReceived;
                client.OnNewSubscriber += Client_OnNewSubscriber;
                client.OnConnected += Client_OnConnected;
                client.OnConnectionError += Client_OnConnectionError;
                client.OnIncorrectLogin += Client_OnIncorrectLogin;
        
                UnityModManager.Logger.Log("[TwitchChat] Connecting to Twitch...");
                client.Connect();
            }
            catch (Exception ex)
            {
                UnityModManager.Logger.Log($"[TwitchChat] Failed to connect to Twitch: {ex.Message}");
            }
        }

        private static void Client_OnLog(object sender, OnLogArgs e)
        {
            TwitchChatLog($"{e.Data}");
        }

        private static void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            TwitchChatLog($"Joined channel: {e.Channel}");
        }

        private static void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            string display_message = $"{e.ChatMessage.Username}: {e.ChatMessage.Message}";
            TwitchChatMessages($"{e.ChatMessage.Username}: {e.ChatMessage.Message}");
            UnityMainThreadDispatcher.Instance().Enqueue(() => AttachNotification(display_message, "null"));
        }

        private static void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            TwitchChatMessages($"New subscriber: {e.Subscriber.DisplayName}");
            string display_message = $"{e.Subscriber.DisplayName} is a new subscriber!";
            UnityMainThreadDispatcher.Instance().Enqueue(() => AttachNotification(display_message, "null"));
        }

        private static void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            TwitchChatLog("Connected to Twitch!");
            
            // Define the path to the twitch_messages file
            string twitch_messagesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "twitch_messages.json");
        
            try
            {
                // Check if the file exists
                if (!File.Exists(twitch_messagesFilePath))
                {
                    UnityModManager.Logger.Log($"[TwitchChat] Twitch Messages file not found: {twitch_messagesFilePath}");
                    return;
                }
        
                // Read messages from the JSON file
                var twitch_messagesJson = File.ReadAllText(twitch_messagesFilePath);
                var twitch_messagesData = JObject.Parse(twitch_messagesJson);
                
                // Log the contents of the twitch_messages object               
                string welcomeMessage = twitch_messagesData["welcomeMessage"]?.ToString() ?? string.Empty;

                UnityModManager.Logger.Log($"[TwitchChat] Welcome Message: {welcomeMessage}");
        
                if (string.IsNullOrEmpty(welcomeMessage))
                {
                    UnityModManager.Logger.Log("[TwitchChat] No welcome message defined.");
                    return;
                }
                else
                {
                    SendMessageToTwitch(welcomeMessage);
                }
            }
            catch (Exception ex)
            {
                UnityModManager.Logger.Log($"[TwitchChat] Error reading Twitch messages file: {ex.Message}");
            }
            TimedMessages();
        }

        private static void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            UnityModManager.Logger.Log($"[TwitchChat] Connection error: {e.Error.Message}");
        }

        private static void Client_OnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
        {
            UnityModManager.Logger.Log("[TwitchChat] Incorrect login credentials.");
        }
        private static void AttachNotification(string displayed_text, string object_name)
        {
            
            // Define the path to the twitch_settings file
            string twitch_settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "twitchchat_settings.json");
            
            // Check if the file exists
            if (!File.Exists(twitch_settingsFilePath))
            {
                UnityModManager.Logger.Log($"[TwitchChat] Twitch Settings file not found: {twitch_settingsFilePath}");
                return;
            }
    
            // Read settings from the JSON file
            var twitch_settingsJson = File.ReadAllText(twitch_settingsFilePath);
            var twitch_settingsData = JObject.Parse(twitch_settingsJson);
            
            // Log the contents of the twitch_messages object               
            float message_duration = float.TryParse(twitch_settingsData["message_duration"]?.ToString(), out float duration) ? duration : 20f;

            UnityModManager.Logger.Log($"[TwitchChat] Message duration: {message_duration}");
            
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
                    message_duration,           // Duration?
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
                // Define the path to the settings file using a relative path
                string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "twitch_messages.json");
                UnityModManager.Logger.Log($"[TwitchChat] Settings file path: {settingsFilePath}");
                
                // Read the settings file
                var settingsJson = File.ReadAllText(settingsFilePath);
                UnityModManager.Logger.Log("[TwitchChat] Settings file read successfully.");
                var settingsData = JObject.Parse(settingsJson);
                UnityModManager.Logger.Log("[TwitchChat] Settings JSON parsed successfully.");
                
                // Extract messages and their timers
                var messages = new Dictionary<string, int>();
                
                void AddMessage(string key, JToken value)
                {
                    if (key != null && value != null && (int)value > 0)
                    {
                        messages.Add(key, (int)value);
                        UnityModManager.Logger.Log($"[TwitchChat] Added message: {key} with timer: {value}");
                    }
                    else
                    {
                        UnityModManager.Logger.Log($"[TwitchChat] Skipped adding message: {key} with timer: {value}");
                    }
                }
                
                AddMessage(settingsData["timedMessage1"]?.ToString(), settingsData["timedMessage1Timer"]);
                AddMessage(settingsData["timedMessage2"]?.ToString(), settingsData["timedMessage2Timer"]);
                AddMessage(settingsData["timedMessage3"]?.ToString(), settingsData["timedMessage3Timer"]);
                AddMessage(settingsData["timedMessage4"]?.ToString(), settingsData["timedMessage4Timer"]);
                AddMessage(settingsData["timedMessage5"]?.ToString(), settingsData["timedMessage5Timer"]);
                AddMessage(settingsData["timedMessage6"]?.ToString(), settingsData["timedMessage6Timer"]);
                AddMessage(settingsData["timedMessage7"]?.ToString(), settingsData["timedMessage7Timer"]);
                AddMessage(settingsData["timedMessage8"]?.ToString(), settingsData["timedMessage8Timer"]);
                AddMessage(settingsData["timedMessage9"]?.ToString(), settingsData["timedMessage9Timer"]);
                AddMessage(settingsData["timedMessage10"]?.ToString(), settingsData["timedMessage10Timer"]);
                
                // Create and start timers for each message
                foreach (var message in messages)
                {
                    if (message.Key != null)
                    {
                        Timer timer = new Timer(message.Value * 1000); // Convert seconds to milliseconds
                        timer.Elapsed += (source, e) => SendMessageToTwitch(message.Key);
                        timer.AutoReset = true;
                        timer.Enabled = true;
                        UnityModManager.Logger.Log($"[TwitchChat] Timer set for message: {message.Key} with interval: {message.Value * 1000} ms");
                    }
                    else
                    {
                        UnityModManager.Logger.Log($"[TwitchChat] Skipped setting timer for message: {message.Key}");
                    }
                }
            }
            catch (Exception ex)
            {
                UnityModManager.Logger.Log($"An error occurred: {ex.Message}");
            }
        }

        private static void SendMessageToTwitch(string message)
        {

            // Define the path to the credentials file
            string credentialsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "twitch_credentials.json");

            // Check if the file exists
            if (!File.Exists(credentialsFilePath))
            {
                UnityModManager.Logger.Log($"[TwitchChat] Credentials file not found: {credentialsFilePath}");
                return;
            }
    
            // Read credentials from the JSON file
            var credentialsJson = File.ReadAllText(credentialsFilePath);
            var credentialsData = JObject.Parse(credentialsJson);
            
            // Log the contents of the credentialsData object               
            string username = credentialsData["username"]?.ToString() ?? string.Empty;

            if (client.IsConnected)
            {
                client.SendMessage(username, message);
                TwitchChatMessages($"[Timed Message] {message}");
            }
            else
            {
                UnityModManager.Logger.Log("[TwitchChat] Unable to send message, Twitch client is not connected.");
            }
        }
        private static void InitializeLogFile()
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

        private static void TwitchChatLog(string message)
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                UnityModManager.Logger.Log("[TwitchChat] Log file path is not initialized.");
                return;
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
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
                    using (StreamWriter writer = new StreamWriter(messageFilePath, true))
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
}