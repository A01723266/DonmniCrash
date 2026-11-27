namespace DonmniCrash.Game
{
    public enum TileZone { Hand, Stock, Offer, Board }

    // Una etapa agrupa las fichas que viajan al mismo tiempo.
    public readonly struct TileMotion
    {
        public const int StageMilliseconds = 1000;
        public readonly TileZone From, To;
        public readonly int Sender, Receiver, Stage, Choice;
        public readonly GameTile? Tile;

        public TileMotion(TileZone from, TileZone to, int sender, int receiver, GameTile? tile, int stage = 0, int choice = -1)
        {
            From = from; To = to; Sender = sender; Receiver = receiver;
            Tile = tile; Stage = stage; Choice = choice;
        }

        public TileMotion ForSeat(int seat)
        {
            bool visible = To == TileZone.Board || seat == Sender || seat == Receiver;
            return new TileMotion(From, To, Sender, Receiver, visible ? Tile : null, Stage, seat == Sender ? Choice : -1);
        }
    }
}
