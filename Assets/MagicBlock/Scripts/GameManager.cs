using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicBlock
{
    public enum PieceSource { Hand, Storage }

    /// <summary>Kết quả của một lần đặt block — UI dùng để chạy hiệu ứng.</summary>
    public sealed class PlaceResult
    {
        public Piece Piece;
        public int X, Y;
        public readonly List<int> Rows = new();
        public readonly List<int> Cols = new();
        public readonly List<ClearedCell> Cleared = new();
        public int Lines => Rows.Count + Cols.Count;
        public int Points;
    }

    /// <summary>
    /// Luật chơi Magic Block: lượt chính gồm nhiều màn, mỗi màn nhận một nhóm block (màn thử thách nhận nhiều hơn
    /// và có debuff), Storage giới hạn slot, End Turn, điểm, game over.
    /// Điểm nằm ở ScoreRules, đếm lượt/màn ở RunProgress, buff ở BuffState.
    /// Không chứa code UI — GameView lắng nghe các event bên dưới.
    /// </summary>
    public sealed partial class GameManager : MonoBehaviour
    {
        [Header("Board & Stage")]
        [SerializeField] int boardSize = 8;
        [Tooltip("Số block mỗi màn thường")]
        [SerializeField] int handSize = 3;
        [Tooltip("Số block ở màn thử thách")]
        [SerializeField] int challengeHandSize = 5;
        [SerializeField] int storageCapacity = 5;
        [Tooltip("Số màn trong một lượt chính; màn cuối là màn thử thách")]
        [SerializeField] int stagesPerRound = 10;
        [Tooltip("Xong màn này (và xong màn thử thách) thì được chọn buff vĩnh viễn")]
        [SerializeField] int buffStage = 5;
        [SerializeField] int colorCount = 7;

        [Header("Debug")]
        [Tooltip("0 = random seed")]
        [SerializeField] int seed;

        // Key mới vì cách tính điểm đã đổi — điểm cũ không so sánh được
        const string HighScoreKey = "MagicBlock.HighScore.v2";

        System.Random _rng;
        readonly List<Piece> _storage = new();

        public BoardModel Board { get; private set; }
        public Piece[] Hand { get; private set; }
        public IReadOnlyList<Piece> Storage => _storage;
        public int StorageCapacity => storageCapacity + BuffState.StorageBonus;
        public int FreeStorageSlots => StorageCapacity - _storage.Count;
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public bool IsNewHighScore { get; private set; }
        public RunProgress Run { get; private set; }
        /// <summary>Debuff của màn thử thách đang chơi; null ở màn thường.</summary>
        public DebuffType? ActiveDebuff { get; private set; }
        public int TotalLinesCleared { get; private set; }
        public int BlocksPlaced { get; private set; }
        public bool IsGameOver { get; private set; }
        /// <summary>Không block nào đặt được nhưng vẫn còn cách thoát (End Turn / consumable).</summary>
        public bool IsStuck { get; private set; }

        public bool IsStorageLocked => ActiveDebuff == DebuffType.StorageLocked;

        public event Action StateChanged;
        public event Action<PlaceResult> Placed;
        public event Action<int> EndedTurn;            // số block đã chuyển vào Storage
        public event Action<DebuffType> ChallengeStarted;
        public event Action StorageReset;               // Kho bị dọn sau màn thử thách
        public event Action GameEnded;

        void Awake()
        {
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            Run = new RunProgress(stagesPerRound, buffStage);
        }

        void Start() => NewGame();

        public void NewGame()
        {
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
            Board = new BoardModel(boardSize);
            _storage.Clear();
            Score = 0;
            TotalLinesCleared = 0;
            BlocksPlaced = 0;
            IsGameOver = false;
            IsNewHighScore = false;
            Run.Reset();
            ResetBuffs();
            StartStage();
            Evaluate();
        }

        public Piece GetPiece(PieceSource source, int index)
        {
            if (source == PieceSource.Hand) return index >= 0 && index < Hand.Length ? Hand[index] : null;
            return index >= 0 && index < _storage.Count ? _storage[index] : null;
        }

        /// <summary>Đặt block ở (x,y) = vị trí ô góc trên-trái của shape. Trả về false nếu không hợp lệ.</summary>
        public bool TryPlace(PieceSource source, int index, int x, int y)
        {
            if (IsGameOver || IsChoosingBuff) return false;
            if (source == PieceSource.Storage && IsStorageLocked) return false;
            var piece = GetPiece(source, index);
            if (piece == null || !Board.CanPlace(piece.Shape, x, y)) return false;

            Board.Place(piece.Shape, x, y, piece.Color);
            if (source == PieceSource.Hand) Hand[index] = null;
            else _storage.RemoveAt(index); // lấy ra khỏi kho -> giải phóng slot

            var result = new PlaceResult { Piece = piece, X = x, Y = y };
            Board.FindFullLines(result.Rows, result.Cols);
            Board.ClearLines(result.Rows, result.Cols, result.Cleared);

            result.Points = CalculatePoints(result.Rows.Count, result.Cols.Count);
            AddScore(result.Points);
            TotalLinesCleared += result.Lines;
            BlocksPlaced++;

            Placed?.Invoke(result);

            if (AllHandUsed()) CompleteStage();
            Evaluate();
            return true;
        }

        public int CalculatePoints(int rows, int cols)
        {
            float debuff = ActiveDebuff == DebuffType.HalfScore ? Debuffs.HalfScoreFactor : 1f;
            return ScoreRules.LinePoints(rows, cols, Board.Size, BuffState.Multiplier, BuffState.ComboFactor, debuff);
        }

        public int UnusedHandCount()
        {
            int n = 0;
            foreach (var p in Hand) if (p != null) n++;
            return n;
        }

        /// <summary>End Turn chỉ hợp lệ khi Storage đủ chỗ cho TẤT CẢ block còn dư và không bị debuff cấm.</summary>
        public bool CanEndTurn()
        {
            if (IsGameOver || IsChoosingBuff || ActiveDebuff == DebuffType.NoEndTurn) return false;
            int unused = UnusedHandCount();
            return unused > 0 && unused <= FreeStorageSlots;
        }

        public bool EndTurn()
        {
            if (!CanEndTurn()) return false;
            int moved = 0;
            for (int i = 0; i < Hand.Length; i++)
            {
                if (Hand[i] == null) continue;
                _storage.Add(Hand[i]);
                Hand[i] = null;
                moved++;
            }
            EndedTurn?.Invoke(moved);
            CompleteStage();
            Evaluate();
            return true;
        }

        /// <summary>
        /// Kết thúc màn hiện tại. Sau màn thử thách: thưởng 1 consumable ngẫu nhiên và dọn Kho (giữ số slot).
        /// Sau màn mốc buff / màn thử thách: mở bảng chọn buff vĩnh viễn. Rồi bắt đầu màn kế tiếp.
        /// </summary>
        void CompleteStage()
        {
            bool wasChallenge = Run.IsChallenge;
            bool offerBuff = Run.IsBuffStage(Run.Stage);
            ActiveDebuff = null;

            if (wasChallenge)
            {
                GrantChallengeReward();
                _storage.Clear();
                StorageReset?.Invoke();
            }

            Run.Advance();
            if (offerBuff) OfferBuffChoice();
            StartStage();
        }

        /// <summary>Chuẩn bị màn mới: màn thử thách thì quay debuff (đá đặt ngay lên bảng) rồi chia block.</summary>
        void StartStage()
        {
            ActiveDebuff = null;
            if (Run.IsChallenge)
            {
                var debuff = Debuffs.Roll(_rng);
                ActiveDebuff = debuff;
                if (debuff == DebuffType.Stones) Board.PlaceStones(Debuffs.StoneCount, _rng);
                ChallengeStarted?.Invoke(debuff);
            }
            DealHand();
        }

        void DealHand()
        {
            Hand = new Piece[Run.IsChallenge ? challengeHandSize : handSize];
            for (int i = 0; i < Hand.Length; i++) Hand[i] = NewPiece();
        }

        /// <summary>Tạo block mới theo Lucky Blocks và debuff Khối Khổng Lồ (nếu đang có).</summary>
        Piece NewPiece()
        {
            int minCells = ActiveDebuff == DebuffType.GiantBlocks ? Debuffs.GiantMinCells : 0;
            return new Piece(ShapeLibrary.Random(_rng, BuffState.EasyBlockBonus, minCells), _rng.Next(colorCount));
        }

        bool AllHandUsed()
        {
            foreach (var p in Hand) if (p != null) return false;
            return true;
        }

        public bool CanPlaceAnywhere(Piece piece) => piece != null && Board.FitsAnywhere(piece.Shape);

        /// <summary>
        /// Kiểm tra thua: không block nào (Hand + Storage nếu không bị khoá) đặt được, không thể End Turn,
        /// không đang chọn buff và không còn consumable buff nào để gỡ thế kẹt.
        /// </summary>
        void Evaluate()
        {
            bool anyFits = false;
            foreach (var p in Hand) if (CanPlaceAnywhere(p)) { anyFits = true; break; }
            if (!anyFits && !IsStorageLocked)
                foreach (var p in _storage) if (CanPlaceAnywhere(p)) { anyFits = true; break; }

            bool canEscape = CanEndTurn() || IsChoosingBuff || ConsumableCount() > 0;
            IsStuck = !anyFits && canEscape;
            if (!anyFits && !canEscape)
            {
                IsGameOver = true;
                PlayerPrefs.Save();
                StateChanged?.Invoke();
                GameEnded?.Invoke();
                return;
            }
            StateChanged?.Invoke();
        }

        void AddScore(int points)
        {
            Score += points;
            if (Score > HighScore)
            {
                HighScore = Score;
                IsNewHighScore = true;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
            }
        }
    }
}
