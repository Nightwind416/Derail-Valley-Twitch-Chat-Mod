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
        
        // Add this new property
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
                var messages = new Dictionary<string, int>();

                // Extract messages and timers from Settings.Instance
                for (int i = 1; i <= 10; i++)
                {
                    var toggleProperty = typeof(Settings).GetProperty($"timedMessage{i}Toggle");
                    var messageProperty = typeof(Settings).GetProperty($"timedMessage{i}");
                    var timerProperty = typeof(Settings).GetProperty($"timedMessage{i}Timer");

                    if (toggleProperty != null && messageProperty != null && timerProperty != null)
                    {
                        bool isActive = (bool)toggleProperty.GetValue(Settings.Instance);
                        string message = (string)messageProperty.GetValue(Settings.Instance);
                        int timerValue = (int)timerProperty.GetValue(Settings.Instance);

                        if (isActive && timerValue > 0 && !string.IsNullOrEmpty(message))
                        {
                            messages.Add(message, timerValue);
                            Main.LogEntry(methodName, $"Added message: {message} with timer: {timerValue}");
                        }
                    }
                }

                // Create and start timers for each message
                foreach (var message in messages)
                {
                    Timer message_timer = new(message.Value * 1000); // Convert seconds to milliseconds
                    message_timer.Elapsed += async (source, e) => await TwitchEventHandler.SendMessage(message.Key);
                    message_timer.AutoReset = true;
                    message_timer.Enabled = true;
                    activeTimers.Add(message_timer); // Add to tracking collection
                    Main.LogEntry(methodName, $"Timer set for message: {message.Key} with interval: {message.Value * 1000} ms");
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"An error occurred: {ex.Message}");
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
    }
}