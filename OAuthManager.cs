using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TwitchChat
{
    /// <summary>
    /// Manages OAuth authentication flow with Twitch API.
    /// Handles secure token acquisition, storage, and validation for Twitch integration.
    /// Provides browser-based authentication and automatic token refresh functionality.
    /// </summary>
    public class OAuthTokenManager : MonoBehaviour
    {
        /// <summary>
        /// Initiates the OAuth token retrieval process through Twitch authentication.
        /// Opens a browser window for user authorization and captures the response token.
        /// Handles token storage and validation after successful authentication.
        /// </summary>
        /// <returns>An asynchronous task representing the OAuth token retrieval operation.</returns>
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
                string scope = "channel:bot user:read:chat user:bot user:read:email user:write:chat chat:edit chat:read user:manage:whispers moderator:manage:announcements";
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
                    
                    // if (File.Exists(htmlPath))
                    // {
                    //     responseString = File.ReadAllText(htmlPath);
                    // }
                    // else
                    // {
                    //     // Fallback to simple HTML if file is not found
                    //     responseString = "<html><body>Authorization successful. You can close this window.</body></html>";
                    //     Main.LogEntry(methodName, "Authorization response HTML file not found, using fallback.");
                    // }

                    responseString = AuthorizationResponse;

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

        /// <summary>
        /// Validates the stored OAuth token with Twitch API.
        /// Checks token validity, handles token refresh if needed, and updates authentication status.
        /// Includes retry logic for failed validation attempts.
        /// </summary>
        /// <returns>An asynchronous task representing the token validation operation.</returns>
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
                    Settings.Instance.authentication_status = "Error decoding saved token. Please try again.";
                    return;
                }
            }
            else
            {
                Main.LogEntry(methodName, "No saved token found.");
                Settings.Instance.authentication_status = "No saved token found. Please try again.";
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
                    Settings.Instance.authentication_status = "HTTP request error";
                    if (ex.InnerException != null)
                    {
                        Main.LogEntry(methodName, $"Inner exception: {ex.InnerException.Message}");
                    }
                }
                catch (TaskCanceledException ex)
                {
                    Main.LogEntry(methodName, $"Request timed out: {ex.Message}");
                    Settings.Instance.authentication_status = "Request timed out";
                    if (ex.InnerException != null)
                    {
                        Main.LogEntry(methodName, $"Inner exception: {ex.InnerException.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Main.LogEntry(methodName, $"Unexpected error: {ex.Message}");
                    Settings.Instance.authentication_status = "Unexpected error";
                    if (ex.InnerException != null)
                    {
                        Main.LogEntry(methodName, $"Inner exception: {ex.InnerException.Message}");
                    }
                    Main.LogEntry(methodName, $"Stack trace: {ex.StackTrace}");
                }

                if (i < retryCount - 1)
                {
                    Main.LogEntry(methodName, "Retrying...");
                    Settings.Instance.authentication_status = "Retrying";
                    await Task.Delay(2000); // Wait for 2 seconds before retrying
                }
                else
                {
                    Main.LogEntry(methodName, "Max retry attempts reached. Giving up.");
                    Settings.Instance.authentication_status = "Max retry attempts reached. Giving up.";
                }
            }
        }

        /// <summary>
        /// HTML template for the authorization response page.
        /// Provides user feedback and instructions after successful authentication.
        /// </summary>
        private static readonly string AuthorizationResponse = @"
<!DOCTYPE html>
<html>
<head>
    <title>Derail Valley TwitchChat Mod Authorization</title>
    <meta charset='UTF-8'>
    <style>
        body {
            font-family: 'Segoe UI', Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            margin: 0;
            background-color: #1a1a1a;
            color: #e0e0e0;
        }
        .container {
            text-align: center;
            padding: 2.5em;
            background-color: #0a0909;
            box-shadow: 0 4px 8px rgba(0,0,0,0.4);
            max-width: 800px;
            width: 90%;
        }
        h2 {
            color: #31e5e5;
            font-size: 2.0em;
        }
        a {
            color: #1596ff;
            text-decoration: none;
            transition: color 0.4s;
        }
        a:hover {
            color: #43ed36;
            text-decoration: underline;
        }
        .section {
            margin-top: 1.0em;
            background-color: #363636;
            border-radius: 50px;
            position: relative;
            border: 5px solid #04051b;
        }
        .paypal-button {
            padding: 8px 16px;
            background: #0070ba;
            border-radius: 4px;
            color: white !important;
        }
    </style>
