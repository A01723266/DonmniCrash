using System.Linq;
using DonmniCrash.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    public abstract partial class PlayableTableUI
    {
        readonly Sprite[] swords = new Sprite[5], shields = new Sprite[5];
        DominoTileView template;
        int boardSize;
        readonly Sprite[] symbols = new Sprite[5];
        readonly TMP_Text[] helpLabels = new TMP_Text[5];

        string ElementName(Element e) => LocalizedText.Read("game.element."+(int)e);

        void BindArt()
        {
            template = Resources.Load<GameObject>("Prefabs/UI/Gameplay/DominoTileView").GetComponent<DominoTileView>();
            for (int i = 0; i < swords.Length; i++)
            {
                var row = FindNode(UserSettings.Emblems[i]+"HelpRow");
                symbols[i] = FindNode("ElementIcon",row).GetComponent<Image>().sprite;
                helpLabels[i] = FindNode("ElementLabel",row).GetComponent<TMP_Text>();
                swords[i] = FindNode("AttackIcon",row).GetComponent<Image>().sprite;
                shields[i] = FindNode("DefenseIcon",row).GetComponent<Image>().sprite;
            }
        }

        // Build the help table from the same element rules that validate moves.
        void RenderHelp()
        {
            for (int i = 0; i < helpLabels.Length; i++)
            {
                var beaten = System.Enum.GetValues(typeof(Element)).Cast<Element>()
                    .Where(incomingAttack => TileRules.Beats((Element)i,incomingAttack)).Select(ElementName).ToArray();
                WriteLabel(helpLabels[i],T("game.beats",ElementName((Element)i),beaten[0],beaten[1]));
            }
        }

        // Deactivate removed objects immediately so the layout never counts two hands in one frame.
        static void ClearChildren(Transform parent)
        {
            foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }

        void Render()
        {
            for (int i = 0; i < 6; i++)
            {
                var card = FindNode("Player"+(i+1)+"Card");
                card.gameObject.SetActive(i < Display.Order.Count);
                if (i >= Display.Order.Count) continue;
                int actor = Display.Order[i];
                int emblem = Emblem(actor);
                FindNode("SeatEmblem",card).GetComponent<Image>().sprite = symbols[emblem];
                WriteLabel(FindNode("PlayerNameLabel",card).GetComponent<TMP_Text>(),DisplayName(actor));
                WriteLabel(FindNode("PlayerStatsLabel",card).GetComponent<TMP_Text>(),T("game.stats",Display.Health(actor),Display.HandCount(actor)));
                FindNode("TurnIndicator",card).gameObject.SetActive(Display.ActiveSeat == actor && !IsFinished);
            }
            SetLabel("TurnLabel",T("game.turn",DisplayName(Display.ActiveSeat)));
            SetLabel("ReserveCountLabel",T("game.stock",Display.StockCount));
            SetLabel("CurrentAttackLabel",Display.Attack.HasValue ? T("game.attack",ElementName(Display.Attack.Value)) : T("game.open"));
            var icon = ComponentOf<Image>("CurrentAttackIcon");
            icon.enabled = Display.Attack.HasValue;
            if (icon.enabled) icon.sprite = swords[(int)Display.Attack.Value];
            bool followHistory = boardSize < Display.BoardTiles.Count;
            while (boardSize < Display.BoardTiles.Count)
                CreateTile(historyScroll.content,Display.BoardTiles[boardSize++],-1,false);
            if (followHistory) { Canvas.ForceUpdateCanvases(); historyScroll.horizontalNormalizedPosition = 1; }
            RenderHand();
            RenderActions();
        }

        void RenderHand()
        {
            tileButtons.Clear();
            ClearChildren(hand.content);
            bool choosing = IsLocalTurn && Selecting;
            if (choosing)
            {
                for (int i=0;i<Display.OptionCount;i++)
                    if (Display.IsAvailable(i)) CreateTile(hand.content,Display.InspectOption(LocalSeat,i),i,true);
            }
            else if (Display.OfferFor(LocalSeat) is GameTile offer)
            {
                selectedId = offer.Id;
                CreateTile(hand.content,offer,offer.Id,IsLocalTurn);
            }
            else foreach (var f in Display.HandOf(LocalSeat)) CreateTile(hand.content,f,f.Id,IsLocalTurn);
        }

        void CreateTile(Transform parent,GameTile? tile,int value,bool clickable)
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
            if (!clickable) return;
            tileButtons[value] = view;
            var hitSurface = view.gameObject.AddComponent<Image>();
            hitSurface.color = Color.clear;
            var button = view.gameObject.AddComponent<Button>();
            button.targetGraphic = hitSurface;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => { GameAudio.Current?.OnButtonGesture(); selectedId = value; target = -1; RenderActions(); });
        }

        void RenderActions()
        {
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
            string title = T("game.hand");
            if (IsLocalTurn && Selecting) title = T(Display.VisibleSelection ? "game.visible" : "game.blind",Display.RemainingChoices);
            else if (offer.HasValue) title = T("game.offer");
            else if (IsLocalTurn && tile.HasValue)
            {
                var f = tile.Value;
                if (f.Kind == TileKind.Standard) title = T("game.tile",ElementName(f.Sword),ElementName(f.Shield));
                else title = T("game.wild."+(int)f.Kind);
                if (!TileRules.IsPlayable(f,Display.Attack) && f.Kind != TileKind.Swap) title += " · "+T("game.illegal");
            }
            SetLabel("HandTitleLabel",title);
        }
    }
}
