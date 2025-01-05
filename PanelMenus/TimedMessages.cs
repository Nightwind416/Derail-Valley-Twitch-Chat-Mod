using UnityEngine;

namespace TwitchChat.PanelMenus
{
    public class TimedMessagesPanel : PanelConstructor.BasePanel
    {
        private GameObject? settingsSection;

        public TimedMessagesPanel(Transform parent) : base(parent)
        {
            CreateSettingsSection();
        }

        private void CreateSettingsSection()
        {
            settingsSection = PanelConstructor.Section.Create(panelObject.transform, "Settings", 25, 100);
            
            PanelConstructor.DisplayText.Create(settingsSection.transform, "Not yet integrated into the panel system. Utilize the Unity Mod Manager menu for Timed Messages.", 10, 25, Color.yellow, 4);
        }

        public override void Show()
        {
            base.Show();
            settingsSection?.SetActive(!isMinimized);
        }
    }
}
