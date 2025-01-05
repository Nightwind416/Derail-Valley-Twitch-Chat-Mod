using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.MenuConstructor
{
    public class CommandMessagesMenu : MenuConstructor.BaseMenu
    {
        private UnityEngine.UI.Toggle? commandsMessageEnabled;
        private UnityEngine.UI.Text? commandsMessage;
        private UnityEngine.UI.Toggle? infoMessageEnabled;
        private UnityEngine.UI.Text? infoMessage;
        private GameObject? commandsSettingsSection;
        private GameObject? customCommandsSection;

        public CommandMessagesMenu(Transform parent) : base(parent)
        {
            CreateCommandsSettingsSection();
            CreateCustomCommandsSection();
        }

        private void CreateCommandsSettingsSection()
        {
            commandsSettingsSection = MenuConstructor.Section.Create(menuObject.transform, "Commands Settings", 25, 175, false);
            
            // !Commands Message Label
            MenuConstructor.Label.Create(commandsSettingsSection.transform, "!Commands Message", 5, 8);
            
            // Connect Message Enabled
            commandsMessageEnabled = MenuConstructor.Toggle.Create(commandsSettingsSection.transform, 155, 15, "Enabled", "Disabled", Settings.Instance.commandsMessageEnabled);
            commandsMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.commandsMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Connect Message Text
            commandsMessage = MenuConstructor.DisplayText.Create(commandsSettingsSection.transform, "", 10, 30, Color.cyan, 2);

            // Horizontal line
            MenuConstructor.HorizontalBar.Create(commandsSettingsSection.transform, 70);
            
            // !Info Message Label
            MenuConstructor.Label.Create(commandsSettingsSection.transform, "!Info Message", 5, 80);
            
            // Disconnect Message Enabled
            infoMessageEnabled = MenuConstructor.Toggle.Create(commandsSettingsSection.transform, 135, 87, "Enabled", "Disabled", Settings.Instance.infoMessageEnabled);
            infoMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.infoMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Disconnect Message Text
            infoMessage = MenuConstructor.DisplayText.Create(commandsSettingsSection.transform, "", 10, 100, Color.cyan, 4);
        }
        private void CreateCustomCommandsSection()
        {
            customCommandsSection = MenuConstructor.Section.Create(menuObject.transform, "Custom Commands", 225, 50);

            // Custom Commands
            MenuConstructor.DisplayText.Create(customCommandsSection.transform, "Future development", 25, 25, Color.yellow);
            
            // TODO: Add Standard Messages Settings content
        }
        public void UpdateCommandMessagesMenuValues()
        {
            if (commandsMessage != null) commandsMessage.text = Settings.Instance.commandsMessage;
            if (infoMessage != null) infoMessage.text = Settings.Instance.infoMessage;
        }

        public override void Show()
        {
            base.Show();
            commandsSettingsSection?.SetActive(!isMinimized);
            customCommandsSection?.SetActive(!isMinimized);
        }
    }
}
