using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Mindlock.Datos
{
    /// <summary>
    /// Guarda el juego en un archivo del dispositivo (R-03: conservar el progreso,
    /// R-05: sin conexión). Usa las mismas reglas que MemoriaJuegoService y, después
    /// de cada cambio, escribe las 9 tablas del diccionario de datos (BaseDatosLocal).
    ///
    /// El contenido (niveles, acertijos, pistas y logros) sale de CatalogoAcertijos,
    /// así una actualización del juego trae acertijos nuevos sin perder el progreso.
    /// Para migrar a SQLite basta con cambiar Cargar/Guardar por consultas a las
    /// mismas tablas.
    /// </summary>
    public class ArchivoJuegoService : MemoriaJuegoService
    {
        public const string NombreArchivo = "mindlock_datos.json";

        const string FormatoFecha = "yyyy-MM-dd HH:mm:ss";
        const string FormatoDia = "yyyy-MM-dd";

        readonly string ruta;
        readonly Dictionary<DateTime, int> diariosAsignados = new Dictionary<DateTime, int>();
        bool cargando;

        public string Ruta { get { return ruta; } }

        public ArchivoJuegoService(string ruta) : base(false)
        {
            this.ruta = ruta;
            Cargar();
        }

        // ==================================================================
        // CARPETA DE DATOS: en PC, .../LocalLow/Mindlock
        // ==================================================================

        /// <summary>Carpetas donde se guardó el progreso antes (empresa del proyecto), de la más nueva a la más vieja.</summary>
        public static readonly string[] EmpresasAnteriores = { "Grupo3", "Unicaribe" };

        /// <summary>
        /// En PC Unity usa .../LocalLow/{empresa}/{producto}; el juego guarda un nivel más arriba,
        /// en .../LocalLow/{producto}. En Android, iOS y web usa la carpeta de Unity tal cual.
        /// </summary>
        public static string Carpeta()
        {
            string unity = Application.persistentDataPath;
#if UNITY_STANDALONE || UNITY_EDITOR
            var empresa = Directory.GetParent(unity);
            if (empresa != null && empresa.Parent != null) return Path.Combine(empresa.Parent.FullName, Application.productName);
#endif
            return unity;
        }

        /// <summary>
        /// Si el progreso sigue en .../LocalLow/{empresa anterior}/{producto}, lo mueve a carpetaActual
        /// (.../LocalLow/{producto}) y borra la carpeta vieja.
        /// </summary>
        public static bool TraerDeCarpetaAnterior(string carpetaActual)
        {
            try
            {
                var raiz = Directory.GetParent(Path.GetFullPath(carpetaActual));
                if (raiz == null) return false;
                string producto = Path.GetFileName(Path.GetFullPath(carpetaActual));
                bool movido = false;
                foreach (var empresa in EmpresasAnteriores)
                    movido |= MoverDatos(Path.Combine(raiz.FullName, empresa, producto), carpetaActual);
                return movido;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Mindlock] No se pudo traer el progreso de la carpeta anterior: " + e.Message);
                return false;
            }
        }

        /// <summary>Mueve el archivo principal y su copia .bak. Nunca pisa un progreso que ya exista en el destino.</summary>
        public static bool MoverDatos(string origen, string destino)
        {
            string principal = Path.Combine(origen, NombreArchivo);
            if (!File.Exists(principal) && !File.Exists(principal + ".bak"))
            {
                BorrarCarpetaAnterior(origen);   // sin progreso: solo quedan registros y cachés de Unity
                return false;
            }
            if (File.Exists(Path.Combine(destino, NombreArchivo))) return false;

            Directory.CreateDirectory(destino);
            foreach (var nombre in new[] { NombreArchivo, NombreArchivo + ".bak" })
            {
                string de = Path.Combine(origen, nombre);
                if (File.Exists(de)) File.Move(de, Path.Combine(destino, nombre));
            }
            Debug.Log("[Mindlock] Progreso movido de " + origen + " a " + destino);

            BorrarCarpetaAnterior(origen);
            return true;
        }

        /// <summary>
        /// Borra la carpeta anterior (lo que queda son registros y cachés que Unity vuelve a crear)
        /// y, si queda vacía, la de la empresa.
        /// </summary>
        static void BorrarCarpetaAnterior(string carpeta)
        {
            try
            {
                if (Directory.Exists(carpeta))
                {
                    Directory.Delete(carpeta, true);
                    // En Windows el borrado puede tardar un instante en hacerse efectivo; si aun así
                    // la carpeta de la empresa no queda vacía, se borra la próxima vez que se abra el juego
                    for (int i = 0; i < 50 && Directory.Exists(carpeta); i++) System.Threading.Thread.Sleep(20);
                }
                var empresa = Directory.GetParent(carpeta);
                if (empresa != null && Directory.Exists(empresa.FullName) && !Directory.EnumerateFileSystemEntries(empresa.FullName).Any())
                    Directory.Delete(empresa.FullName);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Mindlock] No se pudo borrar la carpeta anterior " + carpeta + ": " + e.Message);
            }
        }

        // ==================================================================
        // ACERTIJO DIARIO: uno fijo por fecha (tabla acertijo_diario)
        // ==================================================================

        public override AcertijoDiario ObtenerDiario(DateTime fecha)
        {
            var dia = fecha.Date;
            int id;
            if (diariosAsignados.TryGetValue(dia, out id))
            {
                var a = ObtenerAcertijo(id);
                if (a != null)
                    return new AcertijoDiario { Fecha = dia, Acertijo = a, Nivel = CatalogoAcertijos.NivelDiario(a.Id), PuntosBonus = Reglas.BonusDiario };
            }

            var nuevo = base.ObtenerDiario(dia);
            diariosAsignados[dia] = nuevo.Acertijo.Id;
            Guardar();
            return nuevo;
        }

        // ==================================================================
        // LECTURA
        // ==================================================================

        void Cargar()
        {
            var db = Leer(ruta) ?? Leer(ruta + ".bak");
            if (db == null)
            {
                // Primera vez: se crea la base con el contenido empaquetado
                Guardar();
                return;
            }

            cargando = true;
            try { Importar(db); }
            finally { cargando = false; }
        }

        static BaseDatosLocal Leer(string archivo)
        {
            try
            {
                if (!File.Exists(archivo)) return null;
                var db = JsonUtility.FromJson<BaseDatosLocal>(File.ReadAllText(archivo));
                return db != null && db.jugador != null ? db : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Mindlock] No se pudo leer " + archivo + ": " + e.Message);
                return null;
            }
        }

        void Importar(BaseDatosLocal db)
        {
            // Jugadores (cada uno empieza con todos sus niveles en estado inicial)
            foreach (var f in db.jugador)
            {
                if (string.IsNullOrEmpty(f.nombre) || jugadores.Exists(j => j.Id == f.id_jugador)) continue;
                var j = new Jugador
                {
                    Id = f.id_jugador,
                    Nombre = f.nombre,
                    PuntajeTotal = f.puntaje_total,
                    PistasDisponibles = Mathf.Clamp(f.pistas_disponibles, 0, Reglas.MaxPistasAcumuladas),
                    RachaDiaria = Math.Max(0, f.racha_diaria),
                    UltimoDiario = LeerFecha(f.ultimo_diario),
                    FechaCreacion = LeerFecha(f.fecha_creacion) ?? DateTime.Now,
                    Vidas = f.vidas < 0 ? Economia.MaxVidas : Mathf.Clamp(f.vidas, 0, Economia.MaxVidas),
                    Monedas = Math.Max(0, f.monedas),
                    RecargaVidaDesde = LeerFecha(f.recarga_vida_desde),
                    AnunciosParaVida = Mathf.Clamp(f.anuncios_para_vida, 0, Economia.AnunciosPorVida - 1),
                    AnunciosHoy = Math.Max(0, f.anuncios_hoy),
                    FechaAnuncios = LeerFecha(f.fecha_anuncios)
                };
                jugadores.Add(j);
                InicializarProgreso(j.Id);
                if (f.racha_perdida) rachasPerdidas.Add(j.Id);
                siguienteId = Math.Max(siguienteId, j.Id + 1);
            }

            // Progreso por nivel
            foreach (var f in db.progreso)
            {
                Dictionary<int, ProgresoNivel> porNivel;
                ProgresoNivel p;
                if (!progreso.TryGetValue(f.id_jugador, out porNivel) || !porNivel.TryGetValue(f.id_nivel, out p)) continue;
                EstadoNivel estado;
                if (Enum.TryParse(f.estado, out estado)) p.Estado = estado;
                p.MejorPuntaje = f.mejor_puntaje;
                p.Estrellas = Mathf.Clamp(f.estrellas, 0, Reglas.MaxEstrellas);
                p.FechaCompletado = LeerFecha(f.fecha_completado);
                p.MejorTiempo = f.mejor_tiempo;
                p.SinPistas = f.sin_pistas;
                p.PrimerIntento = f.primer_intento;
            }

            // Logros obtenidos (id_logro -> clave usando la tabla logro guardada)
            var claves = new Dictionary<int, string>();
            foreach (var f in db.logro) if (!string.IsNullOrEmpty(f.clave)) claves[f.id_logro] = f.clave;
            foreach (var f in db.jugador_logro)
            {
                Dictionary<string, DateTime> obtenidos;
                string clave;
                if (!logrosObtenidos.TryGetValue(f.id_jugador, out obtenidos)) continue;
                if (!claves.TryGetValue(f.id_logro, out clave) && f.id_logro >= 1 && f.id_logro <= logros.Count) clave = logros[f.id_logro - 1].Id;
                if (clave == null || !logros.Exists(l => l.Id == clave)) continue;
                obtenidos[clave] = LeerFecha(f.fecha_obtencion) ?? DateTime.Now;
            }

            // Historial de intentos; los aciertos de acertijos diarios marcan los días de la racha
            var idsDiarios = new HashSet<int>(diarios.Select(d => d.Id));
            foreach (var f in db.intento)
            {
                if (!progreso.ContainsKey(f.id_jugador)) continue;
                var reg = new RegistroIntento
                {
                    JugadorId = f.id_jugador,
                    AcertijoId = f.id_acertijo,
                    Respuesta = f.respuesta_dada,
                    EsCorrecto = f.es_correcto,
                    Segundos = f.tiempo_seg,
                    PistasUsadas = f.pistas_usadas,
                    Puntaje = f.puntaje_obtenido,
                    Fecha = LeerFecha(f.fecha_hora) ?? DateTime.Now
                };
                intentos.Add(reg);
                if (reg.EsCorrecto && idsDiarios.Contains(reg.AcertijoId)) diasDiario[reg.JugadorId].Add(reg.Fecha.Date);
            }

            foreach (var f in db.acertijo_diario)
            {
                var dia = LeerFecha(f.fecha);
                if (dia.HasValue && acertijos.ContainsKey(f.id_acertijo)) diariosAsignados[dia.Value.Date] = f.id_acertijo;
            }
        }

        // ==================================================================
        // ESCRITURA
        // ==================================================================

        protected override void Guardar()
        {
            if (cargando || string.IsNullOrEmpty(ruta)) return;
            try
            {
                string json = JsonUtility.ToJson(Exportar(), true);
                string carpeta = Path.GetDirectoryName(ruta);
                if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);

                // Escribir en un temporal y luego reemplazar: si el juego se cierra
                // a mitad de la escritura, queda la copia .bak
                string temporal = ruta + ".tmp";
                File.WriteAllText(temporal, json);
                if (File.Exists(ruta)) File.Copy(ruta, ruta + ".bak", true);
                if (File.Exists(ruta)) File.Delete(ruta);
                File.Move(temporal, ruta);
            }
            catch (Exception e)
            {
                Debug.LogError("[Mindlock] No se pudo guardar el progreso: " + e.Message);
            }
        }

        BaseDatosLocal Exportar()
        {
            var db = new BaseDatosLocal { guardado = EscribirFecha(DateTime.Now) };

            // --- Contenido empaquetado ---
            foreach (var n in niveles)
            {
                db.nivel.Add(new TablaNivel
                {
                    id_nivel = n.Numero,
                    numero = n.Numero,
                    dificultad = n.Dificultad.ToString(),
                    puntaje_base = n.PuntajeBase,
                    tiempo_limite_seg = n.TiempoLimite
                });
            }

            int idPista = 1;
            foreach (var a in acertijos.Values.OrderBy(x => x.Id))
            {
                var nivel = niveles.Find(n => n.AcertijoId == a.Id);
                db.acertijo.Add(new TablaAcertijo
                {
                    id_acertijo = a.Id,
                    id_nivel = nivel != null ? nivel.Numero : 0,
                    categoria = a.Categoria.ToString(),
                    enunciado = a.Enunciado,
                    tipo_respuesta = a.Tipo.ToString(),
                    respuesta_correcta = a.Respuesta,
                    es_diario = nivel == null
                });
                foreach (var p in a.Pistas)
                    db.pista.Add(new TablaPista { id_pista = idPista++, id_acertijo = a.Id, orden = p.Orden, texto = p.Texto, penalizacion = p.Penalizacion });
            }

            for (int i = 0; i < logros.Count; i++)
            {
                var l = logros[i];
                db.logro.Add(new TablaLogro
                {
                    id_logro = i + 1,
                    clave = l.Id,
                    nombre = l.Nombre,
                    descripcion = l.Descripcion,
                    condicion = l.Id + " >= " + l.Meta,
                    icono = l.Icono
                });
            }

            // --- Datos del jugador ---
            int idProgreso = 1;
            foreach (var j in jugadores)
            {
                db.jugador.Add(new TablaJugador
                {
                    id_jugador = j.Id,
                    nombre = j.Nombre,
                    fecha_creacion = EscribirFecha(j.FechaCreacion),
                    puntaje_total = j.PuntajeTotal,
                    pistas_disponibles = j.PistasDisponibles,
                    racha_diaria = j.RachaDiaria,
                    ultimo_diario = EscribirFecha(j.UltimoDiario),
                    racha_perdida = rachasPerdidas.Contains(j.Id),
                    vidas = j.Vidas,
                    monedas = j.Monedas,
                    recarga_vida_desde = EscribirFecha(j.RecargaVidaDesde),
                    anuncios_para_vida = j.AnunciosParaVida,
                    anuncios_hoy = j.AnunciosHoy,
                    fecha_anuncios = EscribirFecha(j.FechaAnuncios)
                });

                foreach (var p in progreso[j.Id].Values.OrderBy(x => x.Numero))
                {
                    db.progreso.Add(new TablaProgreso
                    {
                        id_progreso = idProgreso++,
                        id_jugador = j.Id,
                        id_nivel = p.Numero,
                        estado = p.Estado.ToString(),
                        mejor_puntaje = p.MejorPuntaje,
                        estrellas = p.Estrellas,
                        fecha_completado = EscribirFecha(p.FechaCompletado),
                        mejor_tiempo = p.MejorTiempo,
                        sin_pistas = p.SinPistas,
                        primer_intento = p.PrimerIntento
                    });
                }

                foreach (var kv in logrosObtenidos[j.Id])
                {
                    int indice = logros.FindIndex(l => l.Id == kv.Key);
                    if (indice >= 0)
                        db.jugador_logro.Add(new TablaJugadorLogro { id_jugador = j.Id, id_logro = indice + 1, fecha_obtencion = EscribirFecha(kv.Value) });
                }
            }

            for (int i = 0; i < intentos.Count; i++)
            {
                var r = intentos[i];
                db.intento.Add(new TablaIntento
                {
                    id_intento = i + 1,
                    id_jugador = r.JugadorId,
                    id_acertijo = r.AcertijoId,
                    respuesta_dada = r.Respuesta ?? string.Empty,
                    es_correcto = r.EsCorrecto,
                    tiempo_seg = r.Segundos,
                    pistas_usadas = r.PistasUsadas,
                    puntaje_obtenido = r.Puntaje,
                    fecha_hora = EscribirFecha(r.Fecha)
                });
            }

            int idDiario = 1;
            foreach (var kv in diariosAsignados.OrderBy(x => x.Key))
                db.acertijo_diario.Add(new TablaAcertijoDiario { id_diario = idDiario++, id_acertijo = kv.Value, fecha = kv.Key.ToString(FormatoDia, CultureInfo.InvariantCulture), puntaje_bonus = Reglas.BonusDiario });

            return db;
        }

        // ==================================================================
        // FECHAS EN ISO 8601
        // ==================================================================

        static string EscribirFecha(DateTime? f)
        {
            return f.HasValue ? f.Value.ToString(FormatoFecha, CultureInfo.InvariantCulture) : string.Empty;
        }

        static DateTime? LeerFecha(string texto)
        {
            DateTime f;
            if (!string.IsNullOrEmpty(texto) &&
                DateTime.TryParseExact(texto, new[] { FormatoFecha, FormatoDia }, CultureInfo.InvariantCulture, DateTimeStyles.None, out f))
                return f;
            return null;
        }
    }
}
