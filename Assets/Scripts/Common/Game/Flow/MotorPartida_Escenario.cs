using System.Linq;

namespace DonmniCrash.Juego
{
    public sealed partial class MotorPartida
    {
        /// <summary>El tutorial puede fijar una posición inicial. Todas las decisiones posteriores usan Ejecutar.</summary>
        internal static MotorPartida DesdePosicion(Ficha[][] manos,int[] capacidades,Elemento? ataque,Ficha siguiente)
        {
            var motor = new MotorPartida(new ConfiguracionPartida(jugadores:manos.Length),1234);
            motor.orden.Clear();
            motor.orden.AddRange(Enumerable.Range(0,manos.Length));
            motor.Activo = 0;
            motor.Sentido = 1;
            motor.Ataque = ataque;
            if (ataque.HasValue) motor.mesa.Add(new Ficha(-10,TipoFicha.Ordinaria,ataque.Value,Elemento.Planta));
            for (int i=0;i<manos.Length;i++)
            {
                motor.jugadores[i].Mano.Clear();
                motor.jugadores[i].Mano.AddRange(manos[i]);
                motor.jugadores[i].Maximo = capacidades[i];
            }
            motor.reserva.PrepararRobo(siguiente);
            motor.Etapa = motor.Faltan > 0 ? Fase.CompletarMano : Fase.Decision;
            return motor;
        }
    }
}
