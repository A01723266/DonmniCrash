using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using DonmniCrash.UI;
using Object = UnityEngine.Object;

namespace DonmniCrash.EditorTools
{
    public static partial class DonmniCrashUIAuthoring
    {
        const string UI = "Assets/UI";
        const string Prefabs = "Assets/Resources/Prefabs/UI";
        static TMP_FontAsset regular, medium, semibold;
        static UITheme theme;
        static readonly List<UITheme> themes = new List<UITheme>();
        static Sprite rounded, slab;
        static readonly string[] elements = { "Earth", "Water", "Wind", "Fire", "Plant" };
        static readonly string[] elementNames = { "Earth", "Water", "Wind", "Fire", "Plant" };
        static readonly Dictionary<string, Sprite[]> elemental = new Dictionary<string, Sprite[]>();
        static readonly Dictionary<string, Sprite> symbols = new Dictionary<string, Sprite>();
        static bool library;

        [MenuItem("DonmniCrash/Author visual UI scenes")]
        public static void Build() => AuthorScenes(false);

        // Author only the three practice scenes.
        public static void AuthorPracticePhase() => AuthorScenes(true);

        public static void AuthorOnlinePhase() => AuthorScenes(true,true);

        static void AuthorScenes(bool practiceOnly,bool onlineOnly = false)
        {
            foreach (string dir in new[] { UI, UI + "/Fonts", UI + "/Themes", UI + "/Sprites", Prefabs, Prefabs + "/Controls", Prefabs + "/Panels", Prefabs + "/Gameplay", Prefabs + "/Overlays" }) Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            if (practiceOnly)
            {
                themes.Clear();
                themes.AddRange(AssetDatabase.LoadAssetAtPath<SessionAppearance>("Assets/Resources/UI/Aspecto.asset").palettes);
                theme = themes[0];
                rounded = AssetDatabase.LoadAssetAtPath<Sprite>(UI+"/Sprites/RoundedPanel.png");
            }
            else { CreateThemes(); CreateRoundedSprite(); }
            regular = Font("Regular"); medium = Font("Medium"); semibold = Font("SemiBold");
            slab = Sprites("Assets/Kenney/UI/kenney_rune-pack/PNG/Grey/Slab (outline)/runeGrey_slabOutline_036.png").First();
            foreach (var element in elements)
            {
                elemental[element] = Sprites("Assets/Sprites/" + element + "Domino.png").OrderByDescending(x => x.rect.width * x.rect.height).Take(2).OrderBy(x => x.rect.x).ToArray();
                symbols[element] = Sprites("Assets/Sprites/" + element + ".png").OrderByDescending(x => x.rect.width * x.rect.height).FirstOrDefault();
            }
            if (onlineOnly)
            {
                StartScene(); RoomsScene(); MatchScene("OnlineMatch",false,false);
                AssetDatabase.SaveAssets();
                return;
            }
            if (!practiceOnly)
            {
                PrepareServices();
                CreateLibrary();
                StartScene(); RoomsScene();
                MatchScene("OnlineMatch", false, false);
            }
            OfflineSettingsScene();
            MatchScene("OfflineMatch", true, false);
            MatchScene("Tutorial", true, true);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/Start.unity");
            Debug.Log("DonmniCrash visual UI authoring finished.");
        }

