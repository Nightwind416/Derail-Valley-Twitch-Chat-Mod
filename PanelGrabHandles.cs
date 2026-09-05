using System;
using UnityEngine;
using UnityEngine.UI;
using VRTK;

namespace TwitchChat
{
    /// <summary>
    /// Grab bars around the four edges of the cab display. Bringing a hand to a bar highlights it, and
    /// squeezing the grip then carries the display with that hand until the grip is released, the way the
    /// license papers used to be picked up. Position and rotation both follow the hand, and the new pose is
    /// saved for the locomotive on release.
    /// </summary>
    /// <remarks>
    /// The component lives on the cab display canvas, so it dies with the canvas and stops running whenever
    /// the display is hidden. It only ever reads controller state; nothing here depends on the game's item
    /// or grab systems, which is what made the old license menus break between game builds.
    /// </remarks>
    public class PanelGrabHandles : MonoBehaviour
    {
        /// <summary>Bar width in canvas units. The canvas is scaled to millimetres, so this is 14 mm at scale 1.</summary>
        private const float BarThickness = 14f;

        /// <summary>How far in front of and behind the panel a bar can be reached, in canvas units.</summary>
        private const float BarDepth = 40f;

        /// <summary>How close a hand has to get to a bar before the grip picks the display up, in metres.</summary>
        private const float ReachDistance = 0.08f;

        private const float ControllerSearchInterval = 2f;
        private const int BarCount = 4;

        private static readonly Color IdleColor = new(1f, 1f, 1f, 0.2f);
        private static readonly Color ReadyColor = new(0.35f, 0.75f, 1f, 0.6f);
        private static readonly Color HeldColor = new(0.4f, 1f, 0.5f, 0.8f);

        private readonly RectTransform[] barRects = new RectTransform[BarCount];
        private readonly Image[] barImages = new Image[BarCount];

        private RectTransform? panelRect;
        private Action? released;

        private Transform? leftHand;
        private Transform? rightHand;
        private VRTK_ControllerEvents? leftEvents;
        private VRTK_ControllerEvents? rightEvents;
        private float nextControllerSearch;

        private Transform? holdingHand;
        private VRTK_ControllerEvents? holdingEvents;
        private Vector3 holdOffsetPosition;
        private Quaternion holdOffsetRotation;

        private int highlightedBar = -1;
        private bool barsVisible = true;

        /// <summary>True while a hand is carrying the display.</summary>
        public bool IsHeld => holdingHand != null;

        /// <summary>
        /// Builds the bars around the given panel rect.
        /// </summary>
        /// <param name="menuPanel">The MenuPanel transform the bars frame.</param>
        /// <param name="onReleased">Called each time the display is let go, so the new pose can be saved.</param>
        public void Initialize(Transform menuPanel, Action onReleased)
        {
            panelRect = menuPanel.GetComponent<RectTransform>();
            released = onReleased;

            if (panelRect == null)
            {
                Main.LogEntry("CabDisplay", "Grab handles: the menu panel has no RectTransform, so no bars were created.");
                return;
            }

            CreateBars(panelRect);
        }

        private void CreateBars(RectTransform parent)
        {
            // Each bar sits just outside one edge of the panel and stretches along with it
            barRects[0] = CreateBar(parent, "GrabHandle_Top", new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 0f), new Vector2(BarThickness * 2f, BarThickness));
            barRects[1] = CreateBar(parent, "GrabHandle_Bottom", Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(BarThickness * 2f, BarThickness));
            barRects[2] = CreateBar(parent, "GrabHandle_Left", Vector2.zero, new Vector2(0f, 1f), new Vector2(1f, 0.5f), new Vector2(BarThickness, 0f));
            barRects[3] = CreateBar(parent, "GrabHandle_Right", new Vector2(1f, 0f), Vector2.one, new Vector2(0f, 0.5f), new Vector2(BarThickness, 0f));

            for (int i = 0; i < BarCount; i++)
            {
                barImages[i] = barRects[i].GetComponent<Image>();
            }

            Main.LogEntry("CabDisplay", "Grab handles created around the cab display.");
        }

