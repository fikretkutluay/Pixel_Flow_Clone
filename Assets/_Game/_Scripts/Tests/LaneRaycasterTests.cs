using NUnit.Framework;
using UnityEngine;
using MobileCore;
using Game;

public class LaneRaycasterTests
{
    private GridManager<CubeCell> NewBoard() =>
        new GridManager<CubeCell>(4, 4, 1f, Vector3.zero);

    [Test]
    public void MatchingColor_BreaksCube()
    {
        var board = NewBoard();
        board.SetValue(0, 0, CubeCell.Create(ColorId.Red, false));

        bool hit = LaneRaycaster.TryBreak(board, 0, Direction.Up, ColorId.Red);

        Assert.IsTrue(hit);
        Assert.AreEqual(ColorId.None, board.GetValue(0, 0).color);
    }

    [Test]
    public void WrongColor_DoesNotBreak()
    {
        var board = NewBoard();
        board.SetValue(0, 0, CubeCell.Create(ColorId.Blue, false));

        bool hit = LaneRaycaster.TryBreak(board, 0, Direction.Up, ColorId.Red);

        Assert.IsFalse(hit);
        Assert.AreEqual(ColorId.Blue, board.GetValue(0, 0).color);
    }

    [Test]
    public void Crate_BlocksLane()
    {
        var board = NewBoard();
        board.SetValue(0, 0, CubeCell.Create(ColorId.None, true));
        board.SetValue(0, 1, CubeCell.Create(ColorId.Red, false));

        bool hit = LaneRaycaster.TryBreak(board, 0, Direction.Up, ColorId.Red);

        Assert.IsFalse(hit);
        Assert.AreEqual(ColorId.Red, board.GetValue(0, 1).color);
    }

    [Test]
    public void EmptyCells_AreSkipped()
    {
        var board = NewBoard();
        board.SetValue(0, 2, CubeCell.Create(ColorId.Red, false));

        bool hit = LaneRaycaster.TryBreak(board, 0, Direction.Up, ColorId.Red);

        Assert.IsTrue(hit);
        Assert.AreEqual(ColorId.None, board.GetValue(0, 2).color);
    }

    [Test]
    public void WrongColorInFront_BlocksMatchBehind()
    {
        var board = NewBoard();
        board.SetValue(0, 0, CubeCell.Create(ColorId.Blue, false));
        board.SetValue(0, 1, CubeCell.Create(ColorId.Red, false));

        bool hit = LaneRaycaster.TryBreak(board, 0, Direction.Up, ColorId.Red);

        Assert.IsFalse(hit);
        Assert.AreEqual(ColorId.Red, board.GetValue(0, 1).color);
    }

    [Test]
    public void EmptyLane_ReturnsFalse()
    {
        var board = NewBoard();

        bool hit = LaneRaycaster.TryBreak(board, 0, Direction.Up, ColorId.Red);

        Assert.IsFalse(hit);
    }

    [Test]
    public void Left_TravelsFromRightEdge()
    {
        var board = NewBoard();
        board.SetValue(3, 2, CubeCell.Create(ColorId.Red, false));

        bool hit = LaneRaycaster.TryBreak(board, 2, Direction.Left, ColorId.Red);

        Assert.IsTrue(hit);
        Assert.AreEqual(ColorId.None, board.GetValue(3, 2).color);
    }
}