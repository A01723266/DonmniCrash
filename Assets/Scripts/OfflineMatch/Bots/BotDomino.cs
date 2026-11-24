using System;
using System.Linq;
using DonmniCrash.Juego;

namespace DonmniCrash.UI
{
    /// <summary>El bot recibe la misma información de cartas que el participante cuyo asiento ocupa.</summary>
    public sealed partial class BotDomino
    {
        readonly Random azar;
        public BotDomino(int semilla) { azar = new Random(semilla); }
        public Accion Decidir(VistaJugador v, NivelBot nivel)
        {
            if (nivel == NivelBot.Facil) return v.Opciones[azar.Next(v.Opciones.Length)];
            // El ruido rompe empates entre dorsos indistinguibles y jugadas de igual valor.
            return v.Opciones.OrderByDescending(a => Puntuar(v,a,nivel) + azar.NextDouble() * .8).First();
        }
        double Puntuar(VistaJugador v, Accion a, NivelBot nivel)
        {
            if (a.Tipo == Movimiento.Robar || a.Tipo == Movimiento.Conservar) return -10;
            if (a.Tipo == Movimiento.Elegir)
                return nivel == NivelBot.Dificil && v.Seleccion[a.Valor]?.Tipo == TipoFicha.Intercambio ? 1 : 0;
            if (nivel == NivelBot.Normal) return a.Objetivo < 0 ? 2 : 1;
            var f = v.Oferta ?? v.Mano.First(x => x.Id == a.Valor);
            int rival = f.Tipo == TipoFicha.Reversa ? v.Anterior : v.Siguiente;
            double puntos = 4.0 / Math.Max(1,v.Vidas[rival]);
            if (f.Tipo == TipoFicha.Ordinaria)
                puntos += v.Tablero.Count(t => t.Tipo == TipoFicha.Ordinaria && ReglasJugada.Vence(t.Escudo,f.Espada)) * .05;
            if (a.Objetivo >= 0) puntos += (v.Cantidades[a.Objetivo] - v.Mano.Length) * .4;
            return puntos;
        }
    }
}
