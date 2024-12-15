using System.Net.Http;
using System;
using System.Text.Json;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;

namespace TwitchChat
{
    public class TwitchEventHandler
    {
        private static readonly HttpClient httpClient = new();

        public static async Task<string> GetUserID()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, "Adding Authorization and Client-Id headers.");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Settings.Instance.twitch_oauth_token}");
            _ = new WebSocketClient();
            httpClient.DefaultRequestHeaders.Add("Client-Id", WebSocketClient.client_id);
        
            Main.LogEntry(methodName, "Sending GET request to https://api.twitch.tv/helix/users.");
            var response = await httpClient.GetAsync($"https://api.twitch.tv/helix/users?login={Settings.Instance.twitchUsername}");
            Main.LogEntry(methodName, $"Response status code: {response.StatusCode}");
            
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Main.LogEntry(methodName, $"Response error content: {errorContent}");
            }
        
            response.EnsureSuccessStatusCode();
        
            var content = await response.Content.ReadAsStringAsync();
            Main.LogEntry(methodName, $"Response content: {content}");
            var jsonDocument = JsonDocument.Parse(content);
            var id = jsonDocument.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
        
            if (id == null)
            {
                Main.LogEntry(methodName, "User ID is null.");
                throw new InvalidOperationException("User ID is null");
            }
        
            Settings.Instance.userID = id;
            Main.ModEntry.Logger.Log($"[GetUserID] User ID: {Settings.Instance.userID}");
        
            return Settings.Instance.userID;
        }

        
        public static async Task ConnectionStatus()
        {
            try
            {
                Main.ModEntry.Logger.Log("[ConnectionStatus] Adding Authorization and Client-Id headers.");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Settings.Instance.twitch_oauth_token}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", WebSocketClient.client_id);

                Main.ModEntry.Logger.Log("[ConnectionStatus] Sending GET request to https://api.twitch.tv/helix/users.");
                var userResponse = await httpClient.GetAsync($"https://api.twitch.tv/helix/users?login={Settings.Instance.twitchUsername}");
                Main.ModEntry.Logger.Log($"[ConnectionStatus] User response status code: {userResponse.StatusCode}");

                if (userResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Main.ModEntry.Logger.Log("[ConnectionStatus] Unauthorized. Token might be expired.");
                    return;
                }

                if (userResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorContent = await userResponse.Content.ReadAsStringAsync();
                    Main.ModEntry.Logger.Log($"[ConnectionStatus] User response error content: {errorContent}");
                }

                userResponse.EnsureSuccessStatusCode();

                var userContent = await userResponse.Content.ReadAsStringAsync();
                Main.ModEntry.Logger.Log($"[ConnectionStatus] User response content: {userContent}");

                var jsonResponse = JsonDocument.Parse(userContent);
                var broadcasterId = jsonResponse.RootElement.GetProperty("data")[0].GetProperty("id").GetString();

                Main.ModEntry.Logger.Log("[ConnectionStatus] Sending GET request to https://api.twitch.tv/helix/channels.");
                var channelResponse = await httpClient.GetAsync($"https://api.twitch.tv/helix/channels?broadcaster_id={broadcasterId}");
                Main.ModEntry.Logger.Log($"[ConnectionStatus] Channel response status code: {channelResponse.StatusCode}");

                if (channelResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Main.ModEntry.Logger.Log("[ConnectionStatus] Unauthorized. Token might be expired.");
                    return;
                }

                if (channelResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorContent = await channelResponse.Content.ReadAsStringAsync();
                    Main.ModEntry.Logger.Log($"[ConnectionStatus] Channel response error content: {errorContent}");
                }

                channelResponse.EnsureSuccessStatusCode();

                var channelContent = await channelResponse.Content.ReadAsStringAsync();
                Main.ModEntry.Logger.Log($"[ConnectionStatus] Channel response content: {channelContent}");
            }
            catch (HttpRequestException httpEx)
            {
                Main.ModEntry.Logger.Log($"[ConnectionStatus] HTTP request error: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Main.ModEntry.Logger.Log($"[ConnectionStatus] General error: {ex.Message}");
            }
        }

        // public static async Task JoinChannel()
        // {
        //     if (string.IsNullOrEmpty(WebSocketClient.userID))
        //     {
        //         Main.ModEntry.Logger.Log("[JoinChannel] User ID is not set. Fetching User ID...");
        //         await GetUserID();
        //     }
        //     try
        //     {
        //         Main.ModEntry.Logger.Log("[JoinChannel] Preparing request body.");
        //         var requestBody = new
        //         {
        //             type = "channel.chat.message",
        //             version = "1",
        //             condition = new
        //             {
        //                 broadcaster_user_id = WebSocketClient.userID,
        //                 user_id = WebSocketClient.userID
        //             },
        //             transport = new
        //             {
        //                 method = "webhook",
        //                 callback = WebSocketClient.callbackUrl,
        //                 secret = WebSocketClient.client_secret
        //             }
        //         };

        //         var jsonRequestBody = JsonSerializer.Serialize(requestBody);
        //         Main.ModEntry.Logger.Log($"[JoinChannel] Request body prepared: {jsonRequestBody}");

        //         var content = new StringContent(jsonRequestBody, System.Text.Encoding.UTF8, "application/json");
        //         Main.ModEntry.Logger.Log("[JoinChannel] Headers set: Client-Id and Authorization");
        //         httpClient.DefaultRequestHeaders.Clear();
        //         httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {WebSocketClient.twitch_oauth_token}");
        //         httpClient.DefaultRequestHeaders.Add("Client-Id", WebSocketClient.client_id);

        //         Main.ModEntry.Logger.Log("[JoinChannel] Sending POST request to https://api.twitch.tv/helix/eventsub/subscriptions");
        //         var response = await httpClient.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content);
        //         Main.ModEntry.Logger.Log($"[JoinChannel] Response status code: {response.StatusCode}");

        //         var responseContent = await response.Content.ReadAsStringAsync();
        //         Main.ModEntry.Logger.Log($"[JoinChannel] Response content: {responseContent}");

        //         if (response.StatusCode == HttpStatusCode.Forbidden)
        //         {
        //             Main.ModEntry.Logger.Log("[JoinChannel] Forbidden. Check if the token has the required scopes.");
        //         }

        //         response.EnsureSuccessStatusCode();
        //     }
        //     catch (HttpRequestException httpEx)
        //     {
        //         Main.ModEntry.Logger.Log($"[JoinChannel] HTTP request error: {httpEx.Message}");
        //     }
        //     catch (Exception ex)
        //     {
        //         Main.ModEntry.Logger.Log($"[JoinChannel] General error: {ex.Message}");
        //     }
        // }

        public static async Task SendMessage(string message)
        {
            if (string.IsNullOrEmpty(Settings.Instance.userID))
            {
                Main.ModEntry.Logger.Log("[SendMessage] User ID is not set. Fetching User ID...");
                await GetUserID();
            }
            try
            {
                Main.ModEntry.Logger.Log("[SendMessage] Preparing request body.");
                var requestBody = new
                {
                    broadcaster_id = Settings.Instance.userID,
                    sender_id = Settings.Instance.userID,
                    message
                };

                var jsonRequestBody = JsonSerializer.Serialize(requestBody);
                Main.ModEntry.Logger.Log($"[SendMessage] Request body prepared: {jsonRequestBody}");

                var content = new StringContent(jsonRequestBody, System.Text.Encoding.UTF8, "application/json");

                Main.ModEntry.Logger.Log("[SendMessage] Headers set: Client-Id and Authorization");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Settings.Instance.twitch_oauth_token}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", WebSocketClient.client_id);

                Main.ModEntry.Logger.Log("[SendMessage] Sending POST request to https://api.twitch.tv/helix/chat/messages");
                var response = await httpClient.PostAsync("https://api.twitch.tv/helix/chat/messages", content);
                Main.ModEntry.Logger.Log($"[SendMessage] Response status code: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Main.ModEntry.Logger.Log($"[SendMessage] Response content: {responseContent}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Main.ModEntry.Logger.Log("[SendMessage] Unauthorized. Token might be expired.");
                    return;
                }

                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException httpEx)
            {
                Main.ModEntry.Logger.Log($"[SendMessage] HTTP request error: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Main.ModEntry.Logger.Log($"[SendMessage] General error: {ex.Message}");
            }
        }
    }
}