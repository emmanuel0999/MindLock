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
        /// <summary>Suma una pista (anuncio con recompensa). Devuelve false si ya tiene el máximo.</summary>
        bool GanarPista(int jugadorId);
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
    /// <summary>Vidas, monedas y anuncios con recompensa del jugador.</summary>
    public interface IEconomiaService
    {
        /// <summary>Vidas actuales; antes suma las que se recargaron con el tiempo.</summary>
        int Vidas(int jugadorId);
        /// <summary>Cuánto falta para la próxima vida (TimeSpan.Zero si están llenas).</summary>
        TimeSpan TiempoParaVida(int jugadorId);
        /// <summary>Resta una vida por una respuesta incorrecta o tiempo agotado. Devuelve las que quedan.</summary>
        int PerderVida(int jugadorId);
        bool LlenarVidasConMonedas(int jugadorId);
        bool ComprarPista(int jugadorId);
        int AnunciosRestantesHoy(int jugadorId);
        /// <summary>
        /// Entrega la recompensa de un anuncio visto completo. Vida: cada AnunciosPorVida anuncios suma 1.
        /// DuplicarMonedas suma otra vez "cantidad". Devuelve false si no se pudo (límite diario, máximo...).
        /// </summary>
        bool Recompensar(int jugadorId, Recompensa tipo, int cantidad = 0);
    }

    public interface IIntentoService
    {
        /// <summary>Guarda una respuesta incorrecta. Las correctas las guardan RegistrarNivel y RegistrarDiario.</summary>
        void RegistrarFallo(int jugadorId, int acertijoId, string respuesta, int segundos, int pistasUsadas);
        IReadOnlyList<RegistroIntento> ObtenerIntentos(int jugadorId);
    }
}
