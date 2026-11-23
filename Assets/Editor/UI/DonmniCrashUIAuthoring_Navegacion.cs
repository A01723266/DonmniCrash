using System.IO;
using System.Linq;
using DonmniCrash.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.EditorTools
{
    /// <summary>Guarda componentes, referencias y rutas de la interfaz junto con las escenas editables.</summary>
    public static partial class DonmniCrashUIAuthoring
    {
        static GameObject Instancia(string ruta, Transform padre)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + ruta + ".prefab");
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab,padre);
        }

        static T Hijo<T>(GameObject raiz,string nombre) where T : Component =>
            raiz.GetComponentsInChildren<T>(true).First(c => c.name == nombre);

        static void PrepararServicios()
        {
            const string audioDir = "Assets/Resources/Prefabs/Audio";
            Directory.CreateDirectory(audioDir);
            AssetDatabase.Refresh();
            const string ruta = "Assets/Resources/UI/Aspecto.asset";
            var catalogo = AssetDatabase.LoadAssetAtPath<AspectoSesion>(ruta);
            if (!catalogo) { catalogo = ScriptableObject.CreateInstance<AspectoSesion>(); AssetDatabase.CreateAsset(catalogo,ruta); }
            catalogo.paletas = themes.ToArray();
            catalogo.losas = new[]{"Grey","Blue","Black"}.Select(c => Sprites(
                $"Assets/Kenney/UI/kenney_rune-pack/PNG/{c}/Slab (outline)/rune{c}_slabOutline_036.png").First()).ToArray();
            EditorUtility.SetDirty(catalogo);
            // Una reconstrucción de la UI conserva los clips que el autor asigne al prefab.
            string audioPath = audioDir + "/AudioJuego.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(audioPath)) return;
            var obj = new GameObject("AudioJuego");
            var servicio = obj.AddComponent<AudioJuego>();
            servicio.musica = Fuente(obj.transform,"Musica",true);
            servicio.efectos = Fuente(obj.transform,"Efectos",false);
            PrefabUtility.SaveAsPrefabAsset(obj,audioPath);
            Object.DestroyImmediate(obj);
        }

        static AudioSource Fuente(Transform padre,string nombre,bool repetir)
        {
            var obj = new GameObject(nombre);
            obj.transform.SetParent(padre,false);
            var fuente = obj.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.loop = repetir;
            fuente.spatialBlend = 0;
            return fuente;
        }

        static void CompletarAjustes(RectTransform raiz)
        {
            var panel = raiz.gameObject.AddComponent<PanelAjustes>();
            var obj = raiz.gameObject;
            panel.general = Hijo<Transform>(obj,"MasterVolumeRow").GetComponentInChildren<Slider>(true);
            panel.musica = Hijo<Transform>(obj,"MusicVolumeRow").GetComponentInChildren<Slider>(true);
            panel.efectos = Hijo<Transform>(obj,"EffectsVolumeRow").GetComponentInChildren<Slider>(true);
            var idioma = Hijo<Transform>(obj,"LanguageSelector").gameObject;
            panel.anterior = Hijo<Button>(idioma,"PreviousButton");
            panel.siguiente = Hijo<Button>(idioma,"NextButton");
            panel.idioma = Hijo<TextMeshProUGUI>(idioma,"ValueLabel");
            panel.paletas = Hijo<Transform>(obj,"ThemeGrid").GetComponentsInChildren<Button>(true);
            panel.marcasPaleta = panel.paletas.Select(b => Hijo<Transform>(b.gameObject,"SelectedLabel").gameObject).ToArray();
            var contenedor = Card("TileStyle",raiz,"Aspecto de fichas");
            var grid = Grid("TileGrid",contenedor,98,3);
            grid.GetComponent<UIResponsiveGrid>().minimumCellWidth = 135;
            var catalogo = AssetDatabase.LoadAssetAtPath<AspectoSesion>("Assets/Resources/UI/Aspecto.asset");
            panel.losas = new Button[3];
            panel.marcasLosa = new GameObject[3];
            var nombres = new[]{"Gris","Azul","Negra"};
            for (int i=0;i<3;i++)
            {
                var b = Button("TileStyle"+i,grid,nombres[i]);
                var texto = Hijo<TextMeshProUGUI>(b.gameObject,"TextLabel");
                Place(texto.rectTransform,Vector2.zero,new Vector2(1,.4f),Vector2.zero,Vector2.zero);
                var icono = Icon("Slab",b.transform,catalogo.losas[i]);
                Place(icono.rectTransform,new Vector2(.5f,.4f),new Vector2(.5f,1),new Vector2(-20,0),new Vector2(20,-3));
                var marca = Label("SelectedLabel",b.transform,"●",16,UIThemeRole.Accent);
                Place(marca.rectTransform,new Vector2(1,1),Vector2.one,new Vector2(-24,-24),Vector2.zero);
                panel.losas[i] = b;
                panel.marcasLosa[i] = marca.gameObject;
                marca.gameObject.SetActive(i==0);
            }
        }

        static void ConectarEscena(string nombre)
        {
            var canvas = GameObject.Find("UICanvas");
            var pantallas = canvas.transform.Find("SafeArea/Screens");
            var overlays = canvas.transform.Find("SafeArea/Overlays");
            switch (nombre)
            {
                case "Start":
                    canvas.AddComponent<MenuInicio>();
                    var tarjeta = Hijo<Transform>(canvas,"PlayerPreviewCard");
                    var icono = Icon("ProfileEmblem",tarjeta,symbols["Earth"],32);
                    icono.transform.SetAsFirstSibling();
                    Size(tarjeta,122,164);
                    break;
                case "OnlineRooms": canvas.AddComponent<ControlSalas>(); break;
                case "OfflineSetting": canvas.AddComponent<MenuPractica>(); break;
                case "OnlineMatch": canvas.AddComponent<MesaOnlineUI>(); break;
                case "OfflineMatch": canvas.AddComponent<MesaLocalUI>(); break;
                case "Tutorial": canvas.AddComponent<RecorridoTutorial>(); break;
            }
            foreach (var ficha in canvas.GetComponentsInChildren<DominoTileView>(true))
            {
                if (!ficha.name.StartsWith("HandDomino")) continue;
                var superficie = ficha.gameObject.AddComponent<Image>();
                superficie.color = Color.clear;
                var b = ficha.gameObject.AddComponent<Button>();
                b.targetGraphic = superficie;
                b.transition = Selectable.Transition.None;
            }
            if (nombre=="OnlineRooms" || nombre=="OfflineSetting") PrepararSelectores(canvas);
            CrearVistaPrevia(nombre,pantallas,overlays);
            foreach (var texto in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                // Solo se traduce el placeholder de un campo editable.
                var campo = texto.GetComponentInParent<TMP_InputField>(true);
                if (campo && campo.textComponent==texto) continue;
                // «Listo» también indica disponibilidad en el lobby. El cierre de Ajustes tiene su propia clave.
                var boton = texto.GetComponentInParent<Button>(true);
                string clave = boton && boton.name == "SaveSettingsButton"
                    ? "save.settings"
                    : TextoIdioma.ClaveOriginal(texto.text);
                if (clave != null) Etiqueta(texto).clave = clave;
            }
            // Conserva los valores personalizados (nombre de botón, clave, textos y sprites)
            // como overrides de la instancia enlazada al prefab común.
            foreach (var componente in canvas.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)))
            {
                if (!componente || !PrefabUtility.IsPartOfPrefabInstance(componente)) continue;
                PrefabUtility.RecordPrefabInstancePropertyModifications(componente);
                if (componente is Transform) PrefabUtility.RecordPrefabInstancePropertyModifications(componente.gameObject);
            }
        }

        static TextoIdioma Etiqueta(TextMeshProUGUI t) => t.GetComponent<TextoIdioma>() ?? t.gameObject.AddComponent<TextoIdioma>();

        static void PrepararSelectores(GameObject canvas)
        {
            void Opciones(string nombre,string[] valores,int inicial = 0)
            {
                var nodo = canvas.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name==nombre);
                if (!nodo) return;
                var selector = nodo.gameObject.AddComponent<OpcionesMuestra>();
                selector.opciones = valores;
                selector.indice = inicial;
                Etiqueta(Hijo<TextMeshProUGUI>(nodo.gameObject,"ValueLabel"));
            }
            string[] Rango(int inicio,int cantidad) => Enumerable.Range(inicio,cantidad).Select(n => n.ToString()).ToArray();
            Opciones("RoomPrivacySelector",new[]{"Pública","Privada"});
            Opciones("PlayerCountSelector",Rango(2,5));
            Opciones("StartingHealthSelector",Rango(1,10),2);
            Opciones("StartingHandSelector",Rango(2,7),3);
            Opciones("TurnTimeSelector",Rango(1,10),4);
            Opciones("EffectsSelector",new[]{"Activados","Desactivados"});
            Opciones("BotDifficultySelector",new[]{"Fácil","Normal","Difícil"},1);
        }

        static void CrearVistaPrevia(string nombre,Transform pantallas,Transform overlays)
        {
            var menu = Dialog(overlays,"PreviewDialog","Vista previa","Elige una vista de muestra.");
            void Opcion(string id,string titulo) => Button(id,menu,titulo);
            Opcion("PreviewNotice","Aviso");
            Opcion("PreviewConfirm","Confirmar");
            Opcion("PreviewLoading","Cargando…");
            Opcion("PreviewToast","Aviso breve");
            if (nombre=="Start" || nombre=="OfflineSetting")
            {
                var vista = nombre=="Start" ? pantallas.Find("ModeSelectionScreen/CenteredLayout")
                    : Hijo<Transform>(pantallas.Find("ActivitySelectionScreen").gameObject,"Content");
                Button("PreviewButton",vista,"Vista previa");
                Opcion("ElementGuide","Ayuda de elementos");
            }
            else if (nombre=="OnlineRooms")
            {
                Opcion("ElementGuide","Ayuda de elementos");
                foreach (Transform pantalla in pantallas)
                    Button("PreviewButton",Hijo<Transform>(pantalla.gameObject,"Content"),"Vista previa");
                Opcion("Host","Anfitrión"); Opcion("Guest","Invitado"); Opcion("Request","Solicitud de acceso");
                Opcion("Match","Mesa"); Opcion("EmptyRoomsPanel","Lista vacía");
                Opcion("LoadingRoomsPanel","Buscando salas…"); Opcion("RoomListErrorPanel","Error de búsqueda");
            }
            else
            {
                Button("PreviewButton",Hijo<Transform>(overlays.Find("PauseDialog").gameObject,"Content"),"Vista previa");
                Opcion("Results","Resultados"); Opcion("Target","Selección de objetivo");
                if (nombre=="OnlineMatch")
                {
                    Opcion("Spectator","Espectador"); Opcion("ConnectionLostDialog","Desconexión");
                    Opcion("HostDisconnectedDialog","Cambio de anfitrión"); Opcion("Banner","Aviso de conexión");
                }
                if (nombre=="OfflineMatch") Opcion("Bot","Estado del bot");
                if (nombre=="Tutorial") Opcion("Help","Ataque y defensa");
            }
            Button("CloseButton",menu,"Cerrar");
        }
    }
}
