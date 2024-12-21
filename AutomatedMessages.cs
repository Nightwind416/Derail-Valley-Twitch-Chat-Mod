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
        private static void TimedMessagesInit()
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
                        typeof(TimedMessages).GetProperty("lastTimedMessageSent", BindingFlags.NonPublic | BindingFlags.Static)
                            ?.SetValue(null, $"{message.Key} (Sent at {DateTime.Now:HH:mm:ss})");
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
                typeof(TimedMessages).GetProperty("lastTimedMessageSent", BindingFlags.NonPublic | BindingFlags.Static)
                    ?.SetValue(null, "Error initializing timed messages, see log for details");
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
                    SetLastTimedMessageSent("Timed messages system started");
                }
                else
                {
                    StopAndClearTimers();
                    Main.LogEntry(methodName, "Timed messages system stopped");
                    SetLastTimedMessageSent("Timed messages system stopped");
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Error toggling timed messages: {ex.Message}");
                SetLastTimedMessageSent("Error toggling timed messages, see log for details");
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
        private static void SetLastTimedMessageSent(string message)
        {
            typeof(TimedMessages).GetProperty("lastTimedMessageSent", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, message);
        }

        public static void CommandMessageProcessing(string message, string sender)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            var settings = Settings.Instance;

            try
            {
                if (message == "!commands" && settings.commandMessageActive)
                {
                    var enabledCommands = new List<string>();
                    
                    if (settings.infoMessageActive) enabledCommands.Add("!info");
                    if (settings.customCommand1Active) enabledCommands.Add(settings.customCommand1Trigger);
                    if (settings.customCommand2Active) enabledCommands.Add(settings.customCommand2Trigger);
                    if (settings.customCommand3Active) enabledCommands.Add(settings.customCommand3Trigger);
                    if (settings.customCommand4Active) enabledCommands.Add(settings.customCommand4Trigger);
                    if (settings.customCommand5Active) enabledCommands.Add(settings.customCommand5Trigger);

                    string response = "Available commands: " + string.Join("  ", enabledCommands);
                    _ = TwitchEventHandler.SendMessage(response);
                    Settings.Instance.commandMessage = response;
                    return;
                }

                if (message == "!info" && settings.infoMessageActive)
                {
                    _ = TwitchEventHandler.SendMessage(settings.infoMessage);
                    return;
                }

                // Process custom commands
                if (settings.customCommand1Active && message == settings.customCommand1Trigger.ToLower())
                    _ = TwitchEventHandler.SendMessage(settings.customCommand1Response);
                else if (settings.customCommand2Active && message == settings.customCommand2Trigger.ToLower())
                    _ = TwitchEventHandler.SendMessage(settings.customCommand2Response);
                else if (settings.customCommand3Active && message == settings.customCommand3Trigger.ToLower())
                    _ = TwitchEventHandler.SendMessage(settings.customCommand3Response);
                else if (settings.customCommand4Active && message == settings.customCommand4Trigger.ToLower())
                    _ = TwitchEventHandler.SendMessage(settings.customCommand4Response);
                else if (settings.customCommand5Active && message == settings.customCommand5Trigger.ToLower())
                    _ = TwitchEventHandler.SendMessage(settings.customCommand5Response);
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Error processing command: {ex.Message}");
            }
        }
    }
}