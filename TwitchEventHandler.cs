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

namespace TwitchChat
{
    public class HttpManager
    {
        private static readonly HttpClient httpClient = new();
        public static async Task GetOathToken()
        {
            string methodName = "GetOathToken";
            Main.LogEntry(methodName, "Sending oath Token request to Twitch...");
        
            try
            {
                string clientId = Main.GetClientId();
                string redirectUri = "http://localhost/";
                string scope = "chat:edit chat:read channel:bot channel:manage:broadcast channel:moderate channel:read:subscriptions user:read:chat user:read:subscriptions user:write:chat user:read:email user:edit:follows";
                string state = Guid.NewGuid().ToString();
        
                string authorizationUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={clientId}&redirect_uri={redirectUri}&scope={Uri.EscapeDataString(scope)}&state={state}";
                Main.LogEntry(methodName, $"Authorization URL: {authorizationUrl}");
        
                // Open the authorization URL in the default web browser
                await Task.Run(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authorizationUrl,
                    UseShellExecute = true
                }));
        
                Main.LogEntry(methodName, "Opened Twitch authorization URL in the default web browser.");
        
                // Start a local HTTP listener to capture the response
                HttpListener listener = new();
                listener.Prefixes.Add(redirectUri);
                listener.Prefixes.Add("http://localhost/save_token/");
                listener.Start();
                Main.LogEntry(methodName, "Waiting for Twitch authorization response...");

                CancellationTokenSource cts = new();
                cts.CancelAfter(TimeSpan.FromSeconds(60));
        
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync().WithCancellation(cts.Token);
                    string responseString = @"
                    <html>
                    <body>
                    You can close this window now.
                    <script type='text/javascript'>
                        var xhr = new XMLHttpRequest();
                        xhr.open('GET', 'http://localhost/save_token?' + window.location.hash.substring(1), true);
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
                    Main.LogEntry(methodName, $"Received response: {responseUrl}");

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
                        WebSocketManager.oath_access_token = accessToken;
                        await GetUserID();
                        await WebSocketManager.ConnectToWebSocket();
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
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", WebSocketManager.oath_access_token);
            Main.LogEntry(methodName, $"Validating token: {WebSocketManager.oath_access_token}");

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
                        await WebSocketManager.RefreshAuthToken();
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
        public static async Task<bool> GetUserID()
        {
            string methodName = "GetUserID";
            Main.LogEntry(methodName, "Adding Authorization and Client-Id headers.");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {WebSocketManager.oath_access_token}");
            _ = new WebSocketManager();
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
            var id = jsonDocument.RootElement.GetProperty("data")[0].GetProperty("id").GetString();
        
            if (id == null)
            {
                Main.LogEntry(methodName, "User ID is null.");
                return false;
            }
        
            WebSocketManager.user_id = id;
            Main.LogEntry(methodName, $"User ID: {WebSocketManager.user_id}");
        
            return true;
        }
        public static async Task ConnectionStatus()
        {
            string methodName = "ConnectionStatus";
            try
            {
                Main.LogEntry(methodName, "Adding Authorization and Client-Id headers.");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {WebSocketManager.oath_access_token}");
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

                var jsonResponse = JsonDocument.Parse(userContent);
                var broadcasterId = jsonResponse.RootElement.GetProperty("data")[0].GetProperty("id").GetString();

                Main.LogEntry(methodName, "Sending GET request to https://api.twitch.tv/helix/channels.");
                var channelResponse = await httpClient.GetAsync($"https://api.twitch.tv/helix/channels?broadcaster_id={broadcasterId}");
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
                    broadcaster_id = WebSocketManager.user_id,
                    sender_id = WebSocketManager.user_id,
                    message = message
                };

                var jsonRequestBody = System.Text.Json.JsonSerializer.Serialize(requestBody);
                Main.LogEntry(methodName, $"Request body prepared: {jsonRequestBody}");

                var content = new StringContent(jsonRequestBody, System.Text.Encoding.UTF8, "application/json");

                Main.LogEntry(methodName, "Headers set: Client-Id and Authorization");
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {WebSocketManager.oath_access_token}");
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
                broadcaster_id = WebSocketManager.user_id,
                sender_id = WebSocketManager.user_id,
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