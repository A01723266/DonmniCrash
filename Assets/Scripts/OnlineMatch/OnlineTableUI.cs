using System;
using System.Linq;
using System.Collections.Generic;
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
        TMP_Text timerText;
        bool showedWaiting;
        int renderedRevision=-1;
        protected override bool NetworkTable=>true;
        protected override int LocalSeat=>session.MySeat;
        protected override bool ActionsBlocked=>session.AwaitingAction;
        protected override string DisplayName(int seat)=>session.SeatNames[seat];
        protected override int Emblem(int seat)=>session.Emblems[seat];
        protected override void LeaveTable() { session.Leave(); }
        protected override void Replay() { session.Dismissed=true; GoTo("OnlineRooms","LobbyScreen"); }
        protected override void ExitResults() { PhotonNetwork.Disconnect(); GoTo("Start"); }

        protected override void BindControls()
        {
            session=OnlineSession.Get();
            base.BindControls();
            handInput=FindNode("HandHUDPanel").gameObject.AddComponent<CanvasGroup>();
            handInput.interactable=false;
            timerText=ComponentOf<TMP_Text>("TurnTimerLabel");
            BindButton("PauseDialog","CloseRoomButton",()=>Open("CloseRoomDialog"));
            dialogs=new RoomDialogs(session,FindNode("AdmissionRequestDialog"),FindNode("CloseRoomDialog"),Open,Close);
            session.Changed+=Refresh;
            session.TableChanged+=DrawTable;
            BindButton("ConnectionLostDialog","ReturnToRoomsButton",()=>GoTo("OnlineRooms"));
        }
        protected override void Start() { base.Start(); initialized=true; Refresh(); if (session.Admitted && session.RoundId!=0) session.SignalTableLoaded(); }
        protected override void OnDestroy()
        {
            if (session) { session.Changed-=Refresh; session.TableChanged-=DrawTable; }
            base.OnDestroy();
        }
        protected override void SubmitMove(MoveCommand action)
        {
            if (!InteractionBlocked && session.View!=null && !session.AwaitingAction)
                session.Submit(action);
        }
        void Refresh()
        {
            if (!initialized) return;
            if (!session.Admitted || session.RoundId==0)
            {
                GoTo("OnlineRooms",session.Admitted?"LobbyScreen":null); return;
            }
            FindNode("CloseRoomButton",FindNode("PauseDialog")).gameObject.SetActive(session.Host);
            if (session.View!=null && (!hasView || renderedRevision!=session.View.Revision)) DrawTable();
        }
        protected override void OnLanguageChanged()
        {
            if (session && hasView) WriteRolls();
        }
        void WriteRolls()
        {
            var lines=new List<string>();
            var rolls=session.OpeningRolls;
            if (rolls.Length<3) return;
            lines.Add(T("game.first",DisplayName(rolls[0]),DisplayName(rolls[1])));
            int rollNumber=0;
            for(int i=3;i<rolls.Length;)
            {
                int end=i+1+rolls[i]; i++;
                var row=new List<string>();
                while(i<end) { row.Add(DisplayName(rolls[i])+" "+rolls[i+1]); i+=2; }
                lines.Add(T(rollNumber++<rolls[2]?"game.roll":"game.direction")+" · "+string.Join(" / ",row));
            }
            var details=ComponentOf<TMP_Text>("DescriptionLabel","PauseDialog");
            WriteLabel(details,string.Join("\n",lines));
            details.GetComponent<LayoutElement>().preferredHeight=details.GetPreferredValues(details.text,400,0).y+12;
        }
        void DrawTable()
        {
            if (!initialized || session.View==null) return;
            var view=session.View;
            bool first=!hasView;
            renderedRevision=view.Revision; hasView=true;
            PaintNetwork(view); NetworkClock(OnlineSession.SecondsLeft(session.Deadline));
            if (first) WriteRolls();
            bool watching=LocalSeat<0 || view.Health(LocalSeat)==0;
            FindNode("SpectatorPanel").gameObject.SetActive(watching);
            FindNode("HandHUDPanel").gameObject.SetActive(!watching);
            if (!shownResult && view.Phase==TurnPhase.Finished)
            {
                shownResult=true;
                GameAudio.Current?.PlayCue(GameAudio.Current.resultClip);
                int winner=view.Order.FirstOrDefault(i=>view.Health(i)>0);
                SetLabel("WinnerLabel",T("net.winner",DisplayName(winner)),"ResultsDialog");
                var panel=FindNode("ResultsDialog");
                var row=FindNode("ResultRow1",panel);
                for (int i=0;i<6;i++)
                {
                    var item=FindNode("ResultRow"+(i+1),panel);
                    if (!item && i<view.Order.Count)
                    { item=Instantiate(row,row.parent); item.name="ResultRow"+(i+1); item.SetSiblingIndex(row.GetSiblingIndex()+i); }
                    if (!item) continue;
                    item.gameObject.SetActive(i<view.Order.Count);
                    if (i>=view.Order.Count) continue;
                    int seat=view.Order[i];
                    WriteLabel(FindNode("RankLabel",item).GetComponent<TMP_Text>(),"");
                    WriteLabel(FindNode("PlayerLabel",item).GetComponent<TMP_Text>(),DisplayName(seat));
                    WriteLabel(FindNode("ScoreLabel",item).GetComponent<TMP_Text>(),T("game.health",view.Health(seat)));
                }
                Open("ResultsDialog");
            }
        }
        // Shared deadlines continue while help or settings are visible.
        void LateUpdate()
        {
            if (!initialized || !session.Admitted) return;
            session.CheckPendingAction();
            dialogs.Refresh(!InteractionBlocked);
            handInput.interactable=hasView && !session.AwaitingAction && !session.WaitingForTables;
            if (hasView && session.WaitingForTables)
            {
                if (!showedWaiting) WriteLabel(timerText,T("net.loading"));
                showedWaiting=true;
            }
            else if (hasView) NetworkClock(OnlineSession.SecondsLeft(session.Deadline));
        }
    }
}
