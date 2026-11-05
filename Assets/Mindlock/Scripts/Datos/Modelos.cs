using System;
using System.Collections.Generic;
using System.Text;

namespace Mindlock.Datos
{
    // =====================================================================
    // Modelos de datos (siguen el diccionario de datos del proyecto).
    // No dependen de Unity: se pueden usar con SQLite o con datos en memoria.
    // =====================================================================

    public enum Dificultad { Facil, Media, Dificil, Experto }
    public enum Categoria { Logica, Palabras, Patrones }
    public enum TipoRespuesta { Texto, OpcionMultiple, Secuencia }
    public enum EstadoNivel { Bloqueado, Desbloqueado, Completado }

    /// <summary>Reglas fijas del juego.</summary>
    public static class Reglas
    {
        public const int TotalNiveles = 30;
        public const int MaxLargoNombre = 30;
        public const int MaxPistasPorAcertijo = 3;
        public const int PistasIniciales = 3;
        public const int MaxPistasAcumuladas = 9;
        public const int MaxEstrellas = 3;
        public const int BonusDiario = 150;
    }

    /// <summary>Vidas, monedas y anuncios con recompensa. Todos los valores se cambian aquí.</summary>
    public static class Economia
    {
        public const int MaxVidas = 4;
        public const int MinutosPorVida = 50;          // se recupera 1 vida cada 50 minutos
        public const int AnunciosPorVida = 2;          // anuncios seguidos para ganar 1 vida
        public const int MonedasPorEstrella = 10;      // solo por estrellas nuevas en un nivel
        public const int MonedasDiario = 50;
        public const int MonedasPorLogro = 50;
        public const int MonedasPorAnuncio = 25;
        public const int PrecioLlenarVidas = 100;
        public const int PrecioPista = 50;
        public const int AnunciosRecompensaPorDia = 10;
    }

    /// <summary>Qué da un anuncio con recompensa.</summary>
    public enum Recompensa { Vida, Pista, Monedas, DuplicarMonedas }

    /// <summary>Tabla jugador.</summary>
    public class Jugador
    {
        public int Id;
        public string Nombre;
        public int PuntajeTotal;
        public int PistasDisponibles = Reglas.PistasIniciales;
        public int RachaDiaria;
        public DateTime? UltimoDiario;
        public DateTime FechaCreacion = DateTime.Now;

        public int Vidas = Economia.MaxVidas;
        public int Monedas;
        /// <summary>Desde cuándo corre la recarga de la próxima vida. null = vidas llenas.</summary>
        public DateTime? RecargaVidaDesde;
        public int AnunciosParaVida;     // anuncios ya vistos hacia la próxima vida
        public int AnunciosHoy;          // anuncios con recompensa vistos el día de FechaAnuncios
        public DateTime? FechaAnuncios;

        public string Inicial
        {
            get { return string.IsNullOrEmpty(Nombre) ? "?" : Nombre.Substring(0, 1).ToUpperInvariant(); }
        }
    }

    /// <summary>Tabla nivel. Numero = 0 se usa para el acertijo diario.</summary>
    public class Nivel
    {
        public int Numero;
        public Dificultad Dificultad;
        public int AcertijoId;
        public int PuntajeBase;
        public int TiempoLimite;      // segundos; 0 = sin límite
        public int TiempoObjetivo;    // segundos antes de empezar a perder puntos por tiempo

        public bool TieneLimite { get { return TiempoLimite > 0; } }
    }

    /// <summary>Tabla pista.</summary>
    public class Pista
    {
        public int Orden;
        public string Texto;
        public string TextoEn;
        public int Penalizacion;

        public string TextoVisible { get { return Idioma.EsIngles && !string.IsNullOrEmpty(TextoEn) ? TextoEn : Texto; } }
    }

    /// <summary>Versión en inglés de un acertijo. Respuesta y Opciones en null = las mismas que en español.</summary>
    public class AcertijoEn
    {
        public string Enunciado;
        public string Respuesta;
        public List<string> Opciones;
    }

    /// <summary>Tabla acertijo.</summary>
    public class Acertijo
    {
        public int Id;
        public Categoria Categoria;
        public TipoRespuesta Tipo;
        public string Enunciado;

        /// <summary>
        /// Texto: respuestas válidas separadas por ";" (ej. "hoyo;agujero").
        /// Opción múltiple: el texto exacto de la opción correcta.
        /// Secuencia: fichas en orden separadas por "|" (ej. "48|96").
        /// </summary>
        public string Respuesta;

        /// <summary>Opción múltiple: las opciones. Secuencia: las fichas disponibles.</summary>
        public List<string> Opciones = new List<string>();

        /// <summary>Secuencia: número de casillas a completar.</summary>
        public int Casillas = 1;

        public List<Pista> Pistas = new List<Pista>();

        public AcertijoEn En;

        public string EnunciadoVisible { get { return Idioma.EsIngles && En != null ? En.Enunciado : Enunciado; } }

        public List<string> OpcionesVisibles
        {
            get { return Idioma.EsIngles && En != null && En.Opciones != null ? En.Opciones : Opciones; }
        }

