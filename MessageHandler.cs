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
            { "messageQueuenotification", "Nothing received yet (messageQueuenotification)" },
            { "alertMessage", "Nothing received yet (alertMessage)" }
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
        public static void HandleNotification(dynamic jsonMessage)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
        
            // Additional logging, enable if needed
            // Main.LogEntry($"{methodName}_Start", "HandleNotification called with message: " + jsonMessage.ToString());
        
            try
            {
                if (jsonMessage.metadata.subscription_type == "channel.chat.message")
                {
                    Main.LogEntry($"{methodName}_SubscriptionType", "Message type is channel.chat.message");
        
                    var chatter = jsonMessage.payload.@event.chatter_user_name;
                    var text = jsonMessage.payload.@event.message.text;
        
                    Main.LogEntry($"{methodName}_ExtractChatter", $"Extracted chatter: {chatter}");
                    Main.LogEntry($"{methodName}_ExtractText", $"Extracted text: {text}");
        
                    Main.LogEntry($"{methodName}_Message", $"Message: #{chatter}: {text}");
                    Main.LogEntry("ReceivedMessage", $"{chatter}: {text}");

                    WebSocketManager.lastWebSocketMessageReceived = $"{chatter}: {text}";
        
                    text = jsonMessage.payload.@event.message.text.ToString();
        
                    try
                    {
                        Main.LogEntry($"{methodName}_BeforeTextCheck", "Before checking text content");

                        if (text.Contains("HeyGuys"))
                        {
                            Main.LogEntry($"{methodName}_HeyGuys", "Text contains 'HeyGuys', sending 'VoHiYo'");
                            TwitchEventHandler.SendChatMessageHTTP("[HTTP] VoHiYo").Wait();
                        }
                        else if (text.ToLower().StartsWith("!info"))
                        {
                            Main.LogEntry($"{methodName}_Info", "Text contains '!info', sending 'This is a test message'");
                            TwitchEventHandler.SendChatMessageHTTP("[HTTP] This is an info message").Wait();
                        }
                        else if (text.ToLower().StartsWith("!commands"))
                        {
                            Main.LogEntry($"{methodName}_Commands", "Text contains '!commands', sending 'Available commands: !info !commands !test'");
                            TwitchEventHandler.SendChatMessageHTTP("[HTTP] Available commands: !info !commands !test").Wait();
                        }
                        else
                        {
                            Main.LogEntry($"{methodName}_OtherMessage", "[HTTP] Other message, not responding");
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                MessageHandler.SetVariable("webSocketNotification", $"{chatter}: {text}");
                            });
                        }

                        Main.LogEntry($"{methodName}_AfterTextCheck", "After checking text content");
                    }
                    catch (Exception ex)
                    {
                        Main.LogEntry($"{methodName}_MessageHandling", $"Exception in message handling: {ex.Message}");
                        Main.LogEntry($"{methodName}_MessageHandling", $"Stack Trace: {ex.StackTrace}");
                        Main.LogEntry($"{methodName}_MessageHandling", $"Inner Exception: {ex.InnerException?.Message}");
                    }
                }
                else
                {
                    Main.LogEntry($"{methodName}_NonChatMessage", "Message type is not channel.chat.message");
                }
                 Main.LogEntry($"{methodName}_AfterSubscriptionTypeCheck", "After checking subscription type");
            }
            catch (Exception ex)
            {
                Main.LogEntry($"{methodName}_Exception", $"Exception in HandleNotification: {ex.Message}");
                Main.LogEntry($"{methodName}_Exception", $"Stack Trace: {ex.StackTrace}");
                Main.LogEntry($"{methodName}_Exception", $"Inner Exception: {ex.InnerException?.Message}");
            }
        }
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