using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchChat
{
    /// <summary>
    /// Manages WebSocket connections and message handling for Twitch chat integration.
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
            isConnectionHealthy = timeSinceLastKeepalive <= TimeSpan.FromSeconds(45);  // Twitch timeout is 30s, this adds buffer

            if (wasHealthy && !isConnectionHealthy)
            {
                Main.LogEntry("CheckConnectionHealth", "Connection appears to be dead (no recent keepalive)");
                _ = ReconnectAsync();
            }
        }

        /// <summary>
        /// Attempts to reconnect the WebSocket connection by disconnecting and connecting again.
        /// </summary>
        private static async Task ReconnectAsync()
        {
            await DisconnectFromoWebSocket();
            await ConnectToWebSocket();
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
                    var result = await webSocketClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
        
                    // Skip empty messages
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        Main.LogEntry(methodName, "Received empty message, skipping...");
                        continue;
                    }
            
                    Main.LogEntry(methodName, $"Received message: \n{message}");
        
                    try
                    {
                        var jsonMessage = JsonSerializer.Deserialize<JsonElement>(message);
                        if (jsonMessage.ValueKind == JsonValueKind.Undefined)
                        {
                            Main.LogEntry(methodName, "Failed to parse JSON message");
                            continue;
                        }
                        if (jsonMessage.TryGetProperty("metadata", out JsonElement metadata) && metadata.TryGetProperty("message_type", out JsonElement messageType))
                        {
                            string messageTypeString = messageType.GetString() ?? string.Empty;
                            lastMessageType = messageTypeString;  // Update last message type

                            if (messageTypeString == null)
                            {
                                Main.LogEntry(methodName, "Received message with null message type, skipping...");
                                continue;
                            }
                            if (messageTypeString == "session_welcome")
                            {
                                var sessionId = jsonMessage.GetProperty("payload").GetProperty("session").GetProperty("id").GetString()?.Trim();
                                if (!string.IsNullOrEmpty(sessionId))
                                {
                                    session_id = sessionId!;
                                }
                                
                                Main.LogEntry(methodName, $"Session ID: {session_id}");
                                Main.LogEntry(methodName, "TwitchChat connected to WebSocket");
                                
                                await RegisterWebbSocketChatEvent();
                            }
                            else if (messageTypeString == "notification")
                            {
                                // Extract and store chat message details
                                if (jsonMessage.TryGetProperty("payload", out JsonElement payload) &&
                                    payload.TryGetProperty("event", out JsonElement evt))
                                {
                                    string extractedUserName = evt.GetProperty("chatter_user_name").GetString() ?? "unknown";
                                    string extractedMessage = evt.GetProperty("message").GetProperty("text").GetString() ?? "empty";
                                    lastChatMessage = $"{extractedUserName}: {extractedMessage}";
                                }
                                MessageHandler.HandleNotification(jsonMessage);
                            }
                            else if (messageTypeString == "session_keepalive")
                            {
                                lastKeepaliveTime = DateTime.UtcNow;
                                isConnectionHealthy = true;
                                Main.LogEntry(methodName, "Received keepalive message.");
                            }
                            else if (messageTypeString == "session_reconnect")
                            {
                                Main.LogEntry(methodName, "Received reconnect message.");
                            }
                            else if (messageTypeString == "revocation")
                            {
                                Main.LogEntry(methodName, "Received revocation message.");
                            }
                            else
                            {
                                Main.LogEntry(methodName, $"Unknown message type: {messageTypeString}");
                            }
                        }
                        else
                        {
                            Main.LogEntry(methodName, "Failed to parse JSON message");
                        }
                    }
                    catch (JsonException ex)
                    {
                        Main.LogEntry(methodName, $"JSON parsing error: {ex.Message}");
                    }
                }
                catch (WebSocketException ex)
                {
                    Main.LogEntry(methodName, $"WebSocket error: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Main.LogEntry(methodName, $"Error receiving message: {ex.Message}");
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

            var content = new StringContent(JsonSerializer.Serialize(new
            {
                type = "channel.chat.message",
                version = "1",
                condition = new
                {
                    broadcaster_user_id = TwitchEventHandler.user_id,
                    user_id = TwitchEventHandler.user_id
                },
                transport = new
                {
                    method = "websocket",
                    session_id = session_id
                }
            }), Encoding.UTF8, "application/json");
        
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
            }
        }

        /// <summary>
        /// Gracefully disconnects from the WebSocket server.
        /// </summary>
        public static async Task DisconnectFromoWebSocket()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            connectionMonitorTimer?.Dispose();
            isConnectionHealthy = false;
            
            if (webSocketClient.State == WebSocketState.Open)
            {
                await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Main.LogEntry(methodName, "Disconnected from WebSocket server.");
            }
        }
    }
}