        static Sprite[] Sprites(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        static Color Hex(string value) { ColorUtility.TryParseHtmlString(value, out var c); return c; }
        static void CreateThemes()
        {
            themes.Clear();
            string[][] definitions = {
                new[]{"SlateGold", "Slate and gold", "#171D24", "#28323E", "#F3EFE5", "#D2AA60"},
                new[]{"PetrolIvory", "Teal and ivory", "#102629", "#203B3D", "#F3EBD9", "#CEBE99"},
                new[]{"BlueCopper", "Blue and copper", "#142131", "#24384E", "#F0F2F5", "#D79B77"},
                new[]{"PlumSilver", "Plum and silver", "#251F2B", "#3A3042", "#F1EDF3", "#C5C3CF"},
                new[]{"ForestParchment", "Forest and parchment", "#17251F", "#293D32", "#F2EAD7", "#D3BC89"}
            };
            foreach (var d in definitions)
            {
                string path = UI + "/Themes/" + d[0] + ".asset";
                var t = AssetDatabase.LoadAssetAtPath<UITheme>(path);
                if (!t) { t = ScriptableObject.CreateInstance<UITheme>(); AssetDatabase.CreateAsset(t, path); }
                t.displayName=d[1]; t.background=Hex(d[2]); t.panel=Hex(d[3]); t.text=Hex(d[4]); t.accent=Hex(d[5]);
                t.elevated=Color.Lerp(t.panel,t.text,.075f); t.secondaryText=Color.Lerp(t.text,t.panel,.27f);
                t.onAccent=Hex("#172029"); t.border=Color.Lerp(t.panel,t.text,.24f); t.focus=t.accent;
                t.highlighted=Color.Lerp(t.accent,Color.white,.15f); t.pressed=Color.Lerp(t.accent,Color.black,.18f); t.disabled=Color.Lerp(t.panel,t.text,.14f);
                EditorUtility.SetDirty(t); themes.Add(t);
            }
            theme=themes[0];
        }
        static TMP_FontAsset Font(string weight)
        {
            string path=UI+"/Fonts/Fredoka-"+weight+" SDF.asset";
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font) return font;
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Fredoka/static/Fredoka-"+weight+".ttf");
            font=TMP_FontAsset.CreateFontAsset(source,64,7,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
            font.name="Fredoka-"+weight+" SDF";
            font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 áéíóúüñÁÉÍÓÚÜÑ¿?¡!.,:;+-/%()[]#•—→←×✓",out string missing);
            AssetDatabase.CreateAsset(font,path);
            font.material.name=font.name+" Material";
            AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var atlas in font.atlasTextures) { atlas.name=font.name+" Atlas"; AssetDatabase.AddObjectToAsset(atlas,font); }
            EditorUtility.SetDirty(font);
            return font;
        }
        static void CreateRoundedSprite()
        {
            string path=UI+"/Sprites/RoundedPanel.png";
            if (!File.Exists(path))
            {
                var texture=new Texture2D(48,48,TextureFormat.RGBA32,false);
                for(int y=0;y<48;y++) for(int x=0;x<48;x++)
                {
                    float dx=Mathf.Max(Mathf.Abs(x-23.5f)-13.5f,0), dy=Mathf.Max(Mathf.Abs(y-23.5f)-13.5f,0);
                    texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(10-Mathf.Sqrt(dx*dx+dy*dy))));
                }
                texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.spriteBorder=new Vector4(14,14,14,14); importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
            importer.SaveAndReimport(); rounded=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static RectTransform Node(string name,Transform parent=null)
        {
            var go=new GameObject(name,typeof(RectTransform));
            if(parent) go.transform.SetParent(parent,false);
            return (RectTransform)go.transform;
        }
        static void Stretch(RectTransform r,float left=0,float bottom=0,float right=0,float top=0)
        { r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top); }
        static void Place(RectTransform r,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        { r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax; }
        static UIThemeBinding Bind(Graphic graphic,UIThemeRole role,bool selectable=false)
        {
            var b=graphic.GetComponent<UIThemeBinding>()??graphic.gameObject.AddComponent<UIThemeBinding>();
            b.theme=theme;b.role=role;b.tintSelectable=selectable;b.Apply();return b;
        }
        static Image Panel(string name,Transform parent,UIThemeRole role=UIThemeRole.Panel)
        {
            var r=Node(name,parent);var image=r.gameObject.AddComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;
            Bind(image,role); return image;
        }
        static LayoutElement Size(Component c,float height=-1,float width=-1,float flexible=0)
        {
            var l=c.GetComponent<LayoutElement>()??c.gameObject.AddComponent<LayoutElement>();
            if(height>=0){l.minHeight=height;l.preferredHeight=height;} if(width>=0){l.minWidth=width;l.preferredWidth=width;}
            l.flexibleWidth=flexible;return l;
        }
        static VerticalLayoutGroup Vertical(Transform parent,int padding=0,float spacing=12)
        {
            var g=parent.gameObject.AddComponent<VerticalLayoutGroup>();g.padding=new RectOffset(padding,padding,padding,padding);
            g.spacing=spacing;g.childControlHeight=true;g.childControlWidth=true;g.childForceExpandHeight=false;g.childForceExpandWidth=true;return g;
        }
        static HorizontalLayoutGroup Horizontal(Transform parent,float spacing=12)
        {
            var g=parent.gameObject.AddComponent<HorizontalLayoutGroup>();g.spacing=spacing;g.childControlHeight=true;g.childControlWidth=true;
            g.childForceExpandHeight=true;g.childForceExpandWidth=false;g.childAlignment=TextAnchor.MiddleLeft;return g;
        }
        static RectTransform Row(string name,Transform parent,float height=52)
        { var r=Node(name,parent);Size(r,height);Horizontal(r);return r; }
        static TextMeshProUGUI Label(string name,Transform parent,string value,float size=22,UIThemeRole role=UIThemeRole.Text,bool bold=false)
        {
            var r=Node(name,parent);var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.font=bold?semibold:regular;text.text=value;
            text.fontSize=size;text.color=theme.Get(role);text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;
            text.overflowMode=TextOverflowModes.Ellipsis;Bind(text,role);Size(text,size*1.5f);return text;
        }
        static Button Button(string name,Transform parent,string text,bool primary=false,float width=-1)
        {
            string path=Prefabs+(primary?"/Controls/PrimaryButton.prefab":"/Controls/SecondaryButton.prefab");
            if(!library && AssetDatabase.LoadAssetAtPath<GameObject>(path))
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);go.name=name;
                var button=go.GetComponent<Button>();var label=go.GetComponentInChildren<TextMeshProUGUI>();label.text=text;
                Bind(button.image,primary?UIThemeRole.Accent:UIThemeRole.Elevated,true);Bind(label,primary?UIThemeRole.OnAccent:UIThemeRole.Text);
                Size(button,52,width,width<0?1:0);return button;
            }
            var image=Panel(name,parent,primary?UIThemeRole.Accent:UIThemeRole.Elevated);var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;
            b.navigation=new Navigation{mode=Navigation.Mode.None};Bind(image,primary?UIThemeRole.Accent:UIThemeRole.Elevated,true);Size(b,52,width,width<0?1:0);
            var t=Label("TextLabel",image.transform,text,21,primary?UIThemeRole.OnAccent:UIThemeRole.Text,true);Stretch(t.rectTransform,16,6,16,6);t.alignment=TextAlignmentOptions.Center;
            return b;
        }
        static void Divider(Transform parent)
        {var p=Panel("Divider",parent,UIThemeRole.Border);p.sprite=null;p.raycastTarget=false;Size(p,1);}
        static Image Icon(string name,Transform parent,Sprite sprite,float size=32)
        {var r=Node(name,parent);var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;Size(image,size,size);return image;}
        static RectTransform Scroll(string name,Transform parent,bool horizontal=false)
        {
            var root=Node(name,parent);var sr=root.gameObject.AddComponent<ScrollRect>();sr.horizontal=horizontal;sr.vertical=!horizontal;sr.movementType=ScrollRect.MovementType.Clamped;sr.scrollSensitivity=24;
            var viewport=Node("Viewport",root);Stretch(viewport);var bg=viewport.gameObject.AddComponent<Image>();bg.color=new Color(0,0,0,.001f);viewport.gameObject.AddComponent<RectMask2D>();
            var content=Node("Content",viewport);
            if(horizontal)
            {
                content.anchorMin=new Vector2(0,0);content.anchorMax=new Vector2(0,1);content.pivot=new Vector2(0,.5f);content.sizeDelta=Vector2.zero;
                Horizontal(content,12).padding=new RectOffset(8,8,4,4);
                content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit=ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
                Vertical(content,4,14);content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            }
            sr.viewport=viewport;sr.content=content;return content;
        }
        static RectTransform Card(string name,Transform parent,string title)
        {
            var p=Panel(name,parent);Vertical(p.transform,22,12);p.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            Label("TitleLabel",p.transform,title,24,UIThemeRole.Text,true);return p.rectTransform;
        }
        static TMP_InputField Input(string name,Transform parent,string placeholder,string value="")
        {
            if (!library)
            {
                var obj = PrefabInstance("Controls/TextInputField",parent);
                obj.name = name;
                var field = obj.GetComponent<TMP_InputField>();
                field.SetTextWithoutNotify(value);
                ((TMP_Text)field.placeholder).text = placeholder;
                return field;
            }
            var p=Panel(name,parent,UIThemeRole.Elevated);Size(p,54);
            var input=p.gameObject.AddComponent<TMP_InputField>();input.targetGraphic=p;Bind(p,UIThemeRole.Elevated,true);
            var area=Node("TextArea",p.transform);Stretch(area,16,6,16,6);area.gameObject.AddComponent<RectMask2D>();
            var text=Label("ValueLabel",area,value,21);Stretch(text.rectTransform);text.alignment=TextAlignmentOptions.MidlineLeft;
            var hint=Label("PlaceholderLabel",area,placeholder,21,UIThemeRole.SecondaryText);Stretch(hint.rectTransform);hint.alignment=TextAlignmentOptions.MidlineLeft;
            input.textViewport=area;input.textComponent=text;input.placeholder=hint;input.fontAsset=regular;input.text=value;input.characterLimit=28;
            input.customCaretColor=true;input.caretColor=theme.accent;input.selectionColor=new Color(theme.accent.r,theme.accent.g,theme.accent.b,.3f);return input;
        }
        static void Selector(Transform parent,string name,string label,string value)
        {
            if (!library)
            {
                var obj = PrefabInstance("Controls/ValueSelector",parent);
                obj.name = name;
                ChildComponent<TextMeshProUGUI>(obj,"FieldLabel").text = label;
                ChildComponent<TextMeshProUGUI>(obj,"ValueLabel").text = value;
                return;
            }
            var r=Row(name,parent,52);var l=Label("FieldLabel",r,label,20);Size(l,-1,-1,1);
            Button("PreviousButton",r,"−",false,48);var v=Label("ValueLabel",r,value,20);Size(v,-1,140);v.alignment=TextAlignmentOptions.Center;Button("NextButton",r,"+",false,48);
        }
        static void SliderRow(Transform parent,string name,string title,float value)
        {
            var row=Row(name,parent,52);var label=Label("FieldLabel",row,title,20);Size(label,-1,160);
            var r=Node("VolumeSlider",row);Size(r,44,-1,1);var slider=r.gameObject.AddComponent<Slider>();
            var track=Panel("Track",r,UIThemeRole.Border);Place(track.rectTransform,new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(10,-3),new Vector2(-10,3));
            var fillArea=Node("FillArea",r);Stretch(fillArea,10,19,10,19);var fill=Panel("Fill",fillArea,UIThemeRole.Accent);Stretch(fill.rectTransform);
            var handleArea=Node("HandleArea",r);Stretch(handleArea,12,0,12,0);var handle=Panel("Handle",handleArea,UIThemeRole.Text);handle.rectTransform.sizeDelta=new Vector2(22,22);
            slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.value=value;
        }

        sealed class Shell { public RectTransform screens,overlays; public GameObject canvas; }
        static Shell Scene(string name,bool match=false)
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
            // Preserve the user's camera and scene objects when replacing the generated UI.
            foreach(var root in scene.GetRootGameObjects()) if(new[]{"UICanvas","EventSystem","BoardRoot"}.Contains(root.name)) Object.DestroyImmediate(root);
            var c=Node("UICanvas");var canvas=c.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
            var scaler=c.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            c.gameObject.AddComponent<GraphicRaycaster>();
            var background=Panel("Background",c,UIThemeRole.Background);Stretch(background.rectTransform);background.raycastTarget=false;
            if(match) {background.color=new Color(0,0,0,.14f);Object.DestroyImmediate(background.GetComponent<UIThemeBinding>());Board();}
            var safe=Node("SafeArea",c);Stretch(safe);safe.gameObject.AddComponent<UISafeArea>();
            var screens=Node("Screens",safe);Stretch(screens);var overlays=Node("Overlays",safe);Stretch(overlays);
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return new Shell{screens=screens,overlays=overlays,canvas=c.gameObject};
        }
        static RectTransform Screen(Shell shell,string name,string title,string subtitle,bool active=true)
        {
            var root=Node(name,shell.screens);Stretch(root);var frame=Node("ContentPanel",root);
            Place(frame,new Vector2(.5f,0),new Vector2(.5f,1),new Vector2(-510,28),new Vector2(510,-28));
            var constraint=frame.gameObject.AddComponent<UIWidthConstraint>();constraint.maximumWidth=1040;constraint.horizontalMargin=24;
            var header=Node("HeaderPanel",frame);Place(header,new Vector2(0,1),Vector2.one,new Vector2(0,-90),Vector2.zero);
            var back=Button("BackButton",header,"Back",false,100);Place((RectTransform)back.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(0,-48),new Vector2(100,0));
            var heading=Label("TitleLabel",header,title,32,UIThemeRole.Text,true);Place(heading.rectTransform,new Vector2(0,1),new Vector2(1,1),new Vector2(120,-44),new Vector2(0,0));
            var sub=Label("SubtitleLabel",header,subtitle,18,UIThemeRole.SecondaryText);Place(sub.rectTransform,new Vector2(0,0),new Vector2(1,0),new Vector2(0,4),new Vector2(0,35));
            var content=Scroll("ContentScrollView",frame);Stretch((RectTransform)content.parent.parent,0,0,0,100);
            root.gameObject.SetActive(active);return content;
        }
        static RectTransform Grid(string name,Transform parent,float height=150,int columns=2)
        {
            var r=Node(name,parent);var grid=r.gameObject.AddComponent<GridLayoutGroup>();grid.spacing=new Vector2(16,16);grid.childAlignment=TextAnchor.UpperLeft;
            var responsive=r.gameObject.AddComponent<UIResponsiveGrid>();responsive.maximumColumns=columns;responsive.cellHeight=height;responsive.minimumCellWidth=columns==5?160:320;
            r.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;return r;
        }
        static void Save(string name) {WireScene(name);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Scenes/"+name+".unity");}
        static void SavePrefab(GameObject go,string path) => PrefabUtility.SaveAsPrefabAsset(go,Prefabs+"/"+path+".prefab");
        static RectTransform Dialog(Transform parent,string name,string title,string description,bool active=false,float maxWidth=560)
        {
            if (!library)
            {
                var obj = PrefabInstance("Overlays/ModalDialog",parent);
                obj.name = name;
                ChildComponent<TextMeshProUGUI>(obj,"TitleLabel").text = title;
                ChildComponent<TextMeshProUGUI>(obj,"DescriptionLabel").text = description;
                ChildComponent<UIWidthConstraint>(obj,"WindowPanel").maximumWidth = maxWidth;
                // The caller supplies the actions for this dialog.
                ChildComponent<Button>(obj,"CancelButton").gameObject.SetActive(false);
                ChildComponent<Button>(obj,"ConfirmButton").gameObject.SetActive(false);
                ChildComponent<Button>(obj,"CancelButton").name = "TemplateCancel";
                ChildComponent<Button>(obj,"ConfirmButton").name = "TemplateConfirm";
                obj.SetActive(active);
                return (RectTransform)ChildComponent<Transform>(obj,"Content");
            }
            var root=Node(name,parent);Stretch(root);
            var dim=root.gameObject.AddComponent<Image>();dim.color=new Color(0,0,0,.72f);
            var panel=Panel("WindowPanel",root);Place(panel.rectTransform,new Vector2(.5f,.12f),new Vector2(.5f,.88f),new Vector2(-280,0),new Vector2(280,0));
            var limit=panel.gameObject.AddComponent<UIWidthConstraint>();limit.maximumWidth=maxWidth;limit.horizontalMargin=24;
            var content=Scroll("DialogScrollView",panel.transform);Stretch((RectTransform)content.parent.parent,22,22,22,22);
            Label("TitleLabel",content,title,28,UIThemeRole.Text,true);
            var descriptionLabel=Label("DescriptionLabel",content,description,20,UIThemeRole.SecondaryText);Size(descriptionLabel,64);
            root.gameObject.SetActive(active);return content;
        }
        static void CommonOverlays(Shell shell)
        {
            if (!library)
            {
                foreach (string name in new[]{"MessageDialog","ConfirmationDialog","LoadingOverlay","ToastPanel","ElementHelpDialog","OrientationOverlay"})
                    PrefabInstance("Overlays/"+name,shell.overlays);
                PrefabInstance("Overlays/TransitionOverlay",shell.canvas.transform);
                return;
            }
            var message=Dialog(shell.overlays,"MessageDialog","Notice","Match messages appear here.");Button("AcceptButton",message,"Accept",true);
            var confirm=Dialog(shell.overlays,"ConfirmationDialog","Would you like to continue?","Cancel to stay on this screen.");
            var actions=Row("ActionsPanel",confirm);Button("CancelButton",actions,"Cancel");Button("ConfirmButton",actions,"Confirm",true);
            var loading=Dialog(shell.overlays,"LoadingOverlay","One moment…","Preparing the next screen.");Label("LoadingLabel",loading,"Loading…",24);
            var toast=Panel("ToastPanel",shell.overlays,UIThemeRole.Elevated);Place(toast.rectTransform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-220,28),new Vector2(220,86));
            var toastLabel=Label("MessageLabel",toast.transform,"Changes ready",19);Stretch(toastLabel.rectTransform,16,8,16,8);toastLabel.alignment=TextAlignmentOptions.Center;toast.gameObject.SetActive(false);
            var help=Dialog(shell.overlays,"ElementHelpDialog","The five elements","Each tile has an attack and a defense.",false,680);
            for(int i=0;i<elements.Length;i++)
            {
                var r=Row(elements[i]+"HelpRow",help,58);Icon("ElementIcon",r,symbols[elements[i]],44);var label=Label("ElementLabel",r,elementNames[i],21);Size(label,-1,-1,1);
                Icon("AttackIcon",r,elemental[elements[i]][0],40);Icon("DefenseIcon",r,elemental[elements[i]][1],40);
            }
            Button("CloseButton",help,"Got it",true);
            var orientation=Node("OrientationOverlay",shell.overlays);Stretch(orientation);var hint=orientation.gameObject.AddComponent<UIOrientationHint>();
            var portrait=Panel("PortraitPanel",orientation,UIThemeRole.Background);Stretch(portrait.rectTransform);
            var t=Label("OrientationLabel",portrait.transform,"Gira tu dispositivo\npara jugar en horizontal",28,UIThemeRole.Text,true);Stretch(t.rectTransform,32,64,32,64);t.alignment=TextAlignmentOptions.Center;
            hint.portraitPanel=portrait.gameObject;portrait.gameObject.SetActive(false);
            var transition=Panel("TransitionOverlay",shell.canvas.transform,UIThemeRole.Background);Stretch(transition.rectTransform);transition.gameObject.AddComponent<CanvasGroup>();transition.gameObject.SetActive(false);
        }
        static void Settings(Transform parent,bool compact=false)
        {
            if (!library) { PrefabInstance("Panels/SettingsPanel",parent); return; }
            var audio=Card("AudioPanel",parent,"Audio");SliderRow(audio,"MasterVolumeRow","Master volume",.8f);SliderRow(audio,"MusicVolumeRow","Music",.55f);SliderRow(audio,"EffectsVolumeRow","Effects",.75f);
            var language=Card("LanguagePanel",parent,"Language");Selector(language,"LanguageSelector","Game language","Español");
            var appearance=Card("AppearancePanel",parent,"Appearance");Label("ThemeHintLabel",appearance,"Interface theme",18,UIThemeRole.SecondaryText);
            var grid=Grid("ThemeGrid",appearance,124,compact?2:5);
            for(int i=0;i<themes.Count;i++)
            {
                if (i > 0)
                {
                    var obj = PrefabInstance("Controls/ThemeSwatchButton",grid);
                    obj.name = "Theme"+(i+1)+"Button";
                    var samples = ChildComponent<Transform>(obj,"ColorPreview");
                    var tones = new[]{themes[i].background,themes[i].panel,themes[i].accent,themes[i].text};
                    for (int j=0;j<tones.Length;j++) samples.GetChild(j).GetComponent<Image>().color = tones[j];
                    ChildComponent<TextMeshProUGUI>(obj,"ThemeNameLabel").text = themes[i].displayName;
                    ChildComponent<Transform>(obj,"SelectedLabel").gameObject.SetActive(false);
                    foreach (var component in obj.GetComponentsInChildren<Component>(true))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                        if (component is Transform) PrefabUtility.RecordPrefabInstancePropertyModifications(component.gameObject);
                    }
                    continue;
                }
                var swatch=Panel("Theme"+(i+1)+"Button",grid,UIThemeRole.Elevated);var b=swatch.gameObject.AddComponent<Button>();b.targetGraphic=swatch;Bind(swatch,UIThemeRole.Elevated,true);
                var colors=Node("ColorPreview",swatch.transform);Place(colors,new Vector2(0,.48f),new Vector2(1,1),new Vector2(12,0),new Vector2(-12,-12));Horizontal(colors,0);
                foreach(var color in new[]{themes[i].background,themes[i].panel,themes[i].accent,themes[i].text})
                {var p=Panel("ColorSwatch",colors);Object.DestroyImmediate(p.GetComponent<UIThemeBinding>());p.color=color;p.raycastTarget=false;Size(p,-1,-1,1);}
                var title=Label("ThemeNameLabel",swatch.transform,themes[i].displayName,17);Place(title.rectTransform,Vector2.zero,new Vector2(1,.46f),new Vector2(12,6),new Vector2(-12,0));title.alignment=TextAlignmentOptions.Center;
                {var selected=Label("SelectedLabel",swatch.transform,"●",16,UIThemeRole.Accent);Place(selected.rectTransform,new Vector2(1,1),Vector2.one,new Vector2(-20,-20),new Vector2(0,0));selected.gameObject.SetActive(i==0);}
                if(library && i==0) PrefabUtility.SaveAsPrefabAssetAndConnect(swatch.gameObject,Prefabs+"/Controls/ThemeSwatchButton.prefab",InteractionMode.AutomatedAction);
            }
        }
        static void CreateLibrary()
        {
            library=true;
            var root=Node("UIAuthoringLibrary");
            SavePrefab(Button("PrimaryButton",root,"Continue",true).gameObject,"Controls/PrimaryButton");
            SavePrefab(Button("SecondaryButton",root,"Back").gameObject,"Controls/SecondaryButton");
            SavePrefab(Input("TextInputField",root,"Type here").gameObject,"Controls/TextInputField");
            var selector=Node("ValueSelector",root);Vertical(selector);Selector(selector,"SelectorRow","Settings","Value");SavePrefab(selector.gameObject,"Controls/ValueSelector");
            var tile=Tile(root,0,1);SavePrefab(tile.gameObject,"Gameplay/DominoTileView");
            var player=Player(root,"PlayerCard","Player",false);SavePrefab(player.gameObject,"Gameplay/PlayerCard");
            var room=Room(root,"RoomRow","Sample room","Public · 2 / 6","Waiting");SavePrefab(room.gameObject,"Panels/RoomRow");
            var settings=Node("SettingsPanel",root);Vertical(settings);Settings(settings);CompleteSettings(settings);SavePrefab(settings.gameObject,"Panels/SettingsPanel");
            var modal=Dialog(root,"ModalDialog","Title","Description");Button("CancelButton",modal,"Cancel");Button("ConfirmButton",modal,"Confirm",true);
            SavePrefab(modal.parent.parent.parent.parent.gameObject,"Overlays/ModalDialog");
            var overlayRoot=Node("OverlayLibrary",root);var dummyCanvas=Node("CanvasLibrary",root);
            CommonOverlays(new Shell{overlays=overlayRoot,canvas=dummyCanvas.gameObject});
            foreach(Transform child in overlayRoot)SavePrefab(child.gameObject,"Overlays/"+child.name);
            SavePrefab(dummyCanvas.GetChild(0).gameObject,"Overlays/TransitionOverlay");
            Object.DestroyImmediate(root.gameObject);library=false;
        }
        static void StartScene()
        {
            var shell=Scene("Start");var screen=Node("ModeSelectionScreen",shell.screens);Stretch(screen);
            var layout=Node("CenteredLayout",screen);Place(layout,new Vector2(.5f,0),new Vector2(.5f,1),new Vector2(-230,24),new Vector2(230,-24));
            var constraint=layout.gameObject.AddComponent<UIWidthConstraint>();constraint.maximumWidth=460;constraint.horizontalMargin=24;
            var group=Vertical(layout,12,14);group.childAlignment=TextAnchor.MiddleCenter;
            var eyebrow=Label("EyebrowLabel",layout,"ELEMENTAL DOMINOES",16,UIThemeRole.Accent);eyebrow.alignment=TextAlignmentOptions.Center;
            var title=Label("GameTitleLabel",layout,"DonmniCrash",48,UIThemeRole.Text,true);title.alignment=TextAlignmentOptions.Center;Size(title,72);
            var sub=Label("SubtitleLabel",layout,"Choose how to play",21,UIThemeRole.SecondaryText);sub.alignment=TextAlignmentOptions.Center;Size(sub,38);
            Button("OnlineButton",layout,"Play online",true);Button("OfflineButton",layout,"Play offline");
            var options=Row("OptionsPanel",layout);
            var optionsGroup=options.GetComponent<HorizontalLayoutGroup>();
            optionsGroup.childAlignment=TextAnchor.MiddleCenter;
            optionsGroup.childForceExpandHeight=false;
            foreach(var label in new[]{("CustomizationButton","Customize"),("SettingsButton","Settings")})
            {
                var button=Button(label.Item1,options,label.Item2,false,180);
                button.GetComponent<LayoutElement>().minWidth=120;
            }
            Button("CreditsButton",layout,"Credits");
            CreateCredits(shell);
            var status=Label("ConnectionStatusLabel",layout,"Offline",16,UIThemeRole.SecondaryText);status.alignment=TextAlignmentOptions.Center;
            var customize=Screen(shell,"CustomizationScreen","Your profile","Choose how other players will see you.",false);
            var profile=Card("ProfilePanel",customize,"Player name");Input("PlayerNameInputField",profile,"Your name","Player");
            var preview=Card("PreviewPanel",customize,"Preview");Player(preview,"PlayerPreviewCard","Player",true);
            var style=Card("AppearancePanel",customize,"Emblem");var emblemRow=Row("EmblemOptionsPanel",style,68);
            for(int i=0;i<5;i++){var b=Button(elements[i]+"EmblemButton",emblemRow,"");var icon=Icon("ElementIcon",b.transform,symbols[elements[i]],40);Stretch(icon.rectTransform,14,6,14,6);}
            Button("SaveProfileButton",customize,"Apply profile",true);
            var settings=Screen(shell,"SettingsScreen","Settings","Adjust sound, language and appearance.",false);Settings(settings);Button("SaveSettingsButton",settings,"Ready",true);
            CommonOverlays(shell);Save("Start");
        }
        static RectTransform Room(Transform parent,string name,string roomName,string detail,string status)
        {
            if (!library)
            {
                var instance=PrefabInstance("Panels/RoomRow",parent);instance.name=name;
                ChildComponent<TextMeshProUGUI>(instance,"RoomNameLabel").text=roomName;
                ChildComponent<TextMeshProUGUI>(instance,"RoomDetailsLabel").text=detail;
                ChildComponent<TextMeshProUGUI>(instance,"RoomStatusLabel").text=status;
                return (RectTransform)instance.transform;
            }
            var panel=Panel(name,parent,UIThemeRole.Elevated);Size(panel,84);var row=Horizontal(panel.transform,16);row.padding=new RectOffset(18,18,12,12);
            var labels=Node("RoomInfoPanel",panel.transform);Size(labels,-1,-1,1);Vertical(labels,0,2);
            Label("RoomNameLabel",labels,roomName,22,UIThemeRole.Text,true);Label("RoomDetailsLabel",labels,detail,17,UIThemeRole.SecondaryText);
            var state=Label("RoomStatusLabel",panel.transform,status,17,UIThemeRole.SecondaryText);Size(state,-1,110);state.alignment=TextAlignmentOptions.Center;
            Button("JoinRoomButton",panel.transform,"Join",true,100);return panel.rectTransform;
        }
        static void RoomsScene()
        {
            var shell=Scene("OnlineRooms");var browser=Screen(shell,"RoomBrowserScreen","Online rooms","Find a table or create a room with friends.");
            var toolbar=Row("RoomActionsPanel",browser);Button("CreateRoomButton",toolbar,"Create room",true);Button("JoinPrivateRoomButton",toolbar,"Private room");Button("RefreshRoomsButton",toolbar,"Refresh");
            Input("RoomSearchInputField",browser,"Search rooms…");
            var content=Card("RoomListContentPanel",browser,"Available rooms");
            foreach(var data in new[]{new[]{"EmptyRoomsPanel","No rooms available","Create a room to get started."},new[]{"LoadingRoomsPanel","Searching for rooms…","Please wait."},new[]{"RoomListErrorPanel","Could not load rooms","Try refreshing the list."}})
            {var p=Card(data[0],browser,data[1]);Label("DetailsLabel",p,data[2],20,UIThemeRole.SecondaryText);p.gameObject.SetActive(false);}
            var create=Screen(shell,"CreateRoomScreen","Create room","Choose your table settings.",false);
            var identity=Card("RoomIdentityPanel",create,"Room details");Input("RoomNameInputField",identity,"Room name");Selector(identity,"RoomPrivacySelector","Visibility","Public");
            MatchConfiguration(create,false);Button("CreateRoomButton",create,"Create room",true);
            var join=Screen(shell,"JoinPrivateRoomScreen","Private room","Enter the code shared by the host.",false);
            var joinCard=Card("PrivateRoomPanel",join,"Invitation code");Input("RoomCodeInputField",joinCard,"Room code");Button("RequestAdmissionButton",joinCard,"Request access",true);
            var pending=Screen(shell,"AdmissionPendingScreen","Request sent","The host will see your request.",false);
            var wait=Card("PendingPanel",pending,"Awaiting approval…");Label("RoomNameLabel",wait,"Private room",23);Button("CancelAdmissionButton",wait,"Cancel request");
            var lobby=Screen(shell,"LobbyScreen","Lobby","Everyone should be ready before starting.",false);
            var roomInfo=Card("RoomSummaryPanel",lobby,"Elemental table");Label("RoomCodeLabel",roomInfo,"Code: — — — — — —",22,UIThemeRole.Accent);Label("ConfigurationLabel",roomInfo,"Up to 6 players · Room settings",18,UIThemeRole.SecondaryText);
            var players=Card("PlayersPanel",lobby,"Players · 3 / 6");
            for (int seat=0;seat<6;seat++) LobbyPlayer(players,"SeatRow"+seat,"—","—");
            Label("ReadinessLabel",lobby,"2 of 3 players ready",20,UIThemeRole.Accent);
            var host=Row("HostControls",lobby);Button("EditRoomButton",host,"Edit room");
            var client=Row("ClientControls",lobby);Button("ReadyButton",client,"Ready",true);Button("LeaveRoomButton",client,"Leave room");client.gameObject.SetActive(false);
            var request=Dialog(shell.overlays,"AdmissionRequestDialog","Admission request","A player wants to join your room.");Player(request,"ApplicantCard","Guest",false);var actions=Row("ActionsPanel",request);Button("RejectButton",actions,"Decline");Button("AcceptButton",actions,"Accept",true);
            var leave=Dialog(shell.overlays,"LeaveRoomDialog","Leave the room?","You will leave your seat at this table.");Button("StayButton",leave,"Stay");Button("LeaveButton",leave,"Leave",true);
            Button("CloseRoomButton",host,"Close room");
            AddCloseRoomDialog(shell);
            CommonOverlays(shell);Save("OnlineRooms");
        }
        static void AddCloseRoomDialog(Shell shell)
        {
            var close = Dialog(shell.overlays,"CloseRoomDialog","Close room","Everyone will return to the room browser.");
            Button("StayButton",close,"Cancel");
            Button("CloseRoomButton",close,"Close room",true);
        }
        static void LobbyPlayer(Transform parent,string name,string title,string status)
        {var row=Row(name,parent,58);var label=Label("NameLabel",row,title,22);Size(label,-1,-1,1);var s=Label("StatusLabel",row,status,18,UIThemeRole.SecondaryText);Size(s,-1,210);}
        static void MatchConfiguration(Transform parent,bool bots)
        {
            var card=Card("MatchConfigurationPanel",parent,"Match settings");
            Selector(card,"PlayerCountSelector",bots?"Players (including you)":"Player limit","2");
            if(bots)Selector(card,"BotDifficultySelector","Difficulty","Normal");
            Selector(card,"StartingHandSelector","Starting hand","—");
            Selector(card,"TurnTimeSelector","Turn duration","—");
            Selector(card,"StartingHealthSelector","Starting health","—");
            {
                Selector(card,"ReverseSelector","Reverse tiles per set","1");
                Selector(card,"PassSelector","Pass tiles per set","1");
                Selector(card,"SwapSelector","Swap tiles per set","1");
            }

        }
        static void OfflineSettingsScene()
        {
            var shell=Scene("OfflineSetting");var activity=Screen(shell,"ActivitySelectionScreen","Play offline","Practice at your own pace and learn the elements.");
            var grid=Grid("ActivityCardsPanel",activity,250);
            var tutorial=Card("TutorialCard",grid,"Learn to play");Object.DestroyImmediate(tutorial.GetComponent<ContentSizeFitter>());
            Size(Label("DescriptionLabel",tutorial,"Explore the table, attacks and defenses step by step.",21,UIThemeRole.SecondaryText),90);Button("TutorialButton",tutorial,"Open tutorial",true);
            var bots=Card("BotsCard",grid,"Game against bots");Object.DestroyImmediate(bots.GetComponent<ContentSizeFitter>());
            Size(Label("DescriptionLabel",bots,"Set up a table and practice your moves offline.",21,UIThemeRole.SecondaryText),90);Button("BotConfigurationButton",bots,"Set up match");
            var config=Screen(shell,"BotConfigurationScreen","Against bots","Choose how to practice.",false);MatchConfiguration(config,true);Button("ContinueButton",config,"Continue",true);
            var summary=Screen(shell,"ConfigurationSummaryScreen","All set","Review your settings before you start.",false);
            var details=Card("SummaryPanel",summary,"Your match");Label("PlayersLabel",details,"You + 1 bot",24);Label("DifficultyLabel",details,"Difficulty: Normal",22);Size(Label("RulesLabel",details,"Health, tiles and time: as configured",19,UIThemeRole.SecondaryText),64);
            var actions=Row("ActionsPanel",summary);Button("EditConfigurationButton",actions,"Edit");Button("StartMatchButton",actions,"Start",true);
            CommonOverlays(shell);Save("OfflineSetting");
        }
        static DominoTileView Tile(Transform parent,int attack,int defense,bool selected=false)
        {
            string prefab=Prefabs+"/Gameplay/DominoTileView.prefab";
            DominoTileView view;
            if(!library && AssetDatabase.LoadAssetAtPath<GameObject>(prefab))
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab),parent);view=go.GetComponent<DominoTileView>();
                view.attackIcon.sprite=elemental[elements[attack]][0];view.defenseIcon.sprite=elemental[elements[defense]][1];view.selectionOutline.SetActive(selected);return view;
            }
            var root=Node("DominoTileView",parent);root.sizeDelta=new Vector2(82,142);Size(root,142,82);view=root.gameObject.AddComponent<DominoTileView>();
            var outline=Panel("SelectionOutline",root,UIThemeRole.Accent);Stretch(outline.rectTransform,-4,-4,-4,-4);view.selectionOutline=outline.gameObject;outline.gameObject.SetActive(selected);
            var stone=Node("SlabImage",root);Stretch(stone);var image=stone.gameObject.AddComponent<Image>();image.sprite=slab;image.preserveAspect=false;image.raycastTarget=false;view.slab=image;
            var attackIcon=Icon("AttackIcon",root,elemental[elements[attack]][0]);Place(attackIcon.rectTransform,new Vector2(0,.53f),Vector2.one,new Vector2(13,7),new Vector2(-13,-12));view.attackIcon=attackIcon;
            var defenseIcon=Icon("DefenseIcon",root,elemental[elements[defense]][1]);Place(defenseIcon.rectTransform,Vector2.zero,new Vector2(1,.47f),new Vector2(13,13),new Vector2(-13,-7));view.defenseIcon=defenseIcon;
            var divider=Panel("Separator",root,UIThemeRole.Background);Place(divider.rectTransform,new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(13,-1),new Vector2(-13,1));divider.sprite=null;divider.raycastTarget=false;
            return view;
        }
        static RectTransform Player(Transform parent,string name,string displayName,bool activeTurn)
        {
            if (!library)
            {
                var card=PrefabInstance("Gameplay/PlayerCard",parent);card.name=name;
                ChildComponent<TextMeshProUGUI>(card,"PlayerNameLabel").text=displayName;
                card.transform.Find("TurnIndicator").gameObject.SetActive(activeTurn);
                Bind(card.GetComponent<Image>(),activeTurn?UIThemeRole.Elevated:UIThemeRole.Panel);
                return (RectTransform)card.transform;
            }
            var p=Panel(name,parent,activeTurn?UIThemeRole.Elevated:UIThemeRole.Panel);Size(p,86,164);
            var v=Vertical(p.transform,12,3);
            Label("PlayerNameLabel",p.transform,displayName,20,UIThemeRole.Text,true);
            Label("PlayerStatsLabel",p.transform,"Health —  ·  Tiles —",15,UIThemeRole.SecondaryText);
            var indicator=Panel("TurnIndicator",p.transform,UIThemeRole.Accent);Size(indicator,3);indicator.gameObject.SetActive(activeTurn);
            return p.rectTransform;
        }
        static void Board()
        {
            string texturePath="Assets/Sprites/WoodenTable.jpg";
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=false;importer.SaveAndReimport();
            var root=Node("BoardRoot");var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=0;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var surface=Node("TableSurface",root);Stretch(surface);var image=surface.gameObject.AddComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);image.type=Image.Type.Tiled;image.raycastTarget=false;image.color=new Color(.58f,.56f,.53f,1);
            var tiles=Node("PlacedTilesPreview",root);Place(tiles,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-230,-40),new Vector2(230,102));
            var layout=Horizontal(tiles,24);layout.childAlignment=TextAnchor.MiddleCenter;layout.childForceExpandHeight=false;
            for(int i=0;i<4;i++){var tile=Tile(tiles,i,(i+1)%5);tile.name="PlacedDomino"+(i+1);}
        }
        static void MatchScene(string name,bool offline,bool tutorial)
        {
            var shell=Scene(name,true);var screen=Node("MatchScreen",shell.screens);Stretch(screen);
            var top=Panel("TopHUDPanel",screen);Place(top.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(16,-164),new Vector2(-16,-12));
            var bar=Node("MatchInfoPanel",top.transform);Place(bar,new Vector2(0,1),Vector2.one,new Vector2(16,-48),new Vector2(-16,-8));Horizontal(bar,14);
            var turn=Label("TurnLabel",bar,tutorial?"Learn to play":"Your turn",23,UIThemeRole.Accent,true);Size(turn,-1,-1,1);
            var timer=Label("TurnTimerLabel",bar,"Time  — : —",18);Size(timer,-1,150);
            Button("ElementHelpButton",bar,"Elements",false,130);Button("PauseButton",bar,"Pause",false,90);
            var players=Scroll("PlayersScrollView",top.transform,true);Stretch((RectTransform)players.parent.parent,12,10,12,58);
            for(int i=0;i<6;i++)
            {
                var card=Player(players,"Player"+(i+1)+"Card",i==0?"You":(offline?"Bot ": "Player ")+(i+1),i==0);

                var icon = Icon("SeatEmblem",card,symbols["Earth"],22);
                icon.GetComponent<LayoutElement>().ignoreLayout = true;
                Place(icon.rectTransform,Vector2.one,Vector2.one,new Vector2(-30,-30),new Vector2(-6,-6));
            }
            var status=Panel("BoardStatusPanel",screen);Place(status.rectTransform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-280,-216),new Vector2(280,-176));
            var line=Horizontal(status.transform,18);line.padding=new RectOffset(16,16,2,2);
            var attack=Label("CurrentAttackLabel",status.transform,"Current attack",17);Size(attack,-1,-1,1);Icon("CurrentAttackIcon",status.transform,elemental["Fire"][0],30);
            var reserve=Label("ReserveCountLabel",status.transform,"Stock  —",17);Size(reserve,-1,125);
            var bottom=Panel("HandHUDPanel",screen);Place(bottom.rectTransform,Vector2.zero,new Vector2(1,0),new Vector2(16,12),new Vector2(-16,212));
            var handTitle=Label("HandTitleLabel",bottom.transform,"Your hand",20,UIThemeRole.Text,true);Place(handTitle.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(18,-35),new Vector2(-210,-6));
            var hand=Scroll("HandScrollView",bottom.transform,true);Stretch((RectTransform)hand.parent.parent,10,12,218,42);

            var controls=Node("ActionControlsPanel",bottom.transform);Place(controls,new Vector2(1,0),Vector2.one,new Vector2(-202,16),new Vector2(-16,-16));Vertical(controls,0,8);
            Button("PlayTileButton",controls,"Play tile",true);Button("DrawTileButton",controls,"Draw tile");Button("PassTurnButton",controls,"Pass turn");
            var spectator=Panel("SpectatorPanel",screen);Place(spectator.rectTransform,Vector2.zero,new Vector2(1,0),new Vector2(16,12),new Vector2(-16,170));Vertical(spectator.transform,24,12);
            Label("SpectatorTitleLabel",spectator.transform,"You are spectating",26,UIThemeRole.Text,true);Label("SpectatorDetailsLabel",spectator.transform,"Follow the turns and the tiles on the table.",20,UIThemeRole.SecondaryText);spectator.gameObject.SetActive(false);
            if(offline)
            {
                var bot=Panel("BotStatusPanel",screen);Place(bot.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-150,-90),new Vector2(150,-42));
                var label=Label("BotStatusLabel",bot.transform,"The bot is thinking…",19);Stretch(label.rectTransform,14,6,14,6);label.alignment=TextAlignmentOptions.Center;bot.gameObject.SetActive(false);
            }
            MatchDialogs(shell,offline);
            {
                GameObject.Find("BoardRoot").transform.Find("PlacedTilesPreview").gameObject.SetActive(false);
                var boardContent = Scroll("HistoryScroll",screen,true);
                boardContent.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;
                Place((RectTransform)boardContent.parent.parent,Vector2.zero,Vector2.one,
                    new Vector2(tutorial ? 8 : 18,tutorial ? 282 : 218),new Vector2(-18,-224));
                if (tutorial)
                    Place((RectTransform)boardContent.parent.parent,Vector2.zero,new Vector2(.45f,1),
                        new Vector2(8,282),new Vector2(-8,-224));
            }
            if(tutorial)TutorialPanels(shell,screen);
            CommonOverlays(shell);Save(name);
        }
        static void MatchDialogs(Shell shell,bool offline)
        {
            var pause=Dialog(shell.overlays,"PauseDialog",offline?"Game paused":"Match menu",offline?"Resume when you are ready.":"Online play continues while you use the menu.");
            Button("ResumeButton",pause,"Continue",true);Button("SettingsButton",pause,"Settings");Button("ElementHelpButton",pause,"Element guide");Button("LeaveMatchButton",pause,"Leave match");
            if (!offline)
            {
                Button("CloseRoomButton",pause,"Close room");
                AddCloseRoomDialog(shell);
                var admission = Dialog(shell.overlays,"AdmissionRequestDialog","Admission request","A player wants to join your room.");
                Button("AcceptButton",admission,"Accept"); Button("RejectButton",admission,"Decline");
            }
            var settings=Dialog(shell.overlays,"SettingsDialog","Settings","Sound, language and appearance.",false,840);Settings(settings,true);Button("CloseSettingsButton",settings,"Close",true);
            var results=Dialog(shell.overlays,"ResultsDialog","Match over","Table results.",false,680);
            var winner=Label("WinnerLabel",results,"Winner",30,UIThemeRole.Accent,true);Size(winner,48);
            for(int i=0;i<3;i++){var r=Row("ResultRow"+(i+1),results,48);var rank=Label("RankLabel",r,(i+1).ToString("00"),22,UIThemeRole.Accent);Size(rank,-1,50);var player=Label("PlayerLabel",r,i==0?"You":(offline?"Bot ": "Player ")+(i+1),21);Size(player,-1,-1,1);Label("ScoreLabel",r,"—",21);}
            Button("RematchButton",results,offline?"Play again":"Return to lobby",true);Button("ReturnButton",results,offline?"Change settings":"Main menu");
            var target=Dialog(shell.overlays,"TargetSelectionDialog","Choose a target","Select a player at the table.",false,680);
            for(int i=1;i<6;i++)Button("TargetPlayer"+i+"Button",target,(offline?"Bot ": "Player ")+(i+1));Button("CancelButton",target,"Cancel");
            var exit=Dialog(shell.overlays,"LeaveMatchDialog","Leave the match?","Confirm that you want to leave this table.");Button("StayButton",exit,"Keep playing",true);Button("LeaveButton",exit,"Leave");
            if(!offline)
            {
                var connection=Dialog(shell.overlays,"ConnectionLostDialog","Connection lost","Trying to reconnect to the room…");Button("ReturnToRoomsButton",connection,"Return to rooms");
                var disconnect=Panel("ConnectionBanner",shell.overlays,UIThemeRole.Elevated);Place(disconnect.rectTransform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-240,-62),new Vector2(240,-14));
                var text=Label("ConnectionLabel",disconnect.transform,"Reconnecting…",20);Stretch(text.rectTransform,16,8,16,8);text.alignment=TextAlignmentOptions.Center;disconnect.gameObject.SetActive(false);
                var host=Dialog(shell.overlays,"HostDisconnectedDialog","The host left","Please wait while the room updates.");Button("ReturnToRoomsButton",host,"Return to rooms");
            }
        }
        static void TutorialPanels(Shell shell,RectTransform screen)
        {
            var step=Panel("TutorialStepPanel",screen);Place(step.rectTransform,new Vector2(.45f,0),Vector2.one,new Vector2(4,282),new Vector2(-16,-224));

            Vertical(step.transform,18,8);Label("StepTitleLabel",step.transform,"1. Meet your tiles",25,UIThemeRole.Text,true);
            var explanation=Label("StepDescriptionLabel",step.transform,"The sword shows the attack. The shield shows the defense.",20,UIThemeRole.SecondaryText);Size(explanation,84);
            var progress=Node("TutorialProgressPanel",step.transform);Size(progress,20);Horizontal(progress,8);
            for(int i=0;i<9;i++){var dot=Panel("Step"+(i+1)+"Indicator",progress,i==0?UIThemeRole.Accent:UIThemeRole.Border);Size(dot,5,-1,1);}
            var controls=Panel("TutorialControlsPanel",screen);Place(controls.rectTransform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-320,220),new Vector2(320,276));
            var row=Horizontal(controls.transform,10);row.padding=new RectOffset(4,4,2,2);Button("PreviousStepButton",controls.transform,"Previous");Button("RetryStepButton",controls.transform,"Retry");Button("NextStepButton",controls.transform,"Next",true);Button("SkipTutorialButton",controls.transform,"Leave");
            var highlight=Panel("TutorialHighlightOverlay",shell.overlays,UIThemeRole.Focus);Place(highlight.rectTransform,Vector2.zero,new Vector2(1,0),new Vector2(20,20),new Vector2(-230,205));
            highlight.color=new Color(theme.accent.r,theme.accent.g,theme.accent.b,.18f);highlight.raycastTarget=false;highlight.GetComponent<UIThemeBinding>().opacity=.18f;highlight.gameObject.SetActive(false);
            var welcome=Dialog(shell.overlays,"TutorialWelcomeDialog","Welcome to DonmniCrash","Learn to recognize your tiles and read the table.");Button("BeginTutorialButton",welcome,"Start",true);Button("SkipTutorialButton",welcome,"Later");
            var help=Dialog(shell.overlays,"TutorialHelpDialog","Attack and defense","Each tile has two symbols: a sword for attack and a shield for defense.");var sample=Tile(help,3,1);Button("CloseButton",help,"Got it",true);
            var finish=Dialog(shell.overlays,"TutorialCompleteDialog","Tutorial complete!","You have explored the table. Continue practicing against bots.");Button("PracticeButton",finish,"Play against bots",true);Button("RepeatTutorialButton",finish,"Repeat tutorial");Button("BackButton",finish,"Back");
            var exit=Dialog(shell.overlays,"ExitTutorialDialog","Leave the tutorial?","You can start again from the offline menu.");Button("ContinueButton",exit,"Continue tutorial",true);Button("ExitButton",exit,"Leave");
        }
    }
}
