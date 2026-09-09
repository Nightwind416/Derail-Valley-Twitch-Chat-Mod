using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>How many cab displays a single locomotive type can carry.</summary>
        public const int MaxDisplaysPerCar = 5;

        /// <summary>
        /// The connected account's Twitch login name. Filled in from the token when the account is
        /// linked, so there is nothing for the user to type and nothing to type wrongly.
        /// </summary>
        public string twitchUsername = string.Empty;
        public string authentication_status = "Unverified or not set";

        /// <summary>Access token, written by <see cref="TokenStore"/>. Never log this.</summary>
        public string EncodedOAuthToken = string.Empty;

        /// <summary>Refresh token, written by <see cref="TokenStore"/>. Never log this.</summary>
        public string EncodedRefreshToken = string.Empty;
        public DebugLevel debugLevel = DebugLevel.Minimal;
        public bool notificationsEnabled = true;
        public float notificationDuration = 10;
        public bool processOwn = true;
        public bool processDuplicates = false;
        
        // Panel UI Settings. These are the defaults every panel starts from; a panel the player has given
        // colours of its own has an entry in panelAppearances instead.
        public Color panelColor = new(0, 0, 0, 0.3f);
        public Color sectionColor = new(0, 0, 0, 0.1f);
        public Color buttonColor = new(0, 0, 0, 0.5f);

        /// <summary>Default panel colour, shared by the reset buttons and by a panel that has no entry.</summary>
        public static readonly Color DefaultPanelColor = new(0, 0, 0, 0.3f);
        public static readonly Color DefaultSectionColor = new(0, 0, 0, 0.1f);
        public static readonly Color DefaultButtonColor = new(0, 0, 0, 0.5f);

        /// <summary>
        /// Colours the player has set for one panel on one display. Only the pairs they have actually
        /// changed are in here; everything else falls back to the three colours above.
        /// </summary>
        public List<PanelAppearance> panelAppearances = new();

        // Cab Display and Wrist Panel Settings
        public bool cabDisplayVisible = true;
        public float cabDisplayDistance = 0.7f;
        public float cabDisplayScale = 1.0f;
        public bool cabDisplayGrabHandles = true;
        public string placeDisplayKey = "F7";
        public string toggleDisplayKey = "F8";

        /// <summary>Every saved display, for every locomotive type. Grouped by <see cref="CabDisplayPose.carId"/>.</summary>
        public List<CabDisplayPose> cabDisplayPoses = new();

        /// <summary>
        /// Ids of the plugin panels the player has switched off, separated by commas. Kept as one string
        /// rather than a list so the Unity Mod Manager page and the settings file both stay readable, and
        /// so a plugin that is not installed keeps its setting instead of being quietly dropped.
        /// </summary>
        public string disabledPanels = string.Empty;
        public bool wristPanelEnabled = true;
        public string wristPanel = "Main";
        public bool wristPanelOnLeftHand = true;
        public float wristPanelScale = 0.6f;

        /// <summary>
        /// Open and close the wrist panel with a controller button rather than by pressing a button on the
        /// hand. This is the way in now; the button on the hand is kept for anyone whose controller or
        /// bindings do not give them a spare press.
        /// </summary>
        public bool wristToggleEnabled = true;

        /// <summary>Which hand's button opens the panel. The left one by default, whichever hand wears it.</summary>
        public bool wristToggleOnLeftHand = true;

        /// <summary>
        /// Which button that is, by name rather than as an enum, so a value this version does not know
        /// falls back to the thumbstick instead of refusing to load the settings file at all.
        /// </summary>
        public string wristToggleButton = "Thumbstick";

        /// <summary>The button on the back of the hand, which the controller button has replaced.</summary>
        public bool wristHandButton = false;

        /// <summary>
        /// Where the open menus sit, measured from where the mod puts them on the back of the hand, in metres:
        /// x across the menus, y along them, z out of the hand. Zero leaves them where the mod puts them.
        /// </summary>
        public Vector3 wristMenuOffset = Vector3.zero;

        /// <summary>Extra turn in degrees on top of that, for players who want the menus at another angle.</summary>
        public Vector3 wristMenuAngle = Vector3.zero;

        /// <summary>
        /// Where the button the menus fold down to sits, in the same terms. Kept apart from the menus, since the
        /// button wants to lie on the back of the hand while the menus want to stand up where they can be read.
        /// </summary>
        public Vector3 wristButtonOffset = Vector3.zero;

        /// <summary>Extra turn in degrees for the button, on top of where the mod puts it.</summary>
        public Vector3 wristButtonAngle = Vector3.zero;
        
        // Standard Messages Settings
        public bool connectMessageEnabled = true;
        public string connectMessage = "TwitchChat mod connected! Messages are being relayed to in-game panels.";
        public bool newFollowerMessageEnabled = true;
        public string newFollowerMessage = "Welcome to the crew!";
        public bool newSubscriberMessageEnabled = true;
        public string newSubscriberMessage = "Thank you for subscribing!";
        public bool disconnectMessageEnabled = true;
        public string disconnectMessage = "TwitchChat mod disconnected! Messages are no longer being relayed to in-game panels.";

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

        /// <summary>
        /// The controller buttons the wrist panel can be opened with, in the order the settings button
        /// cycles through them. Menu is last because on most systems that is the headset's own button.
        /// </summary>
        public static readonly string[] WristToggleButtons = ["Thumbstick", "A", "B", "Menu"];

        /// <summary>The configured button, or the thumbstick if the settings file names one we do not know.</summary>
        public string WristToggleButtonName =>
            Array.IndexOf(WristToggleButtons, wristToggleButton) >= 0 ? wristToggleButton : WristToggleButtons[0];

        private void CycleWristToggleButton()
        {
            int index = Array.IndexOf(WristToggleButtons, WristToggleButtonName);
            wristToggleButton = WristToggleButtons[(index + 1) % WristToggleButtons.Length];
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
        /// Draws a labelled horizontal slider with its current value and returns the (possibly changed) value.
        /// </summary>
        private static float SliderRow(string label, float value, float min, float max, string format)
        {
            GUILayout.BeginHorizontal();
                GUILayout.Label(label, GUILayout.Width(160));
                float result = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(200));
                GUILayout.Label(result.ToString(format), GUILayout.Width(60));
            GUILayout.EndHorizontal();
            return result;
        }

        /// <summary>
        /// Draws the mod configuration UI using Unity's IMGUI system.
        /// Handles user input and displays current connection status.
        /// </summary>
        public void DrawButtons()
        {
            GUILayout.Space(10);

            // Twitch Account Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Twitch Account");
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Account:", GUILayout.Width(160));
                    bool linked = OAuthTokenManager.Phase == AuthPhase.Connected && !string.IsNullOrEmpty(twitchUsername);
                    GUI.color = linked ? Color.green : Color.yellow;
                    GUILayout.Label(linked ? twitchUsername : "Not connected");
                    GUI.color = Color.white;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Status:", GUILayout.Width(160));
                    GUILayout.Label(OAuthTokenManager.StatusMessage);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.Label("Connect and disconnect your account from the Authentication panel in game.");
                GUILayout.Label($"Access token storage on this PC: {(TokenStore.IsEncrypted ? "encrypted (Windows DPAPI)" : "encoded only - encryption unavailable on this system")}");
                if (!string.IsNullOrEmpty(EncodedOAuthToken))
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button("Sign Out & Revoke Access", GUILayout.Width(200)))
                    {
                        _ = OAuthTokenManager.RevokeAndSignOut();
                    }
                }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Standard Messages Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Standard Messages");
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                    GUI.color = Instance.connectMessageEnabled ? Color.green : Color.red;
                    GUILayout.Label("Connect Message:", GUILayout.Width(160));
                    Instance.connectMessage = GUILayout.TextField(Instance.connectMessage);
                    GUI.color = Color.white;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUI.color = Instance.disconnectMessageEnabled ? Color.green : Color.red;
                    GUILayout.Label("Disconnect Message:", GUILayout.Width(160));
                    Instance.disconnectMessage = GUILayout.TextField(Instance.disconnectMessage);
                    GUI.color = Color.white;
                GUILayout.EndHorizontal();
                // GUILayout.BeginHorizontal();
                //     GUILayout.Label("New Follower Message:", GUILayout.Width(160));
                //     GUILayout.Label(Instance.newFollowerMessage, GUILayout.Width(200));
                //     GUI.color = Color.yellow;
                //     GUILayout.Label("(Future Implementation)");
                //     GUI.color = Color.white;
                // GUILayout.EndHorizontal();
                // GUILayout.BeginHorizontal();
                //     GUILayout.Label("New Subscriber Message:", GUILayout.Width(160));
                //     GUILayout.Label(Instance.newSubscriberMessage, GUILayout.Width(200));
                //     GUI.color = Color.yellow;
                //     GUILayout.Label("(Future Implementation)");
                //     GUI.color = Color.white;
                // GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Command Messages Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Command Messages");
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                    GUI.color = Instance.commandsMessageEnabled ? Color.green : Color.red;
                    // Instance.commandsMessageEnabled = GUILayout.Toggle(Instance.commandsMessageEnabled, "", GUILayout.Width(20));
                    GUILayout.Label("!Commands Message: ", GUILayout.Width(150));
                    GUILayout.Label(Instance.commandsMessage);
                    GUI.color = Color.white;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    bool prevInfoActive = Instance.infoMessageEnabled;
                    GUI.color = Instance.infoMessageEnabled ? Color.green : Color.red;
                    // Instance.infoMessageEnabled = GUILayout.Toggle(Instance.infoMessageEnabled, "", GUILayout.Width(20));
                    if (prevInfoActive != Instance.infoMessageEnabled)
                        CommandMessages.UpdateCommandsResponse();
                    GUILayout.Label("!Info Message: ", GUILayout.Width(150));
                    Instance.infoMessage = GUILayout.TextField(Instance.infoMessage);
                    GUI.color = Color.white;
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
                    bool newState = !Instance.timedMessageSystemToggle;
                    Instance.timedMessageSystemToggle = newState;
                    AutomatedMessages.ToggleTimedMessages();
                    Instance.Save(Main.ModEntry);
                    MenuManager.Instance.UpdateAllTimedMessageToggles(newState);
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
                GUILayout.Label("Colors/types are not fully implemented yet, except 'Blue'.");
                GUILayout.Label("Blue will send as a channel 'announcment' type, at the given interval.");
                GUILayout.Label("All other color choices will send a regular chat message, at their designated intervals.");
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
            
            // Cab Display and Wrist Panel Section
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Cab Display and Wrist Panel");
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Place Cab Display In Front Of Me", GUILayout.Width(230)))
                    {
                        MenuManager.Instance.PlaceCabDisplay();
                    }
                    if (GUILayout.Button(cabDisplayVisible ? "Hide Cab Display" : "Show Cab Display", GUILayout.Width(150)))
                    {
                        MenuManager.Instance.ToggleCabDisplay();
                    }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Place key:", GUILayout.Width(160));
                    placeDisplayKey = GUILayout.TextField(placeDisplayKey, GUILayout.Width(80));
                    GUILayout.Label("Toggle key:", GUILayout.Width(80));
                    toggleDisplayKey = GUILayout.TextField(toggleDisplayKey, GUILayout.Width(80));
                    GUILayout.Label("(Unity KeyCode names, for example F7 or Keypad1)");
                GUILayout.EndHorizontal();
                cabDisplayDistance = SliderRow("Placement distance (m)", cabDisplayDistance, 0.3f, 2.0f, "0.00");
                cabDisplayScale = SliderRow("Cab display scale", cabDisplayScale, 0.5f, 2.0f, "0.00");
                cabDisplayGrabHandles = GUILayout.Toggle(cabDisplayGrabHandles, $" Grab bars around each display (VR only: grip a bar to move it, trigger to drag that edge and resize it)");
                GUILayout.Label($"    Up to {MaxDisplaysPerCar} displays per locomotive type. Place, lock and close them from the in-game Displays panel.");
                GUILayout.Space(5);
                wristPanelEnabled = GUILayout.Toggle(wristPanelEnabled, " Wrist panel (VR only)");
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Wrist panel hand:", GUILayout.Width(160));
                    if (GUILayout.Button(wristPanelOnLeftHand ? "Left" : "Right", GUILayout.Width(80)))
                    {
                        wristPanelOnLeftHand = !wristPanelOnLeftHand;
                    }
                GUILayout.EndHorizontal();
                wristPanelScale = SliderRow("Wrist panel scale", wristPanelScale, 0.2f, 1.5f, "0.00");
                wristToggleEnabled = GUILayout.Toggle(wristToggleEnabled, " Open and close the wrist panel with a controller button");
                GUI.enabled = wristToggleEnabled;
                GUILayout.BeginHorizontal();
                    GUILayout.Label("    Button hand:", GUILayout.Width(160));
                    if (GUILayout.Button(wristToggleOnLeftHand ? "Left" : "Right", GUILayout.Width(80)))
                    {
                        wristToggleOnLeftHand = !wristToggleOnLeftHand;
                    }
                    GUILayout.Label("Button:", GUILayout.Width(60));
                    if (GUILayout.Button(WristToggleButtonName, GUILayout.Width(100)))
                    {
                        CycleWristToggleButton();
                    }
                GUILayout.EndHorizontal();
                GUI.enabled = true;
                GUILayout.Label("    The game may use that button for something of its own, in which case both things happen at once.");
                GUILayout.Label("    If that is a nuisance, pick A or B, or the other hand. Menu is the headset's own button on most systems.");
                wristHandButton = GUILayout.Toggle(wristHandButton, " Also show the button on the back of the hand");
                GUILayout.Label("    The button comes back on its own if the controller button above is switched off, so there is");
                GUILayout.Label("    always some way to open the panel.");
                GUILayout.Space(5);
                GUILayout.Label("    The panel sits on the back of the hand by itself. To move it, use the in-game Wrist Adjust panel,");
                GUILayout.Label("    which places the button and the open menus separately and can be reached from any Main panel.");
                if (GUILayout.Button("Reset wrist panel to defaults", GUILayout.Width(200)))
                {
                    wristMenuOffset = Vector3.zero;
                    wristMenuAngle = Vector3.zero;
                    wristButtonOffset = Vector3.zero;
                    wristButtonAngle = Vector3.zero;
                    wristPanelScale = 0.6f;
                }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            DrawModPanels();

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
        /// Draws the list of panels contributed by plugins, so a player can see what was found and switch
        /// any of it off without going in game. The same list, with the same switches, is on the in-game
        /// Mods panel.
        /// </summary>
        private void DrawModPanels()
        {
            GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Mod Panels");

                List<Plugins.PanelDescriptor> plugins = Plugins.PanelRegistry.AllPlugins.ToList();
                if (plugins.Count == 0)
                {
                    GUILayout.Label("    None installed. Panels for other mods are dropped into the mod's Plugins folder.");
                }

                foreach (Plugins.PanelDescriptor plugin in plugins)
                {
                    bool off = IsPanelDisabled(plugin.Id);

                    GUILayout.BeginHorizontal();
                        if (GUILayout.Button(off ? "Off" : "On", GUILayout.Width(50)))
                        {
                            SetPanelDisabled(plugin.Id, !off);
                        }
                        GUILayout.Label(plugin.Title, GUILayout.Width(160));

                        // The panel exists for the mod it reports on, so saying whether that mod was found
                        // is the answer to "why is this panel not in my menu"
                        GUILayout.Label(off
                            ? "switched off"
                            : plugin.IsAvailable ? "ready" : "the mod it reports on was not found");
                    GUILayout.EndHorizontal();
                }

                if (plugins.Count > 0)
                {
                    GUILayout.Label("    Changes apply the next time you board a locomotive, when the displays are rebuilt.");
                }
            GUILayout.EndVertical();
        }

        /// <summary>Whether the player has switched a plugin panel off.</summary>
        public bool IsPanelDisabled(string panelId) => DisabledPanelIds().Contains(panelId);

        /// <summary>
        /// Switches a plugin panel on or off. Takes effect the next time a display's panel stack is built,
        /// which is on boarding a locomotive.
        /// </summary>
        public void SetPanelDisabled(string panelId, bool disabled)
        {
            List<string> ids = DisabledPanelIds();

            if (disabled)
            {
                if (ids.Contains(panelId)) return;
                ids.Add(panelId);
            }
            else if (!ids.Remove(panelId))
            {
                return;
            }

            disabledPanels = string.Join(",", ids);
            RequestSave();
        }

        // ------------------------------------------------------------------
        // Per-display, per-panel colours
        // ------------------------------------------------------------------

        /// <summary>
        /// The colours saved for one panel on one display, or null if the player has never changed them
        /// and it should follow the defaults.
        /// </summary>
        public PanelAppearance? FindAppearance(string displayId, string panelId)
        {
            return panelAppearances.FirstOrDefault(a => a.displayId == displayId && a.panelId == panelId);
        }

        /// <summary>
        /// The colours to paint one panel with: its own if it has any, the shared defaults if not. The
        /// result is a copy, so painting with it can never write back into the saved entry by accident.
        /// </summary>
        public PanelAppearance EffectiveAppearance(string displayId, string panelId)
        {
            PanelAppearance? saved = FindAppearance(displayId, panelId);

            return new PanelAppearance
            {
                displayId = displayId,
                panelId = panelId,
                panelColor = saved?.panelColor ?? panelColor,
                sectionColor = saved?.sectionColor ?? sectionColor,
                buttonColor = saved?.buttonColor ?? buttonColor
            };
        }

        /// <summary>
        /// The entry for one panel on one display, creating it from the current defaults if this is the
        /// first change. Having an entry at all is what makes a panel stop following the defaults.
        /// </summary>
        public PanelAppearance GetOrAddAppearance(string displayId, string panelId)
        {
            PanelAppearance? existing = FindAppearance(displayId, panelId);
            if (existing != null)
            {
                return existing;
            }

            PanelAppearance created = new()
            {
                displayId = displayId,
                panelId = panelId,
                panelColor = panelColor,
                sectionColor = sectionColor,
                buttonColor = buttonColor
            };

            panelAppearances.Add(created);
            RequestSave();
            return created;
        }

        /// <summary>Puts one panel back on the shared defaults by forgetting its own colours.</summary>
        public void ResetAppearance(string displayId, string panelId)
        {
            if (panelAppearances.RemoveAll(a => a.displayId == displayId && a.panelId == panelId) > 0)
            {
                RequestSave();
            }
        }

        /// <summary>Forgets every colour saved for one display, for when that display is closed.</summary>
        public void ForgetAppearances(string displayId)
        {
            if (panelAppearances.RemoveAll(a => a.displayId == displayId) > 0)
            {
                RequestSave();
            }
        }

        /// <summary>
        /// Brings a settings file written by an earlier version up to date, and tidies away anything left
        /// over. Called once, immediately after the file is read.
        /// </summary>
        /// <remarks>
        /// The display ids are the important part: they are what a panel's colours are filed under, so they
        /// have to exist and have to stay the same. A pose read from a file that predates them keeps the id
        /// its field initializer minted, and this writes that out at once rather than letting it be minted
        /// afresh on every launch.
        /// </remarks>
        public void UpgradeAfterLoad(UnityModManager.ModEntry modEntry)
        {
            bool changed = false;

            foreach (CabDisplayPose pose in cabDisplayPoses)
            {
                if (string.IsNullOrEmpty(pose.id))
                {
                    pose.id = Guid.NewGuid().ToString("N");
                    changed = true;
                }
            }

            // Colours for displays that have since been closed would otherwise pile up for ever
            HashSet<string> live = new(cabDisplayPoses.Select(p => p.id)) { "Wrist" };
            changed |= panelAppearances.RemoveAll(a => !live.Contains(a.displayId)) > 0;

            if (changed)
            {
                // Not RequestSave: that is flushed from the game loop, and these ids must be on disk before
                // anything can be filed under them
                Save(modEntry);
                Main.LogEntry("Load", "Settings file brought up to date: display ids filled in.");
            }
        }
        private List<string> DisabledPanelIds()
        {
            return string.IsNullOrWhiteSpace(disabledPanels)
                ? new List<string>()
                : disabledPanels.Split(',').Select(id => id.Trim()).Where(id => id.Length > 0).ToList();
        }

        // Deferred saving: panel sliders fire on every tick, so writes are coalesced and flushed after a short quiet period.
        private bool saveRequested;
        private DateTime saveRequestedAt;
        private static readonly TimeSpan saveDelay = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Marks the settings as changed. The file is written by <see cref="FlushPendingSave"/> once changes stop for a second.
        /// Use this from in-game panel controls; use <see cref="Save(UnityModManager.ModEntry)"/> when the write must happen now.
        /// </summary>
        public void RequestSave()
        {
            saveRequested = true;
            saveRequestedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Writes pending changes to disk. Called every frame by the MenuManager; pass true to write immediately.
        /// </summary>
        /// <param name="force">Write now even if changes are still arriving.</param>
        public void FlushPendingSave(bool force = false)
        {
            if (!saveRequested) return;
            if (!force && DateTime.UtcNow - saveRequestedAt < saveDelay) return;
            saveRequested = false;
            Save(Main.ModEntry);
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
    /// Where the cab display sits inside one locomotive type, stored relative to the car interior transform.
    /// </summary>
    [Serializable]
    /// <summary>
    /// One saved cab display: where it sits inside a locomotive type, how big it is, which panel it was
    /// last showing, and whether the player has pinned it down. Up to
    /// <see cref="Settings.MaxDisplaysPerCar"/> of these share a <see cref="carId"/>, one per display.
    /// </summary>
    /// <remarks>
    /// Everything past <see cref="localEuler"/> was added after the single-display versions, so settings
    /// files written by those simply fall back to the defaults here.
    /// </remarks>
    public class CabDisplayPose
    {
        /// <summary>
        /// What this display is known by for as long as it exists, so its colours can be filed against it.
        /// Minted here rather than where displays are placed, which also gives one to a pose read from a
        /// settings file written before this field existed, since that file simply has nothing to overwrite
        /// it with. Closing a display and placing a new one gives a new id, and so the default colours.
        /// </summary>
        public string id = Guid.NewGuid().ToString("N");

        public string carId = string.Empty;
        public Vector3 localPosition;
        public Vector3 localEuler;

        /// <summary>Panel size in canvas units. Zero until the display is first shown, when the panel's own preset seeds it.</summary>
        public Vector2 panelSize = Vector2.zero;

        /// <summary>The panel this display was last showing.</summary>
        public string activePanel = "Main";

        /// <summary>Pinned in place: the grip no longer carries it.</summary>
        public bool lockPosition;

        /// <summary>Pinned at its size: the trigger no longer resizes it.</summary>
        public bool lockSize;

        /// <summary>Folded away to its title strip. Saved, so a display tidied away stays tidied away.</summary>
        public bool minimized;
    }

    /// <summary>
    /// The colours one panel has been given on one display. A pair with no entry here follows the shared
    /// defaults on <see cref="Settings"/>, so the presence of an entry is what marks a panel as customised
    /// and removing it is what puts the panel back on the defaults.
    /// </summary>
    [Serializable]
    public class PanelAppearance
    {
        /// <summary>The display: a <see cref="CabDisplayPose.id"/>, or "Wrist" for the wrist panel.</summary>
        public string displayId = string.Empty;

        /// <summary>The panel, by the id the registry knows it as.</summary>
        public string panelId = string.Empty;

        public Color panelColor;
        public Color sectionColor;
        public Color buttonColor;
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