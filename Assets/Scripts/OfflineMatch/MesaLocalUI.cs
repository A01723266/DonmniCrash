namespace DonmniCrash.UI
{
    /// <summary>Rutas de salida y muestra del indicador de bots de la mesa local.</summary>
    public sealed partial class MesaLocalUI : MesaNavegable
    {
        protected override void SalirMesa() => Ir("OfflineSetting","BotConfigurationScreen");
        protected override void Conectar()
        {
            base.Conectar();
            Opcion("Bot", () => {
                Mostrar(Inicial);
                var panel = Buscar("BotStatusPanel").gameObject;
                panel.SetActive(!panel.activeSelf);
            });
        }
    }
}
