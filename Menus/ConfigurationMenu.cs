using UnityEngine;

namespace TwitchChat.MenuConstructor
{
    public class ConfigurationMenu : MenuConstructor.BaseMenu
    {
        private GameObject? messageSection;

        public ConfigurationMenu(Transform parent) : base(parent)
        {
            CreateMessageSection();
        }

        private void CreateMessageSection()
        {
           // Dimensions - Menu width minus 20

            // Message Section
            messageSection = MenuConstructor.Section.Create(menuObject.transform, "Configuration", 25, 100, false);
            
            // Configuration section
            // TODO: Add configuration section content
        }

        public override void Show()
        {
            base.Show();
            messageSection?.SetActive(!isMinimized);
        }
    }
}
