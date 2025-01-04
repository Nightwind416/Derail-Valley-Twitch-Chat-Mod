using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class WideDisplayBoard : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        private const int MaxVisibleMessages = 5; // Limit the number of visible messages

        public WideDisplayBoard(Transform parent) : base(parent)
        {
            // Dimensions - 900x220
            
            // Title
            CreateTitle("Wide Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 890, 10, Color.white, () => OnBackButtonClicked?.Invoke());

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
    }
}
