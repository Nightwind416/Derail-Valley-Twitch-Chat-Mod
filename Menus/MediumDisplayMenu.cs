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
            scrollableArea = CreateScrollableArea(480, 460);
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
