using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    public class TimedMessagesPanel : PanelConstructor.BasePanel
    {
        private GameObject? settingsSection;
        private Toggle? timedMessageSystemToggle;

        public TimedMessagesPanel(Transform parent) : base(parent)
        {
            CreateSettingsSection();
        }

        private void CreateSettingsSection()
        {
            settingsSection = PanelConstructor.Section.Create(panelObject.transform, "Settings", 25, 175, false);

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
            
            PanelConstructor.DisplayText.Create(settingsSection.transform, "Timed Messages message editing has not yet been integrated into the in-game panel system. Utilize the Unity Mod Manager menu or edit the settings.xml and restart the game.", 10, 70, Color.yellow, 8);
        }

        public override void Show()
        {
            base.Show();
            settingsSection?.SetActive(!isMinimized);
        }
    }
}
