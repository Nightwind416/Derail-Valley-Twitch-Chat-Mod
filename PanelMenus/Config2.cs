using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Manages the configuration panel interface in the mod's settings menu.
    /// Provides a framework for future configuration options and settings.
    /// Currently serves as a placeholder for upcoming configuration features.
    /// </summary>
    public class Config2Panel : PanelConstructor.BasePanel
    {
        /// <summary>
        /// Initializes a new instance of the ConfigurationPanel.
        /// </summary>
        /// <param name="parent">The parent transform this panel will be attached to.</param>
        private GameObject? buttonSettingsSection;
        private Slider? redSlider;
        private Slider? greenSlider;
        private Slider? blueSlider;
        private Slider? alphaSlider;

        public Config2Panel(Transform parent) : base(parent)
        {
            ButtonColorSection();
            ResetColorsSection();
        }

        /// <summary>
        /// Creates the configuration menu interface with navigation buttons.
        /// </summary>
        private void ButtonColorSection()
        {
            // Button Color Section
            buttonSettingsSection = PanelConstructor.Section.Create(panelObject.transform, "Button Color", 25, 130);

            // Button Color Label
            // PanelConstructor.Label.Create(buttonSettingsSection.transform, "Button Color", 5, 5, Color.white);

            // Red
            PanelConstructor.Label.Create(buttonSettingsSection.transform, "R", 5, 27, Color.white);
            redSlider = PanelConstructor.Slider.Create(buttonSettingsSection.transform, 0, 25, 0f, 100f, Settings.Instance.buttonColor.r * 100f);
            
            redSlider.onValueChanged.AddListener((value) => {
                Color newColor = Settings.Instance.buttonColor;
                newColor.r = value / 100f;
                Settings.Instance.buttonColor = newColor;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllButtonColors();
            });

            // Green
            PanelConstructor.Label.Create(buttonSettingsSection.transform, "G", 5, 52, Color.white);
            greenSlider = PanelConstructor.Slider.Create(buttonSettingsSection.transform, 0, 50, 0f, 100f, Settings.Instance.buttonColor.g * 100f);
            
            greenSlider.onValueChanged.AddListener((value) => {
                Color newColor = Settings.Instance.buttonColor;
                newColor.g = value / 100f;
                Settings.Instance.buttonColor = newColor;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllButtonColors();
            });

            // Blue
            PanelConstructor.Label.Create(buttonSettingsSection.transform, "B", 5, 77, Color.white);
            blueSlider = PanelConstructor.Slider.Create(buttonSettingsSection.transform, 0, 75, 0f, 100f, Settings.Instance.buttonColor.b * 100f);
            
            blueSlider.onValueChanged.AddListener((value) => {
                Color newColor = Settings.Instance.buttonColor;
                newColor.b = value / 100f;
                Settings.Instance.buttonColor = newColor;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllButtonColors();
            });

            // Alpha
            PanelConstructor.Label.Create(buttonSettingsSection.transform, "Alpha", 5, 100, Color.white);
            alphaSlider = PanelConstructor.Slider.Create(buttonSettingsSection.transform, 0, 110, 0f, 100f, Settings.Instance.buttonColor.a * 100f);
            
            alphaSlider.onValueChanged.AddListener((value) => {
                Color newColor = Settings.Instance.buttonColor;
                newColor.a = value / 100f;
                Settings.Instance.buttonColor = newColor;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllButtonColors();
            });
        }
        private void ResetColorsSection()
        {
            // Reset Colors Section
            GameObject resetSection = PanelConstructor.Section.Create(panelObject.transform, "Reset Colors", 160, 130);
            
            // Reset Panel Color button
            PanelConstructor.Button.Create(resetSection.transform, "Reset Panel Color", 80, 35, clicked: () => {
                Settings.Instance.panelColor = new Color(0, 0, 0, 0.3f);
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllPanelBackgrounds();
            });
            
            // Reset Section Color button
            PanelConstructor.Button.Create(resetSection.transform, "Reset Section Color", 80, 60, clicked: () => {
                Settings.Instance.sectionColor = new Color(0, 0, 0, 0.1f);
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllSectionBackgrounds();
            });
            
            // Reset Button Color button
            PanelConstructor.Button.Create(resetSection.transform, "Reset Button Color", 80, 85, clicked: () => {
                Settings.Instance.buttonColor = new Color(0, 0, 0, 0.5f);
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllButtonColors();
                
                // Update sliders to reflect new values
                if (redSlider != null) redSlider.value = Settings.Instance.buttonColor.r * 100f;
                if (greenSlider != null) greenSlider.value = Settings.Instance.buttonColor.g * 100f;
                if (blueSlider != null) blueSlider.value = Settings.Instance.buttonColor.b * 100f;
                if (alphaSlider != null) alphaSlider.value = Settings.Instance.buttonColor.a * 100f;
            });
            
            // Reset All Colors button
            PanelConstructor.Button.Create(resetSection.transform, "Reset All Colors", 80, 110, clicked: () => {
                // Reset all colors
                Settings.Instance.panelColor = new Color(0, 0, 0, 0.3f);
                Settings.Instance.sectionColor = new Color(0, 0, 0, 0.1f);
                Settings.Instance.buttonColor = new Color(0, 0, 0, 0.5f);
                
                // Update all UI elements with new colors
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllPanelBackgrounds();
                MenuManager.Instance.UpdateAllSectionBackgrounds();
                MenuManager.Instance.UpdateAllButtonColors();
                
                // Update sliders to reflect new values
                if (redSlider != null) redSlider.value = Settings.Instance.buttonColor.r * 100f;
                if (greenSlider != null) greenSlider.value = Settings.Instance.buttonColor.g * 100f;
                if (blueSlider != null) blueSlider.value = Settings.Instance.buttonColor.b * 100f;
                if (alphaSlider != null) alphaSlider.value = Settings.Instance.buttonColor.a * 100f;
            });
        }
    }
}
