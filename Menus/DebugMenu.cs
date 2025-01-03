using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class DebugMenu : BaseMenu
    {
        public static Toggle processOwn;
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public DebugMenu(Transform parent) : base(parent)
        {
            CreateDebugMenu();
            CreateDebugSetupSection();
            CreateNotificationTestsSection();
            CreateTestSendSection();
        }

        private void CreateDebugMenu()
        {
            // Dimensions - 200x300
            
            // Title
            CreateTitle("Debug Menu", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 190, 10, Color.white, () => OnBackButtonClicked?.Invoke());
        }

        private void CreateDebugSetupSection()
        {
            // Dimensions - Menu width minus 20

            // Debug Section
            GameObject debugLevelSection = CreateSection("Debug Level", 25, 65, false);
            
            // Debug Level
            CreateTextDisplay(debugLevelSection.transform, "Debug Level", 10, 10);
            
            // Debug level dropdown selection
            // Dropdown

            // Horizontal line
            CreateHorizontalBar(debugLevelSection.transform, 35);

            // Ignore self
            CreateTextDisplay(debugLevelSection.transform, "Process Own", 10, 45);
            
            // Processe own messagese
            processOwn = CreateToggle(debugLevelSection.transform, 120, 55, "Enabled", "Disabled", Settings.Instance.processOwn);
            processOwn.onValueChanged.AddListener((value) => {
                Settings.Instance.processOwn = value;
                Settings.Instance.Save(Main.ModEntry);
            });
        }
        private void CreateNotificationTestsSection()
        {
            // Dimensions - Menu width minus 20

            // Test Notifications Section
            GameObject notificationTestsSection = CreateSection("Notification Tests", 105, 80);
            
            // Direct Attachment Notification Test
            CreateButton(notificationTestsSection.transform, "Direct Attachment Test", 90, 35, Color.white, () => NotificationManager.AttachNotification("Direct Attachment Notification Test", "null"));

            // Mesage Queue Notification Test
            CreateButton(notificationTestsSection.transform, "Message Queue Test", 90, 60, Color.white, () => NotificationManager.WebSocketNotificationTest());
        }
        private void CreateTestSendSection()
        {
            // Dimensions - Menu width minus 20

            // Test Send Section
            GameObject TestSendSection = CreateSection("Test Send", 200, 55);
            
            // Send Test Message
            CreateButton(TestSendSection.transform, "Send Test Message", 90, 35, Color.white, async () => await TwitchEventHandler.SendMessage("Test message sent 'from' debug page. If you see this mesage on your channel, your Authentication Token valid and working!"));
        }
    }
}
