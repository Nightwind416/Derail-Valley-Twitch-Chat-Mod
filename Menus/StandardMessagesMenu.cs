using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.MenuConstructor
{
    public class StandardMessagesMenu : MenuConstructor.BaseMenu
    {
        private UnityEngine.UI.Toggle? connectMessageEnabled;
        private UnityEngine.UI.Text? connectMessage;
        private UnityEngine.UI.Toggle? newFollowerMessageEnabled;
        private UnityEngine.UI.Text? newFollowerMessage;
        private UnityEngine.UI.Toggle? newSubscriberMessageEnabled;
        private UnityEngine.UI.Text? newSubscriberMessage;
        private UnityEngine.UI.Toggle? disconnectMessageEnabled;
        private UnityEngine.UI.Text? disconnectMessage;
        private GameObject? connectDisconnectSection;
        private GameObject? newFollowerSubscriberSection;

        public StandardMessagesMenu(Transform parent) : base(parent)
        {
            CreateConnectDisconnectSection();
            CreateNewFollowerSubscriberSection();
        }

        private void CreateConnectDisconnectSection()
        {

            connectDisconnectSection = MenuConstructor.Section.Create(menuObject.transform, "Connect/Disconnect Section", 20, 135, false);
            
            // Connect Message Label
            MenuConstructor.Label.Create(connectDisconnectSection.transform, "Connect", 5, 8);
            
            // Connect Message Enabled
            connectMessageEnabled = MenuConstructor.Toggle.Create(connectDisconnectSection.transform, 135, 15, "Enabled", "Disabled", Settings.Instance.connectMessageEnabled);
            connectMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.connectMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Connect Message Text
            connectMessage = MenuConstructor.DisplayText.Create(connectDisconnectSection.transform, "", 10, 30, Color.cyan, 2);

            // Horizontal line
            MenuConstructor.HorizontalBar.Create(connectDisconnectSection.transform, 70);
            
            // Disconnect Message Label
            MenuConstructor.Label.Create(connectDisconnectSection.transform, "Disconnect", 5, 80);
            
            // Disconnect Message Enabled
            disconnectMessageEnabled = MenuConstructor.Toggle.Create(connectDisconnectSection.transform, 135, 87, "Enabled", "Disabled", Settings.Instance.disconnectMessageEnabled);
            disconnectMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.disconnectMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Disconnect Message Text
            disconnectMessage = MenuConstructor.DisplayText.Create(connectDisconnectSection.transform, "", 10, 100, Color.cyan, 2);
        }
        private void CreateNewFollowerSubscriberSection()
        {

            newFollowerSubscriberSection = MenuConstructor.Section.Create(menuObject.transform, "New Follower/Subscriber Section", 160, 135, false);

            // Message Label
            MenuConstructor.Label.Create(newFollowerSubscriberSection.transform, "New Follower", 5, 8);
            
            // Enabled
            newFollowerMessageEnabled = MenuConstructor.Toggle.Create(newFollowerSubscriberSection.transform, 135, 15, "Enabled", "Disabled", Settings.Instance.newFollowerMessageEnabled);
            newFollowerMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.newFollowerMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Message Text
            newFollowerMessage = MenuConstructor.DisplayText.Create(newFollowerSubscriberSection.transform, "", 10, 30, Color.cyan, 2);

            // Horizontal line
            MenuConstructor.HorizontalBar.Create(newFollowerSubscriberSection.transform, 70);
            
            // Message Label
            MenuConstructor.Label.Create(newFollowerSubscriberSection.transform, "New Subscriber", 5, 80);
            
            // Enabled
            newSubscriberMessageEnabled = MenuConstructor.Toggle.Create(newFollowerSubscriberSection.transform, 135, 87, "Enabled", "Disabled", Settings.Instance.newSubscriberMessageEnabled);
            newSubscriberMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.newSubscriberMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
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
