using UnityEngine;

namespace TwitchChat.Menus
{
    public class WideDisplayBoard : MenuConstructor.BaseMenu
    {
        private const int MaxVisibleMessages = 5;

        public WideDisplayBoard(Transform parent) : base(parent)
        {
            // Scrollable area
            scrollableArea = MenuConstructor.ScrollableArea.Create(menuObject.transform, 880, 190);
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
            // Restore proper height based on minimized state
            scrollableArea?.SetActive(!isMinimized);
        }
    }
}
