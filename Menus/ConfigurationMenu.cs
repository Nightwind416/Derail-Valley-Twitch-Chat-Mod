using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class ConfigurationMenu : BaseMenu
    {
        private GameObject messageSection;

        public ConfigurationMenu(Transform parent) : base(parent)
        {
            CreateMessageSection();
        }

        private void CreateMessageSection()
        {
           // Dimensions - Menu width minus 20

            // Message Section
            messageSection = CreateSection("Message", 25, 100);
            
            // Configuration section
            // TODO: Add configuration section content
        }

        public override void Show()
        {
            base.Show();
            if (messageSection != null) messageSection.SetActive(!isMinimized);
        }
    }
}
