using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelDisplays
{
    public class MediumDisplayPanel : PanelConstructor.BasePanel
    {
        private int maxMessages = 10;
        private int messageCount = 0;

        public MediumDisplayPanel(Transform parent) : base(parent)
        {
            ModifyScrollView();
        }

        private void ModifyScrollView()
        {
            if (contentRectTransform != null)
            {
                // Modify layout for medium display
                VerticalLayoutGroup verticalLayout = contentRectTransform.GetComponent<VerticalLayoutGroup>();
                if (verticalLayout != null)
                {
                    verticalLayout.padding = new RectOffset(8, 8, 8, 8);
                    verticalLayout.spacing = 5;
                }
            }
        }

        public void AddChatMessage(string username, string message)
        {
            if (messageCount >= maxMessages)
            {
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
