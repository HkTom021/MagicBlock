using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MagicBlock
{
    /// <summary>Hiển thị một block trong Hand/Storage và xử lý kéo thả lên bảng.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class PieceView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData e)
        {
            if (!_dragging) _view.OnPieceClicked(this);
        }

        public PieceSource Source { get; private set; }
        public int Index { get; private set; }
        public Piece Piece { get; private set; }

        readonly List<RectTransform> _cells = new();
        GameView _view;
        RectTransform _rect;
        RectTransform _cellRoot;
        Transform _homeParent;
        float _restCellSize;
        bool _dragging;
        bool _playable;

        public void Init(GameView view, Piece piece, PieceSource source, int index, float restCellSize, Vector2 hitSize, bool playable)
        {
            _view = view;
            Piece = piece;
            Source = source;
            Index = index;
            _restCellSize = restCellSize;
            _playable = playable;
            _rect = (RectTransform)transform;
            _homeParent = transform.parent;

            // Vùng chạm trong suốt bằng cả slot cho dễ bấm
            var hit = gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            _rect.sizeDelta = hitSize;

            _cellRoot = GameView.NewRect("Cells", transform);
            foreach (var c in piece.Shape.Cells)
            {
                var cell = GameView.NewRect($"Cell_{c.x}_{c.y}", _cellRoot);
                var img = cell.gameObject.AddComponent<Image>();
                img.sprite = view.TileSprite(piece.Color);
                img.raycastTarget = false;
                if (!playable) img.color = new Color(1f, 1f, 1f, 0.45f);
                _cells.Add(cell);
            }
            Layout(restCellSize, view.TileGapRatio);
        }

        void Layout(float cellSize, float gapRatio)
        {
            var shape = Piece.Shape;
            float tile = cellSize * (1f - gapRatio);
            for (int i = 0; i < shape.Cells.Length; i++)
            {
                var c = shape.Cells[i];
                _cells[i].sizeDelta = new Vector2(tile, tile);
                _cells[i].anchoredPosition = new Vector2(
                    (c.x - (shape.Width - 1) * 0.5f) * cellSize,
                    -(c.y - (shape.Height - 1) * 0.5f) * cellSize);
            }
        }

        /// <summary>Vị trí world của ô đầu tiên trong shape (Cells[0]).</summary>
        public Vector3 FirstCellWorldPosition => _cells[0].position;

        public void OnPointerDown(PointerEventData e)
        {
            if (!_playable) return;
            _cellRoot.localScale = Vector3.one * 1.08f;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!_dragging) _cellRoot.localScale = Vector3.one;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (!_playable || !_view.CanInteract) return;
            _dragging = true;
            _cellRoot.localScale = Vector3.one;
            transform.SetParent(_view.DragLayer, true);
            Layout(_view.BoardCellSize, _view.TileGapRatio);
            Follow(e);
            _view.OnPieceDragStart(this);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dragging) return;
            Follow(e);
            _view.OnPieceDragged(this);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_dragging) return;
            _dragging = false;
            if (!_view.OnPieceDropped(this))
                ReturnHome();
        }

        void Follow(PointerEventData e)
        {
            var parent = (RectTransform)_rect.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out var local))
                _rect.anchoredPosition = local + new Vector2(0f, _view.DragLift);
        }

        void ReturnHome()
        {
            transform.SetParent(_homeParent, false);
            _rect.anchoredPosition = Vector2.zero;
            Layout(_restCellSize, _view.TileGapRatio);
        }
    }
}
