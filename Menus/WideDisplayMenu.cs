using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class WideDisplayBoard : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public WideDisplayBoard(Transform parent) : base(parent)
        {
            CreateWideDisplayBoard();
        }

        private void CreateWideDisplayBoard()
        {
            // Dimensions - 800x200
            
            // Title
            CreateTitle("Wide Display Board", 18, Color.white, TextAnchor.UpperCenter);

            // Add WideDisplayBoard items here
            // TODO: Complete WideDisplayBoard

            // Back button
            Button backButton = CreateButton("BackButton", "Back", 0, -100, 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }
    }
}
