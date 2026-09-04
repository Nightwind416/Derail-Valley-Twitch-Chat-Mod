using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TwitchChat
{
    /// <summary>
    /// Manages in-game notifications and Twitch chat message display.
    /// Handles message routing, command dispatch, and visual presentation of notifications.
    /// All Unity work is marshalled to the main thread, so callers may run on any thread.
    /// </summary>
    public class NotificationManager
    {
        /// <summary>
        /// Counter for testing message queue functionality.
        /// </summary>
        private static int messageQueueTestCounter = 1;

        /// <summary>
        /// Queue storing different types of notifications for processing.
        /// Keys represent notification types, values store the notification messages.
        /// </summary>
        public static Dictionary<string, string> NewNotificationQueue = new()
        {
            { "webSocketNotification", "" },
            { "httpNotification", "" },
            { "alertNotification", "" }
        };

        /// <summary>
        /// Sets a notification message in the queue for processing.
        /// Updates existing notifications or adds new ones as needed.
        /// </summary>
        /// <param name="notification_type">Type identifier for the notification.</param>
        /// <param name="message">Content of the notification message.</param>
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

        /// <summary>
        /// Handles an incoming Twitch EventSub notification and processes commands.
        /// Safe to call from the WebSocket receive thread.
        /// </summary>
        /// <param name="message">The parsed EventSub message envelope.</param>
        public static void HandleNotification(JObject message)
        {
            string methodName = "HandleNotification";

            try
            {
                string subscriptionType = (string?)message.SelectToken("metadata.subscription_type") ?? string.Empty;
                if (subscriptionType != "channel.chat.message")
                {
                    Main.LogEntry(methodName, $"Ignoring non-chat notification: {subscriptionType}");
                    return;
                }

                JToken? chatEvent = message.SelectToken("payload.event");
                string chatter = (string?)chatEvent?["chatter_user_name"] ?? string.Empty;
                string chatterId = (string?)chatEvent?["chatter_user_id"] ?? string.Empty;
                string text = (string?)chatEvent?.SelectToken("message.text") ?? string.Empty;

                Main.LogEntry(methodName, $"Extracted values - Chatter: {chatter}, ChatterId: {chatterId}, Text: {text}");

                // Skip if any required values are missing
                if (string.IsNullOrEmpty(chatter) || string.IsNullOrEmpty(text) || string.IsNullOrEmpty(chatterId))
                {
                    Main.LogEntry(methodName, "Skipping message due to missing required values");
                    return;
                }

                // Skip processing if message is from ourselves (unless debug setting is enabled)
                if (chatterId == TwitchEventHandler.user_id && !Settings.Instance.processOwn)
                {
                    Main.LogEntry(methodName, $"Skipping message from self (ID: {chatterId})");
                    return;
                }

                Main.LogEntry("ReceivedMessage", $"{chatter}: {text}");

                // Redirect command messages
                if (text.StartsWith("!"))
                {
                    AutomatedMessages.CommandMessageProcessing(text, chatterId);
                    return;
                }

                // Everything that touches Unity objects runs on the main thread.
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    try
                    {
                        MenuManager.Instance.AddMessageToPanelDisplays(chatter, text);
                    }
                    catch (Exception ex)
                    {
                        Main.LogEntry(methodName, $"Failed to add message to display boards: {ex.Message}");
                    }

                    if (!Settings.Instance.notificationsEnabled)
                    {
                        Main.LogEntry(methodName, "Notification system disabled, skipping popup.");
                        return;
                    }

                    string displayMessage = $"{chatter}: {text}";
                    NewNotificationQueue["webSocketNotification"] = displayMessage;
                    AttachNotification(displayMessage, "null");
                });
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Error processing notification: {ex.Message}");
                Main.LogEntry(methodName, $"Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Displays an in-game notification. May be called from any thread; the work is queued to the main thread.
        /// </summary>
        /// <param name="displayed_text">The text to display.</param>
        /// <param name="object_name">The name of a GameObject to look for, or "null" for none.</param>
        public static void AttachNotification(string displayed_text, string object_name)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => ShowNotificationOnMainThread(displayed_text, object_name));
        }

        /// <summary>
        /// Performs the actual notification display. Must run on the Unity main thread.
        /// </summary>
        private static void ShowNotificationOnMainThread(string displayed_text, string object_name)
        {
            string methodName = "AttachNotification";
            Main.LogEntry(methodName, $"AttachNotification called with displayed_text: {displayed_text}, object_name: {object_name}");

            if (!string.IsNullOrEmpty(object_name) && object_name != "null")
            {
                GameObject found_object = GameObject.Find(object_name);
                if (found_object != null)
                {
                    Main.LogEntry(methodName, $"Found object: {found_object.name}");
                }
            }

            // Find the game's NotificationManager in the scene
            DV.UIFramework.NotificationManager notificationManager = UnityEngine.Object.FindObjectOfType<DV.UIFramework.NotificationManager>();
            if (notificationManager == null)
            {
                Main.LogEntry(methodName, "NotificationManager not found in the scene.");
                return;
            }

            // Ensure displayed_text contains only standard alphanumeric characters and punctuation, no longer than 80 characters
            if (displayed_text.Length > 80)
            {
                displayed_text = displayed_text.Substring(0, 80);
                Main.LogEntry(methodName, $"Trimmed displayed_text to 80 characters: {displayed_text}");
            }

            displayed_text = new string(displayed_text.Where(c => char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c)).ToArray());

            try
            {
                GameObject? notification;
                try
                {
                    notification = ShowNotificationDirect(notificationManager, displayed_text, Settings.Instance.notificationDuration);
                }
                catch (MissingMethodException mmEx)
                {
                    // The game changed the ShowNotification signature (this is what broke v3.1.0). Fall back to a late-bound call.
                    Main.LogEntry(methodName, $"Game notification API signature changed ({mmEx.Message}); using reflection fallback.");
                    notification = ShowNotificationViaReflection(notificationManager, displayed_text, Settings.Instance.notificationDuration);
                }

                Main.LogEntry(methodName, notification != null ? "Notification shown successfully" : "Notification call returned nothing");
            }
            catch (Exception ex)
            {
                Main.LogEntry(methodName, $"Error showing notification and text: {displayed_text}\nException: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Direct, compile-time bound call to the game's notification API.
        /// Kept in its own non-inlined method so a signature mismatch surfaces as a catchable
        /// MissingMethodException at this call site instead of taking down the caller.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static GameObject? ShowNotificationDirect(DV.UIFramework.NotificationManager manager, string text, float duration)
        {
            return manager.ShowNotification(
                text,       // Text
                null,       // Localization parameters
                duration,   // Duration
                false,      // Clear existing notifications
                null,       // Point at transform
                false,      // Localize
                false       // Target UI
            );
        }

        /// <summary>
        /// Late-bound call to ShowNotification that fills every parameter the current game build declares,
        /// using the game's own defaults for anything this mod does not set.
        /// </summary>
        private static GameObject? ShowNotificationViaReflection(DV.UIFramework.NotificationManager manager, string text, float duration)
        {
            MethodInfo? method = typeof(DV.UIFramework.NotificationManager)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == "ShowNotification")
                .OrderByDescending(m => m.GetParameters().Length)
                .FirstOrDefault();

            if (method == null)
            {
                Main.LogEntry("ShowNotificationViaReflection", "ShowNotification method not found on the game's NotificationManager.");
                return null;
            }

            ParameterInfo[] parameters = method.GetParameters();
            object?[] args = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                Type parameterType = parameter.ParameterType;
                object? value = parameter.HasDefaultValue ? parameter.DefaultValue : null;

                if (value == null && parameterType.IsValueType)
                {
                    value = Activator.CreateInstance(parameterType);
                }
                else if (value != null && parameterType.IsEnum && !parameterType.IsInstanceOfType(value))
                {
                    value = Enum.ToObject(parameterType, value);
                }

                switch (parameter.Name)
                {
                    case "locKey": value = text; break;
                    case "locParams": value = null; break;
                    case "duration": value = duration; break;
                    case "clearExisting": value = false; break;
                    case "pointAt": value = null; break;
                    case "localize": value = false; break;
                    case "targetIsUI": value = false; break;
                }

                args[i] = value;
            }

            return method.Invoke(manager, args) as GameObject;
        }
    }
}
