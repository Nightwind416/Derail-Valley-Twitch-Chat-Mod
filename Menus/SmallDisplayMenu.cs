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
            if (contentRectTransform.childCount >= MaxVisibleMessages)
            {
                RemoveOldestMessage();
            }
            base.AddMessage(username, message);
        }
    }
}
