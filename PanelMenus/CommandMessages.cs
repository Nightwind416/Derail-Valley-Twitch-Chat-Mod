using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Panel for managing command messages and their settings.
    /// Handles the configuration of command responses and info messages.
    /// </summary>
    public class CommandMessagesPanel : PanelConstructor.BasePanel
    {
        private Toggle? commandsMessageEnabled;
        private Text? commandsMessage;
        private Toggle? infoMessageEnabled;
        private Text? infoMessage;
        private GameObject? commandsSettingsSection;
        // private GameObject? customCommandsSection;

        public CommandMessagesPanel(Transform parent) : base(parent)
        {
            CreateCommandsSettingsSection();
            // CreateCustomCommandsSection();
        }

        /// <summary>
        /// Updates the enabled state of the commands message toggle.
        /// </summary>
        /// <param name="value">The new enabled state.</param>
        public void UpdateCommandsMessageEnabled(bool value)
        {
            if (commandsMessageEnabled != null) commandsMessageEnabled.isOn = value;
        }

        /// <summary>
        /// Updates the enabled state of the info message toggle.
        /// </summary>
        /// <param name="value">The new enabled state.</param>
        public void UpdateInfoMessageEnabled(bool value)
        {
            if (infoMessageEnabled != null) infoMessageEnabled.isOn = value;
        }

        /// <summary>
        /// Creates and configures the commands settings section of the panel.
        /// Sets up toggles and message displays for commands and info messages.
        /// </summary>
        private void CreateCommandsSettingsSection()
        {
            commandsSettingsSection = PanelConstructor.Section.Create(panelObject.transform, "Commands Settings", 25, 200, false);
            
            // !Commands Message Label
            PanelConstructor.Label.Create(commandsSettingsSection.transform, "!Commands Message", 5, 8);
            
            // Commands Message Toggle
            commandsMessageEnabled = PanelConstructor.Toggle.Create(commandsSettingsSection.transform, 155, 15, "Enabled", "Disabled", Settings.Instance.commandsMessageEnabled);

            // Add listener after toggle creation
            commandsMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.commandsMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateCommandsMessageToggles(value);
            });

            // Commands Message Text
            commandsMessage = PanelConstructor.DisplayText.Create(commandsSettingsSection.transform, "", 10, 33, Color.cyan, 2);

            // Horizontal line
            PanelConstructor.HorizontalBar.Create(commandsSettingsSection.transform, 70);
            
            // !Info Message Label
            PanelConstructor.Label.Create(commandsSettingsSection.transform, "!Info Message", 5, 80);
            
            // Info Message Toggle
            infoMessageEnabled = PanelConstructor.Toggle.Create(commandsSettingsSection.transform, 135, 87, "Enabled", "Disabled", Settings.Instance.infoMessageEnabled);

            // Add listener after toggle creation
            infoMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.infoMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateInfoMessageToggles(value);
            });

            // Info Message Text
            infoMessage = PanelConstructor.DisplayText.Create(commandsSettingsSection.transform, "", 10, 102, Color.cyan, 7);
        }

        /// <summary>
        /// Updates the displayed values for command messages and info messages.
        /// Should be called when settings are changed.
        /// </summary>
        public void UpdateCommandMessagesPanelValues()
        {
            if (commandsMessage != null) commandsMessage.text = Settings.Instance.commandsMessage;
            if (infoMessage != null) infoMessage.text = Settings.Instance.infoMessage;
        }

        /// <summary>
        /// Controls the visibility of the panel and its sections.
        /// </summary>
        public override void Show()
        {
            base.Show();
            commandsSettingsSection?.SetActive(!isMinimized);
            // customCommandsSection?.SetActive(!isMinimized);
        }
    }
}
