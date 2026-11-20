using System.Collections.Generic;
using UnityEngine;

namespace MagicBlock
{
    /// <summary>Một block người chơi đang cầm (trong Hand hoặc Storage).</summary>
    public sealed class Piece
    {
        public readonly Shape Shape;
        public readonly int Color;
        public Piece(Shape shape, int color) { Shape = shape; Color = color; }
    }

    /// <summary>Ô bị xoá: vị trí + màu (để làm hiệu ứng).</summary>
    public struct ClearedCell
    {
        public int X, Y, Color;
        public ClearedCell(int x, int y, int color) { X = x; Y = y; Color = color; }
    }

    /// <summary>Logic bảng chơi thuần (không phụ thuộc UI). -1 = ô trống, &gt;= 0 = index màu.</summary>
    public sealed class BoardModel
    {
        public const int Empty = -1;
        public readonly int Size;
        readonly int[,] _cells;

        public BoardModel(int size)
        {
            Size = size;
            _cells = new int[size, size];
            _groups = new int[size, size];
            Reset();
        }

        // Mỗi lần đặt block có 1 id riêng -> buff Block Destroyer xoá được cả block đã đặt
        readonly int[,] _groups;
        int _nextGroup = 1;

        public void Reset()
        {
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                {
                    _cells[x, y] = Empty;
                    _groups[x, y] = 0;
                }
        }

        /// <summary>Xoá toàn bộ phần còn lại của block chứa ô (x,y).</summary>
        public void RemoveGroupAt(int x, int y, List<ClearedCell> cleared)
        {
            cleared.Clear();
            if (!InBounds(x, y) || IsEmpty(x, y)) return;
            int id = _groups[x, y];
            for (int cx = 0; cx < Size; cx++)
                for (int cy = 0; cy < Size; cy++)
                    if (!IsEmpty(cx, cy) && _groups[cx, cy] == id) ClearCell(cx, cy, cleared);
        }

        public int Get(int x, int y) => _cells[x, y];
        public bool IsEmpty(int x, int y) => _cells[x, y] == Empty;
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Size && y < Size;

        public bool CanPlace(Shape shape, int ox, int oy)
        {
            foreach (var c in shape.Cells)
            {
                int x = ox + c.x, y = oy + c.y;
                if (!InBounds(x, y) || !IsEmpty(x, y)) return false;
            }
            return true;
        }

        public void Place(Shape shape, int ox, int oy, int color)
        {
            int id = _nextGroup++;
            foreach (var c in shape.Cells)
            {
                _cells[ox + c.x, oy + c.y] = color;
                _groups[ox + c.x, oy + c.y] = id;
            }
        }

        public bool FitsAnywhere(Shape shape)
        {
            for (int x = 0; x <= Size - shape.Width; x++)
                for (int y = 0; y <= Size - shape.Height; y++)
                    if (CanPlace(shape, x, y)) return true;
            return false;
        }

        public void FindFullLines(List<int> rows, List<int> cols)
        {
            rows.Clear(); cols.Clear();
            for (int y = 0; y < Size; y++)
            {
                bool full = true;
                for (int x = 0; x < Size && full; x++) full = !IsEmpty(x, y);
                if (full) rows.Add(y);
            }
            for (int x = 0; x < Size; x++)
            {
                bool full = true;
                for (int y = 0; y < Size && full; y++) full = !IsEmpty(x, y);
                if (full) cols.Add(x);
            }
        }

        /// <summary>Xoá các hàng/cột, trả về danh sách ô bị xoá (không trùng).</summary>
        public void ClearLines(List<int> rows, List<int> cols, List<ClearedCell> cleared)
        {
            cleared.Clear();
            foreach (int y in rows)
                for (int x = 0; x < Size; x++)
                    ClearCell(x, y, cleared);
            foreach (int x in cols)
                for (int y = 0; y < Size; y++)
                    ClearCell(x, y, cleared);
        }

        void ClearCell(int x, int y, List<ClearedCell> cleared)
        {
            if (_cells[x, y] == Empty) return;
            cleared.Add(new ClearedCell(x, y, _cells[x, y]));
            _cells[x, y] = Empty;
            _groups[x, y] = 0;
        }

        /// <summary>Giả lập đặt block để biết hàng/cột nào sẽ được xoá (dùng cho highlight khi kéo).</summary>
        public void PreviewLines(Shape shape, int ox, int oy, List<int> rows, List<int> cols)
        {
            foreach (var c in shape.Cells) _cells[ox + c.x, oy + c.y] = 0;
            FindFullLines(rows, cols);
            foreach (var c in shape.Cells) _cells[ox + c.x, oy + c.y] = Empty;
        }
    }
}
