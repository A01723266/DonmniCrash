using UnityEngine;

namespace DonmniCrash.UI
{
    public sealed partial class StartMenu
    {
        // Only packaged credit URLs are opened, directly from a user gesture.
        void BindCredits()
        {
            BindButton(InitialScreen,"CreditsButton",()=>Open("CreditsDialog"));
            BindButton("CreditsDialog","CloseButton",Close);
            foreach (string group in new[]{"design","technology","graphics","font","effects","music","ai"})
            {
                string page = "Credit"+group;
                BindButton("CreditsDialog","Category"+group,()=>Open(page));
                BindButton(page,"BackButton",Close);
            }
            var lines = Resources.Load<TextAsset>("UI/Credits").text.TrimEnd('\r','\n').Split('\n');
            for (int i=0; i<lines.Length;i++)
            {
                var entry = lines[i].TrimEnd('\r').Split('\t');
                if (entry[2].StartsWith("https://",System.StringComparison.Ordinal))
                    BindButton("Credit"+entry[0],"CreditLink"+i,()=>Application.OpenURL(entry[2]));
            }
        }
    }
}
