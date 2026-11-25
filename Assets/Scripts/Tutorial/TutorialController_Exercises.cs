namespace DonmniCrash.UI
{
    public sealed partial class TutorialController
    {
        bool freeView, zoomedIn, zoomedOut;

        protected override void OnDestroy()
        {
            if (boardNavigator) boardNavigator.Navigated -= OnNavigation;
            base.OnDestroy();
        }

        protected override void OnHandReordered()
        {
            if (stepIndex != TutorialSteps.OrderHand || complete) return;
            complete = true;
            RenderGuide();
        }

        void OnNavigation(BoardNavigator.Gesture gesture)
        {
            if (stepIndex != TutorialSteps.Explore || complete || InteractionBlocked) return;
            switch (gesture)
            {
                case BoardNavigator.Gesture.FreeView: freeView = true; break;
                case BoardNavigator.Gesture.ZoomIn: zoomedIn |= freeView; break;
                case BoardNavigator.Gesture.ZoomOut: zoomedOut |= freeView; break;
                case BoardNavigator.Gesture.Follow:
                    complete = freeView && zoomedIn && zoomedOut;
                    freeView = false;
                    break;
            }
            RenderGuide();
        }

        string InstructionKey()
        {
            if (complete) return "lesson.done";
            if (stepIndex != TutorialSteps.Explore || !freeView) return "lesson.body."+stepIndex;
            if (!zoomedIn && !zoomedOut) return "lesson.zoom.both";
            if (!zoomedIn) return "lesson.zoom.in";
            return zoomedOut ? "lesson.follow" : "lesson.zoom.out";
        }
    }
}
