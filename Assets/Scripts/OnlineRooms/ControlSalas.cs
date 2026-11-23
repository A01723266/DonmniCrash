using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace DonmniCrash.UI
{
    /// <summary>Recorrido visual de salas. Sus eventos de presentación se pueden invocar al recibir datos reales.</summary>
    public sealed partial class ControlSalas : NavegacionUI
    {
        const string Crear = "CreateRoomScreen", Privada = "JoinPrivateRoomScreen", Pendiente = "AdmissionPendingScreen", Lobby = "LobbyScreen";
        protected override string Inicial => "RoomBrowserScreen";
        bool host = true, editando, listo;
        string titulo;
        string estadoLista = "";
        TMP_InputField filtro;

        protected override void Conectar()
        {
            filtro = Pieza<TMP_InputField>("RoomSearchInputField");
            filtro.onValueChanged.AddListener(_ => Lista(""));
            Enlazar(Inicial,"RefreshRoomsButton", () => Lista(""));
            Enlazar(Inicial,"BackButton", () => Ir("Start"));
            Enlazar(Inicial,"CreateRoomButton", () => { editando = false; Mostrar(Crear); });
            Enlazar(Inicial,"JoinPrivateRoomButton", () => Mostrar(Privada));
            foreach (var boton in Buscar("RoomListContentPanel").GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var fila = boton.transform.parent;
                boton.onClick.AddListener(() => {
                    titulo = Buscar("RoomNameLabel", fila).GetComponent<TMP_Text>().text;
                    EntrarLobby(false);
                });
            }
            Enlazar(Crear,"BackButton", () => Mostrar(editando ? Lobby : Inicial));
            Enlazar(Crear,"CreateRoomButton", () => {
                titulo = Pieza<TMP_InputField>("RoomNameInputField").text.Trim();
                EntrarLobby(true);
            });
            Enlazar(Privada,"BackButton", () => Mostrar(Inicial));
            Enlazar(Privada,"RequestAdmissionButton", () => Mostrar(Pendiente));
            Enlazar(Pendiente,"CancelAdmissionButton", () => Mostrar(Privada));
            Enlazar(Pendiente,"BackButton", () => Mostrar(Privada));
            Enlazar(Lobby,"EditRoomButton", () => { editando = true; Mostrar(Crear); });
            Enlazar(Lobby,"ReadyButton", () => { listo = !listo; ActualizarLobby(); });
            Enlazar(Lobby,"StartMatchButton", () => Ir("OnlineMatch"));
            Enlazar(Lobby,"BackButton", () => Abrir("LeaveRoomDialog"));
            Enlazar(Lobby,"LeaveRoomButton", () => Abrir("LeaveRoomDialog"));
            Enlazar("LeaveRoomDialog","StayButton",Cerrar);
            Enlazar("LeaveRoomDialog","LeaveButton", () => Mostrar(Inicial));
            Enlazar("AdmissionRequestDialog","AcceptButton", () => { Cerrar(); Avisar("preview.accept"); });
            Enlazar("AdmissionRequestDialog","RejectButton", () => { Cerrar(); Avisar("preview.reject"); });
            Opcion("Host", () => EntrarLobby(true));
            Opcion("Guest", () => EntrarLobby(false));
            Opcion("Request", () => { EntrarLobby(true); Abrir("AdmissionRequestDialog"); });
            foreach (var estado in new[] {"EmptyRoomsPanel","LoadingRoomsPanel","RoomListErrorPanel"})
                Opcion(estado, () => { Mostrar(Inicial); Lista(estado); });
            Opcion("Match", () => Ir("OnlineMatch"));
        }

        void EntrarLobby(bool comoHost) { host = comoHost; listo = false; Mostrar(Lobby); }

        protected override void AlMostrar(string panel)
        {
            if (panel == Inicial) Lista("");
            if (panel == Lobby) ActualizarLobby();
        }

        void ActualizarLobby()
        {
            Buscar("HostControls").gameObject.SetActive(host);
            Buscar("ClientControls").gameObject.SetActive(!host);
            var label = Pieza<TextoIdioma>("TitleLabel","RoomSummaryPanel");
            if (string.IsNullOrEmpty(titulo)) label.Traducir("sample.table"); else label.Literal(titulo);
            Pieza<TextoIdioma>("TextLabel","ReadyButton").Traducir(listo ? "unready" : "ready.button");
            Pieza<TextoIdioma>("ReadinessLabel",Lobby).Traducir(listo ? "preview.ready" : "preview.lobby");
            if (host && !string.IsNullOrEmpty(titulo))
                Pieza<TextoIdioma>("ConfigurationLabel",Lobby).Traducir("room.capacity", Pieza<OpcionesMuestra>("PlayerCountSelector",Crear).Valor);
        }

        // El buscador opera solo sobre las filas de ejemplo de la escena.
        void Lista(string estado)
        {
            bool alguna = false;
            foreach (Transform fila in Buscar("RoomListContentPanel"))
            {
                if (!fila.name.StartsWith("RoomRow", StringComparison.Ordinal)) continue;
                var nombre = Buscar("RoomNameLabel",fila).GetComponent<TMP_Text>().text;
                bool coincide = nombre.IndexOf(filtro.text, StringComparison.OrdinalIgnoreCase) >= 0;
                fila.gameObject.SetActive(coincide);
                alguna |= coincide;
            }
            estadoLista = estado == "" && !alguna ? "EmptyRoomsPanel" : estado;
            Buscar("RoomListContentPanel").gameObject.SetActive(estadoLista == "");
            foreach (var n in new[] {"EmptyRoomsPanel","LoadingRoomsPanel","RoomListErrorPanel"})
                Buscar(n).gameObject.SetActive(n == estadoLista);
        }
    }
}
