using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class DebugMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public DebugMenu(Transform parent) : base(parent)
        {
            CreateDebugMenu();
            CreateDebugSection();
        }

        private void CreateDebugMenu()
        {
            // Dimensions - 200x300
            
            // Title
            CreateTitle("Debug Menu", 18, Color.white, TextAnchor.UpperCenter);

            // Back button
            Button backButton = CreateButton(menuObject.transform, " X ", 190, 10, Color.white, () => OnBackButtonClicked?.Invoke());
        }

        private void CreateDebugSection()
        {
            // Dimensions - Menu width minus 20

            // Debug Section
            GameObject debugSection = CreateSection("Debug", 25, 100);
            
            // TODO: Add debug section content
        }
    }
}
