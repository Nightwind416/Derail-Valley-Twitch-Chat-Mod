using UnityEngine;

namespace TwitchChat.MenuConstructor
{
    public class TimedMessagesMenu : MenuConstructor.BaseMenu
    {
        private GameObject? settingsSection;

        public TimedMessagesMenu(Transform parent) : base(parent)
        {
            CreateSettingsSection();
        }

        private void CreateSettingsSection()
        {
            settingsSection = MenuConstructor.Section.Create(menuObject.transform, "Settings", 25, 100);
            
            MenuConstructor.DisplayText.Create(settingsSection.transform, "Not yet integrated into the panel system. Utilize the Unity Mod Manager menu for Timed Messages.", 10, 25, Color.yellow, 4);
        }

        public override void Show()
        {
            base.Show();
            settingsSection?.SetActive(!isMinimized);
        }
    }
}
