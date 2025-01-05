using UnityEngine;

namespace TwitchChat.Menus
{
    public class SmallDisplayBoard : MenuConstructor.BaseMenu
    {
        private const int MaxVisibleMessages = 5; // Limit the number of visible messages

        public SmallDisplayBoard(Transform parent) : base(parent)
        {
            // Scrollable area
            scrollableArea = MenuConstructor.ScrollableArea.Create(menuObject.transform, 180, 265);
        }

        public void AddMessage(string username, string message)
        {
            if (contentRectTransform.childCount >= MaxVisibleMessages)
            {
                MenuConstructor.MessageManager.RemoveOldestMessage(contentRectTransform);
            }
            MenuConstructor.MessageManager.AddMessage(contentRectTransform, scrollRect, username, message);
        }

        public override void Show()
        {
            base.Show();
            scrollableArea?.SetActive(!isMinimized);
        }
    }
}
