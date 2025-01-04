using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class SmallDisplayBoard : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        private const int MaxVisibleMessages = 5; // Limit the number of visible messages

        public SmallDisplayBoard(Transform parent) : base(parent)
        {
            // Dimensions - 200x300

            // Title
            CreateTitle("Small Display Board", 18, Color.white, TextAnchor.UpperCenter);
            
            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 190, 10, Color.white, () => OnBackButtonClicked?.Invoke());

            // Scrollable area
            scrollableArea = CreateScrollableArea(180, 265);
        }

        public void AddMessage(string username, string message)
        {
            base.AddMessage(username, message);

            // Limit the number of visible messages
            if (contentRectTransform.childCount > MaxVisibleMessages)
            {
                GameObject oldestMessage = contentRectTransform.GetChild(0).gameObject;
                Object.Destroy(oldestMessage);
            }

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
