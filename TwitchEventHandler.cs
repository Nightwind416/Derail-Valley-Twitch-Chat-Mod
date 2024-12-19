using System.Net.Http;
using System;
using System.Text.Json;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace TwitchChat
{
    public class TwitchEventHandler
    {
        public static readonly HttpClient httpClient = new();
        public static readonly string redirectUri = "http://localhost/";
        public static string oath_access_token = string.Empty;
        public static string oath_token_expiration = string.Empty;
        public static string user_id = string.Empty;
        public static async Task GetOathToken()
        {
            string methodName = "GetOathToken";
            Main.LogEntry(methodName, "Sending oath Token request to Twitch...");
        
            try
            {
                // if (!string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken))
                // {
                //     await ValidateAuthToken();
                // }
                
                string clientId = Main.GetClientId();
                string scope = "chat:edit chat:read channel:bot channel:manage:broadcast channel:moderate channel:read:subscriptions user:read:chat user:read:subscriptions user:write:chat user:read:email user:edit:follows";
                string state = Guid.NewGuid().ToString();
        
                string authorizationUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={clientId}&redirect_uri={redirectUri}&scope={Uri.EscapeDataString(scope)}&state={state}";
                // string authorizationUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={clientId}&redirect_uri={redirectUri}&scope={Uri.EscapeDataString(scope)}&state={state}";
                Main.LogEntry(methodName, $"Authorization URL: {authorizationUrl}");
        
                // Open the authorization URL in the default web browser
                await Task.Run(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authorizationUrl,
                    UseShellExecute = true
                }));
        
                Main.LogEntry(methodName, "Opened Twitch authorization URL in the default web browser.");
        
                // Start an HTTP listener to capture the response
                HttpListener listener = new();
                listener.Prefixes.Add(redirectUri);
                listener.Start();
                Main.LogEntry(methodName, "Waiting for Twitch authorization response...");

                CancellationTokenSource cts = new();
                cts.CancelAfter(TimeSpan.FromSeconds(60));
        
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync().WithCancellation(cts.Token);
                    string responseString = $@"
                    <html>
                    <body>
                    Derail Valley TwitchChat has received your authorization approval. You can close this window and continue in the game.
                    <script type='text/javascript'>
                        var xhr = new XMLHttpRequest();
                        xhr.open('GET', '{redirectUri}?' + window.location.hash.substring(1), true);
                        xhr.send();
                    </script>
                    </body>
                    </html>";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Close();

                    // Handle the /save_token request
                    context = await listener.GetContextAsync().WithCancellation(cts.Token);
                    string responseUrl = context.Request.Url.ToString();
                    Main.LogEntry(methodName, $"Received response:\n{responseUrl}");

                    string accessToken = string.Empty;
                    string tokenParam = "access_token=";
                    int tokenStartIndex = responseUrl.IndexOf(tokenParam);
                    if (tokenStartIndex != -1)
                    {
                        tokenStartIndex += tokenParam.Length;
                        int tokenEndIndex = responseUrl.IndexOf('&', tokenStartIndex);
                        if (tokenEndIndex == -1)
                        {
                            tokenEndIndex = responseUrl.Length;
                        }
                        accessToken = responseUrl.Substring(tokenStartIndex, tokenEndIndex - tokenStartIndex);
                    }

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        Main.LogEntry(methodName, $"Access token: {accessToken}");
                        oath_access_token = accessToken;
                        await GetUserID();
                        await ValidateAuthToken();
                        // GetTokenExpirationTime();
                    
                        // Encode and save the token to settings
                        string encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(accessToken));
                        Settings.Instance.EncodedOAuthToken = encodedToken;
                        Settings.Save(Settings.Instance, Main.ModEntry);
                    }
                    else
                    {
                        Main.LogEntry(methodName, "Failed to extract access token from the response URL.");
                    }

                }
                catch (OperationCanceledException)
                {
                    Main.LogEntry(methodName, "Authorization response timed out.");
                }
                finally
                {
                    listener.Stop();
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Failed to get oath Token: {ex.Message}");
            }
        }
        public static async Task ValidateAuthToken()
        {
            string methodName = "ValidateAuthToken";
            
            // if (string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken))
            // {
            //     Main.LogEntry(methodName, "EncodedOAuthToken is empty. Skipping validation.");
            //     return;
            // }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oath_access_token);
            Main.LogEntry(methodName, $"Validating oath token...");

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

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Main.LogEntry(methodName, "Token is not valid. Refreshing token...");
                    }
                    else
                    {
                        Main.LogEntry(methodName, "Validated token.");
                    }

                    // Fetch your user ID
                    if (await GetUserID())
                    {
                        Main.LogEntry(methodName, "User ID automatically retrieved.");
                    }
                    else
                    {
                        Main.LogEntry(methodName, "Failed to automatically retrieve user ID (check username?).");
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
                    await Task.Delay(2000); // Wait for 2 seconds before retrying
                }
                else
                {
                    Main.LogEntry(methodName, "Max retry attempts reached. Giving up.");
                }
            }
        }
        // Method to check the expiration time of a JWT token
        private static void GetTokenExpirationTime()
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(oath_access_token) as JwtSecurityToken;

            if (jwtToken == null)
                oath_token_expiration = "Not a valid JWT token";

            var expClaim = jwtToken?.Claims.FirstOrDefault(claim => claim.Type == "exp");
            if (expClaim == null)
                oath_token_expiration = "No exp claim";

            var expUnix = expClaim != null ? long.Parse(expClaim.Value) : 0;
            var expDateTime = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;

            oath_token_expiration = expDateTime.ToString("yyyy-MM-dd HH:mm:ss");

        }
        // Example usage
        // string accessToken = "your_access_token_here";
        // DateTime? expirationTime = GetTokenExpirationTime(accessToken);

        // if (expirationTime.HasValue)
        // {
        //     TimeSpan timeLeft = expirationTime.Value - DateTime.UtcNow;
        //     Console.WriteLine($"Token expires in: {timeLeft.TotalMinutes} minutes");
        // }
        // else
        // {
        //     Console.WriteLine("Unable to determine token expiration time.");
        // }


        public static async Task<bool> GetUserID()
        {
            string methodName = "GetUserID";
            Main.LogEntry(methodName, "Adding Authorization and Client-Id headers.");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {oath_access_token}");
            httpClient.DefaultRequestHeaders.Add("Client-Id", Main.GetClientId());
        
            Main.LogEntry(methodName, "Sending GET request to https://api.twitch.tv/helix/users.");
            var response = await httpClient.GetAsync($"https://api.twitch.tv/helix/users?login={Settings.Instance.twitchUsername}");
            Main.LogEntry(methodName, $"Response status code: {response.StatusCode}");
        
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Main.LogEntry(methodName, $"Response error content: {errorContent}");
                return false;
            }
        
            response.EnsureSuccessStatusCode();
        
            var content = await response.Content.ReadAsStringAsync();
            Main.LogEntry(methodName, $"Response content: {content}");
            var jsonDocument = JsonDocument.Parse(content);
            var lookup_id = jsonDocument.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
        
            if (lookup_id == null)
            {
                Main.LogEntry(methodName, "User ID is null.");
                return false;
            }
        
            user_id = lookup_id;
            Main.LogEntry(methodName, $"User ID: {user_id}");
        
            return true;
        }
        public static async Task ConnectionStatus()
        {
            string methodName = "ConnectionStatus";
            try
            {
                Main.LogEntry(methodName, "Adding Authorization and Client-Id headers.");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {oath_access_token}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", Main.GetClientId());

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
        public static async Task SendMessage(string message)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                Main.LogEntry(methodName, "Preparing request body.");
                var requestBody = new
                {
                    broadcaster_id = user_id,
                    sender_id = user_id,
                    message = message
                };

                var jsonRequestBody = System.Text.Json.JsonSerializer.Serialize(requestBody);
                Main.LogEntry(methodName, $"Request body prepared: {jsonRequestBody}");

                var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

                Main.LogEntry(methodName, "Headers set: Client-Id and Authorization");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {oath_access_token}");
                httpClient.DefaultRequestHeaders.Add("Client-Id", Main.GetClientId());

                Main.LogEntry(methodName, "Sending POST request to https://api.twitch.tv/helix/chat/messages");
                var response = await httpClient.PostAsync("https://api.twitch.tv/helix/chat/messages", content);
                Main.LogEntry(methodName, $"Response status code: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Main.LogEntry(methodName, $"Response content: {responseContent}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Main.LogEntry(methodName, "Unauthorized. Token might be expired.");
                    return;
                }

                response.EnsureSuccessStatusCode();
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
        public static async Task SendChatMessageHTTP(string chatMessage)
        {
            string methodName = "SendChatMessageHTTP";
            Main.LogEntry(methodName, $"Preparing to send chat message: {chatMessage}");
        
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                broadcaster_id = user_id,
                sender_id = user_id,
                message = chatMessage
            }), Encoding.UTF8, "application/json");
        
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
                    Main.LogEntry(methodName, $"Sent chat message: {chatMessage}");
                    Main.LogEntry("SentMessage", $"Sent Message: {chatMessage}");
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Exception occurred: {ex.Message}");
            }
        }
    }
}

public static class TaskExtensions
{
    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
        {
            if (task != await Task.WhenAny(task, tcs.Task))
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
        return await task;
    }
}
