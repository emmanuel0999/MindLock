using System;
using Mindlock.Datos;

namespace Mindlock.Logica
{
    /// <summary>
    /// Reglas de puntaje y estrellas (capa de lógica).
    /// Puntaje = base - tiempo extra - intentos extra - pistas  (mínimo 10 % de la base).
    /// Estrellas: 3 si conserva el 85 % o más, 2 si conserva el 60 % o más, si no 1.
    /// </summary>
    public static class CalculadoraPuntaje
    {
        public const int DescuentoPorIntento = 50;     // por cada intento fallido
        public const int DescuentoPorSegundo = 2;      // por cada segundo después del tiempo objetivo
        public const float TopeDescuento = 0.3f;       // tiempo e intentos restan como máximo el 30 % cada uno
        public const float PuntajeMinimo = 0.1f;

        public static int DescuentoTiempo(Nivel nivel, int segundos)
        {
            int extra = segundos - nivel.TiempoObjetivo;
            if (extra <= 0) return 0;
            return Math.Min(extra * DescuentoPorSegundo, (int)(nivel.PuntajeBase * TopeDescuento));
        }

        public static int DescuentoIntentos(Nivel nivel, int intentos)
        {
            int fallos = Math.Max(0, intentos - 1);
            return Math.Min(fallos * DescuentoPorIntento, (int)(nivel.PuntajeBase * TopeDescuento));
        }

        /// <summary>Puntaje que obtendría el jugador si acertara ahora mismo.</summary>
        public static int Potencial(Nivel nivel, int segundos, int intentos, int penalizacionPistas)
        {
            int p = nivel.PuntajeBase - DescuentoTiempo(nivel, segundos) - DescuentoIntentos(nivel, intentos) - penalizacionPistas;
            return Math.Max(p, (int)(nivel.PuntajeBase * PuntajeMinimo));
        }

        public static int Estrellas(int puntaje, int puntajeBase)
        {
            if (puntajeBase <= 0) return 0;
            float r = (float)puntaje / puntajeBase;
            if (r >= 0.85f) return 3;
            if (r >= 0.6f) return 2;
            return 1;
        }

        public static ResultadoNivel Calcular(Nivel nivel, ResultadoIntento intento)
        {
            var r = new ResultadoNivel
            {
                NumeroNivel = nivel.Numero,
                Segundos = intento.Segundos,
                Intentos = intento.Intentos,
                PistasUsadas = intento.PistasUsadas,
                PuntajeBase = nivel.PuntajeBase,
                DescuentoTiempo = DescuentoTiempo(nivel, intento.Segundos),
                DescuentoIntentos = DescuentoIntentos(nivel, intento.Intentos),
                DescuentoPistas = intento.PenalizacionPistas
            };
            r.Puntaje = Potencial(nivel, intento.Segundos, intento.Intentos, intento.PenalizacionPistas);
            r.Estrellas = Estrellas(r.Puntaje, nivel.PuntajeBase);
            return r;
        }
    }
}
