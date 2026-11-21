using System.Collections.Generic;

namespace MagicBlock
{
    public enum Rarity { Common, Uncommon, Rare, Legendary }

    public enum BuffType
    {
        // Consumable (dùng 1 lần, chỉ nhận ngẫu nhiên khi qua màn thử thách)
        BlockDestroyer,
        SingleLineClear,
        BlockReroll,
        StorageCleanup,
        // Permanent (hiệu lực tới Game Over, cộng dồn được)
        MultiplierSmall,
        StorageSmall,
        LuckUp,
        MultiplierMedium,
        ExpandedStorage,
        LuckyBlocks,
        MultiplierLarge,
        ComboBonus,
        DoubleMultiplier,
        BigStorage,
    }

    public sealed class BuffInfo
    {
        public readonly BuffType Type;
        public readonly Rarity Rarity;
        public readonly bool IsConsumable;

        public BuffInfo(BuffType type, Rarity rarity, bool consumable)
        {
            Type = type; Rarity = rarity; IsConsumable = consumable;
        }
    }

    /// <summary>
    /// Danh sách, độ hiếm và thông số của từng buff. Chỉnh số liệu cân bằng ở đây.
    /// Tên/mô tả hiển thị nằm ở UI/Localization.cs (song ngữ).
    /// </summary>
    public static class Buffs
    {
        // ---- Thông số buff vĩnh viễn ----
        public const float MultiplierSmallStep = 0.25f;
        public const float MultiplierMediumStep = 0.5f;
        public const float MultiplierLargeStep = 1f;
        public const float DoubleMultiplierFactor = 2f;   // hệ số nhân x2 mỗi lần chọn
        public const int StorageSmallSlots = 1;
        public const int ExpandedStorageSlots = 2;
        public const int BigStorageSlots = 4;
        public const int LuckStep = 1;
        public const float LuckyBlocksStep = 1f;          // khối dễ ra nhiều gấp (1 + n) lần
        public const float ComboStep = 0.5f;              // mỗi lần chọn: điểm combo x(1 + 0.5n)
        public const int ComboMinLines = 2;               // xoá từ 2 đường trở lên mới tính combo

        // ---- Tỉ lệ độ hiếm theo May mắn ----
        /// <summary>Tỉ lệ (%) Common/Uncommon/Rare/Legendary khi May mắn = 0.</summary>
        public static readonly float[] BaseRarityOdds = { 60f, 28f, 10f, 2f };
        /// <summary>Mỗi điểm May mắn chuyển bấy nhiêu % từ Common sang các độ hiếm cao hơn.</summary>
        public const float LuckShiftPerPoint = 5f;
        public const float MinCommonOdds = 20f;

        public static readonly IReadOnlyList<BuffInfo> All = new List<BuffInfo>
        {
            new(BuffType.BlockDestroyer,   Rarity.Common,    true),
            new(BuffType.SingleLineClear,  Rarity.Rare,      true),
            new(BuffType.BlockReroll,      Rarity.Uncommon,  true),
            new(BuffType.StorageCleanup,   Rarity.Common,    true),
            new(BuffType.MultiplierSmall,  Rarity.Common,    false),
            new(BuffType.StorageSmall,     Rarity.Common,    false),
            new(BuffType.LuckUp,           Rarity.Common,    false),
            new(BuffType.MultiplierMedium, Rarity.Uncommon,  false),
            new(BuffType.ExpandedStorage,  Rarity.Uncommon,  false),
            new(BuffType.LuckyBlocks,      Rarity.Uncommon,  false),
            new(BuffType.MultiplierLarge,  Rarity.Rare,      false),
            new(BuffType.ComboBonus,       Rarity.Rare,      false),
            new(BuffType.DoubleMultiplier, Rarity.Legendary, false),
            new(BuffType.BigStorage,       Rarity.Legendary, false),
        };

        public static BuffInfo Get(BuffType type) => All[(int)type];

        public static readonly BuffType[] Consumables =
            { BuffType.BlockDestroyer, BuffType.SingleLineClear, BuffType.BlockReroll, BuffType.StorageCleanup };
    }
}
