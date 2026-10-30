using System;
using System.Collections.Generic;

namespace Mindlock.Datos
{
    // =====================================================================
    // Contratos entre la interfaz y la capa de datos.
    // Las pantallas SOLO usan estas interfaces. Hoy las implementa
    // MemoriaJuegoService (datos de prueba); cuando exista la base SQLite,
    // se crea una clase que implemente lo mismo y se registra en Servicios.
    // =====================================================================

    public interface IPerfilService
    {
        IReadOnlyList<Jugador> ObtenerPerfiles();
        Jugador ObtenerPerfil(int jugadorId);
        bool ExisteNombre(string nombre);
        /// <summary>Lanza ArgumentException si el nombre está vacío o supera 30 caracteres.</summary>
        Jugador CrearPerfil(string nombre);
        void ReiniciarProgreso(int jugadorId);
    }

    public interface IProgresoService
    {
        IReadOnlyList<Nivel> ObtenerNiveles();
        Nivel ObtenerNivel(int numero);
        ProgresoNivel ObtenerProgreso(int jugadorId, int numero);
        int NivelesCompletados(int jugadorId);
        int EstrellasTotales(int jugadorId);
        /// <summary>Descuenta una pista del contador del jugador. Devuelve false si no le quedan.</summary>
        bool UsarPista(int jugadorId);
        /// <summary>Guarda el resultado, desbloquea el siguiente nivel y revisa logros.</summary>
        ResultadoNivel RegistrarNivel(int jugadorId, ResultadoIntento intento);
    }

    public interface IAcertijoService
    {
        Acertijo ObtenerAcertijo(int acertijoId);
    }

    public interface IDiarioService
    {
        AcertijoDiario ObtenerDiario(DateTime fecha);
        bool ResueltoHoy(int jugadorId);
        int RachaActual(int jugadorId);
        /// <summary>True si el jugador tenía racha y la perdió por no jugar ayer.</summary>
        bool RachaPerdida(int jugadorId);
        /// <summary>7 valores (lunes a domingo) de la semana de "fecha": true = resuelto.</summary>
        bool[] DiasSemana(int jugadorId, DateTime fecha);
        ResultadoNivel RegistrarDiario(int jugadorId, ResultadoIntento intento);
    }

    public interface ILogroService
    {
        IReadOnlyList<LogroJugador> ObtenerLogros(int jugadorId);
    }

    /// <summary>Historial de respuestas (tabla intento).</summary>
    public interface IIntentoService
    {
        /// <summary>Guarda una respuesta incorrecta. Las correctas las guardan RegistrarNivel y RegistrarDiario.</summary>
        void RegistrarFallo(int jugadorId, int acertijoId, string respuesta, int segundos, int pistasUsadas);
        IReadOnlyList<RegistroIntento> ObtenerIntentos(int jugadorId);
    }
}
