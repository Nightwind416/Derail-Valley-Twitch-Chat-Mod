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
        public static Settings Instance { get; set; } = null!;
        public string twitchUsername = string.Empty;
        public int messageDuration = 20;
        public string EncodedOAuthToken = string.Empty;
        private bool getOathTokenFlag = false;
        private bool connectToWebSocketFlag = false;
        private bool disconnectFromWebSocketFlag = false;
        private bool connectionStatusFlag = false;
        private bool sendChatMessageHttpFlag = false;
        private bool directAttachmentMessageTestFlag = false;
        private bool messageQueueAttachmentMessageTestFlag = false;
        private bool indirectAttachmentMessageTestFlag = false;
        public void DrawButtons()
        {
            GUILayout.Label("Twitch Username");
            twitchUsername = GUILayout.TextField(twitchUsername, GUILayout.Width(400));
            GUILayout.Label($"Twitch ID: {TwitchEventHandler.user_id}");

            if (GUILayout.Button("Get Oath Token", GUILayout.Width(200)))
            {
                getOathTokenFlag = true;
            }
            GUILayout.Label($"Current Oath Token: {TwitchEventHandler.oath_access_token}");
            
            GUILayout.Label("WebSocket Actions");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Connect to WebSocket", GUILayout.Width(200)))
            {
                connectToWebSocketFlag = true;
            }
            if (GUILayout.Button("Disconnect from WebSocket", GUILayout.Width(200)))
            {
                disconnectFromWebSocketFlag = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Last WebSocket Message Received: " + MessageHandler.NewNotificationQueue["webSocketNotification"]);
        
            GUILayout.Label("Send Test Messages");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Send test HTTP message", GUILayout.Width(200)))
            {
                sendChatMessageHttpFlag = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Message Attachement Testing: ");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Direct Test", GUILayout.Width(200)))
            {
                directAttachmentMessageTestFlag = true;
            }
            if (GUILayout.Button("Indirect Test", GUILayout.Width(200)))
            {
                indirectAttachmentMessageTestFlag = true;
            }
            if (GUILayout.Button("MessageQueue Attachment Message Test", GUILayout.Width(200)))
            {
                messageQueueAttachmentMessageTestFlag = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Indirect Message: " + MessageHandler.NewNotificationQueue["indirectNotification"]);
            GUILayout.Label("Queued Message: " + MessageHandler.NewNotificationQueue["messageQueuenotification"]);
        }
        public void Update()
        {
            _ = this;

            if (getOathTokenFlag)
            {
                getOathTokenFlag = false;
                _ = TwitchEventHandler.GetOathToken();
            }
            if (connectToWebSocketFlag)
            {
                connectToWebSocketFlag = false;
                _ = WebSocketManager.ConnectToWebSocket();
            }
            if (disconnectFromWebSocketFlag)
            {
                disconnectFromWebSocketFlag = false;
                _ = WebSocketManager.DisconnectFromoWebSocket();
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
            if (indirectAttachmentMessageTestFlag)
            {
                indirectAttachmentMessageTestFlag = false;
                MessageHandler.SetVariable("indirectNotification", "Indirect Attachment Test Message Sent");
            }
            if (messageQueueAttachmentMessageTestFlag)
            {
                messageQueueAttachmentMessageTestFlag = false;
                MessageHandler.MessageQueueAttachmentMessageTest();
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