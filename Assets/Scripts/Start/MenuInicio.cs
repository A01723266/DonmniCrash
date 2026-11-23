using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Menú de entrada y edición temporal de la tarjeta de perfil.</summary>
    public sealed partial class MenuInicio : NavegacionUI
    {
        protected override string Inicial => "ModeSelectionScreen";
        static string nombre = "";
        static int emblema;
        int borrador;
        TMP_InputField campo;
        Image icono;
        readonly string[] elementos = { "Earth", "Water", "Wind", "Fire", "Plant" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPerfil() { nombre = ""; emblema = 0; }

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
            for (int i = 0; i < elementos.Length; i++)
            {
                int elegido = i;
                Enlazar("CustomizationScreen", elementos[i] + "EmblemButton", () => { borrador = elegido; Previsualizar(); });
            }
            Enlazar("CustomizationScreen","SaveProfileButton", () =>
            {
                nombre = campo.text.Trim();
                emblema = borrador;
                Mostrar(Inicial);
            });
        }

        protected override void AlMostrar(string panel)
        {
            if (panel != "CustomizationScreen") return;
            campo.SetTextWithoutNotify(nombre);
            borrador = emblema;
            Previsualizar();
        }

        void Previsualizar()
        {
            var etiqueta = Pieza<TextoIdioma>("PlayerNameLabel","PlayerPreviewCard");
            if (string.IsNullOrWhiteSpace(campo.text)) etiqueta.Traducir("player");
            else etiqueta.Literal(campo.text);
            icono.sprite = Pieza<Image>("ElementIcon", elementos[borrador] + "EmblemButton").sprite;
        }
    }
}
