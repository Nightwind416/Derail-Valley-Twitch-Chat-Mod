using UnityEngine;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Manages the configuration panel interface in the mod's settings menu.
    /// Provides a framework for future configuration options and settings.
    /// Currently serves as a placeholder for upcoming configuration features.
    /// </summary>
    public class ConfigurationPanel : PanelConstructor.BasePanel
    {
        private GameObject? messageSection;

        public ConfigurationPanel(Transform parent) : base(parent)
        {
            CreateMessageSection();
        }

        /// <summary>
        /// Creates the main configuration section of the panel.
        /// Currently a placeholder for future configuration options.
        /// TODO: Implement configuration section content.
        /// </summary>
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
