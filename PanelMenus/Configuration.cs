using UnityEngine;

namespace TwitchChat.PanelMenus
{
    public class ConfigurationPanel : PanelConstructor.BasePanel
    {
        private GameObject? messageSection;

        public ConfigurationPanel(Transform parent) : base(parent)
        {
            CreateMessageSection();
        }

        private void CreateMessageSection()
        {
           // Dimensions - Menu width minus 20

            // Message Section
            messageSection = PanelConstructor.Section.Create(panelObject.transform, "Configuration", 25, 100, false);
            
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
