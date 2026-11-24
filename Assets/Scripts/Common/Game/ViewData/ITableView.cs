using System.Collections.Generic;

namespace DonmniCrash.Game
{
    // Both the local engine and a recipient-specific network view can drive the same HUD.
    public interface ITableView
    {
        int ActiveSeat { get; }
        TurnPhase Phase { get; }
        Element? Attack { get; }
        int StockCount { get; }
        int RemainingChoices { get; }
        int OptionCount { get; }
        bool VisibleSelection { get; }
        IReadOnlyList<int> Order { get; }
        IReadOnlyList<GameTile> BoardTiles { get; }
        int Health(int seat);
        int HandCount(int seat);
        GameTile[] HandOf(int seat);
        GameTile? OfferFor(int seat);
        GameTile? InspectOption(int seat,int index);
        bool IsAvailable(int index);
        bool CanTarget(int seat);
    }
}
