// using UnityModManagerNet;
// using System.IO;
// using UnityEngine;
// using UnityEditor;

// namespace TwitchChat
// {
//     public class XmlSettings : UnityModManager.ModSettings, IDrawable
//     {
//         public string twitchUsername = string.Empty;
//         public string twitchToken = string.Empty;
//         public string twitchChannel = string.Empty;
//         public int messageDuration = 20;
//         public string welcomeMessage = string.Empty;
//         public string infoMessage = string.Empty;
//         public string commandMessage = string.Empty;public CredentialsSettings Credentials = new();
//         public StandardMessagesSettings StandardMessages = new();
//         public DispatcherChatSettings DispatcherChat = new();
//         public bool timedMessagesActive = false;
//         public TimedMessagesSettings TimedMessages = new();

//         public override void Save(UnityModManager.ModEntry entry) {
//             Save(this, entry);
//         }

//         public void OnChange() { }

//         public override string GetPath(UnityModManager.ModEntry modEntry) {
//             return Path.Combine(modEntry.Path, "Settings.xml");
//         }

//         public void OnGUI()
//         {
//             DrawButtons();
//             DrawCredentialsSettings();
//             DrawMessageDuration();
//             DrawStandardMessagesSettings();
//             DrawDispatcherChatSettings();
//             DrawTimedMessagesSettings();
//         }

//         private void DrawButtons()
//         {
//             if (GUILayout.Button("Connect to Twitch", GUILayout.Width(200))) {
//                 Main.ConnectToTwitch();
//             }
//             if (GUILayout.Button("Disconnect from Twitch", GUILayout.Width(200))) {
//                 Main.DisconnectFromTwitch();
//             }
//         }

//         private void DrawCredentialsSettings()
//         {
//             GUILayout.Label("Twitch Credentials", EditorStyles.boldLabel);
//             Credentials.twitchUsername = EditorGUILayout.TextField("Twitch Username", Credentials.twitchUsername);
//             Credentials.twitchToken = EditorGUILayout.TextField("Twitch Token", Credentials.twitchToken);
//             Credentials.twitchChannel = EditorGUILayout.TextField("Twitch Channel", Credentials.twitchChannel);
//         }

//         private void DrawMessageDuration()
//         {
//             messageDuration = EditorGUILayout.IntSlider("Message Display Duration (in seconds, 60 max)", messageDuration, 0, 60);
//         }

//         private void DrawStandardMessagesSettings()
//         {
//             GUILayout.Label("Standard Messages", EditorStyles.boldLabel);
//             StandardMessages.welcomeMessageActive = EditorGUILayout.Toggle("Welcome Message Active", StandardMessages.welcomeMessageActive);
//             StandardMessages.welcomeMessage = EditorGUILayout.TextField("Welcome Message", StandardMessages.welcomeMessage);
//             StandardMessages.infoMessageActive = EditorGUILayout.Toggle("Info Message Active", StandardMessages.infoMessageActive);
//             StandardMessages.infoMessage = EditorGUILayout.TextField("Info Message", StandardMessages.infoMessage);
//             StandardMessages.newFollowerMessageActive = EditorGUILayout.Toggle("New Follower Message Active", StandardMessages.newFollowerMessageActive);
//             StandardMessages.newFollowerMessage = EditorGUILayout.TextField("New Follower Message", StandardMessages.newFollowerMessage);
//             StandardMessages.newSubscriberMessageActive = EditorGUILayout.Toggle("New Subscriber Message Active", StandardMessages.newSubscriberMessageActive);
//             StandardMessages.newSubscriberMessage = EditorGUILayout.TextField("New Subscriber Message", StandardMessages.newSubscriberMessage);
//             StandardMessages.commandMessageActive = EditorGUILayout.Toggle("Command Message Active", StandardMessages.commandMessageActive);
//             StandardMessages.commandMessage = EditorGUILayout.TextField("Command Message", StandardMessages.commandMessage);
//         }

//         private void DrawDispatcherChatSettings()
//         {
//             GUILayout.Label("Dispatcher Messages", EditorStyles.boldLabel);
//             DispatcherChat.dispatcherMessageActive = EditorGUILayout.Toggle("Dispatcher Chat Message Active", DispatcherChat.dispatcherMessageActive);
//             DispatcherChat.dispatcherMessage = EditorGUILayout.TextField("Dispatcher Message", DispatcherChat.dispatcherMessage);
//         }

//         private void DrawTimedMessagesSettings()

//         {
//             GUILayout.Label("Timed Messages", EditorStyles.boldLabel);
//             timedMessagesActive = EditorGUILayout.Toggle("Timed Messages Active?", timedMessagesActive);
//             TimedMessages.TimedMessage1Toggle = EditorGUILayout.Toggle("Timed Message 1 Active", TimedMessages.TimedMessage1Toggle);
//             TimedMessages.TimedMessage1 = EditorGUILayout.TextField("Timed Message 1", TimedMessages.TimedMessage1);
//             TimedMessages.TimedMessage1Timer = EditorGUILayout.FloatField("Timed Message 1 Timer", TimedMessages.TimedMessage1Timer);
//             // Repeat for other timed messages...
//         }
//         public static bool LoadSettings()
//         {
//             UnityModManager.Logger.Log("[TwitchChat] Attempting to load settings.");

