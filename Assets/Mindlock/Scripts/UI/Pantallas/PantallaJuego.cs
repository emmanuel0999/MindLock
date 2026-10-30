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
        Button botonComprobar, botonSugerencia;

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
            Boton("botonSalir", () => UIUtil.CerrarModal(modalPausa, Salir));

            // Pistas
            Boton("botonCancelarPista", () => UIUtil.CerrarModal(modalPista));
            Boton("botonVerPista", RevelarPista);
            Boton("botonVolverAcertijo", () => UIUtil.CerrarModal(modalPista));
            Boton("botonEntendido", () => UIUtil.CerrarModal(modalPista));

            // Tiempo agotado
            Boton("botonReintentar", () => UIUtil.CerrarModal(modalTiempo, Preparar));
            Boton("botonTiempoMapa", () => UIUtil.CerrarModal(modalTiempo, Salir));

            // Respuesta de texto
            UIUtil.Placeholder(campoRespuesta, "Escribe tu respuesta");
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
            UIUtil.CerrarModalInmediato(modalPista);
            UIUtil.CerrarModalInmediato(modalTiempo);
        }

        public override bool AlPulsarAtras()
        {
            if (UIUtil.EstaVisible(modalPista)) UIUtil.CerrarModal(modalPista);
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
                lblNivel.text = "Acertijo del día";
                lblSubtitulo.text = Formato.Puntos(nivel.PuntajeBase) + " + bonus";
                lblBannerFecha.text = Formato.Fecha(DateTime.Today);
                int racha = Servicios.Diario.RachaActual(jugador.Id);
                lblBannerRacha.text = racha > 0 ? Formato.Racha(racha) : "Empieza tu racha hoy";
                lblBannerBonus.text = "+" + Reglas.BonusDiario + " pts";
            }
            else
            {
                lblNivel.text = "Nivel " + nivel.Numero;
                lblSubtitulo.text = Formato.Puntos(nivel.PuntajeBase) + " base";
            }
            UIUtil.Mostrar(bannerDiario, EsDiario);
            UIUtil.Mostrar(etqDificultad, !EsDiario);
            UIUtil.Etiqueta(etqDificultad, Formato.NombreDificultad(nivel.Dificultad), Formato.Clave(nivel.Dificultad));
            UIUtil.Etiqueta(etqCategoria, Formato.NombreCategoria(acertijo.Categoria), Formato.Clave(acertijo.Categoria));

            // Enunciado y respuesta
            lblEnunciado.text = acertijo.Enunciado;
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
                    for (int i = 0; i < acertijo.Opciones.Count; i++)
                    {
                        var b = UIUtil.Boton(null, "opcion");
                        var letra = UIUtil.Caja("opcion__letra");
                        letra.Add(UIUtil.Texto(((char)('A' + i)).ToString(), "opcion__letra-texto"));
                        b.Add(letra);
                        b.Add(UIUtil.Texto(acertijo.Opciones[i], "opcion__texto"));
                        b.userData = acertijo.Opciones[i];
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
                    foreach (var texto in acertijo.Opciones)
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
            UIUtil.PonerTexto(lblIntentos, "Intento " + intentos);
            UIUtil.PonerTexto(lblPuntos, Formato.Puntos(PuntajePotencial()));
        }

        void TiempoAgotado()
        {
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
                case TipoRespuesta.Texto: return "Escribe una respuesta";
                case TipoRespuesta.OpcionMultiple: return "Elige una opción";
                default: return "Completa todas las casillas";
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
            MostrarFranja("¡Correcto! Abriendo el candado...", "franja--exito", "candado-abierto");
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

            MostrarFranja("Casi... inténtalo de nuevo", "franja--error", "candado");
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
            lblBotonPista.text = PistasRestantesAcertijo > 0 ? "Pista (" + PistasRestantesAcertijo + ")" : "Pistas";
        }

        void PulsarPista()
        {
            if (terminado) return;

            if (PistasRestantesAcertijo <= 0)
            {
                lblSinPistas.text = "Ya usaste todas las pistas de este acertijo";
                lblSinPistasDetalle.text = "Están debajo del enunciado. ¡Tú puedes!";
                MostrarEstadoPista(EstadoPista.SinPistas);
                return;
            }
            if (jugador.PistasDisponibles <= 0)
            {
                lblSinPistas.text = "No te quedan pistas disponibles";
                lblSinPistasDetalle.text = "Gana más resolviendo el acertijo diario o consiguiendo 3 estrellas en un nivel.";
                MostrarEstadoPista(EstadoPista.SinPistas);
                return;
            }

            if (!Preferencias.ConfirmarPistas) { RevelarPista(); return; }

            var pista = acertijo.Pistas[pistasUsadas];
            lblConfirmarTitulo.text = "¿Usar la pista " + pista.Orden + "?";
            lblConfirmarTexto.text = "Esta pista resta " + pista.Penalizacion + " puntos al nivel.";
            lblPuntosAntes.text = Formato.Puntos(PuntajePotencial());
            lblPuntosDespues.text = Formato.Puntos(PuntajePotencial(pista.Penalizacion));
            lblPistasGlobales.text = jugador.PistasDisponibles == 1 ? "Te queda 1 pista" : "Te quedan " + jugador.PistasDisponibles + " pistas";
            MostrarEstadoPista(EstadoPista.Confirmar);
        }

        void RevelarPista()
        {
            if (terminado || PistasRestantesAcertijo <= 0) return;
            if (!Servicios.Progreso.UsarPista(jugador.Id))
            {
                lblSinPistas.text = "No te quedan pistas disponibles";
                lblSinPistasDetalle.text = "Gana más resolviendo el acertijo diario.";
                MostrarEstadoPista(EstadoPista.SinPistas);
                return;
            }

            var pista = acertijo.Pistas[pistasUsadas];
            pistasUsadas++;
            penalizacionPistas += pista.Penalizacion;

            var vista = UIUtil.Caja("pista-vista");
            vista.Add(Iconos.Crear("bombilla", "ico-sm"));
            vista.Add(UIUtil.Texto("Pista " + pista.Orden + ": " + pista.Texto, "pista-vista__texto"));
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
                textos.Add(UIUtil.Texto("Pista " + pista.Orden, "pista-item__titulo"));
                if (vista) textos.Add(UIUtil.Texto(pista.Texto, "pista-item__texto"));
                fila.Add(textos);

                if (!vista) fila.Add(UIUtil.Texto("-" + pista.Penalizacion + " pts", "texto-sec"));
                listaPistas.Add(fila);
            }
        }

        void MostrarEstadoPista(EstadoPista estado)
        {
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
            UIUtil.AbrirModal(modalPausa);
        }

        void CerrarPausa()
        {
            pausado = false;
            ultimoTick = Time.realtimeSinceStartup;
            UIUtil.CerrarModal(modalPausa);
        }

        void Salir()
        {
            ui.IrA(EsDiario ? PantallaId.Diario : PantallaId.Mapa);
        }
    }
}
