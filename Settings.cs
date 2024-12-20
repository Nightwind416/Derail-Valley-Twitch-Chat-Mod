using System;
using System.IO;
using UnityEngine;
using UnityModManagerNet;

namespace TwitchChat
{
    /// <summary>
    /// Defines the level of debug information to be logged
    /// </summary>
    public enum DebugLevel
    {
        Off,
        Minimal,
        Reduced,
        Full
    }

    /// <summary>
    /// Handles the mod settings and UI configuration for the Twitch Chat integration.
    /// Implements UnityModManager.ModSettings for save/load functionality and IDrawable for UI rendering.
    /// </summary>
    [DrawFields(DrawFieldMask.Public)]
    [Serializable]
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public static Settings Instance { get; set; } = null!;
        public string twitchUsername = string.Empty;
        public string authentication_status = "Unverified or not set";
        public int messageDuration = 20;
        public string EncodedOAuthToken = string.Empty;
        private bool getOAuthTokenFlag = false;
        private bool toggleWebSocketFlag = false;
        private bool connectionStatusFlag = false;
        private bool sendMessageFlag = false;
        private bool directAttachmentMessageTestFlag = false;
        private bool messageQueueAttachmentMessageTestFlag = false;
        private bool MessageQueueAttachmentTestFlag = false;
        public DebugLevel debugLevel = DebugLevel.Minimal;

        /// <summary>
        /// Draws the mod configuration UI using Unity's IMGUI system.
        /// Handles user input and displays current connection status.
        /// </summary>
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

            // Set button color based on validation status
            GUI.color = authentication_status == "Validated!" ? Color.green : Color.white;
            
            // Create button style that shows if interactive
            GUIStyle buttonStyle = new(GUI.skin.button);
            buttonStyle.normal.textColor = authentication_status == "Validated!" ? Color.black : Color.white;

            // Button is disabled if already validated
            GUI.enabled = authentication_status != "Validated!";
            if (GUILayout.Button(authButtonText, buttonStyle, GUILayout.Width(200)))
            {
                if (string.IsNullOrEmpty(EncodedOAuthToken))
                    getOAuthTokenFlag = true;
                else
                    _ = OAuthTokenManager.ValidateAuthToken();
            }
            GUI.enabled = true;
            GUI.color = Color.white; // Reset color
            
            // Set status message color
            if (authentication_status == "Validated!")
                GUI.color = Color.green;
            else if (authentication_status == "Authorization failed. Please try again." || authentication_status == "No Username Set")
                GUI.color = Color.red;
            else if (authentication_status == "Unverified or not set")
                GUI.color = Color.yellow;
            else
                GUI.color = Color.cyan;
                
            GUILayout.Label($"Last Authorization Status: {authentication_status}");
            GUI.color = Color.white; // Reset color

            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.Space(10);
            
