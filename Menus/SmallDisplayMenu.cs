using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class SmallDisplayBoard : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public SmallDisplayBoard(Transform parent) : base(parent)
        {
            CreateSmallDisplayBoard();
        }

        private void CreateSmallDisplayBoard()
        {
            // Dimensions - 200x300
            
            // Title
            CreateTitle("Small Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Add SmallDisplayBoard items here
            // TODO: Complete SmallDisplayBoard

            // Back button
            Button backButton = CreateButton("BackButton", "Back", 0, -200, 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }
    }
}
