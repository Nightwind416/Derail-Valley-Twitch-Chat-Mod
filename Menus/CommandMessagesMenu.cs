using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class CommandMessagesMenu : BaseMenu
    {
        private Toggle commandsMessageEnabled;
        private Text commandsMessage;
        private Toggle infoMessageEnabled;
        private Text infoMessage;
        private GameObject commandsSettingsSection;
        private GameObject customCommandsSection;

        public CommandMessagesMenu(Transform parent) : base(parent)
        {
            CreateCommandsSettingsSection();
            CreateCustomCommandsSection();
        }

        private void CreateCommandsSettingsSection()
        {
            commandsSettingsSection = CreateSection("Commands Settings", 25, 175, false);
            
            // !Commands Message Label
            UIElementFactory.CreateLabel(commandsSettingsSection.transform, "!Commands Message", 5, 8);
            
            // Connect Message Enabled
            commandsMessageEnabled = UIElementFactory.CreateToggle(commandsSettingsSection.transform, 155, 15, "Enabled", "Disabled", Settings.Instance.commandsMessageEnabled);
            commandsMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.commandsMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Connect Message Text
            commandsMessage = CreateTextDisplay(commandsSettingsSection.transform, "", 10, 30, Color.cyan, 2);

            // Horizontal line
            CreateHorizontalBar(commandsSettingsSection.transform, 70);
            
            // !Info Message Label
            UIElementFactory.CreateLabel(commandsSettingsSection.transform, "!Info Message", 5, 80);
            
            // Disconnect Message Enabled
            infoMessageEnabled = UIElementFactory.CreateToggle(commandsSettingsSection.transform, 135, 87, "Enabled", "Disabled", Settings.Instance.infoMessageEnabled);
            infoMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.infoMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Disconnect Message Text
            infoMessage = CreateTextDisplay(commandsSettingsSection.transform, "", 10, 100, Color.cyan, 4);
        }
        private void CreateCustomCommandsSection()
        {
            customCommandsSection = CreateSection("Custom Commands", 225, 50);

            // Custom Commands
            CreateTextDisplay(customCommandsSection.transform, "Future development", 25, 25, Color.yellow);
            
            // TODO: Add Standard Messages Settings content
        }
        public void UpdateCommandMessagesMenuValues()
        {
            commandsMessage.text = Settings.Instance.commandsMessage;
            infoMessage.text = Settings.Instance.infoMessage;
        }

        public override void Show()
        {
            base.Show();
            if (commandsSettingsSection != null) commandsSettingsSection.SetActive(!isMinimized);
            if (customCommandsSection != null) customCommandsSection.SetActive(!isMinimized);
        }
    }
}
