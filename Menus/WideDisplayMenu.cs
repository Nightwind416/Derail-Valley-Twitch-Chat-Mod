using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class WideDisplayBoard : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        private const int MaxVisibleMessages = 5;

        public WideDisplayBoard(Transform parent) : base(parent)
        {
            // Scrollable area
            scrollableArea = CreateScrollableArea(880, 190);
        }

        public void AddMessage(string username, string message)
        {
            if (contentRectTransform.childCount >= MaxVisibleMessages)
            {
                RemoveOldestMessage();
            }
            base.AddMessage(username, message);

            // Adjust the scrollable area size
            float totalHeight = 0;
            foreach (RectTransform child in contentRectTransform)
            {
                totalHeight += child.sizeDelta.y;
            }
            contentRectTransform.sizeDelta = new Vector2(contentRectTransform.sizeDelta.x, totalHeight);
        }

        public override void Show()
        {
            base.Show();
            // Restore proper height based on minimized state
            if (scrollableArea != null) scrollableArea.SetActive(!isMinimized);
        }
    }
}
