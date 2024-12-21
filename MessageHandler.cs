using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using DV.UIFramework;

namespace TwitchChat
{
    /// <summary>
    /// Handles Twitch chat messages and in-game notifications.
    /// </summary>
    public class MessageHandler
    {
        private static int messageQueueTestCounter = 1;
        public static Dictionary<string, string> NewNotificationQueue = new()
        {
            { "webSocketNotification", "" },
            { "httpNotification", "" },
            { "alertNotification", "" }
        };

        /// <summary>
        /// Sets a notification variable in the queue.
        /// </summary>
        /// <param name="notification_type">The notification type.</param>
        /// <param name="message">The notification message.</param>
        public static void SetVariable(string notification_type, string message)
        {
            if (NewNotificationQueue.ContainsKey(notification_type))
            {
                if (NewNotificationQueue[notification_type] != message)
                {
                    NewNotificationQueue[notification_type] = message;
                    AttachNotification(message, "null");
                }
            }
            else
            {
                NewNotificationQueue.Add(notification_type, message);
                AttachNotification(message, "null");
            }
        }

        /// <summary>
        /// Retrieves the last notification received of a given type from the queue.
        /// </summary>
        /// <param name="notification_type">The notification type to retrieve.</param>
        /// <returns>The notification message or null if not found.</returns>
        public static string? GetVariable(string notification_type)
        {
            return NewNotificationQueue.ContainsKey(notification_type) ? NewNotificationQueue[notification_type] : null;
        }
        public static void WebSocketNotificationTest()
        {
            SetVariable("webSocketNotification", $"Message Queue Attachment Notification Test #{messageQueueTestCounter}");
            messageQueueTestCounter++;            
        }
        private class TwitchMessage
        {
            public Metadata? metadata { get; set; }
            public Payload? payload { get; set; }
        }
        private class Metadata
        {
            public string? subscription_type { get; set; }
        }
        private class Payload
        {
            public Event? @event { get; set; }
        }
        private class Event
        {
            public string? chatter_user_name { get; set; }
            public string? chatter_user_id { get; set; }
            public Message? message { get; set; }
        }
        private class Message
        {
            public string? text { get; set; }
        }

        /// <summary>
        /// Handles incoming Twitch chat notifications and processes commands.
        /// </summary>
        /// <param name="jsonMessage">The raw JSON message received from Twitch.</param>
        public static void HandleNotification(dynamic jsonMessage)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
        
            try
            {
                string jsonString = jsonMessage.ToString();
                Main.LogEntry($"{methodName}", $"Processing message: {jsonString}");
                
                if (!jsonString.Contains("\"subscription_type\":\"channel.chat.message\""))
                {
                    Main.LogEntry($"{methodName}", "Not a chat message, skipping");
                    return;
                }
                
                string chatter = ExtractValue(jsonString, "chatter_user_name");
                string chatterId = ExtractValue(jsonString, "chatter_user_id");
                string text = ExtractValue(jsonString, "text");

                Main.LogEntry($"{methodName}", $"Extracted values - Chatter: {chatter}, ChatterId: {chatterId}, Text: {text}");

                // Skip if any required values are missing
                if (string.IsNullOrEmpty(chatter) || string.IsNullOrEmpty(text) || string.IsNullOrEmpty(chatterId))
                {
                    Main.LogEntry($"{methodName}", "Skipping message due to missing required values");
                    return;
                }

                // Skip processing if message is from ourselves
                if (chatterId == TwitchEventHandler.user_id)
                {
                    Main.LogEntry($"{methodName}", $"Skipping message from self (ID: {chatterId})");
                    return;
                }

                if (!string.IsNullOrEmpty(chatter) && !string.IsNullOrEmpty(text))
                {
                    Main.LogEntry($"{methodName}", $"Valid message received from {chatter}: {text}");

                    // Redirect command messages
                    if (text.ToLower().StartsWith("!"))
                    {
                        AutomatedMessages.CommandMessageProcessing(text, chatter);
                        return; // Skip notification for command messages
                    }

                    // Show notification only for non-command messages
                    try
                    {
                        Main.LogEntry($"{methodName}", "Attempting to queue notification...");
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            try
                            {
                                Main.LogEntry($"{methodName}", "Dispatching notification to main thread");
                                string displayMessage = $"{chatter}: {text}";
                                NewNotificationQueue["webSocketNotification"] = displayMessage;
                                AttachNotification(displayMessage, "null");
                                Main.LogEntry($"{methodName}", $"Successfully queued notification: {displayMessage}");
                            }
                            catch (Exception ex)
                            {
                                Main.LogEntry($"{methodName}", $"Error in main thread notification: {ex.Message}");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Main.LogEntry($"{methodName}", $"Failed to queue notification: {ex.Message}");
                    }
                }
                else
                {
                    Main.LogEntry($"{methodName}", "Invalid message: missing chatter or text");
                }
            }
            catch (Exception ex)
            {
                Main.LogEntry($"{methodName}", $"Error processing notification: {ex.Message}");
                Main.LogEntry($"{methodName}", $"Stack Trace: {ex.StackTrace}");
            }
        }

