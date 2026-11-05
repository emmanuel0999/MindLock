using System;

namespace Mindlock.Datos
{
    /// <summary>Vidas que se recargan con el tiempo, monedas y anuncios con recompensa (ver Economia).</summary>
    public partial class MemoriaJuegoService
    {
        static readonly TimeSpan DuracionVida = TimeSpan.FromMinutes(Economia.MinutosPorVida);

        public int Vidas(int jugadorId) { return Vidas(jugadorId, DateTime.Now); }

        /// <summary>Con "ahora" explícito para las pruebas.</summary>
        public int Vidas(int jugadorId, DateTime ahora)
        {
            var j = ObtenerPerfil(jugadorId);
            if (j == null) return 0;
            if (Recargar(j, ahora)) Guardar();
            return j.Vidas;
        }

        public TimeSpan TiempoParaVida(int jugadorId) { return TiempoParaVida(jugadorId, DateTime.Now); }

        public TimeSpan TiempoParaVida(int jugadorId, DateTime ahora)
        {
            var j = ObtenerPerfil(jugadorId);
            if (j == null) return TimeSpan.Zero;
            if (Recargar(j, ahora)) Guardar();
            if (j.Vidas >= Economia.MaxVidas || !j.RecargaVidaDesde.HasValue) return TimeSpan.Zero;
            return j.RecargaVidaDesde.Value + DuracionVida - ahora;
        }

        public int PerderVida(int jugadorId) { return PerderVida(jugadorId, DateTime.Now); }

        public int PerderVida(int jugadorId, DateTime ahora)
        {
            var j = ObtenerPerfil(jugadorId);
            if (j == null) return 0;
            Recargar(j, ahora);
            if (j.Vidas > 0)
            {
                // La recarga empieza cuando se pierde la primera vida
                if (j.Vidas == Economia.MaxVidas) j.RecargaVidaDesde = ahora;
                j.Vidas--;
            }
            Guardar();
            return j.Vidas;
        }

        public bool LlenarVidasConMonedas(int jugadorId)
        {
            var j = ObtenerPerfil(jugadorId);
            if (j == null) return false;
            Recargar(j, DateTime.Now);
            if (j.Vidas >= Economia.MaxVidas || j.Monedas < Economia.PrecioLlenarVidas) return false;
            j.Monedas -= Economia.PrecioLlenarVidas;
            LlenarVidas(j);
            Guardar();
            return true;
        }

        public bool ComprarPista(int jugadorId)
        {
            var j = ObtenerPerfil(jugadorId);
            if (j == null || j.Monedas < Economia.PrecioPista || j.PistasDisponibles >= Reglas.MaxPistasAcumuladas) return false;
            j.Monedas -= Economia.PrecioPista;
            j.PistasDisponibles++;
            Guardar();
            return true;
        }

        public int AnunciosRestantesHoy(int jugadorId) { return AnunciosRestantesHoy(jugadorId, DateTime.Now); }

        public int AnunciosRestantesHoy(int jugadorId, DateTime ahora)
        {
            var j = ObtenerPerfil(jugadorId);
            if (j == null) return 0;
            return Math.Max(0, Economia.AnunciosRecompensaPorDia - AnunciosDelDia(j, ahora));
        }

        public bool Recompensar(int jugadorId, Recompensa tipo, int cantidad = 0) { return Recompensar(jugadorId, tipo, cantidad, DateTime.Now); }

        public bool Recompensar(int jugadorId, Recompensa tipo, int cantidad, DateTime ahora)
        {
            var j = ObtenerPerfil(jugadorId);
            if (j == null || AnunciosDelDia(j, ahora) >= Economia.AnunciosRecompensaPorDia) return false;
            Recargar(j, ahora);

            switch (tipo)
            {
                case Recompensa.Vida:
                    if (j.Vidas >= Economia.MaxVidas) return false;
                    j.AnunciosParaVida++;
                    if (j.AnunciosParaVida >= Economia.AnunciosPorVida)
                    {
                        j.AnunciosParaVida = 0;
                        j.Vidas++;
                        if (j.Vidas >= Economia.MaxVidas) j.RecargaVidaDesde = null;
                    }
                    break;
                case Recompensa.Pista:
                    if (j.PistasDisponibles >= Reglas.MaxPistasAcumuladas) return false;
                    j.PistasDisponibles++;
                    break;
                case Recompensa.Monedas:
                    j.Monedas += Economia.MonedasPorAnuncio;
                    break;
                case Recompensa.DuplicarMonedas:
                    if (cantidad <= 0) return false;
                    j.Monedas += cantidad;
                    break;
            }

            j.FechaAnuncios = ahora.Date;
            j.AnunciosHoy++;
            Guardar();
            return true;
        }

        static int AnunciosDelDia(Jugador j, DateTime ahora)
        {
            return j.FechaAnuncios.HasValue && j.FechaAnuncios.Value.Date == ahora.Date ? j.AnunciosHoy : 0;
        }

        static void LlenarVidas(Jugador j)
        {
            j.Vidas = Economia.MaxVidas;
            j.RecargaVidaDesde = null;
            j.AnunciosParaVida = 0;
        }

        /// <summary>Suma las vidas recargadas desde RecargaVidaDesde. Devuelve true si cambió algo.</summary>
        static bool Recargar(Jugador j, DateTime ahora)
        {
            if (j.Vidas >= Economia.MaxVidas)
            {
                if (!j.RecargaVidaDesde.HasValue) return false;
                j.RecargaVidaDesde = null;
                return true;
            }
            // Sin fecha (archivo viejo) o reloj atrasado: la recarga empieza ahora
            if (!j.RecargaVidaDesde.HasValue || j.RecargaVidaDesde.Value > ahora)
            {
                j.RecargaVidaDesde = ahora;
                return true;
            }

            int ganadas = (int)((ahora - j.RecargaVidaDesde.Value).Ticks / DuracionVida.Ticks);
            if (ganadas <= 0) return false;
            j.Vidas = Math.Min(Economia.MaxVidas, j.Vidas + ganadas);
            if (j.Vidas >= Economia.MaxVidas) j.RecargaVidaDesde = null;
            else j.RecargaVidaDesde = j.RecargaVidaDesde.Value + TimeSpan.FromTicks(DuracionVida.Ticks * ganadas);
            return true;
        }
    }
}
