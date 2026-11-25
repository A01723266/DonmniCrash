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
        GameObject choicePanel;
        ScrollRect choiceScroll;
        RectTransform offerPreview;
        TMP_Text choiceTitle, choiceHint, choiceClock;
        Button confirmChoices, placeOffer, keepOffer;
        int shownChoice = -1;
        TurnPhase shownPhase;
        readonly List<DominoTileView> choiceTiles = new();
        readonly HashSet<int> desiredMarks = new();
        bool confirmRequested;
        protected virtual string OfferHintKey => "choice.offer.hint";

        void BindChoices()
        {
            choicePanel = FindNode("TileChoicePanel").gameObject;
            choiceScroll = ComponentOf<ScrollRect>("ChoiceScroll");
            offerPreview = ComponentOf<RectTransform>("OfferPreview");
            choiceTitle = ComponentOf<TMP_Text>("ChoiceTitle");
            choiceHint = ComponentOf<TMP_Text>("ChoiceHint");
            choiceClock = ComponentOf<TMP_Text>("ChoiceClock");
            confirmChoices = ComponentOf<Button>("ChoiceContinue");
            placeOffer = ComponentOf<Button>("OfferPlay");
            keepOffer = ComponentOf<Button>("OfferKeep");
            confirmChoices.onClick.AddListener(() => { confirmRequested = true; FlushMarks(); });
            placeOffer.onClick.AddListener(() =>
            {
                if (Display.OfferFor(LocalSeat) is GameTile tile) SubmitMove(new MoveCommand(MoveKind.Play,tile.Id));
            });
            keepOffer.onClick.AddListener(() => SubmitMove(new MoveCommand(MoveKind.Keep)));
        }

        void PresentChoices()
        {
            var phase = Display.Phase;
            var offer = Display.OfferFor(LocalSeat);
            bool offering = phase == TurnPhase.Offer && offer.HasValue;
            bool visible = OwnTurn && !OpeningBlocksActions && (phase.IsSelection() || offering);
            choicePanel.SetActive(visible);
            if (!visible) { shownChoice = -1; confirmRequested = false; return; }

            bool multi = phase == TurnPhase.SwapSelection || Display.RequiredChoices > 1;
            choiceScroll.gameObject.SetActive(!offering);
            offerPreview.gameObject.SetActive(offering);
            confirmChoices.gameObject.SetActive(!offering && multi);
            placeOffer.gameObject.SetActive(offering);
            keepOffer.gameObject.SetActive(offering);
            if (offering)
            {
                placeOffer.interactable = IsLocalTurn && AllowMove(new MoveCommand(MoveKind.Play,offer.Value.Id));
                keepOffer.interactable = IsLocalTurn && AllowMove(new MoveCommand(MoveKind.Keep));
            }

            int id = offering ? offer.Value.Id : Display.SelectionId;
            if (shownPhase != phase || shownChoice != id)
            {
                shownPhase = phase;
                shownChoice = id;
                confirmRequested = false;
                choiceTiles.Clear();
                desiredMarks.Clear();
                ClearChildren(choiceScroll.content);
                ClearChildren(offerPreview);
                if (offering)
                {
                    var rect = (RectTransform)CreateTile(offerPreview,offer,0,false).transform;
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
                else
                {
                    for (int i=0;i<Display.OptionCount;i++)
                    {
                        int index = i;
                        var tile = CreateTile(choiceScroll.content,Display.InspectOption(LocalSeat,i),i,false);
                        AddHitButton(tile).onClick.AddListener(() => TapChoice(index));
                        choiceTiles.Add(tile);
                        if (Display.IsMarked(i)) desiredMarks.Add(i);
                    }
                    choiceScroll.StopMovement();
                    choiceScroll.verticalNormalizedPosition = 1;
                }
            }

            string key = phase switch
            {
                TurnPhase.DrawSelection => "choice.draw",
                TurnPhase.Replacement => "choice.replacement",
                TurnPhase.Refill => "choice.refill",
                TurnPhase.SwapSelection => "choice.swap",
                _ => "choice.offer"
            };
            WriteLabel(choiceTitle,T(key));
            string hint = offering ? OfferHintKey : phase == TurnPhase.SwapSelection ?
                (Display.VisibleSelection ? "choice.visible.hint" : "choice.blind.hint") : key + ".hint";
            WriteLabel(choiceHint,!offering && multi ? T("choice.progress",T(hint),desiredMarks.Count,Display.RequiredChoices) : T(hint));
            confirmChoices.interactable = desiredMarks.Count == Display.RequiredChoices && !confirmRequested;
            for (int i=0;i<choiceTiles.Count;i++) choiceTiles[i].selectionOutline.SetActive(desiredMarks.Contains(i));
            UpdateChoiceClock();
        }

        void UpdateChoiceClock()
        {
            if (choicePanel && choicePanel.activeSelf)
                WriteLabel(choiceClock,Guided ? T("choice.guided") : T("choice.clock",Mathf.CeilToInt(seconds)));
        }

        void TapChoice(int index)
        {
            if (InteractionBlocked || !OwnTurn || confirmRequested) return;
            GameAudio.Current?.OnButtonGesture();
            if (Display.Phase.UsesStock() && Display.RequiredChoices == 1)
            {
                if (IsLocalTurn) SubmitMove(new MoveCommand(MoveKind.Choose,index));
                return;
            }
            if (!desiredMarks.Remove(index) && desiredMarks.Count < Display.RequiredChoices) desiredMarks.Add(index);
            PresentChoices();
            FlushMarks();
        }

        // La UI acepta toques rápidos. En red se envía el conjunto más reciente al quedar libre la revisión.
        void FlushMarks()
        {
            if (!choicePanel.activeSelf || !IsLocalTurn || InteractionBlocked || !Display.Phase.IsSelection()
                || shownChoice != Display.SelectionId || (Display.Phase.UsesStock() && Display.RequiredChoices == 1)) return;
            if (confirmRequested)
            {
                confirmRequested = false;
                SubmitMove(new MoveCommand(MoveKind.Confirm,marks:desiredMarks.ToArray()));
                return;
            }
            for (int i=0;i<choiceTiles.Count;i++)
                if (desiredMarks.Contains(i) != Display.IsMarked(i))
                {
                    SubmitMove(new MoveCommand(MoveKind.Mark,marks:desiredMarks.ToArray()));
                    return;
                }
        }
    }
}
