using System;
using UnityEngine;

namespace Mindlock
{
    /// <summary>
    /// Quién muestra los anuncios: AnunciosLevelPlay (Unity Ads y AdMob, en Android e iOS con
    /// ClavesAnuncios configuradas) o AnunciosDemo (anuncios propios, sin conexión, en el resto).
    /// UIManager elige uno y lo registra con Anuncios.Configurar(...): las pantallas no cambian.
    /// </summary>
    public interface IProveedorAnuncios
    {
        void MostrarBanner(bool visible);
        /// <summary>Anuncio a pantalla completa que se puede cerrar tras unos segundos.</summary>
        void MostrarIntersticial(Action alCerrar);
        /// <summary>Anuncio con recompensa: true si el jugador lo vio completo. "premio" describe lo que gana.</summary>
        void MostrarRecompensa(string premio, Action<bool> alTerminar);
        /// <summary>Hay un anuncio con recompensa cargado para mostrar ahora.</summary>
        bool RecompensaLista { get; }
    }

    /// <summary>Reglas de cuándo aparece cada anuncio.</summary>
    public static class Anuncios
    {
        public const int IntersticialCadaNiveles = 3;        // niveles completados entre un anuncio y otro
        public const float SegundosEntreIntersticiales = 120f;
        public const int NivelMinimoIntersticial = 3;        // los primeros niveles nunca tienen anuncio

        static IProveedorAnuncios proveedor;
        static int nivelesSinAnuncio;
        static float ultimoIntersticial = -SegundosEntreIntersticiales;

        public static bool Activos { get { return proveedor != null; } }

        public static bool RecompensaLista { get { return proveedor != null && proveedor.RecompensaLista; } }

        /// <summary>null desactiva todos los anuncios.</summary>
        public static void Configurar(IProveedorAnuncios nuevo)
        {
            if (proveedor != null) proveedor.MostrarBanner(false);
            proveedor = nuevo;
            nivelesSinAnuncio = 0;
        }

        public static void MostrarBanner(bool visible)
        {
            if (proveedor != null) proveedor.MostrarBanner(visible);
        }

        public static void RegistrarNivelCompletado()
        {
            nivelesSinAnuncio++;
        }

        /// <summary>True si al salir del resultado de este nivel toca un anuncio a pantalla completa.</summary>
        public static bool TocaIntersticial(int numeroNivel, float ahora)
        {
            return proveedor != null
                && numeroNivel >= NivelMinimoIntersticial
                && nivelesSinAnuncio >= IntersticialCadaNiveles
                && ahora - ultimoIntersticial >= SegundosEntreIntersticiales;
        }

        /// <summary>Muestra el anuncio si toca y después sigue con "continuar" (siempre se llama).</summary>
        public static void DespuesDeNivel(int numeroNivel, Action continuar)
        {
            float ahora = Time.realtimeSinceStartup;
            if (!TocaIntersticial(numeroNivel, ahora)) { continuar(); return; }

            nivelesSinAnuncio = 0;
            ultimoIntersticial = ahora;
            proveedor.MostrarIntersticial(continuar);
        }

        public static void MostrarRecompensa(string premio, Action<bool> alTerminar)
        {
            if (proveedor == null) { alTerminar(false); return; }
            proveedor.MostrarRecompensa(premio, alTerminar);
        }
    }
}
