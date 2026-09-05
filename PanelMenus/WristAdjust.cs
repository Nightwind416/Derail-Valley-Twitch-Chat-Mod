using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Places the wrist panel by hand. Six rows slide and turn it against the hand it is attached to, and in VR
    /// the panel itself can be taken in the free hand and put where it is wanted while this panel is open.
    /// </summary>
    /// <remarks>
    /// The rows step rather than slide because a slider in VR is driven by touching it, which walks the value
    /// along in tenths and wraps round at the end: no use for lining a panel up on a hand. Every host carries a
    /// copy, so the wrist panel can also be placed from a cab display, which is easier to press accurately.
    /// </remarks>
    public class WristAdjustPanel : PanelConstructor.BasePanel
    {
        /// <summary>Metres a move row steps, per press of its fine and its coarse button.</summary>
        private const float MoveStep = 0.005f;
        private const float MoveStepCoarse = 0.03f;

        /// <summary>Degrees a turn row steps, per press of its fine and its coarse button.</summary>
        private const float TurnStep = 5f;
        private const float TurnStepCoarse = 45f;

        /// <summary>How far from the hand the panel may be put, in metres, however it is placed.</summary>
        public const float MoveLimit = 0.5f;

        private readonly List<Readout> readouts = new();
        private Text? anchorLine;
        private Text? targetLabel;
        private string anchorSummary = string.Empty;
        private bool shownPlacingButton;

        public WristAdjustPanel(Transform parent) : base(parent)
        {
            CreateRows();
        }

        private void CreateRows()
        {
            anchorLine = PanelConstructor.Label.Create(panelObject.transform, "Attached to:", 6, 24, Color.cyan);

            UnityEngine.UI.Button target = PanelConstructor.Button.Create(
                panelObject.transform, "Placing: floating menus", 120, 48, Color.white,
                () => MenuManager.Instance.SetPlacingWristButton(!MenuManager.Instance.PlacingWristButton));
            targetLabel = target.GetComponentInChildren<Text>();

            CreateRow("Across", 74, MoveStep, MoveStepCoarse, "0.000",
                () => Nudge.x, value => SetNudge(0, value));
            CreateRow("Along", 100, MoveStep, MoveStepCoarse, "0.000",
                () => Nudge.y, value => SetNudge(1, value));
            CreateRow("Out", 126, MoveStep, MoveStepCoarse, "0.000",
                () => Nudge.z, value => SetNudge(2, value));
            CreateRow("Turn X", 156, TurnStep, TurnStepCoarse, "0",
                () => Tilt.x, value => SetTilt(0, value));
            CreateRow("Turn Y", 182, TurnStep, TurnStepCoarse, "0",
                () => Tilt.y, value => SetTilt(1, value));
            CreateRow("Turn Z", 208, TurnStep, TurnStepCoarse, "0",
                () => Tilt.z, value => SetTilt(2, value));

            PanelConstructor.Button.Create(panelObject.transform, "Reset this one", 120, 238, Color.white, ResetPlacement);

            PanelConstructor.DisplayText.Create(panelObject.transform,
                "Metres and degrees, each surface placed on its own. In VR, hold the grip on your free hand next to the one being placed to carry it, and let go where you want it.",
                8, 258, Color.white, 5, 10);
        }

        /// <summary>The placement being edited: the button's while that is what is being placed, or the menus'.</summary>
        private static Vector3 Nudge => MenuManager.Instance.PlacingWristButton
            ? Settings.Instance.wristButtonOffset
            : Settings.Instance.wristMenuOffset;

        private static Vector3 Tilt => MenuManager.Instance.PlacingWristButton
            ? Settings.Instance.wristButtonAngle
            : Settings.Instance.wristMenuAngle;

        /// <summary>
        /// Builds one row: a name, a coarse and a fine step either side of the number it is changing.
        /// </summary>
        private void CreateRow(string name, int y, float step, float coarseStep, string format, Func<float> read, Action<float> write)
        {
            PanelConstructor.Label.Create(panelObject.transform, name, 6, y - 10, Color.white);

            CreateStepButton("<<", 92, y, () => write(read() - coarseStep));
            CreateStepButton("<", 122, y, () => write(read() - step));
            CreateStepButton(">", 178, y, () => write(read() + step));
            CreateStepButton(">>", 208, y, () => write(read() + coarseStep));

            readouts.Add(new Readout(CreateReadout(150, y), read, format));
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

        /// <summary>
        /// Redraws the numbers. They move on their own as well as by pressing the rows, since the panel can be
        /// carried into place by hand, so this runs while the panel is on show.
        /// </summary>
        public void UpdateValues()
        {
            foreach (Readout readout in readouts)
            {
                float value = readout.Read();
                if (readout.Field == null || Mathf.Approximately(value, readout.Last))
                {
                    continue;
                }

                readout.Last = value;
                readout.Field.text = value.ToString(readout.Format);
            }

            string summary = MenuManager.Instance.WristAnchorSummary;
            if (anchorLine != null && summary != anchorSummary)
            {
                anchorSummary = summary;
                anchorLine.text = "Attached to: " + summary;
            }

            bool placingButton = MenuManager.Instance.PlacingWristButton;
            if (targetLabel != null && placingButton != shownPlacingButton)
            {
                shownPlacingButton = placingButton;
                targetLabel.text = placingButton ? "Placing: hand button" : "Placing: floating menus";
            }
        }

        private static void ResetPlacement()
        {
            if (MenuManager.Instance.PlacingWristButton)
            {
                Settings.Instance.wristButtonOffset = Vector3.zero;
                Settings.Instance.wristButtonAngle = Vector3.zero;
            }
            else
            {
                Settings.Instance.wristMenuOffset = Vector3.zero;
                Settings.Instance.wristMenuAngle = Vector3.zero;
            }

            Settings.Instance.RequestSave();
            Main.LogEntry("WristPanel", MenuManager.Instance.PlacingWristButton
                ? "Hand button placement reset."
                : "Floating menu placement reset.");
        }

        private static void SetNudge(int axis, float value)
        {
            Vector3 nudge = Nudge;
            nudge[axis] = Mathf.Clamp(value, -MoveLimit, MoveLimit);

            if (MenuManager.Instance.PlacingWristButton)
            {
                Settings.Instance.wristButtonOffset = nudge;
            }
            else
            {
                Settings.Instance.wristMenuOffset = nudge;
            }
            Settings.Instance.RequestSave();
        }

        private static void SetTilt(int axis, float value)
        {
            Vector3 tilt = Tilt;
            tilt[axis] = Mathf.Repeat(value + 180f, 360f) - 180f;

            if (MenuManager.Instance.PlacingWristButton)
            {
                Settings.Instance.wristButtonAngle = tilt;
            }
            else
            {
                Settings.Instance.wristMenuAngle = tilt;
            }
            Settings.Instance.RequestSave();
        }

        /// <summary>One number on the panel, and where to read it from.</summary>
        private class Readout
        {
            public readonly Text Field;
            public readonly Func<float> Read;
            public readonly string Format;
            public float Last = float.NaN;

            public Readout(Text field, Func<float> read, string format)
            {
                Field = field;
                Read = read;
                Format = format;
            }
        }
    }
}
