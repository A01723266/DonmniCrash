using System.Linq;
using DonmniCrash.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.EditorTools
{
    public static partial class DonmniCrashUIAuthoring
    {
        static void AddPhotoButton(Transform parent)
        {
            var button = Button("ChoosePhotoButton",parent,"Choose photo");
            Localize(button.GetComponentInChildren<TMP_Text>(true),"profile.photo");
        }

        static void ActivityRows(Transform parent)
        {
            ActivityRow(parent,"TutorialButton","learn","tutorial.hint",elemental["Water"][1]);
            ActivityRow(parent,"BotConfigurationButton","bots","bots.hint",slab);
        }

        static void ActivityRow(Transform parent,string name,string title,string hint,Sprite sprite)
        {
            var surface = Panel(name,parent);
            var button = surface.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            Bind(surface,UIThemeRole.Panel,true);
            Size(surface,110);
            var row = Horizontal(surface.transform,16);
            row.padding = new RectOffset(22,22,10,10);
            row.childForceExpandHeight = false;
            Icon("ModeIcon",surface.transform,sprite,48);
            var copy = Node("Labels",surface.transform);
            Size(copy,-1,-1,1);
            Vertical(copy,0,2);
            Localize(Label("TitleLabel",copy,"",24,UIThemeRole.Text,true),title);
            var description = Label("DescriptionLabel",copy,"",18,UIThemeRole.SecondaryText);
            Size(description,46);
            Localize(description,hint);
            var arrow = Label("Arrow",surface.transform,"→",28,UIThemeRole.Accent,true);
            Size(arrow,42,32);
        }

        // Actualiza solamente los controles afectados, conservando los ajustes de cada escena.
        public static void ApplyMenuUpdate()
        {
            theme = AssetDatabase.LoadAssetAtPath<SessionAppearance>("Assets/Resources/UI/Aspecto.asset").palettes[0];
            regular = Font("Regular"); semibold = Font("SemiBold");
            rounded = AssetDatabase.LoadAssetAtPath<Sprite>(UI+"/Sprites/RoundedPanel.png");
            slab = AssetDatabase.LoadAssetAtPath<SessionAppearance>("Assets/Resources/UI/Aspecto.asset").slabs[0];
            LoadElementArt();
            foreach (var name in new[]{"Start","OfflineSetting","Tutorial"})
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var canvas = GameObject.Find("UICanvas");
                var nodes = canvas.GetComponentsInChildren<Transform>(true);
                foreach (var node in nodes.Where(n=>n.name=="PreviewButton" || n.name=="PreviewDialog" || n.name=="TutorialWelcomeDialog").ToArray())
                    if (node) Object.DestroyImmediate(node.gameObject);
                if (name=="Start")
                {
                    var options = nodes.FirstOrDefault(n=>n && n.name=="OptionsPanel");
                    var customize = ChildComponent<Button>(canvas,"CustomizationButton");
                    var settings = ChildComponent<Button>(canvas,"SettingsButton");
                    if (!options)
                    {
                        int index = customize.transform.GetSiblingIndex();
                        options = Row("OptionsPanel",customize.transform.parent);
                        options.SetSiblingIndex(index);
                    }
                    var row = options.GetComponent<HorizontalLayoutGroup>();
                    row.spacing = 12;
                    row.childForceExpandHeight = false;
                    foreach (var item in new[]{customize,settings})
                    {
                        item.transform.SetParent(options,false);
                        var size = item.GetComponent<LayoutElement>();
                        size.minWidth = size.preferredWidth = 0;
                        size.flexibleWidth = 1;
                    }
                    if (!nodes.Any(n=>n && n.name=="ChoosePhotoButton"))
                        AddPhotoButton(ChildComponent<Transform>(canvas,"AppearancePanel"));
                    foreach (var b in canvas.GetComponentsInChildren<Button>(true))
                        if (b.name.StartsWith("Category") || b.name.StartsWith("CreditLink"))
                            Bind(b.image,UIThemeRole.Panel,true);
                }
                var cards = nodes.FirstOrDefault(n=>n && n.name=="ActivityCardsPanel");
                if (cards) { ActivityRows(cards.parent); Object.DestroyImmediate(cards.gameObject); }
                foreach (var c in canvas.GetComponentsInChildren<Component>(true))
                    if (c && PrefabUtility.IsPartOfPrefabInstance(c))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                        if (c is Transform) PrefabUtility.RecordPrefabInstancePropertyModifications(c.gameObject);
                    }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
