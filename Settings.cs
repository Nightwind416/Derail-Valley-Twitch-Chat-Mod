using System;
using System.IO;
using UnityEngine;
using UnityModManagerNet;

namespace TwitchChat
{
    public enum DebugLevel
    {
        Off,
        Minimal,
        Reduced,
        Full
    }

    [DrawFields(DrawFieldMask.Public)]
    [Serializable]
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public static Settings Instance { get; set; } = null!;
        public string twitchUsername = string.Empty;
        public int messageDuration = 20;
        public string EncodedOAuthToken = string.Empty;
        private bool getOathTokenFlag = false;
        private bool toggleWebSocketFlag = false;
        private bool connectionStatusFlag = false;
        private bool sendChatMessageHttpFlag = false;
        private bool directAttachmentMessageTestFlag = false;
        private bool messageQueueAttachmentMessageTestFlag = false;
        private bool MessageQueueAttachmentTestFlag = false;
        public DebugLevel debugLevel = DebugLevel.Minimal;
        public void DrawButtons()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Twitch Username: ", GUILayout.Width(150));
            twitchUsername = GUILayout.TextField(twitchUsername, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Message Duration (seconds): ", GUILayout.Width(150));
            messageDuration = int.Parse(GUILayout.TextField(messageDuration.ToString(), GUILayout.Width(50)));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.Space(10);

            GUILayout.Label("Twitch Authorization");
            string authButtonText = string.IsNullOrEmpty(EncodedOAuthToken) 
                ? "Request Authorization Token" 
                : "Validate Token";
            if (GUILayout.Button(authButtonText, GUILayout.Width(200)))
            {
                if (string.IsNullOrEmpty(EncodedOAuthToken))
                    getOathTokenFlag = true;
                else
                    _ = TwitchEventHandler.ValidateAuthToken();
            }
            GUILayout.Label($"Encoded Authorization Token: {EncodedOAuthToken}");
            GUILayout.Label($"Last Authorization Status: {TwitchEventHandler.oath_status}");

            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.Space(10);
            
            GUILayout.Label("Channel Connection Status");
            GUI.color = WebSocketManager.IsConnectionHealthy ? Color.green : Color.red;
            GUILayout.Label("■", GUILayout.Width(20));
            GUI.color = Color.white;
            GUILayout.Label("Connection Status: " + (WebSocketManager.IsConnectionHealthy ? "Connected" : "Disconnected"));
            GUILayout.BeginHorizontal();
            string buttonText = WebSocketManager.IsConnectionHealthy
                ? "Disconnect Channel"
                : "Connect Channel";
            if (GUILayout.Button(buttonText, GUILayout.Width(150)))
            {
                toggleWebSocketFlag = true;
            }
            GUILayout.EndHorizontal();
        
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Click here...", GUILayout.Width(400)))
            {
                sendChatMessageHttpFlag = true;
            }
            GUILayout.Label("... to send a test message to your Twitch channel");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.Space(10);
            
            GUILayout.Label("Test Message Display While In-Game: ");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Direct Attachment", GUILayout.Width(200)))
            {
                directAttachmentMessageTestFlag = true;
            }
            GUILayout.Label("Typically used by script/mod alerts and non-Twitch messages");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Message Queue", GUILayout.Width(200)))
            {
                MessageQueueAttachmentTestFlag = true;
            }
            GUILayout.Label("Uses same 'queuing' system as received Twitch messages");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.Space(10);

            GUI.color = Color.cyan;
            GUILayout.Label("Debug Settings");
            GUI.color = Color.white;  // Reset color for subsequent elements
            
            // Store current selection before grid
            int currentSelection = (int)debugLevel;
            
            // Create string array for debug levels
            string[] options = ["Off", "Minimal", "Reduced", "Full"];
            
            // Color each button based on selection
            GUILayout.BeginHorizontal();
            for (int i = 0; i < options.Length; i++)
            {
                GUI.color = (i == currentSelection) ? Color.cyan : Color.white;
                if (GUILayout.Button(options[i], GUILayout.Width(75)))
                {
                    debugLevel = (DebugLevel)i;
                }
            }
            GUI.color = Color.white;  // Reset color
            GUILayout.EndHorizontal();
        }
        public void Update()
        {
            _ = this;

            if (getOathTokenFlag)
            {
                getOathTokenFlag = false;
                _ = TwitchEventHandler.GetOathToken();
            }
            if (toggleWebSocketFlag)
            {
                toggleWebSocketFlag = false;
                if (WebSocketManager.IsConnectionHealthy)
                    _ = WebSocketManager.DisconnectFromoWebSocket();
                else
                    _ = WebSocketManager.ConnectToWebSocket();
            }
            if (connectionStatusFlag)
            {
                connectionStatusFlag = false;
                _ = TwitchEventHandler.ConnectionStatus();
            }
            if (sendChatMessageHttpFlag)
            {
                sendChatMessageHttpFlag = false;
                _ = TwitchEventHandler.SendChatMessageHTTP("HTTP message test 'from' Derail Valley");
            }
            if (directAttachmentMessageTestFlag)
            {
                directAttachmentMessageTestFlag = false;
                MessageHandler.AttachNotification("Direct Attachment Notification Test", "null");
            }
            if (MessageQueueAttachmentTestFlag)
            {
                MessageQueueAttachmentTestFlag = false;
                MessageHandler.SetVariable("indirectNotification", "Indirect Attachment Test Message Sent");
            }
            if (messageQueueAttachmentMessageTestFlag)
            {
                messageQueueAttachmentMessageTestFlag = false;
                MessageHandler.WebSocketNotificationTest();
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