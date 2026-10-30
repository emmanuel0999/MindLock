using System;
using System.Collections.Generic;
using Mindlock.Datos;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>PANTALLA 6 - Nivel completado. Recibe un ResultadoNivel en AlMostrar.</summary>
    public class PantallaResultado : Pantalla
    {
        VisualElement candado, icoResultado, avisoDesbloqueo, avisoPista;
        Label insigniaRecord;
        readonly VisualElement[] estrellas = new VisualElement[3];
        Label lblTitulo, lblPuntaje, lblBase, lblTiempoNombre, lblDescTiempo, lblIntentosNombre, lblDescIntentos;
        Label lblPistasNombre, lblDescPistas, lblTotal, lblDesbloqueo;
        Button botonSiguiente;
        ScrollView scroll;

        ResultadoNivel resultado;
        int siguienteNivel;   // 0 = no hay siguiente disponible
        readonly List<IVisualElementScheduledItem> animaciones = new List<IVisualElementScheduledItem>();

        protected override void Configurar()
        {
            candado = Q<VisualElement>("candadoResultado");
            icoResultado = Q<VisualElement>("icoResultado");
            for (int i = 0; i < 3; i++) estrellas[i] = Q<VisualElement>("estrella" + (i + 1));
            lblTitulo = Q<Label>("lblTituloResultado");
            lblPuntaje = Q<Label>("lblPuntajeResultado");
            insigniaRecord = Q<Label>("insigniaRecord");
            lblBase = Q<Label>("lblBase");
            lblTiempoNombre = Q<Label>("lblTiempoNombre");
            lblDescTiempo = Q<Label>("lblDescTiempo");
            lblIntentosNombre = Q<Label>("lblIntentosNombre");
            lblDescIntentos = Q<Label>("lblDescIntentos");
            lblPistasNombre = Q<Label>("lblPistasNombre");
            lblDescPistas = Q<Label>("lblDescPistas");
            lblTotal = Q<Label>("lblTotal");
            avisoDesbloqueo = Q<VisualElement>("avisoDesbloqueo");
            lblDesbloqueo = Q<Label>("lblDesbloqueo");
            avisoPista = Q<VisualElement>("avisoPista");
            scroll = Q<ScrollView>("scrollResultado");
            UIUtil.OcultarBarras(scroll);

            botonSiguiente = Boton("botonSiguiente", Siguiente);
            Boton("botonRepetir", () => { if (resultado != null) ui.IrA(PantallaId.Juego, ParametrosJuego.DeNivel(resultado.NumeroNivel)); });
            Boton("botonMapaResultado", () => ui.IrA(PantallaId.Mapa));
        }

        public override void AlMostrar(object datos)
        {
            resultado = datos as ResultadoNivel;
            var j = Sesion.Jugador;
            if (resultado == null || j == null) { ui.IrA(PantallaId.Mapa); return; }
            var r = resultado;

            lblTitulo.text = "¡Nivel " + r.NumeroNivel + " completado!";
            lblBase.text = Formato.Numero(r.PuntajeBase);
            lblTiempoNombre.text = "Tiempo (" + Formato.Tiempo(r.Segundos) + ")";
            lblDescTiempo.text = Descuento(r.DescuentoTiempo);
            lblIntentosNombre.text = "Intentos (" + r.Intentos + ")";
            lblDescIntentos.text = Descuento(r.DescuentoIntentos);
            lblPistasNombre.text = "Pistas usadas (" + r.PistasUsadas + ")";
            lblDescPistas.text = Descuento(r.DescuentoPistas);
            lblTotal.text = Formato.Numero(r.Puntaje);

            UIUtil.Mostrar(avisoDesbloqueo, r.NivelDesbloqueado > 0);
            lblDesbloqueo.text = "Nivel " + r.NivelDesbloqueado + " desbloqueado";
            UIUtil.Mostrar(avisoPista, r.PistaGanada);
            UIUtil.Ocultar(insigniaRecord);

            // ¿Hay siguiente nivel?
            siguienteNivel = 0;
            if (r.NumeroNivel < Reglas.TotalNiveles &&
                Servicios.Progreso.ObtenerProgreso(j.Id, r.NumeroNivel + 1).Estado != EstadoNivel.Bloqueado)
                siguienteNivel = r.NumeroNivel + 1;
            botonSiguiente.text = siguienteNivel > 0 ? "Siguiente nivel"
                                : r.NumeroNivel == Reglas.TotalNiveles ? "Ver mis logros" : "Volver al mapa";

            scroll.scrollOffset = Vector2.zero;
            Animar(r);
        }

        public override void AlOcultar()
        {
            foreach (var a in animaciones) a.Pause();
            animaciones.Clear();
        }

        static string Descuento(int valor)
        {
            return valor > 0 ? "-" + Formato.Numero(valor) : "0";
        }

        void Siguiente()
        {
            if (siguienteNivel > 0) ui.IrA(PantallaId.Juego, ParametrosJuego.DeNivel(siguienteNivel));
            else if (resultado != null && resultado.NumeroNivel == Reglas.TotalNiveles) ui.IrA(PantallaId.Logros);
            else ui.IrA(PantallaId.Mapa);
        }

        // ------------------------------------------------------------------
        // Animación: candado que se abre, estrellas una por una, puntaje que sube
        // ------------------------------------------------------------------

        void Programar(long ms, Action accion)
        {
            animaciones.Add(Raiz.schedule.Execute(accion).StartingIn(ms));
        }

        void Animar(ResultadoNivel r)
        {
            AlOcultar();

            Iconos.Cambiar(icoResultado, "candado");
            candado.RemoveFromClassList("resultado-candado--abierto");
            foreach (var e in estrellas)
            {
                e.RemoveFromClassList("estrella-resultado--visible");
                e.RemoveFromClassList("estrella-resultado--llena");
                Iconos.Rellenar(e, false);
            }
            lblPuntaje.text = Formato.Puntos(0);

            Programar(250, () =>
            {
                Iconos.Cambiar(icoResultado, "candado-abierto");
                candado.AddToClassList("resultado-candado--abierto");
                Sonidos.Reproducir(Sonido.Desbloqueo);
            });

            for (int i = 0; i < estrellas.Length; i++)
            {
                int k = i;
                Programar(650 + i * 280, () =>
                {
                    bool llena = k < r.Estrellas;
                    estrellas[k].AddToClassList("estrella-resultado--visible");
                    estrellas[k].EnableInClassList("estrella-resultado--llena", llena);
                    Iconos.Rellenar(estrellas[k], llena);
                    if (llena)
                    {
                        ui.Vibrar();
                        Sonidos.Reproducir(Sonido.Estrella);
                    }
                });
            }

            if (r.NuevoRecord) Programar(650 + 3 * 280, () => UIUtil.Mostrar(insigniaRecord));

            // Puntaje que sube de 0 al total
            float inicio = Time.realtimeSinceStartup + 0.3f;
            int objetivo = r.Puntaje;
            IVisualElementScheduledItem cuenta = null;
            cuenta = Raiz.schedule.Execute(() =>
            {
                float t = Mathf.Clamp01((Time.realtimeSinceStartup - inicio) / 0.9f);
                lblPuntaje.text = Formato.Puntos(Mathf.RoundToInt(objetivo * t));
                if (t >= 1f) cuenta.Pause();
            }).Every(30);
            animaciones.Add(cuenta);
        }
    }
}