        private static string ExtractValue(string json, string key)
        {
            try
            {
                switch (key)
                {
                    case "chatter_user_name":
                        // Look for the specific path in the JSON
                        int nameStart = json.IndexOf("\"chatter_user_name\":\"") + "\"chatter_user_name\":\"".Length;
                        if (nameStart > 0)
                        {
                            int nameEnd = json.IndexOf("\"", nameStart);
                            if (nameEnd > nameStart)
                            {
                                return json.Substring(nameStart, nameEnd - nameStart);
                            }
                        }
                        break;

                    case "chatter_user_id":
                        // Look for the specific path in the JSON
                        int idStart = json.IndexOf("\"chatter_user_id\":\"") + "\"chatter_user_id\":\"".Length;
                        if (idStart > 0)
                        {
                            int idEnd = json.IndexOf("\"", idStart);
                            if (idEnd > idStart)
                            {
                                return json.Substring(idStart, idEnd - idStart);
                            }
                        }
                        break;

                    case "text":
                        // Text is nested inside the message object
                        int textStart = json.IndexOf("\"message\":{\"text\":\"") + "\"message\":{\"text\":\"".Length;
                        if (textStart > 0)
                        {
                            int textEnd = json.IndexOf("\"", textStart);
                            if (textEnd > textStart)
                            {
                                return json.Substring(textStart, textEnd - textStart);
                            }
                        }
                        break;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Main.LogEntry("ExtractValue", $"Error extracting {key}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Displays an in-game notification attached to a GameObject (if included).
        /// </summary>
        /// <param name="displayed_text">The text to display.</param>
        /// <param name="object_name">The name of the GameObject to attempt to attach to.</param>
        public static void AttachNotification(string displayed_text, string object_name)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, $"AttachNotification called with displayed_text: {displayed_text}, object_name: {object_name}");

            // Find the object_name GameObject in the scene
            GameObject found_object = GameObject.Find(object_name);
            if (found_object != null)
            {
                Main.LogEntry(methodName, $"Found object: {found_object.name}");
            }
            else
            {
                // Main.LogEntry(methodName, "Object not found");
            }

            // Find NotificationManager in the scene
            NotificationManager notificationManager = UnityEngine.Object.FindObjectOfType<NotificationManager>();
            if (notificationManager == null)
            {
                Main.LogEntry(methodName, "NotificationManager not found in the scene.");
                return;
            }
            else
            {
                Main.LogEntry(methodName, "NotificationManager found in the scene.");
            }

            // Ensure displayed_text is a string with only standard alphanumeric characters and punctuation, no longer than 80 characters
            if (displayed_text.Length > 80)
            {
                displayed_text = displayed_text.Substring(0, 80);
                Main.LogEntry(methodName, $"Trimmed displayed_text to 80 characters: {displayed_text}");
            }

            displayed_text = new string(displayed_text.Where(c => char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c)).ToArray());
            Main.LogEntry(methodName, $"Sanitized displayed_text: {displayed_text}");

            try
            {
                // Display a notification, attached to the found_object if it's not null
                Main.LogEntry(methodName, "Attempting to show notification");
                var notification = notificationManager.ShowNotification(
                    displayed_text,                     // Text
                    null,                               // Localization parameters
                    Settings.Instance.messageDuration,  // Duration
                    false,                              // Clear existing notifications
                    // found_object?.transform,         // Attach to GameObject if not null
                    null,                               // Temp force null for GameObject.transform
                    false,                              // Localize
                    false                               // Target UI
                );
                Main.LogEntry(methodName, "Notification shown successfully");
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Error showing notification and text: {displayed_text}\nException: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }
    }
}