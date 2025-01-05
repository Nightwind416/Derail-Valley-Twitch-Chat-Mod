using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.MenuConstructor
{
    public static class MessageManager
    {
        private const int MAX_MESSAGES = 100;
        private static readonly Queue<string> messageHistory = new();
        private static readonly HashSet<string> recentMessages = [];

        private const float MESSAGE_UPDATE_INTERVAL = 0.1f; // Update every 100ms
        private static float lastUpdateTime;
        private static readonly Queue<(string username, string message)> pendingMessages = new();
        private static bool isProcessingMessages;

        public static void AddMessage(RectTransform contentRectTransform, ScrollRect scrollRect, string username, string message)
        {
            try
            {
                if (contentRectTransform == null)
                {
                    Main.LogEntry("MessageManager.AddMessage", "Error: contentRectTransform is null");
                    return;
                }

                // Create unique message identifier
                string messageId = $"{username}:{message}";

                // Check for duplicate messages within recent history
                lock (recentMessages)
                {
                    if (recentMessages.Contains(messageId) && !Settings.Instance.processDuplicates)
                    {
                        Main.LogEntry("MessageManager.AddMessage", "Duplicate message detected, skipping");
                        return;
                    }

                    // Add to recent messages
                    recentMessages.Add(messageId);
                    messageHistory.Enqueue(messageId);

                    // Remove oldest message if we exceed the limit
                    if (messageHistory.Count > MAX_MESSAGES)
                    {
                        string oldestMessage = messageHistory.Dequeue();
                        recentMessages.Remove(oldestMessage);
                    }
                }

                // Add message to pending queue instead of processing immediately
                lock (pendingMessages)
                {
                    pendingMessages.Enqueue((username, message));
                }

                // Try to process messages if enough time has passed
                TryProcessPendingMessages(contentRectTransform, scrollRect);
            }
            catch (Exception e)
            {
                Main.LogEntry("MessageManager.AddMessage", $"Error adding message: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void TryProcessPendingMessages(RectTransform contentRectTransform, ScrollRect scrollRect)
        {
            if (isProcessingMessages) return;

            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - lastUpdateTime < MESSAGE_UPDATE_INTERVAL) return;

            isProcessingMessages = true;
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                try
                {
                    ProcessMessageBatch(contentRectTransform, scrollRect);
                }
                finally
                {
                    isProcessingMessages = false;
                    lastUpdateTime = Time.realtimeSinceStartup;
                }
            });
        }

        private static void ProcessMessageBatch(RectTransform contentRectTransform, ScrollRect scrollRect)
        {
            const int MAX_BATCH_SIZE = 10;
            int processedCount = 0;

            while (processedCount < MAX_BATCH_SIZE)
            {
                (string username, string message) messageInfo;
                lock (pendingMessages)
                {
                    if (pendingMessages.Count == 0) break;
                    messageInfo = pendingMessages.Dequeue();
                }

                CreateMessageObject(contentRectTransform, messageInfo.username, messageInfo.message);
                processedCount++;
            }

            // Force layout update and scroll after batch
            if (processedCount > 0)
            {
                Canvas.ForceUpdateCanvases();
                if (scrollRect != null)
                {
                    scrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        private static void CreateMessageObject(RectTransform contentRectTransform, string username, string message)
        {
            GameObject messageObj = new("Message");
            messageObj.transform.SetParent(contentRectTransform, false);

            RectTransform messageRect = messageObj.AddComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0, 1);
            messageRect.anchorMax = new Vector2(1, 1);
            messageRect.pivot = new Vector2(0.5f, 1);
            messageRect.offsetMin = new Vector2(5, 0);
            messageRect.offsetMax = new Vector2(-5, 0);

            Text messageText = messageObj.AddComponent<Text>();
            Font arialFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (arialFont == null)
            {
                Main.LogEntry("MessageManager.AddMessage", "Error: Failed to load Arial font");
                GameObject.Destroy(messageObj);
                return;
            }

            messageText.font = arialFont;
            messageText.fontSize = 14;
            messageText.lineSpacing = 1;
            messageText.supportRichText = false;
            messageText.alignByGeometry = true;
            messageText.resizeTextForBestFit = false;
            messageText.color = Color.white;
            messageText.alignment = TextAnchor.UpperLeft;
            messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            messageText.verticalOverflow = VerticalWrapMode.Overflow;
            messageText.raycastTarget = false;

            // Set text content
            string fullText = $"{username}: {message}";
            messageText.text = fullText;

            // Calculate height using a simple character-based estimation
            float availableWidth = contentRectTransform.rect.width - 10f; // Account for padding
            float approxCharWidth = messageText.fontSize * 0.6f; // Approximate width per character
            float charsPerLine = Mathf.Floor(availableWidth / approxCharWidth);
            float estimatedLines = Mathf.Max(1, Mathf.Ceil(fullText.Length / charsPerLine));
            float lineHeight = messageText.fontSize * 1.2f; // Add 20% for line spacing
            float estimatedHeight = Mathf.Max(20f, estimatedLines * lineHeight);

            // Apply height to rect transform
            messageRect.sizeDelta = new Vector2(0, estimatedHeight);

            // Position the new message
            float yPosition = 0;
            if (contentRectTransform.childCount > 1)
            {
                Transform previousChild = contentRectTransform.GetChild(contentRectTransform.childCount - 2);
                if (previousChild != null && previousChild.TryGetComponent<RectTransform>(out RectTransform lastMessage))
                {
                    yPosition = lastMessage.anchoredPosition.y - (estimatedHeight + 5f);
                }
            }
            messageRect.anchoredPosition = new Vector2(0, yPosition);

            // Update content height
            float newHeight = Mathf.Abs(yPosition) + estimatedHeight;
            contentRectTransform.sizeDelta = new Vector2(contentRectTransform.sizeDelta.x, newHeight);
        }

        public static void RemoveOldestMessage(RectTransform contentRectTransform)
        {
            if (contentRectTransform.childCount > 0)
            {
                if (contentRectTransform.GetChild(0) is RectTransform oldestMessage)
                {
                    float messageHeight = oldestMessage.sizeDelta.y + 5f;
                    GameObject.Destroy(oldestMessage.gameObject);

                    // Adjust positions of remaining messages
                    for (int i = 0; i < contentRectTransform.childCount; i++)
                    {
                        if (contentRectTransform.GetChild(i) is RectTransform child)
                        {
                            Vector2 position = child.anchoredPosition;
                            position.y += messageHeight;
                            child.anchoredPosition = position;
                        }
                    }

                    // Adjust content height
                    Vector2 contentSize = contentRectTransform.sizeDelta;
                    contentSize.y -= messageHeight;
                    contentRectTransform.sizeDelta = contentSize;
                }
            }
        }
    }
}
