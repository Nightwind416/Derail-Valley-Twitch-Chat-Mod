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
        public string testMessage = "Test message sent 'from' Derail Valley settings page. If you see this, your Authentication Token is working!";
        private bool getOAuthTokenFlag = false;
        private bool toggleWebSocketFlag = false;
        private bool connectionStatusFlag = false;
        private bool sendMessageFlag = false;
        private bool directAttachmentMessageTestFlag = false;
        private bool messageQueueAttachmentMessageTestFlag = false;
        private bool MessageQueueAttachmentTestFlag = false;
        public DebugLevel debugLevel = DebugLevel.Minimal;

        // Standard Messages Settings
        public bool welcomeMessageActive = true;
        public string welcomeMessage = "Welcome to my Derail Valley stream!";
        public bool newFollowerMessageActive = true;
        public string newFollowerMessage = "Welcome to the crew!";
        public bool newSubscriberMessageActive = true;
        public string newSubscriberMessage = "Thank you for subscribing!";
        public string disconnectMessage = "Stream ending, thanks for watching!";

        // Command Messages Settings
        public bool commandMessageActive = true;
        public string commandMessage = "!info !commands";
        public bool infoMessageActive = true;
        public string infoMessage = "Please keep chat clean and respectful. Use !commands to see available commands.";
        
        // Custom Commands
        public bool customCommand1Active = false;
        public string customCommand1Trigger = "custom1";
        public string customCommand1Response = "Custom command 1 response";
        
        public bool customCommand2Active = false;
        public string customCommand2Trigger = "custom2";
        public string customCommand2Response = "Custom command 2 response";
        
        public bool customCommand3Active = false;
        public string customCommand3Trigger = "custom3";
        public string customCommand3Response = "Custom command 3 response";
        
        public bool customCommand4Active = false;
        public string customCommand4Trigger = "custom4";
        public string customCommand4Response = "Custom command 4 response";
        
        public bool customCommand5Active = false;
        public string customCommand5Trigger = "custom5";
        public string customCommand5Response = "Custom command 5 response";

        // Timed Messages Settings
        public bool timedMessageSystemToggle = false;
        public string lastTimedMessageSent = "No message sent yet";
        public bool timedMessage1Toggle = false;
        public string timedMessage1 = "MessageNotSet";
        public float timedMessage1Timer = 0;
        public bool timedMessage2Toggle = false;
        public string timedMessage2 = "MessageNotSet";
        public float timedMessage2Timer = 0;
        public bool timedMessage3Toggle = false;
        public string timedMessage3 = "MessageNotSet";
        public float timedMessage3Timer = 0;
        public bool timedMessage4Toggle = false;
        public string timedMessage4 = "MessageNotSet";
        public float timedMessage4Timer = 0;
        public bool timedMessage5Toggle = false;
        public string timedMessage5 = "MessageNotSet";
        public float timedMessage5Timer = 0;

        // Dispatcher Messages Settings
        public bool dispatcherMessageActive = false;
        public string dispatcherMessage = "MessageNotSet";

        /// <summary>
        /// Draws the mod configuration UI using Unity's IMGUI system.
        /// Handles user input and displays current connection status.
        /// </summary>
        public void DrawButtons()
        {
            // TwitchChat Primary Settings Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Twitch Chat Integration Settings");           
                
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Twitch Username: ", GUILayout.Width(150));
                    twitchUsername = GUILayout.TextField(twitchUsername, GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                    GUILayout.Label("Message Duration (seconds): ", GUILayout.Width(150));
                    messageDuration = int.Parse(GUILayout.TextField(messageDuration.ToString(), GUILayout.Width(50)));
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Twitch Authentication Section
            GUILayout.BeginVertical(GUI.skin.box);
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
            GUILayout.EndVertical();

            GUILayout.Space(10);
            
            // WebSocket Connection Section
            GUILayout.BeginVertical(GUI.skin.box);
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
            
                GUILayout.Label("Click the button to send the message to your Twitch channel:");
                GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Send", GUILayout.Width(80)))
                    {
                        sendMessageFlag = true;
                    }
                    testMessage = GUILayout.TextField(testMessage, GUILayout.Width(400));
                GUILayout.EndHorizontal();
                GUILayout.Label("Note1: You can edit the message above before sending");
                GUILayout.Label("Note2: These messages are sent to your channel chat and will be visible to all viewers");
                GUILayout.Label("Note3: Mssages received on Twitch only confirm a valid Token");
                GUILayout.Label("Note4: Use Connection Status below to check received message status");
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Test Message Display Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Test Message Display While In-Game: ");
                GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Direct Attachment", GUILayout.Width(150)))
                    {
                        directAttachmentMessageTestFlag = true;
                    }
                    GUILayout.Label("Typically used by script/mod alerts and non-Twitch messages");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Message Queue", GUILayout.Width(150)))
                    {
                        MessageQueueAttachmentTestFlag = true;
                    }
                    GUILayout.Label("Uses same 'queuing' system as received Twitch messages");
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Standard Messages Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Configure standard chat messages and notifications");
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Welcome Message: ", GUILayout.Width(180));
                    Instance.welcomeMessage = GUILayout.TextField(Instance.welcomeMessage, GUILayout.Width(200));
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                    GUILayout.Label("New Follower Message: ", GUILayout.Width(180));
                    // Instance.newFollowerMessage = GUILayout.TextField(Instance.newFollowerMessage, GUILayout.Width(200));
                    GUILayout.Label(Instance.newFollowerMessage);
                    GUILayout.Label("(Not yet implemented)");
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                    GUILayout.Label("New Subscriber Message: ", GUILayout.Width(180));
                    // Instance.newSubscriberMessage = GUILayout.TextField(Instance.newSubscriberMessage, GUILayout.Width(200));
                    GUILayout.Label(Instance.newSubscriberMessage);
                    GUILayout.Label("(Not yet implemented)");
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Command Messages Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Configure command-related messages and responses");
                
                GUILayout.BeginHorizontal();
                    Instance.commandMessageActive = GUILayout.Toggle(Instance.commandMessageActive, "", GUILayout.Width(20));
                    GUILayout.Label("!Commands Message: ", GUILayout.Width(100));
                    GUILayout.Label(Instance.commandMessage, GUILayout.Width(200));
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                    Instance.infoMessageActive = GUILayout.Toggle(Instance.infoMessageActive, "", GUILayout.Width(20));
                    GUILayout.Label("!Info Message: ", GUILayout.Width(100));
                    Instance.infoMessage = GUILayout.TextField(Instance.infoMessage, GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                GUILayout.Label("Custom Commands:");

                // Custom Command 1
                GUILayout.BeginHorizontal();
                    Instance.customCommand1Active = GUILayout.Toggle(Instance.customCommand1Active, "", GUILayout.Width(20));
                    GUILayout.Label("Trigger: !", GUILayout.Width(50));
                    Instance.customCommand1Trigger = GUILayout.TextField(Instance.customCommand1Trigger, GUILayout.Width(100));
                    GUILayout.Label("Response: ", GUILayout.Width(70));
                    Instance.customCommand1Response = GUILayout.TextField(Instance.customCommand1Response, GUILayout.Width(360));
                GUILayout.EndHorizontal();

                // Custom Command 2
                GUILayout.BeginHorizontal();
                    Instance.customCommand2Active = GUILayout.Toggle(Instance.customCommand2Active, "", GUILayout.Width(20));
                    GUILayout.Label("Trigger: !", GUILayout.Width(50));
                    Instance.customCommand2Trigger = GUILayout.TextField(Instance.customCommand2Trigger, GUILayout.Width(100));
                    GUILayout.Label("Response: ", GUILayout.Width(70));
                    Instance.customCommand2Response = GUILayout.TextField(Instance.customCommand2Response, GUILayout.Width(360));
                GUILayout.EndHorizontal();

                // Custom Command 3
                GUILayout.BeginHorizontal();
                    Instance.customCommand3Active = GUILayout.Toggle(Instance.customCommand3Active, "", GUILayout.Width(20));
                    GUILayout.Label("Trigger: !", GUILayout.Width(50));
                    Instance.customCommand3Trigger = GUILayout.TextField(Instance.customCommand3Trigger, GUILayout.Width(100));
                    GUILayout.Label("Response: ", GUILayout.Width(70));
                    Instance.customCommand3Response = GUILayout.TextField(Instance.customCommand3Response, GUILayout.Width(360));
                GUILayout.EndHorizontal();

                // Custom Command 4
                GUILayout.BeginHorizontal();
                    Instance.customCommand4Active = GUILayout.Toggle(Instance.customCommand4Active, "", GUILayout.Width(20));
                    GUILayout.Label("Trigger: !", GUILayout.Width(50));
                    Instance.customCommand4Trigger = GUILayout.TextField(Instance.customCommand4Trigger, GUILayout.Width(100));
                    GUILayout.Label("Response: ", GUILayout.Width(70));
                    Instance.customCommand4Response = GUILayout.TextField(Instance.customCommand4Response, GUILayout.Width(360));
                GUILayout.EndHorizontal();

                // Custom Command 5
                GUILayout.BeginHorizontal();
                    Instance.customCommand5Active = GUILayout.Toggle(Instance.customCommand5Active, "", GUILayout.Width(20));
                    GUILayout.Label("Trigger: !", GUILayout.Width(50));
                    Instance.customCommand5Trigger = GUILayout.TextField(Instance.customCommand5Trigger, GUILayout.Width(100));
                    GUILayout.Label("Response: ", GUILayout.Width(70));
                    Instance.customCommand5Response = GUILayout.TextField(Instance.customCommand5Response, GUILayout.Width(360));
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Timed Messages Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Configure periodic automated messages");
                
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Automated Messages Status:", GUILayout.Width(400));
                    GUI.color = AutomatedMessages.AreTimersRunning ? Color.green : Color.red;
                    GUILayout.Label(AutomatedMessages.AreTimersRunning ? "Running" : "Stopped");
                    GUI.color = Color.white;
                    if (GUILayout.Button("Toggle", GUILayout.Width(100)))
                    {
                        Instance.timedMessageSystemToggle = !Instance.timedMessageSystemToggle;
                        AutomatedMessages.ToggleTimedMessages();
                    }
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Last Message Sent: ", GUILayout.Width(150));
                    GUILayout.Label(Instance.lastTimedMessageSent);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Timed Message 1: ", GUILayout.Width(150));
                    Instance.timedMessage1 = GUILayout.TextField(Instance.timedMessage1, GUILayout.Width(200));
                    GUILayout.Label("Timer (seconds): ", GUILayout.Width(150));
                    Instance.timedMessage1Timer = float.Parse(GUILayout.TextField(Instance.timedMessage1Timer.ToString(), GUILayout.Width(50)));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Timed Message 2: ", GUILayout.Width(150));
                    Instance.timedMessage2 = GUILayout.TextField(Instance.timedMessage2, GUILayout.Width(200));
                    GUILayout.Label("Timer (seconds): ", GUILayout.Width(150));
                    Instance.timedMessage2Timer = float.Parse(GUILayout.TextField(Instance.timedMessage2Timer.ToString(), GUILayout.Width(50)));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Timed Message 3: ", GUILayout.Width(150));
                    Instance.timedMessage3 = GUILayout.TextField(Instance.timedMessage3, GUILayout.Width(200));
                    GUILayout.Label("Timer (seconds): ", GUILayout.Width(150));
                    Instance.timedMessage3Timer = float.Parse(GUILayout.TextField(Instance.timedMessage3Timer.ToString(), GUILayout.Width(50)));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Timed Message 4: ", GUILayout.Width(150));
                    Instance.timedMessage4 = GUILayout.TextField(Instance.timedMessage4, GUILayout.Width(200));
                    GUILayout.Label("Timer (seconds): ", GUILayout.Width(150));
                    Instance.timedMessage4Timer = float.Parse(GUILayout.TextField(Instance.timedMessage4Timer.ToString(), GUILayout.Width(50)));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Timed Message 5: ", GUILayout.Width(150));
                    Instance.timedMessage5 = GUILayout.TextField(Instance.timedMessage5, GUILayout.Width(200));
                    GUILayout.Label("Timer (seconds): ", GUILayout.Width(150));
                    Instance.timedMessage5Timer = float.Parse(GUILayout.TextField(Instance.timedMessage5Timer.ToString(), GUILayout.Width(50)));
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Dispatcher Mod Integration Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Configure Dispatcher Mod integration messages");
                // Add Dispatcher Messages configuration UI here
            GUILayout.EndVertical();

            GUILayout.Space(10);
        
            // Debug Settings Section (existing)
            GUILayout.BeginVertical(GUI.skin.box);
                GUI.color = Color.cyan;
                GUILayout.Label("Debug Settings");
                GUI.color = Color.white;  // Reset color for subsequent elements
                
                // Store current selection before grid
                int currentSelection = (int)debugLevel;
                
                // Create string array for debug levels
                string[] options = {"Off", "Minimal", "Reduced", "Full"};
                
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
            GUILayout.EndVertical();
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
                _ = TwitchEventHandler.SendMessage(testMessage);
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
        public static string welcomeMessage => Settings.Instance.welcomeMessage;
        public static string newFollowerMessage => Settings.Instance.newFollowerMessage;
        public static string newSubscriberMessage => Settings.Instance.newSubscriberMessage;
    }

    /// <summary>
    /// Handles command-related messages and their configurations
    /// </summary>
    public class CommandMessages
    {
        public static string commandMessage => Settings.Instance.commandMessage;
        public static string infoMessage => Settings.Instance.infoMessage;
    }

    /// <summary>
    /// Handles periodic automated messages and their scheduling
    /// </summary>
    public class TimedMessages
    {
        public static bool TimedMessageSystemToggle => Settings.Instance.timedMessageSystemToggle;
        public static string lastTimedMessageSent => Settings.Instance.lastTimedMessageSent;
        public static string TimedMessage1 => Settings.Instance.timedMessage1;
        public static float TimedMessage1Timer => Settings.Instance.timedMessage1Timer;
        public static string TimedMessage2 => Settings.Instance.timedMessage2;
        public static float TimedMessage2Timer => Settings.Instance.timedMessage2Timer;
        public static string TimedMessage3 => Settings.Instance.timedMessage3;
        public static float TimedMessage3Timer => Settings.Instance.timedMessage3Timer;
        public static string TimedMessage4 => Settings.Instance.timedMessage4;
        public static float TimedMessage4Timer => Settings.Instance.timedMessage4Timer;
        public static string TimedMessage5 => Settings.Instance.timedMessage5;
        public static float TimedMessage5Timer => Settings.Instance.timedMessage5Timer;
    }

    /// <summary>
    /// Handles Dispatcher Mod related messages and configurations, when detected
    /// </summary>
    public class DispatcherModMessages
    {
        public static string dispatcherMessage => Settings.Instance.dispatcherMessage;
    }
}