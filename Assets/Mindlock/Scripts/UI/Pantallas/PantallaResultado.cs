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
        VisualElement candado, icoResultado, avisoDesbloqueo, avisoPista, avisoMonedas;
        Label lblMonedas;
        Button botonDuplicar;
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
            avisoMonedas = Q<VisualElement>("avisoMonedas");
            lblMonedas = Q<Label>("lblMonedasResultado");
            botonDuplicar = Boton("botonDuplicar", Duplicar);
            scroll = Q<ScrollView>("scrollResultado");
            UIUtil.OcultarBarras(scroll);

            // Al salir puede tocar un anuncio a pantalla completa (ver Anuncios)
            botonSiguiente = Boton("botonSiguiente", () => Salir(Siguiente));
            Boton("botonRepetir", () => Salir(() => { if (resultado != null) ui.JugarNivel(resultado.NumeroNivel); }));
            Boton("botonMapaResultado", () => Salir(() => ui.IrA(PantallaId.Mapa)));
        }

        public override void AlMostrar(object datos)
        {
            resultado = datos as ResultadoNivel;
            var j = Sesion.Jugador;
            if (resultado == null || j == null) { ui.IrA(PantallaId.Mapa); return; }
            var r = resultado;
            Anuncios.RegistrarNivelCompletado();

            lblTitulo.text = Idioma.F("¡Nivel {0} completado!", r.NumeroNivel);
            lblBase.text = Formato.Numero(r.PuntajeBase);
            lblTiempoNombre.text = Idioma.F("Tiempo ({0})", Formato.Tiempo(r.Segundos));
            lblDescTiempo.text = Descuento(r.DescuentoTiempo);
            lblIntentosNombre.text = Idioma.F("Intentos ({0})", r.Intentos);
            lblDescIntentos.text = Descuento(r.DescuentoIntentos);
            lblPistasNombre.text = Idioma.F("Pistas usadas ({0})", r.PistasUsadas);
            lblDescPistas.text = Descuento(r.DescuentoPistas);
            lblTotal.text = Formato.Numero(r.Puntaje);

            UIUtil.Mostrar(avisoDesbloqueo, r.NivelDesbloqueado > 0);
            lblDesbloqueo.text = Idioma.F("Nivel {0} desbloqueado", r.NivelDesbloqueado);
            UIUtil.Mostrar(avisoPista, r.PistaGanada);
            UIUtil.Mostrar(avisoMonedas, r.MonedasGanadas > 0);
            lblMonedas.text = Idioma.F("Ganaste {0} monedas", r.MonedasGanadas);
            UIUtil.Mostrar(botonDuplicar, r.MonedasGanadas > 0 && Anuncios.Activos && Servicios.Economia != null
                                          && Servicios.Economia.AnunciosRestantesHoy(j.Id) > 0);
            UIUtil.Ocultar(insigniaRecord);

            // ¿Hay siguiente nivel?
            siguienteNivel = 0;
            if (r.NumeroNivel < Reglas.TotalNiveles &&
                Servicios.Progreso.ObtenerProgreso(j.Id, r.NumeroNivel + 1).Estado != EstadoNivel.Bloqueado)
                siguienteNivel = r.NumeroNivel + 1;
            botonSiguiente.text = Idioma.T(siguienteNivel > 0 ? "Siguiente nivel"
                                : r.NumeroNivel == Reglas.TotalNiveles ? "Ver mis logros" : "Volver al mapa");

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

        /// <summary>Un video con recompensa suma otra vez las monedas ganadas en el nivel y lo muestra en el aviso.</summary>
        void Duplicar()
        {
            var r = resultado;
            var j = Sesion.Jugador;
            if (r == null || j == null) return;
            Anuncios.MostrarRecompensa(Idioma.F("{0} monedas más", r.MonedasGanadas), vio =>
            {
                if (!vio || !Servicios.Economia.Recompensar(j.Id, Recompensa.DuplicarMonedas, r.MonedasGanadas)) return;
                lblMonedas.text = Idioma.F("Ganaste {0} monedas (+{1} por el video)", r.MonedasGanadas * 2, r.MonedasGanadas);
                UIUtil.Ocultar(botonDuplicar);
                ui.MostrarAviso(Idioma.F("¡+{0} monedas por ver el video!", r.MonedasGanadas), "moneda");
                ui.Economia.AvisarCambio();
            });
        }

        void Salir(Action continuar)
        {
            if (resultado == null) { continuar(); return; }
            Anuncios.DespuesDeNivel(resultado.NumeroNivel, continuar);
        }

        void Siguiente()
        {
            if (siguienteNivel > 0) ui.JugarNivel(siguienteNivel);
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
