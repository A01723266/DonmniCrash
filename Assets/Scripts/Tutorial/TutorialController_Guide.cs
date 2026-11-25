using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    public sealed partial class TutorialController
    {
        GameObject guideBody;
        TMP_Text guideToggleLabel;
        RectTransform tutorialBoard;
        float guideBoardRightAnchor, guideBoardRightInset;
        bool guideHidden;

        void BindGuide()
        {
            guideBody = FindNode("TutorialGuideBody").gameObject;
            tutorialBoard = ComponentOf<RectTransform>("HistoryScroll");
            guideBoardRightAnchor = tutorialBoard.anchorMax.x;
            guideBoardRightInset = tutorialBoard.offsetMax.x;
            var toggle = ComponentOf<Button>("ToggleTutorialGuideButton");
            guideToggleLabel = toggle.GetComponentInChildren<TMP_Text>(true);
            toggle.onClick.AddListener(ToggleGuide);
            RefreshGuideToggle();
        }

        void ToggleGuide()
        {
            guideHidden = !guideHidden;
            guideBody.SetActive(!guideHidden);
            tutorialBoard.anchorMax = new Vector2(guideHidden ? 1 : guideBoardRightAnchor,tutorialBoard.anchorMax.y);
            tutorialBoard.offsetMax = new Vector2(guideBoardRightInset,tutorialBoard.offsetMax.y);
            RefreshGuideToggle();
        }

        void RefreshGuideToggle() => WriteLabel(guideToggleLabel,
            T(guideHidden ? "lesson.guide.show" : "lesson.guide.hide"));
    }
}
