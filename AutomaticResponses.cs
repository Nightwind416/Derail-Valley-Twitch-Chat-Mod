using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Xml;

namespace TwitchChat
{
    public class AutomaticResponses
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
        public bool dispatcherMessageActive = false;
        public string dispatcherMessage = "MessageNotSet";
        public bool timedMessagesActive = false;
        public bool TimedMessage1Toggle = false;
        public string TimedMessage1 = "MessageNotSet";
        public float TimedMessage1Timer = 0;
        private static async void TimedMessages()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                // Check if the file exists
                if (!File.Exists(Main.settingsFile))
                {
                    Main.LogEntry(methodName, $"Settings file not found: {Main.settingsFile}");
                    return;
                }

                // Load the XML document
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(Main.settingsFile);

                // Extract messages and their timers
                var messages = new Dictionary<string, int>();

                for (int i = 1; i <= 10; i++)
                {
                    string activeKey = $"timedMessage{i}Toggle";
                    string messageKey = $"timedMessage{i}";
                    string timerKey = $"{messageKey}Timer";
                
                    var activeNode = xmlDoc.SelectSingleNode($"//Settings/{activeKey}");
                    var messageNode = xmlDoc.SelectSingleNode($"//Settings/{messageKey}");
                    var timerNode = xmlDoc.SelectSingleNode($"//Settings/{timerKey}");
                
                    bool isActive = activeNode != null && bool.TryParse(activeNode.InnerText, out bool active) && active;
                
                    if (isActive && messageNode != null && timerNode != null && int.TryParse(timerNode.InnerText, out int timerValue) && timerValue > 0)
                    {
                        messages.Add(messageNode.InnerText, timerValue);
                        Main.LogEntry(methodName, $"Added message: {messageNode.InnerText} with timer: {timerValue}");
                    }
                    else
                    {
                        Main.LogEntry(methodName, $"Skipped adding message: {messageKey} with timer: +{timerKey}");
                    }
                }

                // Create and start timers for each message
                foreach (var message in messages)
                {
                    Timer message_timer = new(message.Value * 1000); // Convert seconds to milliseconds
                    message_timer.Elapsed += async (source, e) => await TwitchEventHandler.SendMessage(message.Key);
                    message_timer.AutoReset = true;
                    message_timer.Enabled = true;
                    Main.LogEntry(methodName, $"Timer set for message: {message.Key} with interval: {message.Value * 1000} ms");
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"An error occurred: {ex.Message}");
            }
        }
    }
}