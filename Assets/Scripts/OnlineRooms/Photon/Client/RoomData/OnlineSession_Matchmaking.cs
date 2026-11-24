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
        public void Create(string title,bool hidden,MatchRules rules)
        {
            if (!PhotonNetwork.InLobby || Busy) return;
            requestedTitle=title; requestedPrivate=hidden; requestedRules=rules;
            creating=true; retries=0; TryCreate();
        }
        static int[] PackRules(MatchRules r) => new[]{r.Players,r.Hand,r.Health,r.Seconds,r.Reverses,r.Passes,r.Swaps};
        static MatchRules UnpackRules(int[] v,int seats=0) =>
            new MatchRules(seats==0?v[0]:seats,v[1],v[2],v[3],v[4],v[5],v[6]);

        static bool TryRoomRules(object payload,out MatchRules rules)
        {
            rules=null;
            if (!(payload is int[] values) || values.Length!=7) return false;
            try { rules=UnpackRules(values); return true; }
            catch (ArgumentOutOfRangeException) { return false; }
        }

        void TryCreate()
        {
            const string alphabet="ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var bytes=Guid.NewGuid().ToByteArray();
            var code=new string(bytes.Take(6).Select(b=>alphabet[b%alphabet.Length]).ToArray());
            var props=new Hashtable {{"title",requestedTitle},{"rules",PackRules(requestedRules)},{"private",requestedPrivate},
                {"count",0},{"limit",requestedRules.Players},{"round",0},{"closed",false}};
            // Extra transport slots let private applicants wait without consuming a playing seat.
            var options=new RoomOptions { MaxPlayers=12,IsVisible=!requestedPrivate,PlayerTtl=0,EmptyRoomTtl=0,
                CustomRoomProperties=props,CustomRoomPropertiesForLobby=new[]{"title","count","limit","round"} };
            Busy=PhotonNetwork.CreateRoom(code,options,TypedLobby.Default);
            if (!Busy) Fail("net.connection");
        }
        public void Join(string code)
        {
            if (Busy || !PhotonNetwork.InLobby) return;
            creating=false; Busy=PhotonNetwork.JoinRoom(code.Trim().ToUpperInvariant());
            if (!Busy) Fail("net.joinfailed");
        }
        public override void OnCreateRoomFailed(short code,string message)
        {
            if (code==ErrorCode.GameIdAlreadyExists && retries++<5) { TryCreate(); return; }
            creating=false; Fail("net.joinfailed");
        }
        public override void OnJoinRoomFailed(short code,string message) => Fail("net.joinfailed");
        void Fail(string key) { Busy=false; Notice=key; Changed?.Invoke(); }

    }
}
