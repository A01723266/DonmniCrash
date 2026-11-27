using System.Collections.Generic;
using System.Linq;
using DonmniCrash.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    public abstract partial class PlayableTableUI
    {
        const float ExpandedHudHeight = 180;
        const float CompactHudHeight = 134;
        readonly Dictionary<int, RectTransform> cardBySeat = new();
        PlayerHudCard[] playerCards;
        ScrollRect playerScroll;
        RectTransform topHud, tutorialPanel;
        TMP_Text turnToggleLabel, handContext;
        GameObject drawAssist;
        bool compactTurns;
        int lastVisibleSeat = -1;

        void BindHud()
        {
            playerScroll = ComponentOf<ScrollRect>("PlayersScrollView");
            playerCards = playerScroll.content.GetComponentsInChildren<PlayerHudCard>(true);
            topHud = ComponentOf<RectTransform>("TopHUDPanel");
            tutorialPanel = FindNode("TutorialStepPanel") as RectTransform;
            handContext = ComponentOf<TMP_Text>("HandContextLabel");
            drawAssist = FindNode("DrawAssistMarker").gameObject;
            var toggle = ComponentOf<Button>("ToggleTurnsButton");
            turnToggleLabel = toggle.GetComponentInChildren<TMP_Text>();
            toggle.onClick.AddListener(() =>
            {
                compactTurns = !compactTurns;
                lastVisibleSeat = -1;
                ApplyTurnLayout();
                if (Display != null) RenderPlayers();
            });
            BindHandHud();
            ApplyTurnLayout();
        }

        void ApplyTurnLayout()
        {
            float top = compactTurns ? CompactHudHeight : ExpandedHudHeight;
            topHud.offsetMin = new Vector2(topHud.offsetMin.x,-top);
            var board = boardArea;
            board.offsetMax = new Vector2(board.offsetMax.x,-top - 8);
            if (tutorialPanel)
                tutorialPanel.offsetMax = new Vector2(tutorialPanel.offsetMax.x,-top - 8);
            RefreshTurnToggle();
        }

        void RefreshTurnToggle() => WriteLabel(turnToggleLabel,
            T(compactTurns ? "game.turns.expand" : "game.turns.collapse"));

        protected void RenderPlayers()
        {
            var order = Display.Order;
            RectTransform activeCard = null;
            for (int i = 0; i < playerCards.Length; i++)
            {
                var card = playerCards[i];
                card.gameObject.SetActive(i < order.Count);
                if (i >= order.Count) continue;
                int index = compactTurns && Display.Direction < 0 && i > 0 ? order.Count - i : i;
                int seat = order[index];
                cardBySeat[seat] = (RectTransform)card.transform;
                int health = Display.Health(seat);
                bool active = Display.ActiveSeat == seat && !IsFinished;
                card.Show(DisplayName(seat),Portrait(seat),health,StartingHealth,Display.HandCount(seat),active,compactTurns,seat == LocalSeat);
                if (active) activeCard = (RectTransform)card.transform;
            }
            if (activeCard && lastVisibleSeat != Display.ActiveSeat) RevealActivePlayer(activeCard);
            lastVisibleSeat = Display.ActiveSeat;
        }

        void RevealActivePlayer(RectTransform card)
        {
            Canvas.ForceUpdateCanvases();
            var viewport = playerScroll.viewport;
            var content = playerScroll.content;
            float scrollWidth = content.rect.width - viewport.rect.width;
            if (scrollWidth <= 0) return;

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,card);
            float offset = 0;
            if (bounds.min.x < viewport.rect.xMin) offset = bounds.min.x - viewport.rect.xMin;
            else if (bounds.max.x > viewport.rect.xMax) offset = bounds.max.x - viewport.rect.xMax;
            if (Mathf.Approximately(offset,0)) return;
            playerScroll.StopMovement();
            playerScroll.horizontalNormalizedPosition = Mathf.Clamp01(playerScroll.horizontalNormalizedPosition + offset / scrollWidth);
        }

        void RefreshAssist()
        {
            bool enabled = UserSettings.PlayAssist && IsLocalTurn;
            var phase = Display?.Phase;
            bool placing = enabled && (phase == TurnPhase.Decision || phase == TurnPhase.Offer);
            var offer = placing ? Display.OfferFor(LocalSeat) : null;
            var ownTiles = placing ? (offer.HasValue ? new[] { offer.Value } : Display.HandOf(LocalSeat)) : System.Array.Empty<GameTile>();
            var playable = new HashSet<int>();
            foreach (var tile in ownTiles)
                if (CanSuggestPlay(tile)) playable.Add(tile.Id);

            foreach (var pair in tileButtons)
                pair.Value.assistMarker.SetActive(playable.Contains(pair.Key));
            drawAssist.SetActive(placing && phase == TurnPhase.Decision && playable.Count == 0
                && drawButton.interactable && AllowMove(new MoveCommand(MoveKind.Draw)));
        }

        bool CanSuggestPlay(GameTile tile)
        {
            if (!TileRules.IsPlayable(tile,Display.Attack)) return false;
            if (AllowMove(new MoveCommand(MoveKind.Play,tile.Id))) return true;
            // Intercambio requiere una ficha ordinaria válida y un destinatario elegible.
            return Display.Phase == TurnPhase.Decision && tile.Kind == TileKind.Standard &&
                Display.HandOf(LocalSeat).Any(item => item.Kind == TileKind.Swap) &&
                Display.Order.Any(seat => Display.CanTarget(seat) && AllowMove(new MoveCommand(MoveKind.Play,tile.Id,seat)));
        }
    }
}
