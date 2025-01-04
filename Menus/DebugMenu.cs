using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class DebugMenu : BaseMenu
    {
        public static Toggle processOwn;
        public static Toggle processDuplicates;
        private GameObject debugLevelSection;
        private GameObject notificationTestsSection;
        private GameObject testSendSection;

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
            debugLevelSection = CreateSection("Debug Level", 25, 95, false);
            
            // Debug Level
            UIElementFactory.CreateLabel(debugLevelSection.transform, "Debug Level", 10, 10);
            
            // Debug level dropdown selection
            // Dropdown

            // Horizontal line
            CreateHorizontalBar(debugLevelSection.transform, 35);
            
            // Processe own messagese
            processOwn = UIElementFactory.CreateToggle(debugLevelSection.transform, 35, 50, "Enabled", "Disabled", Settings.Instance.processOwn);
            processOwn.onValueChanged.AddListener((value) => {
                Settings.Instance.processOwn = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Process Own
            UIElementFactory.CreateLabel(debugLevelSection.transform, "Process Own", 70, 45);

            // Horizontal line
            CreateHorizontalBar(debugLevelSection.transform, 65);

            // Process Duplicates
            processDuplicates = UIElementFactory.CreateToggle(debugLevelSection.transform, 35, 80, "Enabled", "Disabled", Settings.Instance.processDuplicates);
            processDuplicates.onValueChanged.AddListener((value) => {
                Settings.Instance.processDuplicates = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Process Duplicates
            UIElementFactory.CreateLabel(debugLevelSection.transform, "Process Duplicates", 70, 75);
        }
        private void CreateNotificationTestsSection()
        {
            // Dimensions - Menu width minus 20

            // Test Notifications Section
            notificationTestsSection = CreateSection("Notification Tests", 135, 80);
            
            // Direct Attachment Notification Test
            UIElementFactory.CreateButton(notificationTestsSection.transform, "Direct Attachment Test", 90, 35, Color.white, () => NotificationManager.AttachNotification("Direct Attachment Notification Test", "null"));

            // Mesage Queue Notification Test
            UIElementFactory.CreateButton(notificationTestsSection.transform, "Message Queue Test", 90, 60, Color.white, () => NotificationManager.WebSocketNotificationTest());
        }
        private void CreateTestSendSection()
        {
            // Dimensions - Menu width minus 20

            // Test Send Section
            testSendSection = CreateSection("Test Send", 230, 55);
            
            // Send Test Message
            UIElementFactory.CreateButton(testSendSection.transform, "Send Test Message", 90, 35, Color.white, async () => await TwitchEventHandler.SendMessage("Test message sent 'from' debug page. If you see this mesage on your channel, your Authentication Token valid and working!"));
        }

        public override void Show()
        {
            base.Show();
            if (debugLevelSection != null) debugLevelSection.SetActive(!isMinimized);
            if (notificationTestsSection != null) notificationTestsSection.SetActive(!isMinimized);
            if (testSendSection != null) testSendSection.SetActive(!isMinimized);
        }
    }
}
