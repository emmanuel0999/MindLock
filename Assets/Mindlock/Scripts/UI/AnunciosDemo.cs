using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>
    /// Anuncios propios del juego (promocionan sus funciones), sin conexión ni cuentas.
    /// Usa el banner y el modal "anuncioFondo" de Raiz.uxml.
    /// </summary>
    public class AnunciosDemo : IProveedorAnuncios
    {
        public const float SegundosParaCerrar = 5f;      // intersticial
        public const float SegundosRecompensa = 8f;      // anuncio con recompensa
        public const long MsCambioBanner = 20000;

        struct Creatividad
        {
            public string Icono, Titulo, Texto, Accion;
            public PantallaId Destino;
        }

        static readonly Creatividad[] Creatividades =
        {
            new Creatividad { Icono = "calendario", Titulo = "Acertijo diario", Texto = "Un reto nuevo cada día y 150 puntos extra.", Accion = "Jugar el diario", Destino = PantallaId.Diario },
            new Creatividad { Icono = "trofeo", Titulo = "Colecciona logros", Texto = "Hay 9 logros esperándote. ¿Cuántos tienes ya?", Accion = "Ver mis logros", Destino = PantallaId.Logros },
            new Creatividad { Icono = "llama", Titulo = "Mantén tu racha", Texto = "Vuelve mañana y tu racha seguirá encendida.", Accion = "Ir al diario", Destino = PantallaId.Diario },
            new Creatividad { Icono = "engranaje", Titulo = "Dale tu estilo", Texto = "Prueba las paletas Océano y Aurora, en modo claro u oscuro.", Accion = "Abrir opciones", Destino = PantallaId.Opciones },
        };

        readonly UIManager ui;
        readonly Button banner, cerrar, accion;
        readonly VisualElement bannerIcono, fondo, icono, barraCont, barra;
        readonly Label bannerTitulo, bannerTexto, cuenta, titulo, texto;

        int indiceBanner, indiceAnuncio;
        bool bannerVisible, esRecompensa, completo, puedeCerrar;
        float inicio, duracion;
        Creatividad actual;
        Action<bool> alTerminar;
        IVisualElementScheduledItem reloj;

        public bool Visible { get { return alTerminar != null; } }

        public bool RecompensaLista { get { return true; } }

        public AnunciosDemo(UIManager manager, VisualElement raiz)
        {
            ui = manager;
            banner = raiz.Q<Button>("banner");
            bannerIcono = raiz.Q("bannerIcono");
            bannerTitulo = raiz.Q<Label>("bannerTitulo");
            bannerTexto = raiz.Q<Label>("bannerTexto");
            fondo = raiz.Q("anuncioFondo");
            cerrar = raiz.Q<Button>("anuncioCerrar");
            accion = raiz.Q<Button>("anuncioAccion");
            icono = raiz.Q("anuncioIcono");
            cuenta = raiz.Q<Label>("anuncioCuenta");
            titulo = raiz.Q<Label>("anuncioTitulo");
            texto = raiz.Q<Label>("anuncioTexto");
            barraCont = raiz.Q("anuncioBarraCont");
            barra = raiz.Q("anuncioBarra");

            indiceBanner = UnityEngine.Random.Range(0, Creatividades.Length);
            PintarBanner();
            banner.clicked += () => AbrirDestino(Creatividades[indiceBanner].Destino);
            banner.schedule.Execute(RotarBanner).Every(MsCambioBanner);

            cerrar.clicked += Cerrar;
            accion.clicked += PulsarAccion;
            reloj = fondo.schedule.Execute(Avanzar).Every(100);
            reloj.Pause();
        }

        // ------------------------------------------------------------------
        // Banner
        // ------------------------------------------------------------------

        public void MostrarBanner(bool visible)
        {
            bannerVisible = visible;
            UIUtil.Mostrar(banner, visible);
        }

        void RotarBanner()
        {
            if (!bannerVisible) return;
            banner.AddToClassList("banner--cambiando");
            banner.schedule.Execute(() =>
            {
                indiceBanner = (indiceBanner + 1) % Creatividades.Length;
                PintarBanner();
                banner.RemoveFromClassList("banner--cambiando");
            }).StartingIn(300);
        }

        void PintarBanner()
        {
            var c = Creatividades[indiceBanner];
            Iconos.Cambiar(bannerIcono, c.Icono);
            bannerTitulo.text = Idioma.T(c.Titulo);
            bannerTexto.text = Idioma.T(c.Texto);
        }

        void AbrirDestino(PantallaId destino)
        {
            // Sin perfil elegido no hay a dónde ir
            if (Sesion.Jugador == null) { ui.MostrarAviso(Idioma.T("Elige un perfil para empezar"), "usuario"); return; }
            if (ui.PantallaActual != destino) ui.IrA(destino);
        }

        // ------------------------------------------------------------------
        // Pantalla completa
        // ------------------------------------------------------------------

        public void MostrarIntersticial(Action alCerrar)
        {
            Abrir(false, SegundosParaCerrar, _ => { if (alCerrar != null) alCerrar(); });
        }

        public void MostrarRecompensa(string premio, Action<bool> alTerminarRecompensa)
        {
            Abrir(true, SegundosRecompensa, alTerminarRecompensa, premio);
        }

        void Abrir(bool recompensa, float segundos, Action<bool> alFinal, string premio = null)
        {
            if (Visible) { alFinal(false); return; }

            esRecompensa = recompensa;
            completo = false;
            duracion = segundos;
            inicio = Time.realtimeSinceStartup;
            alTerminar = alFinal;

            actual = Creatividades[indiceAnuncio];
            indiceAnuncio = (indiceAnuncio + 1) % Creatividades.Length;
            Iconos.Cambiar(icono, esRecompensa ? "trofeo" : actual.Icono);
            titulo.text = esRecompensa ? Idioma.T("Anuncio con premio") : Idioma.T(actual.Titulo);
            texto.text = esRecompensa ? Idioma.F("Míralo completo y ganas {0}.", premio) : Idioma.T(actual.Texto);
            accion.text = esRecompensa ? Idioma.T("Recibir premio") : Idioma.T(actual.Accion);
            accion.SetEnabled(!esRecompensa);
            UIUtil.Mostrar(barraCont, esRecompensa);
            barra.style.width = Length.Percent(0);
            // En el intersticial la X aparece al terminar la cuenta; en el de recompensa
            // siempre se puede cerrar, pero sin pista
            puedeCerrar = esRecompensa;
            UIUtil.Mostrar(cerrar, puedeCerrar);

            Avanzar();
            fondo.BringToFront();
            UIUtil.AbrirModal(fondo);
            reloj.Resume();
        }

        void Avanzar()
        {
            if (!Visible) return;
            float pasado = Time.realtimeSinceStartup - inicio;
            int faltan = Mathf.CeilToInt(duracion - pasado);

            if (esRecompensa)
            {
                barra.style.width = Length.Percent(Mathf.Clamp01(pasado / duracion) * 100f);
                if (faltan > 0) { cuenta.text = Idioma.F("Recompensa en {0} s", faltan); return; }
                cuenta.text = Idioma.T("¡Listo!");
                completo = true;
                accion.SetEnabled(true);
            }
            else
            {
                if (faltan > 0) { cuenta.text = Idioma.F("Puedes cerrar en {0}", faltan); return; }
                cuenta.text = "";
                puedeCerrar = true;
                UIUtil.Mostrar(cerrar);
            }
            reloj.Pause();
        }

        void PulsarAccion()
        {
            if (esRecompensa) { Terminar(completo, null); return; }
            // Tocar el anuncio lleva a la función que promociona
            var destino = actual.Destino;
            Terminar(false, () => AbrirDestino(destino), saltarContinuar: true);
        }

        void Cerrar()
        {
            Terminar(esRecompensa && completo, null);
        }

        /// <summary>Botón "atrás": en el intersticial solo cierra cuando ya se puede.</summary>
        public bool AlPulsarAtras()
        {
            if (!Visible) return false;
            if (puedeCerrar) Cerrar();
            return true;
        }

        void Terminar(bool recompensa, Action despues, bool saltarContinuar = false)
        {
            if (!Visible) return;
            var aviso = alTerminar;
            alTerminar = null;
            reloj.Pause();
            UIUtil.CerrarModal(fondo, () =>
            {
                if (!saltarContinuar) aviso(recompensa);
                if (despues != null) despues();
            });
        }
    }
}
