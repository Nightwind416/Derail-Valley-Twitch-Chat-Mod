using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;  // Add this namespace

namespace TwitchChat.Menus
{
    public class CommandMessagesMenu : BaseMenu
    {
        private Toggle commandMessageToggle;
        private InputField commandMessageInput;
        private Toggle infoMessageToggle;
        private InputField infoMessageInput;

        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public CommandMessagesMenu(Transform parent) : base(parent)
        {
            CreateCommandMessagesMenu();
        }

        private void CreateCommandMessagesMenu()
        {
            // Title
            CreateTitle("Command Messages");

            // Command Message
            CreateToggleWithInput("!commands Message", Settings.Instance.commandMessageActive, 
                Settings.Instance.commandMessage, 0.7f,
                isActive => Settings.Instance.commandMessageActive = isActive,
                value => Settings.Instance.commandMessage = value,
                out commandMessageToggle, out commandMessageInput);

            // Info Message
            CreateToggleWithInput("!info Message", Settings.Instance.infoMessageActive, 
                Settings.Instance.infoMessage, 0.4f,
                isActive => Settings.Instance.infoMessageActive = isActive,
                value => Settings.Instance.infoMessage = value,
                out infoMessageToggle, out infoMessageInput);

            // Back button
            Button backButton = CreateButton("BackButton", "Back", 
                new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.17f));
            backButton.onClick.AddListener(() => OnBackButtonClicked?.Invoke());
        }

        private void CreateTitle(string titleText)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(menuObject.transform, false);
            Text title = titleObj.AddComponent<Text>();
            title.text = titleText;
            title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.fontSize = 24;
            title.alignment = TextAnchor.UpperCenter;
            title.color = Color.white;
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, 0);
        }

        private void CreateToggleWithInput(string label, bool initialToggleState, string initialInputValue, 
            float verticalPosition, UnityAction<bool> onToggleChanged, UnityAction<string> onInputChanged,
            out Toggle toggle, out InputField inputField)
        {
            GameObject container = new GameObject($"{label}Container");
            container.transform.SetParent(menuObject.transform, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, verticalPosition);
            containerRect.anchorMax = new Vector2(0.9f, verticalPosition + 0.2f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Toggle
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(container.transform, false);
            toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = initialToggleState;
            toggle.onValueChanged.AddListener(onToggleChanged);

            // Toggle Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(toggleObj.transform, false);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = Color.white;

            // Toggle Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleObj.transform, false);
            Image checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = Color.green;
            toggle.graphic = checkmarkImage;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);
            Text labelComponent = labelObj.AddComponent<Text>();
            labelComponent.text = label;
            labelComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelComponent.fontSize = 16;
            labelComponent.color = Color.white;

            // Input Field
            GameObject inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(container.transform, false);
            
            Image inputImage = inputObj.AddComponent<Image>();
            inputImage.color = new Color(0.2f, 0.2f, 0.2f);

            inputField = inputObj.AddComponent<InputField>();
            inputField.textComponent = CreateInputText(inputObj.transform);
            inputField.text = initialInputValue;
            inputField.onValueChanged.AddListener(onInputChanged);

            // Layout
            RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0, 0.5f);
            toggleRect.anchorMax = new Vector2(0.1f, 1f);
            toggleRect.offsetMin = Vector2.zero;
            toggleRect.offsetMax = Vector2.zero;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.12f, 0.5f);
            labelRect.anchorMax = new Vector2(1, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

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
