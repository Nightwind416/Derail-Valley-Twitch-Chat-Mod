using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelDisplays
{
    public class LargeDisplayPanel : PanelConstructor.BasePanel
    {
        private int maxMessages = 500;
        private int messageCount = 0;

        public LargeDisplayPanel(Transform parent) : base(parent)
        {
            ModifyScrollView();
        }

        private void ModifyScrollView()
        {
            if (contentRectTransform != null)
            {
                // Modify layout for large display
                VerticalLayoutGroup verticalLayout = contentRectTransform.GetComponent<VerticalLayoutGroup>();
                if (verticalLayout != null)
                {
                    verticalLayout.padding = new RectOffset(10, 10, 10, 10);
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
