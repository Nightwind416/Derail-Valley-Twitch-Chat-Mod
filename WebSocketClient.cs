using System;
using System.Net.WebSockets;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TwitchChat
{
    public class WebSocketClient
    {
        private static ClientWebSocket _webSocket = new();
        private static readonly Uri _serverUri = new("wss://eventsub.wss.twitch.tv/ws");
        private static string _sessionId = string.Empty;
        private static readonly HttpClient httpClient = new();

        public static async Task ConnectToWebSocket()
        {
            try
            {
                await ValidateAuthToken();

                if (_webSocket != null)
                {
                    _webSocket.Dispose();
                }
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(_serverUri, CancellationToken.None);
                Main.ModEntry.Logger.Log("[WS-Connect] Connected to WebSocket server.");

                // Start receiving messages
                _ = Task.Run(ReceiveMessages);

            }
            catch (Exception ex)
            {
                Main.ModEntry.Logger.Log($"[WS-Connect] Connection error: {ex.Message}");
            }
        }

        private static async Task ValidateAuthToken()
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Main.Settings.twitch_oauth_token);
            Main.ModEntry.Logger.Log($"[WS-Validate] Validating token: {Main.Settings.twitch_oauth_token}");
            var response = await httpClient.GetAsync("https://id.twitch.tv/oauth2/validate");
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Main.ModEntry.Logger.Log("[WS-Validate] Token is not valid.");
                throw new Exception("Invalid OAuth token.");
            }
            Main.ModEntry.Logger.Log("[WS-Validate] Validated token.");
        }

        private static async Task ReceiveMessages()
        {
            var buffer = new byte[1024 * 4];
            while (_webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Main.ModEntry.Logger.Log($"[WS-Receive] Received message: {message}");
        
                    var jsonMessage = JsonConvert.DeserializeObject<dynamic>(message);
                    if (jsonMessage?.metadata?.message_type == "session_welcome")
                    {
                        _sessionId = jsonMessage.payload.session.id;
                        Main.ModEntry.Logger.Log($"[WS-Receive] Session ID: {_sessionId}");
        
                        await RegisterEventSubListeners();
                    }
                    else if (jsonMessage?.metadata?.message_type == "subscription_success")
                    {
                        Main.ModEntry.Logger.Log("[WS-Receive] Successfully subscribed to the channel.");
                    }
                    else if (jsonMessage?.metadata?.message_type == "notification")
                    {
                        HandleNotification(jsonMessage);
                    }
                }
                catch (WebSocketException ex)
                {
                    Main.ModEntry.Logger.Log($"[WS-Receive] WebSocket error: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Main.ModEntry.Logger.Log($"[WS-Receive] Error receiving message: {ex.Message}");
                }
            }
        
            Main.ModEntry.Logger.Log("[WS-Receive] WebSocket connection closed.");
        }

        private static void HandleNotification(dynamic jsonMessage)
        {
            Main.ModEntry.Logger.Log("[WS-HandleNotification] HandleNotification called with message: " + jsonMessage.ToString());
        
            if (jsonMessage.metadata.subscription_type == "channel.chat.message")
            {
                Main.ModEntry.Logger.Log("[WS-HandleNotification] Message type is channel.chat.message");
        
                var chatter = jsonMessage.payload.@event.chatter_user_name;
                var text = jsonMessage.payload.@event.message.text;
        
                Main.ModEntry.Logger.Log($"[WS-HandleNotification] Extracted chatter: {chatter}");
                Main.ModEntry.Logger.Log($"[WS-HandleNotification] Extracted text: {text}");
        
                Main.ModEntry.Logger.Log($"[WS-HandleNotification] Message: #{chatter}: {text}");
                Main.TwitchChatMessages($"{chatter}: {text}");
        
                if (text.Trim() == "HeyGuys")
                {
                    Main.ModEntry.Logger.Log("[WS-HandleNotification] Text is 'HeyGuys', sending 'VoHiYo'");
                    SendChatMessage("VoHiYo").Wait();
                }
                else
                {
                    Main.ModEntry.Logger.Log("[WS-HandleNotification] Text is not 'HeyGuys'");
                }
            }
            else
            {
                Main.ModEntry.Logger.Log("[WS-HandleNotification] Message type is not channel.chat.message");
            }
        }


        private static async Task SendChatMessage(string chatMessage)
        {
            Main.ModEntry.Logger.Log($"[WS-SendMessage] Preparing to send chat message: {chatMessage}");
        
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                broadcaster_id = Main.Settings.userID,
                sender_id = Main.Settings.userID,
                message = chatMessage
            }), Encoding.UTF8, "application/json");
        
            Main.ModEntry.Logger.Log($"[WS-SendMessage] Created content: {content}");
        
            try
            {
                var response = await httpClient.PostAsync("https://api.twitch.tv/helix/chat/messages", content);
                Main.ModEntry.Logger.Log($"[WS-SendMessage] Received response: {response.StatusCode}");
        
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Main.ModEntry.Logger.Log("[WS-SendMessage] Failed to send chat message.");
                }
                else
                {
                    Main.ModEntry.Logger.Log($"[WS-SendMessage] Sent chat message: {chatMessage}");
                }
            }
            catch (Exception ex)
            {
                Main.ModEntry.Logger.Log($"[WS-SendMessage] Exception occurred: {ex.Message}");
            }
        }

        private static async Task RegisterEventSubListeners()
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                Main.ModEntry.Logger.Log("[WS-RegisterListener] WebSocket is not open. Cannot send subscription request.");
                return;
            }

            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                type = "channel.chat.message",
                version = "1",
                condition = new
                {
                    broadcaster_user_id = Main.Settings.userID,
                    user_id = Main.Settings.userID
                },
                transport = new
                {
                    method = "websocket",
                    session_id = _sessionId
                }
            }), Encoding.UTF8, "application/json");
        
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Main.Settings.twitch_oauth_token);
            httpClient.DefaultRequestHeaders.Add("Client-ID", Main.Settings.client_id);
            var response = await httpClient.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Main.ModEntry.Logger.Log($"[WS-RegisterListener] Failed to subscribe to channel.chat.message. Status: {response.StatusCode}, Error: {errorContent}");
            }
            else
            {
                Main.ModEntry.Logger.Log("[WS-RegisterListener] Subscribed to channel.chat.message.");
            }
        }

        public static async Task DisconnectAsync()
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Main.ModEntry.Logger.Log("[WS-Disconnect] Disconnected from WebSocket server.");
            }
        }
    }
}