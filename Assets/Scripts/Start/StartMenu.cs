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
        bool enteringOnline;
        TMP_InputField inputField;
        Image icon;
        protected override void BindControls()
        {
            BindCredits();
            BindButton(InitialScreen,"OnlineButton", EnterOnline);
            BindButton(InitialScreen,"OfflineButton", () => GoTo("OfflineSetting"));
            BindButton(InitialScreen,"CustomizationButton", () => Show("CustomizationScreen"));
            BindButton(InitialScreen,"SettingsButton", () => Show("SettingsScreen"));
            BindButton("Screens","BackButton", () => Show(InitialScreen));
            BindButton("SettingsScreen","SaveSettingsButton", () => Show(InitialScreen));
            inputField = ComponentOf<TMP_InputField>("PlayerNameInputField");
            inputField.characterLimit = 28;
            BindButton("MessageDialog","AcceptButton", () =>
            {
                if (enteringOnline && ScreenName == "CustomizationScreen")
                    inputField.ActivateInputField();
            });
            BindButton("CustomizationScreen","ChoosePhotoButton",ChoosePhoto);
#if !UNITY_WEBGL && !UNITY_EDITOR
            FindNode("ChoosePhotoButton").gameObject.SetActive(false);
#endif
            icon = ComponentOf<Image>("ProfileEmblem");
            inputField.onValueChanged.AddListener(_ => PreviewProfile());
            foreach (var selectedId in UserSettings.Emblems)
            {
                BindButton("CustomizationScreen", selectedId + "EmblemButton", () => { ++photoRequest; SetPhoto(null,null); draft = selectedId; PreviewProfile(); });
            }
            BindButton("CustomizationScreen","SaveProfileButton", () =>
            {
                if (enteringOnline && string.IsNullOrWhiteSpace(inputField.text)) return;
                ProfilePhoto.Store(draftPhoto);
                UserSettings.SaveProfile(inputField.text, draft);
                if (enteringOnline) GoTo("OnlineRooms");
                else Show(InitialScreen);
            });
        }

        void EnterOnline()
        {
            if (UserSettings.DisplayName.Length > 0) { GoTo("OnlineRooms"); return; }
            enteringOnline = true;
            Show("CustomizationScreen");
            ComponentOf<Button>("AcceptButton","MessageDialog").GetComponentInChildren<LocalizedText>(true).Literal("OK");
            NotifyUser("profile.online");
        }

        protected override void OnScreenShown(string panel)
        {
            ++photoRequest;
            if (panel != "CustomizationScreen") { enteringOnline = false; return; }
            ComponentOf<LocalizedText>("SubtitleLabel",panel).Localize("profile.hint");
            var bytes = ProfilePhoto.Saved;
            var portrait = ProfilePhoto.Decode(bytes);
            SetPhoto(portrait ? bytes : null,portrait);
            inputField.SetTextWithoutNotify(UserSettings.DisplayName);
            draft = UserSettings.Emblem;
            PreviewProfile();
        }

        void PreviewProfile()
        {
            var label = ComponentOf<LocalizedText>("PlayerNameLabel","PlayerPreviewCard");
            if (string.IsNullOrWhiteSpace(inputField.text)) label.Localize("player");
            else label.Literal(inputField.text);
            ComponentOf<Button>("SaveProfileButton","CustomizationScreen").interactable = !enteringOnline || !string.IsNullOrWhiteSpace(inputField.text);
            icon.sprite = draftPortrait ? draftPortrait : ComponentOf<Image>("ElementIcon", draft + "EmblemButton").sprite;
        }
    }
}
