using UnityModManagerNet;
using System.IO;
using UnityEngine;
using System;
using System.Reflection;
using System.Xml.Serialization;

namespace TwitchChat
{
    [DrawFields(DrawFieldMask.Public)]
    [Serializable]
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public static Settings Instance { get; set; } = null!;
        public string twitchUsername = string.Empty;
        public string twitchChannel = string.Empty;
        public string twitch_oauth_token = string.Empty;
        public string userID = string.Empty;
        public string client_id = "qjklmbrascxsqow5gsvl6la72txnes";
        private bool getOathTokenFlag = false;
        private bool connectToWebSocketFlag = false;
        private bool disconnectFromWebSocketFlag = false;
        private bool connectionStatusFlag = false;
        private bool sendChatMessageHttpFlag = false;
        private bool sendChatMessageSocketFlag = false;
        private bool getUserIDAPIFlag = false;
        private bool getUserIDSocketFlag = false;
        private bool readSettingsFlag = false;
        private bool applySettingsFlag = false;
        private bool printCurrentSettingsFlag = false;
        public void DrawButtons()
        {
            if (GUILayout.Button("Get Oath Token", GUILayout.Width(200))) {
            getOathTokenFlag = true;
            }

            GUILayout.Label("WebSocket Actions");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Connect to WebSocket", GUILayout.Width(200))) {
            connectToWebSocketFlag = true;
            }
            if (GUILayout.Button("Disconnect from WebSocket", GUILayout.Width(200))) {
            disconnectFromWebSocketFlag = true;
            }
            GUILayout.EndHorizontal();
        
