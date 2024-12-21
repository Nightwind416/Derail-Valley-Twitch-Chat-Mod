using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;

namespace TwitchChat
{
    public class AutomatedMessages
    {
        private static readonly List<Timer> activeTimers = new();
        
        public static bool AreTimersRunning => activeTimers.Count > 0 && activeTimers.Any(t => t.Enabled);

        /// <summary>
        /// Initializes the timed message system by reading configurations from XML
        /// and setting up message timers
        /// </summary>
        public static void TimedMessagesInit()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                StopAndClearTimers(); // Clear any existing timers before creating new ones
                var messages = new Dictionary<string, float>();

                // Add messages if they are set and have a valid timer
                if (!string.IsNullOrEmpty(TimedMessages.TimedMessage1) && TimedMessages.TimedMessage1Timer > 0)
                    messages.Add(TimedMessages.TimedMessage1, TimedMessages.TimedMessage1Timer);
                if (!string.IsNullOrEmpty(TimedMessages.TimedMessage2) && TimedMessages.TimedMessage2Timer > 0)
                    messages.Add(TimedMessages.TimedMessage2, TimedMessages.TimedMessage2Timer);
                if (!string.IsNullOrEmpty(TimedMessages.TimedMessage3) && TimedMessages.TimedMessage3Timer > 0)
                    messages.Add(TimedMessages.TimedMessage3, TimedMessages.TimedMessage3Timer);
                if (!string.IsNullOrEmpty(TimedMessages.TimedMessage4) && TimedMessages.TimedMessage4Timer > 0)
                    messages.Add(TimedMessages.TimedMessage4, TimedMessages.TimedMessage4Timer);
                if (!string.IsNullOrEmpty(TimedMessages.TimedMessage5) && TimedMessages.TimedMessage5Timer > 0)
                    messages.Add(TimedMessages.TimedMessage5, TimedMessages.TimedMessage5Timer);

                // Create and start timers for each message
                foreach (var message in messages)
                {
                    if (message.Value <= 0 || string.IsNullOrEmpty(message.Key) || message.Key == "MessageNotSet") 
                        continue;

                    Timer messageTimer = new(message.Value * 1000); // Convert seconds to milliseconds
                    messageTimer.Elapsed += async (source, e) => 
                    {
                        await TwitchEventHandler.SendMessage(message.Key);
                        TimedMessages.lastTimedMessageSent = $"{message.Key} (Sent at {DateTime.Now:HH:mm:ss})";
                    };
                    messageTimer.AutoReset = true;
                    messageTimer.Enabled = true;
                    activeTimers.Add(messageTimer);
                    Main.LogEntry(methodName, $"Timer set for message: {message.Key} with interval: {message.Value} seconds");
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"An error occurred: {ex.Message}");
                TimedMessages.lastTimedMessageSent = "Error initializing timed messages, see log for details";
            }
        }

        public static void ToggleTimedMessages()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (TimedMessages.TimedMessageSystemToggle)
                {
                    TimedMessagesInit();
                    Main.LogEntry(methodName, "Timed messages system started");
                    TimedMessages.lastTimedMessageSent = "Timed messages system started";
                }
                else
                {
                    StopAndClearTimers();
                    Main.LogEntry(methodName, "Timed messages system stopped");
                    TimedMessages.lastTimedMessageSent = "Timed messages system stopped";
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Error toggling timed messages: {ex.Message}");
                TimedMessages.lastTimedMessageSent = "Error toggling timed messages, see log for details";
            }
        }

        public static void StopAndClearTimers()
        {
            string methodName = "StopAndClearTimers";
            try
            {
                foreach (var timer in activeTimers)
                {
                    timer.Stop();
                    timer.Dispose();
                }
                activeTimers.Clear();
                Main.LogEntry(methodName, "All timers stopped and cleared");
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Error clearing timers: {ex.Message}");
            }
        }
        public static void CommandMessageProcessing(string command, string message)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

        }
    }
}