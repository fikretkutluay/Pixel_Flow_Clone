using UnityEngine;
namespace MobileCore
{
    public class GridManager<T>
    {
        private int width;
        private int height;
        private float cellSize;
        private Vector3 originPosition;
        private T[,] gridArray;

        public int Width => width;
        public int Height => height;

        public GridManager(int width, int height, float cellSize, Vector3 originPosition)
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;
            this.originPosition = originPosition;

            gridArray = new T[width, height];
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x, y) * cellSize + originPosition;
        }

        public void SetValue(int x, int y, T value)
        {
            if (IsInBounds(x, y))
                gridArray[x, y] = value;
        }

        public T GetValue(int x, int y)
        {
            return IsInBounds(x, y) ? gridArray[x, y] : default(T);
        }
        public void GetXY(Vector3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
            y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
        }
    }
}