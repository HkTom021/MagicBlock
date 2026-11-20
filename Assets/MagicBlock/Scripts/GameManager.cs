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
    /// Luật chơi Magic Block: Hand 3 block, Storage giới hạn slot, End Turn, điểm, turn, game over.
    /// Không chứa code UI — GameView lắng nghe các event bên dưới.
    /// </summary>
    public sealed partial class GameManager : MonoBehaviour
    {
        [Header("Board & Turn")]
        [SerializeField] int boardSize = 8;
        [SerializeField] int handSize = 3;
        [SerializeField] int storageCapacity = 5;
        [Tooltip("Cứ mỗi N turn hoàn thành thì được chọn buff")]
        [SerializeField] int turnsPerBuff = 5;
        [SerializeField] int colorCount = 7;

        [Header("Scoring")]
        [SerializeField] int pointsPerCell = 1;
        [SerializeField] int pointsPerLine = 10;
        [Tooltip("Thưởng thêm cho mỗi hàng/cột vượt quá 1 khi xoá cùng lúc")]
        [SerializeField] int multiLineBonusPerExtraLine = 10;

        [Header("Debug")]
        [Tooltip("0 = random seed")]
        [SerializeField] int seed;

        const string HighScoreKey = "MagicBlock.HighScore";

        System.Random _rng;
        readonly List<Piece> _storage = new();

        public BoardModel Board { get; private set; }
        public Piece[] Hand { get; private set; }
        public IReadOnlyList<Piece> Storage => _storage;
        public int StorageCapacity => storageCapacity;
        public int FreeStorageSlots => storageCapacity - _storage.Count;
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public bool IsNewHighScore { get; private set; }
        /// <summary>Số turn đã hoàn thành.</summary>
        public int TurnsCompleted { get; private set; }
        public int TurnsPerBuff => turnsPerBuff;
        public int TurnsUntilBuff => turnsPerBuff - (TurnsCompleted % turnsPerBuff);
        public int TotalLinesCleared { get; private set; }
        public int BlocksPlaced { get; private set; }
        public bool IsGameOver { get; private set; }
        /// <summary>Không block nào đặt được nhưng vẫn còn End Turn để thoát.</summary>
        public bool IsStuck { get; private set; }

        public event Action StateChanged;
        public event Action<PlaceResult> Placed;
        public event Action<int> EndedTurn;           // số block đã chuyển vào Storage
        public event Action<int> BuffMilestoneReached; // TurnsCompleted tại mốc
        public event Action GameEnded;

        int _baseStorageCapacity;

        void Awake()
        {
            _baseStorageCapacity = storageCapacity;
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        void Start() => NewGame();

        public void NewGame()
        {
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
            Board = new BoardModel(boardSize);
            Hand = new Piece[handSize];
            _storage.Clear();
            Score = 0;
            TurnsCompleted = 0;
            TotalLinesCleared = 0;
            BlocksPlaced = 0;
            IsGameOver = false;
            IsNewHighScore = false;
            storageCapacity = _baseStorageCapacity;
            ResetBuffs();
            DealHand();
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
            var piece = GetPiece(source, index);
            if (piece == null || !Board.CanPlace(piece.Shape, x, y)) return false;

            Board.Place(piece.Shape, x, y, piece.Color);
            if (source == PieceSource.Hand) Hand[index] = null;
            else _storage.RemoveAt(index); // lấy ra khỏi kho -> giải phóng slot

            var result = new PlaceResult { Piece = piece, X = x, Y = y };
            Board.FindFullLines(result.Rows, result.Cols);
            Board.ClearLines(result.Rows, result.Cols, result.Cleared);

            result.Points = CalculatePoints(piece.Shape.CellCount, result.Lines);
            AddScore(result.Points);
            TotalLinesCleared += result.Lines;
            BlocksPlaced++;

            Placed?.Invoke(result);

            if (AllHandUsed()) CompleteTurn();
            Evaluate();
            return true;
        }

        public int CalculatePoints(int cells, int lines)
        {
            int points = cells * pointsPerCell;
            if (lines > 0)
            {
                int linePoints = lines * pointsPerLine + (lines - 1) * lines * (multiLineBonusPerExtraLine + ComboBonusExtra);
                points += Mathf.RoundToInt(linePoints * ScoreMultiplier) + ExtraPointsPerClear;
            }
            return points;
        }

        public int UnusedHandCount()
        {
            int n = 0;
            foreach (var p in Hand) if (p != null) n++;
            return n;
        }

        /// <summary>End Turn chỉ hợp lệ khi Storage đủ chỗ cho TẤT CẢ block còn dư.</summary>
        public bool CanEndTurn()
        {
            if (IsGameOver || IsChoosingBuff) return false;
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
            CompleteTurn();
            Evaluate();
            return true;
        }

        void CompleteTurn()
        {
            TurnsCompleted++;
            if (turnsPerBuff > 0 && TurnsCompleted % turnsPerBuff == 0)
            {
                BuffMilestoneReached?.Invoke(TurnsCompleted);
                OfferBuffChoice();
            }
            DealHand();
        }

        void DealHand()
        {
            for (int i = 0; i < Hand.Length; i++)
                Hand[i] = new Piece(ShapeLibrary.Random(_rng, EasyBlockBonus), _rng.Next(colorCount));
        }

        bool AllHandUsed()
        {
            foreach (var p in Hand) if (p != null) return false;
            return true;
        }

        public bool CanPlaceAnywhere(Piece piece) => piece != null && Board.FitsAnywhere(piece.Shape);

        /// <summary>
        /// Kiểm tra thua: không block nào (Hand + Storage) đặt được, không thể End Turn,
        /// không đang chọn buff và không còn consumable buff nào để gỡ thế kẹt.
        /// </summary>
        void Evaluate()
        {
            bool anyFits = false;
            foreach (var p in Hand) if (CanPlaceAnywhere(p)) { anyFits = true; break; }
            if (!anyFits)
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
