using System;

namespace MagicBlock
{
    /// <summary>Debuff của màn thử thách. Chỉ có hiệu lực trong màn đó.</summary>
    public enum DebuffType
    {
        GiantBlocks,   // chỉ ra khối từ GiantMinCells ô trở lên
        StorageLocked, // không lấy được khối từ Kho
        NoEndTurn,     // không được Hết Lượt
        HalfScore,     // điểm nhân HalfScoreFactor
        Stones,        // đặt StoneCount ô đá lên bảng
    }

    /// <summary>Thông số debuff. Tên/mô tả hiển thị nằm ở UI/Localization.cs.</summary>
    public static class Debuffs
    {
        public const int GiantMinCells = 4;
        public const float HalfScoreFactor = 0.5f;
        public const int StoneCount = 3;

        static readonly DebuffType[] _all = (DebuffType[])Enum.GetValues(typeof(DebuffType));

        public static DebuffType Roll(Random rng) => _all[rng.Next(_all.Length)];
    }
}
