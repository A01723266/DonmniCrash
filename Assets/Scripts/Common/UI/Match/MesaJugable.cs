using System;
using System.Collections.Generic;
using System.Linq;
using DonmniCrash.Juego;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DonmniCrash.UI
{
    /// <summary>Traduce toques en comandos de la mesa. Solo el motor puede modificar una mano o un turno.</summary>
    public abstract partial class MesaJugable : MesaNavegable
    {
        protected MotorPartida Partida { get; private set; }
        protected override bool MesaInteractiva => true;
        protected virtual bool Guiada => false;
        protected virtual bool BloquearAcciones => false;
        protected virtual bool Permitir(Accion a) => true;
        protected virtual void AlAceptar(Accion a) { }
        protected virtual void AlCambiarIdioma() { }
        protected virtual void ActuarBot() { }
        protected virtual void Finalizar() { }
        protected bool Terminada => Partida.Etapa == Fase.Fin || Partida.Vida(0) == 0;
        bool Humano => Partida.Activo == 0 && !Terminada && !BloquearAcciones;
        bool Seleccion => Partida.Etapa == Fase.SeleccionCambio || Partida.Etapa == Fase.CompletarMano;
        float segundos, esperaBot;
        int segundoVisible, elegido = -1, objetivo = -1;
        Button jugar, robar, cambiar;
        ScrollRect mano, historial;
        TMP_Text reloj;
        readonly Dictionary<int,DominoTileView> botonesFicha = new Dictionary<int,DominoTileView>();

        protected override void Conectar()
        {
            base.Conectar();
            mano = Pieza<ScrollRect>("HandScrollView");
            historial = Pieza<ScrollRect>("HistoryScroll");
            reloj = Pieza<TMP_Text>("TurnTimerLabel");
            jugar = Pieza<Button>("PlayTileButton");
            robar = Pieza<Button>("DrawTileButton");
            cambiar = Pieza<Button>("PassTurnButton");
            jugar.onClick.AddListener(() => Enviar(new Accion(Seleccion ? Movimiento.Elegir : Movimiento.Colocar,elegido,objetivo)));
            robar.onClick.AddListener(() => Enviar(new Accion(Partida.Etapa == Fase.Oferta ? Movimiento.Conservar : Movimiento.Robar)));
            cambiar.onClick.AddListener(CambiarObjetivo);
            PrepararArte();
            PintarAyuda();
            AspectoSesion.Cambio += RefrescarIdioma;
        }

        void OnDestroy() => AspectoSesion.Cambio -= RefrescarIdioma;
        void RefrescarIdioma()
        {
            if (Partida == null) return;
            PintarAyuda();
            Pintar();
            segundoVisible = -1;
            PintarReloj();
            AlCambiarIdioma();
        }

        protected void Iniciar(MotorPartida motor)
        {
            Partida = motor;
            elegido = objetivo = -1;
            Limpiar(historial.content);
            fichasEnMesa = 0;
            ReiniciarReloj();
            Pintar();
        }

        void ReiniciarReloj()
        {
            segundos = Partida.Config.Segundos;
            esperaBot = Mathf.Min(.65f,segundos * .45f);
            segundoVisible = -1;
            PintarReloj();
        }

        void PintarReloj()
        {
            int entero = Mathf.CeilToInt(segundos);
            if (entero == segundoVisible) return;
            segundoVisible = entero;
            Escribir(reloj,Guiada ? T("game.guided") : T("game.time",entero));
        }

        // El reloj tampoco consume tiempo mientras un diálogo anidado de ajustes o ayuda está abierto.
        protected void Update()
        {
            if (Partida == null || Guiada || Interrumpida || Terminada) return;
            segundos = Mathf.Max(0,segundos - Time.unscaledDeltaTime);
            esperaBot -= Time.unscaledDeltaTime;
            PintarReloj();
            if (segundos <= 0)
            {
                Partida.Vencer();
                TrasAccion(true);
            }
            else if (Partida.Activo != 0 && esperaBot <= 0) ActuarBot();
        }

        protected void Enviar(Accion a)
        {
            if (Interrumpida || Terminada || BloquearAcciones) return;
            if (!Permitir(a)) { Avisar("game.trylesson"); return; }
            int actor = Partida.Activo;
            var fase = Partida.Etapa;
            if (!Partida.Ejecutar(actor,Partida.Revision,a)) { Avisar("game.illegal"); return; }
            AlAceptar(a);
            bool mismaSeleccion = a.Tipo == Movimiento.Elegir && Partida.Activo == actor && Partida.Etapa == fase;
            TrasAccion(!mismaSeleccion);
        }

        void TrasAccion(bool reiniciar)
        {
            elegido = objetivo = -1;
            if (reiniciar) ReiniciarReloj();
            else esperaBot = Mathf.Min(.28f,segundos * .3f);
            Pintar();
            if (Terminada) Finalizar();
        }

        // El destino elegido siempre aparece en el botón antes de confirmar la jugada.
        void CambiarObjetivo()
        {
            var destinos = Partida.Orden.Where(Partida.ObjetivoValido).Prepend(-1).ToArray();
            objetivo = destinos[(Array.IndexOf(destinos,objetivo) + 1) % destinos.Length];
            PintarControles();
        }

        protected static string T(string clave,params object[] args) => string.Format(TextoIdioma.Leer(clave),args);
        protected static string Nombre(int id) => id == 0 ? (Preferencias.Nombre.Length > 0 ? Preferencias.Nombre : T("player")) : "Bot " + id;

        // Literal conserva el estado dinámico cuando Unity reactiva una etiqueta localizada.
        protected static void Escribir(TMP_Text label,string valor)
        {
            label.richText = false;
            var idioma = label.GetComponent<TextoIdioma>();
            if (idioma) idioma.Literal(valor);
            else label.text = valor;
        }

        protected void Etiquetar(string nombre,string valor,string panel = null) => Escribir(Pieza<TMP_Text>(nombre,panel),valor);
    }
}
