using System;
using System.Collections.Generic;
using Mindlock.Datos;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>
    /// Controla toda la interfaz: carga las pantallas, cambia de una a otra,
    /// aplica tema y tamaño de texto, área segura y avisos emergentes.
    /// Va en un GameObject con UIDocument (es el único script que hay que agregar).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIManager : MonoBehaviour
    {
        public const string RutaPantallas = "Mindlock/Pantallas/";
        public const string RutaTema = "Mindlock/Estilos/MindlockTheme";

        [Tooltip("Usa perfiles de ejemplo (Ana y Carlos) en memoria, sin guardar. Desmarcado: el progreso se guarda en el dispositivo.")]
        [SerializeField] bool usarDatosDeDemo = false;

        [Tooltip("Acertijos muy difíciles del nivel 5 al 30 y pistas que restan más puntos. Desmarcado: configuración original.")]
        [SerializeField] bool modoDesafio = true;

        UIDocument documento;
        VisualElement app, contenedor, barraNavegacion, toast, toastIcono;
        Label toastTitulo, toastNombre, toastTexto;
        Button tabNiveles, tabLogros, tabOpciones;

        readonly Dictionary<PantallaId, Pantalla> pantallas = new Dictionary<PantallaId, Pantalla>();
        Pantalla actual;
        PantallaId idActual;

        struct Aviso { public string Titulo, Nombre, Texto, Icono; public bool EsLogro; }
        readonly Queue<Aviso> avisos = new Queue<Aviso>();
        bool mostrandoAviso;
        float margenSuperior;

        public PantallaId PantallaActual { get { return idActual; } }

        // ==================================================================
        // ARRANQUE
        // ==================================================================

        void Awake()
        {
            CatalogoAcertijos.ModoDesafio = modoDesafio;
            if (!Servicios.Configurados)
            {
                if (usarDatosDeDemo) Servicios.UsarMemoria(true);
                else Servicios.UsarArchivo(System.IO.Path.Combine(Application.persistentDataPath, ArchivoJuegoService.NombreArchivo));
            }
            Application.targetFrameRate = 60;
            Sonidos.Inicializar(gameObject);

            documento = GetComponent<UIDocument>();
            if (documento.panelSettings == null)
            {
                Debug.LogWarning("[Mindlock] El UI Document no tiene Panel Settings: se crea uno automáticamente. Lo ideal es asignar MindlockPanelSettings.");
                documento.panelSettings = CrearPanelSettings();
            }
            else if (documento.panelSettings.themeStyleSheet == null)
            {
                Debug.LogWarning("[Mindlock] El Panel Settings no tiene Theme Style Sheet: se usa MindlockTheme.tss.");
                documento.panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(RutaTema);
            }
        }

        void Start()
        {
            Construir();
            IrA(PantallaId.Perfil);
        }

        static PanelSettings CrearPanelSettings()
        {
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.themeStyleSheet = Resources.Load<ThemeStyleSheet>(RutaTema);
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(390, 844);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            return ps;
        }

        void Construir()
        {
            var raiz = documento.rootVisualElement;
            if (raiz == null)
            {
                Debug.LogError("[Mindlock] UIDocument sin elemento raíz. Revisa que tenga Panel Settings asignado.");
                return;
            }
            raiz.Clear();

            var plantilla = Cargar("Raiz");
            if (plantilla == null) return;
            plantilla.CloneTree(raiz);

            app = raiz.Q("app");
            contenedor = raiz.Q("contenedorPantallas");
            barraNavegacion = raiz.Q("barraNavegacion");
            toast = raiz.Q("toast");
            toastIcono = raiz.Q("toastIcono");
            toastTitulo = raiz.Q<Label>("toastTitulo");
            toastNombre = raiz.Q<Label>("toastNombre");
            toastTexto = raiz.Q<Label>("toastTexto");
            tabNiveles = raiz.Q<Button>("tabNiveles");
            tabLogros = raiz.Q<Button>("tabLogros");
            tabOpciones = raiz.Q<Button>("tabOpciones");

            UIUtil.LimpiarBotones(app);
            Iconos.Preparar(app);

            // Sonido de toque para cualquier botón de la app
            app.RegisterCallback<ClickEvent>(e =>
            {
                var b = e.target as Button;
                if (b != null && b.enabledInHierarchy) Sonidos.Reproducir(Sonido.Toque);
            }, TrickleDown.TrickleDown);

            tabNiveles.clicked += () => IrA(PantallaId.Mapa);
            tabLogros.clicked += () => IrA(PantallaId.Logros);
            tabOpciones.clicked += () => IrA(PantallaId.Opciones);

            Registrar(PantallaId.Perfil, "Perfil", new PantallaPerfil());
            Registrar(PantallaId.Mapa, "Mapa", new PantallaMapa());
            Registrar(PantallaId.Juego, "Juego", new PantallaJuego());
            Registrar(PantallaId.Resultado, "Resultado", new PantallaResultado());
            Registrar(PantallaId.Diario, "Diario", new PantallaDiario());
            Registrar(PantallaId.Logros, "Logros", new PantallaLogros());
            Registrar(PantallaId.Opciones, "Opciones", new PantallaOpciones());

            UIUtil.AplicarSafeArea(app, arriba => margenSuperior = arriba);
            AplicarPreferencias();
        }

        VisualTreeAsset Cargar(string nombre)
        {
            var asset = Resources.Load<VisualTreeAsset>(RutaPantallas + nombre);
            if (asset == null)
                Debug.LogError("[Mindlock] No se encontró " + RutaPantallas + nombre + ".uxml dentro de una carpeta Resources.");
            return asset;
        }

        void Registrar(PantallaId id, string uxml, Pantalla pantalla)
        {
            var asset = Cargar(uxml);
            if (asset == null) return;

            var contenido = asset.Instantiate();
            contenido.AddToClassList("contenedor-pantalla");
            contenido.style.display = DisplayStyle.None;
            contenedor.Add(contenido);

            UIUtil.LimpiarBotones(contenido);
            Iconos.Preparar(contenido);

            pantalla.Inicializar(this, contenido);
            pantallas[id] = pantalla;
        }

        // ==================================================================
        // NAVEGACIÓN
        // ==================================================================

        /// <summary>
        /// Cambia de pantalla. "datos" se pasa a AlMostrar:
        /// Juego → ParametrosJuego, Resultado/Diario → ResultadoNivel.
        /// </summary>
        public void IrA(PantallaId id, object datos = null)
        {
            Pantalla nueva;
            if (!pantallas.TryGetValue(id, out nueva)) return;

            if (actual != null)
            {
                actual.AlOcultar();
                actual.Raiz.RemoveFromClassList("contenedor-pantalla--visible");
                actual.Raiz.style.display = DisplayStyle.None;
            }

            actual = nueva;
            idActual = id;

            UIUtil.Mostrar(barraNavegacion, nueva.MuestraNavegacion);
            tabNiveles.EnableInClassList("tab--activa", id == PantallaId.Mapa);
            tabLogros.EnableInClassList("tab--activa", id == PantallaId.Logros);
            tabOpciones.EnableInClassList("tab--activa", id == PantallaId.Opciones);
            Iconos.Repintar(barraNavegacion);

            nueva.Raiz.style.display = DisplayStyle.Flex;
            nueva.AlMostrar(datos);

            var raiz = nueva.Raiz;
            raiz.schedule.Execute(() => raiz.AddToClassList("contenedor-pantalla--visible")).StartingIn(16);
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            // Botón "atrás" de Android
            if (Input.GetKeyDown(KeyCode.Escape) && actual != null && !actual.AlPulsarAtras())
            {
                if (idActual != PantallaId.Mapa && idActual != PantallaId.Perfil) IrA(PantallaId.Mapa);
            }
#endif
        }

        // ==================================================================
        // PREFERENCIAS VISUALES
        // ==================================================================

        public void AplicarPreferencias()
        {
            if (app == null) return;
            int tema = Preferencias.Tema;
            int hora = DateTime.Now.Hour;
            bool claro = tema == Preferencias.TemaClaro || (tema == Preferencias.TemaAutomatico && hora >= 7 && hora < 19);

            app.EnableInClassList("tema-claro", claro);
            app.EnableInClassList("escala-texto--pequena", Preferencias.TamanoTexto == Preferencias.TextoPequeno);
            app.EnableInClassList("escala-texto--grande", Preferencias.TamanoTexto == Preferencias.TextoGrande);
            app.EnableInClassList("paleta-oceano", Preferencias.Paleta == Preferencias.PaletaOceano);
            app.EnableInClassList("paleta-aurora", Preferencias.Paleta == Preferencias.PaletaAurora);

            app.schedule.Execute(() => Iconos.Repintar(app)).StartingIn(30);
        }

        public void Vibrar()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Preferencias.Vibracion) Handheld.Vibrate();
