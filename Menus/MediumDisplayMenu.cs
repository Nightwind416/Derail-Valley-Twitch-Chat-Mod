using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class MediumDisplayBoard : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        private const int MaxVisibleMessages = 12; // Limit the number of visible messages

        public MediumDisplayBoard(Transform parent) : base(parent)
        {
            // Dimensions - 500x500
            
            // Title
            CreateTitle("Medium Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 490, 10, Color.white, () => OnBackButtonClicked?.Invoke());

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
    }
}
