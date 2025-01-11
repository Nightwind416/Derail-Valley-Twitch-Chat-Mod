using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Panel for managing timed message settings.
    /// Controls the functionality to send automatic messages at specified intervals.
    /// </summary>
    public class TimedMessagesPanel : PanelConstructor.BasePanel
    {
        private GameObject? settingsSection;
        private Toggle? timedMessageSystemToggle;

        /// <summary>
        /// Initializes a new instance of the TimedMessagesPanel.
        /// </summary>
        /// <param name="parent">The parent transform this panel will be attached to.</param>
        public TimedMessagesPanel(Transform parent) : base(parent)
        {
            CreateSettingsSection();
        }

        /// <summary>
        /// Creates and initializes the settings section of the panel.
        /// Includes toggle controls and informational text about timed messages.
        /// </summary>
        private void CreateSettingsSection()
        {
            settingsSection = PanelConstructor.Section.Create(panelObject.transform, "Settings", 25, 200, false);

            // Timed Messages label
            PanelConstructor.Label.Create(settingsSection.transform, "Toggle", 10, 10, Color.white);

            // Timed Messages toggle
            timedMessageSystemToggle = PanelConstructor.Toggle.Create(settingsSection.transform, 90, 35, "Enabled", "Disabled", Settings.Instance.timedMessageSystemToggle);

            // Add listener after toggle creation
            timedMessageSystemToggle.onValueChanged.AddListener((value) => {
                Settings.Instance.timedMessageSystemToggle = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllNotificationToggles(value);
            });

            // Horizontal line
            PanelConstructor.HorizontalBar.Create(settingsSection.transform, 60);
            
            PanelConstructor.DisplayText.Create(settingsSection.transform, "Timed Messages interval and text editing has not yet been integrated into the in-game panel system. Utilize the Unity Mod Manager menu or edit the TwitchChatMod settings.xml and restart the game.", 10, 70, Color.yellow, 9, 10);
        }

        /// <summary>
        /// Controls the visibility of the panel and its sections.
        /// </summary>
        public override void Show()
        {
            base.Show();
            settingsSection?.SetActive(!isMinimized);
        }
    }
}
