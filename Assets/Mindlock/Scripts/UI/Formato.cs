using System;
using System.Globalization;
using Mindlock.Datos;

namespace Mindlock.UI
{
    /// <summary>Textos con formato en español (sin depender de la cultura del teléfono).</summary>
    public static class Formato
    {
        static readonly NumberFormatInfo Numeros = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };
        static readonly string[] NombresDias = { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
        static readonly string[] NombresMeses = { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };

        public static string Numero(int n) { return n.ToString("#,0", Numeros); }

        public static string Puntos(int n) { return Numero(n) + " pts"; }

        public static string Tiempo(int segundos)
        {
            if (segundos < 0) segundos = 0;
            return (segundos / 60).ToString("00") + ":" + (segundos % 60).ToString("00");
        }

        public static string TiempoLargo(TimeSpan t)
        {
            if (t.TotalSeconds < 0) t = TimeSpan.Zero;
            return ((int)t.TotalHours).ToString("00") + ":" + t.Minutes.ToString("00") + ":" + t.Seconds.ToString("00");
        }

        public static string Fecha(DateTime f)
        {
            return NombresDias[(int)f.DayOfWeek] + " " + f.Day + " de " + NombresMeses[f.Month - 1];
        }

        public static string FechaCorta(DateTime f)
        {
            return f.Day.ToString("00") + "/" + f.Month.ToString("00") + "/" + f.Year;
        }

        public static string Racha(int dias)
        {
            return dias == 1 ? "1 día seguido" : dias + " días seguidos";
        }

        public static string NombreDificultad(Dificultad d)
        {
            switch (d)
            {
                case Dificultad.Facil: return "Fácil";
                case Dificultad.Media: return "Media";
                case Dificultad.Dificil: return "Difícil";
                default: return "Experto";
            }
        }

        public static string NombreCategoria(Categoria c)
        {
            switch (c)
            {
                case Categoria.Logica: return "Lógica";
                case Categoria.Palabras: return "Palabras";
                default: return "Patrones";
            }
        }

        /// <summary>Sufijo de clase USS: facil, media, dificil, experto.</summary>
        public static string Clave(Dificultad d) { return d.ToString().ToLowerInvariant(); }

        /// <summary>Sufijo de clase USS: logica, palabras, patrones.</summary>
        public static string Clave(Categoria c) { return c.ToString().ToLowerInvariant(); }
    }
}
