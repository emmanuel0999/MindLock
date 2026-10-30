using System;
using System.Collections.Generic;
using System.Linq;
using Mindlock.Logica;

namespace Mindlock.Datos
{
    /// <summary>
    /// Implementación EN MEMORIA de todos los servicios. Con datos de demo sirve
    /// para probar la interfaz; ArchivoJuegoService hereda de esta clase y
    /// guarda cada cambio en el dispositivo (ver Guardar()).
    /// </summary>
    public class MemoriaJuegoService : IPerfilService, IProgresoService, IAcertijoService, IDiarioService, ILogroService, IIntentoService
    {
        protected readonly List<Jugador> jugadores = new List<Jugador>();
        protected readonly List<Nivel> niveles = new List<Nivel>();
        protected readonly Dictionary<int, Acertijo> acertijos = new Dictionary<int, Acertijo>();
        protected readonly List<Acertijo> diarios;
        protected readonly List<Logro> logros;
        protected readonly Dictionary<int, Dictionary<int, ProgresoNivel>> progreso = new Dictionary<int, Dictionary<int, ProgresoNivel>>();
        protected readonly Dictionary<int, Dictionary<string, DateTime>> logrosObtenidos = new Dictionary<int, Dictionary<string, DateTime>>();
        protected readonly Dictionary<int, HashSet<DateTime>> diasDiario = new Dictionary<int, HashSet<DateTime>>();
        protected readonly HashSet<int> rachasPerdidas = new HashSet<int>();
        protected readonly List<RegistroIntento> intentos = new List<RegistroIntento>();
        protected int siguienteId = 1;

        public MemoriaJuegoService(bool datosDeDemo = true)
        {
            var lista = CatalogoAcertijos.AcertijosDeNiveles();
            for (int i = 0; i < lista.Count; i++)
            {
                var a = lista[i];
                var nivel = CatalogoAcertijos.CrearNivel(i + 1, a.Id);
                CatalogoAcertijos.AsignarPenalizaciones(a, nivel.Dificultad);
                acertijos[a.Id] = a;
                niveles.Add(nivel);
            }

            diarios = CatalogoAcertijos.AcertijosDiarios();
            foreach (var d in diarios)
            {
                CatalogoAcertijos.AsignarPenalizacionesDiario(d);
                acertijos[d.Id] = d;
            }

            logros = CatalogoAcertijos.Logros();
            var patrones = logros.Find(l => l.Id == "maestro-patrones");
            if (patrones != null) patrones.Meta = niveles.Count(n => acertijos[n.AcertijoId].Categoria == Categoria.Patrones);

            if (datosDeDemo) CargarDemo();
        }

        /// <summary>Se llama después de cada cambio. En memoria no hace nada.</summary>
        protected virtual void Guardar() { }

        // ==================================================================
        // PERFILES
        // ==================================================================

        public IReadOnlyList<Jugador> ObtenerPerfiles() { return jugadores; }

        public Jugador ObtenerPerfil(int jugadorId) { return jugadores.Find(j => j.Id == jugadorId); }

        public bool ExisteNombre(string nombre)
        {
            string n = (nombre ?? "").Trim();
            return jugadores.Exists(j => string.Equals(j.Nombre, n, StringComparison.OrdinalIgnoreCase));
        }

        public Jugador CrearPerfil(string nombre)
        {
            nombre = (nombre ?? "").Trim();
            if (nombre.Length == 0) throw new ArgumentException("El nombre es obligatorio.");
            if (nombre.Length > Reglas.MaxLargoNombre) throw new ArgumentException("El nombre no puede superar " + Reglas.MaxLargoNombre + " caracteres.");

            var j = new Jugador { Id = siguienteId++, Nombre = nombre };
            jugadores.Add(j);
            InicializarProgreso(j.Id);
            Guardar();
            return j;
        }

        public void ReiniciarProgreso(int jugadorId)
        {
            var j = ObtenerPerfil(jugadorId);
            if (j == null) return;
            j.PuntajeTotal = 0;
            j.PistasDisponibles = Reglas.PistasIniciales;
            j.RachaDiaria = 0;
            j.UltimoDiario = null;
            rachasPerdidas.Remove(jugadorId);
            intentos.RemoveAll(i => i.JugadorId == jugadorId);
            InicializarProgreso(jugadorId);
            Guardar();
        }

