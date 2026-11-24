using System;
using System.Collections.Generic;
using System.Linq;

namespace DonmniCrash.Juego
{
    /// <summary>Autoridad local de turnos. Cada solicitud se valida antes de cambiar el estado.</summary>
    public sealed partial class MotorPartida
    {
        sealed class Asiento
        {
            public int Vida, Maximo;
            public readonly List<Ficha> Mano = new List<Ficha>();
        }
        readonly Random azar;
        readonly Reserva reserva;
        readonly Asiento[] jugadores;
        readonly List<int> orden;
        readonly List<Ficha> mesa = new List<Ficha>();
        readonly List<int[]> tiradas = new List<int[]>();
        public readonly ConfiguracionPartida Config;
        public int Activo { get; private set; }
        public int Sentido { get; private set; } = 1;
        public Elemento? Ataque { get; private set; }
        public Fase Etapa { get; private set; }
        public int Revision { get; private set; }
        public int CantidadReserva => reserva.Cantidad;
        public int Faltan => jugadores[Activo].Maximo - jugadores[Activo].Mano.Count;
        public IReadOnlyList<Ficha> Mesa => mesa.AsReadOnly();
        public IReadOnlyList<int> Orden => orden.AsReadOnly();
        // Cada fila contiene pares actor/dado. Las últimas tiradas de vecinos se identifican por este corte.
        public IReadOnlyList<int[]> Tiradas => tiradas.AsReadOnly();
        public int InicioVecinos { get; private set; }
        public int Vida(int actor) => jugadores[actor].Vida;
        public int Maximo(int actor) => jugadores[actor].Maximo;
        public int Cantidad(int actor) => jugadores[actor].Mano.Count;
        public Ficha[] ManoDe(int actor) => jugadores[actor].Mano.ToArray();
        Ficha? oferta;
        public Ficha? OfertaDe(int actor) => actor == Activo ? oferta : null;

        public MotorPartida(ConfiguracionPartida config, int semilla)
        {
            Config = config; azar = new Random(semilla);
            reserva = new Reserva(config, azar);
            jugadores = new Asiento[config.Jugadores];
            orden = Enumerable.Range(0, config.Jugadores).ToList();
            for (int i = orden.Count - 1; i > 0; i--)
            {
                int j = azar.Next(i + 1);
                (orden[i], orden[j]) = (orden[j], orden[i]);
            }
            foreach (int id in orden)
            {
                jugadores[id] = new Asiento { Vida = config.Vida, Maximo = config.Mano };
                for (int i = 0; i < config.Mano; i++) jugadores[id].Mano.Add(reserva.Tomar());
            }
            Activo = Desempatar(orden);
            InicioVecinos = tiradas.Count;
            if (orden.Count > 2)
            {
                int derecha = Vecino(1), izquierda = Vecino(-1);
                Sentido = Desempatar(new[]{ derecha, izquierda }) == derecha ? -1 : 1;
            }
        }

        int Desempatar(IEnumerable<int> candidatos)
        {
            var ids = candidatos.ToList();
            while (ids.Count > 1)
            {
                int[] dados = ids.Select(_ => azar.Next(1,7)).ToArray();
                tiradas.Add(ids.SelectMany((id,i) => new[]{ id, dados[i] }).ToArray());
                int mayor = dados.Max();
                ids = ids.Where((id,i) => dados[i] == mayor).ToList();
            }
            return ids[0];
        }

        public int Vecino(int sentido)
        {
            int i = orden.IndexOf(Activo);
            do { i = (i + sentido + orden.Count) % orden.Count; } while (Vida(orden[i]) == 0);
            return orden[i];
        }

        public bool ObjetivoValido(int id) => id >= 0 && id < jugadores.Length && id != Activo &&
            Vida(id) > 0 && Cantidad(id) == Maximo(id);

        /// <summary>Devuelve false si el comando ya caducó, pertenece a otro actor o viola su fase.</summary>
        public bool Ejecutar(int actor, int revision, Accion a)
        {
            if (actor != Activo || revision != Revision || Etapa == Fase.Fin) return false;
            bool aceptada;
            switch (Etapa)
            {
                case Fase.Decision:
                    if (a.Tipo == Movimiento.Robar) { Robar(); aceptada = true; }
                    else aceptada = a.Tipo == Movimiento.Colocar && Colocar(a);
                    break;
                case Fase.Oferta:
                    if (a.Tipo == Movimiento.Conservar) { Conservar(); aceptada = true; }
                    else aceptada = a.Tipo == Movimiento.Colocar && Colocar(a);
                    break;
                default:
                    aceptada = a.Tipo == Movimiento.Elegir && Elegir(a.Valor);
                    break;
            }
            if (aceptada) Revision++;
            return aceptada;
        }

        void Avanzar()
        {
            Activo = Vecino(Sentido);
            Etapa = Faltan > 0 ? Fase.CompletarMano : Fase.Decision;
        }

        void Robar()
        {
            oferta = reserva.Tomar();
            if (ReglasJugada.Jugable(oferta.Value, Ataque)) Etapa = Fase.Oferta;
            else Conservar();
        }

        void Conservar()
        {
            var jugador = jugadores[Activo];
            jugador.Mano.Add(oferta.Value);
            oferta = null;
            jugador.Maximo++;
            Etapa = Fase.Decision;
            if (--jugador.Vida > 0) return;
            reserva.Devolver(jugador.Mano);
            if (jugadores.Count(j => j.Vida > 0) == 1) Etapa = Fase.Fin;
            else Avanzar();
        }

        bool Colocar(Accion a)
        {
            bool robada = Etapa == Fase.Oferta;
            var mano = jugadores[Activo].Mano;
            int indice = mano.FindIndex(f => f.Id == a.Valor);
            if (robada ? oferta?.Id != a.Valor : indice < 0) return false;
            var ficha = robada ? oferta.Value : mano[indice];
            if (!ReglasJugada.Jugable(ficha,Ataque)) return false;
            int comodin = mano.FindIndex(f => f.Tipo == TipoFicha.Intercambio);
            bool combo = a.Objetivo != -1;
            if (combo && (robada || ficha.Tipo != TipoFicha.Ordinaria || comodin < 0 || !ObjetivoValido(a.Objetivo))) return false;

            mesa.Add(ficha);
            if (ficha.Tipo == TipoFicha.Ordinaria) Ataque = ficha.Espada;
            if (ficha.Tipo == TipoFicha.Reversa) Sentido *= -1;
            if (robada) oferta = null;
            else mano.RemoveAt(indice);
            if (combo)
            {
                int pos = mano.FindIndex(f => f.Tipo == TipoFicha.Intercambio);
                mesa.Add(mano[pos]); mano.RemoveAt(pos);
                PrepararCambio(a.Objetivo);
            }
            else
            {
                // Una ficha de oferta viene de fuera de la mano: su colocación ya conserva la capacidad.
                if (!robada) mano.Add(reserva.Tomar());
                Avanzar();
            }
            return true;
        }
    }
}
