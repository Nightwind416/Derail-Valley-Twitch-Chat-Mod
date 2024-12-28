using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;  // Add this line

namespace TwitchChat.Menus
{
    public class DispatcherMessagesMenu : BaseMenu
    {
        private Toggle dispatcherMessageToggle;
        private InputField messageInput;

        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public DispatcherMessagesMenu(Transform parent) : base(parent)
        {
            CreateDispatcherMessagesMenu();
        }

        private void CreateDispatcherMessagesMenu()
        {
            CreateTitle("Dispatcher Messages", 24, Color.white, TextAnchor.UpperCenter);

            CreateToggleWithInput("Dispatcher Integration", Settings.Instance.dispatcherMessageActive,
                Settings.Instance.dispatcherMessage, 0.7f,
                isActive => Settings.Instance.dispatcherMessageActive = isActive,
                value => Settings.Instance.dispatcherMessage = value,
                out dispatcherMessageToggle, out messageInput);

            // Warning Text
            GameObject warningObj = new GameObject("WarningText");
            warningObj.transform.SetParent(menuObject.transform, false);
            Text warningText = warningObj.AddComponent<Text>();
            warningText.text = "Integration with Dispatcher Mod coming soon!";
            warningText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            warningText.fontSize = 16;
            warningText.color = Color.yellow;
            warningText.alignment = TextAnchor.MiddleCenter;

            RectTransform warningRect = warningObj.GetComponent<RectTransform>();
            warningRect.anchorMin = new Vector2(0.1f, 0.4f);
            warningRect.anchorMax = new Vector2(0.9f, 0.5f);
            warningRect.offsetMin = Vector2.zero;
            warningRect.offsetMax = Vector2.zero;

            Button backButton = CreateButton("BackButton", "Back", 
                new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.17f));
            backButton.onClick.AddListener(() => OnBackButtonClicked?.Invoke());
        }

        private void CreateToggleWithInput(string label, bool initialToggleState, string initialInputValue,
            float verticalPosition, UnityAction<bool> onToggleChanged, UnityAction<string> onInputChanged,
            out Toggle toggle, out InputField inputField)
        {
            // Create container with RectTransform
            GameObject container = new GameObject($"{label}Container");
            container.transform.SetParent(menuObject.transform, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, verticalPosition);
            containerRect.anchorMax = new Vector2(0.9f, verticalPosition + 0.15f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Create toggle
            GameObject toggleObj = new GameObject($"{label}Toggle");
            toggleObj.transform.SetParent(container.transform, false);
            toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = initialToggleState;
            toggle.onValueChanged.AddListener(onToggleChanged);

            // Setup toggle visuals
            RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0f, 0.5f);
            toggleRect.anchorMax = new Vector2(0.3f, 1f);
            toggleRect.offsetMin = Vector2.zero;
            toggleRect.offsetMax = Vector2.zero;

            // Add background image for toggle
            Image toggleImage = toggleObj.AddComponent<Image>();
            toggleImage.color = new Color(0.2f, 0.2f, 0.2f);

            // Add checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleObj.transform, false);
            Image checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = Color.white;
            RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = new Vector2(2, 2);
            checkmarkRect.offsetMax = new Vector2(-2, -2);
            toggle.graphic = checkmarkImage;

            // Add toggle label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);
            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(1.1f, 0f);
            labelRect.anchorMax = new Vector2(4f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            // Create input field
            GameObject inputObj = new GameObject($"{label}Input");
            inputObj.transform.SetParent(container.transform, false);
            
            // Add required components
            Image inputImage = inputObj.AddComponent<Image>();
            inputImage.color = new Color(0.2f, 0.2f, 0.2f);
            inputField = inputObj.AddComponent<InputField>();
            
            // Create input text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            Text inputText = textObj.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            inputText.fontSize = 14;
            inputText.color = Color.white;
            
            // Position input field
            RectTransform inputRect = inputObj.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.35f, 0.1f);
            inputRect.anchorMax = new Vector2(1f, 0.9f);
            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;

            // Setup input field
            inputField.textComponent = inputText;
            inputField.text = initialInputValue;
            inputField.onValueChanged.AddListener(onInputChanged);
        }
    }
}
