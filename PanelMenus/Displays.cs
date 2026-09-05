using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchChat.PanelMenus
{
    /// <summary>
    /// Lists every display placed in the locomotive the player is in, with a lock for each one's position
    /// and size, and a button to close it. The list is rebuilt whenever displays are added or removed, so
    /// it always describes the locomotive currently under the player.
    /// </summary>
    public class DisplaysPanel : PanelConstructor.BasePanel
    {
        private const int RowHeight = 46;
        private const int FirstRowY = 35;

        private readonly List<GameObject> rows = new();

        public DisplaysPanel(Transform parent) : base(parent)
        {
        }

        /// <summary>
        /// Redraws the list. One row per display, marking whichever row is the display being looked at.
        /// </summary>
        /// <param name="displays">Every display in the current locomotive, in placement order.</param>
        /// <param name="self">The display this panel is being shown on, or null for the wrist panel.</param>
        public void Rebuild(IReadOnlyList<CabDisplayHost> displays, PanelHost? self)
        {
            foreach (GameObject row in rows)
            {
                Object.Destroy(row);
            }
            rows.Clear();

            if (displays.Count == 0)
            {
                AddLabel("No displays in this locomotive.", 5, FirstRowY);
                AddLabel("Use Place Display to add one.", 5, FirstRowY + 18);
                return;
            }

            for (int i = 0; i < displays.Count; i++)
            {
                CabDisplayHost display = displays[i];
                int y = FirstRowY + (i * RowHeight);
                bool isSelf = ReferenceEquals(display, self);

                // Button x is the centre of the button, measured from the panel's left edge
                AddLabel($"Display {i + 1}{(isSelf ? "  (this one)" : string.Empty)}", 5, y);
                AddLockButton(display, y + 18, 40, position: true);
                AddLockButton(display, y + 18, 110, position: false);
                AddCloseButton(display, y + 18, 173);
            }

            AddLabel($"{displays.Count} of {Settings.MaxDisplaysPerCar} placed", 5, FirstRowY + (displays.Count * RowHeight) + 4);
        }

        private void AddLabel(string text, int x, int y)
        {
            Text label = PanelConstructor.Label.Create(panelObject.transform, text, x, y, Color.white);
            rows.Add(label.gameObject);
        }

        /// <summary>
        /// A button that toggles one of the two locks and relabels itself to match.
        /// </summary>
        private void AddLockButton(CabDisplayHost display, int y, int x, bool position)
        {
            Button? button = null;
            button = PanelConstructor.Button.Create(
                panelObject.transform,
                LockLabel(display, position),
                x,
                y,
                Color.white,
                () =>
                {
                    if (position)
                    {
                        display.Slot.lockPosition = !display.Slot.lockPosition;
                    }
                    else
                    {
                        display.Slot.lockSize = !display.Slot.lockSize;
                    }
                    Settings.Instance.RequestSave();

                    Text? label = button != null ? button.GetComponentInChildren<Text>() : null;
                    if (label != null)
                    {
                        label.text = LockLabel(display, position);
                    }
                },
                54);

            rows.Add(button.gameObject);
        }

        private static string LockLabel(CabDisplayHost display, bool position)
        {
            bool locked = position ? display.Slot.lockPosition : display.Slot.lockSize;
            return $"{(position ? "Move" : "Size")} {(locked ? "off" : "on")}";
        }

        private void AddCloseButton(CabDisplayHost display, int y, int x)
        {
            Button button = PanelConstructor.Button.Create(
                panelObject.transform,
                "Close",
                x,
                y,
                Color.white,
                () => MenuManager.Instance.CloseCabDisplay(display),
                40);

            rows.Add(button.gameObject);
        }
    }
}
