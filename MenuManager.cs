using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;

namespace TwitchChat
{
    public class MenuManager : MonoBehaviour
    {
        private static MenuManager? instance;
        private static GameObject? menuCanvas;
        private bool isMenuVisible = false;

        public static MenuManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new("CustomMenuManager");
                    instance = go.AddComponent<MenuManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private void Start()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, "Initializing MenuManager");
            CreateMenu();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                string methodName = MethodBase.GetCurrentMethod().Name;
                Main.LogEntry(methodName, "F7 key pressed - toggling menu");
                ToggleMenu();
            }
        }

        private void CreateMenu()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            Main.LogEntry(methodName, "Creating menu UI elements");

            if (menuCanvas != null)
            {
                Main.LogEntry(methodName, "Menu already exists, destroying old instance");
                Destroy(menuCanvas);
            }

            menuCanvas = new GameObject("CustomMenuCanvas");
            menuCanvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            menuCanvas.AddComponent<CanvasScaler>();
            menuCanvas.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(menuCanvas);

            // Create a basic panel
            GameObject panel = new("Panel");
            panel.transform.SetParent(menuCanvas.transform, false);
            
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.2f, 0.2f);
            panelRect.anchorMax = new Vector2(0.8f, 0.8f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Add a title text
            GameObject titleObj = new("Title");
            titleObj.transform.SetParent(panel.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "Custom Menu";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 24;
            titleText.alignment = TextAnchor.UpperCenter;
            titleText.color = Color.white;

            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(0, -50);
            titleRect.offsetMax = new Vector2(0, -10);

            menuCanvas.SetActive(isMenuVisible);
            Main.LogEntry(methodName, "Menu creation completed");
        }

        public void ToggleMenu()
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            isMenuVisible = !isMenuVisible;
            Main.LogEntry(methodName, $"Toggling menu visibility to: {isMenuVisible}");
            
            if (menuCanvas == null)
            {
                Main.LogEntry(methodName, "Menu canvas was null, recreating...");
                CreateMenu();
            }
            
            menuCanvas!.SetActive(isMenuVisible);
        }
    }
}
