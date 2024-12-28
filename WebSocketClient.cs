using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchChat
{
    /// <summary>
    /// Manages WebSocket connections and message handling for Twitch chat integration.
    /// Handles connection establishment, monitoring, reconnection, and message processing.
    /// Provides real-time communication with Twitch's EventSub service.
    /// </summary>
    public class WebSocketManager
    {
        private static ClientWebSocket webSocketClient = new();
        private static string session_id = string.Empty;
        private static DateTime lastKeepaliveTime = DateTime.UtcNow;
        private static bool isConnectionHealthy = false;
        private static Timer? connectionMonitorTimer;
        public static bool IsConnectionHealthy => isConnectionHealthy;

        private static string lastMessageType = "None";
        private static string lastChatMessage = "No messages received";
        public static string LastMessageType => lastMessageType;
        public static string LastChatMessage => lastChatMessage;

        private static readonly SemaphoreSlim reconnectLock = new(1, 1);
        private static int reconnectAttempts = 0;
        private static readonly int maxReconnectAttempts = 5;
        private static readonly TimeSpan reconnectDelay = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Establishes a WebSocket connection to Twitch's EventSub service.
        /// </summary>
        /// <returns>A task representing the asynchronous connection operation.</returns>
        public static async Task ConnectToWebSocket()
        {
            string methodName = "ConnectToWebSocket";

            if (string.IsNullOrEmpty(Settings.Instance.twitchUsername))
            {
                Main.LogEntry(methodName, "Twitch username is empty. Cannot attempt connection to WebSocket.");
                MessageHandler.SetVariable("alertMessage", "Twitch username is empty. Cannot attempt connection to WebSocket.");
                return;
            }

            if (string .IsNullOrEmpty(Settings.Instance.EncodedOAuthToken))
            {
                Main.LogEntry(methodName, "Access token is empty. Cannot attempt connection to WebSocket.");
                MessageHandler.SetVariable("alertMessage", "Access token is empty. Cannot attempt connection to WebSocket.");
                return;
            }

            if (webSocketClient.State == WebSocketState.Open)
            {
                Main.LogEntry(methodName, "WebSocket is already open. Cannot attempt connection to WebSocket.");
                MessageHandler.SetVariable("alertMessage", "WebSocket is already open. Cannot attempt connection to WebSocket.");
                return;
            }

            try
            {
                Uri serverUri = new("wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds=30");
                
                webSocketClient?.Dispose();
                webSocketClient = new ClientWebSocket();
                await webSocketClient.ConnectAsync(serverUri, CancellationToken.None);
                Main.LogEntry(methodName, "Connected to WebSocket server.");

                // Initialize connection monitoring
                lastKeepaliveTime = DateTime.UtcNow;
                isConnectionHealthy = true;
                connectionMonitorTimer = new Timer(CheckConnectionHealth, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));

                // Start receiving messages
                _ = Task.Run(ReceiveMessages);
                

            }
            catch (Exception ex)
            {
                isConnectionHealthy = false;
                Main.LogEntry(methodName, $"Connection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Monitors the health of the WebSocket connection by checking keepalive messages.
        /// </summary>
        /// <param name="state">Timer state object (unused).</param>
        private static void CheckConnectionHealth(object state)
        {
            var timeSinceLastKeepalive = DateTime.UtcNow - lastKeepaliveTime;
            bool wasHealthy = isConnectionHealthy;
            
            // Reduce the threshold to 35 seconds (Twitch timeout is 30s)
            isConnectionHealthy = timeSinceLastKeepalive <= TimeSpan.FromSeconds(35) 
                && webSocketClient.State == WebSocketState.Open;

            if (wasHealthy && !isConnectionHealthy)
            {
                Main.LogEntry("CheckConnectionHealth", $"Connection appears to be dead (no keepalive for {timeSinceLastKeepalive.TotalSeconds:F1}s)");
                _ = ReconnectAsync();
            }
            else if (!wasHealthy && isConnectionHealthy)
            {
                Main.LogEntry("CheckConnectionHealth", "Connection restored");
                reconnectAttempts = 0; // Reset reconnect attempts when connection is healthy
            }
        }

        /// <summary>
        /// Attempts to reconnect the WebSocket connection by disconnecting and connecting again.
        /// </summary>
        private static async Task ReconnectAsync()
        {
            try
            {
                if (!await reconnectLock.WaitAsync(0)) // Don't wait if already reconnecting
                {
                    Main.LogEntry("ReconnectAsync", "Reconnection already in progress");
                    return;
                }

                using (reconnectLock)
                {
                    if (reconnectAttempts >= maxReconnectAttempts)
                    {
                        Main.LogEntry("ReconnectAsync", "Max reconnection attempts reached");
                        isConnectionHealthy = false;
                        return;
                    }

                    Main.LogEntry("ReconnectAsync", $"Attempting reconnect {reconnectAttempts + 1}/{maxReconnectAttempts}");
                    
                    await DisconnectFromoWebSocket();
                    
                    // Add delay before reconnecting
                    await Task.Delay(reconnectDelay);
                    
                    await ConnectToWebSocket();
                    reconnectAttempts++;
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry("ReconnectAsync", $"Reconnection error: {ex.Message}");
                isConnectionHealthy = false;
            }
        }

        /// <summary>
        /// Continuously receives and processes messages from the WebSocket connection.
        /// </summary>
        private static async Task ReceiveMessages()
        {
            string methodName = "ReceiveMessages";
            var buffer = new byte[1024 * 4];

            while (webSocketClient.State == WebSocketState.Open)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(35));
                    var result = await webSocketClient.ReceiveAsync(
                        new ArraySegment<byte>(buffer), 
                        cts.Token
                    );

                    // Reset connection monitoring on any successful message
                    lastKeepaliveTime = DateTime.UtcNow;

                    // Simple string-based message type extraction
                    string messageType = ExtractValue(Encoding.UTF8.GetString(buffer, 0, result.Count), "message_type");
                    lastMessageType = messageType;

                    switch (messageType)
                    {
                        case "session_welcome":
                            session_id = ExtractValue(Encoding.UTF8.GetString(buffer, 0, result.Count), "id");
                            Main.LogEntry(methodName, $"Session ID: {session_id}");
                            Main.LogEntry(methodName, "TwitchChat connected to WebSocket");
                            await RegisterWebbSocketChatEvent();
                            break;

                        case "notification":
                            string userName = ExtractValue(Encoding.UTF8.GetString(buffer, 0, result.Count), "chatter_user_name");
                            string chatMessage = ExtractValue(Encoding.UTF8.GetString(buffer, 0, result.Count), "text");
                            lastChatMessage = $"{userName}: {chatMessage}";
                            MessageHandler.HandleNotification(Encoding.UTF8.GetString(buffer, 0, result.Count));
                            break;

                        case "session_keepalive":
                            lastKeepaliveTime = DateTime.UtcNow;
                            isConnectionHealthy = true;
                            Main.LogEntry(methodName, "Received keepalive message.");
                            break;

                        case "session_reconnect":
                            Main.LogEntry(methodName, "Received reconnect message.");
                            break;

                        case "revocation":
                            Main.LogEntry(methodName, "Received revocation message.");
                            break;

                        default:
                            Main.LogEntry(methodName, $"Unknown message type: {messageType}");
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    Main.LogEntry(methodName, "Receive operation timed out");
                    isConnectionHealthy = false;
                    _ = ReconnectAsync();
                    break;
                }
                catch (WebSocketException wsEx)
                {
                    Main.LogEntry(methodName, $"WebSocket error: {wsEx.Message}");
                    isConnectionHealthy = false;
                    _ = ReconnectAsync();
                    break;
                }
                catch (Exception ex)
                {
                    Main.LogEntry(methodName, $"Error receiving message: {ex.Message}");
                    continue;
                }
            }
            
            if (webSocketClient.CloseStatus.HasValue)
            {
                string closeReason = webSocketClient.CloseStatusDescription;
                int closeCode = (int)webSocketClient.CloseStatus.Value;
                Main.LogEntry(methodName, $"WebSocket connection closed with code {closeCode}: {closeReason}");
            }
            else
            {
                Main.LogEntry(methodName, "WebSocket connection closed.");
            }
        }

        private static string ExtractValue(string json, string key)
        {
            int keyIndex = json.IndexOf($"\"{key}\"");
            if (keyIndex == -1) return string.Empty;

            int valueStart = json.IndexOf(':', keyIndex) + 1;
            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart])) valueStart++;

            if (valueStart >= json.Length) return string.Empty;

            if (json[valueStart] == '"')
            {
                valueStart++;
                int valueEnd = json.IndexOf('"', valueStart);
                return valueEnd == -1 ? string.Empty : json.Substring(valueStart, valueEnd - valueStart);
            }
            else
            {
                int valueEnd = json.IndexOfAny([',', '}'], valueStart);
                return valueEnd == -1 ? string.Empty : json.Substring(valueStart, valueEnd - valueStart).Trim();
            }
        }

        /// <summary>
        /// Registers for Twitch chat events using the WebSocket connection.
        /// </summary>
        private static async Task RegisterWebbSocketChatEvent()
        {
            string methodName = "RegisterWebbSocketChatEvent";
            if (webSocketClient.State != WebSocketState.Open)
            {
                Main.LogEntry(methodName, "WebSocket is not open. Cannot send subscription request.");
                return;
            }

            var jsonBody = $@"{{
                ""type"": ""channel.chat.message"",
                ""version"": ""1"",
                ""condition"": {{
                    ""broadcaster_user_id"": ""{TwitchEventHandler.user_id}"",
                    ""user_id"": ""{TwitchEventHandler.user_id}""
                }},
                ""transport"": {{
                    ""method"": ""websocket"",
                    ""session_id"": ""{session_id}""
                }}
            }}";
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        
            byte[] tokenBytes = Convert.FromBase64String(Settings.Instance.EncodedOAuthToken);
            _ = Encoding.UTF8.GetString(tokenBytes);
            string access_token = Encoding.UTF8.GetString(tokenBytes);

            TwitchEventHandler.httpClient.DefaultRequestHeaders.Clear();
            TwitchEventHandler.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            TwitchEventHandler.httpClient.DefaultRequestHeaders.Add("Client-ID", TwitchEventHandler.GetClientId());
            var response = await TwitchEventHandler.httpClient.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Main.LogEntry(methodName, $"Failed to subscribe to channel.chat.message. Status: {response.StatusCode}, Error: {errorContent}");
            }
            else
            {
                Main.LogEntry(methodName, "Subscribed to channel.chat.message.");
                if (!string.IsNullOrEmpty(Settings.Instance.welcomeMessage))
                {
                    await TwitchEventHandler.SendMessage(Settings.Instance.welcomeMessage);
                }
            }
        }

        /// <summary>
        /// Gracefully disconnects from the WebSocket server.
        /// </summary>
        public static async Task DisconnectFromoWebSocket()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            
            if (webSocketClient.State == WebSocketState.Open)
            {
                if (!string.IsNullOrEmpty(Settings.Instance.disconnectMessage))
                {
                    await TwitchEventHandler.SendMessage(Settings.Instance.disconnectMessage);
                    // Small delay to ensure the message is sent before closing
                    await Task.Delay(500);
                }
                
                connectionMonitorTimer?.Dispose();
                isConnectionHealthy = false;
                await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Main.LogEntry(methodName, "Disconnected from WebSocket server.");
                AutomatedMessages.StopAndClearTimers();
            }
        }
    }
}