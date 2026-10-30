using System;
using Mindlock.Datos;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>Funciones comunes para construir y animar la interfaz.</summary>
    public static class UIUtil
    {
        public const string ClaseModalAbierto = "modal-fondo--abierto";
        const long DuracionModal = 200;

        // ------------------------------------------------------------------
        // Mostrar / ocultar
        // ------------------------------------------------------------------

        public static void Mostrar(VisualElement e, bool visible = true)
        {
            if (e != null) e.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void Ocultar(VisualElement e) { Mostrar(e, false); }

        /// <summary>Oculta sin quitar el espacio que ocupa (evita saltos en el diseño).</summary>
        public static void Visible(VisualElement e, bool visible)
        {
            if (e != null) e.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }

        public static bool EstaVisible(VisualElement e)
        {
            return e != null && e.resolvedStyle.display == DisplayStyle.Flex;
        }

        // ------------------------------------------------------------------
        // Modales y hojas inferiores (con animación)
        // ------------------------------------------------------------------

        public static void AbrirModal(VisualElement fondo)
        {
            if (fondo == null) return;
            Mostrar(fondo);
            fondo.schedule.Execute(() => fondo.AddToClassList(ClaseModalAbierto)).StartingIn(16);
        }

        public static void CerrarModal(VisualElement fondo, Action alCerrar = null)
        {
            if (fondo == null) return;
            fondo.RemoveFromClassList(ClaseModalAbierto);
            fondo.schedule.Execute(() =>
            {
                Ocultar(fondo);
                if (alCerrar != null) alCerrar();
            }).StartingIn(DuracionModal);
        }

        public static void CerrarModalInmediato(VisualElement fondo)
        {
            if (fondo == null) return;
            fondo.RemoveFromClassList(ClaseModalAbierto);
            Ocultar(fondo);
        }

        // ------------------------------------------------------------------
        // Construcción
        // ------------------------------------------------------------------

        /// <summary>Quita el estilo por defecto de Unity a todos los botones (usamos el nuestro).</summary>
        public static void LimpiarBotones(VisualElement raiz)
        {
            raiz.Query<Button>().ForEach(b => b.RemoveFromClassList(Button.ussClassName));
        }

        public static Button Boton(string texto, params string[] clases)
        {
            var b = new Button();
            b.RemoveFromClassList(Button.ussClassName);
            if (texto != null) b.text = texto;
            Clases(b, clases);
            return b;
        }

        public static Label Texto(string texto, params string[] clases)
        {
            var l = new Label(texto);
            Clases(l, clases);
            return l;
        }

        public static VisualElement Caja(params string[] clases)
        {
            var e = new VisualElement();
            Clases(e, clases);
            return e;
        }

        static void Clases(VisualElement e, string[] clases)
        {
            if (clases == null) return;
            foreach (var c in clases) if (!string.IsNullOrEmpty(c)) e.AddToClassList(c);
        }

        /// <summary>Los hijos no capturan toques: el toque llega al botón padre.</summary>
        public static void IgnorarToquesHijos(VisualElement padre)
        {
            padre.Query<VisualElement>().ForEach(e => { if (e != padre) e.pickingMode = PickingMode.Ignore; });
        }

        public static void OcultarBarras(ScrollView scroll)
        {
            if (scroll == null) return;
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        /// <summary>Asigna texto solo si cambió (evita recalcular el diseño cada fotograma).</summary>
        public static void PonerTexto(Label l, string texto)
        {
            if (l != null && l.text != texto) l.text = texto;
        }

        public static void Placeholder(TextField campo, string texto)
        {
#if UNITY_2023_2_OR_NEWER
            if (campo != null) campo.textEdition.placeholder = texto;
#endif
        }

        /// <summary>Cambia el modificador de una etiqueta (ej. "etiqueta--media" por "etiqueta--dificil").</summary>
        public static void Etiqueta(Label l, string texto, string modificador)
        {
            if (l == null) return;
            foreach (var c in new System.Collections.Generic.List<string>(l.GetClasses()))
                if (c.StartsWith("etiqueta--")) l.RemoveFromClassList(c);
            l.AddToClassList("etiqueta--" + modificador);
            l.text = texto;
        }

        public static VisualElement Avatar(Jugador j, params string[] clases)
        {
            var a = Caja("avatar");
            Clases(a, clases);
            a.Add(Texto(j.Inicial, "avatar__inicial"));
            return a;
        }

        /// <summary>Ícono pequeño + texto (ej. estrella "38/90").</summary>
        public static VisualElement Dato(string icono, string texto)
        {
            var d = Caja("dato");
            d.Add(Iconos.Crear(icono, "ico-xs"));
            d.Add(Texto(texto, "dato__texto"));
            return d;
        }

        /// <summary>Fila de 3 estrellas, rellenas según "llenas".</summary>
        public static VisualElement Estrellas(int llenas, string tamano, params string[] clases)
        {
            var fila = Caja("fila");
            Clases(fila, clases);
            for (int i = 0; i < Reglas.MaxEstrellas; i++)
            {
                bool llena = i < llenas;
                var e = Iconos.Crear("estrella", tamano, llena ? "color-acento" : "color-tenue", llena ? Iconos.Relleno : null);
                if (i > 0) e.style.marginLeft = 2;
                fila.Add(e);
            }
            return fila;
        }

        /// <summary>Fila con interruptor: toda la fila es tocable.</summary>
        public static void Interruptor(VisualElement fila, bool valorInicial, Action<bool> alCambiar)
        {
            if (fila == null) return;
            var sw = fila.Q(className: "interruptor");
            bool estado = valorInicial;
            if (sw != null) sw.EnableInClassList("interruptor--activo", estado);
            fila.RegisterCallback<ClickEvent>(_ =>
            {
                estado = !estado;
                if (sw != null) sw.EnableInClassList("interruptor--activo", estado);
                if (alCambiar != null) alCambiar(estado);
            });
        }

        // ------------------------------------------------------------------
        // Animaciones
        // ------------------------------------------------------------------

        static readonly float[] PasosSacudida = { -10f, 10f, -8f, 8f, -5f, 5f, -2f, 0f };

        /// <summary>Sacudida horizontal (respuesta incorrecta, nivel bloqueado...).</summary>
        public static void Sacudir(VisualElement e)
        {
            if (e == null) return;
            int i = 0;
            IVisualElementScheduledItem tarea = null;
            tarea = e.schedule.Execute(() =>
            {
                if (i < PasosSacudida.Length)
                {
                    e.style.translate = new Translate(new Length(PasosSacudida[i]), new Length(0f), 0f);
                    i++;
                }
                else
                {
                    e.style.translate = StyleKeyword.Null;
                    tarea.Pause();
                }
            }).Every(35);
        }

        // ------------------------------------------------------------------
        // Área segura (notch y barra inferior del teléfono)
        // ------------------------------------------------------------------

        public static void AplicarSafeArea(VisualElement elemento, Action<float> alCalcularMargenSuperior = null)
        {
            if (elemento == null) return;
            Rect ultima = new Rect(-1, -1, -1, -1);
            EventCallback<GeometryChangedEvent> calcular = _ =>
            {
                var panel = elemento.panel;
                if (panel == null) return;
                Rect area = Screen.safeArea;
                if (area == ultima) return;
                ultima = area;

                // Distancias del área segura a cada borde, convertidas a unidades del panel
                Vector2 arribaIzq = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(area.xMin, Screen.height - area.yMax));
                Vector2 abajoDer = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width - area.xMax, area.yMin));

                elemento.style.paddingTop = arribaIzq.y;
                elemento.style.paddingBottom = abajoDer.y;
                elemento.style.paddingLeft = arribaIzq.x;
                elemento.style.paddingRight = abajoDer.x;
                if (alCalcularMargenSuperior != null) alCalcularMargenSuperior(arribaIzq.y);
            };
            elemento.RegisterCallback(calcular);
        }
    }
}
