using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicBlock
{
    /// <summary>
    /// Buff người chơi đang có và các chỉ số suy ra từ đó (Hệ số nhân, May mắn, ô Kho…).
    /// Kèm logic quay độ hiếm theo May mắn. Logic thuần, không dính UI.
    /// </summary>
    public sealed class BuffState
    {
        readonly int[] _stacks = new int[Enum.GetValues(typeof(BuffType)).Length];
        readonly List<BuffType> _pool = new();

        /// <summary>Permanent: số lần đã chọn. Consumable: số lượng đang có.</summary>
        public int Stacks(BuffType type) => _stacks[(int)type];

        public void Reset() => Array.Clear(_stacks, 0, _stacks.Length);
        public void Add(BuffType type) => _stacks[(int)type]++;

        public bool TryConsume(BuffType type)
        {
            if (_stacks[(int)type] <= 0) return false;
            _stacks[(int)type]--;
            return true;
        }

        public int ConsumableCount()
        {
            int n = 0;
            foreach (var t in Buffs.Consumables) n += Stacks(t);
            return n;
        }

        // ---- Chỉ số ----
        public float Multiplier =>
            (1f + Stacks(BuffType.MultiplierSmall) * Buffs.MultiplierSmallStep
                + Stacks(BuffType.MultiplierMedium) * Buffs.MultiplierMediumStep
                + Stacks(BuffType.MultiplierLarge) * Buffs.MultiplierLargeStep)
            * Mathf.Pow(Buffs.DoubleMultiplierFactor, Stacks(BuffType.DoubleMultiplier));

        public int Luck => Stacks(BuffType.LuckUp) * Buffs.LuckStep;

        public int StorageBonus =>
            Stacks(BuffType.StorageSmall) * Buffs.StorageSmallSlots
            + Stacks(BuffType.ExpandedStorage) * Buffs.ExpandedStorageSlots
            + Stacks(BuffType.BigStorage) * Buffs.BigStorageSlots;

        public float EasyBlockBonus => 1f + Stacks(BuffType.LuckyBlocks) * Buffs.LuckyBlocksStep;
        public float ComboFactor => 1f + Stacks(BuffType.ComboBonus) * Buffs.ComboStep;

        // ---- Độ hiếm ----

        /// <summary>
        /// Tỉ lệ (%) từng độ hiếm theo May mắn: lấy bớt từ Common (không dưới MinCommonOdds)
        /// rồi chia cho các độ hiếm cao hơn theo đúng tỉ lệ gốc của chúng.
        /// </summary>
        public static float[] RarityOdds(int luck)
        {
            var b = Buffs.BaseRarityOdds;
            var odds = (float[])b.Clone();
            float shift = Mathf.Min(Mathf.Max(0, luck) * Buffs.LuckShiftPerPoint, b[0] - Buffs.MinCommonOdds);
            if (shift <= 0f) return odds;
            float rest = 0f;
            for (int i = 1; i < b.Length; i++) rest += b[i];
            odds[0] -= shift;
            for (int i = 1; i < b.Length; i++) odds[i] += shift * b[i] / rest;
            return odds;
        }

        public Rarity RollRarity(System.Random rng)
        {
            var odds = RarityOdds(Luck);
            double roll = rng.NextDouble() * 100.0;
            for (int i = 0; i < odds.Length; i++)
            {
                roll -= odds[i];
                if (roll < 0) return (Rarity)i;
            }
            return Rarity.Common;
        }

        /// <summary>Đưa ra tối đa <paramref name="count"/> buff vĩnh viễn không trùng nhau, mỗi thẻ quay độ hiếm riêng.</summary>
        public void RollPermanentOptions(System.Random rng, int count, List<BuffType> result)
        {
            result.Clear();
            for (int i = 0; i < count; i++)
            {
                var pick = PickByRarity(rng, RollRarity(rng), consumable: false, exclude: result);
                if (pick == null) break; // hết buff để đưa ra
                result.Add(pick.Value);
            }
        }

        /// <summary>Buff dùng-một-lần ngẫu nhiên theo độ hiếm (không có Legendary thì hạ xuống).</summary>
        public BuffType RollConsumable(System.Random rng)
        {
            var pick = PickByRarity(rng, RollRarity(rng), consumable: true, exclude: null);
            return pick ?? Buffs.Consumables[0];
        }

        /// <summary>
        /// Chọn ngẫu nhiên một buff có độ hiếm <paramref name="rarity"/>. Nếu độ hiếm đó không còn buff
        /// phù hợp thì hạ dần xuống, rồi mới thử các độ hiếm cao hơn.
        /// </summary>
        BuffType? PickByRarity(System.Random rng, Rarity rarity, bool consumable, List<BuffType> exclude)
        {
            int start = (int)rarity;
            int count = Enum.GetValues(typeof(Rarity)).Length;
            for (int step = 0; step < count; step++)
            {
                int r = step <= start ? start - step : step;
                if (r >= count) continue;
                _pool.Clear();
                foreach (var b in Buffs.All)
                    if (b.IsConsumable == consumable && (int)b.Rarity == r && (exclude == null || !exclude.Contains(b.Type)))
                        _pool.Add(b.Type);
                if (_pool.Count > 0) return _pool[rng.Next(_pool.Count)];
            }
            return null;
        }
    }
}