</head>

<body>
    <div class='container'>
        <div>
            <h1 style='color: #35eae4; font-size: 3em;'>Authorization Successful</h1>        
            <p style='color: #43d299; font-weight: bold; font-size: 1.3em;'>TwitchChat Authentication Token Received!</p>
            <p  style='color: #a0a0a0; font-size: 0.9em;'>Your Derail Valley game is now able to receive Twitch messages while in game. Make sure to finish enabling the mod using the in game mod menus. (It is safe to close this window and return to the game at this time.)</p>
        </div>
        
        <div class='section' style='text-align: left; padding: 0em 2em 0em 2em;'>
            <h2 style='color:rgb(237, 73, 55); font-size: 2.0em; text-align: center; margin-bottom: 0.5em;'>Managing Your Twitch Authorization</h2>
            <p>Authentication Tokens are typically good for 30 days, but may be revoked or cancelled for any number of reasons, including by 'you'. If your Token is not validated, you can simply request a new one in the same way as the initial request that got you here.</p>
            <p>If you ever need or want to revoke access/remove authorization:</p>
            <ol>
            <li>Visit the official <a href='https://www.twitch.tv/settings/connections' target='_blank'>Twitch Connection Settings</a> page</li>
            <li>Find DerailValleyChatMod under the Other Connections section, lower on the page</li>
            <li>Click Disconnect</li>
            </ol>
            <p>You can always re-authorize the mod by requesting a new Authorization Token through the in-game settings menu.</p>
        </div>

        <div class='section' style='padding: 0em 0em 1.5em 0em;'>
            <h2>Donations</h2>
            <div style='display: flex; justify-content: center; align-items: center; gap: 10px;'>
            <a href='https://ko-fi.com/A0A217PWSY' target='_blank'><img src='https://storage.ko-fi.com/cdn/kofi5.png?v=6' style='height: 40px !important;border:0px;height:36px;' alt='Buy Me a Coffee at ko-fi.com' /></a>
            <a href='https://www.buymeacoffee.com/christophe1xf' target='_blank'><img src='https://cdn.buymeacoffee.com/buttons/v2/default-blue.png' style='height: 60px !important;width: 217px !important;' ></a>
            <a href='https://paypal.me/Nightwind416?country.x=US&locale.x=en_US' target='_blank' class='paypal-button'>PayPal.me Donation</a>                
            </div>
        </div>
        
        <div class='section'>
            <h2>TwitchChat Mod Details</h2>
        <div>
            <a href='https://www.nexusmods.com/derailvalley/mods/1069' target='_blank'>Nexus Mods Page</a> |
            <a href='https://github.com/Nightwind416/Derail-Valley-Twitch-Chat-Mod' target='_blank'>GitHub Repository</a> |
            <a href='https://github.com/Nightwind416/Derail-Valley-Twitch-Chat-Mod/issues' target='_blank'>Issues and Suggestions</a>
        </div>
            <h2>Created By</h2>
            <p>Derail Valley TwitchChat Mod developed by Nightwind</p>
            <p>Follow <a href='https://www.twitch.tv/nightwind416' target='_blank'>Nightwind's Twitch Channel</a> to see the mod in action</p>
        </div>
        
        <div style='color: #a0a0a0; font-size: 0.9em;'>
            <p>This is a third-party modification created by an independent developer and not affiliated with or endorsed by <a href='https://www.altfuture.gg' target='_blank'>Altfuture</a> or the <a href='https://www.derailvalley.com' target='_blank'>Derail Valley</a> development team.</p>
        </div>
    </div>
    
    <script>
        // Send auth token
        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'http://localhost/?' + window.location.hash.substring(1), true);
        xhr.send();
    </script>
</body>
</html>";
    }
}