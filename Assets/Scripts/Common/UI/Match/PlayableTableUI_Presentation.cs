using System.Linq;
using DonmniCrash.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    public abstract partial class PlayableTableUI
    {
        Sprite[] swords, shields, symbols;
        DominoTileView template;

        string ElementName(Element e) => LocalizedText.Read("game.element."+(int)e);

        void BindArt()
        {
            template = Resources.Load<GameObject>("Prefabs/UI/Gameplay/DominoTileView").GetComponent<DominoTileView>();
            var guide = ComponentOf<ElementHelpView>("ElementHelpDialog");
            swords = guide.swords;
            shields = guide.shields;
            symbols = guide.symbols;
        }

        // Deactivate removed objects immediately so the layout never counts two hands in one frame.
        static void ClearChildren(Transform parent)
        {
            foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }

        void Render()
        {
            RenderPlayers();
            SetLabel("ReserveCountLabel",T("game.stock",Display.StockCount));
            RenderBoard();
            RenderHand();
            RenderPersonalHealth();
            RenderActions();
            bool watching = !Guided && (LocalSeat < 0 || Display.Health(LocalSeat) == 0);
            spectatorPanel.SetActive(watching);
            handPanel.SetActive(!watching);
        }

        DominoTileView CreateTile(Transform parent,GameTile? tile,int value,bool clickable)
        {
            var view = Instantiate(template,parent);
            bool ordinaria = tile.HasValue && tile.Value.Kind == TileKind.Standard;
            view.attackIcon.enabled = view.defenseIcon.enabled = ordinaria;
            view.selectionOutline.SetActive(clickable && selectedId == value);
            if (ordinaria)
            {
                view.attackIcon.sprite = swords[(int)tile.Value.Sword];
                view.defenseIcon.sprite = shields[(int)tile.Value.Shield];
            }
            else
            {
                view.transform.Find("Separator").gameObject.SetActive(false);
                var labelText = new GameObject("TileSymbol",typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                labelText.transform.SetParent(view.transform,false);
                labelText.rectTransform.anchorMin = Vector2.zero;
                labelText.rectTransform.anchorMax = Vector2.one;
                labelText.rectTransform.offsetMin = labelText.rectTransform.offsetMax = Vector2.zero;
                labelText.font = clockLabel.font;
                labelText.fontSize = 22;
                labelText.color = Color.white;
                labelText.outlineWidth = .2f;
                labelText.outlineColor = Color.black;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.raycastTarget = false;
                // Show a compact symbol on the slab and the full name on selection.
                labelText.text = !tile.HasValue ? "?" :
                    tile.Value.Kind == TileKind.Swap ? "A/B" : tile.Value.Kind == TileKind.Reverse ? "REV" : ">>";
            }
            if (!clickable) return view;
            tileButtons[value] = view;
            var button = AddHitButton(view);
            button.onClick.AddListener(() =>
            {
                if (!IsLocalTurn || InteractionBlocked || Display.Phase != TurnPhase.Decision) return;
                OnButtonGesture();
                selectedId = value;
                swapEnabled = false;
                RenderActions();
            });
            return view;
        }

        static Button AddHitButton(DominoTileView view)
        {
            var surface = view.gameObject.AddComponent<Image>();
            surface.color = Color.clear;
            var button = view.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        protected void RenderActions()
        {
            if (Display == null) return;
            ShowOwnTurnActions(OwnTurn && Display.Phase == TurnPhase.Decision && !OpeningBlocksActions && (!Guided || !ActionsBlocked));
            var ownTiles = Display.HandOf(LocalSeat);
            GameTile? tile = ownTiles.Where(f => f.Id == selectedId).Select(f => (GameTile?)f).FirstOrDefault();
            bool combo = IsLocalTurn && Display.Phase == TurnPhase.Decision && tile?.Kind == TileKind.Standard &&
                TileRules.IsPlayable(tile.Value,Display.Attack) && ownTiles.Any(f=>f.Kind==TileKind.Swap) && Display.Order.Any(Display.CanTarget);
            swapButton.gameObject.SetActive(combo);
            WriteLabel(swapButton.GetComponentInChildren<TMP_Text>(),T(swapEnabled ? "swap.on" : "swap.off"));
            drawButton.interactable = IsLocalTurn && Display.Phase == TurnPhase.Decision;
            WriteLabel(drawButton.GetComponentInChildren<TMP_Text>(),T("game.draw"));
            playButton.interactable = IsLocalTurn && selectedId >= 0 && tile.HasValue && TileRules.IsPlayable(tile.Value,Display.Attack);
            WriteLabel(playButton.GetComponentInChildren<TMP_Text>(),T("game.play"));
            foreach (var pair in tileButtons) pair.Value.selectionOutline.SetActive(pair.Key == selectedId);
            string title = "";
            if (IsLocalTurn && tile.HasValue)
            {
                var f = tile.Value;
                if (f.Kind == TileKind.Standard) title = T("game.tile",ElementName(f.Sword),ElementName(f.Shield));
                else title = T("game.wild."+(int)f.Kind);
                if (!TileRules.IsPlayable(f,Display.Attack) && f.Kind != TileKind.Swap) title += " · "+T("game.illegal");
            }
            WriteLabel(handContext,title);
            handContext.gameObject.SetActive(title.Length > 0);
            RefreshAssist();
            PresentChoices();
            RefreshSwapTargets(combo);
        }
    }
}
