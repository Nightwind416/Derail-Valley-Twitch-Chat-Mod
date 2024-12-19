using System;
using System.Net.WebSockets;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Reflection;

namespace TwitchChat
{
    public class WebSocketManager
    {
        private static ClientWebSocket webSocketClient = new();
        private static readonly Uri serverUri = new("wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds=30");
        public static string session_id = string.Empty;
        public static string lastWebSocketTypeReceived = "None";
        public static string lastWebSocketMessageReceived = "None";
        public static async Task ConnectToWebSocket()
        {
            string methodName = "ConnectToWebSocket";

            if (string.IsNullOrEmpty(Settings.Instance.twitchUsername))
            {
                Main.LogEntry(methodName, "Twitch username is empty. Cannot attempt connection to WebSocket.");
                MessageHandler.SetVariable("alertMessage", "Twitch username is empty. Cannot attempt connection to WebSocket.");
                return;
            }

            if (string .IsNullOrEmpty(TwitchEventHandler.oath_access_token))
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
                webSocketClient?.Dispose();
                webSocketClient = new ClientWebSocket();
                await webSocketClient.ConnectAsync(serverUri, CancellationToken.None);
                Main.LogEntry(methodName, "Connected to WebSocket server.");

                // Start receiving messages
                _ = Task.Run(ReceiveMessages);

            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Connection error: {ex.Message}");
            }
        }
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
                        var jsonMessage = JsonConvert.DeserializeObject<dynamic>(message);
                        if (jsonMessage == null)
                        {
                            Main.LogEntry(methodName, "Failed to parse JSON message");
                            continue;
                        }
                        if (jsonMessage?.metadata?.message_type == "session_welcome")
                        {
                            session_id = jsonMessage.payload.session.id.ToString().Trim();
                            Main.LogEntry(methodName, $"Session ID: {session_id}");
            
                            lastWebSocketTypeReceived = "Session Welcome";
                            Main.LogEntry(methodName, "TwitchChat connected to WebSocket");
                            await RegisterWebbSocketChatEvent();
                        }
                        else if (jsonMessage?.metadata?.message_type == "notification")
                        {
                            lastWebSocketTypeReceived = "Notification";
                            MessageHandler.HandleNotification(jsonMessage);
                        }
                        else if (jsonMessage?.metadata?.message_type == "session_keepalive")
                        {
                            lastWebSocketTypeReceived = "Keepalive";
                            Main.LogEntry(methodName, "Received keepalive message.");
                        }
                        else if (jsonMessage?.metadata?.message_type == "session_reconnect")
                        {
                            lastWebSocketTypeReceived = "Reconnect";
                            Main.LogEntry(methodName, "Received reconnect message.");
                        }
                        else if (jsonMessage?.metadata?.message_type == "revocation")
                        {
                            lastWebSocketTypeReceived = "Revocation";
                            Main.LogEntry(methodName, "Received revocation message.");
                        }
                        else
                        {
                            lastWebSocketTypeReceived = "Unknown";
                            Main.LogEntry(methodName, $"Unknown message type: {jsonMessage?.metadata?.message_type}");
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
                    lastWebSocketTypeReceived = "WebSocket Error";
                    break;
                }
                catch (Exception ex)
                {
                    lastWebSocketTypeReceived = "Receive Message Error";
                    Main.LogEntry(methodName, $"Error receiving message: {ex.Message}");
                }
            }
            
            if (webSocketClient.CloseStatus.HasValue)
            {
                string closeReason = webSocketClient.CloseStatusDescription;
                int closeCode = (int)webSocketClient.CloseStatus.Value;
                Main.LogEntry(methodName, $"WebSocket connection closed with code {closeCode}: {closeReason}");
                lastWebSocketTypeReceived = $"Connection closed with code {closeCode}: {closeReason}";
            }
            else
            {
                lastWebSocketTypeReceived = "Connection closed.";
                Main.LogEntry(methodName, "WebSocket connection closed.");
            }
        }
        private static async Task RegisterWebbSocketChatEvent()
        {
            string methodName = "RegisterWebbSocketChatEvent";
            if (webSocketClient.State != WebSocketState.Open)
            {
                Main.LogEntry(methodName, "WebSocket is not open. Cannot send subscription request.");
                return;
            }

            var content = new StringContent(JsonConvert.SerializeObject(new
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
        
            TwitchEventHandler.httpClient.DefaultRequestHeaders.Clear();
            TwitchEventHandler.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TwitchEventHandler.oath_access_token);
            TwitchEventHandler.httpClient.DefaultRequestHeaders.Add("Client-ID", Main.GetClientId());
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
        public static async Task DisconnectFromoWebSocket()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            if (webSocketClient.State == WebSocketState.Open)
            {
                await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Main.LogEntry(methodName, "Disconnected from WebSocket server.");
            }
        }
    }
}