using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class StandardMessagesMenu : BaseMenu
    {
        private Toggle connectMessageEnabled;
        private Text connectMessage;
        private Toggle newFollowerMessageEnabled;
        private Text newFollowerMessage;
        private Toggle newSubscriberMessageEnabled;
        private Text newSubscriberMessage;
        private Toggle disconnectMessageEnabled;
        private Text disconnectMessage;
        private GameObject connectDisconnectSection;
        private GameObject newFollowerSubscriberSection;

        public StandardMessagesMenu(Transform parent) : base(parent)
        {
            CreateConnectDisconnectSection();
            CreateNewFollowerSubscriberSection();
        }

        private void CreateConnectDisconnectSection()
        {
            connectDisconnectSection = CreateSection("Connect/Disconnect Section", 20, 135, false);
            
            // Connect Message Label
            UIElementFactory.CreateLabel(connectDisconnectSection.transform, "Connect", 5, 8);
            
            // Connect Message Enabled
            connectMessageEnabled = UIElementFactory.CreateToggle(connectDisconnectSection.transform, 135, 15, "Enabled", "Disabled", Settings.Instance.connectMessageEnabled);
            connectMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.connectMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Connect Message Text
            connectMessage = CreateTextDisplay(connectDisconnectSection.transform, "", 10, 30, Color.cyan, 2);

            // Horizontal line
            CreateHorizontalBar(connectDisconnectSection.transform, 70);
            
            // Disconnect Message Label
            UIElementFactory.CreateLabel(connectDisconnectSection.transform, "Disconnect", 5, 80);
            
            // Disconnect Message Enabled
            disconnectMessageEnabled = UIElementFactory.CreateToggle(connectDisconnectSection.transform, 135, 87, "Enabled", "Disabled", Settings.Instance.disconnectMessageEnabled);
            disconnectMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.disconnectMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Disconnect Message Text
            disconnectMessage = CreateTextDisplay(connectDisconnectSection.transform, "", 10, 100, Color.cyan, 2);
        }
        private void CreateNewFollowerSubscriberSection()
        {
            newFollowerSubscriberSection = CreateSection("New Follower/Subscriber Section", 160, 135, false);

            // Message Label
            UIElementFactory.CreateLabel(newFollowerSubscriberSection.transform, "New Follower", 5, 8);
            
            // Enabled
            newFollowerMessageEnabled = UIElementFactory.CreateToggle(newFollowerSubscriberSection.transform, 135, 15, "Enabled", "Disabled", Settings.Instance.newFollowerMessageEnabled);
            newFollowerMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.newFollowerMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Message Text
            newFollowerMessage = CreateTextDisplay(newFollowerSubscriberSection.transform, "", 10, 30, Color.cyan, 2);

            // Horizontal line
            CreateHorizontalBar(newFollowerSubscriberSection.transform, 70);
            
            // Message Label
            UIElementFactory.CreateLabel(newFollowerSubscriberSection.transform, "New Subscriber", 5, 80);
            
            // Enabled
            newSubscriberMessageEnabled = UIElementFactory.CreateToggle(newFollowerSubscriberSection.transform, 135, 87, "Enabled", "Disabled", Settings.Instance.newSubscriberMessageEnabled);
            newSubscriberMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.newSubscriberMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
            });

            // Message Text
            newSubscriberMessage = CreateTextDisplay(newFollowerSubscriberSection.transform, "New Subscriber Message not yet implemented.", 10, 102, Color.cyan, 2);
        }
        public void UpdateStandardMessagesMenuValues()
        {
            connectMessageEnabled.isOn = Settings.Instance.connectMessageEnabled;
            newFollowerMessageEnabled.isOn = Settings.Instance.newFollowerMessageEnabled;
            newSubscriberMessageEnabled.isOn = Settings.Instance.newSubscriberMessageEnabled;
            disconnectMessageEnabled.isOn = Settings.Instance.disconnectMessageEnabled;

            connectMessage.text = Settings.Instance.connectMessage;
            newFollowerMessage.text = Settings.Instance.newFollowerMessage;
            newSubscriberMessage.text = Settings.Instance.newSubscriberMessage;
            disconnectMessage.text = Settings.Instance.disconnectMessage;
        }

        public override void Show()
        {
            base.Show();
            if (connectDisconnectSection != null) connectDisconnectSection.SetActive(!isMinimized);
            if (newFollowerSubscriberSection != null) newFollowerSubscriberSection.SetActive(!isMinimized);
        }
    }
}
