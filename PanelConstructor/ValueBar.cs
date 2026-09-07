using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelConstructor
{
    /// <summary>
    /// Factory class for creating labelled bars that show a fraction of something, such as a throttle or
    /// brake position. Unlike <see cref="HorizontalBar"/>, which is a plain separator rule, this is a
    /// readout: a caption on the left, a track that fills from the left, and a value on the right.
    /// </summary>
    public static class ValueBar
    {
        /// <summary>How much of the row's width the caption takes, leaving the rest for the track.</summary>
        private const float LabelWidth = 70f;

        /// <summary>How much of the row's width the value on the right takes.</summary>
        private const float ValueWidth = 44f;

        private const int RowHeight = 16;

        /// <summary>
        /// Creates a labelled bar, empty to begin with. Set its value with <see cref="SetValue"/>.
        /// </summary>
        /// <param name="parent">Parent transform to attach the bar to</param>
        /// <param name="label">Caption shown to the left of the track</param>
        /// <param name="yPosition">Y position relative to parent</param>
        /// <param name="fillColor">Optional colour for the filled part of the track</param>
        /// <returns>Created GameObject, to be passed back to <see cref="SetValue"/></returns>
        public static GameObject Create(Transform parent, string label, int yPosition, Color? fillColor = null)
        {
            GameObject row = new($"{label}ValueBar");
            row.transform.SetParent(parent, false);

            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.pivot = new Vector2(0.5f, 1);
            rowRect.offsetMin = new Vector2(10, 0);
            rowRect.offsetMax = new Vector2(-10, 0);
            rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, RowHeight);
            rowRect.anchoredPosition = new Vector2(0, -yPosition);

            Text caption = NewText(row.transform, "Caption", label, TextAnchor.MiddleLeft, Color.white);
            RectTransform captionRect = caption.rectTransform;
            captionRect.anchorMin = new Vector2(0, 0);
            captionRect.anchorMax = new Vector2(0, 1);
            captionRect.pivot = new Vector2(0, 0.5f);
            captionRect.sizeDelta = new Vector2(LabelWidth, 0);
            captionRect.anchoredPosition = Vector2.zero;

            // The track spans whatever is left between the caption and the value
            GameObject track = new("Track");
            track.transform.SetParent(row.transform, false);
            track.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            RectTransform trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0, 0);
            trackRect.anchorMax = new Vector2(1, 1);
            trackRect.pivot = new Vector2(0, 0.5f);
            trackRect.offsetMin = new Vector2(LabelWidth, 3);
            trackRect.offsetMax = new Vector2(-ValueWidth, -3);

            GameObject fill = new("Fill");
            fill.transform.SetParent(track.transform, false);
            fill.AddComponent<Image>().color = fillColor ?? new Color(0.2f, 0.6f, 1f, 0.9f);

            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Text value = NewText(row.transform, "Value", "0%", TextAnchor.MiddleRight, Color.white);
            RectTransform valueRect = value.rectTransform;
            valueRect.anchorMin = new Vector2(1, 0);
            valueRect.anchorMax = new Vector2(1, 1);
            valueRect.pivot = new Vector2(1, 0.5f);
            valueRect.sizeDelta = new Vector2(ValueWidth, 0);
            valueRect.anchoredPosition = Vector2.zero;

            SetValue(row, 0f);
            return row;
        }

        /// <summary>
        /// Updates a bar made by <see cref="Create"/>. Passing something that is not one is ignored, so a
        /// caller need not guard against a bar that failed to build.
        /// </summary>
        /// <param name="bar">The object returned by <see cref="Create"/></param>
        /// <param name="fraction">How full the bar is, from 0 to 1; values outside that are clamped</param>
        /// <param name="valueText">Text shown on the right, or null for a percentage</param>
        public static void SetValue(GameObject? bar, float fraction, string? valueText = null)
        {
            if (bar == null)
            {
                return;
            }

            fraction = Mathf.Clamp01(float.IsNaN(fraction) ? 0f : fraction);

            Transform? track = bar.transform.Find("Track");
            Transform? fill = track?.Find("Fill");
            if (fill != null)
            {
                // The fill is anchored to the left of the track, so its width is the whole story
                RectTransform fillRect = (RectTransform)fill;
                fillRect.anchorMax = new Vector2(fraction, 1);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
            }

            Transform? value = bar.transform.Find("Value");
            Text? valueLabel = value != null ? value.GetComponent<Text>() : null;
            if (valueLabel != null)
            {
                valueLabel.text = valueText ?? $"{Mathf.RoundToInt(fraction * 100f)}%";
            }
        }

        private static Text NewText(Transform parent, string name, string text, TextAnchor alignment, Color color)
        {
            GameObject obj = new(name);
            obj.transform.SetParent(parent, false);

            Text component = obj.AddComponent<Text>();
            component.text = text;
            component.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            component.fontSize = 12;
            component.alignment = alignment;
            component.color = color;
            component.raycastTarget = false;
            component.horizontalOverflow = HorizontalWrapMode.Overflow;
            component.verticalOverflow = VerticalWrapMode.Overflow;

            return component;
        }
    }
}
