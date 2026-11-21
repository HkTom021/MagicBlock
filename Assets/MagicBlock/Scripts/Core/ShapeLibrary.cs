using System.Collections.Generic;
using UnityEngine;

namespace MagicBlock
{
    /// <summary>Một hình block: danh sách ô (x sang phải, y đi xuống), đã chuẩn hoá về góc (0,0).</summary>
    public sealed class Shape
    {
        public readonly Vector2Int[] Cells;
        public readonly int Width;
        public readonly int Height;
        public readonly int Weight;     // trọng số random, càng lớn càng hay ra
        public readonly string Name;

        public int CellCount => Cells.Length;
        /// <summary>Block "dễ đặt" — dùng cho buff Lucky Blocks sau này.</summary>
        public bool IsEasy => Cells.Length <= 3;

        public Shape(string name, Vector2Int[] cells, int weight)
        {
            Name = name;
            Weight = weight;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var c in cells)
            {
                minX = Mathf.Min(minX, c.x); minY = Mathf.Min(minY, c.y);
                maxX = Mathf.Max(maxX, c.x); maxY = Mathf.Max(maxY, c.y);
            }
            Cells = new Vector2Int[cells.Length];
            for (int i = 0; i < cells.Length; i++) Cells[i] = new Vector2Int(cells[i].x - minX, cells[i].y - minY);
            System.Array.Sort(Cells, (a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            Width = maxX - minX + 1;
            Height = maxY - minY + 1;
        }

        public string Key()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in Cells) sb.Append(c.x).Append(',').Append(c.y).Append(';');
            return sb.ToString();
        }
    }

    /// <summary>Thư viện tất cả hình block. Mỗi hình gốc được sinh đủ các hướng xoay (không trùng).</summary>
    public static class ShapeLibrary
    {
        static List<Shape> _all;
        public static IReadOnlyList<Shape> All => _all ??= Build();

        // "X" = ô có block. Weight là trọng số của MỖI hướng xoay.
        static List<Shape> Build()
        {
            var list = new List<Shape>();
            var seen = new HashSet<string>();

            Add(list, seen, "Dot",      6, true,  "X");
            Add(list, seen, "Line2",    4, true,  "XX");
            Add(list, seen, "Line3",    4, true,  "XXX");
            Add(list, seen, "Line4",    3, true,  "XXXX");
            Add(list, seen, "Line5",    2, true,  "XXXXX");
            Add(list, seen, "Square2",  5, false, "XX", "XX");
            Add(list, seen, "Square3",  2, false, "XXX", "XXX", "XXX");
            Add(list, seen, "Rect2x3",  2, true,  "XXX", "XXX");
            Add(list, seen, "Corner3",  3, true,  "X.", "XX");
            Add(list, seen, "L4",       2, true,  "X.", "X.", "XX");
            Add(list, seen, "J4",       2, true,  ".X", ".X", "XX");
            Add(list, seen, "T4",       2, true,  "XXX", ".X.");
            Add(list, seen, "S4",       2, true,  ".XX", "XX.");
            Add(list, seen, "Z4",       2, true,  "XX.", ".XX");
            Add(list, seen, "BigL5",    2, true,  "X..", "X..", "XXX");
            Add(list, seen, "Diag2",    2, true,  "X.", ".X");
            return list;
        }

        static void Add(List<Shape> list, HashSet<string> seen, string name, int weight, bool rotate, params string[] rows)
        {
            var cells = new List<Vector2Int>();
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (rows[y][x] == 'X') cells.Add(new Vector2Int(x, y));

            var current = cells.ToArray();
            int turns = rotate ? 4 : 1;
            for (int t = 0; t < turns; t++)
            {
                var s = new Shape($"{name}_{t}", current, weight);
                if (seen.Add(s.Key())) list.Add(s);
                // xoay 90 độ: (x, y) -> (-y, x)
                var next = new Vector2Int[current.Length];
                for (int i = 0; i < current.Length; i++) next[i] = new Vector2Int(-current[i].y, current[i].x);
                current = next;
            }
        }

        /// <summary>Random theo trọng số. easyBonus &gt; 1 tăng tỉ lệ ra block dễ (Lucky Blocks).</summary>
        /// <param name="minCells">Chỉ lấy shape có ít nhất bấy nhiêu ô (debuff Khối Khổng Lồ). 0 = không giới hạn.</param>
        public static Shape Random(System.Random rng, float easyBonus = 1f, int minCells = 0)
        {
            float total = 0f;
            foreach (var s in All) total += WeightOf(s, easyBonus, minCells);
            if (total <= 0f) throw new System.InvalidOperationException($"Không có shape nào có từ {minCells} ô trở lên");
            double roll = rng.NextDouble() * total;
            Shape last = null;
            foreach (var s in All)
            {
                float w = WeightOf(s, easyBonus, minCells);
                if (w <= 0f) continue;
                last = s;
                roll -= w;
                if (roll <= 0) return s;
            }
            return last;
        }

        static float WeightOf(Shape s, float easyBonus, int minCells) =>
            s.CellCount < minCells ? 0f : s.Weight * (s.IsEasy ? easyBonus : 1f);
    }
}
