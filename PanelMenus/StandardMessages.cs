using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    public class StandardMessagesPanel : PanelConstructor.BasePanel
    {
        private Toggle? connectMessageEnabled;
        private Text? connectMessage;
        // private Toggle? newFollowerMessageEnabled;
        // private Text? newFollowerMessage;
        // private Toggle? newSubscriberMessageEnabled;
        // private Text? newSubscriberMessage;
        private Toggle? disconnectMessageEnabled;
        private Text? disconnectMessage;
        private GameObject? connectDisconnectSection;
        // private GameObject? newFollowerSubscriberSection;

        public StandardMessagesPanel(Transform parent) : base(parent)
        {
            CreateConnectDisconnectSection();
            // CreateNewFollowerSubscriberSection();
        }

        public void UpdateConnectMessageEnabled(bool enabled)
        {
            if (connectMessageEnabled != null)
                connectMessageEnabled.isOn = enabled;
        }
        // public void UpdateNewFollowerMessageEnabled(bool enabled)
        // {
        //     // if (newFollowerMessageEnabled != null)
        //     //     newFollowerMessageEnabled.isOn = enabled;
        // }
        // public void UpdateNewSubscriberMessageEnabled(bool enabled)
        // {
        //     // if (newSubscriberMessageEnabled != null)
        //     //     newSubscriberMessageEnabled.isOn = enabled;
        // }
        public void UpdateDisconnectMessageEnabled(bool enabled)
        {
            if (disconnectMessageEnabled != null)
                disconnectMessageEnabled.isOn = enabled;
        }

        private void CreateConnectDisconnectSection()
        {

            connectDisconnectSection = PanelConstructor.Section.Create(panelObject.transform, "Connect/Disconnect Section", 25, 200, false);
            
            // Connect Message Label
            PanelConstructor.Label.Create(connectDisconnectSection.transform, "Connect", 5, 8);
            
            // Connect Message Toggle
            connectMessageEnabled = PanelConstructor.Toggle.Create(connectDisconnectSection.transform, 135, 15, "Enabled", "Disabled", Settings.Instance.connectMessageEnabled);

            // Add listener after toggle creation
            connectMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.connectMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllConnectMessageEnabledToggles(value);
            });

            // Connect Message Text
            connectMessage = PanelConstructor.DisplayText.Create(connectDisconnectSection.transform, "", 10, 30, Color.cyan, 5, 10);

            // Horizontal line
            PanelConstructor.HorizontalBar.Create(connectDisconnectSection.transform, 95);
            
            // Disconnect Message Label
            PanelConstructor.Label.Create(connectDisconnectSection.transform, "Disconnect", 5, 110);
            
            // Disconnect Message Toggle
            disconnectMessageEnabled = PanelConstructor.Toggle.Create(connectDisconnectSection.transform, 135, 117, "Enabled", "Disabled", Settings.Instance.disconnectMessageEnabled);

            // Add listener after toggle creation
            disconnectMessageEnabled.onValueChanged.AddListener((value) => {
                Settings.Instance.disconnectMessageEnabled = value;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllDisconnectMessageEnabledToggles(value);
            });

            // Disconnect Message Text
            disconnectMessage = PanelConstructor.DisplayText.Create(connectDisconnectSection.transform, "", 10, 135, Color.cyan, 5, 10);
        }
        // private void CreateNewFollowerSubscriberSection()
        // {

        //     newFollowerSubscriberSection = PanelConstructor.Section.Create(panelObject.transform, "New Follower/Subscriber Section", 200, 100, false);

        //     // Message Label
        //     PanelConstructor.Label.Create(newFollowerSubscriberSection.transform, "New Follower", 5, 8);
            
        //     // // New Follower Toggle
        //     // newFollowerMessageEnabled = PanelConstructor.Toggle.Create(newFollowerSubscriberSection.transform, 135, 15, "Enabled", "Disabled", Settings.Instance.newFollowerMessageEnabled);

        //     // // Add listener after toggle creation
        //     // newFollowerMessageEnabled.onValueChanged.AddListener((value) => {
        //     //     Settings.Instance.newFollowerMessageEnabled = value;
        //     //     Settings.Instance.Save(Main.ModEntry);
        //     //     MenuManager.Instance.UpdateAllNewFollowerMessageEnabledToggles(value);
        //     // });

        //     // Message Text
        //     // newFollowerMessage = PanelConstructor.DisplayText.Create(newFollowerSubscriberSection.transform, "", 10, 30, Color.cyan, 2, 8);
        //     PanelConstructor.DisplayText.Create(newFollowerSubscriberSection.transform, "Future development", 10, 30, Color.yellow);

        //     // Horizontal line
        //     PanelConstructor.HorizontalBar.Create(newFollowerSubscriberSection.transform, 50);
            
        //     // Message Label
        //     PanelConstructor.Label.Create(newFollowerSubscriberSection.transform, "New Subscriber", 5, 60);
            
        //     // // New Subscriber Toggle
        //     // newSubscriberMessageEnabled = PanelConstructor.Toggle.Create(newFollowerSubscriberSection.transform, 135, 67, "Enabled", "Disabled", Settings.Instance.newSubscriberMessageEnabled);

        //     // // Add listener after toggle creation
        //     // newSubscriberMessageEnabled.onValueChanged.AddListener((value) => {
        //     //     Settings.Instance.newSubscriberMessageEnabled = value;
        //     //     Settings.Instance.Save(Main.ModEntry);
        //     //     MenuManager.Instance.UpdateAllNewSubscriberMessageEnabledToggles(value);
        //     // });

        //     // Message Text
        //     // newSubscriberMessage = PanelConstructor.DisplayText.Create(newFollowerSubscriberSection.transform, "New Subscriber Message not yet implemented.", 10, 102, Color.cyan, 2, 8);
        //     PanelConstructor.DisplayText.Create(newFollowerSubscriberSection.transform, "Future development", 10, 82, Color.yellow);
        // }
        public void UpdateStandardMessagesPanelValues()
        {
            if (connectMessageEnabled != null)
                connectMessageEnabled.isOn = Settings.Instance.connectMessageEnabled;
            // if (newFollowerMessageEnabled != null)
            //     newFollowerMessageEnabled.isOn = Settings.Instance.newFollowerMessageEnabled;
            // if (newSubscriberMessageEnabled != null)
            //     newSubscriberMessageEnabled.isOn = Settings.Instance.newSubscriberMessageEnabled;
            if (disconnectMessageEnabled != null)
                disconnectMessageEnabled.isOn = Settings.Instance.disconnectMessageEnabled;

            if (connectMessage != null)
                connectMessage.text = Settings.Instance.connectMessage;
            // if (newFollowerMessage != null)
            //     newFollowerMessage.text = Settings.Instance.newFollowerMessage;
            // if (newSubscriberMessage != null)
            //     newSubscriberMessage.text = Settings.Instance.newSubscriberMessage;
            if (disconnectMessage != null)
                disconnectMessage.text = Settings.Instance.disconnectMessage;
        }

        public override void Show()
        {
            base.Show();
            connectDisconnectSection?.SetActive(!isMinimized);
            // newFollowerSubscriberSection?.SetActive(!isMinimized);
        }
    }
}
