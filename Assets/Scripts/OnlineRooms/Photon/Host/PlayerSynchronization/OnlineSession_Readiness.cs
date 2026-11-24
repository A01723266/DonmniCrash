using System;
using System.Collections.Generic;
using System.Linq;
using DonmniCrash.Game;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace DonmniCrash.UI
{
    public sealed partial class OnlineSession
    {
        public void ToggleReady()
        {
            if (Admitted && RoundId==0) SendMaster(Message.Ready,new[]{lobbyGeneration,ReadyMembers.Contains(LocalId)?0:1});
        }
        void Recalculate()
        {
            if (!Host || RoundId!=0) return;
            if (Members.Count<2 || ReadyMembers.Count*5 < Members.Count*3) Countdown=0;
            else if (ReadyMembers.Count==Members.Count) { StartRound(); return; }
            else if (Countdown==0) Countdown=unchecked(Now+30000);
            WriteRoom();
        }
        public void Resolve(int id,bool allow)
        {
            if (!Host || !Applicants.Remove(id)) return;
            if (!allow || Members.Count>=Rules.Players) Send(Message.Denied,0,id);
            else { Members.Add(id); MaintainBackup(); }
            if (RoundId==0) Recalculate(); else WriteRoom();
            Changed?.Invoke();
        }
        void Apply(int sender)
        {
            if (Members.Contains(sender)) { if (RoundId!=0) Deliver(sender,true); return; }
            if (Members.Count>=Rules.Players) { Send(Message.Denied,0,sender); return; }
            if (!Applicants.Contains(sender)) Applicants.Add(sender);
            if (!IsPrivate) Resolve(sender,true);
            else Changed?.Invoke();
        }
    }
}
