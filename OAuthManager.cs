using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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

        /// <summary>
        /// Plain-language description of each requested scope, shown on the in-game consent screen.
        /// Kept under about 55 characters so the panel renders them without truncating.
        /// </summary>
        public static readonly string[] ScopeExplanations =
        [
            "Read your chat messages, to show them in game",
            "Send chat messages as you (commands, timers)",
            "Send whispers as you, for whisper replies only",
            "Post announcements, for blue timed messages"
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
        /// Returns the stored access token in plain text, or an empty string if none is held
        /// or the stored value cannot be decoded.
        /// </summary>
        /// <remarks>
        /// The result is a live credential. Never write it to a log, a status message or the UI.
        /// </remarks>
        public static string GetAccessToken()
        {
            return TokenStore.Unprotect(Settings.Instance.EncodedOAuthToken);
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

        /// <summary>The short code the user types on twitch.tv/activate, or empty when none is pending.</summary>
        public static string UserCode { get; private set; } = string.Empty;

        /// <summary>The page the user opens to enter <see cref="UserCode"/>.</summary>
        public static string VerificationUri { get; private set; } = ActivationPage;

        /// <summary>When the pending <see cref="UserCode"/> stops being accepted.</summary>
        public static DateTime UserCodeExpiresUtc { get; private set; } = DateTime.MinValue;

        /// <summary>The page users are told to open. Shown in game, so it is kept short and typeable.</summary>
        public const string ActivationPage = "twitch.tv/activate";

        /// <summary>Cancels the authorization currently being polled for, if any.</summary>
        private static CancellationTokenSource? pollingCts;

        /// <summary>
        /// Starts the OAuth device code flow.
        /// </summary>
        /// <remarks>
        /// Twitch hands back a short code which the mod shows on the in-game panel. The user types
        /// it at twitch.tv/activate on any device - a phone works, so a VR player does not have to
        /// take the headset off - while this method polls Twitch for the resulting token.
        /// Nothing is opened on the player's PC and no local web server is involved.
        /// </remarks>
        /// <returns>An asynchronous task representing the authorization.</returns>
        public static async Task StartDeviceAuthorization()
        {
            string methodName = "StartDeviceAuthorization";

            // Abandon any authorization still being polled for, so two pending codes cannot race.
            CancelDeviceAuthorization();

            using var cts = new CancellationTokenSource();
            pollingCts = cts;

            try
            {
                SetPhase(AuthPhase.AwaitingUser, "Asking Twitch for a code...");
                Main.LogEntry(methodName, $"Requesting device code for scopes: {RequestedScopes}");

                using var client = new HttpClient();
                var request = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", TwitchEventHandler.GetClientId()),
                    new KeyValuePair<string, string>("scopes", RequestedScopes)
                });

                var response = await client.PostAsync("https://id.twitch.tv/oauth2/device", request);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Main.LogEntry(methodName, $"Device code request failed: {response.StatusCode} {ReadErrorMessage(body)}");
                    SetPhase(AuthPhase.Failed, "Could not reach Twitch. Check your connection and try again.");
                    return;
                }

                JObject payload = JObject.Parse(body);
                string deviceCode = (string?)payload["device_code"] ?? string.Empty;
                UserCode = (string?)payload["user_code"] ?? string.Empty;
                int interval = (int?)payload["interval"] ?? 5;
                int expiresIn = (int?)payload["expires_in"] ?? 1800;

                // Twitch returns a verification_uri that already carries the code. The in-game panel
                // shows the bare activation page instead, because the user is reading it off a
                // screen and typing it on a phone; the full URL would be unusable there.
                VerificationUri = ActivationPage;
                UserCodeExpiresUtc = DateTime.UtcNow.AddSeconds(expiresIn);

                if (string.IsNullOrEmpty(deviceCode) || string.IsNullOrEmpty(UserCode))
                {
                    Main.LogEntry(methodName, "Device code response did not contain a code.");
                    SetPhase(AuthPhase.Failed, "Twitch did not return a code. Please try again.");
                    return;
                }

                Main.LogEntry(methodName, $"Got user code, valid for {expiresIn}s, polling every {interval}s.");
                SetPhase(AuthPhase.AwaitingUser, $"Enter {UserCode} at {ActivationPage}");

                await PollForDeviceToken(client, deviceCode, interval, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Main.LogEntry(methodName, "Authorization cancelled.");
                ClearPendingCode();
                SetPhase(AuthPhase.NotConnected, "Not connected");
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Device authorization failed: {ex.Message}");
                ClearPendingCode();
                SetPhase(AuthPhase.Failed, "Authorization failed. Please try again.");
            }
            finally
            {
                if (ReferenceEquals(pollingCts, cts))
                {
                    pollingCts = null;
                }
            }
        }

        /// <summary>
        /// Polls Twitch until the user approves the pending code, it expires, or the wait is cancelled.
        /// </summary>
        private static async Task PollForDeviceToken(HttpClient client, string deviceCode, int interval, CancellationToken cancellation)
        {
            string methodName = "PollForDeviceToken";

            while (!cancellation.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(interval), cancellation);

                if (DateTime.UtcNow >= UserCodeExpiresUtc)
                {
                    Main.LogEntry(methodName, "The user code expired before it was entered.");
                    ClearPendingCode();
                    SetPhase(AuthPhase.Failed, "The code expired. Please try again.");
                    return;
                }

                var request = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", TwitchEventHandler.GetClientId()),
                    new KeyValuePair<string, string>("scopes", RequestedScopes),
                    new KeyValuePair<string, string>("device_code", deviceCode),
                    new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code")
                });

                var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", request);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ClearPendingCode();
                    if (StoreTokenResponse(body, methodName))
                    {
                        await IdentifyAccount();
                    }
                    return;
                }

                // Twitch answers 400 with "authorization_pending" until the user finishes on their
                // end. Anything else means this code will never work, so stop rather than spin.
                string message = ReadErrorMessage(body);
                if (message.IndexOf("authorization_pending", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                Main.LogEntry(methodName, $"Polling stopped: {response.StatusCode} {message}");
                ClearPendingCode();
                SetPhase(AuthPhase.Failed, "Authorization was not completed. Please try again.");
                return;
            }

            cancellation.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Marks the account as connected and looks up which account it is.
        /// </summary>
        /// <remarks>
        /// The token is already good by the time this runs, so a failed lookup is reported as a
        /// connection without a name rather than as a failed authorization.
        /// </remarks>
        private static async Task IdentifyAccount()
        {
            SetPhase(AuthPhase.Connected, "Connected");

            try
            {
                await TwitchEventHandler.GetUserID();
            }
            catch (Exception ex)
            {
                Main.LogEntry("IdentifyAccount", $"Could not look up the connected account: {ex.Message}");
                SetPhase(AuthPhase.Connected, "Connected (account name unavailable)");
                return;
            }

            SetPhase(AuthPhase.Connected, string.IsNullOrEmpty(Settings.Instance.twitchUsername)
                ? "Connected"
                : $"Connected as {Settings.Instance.twitchUsername}");
        }

        /// <summary>
        /// Stops polling for a pending authorization and forgets the code shown on screen.
        /// </summary>
        public static void CancelDeviceAuthorization()
        {
            try
            {
                pollingCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The authorization finished on its own between the check and the cancel.
            }

            pollingCts = null;
            ClearPendingCode();
        }

        /// <summary>Forgets the code currently displayed to the user.</summary>
        private static void ClearPendingCode()
        {
            UserCode = string.Empty;
            UserCodeExpiresUtc = DateTime.MinValue;
        }

        /// <summary>
        /// Saves the access and refresh tokens out of a Twitch token response.
        /// </summary>
        /// <returns>True when an access token was found and stored.</returns>
        private static bool StoreTokenResponse(string body, string methodName)
        {
            JObject payload = JObject.Parse(body);
            string accessToken = (string?)payload["access_token"] ?? string.Empty;
            string refreshToken = (string?)payload["refresh_token"] ?? string.Empty;

            if (string.IsNullOrEmpty(accessToken))
            {
                Main.LogEntry(methodName, "Token response contained no access token.");
                SetPhase(AuthPhase.Failed, "Twitch did not return a token. Please try again.");
                return false;
            }

            // Never log either token, nor the response body that carries them.
            Settings.Instance.EncodedOAuthToken = TokenStore.Protect(accessToken);
            Settings.Instance.EncodedRefreshToken = TokenStore.Protect(refreshToken);
            Settings.Save(Settings.Instance, Main.ModEntry);
            Main.LogEntry(methodName, $"Stored a new access token{(string.IsNullOrEmpty(refreshToken) ? "" : " and refresh token")}.");
            return true;
        }

        /// <summary>
        /// Pulls the "message" field out of a Twitch error body, falling back to the raw body.
        /// </summary>
        private static string ReadErrorMessage(string body)
        {
            try
            {
                return (string?)JObject.Parse(body)["message"] ?? body;
            }
            catch (Exception)
            {
                return body;
            }
        }

        /// <summary>
        /// Exchanges the stored refresh token for a fresh access token.
        /// </summary>
        /// <remarks>
        /// Twitch refresh tokens are single use, so the replacement returned here is saved over the
        /// old one. A refresh token that has gone unused for 30 days is rejected, which leaves the
        /// user needing to authorize again.
        /// </remarks>
        /// <returns>True when a new access token was obtained.</returns>
        public static async Task<bool> TryRefreshAccessToken()
        {
            string methodName = "TryRefreshAccessToken";
            string refreshToken = TokenStore.Unprotect(Settings.Instance.EncodedRefreshToken);

            if (string.IsNullOrEmpty(refreshToken))
            {
                Main.LogEntry(methodName, "No refresh token stored.");
                return false;
            }

            try
            {
                Main.LogEntry(methodName, "Refreshing the access token...");
                using var client = new HttpClient();
                var request = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", TwitchEventHandler.GetClientId()),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken)
                });

                var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", request);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Main.LogEntry(methodName, $"Refresh rejected: {response.StatusCode} {ReadErrorMessage(body)}");
                    return false;
                }

                return StoreTokenResponse(body, methodName);
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Refresh failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks the stored token with Twitch, refreshing it once if it has expired.
        /// </summary>
        /// <remarks>
        /// Access tokens last about four hours, so an expired token during a long session is the
        /// normal case rather than an error. When the refresh succeeds the user sees nothing; when
        /// it fails they are told to reconnect, and the dead token is discarded.
        /// </remarks>
        /// <returns>An asynchronous task representing the token validation operation.</returns>
        public static async Task ValidateAuthToken()
        {
            string methodName = "ValidateAuthToken";

            if (string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken))
            {
                Main.LogEntry(methodName, "No saved token found.");
                SetPhase(AuthPhase.NotConnected, "Not connected");
                return;
            }

            SetPhase(AuthPhase.AwaitingUser, "Checking your connection...");

            if (await ValidateOnce(methodName))
            {
                return;
            }

            // Either the token expired or it was revoked. A refresh distinguishes the two.
            Main.LogEntry(methodName, "Stored token was rejected. Attempting a refresh...");
            SetPhase(AuthPhase.AwaitingUser, "Reconnecting...");

            if (await TryRefreshAccessToken() && await ValidateOnce(methodName))
            {
                return;
            }

            Main.LogEntry(methodName, "Could not restore the connection. Clearing the stored token.");
            Settings.Instance.EncodedOAuthToken = string.Empty;
            Settings.Instance.EncodedRefreshToken = string.Empty;
            Settings.Save(Settings.Instance, Main.ModEntry);
            SetPhase(AuthPhase.NotConnected, "Sign in again to reconnect");
        }

        /// <summary>
        /// Asks Twitch whether the stored access token is currently good.
        /// </summary>
        /// <returns>
        /// True when Twitch accepted the token. False when it was rejected, or when Twitch could
        /// not be reached after several attempts.
        /// </returns>
        private static async Task<bool> ValidateOnce(string methodName)
        {
            string accessToken = GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                return false;
            }

            TwitchEventHandler.httpClient.DefaultRequestHeaders.Clear();
            TwitchEventHandler.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            TwitchEventHandler.httpClient.DefaultRequestHeaders.Add("Client-Id", TwitchEventHandler.GetClientId());

            const int retryCount = 3;
            for (int attempt = 0; attempt < retryCount; attempt++)
            {
                try
                {
                    var response = await TwitchEventHandler.httpClient.GetAsync("https://id.twitch.tv/oauth2/validate");
                    Main.LogEntry(methodName, $"Validate response status code: {response.StatusCode}");

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        await IdentifyAccount();
                        return true;
                    }

                    // Twitch gave a definite answer, so retrying the same token is pointless.
                    return false;
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    Main.LogEntry(methodName, $"Could not reach Twitch ({ex.GetType().Name}): {ex.Message}");
                    if (attempt < retryCount - 1)
                    {
                        SetPhase(AuthPhase.AwaitingUser, "Retrying...");
                        await Task.Delay(2000);
                    }
                    else
                    {
                        SetPhase(AuthPhase.Failed, "Could not reach Twitch. Check your connection.");
                    }
                }
                catch (Exception ex)
                {
                    Main.LogEntry(methodName, $"Unexpected error validating the token: {ex.Message}");
                    SetPhase(AuthPhase.Failed, "Unexpected error. See the debug log.");
                    return false;
                }
            }

            return false;
        }
    }
}
