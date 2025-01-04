using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class NotificationMenu : BaseMenu
    {
        public static Toggle notificationsEnabled;
        private Slider notificationDuration;
        private GameObject notificationSection;

        public NotificationMenu(Transform parent) : base(parent)
        {
            CreateNotificationSettingssSection();
        }

        private void CreateNotificationSettingssSection()
        {
            // Dimensions - Menu width minus 20

            // Notifications Section
            notificationSection = CreateSection("Notifications", 25, 240, false);
            
            // Enabled?
            CreateLabel(notificationSection.transform, "Toggle", 10, 10, Color.white);

            // Toggle button with value change listener
            notificationsEnabled = CreateToggle(notificationSection.transform, 90, 35, "Enabled", "Disabled", Settings.Instance.notificationsEnabled);
            notificationsEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.notificationsEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Horizontal line
            CreateHorizontalBar(notificationSection.transform, 60);
            
            // Duration
            CreateLabel(notificationSection.transform, "Duration", 10, 70, Color.white);
            
            // Duration slider
            notificationDuration = CreateSlider(notificationSection.transform, 0, 95, 1, 120, Settings.Instance.notificationDuration);
            
            notificationDuration.onValueChanged.AddListener((value) => {
                Settings.Instance.notificationDuration = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Horizontal line
            CreateHorizontalBar(notificationSection.transform, 130);
            
            // Always or only away from display boards?
            CreateLabel(notificationSection.transform, "Min distance from boards", 10, 140, Color.white);

            // Location multi-selction
            CreateTextDisplay(notificationSection.transform, "Future development", 25, 160, Color.yellow);
            // TODO: Add multi-selection dropdown for location
            
            // Horizontal line
            CreateHorizontalBar(notificationSection.transform, 185);
            
            // Limit number of messages displayed?
            CreateLabel(notificationSection.transform, "Limit number displayed", 10, 195, Color.white);

            // Limit dropdown
            CreateTextDisplay(notificationSection.transform, "Future development", 25, 215, Color.yellow);
            // TODO: Add dropdown for limit
        }

        public override void Show()
        {
            base.Show();
            if (notificationSection != null) notificationSection.SetActive(!isMinimized);
        }
    }
}
