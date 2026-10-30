using Mindlock.Datos;
using UnityEngine;

namespace Mindlock
{
    /// <summary>
    /// Punto único para acceder a los servicios de datos.
    /// Para usar SQLite, antes de que arranque la UI (ej. en un Awake con orden de ejecución bajo):
    ///     var sqlite = new SqliteJuegoService(...);
    ///     Servicios.Configurar(sqlite, sqlite, sqlite, sqlite, sqlite);
    /// </summary>
    public static class Servicios
    {
        public static IPerfilService Perfiles { get; private set; }
        public static IProgresoService Progreso { get; private set; }
        public static IAcertijoService Acertijos { get; private set; }
        public static IDiarioService Diario { get; private set; }
        public static ILogroService Logros { get; private set; }
        /// <summary>Opcional: null si la implementación no guarda el historial de intentos.</summary>
        public static IIntentoService Intentos { get; private set; }

        public static bool Configurados { get { return Perfiles != null; } }

        public static void Configurar(IPerfilService perfiles, IProgresoService progreso, IAcertijoService acertijos,
                                      IDiarioService diario, ILogroService logros)
        {
            Perfiles = perfiles;
            Progreso = progreso;
            Acertijos = acertijos;
            Diario = diario;
            Logros = logros;
            Intentos = perfiles as IIntentoService;
        }

        public static void UsarMemoria(bool datosDeDemo)
        {
            var m = new MemoriaJuegoService(datosDeDemo);
            Configurar(m, m, m, m, m);
        }

        /// <summary>Datos guardados en un archivo del dispositivo (sin conexión).</summary>
        public static void UsarArchivo(string ruta)
        {
            var a = new ArchivoJuegoService(ruta);
            Configurar(a, a, a, a, a);
        }
    }

    /// <summary>Jugador que está usando la app en este momento.</summary>
    public static class Sesion
    {
        public static Jugador Jugador;
    }

    /// <summary>Qué acertijo abrir en la pantalla de juego.</summary>
    public class ParametrosJuego
    {
        public int NumeroNivel;
        public bool EsDiario;

        public static ParametrosJuego DeNivel(int numero) { return new ParametrosJuego { NumeroNivel = numero }; }
        public static ParametrosJuego DeDiario() { return new ParametrosJuego { EsDiario = true }; }
    }

    /// <summary>Opciones del jugador guardadas en el dispositivo (PlayerPrefs).</summary>
    public static class Preferencias
    {
        public const int TemaOscuro = 0, TemaClaro = 1, TemaAutomatico = 2;
        public const int TextoPequeno = 0, TextoNormal = 1, TextoGrande = 2;
        public const int PaletaClasica = 0, PaletaOceano = 1, PaletaAurora = 2;

        static bool Leer(string clave, bool porDefecto) { return PlayerPrefs.GetInt(clave, porDefecto ? 1 : 0) == 1; }
        static void Guardar(string clave, bool valor) { PlayerPrefs.SetInt(clave, valor ? 1 : 0); PlayerPrefs.Save(); }
        static void Guardar(string clave, int valor) { PlayerPrefs.SetInt(clave, valor); PlayerPrefs.Save(); }

        public static bool Musica { get { return Leer("mindlock.musica", true); } set { Guardar("mindlock.musica", value); } }
        public static bool Efectos { get { return Leer("mindlock.efectos", true); } set { Guardar("mindlock.efectos", value); } }
        public static bool Vibracion { get { return Leer("mindlock.vibracion", true); } set { Guardar("mindlock.vibracion", value); } }
        public static bool MostrarTemporizador { get { return Leer("mindlock.temporizador", true); } set { Guardar("mindlock.temporizador", value); } }
        public static bool ConfirmarPistas { get { return Leer("mindlock.confirmarPistas", true); } set { Guardar("mindlock.confirmarPistas", value); } }
        public static int Tema { get { return PlayerPrefs.GetInt("mindlock.tema", TemaOscuro); } set { Guardar("mindlock.tema", value); } }
        public static int TamanoTexto { get { return PlayerPrefs.GetInt("mindlock.tamanoTexto", TextoNormal); } set { Guardar("mindlock.tamanoTexto", value); } }
        public static int Paleta { get { return PlayerPrefs.GetInt("mindlock.paleta", PaletaClasica); } set { Guardar("mindlock.paleta", value); } }
    }
}
