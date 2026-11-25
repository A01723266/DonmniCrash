using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DonmniCrash.Game;

namespace DonmniCrash.UI
{
    /// <summary>Online HUD consumes a recipient-specific view. Touches submit commands to the authority.</summary>
    public sealed partial class OnlineTableUI : PlayableTableUI
    {
        OnlineSession session;
        RoomDialogs dialogs;
        bool initialized, shownResult, hasView;
        CanvasGroup handInput;
        bool actionsWereBlocked;
        int renderedRevision=-1, finishedRound;
        protected override int StartingHealth=>session.Rules.Health;
        protected override int LocalSeat=>session.MySeat;
        protected override bool ActionsBlocked=>session.AwaitingAction;
        protected override bool OpeningBlocksActions=>session.OpeningBlocksPlay;
        protected override string DisplayName(int seat)=>session.SeatNames[seat];
        protected override int Emblem(int seat)=>session.Emblems[seat];
        protected override void LeaveTable() { session.Leave(); }
        protected override void Replay() { session.Dismissed=session.RoundId==finishedRound; GoTo("OnlineRooms","LobbyScreen"); }
        protected override void ExitResults() { PhotonNetwork.Disconnect(); GoTo("Start"); }

        protected override void BindControls()
        {
            session=OnlineSession.Get();
            base.BindControls();
            handInput=FindNode("HandHUDPanel").gameObject.AddComponent<CanvasGroup>();
            handInput.interactable=false;
            BindButton("PauseDialog","CloseRoomButton",()=>Open("CloseRoomDialog"));
            dialogs=new RoomDialogs(session,FindNode("AdmissionRequestDialog"),FindNode("CloseRoomDialog"),Open,Close);
            session.Changed+=Refresh;
            session.TableChanged+=DrawTable;
            BindButton("ConnectionLostDialog","ReturnToRoomsButton",()=>GoTo("OnlineRooms"));
        }
        protected override void Start()
        {
            base.Start();
            initialized=true;
            Refresh();
            if (session.Admitted && session.RoundId!=0) UpdateOpening();
        }
        protected override void OnDestroy()
        {
            if (session) { session.Changed-=Refresh; session.TableChanged-=DrawTable; }
            base.OnDestroy();
        }
        protected override void SubmitMove(MoveCommand action)
        {
            if (!InteractionBlocked && !OpeningBlocksActions && session.View!=null && !session.AwaitingAction)
                session.Submit(action);
        }
        void Refresh()
        {
            if (!initialized || shownResult) return;
            if (!session.Admitted || session.RoundId==0)
            {
                GoTo("OnlineRooms",session.Admitted?"LobbyScreen":null); return;
            }
            FindNode("CloseRoomButton",FindNode("PauseDialog")).gameObject.SetActive(session.Host);
            if (session.View!=null && (!hasView || renderedRevision!=session.View.Revision)) DrawTable();
        }
        void DrawTable()
        {
            if (!initialized || shownResult || session.View==null) return;
            var view=session.View;
            renderedRevision=view.Revision; hasView=true;
            PaintNetwork(view,view.FullFrame);
            UpdateOpening();
            if (!OpeningBlocksActions) NetworkClock(OnlineSession.SecondsLeft(session.Deadline));
            if (!shownResult && view.Phase==TurnPhase.Finished)
            {
                shownResult=true; finishedRound=session.RoundId;
                OpeningView.Hide();
                int winner=view.Order.FirstOrDefault(i=>view.Health(i)>0);
                ShowStandings(T("net.winner",DisplayName(winner)));
            }
        }
        // Shared deadlines continue while help or settings are visible.
        void LateUpdate()
        {
            if (!initialized || shownResult || !session.Admitted || session.RoundId==0) return;
            session.CheckPendingAction();
            UpdateOpening();
            bool blocked = OpeningBlocksActions || ActionsBlocked;
            if (hasView && actionsWereBlocked != blocked)
            {
                actionsWereBlocked=blocked;
                RenderActions();
            }
            dialogs.Refresh(!InteractionBlocked && !OpeningBlocksActions);
            handInput.interactable=hasView && !blocked;
            if (hasView && !OpeningBlocksActions)
            {
                showedWaitingClock=false;
                NetworkClock(OnlineSession.SecondsLeft(session.Deadline));
            }
        }
    }
}
