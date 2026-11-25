using DonmniCrash.Game;

namespace DonmniCrash.UI
{
    public sealed partial class OnlineSession
    {
        int loadBy;
        public int OpeningStartsAt { get; private set; }
        public bool WaitingForTables => Deadline == 0;
        public int OpeningElapsed => OpeningStartsAt == 0 ? -1 : unchecked(Now - OpeningStartsAt);
        public bool OpeningBlocksPlay => WaitingForTables ||
            (OpeningStartsAt != 0 && Opening != null && OpeningElapsed < Opening.DurationMilliseconds);

        public void SignalTableLoaded()
        {
            if (WaitingForTables && View != null && MySeat >= 0)
                SendMaster(Message.TableLoaded,RoundId);
        }

        void ScheduleOpening()
        {
            OpeningStartsAt=unchecked(Now+OpeningRollSequence.StageMilliseconds);
            if (OpeningStartsAt==0) OpeningStartsAt=1;
            Deadline=unchecked(OpeningStartsAt+Opening.DurationMilliseconds+Rules.Seconds*1000);
            if (Deadline==0) Deadline=1;
            // El horario también se envía a clientes cuyo estado de juego tiene la misma revisión.
            boardCursor.Clear();
        }

        void UpdateDeadlineAfterDeparture(int formerActor,TurnPhase formerPhase)
        {
            if (engine.Phase==TurnPhase.Finished)
            {
                if (formerPhase==TurnPhase.Finished) return;
                OpeningStartsAt=0;
                ResetDeadline();
                return;
            }
            if (!OpeningBlocksPlay && (engine.ActiveSeat!=formerActor || engine.Phase!=formerPhase))
                ResetDeadline();
        }
    }
}
