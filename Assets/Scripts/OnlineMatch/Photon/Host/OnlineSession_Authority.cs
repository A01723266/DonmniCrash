using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using DonmniCrash.Game;
using UnityEngine;

namespace DonmniCrash.UI
{
    public sealed partial class OnlineSession
    {
        public int RoundId { get; private set; }
        public int Deadline { get; private set; }
        public int[] Seats { get; private set; } = Array.Empty<int>();
        public string[] SeatNames { get; private set; } = Array.Empty<string>();
        public int[] Emblems { get; private set; } = Array.Empty<int>();
        public OpeningRollSequence Opening { get; private set; }
        public NetworkTableView View { get; private set; }
        public bool AwaitingAction { get; private set; }
        public int MySeat => Array.IndexOf(Seats,LocalId);
        public bool Dismissed { get; set; }
        MatchEngine engine;
        int[] lastRequests;
        int sequence,successor,previousRevision=-1,imageRevision=-1,backupRound,requestDeadline;
        byte[] previousImage=Array.Empty<byte>(), image;
        bool awaitingBackup, departuresPending;
        float retryAt;
        readonly HashSet<int> loadedTables=new HashSet<int>();
        readonly Dictionary<int,int> revisionCursor=new Dictionary<int,int>();
        readonly Dictionary<int,int> boardCursor=new Dictionary<int,int>();
        readonly Dictionary<int,byte[]> privateCursor=new Dictionary<int,byte[]>();

        void ClearRound()
        {
            engine=null; View=null; Dismissed=AwaitingAction=awaitingBackup=departuresPending=false;
            previousImage=Array.Empty<byte>(); image=null; backupOpening=null; imageRevision=previousRevision=-1; backupRound=successor=0;
            Seats=Array.Empty<int>(); SeatNames=Array.Empty<string>(); Emblems=Array.Empty<int>();
            Opening=null; OpeningStartsAt=Deadline=loadBy=0;
            boardCursor.Clear(); privateCursor.Clear(); publicCursor.Clear(); sentDeadlines.Clear(); revisionCursor.Clear(); loadedTables.Clear();
        }
        void StartRound()
        {
            if (!Host || RoundId!=0 || Members.Count<2 || ReadyMembers.Count*5<Members.Count*3) return;
            ClearRound(); RoundId=Guid.NewGuid().GetHashCode() | 1;
            Countdown=0; ReadyMembers.Clear(); Seats=Members.ToArray(); SeatNames=Seats.Select(PlayerName).ToArray();
            Emblems=Seats.Select(id=>Math.Max(0,Array.IndexOf(UserSettings.Emblems.ToArray(),
                PhotonNetwork.CurrentRoom.GetPlayer(id)?.CustomProperties["emblem"] as string))).ToArray();
            lastRequests=new int[Seats.Length];
            engine=new MatchEngine(UnpackRules(PackRules(Rules),Seats.Length),Guid.NewGuid().GetHashCode());
            Opening=OpeningRollSequence.Capture(engine);
            loadBy=unchecked(Now+15000);
            WriteRoom(); Stage();
        }

