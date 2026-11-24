using System;

namespace DonmniCrash.Juego
{
    public enum NivelBot { Facil, Normal, Dificil }
    public enum Elemento { Tierra, Agua, Viento, Fuego, Planta }
    public enum TipoFicha { Ordinaria, Reversa, Pasar, Intercambio }
    public enum Fase { Decision, Oferta, SeleccionCambio, CompletarMano, Fin }
    public enum Movimiento { Colocar, Robar, Conservar, Elegir }

    /// <summary>Parámetros inmutables de una ronda. La dificultad pertenece al modo local.</summary>
    public sealed partial class ConfiguracionPartida
    {
        public readonly int Jugadores, Mano, Vida, Segundos, Reversas, Pases, Cambios;
        public readonly NivelBot Nivel;
        public ConfiguracionPartida(int jugadores = 4, int mano = 5, int vida = 3, int segundos = 5,
            int reversas = 1, int pases = 1, int cambios = 1, NivelBot nivel = NivelBot.Normal)
        {
            if (jugadores < 2 || jugadores > 6 || mano < 2 || mano > 8 || vida < 1 || vida > 10 ||
                segundos < 1 || segundos > 10 || reversas < 0 || pases < 0 || cambios < 0 ||
                (long)reversas + pases + cambios > 8 || !Enum.IsDefined(typeof(NivelBot), nivel))
                throw new ArgumentOutOfRangeException(nameof(jugadores), "Configuración de mesa inválida.");
            Jugadores = jugadores; Mano = mano; Vida = vida; Segundos = segundos;
            Reversas = reversas; Pases = pases; Cambios = cambios; Nivel = nivel;
        }
    }

    /// <summary>La identidad distingue copias de la misma combinación en sets diferentes.</summary>
    public readonly struct Ficha
    {
        public readonly int Id;
        public readonly TipoFicha Tipo;
        public readonly Elemento Espada, Escudo;
        public Ficha(int id, TipoFicha tipo, Elemento espada = default, Elemento escudo = default)
        { Id = id; Tipo = tipo; Espada = espada; Escudo = escudo; }
    }

    /// <summary>Solicitud del jugador, bot o tutorial. El objetivo opcional declara el combo antes de colocarlo.</summary>
    public readonly struct Accion
    {
        public readonly Movimiento Tipo;
        public readonly int Valor, Objetivo;
        public Accion(Movimiento tipo, int valor = -1, int objetivo = -1)
        { Tipo = tipo; Valor = valor; Objetivo = objetivo; }
    }
}
