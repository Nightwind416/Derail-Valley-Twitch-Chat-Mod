using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class StandardMessagesMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public StandardMessagesMenu(Transform parent) : base(parent)
        {
            CreateStandardMessagesMenu();
            CreateSettingsSection();
        }

        private void CreateStandardMessagesMenu()
        {
            // Dimensions - 200x300

            // Title
            CreateTitle("Standard Messages", 18, Color.white, TextAnchor.UpperCenter);
            
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
