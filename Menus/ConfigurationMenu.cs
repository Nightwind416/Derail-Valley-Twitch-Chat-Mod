using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class ConfigurationMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public ConfigurationMenu(Transform parent) : base(parent)
        {
            CreateConfigurationMenu();
            CreateMessageSection();
        }

        private void CreateConfigurationMenu()
        {
            // Dimensions - 200x300

            // Title
            CreateTitle("Configuration Menu", 18, Color.white, TextAnchor.UpperCenter);
            
            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 190, 10, Color.white, () => OnBackButtonClicked?.Invoke());
        }

        private void CreateMessageSection()
        {
           // Dimensions - Menu width minus 20

            // Message Section
            GameObject messageSection = CreateSection("Message", 25, 100);
            
            // Configuration section
            // TODO: Add configuration section content
        }
    }
}
