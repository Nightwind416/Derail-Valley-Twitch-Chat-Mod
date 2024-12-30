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
        private Text lastChatMessage;

        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public StatusMenu(Transform parent) : base(parent)
        {
            CreateStatusMenu();
        }

        private void CreateStatusMenu()
        {
            // Dimensions - 200x300

            // Title
            CreateTitle("Status Menu", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 190, 10, Color.white, () => OnBackButtonClicked?.Invoke());

            // Authentication Section
            GameObject authSection = CreateSection("Authentication Status", 25, 100);

            // Authentication Status Message
            authStatus = CreateMessageDisplay(authSection.transform, Settings.Instance.authentication_status, 25, 35);
            
            // Authentication Button
            authButton = CreateButton(authSection.transform,
            string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken) ? "Request Authorization Token" : "Validate Token",
            100, 75,
            Color.white,
            () => {
                if (string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken))
                _ = OAuthTokenManager.GetOathToken();
                else
                _ = OAuthTokenManager.ValidateAuthToken();
            }
            );
            // authButton.transform.SetParent(authSection.transform, false);

            // WebSocket Section
            GameObject wsSection = CreateSection("WebSocket Status", 150, 120);
            
            // Connection Status Label
            CreateLabel(wsSection.transform, "Connection Status:", 10, 50);

            // Connection Status Indicator
            connectionIndicator = CreateMessageDisplay(wsSection.transform, "■", 25, 60);
            connectionStatus = CreateMessageDisplay(wsSection.transform, WebSocketManager.IsConnectionHealthy ? "Connected" : "Disconnected", 25, 80);
            
            // Connection Button
            connectButton = CreateButton(wsSection.transform,
            WebSocketManager.IsConnectionHealthy ? "Disconnect" : "Connect",
            50, 35,
            Color.white,
            () => {
                if (WebSocketManager.IsConnectionHealthy)
                _ = WebSocketManager.DisconnectFromoWebSocket();
                else
                _ = WebSocketManager.ConnectToWebSocket();
            }
            );
            connectButton.transform.SetParent(wsSection.transform, false);

            // Last Message Type
            CreateLabel(wsSection.transform, "Last Message Type:", 10, 100);
            lastMessageType = CreateMessageDisplay(wsSection.transform, WebSocketManager.LastMessageType, 25, 110);

            // Last Chat Message
            CreateLabel(wsSection.transform, "Last Chat Message:", 10, 120);
            lastChatMessage = CreateMessageDisplay(wsSection.transform, WebSocketManager.LastChatMessage, 25, 125);
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

            lastChatMessage.text = WebSocketManager.LastChatMessage;
            lastChatMessage.color = Color.cyan;
        }
    }
}
