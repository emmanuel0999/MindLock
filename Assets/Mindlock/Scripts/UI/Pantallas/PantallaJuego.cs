using System;
using System.Collections.Generic;
using Mindlock.Datos;
using Mindlock.Logica;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>
    /// PANTALLAS 3, 4, 5 y 7 - Vista de acertijo.
    /// Incluye: los 3 tipos de respuesta, el modal de pista, la respuesta
    /// incorrecta, la pausa, el tiempo agotado y el modo "acertijo diario".
    /// Recibe un ParametrosJuego en AlMostrar.
    /// </summary>
    public class PantallaJuego : Pantalla
    {
        enum EstadoPista { Confirmar, Lista, SinPistas }

        // --- Elementos ---
        Label lblNivel, lblSubtitulo, etqDificultad, etqCategoria;
        Label lblTiempo, lblIntentos, lblPuntos, lblEnunciado, lblBotonPista, lblFranja;
        Label lblBannerFecha, lblBannerRacha, lblBannerBonus;
        VisualElement bannerDiario, indTiempo, barraTiempoCont, barraTiempo, tarjetaEnunciado, pistasVistas;
        VisualElement zonaTexto, zonaOpciones, zonaSecuencia, casillasCont, fichasCont, franja, franjaIcono;
        TextField campoRespuesta;
        ScrollView scroll;
        Button botonComprobar, botonSugerencia, botonVerAnuncio, botonComprarPista;

        VisualElement modalSalir, icoSalir;
        Label lblAvisoSalir, lblSalirTexto, lblSalirVidas;
        VisualElement modalPausa, modalPista, modalTiempo, estadoConfirmar, estadoLista, estadoSinPistas, listaPistas;
        Label lblConfirmarTitulo, lblConfirmarTexto, lblPuntosAntes, lblPuntosDespues, lblPistasGlobales, lblSinPistas, lblSinPistasDetalle;

        // --- Estado de la partida ---
        ParametrosJuego parametros;
        Jugador jugador;
        Nivel nivel;
        Acertijo acertijo;
        float segundos, ultimoTick;
        int intentos, pistasUsadas, penalizacionPistas;
        bool pausado, terminado;
        IVisualElementScheduledItem reloj;

        readonly List<Button> opciones = new List<Button>();
        Button opcionElegida;
        readonly List<Button> casillas = new List<Button>();
        readonly List<Button> fichaEnCasilla = new List<Button>();

        bool EsDiario { get { return parametros != null && parametros.EsDiario; } }
        int PistasRestantesAcertijo { get { return acertijo.Pistas.Count - pistasUsadas; } }

        protected override void Configurar()
        {
            lblNivel = Q<Label>("lblNivel");
            lblSubtitulo = Q<Label>("lblSubtitulo");
            etqDificultad = Q<Label>("etqDificultad");
            etqCategoria = Q<Label>("etqCategoria");
            bannerDiario = Q<VisualElement>("bannerDiario");
            lblBannerFecha = Q<Label>("lblBannerFecha");
            lblBannerRacha = Q<Label>("lblBannerRacha");
            lblBannerBonus = Q<Label>("lblBannerBonus");
            indTiempo = Q<VisualElement>("indTiempo");
            lblTiempo = Q<Label>("lblTiempo");
            lblIntentos = Q<Label>("lblIntentos");
            lblPuntos = Q<Label>("lblPuntos");
            barraTiempoCont = Q<VisualElement>("barraTiempoCont");
            barraTiempo = Q<VisualElement>("barraTiempo");
            scroll = Q<ScrollView>("scrollJuego");
            tarjetaEnunciado = Q<VisualElement>("tarjetaEnunciado");
            lblEnunciado = Q<Label>("lblEnunciado");
            pistasVistas = Q<VisualElement>("pistasVistas");
            zonaTexto = Q<VisualElement>("zonaTexto");
            zonaOpciones = Q<VisualElement>("zonaOpciones");
            zonaSecuencia = Q<VisualElement>("zonaSecuencia");
            casillasCont = Q<VisualElement>("casillas");
            fichasCont = Q<VisualElement>("fichas");
            campoRespuesta = Q<TextField>("campoRespuesta");
            franja = Q<VisualElement>("franjaMensaje");
            franjaIcono = Q<VisualElement>("franjaIcono");
            lblFranja = Q<Label>("lblFranja");
            lblBotonPista = Q<Label>("lblBotonPista");

            modalPausa = Q<VisualElement>("modalPausa");
            modalSalir = Q<VisualElement>("modalSalir");
            icoSalir = Q<VisualElement>("icoSalir");
            lblAvisoSalir = Q<Label>("lblAvisoSalir");
            lblSalirTexto = Q<Label>("lblSalirTexto");
            lblSalirVidas = Q<Label>("lblSalirVidas");
            modalPista = Q<VisualElement>("modalPista");
            modalTiempo = Q<VisualElement>("modalTiempo");
            estadoConfirmar = Q<VisualElement>("estadoConfirmar");
            estadoLista = Q<VisualElement>("estadoLista");
            estadoSinPistas = Q<VisualElement>("estadoSinPistas");
            listaPistas = Q<VisualElement>("listaPistas");
            lblConfirmarTitulo = Q<Label>("lblConfirmarTitulo");
            lblConfirmarTexto = Q<Label>("lblConfirmarTexto");
            lblPuntosAntes = Q<Label>("lblPuntosAntes");
            lblPuntosDespues = Q<Label>("lblPuntosDespues");
            lblPistasGlobales = Q<Label>("lblPistasGlobales");
            lblSinPistas = Q<Label>("lblSinPistas");
            lblSinPistasDetalle = Q<Label>("lblSinPistasDetalle");

            UIUtil.OcultarBarras(scroll);

            // Botones principales
            Boton("botonPausa", AbrirPausa);
            Boton("botonPista", PulsarPista);
            botonSugerencia = Boton("botonSugerencia", PulsarPista);
            botonComprobar = Boton("botonComprobar", Comprobar);

            // Pausa
            Boton("botonContinuar", CerrarPausa);
            Boton("botonReiniciar", () => UIUtil.CerrarModal(modalPausa, Preparar));
            Boton("botonSalir", () => UIUtil.CerrarModal(modalPausa, PreguntarSalir));

            // Confirmar salida
            Boton("botonQuedarme", () => UIUtil.CerrarModal(modalSalir, Reanudar));
            Boton("botonConfirmarSalir", ConfirmarSalir);

            // Pistas
            Boton("botonCancelarPista", () => UIUtil.CerrarModal(modalPista));
            Boton("botonVerPista", RevelarPista);
            Boton("botonVolverAcertijo", () => UIUtil.CerrarModal(modalPista));
            Boton("botonEntendido", () => UIUtil.CerrarModal(modalPista));
            botonVerAnuncio = Boton("botonVerAnuncio", VerAnuncioPorPista);
            botonComprarPista = Boton("botonComprarPista", ComprarPista);
            Q<Label>("lblComprarPista").text = Idioma.F("Comprar pista: {0} monedas", Economia.PrecioPista);

            // Tiempo agotado
            Boton("botonReintentar", () => UIUtil.CerrarModal(modalTiempo, () => ui.Economia.ConVida(Preparar, Salir)));
            Boton("botonTiempoMapa", () => UIUtil.CerrarModal(modalTiempo, Salir));

            // Respuesta de texto
            UIUtil.Placeholder(campoRespuesta, Idioma.T("Escribe tu respuesta"));
            campoRespuesta.RegisterValueChangedCallback(_ => OcultarFranja());
            campoRespuesta.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) Comprobar();
            });

            reloj = Raiz.schedule.Execute(Tick).Every(200);
            reloj.Pause();
        }

        public override void AlMostrar(object datos)
        {
            jugador = Sesion.Jugador;
            if (jugador == null) { ui.IrA(PantallaId.Perfil); return; }

            parametros = datos as ParametrosJuego ?? ParametrosJuego.DeNivel(1);
            if (EsDiario)
            {
                var diario = Servicios.Diario.ObtenerDiario(DateTime.Today);
                nivel = diario.Nivel;
                acertijo = diario.Acertijo;
            }
            else
            {
                nivel = Servicios.Progreso.ObtenerNivel(parametros.NumeroNivel);
                acertijo = Servicios.Acertijos.ObtenerAcertijo(nivel.AcertijoId);
            }
            Preparar();
        }

        public override void AlOcultar()
        {
            reloj.Pause();
            UIUtil.CerrarModalInmediato(modalPausa);
            UIUtil.CerrarModalInmediato(modalSalir);
            UIUtil.CerrarModalInmediato(modalPista);
            UIUtil.CerrarModalInmediato(modalTiempo);
        }

        public override bool AlPulsarAtras()
        {
            if (UIUtil.EstaVisible(modalPista)) UIUtil.CerrarModal(modalPista);
            else if (UIUtil.EstaVisible(modalSalir)) UIUtil.CerrarModal(modalSalir, Reanudar);
            else if (UIUtil.EstaVisible(modalPausa)) CerrarPausa();
            else if (!terminado) AbrirPausa();
            return true;
        }

        // ==================================================================
        // PREPARAR PARTIDA
        // ==================================================================

        void Preparar()
        {
            segundos = 0f;
            ultimoTick = Time.realtimeSinceStartup;
            intentos = 1;
            pistasUsadas = 0;
            penalizacionPistas = 0;
            pausado = false;
            terminado = false;

            // Encabezado
            if (EsDiario)
            {
                lblNivel.text = Idioma.T("Acertijo del día");
                lblSubtitulo.text = Formato.Puntos(nivel.PuntajeBase) + " + bonus";
                lblBannerFecha.text = Formato.Fecha(DateTime.Today);
                int racha = Servicios.Diario.RachaActual(jugador.Id);
                lblBannerRacha.text = racha > 0 ? Formato.Racha(racha) : Idioma.T("Empieza tu racha hoy");
                lblBannerBonus.text = "+" + Reglas.BonusDiario + " pts";
            }
            else
            {
                lblNivel.text = Idioma.F("Nivel {0}", nivel.Numero);
                lblSubtitulo.text = Formato.Puntos(nivel.PuntajeBase) + " base";
            }
            UIUtil.Mostrar(bannerDiario, EsDiario);
            UIUtil.Mostrar(etqDificultad, !EsDiario);
            UIUtil.Etiqueta(etqDificultad, Formato.NombreDificultad(nivel.Dificultad), Formato.Clave(nivel.Dificultad));
            UIUtil.Etiqueta(etqCategoria, Formato.NombreCategoria(acertijo.Categoria), Formato.Clave(acertijo.Categoria));

            // Enunciado y respuesta
            lblEnunciado.text = acertijo.EnunciadoVisible;
            tarjetaEnunciado.RemoveFromClassList("tarjeta-enunciado--correcta");
            pistasVistas.Clear();
            ConstruirRespuesta();

            OcultarFranja();
            UIUtil.Ocultar(botonSugerencia);
            UIUtil.Mostrar(indTiempo, Preferencias.MostrarTemporizador || nivel.TieneLimite);
            UIUtil.Mostrar(barraTiempoCont, nivel.TieneLimite);
            botonComprobar.SetEnabled(true);
            scroll.scrollOffset = Vector2.zero;

            ActualizarIndicadores();
            ActualizarBotonPista();
            reloj.Resume();
        }

        void ConstruirRespuesta()
        {
            UIUtil.Mostrar(zonaTexto, acertijo.Tipo == TipoRespuesta.Texto);
            UIUtil.Mostrar(zonaOpciones, acertijo.Tipo == TipoRespuesta.OpcionMultiple);
            UIUtil.Mostrar(zonaSecuencia, acertijo.Tipo == TipoRespuesta.Secuencia);

            switch (acertijo.Tipo)
            {
                case TipoRespuesta.Texto:
                    campoRespuesta.SetValueWithoutNotify(string.Empty);
                    campoRespuesta.RemoveFromClassList("campo-texto--error");
                    break;

                case TipoRespuesta.OpcionMultiple:
                    zonaOpciones.Clear();
                    opciones.Clear();
                    opcionElegida = null;
                    for (int i = 0; i < acertijo.OpcionesVisibles.Count; i++)
                    {
                        var b = UIUtil.Boton(null, "opcion");
                        var letra = UIUtil.Caja("opcion__letra");
                        letra.Add(UIUtil.Texto(((char)('A' + i)).ToString(), "opcion__letra-texto"));
                        b.Add(letra);
                        b.Add(UIUtil.Texto(acertijo.OpcionesVisibles[i], "opcion__texto"));
                        b.userData = acertijo.OpcionesVisibles[i];
                        UIUtil.IgnorarToquesHijos(b);
                        b.clicked += () => ElegirOpcion(b);
                        zonaOpciones.Add(b);
                        opciones.Add(b);
                    }
                    break;

                case TipoRespuesta.Secuencia:
                    casillasCont.Clear();
                    fichasCont.Clear();
                    casillas.Clear();
                    fichaEnCasilla.Clear();
                    for (int i = 0; i < acertijo.Casillas; i++)
                    {
                        int indice = i;
                        var c = UIUtil.Boton("?", "casilla");
                        c.clicked += () => QuitarDeCasilla(indice);
                        casillas.Add(c);
                        fichaEnCasilla.Add(null);
                        casillasCont.Add(c);
                    }
                    foreach (var texto in acertijo.OpcionesVisibles)
                    {
                        var f = UIUtil.Boton(texto, "ficha");
                        f.clicked += () => ColocarFicha(f);
                        fichasCont.Add(f);
                    }
                    break;
            }
        }

        // ==================================================================
        // TEMPORIZADOR E INDICADORES
        // ==================================================================

        void Tick()
        {
            float ahora = Time.realtimeSinceStartup;
            float dt = Mathf.Min(ahora - ultimoTick, 1f);
            ultimoTick = ahora;
            if (pausado || terminado) return;

            segundos += dt;
            if (nivel.TieneLimite && segundos >= nivel.TiempoLimite)
            {
                segundos = nivel.TiempoLimite;
                ActualizarIndicadores();
                TiempoAgotado();
                return;
            }
            ActualizarIndicadores();
        }

        int PuntajePotencial(int penalizacionExtra = 0)
        {
            return CalculadoraPuntaje.Potencial(nivel, Mathf.FloorToInt(segundos), intentos, penalizacionPistas + penalizacionExtra);
        }

        void ActualizarIndicadores()
        {
            int s = Mathf.FloorToInt(segundos);
            if (nivel.TieneLimite)
            {
                int restante = Mathf.Max(0, nivel.TiempoLimite - s);
                bool alerta = restante <= 10;
                UIUtil.PonerTexto(lblTiempo, Formato.Tiempo(restante));
                barraTiempo.style.width = Length.Percent(100f * (nivel.TiempoLimite - segundos) / nivel.TiempoLimite);
                indTiempo.EnableInClassList("indicador--alerta", alerta);
                barraTiempo.EnableInClassList("barra__relleno--alerta", alerta);
            }
            else
            {
                UIUtil.PonerTexto(lblTiempo, Formato.Tiempo(s));
                indTiempo.RemoveFromClassList("indicador--alerta");
            }
            UIUtil.PonerTexto(lblIntentos, Idioma.F("Intento {0}", intentos));
            UIUtil.PonerTexto(lblPuntos, Formato.Puntos(PuntajePotencial()));
        }

        /// <summary>-1 si el nivel no usa vidas (acertijo diario); si no, las vidas que quedan.</summary>
        int PerderVida()
        {
            if (EsDiario || Servicios.Economia == null) return -1;
            int vidas = Servicios.Economia.PerderVida(jugador.Id);
            ui.Economia.AvisarCambio();
            return vidas;
        }

        /// <summary>Sigue con el mismo acertijo después de conseguir una vida.</summary>
        void Continuar()
        {
            pausado = false;
            ultimoTick = Time.realtimeSinceStartup;
            OcultarFranja();
        }

        void TiempoAgotado()
        {
            PerderVida();
            terminado = true;
            reloj.Pause();
            ui.Vibrar();
            Sonidos.Reproducir(Sonido.Alerta);
            UIUtil.CerrarModalInmediato(modalPista);
            UIUtil.AbrirModal(modalTiempo);
        }

        // ==================================================================
        // RESPUESTAS
        // ==================================================================

        void ElegirOpcion(Button b)
        {
            if (terminado) return;
            foreach (var o in opciones) o.RemoveFromClassList("opcion--elegida");
            b.AddToClassList("opcion--elegida");
            opcionElegida = b;
            OcultarFranja();
        }

        void ColocarFicha(Button ficha)
        {
            if (terminado || ficha.ClassListContains("ficha--usada")) return;
            int libre = fichaEnCasilla.IndexOf(null);
            if (libre < 0) { UIUtil.Sacudir(casillasCont); return; }

            fichaEnCasilla[libre] = ficha;
            casillas[libre].text = ficha.text;
            casillas[libre].AddToClassList("casilla--llena");
            ficha.AddToClassList("ficha--usada");
            OcultarFranja();
        }

        void QuitarDeCasilla(int indice)
        {
            if (terminado) return;
            var ficha = fichaEnCasilla[indice];
            if (ficha == null) return;
            ficha.RemoveFromClassList("ficha--usada");
            fichaEnCasilla[indice] = null;
            casillas[indice].text = "?";
            casillas[indice].RemoveFromClassList("casilla--llena");
        }

        VisualElement ZonaActiva()
        {
            switch (acertijo.Tipo)
            {
                case TipoRespuesta.Texto: return zonaTexto;
                case TipoRespuesta.OpcionMultiple: return zonaOpciones;
                default: return zonaSecuencia;
            }
        }

        string ObtenerRespuesta()
        {
            switch (acertijo.Tipo)
            {
                case TipoRespuesta.Texto:
                    return campoRespuesta.value;
                case TipoRespuesta.OpcionMultiple:
                    return opcionElegida == null ? null : opcionElegida.userData as string;
                default:
                    if (fichaEnCasilla.Contains(null)) return null;
                    var partes = new List<string>();
                    foreach (var f in fichaEnCasilla) partes.Add(f.text);
                    return string.Join("|", partes.ToArray());
            }
        }

        string MensajeVacio()
        {
            switch (acertijo.Tipo)
            {
                case TipoRespuesta.Texto: return Idioma.T("Escribe una respuesta");
                case TipoRespuesta.OpcionMultiple: return Idioma.T("Elige una opción");
                default: return Idioma.T("Completa todas las casillas");
            }
        }

        void LimpiarRespuesta()
        {
            switch (acertijo.Tipo)
            {
                case TipoRespuesta.Texto:
                    campoRespuesta.SetValueWithoutNotify(string.Empty);
                    break;
                case TipoRespuesta.OpcionMultiple:
                    foreach (var o in opciones) o.RemoveFromClassList("opcion--elegida");
                    opcionElegida = null;
                    break;
                default:
                    for (int i = 0; i < fichaEnCasilla.Count; i++) QuitarDeCasilla(i);
                    break;
            }
        }

        void Comprobar()
        {
            if (terminado || pausado) return;
            string respuesta = ObtenerRespuesta();
            if (string.IsNullOrWhiteSpace(respuesta))
            {
                MostrarFranja(MensajeVacio(), null, "alerta");
                UIUtil.Sacudir(ZonaActiva());
                return;
            }

            if (acertijo.EsCorrecta(respuesta)) Acierto(respuesta);
            else Fallo(respuesta);
        }

        void Acierto(string respuesta)
        {
            terminado = true;
            reloj.Pause();
            botonComprobar.SetEnabled(false);
            UIUtil.Ocultar(botonSugerencia);
            tarjetaEnunciado.AddToClassList("tarjeta-enunciado--correcta");
            MostrarFranja(Idioma.T("¡Correcto! Abriendo el candado..."), "franja--exito", "candado-abierto");
            Sonidos.Reproducir(Sonido.Correcto);

            var intento = new ResultadoIntento
            {
                NumeroNivel = nivel.Numero,
                AcertijoId = acertijo.Id,
                Respuesta = respuesta,
                Segundos = Mathf.Max(1, Mathf.RoundToInt(segundos)),
                Intentos = intentos,
                PistasUsadas = pistasUsadas,
                PenalizacionPistas = penalizacionPistas
            };

            bool diario = EsDiario;
            Raiz.schedule.Execute(() =>
            {
                ResultadoNivel r = diario
                    ? Servicios.Diario.RegistrarDiario(jugador.Id, intento)
                    : Servicios.Progreso.RegistrarNivel(jugador.Id, intento);
                ui.IrA(diario ? PantallaId.Diario : PantallaId.Resultado, r);
                ui.MostrarLogros(r.LogrosNuevos, 1600);
            }).StartingIn(750);
        }

        void Fallo(string respuesta)
        {
            if (Servicios.Intentos != null)
                Servicios.Intentos.RegistrarFallo(jugador.Id, acertijo.Id, respuesta, Mathf.RoundToInt(segundos), pistasUsadas);
            intentos++;
            ui.Vibrar();
            Sonidos.Reproducir(Sonido.Error);

            var zona = ZonaActiva();
            if (acertijo.Tipo == TipoRespuesta.Texto) campoRespuesta.AddToClassList("campo-texto--error");
            if (opcionElegida != null) opcionElegida.AddToClassList("opcion--error");
            UIUtil.Sacudir(zona);

            // Limpiar después de la sacudida para que se vea qué estaba mal
            var elegida = opcionElegida;
            Raiz.schedule.Execute(() =>
            {
                if (elegida != null) elegida.RemoveFromClassList("opcion--error");
                campoRespuesta.RemoveFromClassList("campo-texto--error");
                if (!terminado) LimpiarRespuesta();
            }).StartingIn(450);

            // Fuera del acertijo diario, cada respuesta incorrecta cuesta una vida
            int vidas = PerderVida();
            if (vidas < 0) MostrarFranja(Idioma.T("Casi... inténtalo de nuevo"), "franja--error", "candado");
            else if (vidas > 0) MostrarFranja(vidas == 1 ? Idioma.T("Casi... te queda 1 vida") : Idioma.F("Casi... te quedan {0} vidas", vidas), "franja--error", "corazon");
            else
            {
                MostrarFranja(Idioma.T("Te quedaste sin vidas"), "franja--error", "corazon");
                pausado = true;
                Raiz.schedule.Execute(() => ui.Economia.AbrirVidas(Continuar, Salir)).StartingIn(700);
            }
            if (intentos > 3 && PistasRestantesAcertijo > 0 && jugador.PistasDisponibles > 0)
                UIUtil.Mostrar(botonSugerencia);

            ActualizarIndicadores();
        }

        void MostrarFranja(string texto, string clase, string icono)
        {
            franja.RemoveFromClassList("franja--error");
            franja.RemoveFromClassList("franja--exito");
            if (!string.IsNullOrEmpty(clase)) franja.AddToClassList(clase);
            lblFranja.text = texto;
            Iconos.Cambiar(franjaIcono, icono);
            UIUtil.Mostrar(franja);
        }

        void OcultarFranja()
        {
            if (!terminado) UIUtil.Ocultar(franja);
        }

        // ==================================================================
        // PISTAS
        // ==================================================================

        void ActualizarBotonPista()
        {
            lblBotonPista.text = PistasRestantesAcertijo > 0 ? Idioma.F("Pista ({0})", PistasRestantesAcertijo) : Idioma.T("Pistas");
        }

        void PulsarPista()
        {
            if (terminado) return;

            if (PistasRestantesAcertijo <= 0)
            {
                lblSinPistas.text = Idioma.T("Ya usaste todas las pistas de este acertijo");
                lblSinPistasDetalle.text = Idioma.T("Están debajo del enunciado. ¡Tú puedes!");
                MostrarEstadoPista(EstadoPista.SinPistas);
                return;
            }
            if (jugador.PistasDisponibles <= 0)
            {
                lblSinPistas.text = Idioma.T("No te quedan pistas disponibles");
                bool anuncio = Anuncios.Activos && (Servicios.Economia == null || Servicios.Economia.AnunciosRestantesHoy(jugador.Id) > 0);
                lblSinPistasDetalle.text = Idioma.T(anuncio
                    ? "Mira un anuncio, cómprala con monedas, resuelve el acertijo diario o consigue 3 estrellas en un nivel."
                    : "Cómprala con monedas, resuelve el acertijo diario o consigue 3 estrellas en un nivel.");
                MostrarEstadoPista(EstadoPista.SinPistas, anuncio, Servicios.Economia != null);
                return;
            }

            if (!Preferencias.ConfirmarPistas) { RevelarPista(); return; }

            var pista = acertijo.Pistas[pistasUsadas];
            lblConfirmarTitulo.text = Idioma.F("¿Usar la pista {0}?", pista.Orden);
            lblConfirmarTexto.text = Idioma.F("Esta pista resta {0} puntos al nivel.", pista.Penalizacion);
            lblPuntosAntes.text = Formato.Puntos(PuntajePotencial());
            lblPuntosDespues.text = Formato.Puntos(PuntajePotencial(pista.Penalizacion));
            lblPistasGlobales.text = jugador.PistasDisponibles == 1 ? Idioma.T("Te queda 1 pista") : Idioma.F("Te quedan {0} pistas", jugador.PistasDisponibles);
            MostrarEstadoPista(EstadoPista.Confirmar);
        }

        void RevelarPista()
        {
            if (terminado || PistasRestantesAcertijo <= 0) return;
            if (!Servicios.Progreso.UsarPista(jugador.Id))
            {
                lblSinPistas.text = Idioma.T("No te quedan pistas disponibles");
                lblSinPistasDetalle.text = Idioma.T("Gana más resolviendo el acertijo diario.");
                MostrarEstadoPista(EstadoPista.SinPistas);
                return;
            }

            var pista = acertijo.Pistas[pistasUsadas];
            pistasUsadas++;
            penalizacionPistas += pista.Penalizacion;

            var vista = UIUtil.Caja("pista-vista");
            vista.Add(Iconos.Crear("bombilla", "ico-sm"));
            vista.Add(UIUtil.Texto(Idioma.F("Pista {0}: {1}", pista.Orden, pista.TextoVisible), "pista-vista__texto"));
            pistasVistas.Add(vista);
            Sonidos.Reproducir(Sonido.Pista);

            ConstruirListaPistas();
            MostrarEstadoPista(EstadoPista.Lista);
            UIUtil.Ocultar(botonSugerencia);
            ActualizarIndicadores();
            ActualizarBotonPista();
        }

        void ConstruirListaPistas()
        {
            listaPistas.Clear();
            for (int i = 0; i < acertijo.Pistas.Count; i++)
            {
                var pista = acertijo.Pistas[i];
                bool vista = i < pistasUsadas;
                var fila = UIUtil.Caja("pista-item", vista ? null : "pista-item--bloqueada");
                fila.Add(Iconos.Crear(vista ? "bombilla" : "candado", "ico-md", vista ? "color-acento" : "color-sec"));

                var textos = UIUtil.Caja("crecer", "ml-sm");
                textos.Add(UIUtil.Texto(Idioma.F("Pista {0}", pista.Orden), "pista-item__titulo"));
                if (vista) textos.Add(UIUtil.Texto(pista.TextoVisible, "pista-item__texto"));
                fila.Add(textos);

                if (!vista) fila.Add(UIUtil.Texto("-" + pista.Penalizacion + " pts", "texto-sec"));
                listaPistas.Add(fila);
            }
        }

        /// <summary>El reloj se detiene mientras dura el anuncio; con la recompensa se abre la confirmación de la pista.</summary>
        void VerAnuncioPorPista()
        {
            if (terminado) return;
            pausado = true;
            UIUtil.CerrarModal(modalPista, () => Anuncios.MostrarRecompensa(Idioma.T("1 pista"), vioCompleto =>
            {
                pausado = false;
                ultimoTick = Time.realtimeSinceStartup;
                if (terminado) return;
                bool gano = Servicios.Economia != null
                    ? Servicios.Economia.Recompensar(jugador.Id, Recompensa.Pista)
                    : Servicios.Progreso.GanarPista(jugador.Id);
                if (gano)
                {
                    ui.MostrarAviso(Idioma.T("¡Ganaste 1 pista!"), "bombilla");
                    ui.Economia.AvisarCambio();
                    PulsarPista();
                }
            }));
        }

        void ComprarPista()
        {
            if (terminado || Servicios.Economia == null) return;
            if (!Servicios.Economia.ComprarPista(jugador.Id)) return;
            ui.MostrarAviso(Idioma.T("¡Compraste 1 pista!"), "bombilla");
            ui.Economia.AvisarCambio();
            UIUtil.CerrarModal(modalPista, PulsarPista);
        }

        void MostrarEstadoPista(EstadoPista estado, bool ofrecerAnuncio = false, bool ofrecerCompra = false)
        {
            UIUtil.Mostrar(botonVerAnuncio, ofrecerAnuncio);
            UIUtil.Mostrar(botonComprarPista, ofrecerCompra);
            botonComprarPista.SetEnabled(ofrecerCompra && jugador.Monedas >= Economia.PrecioPista);
            UIUtil.Mostrar(estadoConfirmar, estado == EstadoPista.Confirmar);
            UIUtil.Mostrar(estadoLista, estado == EstadoPista.Lista);
            UIUtil.Mostrar(estadoSinPistas, estado == EstadoPista.SinPistas);
            UIUtil.AbrirModal(modalPista);
        }

        // ==================================================================
        // PAUSA Y SALIDA
        // ==================================================================

        void AbrirPausa()
        {
            if (terminado) return;
            pausado = true;
            lblAvisoSalir.text = UsaVidas ? Idioma.T("Si sales, pierdes 1 vida.") : Idioma.T("Perderás el avance de este intento.");
            UIUtil.AbrirModal(modalPausa);
        }

        void CerrarPausa()
        {
            UIUtil.CerrarModal(modalPausa, Reanudar);
        }

        void Reanudar()
        {
            pausado = false;
            ultimoTick = Time.realtimeSinceStartup;
        }

        /// <summary>El acertijo diario no usa vidas: ahí salir solo pierde el intento.</summary>
        bool UsaVidas { get { return !EsDiario && Servicios.Economia != null; } }

        /// <summary>Antes de volver al mapa pregunta si está seguro; el reloj sigue en pausa.</summary>
        void PreguntarSalir()
        {
            if (terminado) { Salir(); return; }
            pausado = true;
            if (UsaVidas)
            {
                int quedan = Mathf.Max(0, jugador.Vidas - 1);
                lblSalirTexto.text = Idioma.T("Si sales ahora perderás 1 vida y el avance de este intento.");
                lblSalirVidas.text = quedan == 1 ? Idioma.T("Te quedará 1 vida") : Idioma.F("Te quedarán {0} vidas", quedan);
            }
            else lblSalirTexto.text = Idioma.T("Si sales ahora perderás el avance de este intento.");
            UIUtil.Mostrar(lblSalirVidas, UsaVidas);
            Iconos.Cambiar(icoSalir, UsaVidas ? "corazon" : "candado");
            UIUtil.AbrirModal(modalSalir);
        }

        void ConfirmarSalir()
        {
            terminado = true;
            reloj.Pause();
            PerderVida();
            UIUtil.CerrarModal(modalSalir, Salir);
        }

        void Salir()
        {
            ui.IrA(EsDiario ? PantallaId.Diario : PantallaId.Mapa);
        }
    }
}
