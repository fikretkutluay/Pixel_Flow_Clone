using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MobileCore
{
    public class PointerRouter : MonoBehaviour, IInputRouter
    {
        public event Action<Vector2> OnTap;

        private InputAction pressAction;

        private void Awake()
        {
            // Bound as an InputAction rather than polled in Update(): the Input
            // System dispatches "performed" once per queued press event, so two
            // taps landing between two rendered frames (heavy tension speed, many
            // shooters animating at once) both fire. Polling
            // Pointer.current.press.wasPressedThisFrame once per Update collapses
            // any presses that land in the same frame window into a single flag,
            // silently dropping the second tap — no raycast, no denied shake,
            // nothing.
            pressAction = new InputAction("Tap", InputActionType.Button, "<Pointer>/press");
            pressAction.performed += HandlePerformed;
        }

        private void OnEnable() => pressAction.Enable();
        private void OnDisable() => pressAction.Disable();
        private void OnDestroy() => pressAction.Dispose();

        private void HandlePerformed(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is not Pointer pointer) return;
            OnTap?.Invoke(pointer.position.ReadValue());
        }
    }
}