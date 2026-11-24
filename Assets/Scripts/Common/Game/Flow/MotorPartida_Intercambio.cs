using System;
using System.Collections.Generic;
using System.Linq;

namespace DonmniCrash.Juego
{
    public sealed partial class MotorPartida
    {
        int receptor, cantidadElegir;
        readonly List<Ficha> candidatas = new List<Ficha>();
        readonly HashSet<int> elecciones = new HashSet<int>();
        public bool SeleccionVisible => Etapa == Fase.SeleccionCambio && Maximo(Activo) > Maximo(receptor);
        public int Restantes => Etapa == Fase.CompletarMano ? Faltan : cantidadElegir - elecciones.Count;
        public int NumeroOpciones => Etapa == Fase.CompletarMano ? reserva.Cantidad : candidatas.Count;
        public bool Disponible(int indice) => indice >= 0 && indice < NumeroOpciones &&
            (Etapa == Fase.CompletarMano || !elecciones.Contains(indice));
        public Ficha? VerOpcion(int actor,int indice) =>
            actor == Activo && SeleccionVisible && Disponible(indice) ? candidatas[indice] : (Ficha?)null;

        // A conserva su exceso visible o elige su capacidad completa de la mano oculta de B.
        void PrepararCambio(int destino)
        {
            receptor = destino;
            candidatas.Clear(); elecciones.Clear();
            int diferencia = Maximo(Activo) - Maximo(receptor);
            Etapa = Fase.SeleccionCambio;
            cantidadElegir = diferencia >= 0 ? diferencia : Maximo(Activo);
            candidatas.AddRange(jugadores[diferencia >= 0 ? Activo : receptor].Mano);
            if (diferencia < 0)
                for (int i = candidatas.Count - 1; i > 0; i--)
                {
                    int j = azar.Next(i + 1);
                    (candidatas[i], candidatas[j]) = (candidatas[j], candidatas[i]);
                }
            if (cantidadElegir == 0) FinalizarCambio();
        }

        bool Elegir(int indice)
        {
            if (!Disponible(indice)) return false;
            if (Etapa == Fase.CompletarMano)
            {
                jugadores[Activo].Mano.Add(reserva.Tomar(indice));
                if (Faltan == 0) Etapa = Fase.Decision;
                return true;
            }
            elecciones.Add(indice);
            if (Restantes == 0) FinalizarCambio();
            return true;
        }

        void FinalizarCambio()
        {
            var a = jugadores[Activo].Mano;
            var b = jugadores[receptor].Mano;
            var toma = elecciones.Select(i => candidatas[i]).ToList();
            if (Maximo(Activo) >= Maximo(receptor))
            {
                var entrega = a.Except(toma).ToArray();
                a.Clear(); a.AddRange(toma); a.AddRange(b);
                b.Clear(); b.AddRange(entrega);
            }
            else
            {
                b.RemoveAll(f => toma.Contains(f));
                b.AddRange(a); a.Clear(); a.AddRange(toma);
            }
            candidatas.Clear(); elecciones.Clear();
            Avanzar();
        }

        /// <summary>La oferta conserva al vencer. Una selección parcial completa al azar solo lo pendiente.</summary>
        public void Vencer()
        {
            if (Etapa == Fase.Fin) return;
            if (Etapa == Fase.Decision) Robar();
            else if (Etapa == Fase.Oferta) Conservar();
            else
            {
                var fase = Etapa;
                int actor = Activo;
                while (Etapa == fase && Activo == actor)
                {
                    var indices = Enumerable.Range(0,NumeroOpciones).Where(Disponible).ToArray();
                    Elegir(indices[azar.Next(indices.Length)]);
                }
            }
            Revision++;
        }
    }
}
