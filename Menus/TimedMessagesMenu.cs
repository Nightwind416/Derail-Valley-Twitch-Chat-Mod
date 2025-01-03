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
            GameObject settingsSection = CreateSection("Settings", 25, 100);
            
            // Timed Messages Settings
            CreateTextDisplay(settingsSection.transform, "Not yet integrated into the panel system. Utilize the Unity Mod Manager menu for Timed Messages.", 10, 25, Color.yellow, 4);
            
            // TODO: Add Timed Messages Settings content
        }
    }
}
