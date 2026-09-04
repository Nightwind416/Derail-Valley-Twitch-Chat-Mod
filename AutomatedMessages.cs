using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace TwitchChat
{
    /// <summary>
    /// Manages automated messaging system and chat command processing.
    /// Handles scheduled messages, command responses, and timed broadcasts.
    /// Provides configuration and control of automated chat interactions.
    /// </summary>
    public class AutomatedMessages
    {
        /// <summary>
        /// Collection of active message timers.
        /// </summary>
        private static readonly List<Timer> activeTimers = new();

        /// <summary>
        /// Indicates whether any message timers are currently active and running.
        /// </summary>
        public static bool AreTimersRunning => activeTimers.Count > 0 && activeTimers.Any(t => t.Enabled);

        /// <summary>
        /// Initializes the timed message system from configuration.
        /// Sets up message schedules and starts timer operations.
        /// </summary>
        private static void TimedMessagesInit()
        {
            string methodName = "TimedMessagesInit";
            try
            {
                StopAndClearTimers(); // Clear any existing timers before creating new ones

                var messages = new List<(string text, float timer, string color)>
                {
                    (TimedMessages.TimedMessage1, TimedMessages.TimedMessage1Timer, TimedMessages.TimedMessage1Color),
                    (TimedMessages.TimedMessage2, TimedMessages.TimedMessage2Timer, TimedMessages.TimedMessage2Color),
                    (TimedMessages.TimedMessage3, TimedMessages.TimedMessage3Timer, TimedMessages.TimedMessage3Color),
                    (TimedMessages.TimedMessage4, TimedMessages.TimedMessage4Timer, TimedMessages.TimedMessage4Color),
                    (TimedMessages.TimedMessage5, TimedMessages.TimedMessage5Timer, TimedMessages.TimedMessage5Color)
                };

                // Create and start timers for each configured message
                foreach (var (text, timer, color) in messages)
                {
                    if (timer <= 0 || string.IsNullOrEmpty(text) || text == "MessageNotSet")
                        continue;

                    Timer messageTimer = new(timer * 1000); // Convert seconds to milliseconds
                    string messageText = text;
                    string messageColor = color;

                    messageTimer.Elapsed += async (source, e) =>
                    {
                        if (messageColor.ToLower() == "normal")
                            await TwitchEventHandler.SendMessage(messageText);
                        else
                            await TwitchEventHandler.SendAnnouncement(messageText, messageColor);

                        SetLastTimedMessageSent($"{messageText} (Sent at {DateTime.Now:HH:mm:ss})");
                    };
                    messageTimer.AutoReset = true;
                    messageTimer.Enabled = true;
                    activeTimers.Add(messageTimer);
                    Main.LogEntry(methodName, $"Timer set for message: {messageText} with interval: {timer} seconds");
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"An error occurred: {ex.Message}");
                SetLastTimedMessageSent("Error initializing timed messages, see log for details");
            }
        }

        public static void ToggleTimedMessages()
        {
            string methodName = "ToggleTimedMessages";
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

        /// <summary>
        /// Records the most recent timed-message status so the settings UI can display it.
        /// </summary>
        private static void SetLastTimedMessageSent(string message)
        {
            Settings.Instance.lastTimedMessageSent = message;
        }

        public static void CommandMessageProcessing(string message, string sender)
        {
            string methodName = "CommandMessageProcessing";
            var settings = Settings.Instance;

            try
            {
                string lowerMessage = message.ToLower();

                if (lowerMessage == "!commands" && settings.commandsMessageEnabled)
                {
                    _ = TwitchEventHandler.SendWhisper(sender, settings.commandsMessage);
                    return;
                }

                if (lowerMessage == "!info" && settings.infoMessageEnabled)
                {
                    _ = TwitchEventHandler.SendWhisper(sender, settings.infoMessage);
                    return;
                }

                // Process custom commands
                if (settings.customCommand1Active && lowerMessage == settings.customCommand1Trigger.ToLower())
                    _ = TwitchEventHandler.SendMessage(settings.customCommand1Response);
                else if (settings.customCommand2Active && lowerMessage == settings.customCommand2Trigger.ToLower())
                    _ = TwitchEventHandler.SendMessage(settings.customCommand2Response);
                else if (settings.customCommand3Active && lowerMessage == settings.customCommand3Trigger.ToLower())
                    _ = TwitchEventHandler.SendMessage(settings.customCommand3Response);
                else if (settings.customCommand4Active && lowerMessage == settings.customCommand4Trigger.ToLower())
                    _ = TwitchEventHandler.SendMessage(settings.customCommand4Response);
                else if (settings.customCommand5Active && lowerMessage == settings.customCommand5Trigger.ToLower())
                    _ = TwitchEventHandler.SendMessage(settings.customCommand5Response);
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Error processing command: {ex.Message}");
            }
        }
    }
}
