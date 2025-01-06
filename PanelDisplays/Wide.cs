using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelDisplays
{
    public class WideDisplayPanel : PanelConstructor.BasePanel
    {
        private int maxMessages = 3;
        private int messageCount = 0;

        public WideDisplayPanel(Transform parent) : base(parent)
        {
            ModifyScrollView();
        }

        private void ModifyScrollView()
        {
            if (contentRectTransform != null)
            {
                // Modify layout for wide display
                VerticalLayoutGroup verticalLayout = contentRectTransform.GetComponent<VerticalLayoutGroup>();
                if (verticalLayout != null)
                {
                    verticalLayout.padding = new RectOffset(10, 10, 10, 10);
                    verticalLayout.spacing = 10;
                }
            }
        }

        public void AddChatMessage(string username, string message)
        {
            if (messageCount >= maxMessages)
            {
                // Remove oldest message
                if (contentRectTransform.childCount > 0)
                {
                    GameObject.Destroy(contentRectTransform.GetChild(0).gameObject);
                    messageCount--;
                }
            }
            
            AddMessage(username, message);
            messageCount++;
        }
    }
}
