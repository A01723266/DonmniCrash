namespace DonmniCrash.UI
{
    /// <summary>Permite recorrer los estados visuales del anfitrión, jugador y espectador.</summary>
    public sealed partial class MesaOnlineUI : MesaNavegable
    {
        protected override void SalirMesa() => Ir("OnlineRooms","LobbyScreen");
        protected override void Repetir() => SalirMesa();
        protected override void RegresarResultado() => Ir("Start");
        protected override void Conectar()
        {
            base.Conectar();
            foreach (var dialogo in new[] {"ConnectionLostDialog","HostDisconnectedDialog"})
            {
                Opcion(dialogo, () => { Mostrar(Inicial); Abrir(dialogo); });
                Enlazar(dialogo,"ReturnToRoomsButton", () => Ir("OnlineRooms"));
            }
            Opcion("Banner", () => {
                Mostrar(Inicial);
                var aviso = Buscar("ConnectionBanner").gameObject;
                aviso.SetActive(!aviso.activeSelf);
            });
            Opcion("Spectator", () => {
                Mostrar(Inicial);
                var panel = Buscar("SpectatorPanel").gameObject;
                panel.SetActive(!panel.activeSelf);
                Buscar("HandHUDPanel").gameObject.SetActive(!panel.activeSelf);
            });
        }
    }
}
