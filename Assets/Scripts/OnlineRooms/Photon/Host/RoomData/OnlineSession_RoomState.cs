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
        void WriteRoom()
        {
            var room=PhotonNetwork.CurrentRoom;
            var metadata=new Hashtable();
            void Publish(string key,object value)
            {
                if (!Equals(room.CustomProperties[key],value)) metadata[key]=value;
            }
            Publish("successor",Members.Skip(1).FirstOrDefault());
            Publish("count",Members.Count); Publish("round",RoundId); Publish("title",RoomTitle);
            Publish("limit",Rules.Players); Publish("private",IsPrivate);
            if (metadata.Count>0) room.SetCustomProperties(metadata);
            Send(Message.Roster,new object[]{Members.ToArray(),ReadyMembers.ToArray(),Countdown,RoundId,
                RoomTitle,IsPrivate,PackRules(Rules),lobbyGeneration,closing},Members.Where(id=>id!=LocalId).ToArray());
            Changed?.Invoke();
        }

    }
}
