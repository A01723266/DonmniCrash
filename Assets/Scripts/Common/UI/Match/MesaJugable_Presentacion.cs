using System.Linq;
using DonmniCrash.Juego;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    public abstract partial class MesaJugable
    {
        readonly Sprite[] espadas = new Sprite[5], escudos = new Sprite[5];
        DominoTileView plantilla;
        int fichasEnMesa;
        readonly Sprite[] simbolos = new Sprite[5];
        readonly TMP_Text[] etiquetasAyuda = new TMP_Text[5];

        string ElementoTexto(Elemento e) => TextoIdioma.Leer("game.element."+(int)e);

        void PrepararArte()
        {
            plantilla = Resources.Load<GameObject>("Prefabs/UI/Gameplay/DominoTileView").GetComponent<DominoTileView>();
            for (int i = 0; i < espadas.Length; i++)
            {
                var fila = Buscar(Preferencias.Emblemas[i]+"HelpRow");
                simbolos[i] = Buscar("ElementIcon",fila).GetComponent<Image>().sprite;
                etiquetasAyuda[i] = Buscar("ElementLabel",fila).GetComponent<TMP_Text>();
                espadas[i] = Buscar("AttackIcon",fila).GetComponent<Image>().sprite;
                escudos[i] = Buscar("DefenseIcon",fila).GetComponent<Image>().sprite;
            }
        }

        // Construir la ayuda desde la tabla de compatibilidad evita mantener dos versiones de las reglas.
        void PintarAyuda()
        {
            for (int i = 0; i < etiquetasAyuda.Length; i++)
            {
                var vence = System.Enum.GetValues(typeof(Elemento)).Cast<Elemento>()
                    .Where(ataque => ReglasJugada.Vence((Elemento)i,ataque)).Select(ElementoTexto).ToArray();
                Escribir(etiquetasAyuda[i],T("game.beats",ElementoTexto((Elemento)i),vence[0],vence[1]));
            }
        }

        // Desactivar antes de destruir evita que el layout cuente dos manos en el mismo frame.
        static void Limpiar(Transform padre)
        {
            foreach (Transform hijo in padre) { hijo.gameObject.SetActive(false); Destroy(hijo.gameObject); }
        }

        void Pintar()
        {
            for (int i = 0; i < 6; i++)
            {
                var tarjeta = Buscar("Player"+(i+1)+"Card");
                tarjeta.gameObject.SetActive(i < Partida.Orden.Count);
                if (i >= Partida.Orden.Count) continue;
                int actor = Partida.Orden[i];
                int emblema = actor == 0 ? System.Array.IndexOf(Preferencias.Emblemas.ToArray(),Preferencias.Emblema) : actor % simbolos.Length;
                Buscar("SeatEmblem",tarjeta).GetComponent<Image>().sprite = simbolos[emblema];
                Escribir(Buscar("PlayerNameLabel",tarjeta).GetComponent<TMP_Text>(),Nombre(actor));
                Escribir(Buscar("PlayerStatsLabel",tarjeta).GetComponent<TMP_Text>(),T("game.stats",Partida.Vida(actor),Partida.Cantidad(actor)));
                Buscar("TurnIndicator",tarjeta).gameObject.SetActive(Partida.Activo == actor && !Terminada);
            }
            Etiquetar("TurnLabel",T("game.turn",Nombre(Partida.Activo)));
            Etiquetar("ReserveCountLabel",T("game.stock",Partida.CantidadReserva));
            Etiquetar("CurrentAttackLabel",Partida.Ataque.HasValue ? T("game.attack",ElementoTexto(Partida.Ataque.Value)) : T("game.open"));
            var icono = Pieza<Image>("CurrentAttackIcon");
            icono.enabled = Partida.Ataque.HasValue;
            if (icono.enabled) icono.sprite = espadas[(int)Partida.Ataque.Value];
            bool avanzar = fichasEnMesa < Partida.Mesa.Count;
            while (fichasEnMesa < Partida.Mesa.Count)
                CrearFicha(historial.content,Partida.Mesa[fichasEnMesa++],-1,false);
            if (avanzar) { Canvas.ForceUpdateCanvases(); historial.horizontalNormalizedPosition = 1; }
            PintarMano();
            PintarControles();
        }

        void PintarMano()
        {
            botonesFicha.Clear();
            Limpiar(mano.content);
            bool eligiendo = Humano && Seleccion;
            if (eligiendo)
            {
                for (int i=0;i<Partida.NumeroOpciones;i++)
                    if (Partida.Disponible(i)) CrearFicha(mano.content,Partida.VerOpcion(0,i),i,true);
            }
            else if (Partida.OfertaDe(0) is Ficha oferta)
            {
                elegido = oferta.Id;
                CrearFicha(mano.content,oferta,oferta.Id,Humano);
            }
            else foreach (var f in Partida.ManoDe(0)) CrearFicha(mano.content,f,f.Id,Humano);
        }

        void CrearFicha(Transform padre,Ficha? ficha,int valor,bool pulsable)
        {
            var vista = Instantiate(plantilla,padre);
            bool ordinaria = ficha.HasValue && ficha.Value.Tipo == TipoFicha.Ordinaria;
            vista.attackIcon.enabled = vista.defenseIcon.enabled = ordinaria;
            vista.selectionOutline.SetActive(pulsable && elegido == valor);
            if (ordinaria)
            {
                vista.attackIcon.sprite = espadas[(int)ficha.Value.Espada];
                vista.defenseIcon.sprite = escudos[(int)ficha.Value.Escudo];
            }
            else
            {
                vista.transform.Find("Separator").gameObject.SetActive(false);
                var texto = new GameObject("Simbolo",typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                texto.transform.SetParent(vista.transform,false);
                texto.rectTransform.anchorMin = Vector2.zero;
                texto.rectTransform.anchorMax = Vector2.one;
                texto.rectTransform.offsetMin = texto.rectTransform.offsetMax = Vector2.zero;
                texto.font = reloj.font;
                texto.fontSize = 22;
                texto.color = Color.white;
                texto.outlineWidth = .2f;
                texto.outlineColor = Color.black;
                texto.alignment = TextAlignmentOptions.Center;
                texto.raycastTarget = false;
                // Símbolos cortos sobre la losa. El nombre completo aparece al seleccionarla.
                texto.text = !ficha.HasValue ? "?\n"+(valor+1) :
                    ficha.Value.Tipo == TipoFicha.Intercambio ? "A/B" : ficha.Value.Tipo == TipoFicha.Reversa ? "REV" : ">>";
            }
            if (!pulsable) return;
            botonesFicha[valor] = vista;
            var superficie = vista.gameObject.AddComponent<Image>();
            superficie.color = Color.clear;
            var boton = vista.gameObject.AddComponent<Button>();
            boton.targetGraphic = superficie;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            boton.onClick.AddListener(() => { elegido = valor; objetivo = -1; PintarControles(); });
        }

        void PintarControles()
        {
            var oferta = Partida.OfertaDe(0);
            var propia = Partida.ManoDe(0);
            Ficha? ficha = oferta ?? propia.Where(f => f.Id == elegido).Select(f => (Ficha?)f).FirstOrDefault();
            bool combo = Humano && Partida.Etapa == Fase.Decision && ficha?.Tipo == TipoFicha.Ordinaria &&
                ReglasJugada.Jugable(ficha.Value,Partida.Ataque) && propia.Any(f=>f.Tipo==TipoFicha.Intercambio) && Partida.Orden.Any(Partida.ObjetivoValido);
            cambiar.gameObject.SetActive(combo);
            Escribir(cambiar.GetComponentInChildren<TMP_Text>(),objetivo < 0 ? T("game.swap") : T("game.target",Nombre(objetivo)));
            robar.interactable = Humano && !Seleccion;
            Escribir(robar.GetComponentInChildren<TMP_Text>(),oferta.HasValue ? T("game.keep") : T("game.draw"));
            jugar.interactable = Humano && elegido >= 0 && (Seleccion || ficha.HasValue && ReglasJugada.Jugable(ficha.Value,Partida.Ataque));
            Escribir(jugar.GetComponentInChildren<TMP_Text>(),Seleccion ? T("game.choose") : T("game.play"));
            foreach (var par in botonesFicha) par.Value.selectionOutline.SetActive(par.Key == elegido);
            string titulo = T("game.hand");
            if (Humano && Seleccion) titulo = T(Partida.SeleccionVisible ? "game.visible" : "game.blind",Partida.Restantes);
            else if (oferta.HasValue) titulo = T("game.offer");
            else if (Humano && ficha.HasValue)
            {
                var f = ficha.Value;
                if (f.Tipo == TipoFicha.Ordinaria) titulo = T("game.tile",ElementoTexto(f.Espada),ElementoTexto(f.Escudo));
                else titulo = T("game.wild."+(int)f.Tipo);
                if (!ReglasJugada.Jugable(f,Partida.Ataque) && f.Tipo != TipoFicha.Intercambio) titulo += " · "+T("game.illegal");
            }
            Etiquetar("HandTitleLabel",titulo);
        }
    }
}
