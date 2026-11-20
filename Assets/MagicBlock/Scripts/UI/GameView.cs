using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MagicBlock
{
    /// <summary>
    /// Dựng toàn bộ UI (uGUI) lúc chạy và hiển thị state của GameManager.
    /// Bố cục dọc 1080x1920: Score / Board 8x8 / Hand 3 block / Storage / End Turn.
    /// </summary>
    public sealed partial class GameView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameManager game;

        [Header("Art")]
        [SerializeField] Sprite[] tileSprites;
        [SerializeField] Sprite cellSprite;
        [SerializeField] Sprite backgroundSprite;
        [SerializeField] Font font;

        [Header("Audio")]
        [SerializeField] AudioClip sfxBeginDrag;
        [SerializeField] AudioClip sfxPut;
        [SerializeField] AudioClip[] sfxClear;
        [SerializeField] AudioClip sfxGameOver;
        [SerializeField] AudioClip sfxNewHighScore;
        [SerializeField] AudioClip sfxClick;

        [Header("Layout (canvas units)")]
        [SerializeField] float boardCellSize = 110f;
        [SerializeField] float boardPadding = 12f;
        [SerializeField, Range(0f, 0.3f)] float tileGapRatio = 0.05f;
        [SerializeField] float handCellSize = 52f;
        [SerializeField] float storageSlotSize = 130f;
        [SerializeField] float dragLift = 230f;

        static readonly Vector2 RefSize = new(1080f, 1920f);
        static readonly Color PanelColor = new(0.06f, 0.10f, 0.28f, 0.85f);
        static readonly Color SlotColor = new(1f, 1f, 1f, 0.10f);
        static readonly Color GhostColor = new(1f, 1f, 1f, 0.5f);
        static readonly Color WarnColor = new(1f, 0.65f, 0.2f);
        static readonly Color DangerColor = new(1f, 0.35f, 0.35f);
        static readonly string[] Praise = { "Good!", "Great!", "Excellent!", "Amazing!", "Unbelievable!" };

        public float BoardCellSize => boardCellSize;
        public float TileGapRatio => tileGapRatio;
        public float DragLift => dragLift;
        public RectTransform DragLayer { get; private set; }
        public bool CanInteract => game != null && !game.IsGameOver && !game.IsChoosingBuff && _targeting == TargetMode.None;

        RectTransform _root, _board, _fxLayer, _storageRow;
        Image[,] _blockImages;
        readonly RectTransform[] _handSlots = new RectTransform[3];
        readonly List<RectTransform> _storageSlots = new();
        Text _scoreText, _bestText, _turnText, _buffText, _storageLabel, _hintText, _toastText;
        Button _endTurnButton;
        GameObject _gameOverPanel;
        Text _gameOverScore, _gameOverStats;
        AudioSource _audio;
        float _shownScore;
        Coroutine _toastRoutine;
        readonly List<int> _previewRows = new(), _previewCols = new();

        public Sprite TileSprite(int color) =>
            tileSprites != null && tileSprites.Length > 0 ? tileSprites[Mathf.Abs(color) % tileSprites.Length] : null;

        void Awake()
        {
            if (game == null) game = FindFirstObjectByType<GameManager>();
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            Build();
        }

        void OnEnable()
        {
            game.StateChanged += Refresh;
            game.Placed += OnPlaced;
            game.EndedTurn += OnEndedTurn;
            game.BuffMilestoneReached += OnBuffMilestone;
            game.GameEnded += OnGameEnded;
            SubscribeBuffs(true);
        }

        void OnDisable()
        {
            game.StateChanged -= Refresh;
            game.Placed -= OnPlaced;
            game.EndedTurn -= OnEndedTurn;
            game.BuffMilestoneReached -= OnBuffMilestone;
            game.GameEnded -= OnGameEnded;
            SubscribeBuffs(false);
        }

        void Update()
        {
            if (game == null || _scoreText == null) return;
            // điểm tăng dần theo thời gian thực
            _shownScore = Mathf.MoveTowards(_shownScore, game.Score, Mathf.Max(1f, (game.Score - _shownScore) * 8f) * Time.deltaTime);
            if (game.Score < _shownScore) _shownScore = game.Score;
            _scoreText.text = Mathf.RoundToInt(_shownScore).ToString();
        }

        // ================================================================ BUILD

        void Build()
        {
            var canvasGo = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = RefSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var canvasRect = (RectTransform)canvasGo.transform;

            // Background phủ kín màn hình
            var bg = NewRect("Background", canvasRect);
            Stretch(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = backgroundSprite;
            bgImg.color = backgroundSprite ? Color.white : new Color(0.16f, 0.32f, 0.75f);
            bgImg.raycastTarget = false;
            if (backgroundSprite)
            {
                var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;
            }

            _root = NewRect("Root", canvasRect);
            _root.sizeDelta = RefSize;

            BuildHeader();
            BuildBoard();
            BuildHand();
            BuildStorage();
            BuildEndTurn();

            _fxLayer = NewRect("FxLayer", _root);
            Stretch(_fxLayer);
            _toastText = NewText("Toast", _root, "", 96, Color.white, TextAnchor.MiddleCenter);
            Place(_toastText.rectTransform, new Vector2(0, BoardCenterY()), new Vector2(1000, 260));
            _toastText.gameObject.AddComponent<Outline>().effectDistance = new Vector2(4, -4);
            _toastText.gameObject.SetActive(false);

            BuildBuffUI(canvasRect);

            DragLayer = NewRect("DragLayer", _root);
            Stretch(DragLayer);

            BuildBuffPanel(canvasRect);
            BuildGameOver(canvasRect);
        }

        float BoardSizePx => game.Board != null ? game.Board.Size * boardCellSize + boardPadding * 2 : 8 * boardCellSize + boardPadding * 2;
        float BoardCenterY() => -260f - BoardSizePx * 0.5f;

        void BuildHeader()
        {
            _bestText = NewText("Best", _root, "BEST 0", 44, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleLeft);
            Place(_bestText.rectTransform, new Vector2(-250, -80), new Vector2(480, 70));

            _scoreText = NewText("Score", _root, "0", 120, Color.white, TextAnchor.MiddleCenter);
            Place(_scoreText.rectTransform, new Vector2(0, -175), new Vector2(700, 150));
            _scoreText.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3, -3);

            _turnText = NewText("Turn", _root, "TURN 1", 44, Color.white, TextAnchor.MiddleRight);
            Place(_turnText.rectTransform, new Vector2(250, -80), new Vector2(480, 70));

            _buffText = NewText("BuffProgress", _root, "", 34, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleRight);
            Place(_buffText.rectTransform, new Vector2(250, -135), new Vector2(480, 50));
        }

        void BuildBoard()
        {
            int n = 8; // kích thước thật lấy từ game khi Refresh; GameManager.Start chạy sau Awake
            n = Mathf.Max(n, 1);
            float size = n * boardCellSize + boardPadding * 2;
            _board = NewRect("Board", _root);
            Place(_board, new Vector2(0, -260f - size * 0.5f), new Vector2(size, size));
            var panel = _board.gameObject.AddComponent<Image>();
            panel.color = PanelColor;
            panel.raycastTarget = true; // nhận tap khi dùng buff chọn ô
            _board.gameObject.AddComponent<BoardClickHandler>().Init(this);
            BuildBoardCells(n);
        }

        void BuildBoardCells(int n)
        {
            for (int i = _board.childCount - 1; i >= 0; i--) Destroy(_board.GetChild(i).gameObject);
            float size = n * boardCellSize + boardPadding * 2;
            _board.sizeDelta = new Vector2(size, size);
            _board.anchoredPosition = new Vector2(0, -260f - size * 0.5f);
            _blockImages = new Image[n, n];
            float tile = boardCellSize * (1f - tileGapRatio);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var cell = NewRect($"Cell_{x}_{y}", _board);
                cell.anchoredPosition = CellLocalPosition(x, y, n);
                cell.sizeDelta = new Vector2(tile, tile);
                var bgImg = cell.gameObject.AddComponent<Image>();
                bgImg.sprite = cellSprite;
                bgImg.color = cellSprite ? new Color(1, 1, 1, 0.9f) : new Color(1, 1, 1, 0.08f);
                bgImg.raycastTarget = false;

                var block = NewRect("Block", cell);
                Stretch(block);
                var img = block.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                img.enabled = false;
                _blockImages[x, y] = img;
            }
        }

        Vector2 CellLocalPosition(int x, int y, int n)
        {
            float half = (n * boardCellSize) * 0.5f;
            return new Vector2(-half + (x + 0.5f) * boardCellSize, half - (y + 0.5f) * boardCellSize);
        }

        void BuildHand()
        {
            float top = -260f - BoardSizePx - 30f;
            for (int i = 0; i < _handSlots.Length; i++)
            {
                var slot = NewRect($"HandSlot_{i}", _root);
                Place(slot, new Vector2((i - 1) * 345f, top - 135f), new Vector2(330, 270));
                _handSlots[i] = slot;
            }
        }

        void BuildStorage()
        {
            float top = -260f - BoardSizePx - 330f;
            _storageLabel = NewText("StorageLabel", _root, "STORAGE", 38, Color.white, TextAnchor.MiddleCenter);
            Place(_storageLabel.rectTransform, new Vector2(0, top - 20f), new Vector2(800, 50));
            _storageRow = NewRect("StorageRow", _root);
            Place(_storageRow, new Vector2(0, top - 60f - storageSlotSize * 0.5f), new Vector2(1060, storageSlotSize));
        }

        void RebuildStorageSlots(int capacity)
        {
            if (_storageSlots.Count == capacity) return;
            foreach (var s in _storageSlots) Destroy(s.gameObject);
            _storageSlots.Clear();
            float gap = 10f;
            float slot = Mathf.Min(storageSlotSize, (1060f - gap * (capacity - 1)) / capacity);
            float width = capacity * slot + (capacity - 1) * gap;
            for (int i = 0; i < capacity; i++)
            {
                var s = NewRect($"StorageSlot_{i}", _storageRow);
                s.anchoredPosition = new Vector2(-width * 0.5f + slot * 0.5f + i * (slot + gap), 0);
                s.sizeDelta = new Vector2(slot, slot);
                var img = s.gameObject.AddComponent<Image>();
                img.color = SlotColor;
                img.raycastTarget = false;
                _storageSlots.Add(s);
            }
        }

        void BuildEndTurn()
        {
            float top = -260f - BoardSizePx - 330f - 60f - storageSlotSize - 30f;
            _endTurnButton = NewButton("EndTurnButton", _root, "END TURN", new Color(0.95f, 0.45f, 0.25f), () =>
            {
                Play(sfxClick);
                game.EndTurn();
            });
            Place((RectTransform)_endTurnButton.transform, new Vector2(0, top - 60f), new Vector2(360, 120));

            _hintText = NewText("Hint", _root, "", 34, WarnColor, TextAnchor.MiddleCenter);
            Place(_hintText.rectTransform, new Vector2(0, top - 130f), new Vector2(1000, 60));
        }

        void BuildGameOver(RectTransform canvasRect)
        {
            var panel = NewRect("GameOverPanel", canvasRect);
            Stretch(panel);
            var dim = panel.gameObject.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.75f);
            _gameOverPanel = panel.gameObject;

            var title = NewText("Title", panel, "GAME OVER", 110, Color.white, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0, 300), new Vector2(1000, 160), center: true);
            _gameOverScore = NewText("FinalScore", panel, "", 70, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            Place(_gameOverScore.rectTransform, new Vector2(0, 140), new Vector2(1000, 200), center: true);
            _gameOverStats = NewText("Stats", panel, "", 44, Color.white, TextAnchor.MiddleCenter);
            Place(_gameOverStats.rectTransform, new Vector2(0, -40), new Vector2(1000, 200), center: true);

            var again = NewButton("PlayAgainButton", panel, "PLAY AGAIN", new Color(0.3f, 0.75f, 0.35f), () =>
            {
                Play(sfxClick);
                _gameOverPanel.SetActive(false);
                _shownScore = 0;
                game.NewGame();
            });
            Place((RectTransform)again.transform, new Vector2(0, -260), new Vector2(520, 140), center: true);
            _gameOverPanel.SetActive(false);
        }

        // ================================================================ REFRESH

        void Refresh()
        {
            if (game.Board == null) return;
            if (_blockImages == null || _blockImages.GetLength(0) != game.Board.Size) BuildBoardCells(game.Board.Size);
            RefreshBoard();
            RebuildPieces();

            _bestText.text = $"BEST {game.HighScore}";
            _turnText.text = $"TURN {game.TurnsCompleted + 1}";
            _buffText.text = $"Buff in {game.TurnsUntilBuff} turn{(game.TurnsUntilBuff == 1 ? "" : "s")}";

            int used = game.Storage.Count, cap = game.StorageCapacity;
            _storageLabel.text = $"STORAGE  {used}/{cap}";
            _storageLabel.color = game.FreeStorageSlots == 0 ? DangerColor : game.FreeStorageSlots == 1 ? WarnColor : Color.white;

            _endTurnButton.interactable = game.CanEndTurn();
            int unused = game.UnusedHandCount();
            if (RefreshBuffUI()) { }
            else if (game.IsStuck) { _hintText.text = game.CanEndTurn() ? "No space for any block - press END TURN!" : "No space - use a buff!"; _hintText.color = DangerColor; }
            else if (unused > game.FreeStorageSlots) { _hintText.text = $"Storage needs {unused} free slots to End Turn"; _hintText.color = WarnColor; }
            else if (game.FreeStorageSlots <= 1) { _hintText.text = "Storage almost full!"; _hintText.color = WarnColor; }
            else _hintText.text = "";
        }

        void RefreshBoard()
        {
            int n = game.Board.Size;
            for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            {
                var img = _blockImages[x, y];
                int c = game.Board.Get(x, y);
                img.enabled = c != BoardModel.Empty;
                if (img.enabled) img.sprite = TileSprite(c);
                img.color = Color.white;
                img.rectTransform.localScale = Vector3.one;
            }
        }

        void RebuildPieces()
        {
            foreach (var slot in _handSlots)
                for (int i = slot.childCount - 1; i >= 0; i--) Destroy(slot.GetChild(i).gameObject);
            RebuildStorageSlots(game.StorageCapacity);
            foreach (var slot in _storageSlots)
                for (int i = slot.childCount - 1; i >= 0; i--) Destroy(slot.GetChild(i).gameObject);

            for (int i = 0; i < game.Hand.Length && i < _handSlots.Length; i++)
            {
                var p = game.Hand[i];
                if (p == null) continue;
                CreatePieceView(p, PieceSource.Hand, i, _handSlots[i], handCellSize, _handSlots[i].sizeDelta);
            }
            for (int i = 0; i < game.Storage.Count && i < _storageSlots.Count; i++)
            {
                var p = game.Storage[i];
                var slot = _storageSlots[i];
                float cell = Mathf.Min(handCellSize * 0.5f, (slot.sizeDelta.x - 16f) / Mathf.Max(p.Shape.Width, p.Shape.Height));
                CreatePieceView(p, PieceSource.Storage, i, slot, cell, slot.sizeDelta);
            }
        }

        void CreatePieceView(Piece p, PieceSource source, int index, RectTransform slot, float cell, Vector2 hitSize)
        {
            var rt = NewRect($"Piece_{source}_{index}", slot);
            var view = rt.gameObject.AddComponent<PieceView>();
            view.Init(this, p, source, index, cell, hitSize, !game.IsGameOver && game.CanPlaceAnywhere(p));
        }

        // ================================================================ DRAG & DROP

        public void OnPieceDragStart(PieceView piece) => Play(sfxBeginDrag);

        public void OnPieceDragged(PieceView piece)
        {
            RefreshBoard();
            if (!TryGetDropOrigin(piece, out int ox, out int oy)) return;
            var shape = piece.Piece.Shape;
            var ghostSprite = TileSprite(piece.Piece.Color);
            foreach (var c in shape.Cells)
            {
                var img = _blockImages[ox + c.x, oy + c.y];
                img.enabled = true;
                img.sprite = ghostSprite;
                img.color = GhostColor;
            }
            // Highlight những hàng/cột sẽ được xoá bằng màu của block đang kéo
            game.Board.PreviewLines(shape, ox, oy, _previewRows, _previewCols);
            int n = game.Board.Size;
            foreach (int y in _previewRows)
                for (int x = 0; x < n; x++) { var img = _blockImages[x, y]; img.enabled = true; img.sprite = ghostSprite; img.color = Color.white; }
            foreach (int x in _previewCols)
                for (int y = 0; y < n; y++) { var img = _blockImages[x, y]; img.enabled = true; img.sprite = ghostSprite; img.color = Color.white; }
        }

        public bool OnPieceDropped(PieceView piece)
        {
            RefreshBoard();
            if (!TryGetDropOrigin(piece, out int ox, out int oy)) return false;
            Destroy(piece.gameObject);
            if (game.TryPlace(piece.Source, piece.Index, ox, oy)) return true;
            Refresh();
            return true;
        }

        /// <summary>Tính ô góc trên-trái trên bảng từ vị trí ô đầu tiên của block đang kéo.</summary>
        bool TryGetDropOrigin(PieceView piece, out int ox, out int oy)
        {
            int n = game.Board.Size;
            Vector3 local = _board.InverseTransformPoint(piece.FirstCellWorldPosition);
            float half = n * boardCellSize * 0.5f;
            int cx = Mathf.FloorToInt((local.x + half) / boardCellSize);
            int cy = Mathf.FloorToInt((half - local.y) / boardCellSize);
            var first = piece.Piece.Shape.Cells[0];
            ox = cx - first.x;
            oy = cy - first.y;
            return game.Board.CanPlace(piece.Piece.Shape, ox, oy);
        }

        // ================================================================ FEEDBACK

        void OnPlaced(PlaceResult r)
        {
            int n = game.Board.Size;
            // Ô vừa đặt (chưa bị xoá) nảy nhẹ
            foreach (var c in r.Piece.Shape.Cells)
                StartCoroutine(Punch(_blockImages[r.X + c.x, r.Y + c.y].rectTransform));

            if (r.Lines == 0) { Play(sfxPut); return; }

            Play(sfxClear != null && sfxClear.Length > 0 ? sfxClear[Mathf.Min(r.Lines - 1, sfxClear.Length - 1)] : sfxPut);
            Vector2 boardCenter = _board.anchoredPosition;
            foreach (var cell in r.Cleared)
            {
                var fx = NewRect("ClearFx", _fxLayer);
                fx.anchorMin = fx.anchorMax = _board.anchorMin; // cùng hệ neo với Board (mép trên Root)
                fx.anchoredPosition = boardCenter + CellLocalPosition(cell.X, cell.Y, n);
                float tile = boardCellSize * (1f - tileGapRatio);
                fx.sizeDelta = new Vector2(tile, tile);
                var img = fx.gameObject.AddComponent<Image>();
                img.sprite = TileSprite(r.Piece.Color);
                img.raycastTarget = false;
                // lan toả từ vị trí block vừa đặt
                float dist = Mathf.Abs(cell.X - (r.X + r.Piece.Shape.Width * 0.5f)) + Mathf.Abs(cell.Y - (r.Y + r.Piece.Shape.Height * 0.5f));
                StartCoroutine(ClearFx(fx, img, dist * 0.025f));
            }
            ShowToast($"{Praise[Mathf.Min(r.Lines - 1, Praise.Length - 1)]}\n+{r.Points}");
        }

        void OnEndedTurn(int moved)
        {
            if (moved > 0) ShowToast($"{moved} block{(moved > 1 ? "s" : "")} to Storage", 64);
        }

        void OnBuffMilestone(int turns)
        {
            // TODO: mở Buff Selection Panel ở bước tiếp theo
            ShowToast("BUFF REWARD!\n", 90);
        }

        void OnGameEnded()
        {
            Play(game.IsNewHighScore ? sfxNewHighScore : sfxGameOver);
            _shownScore = game.Score;
            _gameOverScore.text = game.IsNewHighScore ? $"NEW HIGH SCORE!\n{game.Score}" : $"SCORE {game.Score}\nBEST {game.HighScore}";
            _gameOverStats.text = $"Turns: {game.TurnsCompleted}   Lines: {game.TotalLinesCleared}\nBlocks placed: {game.BlocksPlaced}";
            StartCoroutine(ShowGameOverDelayed());
        }

        IEnumerator ShowGameOverDelayed()
        {
            yield return new WaitForSeconds(0.6f);
            _gameOverPanel.SetActive(true);
            _gameOverPanel.transform.SetAsLastSibling();
        }

        void ShowToast(string text, int size = 96)
        {
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(Toast(text, size));
        }

        IEnumerator Toast(string text, int size)
        {
            _toastText.text = text;
            _toastText.fontSize = size;
            _toastText.gameObject.SetActive(true);
            var rt = _toastText.rectTransform;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.9f)
            {
                float pop = t < 0.15f ? Mathf.Lerp(0.6f, 1.1f, t / 0.15f) : Mathf.Lerp(1.1f, 1f, (t - 0.15f) / 0.2f);
                rt.localScale = Vector3.one * pop;
                _toastText.color = new Color(1, 1, 1, t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f);
                yield return null;
            }
            _toastText.gameObject.SetActive(false);
        }

        static IEnumerator Punch(RectTransform rt)
        {
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.18f)
            {
                if (rt == null) yield break;
                rt.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        static IEnumerator ClearFx(RectTransform rt, Image img, float delay)
        {
            yield return new WaitForSeconds(delay);
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.28f)
            {
                rt.localScale = Vector3.one * Mathf.Lerp(1.1f, 0f, t * t);
                img.color = new Color(1, 1, 1, 1f - t);
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        void Play(AudioClip clip)
        {
            if (clip != null) _audio.PlayOneShot(clip);
        }

        // ================================================================ UI HELPERS

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Đặt theo mép trên giữa của Root (mặc định) hoặc theo tâm.</summary>
        static void Place(RectTransform rt, Vector2 pos, Vector2 size, bool center = false)
        {
            rt.anchorMin = rt.anchorMax = center ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        Text NewText(string name, Transform parent, string value, int size, Color color, TextAnchor align)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = value;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        Button NewButton(string name, Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            var text = NewText("Label", rt, label, 56, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -3);
            return btn;
        }
    }
}
