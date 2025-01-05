using UnityEngine;

namespace TwitchChat.Menus
{
    public class LargeDisplayBoard : MenuConstructor.BaseMenu
    {
        private const int MaxVisibleMessages = 30; // Limit the number of visible messages

        public LargeDisplayBoard(Transform parent) : base(parent)
        {
            // Scrollable area
            scrollableArea = MenuConstructor.ScrollableArea.Create(menuObject.transform, 1180, 620);
        }

        public void AddMessage(string username, string message)
        {
            Main.LogEntry("LargeDisplayBoard.AddMessage", "Adding message to large display board...");

            if (contentRectTransform.childCount >= MaxVisibleMessages)
            {
                MenuConstructor.MessageManager.RemoveOldestMessage(contentRectTransform);
            }
            MenuConstructor.MessageManager.AddMessage(contentRectTransform, scrollRect, username, message);

            Main.LogEntry("LargeDisplayBoard.AddMessage", "Message added to large display board: " + username + ": " + message);
        }

        public override void Show()
        {
            base.Show();
            scrollableArea?.SetActive(!isMinimized);
        }
    }
}
