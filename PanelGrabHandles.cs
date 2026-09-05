using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRTK;

namespace TwitchChat
{
    /// <summary>
    /// Grab bars around the four edges of a cab display. Bring a hand to a bar and it lights up; squeeze the
    /// grip to carry the whole display, or the trigger to drag that one edge in or out and resize it. The
    /// display's slot remembers the result, and either lock on it can take a gesture away.
    /// </summary>
    /// <remarks>
    /// The component lives on the display's canvas, so it dies with the canvas and stops running whenever the
    /// display is hidden. It only ever reads controller state and creates no colliders: a collider here would
    /// join the rigidbody of the locomotive the display is parented to and disturb its physics.
    /// </remarks>
    public class PanelGrabHandles : MonoBehaviour
    {
        /// <summary>Bar width in canvas units. The canvas is scaled to millimetres, so this is 14 mm at scale 1.</summary>
        private const float BarThickness = 14f;

        /// <summary>How far in front of and behind the panel a bar can be reached, in canvas units.</summary>
        private const float BarDepth = 40f;

        /// <summary>How close a hand has to get to a bar before it can grab it, in metres.</summary>
        private const float ReachDistance = 0.08f;

        private const float ControllerSearchInterval = 2f;
        private const int BarCount = 4;

        // Bar indices, and the edge of the panel each one runs along
        private const int Top = 0;
        private const int Bottom = 1;
        private const int Left = 2;
        private const int Right = 3;

        private static readonly Color IdleColor = new(1f, 1f, 1f, 0.2f);
        private static readonly Color ReadyColor = new(0.35f, 0.75f, 1f, 0.6f);
        private static readonly Color MovingColor = new(0.4f, 1f, 0.5f, 0.8f);
        private static readonly Color ResizingColor = new(1f, 0.8f, 0.3f, 0.8f);
        private static readonly Color LockedColor = new(0.6f, 0.6f, 0.6f, 0.12f);

        private enum Mode
        {
            None,

            /// <summary>Carried by a VR hand holding the grip.</summary>
            Moving,

            /// <summary>An edge dragged by a VR hand holding the trigger.</summary>
            Resizing,

            /// <summary>An edge dragged with the mouse, outside VR. Driven by pointer events, not by this component's update.</summary>
            MouseResizing
        }

        private readonly RectTransform[] barRects = new RectTransform[BarCount];
        private readonly Image[] barImages = new Image[BarCount];

        private RectTransform? panelRect;
        private RectTransform? canvasRect;
        private CabDisplayHost? host;
        private Action? changed;

        private Transform? leftHand;
        private Transform? rightHand;
        private VRTK_ControllerEvents? leftEvents;
        private VRTK_ControllerEvents? rightEvents;
        private float nextControllerSearch;

        private Mode mode = Mode.None;
        private Transform? activeHand;
        private VRTK_ControllerEvents? activeEvents;
        private int activeBar = -1;

        private Vector3 holdOffsetPosition;
        private Quaternion holdOffsetRotation;
        private Vector2 lastResizeLocal;

        private int highlightedBar = -1;
        private bool lockedPainted;
        private bool barsVisible = true;

        /// <summary>True while a hand is carrying or resizing this display.</summary>
        public bool IsBusy => mode != Mode.None;

        /// <summary>
        /// Builds the bars around the given panel rect.
        /// </summary>
        /// <param name="menuPanel">The MenuPanel transform the bars frame.</param>
        /// <param name="display">The display these bars belong to, for its size and its locks.</param>
        /// <param name="onChanged">Called when a move or a resize finishes, so the slot can be saved.</param>
        public void Initialize(Transform menuPanel, CabDisplayHost display, Action onChanged)
        {
            panelRect = menuPanel.GetComponent<RectTransform>();
            canvasRect = GetComponent<RectTransform>();
            host = display;
            changed = onChanged;

            if (panelRect == null || canvasRect == null)
            {
                Main.LogEntry("CabDisplay", "Grab handles: the canvas is missing a rect, so no bars were created.");
                return;
            }

            CreateBars(panelRect);
        }

