using NUnit.Framework;
using MobileCore;

public class BoundedBufferTests
{
    [Test]
    public void NewBuffer_IsEmpty_AndHasFreeSlot()
    {
        var buffer = new BoundedBuffer<string>(3);

        Assert.AreEqual(0, buffer.Count);
        Assert.AreEqual(3, buffer.Capacity);
        Assert.IsTrue(buffer.HasFreeSlot);
    }

    [Test]
    public void TryAdd_UnderCapacity_Succeeds()
    {
        var buffer = new BoundedBuffer<string>(3);

        bool added = buffer.TryAdd("a");

        Assert.IsTrue(added);
        Assert.AreEqual(1, buffer.Count);
        Assert.IsTrue(buffer.Contains("a"));
    }

    [Test]
    public void TryAdd_AtCapacity_Fails()
    {
        var buffer = new BoundedBuffer<string>(2);
        buffer.TryAdd("a");
        buffer.TryAdd("b");

        bool added = buffer.TryAdd("c");

        Assert.IsFalse(added);
        Assert.AreEqual(2, buffer.Count);
        Assert.IsFalse(buffer.Contains("c"));
        Assert.IsFalse(buffer.HasFreeSlot);
    }

    [Test]
    public void TryRemove_ExistingItem_Succeeds()
    {
        var buffer = new BoundedBuffer<string>(3);
        buffer.TryAdd("a");
        buffer.TryAdd("b");

        bool removed = buffer.TryRemove("a");

        Assert.IsTrue(removed);
        Assert.AreEqual(1, buffer.Count);
        Assert.IsFalse(buffer.Contains("a"));
        Assert.IsTrue(buffer.Contains("b"));
    }

    [Test]
    public void TryRemove_MissingItem_Fails()
    {
        var buffer = new BoundedBuffer<string>(3);
        buffer.TryAdd("a");

        bool removed = buffer.TryRemove("zzz");

        Assert.IsFalse(removed);
        Assert.AreEqual(1, buffer.Count);
    }

    [Test]
    public void RemoveThenAdd_FreesSlot()
    {
        var buffer = new BoundedBuffer<string>(1);
        buffer.TryAdd("a");

        Assert.IsFalse(buffer.TryAdd("b"));   // dolu

        buffer.TryRemove("a");

        Assert.IsTrue(buffer.HasFreeSlot);
        Assert.IsTrue(buffer.TryAdd("b"));    // yer açıldı
    }

    [Test]
    public void OnChanged_FiresOnSuccessfulAdd()
    {
        var buffer = new BoundedBuffer<string>(2);
        int fireCount = 0;
        buffer.OnChanged += () => fireCount++;

        buffer.TryAdd("a");

        Assert.AreEqual(1, fireCount);
    }

    [Test]
    public void OnChanged_DoesNotFireOnFailedAdd()
    {
        var buffer = new BoundedBuffer<string>(1);
        buffer.TryAdd("a");

        int fireCount = 0;
        buffer.OnChanged += () => fireCount++;

        buffer.TryAdd("b");   // kapasite dolu, reddedilmeli

        Assert.AreEqual(0, fireCount);
    }

    [Test]
    public void OnChanged_DoesNotFireOnFailedRemove()
    {
        var buffer = new BoundedBuffer<string>(2);
        buffer.TryAdd("a");

        int fireCount = 0;
        buffer.OnChanged += () => fireCount++;

        buffer.TryRemove("zzz");   // yok, reddedilmeli

        Assert.AreEqual(0, fireCount);
    }

    [Test]
    public void Enumeration_YieldsItemsInInsertionOrder()
    {
        var buffer = new BoundedBuffer<string>(3);
        buffer.TryAdd("a");
        buffer.TryAdd("b");
        buffer.TryAdd("c");

        var seen = new System.Collections.Generic.List<string>();
        foreach (var item in buffer)
            seen.Add(item);

        Assert.AreEqual(3, seen.Count);
        Assert.AreEqual("a", seen[0]);
        Assert.AreEqual("b", seen[1]);
        Assert.AreEqual("c", seen[2]);
    }
}