            GUILayout.Label("User ID Actions");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Get User ID API", GUILayout.Width(200))) {
            getUserIDAPIFlag = true;
            }
            if (GUILayout.Button("Get User ID Socket", GUILayout.Width(200))) {
            getUserIDSocketFlag = true;
            }
            GUILayout.EndHorizontal();
        
            GUILayout.Label("Send Test Messages");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Send test HTTP message", GUILayout.Width(200))) {
            sendChatMessageHttpFlag = true;
            }
            if (GUILayout.Button("Send test Socket message", GUILayout.Width(200))) {
            sendChatMessageSocketFlag = true;
            }
            GUILayout.EndHorizontal();
        
            GUILayout.Label("Settings Actions");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Read Settings From File", GUILayout.Width(200))) {
            readSettingsFlag = true;
            }
            if (GUILayout.Button("Apply Settings From File", GUILayout.Width(200))) {
            applySettingsFlag = true;
            }
            GUILayout.EndHorizontal();
        
            if (GUILayout.Button("Print current variable data", GUILayout.Width(200))) {
            printCurrentSettingsFlag = true;
            }
        }

        public string tempToken = string.Empty;
        public int messageDuration = 20;
        public bool welcomeMessageActive = true;
        public string welcomeMessage = "Welcome to my Derail Valley stream!";
        public bool infoMessageActive = true;
        public string infoMessage = "Please keep chat clean and respectful. Use !commands to see available commands.";
        public bool newFollowerMessageActive = true;
        public string newFollowerMessage = "Welcome to the crew!";
        public bool newSubscriberMessageActive = true;
        public string newSubscriberMessage = "Thank you for subscribing!";
        public bool commandMessageActive = true;
        public string commandMessage = "!info !commands";
        public bool dispatcherMessageActive = false;
        public string dispatcherMessage = "MessageNotSet";
        public bool timedMessagesActive = false;
        public bool TimedMessage1Toggle = false;
        public string TimedMessage1 = "MessageNotSet";
        public float TimedMessage1Timer = 0;
    

        public void Update()
        {
            _ = this;

            if (getOathTokenFlag)
            {
                getOathTokenFlag = false;
                _ = Main.GetOathToken();
            }
            if (connectToWebSocketFlag)
            {
                connectToWebSocketFlag = false;
                _ = WebSocketClient.ConnectToWebSocket();
            }
            if (disconnectFromWebSocketFlag)
            {
                disconnectFromWebSocketFlag = false;
                _ = WebSocketClient.DisconnectFromoWebSocket();
            }
            if (connectionStatusFlag)
            {
                connectionStatusFlag = false;
                _ = TwitchEventHandler.ConnectionStatus();
            }
            if (getUserIDAPIFlag)
            {
                getUserIDAPIFlag = false;
                _ = TwitchEventHandler.GetUserID();
            }
            if (getUserIDSocketFlag)
            {
                getUserIDSocketFlag = false;
                _ = WebSocketClient.GetUserIdAsync();
            }
            if (sendChatMessageHttpFlag)
            {
                sendChatMessageHttpFlag = false;
                _ = TwitchEventHandler.SendChatMessageHTTP("Test HTTP message from Derail Valley");
            }
            if (sendChatMessageSocketFlag)
            {
                sendChatMessageSocketFlag = false;
                _ = WebSocketClient.SendChatMessageWebSocket("Test Socket message from Derail Valley");
            }
            if (readSettingsFlag)
            {
                readSettingsFlag = false;
                ReadSettingsFromFile();
            }
            if (applySettingsFlag)
            {
                applySettingsFlag = false;
                ApplySettingsFromFile();
            }
            if (printCurrentSettingsFlag)
            {
                printCurrentSettingsFlag = false;
                PrintCurrentSettings();
            }
        }
        public Settings() { }
        public override void Save(UnityModManager.ModEntry entry) {
            Save(this, entry);
        }

        public void OnChange() { }

        public override string GetPath(UnityModManager.ModEntry modEntry) {
            return Path.Combine(modEntry.Path, "Settings.xml");
        }
        public static void ApplySettingsFromFile()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, "Attempting to apply settings from file.");
        
            // Check if the settings file exists
            if (!File.Exists(Main.settingsFile))
            {
                Main.LogEntry(methodName, "Settings file not found.");
                return;
            }
        
            try
            {
                // Deserialize the settings from the XML file
                var serializer = new XmlSerializer(typeof(Settings));
                Settings applySettings;
                using (var fs = new FileStream(Main.settingsFile, FileMode.Open))
                {
                    applySettings = (Settings)serializer.Deserialize(fs);
                }
        
                // Access static members using the class name
                var settingsInstance = Instance;
                settingsInstance.twitchUsername = applySettings.twitchUsername;
                settingsInstance.twitchChannel = applySettings.twitchChannel;
                var webSocketClient = new WebSocketClient();
                WebSocketClient.client_id = applySettings.client_id;

                Main.LogEntry(methodName, "Successfully applied settings from file");
        
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Error applying settings from file: {ex.Message}");
                return;
            }
        }

        public static void ReadSettingsFromFile()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, "Attempting to read settings from file.");
        
            // Check if the settings file exists
            if (!File.Exists(Main.settingsFile))
            {
                Main.LogEntry(methodName, "Settings file not found.");
                return;
            }
        
            try
            {
                // Deserialize the settings from the XML file
                XmlSerializer settingsSerializer = new(typeof(Settings));
                Settings readSettings;
                using (FileStream settingsFileStream = new(Main.settingsFile, FileMode.Open))
                {
                    readSettings = (Settings)settingsSerializer.Deserialize(settingsFileStream);
                }
        
                // Log the settings from the file
                Main.LogEntry(methodName, $"Settings File read successfully:");
                Main.LogEntry(methodName, $"Twitch Username: {readSettings.twitchUsername}");
                Main.LogEntry(methodName, $"Twitch Channel: {readSettings.twitchChannel}");
                Main.LogEntry(methodName, $"Client ID: {readSettings.client_id}");
                Main.LogEntry(methodName, $"Twitch oath Token: {readSettings.twitch_oauth_token}");
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Error reading settings file: {ex.Message}");
                return;
            }
        }

        public static void PrintCurrentSettings()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, $"Current settings:");
            Main.LogEntry(methodName, $"Twitch Username: {Instance.twitchUsername}");
            Main.LogEntry(methodName, $"Twitch Channel: {Instance.twitchChannel}");
            Main.LogEntry(methodName, $"Client ID: {Instance.client_id}");
        }
    }
}