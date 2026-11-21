using UnityEngine;

namespace MagicBlock
{
    /// <summary>
    /// Công thức điểm (đã chốt):
    /// điểm = (số khối các hàng ngang bị xoá × số hàng ngang) + (số khối các cột dọc bị xoá × số cột dọc),
    /// rồi nhân hệ số nhân, hệ số combo (khi xoá từ ComboMinLines đường) và hệ số debuff.
    /// Ô giao giữa hàng và cột được tính ở cả hai phía. Đặt khối mà không xoá được gì = 0 điểm.
    /// </summary>
    public static class ScoreRules
    {
        public static int LinePoints(int rows, int cols, int boardSize, float multiplier, float comboFactor, float debuffFactor)
        {
            if (rows < 0 || cols < 0 || boardSize <= 0) return 0;
            int basePoints = boardSize * rows * rows + boardSize * cols * cols;
            if (basePoints == 0) return 0;
            float factor = multiplier * debuffFactor;
            if (rows + cols >= Buffs.ComboMinLines) factor *= comboFactor;
            return Mathf.RoundToInt(basePoints * factor);
        }
    }
}
