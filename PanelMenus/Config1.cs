using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Manages the configuration panel interface in the mod's settings menu.
    /// Provides a framework for future configuration options and settings.
    /// Currently serves as a placeholder for upcoming configuration features.
    /// </summary>
    public class Config1Panel : PanelConstructor.BasePanel
    {
        /// <summary>
        /// Initializes a new instance of the ConfigurationPanel.
        /// </summary>
        /// <param name="parent">The parent transform this panel will be attached to.</param>
        private GameObject? panelColorSection;
        private GameObject? sectionColorSection;
        private Slider? redSlider;
        private Slider? greenSlider;
        private Slider? blueSlider;
        private Slider? alphaSlider;

        public Config1Panel(Transform parent) : base(parent)
        {
            PanelColorSection();
            SectionColorSection();
        }

        /// <summary>
        /// Creates the configuration menu interface with navigation buttons.
        /// </summary>
        private void PanelColorSection()
        {
            // Panel Color Section
            panelColorSection = PanelConstructor.Section.Create(panelObject.transform, "Panel Color", 25, 130);

            // Panel Color Label
            // PanelConstructor.Label.Create(panelColorSection.transform, "Panel Color", 5, 5, Color.white);

            // Red
            PanelConstructor.Label.Create(panelColorSection.transform, "R", 5, 27, Color.white);
            redSlider = PanelConstructor.Slider.Create(panelColorSection.transform, 0, 25, 0f, 100f, Settings.Instance.panelColor.r * 100f);
            
            redSlider.onValueChanged.AddListener((value) => {
                Color newColor = Settings.Instance.panelColor;
                newColor.r = value / 100f;
                Settings.Instance.panelColor = newColor;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllPanelBackgrounds();
            });

            // Green
            PanelConstructor.Label.Create(panelColorSection.transform, "G", 5, 52, Color.white);
            greenSlider = PanelConstructor.Slider.Create(panelColorSection.transform, 0, 50, 0f, 100f, Settings.Instance.panelColor.g * 100f);
            
            greenSlider.onValueChanged.AddListener((value) => {
                Color newColor = Settings.Instance.panelColor;
                newColor.g = value / 100f;
                Settings.Instance.panelColor = newColor;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllPanelBackgrounds();
            });

            // Blue
            PanelConstructor.Label.Create(panelColorSection.transform, "B", 5, 77, Color.white);
            blueSlider = PanelConstructor.Slider.Create(panelColorSection.transform, 0, 75, 0f, 100f, Settings.Instance.panelColor.b * 100f);
            
            blueSlider.onValueChanged.AddListener((value) => {
                Color newColor = Settings.Instance.panelColor;
                newColor.b = value / 100f;
                Settings.Instance.panelColor = newColor;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllPanelBackgrounds();
            });

            // Alpha
            PanelConstructor.Label.Create(panelColorSection.transform, "Alpha", 5, 100, Color.white);
            alphaSlider = PanelConstructor.Slider.Create(panelColorSection.transform, 0, 110, 0f, 100f, Settings.Instance.panelColor.a * 100f);
            
            alphaSlider.onValueChanged.AddListener((value) => {
                Color newColor = Settings.Instance.panelColor;
                newColor.a = value / 100f;
                Settings.Instance.panelColor = newColor;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllPanelBackgrounds();
            });
        }

        private void SectionColorSection()
        {
            // Section Color Section
            sectionColorSection = PanelConstructor.Section.Create(panelObject.transform, "Section Color", 160, 130);

            // Section Color Label
            // PanelConstructor.Label.Create(sectionColorSection.transform, "Section Color", 5, 5, Color.white);

            // Red
            PanelConstructor.Label.Create(sectionColorSection.transform, "R", 5, 27, Color.white);
            redSlider = PanelConstructor.Slider.Create(sectionColorSection.transform, 0, 25, 0f, 100f, Settings.Instance.sectionColor.r * 100f);
            
            redSlider.onValueChanged.AddListener((value) => {
                Color newColor = Settings.Instance.sectionColor;
                newColor.r = value / 100f;
                Settings.Instance.sectionColor = newColor;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllSectionBackgrounds();
            });

            // Green
            PanelConstructor.Label.Create(sectionColorSection.transform, "G", 5, 52, Color.white);
            greenSlider = PanelConstructor.Slider.Create(sectionColorSection.transform, 0, 50, 0f, 100f, Settings.Instance.sectionColor.g * 100f);
            
            greenSlider.onValueChanged.AddListener((value) => {
                Color newColor = Settings.Instance.sectionColor;
                newColor.g = value / 100f;
                Settings.Instance.sectionColor = newColor;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllSectionBackgrounds();
            });

            // Blue
            PanelConstructor.Label.Create(sectionColorSection.transform, "B", 5, 77, Color.white);
            blueSlider = PanelConstructor.Slider.Create(sectionColorSection.transform, 0, 75, 0f, 100f, Settings.Instance.sectionColor.b * 100f);
            
            blueSlider.onValueChanged.AddListener((value) => {
                Color newColor = Settings.Instance.sectionColor;
                newColor.b = value / 100f;
                Settings.Instance.sectionColor = newColor;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllSectionBackgrounds();
            });

            // Alpha
            PanelConstructor.Label.Create(sectionColorSection.transform, "Alpha", 5, 100, Color.white);
            alphaSlider = PanelConstructor.Slider.Create(sectionColorSection.transform, 0, 110, 0f, 100f, Settings.Instance.sectionColor.a * 100f);

            alphaSlider.onValueChanged.AddListener((value) => {
                Color newColor = Settings.Instance.sectionColor;
                newColor.a = value / 100f;
                Settings.Instance.sectionColor = newColor;
                Settings.Instance.Save(Main.ModEntry);
                MenuManager.Instance.UpdateAllSectionBackgrounds();
            });
        }
    }
}
