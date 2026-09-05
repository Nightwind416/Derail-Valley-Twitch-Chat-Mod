using System;
using UnityEngine;
using VRTK;

namespace TwitchChat
{
    /// <summary>
    /// Lets the wrist panel be taken in the free hand and put where the player wants it, while its adjust panel
    /// is open. Where it lands is written straight back into the placement settings, so the numbers on that
    /// panel and the panel itself always agree.
    /// </summary>
    /// <remarks>
    /// Like the display grab bars this creates no collider and only reads controller state: a collider here
    /// would be folded into the rigidbody of whatever the panel hangs off. The hand wearing the panel is
    /// ignored, since it is always within reach of it and gripping is how that hand holds everything else.
    /// </remarks>
    public class WristPanelGrab : MonoBehaviour
    {
        /// <summary>How close a hand has to get before its grip picks the panel up, in metres.</summary>
        private const float ReachDistance = 0.2f;

        private const float ControllerSearchInterval = 2f;

        private Transform? leftHand;
        private Transform? rightHand;
        private VRTK_ControllerEvents? leftEvents;
        private VRTK_ControllerEvents? rightEvents;
        private float nextControllerSearch;

        private Transform? activeHand;
        private VRTK_ControllerEvents? activeEvents;
        private Vector3 holdOffsetPosition;
        private Quaternion holdOffsetRotation;
        private Action? placed;

        /// <summary>True only while the panel is being placed, which is while its adjust panel is open.</summary>
        public bool AdjustMode { get; set; }

        /// <summary>Which hand is wearing the panel, so that hand's grip is left to do its normal job.</summary>
        public bool WornOnLeft { get; set; }

        /// <summary>What the reach is measured to: the menus themselves, rather than the point on the hand.</summary>
        public Transform? Reference { get; set; }

        /// <summary>True while a hand is carrying the panel, so its usual pose is left alone meanwhile.</summary>
        public bool IsHeld => activeHand != null;

        /// <summary>Gives the component somewhere to report a panel that has just been put down.</summary>
        public void Initialize(Action onPlaced)
        {
            placed = onPlaced;
        }

        private void LateUpdate()
        {
            if (!AdjustMode || !VRManager.IsVREnabled())
            {
                Drop();
                return;
            }

            FindControllers();

            if (activeHand != null)
            {
                CarryOrDrop();
                return;
            }

            TryPickUp();
        }

        private void TryPickUp()
        {
            Transform target = Reference != null ? Reference : transform;

            for (int hand = 0; hand < 2; hand++)
            {
                bool isLeft = hand == 0;
                if (isLeft == WornOnLeft)
                {
                    continue; // the hand the panel is on
                }

                Transform? handTransform = isLeft ? leftHand : rightHand;
                VRTK_ControllerEvents? events = isLeft ? leftEvents : rightEvents;
                if (handTransform == null || events == null || !events.gripPressed)
                {
                    continue;
                }

                if (Vector3.Distance(handTransform.position, target.position) > ReachDistance)
                {
                    continue;
                }

                activeHand = handTransform;
                activeEvents = events;
                holdOffsetPosition = Quaternion.Inverse(handTransform.rotation) * (transform.position - handTransform.position);
                holdOffsetRotation = Quaternion.Inverse(handTransform.rotation) * transform.rotation;

                Main.LogEntry("WristPanel", $"Wrist panel picked up by the {(isLeft ? "left" : "right")} hand for placing.");
                return;
            }
        }

        private void CarryOrDrop()
        {
            bool stillHeld = activeEvents != null
                && activeHand != null
                && activeHand.gameObject.activeInHierarchy
                && activeEvents.gripPressed;

            if (!stillHeld)
            {
                Drop();
                return;
            }

            transform.SetPositionAndRotation(
                activeHand!.position + (activeHand.rotation * holdOffsetPosition),
                activeHand.rotation * holdOffsetRotation);
        }

        private void Drop()
        {
            if (activeHand == null)
            {
                return;
            }

            activeHand = null;
            activeEvents = null;
            placed?.Invoke();
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
                PanelGrabHandles.Resolve(VRTK_DeviceFinder.GetControllerLeftHand(false), VRTK_DeviceFinder.GetControllerLeftHand(true), ref leftHand, ref leftEvents);
            }
            if (rightHand == null)
            {
                PanelGrabHandles.Resolve(VRTK_DeviceFinder.GetControllerRightHand(false), VRTK_DeviceFinder.GetControllerRightHand(true), ref rightHand, ref rightEvents);
            }
        }
    }
}
