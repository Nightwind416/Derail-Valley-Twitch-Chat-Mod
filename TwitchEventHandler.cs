using System.Net.Http;
using System;
using System.Text.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TwitchChat
{
    public class TwitchEventHandler
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string eventSubUrl = "https://api.twitch.tv/helix/eventsub/subscriptions";
        private static readonly string callbackUrl = "https://www.twitch.tv/nightwind416/";
        // private static readonly string callbackUrl = "endpoint";
        public static string userID = string.Empty;

        public static async Task<string> GetUserID()
        {
            Console.WriteLine("[GetUserID] Adding Authorization and Client-Id headers.");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Main.twitch_oauth_token}");
            httpClient.DefaultRequestHeaders.Add("Client-Id", Settings.client_id);
        
            // var temp_username = "Nightwind416"; // Replace with the actual username if needed
        
            Console.WriteLine("[GetUserID] Sending GET request to https://api.twitch.tv/helix/users.");
            var response = await httpClient.GetAsync($"https://api.twitch.tv/helix/users?login={Settings.twitchUsername}");
            Console.WriteLine($"[GetUserID] Response status code: {response.StatusCode}");
            
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GetUserID] Response error content: {errorContent}");
            }
        
            response.EnsureSuccessStatusCode();
        
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[GetUserID] Response content: {content}");
            var jsonDocument = JsonDocument.Parse(content);
            var id = jsonDocument.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
        
            if (id == null)
            {
                Console.WriteLine("[GetUserID] User ID is null.");
                throw new InvalidOperationException("User ID is null");
            }
        
            userID = id;
            Console.WriteLine($"[GetUserID] User ID: {userID}");
        
            return userID;
        }

        public static async Task JoinChannel()
        {
            Console.WriteLine("[SubscribeToOwnChannelMessages] Preparing request body.");
            var requestBody = new
            {
                type = "channel.chat.message",
                version = "1",
                condition = new
                {
                    broadcaster_user_id = userID,
                    user_id = userID
                },
                transport = new
                {
                    method = "webhook",
                    callback = callbackUrl,
                    secret = Settings.client_secret
                }
            };

            var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            Console.WriteLine("[SubscribeToOwnChannelMessages] Request body prepared: " + JsonSerializer.Serialize(requestBody));

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Client-Id", Settings.client_id);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Main.twitch_oauth_token}");
            Console.WriteLine("[SubscribeToOwnChannelMessages] Headers set: Client-Id and Authorization");

            Console.WriteLine("[SubscribeToOwnChannelMessages] Sending POST request to " + eventSubUrl);
            var response = await httpClient.PostAsync(eventSubUrl, requestContent);
            Console.WriteLine($"[SubscribeToOwnChannelMessages] Response status code: {response.StatusCode}");

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[SubscribeToOwnChannelMessages] Response content: {responseContent}");

            response.EnsureSuccessStatusCode();
            Console.WriteLine("[SubscribeToOwnChannelMessages] Subscription successful.");
        }

        public static async Task<string> SendMessage(string message, string? replyParentMessageId = null)
        {
            var requestBody = new
            {
                broadcaster_id = userID,
                sender_id = userID,
                message = message,
                reply_parent_message_id = replyParentMessageId
            };
        
            var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        
            // Validate client_id and oauth_token
            if (string.IsNullOrEmpty(Settings.client_id) || string.IsNullOrEmpty(Main.twitch_oauth_token))
            {
                throw new InvalidOperationException("Client ID or OAuth token is null or empty.");
            }
        
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Client-Id", Settings.client_id);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Main.twitch_oauth_token}");
        
            var response = await httpClient.PostAsync("https://api.twitch.tv/helix/chat/messages", requestContent);
            response.EnsureSuccessStatusCode();
        
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseContent);
            var messageId = jsonResponse.RootElement.GetProperty("data")[0].GetProperty("message_id").GetString() ?? throw new InvalidOperationException("Message ID is null");
            var isSent = jsonResponse.RootElement.GetProperty("data")[0].GetProperty("is_sent").GetBoolean();
        
            if (!isSent)
            {
                var dropReason = jsonResponse.RootElement.GetProperty("data")[0].GetProperty("drop_reason").GetProperty("message").GetString();
                throw new InvalidOperationException($"Message was not sent: {dropReason}");
            }
        
            return messageId;
        }
        public static string channelContent = string.Empty;
        public static async Task ConnectionStatus()
        {
            try
            {
                Console.WriteLine("[GetConnectionStatusAndChannels] Adding Authorization and Client-Id headers.");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Main.twitch_oauth_token}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", Settings.client_id);
        
                // var temp_username = "Nightwind416";
        
                Console.WriteLine("[GetConnectionStatusAndChannels] Sending GET request to https://api.twitch.tv/helix/users.");
                var userResponse = await httpClient.GetAsync($"https://api.twitch.tv/helix/users?login={Settings.twitchUsername}");
                Console.WriteLine($"[GetConnectionStatusAndChannels] User response status code: {userResponse.StatusCode}");
        
                if (userResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("[GetConnectionStatusAndChannels] Unauthorized. Token might be expired.");
                    // Handle token expiry, e.g., refresh the token
                    await Main.ConnectToTwitch();
                    return;
                }
        
                if (userResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorContent = await userResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GetConnectionStatusAndChannels] User response error content: {errorContent}");
                }
        
                userResponse.EnsureSuccessStatusCode();
        
                var userContent = await userResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[GetConnectionStatusAndChannels] User response content: {userContent}");
        
                var jsonResponse = JsonDocument.Parse(userContent);
                var broadcasterId = jsonResponse.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
        
                Console.WriteLine("[GetConnectionStatusAndChannels] Sending GET request to https://api.twitch.tv/helix/channels.");
                var channelResponse = await httpClient.GetAsync($"https://api.twitch.tv/helix/channels?broadcaster_id={broadcasterId}");
                Console.WriteLine($"[GetConnectionStatusAndChannels] Channel response status code: {channelResponse.StatusCode}");
        
                if (channelResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("[GetConnectionStatusAndChannels] Unauthorized. Token might be expired.");
                    // Handle token expiry, e.g., refresh the token
                    await Main.ConnectToTwitch();
                    return;
                }
        
                if (channelResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorContent = await channelResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GetConnectionStatusAndChannels] Channel response error content: {errorContent}");
                }
        
                channelResponse.EnsureSuccessStatusCode();
        
                channelContent = await channelResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[GetConnectionStatusAndChannels] Channel response content: {channelContent}");
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"[GetConnectionStatusAndChannels] HTTP request error: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetConnectionStatusAndChannels] General error: {ex.Message}");
            }
        }
    }
}