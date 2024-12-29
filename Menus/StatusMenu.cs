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
            CreateTitle("Status Menu", 18, Color.white, TextAnchor.UpperCenter);

            // Authentication Section
            GameObject authSection = CreateSection("Authentication", 0, 50);
            
            authButton = CreateButton(
            string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken) ? "Request Authorization Token" : "Validate Token",
            100, 20,
            Color.white,
            () => {
                if (string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken))
                _ = OAuthTokenManager.GetOathToken();
                else
                _ = OAuthTokenManager.ValidateAuthToken();
            }
            );
            authButton.transform.SetParent(authSection.transform, false);
            
            authStatus = CreateStatusIndicator(authSection.transform, Settings.Instance.authentication_status, 0.2f);

            // WebSocket Section
            GameObject wsSection = CreateSection("WebSocket Status", 100, 50);
            // CreateLabel(wsSection.transform, "Channel Connection", 0, 0);
            
            connectButton = CreateButton(
            WebSocketManager.IsConnectionHealthy ? "Disconnect" : "Connect",
            -0, 0,
            Color.white,
            () => {
                if (WebSocketManager.IsConnectionHealthy)
                _ = WebSocketManager.DisconnectFromoWebSocket();
                else
                _ = WebSocketManager.ConnectToWebSocket();
            }
            );
            connectButton.transform.SetParent(wsSection.transform, false);

            CreateLabel(wsSection.transform, "Connection Status:", 10, 50);
            connectionIndicator = CreateStatusIndicator(wsSection.transform, "■", 0.5f);
            connectionStatus = CreateStatusIndicator(wsSection.transform, 
            WebSocketManager.IsConnectionHealthy ? "Connected" : "Disconnected", 0.5f, 155f);

            CreateLabel(wsSection.transform, "Last Message Type:", 10, 30);
            lastMessageType = CreateStatusIndicator(wsSection.transform, WebSocketManager.LastMessageType, 0.3f);

            CreateLabel(wsSection.transform, "Last Chat Message:", 10, 10);
            lastChatMessage = CreateStatusIndicator(wsSection.transform, WebSocketManager.LastChatMessage, 0.1f);

            Button backButton = CreateButton("Back", 0, -125, Color.white, () => OnBackButtonClicked?.Invoke());
        }

        public void Update()
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
