using System.Net.Http;
using System;
using System.Text.Json;
using System.Net;
using System.Threading.Tasks;

namespace TwitchChat
{
    public class TwitchEventHandler
    {
        private static readonly HttpClient httpClient = new();
        public static string channelContent = string.Empty;

        public static async Task<string> GetUserID()
        {
            Main.ModEntry.Logger.Log("[GetUserID] Adding Authorization and Client-Id headers.");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Main.Settings.twitch_oauth_token}");
            httpClient.DefaultRequestHeaders.Add("Client-Id", Main.Settings.client_id);
        
            Main.ModEntry.Logger.Log("[GetUserID] Sending GET request to https://api.twitch.tv/helix/users.");
            var response = await httpClient.GetAsync($"https://api.twitch.tv/helix/users?login={Main.Settings.twitchUsername}");
            Main.ModEntry.Logger.Log($"[GetUserID] Response status code: {response.StatusCode}");
            
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Main.ModEntry.Logger.Log($"[GetUserID] Response error content: {errorContent}");
            }
        
            response.EnsureSuccessStatusCode();
        
            var content = await response.Content.ReadAsStringAsync();
            Main.ModEntry.Logger.Log($"[GetUserID] Response content: {content}");
            var jsonDocument = JsonDocument.Parse(content);
            var id = jsonDocument.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
        
            if (id == null)
            {
                Main.ModEntry.Logger.Log("[GetUserID] User ID is null.");
                throw new InvalidOperationException("User ID is null");
            }
        
            Main.Settings.userID = id;
            Main.ModEntry.Logger.Log($"[GetUserID] User ID: {Main.Settings.userID}");
        
            return Main.Settings.userID;
        }

        
        public static async Task ConnectionStatus()
        {
            try
            {
                Main.ModEntry.Logger.Log("[ConnectionStatus] Adding Authorization and Client-Id headers.");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Main.Settings.twitch_oauth_token}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", Main.Settings.client_id);

                Main.ModEntry.Logger.Log("[ConnectionStatus] Sending GET request to https://api.twitch.tv/helix/users.");
                var userResponse = await httpClient.GetAsync($"https://api.twitch.tv/helix/users?login={Main.Settings.twitchUsername}");
                Main.ModEntry.Logger.Log($"[ConnectionStatus] User response status code: {userResponse.StatusCode}");

                if (userResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Main.ModEntry.Logger.Log("[ConnectionStatus] Unauthorized. Token might be expired.");
                    await Main.ConnectToTwitch();
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
                    await Main.ConnectToTwitch();
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

        public static async Task JoinChannel()
        {
            if (string.IsNullOrEmpty(Main.Settings.userID))
            {
                Main.ModEntry.Logger.Log("[JoinChannel] User ID is not set. Fetching User ID...");
                await GetUserID();
            }
            try
            {
                Main.ModEntry.Logger.Log("[JoinChannel] Preparing request body.");
                var requestBody = new
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
                        method = "webhook",
                        callback = Main.Settings.callbackUrl,
                        secret = Main.Settings.client_secret
                    }
                };

                var jsonRequestBody = JsonSerializer.Serialize(requestBody);
                Main.ModEntry.Logger.Log($"[JoinChannel] Request body prepared: {jsonRequestBody}");

                var content = new StringContent(jsonRequestBody, System.Text.Encoding.UTF8, "application/json");
                Main.ModEntry.Logger.Log("[JoinChannel] Headers set: Client-Id and Authorization");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Main.Settings.twitch_oauth_token}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", Main.Settings.client_id);

                Main.ModEntry.Logger.Log("[JoinChannel] Sending POST request to https://api.twitch.tv/helix/eventsub/subscriptions");
                var response = await httpClient.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content);
                Main.ModEntry.Logger.Log($"[JoinChannel] Response status code: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Main.ModEntry.Logger.Log($"[JoinChannel] Response content: {responseContent}");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    Main.ModEntry.Logger.Log("[JoinChannel] Forbidden. Check if the token has the required scopes.");
                }

                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException httpEx)
            {
                Main.ModEntry.Logger.Log($"[JoinChannel] HTTP request error: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Main.ModEntry.Logger.Log($"[JoinChannel] General error: {ex.Message}");
            }
        }

        public static async Task SendMessage(string message)
        {
            if (string.IsNullOrEmpty(Main.Settings.userID))
            {
                Main.ModEntry.Logger.Log("[SendMessage] User ID is not set. Fetching User ID...");
                await GetUserID();
            }
            try
            {
                Main.ModEntry.Logger.Log("[SendMessage] Preparing request body.");
                var requestBody = new
                {
                    broadcaster_id = Main.Settings.userID,
                    sender_id = Main.Settings.userID,
                    message
                };

                var jsonRequestBody = JsonSerializer.Serialize(requestBody);
                Main.ModEntry.Logger.Log($"[SendMessage] Request body prepared: {jsonRequestBody}");

                var content = new StringContent(jsonRequestBody, System.Text.Encoding.UTF8, "application/json");

                Main.ModEntry.Logger.Log("[SendMessage] Headers set: Client-Id and Authorization");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Main.Settings.twitch_oauth_token}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", Main.Settings.client_id);

                Main.ModEntry.Logger.Log("[SendMessage] Sending POST request to https://api.twitch.tv/helix/chat/messages");
                var response = await httpClient.PostAsync("https://api.twitch.tv/helix/chat/messages", content);
                Main.ModEntry.Logger.Log($"[SendMessage] Response status code: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Main.ModEntry.Logger.Log($"[SendMessage] Response content: {responseContent}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Main.ModEntry.Logger.Log("[SendMessage] Unauthorized. Token might be expired.");
                    await Main.ConnectToTwitch();
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