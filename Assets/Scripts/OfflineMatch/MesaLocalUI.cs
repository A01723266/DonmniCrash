using System;
using System.Linq;
using System.Text;
using DonmniCrash.Juego;
using TMPro;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Inicia una ronda offline y ejecuta cada bot sobre su propia vista del turno.</summary>
    public sealed partial class MesaLocalUI : MesaJugable
    {
        BotDomino bot;
        protected override void SalirMesa() => Ir("OfflineSetting","BotConfigurationScreen");
        protected override void AlMostrar(string nombre)
        {
            if (Partida != null) return;
            int semilla = Guid.NewGuid().GetHashCode();
            bot = new BotDomino(~semilla);
            Iniciar(new MotorPartida(OpcionesPractica.Actual,semilla));
            MostrarDados();
        }

        protected override void ActuarBot() => Enviar(bot.Decidir(Partida.ConsultarTurno(),Partida.Config.Nivel));

        // Se muestran también los desempates, en el orden en que ocurrieron.
        void MostrarDados()
        {
            var texto = new StringBuilder();
            for (int i=0;i<Partida.Tiradas.Count;i++)
            {
                var fila = Partida.Tiradas[i];
                texto.AppendLine(T(i < Partida.InicioVecinos ? "game.roll" : "game.direction") + " · " +
                    string.Join(" / ",Enumerable.Range(0,fila.Length/2).Select(n=>Nombre(fila[n*2])+" "+fila[n*2+1])));
            }
            texto.Append(T("game.first",Nombre(Partida.Activo),Nombre(Partida.Vecino(Partida.Sentido))));
            var label = Pieza<TMP_Text>("DescriptionLabel","MessageDialog");
            Escribir(label,texto.ToString());
            label.GetComponent<LayoutElement>().preferredHeight = label.GetPreferredValues(label.text,400,0).y + 12;
            Etiquetar("TitleLabel",T("game.dice"),"MessageDialog");
            Abrir("MessageDialog");
        }

        protected override void Finalizar()
        {
            bool victoria = Partida.Vida(0) > 0;
            Etiquetar("WinnerLabel",T(victoria ? "game.win" : "game.loss"),"ResultsDialog");
            var fila = Buscar("ResultRow1",Buscar("ResultsDialog"));
            for (int i=0;i<6;i++)
            {
                string id = "ResultRow"+(i+1);
                var actual = Buscar(id,Buscar("ResultsDialog"));
                if (!actual && i < Partida.Orden.Count)
                {
                    actual = Instantiate(fila,fila.parent);
                    actual.name = id;
                    actual.SetSiblingIndex(fila.GetSiblingIndex()+i);
                }
                if (!actual) continue;
                actual.gameObject.SetActive(i < Partida.Orden.Count);
                if (!actual.gameObject.activeSelf) continue;
                // Al caer el humano todavía puede haber varios bots vivos. Mostrar vidas, sin inventar un podio.
                int actor = Partida.Orden[i];
                Escribir(Buscar("RankLabel",actual).GetComponent<TMP_Text>(),"");
                Escribir(Buscar("PlayerLabel",actual).GetComponent<TMP_Text>(),Nombre(actor));
                Escribir(Buscar("ScoreLabel",actual).GetComponent<TMP_Text>(),T("game.health",Partida.Vida(actor)));
            }
            Abrir("ResultsDialog");
        }
    }
}
