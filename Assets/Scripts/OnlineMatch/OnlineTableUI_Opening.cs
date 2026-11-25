using DonmniCrash.Game;
using UnityEngine;

namespace DonmniCrash.UI
{
    public sealed partial class OnlineTableUI
    {
        bool showedWaitingClock;
        float nextReadySignal;

        void UpdateOpening()
        {
            var view = session.View;
            if (!OpeningBlocksActions || view?.Phase == TurnPhase.Finished)
            {
                if (OpeningView.gameObject.activeSelf) OpeningView.Hide();
                return;
            }

            if (!showedWaitingClock)
            {
                ShowWaitingClock();
                showedWaitingClock = true;
            }
            if (session.WaitingForTables && view != null && LocalSeat >= 0 && Time.unscaledTime >= nextReadySignal)
            {
                // El sucesor también puede recibir la confirmación si el anfitrión cambia durante la carga.
                nextReadySignal = Time.unscaledTime + 1f;
                session.SignalTableLoaded();
            }
            var sequence = session.Opening;
            int elapsed = session.OpeningElapsed;
            if (view == null || sequence == null || session.WaitingForTables)
            {
                OpeningView.Show(null,-1,LocalSeat,-1,-1,DisplayName);
                return;
            }

            int first = view.ActiveSeat;
            OpeningView.Show(sequence,elapsed,LocalSeat,first,NextSurvivingSeat(view,first),DisplayName);
        }

        static int NextSurvivingSeat(ITableView view,int first)
        {
            var order = view.Order;
            int index = 0;
            while (order[index] != first) index++;
            for (int step = 1; step < order.Count; step++)
            {
                int candidate = order[(index + order.Count + view.Direction * step) % order.Count];
                if (view.Health(candidate) > 0) return candidate;
            }
            return first;
        }
    }
}
