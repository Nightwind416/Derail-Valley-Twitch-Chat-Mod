using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class NotificationMenu : MenuConstructor.BaseMenu
    {
        private Toggle? notificationsEnabled;
        private Slider? notificationDuration;

        private GameObject? notificationSection;

        public NotificationMenu(Transform parent) : base(parent)
        {
            CreateNotificationSettingssSection();
        }

        public void UpdateNotificationsEnabled(bool value)
        {
            if (notificationsEnabled != null)
            {
                notificationsEnabled.isOn = value;
            }
        }

        public void UpdateNotificationDuration(float value)
        {
            if (notificationDuration != null)
            {
                notificationDuration.value = value;
            }
        }

        private void CreateNotificationSettingssSection()
        {
            // Dimensions - Menu width minus 20

            // Notifications Section
            notificationSection = MenuConstructor.Section.Create(menuObject.transform, "Notifications", 25, 240, false);
            
            // Enabled?
            MenuConstructor.Label.Create(notificationSection.transform, "Toggle", 10, 10, Color.white);

            // Toggle button with value change listener
            notificationsEnabled = MenuConstructor.Toggle.Create(notificationSection.transform, 90, 35, "Enabled", "Disabled", Settings.Instance.notificationsEnabled);
            
            // Add listener after toggle creation
            notificationsEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.notificationsEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllNotificationToggles(value);
            });

            // Horizontal line
            MenuConstructor.HorizontalBar.Create(notificationSection.transform, 60);
            
            // Duration
            MenuConstructor.Label.Create(notificationSection.transform, "Duration", 10, 70, Color.white);
            
            // Duration slider
            notificationDuration = MenuConstructor.Slider.Create(notificationSection.transform, 0, 95, 1, 120, Settings.Instance.notificationDuration);
            
            notificationDuration.onValueChanged.AddListener((value) => {
                Settings.Instance.notificationDuration = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllNotificationDurations(value);
            });

            // Horizontal line
            MenuConstructor.HorizontalBar.Create(notificationSection.transform, 130);
            
            // Always or only away from display boards?
            MenuConstructor.Label.Create(notificationSection.transform, "Min distance from boards", 10, 140, Color.white);

            // Location multi-selction
            MenuConstructor.DisplayText.Create(notificationSection.transform, "Future development", 25, 160, Color.yellow);
            // TODO: Add multi-selection dropdown for location
            
            // Horizontal line
            MenuConstructor.HorizontalBar.Create(notificationSection.transform, 185);
            
            // Limit number of messages displayed?
            MenuConstructor.Label.Create(notificationSection.transform, "Limit number displayed", 10, 195, Color.white);

            // Limit dropdown
            MenuConstructor.DisplayText.Create(notificationSection.transform, "Future development", 25, 215, Color.yellow);
            // TODO: Add dropdown for limit
        }

        public override void Show()
        {
            base.Show();
            notificationSection?.SetActive(!isMinimized);
        }
    }
}
