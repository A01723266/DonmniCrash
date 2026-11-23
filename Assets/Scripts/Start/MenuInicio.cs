using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Menú de entrada y edición por borrador de la tarjeta de perfil.</summary>
    public sealed partial class MenuInicio : NavegacionUI
    {
        protected override string Inicial => "ModeSelectionScreen";
        string borrador;
        TMP_InputField campo;
        Image icono;
        protected override void Conectar()
        {
            Enlazar(Inicial,"OnlineButton", () => Ir("OnlineRooms"));
            Enlazar(Inicial,"OfflineButton", () => Ir("OfflineSetting"));
            Enlazar(Inicial,"CustomizationButton", () => Mostrar("CustomizationScreen"));
            Enlazar(Inicial,"SettingsButton", () => Mostrar("SettingsScreen"));
            Enlazar("Screens","BackButton", () => Mostrar(Inicial));
            Enlazar("SettingsScreen","SaveSettingsButton", () => Mostrar(Inicial));
            campo = Pieza<TMP_InputField>("PlayerNameInputField");
            icono = Pieza<Image>("ProfileEmblem");
            campo.onValueChanged.AddListener(_ => Previsualizar());
            foreach (var elegido in Preferencias.Emblemas)
            {
                Enlazar("CustomizationScreen", elegido + "EmblemButton", () => { borrador = elegido; Previsualizar(); });
            }
            Enlazar("CustomizationScreen","SaveProfileButton", () =>
            {
                Preferencias.GuardarPerfil(campo.text, borrador);
                Mostrar(Inicial);
            });
        }

        protected override void AlMostrar(string panel)
        {
            if (panel != "CustomizationScreen") return;
            campo.SetTextWithoutNotify(Preferencias.Nombre);
            borrador = Preferencias.Emblema;
            Previsualizar();
        }

        void Previsualizar()
        {
            var etiqueta = Pieza<TextoIdioma>("PlayerNameLabel","PlayerPreviewCard");
            if (string.IsNullOrWhiteSpace(campo.text)) etiqueta.Traducir("player");
            else etiqueta.Literal(campo.text);
            icono.sprite = Pieza<Image>("ElementIcon", borrador + "EmblemButton").sprite;
        }
    }
}
