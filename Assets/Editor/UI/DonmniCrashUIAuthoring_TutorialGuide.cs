using DonmniCrash.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DonmniCrash.EditorTools
{
    public static partial class DonmniCrashUIAuthoring
    {
        static void PolishTutorialGuide(RectTransform screen)
        {
            RectTransform Find(string name) => ChildComponent<RectTransform>(screen.gameObject,name);
            var slot = Find("TutorialStepPanel");
            if (slot.Find("TutorialGuideBody")) return;

            var title = Find("StepTitleLabel");
            var description = Find("StepDescriptionLabel");
            var progress = Find("TutorialProgressPanel");
            var controls = Find("TutorialControlsPanel");
            HidePanelBackground(slot);
            Object.DestroyImmediate(slot.GetComponent<VerticalLayoutGroup>());
            slot.offsetMin = new Vector2(slot.offsetMin.x,236);

            var body = Panel("TutorialGuideBody",slot);
            body.raycastTarget = false;
            Stretch(body.rectTransform);
            var content = Scroll("TutorialGuideScroll",body.transform);
            Stretch((RectTransform)content.parent.parent,14,12,14,48);
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 7;

            title.SetParent(content,false);
            var titleText = title.GetComponent<TMP_Text>();
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.fontSizeMin = 16;
            titleText.fontSizeMax = 25;
            titleText.enableAutoSizing = true;
            Size(title,34);

            description.SetParent(content,false);
            var explanation = description.GetComponent<TMP_Text>();
            explanation.overflowMode = TextOverflowModes.Overflow;
            var descriptionSize = description.GetComponent<LayoutElement>();
            // El texto localizado puede ocupar más líneas según el ancho del panel.
            descriptionSize.minHeight = 26;
            descriptionSize.preferredHeight = -1;

            progress.SetParent(content,false);
            Size(progress,12);
            var dots = progress.GetComponent<HorizontalLayoutGroup>();
            dots.childAlignment = TextAnchor.MiddleLeft;
            dots.childForceExpandHeight = false;
            dots.spacing = 6;
            foreach (Transform dot in progress)
            {
                Size(dot,8,8);
                dot.GetComponent<Image>().type = Image.Type.Simple;
            }

            controls.SetParent(content,false);
            HidePanelBackground(controls);
            Object.DestroyImmediate(controls.GetComponent<HorizontalLayoutGroup>());
            var grid = controls.gameObject.AddComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(7,7);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            var responsive = controls.gameObject.AddComponent<UIResponsiveGrid>();
            responsive.minimumCellWidth = 94;
            responsive.maximumColumns = 4;
            responsive.cellHeight = 44;
            // VerticalLayoutGroup reads the GridLayoutGroup's preferred height.
            foreach (var button in controls.GetComponentsInChildren<Button>(true)) FitButtonText(button.transform);

            var toggle = Button("ToggleTutorialGuideButton",slot,"Hide instructions");
            Place((RectTransform)toggle.transform,Vector2.one,Vector2.one,new Vector2(-228,-42),new Vector2(-8,-5));
            var toggleLabel = toggle.GetComponentInChildren<TMP_Text>(true);
            Localize(toggleLabel,"lesson.guide.hide");
            FitButtonText(toggle.transform);

            var board = Find("HistoryScroll");
            board.offsetMin = new Vector2(board.offsetMin.x,236);
        }
    }
}