        private void CreateBars(RectTransform parent)
        {
            // Each bar sits just outside one edge of the panel and stretches along with it
            barRects[Top] = CreateBar(parent, "GrabHandle_Top", new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 0f), new Vector2(BarThickness * 2f, BarThickness));
            barRects[Bottom] = CreateBar(parent, "GrabHandle_Bottom", Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(BarThickness * 2f, BarThickness));
            barRects[Left] = CreateBar(parent, "GrabHandle_Left", Vector2.zero, new Vector2(0f, 1f), new Vector2(1f, 0.5f), new Vector2(BarThickness, 0f));
            barRects[Right] = CreateBar(parent, "GrabHandle_Right", new Vector2(1f, 0f), Vector2.one, new Vector2(0f, 0.5f), new Vector2(BarThickness, 0f));

            for (int i = 0; i < BarCount; i++)
            {
                barImages[i] = barRects[i].GetComponent<Image>();
                barRects[i].gameObject.AddComponent<PanelResizeHandle>().Bind(this, i);
            }

            Main.LogEntry("CabDisplay", $"Grab handles created around {(host != null ? host.Name : "a display")}.");
        }

        private static RectTransform CreateBar(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
        {
            GameObject bar = new(name);
            bar.transform.SetParent(parent, false);

            Image image = bar.AddComponent<Image>();
            image.color = IdleColor;
            image.raycastTarget = true; // so the mouse can drag the bar outside VR; VR hands use proximity instead

            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = Vector2.zero;

            // Deliberately no collider: hand proximity is measured against the bar rect in DistanceToBar instead,
            // because a collider here would be absorbed by the locomotive's rigidbody.
            return rect;
        }

        private void LateUpdate()
        {
            if (panelRect == null || canvasRect == null || host == null)
            {
                return;
            }

            if (!Settings.Instance.cabDisplayGrabHandles)
            {
                Finish();
                SetBarsVisible(false);
                return;
            }

            SetBarsVisible(true);

            if (mode == Mode.MouseResizing)
            {
                return; // pointer events drive this one
            }

            if (!VRManager.IsVREnabled())
            {
                return; // outside VR the bars are dragged with the mouse instead
            }

            FindControllers();

            if (mode != Mode.None)
            {
                ContinueOrFinish();
                return;
            }

            UpdateHighlightAndStart();
        }

        /// <summary>
        /// Keeps carrying or resizing the display while the button that started it is held.
        /// </summary>
        private void ContinueOrFinish()
        {
            bool stillHeld = activeEvents != null
                && activeHand != null
                && activeHand.gameObject.activeInHierarchy
                && (mode == Mode.Moving ? activeEvents.gripPressed : activeEvents.triggerPressed);

            if (!stillHeld)
            {
                Finish();
                return;
            }

            if (mode == Mode.Moving)
            {
                transform.SetPositionAndRotation(
                    activeHand!.position + activeHand.rotation * holdOffsetPosition,
                    activeHand.rotation * holdOffsetRotation);
            }
            else
            {
                UpdateResize();
            }
        }

        /// <summary>
        /// Highlights whichever bar is nearest to a hand, and starts a move or a resize when that hand
        /// squeezes. Hands over the panel face are ignored, so pressing a button near an edge cannot be
        /// mistaken for grabbing the frame.
        /// </summary>
        private void UpdateHighlightAndStart()
        {
            bool canMove = !host!.Slot.lockPosition;
            bool canResize = !host.Slot.lockSize;

            int nearestBar = -1;
            float nearestDistance = ReachDistance;
            Transform? nearestHand = null;
            VRTK_ControllerEvents? nearestEvents = null;

            if (canMove || canResize)
            {
                for (int hand = 0; hand < 2; hand++)
                {
                    Transform? handTransform = hand == 0 ? leftHand : rightHand;
                    if (handTransform == null || IsOverPanelFace(handTransform.position))
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
            }

            Highlight(nearestBar);

            if (nearestBar < 0 || nearestHand == null || nearestEvents == null)
            {
                return;
            }

            if (canMove && nearestEvents.gripPressed)
            {
                BeginMove(nearestHand, nearestEvents, nearestBar);
            }
            else if (canResize && nearestEvents.triggerPressed)
            {
                BeginResize(nearestHand, nearestEvents, nearestBar);
            }
        }

        private void BeginMove(Transform hand, VRTK_ControllerEvents events, int bar)
        {
            mode = Mode.Moving;
            activeHand = hand;
            activeEvents = events;
            activeBar = bar;

            holdOffsetPosition = Quaternion.Inverse(hand.rotation) * (transform.position - hand.position);
            holdOffsetRotation = Quaternion.Inverse(hand.rotation) * transform.rotation;

            PaintBar(bar, MovingColor);
            Main.LogEntry("CabDisplay", $"{host!.Name} picked up by the {(hand == leftHand ? "left" : "right")} hand.");
        }

        private void BeginResize(Transform hand, VRTK_ControllerEvents events, int bar)
        {
            mode = Mode.Resizing;
            activeHand = hand;
            activeEvents = events;
            activeBar = bar;
            lastResizeLocal = PanelLocal(hand.position);

            PaintBar(bar, ResizingColor);
            Main.LogEntry("CabDisplay", $"{host!.Name} being resized from its {BarName(bar)} edge.");
        }

        /// <summary>
        /// Drags the held edge with the hand, keeping the opposite edge where it is, and clamps the result to
        /// the sizes a display is allowed to take.
        /// </summary>
        private void UpdateResize()
        {
            ResizeBy(PanelLocal(activeHand!.position) - lastResizeLocal);

            // The panel moved to hold its far edge still, so re-read where the hand is in the new local space
            lastResizeLocal = PanelLocal(activeHand.position);
        }

        /// <summary>
        /// Moves the held edge by a delta measured in panel coordinates, whether that came from a hand or a
        /// mouse, and keeps the result within the sizes a display is allowed to take.
        /// </summary>
        private void ResizeBy(Vector2 delta)
        {
            Vector2 size = panelRect!.rect.size;

            switch (activeBar)
            {
                case Top:
                    size.y += delta.y;
                    break;
                case Bottom:
                    size.y -= delta.y;
                    break;
                case Left:
                    size.x -= delta.x;
                    break;
                default:
                    size.x += delta.x;
                    break;
            }

            size.x = Mathf.Clamp(size.x, MenuManager.MinPanelSize.x, MenuManager.MaxPanelSize.x);
            size.y = Mathf.Clamp(size.y, MenuManager.MinPanelSize.y, MenuManager.MaxPanelSize.y);

            ApplySize(size);
        }

        /// <summary>
        /// Resizes the canvas and its panel, then shifts the canvas so the edge opposite the held one stays
        /// put. Doing it by measuring that edge before and after avoids having to reason about how the
        /// canvas, its rect pivot and the panel anchor interact.
        /// </summary>
        private void ApplySize(Vector2 size)
        {
            int fixedEdge = OppositeBar(activeBar);
            Vector3 before = EdgeWorldPoint(fixedEdge);

            canvasRect!.sizeDelta = size;
            panelRect!.sizeDelta = size;

            Vector3 after = EdgeWorldPoint(fixedEdge);
            transform.position += before - after;

            host!.Slot.panelSize = size;
        }

        // ------------------------------------------------------------------
        // Mouse dragging, for playing outside VR
        // ------------------------------------------------------------------

        /// <summary>
        /// Starts a mouse resize of one edge. Called by the bar's own <see cref="PanelResizeHandle"/>.
        /// </summary>
        /// <returns>False if this display cannot be resized right now, so the drag is ignored.</returns>
        internal bool BeginMouseResize(int bar, PointerEventData pointer)
        {
            if (panelRect == null || host == null || mode != Mode.None
                || host.Slot.lockSize || !Settings.Instance.cabDisplayGrabHandles
                || !ScreenToPanelLocal(pointer, out Vector2 local))
            {
                return false;
            }

            mode = Mode.MouseResizing;
            activeBar = bar;
            lastResizeLocal = local;

            PaintBar(bar, ResizingColor);
            Main.LogEntry("CabDisplay", $"{host.Name} being resized from its {BarName(bar)} edge with the mouse.");
            return true;
        }

        internal void ContinueMouseResize(PointerEventData pointer)
        {
            if (mode != Mode.MouseResizing || !ScreenToPanelLocal(pointer, out Vector2 local))
            {
                return;
            }

            ResizeBy(local - lastResizeLocal);

            // The panel moved to hold its far edge still, so re-read the pointer in the new local space
            if (ScreenToPanelLocal(pointer, out Vector2 moved))
            {
                lastResizeLocal = moved;
            }
        }

        internal void EndMouseResize()
        {
            if (mode == Mode.MouseResizing)
            {
                Finish();
            }
        }

        /// <summary>
        /// Converts a screen position into the panel's own coordinates, which is where all the resize maths
        /// happens. Handles the world-space canvas by asking for the camera the pointer event came through.
        /// </summary>
        private bool ScreenToPanelLocal(PointerEventData pointer, out Vector2 local)
        {
            Camera? eventCamera = pointer.pressEventCamera != null ? pointer.pressEventCamera : Camera.main;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, pointer.position, eventCamera, out local);
        }

        private void Finish()
        {
            if (mode == Mode.None)
            {
                return;
            }

            Main.LogEntry("CabDisplay", $"{host!.Name} {(mode == Mode.Moving ? "released" : $"resized to {panelRect!.rect.size}")}.");

            mode = Mode.None;
            activeHand = null;
            activeEvents = null;
            activeBar = -1;
            highlightedBar = -1;

            for (int i = 0; i < BarCount; i++)
            {
                PaintBar(i, IdleColor);
            }

            changed?.Invoke();
        }

        private void Highlight(int bar)
        {
            bool fullyLocked = host!.Slot.lockPosition && host.Slot.lockSize;
            if (bar == highlightedBar && fullyLocked == lockedPainted)
            {
                return;
            }

            for (int i = 0; i < BarCount; i++)
            {
                PaintBar(i, fullyLocked ? LockedColor : i == bar ? ReadyColor : IdleColor);
            }
            highlightedBar = bar;
            lockedPainted = fullyLocked;
        }

        private void PaintBar(int bar, Color color)
        {
            if (bar >= 0 && bar < BarCount && barImages[bar] != null)
            {
                barImages[bar].color = color;
            }
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

        /// <summary>True when the hand is in front of the panel itself rather than out at its frame.</summary>
        private bool IsOverPanelFace(Vector3 handPosition)
        {
            Vector2 local = PanelLocal(handPosition);
            return panelRect!.rect.Contains(local);
        }

        private Vector2 PanelLocal(Vector3 worldPosition)
        {
            return panelRect!.InverseTransformPoint(worldPosition);
        }

        /// <summary>Middle of one edge of the panel, in world space.</summary>
        private Vector3 EdgeWorldPoint(int bar)
        {
            Rect area = panelRect!.rect;
            Vector2 point = bar switch
            {
                Top => new Vector2(area.center.x, area.yMax),
                Bottom => new Vector2(area.center.x, area.yMin),
                Left => new Vector2(area.xMin, area.center.y),
                _ => new Vector2(area.xMax, area.center.y)
            };
            return panelRect.TransformPoint(point);
        }

        private static int OppositeBar(int bar)
        {
            return bar switch
            {
                Top => Bottom,
                Bottom => Top,
                Left => Right,
                _ => Left
            };
        }

        private static string BarName(int bar)
        {
            return bar switch
            {
                Top => "top",
                Bottom => "bottom",
                Left => "left",
                _ => "right"
            };
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
        /// hand is never tracked without buttons to go with it.
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

    /// <summary>
    /// Sits on one grab bar and turns mouse drags on it into a resize of that edge, which is how displays are
    /// sized outside VR. In VR the same bars are driven by hand proximity instead, and these events simply
    /// never fire.
    /// </summary>
    public class PanelResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private PanelGrabHandles? handles;
        private int bar;
        private bool dragging;

        internal void Bind(PanelGrabHandles owner, int barIndex)
        {
            handles = owner;
            bar = barIndex;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = handles != null && handles.BeginMouseResize(bar, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragging)
            {
                handles!.ContinueMouseResize(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragging)
            {
                dragging = false;
                handles!.EndMouseResize();
            }
        }
    }
}
