using System;
using System.Collections.Generic;
using System.Linq;
using DonmniCrash.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Translate pointer gestures into commands and present the resulting state.</summary>
    public abstract partial class PlayableTableUI : TableNavigation
    {
        protected MatchEngine Match { get; private set; }
        protected ITableView Display { get; private set; }
        protected virtual int LocalSeat => 0;
        protected virtual int StartingHealth => Match.Config.Health;
        protected virtual int Emblem(int seat) => seat == 0 ? Array.IndexOf(UserSettings.Emblems.ToArray(),UserSettings.Emblem) : seat % 5;
        protected override void OnButtonGesture() => GameAudio.Current?.UnlockMusic();
        protected virtual bool Guided => false;
        protected virtual bool ActionsBlocked => false;
        protected virtual bool OpeningBlocksActions => false;
        protected OpeningRollView OpeningView { get; private set; }
        protected virtual bool AllowMove(MoveCommand a) => true;
        protected virtual void OnAccepted(MoveCommand a) { }
        protected virtual void OnLanguageChanged() { }
        protected virtual void RunBot() { }
        protected virtual void Finish() { }
        protected bool IsFinished => Display.Phase == TurnPhase.Finished || (Guided && Display.Health(0) == 0);
        bool OwnTurn => Display != null && LocalSeat >= 0 && Display.ActiveSeat == LocalSeat && !IsFinished;
        bool IsLocalTurn => OwnTurn && !ActionsBlocked && !OpeningBlocksActions;
        float seconds, botDelay;
        int displayedSecond, selectedId = -1;
        Button playButton, drawButton, swapButton;
        ScrollRect hand;
        TMP_Text clockLabel;
        readonly Dictionary<int,DominoTileView> tileButtons = new Dictionary<int,DominoTileView>();

        protected override void BindControls()
        {
            base.BindControls();
            hand = ComponentOf<ScrollRect>("HandScrollView");
            handDrag = hand.content.gameObject.AddComponent<HandDrag>();
            handDrag.Bind(hand,RememberHandOrder);
            boardNavigator = ComponentOf<BoardNavigator>("BoardViewport");
            boardArea = boardNavigator.followArea;
            clockLabel = ComponentOf<TMP_Text>("TurnTimerLabel");
            if (!Guided) OpeningView = ComponentOf<OpeningRollView>("OpeningRollOverlay");
            playButton = ComponentOf<Button>("PlayTileButton");
            drawButton = ComponentOf<Button>("DrawTileButton");
            swapButton = ComponentOf<Button>("PassTurnButton");
            playButton.onClick.AddListener(PlaceSelected);
            drawButton.onClick.AddListener(() => SubmitMove(new MoveCommand(MoveKind.Draw)));
            swapButton.onClick.AddListener(() => { swapEnabled = !swapEnabled; RenderActions(); });
            BindHud();
            BindArt();
            BindChoices();
            BindSwapTargets();
            SessionAppearance.Changed += RefreshLanguage;
            UserSettings.PlayAssistChanged += RefreshAssist;
        }

        protected virtual void OnDestroy()
        {
            SessionAppearance.Changed -= RefreshLanguage;
            UserSettings.PlayAssistChanged -= RefreshAssist;
        }
        void RefreshLanguage()
        {
            if (standingsShown) return;
            if (Display == null) return;
            RefreshTurnToggle();
            RefreshHandToggle();
            Render();
            displayedSecond = -1;
            if (OpeningBlocksActions) ShowWaitingClock();
            else RenderClock();
            OnLanguageChanged();
        }

        protected void Begin(MatchEngine engine)
        {
            Match = engine;
            Display = engine;
            lastVisibleSeat = -1;
            ResetSelection();
            handOrder.Clear();
            shownChoice = -1;
            ClearBoard(true);
            ResetClock();
            Render();
        }

        protected void PaintNetwork(ITableView view,bool fullFrame)
        {
            Display = view;
            if (!choosingTarget || !OwnTurn || Display.Phase != TurnPhase.Decision) ResetSelection();
            if (fullFrame) soundOnAppend = false;
            Render();
        }

        protected void NetworkClock(float remaining)
        {
            seconds = Mathf.Max(0,remaining);
            RenderClock();
        }

        protected void ResetClock()
        {
            seconds = Match.PhaseSeconds;
            botDelay = Mathf.Min(.65f,seconds * .45f);
            displayedSecond = -1;
            RenderClock();
        }

        protected void ShowWaitingClock()
        {
            displayedSecond = -1;
            WriteLabel(clockLabel,T("game.time.wait"));
        }

        void RenderClock()
        {
            int wholeSeconds = Mathf.CeilToInt(seconds);
            if (wholeSeconds == displayedSecond) return;
            displayedSecond = wholeSeconds;
            UpdateChoiceClock();
            WriteLabel(clockLabel,Guided ? T("game.guided") : T("game.time",wholeSeconds));
        }

        // Pause the local clock while a nested help or settings dialog is open.
        protected void Update()
        {
            if (Display != null) FlushMarks();
            if (Match == null || Guided || InteractionBlocked || OpeningBlocksActions || IsFinished) return;
            seconds = Mathf.Max(0,seconds - Time.unscaledDeltaTime);
            botDelay -= Time.unscaledDeltaTime;
            RenderClock();
            if (seconds <= 0)
            {
                Match.Expire();
                AfterMove(true);
            }
            else if (Match.ActiveSeat != 0 && botDelay <= 0) RunBot();
        }

        protected virtual void SubmitMove(MoveCommand a)
        {
            if (InteractionBlocked || IsFinished || ActionsBlocked || OpeningBlocksActions) return;
            if (!AllowMove(a)) { NotifyUser("game.trylesson"); return; }
            int actor = Match.ActiveSeat;
            var phase = Match.Phase;
            if (!Match.TryApply(actor,Match.Revision,a)) { NotifyUser("game.illegal"); return; }
            OnAccepted(a);
            bool sameSelection = (a.Kind == MoveKind.Choose || a.Kind == MoveKind.Mark) && Match.ActiveSeat == actor && Match.Phase == phase;
            AfterMove(!sameSelection);
        }

        void AfterMove(bool reset)
        {
            ResetSelection();
            if (reset) ResetClock();
            else botDelay = Mathf.Min(.28f,seconds * .3f);
            Render();
            if (IsFinished) Finish();
        }

        protected static string T(string key,params object[] args) => string.Format(LocalizedText.Read(key),args);
        protected virtual string DisplayName(int id) => id == 0 ? (UserSettings.DisplayName.Length > 0 ? UserSettings.DisplayName : T("player")) : "Bot " + id;

        // Literal text survives reactivation of a localized label.
        protected static void WriteLabel(TMP_Text label,string value)
        {
            label.richText = false;
            var language = label.GetComponent<LocalizedText>();
            if (language) language.Literal(value);
            else label.text = value;
        }

        protected void SetLabel(string name,string value,string panel = null) => WriteLabel(ComponentOf<TMP_Text>(name,panel),value);
    }
}
