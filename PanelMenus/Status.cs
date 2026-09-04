using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Panel for displaying and managing connection status information.
    /// Handles authentication status, WebSocket connection state, and related metrics.
    /// </summary>
    public class StatusPanel : PanelConstructor.BasePanel
    {
        private Button? authButton;
        private Button? revokeButton;
        private Text? authStatus;
        private Button? connectButton;
        private Text? connectionStatus;
        private Text? connectionIndicator;
        private Text? lastMessageType;
        private Text? lastTypeReceivedTime;
        private Text? lastKeepaliveTime;

        private GameObject? authSection;
        private GameObject? wsSection;

        public StatusPanel(Transform parent) : base(parent)
        {
            CreateAuthenticationSection();
            CreateWebSocketSection();
        }

        /// <summary>
        /// Creates the authentication section of the panel.
        /// Includes status display and authentication controls.
        /// </summary>
        private void CreateAuthenticationSection()
        {;
            
            // Dimensions - Menu width minus 20

            // Authentication Section
            authSection = PanelConstructor.Section.Create(panelObject.transform, "Twitch Authentication", 25, 100);

            // Authentication Status Message
            authStatus = PanelConstructor.DisplayText.Create(authSection.transform, OAuthTokenManager.StatusMessage, 15, 25);

            // Authentication Button
            authButton = PanelConstructor.Button.Create(authSection.transform,
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

            // Sign Out button. Revokes the token at Twitch and deletes the local copy, so a user
            // can withdraw the mod's access from inside the game rather than hunting through
            // Twitch's own connection settings.
            revokeButton = PanelConstructor.Button.Create(authSection.transform,
            "Sign Out & Revoke Access",
            90, 80,
            Color.white,
            () => {
                _ = OAuthTokenManager.RevokeAndSignOut();
            }
            );
        }

        /// <summary>
        /// Creates the WebSocket status section of the panel.
        /// Displays connection status, message statistics, and connection controls.
        /// </summary>
        private void CreateWebSocketSection()
        {

            // Dimensions - Menu width minus 20

            // WebSocket Section
            wsSection = PanelConstructor.Section.Create(panelObject.transform, "WebSocket Status", 135, 155);

            // Connection Status Indicator
            connectionIndicator = PanelConstructor.DisplayText.Create(wsSection.transform, "■", 25, 25);
            connectionStatus = PanelConstructor.DisplayText.Create(wsSection.transform, WebSocketManager.IsConnectionHealthy ? "Connected" : "Disconnected", 40, 25);
            
            // Connection Button
            connectButton = PanelConstructor.Button.Create(wsSection.transform,
            WebSocketManager.IsConnectionHealthy ? "Disconnect" : "Connect",
            90, 55,
            Color.white,
            () => {
                if (WebSocketManager.IsConnectionHealthy)
                _ = WebSocketManager.DisconnectFromWebSocket();
                else
                _ = WebSocketManager.ConnectToWebSocket();
            },
            70
            );

            // Horizontal line
            PanelConstructor.HorizontalBar.Create(wsSection.transform, 75);
            
            // Last Message Type
            PanelConstructor.Label.Create(wsSection.transform, "Last Packet Type Received", 5, 85);
            lastMessageType = PanelConstructor.DisplayText.Create(wsSection.transform, WebSocketManager.lastMessageType, 15, 105);
            
            // Last Chat Message
            PanelConstructor.Label.Create(wsSection.transform, "At time:", 25, 125);
            lastTypeReceivedTime = PanelConstructor.DisplayText.Create(wsSection.transform, WebSocketManager.lastTypeReceivedTime.ToString("h:mm:ss tt"), 75, 125);

            // Horizontal line
            PanelConstructor.HorizontalBar.Create(wsSection.transform, 150);
            
            // Last Chat Message
            PanelConstructor.Label.Create(wsSection.transform, "Last Keepalive: ", 5, 160);
            lastKeepaliveTime = PanelConstructor.DisplayText.Create(wsSection.transform, WebSocketManager.lastKeepaliveTime.ToString("h:mm:ss tt"), 100, 160);
        }

        /// <summary>
        /// Updates all status indicators and values in the panel.
        /// Should be called when any relevant status changes occur.
        /// </summary>
        public void UpdateStatusPanelValues()
        {
            if (authButton != null && authStatus != null && revokeButton != null && connectButton != null &&
                connectionIndicator != null && connectionStatus != null && lastMessageType != null &&
                lastTypeReceivedTime != null && lastKeepaliveTime != null)
            {
                // Update authentication status
                bool hasToken = !string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken);
                authButton.GetComponentInChildren<Text>().text = hasToken
                    ? "Validate Token"
                    : "Request Authorization Token";

                authStatus.color = OAuthTokenManager.Phase switch
                {
                    AuthPhase.Connected => Color.green,
                    AuthPhase.Failed => Color.red,
                    AuthPhase.AwaitingUser => Color.cyan,
                    _ => Color.yellow
                };
                authStatus.text = OAuthTokenManager.StatusMessage;
                authButton.interactable = OAuthTokenManager.Phase != AuthPhase.Connected;

                // Signing out is only meaningful while something is stored to sign out of.
                revokeButton.gameObject.SetActive(hasToken);

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
        }

        /// <summary>
        /// Controls the visibility of the panel and its sections.
        /// </summary>
        public override void Show()
        {
            base.Show();
            authSection?.SetActive(!isMinimized);
            wsSection?.SetActive(!isMinimized);
        }
    }
}
