using System;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

namespace TwitchChat
{
    /// <summary>
    /// Watches one button on one controller and reports the moment it goes down. Used to open and close the
    /// wrist panel without having to reach over and press it.
    /// </summary>
    /// <remarks>
    /// Not a component: there is nothing to draw and nothing to place, and the menu manager already runs
    /// every frame. It finds its controller the same way the grab handles and the wrist grab do, and on the
    /// same unhurried interval, since a controller that has not appeared yet will not appear any sooner for
    /// being asked about more often.
    /// </remarks>
    internal sealed class VrButtonWatch
    {
        private const float ControllerSearchInterval = 2f;

        /// <summary>
        /// Which field on the controller each button name reads. Held as delegates and looked up once per
        /// change of setting, so the per-frame cost is a string compare and a call.
        /// </summary>
        private static readonly Dictionary<string, Func<VRTK_ControllerEvents, bool>> Buttons =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // VRTK calls the thumbstick click a touchpad press, whatever the controller actually has
                ["Thumbstick"] = events => events.touchpadPressed,
                ["A"] = events => events.buttonOnePressed,
                ["B"] = events => events.buttonTwoPressed,
                ["Menu"] = events => events.startMenuPressed
            };

        private Transform? hand;
        private VRTK_ControllerEvents? events;
        private bool watchingLeft;
        private float nextControllerSearch;

        private string parsedName = string.Empty;
        private Func<VRTK_ControllerEvents, bool> read = Buttons["Thumbstick"];

        private bool wasDown;

        /// <summary>
        /// True on the frame the configured button goes down, and only then.
        /// </summary>
        /// <param name="wantLeft">Which hand to watch. Changing it re-finds the controller at once.</param>
        /// <param name="buttonName">One of <see cref="Settings.WristToggleButtons"/>.</param>
        public bool Pressed(bool wantLeft, string buttonName)
        {
            UpdateButton(buttonName);

            if (watchingLeft != wantLeft)
            {
                watchingLeft = wantLeft;
                Forget();
            }

            FindController();

            if (events == null)
            {
                // A controller that has gone leaves nothing to compare against next time it comes back
                wasDown = false;
                return false;
            }

            bool down = read(events);
            bool pressed = down && !wasDown;
            wasDown = down;
            return pressed;
        }

        private void UpdateButton(string buttonName)
        {
            if (buttonName == parsedName)
            {
                return;
            }

            parsedName = buttonName;

            if (!Buttons.TryGetValue(buttonName, out Func<VRTK_ControllerEvents, bool>? found))
            {
                found = Buttons["Thumbstick"];
                Main.LogEntry("WristPanel", $"'{buttonName}' is not a button this mod knows; using the thumbstick.");
            }

            read = found;

            // A different button has a different state, so there is nothing to compare against
            wasDown = false;
        }

        private void FindController()
        {
            if (hand != null && events != null)
            {
                return;
            }
            if (Time.unscaledTime < nextControllerSearch)
            {
                return;
            }
            nextControllerSearch = Time.unscaledTime + ControllerSearchInterval;

            GameObject? alias = watchingLeft
                ? VRTK_DeviceFinder.GetControllerLeftHand(false)
                : VRTK_DeviceFinder.GetControllerRightHand(false);
            GameObject? actual = watchingLeft
                ? VRTK_DeviceFinder.GetControllerLeftHand(true)
                : VRTK_DeviceFinder.GetControllerRightHand(true);

            PanelGrabHandles.Resolve(alias, actual, ref hand, ref events);

            if (events == null)
            {
                return;
            }

            // Prime rather than fire: a controller found while the player happens to be holding the button
            // must not be reported as a press they have just made
            wasDown = read(events);
        }

        private void Forget()
        {
            hand = null;
            events = null;
            wasDown = false;
            nextControllerSearch = 0f;
        }
    }
}