        protected void InicializarProgreso(int jugadorId)
        {
            var d = new Dictionary<int, ProgresoNivel>();
            foreach (var n in niveles)
                d[n.Numero] = new ProgresoNivel { Numero = n.Numero, Estado = n.Numero == 1 ? EstadoNivel.Desbloqueado : EstadoNivel.Bloqueado };
            progreso[jugadorId] = d;
            logrosObtenidos[jugadorId] = new Dictionary<string, DateTime>();
            diasDiario[jugadorId] = new HashSet<DateTime>();
        }

        // ==================================================================
        // NIVELES Y PROGRESO
        // ==================================================================

        public IReadOnlyList<Nivel> ObtenerNiveles() { return niveles; }

        public Nivel ObtenerNivel(int numero) { return niveles.Find(n => n.Numero == numero); }

        public ProgresoNivel ObtenerProgreso(int jugadorId, int numero)
        {
            Dictionary<int, ProgresoNivel> d;
            ProgresoNivel p;
            if (progreso.TryGetValue(jugadorId, out d) && d.TryGetValue(numero, out p)) return p;
            return new ProgresoNivel { Numero = numero, Estado = EstadoNivel.Bloqueado };
        }

        public int NivelesCompletados(int jugadorId)
        {
            return progreso[jugadorId].Values.Count(p => p.Estado == EstadoNivel.Completado);
        }

        public int EstrellasTotales(int jugadorId)
        {
            return progreso[jugadorId].Values.Sum(p => p.Estrellas);
        }

        public bool UsarPista(int jugadorId)
        {
            var j = ObtenerPerfil(jugadorId);
            if (j == null || j.PistasDisponibles <= 0) return false;
            j.PistasDisponibles--;
            Guardar();
            return true;
        }

        public ResultadoNivel RegistrarNivel(int jugadorId, ResultadoIntento intento)
        {
            var jugador = ObtenerPerfil(jugadorId);
            var nivel = ObtenerNivel(intento.NumeroNivel);
            var p = progreso[jugadorId][nivel.Numero];
            var r = CalculadoraPuntaje.Calcular(nivel, intento);

            bool primeraVez = p.Estado != EstadoNivel.Completado;
            r.NuevoRecord = !primeraVez && r.Puntaje > p.MejorPuntaje;

            if (r.Puntaje > p.MejorPuntaje)
            {
                jugador.PuntajeTotal += r.Puntaje - p.MejorPuntaje;
                p.MejorPuntaje = r.Puntaje;
            }

            // Primera vez con 3 estrellas en este nivel: +1 pista
            if (r.Estrellas == 3 && p.Estrellas < 3 && jugador.PistasDisponibles < Reglas.MaxPistasAcumuladas)
            {
                jugador.PistasDisponibles++;
                r.PistaGanada = true;
            }

            p.Estrellas = Math.Max(p.Estrellas, r.Estrellas);
            if (primeraVez) p.FechaCompletado = DateTime.Now;
            p.Estado = EstadoNivel.Completado;
            p.MejorTiempo = p.MejorTiempo == 0 ? intento.Segundos : Math.Min(p.MejorTiempo, intento.Segundos);
            if (intento.PistasUsadas == 0) p.SinPistas = true;
            if (intento.Intentos == 1) p.PrimerIntento = true;

            if (nivel.Numero < Reglas.TotalNiveles)
            {
                var siguiente = progreso[jugadorId][nivel.Numero + 1];
                if (siguiente.Estado == EstadoNivel.Bloqueado)
                {
                    siguiente.Estado = EstadoNivel.Desbloqueado;
                    r.NivelDesbloqueado = nivel.Numero + 1;
                }
            }

            r.LogrosNuevos = EvaluarLogros(jugadorId, DateTime.Now);
            AnotarAcierto(jugadorId, nivel.AcertijoId, intento, r.Puntaje);
            return r;
        }

        // ==================================================================
        // ACERTIJOS
        // ==================================================================

        public Acertijo ObtenerAcertijo(int acertijoId)
        {
            Acertijo a;
            return acertijos.TryGetValue(acertijoId, out a) ? a : null;
        }

        // ==================================================================
        // ACERTIJO DIARIO Y RACHA
        // ==================================================================

        public virtual AcertijoDiario ObtenerDiario(DateTime fecha)
        {
            var a = diarios[(fecha.DayOfYear + fecha.Year) % diarios.Count];
            return new AcertijoDiario
            {
                Fecha = fecha.Date,
                Acertijo = a,
                Nivel = CatalogoAcertijos.NivelDiario(a.Id),
                PuntosBonus = Reglas.BonusDiario
            };
        }

