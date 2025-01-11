using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
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

        public void UpdateCommandsMessageEnabled(bool value)
        {
            if (commandsMessageEnabled != null) commandsMessageEnabled.isOn = value;
        }

        public void UpdateInfoMessageEnabled(bool value)
        {
            if (infoMessageEnabled != null) infoMessageEnabled.isOn = value;
        }

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
        // private void CreateCustomCommandsSection()
        // {
        //     customCommandsSection = PanelConstructor.Section.Create(panelObject.transform, "Custom Commands", 235, 50);

        //     // Custom Commands
        //     PanelConstructor.DisplayText.Create(customCommandsSection.transform, "Future development", 25, 25, Color.yellow);
            
        //     // TODO: Add Standard Messages Settings content
        // }
        public void UpdateCommandMessagesPanelValues()
        {
            if (commandsMessage != null) commandsMessage.text = Settings.Instance.commandsMessage;
            if (infoMessage != null) infoMessage.text = Settings.Instance.infoMessage;
        }

        public override void Show()
        {
            base.Show();
            commandsSettingsSection?.SetActive(!isMinimized);
            // customCommandsSection?.SetActive(!isMinimized);
        }
    }
}
