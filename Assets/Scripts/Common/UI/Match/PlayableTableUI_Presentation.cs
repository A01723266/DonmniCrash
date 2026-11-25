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
        }

        void RenderHand()
        {
            tileButtons.Clear();
            ClearChildren(hand.content);
            bool choosing = OwnTurn && Selecting;
            if (choosing)
            {
                for (int i=0;i<Display.OptionCount;i++)
                    if (Display.IsAvailable(i)) CreateTile(hand.content,Display.InspectOption(LocalSeat,i),i,true);
            }
            else if (Display.OfferFor(LocalSeat) is GameTile offer)
            {
                selectedId = offer.Id;
                CreateTile(hand.content,offer,offer.Id,OwnTurn);
            }
            else foreach (var f in Display.HandOf(LocalSeat)) CreateTile(hand.content,f,f.Id,OwnTurn);
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
                labelText.text = !tile.HasValue ? "?\n"+(value+1) :
                    tile.Value.Kind == TileKind.Swap ? "A/B" : tile.Value.Kind == TileKind.Reverse ? "REV" : ">>";
            }
            if (!clickable) return view;
            tileButtons[value] = view;
            var hitSurface = view.gameObject.AddComponent<Image>();
            hitSurface.color = Color.clear;
            var button = view.gameObject.AddComponent<Button>();
            button.targetGraphic = hitSurface;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() =>
            {
                if (!IsLocalTurn || InteractionBlocked) return;
                GameAudio.Current?.OnButtonGesture();
                selectedId = value;
                target = -1;
                RenderActions();
            });
            return view;
        }

        protected void RenderActions()
        {
            if (Display == null) return;
            ShowOwnTurnActions(OwnTurn && !OpeningBlocksActions && (!Guided || !ActionsBlocked));
            var offer = Display.OfferFor(LocalSeat);
            var ownTiles = Display.HandOf(LocalSeat);
            GameTile? tile = offer ?? ownTiles.Where(f => f.Id == selectedId).Select(f => (GameTile?)f).FirstOrDefault();
            bool combo = IsLocalTurn && Display.Phase == TurnPhase.Decision && tile?.Kind == TileKind.Standard &&
                TileRules.IsPlayable(tile.Value,Display.Attack) && ownTiles.Any(f=>f.Kind==TileKind.Swap) && Display.Order.Any(Display.CanTarget);
            swapButton.gameObject.SetActive(combo);
            WriteLabel(swapButton.GetComponentInChildren<TMP_Text>(),target < 0 ? T("game.swap") : T("game.target",DisplayName(target)));
            drawButton.interactable = IsLocalTurn && !Selecting;
            WriteLabel(drawButton.GetComponentInChildren<TMP_Text>(),offer.HasValue ? T("game.keep") : T("game.draw"));
            playButton.interactable = IsLocalTurn && selectedId >= 0 && (Selecting || tile.HasValue && TileRules.IsPlayable(tile.Value,Display.Attack));
            WriteLabel(playButton.GetComponentInChildren<TMP_Text>(),Selecting ? T("game.choose") : T("game.play"));
            foreach (var pair in tileButtons) pair.Value.selectionOutline.SetActive(pair.Key == selectedId);
            string title = "";
            if (IsLocalTurn && Selecting) title = T(Display.VisibleSelection ? "game.visible" : "game.blind",Display.RemainingChoices);
            else if (offer.HasValue) title = T("game.offer");
            else if (IsLocalTurn && tile.HasValue)
            {
                var f = tile.Value;
                if (f.Kind == TileKind.Standard) title = T("game.tile",ElementName(f.Sword),ElementName(f.Shield));
                else title = T("game.wild."+(int)f.Kind);
                if (!TileRules.IsPlayable(f,Display.Attack) && f.Kind != TileKind.Swap) title += " · "+T("game.illegal");
            }
            WriteLabel(handContext,title);
            handContext.gameObject.SetActive(title.Length > 0);
            RefreshAssist();
        }
    }
}
