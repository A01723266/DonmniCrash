using System;
using System.Collections.Generic;

namespace DonmniCrash.Juego
{
    /// <summary>Montón oculto con muestreo sin reemplazo. Sus índices se usan para elegir dorsos.</summary>
    internal sealed partial class Reserva
    {
        readonly List<Ficha> fichas = new List<Ficha>();
        readonly ConfiguracionPartida reglas;
        readonly Random azar;
        int siguienteId = 1;
        public int Cantidad => fichas.Count;

        public Reserva(ConfiguracionPartida c, Random azar)
        {
            reglas = c; this.azar = azar;
            int porSet = 25 + c.Reversas + c.Pases + c.Cambios;
            int sets = (c.Jugadores * c.Mano + porSet - 1) / porSet + 1;
            for (int i = 0; i < sets; i++) NuevoSet();
            Mezclar();
        }

        void NuevoSet()
        {
            foreach (Elemento espada in Enum.GetValues(typeof(Elemento)))
                foreach (Elemento escudo in Enum.GetValues(typeof(Elemento)))
                    fichas.Add(new Ficha(siguienteId++, TipoFicha.Ordinaria, espada, escudo));
            Agregar(TipoFicha.Reversa, reglas.Reversas);
            Agregar(TipoFicha.Pasar, reglas.Pases);
            Agregar(TipoFicha.Intercambio, reglas.Cambios);
        }
        void Agregar(TipoFicha tipo, int cantidad)
        {
            for (int i = 0; i < cantidad; i++) fichas.Add(new Ficha(siguienteId++, tipo));
        }
        void Mezclar()
        {
            for (int i = fichas.Count - 1; i > 0; i--)
            {
                int j = azar.Next(i + 1);
                (fichas[i], fichas[j]) = (fichas[j], fichas[i]);
            }
        }
        public Ficha Tomar(int indice = -1)
        {
            if (indice < 0) indice = fichas.Count - 1;
            Ficha ficha = fichas[indice];
            fichas.RemoveAt(indice);
            if (fichas.Count == 0) { NuevoSet(); Mezclar(); }
            return ficha;
        }
        public void Devolver(List<Ficha> mano) { fichas.AddRange(mano); mano.Clear(); Mezclar(); }

        // Un escenario didáctico puede fijar el próximo robo y seguir usando el mismo motor.
        internal void PrepararRobo(Ficha ficha) => fichas.Add(ficha);
    }
}
