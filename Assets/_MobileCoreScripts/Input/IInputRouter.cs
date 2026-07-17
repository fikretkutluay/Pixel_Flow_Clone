using System;
using UnityEngine;

namespace MobileCore
{
    public interface IInputRouter
    {
        event Action<Vector2> OnTap;   // ekran koordinatı
    }
}