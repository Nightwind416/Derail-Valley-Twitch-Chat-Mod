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

        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public StatusMenu(Transform parent) : base(parent)
        {
            CreateStatusMenu();
            CreateAuthenticationSection();
            CreateWebSocketSection();
        }

        private void CreateStatusMenu()
        {
            // Dimensions - 200x300

            // Title
            CreateTitle("Status Menu", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 190, 10, Color.white, () => OnBackButtonClicked?.Invoke());

        }
        private void CreateAuthenticationSection()
        {
            // Dimensions - Menu width minus 20

            // Authentication Section
            GameObject authSection = CreateSection("Authentication Status", 25, 75);

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
            GameObject wsSection = CreateSection("WebSocket Status", 120, 175);

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

            // Last Message Type
            CreateLabel(wsSection.transform, "Last Type Received", 15, 75);
            lastMessageType = CreateTextDisplay(wsSection.transform, WebSocketManager.LastMessageType, 25, 95);

            // Last Chat Message
            CreateLabel(wsSection.transform, "At time:", 15, 115);
            lastTypeReceivedTime = CreateTextDisplay(wsSection.transform, WebSocketManager.lastTypeReceivedTime.ToString("h:mm:ss tt"), 60, 115);

            // Last Chat Message
            CreateLabel(wsSection.transform, "Last Keepalive Received", 15, 135);
            lastKeepaliveTime = CreateTextDisplay(wsSection.transform, WebSocketManager.lastKeepaliveTime.ToString("h:mm:ss tt"), 25, 155);
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

            lastMessageType.text = WebSocketManager.LastMessageType;
            lastMessageType.color = Color.cyan;

            lastTypeReceivedTime.text = WebSocketManager.lastTypeReceivedTime.ToString("h:mm:ss tt");
            lastTypeReceivedTime.color = Color.cyan;

            lastKeepaliveTime.text = WebSocketManager.lastKeepaliveTime.ToString("h:mm:ss tt");
            lastKeepaliveTime.color = Color.cyan;
        }
    }
}
