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
        public static event Action OnPlayRequested;
        public static event Action OnRetryRequested;
        public static event Action<int> OnRemainingCubesChanged;
        public static event Action<int, int> OnTrackOccupancyChanged;
        public static event Action<int, int> OnParkOccupancyChanged;
        public static event Action OnRescueStarted;

        /// <summary>A shooter was committed to the rail, from the queue or the park.</summary>
        public static event Action OnShooterLaunched;

        // UI-to-UI requests. Direction is still one-way — a panel raises them and
        // UIManager answers; no gameplay class touches these (RULE 6).
        public static event Action<int> OnLevelStarted;
        public static event Action OnMainMenuRequested;
        public static event Action OnSettingsRequested;
        public static event Action OnStoreRequested;
        public static event Action OnProfileRequested;

        public static void TriggerShooterLaunched() => OnShooterLaunched?.Invoke();

        public static void TriggerRescueStarted() => OnRescueStarted?.Invoke();

        public static void TriggerGameStarted() => OnGameStarted?.Invoke();
        public static void TriggerLevelCompleted() => OnLevelCompleted?.Invoke();
        public static void TriggerLevelFailed() => OnLevelFailed?.Invoke();
        public static void TriggerPlayRequested() => OnPlayRequested?.Invoke();
        public static void TriggerRetryRequested() => OnRetryRequested?.Invoke();
        public static void TriggerRemainingCubesChanged(int count) => OnRemainingCubesChanged?.Invoke(count);
        public static void TriggerTrackOccupancyChanged(int count, int cap) => OnTrackOccupancyChanged?.Invoke(count, cap);
        public static void TriggerParkOccupancyChanged(int count, int cap) => OnParkOccupancyChanged?.Invoke(count, cap);

        public static void TriggerLevelStarted(int level) => OnLevelStarted?.Invoke(level);
        public static void TriggerMainMenuRequested() => OnMainMenuRequested?.Invoke();
        public static void TriggerSettingsRequested() => OnSettingsRequested?.Invoke();
        public static void TriggerStoreRequested() => OnStoreRequested?.Invoke();
        public static void TriggerProfileRequested() => OnProfileRequested?.Invoke();
    }

}

