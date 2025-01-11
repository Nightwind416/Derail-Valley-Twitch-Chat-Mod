using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace TwitchChat
{
    /// <summary>
    /// Defines the level of debug information logging
    /// </summary>
    public enum DebugLevel
    {
        /// <summary>No debug logging</summary>
        Off,
        /// <summary>Basic error and connection status logging</summary>
        Minimal,
        /// <summary>Intermediate level of operational logging</summary>
        Reduced,
        /// <summary>Comprehensive debug information</summary>
        Full
    }

    /// <summary>
    /// Manages mod settings and configuration UI.
    /// </summary>
    /// <remarks>
    /// This class handles:
    /// - Mod configuration persistence
    /// - Settings UI rendering and interaction
    /// - Message templates and automation settings
    /// - Integration settings for other mods
    /// </remarks>
    [DrawFields(DrawFieldMask.Public)]
    [Serializable]
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        /// <summary>Singleton instance of Settings</summary>
        public static Settings Instance { get; set; } = null!;
        
        /// <summary>User's Twitch username</summary>
        public string twitchUsername = string.Empty;
        public string authentication_status = "Unverified or not set";
        public string EncodedOAuthToken = string.Empty;
        public DebugLevel debugLevel = DebugLevel.Minimal;
        public string[] activePanels = new string[] { "Main", "Main", "Main", "Main", "Main", "Main" };
        public bool notificationsEnabled = true;
        public float notificationDuration = 10;
        public bool processOwn = true;
        public bool processDuplicates = false;
        
        // Standard Messages Settings
        public bool connectMessageEnabled = true;
        public string connectMessage = "TwitchChatMod connected! Messages are being relayed to in-game panels.";
        public bool newFollowerMessageEnabled = true;
        public string newFollowerMessage = "Welcome to the crew!";
        public bool newSubscriberMessageEnabled = true;
        public string newSubscriberMessage = "Thank you for subscribing!";
        public bool disconnectMessageEnabled = true;
        public string disconnectMessage = "TwitchChatMod disconnected! Messages are no longer being relayed to in-game panels.";

        // Command Messages Settings
        public bool commandsMessageEnabled = true;
        public string commandsMessage = "Channel Commands:  !info";
        public bool infoMessageEnabled = true;
        public string infoMessage = "Please keep chat clean and respectful. Use !commands to see enabled channel commands.";

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

        // Color Options for announcement messages
        public readonly string[] ColorOptions = ["Normal", "Blue", "Green", "Orange", "Purple", "Primary"];
        private int message1ColorIndex = 0;
        private int message2ColorIndex = 0;
        private int message3ColorIndex = 0;
        private int message4ColorIndex = 0;
        private int message5ColorIndex = 0;

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

        /// <summary>
        /// Draws the mod configuration UI using Unity's IMGUI system.
        /// Handles user input and displays current connection status.
        /// </summary>
        public void DrawButtons()
        {
            GUILayout.Space(10);

            // Twitch Username Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Twitch Username:", GUILayout.Width(160));
                    twitchUsername = GUILayout.TextField(twitchUsername);
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Standard Messages Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Standard Messages");
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Connect Message:", GUILayout.Width(160));
                    Instance.connectMessage = GUILayout.TextField(Instance.connectMessage);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Disconnect Message:", GUILayout.Width(160));
                    Instance.disconnectMessage = GUILayout.TextField(Instance.disconnectMessage);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("New Follower Message:", GUILayout.Width(160));
                    GUILayout.Label(Instance.newFollowerMessage, GUILayout.Width(200));
                    GUI.color = Color.yellow;
                    GUILayout.Label("(Future Implementation)");
                    GUI.color = Color.white;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("New Subscriber Message:", GUILayout.Width(160));
                    GUILayout.Label(Instance.newSubscriberMessage, GUILayout.Width(200));
                    GUI.color = Color.yellow;
                    GUILayout.Label("(Future Implementation)");
                    GUI.color = Color.white;
                GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Command Messages Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Command Messages");
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                    // Instance.commandsMessageEnabled = GUILayout.Toggle(Instance.commandsMessageEnabled, "", GUILayout.Width(20));
                    GUILayout.Label("!Commands Message: ", GUILayout.Width(150));
                    GUILayout.Label(Instance.commandsMessage);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    bool prevInfoActive = Instance.infoMessageEnabled;
                    // Instance.infoMessageEnabled = GUILayout.Toggle(Instance.infoMessageEnabled, "", GUILayout.Width(20));
                    if (prevInfoActive != Instance.infoMessageEnabled)
                        CommandMessages.UpdateCommandsResponse();
                    GUILayout.Label("!Info Message: ", GUILayout.Width(150));
                    Instance.infoMessage = GUILayout.TextField(Instance.infoMessage);
                GUILayout.EndHorizontal();
                // GUILayout.Space(10);
                // GUILayout.Label("Custom Commands:");
                // GUI.color = Color.yellow;
                // GUILayout.Label("(Custom Commands Currently Disabled - Future Implementation)");
                // GUI.color = Color.white;
                // GUILayout.Label(" On?   Trigger Word       Response");

                // // Custom Commands
                // for (int i = 1; i <= 5; i++)
                // {
                //     GUILayout.BeginHorizontal();
                //         GUI.enabled = false;  // Disable interaction
                //         bool prevCmdActive = (bool)typeof(Settings).GetField($"customCommand{i}Active").GetValue(Instance);
                //         bool cmdActive = GUILayout.Toggle(prevCmdActive, "", GUILayout.Width(22));
                //         typeof(Settings).GetField($"customCommand{i}Active").SetValue(Instance, cmdActive);
                //         string trigger = (string)typeof(Settings).GetField($"customCommand{i}Trigger").GetValue(Instance);
                //         string response = (string)typeof(Settings).GetField($"customCommand{i}Response").GetValue(Instance);
                //         GUILayout.Label(trigger, GUILayout.Width(100));
                //         GUILayout.Label(response);
                //         GUI.enabled = true;  // Re-enable interaction
                //     GUILayout.EndHorizontal();
                // }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Timed Messages Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Timed Messages");
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

            // // Dispatcher Mod Integration Section
            // GUILayout.BeginVertical(GUI.skin.box);
            //     GUILayout.Label("Dispatcher Mod Integration");
            //     GUILayout.Space(10);
            //     // Add Dispatcher Messages configuration UI here
            //     GUI.color = Color.yellow;
            //     GUILayout.Label("(Future Implementation)");
            //     GUI.color = Color.white;
            // GUILayout.EndVertical();

            // GUILayout.Space(10);
            
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
        }

        /// <summary>
        /// Updates the settings based on UI interactions and handles various action flags.
        /// Called every frame to process pending actions.
        /// </summary>
        public void Update()
        {
            _ = this;
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
    /// Manages chat command responses and configurations.
    /// </summary>
    /// <remarks>
    /// Handles registration and updates of chat commands,
    /// maintaining the list of available commands and their responses.
    /// </remarks>
    public class CommandMessages
    {
        /// <summary>Current commands help message</summary>
        public static string commandMessage => Settings.Instance.commandsMessage;
        public static string infoMessage => Settings.Instance.infoMessage;
        public static void UpdateCommandsResponse()
        {
            var enabledCommands = new List<string>();
            
            if (Settings.Instance.infoMessageEnabled) enabledCommands.Add("!info");
            if (Settings.Instance.customCommand1Active) enabledCommands.Add($"!{Settings.Instance.customCommand1Trigger}");
            if (Settings.Instance.customCommand2Active) enabledCommands.Add($"!{Settings.Instance.customCommand2Trigger}");
            if (Settings.Instance.customCommand3Active) enabledCommands.Add($"!{Settings.Instance.customCommand3Trigger}");
            if (Settings.Instance.customCommand4Active) enabledCommands.Add($"!{Settings.Instance.customCommand4Trigger}");
            if (Settings.Instance.customCommand5Active) enabledCommands.Add($"!{Settings.Instance.customCommand5Trigger}");

            string response = "Channel Commands:" + string.Join("  ", enabledCommands);
            Settings.Instance.commandsMessage = response;
        }
    }

    /// <summary>
    /// Manages automated message scheduling and configuration.
    /// </summary>
    /// <remarks>
    /// Handles periodic message settings, timing, and color configurations
    /// for automated channel messages.
    /// </remarks>
    public class TimedMessages
    {
        /// <summary>Indicates if the timed message system is active</summary>
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
}