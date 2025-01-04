using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class LargeDisplayBoard : BaseMenu
    {
        private const int MaxVisibleMessages = 30; // Limit the number of visible messages

        public LargeDisplayBoard(Transform parent) : base(parent)
        {
            // Scrollable area
            scrollableArea = ScrollableArea.CreateScrollableArea(menuObject.transform, 1180, 620);
        }

        public void AddMessage(string username, string message)
        {
            Main.LogEntry("LargeDisplayBoard.AddMessage", "Adding message to large display board...");

            if (contentRectTransform.childCount >= MaxVisibleMessages)
            {
                MessageManager.RemoveOldestMessage(contentRectTransform);
            }
            MessageManager.AddMessage(contentRectTransform, scrollRect, username, message);

            Main.LogEntry("LargeDisplayBoard.AddMessage", "Message added to large display board: " + username + ": " + message);
        }

        public override void Show()
        {
            base.Show();
            if (scrollableArea != null) scrollableArea.SetActive(!isMinimized);
        }
    }
}