        public bool ResueltoHoy(int jugadorId)
        {
            return diasDiario[jugadorId].Contains(DateTime.Today);
        }

        public int RachaActual(int jugadorId)
        {
            RevisarRacha(jugadorId);
            var j = ObtenerPerfil(jugadorId);
            return j == null ? 0 : j.RachaDiaria;
        }

        public bool RachaPerdida(int jugadorId)
        {
            RevisarRacha(jugadorId);
            return rachasPerdidas.Contains(jugadorId);
        }

        void RevisarRacha(int jugadorId)
        {
            var j = ObtenerPerfil(jugadorId);
            if (j == null || !j.UltimoDiario.HasValue || j.RachaDiaria == 0) return;
            if (j.UltimoDiario.Value.Date < DateTime.Today.AddDays(-1))
            {
                j.RachaDiaria = 0;
                rachasPerdidas.Add(jugadorId);
                Guardar();
            }
        }

        public bool[] DiasSemana(int jugadorId, DateTime fecha)
        {
            int desdeLunes = ((int)fecha.DayOfWeek + 6) % 7;
            var lunes = fecha.Date.AddDays(-desdeLunes);
            var r = new bool[7];
            for (int i = 0; i < 7; i++) r[i] = diasDiario[jugadorId].Contains(lunes.AddDays(i));
            return r;
        }

        public ResultadoNivel RegistrarDiario(int jugadorId, ResultadoIntento intento)
        {
            var j = ObtenerPerfil(jugadorId);
            var diario = ObtenerDiario(DateTime.Today);
            var r = CalculadoraPuntaje.Calcular(diario.Nivel, intento);
            r.EsDiario = true;
            r.BonusDiario = diario.PuntosBonus;
            r.Puntaje += r.BonusDiario;

            if (!ResueltoHoy(jugadorId))
            {
                RevisarRacha(jugadorId);
                bool jugoAyer = j.UltimoDiario.HasValue && j.UltimoDiario.Value.Date == DateTime.Today.AddDays(-1);
                j.RachaDiaria = jugoAyer ? j.RachaDiaria + 1 : 1;
                j.UltimoDiario = DateTime.Today;
                diasDiario[jugadorId].Add(DateTime.Today);
                rachasPerdidas.Remove(jugadorId);
                j.PuntajeTotal += r.Puntaje;
                if (j.PistasDisponibles < Reglas.MaxPistasAcumuladas)
                {
                    j.PistasDisponibles++;
                    r.PistaGanada = true;
                }
            }

            r.Racha = j.RachaDiaria;
            r.LogrosNuevos = EvaluarLogros(jugadorId, DateTime.Now);
            AnotarAcierto(jugadorId, diario.Acertijo.Id, intento, r.Puntaje);
            return r;
        }

        // ==================================================================
        // HISTORIAL DE INTENTOS
        // ==================================================================

        public void RegistrarFallo(int jugadorId, int acertijoId, string respuesta, int segundos, int pistasUsadas)
        {
            intentos.Add(new RegistroIntento
            {
                JugadorId = jugadorId,
                AcertijoId = acertijoId,
                Respuesta = respuesta,
                EsCorrecto = false,
                Segundos = Math.Max(0, segundos),
                PistasUsadas = pistasUsadas
            });
            Guardar();
        }

        public IReadOnlyList<RegistroIntento> ObtenerIntentos(int jugadorId)
        {
            return intentos.FindAll(i => i.JugadorId == jugadorId);
        }

        /// <summary>Guarda la respuesta correcta en el historial y persiste el resultado.</summary>
        void AnotarAcierto(int jugadorId, int acertijoId, ResultadoIntento intento, int puntaje)
        {
            intentos.Add(new RegistroIntento
            {
                JugadorId = jugadorId,
                AcertijoId = acertijoId,
                Respuesta = intento.Respuesta,
                EsCorrecto = true,
                Segundos = intento.Segundos,
                PistasUsadas = intento.PistasUsadas,
                Puntaje = puntaje
            });
            Guardar();
        }

        // ==================================================================
        // LOGROS
        // ==================================================================

