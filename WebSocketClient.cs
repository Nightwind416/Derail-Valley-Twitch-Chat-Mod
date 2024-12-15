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
        private static ClientWebSocket _webSocket = new();
        private static readonly Uri _serverUri = new("wss://eventsub.wss.twitch.tv/ws");
        public static string _sessionId = string.Empty;
        public static string client_id = "qjklmbrascxsqow5gsvl6la72txnes";
        private static string refresh_token = string.Empty;
        private static readonly HttpClient httpClient = new(new LoggingHttpClientHandler())
        {
            Timeout = TimeSpan.FromSeconds(60) // Set the timeout to 60 seconds
        };
        public static async Task ConnectToWebSocket()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                await ValidateAuthToken();

                if (_webSocket != null)
                {
                    _webSocket.Dispose();
                }
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(_serverUri, CancellationToken.None);
                Main.LogEntry(methodName, "Connected to WebSocket server.");

                // Start receiving messages
                _ = Task.Run(ReceiveMessages);

            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Connection error: {ex.Message}");
            }
        }

        private static async Task ValidateAuthToken()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Settings.Instance.twitch_oauth_token);
            Main.LogEntry(methodName, $"Validating token: {Settings.Instance.twitch_oauth_token}");

            // Log the headers
            foreach (var header in httpClient.DefaultRequestHeaders)
            {
                Main.LogEntry(methodName, $"Header: {header.Key} = {string.Join(", ", header.Value)}");
            }
        
            int retryCount = 3;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    Main.LogEntry(methodName, "Sending request to Twitch API...");
                    var response = await httpClient.GetAsync("https://id.twitch.tv/oauth2/validate");
                    stopwatch.Stop();
                    Main.LogEntry(methodName, $"Response status code: {response.StatusCode}, Time taken: {stopwatch.ElapsedMilliseconds} ms");

                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        Main.LogEntry(methodName, "Token is not valid. Refreshing token...");
                        await RefreshAuthToken();
                    }
                    else
                    {
                        Main.LogEntry(methodName, "Validated token.");
                    }

                    // Fetch your user ID
                    if (await GetUserIdAsync())
                    {
                        Main.LogEntry(methodName, "User ID automatically fetched successfully.");
                    }
                    else
                    {
                        Main.LogEntry(methodName, "Failed to automatically fetch user ID.");
                    }
                    break; // Exit the retry loop if successful
                }
                catch (HttpRequestException ex)
                {
                    Main.LogEntry(methodName, $"HTTP request error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Main.LogEntry(methodName, $"Inner exception: {ex.InnerException.Message}");
                    }
                }
                catch (TaskCanceledException ex)
                {
                    Main.LogEntry(methodName, $"Request timed out: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Main.LogEntry(methodName, $"Inner exception: {ex.InnerException.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Main.LogEntry(methodName, $"Unexpected error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Main.LogEntry(methodName, $"Inner exception: {ex.InnerException.Message}");
                    }
                    Main.LogEntry(methodName, $"Stack trace: {ex.StackTrace}");
                }

                if (i < retryCount - 1)
                {
                    Main.LogEntry(methodName, "Retrying...");
                    await Task.Delay(4000); // Wait for 4 seconds before retrying
                }
                else
                {
                    Main.LogEntry(methodName, "Max retry attempts reached. Giving up.");
                }
            }
        }
        private static async Task RefreshAuthToken()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token");
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", client_id),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refresh_token)
            });
            request.Content = content;

            var response = await httpClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseContent);
                Settings.Instance.twitch_oauth_token = json["access_token"]?.ToString() ?? throw new Exception("Access token is null");
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
            string methodName = MethodBase.GetCurrentMethod().Name;

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.twitch.tv/helix/users?login={Settings.Instance.twitchUsername}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Settings.Instance.twitch_oauth_token);
            request.Headers.Add("Client-Id", client_id);

            var response = await httpClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                var user_id = json["data"]?[0]?["id"]?.ToString();
                if (!string.IsNullOrEmpty(user_id))
                {
                    if (user_id != null)
                    {
                        Settings.Instance.userID = user_id;
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
            string methodName = MethodBase.GetCurrentMethod().Name;
            var buffer = new byte[1024 * 4];
            while (_webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    // Main.LogEntry(methodName, $"Received message: {message}");
        
                    var jsonMessage = JsonConvert.DeserializeObject<dynamic>(message);
                    if (jsonMessage?.metadata?.message_type == "session_welcome")
                    {
                        _sessionId = jsonMessage.payload.session.id.ToString().Trim();
                        Main.LogEntry(methodName, $"Session ID: {_sessionId}");
        
                        await RegisterEventSubListeners();
                    }
                    else if (jsonMessage?.metadata?.message_type == "subscription_success")
                    {
                        Main.LogEntry(methodName, "Successfully subscribed to the channel.");
                    }
                    else if (jsonMessage?.metadata?.message_type == "notification")
                    {
                        HandleNotification(jsonMessage);
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
        
            Main.LogEntry(methodName, "WebSocket connection closed.");
        }

        private static void HandleNotification(dynamic jsonMessage)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, "HandleNotification called with message: " + jsonMessage.ToString());
        
            if (jsonMessage.metadata.subscription_type == "channel.chat.message")
            {
                Main.LogEntry(methodName, "Message type is channel.chat.message");
        
                var chatter = jsonMessage.payload.@event.chatter_user_name;
                var text = jsonMessage.payload.@event.message.text;
        
                Main.LogEntry(methodName, $"Extracted chatter: {chatter}");
                Main.LogEntry(methodName, $"Extracted text: {text}");
        
                Main.LogEntry(methodName, $"Message: #{chatter}: {text}");
                Main.LogEntry("ReceivedMessage", $"{chatter}: {text}");

                text = jsonMessage.payload.@event.message.text.ToString();
        
                if (text.Contains("HeyGuys"))
                {
                    Main.LogEntry(methodName, "Text contains 'HeyGuys', sending 'VoHiYo'");
                    SendChatMessage("VoHiYo").Wait();
                }
                if (text.ToLower().StartsWith("!info"))
                {
                    Main.LogEntry(methodName, "Text contains '!info', sending 'This is a test message'");
                    SendChatMessage("This is an info message").Wait();
                }
                if (text.ToLower().StartsWith("!commands"))
                {
                    Main.LogEntry(methodName, "Text contains '!commands', sending 'Available commands: !info !commands !test'");
                    SendChatMessage("Available commands: !info !commands !test").Wait();
                }
                else
                {
                    Main.LogEntry(methodName, "Other message, not responding");
                }
            }
            else
            {
                Main.LogEntry(methodName, "Message type is not channel.chat.message");
            }
        }

        public static async Task SendChatMessage(string chatMessage)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, $"Preparing to send chat message: {chatMessage}");
        
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                broadcaster_id = Settings.Instance.userID,
                sender_id = Settings.Instance.userID,
                message = chatMessage
            }), Encoding.UTF8, "application/json");
        
            Main.LogEntry(methodName, $"Created content: {content}");
        
            try
            {
                var response = await httpClient.PostAsync("https://api.twitch.tv/helix/chat/messages", content);
                Main.LogEntry(methodName, $"Received response: {response.StatusCode}");
        
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Main.LogEntry(methodName, "Failed to send chat message.");
                }
                else
                {
                    Main.LogEntry(methodName, $"Sent chat message: {chatMessage}");
                    Main.LogEntry("SentMessage", $"Sent Message: {chatMessage}");
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Exception occurred: {ex.Message}");
            }
        }

        private static async Task RegisterEventSubListeners()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            if (_webSocket.State != WebSocketState.Open)
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
                    broadcaster_user_id = Settings.Instance.userID,
                    user_id = Settings.Instance.userID
                },
                transport = new
                {
                    method = "websocket",
                    session_id = _sessionId
                }
            }), Encoding.UTF8, "application/json");
        
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Settings.Instance.twitch_oauth_token);
            httpClient.DefaultRequestHeaders.Add("Client-ID", client_id);
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

        public static async Task DisconnectAsync()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Main.LogEntry(methodName, "Disconnected from WebSocket server.");
            }
        }
        private class LoggingHttpClientHandler : HttpClientHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Main.LogEntry("HttpClient", $"Request: {request}");
                if (request.Content != null)
                {
                    Main.LogEntry("HttpClient", $"Request Content: {await request.Content.ReadAsStringAsync()}");
                }

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