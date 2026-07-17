using System;

namespace MobileCore
{
    public interface IReadOnlyBuffer
    {
        int Count { get; }
        int Capacity { get; }
        bool HasFreeSlot { get; }
        event Action OnChanged;
    }
}