        // A single outstanding checkpoint serializes commands, deadlines and membership changes.
        // Public/private views only leave this authority after the successor can recover that revision.
        void Stage()
        {
            image=engine.SaveAuthority(); imageRevision=engine.Revision; backupRound=RoundId;
            int next=Members.Skip(1).FirstOrDefault();
            if (successor!=next) { successor=next; previousRevision=-1; previousImage=Array.Empty<byte>(); }
            if (successor==0) { Commit(); return; }
            awaitingBackup=true;
            TransmitBackup();
        }
        void TransmitBackup()
        {
            retryAt=Time.unscaledTime+2;
            bool full=previousRevision<0;
            Send(Message.Backup,new object[]{RoundId,imageRevision,previousRevision,
                CheckpointDelta.Encode(previousImage,image),new[]{Deadline,OpeningStartsAt,loadBy},lastRequests,
                full?Seats:Array.Empty<int>(),full?SeatNames:Array.Empty<string>(),full?Emblems:Array.Empty<int>(),
                full?Opening.Encode():Array.Empty<int>()},successor);
        }
        void MaintainBackup()
        {
            if (engine!=null && Members.Skip(1).FirstOrDefault()!=successor) Stage();
        }
        void ReceiveBackup(object body)
        {
            if (!(body is object[] b) || b.Length!=10 || !(b[0] is int round) || round!=RoundId ||
                Members.Skip(1).FirstOrDefault()!=LocalId) return;
            int revision=(int)b[1],baseline=(int)b[2];
            if (!(b[4] is int[] timing) || timing.Length!=3) return;
            if (imageRevision!=revision || baseline<0)
            {
                if (baseline>=0 && baseline!=imageRevision)
                { SendMaster(Message.Received,new[]{round,-1,timing[0],timing[1]}); return; }
                image=CheckpointDelta.Apply(baseline<0?Array.Empty<byte>():image,(byte[])b[3]);
                imageRevision=revision; backupRound=round;
            }
            backupDeadline=timing[0]; backupOpeningStartsAt=timing[1]; backupLoadBy=timing[2];
            lastRequests=(int[])b[5];
            if (baseline<0)
            {
                Seats=(int[])b[6]; SeatNames=(string[])b[7]; Emblems=(int[])b[8];
                backupOpening=OpeningRollSequence.Decode((int[])b[9],Seats.Length);
            }
            SendMaster(Message.Received,new[]{round,revision,backupDeadline,backupOpeningStartsAt});
        }
        int backupDeadline, backupOpeningStartsAt, backupLoadBy;
        OpeningRollSequence backupOpening;
        void ReceivedBackup(int sender,object body)
        {
            if (!awaitingBackup || sender!=successor || !(body is int[] ack) || ack.Length!=4 ||
                ack[0]!=RoundId || ack[2]!=Deadline || ack[3]!=OpeningStartsAt) return;
            if (ack[1]<0) { previousRevision=-1; previousImage=Array.Empty<byte>(); TransmitBackup(); }
            else if (ack[1]==imageRevision) Commit();
        }
        void Commit()
        {
            awaitingBackup=false; previousImage=image; previousRevision=imageRevision;
            foreach (int id in Members.ToArray()) Deliver(id,!boardCursor.ContainsKey(id));
        }
        void TakeAuthority()
        {
            boardCursor.Clear(); privateCursor.Clear(); publicCursor.Clear(); sentDeadlines.Clear(); revisionCursor.Clear(); loadedTables.Clear();
            if (backupRound!=RoundId || image==null || Seats.Length<2 || backupOpening==null)
            {
                Notice="net.lost";
                Send(Message.RoundLost,0,Members.Where(id=>id!=LocalId).ToArray());
                EndRound(); return;
            }
            engine=new MatchEngine(UnpackRules(PackRules(Rules),Seats.Length),Guid.NewGuid().GetHashCode());
            engine.LoadAuthority(image);
            Opening=backupOpening; OpeningStartsAt=backupOpeningStartsAt; loadBy=backupLoadBy; Deadline=backupDeadline;
            int formerActor=engine.ActiveSeat;
            var formerPhase=engine.Phase;
            successor=0; previousRevision=-1; previousImage=Array.Empty<byte>(); awaitingBackup=false;
            foreach(int seat in Enumerable.Range(0,Seats.Length).Where(i=>!Members.Contains(Seats[i]))) engine.RemoveSeat(seat);
            UpdateDeadlineAfterDeparture(formerActor,formerPhase);
            Stage();
        }
        void QueueDepartures()
        {
            if (!departuresPending || engine==null || awaitingBackup) return;
            departuresPending=false;
            bool changed=false;
            int actor=engine.ActiveSeat; var phase=engine.Phase;
            for (int i=0;i<Seats.Length;i++)
                if (!Members.Contains(Seats[i])) changed |= engine.RemoveSeat(i);
            if (!changed) return;
            UpdateDeadlineAfterDeparture(actor,phase);
            Stage();
        }
        void ResetDeadline() => Deadline=unchecked(Now+(engine.Phase==TurnPhase.Finished?12000:Rules.Seconds*1000));
        void TickRound()
        {
            if (engine==null) return;
            if (awaitingBackup)
            {
                if (Time.unscaledTime>=retryAt) TransmitBackup();
                return;
            }
            QueueDepartures();
            if (awaitingBackup) return;
            if (WaitingForTables)
            {
                if (SecondsLeft(loadBy)>0 && !Seats.Where(Members.Contains).All(loadedTables.Contains)) return;
                ScheduleOpening();
                Stage();
                return;
            }
            if (OpeningBlocksPlay) return;
            if (SecondsLeft(Deadline)>0) return;
            if (engine.Phase==TurnPhase.Finished) { EndRound(); return; }
            engine.Expire(); ResetDeadline(); Stage();
        }
        void EndRound()
        {
            ClearRound(); lobbyGeneration++; RoundId=Countdown=0; ReadyMembers.Clear(); WriteRoom();
        }
        void ReceiveAction(int sender,object body)
        {
            if (!(body is int[] a) || a.Length!=6 || engine==null) return;
            int seat=Array.IndexOf(Seats,sender);
            if (OpeningBlocksPlay || awaitingBackup || a[0]!=RoundId || seat<0 || seat!=engine.ActiveSeat || a[2]<=lastRequests[seat] ||
                a[1]!=engine.Revision || SecondsLeft(Deadline)<=0)
            { Reject(sender); return; }
            var command=new MoveCommand((MoveKind)a[3],a[4],a[5]);
            var phase=engine.Phase;
            if (!engine.TryApply(seat,a[1],command)) { Reject(sender); return; }
            lastRequests[seat]=a[2];
            if (command.Kind!=MoveKind.Choose || phase!=engine.Phase || seat!=engine.ActiveSeat) ResetDeadline();
            Stage();
        }
        void Reject(int sender)
        {
            if (sender==LocalId) AwaitingAction=false;
            else Send(Message.RejectAction,RoundId,sender);
        }
        public void Submit(MoveCommand command)
        {
            if (OpeningBlocksPlay || AwaitingAction || View==null || MySeat<0 || MySeat!=View.ActiveSeat) return;
            AwaitingAction=true; requestDeadline=unchecked(Now+3000);
            SendMaster(Message.Action,new[]{RoundId,View.Revision,++sequence,(int)command.Kind,command.Value,command.Target});
        }
        public void CheckPendingAction()
        {
            if (AwaitingAction && SecondsLeft(requestDeadline)<=0)
            { AwaitingAction=false; RequestFrame(); }
        }
    }
}