            GUILayout.Label("Channel Connection");
            GUILayout.BeginHorizontal();
            GUI.color = Color.white;
            GUI.color = WebSocketManager.IsConnectionHealthy ? Color.green : Color.red;
            GUILayout.Label(WebSocketManager.IsConnectionHealthy ? "Connected" : "Disconnected", GUILayout.Width(100));
            GUILayout.Label("■", GUILayout.Width(25));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Last Message Type:", GUILayout.Width(150));
            GUI.color = Color.cyan;
            GUILayout.Label(WebSocketManager.LastMessageType);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Last Chat Message:", GUILayout.Width(150));
            GUI.color = Color.cyan;
            GUILayout.Label(WebSocketManager.LastChatMessage);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            string buttonText = WebSocketManager.IsConnectionHealthy
                ? "Disconnect"
                : " Connect ";
            if (GUILayout.Button(buttonText, GUILayout.Width(100)))
            {
                toggleWebSocketFlag = true;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Send Test", GUILayout.Width(100)))
            {
                sendMessageFlag = true;
            }
            GUILayout.Label("Each click of the button will send a 'test' message to your Twitch channel");
            GUILayout.EndHorizontal();
            GUILayout.Label("Note1: Test messages are sent to the channel chat and are visible to all viewers");
            GUILayout.Label("Note2: 'Good' test messages only confirm a valid Token, use Connection Status for status on receiving messages");

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

            GUILayout.BeginHorizontal();
            GUILayout.Label("Automated Messages Status:", GUILayout.Width(150));
            GUI.color = AutomatedMessages.AreTimersRunning ? Color.green : Color.red;
            GUILayout.Label(AutomatedMessages.AreTimersRunning ? "Running" : "Stopped");
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

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

        /// <summary>
        /// Updates the settings based on UI interactions and handles various action flags.
        /// Called every frame to process pending actions.
        /// </summary>
        public void Update()
        {
            _ = this;

            if (getOAuthTokenFlag)
            {
                getOAuthTokenFlag = false;
                _ = OAuthTokenManager.GetOathToken();
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
            if (sendMessageFlag)
            {
                sendMessageFlag = false;
                _ = TwitchEventHandler.SendMessage("Test message sent 'from' Derail Valley settings page. If you see this, your Authentication Token is working!");
            }
            if (directAttachmentMessageTestFlag)
            {
                directAttachmentMessageTestFlag = false;
                MessageHandler.AttachNotification("Direct Attachment Notification Test", "null");
            }
            if (MessageQueueAttachmentTestFlag)
            {
                MessageQueueAttachmentTestFlag = false;
                MessageHandler.WebSocketNotificationTest();
            }
            if (messageQueueAttachmentMessageTestFlag)
            {
                messageQueueAttachmentMessageTestFlag = false;
                MessageHandler.WebSocketNotificationTest();
            }
        }

        /// <summary>
        /// Default constructor for Settings class
        /// </summary>
        public Settings() { }

        /// <summary>
        /// Saves the current settings to the mod's configuration file
        /// </summary>
        /// <param name="entry">The mod entry point containing mod information</param>
        public override void Save(UnityModManager.ModEntry entry) {
            Save(this, entry);
        }

        /// <summary>
        /// Handles settings changes
        /// </summary>
        public void OnChange() { }

        /// <summary>
        /// Gets the path for the settings file
        /// </summary>
        /// <param name="modEntry">The mod entry point containing mod information</param>
        /// <returns>The full path to the settings file</returns>
        public override string GetPath(UnityModManager.ModEntry modEntry) {
            return Path.Combine(modEntry.Path, "Settings.xml");
        }
    }

    /// <summary>
    /// Handles standard messages and their configurations
    /// </summary>
    public class StandardMessages
    {
        public bool welcomeMessageActive = true;
        public string welcomeMessage = "Welcome to my Derail Valley stream!";
        public bool newFollowerMessageActive = true;
        public string newFollowerMessage = "Welcome to the crew!";
        public bool newSubscriberMessageActive = true;
        public string newSubscriberMessage = "Thank you for subscribing!";
    }

    /// <summary>
    /// Handles command-related messages and their configurations
    /// </summary>
    public class CommandMessages
    {
        public bool commandMessageActive = true;
        public string commandMessage = "!info !commands";
        public bool infoMessageActive = true;
        public string infoMessage = "Please keep chat clean and respectful. Use !commands to see available commands.";
    }

    /// <summary>
    /// Handles periodic automated messages and their scheduling
    /// </summary>
    public class TimedMessages
    {
        public bool timedMessageSystemActive = false;
        public bool TimedMessage1Toggle = false;
        public string TimedMessage1 = "MessageNotSet";
        public float TimedMessage1Timer = 0;
    }

    /// <summary>
    /// Handles Dispatcher Mod related messages and configurations, when detected
    /// </summary>
    public class DispatcherModMessages
    {
        public bool dispatcherMessageActive = false;
        public string dispatcherMessage = "MessageNotSet";
    }
}