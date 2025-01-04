using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class StatusMenu : BaseMenu
    {
        private Button authButton;
        private Text authStatus;
        private Button connectButton;
        private Text connectionStatus;
        private Text connectionIndicator;
        private Text lastMessageType;
        private Text lastTypeReceivedTime;
        private Text lastKeepaliveTime;

        private GameObject authSection;
        private GameObject wsSection;

        public StatusMenu(Transform parent) : base(parent)
        {
            CreateAuthenticationSection();
            CreateWebSocketSection();
        }

        private void CreateAuthenticationSection()
        {
            // Dimensions - Menu width minus 20

            // Authentication Section
            authSection = CreateSection("Twitch Authentication Status", 25, 75);

            // Authentication Status Message
            authStatus = CreateTextDisplay(authSection.transform, Settings.Instance.authentication_status, 15, 25);
            
            // Authentication Button
            authButton = CreateButton(authSection.transform,
            string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken) ? "Request Authorization Token" : "Validate Token",
            90, 55,
            Color.white,
            () => {
                if (string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken))
                _ = OAuthTokenManager.GetOathToken();
                else
                _ = OAuthTokenManager.ValidateAuthToken();
            }
            );
        }
        private void CreateWebSocketSection()
        {
            // Dimensions - Menu width minus 20

            // WebSocket Section
            wsSection = CreateSection("WebSocket Status", 110, 180);

            // Connection Status Indicator
            connectionIndicator = CreateTextDisplay(wsSection.transform, "■", 25, 25);
            connectionStatus = CreateTextDisplay(wsSection.transform, WebSocketManager.IsConnectionHealthy ? "Connected" : "Disconnected", 40, 25);
            
            // Connection Button
            connectButton = CreateButton(wsSection.transform,
            WebSocketManager.IsConnectionHealthy ? "Disconnect" : "Connect",
            90, 55,
            Color.white,
            () => {
                if (WebSocketManager.IsConnectionHealthy)
                _ = WebSocketManager.DisconnectFromoWebSocket();
                else
                _ = WebSocketManager.ConnectToWebSocket();
            },
            80  // Fixed width that accommodates both "Connect" and "Disconnect"
            );

            // Horizontal line
            CreateHorizontalBar(wsSection.transform, 75);
            
            // Last Message Type
            CreateLabel(wsSection.transform, "Last Type Received", 5, 85);
            lastMessageType = CreateTextDisplay(wsSection.transform, WebSocketManager.lastMessageType, 15, 105);
            
            // Last Chat Message
            CreateLabel(wsSection.transform, "At time:", 25, 125);
            lastTypeReceivedTime = CreateTextDisplay(wsSection.transform, WebSocketManager.lastTypeReceivedTime.ToString("h:mm:ss tt"), 75, 125);

            // Horizontal line
            CreateHorizontalBar(wsSection.transform, 150);
            
            // Last Chat Message
            CreateLabel(wsSection.transform, "Last Keepalive: ", 5, 160);
            lastKeepaliveTime = CreateTextDisplay(wsSection.transform, WebSocketManager.lastKeepaliveTime.ToString("h:mm:ss tt"), 100, 160);
        }

        public void UpdateStatusMenuValues()
        {
            // Update authentication status
            string authText = string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken) 
                ? "Request Authorization Token" 
                : "Validate Token";
            authButton.GetComponentInChildren<Text>().text = authText;
            
            Color authColor = Settings.Instance.authentication_status switch
            {
                "Validated!" => Color.green,
                "Authorization failed. Please try again." or "No Username Set" => Color.red,
                "Unverified or not set" => Color.yellow,
                _ => Color.cyan
            };
            authStatus.color = authColor;
            authStatus.text = Settings.Instance.authentication_status;
            authButton.interactable = Settings.Instance.authentication_status != "Validated!";

            // Update websocket status
            connectButton.GetComponentInChildren<Text>().text = 
                WebSocketManager.IsConnectionHealthy ? "Disconnect" : "Connect";
            
            connectionIndicator.color = WebSocketManager.IsConnectionHealthy ? Color.green : Color.red;
            connectionStatus.text = WebSocketManager.IsConnectionHealthy ? "Connected" : "Disconnected";
            connectionStatus.color = WebSocketManager.IsConnectionHealthy ? Color.green : Color.red;

            lastMessageType.text = WebSocketManager.lastMessageType;
            lastMessageType.color = Color.cyan;

            lastTypeReceivedTime.text = WebSocketManager.lastTypeReceivedTime.ToString("h:mm:ss tt");
            lastTypeReceivedTime.color = Color.cyan;

            lastKeepaliveTime.text = WebSocketManager.lastKeepaliveTime.ToString("h:mm:ss tt");
            lastKeepaliveTime.color = Color.cyan;
        }

        public override void Show()
        {
            base.Show();
            if (authSection != null) authSection.SetActive(!isMinimized);
            if (wsSection != null) wsSection.SetActive(!isMinimized);
        }
    }
}
