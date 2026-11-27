using System;
using UnityEngine;

namespace DonmniCrash.UI
{
    internal static class ProfilePhoto
    {
        public const int ByteLimit = 16 * 1024;
        const string Key = "DonmniCrash.v1.profile.photo";

        public static byte[] Saved => FromBase64(PlayerPrefs.GetString(Key,""));
        public static byte[] FromBase64(string data)
        {
            if (string.IsNullOrEmpty(data) || data.Length > 21848) return null;
            try { return Convert.FromBase64String(data); }
            catch (FormatException) { return null; }
        }

        public static void Store(byte[] bytes)
        {
            if (bytes == null) PlayerPrefs.DeleteKey(Key);
            else PlayerPrefs.SetString(Key,Convert.ToBase64String(bytes));
        }

        public static Sprite Decode(byte[] bytes)
        {
            // Limitar las dimensiones antes de pedir memoria al decodificador de Unity.
            if (bytes == null || bytes.Length < 33 || bytes.Length > ByteLimit) return null;
            byte[] header = {137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82};
            for (int i=0;i<header.Length;i++) if (bytes[i]!=header[i]) return null;
            int size = bytes[19];
            if (size == 0 || size > 128 || bytes[23]!=size ||
                (bytes[16]|bytes[17]|bytes[18]|bytes[20]|bytes[21]|bytes[22])!=0) return null;
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            if (texture.LoadImage(bytes,true))
                return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f));
            UnityEngine.Object.Destroy(texture);
            return null;
        }

        public static void Release(Sprite sprite)
        {
            if (!sprite) return;
            UnityEngine.Object.Destroy(sprite.texture);
            UnityEngine.Object.Destroy(sprite);
        }
    }
}
