using UnityModManagerNet;
using System.IO;
using UnityEngine;

namespace TwitchChat
{
    [DrawFields(DrawFieldMask.Public)]
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public static async void DrawButtons()
        {
            if (GUILayout.Button("Connect to Twitch", GUILayout.Width(200))) {
                Main.ConnectToTwitch();
            }
            if (GUILayout.Button("Connection Status", GUILayout.Width(200))) {
                await TwitchEventHandler.ConnectionStatus();
            }
            if (GUILayout.Button("Get User ID", GUILayout.Width(200))) {
                await TwitchEventHandler.GetUserID();
            }
            if (GUILayout.Button("Join Channel", GUILayout.Width(200))) {
                await TwitchEventHandler.JoinChannel();
            }
            if (GUILayout.Button("Send test message", GUILayout.Width(200))) {
                await TwitchEventHandler.SendMessage(message: "Test message from Derail Valley");
            }
        }

        public static string twitchUsername = "TwitchUsernameNotSet";
        public static string twitchChannel = "TwitchChannelNotSet";
        public static readonly string client_id = "ClientIDNotSet";
        public static readonly string client_secret = "ClientSecretNotSet";
        
        // public int messageDuration = 20;
        // public bool welcomeMessageActive = true;
        // public string welcomeMessage = "Welcome to my Derail Valley stream!";
        // public bool infoMessageActive = true;
        // public string infoMessage = "Please keep chat clean and respectful. Use !commands to see available commands.";
        // public bool newFollowerMessageActive = true;
        // public string newFollowerMessage = "Welcome to the crew!";
        // public bool newSubscriberMessageActive = true;
        // public string newSubscriberMessage = "Thank you for subscribing!";
        // public bool commandMessageActive = true;
        // public string commandMessage = "!info !commands";
        // public bool dispatcherMessageActive = false;
        // public string dispatcherMessage = "MessageNotSet";
        // public bool timedMessagesActive = false;

        // public bool TimedMessage1Toggle = false;
        // public string TimedMessage1 = "MessageNotSet";
        // public float TimedMessage1Timer = 0;
    
        // public bool TimedMessage2Toggle = false;
        // public string TimedMessage2 = "MessageNotSet";
        // public float TimedMessage2Timer = 0;
    
        // public bool TimedMessage3Toggle = false;
        // public string TimedMessage3 = "MessageNotSet";
        // public float TimedMessage3Timer = 0;
    
        // public bool TimedMessage4Toggle = false;
        // public string TimedMessage4 = "MessageNotSet";
        // public float TimedMessage4Timer = 0;
    
        // public bool TimedMessage5Toggle = false;
        // public string TimedMessage5 = "MessageNotSet";
        // public float TimedMessage5Timer = 0;
    
        // public bool TimedMessage6Toggle = false;
        // public string TimedMessage6 = "MessageNotSet";
        // public float TimedMessage6Timer = 0;
    
        // public bool TimedMessage7Toggle = false;
        // public string TimedMessage7 = "MessageNotSet";
        // public float TimedMessage7Timer = 0;
    
        // public bool TimedMessage8Toggle = false;
        // public string TimedMessage8 = "MessageNotSet";
        // public float TimedMessage8Timer = 0;
    
        // public bool TimedMessage9Toggle = false;
        // public string TimedMessage9 = "MessageNotSet";
        // public float TimedMessage9Timer = 0;
    
        // public bool TimedMessage10Toggle = false;
        // public string TimedMessage10 = "MessageNotSet";
        // public float TimedMessage10Timer = 0;

        public override void Save(UnityModManager.ModEntry entry) {
            Save(this, entry);
        }

        public void OnChange() { }

        public override string GetPath(UnityModManager.ModEntry modEntry) {
            return Path.Combine(modEntry.Path, "Settings.xml");
        }
    }
}