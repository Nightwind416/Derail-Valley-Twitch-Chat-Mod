using UnityEngine;

namespace TwitchChat.MenuConstructor
{
    public abstract class DisplayBoard : BaseMenu
    {
        protected readonly int maxVisibleMessages;

        protected DisplayBoard(Transform parent, int maxMessages, float width, float height) : base(parent)
        {
            maxVisibleMessages = maxMessages;
            scrollableArea = ScrollableArea.Create(menuObject.transform, width, height);
            scrollRect = scrollableArea.GetComponent<UnityEngine.UI.ScrollRect>();
            contentRectTransform = scrollRect.content;
        }

        public virtual void AddMessage(string username, string message)
        {
            if (contentRectTransform == null || scrollRect == null)
            {
                Main.LogEntry($"{GetType().Name}.AddMessage", "Content or ScrollRect is null");
                return;
            }

            if (contentRectTransform.childCount >= maxVisibleMessages)
            {
                MessageManager.RemoveOldestMessage(contentRectTransform);
            }
            MessageManager.AddMessage(contentRectTransform, scrollRect, username, message);
        }
    }
}
