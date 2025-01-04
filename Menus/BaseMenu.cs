using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TwitchChat.Menus
{
    public abstract class BaseMenu
    {
        protected GameObject menuObject;
        protected RectTransform rectTransform;
        protected GameObject textInputField;
        protected GameObject scrollableArea;
        protected RectTransform contentRectTransform;
        protected ScrollRect scrollRect;  // Add this field at the top with other fields

        // Add fields for minimize functionality
        protected bool isMinimized = false;
        protected Vector2 originalSize;
        protected Button minimizeButton;

        // Add delegate and event for back button
        public delegate void OnBackButtonClickedHandler();
        public event OnBackButtonClickedHandler OnBackButtonClicked;

        public GameObject MenuObject => menuObject;

        // Add these fields near the top of the class
        protected bool showBackButton = true;
        protected bool showMinimizeButton = true;

        protected BaseMenu(Transform parent)
        {
            CreateBaseMenu(parent);
        }

        protected virtual void CreateBaseMenu(Transform parent)
        {
            menuObject = new GameObject(GetType().Name);
            menuObject.transform.SetParent(parent, false);
            
            Image panelImage = menuObject.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.3f);
            
            rectTransform = menuObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = Vector2.zero;

            // Create title
            Title.CreateTitle(menuObject.transform, GetType().Name.Replace("Menu", ""), 18, Color.white, TextAnchor.UpperCenter);

            // Only create buttons if they're enabled
            if (showBackButton)
            {
                Button backButton = UIElementFactory.CreateButton(menuObject.transform, " X ", 0, 0, Color.white, () => OnBackButtonClicked?.Invoke());
                RectTransform backRect = backButton.GetComponent<RectTransform>();
                backRect.anchorMin = new Vector2(1, 1);
                backRect.anchorMax = new Vector2(1, 1);
                backRect.pivot = new Vector2(1, 1);
                // backRect.anchoredPosition = new Vector2(-5, -5);
            }

            if (showMinimizeButton)
            {
                minimizeButton = UIElementFactory.CreateButton(menuObject.transform, " − ", 0, 0, Color.white, OnMinimizeClick);
                RectTransform minimizeRect = minimizeButton.GetComponent<RectTransform>();
                minimizeRect.anchorMin = new Vector2(0, 1);
                minimizeRect.anchorMax = new Vector2(0, 1);
                minimizeRect.pivot = new Vector2(0, 1);
                // minimizeRect.anchoredPosition = new Vector2(5, -5);
            }
        }

        protected virtual void OnMinimizeClick()
        {
            if (!isMinimized)
            {
                // Store original size and anchors before minimizing
                originalSize = rectTransform.sizeDelta;
                
                // Get panel image and adjust transparency
                // Image panelImage = menuObject.GetComponent<Image>();
                // if (panelImage != null)
                // {
                //     panelImage.color = new Color(0, 0, 0, 0.3f);
                // }
                
                // Collapse the menu, but keep title and buttons visible
                foreach (Transform child in menuObject.transform)
                {
                    // Skip the minimize button, title, and back button
                    if (child.gameObject != minimizeButton.gameObject && 
                        !child.name.Equals("Title") && 
                        !child.name.Equals(" X Button"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
                
                // Set the height to exactly 30 pixels from the top
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, -30);
                rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, 0);
                
                minimizeButton.GetComponentInChildren<Text>().text = " + ";
            }
            else
            {
                // Restore the menu
                // Image panelImage = menuObject.GetComponent<Image>();
                // if (panelImage != null)
                // {
                //     panelImage.color = new Color(0, 0, 0, 0.3f);
                // }
                
                foreach (Transform child in menuObject.transform)
                {
                    child.gameObject.SetActive(true);
                }
                
                // Restore original anchors and position
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                
                minimizeButton.GetComponentInChildren<Text>().text = " − ";
            }
            
            isMinimized = !isMinimized;
        }

        protected GameObject CreateScrollableArea(int width = 180, int height = 250)
        {
            return ScrollableArea.CreateScrollableArea(menuObject.transform, width, height);
        }

        protected GameObject CreateSection(string name, int yPosition, int height, bool createLabel = true)
        {
            return Section.CreateSection(menuObject.transform, name, yPosition, height, createLabel);
        }

        public virtual void AddMessage(string username, string message)
        {
            MessageManager.AddMessage(contentRectTransform, scrollRect, username, message);
        }

        public virtual void Show() => menuObject.SetActive(true);
        public virtual void Hide() => menuObject.SetActive(false);
    }
}
