namespace DonmniCrash.Juego
{
    /// <summary>Única tabla elemental para el motor, los bots y la ayuda de la mesa.</summary>
    public static partial class ReglasJugada
    {
        public static bool Vence(Elemento escudo, Elemento ataque)
        {
            switch (escudo)
            {
                case Elemento.Fuego: return ataque == Elemento.Planta || ataque == Elemento.Viento;
                case Elemento.Viento: return ataque == Elemento.Planta || ataque == Elemento.Tierra;
                case Elemento.Planta: return ataque == Elemento.Agua || ataque == Elemento.Tierra;
                case Elemento.Tierra: return ataque == Elemento.Fuego || ataque == Elemento.Agua;
                case Elemento.Agua: return ataque == Elemento.Fuego || ataque == Elemento.Viento;
                default: return false;
            }
        }

        public static bool Jugable(Ficha ficha, Elemento? ataque)
        {
            if (ficha.Tipo == TipoFicha.Ordinaria) return !ataque.HasValue || Vence(ficha.Escudo, ataque.Value);
            return ataque.HasValue && (ficha.Tipo == TipoFicha.Reversa || ficha.Tipo == TipoFicha.Pasar);
        }
    }
}
