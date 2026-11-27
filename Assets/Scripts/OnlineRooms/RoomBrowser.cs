using System;
using System.Linq;
using DonmniCrash.Game;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Projects the room service into the browser and lobby. Only the authority changes membership.</summary>
    public sealed partial class RoomBrowser : CanvasNavigation
    {
        const string Browser="RoomBrowserScreen",Form="CreateRoomScreen",Private="JoinPrivateRoomScreen",
            Pending="AdmissionPendingScreen",Lobby="LobbyScreen";
        protected override string InitialScreen => Browser;
        OnlineSession session;
        RoomDialogs dialogs;
        TMP_InputField filter;
        readonly Image[] seatPortraits = new Image[6];
        Sprite[] emblemSprites;
        int displayedSecond=-1;
        bool editing, initialized;
        string previousScreen;
        protected override void BindControls()
        {
            session=OnlineSession.Get();
            emblemSprites = ComponentOf<ElementHelpView>("ElementHelpDialog").symbols;
            for (int i=0;i<seatPortraits.Length;i++)
            {
                var row=FindNode("SeatRow"+i);
                var portrait=new GameObject("Portrait",typeof(RectTransform),typeof(Image),typeof(LayoutElement));
                portrait.transform.SetParent(row,false);
                portrait.transform.SetAsFirstSibling();
                var size=portrait.GetComponent<LayoutElement>();
                size.minWidth=size.preferredWidth=42;
                seatPortraits[i]=portrait.GetComponent<Image>();
                seatPortraits[i].preserveAspect=true;
                seatPortraits[i].raycastTarget=false;
            }
            filter=ComponentOf<TMP_InputField>("RoomSearchInputField");
            filter.onValueChanged.AddListener(_=>RenderRooms());
            BindButton(Browser,"CreateRoomButton",()=>{ editing=false; Show(Form); });
            BindButton(Browser,"JoinPrivateRoomButton",()=>Show(Private));
            BindButton(Browser,"RefreshRoomsButton",()=>{ session.Connect(); RenderRooms(); });
            BindButton(Browser,"BackButton",()=>{ PhotonNetwork.Disconnect(); GoTo("Start"); });
            BindButton(Form,"BackButton",()=>Show(editing?Lobby:Browser));
            BindButton(Form,"CreateRoomButton",ConfirmForm);
            BindButton(Private,"BackButton",()=>Show(Browser));
            BindButton(Private,"RequestAdmissionButton",()=>Join(ComponentOf<TMP_InputField>("RoomCodeInputField").text));
            BindButton(Pending,"CancelAdmissionButton",session.Leave);
            BindButton(Pending,"BackButton",session.Leave);
            BindButton(Lobby,"ReadyButton",session.ToggleReady);
            BindButton(Lobby,"EditRoomButton",Edit);
            BindButton(Lobby,"LeaveRoomButton",()=>Open("LeaveRoomDialog"));
            BindButton(Lobby,"BackButton",()=>Open("LeaveRoomDialog"));
            BindButton(Lobby,"CloseRoomButton",()=>Open("CloseRoomDialog"));
            BindButton("LeaveRoomDialog","StayButton",Close);
            BindButton("LeaveRoomDialog","LeaveButton",session.Leave);
            dialogs=new RoomDialogs(session,FindNode("AdmissionRequestDialog"),FindNode("CloseRoomDialog"),Open,Close);
            session.Changed+=Refresh;
            if (string.IsNullOrEmpty(session.Notice)) session.Connect();
        }
        protected override void Start() { base.Start(); initialized=true; Refresh(); }
        void OnDestroy() { if (session) session.Changed-=Refresh; }
        void Join(string code)
        {
            if (code.Trim().Length!=6) { NotifyUser("net.code"); return; }
            session.Join(code);
        }

        void Refresh()
        {
            if (!initialized) return;
            if (session.Admitted)
            {
                if (session.RoundId!=0 && !session.Dismissed) { GoTo("OnlineMatch"); return; }
                if (ScreenName!=Lobby && !(editing && ScreenName==Form && session.Host)) Show(Lobby);
                RenderLobby();
            }
            else if (PhotonNetwork.InRoom) { if (ScreenName!=Pending) Show(Pending); }
            else
            {
                if (ScreenName==Lobby || ScreenName==Pending || previousScreen==Lobby && ScreenName==Form) Show(Browser);
                if (ScreenName==Browser) RenderRooms();
            }
            previousScreen=ScreenName;
            if (!string.IsNullOrEmpty(session.Notice))
            {
                string key=session.Notice; session.ClearNotice(); NotifyUser(key);
            }
        }
        void Update()
        {
            if (!initialized) return;
            dialogs.Refresh(!InteractionBlocked && ScreenName==Lobby);
            int second=session.Countdown==0?-1:Mathf.CeilToInt(OnlineSession.SecondsLeft(session.Countdown));
            if (ScreenName==Lobby && second!=displayedSecond) RenderReadiness();
        }
        protected override void OnScreenShown(string screen)
        {
            if (screen==Browser && session) RenderRooms();
            if (screen==Lobby && session && session.Admitted) RenderLobby();
        }

        void SetText(Transform root,string id,string value)
        {
            var label=FindNode(id,root).GetComponent<TMP_Text>();
            label.richText=false;
            var localized=label.GetComponent<LocalizedText>();
            if (localized) localized.Literal(value); else label.text=value;
        }
        static string L(string key,params object[] args) => string.Format(LocalizedText.Read(key),args);

        void RenderRooms()
        {
            var root=FindNode("RoomListContentPanel");
            foreach (Transform child in root)
                if (child.name.StartsWith("RoomRow",StringComparison.Ordinal))
                { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            int count=0;
            foreach (RoomInfo info in session.Rooms.Values.OrderBy(i=>i.Name))
            {
                var props=info.CustomProperties;
                string title=props["title"] as string;
                if (!(props["count"] is int occupants) || !(props["limit"] is int limit) || !(props["round"] is int round)) continue;
                if (title==null || title.IndexOf(filter.text,StringComparison.OrdinalIgnoreCase)<0) continue;
                var row=Instantiate(Resources.Load<GameObject>("Prefabs/UI/Panels/RoomRow"),root).transform;
                row.name="RoomRow"+count++;
                SetText(row,"RoomNameLabel",title);
                SetText(row,"RoomDetailsLabel",occupants+" / "+limit);
                SetText(row,"RoomStatusLabel",L(round==0?"net.waiting":"net.playing"));
                var join=row.GetComponentInChildren<Button>();
                SetText(join.transform,"TextLabel",L("net.join"));
                join.interactable=!session.Busy && occupants<limit;
                join.onClick.AddListener(()=>{ GameAudio.Current?.OnButtonGesture(); Join(info.Name); });
            }
            bool connected=PhotonNetwork.InLobby;
            root.gameObject.SetActive(count>0 && connected);
            FindNode("EmptyRoomsPanel").gameObject.SetActive(connected && count==0);
            FindNode("LoadingRoomsPanel").gameObject.SetActive(!connected && session.Busy);
            FindNode("RoomListErrorPanel").gameObject.SetActive(!connected && !session.Busy);
            ComponentOf<Button>("CreateRoomButton",Browser).interactable=connected && !session.Busy;
            ComponentOf<Button>("JoinPrivateRoomButton",Browser).interactable=connected && !session.Busy;
        }
        void RenderLobby()
        {
            SetText(FindNode("RoomSummaryPanel"),"TitleLabel",session.RoomTitle);
            SetText(FindNode("RoomSummaryPanel"),"RoomCodeLabel",session.Code);
            var r=session.Rules;
            SetText(FindNode("RoomSummaryPanel"),"ConfigurationLabel",L("game.rules",r.Health,r.Hand,r.Seconds,r.Reverses,r.Passes,r.Swaps));
            var players=FindNode("PlayersPanel");
            SetText(players,"TitleLabel",L("practice.players",session.Members.Count)+" / "+r.Players);
            for(int i=0;i<6;i++)
            {
                var row=FindNode("SeatRow"+i,players);
                row.gameObject.SetActive(i<session.Members.Count);
                if (i>=session.Members.Count) continue;
                int id=session.Members[i];
                var player=PhotonNetwork.CurrentRoom?.GetPlayer(id);
                string emblem=player?.CustomProperties["emblem"] as string;
                int index=Array.IndexOf(UserSettings.Emblems.ToArray(),emblem);
                seatPortraits[i].sprite=session.PlayerPortrait(id,emblemSprites[Math.Max(0,index)]);
                SetText(row,"NameLabel",OnlineSession.PlayerName(id));
                SetText(row,"StatusLabel",(i==0?L("net.host")+" · ":"")+L(session.ReadyMembers.Contains(id)?"net.ready":"net.waiting"));
            }
            FindNode("HostControls").gameObject.SetActive(session.Host);
            FindNode("ClientControls").gameObject.SetActive(true);
            ComponentOf<Button>("ReadyButton",Lobby).interactable=session.RoundId==0;
            ComponentOf<Button>("EditRoomButton",Lobby).interactable=session.RoundId==0;
            SetText(FindNode("ReadyButton"),"TextLabel",L(session.ReadyMembers.Contains(session.LocalId)?"unready":"ready.button"));
            RenderReadiness();
        }
        void RenderReadiness()
        {
            displayedSecond=session.Countdown==0?-1:Mathf.CeilToInt(OnlineSession.SecondsLeft(session.Countdown));
            SetText(FindNode(Lobby),"ReadinessLabel",displayedSecond<0?L("net.readiness",session.ReadyMembers.Count,session.Members.Count):L("net.countdown",displayedSecond));
        }
        OptionSelector Select(string id)=>ComponentOf<OptionSelector>(id,Form);
        void Edit()
        {
            editing=true; Show(Form);
            var r=session.Rules;
            ComponentOf<TMP_InputField>("RoomNameInputField").text=session.RoomTitle;
            Select("RoomPrivacySelector").Choose(session.IsPrivate?1:0);
            Select("PlayerCountSelector").Choose(r.Players-2); Select("StartingHandSelector").Choose(r.Hand-2);
            Select("StartingHealthSelector").Choose(r.Health-1); Select("TurnTimeSelector").Choose(r.Seconds-1);
            Select("ReverseSelector").Choose(r.Reverses); Select("PassSelector").Choose(r.Passes); Select("SwapSelector").Choose(r.Swaps);
        }
        void ConfirmForm()
        {
            int N(string id)=>int.Parse(Select(id).ValueText);
            int changes=N("SwapSelector"),passes=N("PassSelector"),reverses=N("ReverseSelector");
            if (changes+passes+reverses>8) { NotifyUser("game.limit"); return; }
            var rules=new MatchRules(N("PlayerCountSelector"),N("StartingHandSelector"),N("StartingHealthSelector"),N("TurnTimeSelector"),reverses,passes,changes);
            string title=ComponentOf<TMP_InputField>("RoomNameInputField").text.Trim();
            if (title.Length==0) { NotifyUser("net.title"); return; }
            bool hidden=Select("RoomPrivacySelector").index==1;
            if (editing)
            {
                if (rules.Players<session.Members.Count) { NotifyUser("net.capacity"); return; }
                editing=false; session.Edit(title,hidden,rules); Show(Lobby);
            }
            else session.Create(title,hidden,rules);
        }
    }
}
