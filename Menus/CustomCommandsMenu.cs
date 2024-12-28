using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TwitchChat.Menus
{
    public class CustomCommandsMenu : BaseMenu
    {
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public CustomCommandsMenu(Transform parent) : base(parent)
        {
            CreateCustomCommandsMenu();
        }

        private void CreateCustomCommandsMenu()
        {
            // Title
            CreateTitle("Custom Commands", 24, Color.white, TextAnchor.UpperCenter);

            // Custom Command 1
            CreateCustomCommandSet("Custom Command 1", 0.8f,
                Settings.Instance.customCommand1Active,
                Settings.Instance.customCommand1Trigger,
                Settings.Instance.customCommand1Response,
                Settings.Instance.customCommand1IsWhisper,
                (active) => Settings.Instance.customCommand1Active = active,
                (trigger) => Settings.Instance.customCommand1Trigger = trigger,
                (response) => Settings.Instance.customCommand1Response = response,
                (whisper) => Settings.Instance.customCommand1IsWhisper = whisper);

            // Repeat for commands 2-5 at different vertical positions (0.6f, 0.4f, 0.2f, 0.0f)

            Button backButton = CreateButton("BackButton", "Back", 
                new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.17f));
            backButton.onClick.AddListener(() => OnBackButtonClicked?.Invoke());
        }

        private void CreateCustomCommandSet(string label, float verticalPosition,
            bool initialActiveState, string initialTrigger, string initialResponse, bool initialWhisperState,
            UnityAction<bool> onActiveChanged, UnityAction<string> onTriggerChanged,
            UnityAction<string> onResponseChanged, UnityAction<bool> onWhisperChanged)
        {
            GameObject container = new GameObject($"{label}Container");
            container.transform.SetParent(menuObject.transform, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, verticalPosition);
            containerRect.anchorMax = new Vector2(0.9f, verticalPosition + 0.15f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Active Toggle
            Toggle activeToggle = CreateToggle("Active", initialActiveState, onActiveChanged,
                new Vector2(0, 0.5f), new Vector2(0.1f, 1f));

            // Trigger Input
            InputField triggerInput = CreateInputField("Trigger", initialTrigger, onTriggerChanged,
                new Vector2(0.12f, 0.5f), new Vector2(0.3f, 1f));

            // Response Input
            InputField responseInput = CreateInputField("Response", initialResponse, onResponseChanged,
                new Vector2(0.32f, 0.5f), new Vector2(0.9f, 1f));

            // Whisper Toggle
            Toggle whisperToggle = CreateToggle("Whisper", initialWhisperState, onWhisperChanged,
                new Vector2(0.92f, 0.5f), new Vector2(1f, 1f));
        }

        private Toggle CreateToggle(string name, bool initialState, UnityAction<bool> onValueChanged,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject toggleObj = new GameObject(name);
            toggleObj.transform.SetParent(menuObject.transform, false);
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = initialState;
            toggle.onValueChanged.AddListener(onValueChanged);

            RectTransform rect = toggleObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return toggle;
        }

        private InputField CreateInputField(string name, string initialValue, UnityAction<string> onValueChanged,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(menuObject.transform, false);
            
            // Create background image
            Image backgroundImage = inputObj.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Create child text area
            GameObject textArea = new GameObject("Text");
            textArea.transform.SetParent(inputObj.transform, false);
            Text inputText = textArea.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            inputText.fontSize = 14;
            inputText.color = Color.white;
            
            // Set up the RectTransform for the text area
            RectTransform textRect = textArea.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(5, 2);
            textRect.offsetMax = new Vector2(-5, -2);

            // Set up the InputField
            InputField input = inputObj.AddComponent<InputField>();
            input.textComponent = inputText;
            input.text = initialValue;
            input.onValueChanged.AddListener(onValueChanged);

            // Set up the main RectTransform
            RectTransform rect = inputObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return input;
        }
    }
}
