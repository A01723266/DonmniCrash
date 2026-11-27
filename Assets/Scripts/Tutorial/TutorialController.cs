using DonmniCrash.Game;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Guide players through replayable positions using the real table.</summary>
    public sealed partial class TutorialController : PlayableTableUI
    {
        int stepIndex;
        bool complete;
        bool elementsOpened;
        bool NeedsElementGuide => stepIndex == 0 && !elementsOpened;
        protected override bool Guided => true;
        protected override bool ActionsBlocked => NeedsElementGuide || complete || stepIndex >= TutorialSteps.OrderHand;
        protected override string OfferHintKey => stepIndex == 2 ? "lesson.offer.play" :
            stepIndex == 3 ? "lesson.offer.keep" : base.OfferHintKey;
        protected override void LeaveTable() => GoTo("OfflineSetting");
        protected override void BindControls()
        {
            base.BindControls();
            BindGuide();
            BindButton(InitialScreen,"ElementHelpButton",OnElementGuideOpened);
            BindButton("PauseDialog","ElementHelpButton",OnElementGuideOpened);
            boardNavigator.Navigated += OnNavigation;
            BindButton("TutorialWelcomeDialog","BeginTutorialButton",Close);
            BindButton("TutorialWelcomeDialog","SkipTutorialButton",LeaveTable);
            BindButton(InitialScreen,"SkipTutorialButton", () => Open("ExitTutorialDialog"));
            BindButton(InitialScreen,"PreviousStepButton", () => Step(stepIndex - 1));
            BindButton(InitialScreen,"RetryStepButton", () => Step(stepIndex));
            BindButton(InitialScreen,"NextStepButton", () => {
                if (!complete) return;
                if (stepIndex+1 == TutorialSteps.Total) Open("TutorialCompleteDialog");
                else Step(stepIndex+1);
            });
            BindButton("ExitTutorialDialog","ContinueButton",Close);
            BindButton("ExitTutorialDialog","ExitButton",LeaveTable);
            BindButton("TutorialCompleteDialog","PracticeButton", () => GoTo("OfflineSetting","BotConfigurationScreen"));
            BindButton("TutorialCompleteDialog","RepeatTutorialButton", () => { Close(); Step(0); });
            BindButton("TutorialCompleteDialog","BackButton",LeaveTable);
        }
        protected override void OnScreenShown(string name)
        {
            if (Match != null) return;
            Step(0);
            Open("TutorialWelcomeDialog");
        }
        protected override bool AllowMove(MoveCommand a) => TutorialSteps.Allows(stepIndex,Match,a);
        protected override void OnAccepted(MoveCommand a)
        {
            complete = Match.ActiveSeat != 0 || a.Kind == MoveKind.Keep ||
                (stepIndex == 8 && Match.Phase == TurnPhase.Decision);
            RenderGuide();
        }
        void Step(int index)
        {
            stepIndex = index;
            elementsOpened = false;
            freeView = zoomedIn = zoomedOut = false;
            complete = false;
            Begin(TutorialSteps.Prepare(stepIndex));
            RenderGuide();
        }
        void OnElementGuideOpened()
        {
            if (!NeedsElementGuide) return;
            elementsOpened = true;
            RenderActions();
            RenderGuide();
        }
        protected override void OnLanguageChanged()
        {
            RefreshGuideToggle();
            RenderGuide();
        }
        void RenderGuide()
        {
            SetLabel("StepTitleLabel",(stepIndex+1)+" / "+TutorialSteps.Total+" · "+T(NeedsElementGuide ? "lesson.elements.title" : "lesson.title."+stepIndex));
            SetLabel("StepDescriptionLabel",T(InstructionKey()));
            ComponentOf<Button>("PreviousStepButton").interactable = stepIndex > 0;
            ComponentOf<Button>("NextStepButton").interactable = complete;
            for (int i=0;i<TutorialSteps.Total;i++)
            {
                var marker = ComponentOf<UIThemeBinding>("Step"+(i+1)+"Indicator");
                marker.role = i == stepIndex ? UIThemeRole.Accent : UIThemeRole.Border;
                marker.Apply();
            }
        }
    }
}
