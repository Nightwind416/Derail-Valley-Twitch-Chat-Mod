using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// The colours of one panel on one display. Opened from the gear on that panel's title row, and back
    /// returns to it.
    /// </summary>
    /// <remarks>
    /// The rows step rather than slide for the same reason the Wrist Adjust rows do: a slider in VR is
    /// driven by touching it, which walks the value along in tenths and wraps round at the end. Twelve
    /// stepped rows would not fit on a display, so one button chooses which of the three things is being
    /// coloured and four rows colour it.
    /// </remarks>
    public class AppearancePanel : PanelConstructor.BasePanel
    {
        /// <summary>How far a channel steps per press of its fine and its coarse button, out of one.</summary>
        private const float Step = 0.05f;
        private const float StepCoarse = 0.25f;

        /// <summary>Which of a panel's three colours is being edited.</summary>
        private enum Target
        {
            Panel,
            Section,
            Button
        }

        private readonly List<Readout> readouts = new();

        private PanelHost? target;
        private string targetPanelId = string.Empty;
        private string returnTo = "Main";
        private Target editing = Target.Panel;

        private Text? subjectLabel;
        private Text? targetLabel;
        private Image? swatch;
        private Text? sourceLabel;

        public AppearancePanel(Transform parent) : base(parent)
        {
            CreateRows();
            OnBackButtonClicked += GoBack;
        }

        /// <summary>
        /// No gear of its own: this panel is the gear, and one here would send the player round in a circle
        /// editing the editor.
        /// </summary>
        public override void Bind(PanelHost host, string id)
        {
            base.Bind(host, id);
            gearButton?.gameObject.SetActive(false);
        }

        /// <summary>
        /// Points the panel at one panel on one display, and remembers where back should go.
        /// </summary>
        public void Edit(PanelHost host, string panelId)
        {
            target = host;
            targetPanelId = panelId;
            returnTo = panelId;
            UpdateValues();
        }

        private void CreateRows()
        {
            subjectLabel = PanelConstructor.Label.Create(panelObject.transform, string.Empty, 6, 24, Color.cyan);

            UnityEngine.UI.Button targetButton = PanelConstructor.Button.Create(
                panelObject.transform, "Colouring: Panel", 120, 48, Color.white, CycleTarget);
            targetLabel = targetButton.GetComponentInChildren<Text>();

            CreateRow("Red", 78, () => Current.r, value => SetChannel(0, value));
            CreateRow("Green", 104, () => Current.g, value => SetChannel(1, value));
            CreateRow("Blue", 130, () => Current.b, value => SetChannel(2, value));
            CreateRow("Alpha", 156, () => Current.a, value => SetChannel(3, value));

            GameObject preview = new("Preview");
            preview.transform.SetParent(panelObject.transform, false);
            swatch = preview.AddComponent<Image>();

            RectTransform previewRect = preview.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0, 1);
            previewRect.anchorMax = new Vector2(0, 1);
            previewRect.pivot = new Vector2(0, 1);
            previewRect.sizeDelta = new Vector2(60, 24);
            previewRect.anchoredPosition = new Vector2(10, -184);

            sourceLabel = PanelConstructor.Label.Create(panelObject.transform, string.Empty, 80, 190, Color.gray);

            PanelConstructor.Button.Create(panelObject.transform, "Copy to the whole display", 120, 220, Color.white, CopyToDisplay);
            PanelConstructor.Button.Create(panelObject.transform, "Back to the shared colours", 120, 246, Color.white, UseDefaults);

            PanelConstructor.DisplayText.Create(panelObject.transform,
                "These colours belong to this one panel on this one display. Every panel with none of its own follows the shared colours on the Config panels.",
                8, 270, Color.white, 4, 10);
        }

        /// <summary>Builds one channel row: a name, a coarse and a fine step either side of the number.</summary>
        private void CreateRow(string name, int y, Func<float> read, Action<float> write)
        {
            PanelConstructor.Label.Create(panelObject.transform, name, 6, y - 10, Color.white);

            CreateStepButton("<<", 92, y, () => write(read() - StepCoarse));
            CreateStepButton("<", 122, y, () => write(read() - Step));
            CreateStepButton(">", 178, y, () => write(read() + Step));
            CreateStepButton(">>", 208, y, () => write(read() + StepCoarse));

            readouts.Add(new Readout(CreateReadout(150, y), read));
        }

        private void CreateStepButton(string text, int x, int y, Action step)
        {
            PanelConstructor.Button.Create(panelObject.transform, text, x, y, Color.white, () => step(), 18);
        }

        private Text CreateReadout(int x, int y)
        {
            GameObject fieldObject = new("Value");
            fieldObject.transform.SetParent(panelObject.transform, false);

            Text field = fieldObject.AddComponent<Text>();
            field.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            field.fontSize = 12;
            field.alignment = TextAnchor.MiddleCenter;
            field.color = Color.cyan;

            RectTransform rect = fieldObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(48, 20);
            rect.anchoredPosition = new Vector2(x, -y);

            return field;
        }

        // ------------------------------------------------------------------
        // The colours themselves
        // ------------------------------------------------------------------

        /// <summary>The colour being edited, whether it is this panel's own or the shared default.</summary>
        private Color Current
        {
            get
            {
                if (target == null)
                {
                    return Color.black;
                }

                PanelAppearance effective = Settings.Instance.EffectiveAppearance(target.AppearanceKey, targetPanelId);
                return editing switch
                {
                    Target.Section => effective.sectionColor,
                    Target.Button => effective.buttonColor,
                    _ => effective.panelColor
                };
            }
        }

        /// <summary>
        /// Writes one channel of the colour being edited. The first change is what gives this panel colours
        /// of its own; until then it was simply following the shared ones.
        /// </summary>
        private void SetChannel(int channel, float value)
        {
            if (target == null)
            {
                return;
            }

            PanelAppearance entry = Settings.Instance.GetOrAddAppearance(target.AppearanceKey, targetPanelId);
            Color colour = editing switch
            {
                Target.Section => entry.sectionColor,
                Target.Button => entry.buttonColor,
                _ => entry.panelColor
            };

            colour[channel] = Mathf.Clamp01(value);

            switch (editing)
            {
                case Target.Section:
                    entry.sectionColor = colour;
                    break;
                case Target.Button:
                    entry.buttonColor = colour;
                    break;
                default:
                    entry.panelColor = colour;
                    break;
            }

            Settings.Instance.RequestSave();
            Repaint();
        }

        private void CycleTarget()
        {
            editing = editing switch
            {
                Target.Panel => Target.Section,
                Target.Section => Target.Button,
                _ => Target.Panel
            };

            UpdateValues();
        }

        /// <summary>Gives every panel on this display the colours this one has.</summary>
        private void CopyToDisplay()
        {
            if (target == null)
            {
                return;
            }

            PanelAppearance source = Settings.Instance.EffectiveAppearance(target.AppearanceKey, targetPanelId);

            foreach (PanelConstructor.BasePanel panel in target.Panels)
            {
                PanelAppearance entry = Settings.Instance.GetOrAddAppearance(target.AppearanceKey, panel.PanelId);
                entry.panelColor = source.panelColor;
                entry.sectionColor = source.sectionColor;
                entry.buttonColor = source.buttonColor;
                panel.ApplyAppearance();
            }

            Settings.Instance.RequestSave();
            UpdateValues();
            Main.LogEntry("Appearance", $"Colours copied to every panel on {target.Name}.");
        }

        /// <summary>Puts this panel back on the shared colours by forgetting the ones it was given.</summary>
        private void UseDefaults()
        {
            if (target == null)
            {
                return;
            }

            Settings.Instance.ResetAppearance(target.AppearanceKey, targetPanelId);
            Repaint();
        }

        /// <summary>Repaints the panel being edited, which is behind this one, and this one's readouts.</summary>
        private void Repaint()
        {
            target?.GetPanel(targetPanelId)?.ApplyAppearance();
            UpdateValues();
        }

        private void GoBack()
        {
            if (target != null)
            {
                MenuManager.Instance.OnPanelButtonClicked(returnTo, target);
            }
        }

        /// <inheritdoc/>
        public override void Tick(float deltaTime) => UpdateValues();

        private void UpdateValues()
        {
            if (subjectLabel != null)
            {
                subjectLabel.text = target == null
                    ? "Nothing to colour"
                    : $"{targetPanelId} on {DisplayName(target)}";
            }

            if (targetLabel != null)
            {
                targetLabel.text = $"Colouring: {editing}";
            }

            Color colour = Current;

            if (swatch != null)
            {
                swatch.color = colour;
            }

            if (sourceLabel != null)
            {
                bool own = target != null
                    && Settings.Instance.FindAppearance(target.AppearanceKey, targetPanelId) != null;
                sourceLabel.text = own ? "its own colours" : "following the shared colours";
            }

            foreach (Readout readout in readouts)
            {
                readout.Field.text = Mathf.RoundToInt(readout.Read() * 100f).ToString();
            }
        }

        /// <summary>What to call the display in a sentence: the hand panel, or which cab display it is.</summary>
        private static string DisplayName(PanelHost host)
        {
            return host is WristPanelHost ? "the hand panel" : host.Name.Replace("CabDisplay", "Display ");
        }

        private sealed class Readout
        {
            internal Readout(Text field, Func<float> read)
            {
                Field = field;
                Read = read;
            }

            internal Text Field { get; }
            internal Func<float> Read { get; }
        }
    }
}
