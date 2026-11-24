using System.Collections.Generic;
using System.Linq;

namespace DonmniCrash.Juego
{
    /// <summary>Información entregada a una estrategia: mesa pública, mano propia y comandos legales.</summary>
    public sealed partial class VistaJugador
    {
        public Ficha[] Mano, Tablero;
        public Ficha? Oferta;
        public Elemento? Ataque;
        public int Actor, Siguiente, Anterior;
        public int[] Vidas, Cantidades;
        public Accion[] Opciones;
        public Ficha?[] Seleccion;
    }
}
