using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TwitchChat
{
    /// <summary>
    /// Handles Twitch API events and interactions.
    /// </summary>
    public class TwitchEventHandler
    {
        public static readonly HttpClient httpClient = new();
        public static string user_id = string.Empty;

        /// <summary>
        /// Retrieves the user ID for the configured Twitch username.
        /// </summary>
        public static async Task GetUserID()
        {
            string methodName = "GetUserID";
            
            byte[] tokenBytes = Convert.FromBase64String(Settings.Instance.EncodedOAuthToken);
            _ = Encoding.UTF8.GetString(tokenBytes);
            string access_token = Encoding.UTF8.GetString(tokenBytes);
            
            Main.LogEntry(methodName, "Adding Authorization and Client-Id headers.");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {access_token}");
            httpClient.DefaultRequestHeaders.Add("Client-Id", GetClientId());
        
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
            var lookup_id = jsonDocument.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
        
            if (lookup_id == null)
            {
                Main.LogEntry(methodName, "User ID is null.");
            }
        
            if (lookup_id != null)
            {
                user_id = lookup_id;
            }
            else
            {
                Main.LogEntry(methodName, "Failed to retrieve user ID.");
            }
            Main.LogEntry(methodName, $"User ID: {user_id}");
        }

        /// <summary>
        /// Checks the connection status with Twitch API.
        /// </summary>
        public static async Task ConnectionStatus()
        {
            string methodName = "ConnectionStatus";
            try
            {
                byte[] tokenBytes = Convert.FromBase64String(Settings.Instance.EncodedOAuthToken);
                _ = Encoding.UTF8.GetString(tokenBytes);
                string access_token = Encoding.UTF8.GetString(tokenBytes);
                
                Main.LogEntry(methodName, "Adding Authorization and Client-Id headers.");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {access_token}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", GetClientId());

                Main.LogEntry(methodName, "Sending GET request to https://api.twitch.tv/helix/users.");
                var userResponse = await httpClient.GetAsync($"https://api.twitch.tv/helix/users?login={Settings.Instance.twitchUsername}");
                Main.LogEntry(methodName, $"User response status code: {userResponse.StatusCode}");

                if (userResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Main.LogEntry(methodName, "Unauthorized. Token might be expired.");
                    return;
                }

                if (userResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorContent = await userResponse.Content.ReadAsStringAsync();
                    Main.LogEntry(methodName, $"User response error content: {errorContent}");
                }

                userResponse.EnsureSuccessStatusCode();

                var userContent = await userResponse.Content.ReadAsStringAsync();
                Main.LogEntry(methodName, $"User response content: {userContent}");

                Main.LogEntry(methodName, "Sending GET request to https://api.twitch.tv/helix/channels.");
                var channelResponse = await httpClient.GetAsync($"https://api.twitch.tv/helix/channels?broadcaster_id={user_id}");
                Main.LogEntry(methodName, $"Channel response status code: {channelResponse.StatusCode}");

                if (channelResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Main.LogEntry(methodName, "Unauthorized. Token might be expired.");
                    return;
                }

                if (channelResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorContent = await channelResponse.Content.ReadAsStringAsync();
                    Main.LogEntry(methodName, $"Channel response error content: {errorContent}");
                }

                channelResponse.EnsureSuccessStatusCode();

                var channelContent = await channelResponse.Content.ReadAsStringAsync();
                Main.LogEntry(methodName, $"Channel response content: {channelContent}");
            }
            catch (HttpRequestException httpEx)
            {
                Main.LogEntry(methodName, $"HTTP request error: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"General error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a chat message to the Twitch channel.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public static async Task SendMessage(string message)
        {
            string methodName = "SendChatMessageHTTP";
            Main.LogEntry(methodName, $"Preparing to send chat message: {message}");
        
            var messageData = new
            {
                broadcaster_id = user_id,
                sender_id = user_id,
                message = message
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(messageData),
                Encoding.UTF8, 
                "application/json"
            );
        
            Main.LogEntry(methodName, $"Created content: {content}");
        
            try
            {
                var response = await httpClient.PostAsync("https://api.twitch.tv/helix/chat/messages", content);
                Main.LogEntry(methodName, $"Received response: {response.StatusCode}");
        
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Main.LogEntry(methodName, "Failed to send chat message.");
                }
                else
                {
                    Main.LogEntry(methodName, $"Sent chat message: {message}");
                    Main.LogEntry("SentMessage", $"Sent Message: {message}");
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Exception occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the decoded client ID for Twitch API authentication.
        /// </summary>
        /// <returns>The decoded client ID string.</returns>
        public static string GetClientId()
        {
            string encodedClientId = "cWprbG1icmFzY3hzcW93NWdzdmw2bGE3MnR4bmVz";
            byte[] data = Convert.FromBase64String(encodedClientId);
            return Encoding.UTF8.GetString(data);
        }
    }
}
