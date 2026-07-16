using UnityEngine;
using System;

namespace MobileCore
{
    public static class GameEvents
    {
        public static event Action OnGameStarted;
        public static event Action OnLevelCompleted;
        public static event Action OnLevelFailed;
        public static event Action<int> OnEconomyChanged;

        public static void TriggerGameStarted() => OnGameStarted?.Invoke();
        public static void TriggerLevelCompleted() => OnLevelCompleted?.Invoke();
        public static void TriggerLevelFailed() => OnLevelFailed?.Invoke();

    }

}

