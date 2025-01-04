using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class SmallDisplayBoard : BaseMenu
    {
        private const int MaxVisibleMessages = 5; // Limit the number of visible messages

        public SmallDisplayBoard(Transform parent) : base(parent)
        {
            // Scrollable area
            scrollableArea = CreateScrollableArea(180, 265);
        }

        public void AddMessage(string username, string message)
        {
            if (contentRectTransform.childCount >= MaxVisibleMessages)
            {
                RemoveOldestMessage();
            }
            base.AddMessage(username, message);
        }

        public override void Show()
        {
            base.Show();
            if (scrollableArea != null) scrollableArea.SetActive(!isMinimized);
        }
    }
}
