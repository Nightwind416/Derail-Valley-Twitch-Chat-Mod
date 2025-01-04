using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class LargeDisplayBoard : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        private const int MaxVisibleMessages = 10; // Limit the number of visible messages

        public LargeDisplayBoard(Transform parent) : base(parent)
        {
            CreateLargeDisplayBoard();
        }

        private void CreateLargeDisplayBoard()
        {
            // Dimensions - 1200x650
            
            // Title
            CreateTitle("Large Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 1190, 10, Color.white, () => OnBackButtonClicked?.Invoke());
        }

        public void AddMessage(string username, string message)
        {
            base.AddMessage(username, message);

            // Limit the number of visible messages
            if (contentRectTransform.childCount > MaxVisibleMessages)
            {
                GameObject oldestMessage = contentRectTransform.GetChild(0).gameObject;
                Destroy(oldestMessage);
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
