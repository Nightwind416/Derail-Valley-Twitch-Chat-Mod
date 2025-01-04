using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class MediumDisplayBoard : BaseMenu
    {
        private const int MaxVisibleMessages = 12; // Limit the number of visible messages

        public MediumDisplayBoard(Transform parent) : base(parent)
        {
            // Scrollable area
            scrollableArea = ScrollableArea.CreateScrollableArea(menuObject.transform, 480, 460);
        }

        public void AddMessage(string username, string message)
        {
            if (contentRectTransform.childCount >= MaxVisibleMessages)
            {
                MessageManager.RemoveOldestMessage(contentRectTransform);
            }
            MessageManager.AddMessage(contentRectTransform, scrollRect, username, message);
        }

        public override void Show()
        {
            base.Show();
            if (scrollableArea != null) scrollableArea.SetActive(!isMinimized);
        }
    }
}
