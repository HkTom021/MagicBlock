using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MagicBlock
{
    /// <summary>UI Buff: bảng chọn buff, thanh consumable cạnh End Turn, chế độ chạm chọn mục tiêu.</summary>
    public sealed partial class GameView
    {
        enum TargetMode { None, Destroy, Line, StorageCleanup }

        static readonly Color ConsumableColor = new(0.15f, 0.70f, 0.75f);
        static readonly Color PermanentColor = new(0.62f, 0.35f, 0.90f);
        static readonly Color CardColor = new(0.10f, 0.14f, 0.32f, 0.97f);
        static readonly Color TargetSlotColor = new(1f, 0.85f, 0.3f, 0.45f);

        TargetMode _targeting = TargetMode.None;
        bool _lineIsRow = true;

        readonly Dictionary<BuffType, Button> _consumableButtons = new();
        readonly Dictionary<BuffType, Text> _consumableLabels = new();
        Text _perksText;
        Button _cancelButton, _lineModeButton;
        Text _lineModeLabel;
        Image _boardPanel;

        GameObject _buffPanel;
        readonly List<Button> _buffCards = new();
        readonly List<Text> _cardNames = new(), _cardTags = new(), _cardDescs = new(), _cardOwned = new();
        readonly List<Image> _cardStripes = new();
        Coroutine _showPanelRoutine;

        void SubscribeBuffs(bool on)
        {
            if (on)
            {
                game.BuffChoiceOffered += OnBuffChoiceOffered;
                game.BuffChosen += OnBuffChosen;
                game.ConsumableUsed += OnConsumableUsed;
            }
            else
            {
                game.BuffChoiceOffered -= OnBuffChoiceOffered;
                game.BuffChosen -= OnBuffChosen;
                game.ConsumableUsed -= OnConsumableUsed;
            }
        }

        // ================================================================ BUILD

        void BuildBuffUI(RectTransform canvasRect)
        {
            _boardPanel = _board.GetComponent<Image>();

            _perksText = NewText("Perks", _root, "", 28, new Color(0.85f, 0.75f, 1f), TextAnchor.UpperLeft);
            _perksText.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(_perksText.rectTransform, new Vector2(-250, -150), new Vector2(480, 90));

            // 4 nút consumable: 2 bên trái, 2 bên phải nút End Turn
            float y = ((RectTransform)_endTurnButton.transform).anchoredPosition.y;
            float[] xs = { -445f, -275f, 275f, 445f };
            for (int i = 0; i < Buffs.Consumables.Length; i++)
            {
                var type = Buffs.Consumables[i];
                var btn = NewButton($"Buff_{type}", _root, "", ConsumableColor, () => OnConsumableButton(type));
                Place((RectTransform)btn.transform, new Vector2(xs[i], y), new Vector2(160, 120));
                var label = btn.GetComponentInChildren<Text>();
                label.fontSize = 28;
                _consumableButtons[type] = btn;
                _consumableLabels[type] = label;
            }

            // Thanh điều khiển khi đang chọn mục tiêu
            _lineModeButton = NewButton("LineModeButton", _root, "ROW", ConsumableColor, () =>
            {
                Play(sfxClick);
                _lineIsRow = !_lineIsRow;
                Refresh();
            });
            Place((RectTransform)_lineModeButton.transform, new Vector2(-200, y), new Vector2(340, 120));
            _lineModeLabel = _lineModeButton.GetComponentInChildren<Text>();

            _cancelButton = NewButton("CancelBuffButton", _root, "CANCEL", new Color(0.55f, 0.55f, 0.6f), () =>
            {
                Play(sfxClick);
                SetTargeting(TargetMode.None);
            });
            Place((RectTransform)_cancelButton.transform, new Vector2(200, y), new Vector2(340, 120));
        }

        void BuildBuffPanel(RectTransform canvasRect)
        {
            var panel = NewRect("BuffPanel", canvasRect);
            Stretch(panel);
            panel.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            _buffPanel = panel.gameObject;

            var title = NewText("Title", panel, "CHOOSE A BUFF", 90, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0, 560), new Vector2(1000, 140), center: true);
            title.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3, -3);

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var card = NewRect($"BuffCard_{i}", panel);
                Place(card, new Vector2(0, 300 - i * 310), new Vector2(940, 280), center: true);
                var bg = card.gameObject.AddComponent<Image>();
                bg.color = CardColor;
                var btn = card.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => OnBuffCardClicked(index));

                var stripe = NewRect("Stripe", card);
                stripe.anchorMin = new Vector2(0, 0); stripe.anchorMax = new Vector2(0, 1);
                stripe.pivot = new Vector2(0, 0.5f);
                stripe.sizeDelta = new Vector2(24, 0);
                stripe.anchoredPosition = Vector2.zero;
                var stripeImg = stripe.gameObject.AddComponent<Image>();
                stripeImg.raycastTarget = false;

                var name = NewText("Name", card, "", 56, Color.white, TextAnchor.MiddleLeft);
                Place(name.rectTransform, new Vector2(-80, -60), new Vector2(700, 80));
                var tag = NewText("Tag", card, "", 30, Color.white, TextAnchor.MiddleRight);
                Place(tag.rectTransform, new Vector2(300, -60), new Vector2(280, 60));
                var desc = NewText("Desc", card, "", 36, new Color(0.85f, 0.9f, 1f), TextAnchor.UpperLeft);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(desc.rectTransform, new Vector2(10, -165), new Vector2(840, 100));
                var owned = NewText("Owned", card, "", 28, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleRight);
                Place(owned.rectTransform, new Vector2(300, -245), new Vector2(280, 40));

                _buffCards.Add(btn);
                _cardStripes.Add(stripeImg);
                _cardNames.Add(name);
                _cardTags.Add(tag);
                _cardDescs.Add(desc);
                _cardOwned.Add(owned);
            }
            _buffPanel.SetActive(false);
        }

        // ================================================================ REFRESH

        /// <summary>Cập nhật UI buff. Trả về true nếu đã ghi đè dòng hint (đang chọn mục tiêu).</summary>
        bool RefreshBuffUI()
        {
            bool targeting = _targeting != TargetMode.None;

            foreach (var type in Buffs.Consumables)
            {
                int count = game.Stacks(type);
                var btn = _consumableButtons[type];
                btn.gameObject.SetActive(!targeting);
                btn.interactable = count > 0 && CanInteract;
                _consumableLabels[type].text = $"{Buffs.Get(type).ShortName}\nx{count}";
            }
            _endTurnButton.gameObject.SetActive(!targeting);
            _cancelButton.gameObject.SetActive(targeting);
            _lineModeButton.gameObject.SetActive(_targeting == TargetMode.Line);
            _lineModeLabel.text = _lineIsRow ? "MODE: ROW" : "MODE: COLUMN";

            _perksText.text = PerksSummary();

            _boardPanel.color = _targeting is TargetMode.Destroy or TargetMode.Line ? new Color(0.45f, 0.30f, 0.05f, 0.9f) : PanelColor;
            if (_targeting == TargetMode.StorageCleanup)
                for (int i = 0; i < _storageSlots.Count; i++)
                    _storageSlots[i].GetComponent<Image>().color = i < game.Storage.Count ? TargetSlotColor : SlotColor;
            else
                foreach (var s in _storageSlots) s.GetComponent<Image>().color = SlotColor;

            if (!targeting) return false;
            _hintText.color = new Color(1f, 0.85f, 0.3f);
            _hintText.text = _targeting switch
            {
                TargetMode.Destroy => "Tap a block on the board to destroy it",
                TargetMode.Line => _lineIsRow ? "Tap the board to clear that ROW" : "Tap the board to clear that COLUMN",
                TargetMode.StorageCleanup => "Tap a block in Storage to remove it",
                _ => "",
            };
            return true;
        }

        string PerksSummary()
        {
            var parts = new List<string>();
            if (game.Stacks(BuffType.ScoreMultiplier) > 0) parts.Add($"Score x{game.ScoreMultiplier:0.#}");
            if (game.Stacks(BuffType.ClearBonus) > 0) parts.Add($"Clear +{game.ExtraPointsPerClear}");
            if (game.Stacks(BuffType.ComboBonus) > 0) parts.Add($"Combo +{game.ComboBonusExtra}");
            if (game.Stacks(BuffType.LuckyBlocks) > 0) parts.Add($"Lucky x{game.EasyBlockBonus:0.#}");
            if (game.Stacks(BuffType.ExpandedStorage) > 0) parts.Add($"Storage +{game.Stacks(BuffType.ExpandedStorage) * Buffs.ExpandedStorageSlots}");
            return string.Join("  ", parts);
        }

        // ================================================================ INPUT

        void OnConsumableButton(BuffType type)
        {
            if (!CanInteract || game.Stacks(type) <= 0) return;
            Play(sfxClick);
            switch (type)
            {
                case BuffType.BlockReroll:
                    if (!game.UseReroll()) ShowToast("Nothing to reroll", 60);
                    break;
                case BuffType.BlockDestroyer: SetTargeting(TargetMode.Destroy); break;
                case BuffType.SingleLineClear: SetTargeting(TargetMode.Line); break;
                case BuffType.StorageCleanup:
                    if (game.Storage.Count == 0) ShowToast("Storage is empty", 60);
                    else SetTargeting(TargetMode.StorageCleanup);
                    break;
            }
        }

        void SetTargeting(TargetMode mode)
        {
            _targeting = mode;
            Refresh();
        }

        public void OnBoardClicked(PointerEventData e)
        {
            if (_targeting is not (TargetMode.Destroy or TargetMode.Line)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_board, e.position, e.pressEventCamera, out var local)) return;
            int n = game.Board.Size;
            float half = n * boardCellSize * 0.5f;
            int x = Mathf.FloorToInt((local.x + half) / boardCellSize);
            int y = Mathf.FloorToInt((half - local.y) / boardCellSize);
            if (!game.Board.InBounds(x, y)) return;

            bool ok = _targeting == TargetMode.Destroy ? game.UseBlockDestroyer(x, y) : game.UseLineClear(x, y, _lineIsRow);
            if (!ok) ShowToast(_targeting == TargetMode.Destroy ? "Tap a filled cell" : "That line is empty", 60);
        }

        public void OnPieceClicked(PieceView piece)
        {
            if (_targeting != TargetMode.StorageCleanup || piece.Source != PieceSource.Storage) return;
            game.UseStorageCleanup(piece.Index);
        }

        void OnBuffCardClicked(int index)
        {
            Play(sfxClick);
            game.ChooseBuff(index);
        }

        // ================================================================ EVENTS

        void OnBuffChoiceOffered(IReadOnlyList<BuffType> options)
        {
            _targeting = TargetMode.None;
            for (int i = 0; i < _buffCards.Count; i++)
            {
                bool has = i < options.Count;
                _buffCards[i].gameObject.SetActive(has);
                if (!has) continue;
                var info = Buffs.Get(options[i]);
                var color = info.IsConsumable ? ConsumableColor : PermanentColor;
                _cardStripes[i].color = color;
                _cardNames[i].text = info.Name;
                _cardTags[i].text = info.IsConsumable ? "CONSUMABLE" : "PERMANENT";
                _cardTags[i].color = color;
                _cardDescs[i].text = info.Description;
                int owned = game.Stacks(info.Type);
                _cardOwned[i].text = owned > 0 ? (info.IsConsumable ? $"You have {owned}" : $"Owned x{owned}") : "";
            }
            if (_showPanelRoutine != null) StopCoroutine(_showPanelRoutine);
            _showPanelRoutine = StartCoroutine(ShowBuffPanel());
        }

        IEnumerator ShowBuffPanel()
        {
            yield return new WaitForSeconds(0.45f); // để hiệu ứng xoá hàng chạy xong
            _buffPanel.SetActive(true);
            _buffPanel.transform.SetAsLastSibling();
            var rt = (RectTransform)_buffPanel.transform;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.2f)
            {
                rt.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, t);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        void OnBuffChosen(BuffType type)
        {
            if (_showPanelRoutine != null) StopCoroutine(_showPanelRoutine);
            _buffPanel.SetActive(false);
            Play(sfxNewHighScore != null ? sfxNewHighScore : sfxClick);
            ShowToast($"{Buffs.Get(type).Name}!", 72);
        }

        void OnConsumableUsed(BuffType type, IReadOnlyList<ClearedCell> cleared)
        {
            _targeting = TargetMode.None;
            int n = game.Board.Size;
            if (cleared.Count > 0)
            {
                Play(sfxClear != null && sfxClear.Length > 0 ? sfxClear[0] : sfxPut);
                Vector2 boardCenter = _board.anchoredPosition;
                float tile = boardCellSize * (1f - tileGapRatio);
                foreach (var cell in cleared)
                {
                    var fx = NewRect("BuffFx", _fxLayer);
                    fx.anchorMin = fx.anchorMax = _board.anchorMin;
                    fx.anchoredPosition = boardCenter + CellLocalPosition(cell.X, cell.Y, n);
                    fx.sizeDelta = new Vector2(tile, tile);
                    var img = fx.gameObject.AddComponent<Image>();
                    img.sprite = TileSprite(cell.Color);
                    img.raycastTarget = false;
                    StartCoroutine(ClearFx(fx, img, 0f));
                }
            }
            else Play(sfxClick);
            ShowToast(Buffs.Get(type).Name, 64);
        }
    }

    /// <summary>Chuyển tap trên bảng về GameView (dùng cho buff chọn ô).</summary>
    public sealed class BoardClickHandler : MonoBehaviour, IPointerClickHandler
    {
        GameView _view;
        public void Init(GameView view) => _view = view;
        public void OnPointerClick(PointerEventData e) => _view.OnBoardClicked(e);
    }
}
