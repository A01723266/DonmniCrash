using System.Collections.Generic;
using System.Linq;

namespace DonmniCrash.Juego
{
    // Proyecta únicamente lo que puede conocer el asiento activo.
    public sealed partial class MotorPartida
    {
        public VistaJugador ConsultarTurno()
        {
            var mano = ManoDe(Activo);
            var opciones = new List<Accion>();
            Ficha?[] seleccion = null;
            if (Etapa == Fase.Decision)
            {
                opciones.Add(new Accion(Movimiento.Robar));
                foreach (var ficha in mano.Where(f => ReglasJugada.Jugable(f,Ataque)))
                {
                    opciones.Add(new Accion(Movimiento.Colocar,ficha.Id));
                    if (ficha.Tipo == TipoFicha.Ordinaria && mano.Any(f => f.Tipo == TipoFicha.Intercambio))
                        foreach (int id in orden.Where(ObjetivoValido))
                            opciones.Add(new Accion(Movimiento.Colocar,ficha.Id,id));
                }
            }
            else if (Etapa == Fase.Oferta)
            {
                opciones.Add(new Accion(Movimiento.Colocar,oferta.Value.Id));
                opciones.Add(new Accion(Movimiento.Conservar));
            }
            else if (Etapa == Fase.CompletarMano || Etapa == Fase.SeleccionCambio)
            {
                seleccion = new Ficha?[NumeroOpciones];
                for (int i = 0; i < seleccion.Length; i++)
                    if (Disponible(i))
                    {
                        seleccion[i] = VerOpcion(Activo,i);
                        opciones.Add(new Accion(Movimiento.Elegir,i));
                    }
            }
            return new VistaJugador {
                Actor=Activo, Siguiente=Vecino(Sentido), Anterior=Vecino(-Sentido), Ataque=Ataque,
                Mano=mano, Tablero=mesa.ToArray(), Oferta=oferta, Opciones=opciones.ToArray(), Seleccion=seleccion,
                Vidas=jugadores.Select(j=>j.Vida).ToArray(), Cantidades=jugadores.Select(j=>j.Mano.Count).ToArray()
            };
        }
    }
}
