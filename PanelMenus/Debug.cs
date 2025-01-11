using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Manages the debug panel interface in the mod's settings menu.
    /// Provides controls for debug levels, message processing options,
    /// and testing functionality for notifications and WebSocket connections.
    /// </summary>
    public class DebugPanel : PanelConstructor.BasePanel
    {
        private Toggle? processOwn;
        private Toggle? processDuplicates;
        private GameObject? debugLevelSection;
        private GameObject? notificationTestsSection;
        private GameObject? testSendSection;
        private Button? debugLevelButton;  // Add field to store button reference

        public DebugPanel(Transform parent) : base(parent)
        {
            CreateDebugSetupSection();
            CreateNotificationTestsSection();
            CreateTestSendSection();
        }

        public void UpdateProcessOwn(bool value)
        {
            if (processOwn != null)
            {
                processOwn.isOn = value;
            }
        }

        public void UpdateProcessDuplicates(bool value)
        {
            if (processDuplicates != null)
            {
                processDuplicates.isOn = value;
            }
        }

        /// <summary>
        /// Creates a panel section containing debug level controls and message processing options.
        /// Includes toggles for processing own messages and duplicate messages,
        /// as well as a button to cycle through available debug levels.
        /// </summary>
        private void CreateDebugSetupSection()
        {
            // Dimensions - Menu width minus 20

            // Debug Section
            debugLevelSection = PanelConstructor.Section.Create(panelObject.transform, "Debug Level", 25, 95, false);
            
            // Debug Level button - modify the button creation
            debugLevelButton = PanelConstructor.Button.Create(debugLevelSection.transform, $"{Settings.Instance.debugLevel}", 35, 17, Color.yellow, () => {
                CycleDebugLevel();
            }, 50);
            
            // Debug Level label
            PanelConstructor.Label.Create(debugLevelSection.transform, "Debug Level", 70, 10);

            // Horizontal line
            PanelConstructor.HorizontalBar.Create(debugLevelSection.transform, 35);
            
            // Process Own label
            PanelConstructor.Label.Create(debugLevelSection.transform, "Allow Own Name", 70, 44);
            
            // Processe own messages toggle
            processOwn = PanelConstructor.Toggle.Create(debugLevelSection.transform, 35, 50, "Enabled", "Disabled", Settings.Instance.processOwn);

            // Add listener after toggle creation
            processOwn.onValueChanged.AddListener((value) => {
                Settings.Instance.processOwn = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllProcessOwnToggles(value);
            });

            // Horizontal line
            PanelConstructor.HorizontalBar.Create(debugLevelSection.transform, 65);

            // Process Duplicates label
            PanelConstructor.Label.Create(debugLevelSection.transform, "Allow Duplicates", 70, 74);

            // Process Duplicates toggle
            processDuplicates = PanelConstructor.Toggle.Create(debugLevelSection.transform, 35, 80, "Enabled", "Disabled", Settings.Instance.processDuplicates);

            // Add listener after toggle creation
            processDuplicates.onValueChanged.AddListener((value) => {
                Settings.Instance.processDuplicates = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllProcessDuplicatesToggles(value);
            });
        }

        /// <summary>
        /// Cycles through available debug levels (Off, Minimal, Reduced, Full)
        /// and updates both the settings and UI to reflect the new level.
        /// </summary>
        private void CycleDebugLevel()
        {
            // Get current debug level and calculate next level
            int currentLevel = (int)Settings.Instance.debugLevel;
            int nextLevel = (currentLevel + 1) % 4; // 4 is the number of debug levels
            
            // Update settings
            Settings.Instance.debugLevel = (DebugLevel)nextLevel;
            Settings.Instance.Save(Main.ModEntry);

            // Update button text
            if (debugLevelButton != null)
            {
                debugLevelButton.GetComponentInChildren<Text>().text = $"{Settings.Instance.debugLevel}";
            }
        }

        /// <summary>
        /// Creates a section containing buttons for testing the notification system.
        /// Includes tests for direct notification attachment and message queue functionality.
        /// </summary>
        private void CreateNotificationTestsSection()
        {
            // Dimensions - Menu width minus 20

            // Test Notifications Section
            notificationTestsSection = PanelConstructor.Section.Create(panelObject.transform, "Notification Tests", 135, 80);
            
            // Direct Attachment Notification Test
            PanelConstructor.Button.Create(notificationTestsSection.transform, "Direct Attachment Test", 90, 35, Color.white, () => NotificationManager.AttachNotification("Direct Attachment Notification Test", "null"));

            // Mesage Queue Notification Test
            PanelConstructor.Button.Create(notificationTestsSection.transform, "Message Queue Test", 90, 60, Color.white, () => NotificationManager.WebSocketNotificationTest());
        }

        /// <summary>
        /// Creates a section for testing WebSocket functionality,
        /// including the ability to send test messages to verify connection status.
        /// </summary>
        private void CreateTestSendSection()
        {
            // Dimensions - Menu width minus 20

            // Test Send Section
            testSendSection = PanelConstructor.Section.Create(panelObject.transform, "WebSocket Test", 230, 55);
            
            // Send Test Message
            PanelConstructor.Button.Create(testSendSection.transform, "Send Test Message", 90, 35, Color.white, async () => await TwitchEventHandler.SendMessage("Test message sent from debug page. If you see this mesage on your channel (and in game), your Twitch Authentication Token is valid and working!"));
        }

        public override void Show()
        {
            base.Show();
            debugLevelSection?.SetActive(!isMinimized);
            notificationTestsSection?.SetActive(!isMinimized);
            testSendSection?.SetActive(!isMinimized);
        }
    }
}
