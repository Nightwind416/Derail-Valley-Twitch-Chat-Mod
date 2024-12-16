using System;
using System.Net.WebSockets;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace TwitchChat
{
    public class WebSocketClient
    {
        private static ClientWebSocket webSocketClient = new();
        private static readonly Uri serverUri = new("wss://eventsub.wss.twitch.tv/ws");
        public static string session_id = string.Empty;
        public static string user_id = string.Empty;
        private static string refresh_token = string.Empty;
        public static string oath_access_token = string.Empty;
        private static readonly HttpClient httpClient = new(new LoggingHttpClientHandler());
        public static bool isWebSocketConnected = false;

        public static async Task ConnectToWebSocket()
        {
            string methodName = "ConnectToWebSocket";
            try
            {
                await TwitchEventHandler.ValidateAuthToken();

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

        public static async Task RefreshAuthToken()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token");
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", Main.GetClientId()),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refresh_token)
            });
            request.Content = content;

            var response = await httpClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseContent);
                oath_access_token = json["access_token"]?.ToString() ?? throw new Exception("Access token is null");
                refresh_token = json["refresh_token"]?.ToString() ?? throw new Exception("Refresh token is null");
                Main.LogEntry(methodName, "Token refreshed successfully.");
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Main.LogEntry(methodName, $"Failed to refresh token. Response: {response.ReasonPhrase}, Content: {responseContent}");
                throw new Exception("Failed to refresh token.");
            }
        }

        public static async Task<bool> GetUserIdAsync()
        {
            string methodName = "GetUserIdAsync";

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.twitch.tv/helix/users?login={Settings.Instance.twitchUsername}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oath_access_token);
            request.Headers.Add("Client-Id", Main.GetClientId());

            var response = await httpClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                var extracted_id = json["data"]?[0]?["id"]?.ToString();
                if (!string.IsNullOrEmpty(extracted_id))
                {
                    if (extracted_id != null)
                    {
                        user_id = extracted_id;
                        Main.LogEntry(methodName, $"User ID for {Settings.Instance.twitchUsername}: {user_id}");
                        return true;
                    }
                    else
                    {
                        Main.LogEntry(methodName, "User ID is null.");
                    }
                }
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Main.LogEntry(methodName, $"Failed to get user ID for {Settings.Instance.twitchUsername}. Response: {response.ReasonPhrase}, Content: {content}");
                return false;
            }

            Main.LogEntry(methodName, $"Failed to get user ID for {Settings.Instance.twitchUsername}. Response: {response.ReasonPhrase}");
            return false;
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
                    // Main.LogEntry(methodName, $"Received message: {message}");
        
                    var jsonMessage = JsonConvert.DeserializeObject<dynamic>(message);
                    if (jsonMessage?.metadata?.message_type == "session_welcome")
                    {
                        session_id = jsonMessage.payload.session.id.ToString().Trim();
                        Main.LogEntry(methodName, $"Session ID: {session_id}");
        
                        await RegisterEventSubListeners();
                        await TwitchEventHandler.SendChatMessageHTTP("TwitchChat connected to WebSocket and listening for messages.");
                    }
                    else if (jsonMessage?.metadata?.message_type == "subscription_success")
                    {
                        isWebSocketConnected = true;
                        Main.LogEntry(methodName, "Successfully subscribed to the channel.");
                    }
                    else if (jsonMessage?.metadata?.message_type == "notification")
                    {
                        HandleNotification(jsonMessage);
                    }
                    else if (jsonMessage?.metadata?.message_type == "keepalive")
                    {
                        Main.LogEntry(methodName, "Received keepalive message.");
                    }
                }
                catch (WebSocketException ex)
                {
                    Main.LogEntry(methodName, $"WebSocket error: {ex.Message}");
                    isWebSocketConnected = false;
                    break;
                }
                catch (Exception ex)
                {
                    Main.LogEntry(methodName, $"Error receiving message: {ex.Message}");
                }
            }
        
            isWebSocketConnected = false;
            Main.LogEntry(methodName, "WebSocket connection closed.");
        }

        private static void HandleNotification(dynamic jsonMessage)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            // Additional logging, enable if needed
            // Main.LogEntry(methodName, "HandleNotification called with message: " + jsonMessage.ToString());
        
            if (jsonMessage.metadata.subscription_type == "channel.chat.message")
            {
                Main.LogEntry(methodName, "Message type is channel.chat.message");
        
                var chatter = jsonMessage.payload.@event.chatter_user_name;
                var text = jsonMessage.payload.@event.message.text;
        
                Main.LogEntry(methodName, $"Extracted chatter: {chatter}");
                Main.LogEntry(methodName, $"Extracted text: {text}");
        
                Main.LogEntry(methodName, $"Message: #{chatter}: {text}");
                Main.LogEntry("ReceivedMessage", $"{chatter}: {text}");
                Main.AttachNotification($"{chatter}: {text}", "null");

                text = jsonMessage.payload.@event.message.text.ToString();
        
                if (text.Contains("HeyGuys"))
                {
                    Main.LogEntry(methodName, "Text contains 'HeyGuys', sending 'VoHiYo'");
                    TwitchEventHandler.SendChatMessageHTTP("[HTTP] VoHiYo").Wait();
                    SendChatMessageWebSocket("[Socket] VoHiYo").Wait();
                }
                if (text.ToLower().StartsWith("!info"))
                {
                    Main.LogEntry(methodName, "Text contains '!info', sending 'This is a test message'");
                    TwitchEventHandler.SendChatMessageHTTP("[HTTP] This is an info message").Wait();
                    SendChatMessageWebSocket("[Socket] This is an info message").Wait();
                }
                if (text.ToLower().StartsWith("!commands"))
                {
                    Main.LogEntry(methodName, "Text contains '!commands', sending 'Available commands: !info !commands !test'");
                    TwitchEventHandler.SendChatMessageHTTP("[HTTP] Available commands: !info !commands !test").Wait();
                    SendChatMessageWebSocket("[Socket] Available commands: !info !commands !test").Wait();
                }
                else
                {
                    Main.LogEntry(methodName, "[HTTP] Other message, not responding");
                    SendChatMessageWebSocket("[Socket] Other message, not responding").Wait();
                }
            }
            else
            {
                Main.LogEntry(methodName, "Message type is not channel.chat.message");
            }
        }

        private static async Task RegisterEventSubListeners()
        {
            string methodName = "RegisterEventSubListeners";
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
                    broadcaster_user_id = user_id,
                    user_id = user_id
                },
                transport = new
                {
                    method = "websocket",
                    session_id = session_id
                }
            }), Encoding.UTF8, "application/json");
        
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oath_access_token);
            httpClient.DefaultRequestHeaders.Add("Client-ID", Main.GetClientId());
            var response = await httpClient.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content);
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
        
        public static async Task SendChatMessageWebSocket(string chatMessage)
        {
            string methodName = "SendChatMessageWebSocket";
            Main.LogEntry(methodName, $"Preparing to send chat message: {chatMessage}");

            using ClientWebSocket webSocket = new();
            try
            {
                await webSocket.ConnectAsync(serverUri, CancellationToken.None);
                Main.LogEntry(methodName, "WebSocket connection established.");

                var messageBytes = Encoding.UTF8.GetBytes(chatMessage);
                var segment = new ArraySegment<byte>(messageBytes);

                await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                Main.LogEntry(methodName, $"Sent chat message: {chatMessage}");

                var buffer = new byte[1024];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var responseMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Main.LogEntry(methodName, $"Received response: {responseMessage}");
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Exception occurred: {ex.Message}");
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
        private class LoggingHttpClientHandler : HttpClientHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                
                // Additional logging, enable if needed
                // Main.LogEntry("HttpClient", $"Request: {request}");
                // if (request.Content != null)
                // {
                //     Main.LogEntry("HttpClient", $"Request Content: {await request.Content.ReadAsStringAsync()}");
                // }

                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

                Main.LogEntry("HttpClient", $"Response: {response}");
                if (response.Content != null)
                {
                    Main.LogEntry("HttpClient", $"Response Content: {await response.Content.ReadAsStringAsync()}");

                }

                return response;
            }
        }
    }
}