using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class TimedMessagesMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public TimedMessagesMenu(Transform parent) : base(parent)
        {
            CreateTimedMessagesMenu();
            CreateSettingsSection();
        }

        private void CreateTimedMessagesMenu()
        {
            // Dimensions - 200x300

            // Title
            CreateTitle("Timed Messages", 18, Color.white, TextAnchor.UpperCenter);
            
            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 190, 10, Color.white, () => OnBackButtonClicked?.Invoke());
        }

        private void CreateSettingsSection()
        {
            GameObject settingsSection = CreateSection("Settings", 25, 50);
            
            // Standard Messages Settings
            CreateTextDisplay(settingsSection.transform, "Future development", 25, 25, Color.yellow);
            // TODO: Add Standard Messages Settings content
        }
    }
}
