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
        public static event Action OnRescueEnded;

        public static void TriggerRescueStarted() => OnRescueStarted?.Invoke();
        public static void TriggerRescueEnded() => OnRescueEnded?.Invoke();

        public static void TriggerGameStarted() => OnGameStarted?.Invoke();
        public static void TriggerLevelCompleted() => OnLevelCompleted?.Invoke();
        public static void TriggerLevelFailed() => OnLevelFailed?.Invoke();
        public static void TriggerPlayRequested() => OnPlayRequested?.Invoke();
        public static void TriggerRetryRequested() => OnRetryRequested?.Invoke();
        public static void TriggerRemainingCubesChanged(int count) => OnRemainingCubesChanged?.Invoke(count);
        public static void TriggerTrackOccupancyChanged(int count, int cap) => OnTrackOccupancyChanged?.Invoke(count, cap);
        public static void TriggerParkOccupancyChanged(int count, int cap) => OnParkOccupancyChanged?.Invoke(count, cap);

    }

}