        private static RectTransform CreateBar(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
        {
            GameObject bar = new(name);
            bar.transform.SetParent(parent, false);

            Image image = bar.AddComponent<Image>();
            image.color = IdleColor;
            image.raycastTarget = false; // the bars are grabbed, never clicked

            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = Vector2.zero;

            // Deliberately no collider: the display hangs off a locomotive's rigidbody, and any collider
            // without a body of its own would join that rigidbody and wreck the locomotive's physics.
            // Hand proximity is measured against the bar rect instead, in DistanceToBar.
            return rect;
        }

        private void LateUpdate()
        {
            if (panelRect == null)
            {
                return;
            }

            if (!Settings.Instance.cabDisplayGrabHandles || !VRManager.IsVREnabled())
            {
                if (holdingHand != null)
                {
                    Release();
                }
                SetBarsVisible(false);
                return;
            }

            SetBarsVisible(true);
            FindControllers();

            if (holdingHand != null)
            {
                if (holdingEvents == null || !holdingEvents.gripPressed || !holdingHand.gameObject.activeInHierarchy)
                {
                    Release();
                }
                else
                {
                    transform.SetPositionAndRotation(
                        holdingHand.position + holdingHand.rotation * holdOffsetPosition,
                        holdingHand.rotation * holdOffsetRotation);
                    return;
                }
            }

            UpdateHighlightAndGrab();
        }

        /// <summary>
        /// Highlights whichever bar is nearest to either hand, and starts carrying the display if that hand grips.
        /// </summary>
        private void UpdateHighlightAndGrab()
        {
            int nearestBar = -1;
            float nearestDistance = ReachDistance;
            Transform? nearestHand = null;
            VRTK_ControllerEvents? nearestEvents = null;

            for (int hand = 0; hand < 2; hand++)
            {
                Transform? handTransform = hand == 0 ? leftHand : rightHand;
                if (handTransform == null)
                {
                    continue;
                }

                for (int i = 0; i < BarCount; i++)
                {
                    RectTransform bar = barRects[i];
                    if (bar == null)
                    {
                        continue;
                    }

                    float distance = DistanceToBar(bar, handTransform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestBar = i;
                        nearestHand = handTransform;
                        nearestEvents = hand == 0 ? leftEvents : rightEvents;
                    }
                }
            }

            if (nearestBar != highlightedBar)
            {
                if (highlightedBar >= 0 && barImages[highlightedBar] != null)
                {
                    barImages[highlightedBar].color = IdleColor;
                }
                if (nearestBar >= 0 && barImages[nearestBar] != null)
                {
                    barImages[nearestBar].color = ReadyColor;
                }
                highlightedBar = nearestBar;
            }

            if (nearestBar >= 0 && nearestHand != null && nearestEvents != null && nearestEvents.gripPressed)
            {
                Grab(nearestHand, nearestEvents, nearestBar);
            }
        }

        private void Grab(Transform hand, VRTK_ControllerEvents events, int bar)
        {
            bool leftHanded = hand == leftHand;
            holdingHand = hand;
            holdingEvents = events;
            holdOffsetPosition = Quaternion.Inverse(hand.rotation) * (transform.position - hand.position);
            holdOffsetRotation = Quaternion.Inverse(hand.rotation) * transform.rotation;

            if (barImages[bar] != null)
            {
                barImages[bar].color = HeldColor;
            }

            Main.LogEntry("CabDisplay", $"Cab display picked up by the {(leftHanded ? "left" : "right")} hand.");
        }

        private void Release()
        {
            holdingHand = null;
            holdingEvents = null;
            highlightedBar = -1;

            for (int i = 0; i < BarCount; i++)
            {
                if (barImages[i] != null)
                {
                    barImages[i].color = IdleColor;
                }
            }

            released?.Invoke();
        }

        /// <summary>
        /// Distance in metres from a hand to the nearest point of a bar, treating the bar as a slab
        /// <see cref="BarDepth"/> deep. Done in the bar's own space, so it follows the panel as it is resized,
        /// rescaled and moved without any physics involvement.
        /// </summary>
        private static float DistanceToBar(RectTransform bar, Vector3 handPosition)
        {
            Vector3 local = bar.InverseTransformPoint(handPosition);
            Rect area = bar.rect;

            Vector3 nearest = new(
                Mathf.Clamp(local.x, area.xMin, area.xMax),
                Mathf.Clamp(local.y, area.yMin, area.yMax),
                Mathf.Clamp(local.z, -BarDepth * 0.5f, BarDepth * 0.5f));

            return Vector3.Distance(bar.TransformPoint(nearest), handPosition);
        }

        private void SetBarsVisible(bool visible)
        {
            if (visible == barsVisible)
            {
                return;
            }
            barsVisible = visible;

            for (int i = 0; i < BarCount; i++)
            {
                if (barRects[i] != null)
                {
                    barRects[i].gameObject.SetActive(visible);
                }
            }
        }

        private void FindControllers()
        {
            if (leftHand != null && rightHand != null)
            {
                return;
            }
            if (Time.unscaledTime < nextControllerSearch)
            {
                return;
            }
            nextControllerSearch = Time.unscaledTime + ControllerSearchInterval;

            if (leftHand == null)
            {
                Resolve(VRTK_DeviceFinder.GetControllerLeftHand(false), VRTK_DeviceFinder.GetControllerLeftHand(true), ref leftHand, ref leftEvents);
            }
            if (rightHand == null)
            {
                Resolve(VRTK_DeviceFinder.GetControllerRightHand(false), VRTK_DeviceFinder.GetControllerRightHand(true), ref rightHand, ref rightEvents);
            }
        }

        /// <summary>
        /// Takes the controller transform and its button events, preferring the script alias object that carries
        /// the events and falling back to the actual controller. Leaves both null until the events turn up, so a
        /// hand is never tracked without a grip button to go with it.
        /// </summary>
        private static void Resolve(GameObject? alias, GameObject? actual, ref Transform? hand, ref VRTK_ControllerEvents? events)
        {
            GameObject? source = alias != null ? alias : actual;
            if (source == null)
            {
                return;
            }

            VRTK_ControllerEvents? found = source.GetComponentInChildren<VRTK_ControllerEvents>(true);
            if (found == null && actual != null)
            {
                found = actual.GetComponentInChildren<VRTK_ControllerEvents>(true);
            }
            if (found == null)
            {
                return;
            }

            hand = source.transform;
            events = found;
        }
    }
}
