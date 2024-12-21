using System;
using System.IO;
using System.Collections.Generic;
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
        public string disconnectMessage = "TwitchChatMod disconnecting, thanks for chatting!";

        // Command Messages Settings
        public bool commandMessageActive = true;
        public string commandMessage = "Channel Commands:  !info";
        public bool infoMessageActive = true;
        public string infoMessage = "Please keep chat clean and respectful. Use !commands to see enabled channel commands.";
        
        // Custom Commands
        public bool customCommand1Active = false;
        public string customCommand1Trigger = "custom1";
        public string customCommand1Response = "Custom command 1 response";
        public bool customCommand1IsWhisper = false;
        
        public bool customCommand2Active = false;
        public string customCommand2Trigger = "custom2";
        public string customCommand2Response = "Custom command 2 response";
        public bool customCommand2IsWhisper = false;
        
        public bool customCommand3Active = false;
        public string customCommand3Trigger = "custom3";
        public string customCommand3Response = "Custom command 3 response";
        public bool customCommand3IsWhisper = false;
        
        public bool customCommand4Active = false;
        public string customCommand4Trigger = "custom4";
        public string customCommand4Response = "Custom command 4 response";
        public bool customCommand4IsWhisper = false;
        
        public bool customCommand5Active = false;
        public string customCommand5Trigger = "custom5";
        public string customCommand5Response = "Custom command 5 response";
        public bool customCommand5IsWhisper = false;

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

        public readonly string[] ColorOptions = ["Normal", "Blue", "Green", "Orange", "Purple", "Primary"];
        private int message1ColorIndex = 0;
        private int message2ColorIndex = 0;
        private int message3ColorIndex = 0;
        private int message4ColorIndex = 0;
        private int message5ColorIndex = 0;

        // Method to get announcement type by message number
        // public string GetAnnouncementType(int messageNumber)
        // {
        //     int colorIndex = messageNumber switch
        //     {
        //         1 => message1ColorIndex,
        //         2 => message2ColorIndex,
        //         3 => message3ColorIndex,
        //         4 => message4ColorIndex,
        //         5 => message5ColorIndex,
        //         _ => 0
        //     };
        //     return ColorOptions[colorIndex];
        // }

        private void CycleColor(ref int colorIndex) {
            colorIndex = (colorIndex + 1) % ColorOptions.Length;
        }

        public Color GetAnnouncementColor(int colorIndex) {
            string colorName = ColorOptions[colorIndex];
            return colorName switch {
                "Normal" => Color.white,
                "Blue" => Color.blue,
                "Green" => Color.green,
                "Orange" => new Color(1f, 0.5f, 0f),
                "Purple" => new Color(0.5f, 0f, 0.5f),
                "Primary" => Color.red,
                _ => Color.white
            };
        }

        // Add this field near the other private fields at the top of the Settings class
        private bool debugSectionExpanded = false;

        /// <summary>
        /// Draws the mod configuration UI using Unity's IMGUI system.
        /// Handles user input and displays current connection status.
        /// </summary>
        public async void DrawButtons()
        {
            GUILayout.Space(10);

            // TwitchChat Primary Settings Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Twitch Chat Integration Settings");        
                GUILayout.Space(10);   
                
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Twitch Username: ", GUILayout.Width(120));
                    twitchUsername = GUILayout.TextField(twitchUsername, GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                    GUILayout.Label("Message Duration: ", GUILayout.Width(120));
                    messageDuration = int.Parse(GUILayout.TextField(messageDuration.ToString(), GUILayout.Width(30)));
                    GUILayout.Label("(in seconds)");
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Twitch Authentication Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Twitch Authorization");
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
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
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);
            
            // WebSocket (Channel) Connection Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Channel Connection");
                GUILayout.Space(10);

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
                    GUILayout.Label("Connection Status:", GUILayout.Width(125));
                    GUI.color = Color.white;
                    GUI.color = WebSocketManager.IsConnectionHealthy ? Color.green : Color.red;
                    GUILayout.Label("■", GUILayout.Width(10));
                    GUILayout.Label(WebSocketManager.IsConnectionHealthy ? "Connected" : "Disconnected");
                    GUI.color = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                    GUILayout.Label("Last Message Type:", GUILayout.Width(125));
                    GUI.color = Color.cyan;
                    GUILayout.Label(WebSocketManager.LastMessageType);
                    GUI.color = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                    GUILayout.Label("Last Chat Message:", GUILayout.Width(125));
                    GUI.color = Color.cyan;
                    GUILayout.Label(WebSocketManager.LastChatMessage);
                    GUI.color = Color.white;
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Standard Messages Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Configure standard chat messages and notifications");
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Welcome Message: ", GUILayout.Width(160));
                    Instance.welcomeMessage = GUILayout.TextField(Instance.welcomeMessage);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                    GUILayout.Label("New Follower Message: ", GUILayout.Width(160));
                    // Instance.newFollowerMessage = GUILayout.TextField(Instance.newFollowerMessage);
                    GUILayout.Label(Instance.newFollowerMessage, GUILayout.Width(200));
                    GUI.color = Color.yellow;
                    GUILayout.Label("(Future Implementation)");
                    GUI.color = Color.white;
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                    GUILayout.Label("New Subscriber Message: ", GUILayout.Width(160));
                    // Instance.newSubscriberMessage = GUILayout.TextField(Instance.newSubscriberMessage);
                    GUILayout.Label(Instance.newSubscriberMessage, GUILayout.Width(200));
                    GUI.color = Color.yellow;
                    GUILayout.Label("(Future Implementation)");
                    GUI.color = Color.white;
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Command Messages Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Configure command-related messages and responses");
                GUILayout.Space(10);
                
                GUILayout.BeginHorizontal();
                    Instance.commandMessageActive = GUILayout.Toggle(Instance.commandMessageActive, "", GUILayout.Width(20));
                    GUILayout.Label("!Commands Message: ", GUILayout.Width(150));
                    GUILayout.Label(Instance.commandMessage);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                    bool prevInfoActive = Instance.infoMessageActive;
                    Instance.infoMessageActive = GUILayout.Toggle(Instance.infoMessageActive, "", GUILayout.Width(20));
                    if (prevInfoActive != Instance.infoMessageActive)
                        CommandMessages.UpdateCommandsResponse();
                    GUILayout.Label("!Info Message: ", GUILayout.Width(150));
                    Instance.infoMessage = GUILayout.TextField(Instance.infoMessage);
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                
                GUILayout.Label("Custom Commands:");
                
                GUI.color = Color.yellow;
                GUILayout.Label("(Custom Commands Currently Disabled - Future Implementation)");
                GUI.color = Color.white;

                GUILayout.Label("On?    Trigger Word     Response");

                // Custom Command 1
                GUILayout.BeginHorizontal();
                    GUI.enabled = false;  // Disable interaction
                    bool prevCmd1Active = Instance.customCommand1Active;
                    Instance.customCommand1Active = GUILayout.Toggle(Instance.customCommand1Active, "", GUILayout.Width(20));
                    GUILayout.Label(Instance.customCommand1Trigger, GUILayout.Width(100));
                    GUILayout.Label(Instance.customCommand1Response);
                    GUI.enabled = true;  // Re-enable interaction
                GUILayout.EndHorizontal();

                // Custom Command 2
                GUILayout.BeginHorizontal();
                    GUI.enabled = false;
                    bool prevCmd2Active = Instance.customCommand2Active;
                    Instance.customCommand2Active = GUILayout.Toggle(Instance.customCommand2Active, "", GUILayout.Width(20));
                    GUILayout.Label(Instance.customCommand2Trigger, GUILayout.Width(100));
                    GUILayout.Label(Instance.customCommand2Response);
                    GUI.enabled = true;
                GUILayout.EndHorizontal();

                // Custom Command 3
                GUILayout.BeginHorizontal();
                    GUI.enabled = false;
                    bool prevCmd3Active = Instance.customCommand3Active;
                    Instance.customCommand3Active = GUILayout.Toggle(Instance.customCommand3Active, "", GUILayout.Width(20));
                    GUILayout.Label(Instance.customCommand3Trigger, GUILayout.Width(100));
                    GUILayout.Label(Instance.customCommand3Response);
                    GUI.enabled = true;
                GUILayout.EndHorizontal();

                // Custom Command 4
                GUILayout.BeginHorizontal();
                    GUI.enabled = false;
                    bool prevCmd4Active = Instance.customCommand4Active;
                    Instance.customCommand4Active = GUILayout.Toggle(Instance.customCommand4Active, "", GUILayout.Width(20));
                    GUILayout.Label(Instance.customCommand4Trigger, GUILayout.Width(100));
                    GUILayout.Label(Instance.customCommand4Response);
                    GUI.enabled = true;
                GUILayout.EndHorizontal();

                // Custom Command 5
                GUILayout.BeginHorizontal();
                    GUI.enabled = false;
                    bool prevCmd5Active = Instance.customCommand5Active;
                    Instance.customCommand5Active = GUILayout.Toggle(Instance.customCommand5Active, "", GUILayout.Width(20));
                    GUILayout.Label(Instance.customCommand5Trigger, GUILayout.Width(100));
                    GUILayout.Label(Instance.customCommand5Response);
                    GUI.enabled = true;
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Timed Messages Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Configure Timed Messages");
                GUILayout.Space(10);
                
                // Always enable the toggle button
                GUI.enabled = true;
                if (GUILayout.Button("Toggle System", GUILayout.Width(150)))
                {
                    Instance.timedMessageSystemToggle = !Instance.timedMessageSystemToggle;
                    AutomatedMessages.ToggleTimedMessages();
                }
                
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Timed Messaging System is currently:", GUILayout.Width(250));
                    GUI.color = AutomatedMessages.AreTimersRunning ? Color.green : Color.red;
                    GUILayout.Label(AutomatedMessages.AreTimersRunning ? "Enabled" : "Disabled");
                    GUI.color = Color.white;
                GUILayout.EndHorizontal();
                
                // Add warning message when system is running
                if (AutomatedMessages.AreTimersRunning)
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label("⚠️ Disable the system to modify timed message settings");
                    GUI.color = Color.white;
                }

                GUILayout.BeginHorizontal();
                    GUILayout.Label("Last Message Sent: ", GUILayout.Width(125));
                    GUILayout.Label(Instance.lastTimedMessageSent);
                GUILayout.EndHorizontal();

                // Disable all controls when system is running
                GUI.enabled = !AutomatedMessages.AreTimersRunning;

                GUI.color = Color.yellow;
                GUILayout.Label("Setting type/colors is not implemented yet. All choices will send as regular channel chat messages at their designated intervals.");
                GUI.color = Color.white;
                GUILayout.Label("Note 1: Timed messages only require a validated authentication token and can be sent even if not connected to the channel and receiving messages.");
                GUILayout.Label("Note 2: Set the message timer to '0' to completly ignore a timed message when enabling the system.");

                GUILayout.Label("     Type       Every               Message to Send");
                
                GUI.backgroundColor = GetAnnouncementColor(message1ColorIndex);
                GUILayout.BeginHorizontal();
                    if (GUILayout.Button(ColorOptions[message1ColorIndex], GUILayout.Width(70))) {
                        CycleColor(ref message1ColorIndex);
                        TimedMessages.TimedMessage1Color = ColorOptions[message1ColorIndex];
                    }
                    Instance.timedMessage1Timer = float.Parse(GUILayout.TextField(Instance.timedMessage1Timer.ToString(), GUILayout.Width(30)));
                    GUILayout.Label("seconds", GUILayout.Width(60));
                    Instance.timedMessage1 = GUILayout.TextField(Instance.timedMessage1);
                GUILayout.EndHorizontal();

                GUI.backgroundColor = GetAnnouncementColor(message2ColorIndex);
                GUILayout.BeginHorizontal();
                    if (GUILayout.Button(ColorOptions[message2ColorIndex], GUILayout.Width(70))) {
                        CycleColor(ref message2ColorIndex);
                    }
                    Instance.timedMessage2Timer = float.Parse(GUILayout.TextField(Instance.timedMessage2Timer.ToString(), GUILayout.Width(30)));
                    GUILayout.Label("seconds", GUILayout.Width(60));
                    Instance.timedMessage2 = GUILayout.TextField(Instance.timedMessage2);
                GUILayout.EndHorizontal();

                GUI.backgroundColor = GetAnnouncementColor(message3ColorIndex);
                GUILayout.BeginHorizontal();
                    if (GUILayout.Button(ColorOptions[message3ColorIndex], GUILayout.Width(70))) {
                        CycleColor(ref message3ColorIndex);
                    }
                    Instance.timedMessage3Timer = float.Parse(GUILayout.TextField(Instance.timedMessage3Timer.ToString(), GUILayout.Width(30)));
                    GUILayout.Label("seconds", GUILayout.Width(60));
                    Instance.timedMessage3 = GUILayout.TextField(Instance.timedMessage3);
                GUILayout.EndHorizontal();

                GUI.backgroundColor = GetAnnouncementColor(message4ColorIndex);
                GUILayout.BeginHorizontal();
                    if (GUILayout.Button(ColorOptions[message4ColorIndex], GUILayout.Width(70))) {
                        CycleColor(ref message4ColorIndex);
                    }
                    Instance.timedMessage4Timer = float.Parse(GUILayout.TextField(Instance.timedMessage4Timer.ToString(), GUILayout.Width(30)));
                    GUILayout.Label("seconds", GUILayout.Width(60));
                    Instance.timedMessage4 = GUILayout.TextField(Instance.timedMessage4);
                GUILayout.EndHorizontal();

                GUI.backgroundColor = GetAnnouncementColor(message5ColorIndex);
                GUILayout.BeginHorizontal();
                    if (GUILayout.Button(ColorOptions[message5ColorIndex], GUILayout.Width(70))) {
                        CycleColor(ref message5ColorIndex);
                    }
                    Instance.timedMessage5Timer = float.Parse(GUILayout.TextField(Instance.timedMessage5Timer.ToString(), GUILayout.Width(30)));
                    GUILayout.Label("seconds", GUILayout.Width(60));
                    Instance.timedMessage5 = GUILayout.TextField(Instance.timedMessage5);
                GUILayout.EndHorizontal();

                // Reset enabled state and background color after drawing
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Dispatcher Mod Integration Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Configure Dispatcher Mod integration messages");
                GUILayout.Space(10);
                // Add Dispatcher Messages configuration UI here
                GUI.color = Color.yellow;
                GUILayout.Label("(Future Implementation)");
                GUI.color = Color.white;
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Debug and Troubleshooting Section Header
            GUILayout.BeginHorizontal();
                GUI.color = Color.cyan;
                string toggleSymbol = debugSectionExpanded ? "▼" : "►";
                if (GUILayout.Button($"{toggleSymbol} Debug and Troubleshooting", GUI.skin.label))
                {
                    debugSectionExpanded = !debugSectionExpanded;
                }
                GUI.color = Color.white;
            GUILayout.EndHorizontal();
            
            if (debugSectionExpanded)
            {
                GUILayout.Space(10);
            
                // Debug Settings Section
                GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label("Debug Level");
                    
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
                GUILayout.EndVertical();

                GUILayout.Space(10);
                
                // Test Message Display Section
                GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label("Test In-Game Message Display");
                    GUILayout.Space(10);

                    GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Direct Attachment", GUILayout.Width(125)))
                        {
                            directAttachmentMessageTestFlag = true;
                        }
                        GUILayout.Label("Typically used by script/mod alerts and non-Twitch messages");
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Message Queue", GUILayout.Width(125)))
                        {
                            MessageQueueAttachmentTestFlag = true;
                        }
                        GUILayout.Label("Uses same 'queuing' system as received Twitch messages");
                    GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                    
                GUILayout.Space(10);
                
                GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label("Click the button to send the message to your Twitch channel");
                    GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Send", GUILayout.Width(80)))
                        {
                            sendMessageFlag = true;
                        }
                        testMessage = GUILayout.TextField(testMessage);
                    GUILayout.EndHorizontal();
                    GUILayout.Label("Note1: You can edit the message above before sending");
                    GUILayout.Label("Note2: These messages are sent to your channel chat and will be visible to all viewers");
                    GUILayout.Label("Note3: Mssages received on Twitch only confirm a valid Token");
                    GUILayout.Label("Note4: Use Connection Status below to check received message status");
                GUILayout.EndVertical();

                // GUILayout.BeginVertical(GUI.skin.box);
                //     GUILayout.Label("Click the button to send a whisper to the user_id");
                //     GUILayout.BeginHorizontal();
                //         if (GUILayout.Button("Send", GUILayout.Width(80)))
                //         {
                //             await TwitchEventHandler.SendWhisper("1218746026", "This is a test whisper message");
                //         }
                //     GUILayout.EndHorizontal();
                //     GUILayout.Label("Note1: You can edit the message above before sending");
                // GUILayout.EndVertical();
            }

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
    /// Manages standard welcome and event messages.
    /// Provides access to configured messages for new followers, subscribers, and general welcomes.
    /// </summary>
    public class StandardMessages
    {
        public static string welcomeMessage => Settings.Instance.welcomeMessage;
        public static string newFollowerMessage => Settings.Instance.newFollowerMessage;
        public static string newSubscriberMessage => Settings.Instance.newSubscriberMessage;
    }

    /// <summary>
    /// Manages chat command messages and responses.
    /// Handles command registration, response configuration, and message updates
    /// for interactive chat commands.
    /// </summary>
    public class CommandMessages
    {
        public static string commandMessage => Settings.Instance.commandMessage;
        public static string infoMessage => Settings.Instance.infoMessage;
        public static void UpdateCommandsResponse()
        {
            var enabledCommands = new List<string>();
            
            if (Settings.Instance.infoMessageActive) enabledCommands.Add("!info");
            if (Settings.Instance.customCommand1Active) enabledCommands.Add($"!{Settings.Instance.customCommand1Trigger}");
            if (Settings.Instance.customCommand2Active) enabledCommands.Add($"!{Settings.Instance.customCommand2Trigger}");
            if (Settings.Instance.customCommand3Active) enabledCommands.Add($"!{Settings.Instance.customCommand3Trigger}");
            if (Settings.Instance.customCommand4Active) enabledCommands.Add($"!{Settings.Instance.customCommand4Trigger}");
            if (Settings.Instance.customCommand5Active) enabledCommands.Add($"!{Settings.Instance.customCommand5Trigger}");

            string response = "Channel Commands:" + string.Join("  ", enabledCommands);
            Settings.Instance.commandMessage = response;
        }
    }

    /// <summary>
    /// Manages periodic automated message configurations.
    /// Provides access to timed message settings and scheduling information.
    /// </summary>
    public class TimedMessages
    {
        public static bool TimedMessageSystemToggle => Settings.Instance.timedMessageSystemToggle;
        public static string lastTimedMessageSent => Settings.Instance.lastTimedMessageSent;
        public static string TimedMessage1 => Settings.Instance.timedMessage1;
        public static float TimedMessage1Timer => Settings.Instance.timedMessage1Timer;
        public static string TimedMessage1Color = "Normal";
        public static string TimedMessage2 => Settings.Instance.timedMessage2;
        public static float TimedMessage2Timer => Settings.Instance.timedMessage2Timer;
        public static string TimedMessage2Color = "Normal";
        public static string TimedMessage3 => Settings.Instance.timedMessage3;
        public static float TimedMessage3Timer => Settings.Instance.timedMessage3Timer;
        public static string TimedMessage3Color = "Normal";
        public static string TimedMessage4 => Settings.Instance.timedMessage4;
        public static float TimedMessage4Timer => Settings.Instance.timedMessage4Timer;
        public static string TimedMessage4Color = "Normal";
        public static string TimedMessage5 => Settings.Instance.timedMessage5;
        public static float TimedMessage5Timer => Settings.Instance.timedMessage5Timer;
        public static string TimedMessage5Color = "Normal";
    }

    /// <summary>
    /// Manages integration with the Dispatcher Mod.
    /// Handles message routing and configuration when the Dispatcher Mod is present.
    /// </summary>
    public class DispatcherModMessages
    {
        public static string dispatcherMessage => Settings.Instance.dispatcherMessage;
    }
}