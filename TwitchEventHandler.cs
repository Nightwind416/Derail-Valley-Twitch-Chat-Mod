using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TwitchChat
{
    /// <summary>
    /// Manages Twitch API interactions and event handling.
    /// </summary>
    /// <remarks>
    /// This class handles:
    /// - User authentication and validation
    /// - Chat message sending and receiving
    /// - Channel announcements and whispers
    /// - API endpoint communication
    /// - Event subscription management
    /// </remarks>
    public class TwitchEventHandler
    {
        /// <summary>Shared HttpClient instance for API requests</summary>
        public static readonly HttpClient httpClient = new();

        /// <summary>Current user's Twitch ID</summary>
        public static string user_id = string.Empty;

        /// <summary>
        /// Retrieves the user ID for the configured Twitch username.
        /// </summary>
        public static async Task GetUserID()
        {
            string methodName = "GetUserID";

            string access_token = OAuthTokenManager.GetAccessToken();

            Main.LogEntry(methodName, "Adding Authorization and Client-Id headers.");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {access_token}");
            httpClient.DefaultRequestHeaders.Add("Client-Id", GetClientId());

            Main.LogEntry(methodName, "Sending GET request to https://api.twitch.tv/helix/users.");
            var response = await httpClient.GetAsync($"https://api.twitch.tv/helix/users?login={Uri.EscapeDataString(Settings.Instance.twitchUsername)}");
            Main.LogEntry(methodName, $"Response status code: {response.StatusCode}");

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Main.LogEntry(methodName, $"Response error content: {errorContent}");
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Main.LogEntry(methodName, $"Response content: {content}");

            string? lookup_id = (string?)JObject.Parse(content).SelectToken("data[0].id");

            if (!string.IsNullOrEmpty(lookup_id))
            {
                user_id = lookup_id!;
            }
            else
            {
                Main.LogEntry(methodName, "Failed to retrieve user ID (no user returned for that login name).");
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
                string access_token = OAuthTokenManager.GetAccessToken();

                Main.LogEntry(methodName, "Adding Authorization and Client-Id headers.");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {access_token}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", GetClientId());

                Main.LogEntry(methodName, "Sending GET request to https://api.twitch.tv/helix/users.");
                var userResponse = await httpClient.GetAsync($"https://api.twitch.tv/helix/users?login={Uri.EscapeDataString(Settings.Instance.twitchUsername)}");
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
            string methodName = "SendMessage";
            Main.LogEntry(methodName, $"Preparing to send chat message: {message}");

            string jsonMessage = JsonConvert.SerializeObject(new
            {
                broadcaster_id = user_id,
                sender_id = user_id,
                message
            });
            var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("https://api.twitch.tv/helix/chat/messages", content);
                Main.LogEntry(methodName, $"Received response: {response.StatusCode}");

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Main.LogEntry(methodName, $"Failed to send chat message. Error: {errorContent}");
                    return;
                }

                // Twitch answers 200 even when the message was dropped (for example by AutoMod), so check the body.
                var responseContent = await response.Content.ReadAsStringAsync();
                JToken? result = JObject.Parse(responseContent).SelectToken("data[0]");
                bool isSent = result?["is_sent"]?.Value<bool>() ?? true;
                if (!isSent)
                {
                    string dropReason = (string?)result?.SelectToken("drop_reason.message") ?? "unknown";
                    Main.LogEntry(methodName, $"Chat message was dropped by Twitch: {dropReason}");
                    return;
                }

                Main.LogEntry(methodName, $"Sent chat message: {message}");
                Main.LogEntry("SentMessage", $"Sent Message: {message}");
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Exception occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a whisper message to a specific Twitch user.
        /// </summary>
        /// <param name="toUserId">The user ID of the recipient.</param>
        /// <param name="message">The message to send.</param>
        public static async Task SendWhisper(string toUserId, string message)
        {
            string methodName = "SendWhisper";
            Main.LogEntry(methodName, $"Preparing to send whisper to user {toUserId}: {message}");

            string jsonMessage = JsonConvert.SerializeObject(new
            {
                from_user_id = user_id,
                to_user_id = toUserId,
                message
            });
            var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("https://api.twitch.tv/helix/whispers", content);
                Main.LogEntry(methodName, $"Received response: {response.StatusCode}");

                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Main.LogEntry(methodName, $"Failed to send whisper. Error: {errorContent}");
                }
                else
                {
                    Main.LogEntry(methodName, $"Sent whisper to {toUserId}: {message}");
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Exception occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends an announcement to the channel.
        /// </summary>
        /// <param name="message">The announcement message.</param>
        /// <param name="color">Optional color for the announcement (blue, green, orange, purple).</param>
        public static async Task SendAnnouncement(string message, string color = "primary")
        {
            string methodName = "SendAnnouncement";
            Main.LogEntry(methodName, $"Preparing to send announcement: {message}");

            // Validate color parameter
            color = color.ToLower();
            string[] validColors = ["blue", "green", "orange", "purple", "primary"]; // primary is the channel's accent color
            if (!Array.Exists(validColors, c => c.Equals(color, StringComparison.OrdinalIgnoreCase)))
            {
                color = "primary";
            }

            string jsonMessage = JsonConvert.SerializeObject(new
            {
                broadcaster_id = user_id,
                moderator_id = user_id,
                message,
                color
            });
            var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("https://api.twitch.tv/helix/chat/announcements", content);
                Main.LogEntry(methodName, $"Received response: {response.StatusCode}");

                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Main.LogEntry(methodName, $"Failed to send announcement. Error: {errorContent}");
                }
                else
                {
                    Main.LogEntry(methodName, $"Sent announcement: {message}");
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
