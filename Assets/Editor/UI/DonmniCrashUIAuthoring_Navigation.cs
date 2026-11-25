using System.IO;
using System.Linq;
using DonmniCrash.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.EditorTools
{
    /// <summary>Serialize UI bindings with their scene objects and prefab instances.</summary>
    public static partial class DonmniCrashUIAuthoring
    {
        static GameObject PrefabInstance(string path, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + path + ".prefab");
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
        }

        static T ChildComponent<T>(GameObject root,string name) where T : Component =>
            root.GetComponentsInChildren<T>(true).First(c => c.name == name);

        static void PrepareServices()
        {
            const string audioDir = "Assets/Resources/Prefabs/Audio";
            Directory.CreateDirectory(audioDir);
            AssetDatabase.Refresh();
            const string path = "Assets/Resources/UI/Aspecto.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<SessionAppearance>(path);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<SessionAppearance>(); AssetDatabase.CreateAsset(catalog,path); }
            catalog.palettes = themes.ToArray();
            catalog.slabs = new[]{"Grey","Blue","Black"}.Select(c => Sprites(
                $"Assets/Kenney/UI/kenney_rune-pack/PNG/{c}/Slab (outline)/rune{c}_slabOutline_{(c == "Grey" ? "036" : "035")}.png").First()).ToArray();
            EditorUtility.SetDirty(catalog);
            // Reauthoring must preserve clips assigned to the existing audio prefab.
            string audioPath = audioDir + "/AudioJuego.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(audioPath)) return;
            var obj = new GameObject("AudioJuego");
            var service = obj.AddComponent<GameAudio>();
            service.music = Source(obj.transform,"Music",true);
            service.effects = Source(obj.transform,"Effects",false);
            AddPlacementAudio(service);
            PrefabUtility.SaveAsPrefabAsset(obj,audioPath);
            Object.DestroyImmediate(obj);
        }

        static AudioSource Source(Transform parent,string name,bool repeat)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent,false);
            var source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = repeat;
            source.spatialBlend = 0;
            return source;
        }

        static void CompleteSettings(RectTransform root)
        {
            var panel = root.gameObject.AddComponent<SettingsPanel>();
            var obj = root.gameObject;
            AddPlayAssist(panel);
            panel.general = ChildComponent<Transform>(obj,"MasterVolumeRow").GetComponentInChildren<Slider>(true);
            panel.music = ChildComponent<Transform>(obj,"MusicVolumeRow").GetComponentInChildren<Slider>(true);
            panel.effects = ChildComponent<Transform>(obj,"EffectsVolumeRow").GetComponentInChildren<Slider>(true);
            AddPlacementSlider(panel);
            var language = ChildComponent<Transform>(obj,"LanguageSelector").gameObject;
            panel.previous = ChildComponent<Button>(language,"PreviousButton");
            panel.next = ChildComponent<Button>(language,"NextButton");
            panel.language = ChildComponent<TextMeshProUGUI>(language,"ValueLabel");
            panel.palettes = ChildComponent<Transform>(obj,"ThemeGrid").GetComponentsInChildren<Button>(true);
            panel.paletteMarkers = panel.palettes.Select(b => ChildComponent<Transform>(b.gameObject,"SelectedLabel").gameObject).ToArray();
            var container = Card("TileStyle",root,"Tile style");
            var grid = Grid("TileGrid",container,98,3);
            grid.GetComponent<UIResponsiveGrid>().minimumCellWidth = 135;
            var catalog = AssetDatabase.LoadAssetAtPath<SessionAppearance>("Assets/Resources/UI/Aspecto.asset");
            panel.slabs = new Button[3];
            panel.slabMarkers = new GameObject[3];
            var names = new[]{"Grey","Blue","Black"};
            for (int i=0;i<3;i++)
            {
                var b = Button("TileStyle"+i,grid,names[i]);
                var labelText = ChildComponent<TextMeshProUGUI>(b.gameObject,"TextLabel");
                Place(labelText.rectTransform,Vector2.zero,new Vector2(1,.4f),Vector2.zero,Vector2.zero);
                var icon = Icon("Slab",b.transform,catalog.slabs[i]);
                Place(icon.rectTransform,new Vector2(.5f,.4f),new Vector2(.5f,1),new Vector2(-20,0),new Vector2(20,-3));
                var marker = Label("SelectedLabel",b.transform,"●",16,UIThemeRole.Accent);
                Place(marker.rectTransform,new Vector2(1,1),Vector2.one,new Vector2(-24,-24),Vector2.zero);
                panel.slabs[i] = b;
                panel.slabMarkers[i] = marker.gameObject;
                marker.gameObject.SetActive(i==0);
            }
        }

        static void WireScene(string name)
        {
            var canvas = GameObject.Find("UICanvas");
            var screens = canvas.transform.Find("SafeArea/Screens");
            var overlays = canvas.transform.Find("SafeArea/Overlays");
            switch (name)
            {
                case "Start":
                    canvas.AddComponent<StartMenu>();
                    var card = ChildComponent<Transform>(canvas,"PlayerPreviewCard");
                    var icon = Icon("ProfileEmblem",card,symbols["Earth"],32);
                    icon.transform.SetAsFirstSibling();
                    Size(card,122,164);
                    break;
                case "OnlineRooms": canvas.AddComponent<RoomBrowser>(); break;
                case "OfflineSetting": canvas.AddComponent<PracticeMenu>(); break;
                case "OnlineMatch": canvas.AddComponent<OnlineTableUI>(); break;
                case "OfflineMatch": canvas.AddComponent<PracticeTableUI>(); break;
                case "Tutorial": canvas.AddComponent<TutorialController>(); break;
            }
            foreach (var tile in canvas.GetComponentsInChildren<DominoTileView>(true))
            {
                if (!tile.name.StartsWith("HandDomino")) continue;
                var hitSurface = tile.gameObject.AddComponent<Image>();
                hitSurface.color = Color.clear;
                var b = tile.gameObject.AddComponent<Button>();
                b.targetGraphic = hitSurface;
                b.transition = Selectable.Transition.None;
            }
            if (name=="OnlineRooms" || name=="OfflineSetting") PrepareSelectors(canvas);
            // Live scenes expose gameplay and service state through their normal controls.
            foreach (var labelText in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                // Translate the placeholder and preserve player-entered text.
                var inputField = labelText.GetComponentInParent<TMP_InputField>(true);
                if (inputField && inputField.textComponent==labelText) continue;
                // The settings dismissal and lobby readiness labels have separate keys.
                var button = labelText.GetComponentInParent<Button>(true);
                string key = button && button.name == "SaveSettingsButton"
                    ? "save.settings"
                    : LocalizedText.KeyForText(labelText.text);
                if (key != null) TextBinding(labelText).key = key;
            }
            // Record per-instance names, keys, text and sprites
            // as overrides on the shared prefab instance.
            foreach (var component in canvas.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)))
            {
                if (!component || !PrefabUtility.IsPartOfPrefabInstance(component)) continue;
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                if (component is Transform) PrefabUtility.RecordPrefabInstancePropertyModifications(component.gameObject);
            }
        }

        static LocalizedText TextBinding(TMP_Text t) => t.GetComponent<LocalizedText>() ?? t.gameObject.AddComponent<LocalizedText>();

        static void PrepareSelectors(GameObject canvas)
        {
            void Options(string name,string[] values,int initial = 0)
            {
                var node = canvas.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name==name);
                if (!node) return;
                var selector = node.gameObject.AddComponent<OptionSelector>();
                selector.options = values;
                selector.index = initial;
                TextBinding(ChildComponent<TextMeshProUGUI>(node.gameObject,"ValueLabel"));
            }
            string[] RangeOf(int startTracks,int amount) => Enumerable.Range(startTracks,amount).Select(n => n.ToString()).ToArray();
            Options("RoomPrivacySelector",new[]{"Public","Private"});
            Options("PlayerCountSelector",RangeOf(2,5),canvas.GetComponent<PracticeMenu>() ? 2 : 0);
            foreach (string name in new[] {"ReverseSelector","PassSelector","SwapSelector"}) Options(name,RangeOf(0,9),1);
            Options("StartingHealthSelector",RangeOf(1,10),2);
            Options("StartingHandSelector",RangeOf(2,7),3);
            Options("TurnTimeSelector",RangeOf(1,10),4);
            Options("EffectsSelector",new[]{"On","Off"});
            Options("BotDifficultySelector",new[]{"Easy","Normal","Hard"},1);
        }

    }
}
