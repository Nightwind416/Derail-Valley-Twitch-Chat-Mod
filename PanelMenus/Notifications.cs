using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Panel for managing in-game notification settings.
    /// Controls notification visibility, duration, and display preferences.
    /// </summary>
    public class NotificationsPanel : PanelConstructor.BasePanel
    {
        private Toggle? notificationsEnabled;
        private Slider? notificationDuration;

        private GameObject? notificationSection;

        public NotificationsPanel(Transform parent) : base(parent)
        {
            CreateNotificationSettingssSection();
        }

        /// <summary>
        /// Updates the notifications enabled toggle state.
        /// </summary>
        /// <param name="value">The new enabled state.</param>
        public void UpdateNotificationsEnabled(bool value)
        {
            if (notificationsEnabled != null)
            {
                notificationsEnabled.isOn = value;
            }
        }

        /// <summary>
        /// Updates the notification duration slider value.
        /// </summary>
        /// <param name="value">The new duration value.</param>
        public void UpdateNotificationDuration(float value)
        {
            if (notificationDuration != null)
            {
                notificationDuration.value = value;
            }
        }

        /// <summary>
        /// Creates and initializes the notification settings section.
        /// Includes toggles, sliders, and future configuration options.
        /// </summary>
        private void CreateNotificationSettingssSection()
        {
            // Dimensions - Menu width minus 20

            // Notifications Section
            notificationSection = PanelConstructor.Section.Create(panelObject.transform, "Notifications", 25, 240, false);
            
            // Enabled?
            PanelConstructor.Label.Create(notificationSection.transform, "Toggle", 10, 10, Color.white);

            // Toggle button with value change listener
            notificationsEnabled = PanelConstructor.Toggle.Create(notificationSection.transform, 90, 35, "Enabled", "Disabled", Settings.Instance.notificationsEnabled);
            
            // Add listener after toggle creation
            notificationsEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.notificationsEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllNotificationToggles(value);
            });

            // Horizontal line
            PanelConstructor.HorizontalBar.Create(notificationSection.transform, 60);
            
            // Duration
            PanelConstructor.Label.Create(notificationSection.transform, "Duration", 10, 70, Color.white);
            
            // Duration slider
            notificationDuration = PanelConstructor.Slider.Create(notificationSection.transform, 0, 95, 10, 120, Settings.Instance.notificationDuration);
            
            notificationDuration.onValueChanged.AddListener((value) => {
                Settings.Instance.notificationDuration = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllNotificationDurations(value);
            });

            // // Horizontal line
            // PanelConstructor.HorizontalBar.Create(notificationSection.transform, 130);
            
            // // Always or only away from display boards?
            // PanelConstructor.Label.Create(notificationSection.transform, "Min distance from boards", 10, 140, Color.white);

            // // Location multi-selction
            // PanelConstructor.DisplayText.Create(notificationSection.transform, "Future development", 25, 160, Color.yellow);
            // // TODO: Add multi-selection dropdown for location
            
            // // Horizontal line
            // PanelConstructor.HorizontalBar.Create(notificationSection.transform, 185);
            
            // // Limit number of messages displayed?
            // PanelConstructor.Label.Create(notificationSection.transform, "Limit number displayed", 10, 195, Color.white);

            // // Limit dropdown
            // PanelConstructor.DisplayText.Create(notificationSection.transform, "Future development", 25, 215, Color.yellow);
            // // TODO: Add dropdown for limit
        }

        /// <summary>
        /// Controls the visibility of the panel and its sections.
        /// </summary>
        public override void Show()
        {
            base.Show();
            notificationSection?.SetActive(!isMinimized);
        }
    }
}
