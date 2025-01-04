using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class TimedMessagesMenu : BaseMenu
    {
        private GameObject settingsSection;

        public TimedMessagesMenu(Transform parent) : base(parent)
        {
            CreateSettingsSection();
        }

        private void CreateSettingsSection()
        {
            settingsSection = CreateSection("Settings", 25, 100);
            
            // Timed Messages Settings
            UIElementFactory.CreateTextDisplay(settingsSection.transform, "Not yet integrated into the panel system. Utilize the Unity Mod Manager menu for Timed Messages.", 10, 25, Color.yellow, 4);
            
            // TODO: Add Timed Messages Settings content
        }

        public override void Show()
        {
            base.Show();
            if (settingsSection != null) settingsSection.SetActive(!isMinimized);
        }
    }
}
