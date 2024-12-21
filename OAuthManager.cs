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
    /// </summary>
    public class OAuthTokenManager : MonoBehaviour
    {
        /// <summary>
        /// Initiates the OAuth token retrieval process through Twitch authentication.
        /// </summary>
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
        /// </summary>
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
        private static readonly string AuthorizationResponse = @"
<!DOCTYPE html>
<html>
<head>
    <title>Derail Valley TwitchChat Authorization</title>
    <meta charset='UTF-8'>
    <style>
        .holiday-effects {
            z-index: 1;
            position: relative;
        }
        body {
            font-family: 'Segoe UI', Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            background-color: #1a1a1a;
            color: #e0e0e0;
        }
        .container {
            text-align: center;
            padding: 2.5em;
            background-color: #0a0909;
            border-radius: 12px;
            box-shadow: 0 4px 8px rgba(0,0,0,0.4);
            max-width: 800px;
            width: 90%;
        }
        h1 {
            color: #35eae4;
            margin-bottom: 0.5em;
            font-size: 2em;
        }
        h2 {
            color: #31e5e5;
            margin-bottom: 0.5em;
            font-size: 1.5em;
        }
        .success-message {
            color: #43d299;
            font-weight: bold;
            margin-bottom: 1.5em;
        }
        .link-list {
            margin: 1.5em 0;
            padding: 1em;
            background-color: #363636;
            border-radius: 8px;
        }
        a {
            color: #1596ff;
            text-decoration: none;
            transition: color 0.3s;
        }
        a:hover {
            color: #43ed36;
            text-decoration: underline;
        }
        .close-note {
            font-size: 0.9em;
            color: #a0a0a0;
            margin-top: 1.5em;
            padding-top: 1.5em;
            z-index: 1000;
        }
        .revoke-section {
            margin-top: 1.5em;
            padding: 1em;
            background-color: #363636;
            border-radius: 5px;
            border: 1px solid #404040;
            text-align: left;
            position: relative;
            z-index: 10;
        }
        .revoke-section h2 {
            text-align: center;
            color:rgb(237, 73, 55);
            font-size: 1.2em;
            margin-bottom: 0.5em;
        }
        .details-section {
            margin: 1.5em 0;
            padding: 1em;
            background-color: #363636;
            border-radius: 8px;
            position: relative;
            z-index: 10;
        }
        .donation-section {
            margin: 1.5em 0;
            padding: 1em;
            background-color: #363636;
            border-radius: 8px;
            position: relative;
            z-index: 10;
        }
        .paypal-button {
            padding: 8px 16px;
            background: #0070ba;
            border-radius: 4px;
            color: white !important;
        }
        .disclaimer-section {
            font-size: 0.9em;
            color: #a0a0a0;
            margin-top: 1.5em;
            padding-top: 1.5em;
            z-index: 1000;
        }
        
        /* Holiday Effects */
        .snow-container, .confetti-container {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            pointer-events: none;
            z-index: 0;
        }

        /* holiday Lights Effect */
        .holiday-lights {
            position: relative;
        }

        .holiday-lights::before,
        .holiday-lights::after {
            content: '';
            position: absolute;
            left: -15px;
            right: -15px;
            height: 6px;
        }

        .holiday-lights::before {
            top: -15px;
            background: repeating-linear-gradient(
                90deg,
                transparent,
                transparent 12px,
                #222 12px,
                #222 17px
            );
        }

        .holiday-lights::after {
            top: -12px;
            z-index: 2;
            background-image: 
                radial-gradient(circle, red 2px, transparent 3px),
                radial-gradient(circle, green 2px, transparent 3px),
                radial-gradient(circle, blue 2px, transparent 3px),
                radial-gradient(circle, yellow 2px, transparent 3px);
            background-size: 17px 6px;
            background-position: 0 0, 4px 0, 8px 0, 12px 0;
            animation: lightTwinkle 1s infinite;
        }

        @keyframes lightTwinkle {
            0%, 100% {
                opacity: 0.9;
                filter: brightness(1);
            }
            50% {
                opacity: 0.5;
                filter: brightness(0.75);
            }
        }

        .holiday-lights .light {
            position: absolute;
            width: 6px;
            height: 6px;
            border-radius: 50%;
            animation: twinkle var(--twinkle-duration) infinite;
            animation-delay: var(--twinkle-delay);
        }
        
        .snowflake, .confetti {
            position: absolute;
            color: #fff;
            animation-timing-function: linear;
            animation-iteration-count: infinite;
        }
        
        .snowflake {
            animation-name: snowfall;
            will-change: transform;
            filter: drop-shadow(0 0 2px rgba(255,255,255,0.2));
        }
        
        .confetti {
            width: 10px;
            height: 10px;
            animation-name: confettifall;
            transform-origin: center;
            will-change: transform;
        }
        
        @keyframes snowfall {
            0% { transform: translateY(-100vh) translateX(0) rotate(0deg); }
            25% { transform: translateY(-50vh) translateX(var(--drift1)) rotate(90deg); }
            50% { transform: translateY(0vh) translateX(var(--drift2)) rotate(180deg); }
            75% { transform: translateY(50vh) translateX(var(--drift3)) rotate(270deg); }
            100% { transform: translateY(100vh) translateX(var(--drift4)) rotate(360deg); }
        }
        
        @keyframes confettifall {
            0% {
                transform: translateY(-100vh) rotate(0deg) translateX(0);
            }
            100% {
                transform: translateY(100vh) rotate(var(--spin)) translateX(var(--drift));
            }
        }
    </style>
</head>
<body>
    <div id='effects-container'></div>
    <div class='container'>
        <h1>Authorization Successful</h1>        
        <p class='success-message'>TwitchChat Authentication Token Received!</p>
        <p class='close-note'>(It is safe to close this window and return to the game at this time.)</p>
        
        <div class='revoke-section'>
            <h2>Managing Your Twitch Authorization</h2>
            <p>If you ever need or want to revoke access/remove authorization:</p>
            <ol>
                <li>Visit the official <a href='https://www.twitch.tv/settings/connections' target='_blank'>Twitch Connection Settings</a> page</li>
                <li>Find DerailValleyChatMod under the Other Connections section, lower on the page</li>
                <li>Click Disconnect</li>
            </ol>
            <p>You can always re-authorize the mod by requesting a new Authorization Token through the in-game settings menu.</p>
        </div>
        
        <div class='details-section'>
            <h2>TwitchChatMod Details</h2>
        <div class='link-list'>
            <a href='https://www.nexusmods.com/derailvalley/mods/1069' target='_blank'>Nexus Mods Page</a> |
            <a href='https://github.com/Nightwind416/Derail-Valley-Twitch-Chat-Mod' target='_blank'>GitHub Repository</a> |
            <a href='https://github.com/Nightwind416/Derail-Valley-Twitch-Chat-Mod/issues' target='_blank'>Issues and Suggestions</a>
        </div>
            <p>Your Derail Valley game is now hooked up to Twitch to enable in-game messaging and alerts. Make sure to finish configuring and enabling the mod using the in game settings. Authentication Tokens are typically good for 30 days, but may be revoked or cancelled for any number of reasons. If your Token is not validated, you can request a new one using the same in-game process.</p>
            <h2>Created By</h2>
            <p>Derail Valley TwitchChat Mod developed by Nightwind</p>
            <p>Follow <a href='https://www.twitch.tv/nightwind416' target='_blank'>Nightwind's Twitch Channel</a> to see the mod in action</p>
        </div>

        <div class='donation-section'>
            <h2>Donations</h2>
            <div style='display: flex; justify-content: center; align-items: center; gap: 10px;'>
                <a href='https://ko-fi.com/A0A217PWSY' target='_blank'><img src='https://storage.ko-fi.com/cdn/kofi5.png?v=6' style='height: 40px !important;border:0px;height:36px;' alt='Buy Me a Coffee at ko-fi.com' /></a>
                <a href='https://www.buymeacoffee.com/christophe1xf' target='_blank'><img src='https://cdn.buymeacoffee.com/buttons/v2/default-blue.png' style='height: 60px !important;width: 217px !important;' ></a>
                <a href='https://paypal.me/Nightwind416?country.x=US&locale.x=en_US' target='_blank' class='paypal-button'>PayPal.me Donation</a>                
            </div>
        </div>
        
        <div class='disclaimer-section'>
            <p>This is a third-party modification created by an independent developer and not affiliated with or endorsed by <a href='https://www.altfuture.gg' target='_blank'>Altfuture</a> or the <a href='https://www.derailvalley.com' target='_blank'>Derail Valley</a> development team.</p>
        </div>
    </div>
    
    <script>
        // Send auth token
        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'http://localhost/?' + window.location.hash.substring(1), true);
        xhr.send();
        
        // Holiday effects
        function checkHoliday() {
            const date = new Date();
            const month = date.getMonth();
            const day = date.getDate();
            
            // Show snow in December and January
            if (month === 11 || month === 0) {
                document.body.classList.add('holiday-active');
                const container = document.getElementById('effects-container');
                createSnow(container);
            }
            
            // Add holiday lights around christmas time
            if (month === 11 && day >= 21 && day <= 29) {
                const sections = document.querySelectorAll('.revoke-section, .details-section, .donation-section');
                sections.forEach(section => {
                    section.classList.add('holiday-lights');
                    // Add random twinkle animation delays
                    const lights = section.querySelectorAll('.light');
                    lights.forEach(light => {
                        light.style.setProperty('--twinkle-duration', `${0.5 + Math.random() * 1}s`);
                        light.style.setProperty('--twinkle-delay', `${Math.random() * 1}s`);
                    });
                });
            }
            
            // Show confetti for New Year's
            if ((month === 11 && day === 31) || (month === 0 && day === 1)) {
                const container = document.getElementById('effects-container');
                createConfetti(container);
            }
        }
        
        function createSnow(container) {
            container.className = 'snow-container';
            for (let i = 0; i < 250; i++) {
                const snowflake = document.createElement('div');
                snowflake.className = 'snowflake';
                snowflake.style.left = `${Math.random() * 100}%`;
                snowflake.style.fontSize = `${Math.random() * (2.5 - 0.4) + 0.4}em`;
                snowflake.style.animationDuration = `${15 + Math.random() * 50}s`;
                
                // Set multiple random drift points for more natural movement
                snowflake.style.setProperty('--drift1', `${-50 + Math.random() * 100}px`);
                snowflake.style.setProperty('--drift2', `${-50 + Math.random() * 100}px`);
                snowflake.style.setProperty('--drift3', `${-50 + Math.random() * 100}px`);
                snowflake.style.setProperty('--drift4', `${-50 + Math.random() * 100}px`);
                
                snowflake.innerHTML = '&#10052;';
                container.appendChild(snowflake);
            }
        }
        
        function createConfetti(container) {
            container.className = 'confetti-container';
            const colors = ['#ff0000', '#00ff00', '#0000ff', '#ffff00', '#ff00ff'];
            for (let i = 0; i < 500; i++) {
                const confetti = document.createElement('div');
                confetti.className = 'confetti';
                confetti.style.left = `${Math.random() * 100}%`;
                // Random size between 5px and 15px
                const size = 5 + Math.random() * 10;
                confetti.style.width = `${size}px`;
                confetti.style.height = `${size}px`;
                // Random fall duration between 6s and 12s
                confetti.style.animationDuration = `${6 + Math.random() * 6}s`;
                // Random rotation between 360 and 720 degrees
                confetti.style.setProperty('--spin', `${360 + Math.random() * 360}deg`);
                // Random horizontal drift between -100px and 100px
                confetti.style.setProperty('--drift', `${-100 + Math.random() * 200}px`);
                confetti.style.backgroundColor = colors[Math.floor(Math.random() * colors.length)];
                container.appendChild(confetti);
            }
        }
        
        checkHoliday();
    </script>
</body>
</html>";
    }
}