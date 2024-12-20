using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Net;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Net.Http.Headers;
using System.Net.Http;

namespace TwitchChat
{
    public class OAuthTokenManager : MonoBehaviour
    {
        public static async Task GetOathToken()
        {
            string methodName = "GetOathToken";
            Main.LogEntry(methodName, "Sending oath Token request to Twitch...");
            Settings.Instance.authentication_status = "Attempting Authentication...";
        
            try
            {
                if (string.IsNullOrEmpty(Settings.Instance.twitchUsername))
                {
                    Main.LogEntry(methodName, "Twitch username is not set. Please set a username in the settings first.");
                    Settings.Instance.authentication_status = "No Username Set";
                    return;
                }
                
                Main.LogEntry(methodName, $"Using Twitch username: {Settings.Instance.twitchUsername}");
                
                string clientId = TwitchEventHandler.GetClientId();
                string scope = "chat:edit chat:read channel:bot channel:manage:broadcast channel:moderate channel:read:subscriptions user:read:chat user:read:subscriptions user:write:chat user:read:email user:edit:follows user:edit:broadcast";
                string state = Guid.NewGuid().ToString();
        
                string authorizationUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={clientId}&redirect_uri=http://localhost/&scope={Uri.EscapeDataString(scope)}&state={state}";
                Main.LogEntry(methodName, $"Authorization URL: {authorizationUrl}");
        
                // Open the authorization URL in the default web browser
                await Task.Run(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authorizationUrl,
                    UseShellExecute = true
                }));
        
                Main.LogEntry(methodName, "Opened Twitch authorization URL in the default web browser.");
                Settings.Instance.authentication_status = "Check external browser...";
        
                // Start an HTTP listener to capture the response
                using var listener = new HttpListener();
                listener.Prefixes.Add("http://localhost/");
                listener.Start();
                Main.LogEntry(methodName, "Waiting for Twitch authorization response...");
                Settings.Instance.authentication_status = "Awaiting Authentication...";

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        
                try
                {
                    var getContextTask = listener.GetContextAsync();
                    var context = await Task.WhenAny(getContextTask, Task.Delay(-1, cts.Token))
                        .ContinueWith(t => t.IsFaulted || t.IsCanceled ? null : getContextTask.Result);

                    if (context == null)
                    {
                        Main.LogEntry(methodName, "Authorization response timed out.");
                        Settings.Instance.authentication_status = "Authorization failed. Please try again.";
                        return;
                    }

                    string htmlPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "authorization_response.html");
                    string responseString;
                    
                    if (File.Exists(htmlPath))
                    {
                        responseString = File.ReadAllText(htmlPath);
                    }
                    else
                    {
                        // Fallback to simple HTML if file is not found
                        responseString = "<html><body>Authorization successful. You can close this window.</body></html>";
                        Main.LogEntry(methodName, "Authorization response HTML file not found, using fallback.");
                    }

                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.ContentType = "text/html";
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Close();

                    // Handle the /save_token request
                    var secondContext = await listener.GetContextAsync();
                    string responseUrl = secondContext.Request.Url.ToString();
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
                        // oath_access_token = accessToken;
                    
                        // Encode and save the token to settings
                        string encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(accessToken));
                        Settings.Instance.EncodedOAuthToken = encodedToken;
                        Settings.Save(Settings.Instance, Main.ModEntry);

                        Settings.Instance.authentication_status = "Validated!";

                        await TwitchEventHandler.GetUserID();
                    }
                    else
                    {
                        Main.LogEntry(methodName, "Failed to extract access token from the response URL.");
                        Settings.Instance.authentication_status = "Authorization failed. Please try again.";
                    }

                }
                catch (OperationCanceledException)
                {
                    Main.LogEntry(methodName, "Authorization response timed out.");
                    Settings.Instance.authentication_status = "Authorization failed. Please try again.";
                }
                finally
                {
                    listener.Stop();
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Failed to get oath Token: {ex.Message}");
                Settings.Instance.authentication_status = "Authorization failed. Please try again.";
            }
        }
        public static async Task ValidateAuthToken()
        {
            string methodName = "ValidateAuthToken";

            byte[] tokenBytes = Convert.FromBase64String(Settings.Instance.EncodedOAuthToken);
            _ = Encoding.UTF8.GetString(tokenBytes);
            string access_token = Encoding.UTF8.GetString(tokenBytes);
            
            // Check for encoded token first
            if (!string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken))
            {
                try
                {
                    Main.LogEntry(methodName, "Found saved token, attempting to validate...");
                    Settings.Instance.authentication_status = "Found saved token, attempting to validate...";
                }
                catch (Exception ex)
                {
                    Main.LogEntry(methodName, $"Error decoding saved token: {ex.Message}");
                    Settings.Instance.EncodedOAuthToken = string.Empty;
                    Settings.Save(Settings.Instance, Main.ModEntry);
                    Settings.Instance.authentication_status = "Authorization failed. Please try again.";
                    return;
                }
            }
            else
            {
                Main.LogEntry(methodName, "No saved token found.");
                Settings.Instance.authentication_status = "Authorization failed. Please try again.";
                return;
            }
            
            TwitchEventHandler.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            Main.LogEntry(methodName, $"Validating oath token...");
            Settings.Instance.authentication_status = "Validating Authorization Token...";

            // Log the headers
            foreach (var header in TwitchEventHandler.httpClient.DefaultRequestHeaders)
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
                    Settings.Instance.authentication_status = "Sending Validation request...";
                    var response = await TwitchEventHandler.httpClient.GetAsync("https://id.twitch.tv/oauth2/validate");
                    stopwatch.Stop();
                    Main.LogEntry(methodName, $"Response status code: {response.StatusCode}, Time taken: {stopwatch.ElapsedMilliseconds} ms");

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Main.LogEntry(methodName, "Token is not valid. Clearing saved token...");
                        Settings.Instance.EncodedOAuthToken = string.Empty;
                        Settings.Save(Settings.Instance, Main.ModEntry);
                        Settings.Instance.authentication_status = "Validation failed. Please try again.";
                        return;
                    }
                    else
                    {
                        Main.LogEntry(methodName, "Validated token.");
                        Settings.Instance.authentication_status = "Validated!";
                        
                        await TwitchEventHandler.GetUserID();
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
    }
}