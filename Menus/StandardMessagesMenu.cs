using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.Menus
{
    public class StandardMessagesMenu : BaseMenu
    {
        private InputField welcomeMessageInput;
        private InputField followerMessageInput;
        private InputField subscriberMessageInput;

        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public StandardMessagesMenu(Transform parent) : base(parent)
        {
            CreateStandardMessagesMenu();
        }

        private void CreateStandardMessagesMenu()
        {
            // Title
            CreateTitle("Standard Messages", 18, Color.white, TextAnchor.UpperCenter);

            // Welcome Message
            CreateInputField("Welcome Message", Settings.Instance.welcomeMessage, 0.8f, value => 
                Settings.Instance.welcomeMessage = value, out welcomeMessageInput);

            // Follower Message
            CreateInputField("New Follower Message", Settings.Instance.newFollowerMessage, 0.6f, value => 
                Settings.Instance.newFollowerMessage = value, out followerMessageInput);

            // Subscriber Message
            CreateInputField("New Subscriber Message", Settings.Instance.newSubscriberMessage, 0.4f, value => 
                Settings.Instance.newSubscriberMessage = value, out subscriberMessageInput);

            // Back button
            CreateButton("BackButton", "Back", new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.17f), 18, Color.white, TextAnchor.MiddleCenter, () => OnBackButtonClicked?.Invoke());
        }

        private void CreateInputField(string label, string initialValue, float verticalPosition, 
            System.Action<string> onValueChanged, out InputField inputField)
        {
            GameObject container = new GameObject($"{label}Container");
            container.transform.SetParent(menuObject.transform, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, verticalPosition);
            containerRect.anchorMax = new Vector2(0.9f, verticalPosition + 0.15f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Label
            CreateTextElement("Label", label, 16, Color.white, TextAnchor.UpperLeft, 1f, true);

            RectTransform labelRect = menuObject.transform.Find("Label").GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            // Input Field
            GameObject inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(container.transform, false);
            
            Image inputImage = inputObj.AddComponent<Image>();
            inputImage.color = new Color(0.2f, 0.2f, 0.2f);

            inputField = inputObj.AddComponent<InputField>();
            inputField.textComponent = CreateInputText(inputObj.transform);
            inputField.text = initialValue;
            inputField.onValueChanged.AddListener((value) => onValueChanged(value));

            RectTransform inputRect = inputObj.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0.5f);
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;
        }

        private Text CreateInputText(Transform parent)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);
            
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(5, 2);
            rect.offsetMax = new Vector2(-5, -2);
            
            return text;
        }
    }
}
