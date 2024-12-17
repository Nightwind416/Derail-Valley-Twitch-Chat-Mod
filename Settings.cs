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
        private bool getOathTokenFlag = false;
        private bool connectToWebSocketFlag = false;
        private bool disconnectFromWebSocketFlag = false;
        private bool connectionStatusFlag = false;
        private bool sendChatMessageHttpFlag = false;
        private bool sendChatMessageSocketFlag = false;
        public int messageDuration = 20;
        public void DrawButtons()
        {
            GUILayout.Label("Twitch Username");
            twitchUsername = GUILayout.TextField(twitchUsername, GUILayout.Width(400));
            GUILayout.Label($"Twitch ID: {WebSocketClient.user_id}");

            if (GUILayout.Button("Get Oath Token", GUILayout.Width(200))) {
            getOathTokenFlag = true;
            }
            GUILayout.Label($"Current Oath Token: {WebSocketClient.oath_access_token}");
            
            GUILayout.Label("WebSocket Actions");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Connect to WebSocket", GUILayout.Width(200))) {
                connectToWebSocketFlag = true;
            }
            if (GUILayout.Button("Disconnect from WebSocket", GUILayout.Width(200))) {
                disconnectFromWebSocketFlag = true;
            }
            GUILayout.Label("Last WebSocket Received: " + WebSocketClient.lastWebSocketReceived);
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