using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>
    /// Íconos vectoriales dibujados con Painter2D (no necesitan imágenes PNG).
    ///
    /// En UXML:   &lt;ui:VisualElement class="ico ico-candado ico-lg color-acento" /&gt;
    /// En código: Iconos.Crear("estrella", "ico-sm", "color-acento");
    ///
    /// Tamaño: clases ico-xs / ico-sm / ico-md / ico-lg / ico-xl / ico-xxl.
    /// Color:  propiedad "color" del elemento (o la heredada del padre).
    /// Relleno: clase "ico--relleno" (estrella, candado, llama, rayo, bandera, trofeo).
    /// </summary>
    public static class Iconos
    {
        public const string Clase = "ico";
        public const string Relleno = "ico--relleno";
        const string Prefijo = "ico-";
        const string Listo = "ico--listo";

        static readonly CustomStyleProperty<float> Grosor = new CustomStyleProperty<float>("--ico-grosor");
        static readonly HashSet<string> Modificadores = new HashSet<string> { "ico-xs", "ico-sm", "ico-md", "ico-lg", "ico-xl", "ico-xxl", Relleno, Listo };

        /// <summary>Lista de íconos disponibles.</summary>
        public static readonly string[] Disponibles =
        {
            "candado", "candado-abierto", "estrella", "bombilla", "llama", "trofeo", "engranaje", "calendario",
            "reloj", "check", "flecha-izq", "flecha-der", "pausa", "mas", "cerrar", "repetir", "mapa", "rayo",
            "diana", "cuadricula", "bandera", "usuario", "alerta", "anillo", "cerradura"
        };

        /// <summary>Activa todos los elementos con clase "ico" dentro de raiz.</summary>
        public static void Preparar(VisualElement raiz)
        {
            raiz.Query<VisualElement>(className: Clase).ForEach(Registrar);
        }

        public static VisualElement Crear(string tipo, params string[] clases)
        {
            var e = new VisualElement();
            e.AddToClassList(Clase);
            e.AddToClassList(Prefijo + tipo);
            foreach (var c in clases) if (!string.IsNullOrEmpty(c)) e.AddToClassList(c);
            Registrar(e);
            return e;
        }

        public static void Cambiar(VisualElement e, string tipo)
        {
            if (e == null) return;
            foreach (var c in new List<string>(e.GetClasses()))
                if (EsClaseDeTipo(c)) e.RemoveFromClassList(c);
            e.AddToClassList(Prefijo + tipo);
            e.MarkDirtyRepaint();
        }

        public static void Rellenar(VisualElement e, bool relleno)
        {
            if (e == null) return;
            e.EnableInClassList(Relleno, relleno);
            e.MarkDirtyRepaint();
        }

        /// <summary>Vuelve a dibujar los íconos (útil al cambiar de tema).</summary>
        public static void Repintar(VisualElement raiz)
        {
            raiz.Query<VisualElement>(className: Clase).ForEach(e => e.MarkDirtyRepaint());
        }

        static bool EsClaseDeTipo(string c)
        {
            return c.StartsWith(Prefijo) && !Modificadores.Contains(c);
        }

        static string Tipo(VisualElement e)
        {
            foreach (var c in e.GetClasses())
                if (EsClaseDeTipo(c)) return c.Substring(Prefijo.Length);
            return null;
        }

        static void Registrar(VisualElement e)
        {
            if (e.ClassListContains(Listo)) return;
            e.AddToClassList(Listo);
            e.pickingMode = PickingMode.Ignore;
            e.generateVisualContent += ctx => Dibujar(ctx, e);
            e.RegisterCallback<CustomStyleResolvedEvent>(_ => e.MarkDirtyRepaint());
            e.RegisterCallback<GeometryChangedEvent>(_ => e.MarkDirtyRepaint());
        }

        // ------------------------------------------------------------------
        // Dibujo (cuadrícula de 24 x 24, igual que los íconos tipo "Lucide")
        // ------------------------------------------------------------------

        static void Dibujar(MeshGenerationContext ctx, VisualElement e)
        {
            string tipo = Tipo(e);
            if (tipo == null) return;
            Rect r = e.contentRect;
            if (r.width < 1f || r.height < 1f) return;

            float lado = Mathf.Min(r.width, r.height);
            float s = lado / 24f;
            var origen = new Vector2(r.x + (r.width - lado) * 0.5f, r.y + (r.height - lado) * 0.5f);

            float grosor = 2f;
            float g;
            if (e.customStyle.TryGetValue(Grosor, out g)) grosor = g;

            var p = ctx.painter2D;
            Color color = e.resolvedStyle.color;
            p.strokeColor = color;
            p.fillColor = color;
            p.lineWidth = Mathf.Max(1.2f, grosor * s);
            p.lineJoin = LineJoin.Round;
            p.lineCap = LineCap.Round;

            var d = new Lienzo(p, origen, s);
            bool relleno = e.ClassListContains(Relleno);

            switch (tipo)
            {
                case "candado":
                    d.Rect(4.5f, 11f, 15f, 10.5f, 2.5f, relleno);
                    d.Linea(8f, 11f, 8f, 7.5f);
                    d.Arco(12f, 7.5f, 4f, 180f, 360f);
                    d.Linea(16f, 7.5f, 16f, 11f);
                    if (!relleno) d.Punto(12f, 16.2f, 1.4f);
                    break;

                case "candado-abierto":
                    d.Rect(4.5f, 11f, 15f, 10.5f, 2.5f, relleno);
                    d.Linea(8f, 11f, 8f, 7.5f);
                    d.Arco(12f, 7.5f, 4f, 180f, 340f);
                    if (!relleno) d.Punto(12f, 16.2f, 1.4f);
                    break;

                case "estrella":
                {
                    var pts = new float[20];
                    for (int i = 0; i < 10; i++)
                    {
                        float ang = (-90f + i * 36f) * Mathf.Deg2Rad;
                        float rad = i % 2 == 0 ? 10f : 4.3f;
                        pts[i * 2] = 12f + rad * Mathf.Cos(ang);
                        pts[i * 2 + 1] = 12.8f + rad * Mathf.Sin(ang);
                    }
                    d.Poligono(relleno, pts);
                    break;
                }

                case "bombilla":
                    d.Arco(12f, 9.5f, 6.2f, 125f, 415f);
                    d.Linea(8.4f, 14.6f, 9.2f, 17.2f, 14.8f, 17.2f, 15.6f, 14.6f);
                    d.Linea(9.8f, 20.5f, 14.2f, 20.5f);
                    break;

                case "llama":
                    d.Llama(relleno);
                    break;

                case "trofeo":
                    d.Copa(relleno);
                    d.Arco(7f, 8.5f, 3f, 90f, 270f);
                    d.Arco(17f, 8.5f, 3f, -90f, 90f);
                    d.Linea(12f, 15f, 12f, 19f);
                    d.Linea(8f, 20.5f, 16f, 20.5f);
                    break;

                case "engranaje":
                    d.Circulo(12f, 12f, 3f);
                    d.Circulo(12f, 12f, 6.5f);
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * 45f * Mathf.Deg2Rad;
                        float cx = Mathf.Cos(a), cy = Mathf.Sin(a);
                        d.Linea(12f + 6.5f * cx, 12f + 6.5f * cy, 12f + 9.5f * cx, 12f + 9.5f * cy);
                    }
                    break;

                case "calendario":
                    d.Rect(3.5f, 5f, 17f, 16f, 2.5f, false);
                    d.Linea(3.5f, 10f, 20.5f, 10f);
                    d.Linea(8f, 3f, 8f, 7f);
                    d.Linea(16f, 3f, 16f, 7f);
                    break;

                case "reloj":
                    d.Circulo(12f, 12f, 9f);
                    d.Linea(12f, 7f, 12f, 12f, 15.5f, 14f);
                    break;

                case "check":
                    d.Linea(5f, 12.5f, 10f, 17.5f, 19f, 7f);
                    break;

                case "flecha-izq":
                    d.Linea(15f, 5f, 8f, 12f, 15f, 19f);
                    break;

                case "flecha-der":
                    d.Linea(9f, 5f, 16f, 12f, 9f, 19f);
                    break;

                case "pausa":
                    d.Linea(9f, 6f, 9f, 18f);
                    d.Linea(15f, 6f, 15f, 18f);
                    break;

                case "mas":
                    d.Linea(12f, 5f, 12f, 19f);
                    d.Linea(5f, 12f, 19f, 12f);
                    break;

                case "cerrar":
                    d.Linea(6f, 6f, 18f, 18f);
                    d.Linea(18f, 6f, 6f, 18f);
                    break;

                case "repetir":
                    d.Arco(12f, 12f, 7.5f, 210f, 480f);
                    d.Linea(5.5f, 3.5f, 5.5f, 8.2f, 10f, 8.2f);
                    break;

                case "mapa":
                    d.Poligono(false, 3f, 6.5f, 9f, 4f, 15f, 6.5f, 21f, 4f, 21f, 17.5f, 15f, 20f, 9f, 17.5f, 3f, 20f);
                    d.Linea(9f, 4f, 9f, 17.5f);
                    d.Linea(15f, 6.5f, 15f, 20f);
                    break;

                case "rayo":
                    d.Poligono(relleno, 13f, 2f, 4f, 14f, 11f, 14f, 10f, 22f, 20f, 10f, 13f, 10f);
                    break;

                case "diana":
                    d.Circulo(12f, 12f, 9f);
                    d.Circulo(12f, 12f, 5.2f);
                    d.Punto(12f, 12f, 1.8f);
                    break;

                case "cuadricula":
                    for (int x = 0; x < 3; x++)
                        for (int y = 0; y < 3; y++)
                            d.Punto(6f + x * 6f, 6f + y * 6f, 1.9f);
                    break;

                case "bandera":
                    d.Linea(5f, 22f, 5f, 3f);
                    d.Poligono(relleno, 5f, 4f, 19f, 4f, 15.5f, 8.5f, 19f, 13f, 5f, 13f);
                    break;

                case "usuario":
                    d.Circulo(12f, 8f, 4f);
                    d.Arco(12f, 21f, 7.5f, 180f, 360f);
                    break;

                case "alerta":
                    d.Poligono(false, 12f, 3f, 22f, 20.5f, 2f, 20.5f);
                    d.Linea(12f, 9.5f, 12f, 14f);
                    d.Punto(12f, 17.3f, 1.2f);
                    break;

                // Logotipo: la "O" de MINDLOCK es un anillo con una cerradura dentro
                case "anillo":
                    d.Circulo(12f, 12f, 12f - grosor * 0.5f);
                    break;

                case "cerradura":
                    d.Punto(12f, 9f, 4.2f);
                    p.lineWidth = s;
                    d.Poligono(true, 10f, 11f, 14f, 11f, 15.6f, 20.5f, 8.4f, 20.5f);
                    break;
            }
        }

        /// <summary>Ayudante para dibujar en coordenadas de 24 x 24.</summary>
        struct Lienzo
        {
            readonly Painter2D p;
            readonly Vector2 o;
            readonly float s;

            public Lienzo(Painter2D painter, Vector2 origen, float escala)
            {
                p = painter;
                o = origen;
                s = escala;
            }

            Vector2 P(float x, float y) { return new Vector2(o.x + x * s, o.y + y * s); }

            public void Linea(params float[] c)
            {
                p.BeginPath();
                p.MoveTo(P(c[0], c[1]));
                for (int i = 2; i + 1 < c.Length; i += 2) p.LineTo(P(c[i], c[i + 1]));
                p.Stroke();
            }

            public void Poligono(bool rellenar, params float[] c)
            {
                p.BeginPath();
                p.MoveTo(P(c[0], c[1]));
                for (int i = 2; i + 1 < c.Length; i += 2) p.LineTo(P(c[i], c[i + 1]));
                p.ClosePath();
                if (rellenar) p.Fill();
                p.Stroke();
            }

            public void Circulo(float cx, float cy, float r, bool rellenar = false)
            {
                p.BeginPath();
                p.Arc(P(cx, cy), r * s, Angle.Degrees(0f), Angle.Degrees(180f));
                p.Arc(P(cx, cy), r * s, Angle.Degrees(180f), Angle.Degrees(360f));
                p.ClosePath();
                if (rellenar) p.Fill();
                p.Stroke();
            }

            public void Punto(float cx, float cy, float r)
            {
                p.BeginPath();
                p.Arc(P(cx, cy), r * s, Angle.Degrees(0f), Angle.Degrees(180f));
                p.Arc(P(cx, cy), r * s, Angle.Degrees(180f), Angle.Degrees(360f));
                p.ClosePath();
                p.Fill();
            }

            public void Arco(float cx, float cy, float r, float desde, float hasta)
            {
                p.BeginPath();
                p.Arc(P(cx, cy), r * s, Angle.Degrees(desde), Angle.Degrees(hasta));
                p.Stroke();
            }

            public void Rect(float x, float y, float w, float h, float r, bool rellenar)
            {
                p.BeginPath();
                p.MoveTo(P(x + r, y));
                p.LineTo(P(x + w - r, y));
                p.Arc(P(x + w - r, y + r), r * s, Angle.Degrees(270f), Angle.Degrees(360f));
                p.LineTo(P(x + w, y + h - r));
                p.Arc(P(x + w - r, y + h - r), r * s, Angle.Degrees(0f), Angle.Degrees(90f));
                p.LineTo(P(x + r, y + h));
                p.Arc(P(x + r, y + h - r), r * s, Angle.Degrees(90f), Angle.Degrees(180f));
                p.LineTo(P(x, y + r));
                p.Arc(P(x + r, y + r), r * s, Angle.Degrees(180f), Angle.Degrees(270f));
                p.ClosePath();
                if (rellenar) p.Fill();
                p.Stroke();
            }

            public void Llama(bool rellenar)
            {
                p.BeginPath();
                p.MoveTo(P(12f, 22f));
                p.BezierCurveTo(P(7.5f, 22f), P(4.8f, 18.8f), P(5.2f, 15f));
                p.BezierCurveTo(P(5.6f, 11.2f), P(9f, 9.5f), P(10f, 3f));
                p.BezierCurveTo(P(13f, 5.5f), P(14.2f, 8f), P(14.4f, 10.8f));
                p.BezierCurveTo(P(15.5f, 9.8f), P(16f, 8.8f), P(16.2f, 7.6f));
                p.BezierCurveTo(P(18.4f, 10f), P(19.4f, 12.8f), P(18.9f, 15.6f));
                p.BezierCurveTo(P(18.3f, 19.4f), P(15.8f, 22f), P(12f, 22f));
                p.ClosePath();
                if (rellenar) p.Fill();
                p.Stroke();
            }

            public void Copa(bool rellenar)
            {
                p.BeginPath();
                p.MoveTo(P(7f, 4f));
                p.LineTo(P(17f, 4f));
                p.LineTo(P(17f, 9f));
                p.QuadraticCurveTo(P(17f, 15f), P(12f, 15f));
                p.QuadraticCurveTo(P(7f, 15f), P(7f, 9f));
                p.ClosePath();
                if (rellenar) p.Fill();
                p.Stroke();
            }
        }
    }
}