        public IReadOnlyList<LogroJugador> ObtenerLogros(int jugadorId)
        {
            var obtenidos = logrosObtenidos[jugadorId];
            var lista = new List<LogroJugador>();
            foreach (var l in logros)
            {
                DateTime fecha;
                bool ok = obtenidos.TryGetValue(l.Id, out fecha);
                lista.Add(new LogroJugador
                {
                    Logro = l,
                    Obtenido = ok,
                    Fecha = ok ? fecha : (DateTime?)null,
                    Progreso = ok ? l.Meta : Math.Min(Progreso(jugadorId, l), l.Meta)
                });
            }
            return lista;
        }

        int Progreso(int jugadorId, Logro l)
        {
            var ps = progreso[jugadorId].Values;
            var j = ObtenerPerfil(jugadorId);
            switch (l.Id)
            {
                case "primer-candado": return progreso[jugadorId][1].Estado == EstadoNivel.Completado ? 1 : 0;
                case "mente-brillante": return ps.Count(p => p.SinPistas);
                case "punteria": return ps.Count(p => p.PrimerIntento);
                case "velocista": return ps.Any(p => p.MejorTiempo > 0 && p.MejorTiempo < 30) ? 1 : 0;
                case "perfeccionista": return ps.Count(p => p.Estrellas == 3);
                case "constante": return j == null ? 0 : j.RachaDiaria;
                case "maestro-patrones":
                    return ps.Count(p => p.Estado == EstadoNivel.Completado
                                         && acertijos[ObtenerNivel(p.Numero).AcertijoId].Categoria == Categoria.Patrones);
                case "medio-camino": return NivelesCompletados(jugadorId);
                case "experto": return progreso[jugadorId][Reglas.TotalNiveles].Estado == EstadoNivel.Completado ? 1 : 0;
                default: return 0;
            }
        }

        /// <summary>Marca los logros recién cumplidos y los devuelve.</summary>
        List<Logro> EvaluarLogros(int jugadorId, DateTime fecha)
        {
            var nuevos = new List<Logro>();
            var obtenidos = logrosObtenidos[jugadorId];
            foreach (var l in logros)
            {
                if (obtenidos.ContainsKey(l.Id)) continue;
                if (Progreso(jugadorId, l) >= l.Meta)
                {
                    obtenidos[l.Id] = fecha;
                    nuevos.Add(l);
                }
            }
            return nuevos;
        }

        // ==================================================================
        // DATOS DE DEMOSTRACIÓN
        // ==================================================================

        void CargarDemo()
        {
            var ana = CrearPerfil("Ana");
            Sembrar(ana, new[] { 3, 3, 2, 3, 2, 2, 3, 3, 2, 1, 3, 2, 2, 3 }, 5, 3);

            var carlos = CrearPerfil("Carlos");
            Sembrar(carlos, new[] { 3, 2, 2, 1, 2, 3 }, 0, 2);
        }

        void Sembrar(Jugador j, int[] estrellas, int racha, int pistas)
        {
            var rnd = new Random(j.Id * 31);
            for (int i = 0; i < estrellas.Length && i < niveles.Count; i++)
            {
                var nivel = niveles[i];
                var p = progreso[j.Id][nivel.Numero];
                float proporcion = estrellas[i] == 3 ? 0.92f : estrellas[i] == 2 ? 0.72f : 0.5f;
                p.Estado = EstadoNivel.Completado;
                p.Estrellas = estrellas[i];
                p.MejorPuntaje = (int)(nivel.PuntajeBase * proporcion) / 10 * 10;
                p.MejorTiempo = 35 + rnd.Next(0, 90);
                p.SinPistas = estrellas[i] == 3;
                p.PrimerIntento = estrellas[i] >= 2 && i % 2 == 0;
                j.PuntajeTotal += p.MejorPuntaje;
            }
            if (estrellas.Length < niveles.Count)
                progreso[j.Id][estrellas.Length + 1].Estado = EstadoNivel.Desbloqueado;

            j.PistasDisponibles = pistas;
            if (racha > 0)
            {
                j.RachaDiaria = racha;
                j.UltimoDiario = DateTime.Today.AddDays(-1);
                for (int d = 1; d <= racha; d++) diasDiario[j.Id].Add(DateTime.Today.AddDays(-d));
                j.PuntajeTotal += racha * 400;
            }

            // Los logros ya cumplidos quedan con fechas pasadas
            EvaluarLogros(j.Id, DateTime.Today);
            var obtenidos = logrosObtenidos[j.Id];
            int k = 1;
            foreach (var id in obtenidos.Keys.ToList()) obtenidos[id] = DateTime.Today.AddDays(-(k++ * 2));
        }
    }
}
