using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class StandardMessagesMenu : MenuConstructor.BaseMenu
    {
        private Toggle? connectMessageEnabled;
        private Text? connectMessage;
        private Toggle? newFollowerMessageEnabled;
        private Text? newFollowerMessage;
        private Toggle? newSubscriberMessageEnabled;
        private Text? newSubscriberMessage;
        private Toggle? disconnectMessageEnabled;
        private Text? disconnectMessage;
        private GameObject? connectDisconnectSection;
        private GameObject? newFollowerSubscriberSection;

        public StandardMessagesMenu(Transform parent) : base(parent)
        {
            CreateConnectDisconnectSection();
            CreateNewFollowerSubscriberSection();
        }

        public void UpdateConnectMessageEnabled(bool enabled)
        {
            if (connectMessageEnabled != null)
                connectMessageEnabled.isOn = enabled;
        }
        public void UpdateNewFollowerMessageEnabled(bool enabled)
        {
            if (newFollowerMessageEnabled != null)
                newFollowerMessageEnabled.isOn = enabled;
        }
        public void UpdateNewSubscriberMessageEnabled(bool enabled)
        {
            if (newSubscriberMessageEnabled != null)
                newSubscriberMessageEnabled.isOn = enabled;
        }
        public void UpdateDisconnectMessageEnabled(bool enabled)
        {
            if (disconnectMessageEnabled != null)
                disconnectMessageEnabled.isOn = enabled;
        }

        private void CreateConnectDisconnectSection()
        {

            connectDisconnectSection = MenuConstructor.Section.Create(menuObject.transform, "Connect/Disconnect Section", 20, 135, false);
            
            // Connect Message Label
            MenuConstructor.Label.Create(connectDisconnectSection.transform, "Connect", 5, 8);
            
            // Connect Message Toggle
            connectMessageEnabled = MenuConstructor.Toggle.Create(connectDisconnectSection.transform, 135, 15, "Enabled", "Disabled", Settings.Instance.connectMessageEnabled);

            // Add listener after toggle creation
            connectMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.connectMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllConnectMessageEnabledToggles(value);
            });

            // Connect Message Text
            connectMessage = MenuConstructor.DisplayText.Create(connectDisconnectSection.transform, "", 10, 30, Color.cyan, 2);

            // Horizontal line
            MenuConstructor.HorizontalBar.Create(connectDisconnectSection.transform, 70);
            
            // Disconnect Message Label
            MenuConstructor.Label.Create(connectDisconnectSection.transform, "Disconnect", 5, 80);
            
            // Disconnect Message Toggle
            disconnectMessageEnabled = MenuConstructor.Toggle.Create(connectDisconnectSection.transform, 135, 87, "Enabled", "Disabled", Settings.Instance.disconnectMessageEnabled);

            // Add listener after toggle creation
            disconnectMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.disconnectMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllDisconnectMessageEnabledToggles(value);
            });

            // Disconnect Message Text
            disconnectMessage = MenuConstructor.DisplayText.Create(connectDisconnectSection.transform, "", 10, 100, Color.cyan, 2);
        }
        private void CreateNewFollowerSubscriberSection()
        {

            newFollowerSubscriberSection = MenuConstructor.Section.Create(menuObject.transform, "New Follower/Subscriber Section", 160, 135, false);

            // Message Label
            MenuConstructor.Label.Create(newFollowerSubscriberSection.transform, "New Follower", 5, 8);
            
            // New Follower Toggle
            newFollowerMessageEnabled = MenuConstructor.Toggle.Create(newFollowerSubscriberSection.transform, 135, 15, "Enabled", "Disabled", Settings.Instance.newFollowerMessageEnabled);

            // Add listener after toggle creation
            newFollowerMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.newFollowerMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllNewFollowerMessageEnabledToggles(value);
            });

            // Message Text
            newFollowerMessage = MenuConstructor.DisplayText.Create(newFollowerSubscriberSection.transform, "", 10, 30, Color.cyan, 2);

            // Horizontal line
            MenuConstructor.HorizontalBar.Create(newFollowerSubscriberSection.transform, 70);
            
            // Message Label
            MenuConstructor.Label.Create(newFollowerSubscriberSection.transform, "New Subscriber", 5, 80);
            
            // New Subscriber Toggle
            newSubscriberMessageEnabled = MenuConstructor.Toggle.Create(newFollowerSubscriberSection.transform, 135, 87, "Enabled", "Disabled", Settings.Instance.newSubscriberMessageEnabled);

            // Add listener after toggle creation
            newSubscriberMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.newSubscriberMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllNewSubscriberMessageEnabledToggles(value);
            });

            // Message Text
            newSubscriberMessage = MenuConstructor.DisplayText.Create(newFollowerSubscriberSection.transform, "New Subscriber Message not yet implemented.", 10, 102, Color.cyan, 2);
        }
        public void UpdateStandardMessagesMenuValues()
        {
            if (connectMessageEnabled != null)
                connectMessageEnabled.isOn = Settings.Instance.connectMessageEnabled;
            if (newFollowerMessageEnabled != null)
                newFollowerMessageEnabled.isOn = Settings.Instance.newFollowerMessageEnabled;
            if (newSubscriberMessageEnabled != null)
                newSubscriberMessageEnabled.isOn = Settings.Instance.newSubscriberMessageEnabled;
            if (disconnectMessageEnabled != null)
                disconnectMessageEnabled.isOn = Settings.Instance.disconnectMessageEnabled;

            if (connectMessage != null)
                connectMessage.text = Settings.Instance.connectMessage;
            if (newFollowerMessage != null)
                newFollowerMessage.text = Settings.Instance.newFollowerMessage;
            if (newSubscriberMessage != null)
                newSubscriberMessage.text = Settings.Instance.newSubscriberMessage;
            if (disconnectMessage != null)
                disconnectMessage.text = Settings.Instance.disconnectMessage;
        }

        public override void Show()
        {
            base.Show();
            connectDisconnectSection?.SetActive(!isMinimized);
            newFollowerSubscriberSection?.SetActive(!isMinimized);
        }
    }
}
