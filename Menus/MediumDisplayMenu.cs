using UnityEngine;

namespace TwitchChat.Menus
{
    public class MediumDisplayBoard : MenuConstructor.BaseMenu
    {
        private const int MaxVisibleMessages = 12; // Limit the number of visible messages

        public MediumDisplayBoard(Transform parent) : base(parent)
        {
            // Scrollable area
            scrollableArea = MenuConstructor.ScrollableArea.Create(menuObject.transform, 480, 460);
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
