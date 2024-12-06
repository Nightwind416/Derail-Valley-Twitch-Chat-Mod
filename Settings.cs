using UnityModManagerNet;
using System.IO;
using UnityEngine;

namespace TwitchChat
{
    [DrawFields(DrawFieldMask.Public)]
    public class XmlSettings : UnityModManager.ModSettings, IDrawable
    {
        public static void DrawButtons()
        {
            if (GUILayout.Button("Connect to Twitch", GUILayout.Width(200))) {
                Main.ConnectToTwitch();
            }
            if (GUILayout.Button("Disconnect from Twitch", GUILayout.Width(200))) {
                Main.DisconnectFromTwitch();
            }
        }
        
        [Draw("Twitch Credentials", Collapsible = true)]
        public CredentialsSettings Credentials = new();
        
        [Draw("Message Display Duration (in seconds, 60 max)", Precision = 0, Min = 0, Max = 60)]
        public int messageDuration = 20;
        
        [Draw("Standard Messages", Collapsible = true)]
        public StandardMessagesSettings StandardMessages = new();

        [Draw("Dispatcher Messages", Collapsible = true)]
        public DispatcherChatSettings DispatcherChat = new();
        
        [Draw("Timed Messages Active?")]
        public bool timedMessagesActive = false;
        
        [Draw("Timed Messages", Collapsible = true)]
        public TimedMessagesSettings TimedMessages = new();

        public override void Save(UnityModManager.ModEntry entry) {
            Save(this, entry);
        }

        public void OnChange() { }

        public override string GetPath(UnityModManager.ModEntry modEntry) {
            return Path.Combine(modEntry.Path, "Settings.xml");
        }
    }

    [DrawFields(DrawFieldMask.Public)]
    public class CredentialsSettings
    {
        [Draw("Twitch Username")]
        public string twitchUsername = "TwitchUsernameNotSet";
        
        [Draw("Credentials Token", Collapsible = true)]
        public CredentialsTokenSettings token = new();

        [Draw("Twitch Channel")]
        public string twitchChannel = "twitchChannelNotSet";
    }
    
    [DrawFields(DrawFieldMask.Public)]
    public class CredentialsTokenSettings
    {
        [Draw("Twitch Token")]
        public string twitchToken = "twitchTokenNotSet";
    }
    
    [DrawFields(DrawFieldMask.Public)]
    public class StandardMessagesSettings
    {
        public bool welcomeMessageActive = true;
        public string welcomeMessage = "Welcome to my Derail Valley stream!";
        public bool infoMessageActive = true;
        public string infoMessage = "Please keep chat clean and respectful. Use !commands to see available commands.";
        public bool newFollowerMessageActive = true;
        public string newFollowerMessage = "Welcome to the crew!";
        public bool newSubscriberMessageActive = true;
        public string newSubscriberMessage = "Thank you for subscribing!";
        public bool commandMessageActive = true;
        public string commandMessage = "!info !commands";
    }

    [DrawFields(DrawFieldMask.Public)]
    public class DispatcherChatSettings
    {
        [Draw("Dispatcher Chat Message Active?")]
        public bool dispatcherMessageActive = false;

        [Draw("Dispatcher Message")]
        public string dispatcherMessage = "MessageNotSet";
    }

    [DrawFields(DrawFieldMask.Public)]
    public class TimedMessagesSettings
    {
        public bool TimedMessage1Toggle = false;
        public string TimedMessage1 = "MessageNotSet";
        public float TimedMessage1Timer = 0;
    
        public bool TimedMessage2Toggle = false;
        public string TimedMessage2 = "MessageNotSet";
        public float TimedMessage2Timer = 0;
    
        public bool TimedMessage3Toggle = false;
        public string TimedMessage3 = "MessageNotSet";
        public float TimedMessage3Timer = 0;
    
        public bool TimedMessage4Toggle = false;
        public string TimedMessage4 = "MessageNotSet";
        public float TimedMessage4Timer = 0;
    
        public bool TimedMessage5Toggle = false;
        public string TimedMessage5 = "MessageNotSet";
        public float TimedMessage5Timer = 0;
    
        public bool TimedMessage6Toggle = false;
        public string TimedMessage6 = "MessageNotSet";
        public float TimedMessage6Timer = 0;
    
        public bool TimedMessage7Toggle = false;
        public string TimedMessage7 = "MessageNotSet";
        public float TimedMessage7Timer = 0;
    
        public bool TimedMessage8Toggle = false;
        public string TimedMessage8 = "MessageNotSet";
        public float TimedMessage8Timer = 0;
    
        public bool TimedMessage9Toggle = false;
        public string TimedMessage9 = "MessageNotSet";
        public float TimedMessage9Timer = 0;
    
        public bool TimedMessage10Toggle = false;
        public string TimedMessage10 = "MessageNotSet";
        public float TimedMessage10Timer = 0;
    }
}