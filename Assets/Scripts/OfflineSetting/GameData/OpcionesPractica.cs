using UnityEngine;
using DonmniCrash.Juego;

namespace DonmniCrash.UI
{
    /// <summary>Transporta solo las reglas elegidas entre el formulario, la mesa y la revancha.</summary>
    public static partial class OpcionesPractica
    {
        public static ConfiguracionPartida Actual { get; private set; } = new ConfiguracionPartida();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() => Actual = new ConfiguracionPartida();
        public static void Confirmar(ConfiguracionPartida c) => Actual = c;
    }
}
