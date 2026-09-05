using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TwitchChat
{
    /// <summary>
    /// Manages WebSocket connections and message handling for Twitch chat integration.
    /// </summary>
    /// <remarks>
    /// This class is responsible for:
    /// - Establishing and maintaining WebSocket connections to Twitch's EventSub service
    /// - Reassembling fragmented frames into complete JSON messages
    /// - Honouring Twitch-initiated session reconnects without dropping subscriptions
    /// - Handling connection monitoring and automatic reconnection with backoff
    /// - Processing incoming messages and events
    /// </remarks>
    public class WebSocketManager
    {
        /// <summary>Default EventSub endpoint. Twitch sends a keepalive at least this often when the channel is idle.</summary>
        private const string EventSubUri = "wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds=30";

        /// <summary>How long a single receive may wait before the connection is considered dead.</summary>
        private static readonly TimeSpan receiveTimeout = TimeSpan.FromSeconds(45);

        /// <summary>How long without any message before the health monitor declares the connection dead.</summary>
        private static readonly TimeSpan keepaliveGrace = TimeSpan.FromSeconds(45);

        /// <summary>The active WebSocket client instance for Twitch communication.</summary>
        private static ClientWebSocket? webSocketClient;

        /// <summary>The previous socket during a Twitch-initiated session migration. Closed once the new session is welcomed.</summary>
        private static ClientWebSocket? supersededSocket;

        /// <summary>The current session ID from Twitch.</summary>
        private static string session_id = string.Empty;

        /// <summary>Timestamp of the last message received on the active connection.</summary>
        public static DateTime lastKeepaliveTime = DateTime.UtcNow;

        /// <summary>Indicates whether the connection is currently healthy.</summary>
        private static bool isConnectionHealthy = false;

        /// <summary>Timer for monitoring connection health.</summary>
        private static Timer? connectionMonitorTimer;

        /// <summary>Public accessor for connection health status.</summary>
        public static bool IsConnectionHealthy => isConnectionHealthy;

        public static string lastMessageType = "None";
        public static string lastChatMessage = "No messages received";
        public static DateTime lastTypeReceivedTime = DateTime.UtcNow;

        private static readonly SemaphoreSlim reconnectLock = new(1, 1);
        private static int reconnectAttempts = 0;
        private static readonly int maxReconnectAttempts = 8;
        private static readonly TimeSpan reconnectDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan maxReconnectDelay = TimeSpan.FromSeconds(60);

        /// <summary>True from a user-initiated connect until a user-initiated disconnect. Automatic reconnects only happen while this is set.</summary>
        private static bool userWantsConnection = false;

        /// <summary>Whether the next successful subscription should send the connect chat message. Suppressed for automatic reconnects.</summary>
        private static bool announceOnWelcome = false;

        /// <summary>
        /// Establishes a WebSocket connection to Twitch's EventSub service.
        /// </summary>
        /// <returns>A task representing the asynchronous connection operation.</returns>
        public static async Task ConnectToWebSocket()
        {
            string methodName = "ConnectToWebSocket";

            if (string.IsNullOrEmpty(Settings.Instance.EncodedOAuthToken))
            {
                Main.LogEntry(methodName, "No Twitch account connected. Cannot attempt connection to WebSocket.");
                NotificationManager.SetVariable("alertMessage", "Connect your Twitch account first, from the Authentication panel.");
                return;
            }

            if (webSocketClient?.State == WebSocketState.Open)
            {
                Main.LogEntry(methodName, "WebSocket is already open. Cannot attempt connection to WebSocket.");
                NotificationManager.SetVariable("alertMessage", "WebSocket is already open. Cannot attempt connection to WebSocket.");
                return;
            }

            // The chat subscription needs the broadcaster's numeric ID. Look it up if the token was never validated this session.
            if (string.IsNullOrEmpty(TwitchEventHandler.user_id))
            {
                Main.LogEntry(methodName, "User ID not yet known, looking it up before connecting.");
                try
                {
                    await TwitchEventHandler.GetUserID();
                }
                catch (Exception ex)
                {
                    Main.LogEntry(methodName, $"User ID lookup failed: {ex.Message}");
                }

                if (string.IsNullOrEmpty(TwitchEventHandler.user_id))
                {
                    NotificationManager.SetVariable("alertMessage", "Could not look up your Twitch user ID. Validate your token and try again.");
                    return;
                }
            }

            userWantsConnection = true;
            announceOnWelcome = true;
            reconnectAttempts = 0;
            await OpenSocketAsync(new Uri(EventSubUri), isSessionMigration: false);
        }

        /// <summary>
        /// Opens a new socket to the given URI and starts its receive loop.
        /// </summary>
        /// <param name="serverUri">EventSub endpoint to connect to.</param>
        /// <param name="isSessionMigration">True when following a Twitch session_reconnect request, in which case the old socket is kept open until the new session is welcomed.</param>
        /// <returns>True if the socket connected.</returns>
        private static async Task<bool> OpenSocketAsync(Uri serverUri, bool isSessionMigration)
        {
            string methodName = "OpenSocketAsync";
            ClientWebSocket socket = new();
            try
            {
                using CancellationTokenSource connectCts = new(TimeSpan.FromSeconds(20));
                await socket.ConnectAsync(serverUri, connectCts.Token);
                Main.LogEntry(methodName, $"Connected to WebSocket server ({serverUri.Host}){(isSessionMigration ? " for session migration" : "")}.");

                if (isSessionMigration)
                {
                    supersededSocket = webSocketClient;
                }
                else
                {
                    ClientWebSocket? stale = webSocketClient;
                    if (stale != null && stale.State != WebSocketState.Open)
                    {
                        stale.Dispose();
                    }
                }
                webSocketClient = socket;

                lastKeepaliveTime = DateTime.UtcNow;
                isConnectionHealthy = true;
                StartConnectionMonitor();

                _ = Task.Run(() => ReceiveMessages(socket, isSessionMigration));
                return true;
            }
            catch (Exception ex)
            {
                socket.Dispose();
                if (!isSessionMigration)
                {
                    isConnectionHealthy = false;
                }
                Main.LogEntry(methodName, $"Connection error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Starts (or restarts) the periodic connection health check.
        /// </summary>
        private static void StartConnectionMonitor()
        {
            connectionMonitorTimer?.Dispose();
            connectionMonitorTimer = new Timer(CheckConnectionHealth, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
        }

        /// <summary>
        /// Stops the periodic connection health check.
        /// </summary>
        private static void StopConnectionMonitor()
        {
            connectionMonitorTimer?.Dispose();
            connectionMonitorTimer = null;
        }

        /// <summary>
        /// Monitors the health of the WebSocket connection by checking keepalive messages.
        /// </summary>
        /// <param name="state">Timer state object (unused).</param>
        private static void CheckConnectionHealth(object state)
        {
            ClientWebSocket? socket = webSocketClient;
            TimeSpan timeSinceLastKeepalive = DateTime.UtcNow - lastKeepaliveTime;
            bool wasHealthy = isConnectionHealthy;

            isConnectionHealthy = socket != null
                && socket.State == WebSocketState.Open
                && timeSinceLastKeepalive <= keepaliveGrace;

            if (wasHealthy && !isConnectionHealthy)
            {
                Main.LogEntry("CheckConnectionHealth", $"Connection appears to be dead (no message for {timeSinceLastKeepalive.TotalSeconds:F1}s)");
                _ = ReconnectAsync();
            }
            else if (!wasHealthy && isConnectionHealthy)
            {
                Main.LogEntry("CheckConnectionHealth", "Connection restored");
                reconnectAttempts = 0;
            }
        }

        /// <summary>
        /// Attempts to re-establish a lost connection with exponential backoff.
        /// Only one reconnect sequence runs at a time, and none run after a user-initiated disconnect.
        /// </summary>
        private static async Task ReconnectAsync()
        {
            string methodName = "ReconnectAsync";

            if (!await reconnectLock.WaitAsync(0))
            {
                Main.LogEntry(methodName, "Reconnection already in progress");
                return;
            }

            try
            {
                while (userWantsConnection && reconnectAttempts < maxReconnectAttempts)
                {
                    reconnectAttempts++;
                    double backoffSeconds = Math.Min(maxReconnectDelay.TotalSeconds, reconnectDelay.TotalSeconds * Math.Pow(2, reconnectAttempts - 1));
                    Main.LogEntry(methodName, $"Attempting reconnect {reconnectAttempts}/{maxReconnectAttempts} in {backoffSeconds:F0}s");

                    await CloseSocketQuietly(webSocketClient, "Reconnecting");
                    await Task.Delay(TimeSpan.FromSeconds(backoffSeconds));

                    if (!userWantsConnection)
                    {
                        return;
                    }

                    announceOnWelcome = false;
                    if (await OpenSocketAsync(new Uri(EventSubUri), isSessionMigration: false))
                    {
                        return;
                    }
                }

                if (userWantsConnection)
                {
                    Main.LogEntry(methodName, "Max reconnection attempts reached");
                    isConnectionHealthy = false;
                    NotificationManager.SetVariable("alertMessage", "Lost connection to Twitch and could not reconnect. Use the Status panel to reconnect.");
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Reconnection error: {ex.Message}");
                isConnectionHealthy = false;
            }
            finally
            {
                reconnectLock.Release();
            }
        }

        /// <summary>
        /// Sends a close frame on the given socket without any chat announcements.
        /// The socket's receive loop completes the handshake and disposes it.
        /// </summary>
        private static async Task CloseSocketQuietly(ClientWebSocket? socket, string reason)
        {
            if (socket == null)
            {
                return;
            }

            try
            {
                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                {
                    using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, reason, cts.Token);
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry("CloseSocketQuietly", $"Close failed ({reason}): {ex.Message}");
            }
        }

        /// <summary>
        /// Continuously receives and processes messages from one WebSocket connection.
        /// Frames are accumulated until EndOfMessage so long payloads are never truncated.
        /// </summary>
        /// <param name="socket">The socket to read from.</param>
        /// <param name="isSessionMigration">True if this socket was opened in response to a session_reconnect request.</param>
        private static async Task ReceiveMessages(ClientWebSocket socket, bool isSessionMigration)
        {
            string methodName = "ReceiveMessages";
            byte[] buffer = new byte[8192];
            using MemoryStream messageStream = new();
            bool closeRequestedByServer = false;

            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    messageStream.SetLength(0);
                    WebSocketReceiveResult result;
                    do
                    {
                        using CancellationTokenSource cts = new(receiveTimeout);
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }
                        messageStream.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        closeRequestedByServer = true;
                        Main.LogEntry(methodName, $"Server closed the connection: {(socket.CloseStatus.HasValue ? (int)socket.CloseStatus.Value : 0)} {socket.CloseStatusDescription}");
                        break;
                    }

                    if (socket == webSocketClient)
                    {
                        lastKeepaliveTime = DateTime.UtcNow;
                    }

                    string json = Encoding.UTF8.GetString(messageStream.GetBuffer(), 0, (int)messageStream.Length);
                    await HandleMessage(json, socket, isSessionMigration);
                }
                catch (OperationCanceledException)
                {
                    if (socket == webSocketClient)
                    {
                        Main.LogEntry(methodName, "Receive operation timed out");
                    }
                    break;
                }
                catch (WebSocketException wsEx)
                {
                    Main.LogEntry(methodName, $"WebSocket error: {wsEx.Message}");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // A bad or unexpected message should not take the connection down.
                    Main.LogEntry(methodName, $"Error handling message: {ex.Message}");
                }
            }

            if (closeRequestedByServer)
            {
                await CloseSocketQuietly(socket, "Acknowledging close");
            }

            bool wasCurrent = socket == webSocketClient;
            string closeInfo = socket.CloseStatus.HasValue
                ? $"code {(int)socket.CloseStatus.Value}: {socket.CloseStatusDescription}"
                : "no close status";
            Main.LogEntry(methodName, $"WebSocket connection closed ({closeInfo}){(wasCurrent ? "" : " [superseded connection]")}.");

            if (socket == supersededSocket)
            {
                supersededSocket = null;
            }
            socket.Dispose();

            if (wasCurrent)
            {
                isConnectionHealthy = false;
                if (userWantsConnection)
                {
                    Main.LogEntry(methodName, "Connection lost, scheduling reconnect.");
                    _ = ReconnectAsync();
                }
            }
        }

        /// <summary>
        /// Dispatches one complete EventSub message.
        /// </summary>
        private static async Task HandleMessage(string json, ClientWebSocket socket, bool isSessionMigration)
        {
            string methodName = "HandleMessage";
            JObject message = JObject.Parse(json);
            string messageType = (string?)message.SelectToken("metadata.message_type") ?? "unknown";
            lastMessageType = messageType;
            lastTypeReceivedTime = DateTime.UtcNow;

            switch (messageType)
            {
                case "session_welcome":
                    session_id = (string?)message.SelectToken("payload.session.id") ?? string.Empty;
                    Main.LogEntry(methodName, $"Session ID: {session_id}");
                    reconnectAttempts = 0;
                    if (isSessionMigration)
                    {
                        // Twitch carries existing subscriptions over to the new session; only the old socket needs closing.
                        Main.LogEntry(methodName, "Session migrated to new connection, closing the old one.");
                        ClientWebSocket? old = supersededSocket;
                        supersededSocket = null;
                        await CloseSocketQuietly(old, "Session migrated");
                    }
                    else
                    {
                        Main.LogEntry(methodName, "TwitchChat connected to WebSocket");
                        await RegisterWebbSocketChatEvent();
                    }
                    break;

                case "notification":
                    string userName = (string?)message.SelectToken("payload.event.chatter_user_name") ?? string.Empty;
                    string chatMessage = (string?)message.SelectToken("payload.event.message.text") ?? string.Empty;
                    lastChatMessage = $"{userName}: {chatMessage}";
                    NotificationManager.HandleNotification(message);
                    break;

                case "session_keepalive":
                    if (socket == webSocketClient)
                    {
                        lastKeepaliveTime = DateTime.UtcNow;
                        isConnectionHealthy = true;
                    }
                    Main.LogEntry(methodName, "Received keepalive message.");
                    break;

                case "session_reconnect":
                    string reconnectUrl = (string?)message.SelectToken("payload.session.reconnect_url") ?? string.Empty;
                    if (socket != webSocketClient)
                    {
                        Main.LogEntry(methodName, "Ignoring reconnect request on a superseded connection.");
                    }
                    else if (string.IsNullOrEmpty(reconnectUrl))
                    {
                        Main.LogEntry(methodName, "Reconnect request had no URL; the health monitor will handle it.");
                    }
                    else
                    {
                        Main.LogEntry(methodName, "Twitch requested a session reconnect, migrating.");
                        _ = OpenSocketAsync(new Uri(reconnectUrl), isSessionMigration: true);
                    }
                    break;

                case "revocation":
                    string status = (string?)message.SelectToken("payload.subscription.status") ?? "unknown";
                    Main.LogEntry(methodName, $"Chat subscription revoked: {status}");
                    NotificationManager.SetVariable("alertMessage", $"Twitch revoked the chat subscription ({status}). Re-authorize the mod and reconnect.");
                    break;

                default:
                    Main.LogEntry(methodName, $"Unknown message type: {messageType}");
                    break;
            }
        }

        /// <summary>
        /// Registers for Twitch chat events using the WebSocket connection.
        /// </summary>
        private static async Task RegisterWebbSocketChatEvent()
        {
            string methodName = "RegisterWebbSocketChatEvent";
            if (webSocketClient?.State != WebSocketState.Open)
            {
                Main.LogEntry(methodName, "WebSocket is not open. Cannot send subscription request.");
                return;
            }

            string jsonBody = JsonConvert.SerializeObject(new
            {
                type = "channel.chat.message",
                version = "1",
                condition = new
                {
                    broadcaster_user_id = TwitchEventHandler.user_id,
                    user_id = TwitchEventHandler.user_id
                },
                transport = new
                {
                    method = "websocket",
                    session_id
                }
            });
            StringContent content = new(jsonBody, Encoding.UTF8, "application/json");

            string access_token = OAuthTokenManager.GetAccessToken();

            TwitchEventHandler.httpClient.DefaultRequestHeaders.Clear();
            TwitchEventHandler.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            TwitchEventHandler.httpClient.DefaultRequestHeaders.Add("Client-ID", TwitchEventHandler.GetClientId());
            HttpResponseMessage response = await TwitchEventHandler.httpClient.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Main.LogEntry(methodName, $"Failed to subscribe to channel.chat.message. Status: {response.StatusCode}, Error: {errorContent}");
                NotificationManager.SetVariable("alertMessage", $"Twitch chat subscription failed ({(int)response.StatusCode}). Check the debug log.");
                return;
            }

            Main.LogEntry(methodName, "Subscribed to channel.chat.message.");
            if (announceOnWelcome && Settings.Instance.connectMessageEnabled && !string.IsNullOrEmpty(Settings.Instance.connectMessage))
            {
                await TwitchEventHandler.SendMessage(Settings.Instance.connectMessage);
            }
            announceOnWelcome = false;
        }

        /// <summary>
        /// Gracefully disconnects from the WebSocket server at the user's request.
        /// No automatic reconnect follows.
        /// </summary>
        public static async Task DisconnectFromWebSocket()
        {
            string methodName = "DisconnectFromWebSocket";

            userWantsConnection = false;
            announceOnWelcome = false;
            StopConnectionMonitor();

            ClientWebSocket? socket = webSocketClient;
            if (socket != null && socket.State == WebSocketState.Open)
            {
                if (Settings.Instance.disconnectMessageEnabled && !string.IsNullOrEmpty(Settings.Instance.disconnectMessage))
                {
                    await TwitchEventHandler.SendMessage(Settings.Instance.disconnectMessage);
                    // Small delay to ensure the message is sent before closing
                    await Task.Delay(500);
                }

                await CloseSocketQuietly(socket, "Closing");
                Main.LogEntry(methodName, "Disconnected from WebSocket server.");
                AutomatedMessages.StopAndClearTimers();
            }
            else
            {
                Main.LogEntry(methodName, "WebSocket was not open.");
            }

            isConnectionHealthy = false;
        }
    }
}
