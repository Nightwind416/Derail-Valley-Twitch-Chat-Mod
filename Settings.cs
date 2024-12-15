using UnityModManagerNet;
using System.IO;
using UnityEngine;
using System;

namespace TwitchChat
{
    [DrawFields(DrawFieldMask.Public)]
    [Serializable]
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public string twitchUsername = "Nightwind416";
        public string twitchChannel = "nightwind416";
        public string client_id = "qjklmbrascxsqow5gsvl6la72txnes";
        public string client_secret = "7fmru5kdzx6c5mzpjisk2u9l7d8u9i";
        public string manual_token = "oath";
        public string callbackUrl = "https://localhost:3000/";
        public string logFilePath = string.Empty;
        public string messageFilePath = string.Empty;
        public string twitch_oauth_token = string.Empty;
        public string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "Settings.xml");
        public string userID = string.Empty;
        private bool connectToTwitchFlag = false;
        private bool connectToWebSocketFlag = false;
        private bool connectionStatusFlag = false;
        private bool getUserIDFlag = false;
        private bool joinChannelFlag = false;
        private bool sendMessageFlag = false;
        private bool readSettingsFlag = false;
        private bool applySettingsFlag = false;
        private bool printCurrentSettingsFlag = false;
        public void DrawButtons()
        {
            if (GUILayout.Button("Connect to Twitch", GUILayout.Width(200))) {
                connectToTwitchFlag = true;
            }
            if (GUILayout.Button("Connect to WebSocket", GUILayout.Width(200))) {
                connectToWebSocketFlag = true;
            }
            if (GUILayout.Button("Connection Status", GUILayout.Width(200))) {
                connectionStatusFlag = true;
            }
            if (GUILayout.Button("Get User ID", GUILayout.Width(200))) {
                getUserIDFlag = true;
            }
            if (GUILayout.Button("Join Channel", GUILayout.Width(200))) {
                joinChannelFlag = true;
            }
            if (GUILayout.Button("Send test message", GUILayout.Width(200))) {
                sendMessageFlag = true;
            }
            if (GUILayout.Button("Read Settings From File", GUILayout.Width(200))) {
                readSettingsFlag = true;
            }
            if (GUILayout.Button("Apply Settings From File", GUILayout.Width(200))) {
                applySettingsFlag = true;
            }
            if (GUILayout.Button("Print current variable data", GUILayout.Width(200))) {
                printCurrentSettingsFlag = true;
            }
        }
        
        // public int messageDuration = 20;
        // public bool welcomeMessageActive = true;
        // public string welcomeMessage = "Welcome to my Derail Valley stream!";
        // public bool infoMessageActive = true;
        // public string infoMessage = "Please keep chat clean and respectful. Use !commands to see available commands.";
        // public bool newFollowerMessageActive = true;
        // public string newFollowerMessage = "Welcome to the crew!";
        // public bool newSubscriberMessageActive = true;
        // public string newSubscriberMessage = "Thank you for subscribing!";
        // public bool commandMessageActive = true;
        // public string commandMessage = "!info !commands";
        // public bool dispatcherMessageActive = false;
        // public string dispatcherMessage = "MessageNotSet";
        // public bool timedMessagesActive = false;

        // public bool TimedMessage1Toggle = false;
        // public string TimedMessage1 = "MessageNotSet";
        // public float TimedMessage1Timer = 0;
    

        public void Update()
        {
            _ = this;

            if (connectToTwitchFlag)
            {
                connectToTwitchFlag = false;
                _ = Main.ConnectToTwitch();
            }
            if (connectToWebSocketFlag)
            {
                connectToWebSocketFlag = false;
                _ = WebSocketClient.ConnectToWebSocket();
            }
            if (connectionStatusFlag)
            {
                connectionStatusFlag = false;
                _ = TwitchEventHandler.ConnectionStatus();
            }
            if (getUserIDFlag)
            {
                getUserIDFlag = false;
                _ = TwitchEventHandler.GetUserID();
            }
            if (joinChannelFlag)
            {
                joinChannelFlag = false;
                _ = TwitchEventHandler.JoinChannel();
            }
            if (sendMessageFlag)
            {
                sendMessageFlag = false;
                _ = TwitchEventHandler.SendMessage(message: "Test message from Derail Valley");
            }
            if (readSettingsFlag)
            {
                readSettingsFlag = false;
                Main.ReadSettingsFromFile();
            }
            if (applySettingsFlag)
            {
                applySettingsFlag = false;
                Main.ApplySettingsFromFile();
            }
            if (printCurrentSettingsFlag)
            {
                printCurrentSettingsFlag = false;
                Main.PrintCurrentSettings();
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
    }
}