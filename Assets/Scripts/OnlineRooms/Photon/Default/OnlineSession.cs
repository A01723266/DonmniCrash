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
    /// <summary>Owns one Photon connection across the browser, waiting room and match scenes.</summary>
    public sealed partial class OnlineSession : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        enum Message : byte { Apply = 11, Ready, Denied, Closed, Action, Frame, Sync, Backup, Received, RejectAction, RoundLost, Roster, TableLoaded }
        public static OnlineSession Instance { get; private set; }
        public static OnlineSession Get()
        {
            if (!Instance) Instance = new GameObject("OnlineSession").AddComponent<OnlineSession>();
            return Instance;
        }
        void Awake() { Instance=this; DontDestroyOnLoad(gameObject); }
        void OnDestroy() { if (Instance==this) Instance=null; }

        public event Action Changed;
        public event Action TableChanged;
        public string Notice { get; private set; }
        public void ClearNotice() => Notice=null;
        public Dictionary<string,RoomInfo> Rooms { get; } = new Dictionary<string,RoomInfo>();
        public readonly List<int> Members = new List<int>();
        public readonly HashSet<int> ReadyMembers = new HashSet<int>();
        public readonly List<int> Applicants = new List<int>();
        public string RoomTitle { get; private set; }
        public string Code => PhotonNetwork.CurrentRoom?.Name ?? "";
        public bool IsPrivate { get; private set; }
        public MatchRules Rules { get; private set; } = new MatchRules();
        public int LocalId => PhotonNetwork.LocalPlayer.ActorNumber;
        public bool Admitted => PhotonNetwork.InRoom && Members.Contains(LocalId);
        public bool Host => Admitted && PhotonNetwork.IsMasterClient && Members[0] == LocalId;
        public int Countdown { get; private set; }
        public static int Now => PhotonNetwork.ServerTimestamp;
        public static float SecondsLeft(int deadline) => Mathf.Max(0,unchecked(deadline-Now)/1000f);
        public bool Busy { get; private set; }
        bool creating, leaving, closing;
        int retries;
        string requestedTitle;
        bool requestedPrivate;
        MatchRules requestedRules;
        public static string PlayerName(int id) => PhotonNetwork.CurrentRoom?.GetPlayer(id)?.NickName ?? ("#"+id);
        public void Connect()
        {
            if (PhotonNetwork.InRoom || PhotonNetwork.InLobby || Busy) return;
            PhotonNetwork.NickName = string.IsNullOrWhiteSpace(UserSettings.DisplayName) ? LocalizedText.Read("player") : UserSettings.DisplayName;
            var settings=PhotonNetwork.PhotonServerSettings.AppSettings.CopyTo(new AppSettings());
            if (string.IsNullOrEmpty(settings.FixedRegion)) settings.FixedRegion="us";
#if UNITY_WEBGL && !UNITY_EDITOR
            settings.Protocol=ConnectionProtocol.WebSocketSecure;
#endif
            settings.AppVersion="donmnicrash-4";
            if (PhotonNetwork.IsConnectedAndReady) Busy=PhotonNetwork.JoinLobby();
            else Busy=PhotonNetwork.ConnectUsingSettings(settings);
            if (!Busy) Fail("net.connection");
            Changed?.Invoke();
        }
        public override void OnConnectedToMaster()
        {
            Busy=PhotonNetwork.JoinLobby();
            if (Busy) Changed?.Invoke(); else Fail("net.connection");
        }
        public override void OnJoinedLobby() { Busy=false; Changed?.Invoke(); }
        public override void OnLeftLobby() { Rooms.Clear(); }
        public override void OnRoomListUpdate(List<RoomInfo> updates)
        {
            foreach (var info in updates)
                if (info.RemovedFromList) Rooms.Remove(info.Name); else Rooms[info.Name]=info;
            Changed?.Invoke();
        }
        public override void OnJoinedRoom()
        {
            Busy=leaving=closing=false;
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable {{"emblem",UserSettings.Emblem}});
            var props=PhotonNetwork.CurrentRoom.CustomProperties;
            closing=props["closed"] is bool closed && closed;
            RoomTitle=props["title"] as string;
            IsPrivate=props["private"] is bool hidden && hidden;
            if (!TryRoomRules(props["rules"],out var rules)) { Notice="net.joinfailed"; Leave(); return; }
            Rules=rules;
            if (creating && PhotonNetwork.IsMasterClient)
            {
                creating=false; Members.Add(LocalId); WriteRoom();
            }
            else if (closing) { Leave(); return; }
            else SendMaster(Message.Apply,0);
            Changed?.Invoke();
        }
        public void Leave()
        {
            if (leaving || !PhotonNetwork.InRoom) return;
            leaving=PhotonNetwork.LeaveRoom(false);
        }
        public void CloseRoom()
        {
            if (!Host || closing) return;
            closing=true;
            PhotonNetwork.CurrentRoom.IsOpen=false; PhotonNetwork.CurrentRoom.IsVisible=false;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable {{"closed",true}});
            Send(Message.Closed,0,PhotonNetwork.PlayerListOthers.Select(p=>p.ActorNumber).ToArray());
            Leave();
        }
        public override void OnLeftRoom() { ResetRoom(); Changed?.Invoke(); }
        public override void OnDisconnected(DisconnectCause cause)
        {
            ResetRoom();
            Rooms.Clear();
            if (cause==DisconnectCause.DisconnectByClientLogic)
            {
                Notice=null;
                Changed?.Invoke();
            }
            else Fail("net.connection");
        }
        void ResetRoom()
        {
            Members.Clear(); Applicants.Clear(); ReadyMembers.Clear();
            Countdown=RoundId=0; Busy=leaving=closing=creating=false;
            ClearRound();
        }
        // Lobby discovery exposes aggregate metadata only. Admission, readiness and succession
        // come from an uncached message sent by the authority directly to admitted members.
        void AcceptRoster(object body)
        {
            if (!(body is object[] state) || state.Length!=9 || !(state[0] is int[] members) ||
                !members.Contains(LocalId)) return;
            Members.Clear(); Members.AddRange(members);
            ReadyMembers.Clear(); ReadyMembers.UnionWith((int[])state[1]);
            Countdown=(int)state[2];
            int next=(int)state[3];
            if (next!=RoundId) { ClearRound(); RoundId=next; }
            RoomTitle=(string)state[4]; IsPrivate=(bool)state[5]; Rules=UnpackRules((int[])state[6]);
            lobbyGeneration=(int)state[7];
            closing=(bool)state[8];
            if (RoundId!=0 && View==null) RequestFrame();
            Changed?.Invoke();
        }

        int lobbyGeneration;
        // Room properties are discovery metadata and have no authenticated writer in PUN.
        // Members act on the explicit close event from the current master.
        public override void OnPlayerPropertiesUpdate(Player actor,Hashtable changed) => Changed?.Invoke();
        public void Edit(string title,bool hidden,MatchRules rules)
        {
            if (!Host || RoundId!=0 || rules.Players<Members.Count) { Fail("net.capacity"); return; }
            RoomTitle=title; Rules=rules; IsPrivate=hidden;
            PhotonNetwork.CurrentRoom.IsVisible=!hidden;
            ReadyMembers.Clear(); Countdown=0; WriteRoom();
        }
        public override void OnPlayerLeftRoom(Player other)
        {
            if (Host)
            {
                int id=other.ActorNumber;
                Applicants.Remove(id); Members.Remove(id); ReadyMembers.Remove(id);
                departuresPending=true;
                MaintainBackup();
                if (RoundId!=0) { QueueDepartures(); WriteRoom(); } else Recalculate();
            }
            Changed?.Invoke();
        }
        public override void OnMasterClientSwitched(Player next)
        {
            // Photon elects by actor number. Admission order may differ in private rooms.
            Members.RemoveAll(id=>PhotonNetwork.CurrentRoom.GetPlayer(id)==null);
            ReadyMembers.IntersectWith(Members);
            if (closing) { Leave(); return; }
            if (Members.Count==0)
            {
                // A pending applicant cannot inherit a private room. Photon elects again on departure.
                if (!PhotonNetwork.IsMasterClient) SendMaster(Message.Apply,0);
                else
                {
                    var room=PhotonNetwork.CurrentRoom;
                    var candidate=room.CustomProperties["successor"] is int id ? room.GetPlayer(id) : null;
                    if (candidate==null || candidate.IsLocal) Leave();
                    else PhotonNetwork.SetMasterClient(candidate);
                }
                return;
            }
            if (PhotonNetwork.IsMasterClient && next.ActorNumber!=Members[0])
            { PhotonNetwork.SetMasterClient(PhotonNetwork.CurrentRoom.GetPlayer(Members[0])); return; }
            if (Host)
            {
                Applicants.Clear();
                if (RoundId!=0) TakeAuthority();
                if (RoundId==0) Recalculate(); else WriteRoom();
            }
            else if (!Admitted) SendMaster(Message.Apply,0);
            else if (RoundId!=0) RequestFrame();
            Changed?.Invoke();
        }

        void Update()
        {
            if (!Host || leaving || closing) return;
            if (RoundId!=0) TickRound();
            else if (Countdown!=0 && SecondsLeft(Countdown)<=0) StartRound();
        }

        static void Send(Message kind,object body,params int[] recipients)
        {
            if (recipients.Length==0) return;
            PhotonNetwork.RaiseEvent((byte)kind,body,new RaiseEventOptions{TargetActors=recipients},SendOptions.SendReliable);
        }
        void SendMaster(Message kind,object body)
        {
            if (Host) Receive(kind,LocalId,body);
            else if (PhotonNetwork.MasterClient!=null) Send(kind,body,PhotonNetwork.MasterClient.ActorNumber);
        }
        public void OnEvent(EventData e)
        {
            if (!PhotonNetwork.InRoom || !Enum.IsDefined(typeof(Message),e.Code)) return;
            Receive((Message)e.Code,e.Sender,e.CustomData);
        }

        void Receive(Message kind,int sender,object body)
        {
            if (!PhotonNetwork.InRoom || leaving || !PhotonNetwork.CurrentRoom.Players.ContainsKey(sender)) return;
            if (Host)
            {
                if (kind==Message.Apply) { Apply(sender); return; }
                if (Members.Contains(sender))
                {
                    if (kind==Message.Ready && RoundId==0 && body is int[] vote && vote.Length==2 && vote[0]==lobbyGeneration)
                    { if (vote[1]==1) ReadyMembers.Add(sender); else ReadyMembers.Remove(sender); Recalculate(); return; }
                    if (kind==Message.TableLoaded && body is int round && round==RoundId)
                    { loadedTables.Add(sender); return; }
                    if (kind==Message.Sync) { Deliver(sender,true); return; }
                    if (kind==Message.Action) { ReceiveAction(sender,body); return; }
                    if (kind==Message.Received) { ReceivedBackup(sender,body); return; }
                }
            }
            if (sender!=PhotonNetwork.MasterClient?.ActorNumber) return;
            if (kind==Message.Closed || kind==Message.Denied)
            { Notice=kind==Message.Closed?"net.closed":"net.denied"; Leave(); return; }
            if (kind==Message.Roster) { AcceptRoster(body); return; }
            if (!Admitted) return;
            if (kind==Message.Backup) ReceiveBackup(body);
            if (kind==Message.Frame) ReceiveFrame(body);
            if (kind==Message.RejectAction && body is int rejectedRound && rejectedRound==RoundId) { AwaitingAction=false; RequestFrame(); }
            if (kind==Message.RoundLost) { Notice="net.lost"; Changed?.Invoke(); }
        }
    }
}
