using System;
using Mindlock.Datos;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>PANTALLA 8 - Logros y estadísticas del jugador.</summary>
    public class PantallaLogros : Pantalla
    {
        public override bool MuestraNavegacion { get { return true; } }

        Label lblJugador, lblContador;
        VisualElement gridEstadisticas, listaDificultades, gridLogros, barraLogros;
        VisualElement hojaLogro, hojaInsignia, hojaIcono, barraHojaCont, barraHoja;
        Label lblHojaNombre, lblHojaDescripcion, lblHojaEstado;

        protected override void Configurar()
        {
            lblJugador = Q<Label>("lblJugadorLogros");
            lblContador = Q<Label>("lblContadorLogros");
            gridEstadisticas = Q<VisualElement>("gridEstadisticas");
            listaDificultades = Q<VisualElement>("listaDificultades");
            gridLogros = Q<VisualElement>("gridLogros");
            barraLogros = Q<VisualElement>("barraLogros");
            hojaLogro = Q<VisualElement>("hojaLogro");
            hojaInsignia = Q<VisualElement>("hojaLogroInsignia");
            hojaIcono = Q<VisualElement>("hojaLogroIcono");
            barraHojaCont = Q<VisualElement>("barraHojaLogroCont");
            barraHoja = Q<VisualElement>("barraHojaLogro");
            lblHojaNombre = Q<Label>("lblHojaLogroNombre");
            lblHojaDescripcion = Q<Label>("lblHojaLogroDescripcion");
            lblHojaEstado = Q<Label>("lblHojaLogroEstado");
            UIUtil.OcultarBarras(Q<ScrollView>("scrollLogros"));

            Boton("botonCerrarLogro", () => UIUtil.CerrarModal(hojaLogro));
            hojaLogro.RegisterCallback<ClickEvent>(e => { if (e.target == hojaLogro) UIUtil.CerrarModal(hojaLogro); });
        }

        public override void AlMostrar(object datos)
        {
            var j = Sesion.Jugador;
            if (j == null) { ui.IrA(PantallaId.Perfil); return; }
            UIUtil.CerrarModalInmediato(hojaLogro);

            lblJugador.text = j.Nombre;

            // Estadísticas
            int completados = Servicios.Progreso.NivelesCompletados(j.Id);
            gridEstadisticas.Clear();
            gridEstadisticas.Add(Estadistica("trofeo", "color-acento", Formato.Numero(j.PuntajeTotal), "Puntaje total"));
            gridEstadisticas.Add(Estadistica("estrella", "color-acento", Servicios.Progreso.EstrellasTotales(j.Id) + "/" + (Reglas.TotalNiveles * Reglas.MaxEstrellas), "Estrellas"));
            gridEstadisticas.Add(Estadistica("candado-abierto", "color-accion", completados + "/" + Reglas.TotalNiveles, "Niveles completados"));
            int racha = Servicios.Diario.RachaActual(j.Id);
            gridEstadisticas.Add(Estadistica("llama", "color-llama", racha.ToString(), racha == 1 ? "Día de racha" : "Días de racha"));

            // Avance por dificultad
            listaDificultades.Clear();
            foreach (Dificultad d in Enum.GetValues(typeof(Dificultad)))
            {
                int total = 0, hechos = 0;
                foreach (var n in Servicios.Progreso.ObtenerNiveles())
                {
                    if (n.Dificultad != d) continue;
                    total++;
                    if (Servicios.Progreso.ObtenerProgreso(j.Id, n.Numero).Estado == EstadoNivel.Completado) hechos++;
                }
                var fila = UIUtil.Caja("fila-dificultad");
                fila.Add(UIUtil.Texto(Formato.NombreDificultad(d), "fila-dificultad__nombre"));
                var barra = UIUtil.Caja("barra", "crecer");
                var relleno = UIUtil.Caja("barra__relleno", "fondo--" + Formato.Clave(d));
                relleno.style.width = Length.Percent(total == 0 ? 0f : 100f * hechos / total);
                barra.Add(relleno);
                fila.Add(barra);
                fila.Add(UIUtil.Texto(hechos + "/" + total, "fila-dificultad__cuenta"));
                listaDificultades.Add(fila);
            }

            // Logros
            var logros = Servicios.Logros.ObtenerLogros(j.Id);
            int obtenidos = 0;
            foreach (var l in logros) if (l.Obtenido) obtenidos++;
            lblContador.text = obtenidos + " de " + logros.Count + " logros obtenidos";
            barraLogros.style.width = Length.Percent(logros.Count == 0 ? 0f : 100f * obtenidos / logros.Count);

            gridLogros.Clear();
            foreach (var l in logros) gridLogros.Add(CrearInsignia(l));
        }

        public override bool AlPulsarAtras()
        {
            if (UIUtil.EstaVisible(hojaLogro)) { UIUtil.CerrarModal(hojaLogro); return true; }
            return false;
        }

        static VisualElement Estadistica(string icono, string color, string valor, string etiqueta)
        {
            var celda = UIUtil.Caja("estadistica");
            var t = UIUtil.Caja("estadistica__tarjeta");
            t.Add(Iconos.Crear(icono, "ico-md", color));
            t.Add(UIUtil.Texto(valor, "estadistica__valor"));
            t.Add(UIUtil.Texto(etiqueta, "estadistica__etiqueta"));
            celda.Add(t);
            return celda;
        }

        VisualElement CrearInsignia(LogroJugador l)
        {
            var celda = UIUtil.Caja("insignia-celda");
            var b = UIUtil.Boton(null, "insignia", l.Obtenido ? "insignia--obtenida" : "insignia--pendiente");

            var circulo = UIUtil.Caja("insignia__circulo");
            circulo.Add(Iconos.Crear(l.Logro.Icono, "ico-lg", l.Obtenido ? "color-acento" : "color-tenue"));
            if (!l.Obtenido)
            {
                var cand = UIUtil.Caja("insignia__candado");
                cand.Add(Iconos.Crear("candado", "ico-xs", "color-sec"));
                circulo.Add(cand);
            }
            b.Add(circulo);
            b.Add(UIUtil.Texto(l.Logro.Nombre, "insignia__nombre"));

            string detalle;
            if (l.Obtenido && l.Fecha.HasValue) detalle = Formato.FechaCorta(l.Fecha.Value);
            else if (l.Logro.Meta > 1) detalle = l.Progreso + "/" + l.Logro.Meta;
            else detalle = "Pendiente";
            b.Add(UIUtil.Texto(detalle, "insignia__detalle"));

            UIUtil.IgnorarToquesHijos(b);
            b.clicked += () => AbrirHoja(l);
            celda.Add(b);
            return celda;
        }

        void AbrirHoja(LogroJugador l)
        {
            Iconos.Cambiar(hojaIcono, l.Logro.Icono);
            hojaIcono.EnableInClassList("color-acento", l.Obtenido);
            hojaIcono.EnableInClassList("color-tenue", !l.Obtenido);
            hojaInsignia.EnableInClassList("insignia__circulo--pendiente", !l.Obtenido);

            lblHojaNombre.text = l.Logro.Nombre;
            lblHojaDescripcion.text = l.Logro.Descripcion;

            if (l.Obtenido)
            {
                lblHojaEstado.text = l.Fecha.HasValue ? "Obtenido el " + Formato.FechaCorta(l.Fecha.Value) : "Obtenido";
                lblHojaEstado.EnableInClassList("pastilla--exito", true);
                lblHojaEstado.EnableInClassList("pastilla--suave", false);
                UIUtil.Ocultar(barraHojaCont);
            }
            else
            {
                lblHojaEstado.text = "Progreso: " + l.Progreso + " de " + l.Logro.Meta;
                lblHojaEstado.EnableInClassList("pastilla--exito", false);
                lblHojaEstado.EnableInClassList("pastilla--suave", true);
                UIUtil.Mostrar(barraHojaCont, l.Logro.Meta > 1);
                barraHoja.style.width = Length.Percent(100f * l.Progreso / l.Logro.Meta);
            }

            UIUtil.AbrirModal(hojaLogro);
        }
    }
}
