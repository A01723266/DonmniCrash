using System.Linq;
using DonmniCrash.Juego;

namespace DonmniCrash.UI
{
    /// <summary>Posiciones reproducibles y objetivos de cada ejercicio. No ejecuta reglas propias.</summary>
    internal static partial class PasosTutorial
    {
        public const int Total = 9;
        public static MotorPartida Preparar(int paso)
        {
            // Los IDs didácticos no se cruzan con los del generador de sets.
            int id = 100000;
            Ficha Normal(Elemento escudo) => new Ficha(id++,TipoFicha.Ordinaria,Elemento.Tierra,escudo);
            Ficha Comodin(TipoFicha tipo) => new Ficha(id++,tipo);
            var mia = new[]{ Normal(Elemento.Agua), Normal(Elemento.Fuego), Normal(Elemento.Planta) }.ToList();
            var otra = new[]{ Normal(Elemento.Planta), Normal(Elemento.Agua), Normal(Elemento.Tierra) }.ToList();
            if (paso == 4) mia[0] = Comodin(TipoFicha.Reversa);
            if (paso == 5) mia[0] = Comodin(TipoFicha.Pasar);
            if (paso == 6 || paso == 7)
            {
                mia[1] = Comodin(TipoFicha.Intercambio);
                if (paso == 6) mia.Add(Normal(Elemento.Agua));
                else otra.Add(Normal(Elemento.Fuego));
            }
            if (paso == 8) mia.RemoveRange(1,2);
            var manos = new[]{mia.ToArray(),otra.ToArray(),new[]{Normal(Elemento.Agua),Normal(Elemento.Viento),Normal(Elemento.Planta)}};
            return MotorPartida.DesdePosicion(manos,new[]{paso==8 ? 3 : mia.Count,otra.Count,3},
                paso==0 ? (Elemento?)null : Elemento.Fuego,Normal(Elemento.Agua));
        }

        public static bool Permite(int paso,MotorPartida motor,Accion a)
        {
            if (motor.Etapa == Fase.SeleccionCambio || motor.Etapa == Fase.CompletarMano) return a.Tipo == Movimiento.Elegir;
            if (paso == 2 || paso == 3)
                return motor.Etapa == Fase.Decision ? a.Tipo == Movimiento.Robar :
                    a.Tipo == (paso == 2 ? Movimiento.Colocar : Movimiento.Conservar);
            if (a.Tipo != Movimiento.Colocar) return false;
            var ficha = motor.ManoDe(0).FirstOrDefault(f=>f.Id == a.Valor);
            if (paso == 4) return ficha.Tipo == TipoFicha.Reversa;
            if (paso == 5) return ficha.Tipo == TipoFicha.Pasar;
            if (paso == 6 || paso == 7) return a.Objetivo == 1;
            return ficha.Tipo == TipoFicha.Ordinaria;
        }
    }
}