//             // Load the settings
//             XmlSettings settings = new();
//             CredentialsSettings credentials = new();
//             StandardMessagesSettings messages = new();

//             // Add debug logs to check if settings are loaded correctly
//             UnityModManager.Logger.Log($"[TwitchChat] Loaded Settings: {settings}");
//             UnityModManager.Logger.Log($"[TwitchChat] Loaded CredentialsSettings: {credentials}");
//             UnityModManager.Logger.Log($"[TwitchChat] Loaded StandardMessagesSettings: {messages}");

//             // Log detailed settings values
//             UnityModManager.Logger.Log($"[TwitchChat] Settings - Message Duration: {settings.messageDuration}");
//             UnityModManager.Logger.Log($"[TwitchChat] CredentialsSettings - Username: {credentials.twitchUsername}");
//             UnityModManager.Logger.Log($"[TwitchChat] CredentialsSettings - Token: {credentials.twitchToken}");
//             UnityModManager.Logger.Log($"[TwitchChat] CredentialsSettings - Channel: {credentials.twitchChannel}");
//             UnityModManager.Logger.Log($"[TwitchChat] StandardMessagesSettings - Welcome Message: {messages.welcomeMessage}");
//             UnityModManager.Logger.Log($"[TwitchChat] StandardMessagesSettings - Info Message: {messages.infoMessage}");
//             UnityModManager.Logger.Log($"[TwitchChat] StandardMessagesSettings - Command Message: {messages.commandMessage}");
//             UnityModManager.Logger.Log($"[TwitchChat] StandardMessagesSettings - New Follower Message: {messages.newFollowerMessage}");
//             UnityModManager.Logger.Log($"[TwitchChat] StandardMessagesSettings - New Subscriber Message: {messages.newSubscriberMessage}");
        
//             messageDuration = settings.messageDuration;
//             twitchUsername = credentials.twitchUsername;
//             twitchToken = credentials.twitchToken;
//             twitchChannel = credentials.twitchChannel;
//             welcomeMessage = messages.welcomeMessage;
//             infoMessage = messages.infoMessage;
//             commandMessage = messages.commandMessage;
//             newFollowerMessage = messages.newFollowerMessage;
//             newSubscriberMessage = messages.newSubscriberMessage;
        
//             UnityModManager.Logger.Log($"[TwitchChat] Username: {twitchUsername}");
//             UnityModManager.Logger.Log($"[TwitchChat] Token: {twitchToken}");
//             UnityModManager.Logger.Log($"[TwitchChat] Channel: {twitchChannel}");
//             UnityModManager.Logger.Log($"[TwitchChat] Message Duration: {messageDuration}");
//             UnityModManager.Logger.Log($"[TwitchChat] Welcome Message: {welcomeMessage}");
//             UnityModManager.Logger.Log($"[TwitchChat] Info Message: {infoMessage}");
//             UnityModManager.Logger.Log($"[TwitchChat] Command Message: {commandMessage}");
//             UnityModManager.Logger.Log($"[TwitchChat] New Follower Message: {newFollowerMessage}");
//             UnityModManager.Logger.Log($"[TwitchChat] New Subscriber Message: {newSubscriberMessage}");

//             UnityModManager.Logger.Log("[TwitchChat] Settings loaded successfully.");
        
//             return true;
//         }
//     }

//     public class CredentialsSettings
//     {
//         public string twitchUsername = "TwitchUsernameNotSet";
//         public string twitchChannel = "twitchChannelNotSet";
//         public string twitchToken = "twitchTokenNotSet";
//     }

//     public class StandardMessagesSettings
//     {
//         public bool welcomeMessageActive = true;
//         public string welcomeMessage = "Welcome to my Derail Valley stream!";
//         public bool infoMessageActive = true;
//         public string infoMessage = "Please keep chat clean and respectful. Use !commands to see available commands.";
//         public bool newFollowerMessageActive = true;
//         public string newFollowerMessage = "Welcome to the crew!";
//         public bool newSubscriberMessageActive = true;
//         public string newSubscriberMessage = "Thank you for subscribing!";
//         public bool commandMessageActive = true;
//         public string commandMessage = "!info !commands";
//     }

//     public class DispatcherChatSettings
//     {
//         public bool dispatcherMessageActive = false;
//         public string dispatcherMessage = "MessageNotSet";
//     }

//     public class TimedMessagesSettings
//     {
//         public bool TimedMessage1Toggle = false;
//         public string TimedMessage1 = "MessageNotSet";
//         public float TimedMessage1Timer = 0;
//         // Repeat for other timed messages...
//     }
// }