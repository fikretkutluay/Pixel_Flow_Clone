using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MobileCore
{
    public class PointerRouter : MonoBehaviour, IInputRouter
    {
        public event Action<Vector2> OnTap;

        private void Update()
        {
            if (Pointer.current == null) return;
            if (Pointer.current.press.wasPressedThisFrame)
            {
                OnTap?.Invoke(Pointer.current.position.ReadValue());
            }
        }
    }
}