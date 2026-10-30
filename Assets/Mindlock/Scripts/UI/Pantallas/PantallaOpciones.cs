using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>PANTALLA 9 - Opciones (sonido, pantalla, juego, perfil, acerca de).</summary>
    public class PantallaOpciones : Pantalla
    {
        public override bool MuestraNavegacion { get { return true; } }

        Button[] segTema, segTexto, segPaleta;
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
            if (version != null) version.text = "Versión " + Application.version;

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
            lblConfirmarNombre.text = "Escribe «" + Sesion.Jugador.Nombre + "» para confirmar.";
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
                ui.MostrarAviso("Progreso reiniciado. ¡A empezar de nuevo!", "repetir");
                ui.IrA(PantallaId.Mapa);
            });
        }
    }
}
