using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicBlock
{
    // Thứ tự enum được lưu bằng số vào PlayerPrefs: chỉ thêm ngôn ngữ mới vào cuối.
    public enum Language { Vietnamese, English }

    /// <summary>
    /// Bảng chữ hiển thị song ngữ VI/EN. Chỉ UI dùng class này; logic game không phụ thuộc ngôn ngữ.
    /// Ngôn ngữ đã chọn được lưu ở PlayerPrefs (key MagicBlock.Language), mặc định tiếng Việt.
    /// </summary>
    public static class Localization
    {
        const string PrefKey = "MagicBlock.Language";
        static Language? _current;
        static readonly HashSet<string> _warnedKeys = new();

        public static event Action LanguageChanged;

        public static Language Current
        {
            get
            {
                if (_current == null)
                {
                    int saved = PlayerPrefs.GetInt(PrefKey, (int)Language.Vietnamese);
                    _current = Enum.IsDefined(typeof(Language), saved) ? (Language)saved : Language.Vietnamese;
                }
                return _current.Value;
            }
        }

        public static void Set(Language language)
        {
            if (_current == language) return;
            _current = language;
            PlayerPrefs.SetInt(PrefKey, (int)language);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }

        public static void Toggle() => Set(Current == Language.Vietnamese ? Language.English : Language.Vietnamese);

        /// <summary>Lấy chữ theo key. Key không có trong bảng thì báo warning một lần và trả về chính key để dễ phát hiện.</summary>
        public static string T(string key)
        {
            if (Table.TryGetValue(key, out var texts)) return texts[(int)Current];
            if (_warnedKeys.Add(key)) Debug.LogWarning($"[Localization] Thiếu key: {key}");
            return key;
        }

        public static string T(string key, params object[] args) => string.Format(T(key), args);

        public static string BuffName(BuffType type) => T($"buff.{type}.name");
        public static string BuffShortName(BuffType type) => T($"buff.{type}.short");

        public static string RarityName(Rarity rarity) => T($"rarity.{rarity}");
        public static string DebuffName(DebuffType debuff) => T($"debuff.{debuff}");

        public static string BuffDescription(BuffType type)
        {
            // Mô tả chèn thông số lấy từ BuffDefinitions để khi cân bằng lại không phải sửa chữ.
            object arg = type switch
            {
                BuffType.MultiplierSmall => Buffs.MultiplierSmallStep,
                BuffType.MultiplierMedium => Buffs.MultiplierMediumStep,
                BuffType.MultiplierLarge => Buffs.MultiplierLargeStep,
                BuffType.DoubleMultiplier => Buffs.DoubleMultiplierFactor,
                BuffType.StorageSmall => Buffs.StorageSmallSlots,
                BuffType.ExpandedStorage => Buffs.ExpandedStorageSlots,
                BuffType.BigStorage => Buffs.BigStorageSlots,
                BuffType.LuckUp => Buffs.LuckStep,
                BuffType.ComboBonus => 1f + Buffs.ComboStep,
                _ => null,
            };
            return arg == null ? T($"buff.{type}.desc") : T($"buff.{type}.desc", arg);
        }

        // { key, { Tiếng Việt, English } } — thứ tự cột theo enum Language.
        static readonly Dictionary<string, string[]> Table = new()
        {
            // Header / chung
            { "lang.button",      new[] { "VI", "EN" } },
            { "best",             new[] { "KỶ LỤC {0}", "BEST {0}" } },
            { "stage",            new[] { "{0} - {1}", "{0} - {1}" } },
            { "buffIn.one",       new[] { "Buff sau màn này", "Buff after this stage" } },
            { "buffIn.many",      new[] { "Buff sau {0} màn", "Buff in {0} stages" } },
            { "challenge.header", new[] { "THỬ THÁCH: {0}", "CHALLENGE: {0}" } },
            { "storage",          new[] { "KHO  {0}/{1}", "STORAGE  {0}/{1}" } },
            { "storage.locked",   new[] { "KHO  {0}/{1}  (BỊ KHÓA)", "STORAGE  {0}/{1}  (LOCKED)" } },
            { "endTurn",          new[] { "HẾT LƯỢT", "END TURN" } },
            { "cancel",           new[] { "HỦY", "CANCEL" } },

            // Dòng nhắc
            { "hint.stuckEndTurn",   new[] { "Hết chỗ đặt block - bấm HẾT LƯỢT!", "No space for any block - press END TURN!" } },
            { "hint.stuckBuff",      new[] { "Hết chỗ - hãy dùng buff!", "No space - use a buff!" } },
            { "hint.storageNeeds",   new[] { "Kho cần {0} ô trống để Hết Lượt", "Storage needs {0} free slots to End Turn" } },
            { "hint.storageAlmost",  new[] { "Kho sắp đầy!", "Storage almost full!" } },
            { "hint.tapDestroy",     new[] { "Chạm vào một khối trên bảng để phá", "Tap a block on the board to destroy it" } },
            { "hint.tapRow",         new[] { "Chạm vào bảng để xóa HÀNG đó", "Tap the board to clear that ROW" } },
            { "hint.tapColumn",      new[] { "Chạm vào bảng để xóa CỘT đó", "Tap the board to clear that COLUMN" } },
            { "hint.tapStorage",     new[] { "Chạm vào khối trong Kho để bỏ", "Tap a block in Storage to remove it" } },
            { "lineMode.row",        new[] { "XÓA: HÀNG", "MODE: ROW" } },
            { "lineMode.column",     new[] { "XÓA: CỘT", "MODE: COLUMN" } },

            // Thông báo nổi
            { "praise.0",            new[] { "Tốt!", "Good!" } },
            { "praise.1",            new[] { "Tuyệt!", "Great!" } },
            { "praise.2",            new[] { "Xuất sắc!", "Excellent!" } },
            { "praise.3",            new[] { "Đỉnh cao!", "Amazing!" } },
            { "praise.4",            new[] { "Không thể tin!", "Unbelievable!" } },
            { "toast.toStorage.one", new[] { "1 block vào Kho", "1 block to Storage" } },
            { "toast.toStorage.many",new[] { "{0} block vào Kho", "{0} blocks to Storage" } },
            { "toast.challenge",     new[] { "THỬ THÁCH!\n{0}", "CHALLENGE!\n{0}" } },
            { "toast.storageReset",  new[] { "Kho đã được dọn", "Storage cleared" } },
            { "toast.nothingReroll", new[] { "Không có gì để đổi", "Nothing to reroll" } },
            { "toast.storageEmpty",  new[] { "Kho đang trống", "Storage is empty" } },
            { "toast.tapFilled",     new[] { "Hãy chạm vào ô có khối", "Tap a filled cell" } },
            { "toast.lineEmpty",     new[] { "Hàng/cột đó đang trống", "That line is empty" } },

            // Bảng chọn buff
            { "buffPanel.title",     new[] { "CHỌN BUFF", "CHOOSE A BUFF" } },
            { "buffPanel.reward",    new[] { "Thưởng thử thách: {0} ({1})", "Challenge reward: {0} ({1})" } },
            { "rarity.Common",       new[] { "THƯỜNG", "COMMON" } },
            { "rarity.Uncommon",     new[] { "KHÁ HIẾM", "UNCOMMON" } },
            { "rarity.Rare",         new[] { "HIẾM", "RARE" } },
            { "rarity.Legendary",    new[] { "HUYỀN THOẠI", "LEGENDARY" } },

            // Debuff màn thử thách
            { "debuff.GiantBlocks",   new[] { "Khối Khổng Lồ", "Giant Blocks" } },
            { "debuff.StorageLocked", new[] { "Khóa Kho", "Storage Locked" } },
            { "debuff.NoEndTurn",     new[] { "Không Lùi Bước", "No Retreat" } },
            { "debuff.HalfScore",     new[] { "Điểm Giảm", "Half Score" } },
            { "debuff.Stones",        new[] { "Ô Đá", "Stones" } },
            { "buffTag.consumable",  new[] { "DÙNG 1 LẦN", "CONSUMABLE" } },
            { "buffTag.permanent",   new[] { "VĨNH VIỄN", "PERMANENT" } },
            { "buffOwned.consumable",new[] { "Đang có {0}", "You have {0}" } },
            { "buffOwned.permanent", new[] { "Đã có x{0}", "Owned x{0}" } },

            // Tóm tắt permanent đang có
            { "perk.multiplier",     new[] { "Hệ số x{0}", "Mult x{0}" } },
            { "perk.luck",           new[] { "May mắn {0}", "Luck {0}" } },
            { "perk.combo",          new[] { "Combo x{0}", "Combo x{0}" } },
            { "perk.easy",           new[] { "Khối dễ x{0}", "Easy x{0}" } },
            { "perk.storage",        new[] { "Kho +{0}", "Storage +{0}" } },

            // Game Over
            { "gameOver.title",      new[] { "KẾT THÚC", "GAME OVER" } },
            { "gameOver.newHigh",    new[] { "KỶ LỤC MỚI!\n{0}", "NEW HIGH SCORE!\n{0}" } },
            { "gameOver.score",      new[] { "ĐIỂM {0}\nKỶ LỤC {1}", "SCORE {0}\nBEST {1}" } },
            { "gameOver.stats",      new[] { "Đạt màn: {0} - {1}   Hàng: {2}\nBlock đã đặt: {3}", "Reached: {0} - {1}   Lines: {2}\nBlocks placed: {3}" } },
            { "gameOver.playAgain",  new[] { "CHƠI LẠI", "PLAY AGAIN" } },

            // Buff: tên, tên ngắn trên nút, mô tả
            { "buff.BlockDestroyer.name",  new[] { "Phá Khối", "Block Destroyer" } },
            { "buff.BlockDestroyer.short", new[] { "PHÁ", "DESTROY" } },
            { "buff.BlockDestroyer.desc",  new[] { "Chạm vào một khối trên bảng để phá nó.", "Tap a block on the board to remove it." } },
            { "buff.SingleLineClear.name", new[] { "Xóa Hàng", "Single Line Clear" } },
            { "buff.SingleLineClear.short",new[] { "XÓA", "LINE" } },
            { "buff.SingleLineClear.desc", new[] { "Chạm vào bảng để xóa một hàng hoặc một cột.", "Tap the board to clear one row or column." } },
            { "buff.BlockReroll.name",     new[] { "Đổi Khối", "Block Reroll" } },
            { "buff.BlockReroll.short",    new[] { "ĐỔI", "REROLL" } },
            { "buff.BlockReroll.desc",     new[] { "Đổi các khối đang có trên tay thành khối mới.", "Replace your current blocks with new ones." } },
            { "buff.StorageCleanup.name",  new[] { "Dọn Kho", "Storage Cleanup" } },
            { "buff.StorageCleanup.short", new[] { "DỌN", "CLEAN" } },
            { "buff.StorageCleanup.desc",  new[] { "Bỏ một khối không cần khỏi Kho.", "Remove one unwanted block from Storage." } },
            // Buff vĩnh viễn (chỉ cần tên + mô tả; tên ngắn chỉ dùng cho nút consumable)
            { "buff.MultiplierSmall.name",  new[] { "Hệ Số +", "Multiplier +" } },
            { "buff.MultiplierSmall.desc",  new[] { "Hệ số nhân +{0}.", "+{0} score multiplier." } },
            { "buff.StorageSmall.name",     new[] { "Kho +", "Storage +" } },
            { "buff.StorageSmall.desc",     new[] { "+{0} ô Kho.", "+{0} Storage slot." } },
            { "buff.LuckUp.name",           new[] { "May Mắn +", "Luck +" } },
            { "buff.LuckUp.desc",           new[] { "May mắn +{0}: dễ gặp buff hiếm hơn.", "+{0} Luck: rarer buffs show up more often." } },
            { "buff.MultiplierMedium.name", new[] { "Hệ Số ++", "Multiplier ++" } },
            { "buff.MultiplierMedium.desc", new[] { "Hệ số nhân +{0}.", "+{0} score multiplier." } },
            { "buff.ExpandedStorage.name",  new[] { "Mở Rộng Kho", "Expanded Storage" } },
            { "buff.ExpandedStorage.desc",  new[] { "+{0} ô Kho.", "+{0} Storage slots." } },
            { "buff.LuckyBlocks.name",      new[] { "Khối May Mắn", "Lucky Blocks" } },
            { "buff.LuckyBlocks.desc",      new[] { "Khối nhỏ, dễ đặt xuất hiện nhiều hơn.", "Small, easy blocks appear more often." } },
            { "buff.MultiplierLarge.name",  new[] { "Hệ Số +++", "Multiplier +++" } },
            { "buff.MultiplierLarge.desc",  new[] { "Hệ số nhân +{0}.", "+{0} score multiplier." } },
            { "buff.ComboBonus.name",       new[] { "Thưởng Combo", "Combo Bonus" } },
            { "buff.ComboBonus.desc",       new[] { "Xóa từ 2 đường trở lên cùng lúc: điểm lần đó x{0}.", "Clear 2+ lines at once: that clear scores x{0}." } },
            { "buff.DoubleMultiplier.name", new[] { "Nhân Đôi", "Double Up" } },
            { "buff.DoubleMultiplier.desc", new[] { "Hệ số nhân x{0}.", "Score multiplier x{0}." } },
            { "buff.BigStorage.name",       new[] { "Kho Lớn", "Big Storage" } },
            { "buff.BigStorage.desc",       new[] { "+{0} ô Kho.", "+{0} Storage slots." } },
        };
    }
}
