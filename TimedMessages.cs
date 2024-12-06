// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Timers;
// using System.Xml;
// using UnityModManagerNet;

// namespace TwitchChat
// {
//     public static class TimedMessagesHandler
//     {
//         public static void TimedMessages()
//         {
//             try
//             {
//                 // Check if the file exists
//                 if (!File.Exists(Main.settingsFilePath))
//                 {
//                     UnityModManager.Logger.Log($"[TwitchChat] Settings file not found: {Main.settingsFilePath}");
//                     return;
//                 }

//                 // Load the XML document
//                 var xmlDoc = new XmlDocument();
//                 xmlDoc.Load(Main.settingsFilePath);

//                 // Extract messages and their timers
//                 var messages = new Dictionary<string, int>();

//                 for (int i = 1; i <= 10; i++)
//                 {
//                     string activeKey = $"timedMessage{i}Toggle";
//                     string messageKey = $"timedMessage{i}";
//                     string timerKey = $"{messageKey}Timer";

//                     var activeNode = xmlDoc.SelectSingleNode($"//Settings/{activeKey}");
//                     var messageNode = xmlDoc.SelectSingleNode($"//Settings/{messageKey}");
//                     var timerNode = xmlDoc.SelectSingleNode($"//Settings/{timerKey}");

//                     bool isActive = activeNode != null && bool.TryParse(activeNode.InnerText, out bool active) && active;

//                     if (isActive && messageNode != null && timerNode != null && int.TryParse(timerNode.InnerText, out int timerValue) && timerValue > 0)
//                     {
//                         messages.Add(messageNode.InnerText, timerValue);
//                         UnityModManager.Logger.Log($"[TwitchChat] Added message: {messageNode.InnerText} with timer: {timerValue}");
//                     }
//                     else
//                     {
//                         UnityModManager.Logger.Log($"[TwitchChat] Skipped adding message: {messageKey} with timer: {timerKey}");
//                     }
//                 }

//                 // Create and start timers for each message
//                 foreach (var message in messages)
//                 {
//                     Timer timer = new(message.Value * 1000); // Convert seconds to milliseconds
//                     timer.Elapsed += (source, e) => Main.SendMessageToTwitch(message.Key);
//                     timer.AutoReset = true;
//                     timer.Enabled = true;
//                     UnityModManager.Logger.Log($"[TwitchChat] Timer set for message: {message.Key} with interval: {message.Value * 1000} ms");
//                 }
//             }
//             catch (Exception ex)
//             {
//                 UnityModManager.Logger.Log($"An error occurred: {ex.Message}");
//             }
//         }
//     }
// }