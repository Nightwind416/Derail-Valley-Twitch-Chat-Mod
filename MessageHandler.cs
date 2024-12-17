using System;
using UnityEngine;
using DV.UIFramework;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace TwitchChat
{
    public class MessageHandler
    {
        public static Dictionary<string, string> NewNotificationQueue = new()
        {
            { "webSocketNotification", "Nothing received yet (webSocketNotification)" },
            { "httpNotification", "Nothing received yet (httpNotification)" },
            { "indirectNotification", "Nothing received yet (indirectNotification)" },
            { "messageQueuenotification", "Nothing received yet (messageQueuenotification)" }
        };

        public static void SetVariable(string key, string value)
        {
            if (NewNotificationQueue.ContainsKey(key))
            {
                if (NewNotificationQueue[key] != value)
                {
                    NewNotificationQueue[key] = value;
                    AttachNotification(value, "null");
                }
            }
            else
            {
                NewNotificationQueue.Add(key, value);
                AttachNotification(value, "null");
            }
        }
        public static string? GetVariable(string key)
        {
            return NewNotificationQueue.ContainsKey(key) ? NewNotificationQueue[key] : null;
        }
        private static int messageQueueTestCounter = 1;
        public static void MessageQueueAttachmentMessageTest()
        {
            SetVariable("messageQueuenotification", $"Queued Attachment Test {messageQueueTestCounter}");
            messageQueueTestCounter++;            
        }
        public static void AttachNotification(string displayed_text, string object_name)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, $"AttachNotification called with displayed_text: {displayed_text}, object_name: {object_name}");

            // Set message to 5 seconds               
            float message_duration = 5f;
            Main.LogEntry(methodName, $"Message duration set to: {message_duration}");

            // Find the object_name GameObject in the scene
            GameObject found_object = GameObject.Find(object_name);
            if (found_object != null)
            {
                Main.LogEntry(methodName, $"Found object: {found_object.name}");
            }
            else
            {
                Main.LogEntry(methodName, "Object not found");
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

            // displayed_text = "Temp replaced text";

            try
            {
                // Display a notification, attached to the found_object if it's not null
                Main.LogEntry(methodName, "Attempting to show notification");
                var notification = notificationManager.ShowNotification(
                    displayed_text,             // Text?
                    null,                       // Localization parameters?
                    message_duration,           // Duration?
                    false,                      // Clear existing notifications?
                    // found_object?.transform,    // Attach to GameObject if not null
                    null,                       // Temp force null
                    false,                      // Localize?
                    false                       // Target UI?
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