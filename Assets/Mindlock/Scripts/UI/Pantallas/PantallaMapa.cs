using System;
using System.Collections.Generic;
using Mindlock.Datos;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>PANTALLA 2 - Menú principal con el mapa de los 30 niveles.</summary>
    public class PantallaMapa : Pantalla
    {
        public override bool MuestraNavegacion { get { return true; } }

        Label lblInicial, lblNombre, lblPuntaje, lblPistas, lblRacha;
        Label lblFechaDiario, lblBonusDiario, lblEstadoDiario, lblProgreso;
        VisualElement barraProgreso, contenedorNiveles;
        ScrollView scroll;

        VisualElement hojaNivel, hojaEstrellas;
        Label lblHojaTitulo, etqHojaDificultad, etqHojaCategoria, lblHojaBase, lblHojaTiempo, lblHojaMejor;
        Button botonEmpezar;
        Nivel nivelSeleccionado;

        readonly List<Button> nodos = new List<Button>();
        readonly List<Vector2> puntosCamino = new List<Vector2>();
        Button nodoActual;
        IVisualElementScheduledItem pulso;
        bool pulsoArriba;

        // Posición horizontal (%) de cada nodo para formar el camino en zigzag
        static readonly float[] Zigzag = { 50f, 76f, 50f, 24f };

        protected override void Configurar()
        {
            lblInicial = Q<Label>("lblInicial");
            lblNombre = Q<Label>("lblNombreJugador");
            lblPuntaje = Q<Label>("lblPuntaje");
            lblPistas = Q<Label>("lblPistas");
            lblRacha = Q<Label>("lblRacha");
            lblFechaDiario = Q<Label>("lblFechaDiario");
            lblBonusDiario = Q<Label>("lblBonusDiario");
            lblEstadoDiario = Q<Label>("lblEstadoDiario");
            lblProgreso = Q<Label>("lblProgreso");
            barraProgreso = Q<VisualElement>("barraProgreso");
            contenedorNiveles = Q<VisualElement>("contenedorNiveles");
            scroll = Q<ScrollView>("scrollMapa");
            UIUtil.OcultarBarras(scroll);

            hojaNivel = Q<VisualElement>("hojaNivel");
            hojaEstrellas = Q<VisualElement>("hojaEstrellas");
            lblHojaTitulo = Q<Label>("lblHojaTitulo");
            etqHojaDificultad = Q<Label>("etqHojaDificultad");
            etqHojaCategoria = Q<Label>("etqHojaCategoria");
            lblHojaBase = Q<Label>("lblHojaBase");
            lblHojaTiempo = Q<Label>("lblHojaTiempo");
            lblHojaMejor = Q<Label>("lblHojaMejor");

            Boton("tarjetaDiario", () => ui.IrA(PantallaId.Diario));
            Boton("botonCerrarHoja", () => UIUtil.CerrarModal(hojaNivel));
            botonEmpezar = Boton("botonEmpezar", () =>
            {
                if (nivelSeleccionado == null) return;
                int numero = nivelSeleccionado.Numero;
                UIUtil.CerrarModal(hojaNivel, () => ui.IrA(PantallaId.Juego, ParametrosJuego.DeNivel(numero)));
            });

            // Tocar el fondo oscuro también cierra la hoja
            hojaNivel.RegisterCallback<ClickEvent>(e => { if (e.target == hojaNivel) UIUtil.CerrarModal(hojaNivel); });

            // Camino punteado entre nodos
            contenedorNiveles.generateVisualContent += DibujarCamino;
            contenedorNiveles.RegisterCallback<GeometryChangedEvent>(_ => CalcularCamino());

            pulso = Raiz.schedule.Execute(Pulsar).Every(900);
            pulso.Pause();
        }

        public override void AlMostrar(object datos)
        {
            var j = Sesion.Jugador;
            if (j == null) { ui.IrA(PantallaId.Perfil); return; }

            UIUtil.CerrarModalInmediato(hojaNivel);

            // Encabezado
            lblInicial.text = j.Inicial;
            lblNombre.text = j.Nombre;
            lblPuntaje.text = Formato.Numero(j.PuntajeTotal);
            lblPistas.text = j.PistasDisponibles.ToString();
            lblRacha.text = Servicios.Diario.RachaActual(j.Id).ToString();

            // Acertijo diario
            bool resuelto = Servicios.Diario.ResueltoHoy(j.Id);
            lblFechaDiario.text = Formato.Fecha(DateTime.Today);
            lblBonusDiario.text = resuelto ? "Vuelve mañana por otro" : "+" + Reglas.BonusDiario + " pts bonus";
            lblEstadoDiario.text = resuelto ? "Resuelto" : "Jugar";
            lblEstadoDiario.EnableInClassList("pastilla--acento", !resuelto);
            lblEstadoDiario.EnableInClassList("pastilla--exito", resuelto);

            // Progreso general
            int completados = Servicios.Progreso.NivelesCompletados(j.Id);
            lblProgreso.text = completados + " de " + Reglas.TotalNiveles + " niveles";
            barraProgreso.style.width = Length.Percent(100f * completados / Reglas.TotalNiveles);

            ConstruirNiveles(j);
            pulso.Resume();

            // Desplazar hasta el nivel que toca jugar
            scroll.schedule.Execute(() =>
            {
                CalcularCamino();
                if (nodoActual != null) scroll.ScrollTo(nodoActual);
            }).StartingIn(80);
        }

        public override void AlOcultar()
        {
            pulso.Pause();
        }

        public override bool AlPulsarAtras()
        {
            if (UIUtil.EstaVisible(hojaNivel)) UIUtil.CerrarModal(hojaNivel);
            return true;
        }

        // ------------------------------------------------------------------
        // Niveles
        // ------------------------------------------------------------------

        void ConstruirNiveles(Jugador j)
        {
            contenedorNiveles.Clear();
            nodos.Clear();
            nodoActual = null;

            var niveles = Servicios.Progreso.ObtenerNiveles();
            bool primera = true;
            Dificultad seccion = Dificultad.Facil;
            int indice = 0;

            foreach (var n in niveles)
            {
                if (primera || n.Dificultad != seccion)
                {
                    primera = false;
                    seccion = n.Dificultad;
                    contenedorNiveles.Add(CrearEncabezadoSeccion(j, seccion, niveles));
                }
                var p = Servicios.Progreso.ObtenerProgreso(j.Id, n.Numero);
                contenedorNiveles.Add(CrearFilaNodo(n, p, indice++));
            }
        }

        VisualElement CrearEncabezadoSeccion(Jugador j, Dificultad d, IReadOnlyList<Nivel> niveles)
        {
            int total = 0, hechos = 0;
            foreach (var n in niveles)
            {
                if (n.Dificultad != d) continue;
                total++;
                if (Servicios.Progreso.ObtenerProgreso(j.Id, n.Numero).Estado == EstadoNivel.Completado) hechos++;
            }

            var fila = UIUtil.Caja("seccion-niveles");
            fila.Add(UIUtil.Caja("seccion-niveles__punto", "fondo--" + Formato.Clave(d)));
            fila.Add(UIUtil.Texto(Formato.NombreDificultad(d), "seccion-niveles__titulo"));
            fila.Add(UIUtil.Caja("crecer"));
            fila.Add(UIUtil.Texto(hechos + "/" + total, "texto-sec"));
            return fila;
        }

        VisualElement CrearFilaNodo(Nivel n, ProgresoNivel p, int indice)
        {
            var fila = UIUtil.Caja("fila-nodo");
            var col = UIUtil.Caja("nodo-col");
            col.style.left = Length.Percent(Zigzag[indice % Zigzag.Length]);

            string estado = p.Estado.ToString().ToLowerInvariant();   // bloqueado / desbloqueado / completado
            var nodo = UIUtil.Boton(null, "nodo", "nodo--" + estado);

            if (p.Estado == EstadoNivel.Bloqueado)
            {
                nodo.Add(Iconos.Crear("candado", "ico-md"));
            }
            else
            {
                nodo.Add(UIUtil.Texto(n.Numero.ToString(), "nodo__numero"));
            }

            if (p.Estado == EstadoNivel.Completado)
            {
                var insignia = UIUtil.Caja("nodo__insignia");
                insignia.Add(Iconos.Crear("candado-abierto", "ico-xs", "color-acento"));
                nodo.Add(insignia);
            }

            UIUtil.IgnorarToquesHijos(nodo);
            col.Add(nodo);

            switch (p.Estado)
            {
                case EstadoNivel.Completado:
                    col.Add(UIUtil.Estrellas(p.Estrellas, "ico-sm", "mt-xs"));
                    col.Add(UIUtil.Texto(Formato.Puntos(p.MejorPuntaje), "nodo__puntaje"));
                    break;
                case EstadoNivel.Desbloqueado:
                    col.Add(UIUtil.Texto("Jugar", "pastilla", "pastilla--accion", "mt-xs"));
                    nodoActual = nodo;
                    break;
                default:
                    col.Add(UIUtil.Texto("Nivel " + n.Numero, "nodo__puntaje", "nodo__puntaje--tenue", "mt-xs"));
                    break;
            }

            nodo.clicked += () =>
            {
                if (p.Estado == EstadoNivel.Bloqueado)
                {
                    UIUtil.Sacudir(nodo);
                    ui.MostrarAviso("Completa el nivel anterior para abrir este candado.", "candado");
                }
                else
                {
                    AbrirHoja(n, p);
                }
            };

            nodos.Add(nodo);
            fila.Add(col);
            return fila;
        }

        void AbrirHoja(Nivel n, ProgresoNivel p)
        {
            nivelSeleccionado = n;
            var a = Servicios.Acertijos.ObtenerAcertijo(n.AcertijoId);

            lblHojaTitulo.text = "Nivel " + n.Numero;
            UIUtil.Etiqueta(etqHojaDificultad, Formato.NombreDificultad(n.Dificultad), Formato.Clave(n.Dificultad));
            UIUtil.Etiqueta(etqHojaCategoria, Formato.NombreCategoria(a.Categoria), Formato.Clave(a.Categoria));
            lblHojaBase.text = Formato.Puntos(n.PuntajeBase);
            lblHojaTiempo.text = n.TieneLimite ? Formato.Tiempo(n.TiempoLimite) : "Sin límite";
            lblHojaMejor.text = p.Estado == EstadoNivel.Completado ? Formato.Puntos(p.MejorPuntaje) : "-";
            hojaEstrellas.Clear();
            hojaEstrellas.Add(UIUtil.Estrellas(p.Estrellas, "ico-md"));
            botonEmpezar.text = p.Estado == EstadoNivel.Completado ? "Jugar de nuevo" : "Empezar";

            UIUtil.AbrirModal(hojaNivel);
        }

        void Pulsar()
        {
            if (nodoActual == null) return;
            pulsoArriba = !pulsoArriba;
            nodoActual.EnableInClassList("nodo--pulso", pulsoArriba);
        }

        // ------------------------------------------------------------------
        // Camino punteado
        // ------------------------------------------------------------------

        void CalcularCamino()
        {
            puntosCamino.Clear();
            foreach (var nodo in nodos)
                puntosCamino.Add(contenedorNiveles.WorldToLocal(nodo.worldBound.center));
            contenedorNiveles.MarkDirtyRepaint();
        }

        void DibujarCamino(MeshGenerationContext ctx)
        {
            if (puntosCamino.Count < 2) return;
            var p = ctx.painter2D;
            Color c = contenedorNiveles.resolvedStyle.color;
            c.a = 0.45f;
            p.strokeColor = c;
            p.lineWidth = 3f;
            p.lineCap = LineCap.Round;

            const float trazo = 7f, hueco = 9f;
            for (int i = 0; i < puntosCamino.Count - 1; i++)
            {
                Vector2 a = puntosCamino[i], b = puntosCamino[i + 1];
                float largo = Vector2.Distance(a, b);
                if (largo < 1f) continue;
                Vector2 dir = (b - a) / largo;
                // Empezar y terminar fuera de los círculos de los nodos
                for (float t = 40f; t < largo - 40f; t += trazo + hueco)
                {
                    float fin = Mathf.Min(t + trazo, largo - 40f);
                    p.BeginPath();
                    p.MoveTo(a + dir * t);
                    p.LineTo(a + dir * fin);
                    p.Stroke();
                }
            }
        }
    }
}