        /// <summary>Acepta la respuesta en español o en inglés (sirve aunque se cambie de idioma a mitad del acertijo).</summary>
        public bool EsCorrecta(string intento)
        {
            string n = Normalizar(intento);
            if (n.Length == 0) return false;
            return Coincide(n, Respuesta) || (En != null && En.Respuesta != null && Coincide(n, En.Respuesta));
        }

        bool Coincide(string n, string respuesta)
        {
            if (Tipo == TipoRespuesta.Secuencia) return n == Normalizar(respuesta);
            foreach (var r in respuesta.Split(';'))
                if (Normalizar(r) == n) return true;
            return false;
        }

        static readonly string[] Articulos = { "el ", "la ", "los ", "las ", "un ", "una ", "unos ", "unas ", "the ", "a ", "an " };

        /// <summary>Minúsculas, sin acentos, sin signos ni artículo inicial.</summary>
        public static string Normalizar(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var sb = new StringBuilder(s.Length);
            bool espacio = false;
            foreach (char c0 in s.Trim().ToLowerInvariant())
            {
                char c = QuitarAcento(c0);
                if ("¿?¡!.,;:\"'".IndexOf(c) >= 0) continue;
                if (char.IsWhiteSpace(c))
                {
                    if (!espacio && sb.Length > 0) sb.Append(' ');
                    espacio = true;
                    continue;
                }
                espacio = false;
                sb.Append(c);
            }
            string r = sb.ToString().Trim();
            foreach (var art in Articulos)
            {
                if (r.StartsWith(art, StringComparison.Ordinal)) { r = r.Substring(art.Length); break; }
            }
            return r;
        }

        static char QuitarAcento(char c)
        {
            switch (c)
            {
                case 'á': case 'à': case 'ä': case 'â': return 'a';
                case 'é': case 'è': case 'ë': case 'ê': return 'e';
                case 'í': case 'ì': case 'ï': case 'î': return 'i';
                case 'ó': case 'ò': case 'ö': case 'ô': return 'o';
                case 'ú': case 'ù': case 'ü': case 'û': return 'u';
                case 'ñ': return 'n';
                default: return c;
            }
        }
    }

    /// <summary>Tabla progreso_nivel (por jugador).</summary>
    public class ProgresoNivel
    {
        public int Numero;
        public EstadoNivel Estado;
        public int MejorPuntaje;
        public int Estrellas;          // 0 a 3
        public int MejorTiempo;        // segundos; 0 = nunca completado
        public bool SinPistas;         // alguna vez completado sin pistas
        public bool PrimerIntento;     // alguna vez completado al primer intento
        public DateTime? FechaCompletado;
    }

    /// <summary>Lo que la pantalla de juego envía al terminar un acertijo.</summary>
    public class ResultadoIntento
    {
        public int NumeroNivel;
        public int AcertijoId;
        public string Respuesta;
        public int Segundos;
        public int Intentos = 1;
        public int PistasUsadas;
        public int PenalizacionPistas;
    }

    /// <summary>Tabla intento: cada respuesta enviada, correcta o incorrecta.</summary>
    public class RegistroIntento
    {
        public int JugadorId;
        public int AcertijoId;
        public string Respuesta;
        public bool EsCorrecto;
        public int Segundos;
        public int PistasUsadas;
        public int Puntaje;
        public DateTime Fecha = DateTime.Now;
    }

    /// <summary>Lo que se muestra en la pantalla de resultado.</summary>
    public class ResultadoNivel
    {
        public int NumeroNivel;
        public bool EsDiario;
        public int Segundos;
        public int Intentos;
        public int PistasUsadas;
        public int PuntajeBase;
        public int DescuentoTiempo;
        public int DescuentoIntentos;
        public int DescuentoPistas;
        public int BonusDiario;
        public int Puntaje;
        public int Estrellas;
        public bool NuevoRecord;
        public int NivelDesbloqueado;  // 0 = ninguno
        public bool PistaGanada;
        public int MonedasGanadas;
        public int Racha;
        public List<Logro> LogrosNuevos = new List<Logro>();
    }

    /// <summary>Tabla logro.</summary>
    public class Logro
    {
        public string Id;
        public string Nombre;
        public string Descripcion;
        public string Icono;           // nombre del ícono (ver Iconos.cs)
        public int Meta = 1;
        public string NombreEn, DescripcionEn;

        public string NombreVisible { get { return Idioma.EsIngles && NombreEn != null ? NombreEn : Nombre; } }
        public string DescripcionVisible { get { return Idioma.EsIngles && DescripcionEn != null ? DescripcionEn : Descripcion; } }
    }

    /// <summary>Tabla jugador_logro.</summary>
    public class LogroJugador
    {
        public Logro Logro;
        public int Progreso;
        public bool Obtenido;
        public DateTime? Fecha;
    }

    /// <summary>Tabla acertijo_diario.</summary>
    public class AcertijoDiario
    {
        public DateTime Fecha;
        public Nivel Nivel;
        public Acertijo Acertijo;
        public int PuntosBonus;
    }
}
