using UnityEngine;

namespace DonmniCrash.UI
{
    public sealed partial class StartMenu
    {
        int photoRequest;
        byte[] draftPhoto;
        Sprite draftPortrait;

        void SetPhoto(byte[] bytes, Sprite portrait)
        {
            ProfilePhoto.Release(draftPortrait);
            draftPhoto = bytes;
            draftPortrait = portrait;
        }

        void OnDestroy() => ProfilePhoto.Release(draftPortrait);

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void PickProfilePhoto(string target,int request);
#endif
        void ChoosePhoto()
        {
            ++photoRequest;
#if UNITY_WEBGL && !UNITY_EDITOR
            PickProfilePhoto(gameObject.name,photoRequest);
#elif UNITY_EDITOR
            var path = UnityEditor.EditorUtility.OpenFilePanelWithFilters("JPG / PNG","",new[]{"Images","jpg,jpeg,png"});
            if (path.Length == 0) return;
            var source = new Texture2D(2,2);
            try
            {
                if (new System.IO.FileInfo(path).Length > 32L*1024*1024 ||
                    !source.LoadImage(System.IO.File.ReadAllBytes(path))) { NotifyUser("profile.photo.error"); return; }
                float side = Mathf.Min(source.width,source.height);
                for (int n=128;n>=32;n/=2)
                {
                    var thumb = new Texture2D(n,n,TextureFormat.RGB24,false);
                    for (int y=0;y<n;y++) for (int x=0;x<n;x++)
                        thumb.SetPixel(x,y,source.GetPixelBilinear(.5f+((x+.5f)/n-.5f)*side/source.width,
                            .5f+((y+.5f)/n-.5f)*side/source.height));
                    thumb.Apply();
                    var data = thumb.EncodeToPNG();
                    Destroy(thumb);
                    if (data.Length > ProfilePhoto.ByteLimit) continue;
                    ApplyPhoto(data);
                    return;
                }
                NotifyUser("profile.photo.error");
            }
            catch (System.IO.IOException) { NotifyUser("profile.photo.error"); }
            finally { Destroy(source); }
#endif
        }

        [UnityEngine.Scripting.Preserve]
        public void ReceiveProfilePhoto(string result)
        {
            int split = result.IndexOf(':');
            if (ScreenName!="CustomizationScreen" || split<0 ||
                !int.TryParse(result.Substring(0,split),out int request) || request!=photoRequest) return;
            ApplyPhoto(ProfilePhoto.FromBase64(result.Substring(split+1)));
        }

        void ApplyPhoto(byte[] bytes)
        {
            var portrait = ProfilePhoto.Decode(bytes);
            if (!portrait) { NotifyUser("profile.photo.error"); return; }
            SetPhoto(bytes,portrait);
            PreviewProfile();
        }
    }
}
