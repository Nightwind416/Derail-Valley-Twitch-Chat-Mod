using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// Coarse state of the link between the mod and the user's Twitch account.
    /// The UI switches on this rather than on the wording of the status message.
    /// </summary>
    public enum AuthPhase
    {
        /// <summary>No token held. The user has not linked an account, or has signed out.</summary>
        NotConnected,
        /// <summary>An authorization is in flight and the mod is waiting on the user.</summary>
        AwaitingUser,
        /// <summary>A token is held and was accepted by Twitch.</summary>
        Connected,
        /// <summary>The last attempt failed. <see cref="OAuthTokenManager.StatusMessage"/> says why.</summary>
        Failed
    }

    /// <summary>
    /// Manages OAuth authentication flow with Twitch API.
    /// Handles secure token acquisition, storage, and validation for Twitch integration.
    /// Provides browser-based authentication and automatic token refresh functionality.
    /// </summary>
    public class OAuthTokenManager : MonoBehaviour
    {
        /// <summary>
        /// The exact scopes the mod asks Twitch for, and nothing beyond them.
        /// Every entry here becomes a line on the consent screen the user has to trust us with,
        /// so each one is listed with the single thing it is needed for:
        ///   user:read:chat                 - EventSub channel.chat.message, to show chat in game
        ///   user:write:chat                - POST /helix/chat/messages, for command replies and timed messages
        ///   user:manage:whispers           - POST /helix/whispers, for command replies set to whisper
        ///   moderator:manage:announcements - POST /helix/chat/announcements, for blue timed messages
        /// The mod talks to Twitch with a user access token, so the bot scopes (user:bot, channel:bot)
        /// are not required, and the legacy IRC scopes (chat:read, chat:edit) are superseded by the
        /// user:*:chat pair. user:read:email was never used by any code path.
        /// </summary>
        public const string RequestedScopes = "user:read:chat user:write:chat user:manage:whispers moderator:manage:announcements";

        /// <summary>Plain-language description of each requested scope, shown on the in-game consent screen.</summary>
        public static readonly string[] ScopeExplanations =
        [
            "Read the messages in your chat, to show them on your in-game panels",
            "Send chat messages as you, for the !info and !commands replies and any timed messages you set up",
            "Send whispers as you, used only for command replies you have set to whisper",
            "Post announcements in your chat, used only for timed messages you have coloured blue"
        ];

        /// <summary>Things the mod deliberately cannot do, shown alongside the scope list.</summary>
        public static readonly string[] ScopeExclusions =
        [
            "Read your email address",
            "See your subscribers, followers or revenue",
            "Change your stream title, category or settings",
            "Follow, subscribe or buy anything as you"
        ];

        /// <summary>Current state of the account link. Drives the authentication UI.</summary>
        public static AuthPhase Phase { get; private set; } = AuthPhase.NotConnected;

        /// <summary>Human-readable detail for the current <see cref="Phase"/>.</summary>
        public static string StatusMessage { get; private set; } = "Not connected";

        /// <summary>
        /// Records a new authentication state and mirrors it into settings so the
        /// Unity Mod Manager menu and the status panel stay in step.
        /// </summary>
        internal static void SetPhase(AuthPhase phase, string message)
        {
            Phase = phase;
            StatusMessage = message;
            Settings.Instance.authentication_status = message;
        }

        /// <summary>
        /// Restores the phase from whatever is on disk at start-up, without contacting Twitch.
        /// </summary>
        public static void InitialisePhaseFromSettings()
        {
            if (string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken))
            {
                SetPhase(AuthPhase.NotConnected, "Not connected");
            }
            else
            {
                SetPhase(AuthPhase.NotConnected, "Saved token found, not yet checked");
            }
        }

        /// <summary>
        /// Reads a single parameter out of a callback URL's query string or fragment.
        /// </summary>
        /// <param name="url">The URL to read from.</param>
        /// <param name="name">The parameter name, without the trailing '='.</param>
        /// <returns>The parameter value, or an empty string if it is not present.</returns>
        private static string ReadParameter(string url, string name)
        {
            string marker = name + "=";
            int start = url.IndexOf(marker, StringComparison.Ordinal);
            if (start == -1)
            {
                return string.Empty;
            }

            start += marker.Length;
            int end = url.IndexOf('&', start);
            if (end == -1)
            {
                end = url.Length;
            }

            return url.Substring(start, end - start);
        }

        /// <summary>
        /// Returns the stored access token in plain text, or an empty string if none is held
        /// or the stored value cannot be decoded.
        /// </summary>
        /// <remarks>
        /// The result is a live credential. Never write it to a log, a status message or the UI.
        /// </remarks>
        public static string GetAccessToken()
        {
            if (string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken))
            {
                return string.Empty;
            }

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(Settings.Instance.EncodedOAuthToken));
            }
            catch (FormatException)
            {
                Main.LogEntry("GetAccessToken", "Stored token is not valid base64. Treating it as absent.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Tells Twitch to revoke the stored token, then clears it from this machine.
        /// </summary>
        /// <remarks>
        /// The local token is cleared whether or not Twitch accepts the revoke call, so a user who
        /// asks to sign out always ends up signed out locally. Revocation is immediate and permanent;
        /// reconnecting requires a fresh authorization.
        /// </remarks>
        /// <returns>An asynchronous task representing the revoke operation.</returns>
        public static async Task RevokeAndSignOut()
        {
            string methodName = "RevokeAndSignOut";
            string accessToken = GetAccessToken();

            if (!string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    Main.LogEntry(methodName, "Asking Twitch to revoke the stored token...");
                    var body = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", TwitchEventHandler.GetClientId()),
                        new KeyValuePair<string, string>("token", accessToken)
                    });

                    // A bare client is used here so the shared client's Authorization header,
                    // which carries the very token being revoked, is not sent along with it.
                    using var client = new HttpClient();
                    var response = await client.PostAsync("https://id.twitch.tv/oauth2/revoke", body);
                    Main.LogEntry(methodName, $"Revoke response status code: {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    // Non-fatal: the local copy is still discarded below.
                    Main.LogEntry(methodName, $"Revoke request failed: {ex.Message}. Clearing the local token anyway.");
                }
            }

            Settings.Instance.EncodedOAuthToken = string.Empty;
            TwitchEventHandler.user_id = string.Empty;
            TwitchEventHandler.httpClient.DefaultRequestHeaders.Clear();
            Settings.Save(Settings.Instance, Main.ModEntry);
            SetPhase(AuthPhase.NotConnected, "Signed out. Access revoked.");
            Main.LogEntry(methodName, "Local token cleared.");
        }

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
            SetPhase(AuthPhase.AwaitingUser, "Attempting Authentication...");
        
            try
            {
                if (string.IsNullOrEmpty(Settings.Instance.twitchUsername))
                {
                    Main.LogEntry(methodName, "Twitch username is not set. Please set a username in the settings first.");
                    SetPhase(AuthPhase.Failed, "No Username Set");
                    return;
                }
                
                Main.LogEntry(methodName, $"Using Twitch username: {Settings.Instance.twitchUsername}");
                
                string clientId = TwitchEventHandler.GetClientId();
                string scope = RequestedScopes;
                string state = Guid.NewGuid().ToString();

                string authorizationUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={clientId}&redirect_uri=http://localhost/&scope={Uri.EscapeDataString(scope)}&state={state}";
                Main.LogEntry(methodName, $"Requesting scopes: {scope}");
        
                // Open the authorization URL in the default web browser
                await Task.Run(() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authorizationUrl,
                    UseShellExecute = true
                }));
        
                Main.LogEntry(methodName, "Opened Twitch authorization URL in the default web browser.");
                SetPhase(AuthPhase.AwaitingUser, "Check external browser...");
        
                // Start an HTTP listener to capture the response
                using var listener = new HttpListener();
                listener.Prefixes.Add("http://localhost/");
                listener.Start();
                Main.LogEntry(methodName, "Waiting for Twitch authorization response...");
                SetPhase(AuthPhase.AwaitingUser, "Awaiting Authentication...");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        
                try
                {
                    var getContextTask = listener.GetContextAsync();
                    var context = await Task.WhenAny(getContextTask, Task.Delay(-1, cts.Token))
                        .ContinueWith(t => t.IsFaulted || t.IsCanceled ? null : getContextTask.Result);

                    if (context == null)
                    {
                        Main.LogEntry(methodName, "Authorization response timed out.");
                        SetPhase(AuthPhase.Failed, "Authorization failed. Please try again.");
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
                    // Never log this URL: on success it carries the access token in its query string.
                    Main.LogEntry(methodName, $"Received callback ({responseUrl.Length} chars).");

                    string accessToken = ReadParameter(responseUrl, "access_token");
                    string returnedState = ReadParameter(responseUrl, "state");

                    if (!string.IsNullOrEmpty(accessToken) && returnedState != state)
                    {
                        // The callback did not come from the request we started. Discard it.
                        Main.LogEntry(methodName, "Callback state did not match the request. Ignoring the token.");
                        SetPhase(AuthPhase.Failed, "Authorization response did not match. Please try again.");
                        return;
                    }

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        Main.LogEntry(methodName, "Access token received.");

                        // Encode and save the token to settings
                        string encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(accessToken));
                        Settings.Instance.EncodedOAuthToken = encodedToken;
                        Settings.Save(Settings.Instance, Main.ModEntry);

                        SetPhase(AuthPhase.Connected, "Validated!");

                        await TwitchEventHandler.GetUserID();
                    }
                    else
                    {
                        Main.LogEntry(methodName, "Failed to extract access token from the response URL.");
                        SetPhase(AuthPhase.Failed, "Authorization failed. Please try again.");
                    }

                }
                catch (OperationCanceledException)
                {
                    Main.LogEntry(methodName, "Authorization response timed out.");
                    SetPhase(AuthPhase.Failed, "Authorization failed. Please try again.");
                }
                finally
                {
                    listener.Stop();
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Failed to get oath Token: {ex.Message}");
                SetPhase(AuthPhase.Failed, "Authorization failed. Please try again.");
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

            if (string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken))
            {
                Main.LogEntry(methodName, "No saved token found.");
                SetPhase(AuthPhase.NotConnected, "No saved token found. Please try again.");
                return;
            }

            // Decode before anything else: a settings file that has been hand-edited or truncated
            // used to throw out of this method and leave the mod with no way to recover.
            string access_token = GetAccessToken();
            if (string.IsNullOrEmpty(access_token))
            {
                Settings.Instance.EncodedOAuthToken = string.Empty;
                Settings.Save(Settings.Instance, Main.ModEntry);
                SetPhase(AuthPhase.Failed, "Error decoding saved token. Please try again.");
                return;
            }

            Main.LogEntry(methodName, "Found saved token, attempting to validate...");
            SetPhase(AuthPhase.AwaitingUser, "Found saved token, attempting to validate...");


            TwitchEventHandler.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            Main.LogEntry(methodName, $"Validating oath token...");
            SetPhase(AuthPhase.AwaitingUser, "Validating Authorization Token...");

            // Header names only. The Authorization header's value is the access token itself.
            Main.LogEntry(methodName, $"Request headers: {string.Join(", ", TwitchEventHandler.httpClient.DefaultRequestHeaders.Select(h => h.Key))}");


            int retryCount = 3;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    Main.LogEntry(methodName, "Sending request to Twitch API...");
                    SetPhase(AuthPhase.AwaitingUser, "Sending Validation request...");
                    var response = await TwitchEventHandler.httpClient.GetAsync("https://id.twitch.tv/oauth2/validate");
                    stopwatch.Stop();
                    Main.LogEntry(methodName, $"Response status code: {response.StatusCode}, Time taken: {stopwatch.ElapsedMilliseconds} ms");

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Main.LogEntry(methodName, "Token is not valid. Clearing saved token...");
                        Settings.Instance.EncodedOAuthToken = string.Empty;
                        Settings.Save(Settings.Instance, Main.ModEntry);
                        SetPhase(AuthPhase.Failed, "Validation failed. Please try again.");
                        return;
                    }
                    else
                    {
                        Main.LogEntry(methodName, "Validated token.");
                        SetPhase(AuthPhase.Connected, "Validated!");
                        
                        await TwitchEventHandler.GetUserID();
                    }

                break; // Exit the retry loop if successful
                }
                catch (HttpRequestException ex)
                {
                    Main.LogEntry(methodName, $"HTTP request error: {ex.Message}");
                    SetPhase(AuthPhase.Failed, "HTTP request error");
                    if (ex.InnerException != null)
                    {
                        Main.LogEntry(methodName, $"Inner exception: {ex.InnerException.Message}");
                    }
                }
                catch (TaskCanceledException ex)
                {
                    Main.LogEntry(methodName, $"Request timed out: {ex.Message}");
                    SetPhase(AuthPhase.Failed, "Request timed out");
                    if (ex.InnerException != null)
                    {
                        Main.LogEntry(methodName, $"Inner exception: {ex.InnerException.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Main.LogEntry(methodName, $"Unexpected error: {ex.Message}");
                    SetPhase(AuthPhase.Failed, "Unexpected error");
                    if (ex.InnerException != null)
                    {
                        Main.LogEntry(methodName, $"Inner exception: {ex.InnerException.Message}");
                    }
                    Main.LogEntry(methodName, $"Stack trace: {ex.StackTrace}");
                }

                if (i < retryCount - 1)
                {
                    Main.LogEntry(methodName, "Retrying...");
                    SetPhase(AuthPhase.AwaitingUser, "Retrying");
                    await Task.Delay(2000); // Wait for 2 seconds before retrying
                }
                else
                {
                    Main.LogEntry(methodName, "Max retry attempts reached. Giving up.");
                    SetPhase(AuthPhase.Failed, "Max retry attempts reached. Giving up.");
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