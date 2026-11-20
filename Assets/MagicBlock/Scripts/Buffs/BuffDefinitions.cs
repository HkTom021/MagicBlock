using System.Collections.Generic;

namespace MagicBlock
{
    public enum BuffType
    {
        // Consumable (dùng 1 lần, tích trữ được)
        BlockDestroyer,
        SingleLineClear,
        BlockReroll,
        StorageCleanup,
        // Permanent (hiệu lực tới Game Over, cộng dồn được)
        ExpandedStorage,
        ScoreMultiplier,
        ComboBonus,
        LuckyBlocks,
        ClearBonus,
    }

    public sealed class BuffInfo
    {
        public readonly BuffType Type;
        public readonly string Name;
        public readonly string ShortName;   // hiển thị trên nút nhỏ
        public readonly string Description;
        public readonly bool IsConsumable;

        public BuffInfo(BuffType type, string name, string shortName, string description, bool consumable)
        {
            Type = type; Name = name; ShortName = shortName; Description = description; IsConsumable = consumable;
        }
    }

    /// <summary>Thông số và mô tả của từng buff. Chỉnh số liệu cân bằng ở đây.</summary>
    public static class Buffs
    {
        public const int ExpandedStorageSlots = 2;
        public const float ScoreMultiplierStep = 0.5f;   // +50% điểm xoá hàng mỗi lần chọn
        public const int ComboBonusStep = 10;            // +10 điểm cho mỗi hàng thêm khi xoá nhiều hàng
        public const float LuckyBlocksStep = 1f;         // block dễ ra nhiều gấp (1 + n) lần
        public const int ClearBonusStep = 20;            // +20 điểm mỗi lần xoá hàng/cột

        public static readonly IReadOnlyList<BuffInfo> All = new List<BuffInfo>
        {
            new(BuffType.BlockDestroyer,  "Block Destroyer",  "DESTROY", "Tap a block on the board to remove it.", true),
            new(BuffType.SingleLineClear, "Single Line Clear", "LINE",    "Tap the board to clear one row or column.", true),
            new(BuffType.BlockReroll,     "Block Reroll",     "REROLL",  "Replace your current blocks with new ones.", true),
            new(BuffType.StorageCleanup,  "Storage Cleanup",  "CLEAN",   "Remove one unwanted block from Storage.", true),
            new(BuffType.ExpandedStorage, "Expanded Storage", "STORAGE", $"+{ExpandedStorageSlots} Storage slots.", false),
            new(BuffType.ScoreMultiplier, "Score Multiplier", "SCORE",   $"+{(int)(ScoreMultiplierStep * 100)}% points from clearing lines.", false),
            new(BuffType.ComboBonus,      "Combo Bonus",      "COMBO",   $"+{ComboBonusStep} bonus per extra line in a multi-line clear.", false),
            new(BuffType.LuckyBlocks,     "Lucky Blocks",     "LUCKY",   "Small, easy blocks appear more often.", false),
            new(BuffType.ClearBonus,      "Clear Bonus",      "BONUS",   $"+{ClearBonusStep} points every time you clear.", false),
        };

        public static BuffInfo Get(BuffType type) => All[(int)type];

        public static readonly BuffType[] Consumables =
            { BuffType.BlockDestroyer, BuffType.SingleLineClear, BuffType.BlockReroll, BuffType.StorageCleanup };
    }
}
