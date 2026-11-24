using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    // The waiting room and match can both receive an application. Keep the displayed actor
    // attached to the dialog so a disappearing applicant can never approve the next name by accident.
    internal sealed class RoomDialogs
    {
        readonly OnlineSession session;
        readonly Action<string> open;
        readonly Action close;
        readonly Transform admission;
        readonly TMP_Text description;
        int applicant;

        public RoomDialogs(OnlineSession session,Transform admission,Transform confirmation,Action<string> open,Action close)
        {
            this.session=session; this.admission=admission; this.open=open; this.close=close;
            description=admission.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="DescriptionLabel");
            void Bind(Transform root,string name,Action action) =>
                root.GetComponentsInChildren<Button>(true).First(b=>b.name==name).onClick.AddListener(()=>action());
            Bind(admission,"AcceptButton",()=>Resolve(true));
            Bind(admission,"RejectButton",()=>Resolve(false));
            Bind(confirmation,"StayButton",close);
            Bind(confirmation,"CloseRoomButton",session.CloseRoom);
        }

        void Resolve(bool accept)
        {
            int id=applicant; applicant=0; close(); session.Resolve(id,accept);
        }

        public void Refresh(bool canOpen)
        {
            if (applicant!=0 && (!session.Host || !session.Applicants.Contains(applicant)))
            {
                if (admission.gameObject.activeSelf) close();
                applicant=0;
            }
            if (!canOpen || applicant!=0 || !session.Host || session.Applicants.Count==0) return;
            applicant=session.Applicants[0];
            open("AdmissionRequestDialog");
            description.richText=false;
            string message=string.Format(LocalizedText.Read("net.applicant"),OnlineSession.PlayerName(applicant));
            var label=description.GetComponent<LocalizedText>();
            if (label) label.Literal(message); else description.text=message;
        }
    }
}
