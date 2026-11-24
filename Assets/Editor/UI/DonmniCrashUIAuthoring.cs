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
        static readonly string[] elementNames = { "Tierra", "Agua", "Viento", "Fuego", "Planta" };
        static readonly Dictionary<string, Sprite[]> elemental = new Dictionary<string, Sprite[]>();
        static readonly Dictionary<string, Sprite> symbols = new Dictionary<string, Sprite>();
        static bool library;

        [MenuItem("DonmniCrash/Author visual UI scenes")]
        public static void Build() => Construir(false);

        // La fase local solo guarda las tres escenas de práctica.
        public static void FaseTres() => Construir(true);

        static void Construir(bool soloPractica)
        {
            foreach (string dir in new[] { UI, UI + "/Fonts", UI + "/Themes", UI + "/Sprites", Prefabs, Prefabs + "/Controls", Prefabs + "/Panels", Prefabs + "/Gameplay", Prefabs + "/Overlays" }) Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            if (soloPractica)
            {
                themes.Clear();
                themes.AddRange(AssetDatabase.LoadAssetAtPath<AspectoSesion>("Assets/Resources/UI/Aspecto.asset").paletas);
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
            if (!soloPractica)
            {
                PrepararServicios();
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
                new[]{"SlateGold", "Pizarra y dorado", "#171D24", "#28323E", "#F3EFE5", "#D2AA60"},
                new[]{"PetrolIvory", "Petróleo y marfil", "#102629", "#203B3D", "#F3EBD9", "#CEBE99"},
                new[]{"BlueCopper", "Azul y cobre", "#142131", "#24384E", "#F0F2F5", "#D79B77"},
                new[]{"PlumSilver", "Ciruela y plata", "#251F2B", "#3A3042", "#F1EDF3", "#C5C3CF"},
                new[]{"ForestParchment", "Bosque y pergamino", "#17251F", "#293D32", "#F2EAD7", "#D3BC89"}
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
                var obj = Instancia("Controls/TextInputField",parent);
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
                var obj = Instancia("Controls/ValueSelector",parent);
                obj.name = name;
                Hijo<TextMeshProUGUI>(obj,"FieldLabel").text = label;
                Hijo<TextMeshProUGUI>(obj,"ValueLabel").text = value;
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
            // Se conservan cámara y componentes del usuario; solo se reemplaza nuestra jerarquía UI.
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
            var back=Button("BackButton",header,"Volver",false,100);Place((RectTransform)back.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(0,-48),new Vector2(100,0));
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
        static void Save(string name) {ConectarEscena(name);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Scenes/"+name+".unity");}
        static void SavePrefab(GameObject go,string path) => PrefabUtility.SaveAsPrefabAsset(go,Prefabs+"/"+path+".prefab");
        static RectTransform Dialog(Transform parent,string name,string title,string description,bool active=false,float maxWidth=560)
        {
            if (!library)
            {
                var obj = Instancia("Overlays/ModalDialog",parent);
                obj.name = name;
                Hijo<TextMeshProUGUI>(obj,"TitleLabel").text = title;
                Hijo<TextMeshProUGUI>(obj,"DescriptionLabel").text = description;
                Hijo<UIWidthConstraint>(obj,"WindowPanel").maximumWidth = maxWidth;
                // El llamador coloca las acciones específicas de esta ventana.
                Hijo<Button>(obj,"CancelButton").gameObject.SetActive(false);
                Hijo<Button>(obj,"ConfirmButton").gameObject.SetActive(false);
                Hijo<Button>(obj,"CancelButton").name = "TemplateCancel";
                Hijo<Button>(obj,"ConfirmButton").name = "TemplateConfirm";
                obj.SetActive(active);
                return (RectTransform)Hijo<Transform>(obj,"Content");
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
                foreach (string nombre in new[]{"MessageDialog","ConfirmationDialog","LoadingOverlay","ToastPanel","ElementHelpDialog","OrientationOverlay"})
                    Instancia("Overlays/"+nombre,shell.overlays);
                Instancia("Overlays/TransitionOverlay",shell.canvas.transform);
                return;
            }
            var message=Dialog(shell.overlays,"MessageDialog","Aviso","Aquí aparecerán los mensajes de la partida.");Button("AcceptButton",message,"Aceptar",true);
            var confirm=Dialog(shell.overlays,"ConfirmationDialog","¿Quieres continuar?","Puedes cancelar para permanecer en esta pantalla.");
            var actions=Row("ActionsPanel",confirm);Button("CancelButton",actions,"Cancelar");Button("ConfirmButton",actions,"Confirmar",true);
            var loading=Dialog(shell.overlays,"LoadingOverlay","Un momento…","Preparando la siguiente pantalla.");Label("LoadingLabel",loading,"Cargando…",24);
            var toast=Panel("ToastPanel",shell.overlays,UIThemeRole.Elevated);Place(toast.rectTransform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-220,28),new Vector2(220,86));
            var toastLabel=Label("MessageLabel",toast.transform,"Cambios preparados",19);Stretch(toastLabel.rectTransform,16,8,16,8);toastLabel.alignment=TextAlignmentOptions.Center;toast.gameObject.SetActive(false);
            var help=Dialog(shell.overlays,"ElementHelpDialog","Los cinco elementos","Cada ficha tiene un ataque y una defensa.",false,680);
            for(int i=0;i<elements.Length;i++)
            {
                var r=Row(elements[i]+"HelpRow",help,58);Icon("ElementIcon",r,symbols[elements[i]],44);var label=Label("ElementLabel",r,elementNames[i],21);Size(label,-1,-1,1);
                Icon("AttackIcon",r,elemental[elements[i]][0],40);Icon("DefenseIcon",r,elemental[elements[i]][1],40);
            }
            Button("CloseButton",help,"Entendido",true);
            var orientation=Node("OrientationOverlay",shell.overlays);Stretch(orientation);var hint=orientation.gameObject.AddComponent<UIOrientationHint>();
            var portrait=Panel("PortraitPanel",orientation,UIThemeRole.Background);Stretch(portrait.rectTransform);
            var t=Label("OrientationLabel",portrait.transform,"Gira tu dispositivo\npara jugar en horizontal",28,UIThemeRole.Text,true);Stretch(t.rectTransform,32,64,32,64);t.alignment=TextAlignmentOptions.Center;
            hint.portraitPanel=portrait.gameObject;portrait.gameObject.SetActive(false);
            var transition=Panel("TransitionOverlay",shell.canvas.transform,UIThemeRole.Background);Stretch(transition.rectTransform);transition.gameObject.AddComponent<CanvasGroup>();transition.gameObject.SetActive(false);
        }
        static void Settings(Transform parent,bool compact=false)
        {
            if (!library) { Instancia("Panels/SettingsPanel",parent); return; }
            var audio=Card("AudioPanel",parent,"Audio");SliderRow(audio,"MasterVolumeRow","Volumen general",.8f);SliderRow(audio,"MusicVolumeRow","Música",.55f);SliderRow(audio,"EffectsVolumeRow","Efectos",.75f);
            var language=Card("LanguagePanel",parent,"Idioma");Selector(language,"LanguageSelector","Idioma del juego","Español");
            var appearance=Card("AppearancePanel",parent,"Apariencia");Label("ThemeHintLabel",appearance,"Tema de la interfaz",18,UIThemeRole.SecondaryText);
            var grid=Grid("ThemeGrid",appearance,124,compact?2:5);
            for(int i=0;i<themes.Count;i++)
            {
                if (i > 0)
                {
                    var obj = Instancia("Controls/ThemeSwatchButton",grid);
                    obj.name = "Theme"+(i+1)+"Button";
                    var muestras = Hijo<Transform>(obj,"ColorPreview");
                    var tonos = new[]{themes[i].background,themes[i].panel,themes[i].accent,themes[i].text};
                    for (int j=0;j<tonos.Length;j++) muestras.GetChild(j).GetComponent<Image>().color = tonos[j];
                    Hijo<TextMeshProUGUI>(obj,"ThemeNameLabel").text = themes[i].displayName;
                    Hijo<Transform>(obj,"SelectedLabel").gameObject.SetActive(false);
                    foreach (var componente in obj.GetComponentsInChildren<Component>(true))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(componente);
                        if (componente is Transform) PrefabUtility.RecordPrefabInstancePropertyModifications(componente.gameObject);
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
            SavePrefab(Button("PrimaryButton",root,"Continuar",true).gameObject,"Controls/PrimaryButton");
            SavePrefab(Button("SecondaryButton",root,"Volver").gameObject,"Controls/SecondaryButton");
            SavePrefab(Input("TextInputField",root,"Escribe aquí").gameObject,"Controls/TextInputField");
            var selector=Node("ValueSelector",root);Vertical(selector);Selector(selector,"SelectorRow","Configuración","Valor");SavePrefab(selector.gameObject,"Controls/ValueSelector");
            var tile=Tile(root,0,1);SavePrefab(tile.gameObject,"Gameplay/DominoTileView");
            var player=Player(root,"PlayerCard","Jugador",false);SavePrefab(player.gameObject,"Gameplay/PlayerCard");
            var room=Room(root,"RoomRow","Sala de ejemplo","Pública · 2 / 6","En espera");SavePrefab(room.gameObject,"Panels/RoomRow");
            var settings=Node("SettingsPanel",root);Vertical(settings);Settings(settings);CompletarAjustes(settings);SavePrefab(settings.gameObject,"Panels/SettingsPanel");
            var modal=Dialog(root,"ModalDialog","Título","Descripción");Button("CancelButton",modal,"Cancelar");Button("ConfirmButton",modal,"Confirmar",true);
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
            var eyebrow=Label("EyebrowLabel",layout,"DOMINÓ ELEMENTAL",16,UIThemeRole.Accent);eyebrow.alignment=TextAlignmentOptions.Center;
            var title=Label("GameTitleLabel",layout,"DonmniCrash",48,UIThemeRole.Text,true);title.alignment=TextAlignmentOptions.Center;Size(title,72);
            var sub=Label("SubtitleLabel",layout,"Elige cómo quieres jugar",21,UIThemeRole.SecondaryText);sub.alignment=TextAlignmentOptions.Center;Size(sub,38);
            Button("OnlineButton",layout,"Jugar online",true);Button("OfflineButton",layout,"Jugar offline");
            var options=Row("OptionsPanel",layout);
            var optionsGroup=options.GetComponent<HorizontalLayoutGroup>();
            optionsGroup.childAlignment=TextAnchor.MiddleCenter;
            optionsGroup.childForceExpandHeight=false;
            foreach(var label in new[]{("CustomizationButton","Personalizar"),("SettingsButton","Ajustes")})
            {
                var button=Button(label.Item1,options,label.Item2,false,180);
                button.GetComponent<LayoutElement>().minWidth=120;
            }
            var status=Label("ConnectionStatusLabel",layout,"Sin conexión",16,UIThemeRole.SecondaryText);status.alignment=TextAlignmentOptions.Center;
            var customize=Screen(shell,"CustomizationScreen","Tu perfil","Personaliza cómo te verán los demás jugadores.",false);
            var profile=Card("ProfilePanel",customize,"Nombre de jugador");Input("PlayerNameInputField",profile,"Tu nombre","Jugador");
            var preview=Card("PreviewPanel",customize,"Vista previa");Player(preview,"PlayerPreviewCard","Jugador",true);
            var style=Card("AppearancePanel",customize,"Emblema");var emblemRow=Row("EmblemOptionsPanel",style,68);
            for(int i=0;i<5;i++){var b=Button(elements[i]+"EmblemButton",emblemRow,"");var icon=Icon("ElementIcon",b.transform,symbols[elements[i]],40);Stretch(icon.rectTransform,14,6,14,6);}
            Button("SaveProfileButton",customize,"Guardar perfil",true);
            var settings=Screen(shell,"SettingsScreen","Ajustes","Configura el sonido, el idioma y la apariencia.",false);Settings(settings);Button("SaveSettingsButton",settings,"Listo",true);
            CommonOverlays(shell);Save("Start");
        }
        static RectTransform Room(Transform parent,string name,string roomName,string detail,string status)
        {
            if (!library)
            {
                var fila=Instancia("Panels/RoomRow",parent);fila.name=name;
                Hijo<TextMeshProUGUI>(fila,"RoomNameLabel").text=roomName;
                Hijo<TextMeshProUGUI>(fila,"RoomDetailsLabel").text=detail;
                Hijo<TextMeshProUGUI>(fila,"RoomStatusLabel").text=status;
                return (RectTransform)fila.transform;
            }
            var panel=Panel(name,parent,UIThemeRole.Elevated);Size(panel,84);var row=Horizontal(panel.transform,16);row.padding=new RectOffset(18,18,12,12);
            var labels=Node("RoomInfoPanel",panel.transform);Size(labels,-1,-1,1);Vertical(labels,0,2);
            Label("RoomNameLabel",labels,roomName,22,UIThemeRole.Text,true);Label("RoomDetailsLabel",labels,detail,17,UIThemeRole.SecondaryText);
            var state=Label("RoomStatusLabel",panel.transform,status,17,UIThemeRole.SecondaryText);Size(state,-1,110);state.alignment=TextAlignmentOptions.Center;
            Button("JoinRoomButton",panel.transform,"Entrar",true,100);return panel.rectTransform;
        }
        static void RoomsScene()
        {
            var shell=Scene("OnlineRooms");var browser=Screen(shell,"RoomBrowserScreen","Salas online","Busca una mesa o crea una partida con tus amigos.");
            var toolbar=Row("RoomActionsPanel",browser);Button("CreateRoomButton",toolbar,"Crear sala",true);Button("JoinPrivateRoomButton",toolbar,"Sala privada");Button("RefreshRoomsButton",toolbar,"Actualizar");
            Input("RoomSearchInputField",browser,"Buscar sala…");
            var content=Card("RoomListContentPanel",browser,"Salas disponibles");Room(content,"RoomRow01","Mesa elemental","Pública · 2 / 6","En espera");Room(content,"RoomRow02","Partida tranquila","Pública · 4 / 6","En espera");Room(content,"RoomRow03","Duelo de elementos","Pública · 2 / 2","En partida");
            foreach(var data in new[]{new[]{"EmptyRoomsPanel","No hay salas disponibles","Crea una sala para comenzar."},new[]{"LoadingRoomsPanel","Buscando salas…","Espera un momento."},new[]{"RoomListErrorPanel","No se pudieron cargar las salas","Intenta actualizar la lista."}})
            {var p=Card(data[0],browser,data[1]);Label("DetailsLabel",p,data[2],20,UIThemeRole.SecondaryText);p.gameObject.SetActive(false);}
            var create=Screen(shell,"CreateRoomScreen","Crear sala","Prepara los detalles de tu mesa.",false);
            var identity=Card("RoomIdentityPanel",create,"Datos de la sala");Input("RoomNameInputField",identity,"Nombre de la sala");Selector(identity,"RoomPrivacySelector","Visibilidad","Pública");
            MatchConfiguration(create,false);Button("CreateRoomButton",create,"Crear sala",true);
            var join=Screen(shell,"JoinPrivateRoomScreen","Sala privada","Introduce el código que compartió el anfitrión.",false);
            var joinCard=Card("PrivateRoomPanel",join,"Código de invitación");Input("RoomCodeInputField",joinCard,"Código de sala");Button("RequestAdmissionButton",joinCard,"Solicitar acceso",true);
            var pending=Screen(shell,"AdmissionPendingScreen","Solicitud enviada","El anfitrión recibirá tu solicitud de acceso.",false);
            var wait=Card("PendingPanel",pending,"Esperando aprobación…");Label("RoomNameLabel",wait,"Sala privada",23);Button("CancelAdmissionButton",wait,"Cancelar solicitud");
            var lobby=Screen(shell,"LobbyScreen","Sala de espera","Todos los jugadores deben estar listos para comenzar.",false);
            var roomInfo=Card("RoomSummaryPanel",lobby,"Mesa elemental");Label("RoomCodeLabel",roomInfo,"Código: — — — — — —",22,UIThemeRole.Accent);Label("ConfigurationLabel",roomInfo,"Hasta 6 jugadores · Configuración de la sala",18,UIThemeRole.SecondaryText);
            var players=Card("PlayersPanel",lobby,"Jugadores · 3 / 6");
            LobbyPlayer(players,"HostPlayerRow","Jugador","Anfitrión · Listo");LobbyPlayer(players,"GuestPlayerRow","Invitado 1","Listo");LobbyPlayer(players,"PendingPlayerRow","Invitado 2","Preparándose");
            Label("ReadinessLabel",lobby,"2 de 3 jugadores listos",20,UIThemeRole.Accent);
            var host=Row("HostControls",lobby);Button("EditRoomButton",host,"Editar sala");Button("StartMatchButton",host,"Comenzar partida",true);
            var client=Row("ClientControls",lobby);Button("ReadyButton",client,"Estoy listo",true);Button("LeaveRoomButton",client,"Salir de la sala");client.gameObject.SetActive(false);
            var request=Dialog(shell.overlays,"AdmissionRequestDialog","Solicitud de acceso","Un jugador quiere entrar a tu sala.");Player(request,"ApplicantCard","Invitado",false);var actions=Row("ActionsPanel",request);Button("RejectButton",actions,"Rechazar");Button("AcceptButton",actions,"Aceptar",true);
            var leave=Dialog(shell.overlays,"LeaveRoomDialog","¿Salir de la sala?","Dejarás tu lugar en esta mesa.");Button("StayButton",leave,"Quedarme");Button("LeaveButton",leave,"Salir",true);
            CommonOverlays(shell);Save("OnlineRooms");
        }
        static void LobbyPlayer(Transform parent,string name,string title,string status)
        {var row=Row(name,parent,58);var label=Label("NameLabel",row,title,22);Size(label,-1,-1,1);var s=Label("StatusLabel",row,status,18,UIThemeRole.SecondaryText);Size(s,-1,210);}
        static void MatchConfiguration(Transform parent,bool bots)
        {
            var card=Card("MatchConfigurationPanel",parent,"Configuración de partida");
            Selector(card,"PlayerCountSelector",bots?"Jugadores (incluyéndote)":"Máximo de jugadores","2");
            if(bots)Selector(card,"BotDifficultySelector","Dificultad","Normal");
            Selector(card,"StartingHandSelector","Fichas iniciales","—");
            Selector(card,"TurnTimeSelector","Tiempo por turno","—");
            Selector(card,"StartingHealthSelector","Vida inicial","—");
            if (bots)
            {
                Selector(card,"ReverseSelector","Reversas por set","1");
                Selector(card,"PassSelector","Pases por set","1");
                Selector(card,"SwapSelector","Intercambios por set","1");
            }
            else Selector(card,"EffectsSelector","Efectos de elementos","Activados");
        }
        static void OfflineSettingsScene()
        {
            var shell=Scene("OfflineSetting");var activity=Screen(shell,"ActivitySelectionScreen","Jugar offline","Practica a tu ritmo y conoce tus elementos.");
            var grid=Grid("ActivityCardsPanel",activity,250);
            var tutorial=Card("TutorialCard",grid,"Aprender a jugar");Object.DestroyImmediate(tutorial.GetComponent<ContentSizeFitter>());
            Size(Label("DescriptionLabel",tutorial,"Descubre la mesa, los ataques y las defensas paso a paso.",21,UIThemeRole.SecondaryText),90);Button("TutorialButton",tutorial,"Abrir tutorial",true);
            var bots=Card("BotsCard",grid,"Partida contra bots");Object.DestroyImmediate(bots.GetComponent<ContentSizeFitter>());
            Size(Label("DescriptionLabel",bots,"Prepara una mesa y practica tus decisiones sin conexión.",21,UIThemeRole.SecondaryText),90);Button("BotConfigurationButton",bots,"Configurar partida");
            var config=Screen(shell,"BotConfigurationScreen","Contra bots","Elige cómo quieres practicar.",false);MatchConfiguration(config,true);Button("ContinueButton",config,"Continuar",true);
            var summary=Screen(shell,"ConfigurationSummaryScreen","Todo listo","Consulta tu configuración antes de comenzar.",false);
            var details=Card("SummaryPanel",summary,"Tu partida");Label("PlayersLabel",details,"Tú + 1 bot",24);Label("DifficultyLabel",details,"Dificultad: Normal",22);Size(Label("RulesLabel",details,"Vida, fichas y tiempo: según configuración",19,UIThemeRole.SecondaryText),64);
            var actions=Row("ActionsPanel",summary);Button("EditConfigurationButton",actions,"Editar");Button("StartMatchButton",actions,"Comenzar",true);
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
                var card=Instancia("Gameplay/PlayerCard",parent);card.name=name;
                Hijo<TextMeshProUGUI>(card,"PlayerNameLabel").text=displayName;
                card.transform.Find("TurnIndicator").gameObject.SetActive(activeTurn);
                Bind(card.GetComponent<Image>(),activeTurn?UIThemeRole.Elevated:UIThemeRole.Panel);
                return (RectTransform)card.transform;
            }
            var p=Panel(name,parent,activeTurn?UIThemeRole.Elevated:UIThemeRole.Panel);Size(p,86,164);
            var v=Vertical(p.transform,12,3);
            Label("PlayerNameLabel",p.transform,displayName,20,UIThemeRole.Text,true);
            Label("PlayerStatsLabel",p.transform,"Vida —  ·  Fichas —",15,UIThemeRole.SecondaryText);
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
            var turn=Label("TurnLabel",bar,tutorial?"Aprende a jugar":"Tu turno",23,UIThemeRole.Accent,true);Size(turn,-1,-1,1);
            var timer=Label("TurnTimerLabel",bar,"Tiempo  — : —",18);Size(timer,-1,150);
            Button("ElementHelpButton",bar,"Elementos",false,130);Button("PauseButton",bar,"Pausa",false,90);
            var players=Scroll("PlayersScrollView",top.transform,true);Stretch((RectTransform)players.parent.parent,12,10,12,58);
            for(int i=0;i<6;i++)
            {
                var card=Player(players,"Player"+(i+1)+"Card",i==0?"Tú":(offline?"Bot ":"Jugador ")+(i+1),i==0);
                if (!offline) continue;
                var icon = Icon("SeatEmblem",card,symbols["Earth"],22);
                icon.GetComponent<LayoutElement>().ignoreLayout = true;
                Place(icon.rectTransform,Vector2.one,Vector2.one,new Vector2(-30,-30),new Vector2(-6,-6));
            }
            var status=Panel("BoardStatusPanel",screen);Place(status.rectTransform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-280,-216),new Vector2(280,-176));
            var line=Horizontal(status.transform,18);line.padding=new RectOffset(16,16,2,2);
            var attack=Label("CurrentAttackLabel",status.transform,"Ataque actual",17);Size(attack,-1,-1,1);Icon("CurrentAttackIcon",status.transform,elemental["Fire"][0],30);
            var reserve=Label("ReserveCountLabel",status.transform,"Reserva  —",17);Size(reserve,-1,125);
            var bottom=Panel("HandHUDPanel",screen);Place(bottom.rectTransform,Vector2.zero,new Vector2(1,0),new Vector2(16,12),new Vector2(-16,212));
            var handTitle=Label("HandTitleLabel",bottom.transform,"Tu mano",20,UIThemeRole.Text,true);Place(handTitle.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(18,-35),new Vector2(-210,-6));
            var hand=Scroll("HandScrollView",bottom.transform,true);Stretch((RectTransform)hand.parent.parent,10,12,218,42);
            for(int i=0;i<(offline ? 0 : 6);i++){var tile=Tile(hand,i%5,(i+2)%5,i==1);tile.name="HandDomino"+(i+1);}
            var controls=Node("ActionControlsPanel",bottom.transform);Place(controls,new Vector2(1,0),Vector2.one,new Vector2(-202,16),new Vector2(-16,-16));Vertical(controls,0,8);
            Button("PlayTileButton",controls,"Colocar ficha",true);Button("DrawTileButton",controls,"Tomar ficha");Button("PassTurnButton",controls,"Pasar turno");
            var spectator=Panel("SpectatorPanel",screen);Place(spectator.rectTransform,Vector2.zero,new Vector2(1,0),new Vector2(16,12),new Vector2(-16,170));Vertical(spectator.transform,24,12);
            Label("SpectatorTitleLabel",spectator.transform,"Estás observando la partida",26,UIThemeRole.Text,true);Label("SpectatorDetailsLabel",spectator.transform,"Sigue los turnos y las fichas de la mesa.",20,UIThemeRole.SecondaryText);spectator.gameObject.SetActive(false);
            if(offline)
            {
                var bot=Panel("BotStatusPanel",screen);Place(bot.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-150,-90),new Vector2(150,-42));
                var label=Label("BotStatusLabel",bot.transform,"El bot está pensando…",19);Stretch(label.rectTransform,14,6,14,6);label.alignment=TextAlignmentOptions.Center;bot.gameObject.SetActive(false);
            }
            MatchDialogs(shell,offline);
            if (offline)
            {
                GameObject.Find("BoardRoot").transform.Find("PlacedTilesPreview").gameObject.SetActive(false);
                var historia = Scroll("HistoryScroll",screen,true);
                historia.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;
                Place((RectTransform)historia.parent.parent,Vector2.zero,Vector2.one,
                    new Vector2(tutorial ? 8 : 18,tutorial ? 282 : 218),new Vector2(-18,-224));
                if (tutorial)
                    Place((RectTransform)historia.parent.parent,Vector2.zero,new Vector2(.45f,1),
                        new Vector2(8,282),new Vector2(-8,-224));
            }
            if(tutorial)TutorialPanels(shell,screen);
            CommonOverlays(shell);Save(name);
        }
        static void MatchDialogs(Shell shell,bool offline)
        {
            var pause=Dialog(shell.overlays,"PauseDialog",offline?"Partida en pausa":"Menú de partida",offline?"Retoma la partida cuando estés listo.":"La partida online continúa mientras consultas el menú.");
            Button("ResumeButton",pause,"Continuar",true);Button("SettingsButton",pause,"Ajustes");Button("ElementHelpButton",pause,"Ayuda de elementos");Button("LeaveMatchButton",pause,"Salir de la partida");
            var settings=Dialog(shell.overlays,"SettingsDialog","Ajustes","Sonido, idioma y apariencia.",false,840);Settings(settings,true);Button("CloseSettingsButton",settings,"Cerrar",true);
            var results=Dialog(shell.overlays,"ResultsDialog","Fin de la partida","Resultados de la mesa.",false,680);
            var winner=Label("WinnerLabel",results,"Jugador ganador",30,UIThemeRole.Accent,true);Size(winner,48);
            for(int i=0;i<3;i++){var r=Row("ResultRow"+(i+1),results,48);var rank=Label("RankLabel",r,(i+1).ToString("00"),22,UIThemeRole.Accent);Size(rank,-1,50);var player=Label("PlayerLabel",r,i==0?"Tú":(offline?"Bot ":"Jugador ")+(i+1),21);Size(player,-1,-1,1);Label("ScoreLabel",r,"—",21);}
            Button("RematchButton",results,offline?"Jugar de nuevo":"Volver a la sala",true);Button("ReturnButton",results,offline?"Cambiar configuración":"Menú principal");
            var target=Dialog(shell.overlays,"TargetSelectionDialog","Elige un objetivo","Selecciona a un jugador de la mesa.",false,680);
            for(int i=1;i<6;i++)Button("TargetPlayer"+i+"Button",target,(offline?"Bot ":"Jugador ")+(i+1));Button("CancelButton",target,"Cancelar");
            var exit=Dialog(shell.overlays,"LeaveMatchDialog","¿Salir de la partida?","Confirma que quieres abandonar esta mesa.");Button("StayButton",exit,"Seguir jugando",true);Button("LeaveButton",exit,"Salir");
            if(!offline)
            {
                var connection=Dialog(shell.overlays,"ConnectionLostDialog","Se perdió la conexión","Intentando recuperar la conexión con la sala…");Button("ReturnToRoomsButton",connection,"Volver a salas");
                var disconnect=Panel("ConnectionBanner",shell.overlays,UIThemeRole.Elevated);Place(disconnect.rectTransform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-240,-62),new Vector2(240,-14));
                var text=Label("ConnectionLabel",disconnect.transform,"Reconectando…",20);Stretch(text.rectTransform,16,8,16,8);text.alignment=TextAlignmentOptions.Center;disconnect.gameObject.SetActive(false);
                var host=Dialog(shell.overlays,"HostDisconnectedDialog","El anfitrión salió","Espera mientras se actualiza la sala.");Button("ReturnToRoomsButton",host,"Volver a salas");
            }
        }
        static void TutorialPanels(Shell shell,RectTransform screen)
        {
            var step=Panel("TutorialStepPanel",screen);Place(step.rectTransform,new Vector2(.45f,0),Vector2.one,new Vector2(4,282),new Vector2(-16,-224));

            Vertical(step.transform,18,8);Label("StepTitleLabel",step.transform,"1. Conoce tus fichas",25,UIThemeRole.Text,true);
            var explanation=Label("StepDescriptionLabel",step.transform,"La espada representa el ataque. El escudo representa la defensa.",20,UIThemeRole.SecondaryText);Size(explanation,84);
            var progress=Node("TutorialProgressPanel",step.transform);Size(progress,20);Horizontal(progress,8);
            for(int i=0;i<9;i++){var dot=Panel("Step"+(i+1)+"Indicator",progress,i==0?UIThemeRole.Accent:UIThemeRole.Border);Size(dot,5,-1,1);}
            var controls=Panel("TutorialControlsPanel",screen);Place(controls.rectTransform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-320,220),new Vector2(320,276));
            var row=Horizontal(controls.transform,10);row.padding=new RectOffset(4,4,2,2);Button("PreviousStepButton",controls.transform,"Anterior");Button("RetryStepButton",controls.transform,"Repetir");Button("NextStepButton",controls.transform,"Siguiente",true);Button("SkipTutorialButton",controls.transform,"Salir");
            var highlight=Panel("TutorialHighlightOverlay",shell.overlays,UIThemeRole.Focus);Place(highlight.rectTransform,Vector2.zero,new Vector2(1,0),new Vector2(20,20),new Vector2(-230,205));
            highlight.color=new Color(theme.accent.r,theme.accent.g,theme.accent.b,.18f);highlight.raycastTarget=false;highlight.GetComponent<UIThemeBinding>().opacity=.18f;highlight.gameObject.SetActive(false);
            var welcome=Dialog(shell.overlays,"TutorialWelcomeDialog","Bienvenido a DonmniCrash","Aprende a reconocer tus fichas y a leer la mesa.");Button("BeginTutorialButton",welcome,"Comenzar",true);Button("SkipTutorialButton",welcome,"Más tarde");
            var help=Dialog(shell.overlays,"TutorialHelpDialog","Ataque y defensa","Cada ficha tiene dos símbolos: una espada para atacar y un escudo para defender.");var sample=Tile(help,3,1);Button("CloseButton",help,"Entendido",true);
            var finish=Dialog(shell.overlays,"TutorialCompleteDialog","¡Tutorial completado!","Ya conoces los elementos de la mesa. Puedes seguir practicando contra bots.");Button("PracticeButton",finish,"Jugar contra bots",true);Button("RepeatTutorialButton",finish,"Repetir tutorial");Button("BackButton",finish,"Volver");
            var exit=Dialog(shell.overlays,"ExitTutorialDialog","¿Salir del tutorial?","Puedes volver a empezar desde el menú offline.");Button("ContinueButton",exit,"Continuar tutorial",true);Button("ExitButton",exit,"Salir");
        }
    }
}