#endif
        }

        // ==================================================================
        // AVISOS EMERGENTES
        // ==================================================================

        public void MostrarLogros(List<Logro> logros, long retrasoMs = 0)
        {
            if (logros == null || logros.Count == 0) return;
            var copia = new List<Logro>(logros);
            app.schedule.Execute(() =>
            {
                foreach (var l in copia)
                    avisos.Enqueue(new Aviso { Titulo = "¡Logro obtenido!", Nombre = l.Nombre, Texto = l.Descripcion, Icono = l.Icono, EsLogro = true });
                ProcesarAvisos();
            }).StartingIn(retrasoMs);
        }

        public void MostrarAviso(string texto, string icono = "check")
        {
            if (avisos.Count >= 2) return;   // evita acumular avisos repetidos
            avisos.Enqueue(new Aviso { Titulo = "Mindlock", Nombre = texto, Texto = null, Icono = icono });
            ProcesarAvisos();
        }

        void ProcesarAvisos()
        {
            if (mostrandoAviso || avisos.Count == 0 || toast == null) return;
            mostrandoAviso = true;
            var a = avisos.Dequeue();

            toastTitulo.text = a.Titulo;
            toastNombre.text = a.Nombre;
            toastTexto.text = a.Texto ?? "";
            UIUtil.Mostrar(toastTexto, !string.IsNullOrEmpty(a.Texto));
            Iconos.Cambiar(toastIcono, string.IsNullOrEmpty(a.Icono) ? "trofeo" : a.Icono);
            toast.style.top = margenSuperior + 12f;
            toast.BringToFront();

            toast.AddToClassList("toast--visible");
            if (a.EsLogro) Sonidos.Reproducir(Sonido.Logro);
            toast.schedule.Execute(() => toast.RemoveFromClassList("toast--visible")).StartingIn(2800);
            toast.schedule.Execute(() =>
            {
                mostrandoAviso = false;
                ProcesarAvisos();
            }).StartingIn(3200);
        }
    }
}
