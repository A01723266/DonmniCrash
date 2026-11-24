using System.Linq;
using UnityEngine;

namespace DonmniCrash.EditorTools
{
    public static partial class DonmniCrashUIAuthoring
    {
        // Separate scrollable sections keep individual music attributions readable on narrow screens.
        static void CreateCredits(Shell shell)
        {
            var groups = new[]{"design","technology","graphics","font","effects","music","ai"};
            var entries = Resources.Load<TextAsset>("UI/Credits").text.TrimEnd('\r','\n').Split('\n');
            var menu = Dialog(shell.overlays,"CreditsDialog","Credits","DonmniCrash");
            foreach (string group in groups)
            {
                string heading = DonmniCrash.UI.LocalizedText.Read("credit."+group);
                Button("Category"+group,menu,heading);
                var page = Dialog(shell.overlays,"Credit"+group,heading,"DonmniCrash");
                for (int i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i].TrimEnd('\r').Split('\t');
                    if (entry[0] != group) continue;
                    if (entry[2].Length > 0) Button("CreditLink"+i,page,entry[1]);
                    else Size(Label("CreditText",page,DonmniCrash.UI.LocalizedText.Read(entry[1]),21),group=="ai"?165:85);
                }
                Button("BackButton",page,"Back");
            }
            Button("CloseButton",menu,"Close");
        }
    }
}
