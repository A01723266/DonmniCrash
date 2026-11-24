using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Start menu and editable profile draft.</summary>
    public sealed partial class StartMenu : CanvasNavigation
    {
        protected override string InitialScreen => "ModeSelectionScreen";
        string draft;
        TMP_InputField inputField;
        Image icon;
        protected override void BindControls()
        {
            BindCredits();
            BindButton(InitialScreen,"OnlineButton", () => GoTo("OnlineRooms"));
            BindButton(InitialScreen,"OfflineButton", () => GoTo("OfflineSetting"));
            BindButton(InitialScreen,"CustomizationButton", () => Show("CustomizationScreen"));
            BindButton(InitialScreen,"SettingsButton", () => Show("SettingsScreen"));
            BindButton("Screens","BackButton", () => Show(InitialScreen));
            BindButton("SettingsScreen","SaveSettingsButton", () => Show(InitialScreen));
            inputField = ComponentOf<TMP_InputField>("PlayerNameInputField");
            icon = ComponentOf<Image>("ProfileEmblem");
            inputField.onValueChanged.AddListener(_ => PreviewProfile());
            foreach (var selectedId in UserSettings.Emblems)
            {
                BindButton("CustomizationScreen", selectedId + "EmblemButton", () => { draft = selectedId; PreviewProfile(); });
            }
            BindButton("CustomizationScreen","SaveProfileButton", () =>
            {
                UserSettings.SaveProfile(inputField.text, draft);
                Show(InitialScreen);
            });
        }

        protected override void OnScreenShown(string panel)
        {
            if (panel != "CustomizationScreen") return;
            inputField.SetTextWithoutNotify(UserSettings.DisplayName);
            draft = UserSettings.Emblem;
            PreviewProfile();
        }

        void PreviewProfile()
        {
            var label = ComponentOf<LocalizedText>("PlayerNameLabel","PlayerPreviewCard");
            if (string.IsNullOrWhiteSpace(inputField.text)) label.Localize("player");
            else label.Literal(inputField.text);
            icon.sprite = ComponentOf<Image>("ElementIcon", draft + "EmblemButton").sprite;
        }
    }
}
