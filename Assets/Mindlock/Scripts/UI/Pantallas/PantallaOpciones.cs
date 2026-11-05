using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>PANTALLA 9 - Opciones (sonido, pantalla, juego, perfil, acerca de).</summary>
    public class PantallaOpciones : Pantalla
    {
        public const string Empresa = "Frailes City Studios";
        public const string Contacto = "frailescitystudios@gmail.com";
        public const string UrlPolitica = "https://sites.google.com/view/frailes-city-studios/privacy-policy";
        public const string UrlTerminos = "https://sites.google.com/view/frailes-city-studios/terms";

        Button botonTerminos, botonPreferenciasAnuncios;

        public override bool MuestraNavegacion { get { return true; } }

        Button[] segTema, segTexto, segPaleta, segIdioma;
        Label lblJugadorActual, lblConfirmarNombre;
        VisualElement modalReiniciar, pasoAdvertencia, pasoConfirmar;
        TextField campoConfirmar;
        Button botonConfirmarReinicio;

        protected override void Configurar()
        {
            lblJugadorActual = Q<Label>("lblJugadorActual");
            modalReiniciar = Q<VisualElement>("modalReiniciar");
            pasoAdvertencia = Q<VisualElement>("pasoAdvertencia");
            pasoConfirmar = Q<VisualElement>("pasoConfirmar");
            lblConfirmarNombre = Q<Label>("lblConfirmarNombre");
            campoConfirmar = Q<TextField>("campoConfirmar");
            UIUtil.OcultarBarras(Q<ScrollView>("scrollOpciones"));

            var version = Q<Label>("lblVersion");
            if (version != null) version.text = Idioma.F("Versión {0}", Application.version);

            // Acerca de: el correo abre la app de correo del dispositivo
            Q<Label>("lblContacto").text = Contacto;
            Q<Label>("lblDerechos").text = "© " + DateTime.Today.Year + " " + Empresa;
            Boton("botonContacto", () => Application.OpenURL("mailto:" + Contacto + "?subject=Mindlock"));

            // Privacidad
            Boton("botonPolitica", () => Application.OpenURL(UrlPolitica));
            botonTerminos = Boton("botonTerminos", () => Application.OpenURL(UrlTerminos));
            botonPreferenciasAnuncios = Boton("botonPreferenciasAnuncios", () =>
                ConsentimientoAnuncios.MostrarOpciones(ConsentimientoAnuncios.AplicarALevelPlay));

            // Idioma: al cambiarlo se vuelven a crear todas las pantallas
            segIdioma = new[] { Q<Button>("segIdiomaIngles"), Q<Button>("segIdiomaEspanol") };
            for (int i = 0; i < segIdioma.Length; i++)
            {
                int valor = i == 0 ? Idioma.Ingles : Idioma.Espanol;
                segIdioma[i].clicked += () =>
                {
                    if (valor == Idioma.Actual) return;
                    Idioma.Cambiar(valor);
                    ui.CambiarIdioma();
                };
            }

            // Interruptores (se guardan al instante en el dispositivo)
            UIUtil.Interruptor(Q<VisualElement>("filaMusica"), Preferencias.Musica, v =>
            {
                Preferencias.Musica = v;
                Sonidos.ActualizarMusica();
            });
            UIUtil.Interruptor(Q<VisualElement>("filaEfectos"), Preferencias.Efectos, v => Preferencias.Efectos = v);
            UIUtil.Interruptor(Q<VisualElement>("filaVibracion"), Preferencias.Vibracion, v =>
            {
                Preferencias.Vibracion = v;
                if (v) ui.Vibrar();
            });
            UIUtil.Interruptor(Q<VisualElement>("filaTemporizador"), Preferencias.MostrarTemporizador, v => Preferencias.MostrarTemporizador = v);
            UIUtil.Interruptor(Q<VisualElement>("filaConfirmarPistas"), Preferencias.ConfirmarPistas, v => Preferencias.ConfirmarPistas = v);

            // Segmentos: tema y tamaño de texto
            segTema = new[] { Q<Button>("segTemaOscuro"), Q<Button>("segTemaClaro"), Q<Button>("segTemaAuto") };
            segTexto = new[] { Q<Button>("segTextoPequeno"), Q<Button>("segTextoNormal"), Q<Button>("segTextoGrande") };
            ConfigurarSegmentos(segTema, v => Preferencias.Tema = v);
            ConfigurarSegmentos(segTexto, v => Preferencias.TamanoTexto = v);
            segPaleta = new[] { Q<Button>("segPaletaClasica"), Q<Button>("segPaletaOceano"), Q<Button>("segPaletaAurora") };
            ConfigurarSegmentos(segPaleta, v => Preferencias.Paleta = v);

            // Perfil
            Boton("botonCambiarPerfil", () =>
            {
                Sesion.Jugador = null;
                ui.IrA(PantallaId.Perfil);
            });
            Boton("botonReiniciarProgreso", AbrirReinicio);

            // Reinicio en dos pasos
            Boton("botonCancelarReinicio1", () => UIUtil.CerrarModal(modalReiniciar));
            Boton("botonCancelarReinicio2", () => UIUtil.CerrarModal(modalReiniciar));
            Boton("botonContinuarReinicio", () =>
            {
                UIUtil.Ocultar(pasoAdvertencia);
                UIUtil.Mostrar(pasoConfirmar);
                campoConfirmar.SetValueWithoutNotify(string.Empty);
                botonConfirmarReinicio.SetEnabled(false);
                campoConfirmar.schedule.Execute(() => campoConfirmar.Focus()).StartingIn(100);
            });
            botonConfirmarReinicio = Boton("botonConfirmarReinicio", ConfirmarReinicio);
            campoConfirmar.RegisterValueChangedCallback(_ => botonConfirmarReinicio.SetEnabled(NombreCoincide()));
        }

        public override void AlMostrar(object datos)
        {
            UIUtil.CerrarModalInmediato(modalReiniciar);
            lblJugadorActual.text = Sesion.Jugador != null ? Sesion.Jugador.Nombre : "-";
            MarcarSegmentos(segTema, Preferencias.Tema);
            MarcarSegmentos(segTexto, Preferencias.TamanoTexto);
            MarcarSegmentos(segPaleta, Preferencias.Paleta);
            MarcarSegmentos(segIdioma, Idioma.EsIngles ? 0 : 1);

            // "Preferencias de anuncios" solo donde el jugador debe poder cambiar su decisión (Europa, Reino Unido, Suiza)
            bool preferencias = ConsentimientoAnuncios.OpcionesDisponibles;
            UIUtil.Mostrar(botonPreferenciasAnuncios, preferencias);
            botonTerminos.EnableInClassList("fila-opcion--ultima", !preferencias);
        }

        public override bool AlPulsarAtras()
        {
            if (UIUtil.EstaVisible(modalReiniciar)) { UIUtil.CerrarModal(modalReiniciar); return true; }
            return false;
        }

        void ConfigurarSegmentos(Button[] botones, Action<int> guardar)
        {
            for (int i = 0; i < botones.Length; i++)
            {
                int valor = i;
                if (botones[i] == null) continue;
                botones[i].clicked += () =>
                {
                    guardar(valor);
                    MarcarSegmentos(botones, valor);
                    ui.AplicarPreferencias();
                };
            }
        }

        static void MarcarSegmentos(Button[] botones, int activo)
        {
            for (int i = 0; i < botones.Length; i++)
                if (botones[i] != null) botones[i].EnableInClassList("segmento--activo", i == activo);
        }

        void AbrirReinicio()
        {
            if (Sesion.Jugador == null) return;
            lblConfirmarNombre.text = Idioma.F("Escribe «{0}» para confirmar.", Sesion.Jugador.Nombre);
            UIUtil.Mostrar(pasoAdvertencia);
            UIUtil.Ocultar(pasoConfirmar);
            UIUtil.AbrirModal(modalReiniciar);
        }

        bool NombreCoincide()
        {
            if (Sesion.Jugador == null) return false;
            return string.Equals((campoConfirmar.value ?? "").Trim(), Sesion.Jugador.Nombre, StringComparison.OrdinalIgnoreCase);
        }

        void ConfirmarReinicio()
        {
            if (!NombreCoincide()) return;
            Servicios.Perfiles.ReiniciarProgreso(Sesion.Jugador.Id);
            UIUtil.CerrarModal(modalReiniciar, () =>
            {
                ui.MostrarAviso(Idioma.T("Progreso reiniciado. ¡A empezar de nuevo!"), "repetir");
                ui.IrA(PantallaId.Mapa);
            });
        }
    }
}
