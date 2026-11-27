using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace DonmniCrash.UI
{
    public sealed partial class OnlineSession
    {
        readonly Dictionary<int,Sprite> portraits = new Dictionary<int,Sprite>();
        public event Action PortraitsChanged;

        public Sprite PlayerPortrait(int actor,Sprite fallback)
        {
            if (!portraits.TryGetValue(actor,out var photo))
            {
                var player = PhotonNetwork.CurrentRoom?.GetPlayer(actor);
                photo = ProfilePhoto.Decode(player?.CustomProperties["photo"] as byte[]);
                portraits[actor] = photo;
            }
            return photo ? photo : fallback;
        }

        void ForgetPortrait(int actor)
        {
            if (portraits.TryGetValue(actor,out var old)) ProfilePhoto.Release(old);
            portraits.Remove(actor);
        }

        void ClearPortraits()
        {
            foreach (var portrait in portraits.Values) ProfilePhoto.Release(portrait);
            portraits.Clear();
        }
    }
}
