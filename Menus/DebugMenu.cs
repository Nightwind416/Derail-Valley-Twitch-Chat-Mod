using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class DebugMenu : MenuConstructor.BaseMenu
    {
        public static UnityEngine.UI.Toggle? processOwn;
        public static UnityEngine.UI.Toggle? processDuplicates;
        private GameObject? debugLevelSection;
        private GameObject? notificationTestsSection;
        private GameObject? testSendSection;

        public DebugMenu(Transform parent) : base(parent)
        {
            CreateDebugSetupSection();
            CreateNotificationTestsSection();
            CreateTestSendSection();
        }

        private void CreateDebugSetupSection()
        {
            // Dimensions - Menu width minus 20

            // Debug Section
            debugLevelSection = MenuConstructor.Section.Create(menuObject.transform, "Debug Level", 25, 95, false);
            
            // Debug Level
            MenuConstructor.Label.Create(debugLevelSection.transform, "Debug Level", 10, 10);
            
            // Debug level dropdown selection
            // Dropdown

            // Horizontal line
            MenuConstructor.HorizontalBar.Create(debugLevelSection.transform, 35);
            
            // Processe own messagese
            processOwn = MenuConstructor.Toggle.Create(debugLevelSection.transform, 35, 50, "Enabled", "Disabled", Settings.Instance.processOwn);
            processOwn.onValueChanged.AddListener((value) => {
                Settings.Instance.processOwn = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Process Own
            MenuConstructor.Label.Create(debugLevelSection.transform, "Process Own", 70, 45);

            // Horizontal line
            MenuConstructor.HorizontalBar.Create(debugLevelSection.transform, 65);

            // Process Duplicates
            processDuplicates = MenuConstructor.Toggle.Create(debugLevelSection.transform, 35, 80, "Enabled", "Disabled", Settings.Instance.processDuplicates);
            processDuplicates.onValueChanged.AddListener((value) => {
                Settings.Instance.processDuplicates = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Process Duplicates
            MenuConstructor.Label.Create(debugLevelSection.transform, "Process Duplicates", 70, 75);
        }
        private void CreateNotificationTestsSection()
        {
            // Dimensions - Menu width minus 20

            // Test Notifications Section
            notificationTestsSection = MenuConstructor.Section.Create(menuObject.transform, "Notification Tests", 135, 80);
            
            // Direct Attachment Notification Test
            MenuConstructor.Button.Create(notificationTestsSection.transform, "Direct Attachment Test", 90, 35, Color.white, () => NotificationManager.AttachNotification("Direct Attachment Notification Test", "null"));

            // Mesage Queue Notification Test
            MenuConstructor.Button.Create(notificationTestsSection.transform, "Message Queue Test", 90, 60, Color.white, () => NotificationManager.WebSocketNotificationTest());
        }
        private void CreateTestSendSection()
        {
            // Dimensions - Menu width minus 20

            // Test Send Section
            testSendSection = MenuConstructor.Section.Create(menuObject.transform, "Test Send", 230, 55);
            
            // Send Test Message
            MenuConstructor.Button.Create(testSendSection.transform, "Send Test Message", 90, 35, Color.white, async () => await TwitchEventHandler.SendMessage("Test message sent 'from' debug page. If you see this mesage on your channel, your Authentication Token valid and working!"));
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
