using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicBlock
{
    /// <summary>Phần Buff của GameManager: chọn buff ở mốc turn, kho consumable, hiệu ứng permanent.</summary>
    public sealed partial class GameManager
    {
        [Header("Buffs")]
        [Tooltip("Số buff ngẫu nhiên đưa ra mỗi lần chọn")]
        [SerializeField] int buffChoices = 3;

        readonly int[] _buffStacks = new int[Enum.GetValues(typeof(BuffType)).Length];
        readonly List<BuffType> _buffOptions = new();
        readonly List<ClearedCell> _buffCleared = new();
        readonly List<int> _tmpRows = new(), _tmpCols = new();

        /// <summary>Đang hiện bảng chọn buff — khoá đặt block và End Turn.</summary>
        public bool IsChoosingBuff { get; private set; }
        public IReadOnlyList<BuffType> BuffOptions => _buffOptions;

        // ---- Hiệu ứng permanent (tính từ số lần đã chọn) ----
        public float ScoreMultiplier => 1f + Stacks(BuffType.ScoreMultiplier) * Buffs.ScoreMultiplierStep;
        public int ExtraPointsPerClear => Stacks(BuffType.ClearBonus) * Buffs.ClearBonusStep;
        public int ComboBonusExtra => Stacks(BuffType.ComboBonus) * Buffs.ComboBonusStep;
        public float EasyBlockBonus => 1f + Stacks(BuffType.LuckyBlocks) * Buffs.LuckyBlocksStep;

        public event Action<IReadOnlyList<BuffType>> BuffChoiceOffered;
        public event Action<BuffType> BuffChosen;
        /// <summary>Consumable vừa dùng + các ô bị xoá trên bảng (để làm hiệu ứng).</summary>
        public event Action<BuffType, IReadOnlyList<ClearedCell>> ConsumableUsed;

        /// <summary>Permanent: số lần đã chọn. Consumable: số lượng đang có.</summary>
        public int Stacks(BuffType type) => _buffStacks[(int)type];

        public int ConsumableCount()
        {
            int n = 0;
            foreach (var t in Buffs.Consumables) n += Stacks(t);
            return n;
        }

        void ResetBuffs()
        {
            Array.Clear(_buffStacks, 0, _buffStacks.Length);
            _buffOptions.Clear();
            IsChoosingBuff = false;
        }

        void OfferBuffChoice()
        {
            _buffOptions.Clear();
            var pool = new List<BuffType>();
            foreach (var b in Buffs.All) pool.Add(b.Type);
            for (int i = 0; i < buffChoices && pool.Count > 0; i++)
            {
                int k = _rng.Next(pool.Count);
                _buffOptions.Add(pool[k]);
                pool.RemoveAt(k);
            }
            IsChoosingBuff = _buffOptions.Count > 0;
            if (IsChoosingBuff) BuffChoiceOffered?.Invoke(_buffOptions);
        }

        public bool ChooseBuff(int optionIndex)
        {
            if (!IsChoosingBuff || optionIndex < 0 || optionIndex >= _buffOptions.Count) return false;
            var type = _buffOptions[optionIndex];
            _buffStacks[(int)type]++;
            if (type == BuffType.ExpandedStorage) storageCapacity += Buffs.ExpandedStorageSlots;
            IsChoosingBuff = false;
            _buffOptions.Clear();
            BuffChosen?.Invoke(type);
            Evaluate();
            return true;
        }

        bool CanUse(BuffType type) => !IsGameOver && !IsChoosingBuff && Stacks(type) > 0;

        void Consume(BuffType type)
        {
            _buffStacks[(int)type]--;
            ConsumableUsed?.Invoke(type, _buffCleared);
            Evaluate();
        }

        /// <summary>Block Destroyer: xoá block (đã đặt) chứa ô (x,y).</summary>
        public bool UseBlockDestroyer(int x, int y)
        {
            if (!CanUse(BuffType.BlockDestroyer) || !Board.InBounds(x, y) || Board.IsEmpty(x, y)) return false;
            Board.RemoveGroupAt(x, y, _buffCleared);
            Consume(BuffType.BlockDestroyer);
            return true;
        }

        /// <summary>Single Line Clear: xoá hàng (isRow) hoặc cột chứa ô (x,y).</summary>
        public bool UseLineClear(int x, int y, bool isRow)
        {
            if (!CanUse(BuffType.SingleLineClear) || !Board.InBounds(x, y)) return false;
            _tmpRows.Clear(); _tmpCols.Clear();
            if (isRow) _tmpRows.Add(y); else _tmpCols.Add(x);
            Board.ClearLines(_tmpRows, _tmpCols, _buffCleared);
            if (_buffCleared.Count == 0) return false; // hàng/cột trống thì không tốn buff
            Consume(BuffType.SingleLineClear);
            return true;
        }

        /// <summary>Block Reroll: đổi các block còn trong Hand thành block mới.</summary>
        public bool UseReroll()
        {
            if (!CanUse(BuffType.BlockReroll) || UnusedHandCount() == 0) return false;
            for (int i = 0; i < Hand.Length; i++)
                if (Hand[i] != null)
                    Hand[i] = new Piece(ShapeLibrary.Random(_rng, EasyBlockBonus), _rng.Next(colorCount));
            _buffCleared.Clear();
            Consume(BuffType.BlockReroll);
            return true;
        }

        /// <summary>Storage Cleanup: bỏ một block khỏi Storage.</summary>
        public bool UseStorageCleanup(int storageIndex)
        {
            if (!CanUse(BuffType.StorageCleanup) || storageIndex < 0 || storageIndex >= _storage.Count) return false;
            _storage.RemoveAt(storageIndex);
            _buffCleared.Clear();
            Consume(BuffType.StorageCleanup);
            return true;
        }
    }
}
