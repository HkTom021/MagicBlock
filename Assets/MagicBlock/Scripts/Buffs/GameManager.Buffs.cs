using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicBlock
{
    /// <summary>Phần Buff của GameManager: chọn buff vĩnh viễn, thưởng consumable, dùng consumable.</summary>
    public sealed partial class GameManager
    {
        [Header("Buffs")]
        [Tooltip("Số buff vĩnh viễn đưa ra mỗi lần chọn")]
        [SerializeField] int buffChoices = 3;

        readonly List<BuffType> _buffOptions = new();
        readonly List<ClearedCell> _buffCleared = new();
        readonly List<int> _tmpRows = new(), _tmpCols = new();

        public BuffState BuffState { get; } = new();

        /// <summary>Đang hiện bảng chọn buff — khoá đặt block và End Turn.</summary>
        public bool IsChoosingBuff { get; private set; }
        public IReadOnlyList<BuffType> BuffOptions => _buffOptions;
        /// <summary>Consumable vừa được thưởng ở màn thử thách gần nhất (để UI hiển thị); null nếu chưa có.</summary>
        public BuffType? LastReward { get; private set; }

        public event Action<IReadOnlyList<BuffType>> BuffChoiceOffered;
        public event Action<BuffType> BuffChosen;
        public event Action<BuffType> ConsumableRewarded;
        /// <summary>Consumable vừa dùng + các ô bị xoá trên bảng (để làm hiệu ứng).</summary>
        public event Action<BuffType, IReadOnlyList<ClearedCell>> ConsumableUsed;

        public int Stacks(BuffType type) => BuffState.Stacks(type);
        public int ConsumableCount() => BuffState.ConsumableCount();

        void ResetBuffs()
        {
            BuffState.Reset();
            _buffOptions.Clear();
            IsChoosingBuff = false;
            LastReward = null;
        }

        void OfferBuffChoice()
        {
            BuffState.RollPermanentOptions(_rng, buffChoices, _buffOptions);
            IsChoosingBuff = _buffOptions.Count > 0;
            if (IsChoosingBuff) BuffChoiceOffered?.Invoke(_buffOptions);
        }

        void GrantChallengeReward()
        {
            var reward = BuffState.RollConsumable(_rng);
            BuffState.Add(reward);
            LastReward = reward;
            ConsumableRewarded?.Invoke(reward);
        }

        public bool ChooseBuff(int optionIndex)
        {
            if (!IsChoosingBuff || optionIndex < 0 || optionIndex >= _buffOptions.Count) return false;
            var type = _buffOptions[optionIndex];
            BuffState.Add(type);
            IsChoosingBuff = false;
            _buffOptions.Clear();
            BuffChosen?.Invoke(type);
            Evaluate();
            return true;
        }

        bool CanUse(BuffType type) => !IsGameOver && !IsChoosingBuff && Stacks(type) > 0;

        void Consume(BuffType type)
        {
            BuffState.TryConsume(type);
            ConsumableUsed?.Invoke(type, _buffCleared);
            Evaluate();
        }

        /// <summary>Block Destroyer: xoá block (đã đặt) chứa ô (x,y). Không dùng được lên ô đá.</summary>
        public bool UseBlockDestroyer(int x, int y)
        {
            if (!CanUse(BuffType.BlockDestroyer) || !Board.InBounds(x, y) || Board.IsEmpty(x, y) || Board.IsStone(x, y)) return false;
            Board.RemoveGroupAt(x, y, _buffCleared);
            Consume(BuffType.BlockDestroyer);
            return true;
        }

        /// <summary>Single Line Clear: xoá hàng (isRow) hoặc cột chứa ô (x,y). Không cộng điểm.</summary>
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
                if (Hand[i] != null) Hand[i] = NewPiece();
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
