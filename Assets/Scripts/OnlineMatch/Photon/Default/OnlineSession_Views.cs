using System;
using System.Linq;
using DonmniCrash.Game;

namespace DonmniCrash.UI
{
    public sealed partial class OnlineSession
    {
        readonly System.Collections.Generic.Dictionary<int,byte[]> publicCursor=new System.Collections.Generic.Dictionary<int,byte[]>();
        readonly System.Collections.Generic.Dictionary<int,int> sentDeadlines=new System.Collections.Generic.Dictionary<int,int>();

        public void RequestFrame()
        {
            if (RoundId!=0 && Admitted) SendMaster(Message.Sync,RoundId);
        }

        // No room property or cached event contains a hand, an offered tile, or the deck.
        void Deliver(int id,bool full)
        {
            if (engine==null || awaitingBackup || !Members.Contains(id)) return;
            int seat=Array.IndexOf(Seats,id);
            int start=full || !boardCursor.TryGetValue(id,out int cursor)?0:cursor;
            var privateData=NetworkTableView.PrivateFrame(engine,seat);
            bool dirty=full || !privateCursor.TryGetValue(id,out var old) || !privateData.SequenceEqual(old);
            var publicData=NetworkTableView.PublicFrame(engine,start);
            // A private pick can advance the engine revision without changing another player's view.
            // Keep that recipient's own baseline so their next visible update remains applicable.
            if (!full && !dirty && sentDeadlines.TryGetValue(id,out int sent) && sent==Deadline &&
                publicCursor.TryGetValue(id,out var prior) && publicData.AsSpan(4).SequenceEqual(prior.AsSpan(4))) return;
            publicCursor[id]=publicData;
            sentDeadlines[id]=Deadline;
            object[] frame={RoundId,full,publicData,dirty?privateData:Array.Empty<byte>(),
                new[]{Deadline,OpeningStartsAt}, full?Seats:Array.Empty<int>(), full?SeatNames:Array.Empty<string>(),full?Emblems:Array.Empty<int>(),
                full?Opening.Encode():Array.Empty<int>(),revisionCursor.TryGetValue(id,out int baseline)?baseline:-1};
            revisionCursor[id]=engine.Revision; privateCursor[id]=privateData; boardCursor[id]=engine.BoardTiles.Count;
            if (id==LocalId) ReceiveFrame(frame); else Send(Message.Frame,frame,id);
        }
        void ReceiveFrame(object body)
        {
            if (!(body is object[] f) || f.Length!=10 || !(f[0] is int round) || round!=RoundId) return;
            if (!(f[4] is int[] timing) || timing.Length!=2) return;
            bool full=(bool)f[1];
            if (!full && View==null) { RequestFrame(); return; }
            var payload=(byte[])f[2];
            int revision=BitConverter.ToInt32(payload,0);
            if (View!=null && (revision<View.Revision || (!full && revision==View.Revision))) return;
            if (!full && (int)f[9]!=View.Revision) { RequestFrame(); return; }
            try
            {
                if (full)
                {
                    Seats=(int[])f[5]; SeatNames=(string[])f[6]; Emblems=(int[])f[7];
                    Opening=OpeningRollSequence.Decode((int[])f[8],Seats.Length);
                }
                View ??= new NetworkTableView();
                View.Apply(payload,(byte[])f[3],full,MySeat);
            }
            catch (System.IO.InvalidDataException) { View=null; RequestFrame(); return; }
            Deadline=timing[0]; OpeningStartsAt=timing[1]; AwaitingAction=false;
            TableChanged?.Invoke(); Changed?.Invoke();
        }
    }
}
