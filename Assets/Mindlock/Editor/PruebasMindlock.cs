using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mindlock.Datos;
using Mindlock.Logica;
using UnityEditor;
using UnityEngine;

namespace Mindlock.Herramientas
{
    /// <summary>
    /// Pruebas rápidas de la capa de datos y la lógica (sin entrar en Play).
    /// Menú: Mindlock > Probar lógica y guardado. El resultado sale en la consola.
    /// </summary>
    public static class PruebasMindlock
    {
        static readonly List<string> fallos = new List<string>();

        [MenuItem("Mindlock/Probar lógica y guardado", priority = 40)]
        public static void Ejecutar()
        {
            fallos.Clear();
            string carpeta = Path.Combine(Path.GetTempPath(), "mindlock_prueba_" + Guid.NewGuid().ToString("N"));
            string ruta = Path.Combine(carpeta, ArchivoJuegoService.NombreArchivo);
            bool modo = CatalogoAcertijos.ModoDesafio;
            try
            {
                CatalogoAcertijos.ModoDesafio = false;
                ProbarRespuestas();
                CatalogoAcertijos.ModoDesafio = true;
                ProbarRespuestas();
                ProbarDesafio();
                CatalogoAcertijos.ModoDesafio = false;
                ProbarNormal();
                CatalogoAcertijos.ModoDesafio = modo;
                ProbarPuntaje();
                ProbarGuardado(ruta);
                ProbarPistaPorAnuncio(Path.Combine(carpeta, "pistas", ArchivoJuegoService.NombreArchivo));
                ProbarEconomia(Path.Combine(carpeta, "economia", ArchivoJuegoService.NombreArchivo));
                ProbarCarpetaAnterior(Path.Combine(carpeta, "LocalLow"));
                ProbarAnuncios();
                ProbarIngles();
                ProbarTextosIngles();
            }
            catch (Exception e)
            {
                fallos.Add("Excepción: " + e);
            }
            finally
            {
                Anuncios.Configurar(null);
                CatalogoAcertijos.ModoDesafio = modo;
                if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            }

            if (fallos.Count == 0) Debug.Log("[Mindlock] PRUEBAS OK: todas las comprobaciones pasaron.");
            else Debug.LogError("[Mindlock] PRUEBAS FALLIDAS (" + fallos.Count + "):\n" + string.Join("\n", fallos));

            if (Application.isBatchMode) EditorApplication.Exit(fallos.Count == 0 ? 0 : 1);
        }

        static void Comprobar(bool condicion, string descripcion)
        {
            if (!condicion) fallos.Add(descripcion);
        }

        /// <summary>Cada acertijo acepta su propia respuesta y tiene sus opciones/fichas coherentes.</summary>
        static void ProbarRespuestas()
        {
            var todos = new List<Acertijo>(CatalogoAcertijos.AcertijosDeNiveles());
            todos.AddRange(CatalogoAcertijos.AcertijosDiarios());
            Comprobar(CatalogoAcertijos.AcertijosDeNiveles().Count == Reglas.TotalNiveles, "Debe haber 30 acertijos de nivel");

            var ids = new HashSet<int>();
            foreach (var a in todos)
            {
                Comprobar(ids.Add(a.Id), "Id repetido: " + a.Id);
                Comprobar(a.Pistas.Count == Reglas.MaxPistasPorAcertijo, "Acertijo " + a.Id + " debe tener 3 pistas");
                Comprobar(!a.EsCorrecta("respuesta claramente incorrecta"), "Acertijo " + a.Id + " acepta cualquier cosa");

                switch (a.Tipo)
                {
                    case TipoRespuesta.Texto:
                        foreach (var r in a.Respuesta.Split(';'))
                            Comprobar(a.EsCorrecta(r) && a.EsCorrecta(r.ToUpperInvariant()), "Acertijo " + a.Id + " no acepta '" + r + "'");
                        break;
                    case TipoRespuesta.OpcionMultiple:
                        Comprobar(a.Opciones.Contains(a.Respuesta), "Acertijo " + a.Id + ": la respuesta no está entre las opciones");
                        Comprobar(a.EsCorrecta(a.Respuesta), "Acertijo " + a.Id + " no acepta su opción correcta");
                        break;
                    case TipoRespuesta.Secuencia:
                        var fichas = new List<string>(a.Opciones);
                        foreach (var parte in a.Respuesta.Split('|'))
                            Comprobar(fichas.Remove(parte), "Acertijo " + a.Id + ": falta la ficha '" + parte + "'");
                        Comprobar(a.EsCorrecta(a.Respuesta), "Acertijo " + a.Id + " no acepta su secuencia");
                        break;
                }
            }

            var reloj = todos.Find(x => x.Id == 2);
            Comprobar(reloj.EsCorrecta("El Reloj"), "Debe ignorar mayúsculas y artículos");
        }

        static void ProbarDesafio()
        {
            var niveles = CatalogoAcertijos.AcertijosDeNiveles();
            var originales = CatalogoAcertijos.AcertijosOriginales();
            for (int i = 0; i < niveles.Count; i++)
            {
                bool cambia = i + 1 >= CatalogoAcertijos.DesafioDesdeNivel;
                Comprobar(cambia == (niveles[i].Id != originales[i].Id), "Nivel " + (i + 1) + ": el modo desafío debe cambiar solo del nivel 5 en adelante");
            }

            var a = niveles[29];
            CatalogoAcertijos.AsignarPenalizaciones(a, Dificultad.Experto);
            Comprobar(a.Pistas[2].Penalizacion == 650, "En modo desafío las pistas de Experto restan más");
            CatalogoAcertijos.ModoDesafio = false;
            CatalogoAcertijos.AsignarPenalizaciones(a, Dificultad.Experto);
            Comprobar(a.Pistas[2].Penalizacion == 300, "En modo original las pistas de Experto restan lo de siempre");
            CatalogoAcertijos.ModoDesafio = true;
        }

        /// <summary>Niveles 1-15 originales; del 16 al 30 difíciles, sin repetir acertijos.</summary>
        static void ProbarNormal()
        {
            var niveles = CatalogoAcertijos.AcertijosDeNiveles();
            var originales = CatalogoAcertijos.AcertijosOriginales();
            for (int i = 0; i < CatalogoAcertijos.DificilesDesdeNivel - 1; i++)
                Comprobar(niveles[i].Id == originales[i].Id, "Nivel " + (i + 1) + ": debe ser el acertijo original");
            for (int i = CatalogoAcertijos.DificilesDesdeNivel - 1; i < niveles.Count; i++)
                Comprobar(niveles[i].Id == CatalogoAcertijos.IdsNivelesDificiles[i - CatalogoAcertijos.DificilesDesdeNivel + 1], "Nivel " + (i + 1) + ": no es el acertijo difícil esperado");
            Comprobar(CatalogoAcertijos.IdsNivelesDificiles.Length == Reglas.TotalNiveles - CatalogoAcertijos.DificilesDesdeNivel + 1, "Debe haber un acertijo difícil por cada nivel del 16 al 30");
            Comprobar(CatalogoAcertijos.DificultadDe(15) == Dificultad.Media && CatalogoAcertijos.DificultadDe(16) == Dificultad.Dificil, "Desde el nivel 16 la dificultad es Difícil");
            Comprobar(CatalogoAcertijos.DificultadDe(25) == Dificultad.Experto, "Del 25 al 30 la dificultad es Experto");

            var a = niveles[29];
            CatalogoAcertijos.AsignarPenalizaciones(a, Dificultad.Experto);
            Comprobar(a.Pistas[2].Penalizacion == 300, "En la configuración normal las pistas restan lo de siempre");
        }

        static void ProbarPuntaje()
        {
            var nivel = CatalogoAcertijos.CrearNivel(1, 1);
            var perfecto = CalculadoraPuntaje.Calcular(nivel, new ResultadoIntento { NumeroNivel = 1, Segundos = 10, Intentos = 1 });
            Comprobar(perfecto.Puntaje == nivel.PuntajeBase && perfecto.Estrellas == 3, "Respuesta rápida al primer intento = base y 3 estrellas");

            var conPistas = CalculadoraPuntaje.Calcular(nivel, new ResultadoIntento { NumeroNivel = 1, Segundos = 10, Intentos = 3, PenalizacionPistas = 225 });
            Comprobar(conPistas.Puntaje == 500 - 100 - 225 && conPistas.Estrellas == 1, "Descuentos de intentos y pistas");

            var minimo = CalculadoraPuntaje.Calcular(nivel, new ResultadoIntento { NumeroNivel = 1, Segundos = 999, Intentos = 99, PenalizacionPistas = 999 });
            Comprobar(minimo.Puntaje == 50, "El puntaje nunca baja del 10 % de la base");
        }

        static void ProbarGuardado(string ruta)
        {
            var db = new ArchivoJuegoService(ruta);
            Comprobar(File.Exists(ruta), "Al iniciar se crea la base local con el contenido");
            Comprobar(db.ObtenerPerfiles().Count == 0, "La base nueva no tiene perfiles");

            var ana = db.CrearPerfil("Ana");
            bool repetido = db.ExisteNombre("ana");
            Comprobar(repetido, "Los nombres no se repiten (sin distinguir mayúsculas)");
            try { db.CrearPerfil("   "); fallos.Add("Debe rechazar un nombre vacío"); } catch (ArgumentException) { }

            // Nivel 1: un fallo, una pista y luego acierto
            db.RegistrarFallo(ana.Id, 1, "9", 5, 0);
            Comprobar(db.UsarPista(ana.Id), "Puede usar una pista");
            var r1 = db.RegistrarNivel(ana.Id, new ResultadoIntento { NumeroNivel = 1, AcertijoId = 1, Respuesta = "10", Segundos = 20, Intentos = 2, PistasUsadas = 1, PenalizacionPistas = 50 });
            Comprobar(r1.NivelDesbloqueado == 2, "Completar el nivel 1 desbloquea el 2");
            Comprobar(r1.LogrosNuevos.Exists(l => l.Id == "primer-candado"), "Logro 'Primer candado'");

            // Nivel 2 perfecto: 3 estrellas dan una pista extra
            var r2 = db.RegistrarNivel(ana.Id, new ResultadoIntento { NumeroNivel = 2, AcertijoId = 2, Respuesta = "reloj", Segundos = 15, Intentos = 1 });
            Comprobar(r2.Estrellas == 3 && r2.PistaGanada, "3 estrellas regalan una pista");

            var diario = db.ObtenerDiario(DateTime.Today);
            var rd = db.RegistrarDiario(ana.Id, new ResultadoIntento { AcertijoId = diario.Acertijo.Id, Respuesta = diario.Acertijo.Respuesta, Segundos = 30, Intentos = 1 });
            Comprobar(rd.Racha == 1 && rd.BonusDiario == Reglas.BonusDiario, "El diario empieza la racha y da bonus");

            int puntaje = ana.PuntajeTotal, pistas = ana.PistasDisponibles;
            db.CrearPerfil("Carlos");

            // Volver a abrir el juego: todo debe seguir ahí
            var otra = new ArchivoJuegoService(ruta);
            var anaGuardada = otra.ObtenerPerfil(ana.Id);
            Comprobar(otra.ObtenerPerfiles().Count == 2, "Se conservan los 2 perfiles");
            Comprobar(anaGuardada != null && anaGuardada.PuntajeTotal == puntaje, "Se conserva el puntaje total");
            Comprobar(anaGuardada != null && anaGuardada.PistasDisponibles == pistas, "Se conservan las pistas");
            Comprobar(otra.NivelesCompletados(ana.Id) == 2, "Se conservan los niveles completados");
            Comprobar(otra.ObtenerProgreso(ana.Id, 3).Estado == EstadoNivel.Desbloqueado, "El nivel 3 sigue desbloqueado");
            Comprobar(otra.ObtenerProgreso(ana.Id, 2).Estrellas == 3, "Se conservan las estrellas");
            Comprobar(otra.ResueltoHoy(ana.Id) && otra.RachaActual(ana.Id) == 1, "Se conserva el diario de hoy y la racha");
            Comprobar(otra.ObtenerDiario(DateTime.Today).Acertijo.Id == diario.Acertijo.Id, "El acertijo diario de la fecha no cambia");
            Comprobar(otra.ObtenerIntentos(ana.Id).Count == 4, "Se guardan los 4 intentos (1 fallo + 3 aciertos)");
            Comprobar(otra.ObtenerLogros(ana.Id).Any(l => l.Logro.Id == "primer-candado" && l.Obtenido), "Se conservan los logros");
            Comprobar(otra.CrearPerfil("Luis").Id == 3, "Los ids siguen la secuencia");

            otra.ReiniciarProgreso(ana.Id);
            var tercera = new ArchivoJuegoService(ruta);
            Comprobar(tercera.NivelesCompletados(ana.Id) == 0 && tercera.ObtenerPerfil(ana.Id).PuntajeTotal == 0, "Reiniciar borra el progreso guardado");
            Comprobar(tercera.ObtenerIntentos(ana.Id).Count == 0, "Reiniciar borra los intentos");
            Comprobar(tercera.NivelesCompletados(2) == 0 && tercera.ObtenerPerfiles().Count == 3, "Reiniciar no toca a los demás");

            // Archivo dañado: usa la copia de respaldo
            File.WriteAllText(ruta, "{ dañado");
            var cuarta = new ArchivoJuegoService(ruta);
            Comprobar(cuarta.ObtenerPerfiles().Count == 3, "Con el archivo dañado se recupera la copia .bak");
        }

        static void ProbarPistaPorAnuncio(string ruta)
        {
            var db = new ArchivoJuegoService(ruta);
            var ana = db.CrearPerfil("Ana");
            Comprobar(db.GanarPista(ana.Id) && ana.PistasDisponibles == Reglas.PistasIniciales + 1, "El anuncio con recompensa suma una pista");
            while (db.GanarPista(ana.Id)) { }
            Comprobar(ana.PistasDisponibles == Reglas.MaxPistasAcumuladas, "Las pistas por anuncio no pasan del máximo");
            Comprobar(new ArchivoJuegoService(ruta).ObtenerPerfil(ana.Id).PistasDisponibles == Reglas.MaxPistasAcumuladas, "Las pistas ganadas se guardan");
        }

        /// <summary>Vidas que se recargan, monedas, compras y límite diario de anuncios con premio.</summary>
        static void ProbarEconomia(string ruta)
        {
            var db = new ArchivoJuegoService(ruta);
            var ana = db.CrearPerfil("Ana");
            var t0 = new DateTime(2026, 11, 3, 10, 0, 0);
            Comprobar(db.Vidas(ana.Id, t0) == Economia.MaxVidas, "Un perfil nuevo empieza con " + Economia.MaxVidas + " vidas");

            Comprobar(db.PerderVida(ana.Id, t0) == 3, "Una respuesta incorrecta resta una vida");
            db.PerderVida(ana.Id, t0.AddMinutes(10));
            Comprobar(db.Vidas(ana.Id, t0.AddMinutes(49)) == 2, "Antes de " + Economia.MinutosPorVida + " minutos no se recupera ninguna");
            Comprobar(db.Vidas(ana.Id, t0.AddMinutes(50)) == 3, "A los " + Economia.MinutosPorVida + " minutos se recupera una vida");
            Comprobar(db.TiempoParaVida(ana.Id, t0.AddMinutes(60)) == TimeSpan.FromMinutes(40), "La siguiente vida cuenta desde la anterior");
            Comprobar(db.Vidas(ana.Id, t0.AddHours(5)) == Economia.MaxVidas && ana.RecargaVidaDesde == null, "Nunca pasa del máximo y deja de contar");

            for (int i = 0; i < Economia.MaxVidas; i++) db.PerderVida(ana.Id, t0.AddHours(6));
            Comprobar(db.Vidas(ana.Id, t0.AddHours(6)) == 0 && db.PerderVida(ana.Id, t0.AddHours(6)) == 0, "Con 0 vidas no baja de 0");
            Comprobar(db.Vidas(ana.Id, t0.AddHours(5)) == 0 && db.Vidas(ana.Id, t0.AddHours(5).AddMinutes(50)) == 1, "Si el reloj se atrasa, la recarga empieza de nuevo");

            var t1 = t0.AddDays(1);
            db.PerderVida(ana.Id, t1);
            int antes = db.Vidas(ana.Id, t1);
            Comprobar(db.Recompensar(ana.Id, Recompensa.Vida, 0, t1) && db.Vidas(ana.Id, t1) == antes, "Con un anuncio todavía no se gana la vida");
            Comprobar(db.Recompensar(ana.Id, Recompensa.Vida, 0, t1) && db.Vidas(ana.Id, t1) == antes + 1, "Con " + Economia.AnunciosPorVida + " anuncios se gana una vida");

            Comprobar(!db.LlenarVidasConMonedas(ana.Id), "Sin monedas no se pueden llenar las vidas");
            Comprobar(db.Recompensar(ana.Id, Recompensa.Monedas, 0, t1) && ana.Monedas == Economia.MonedasPorAnuncio, "Un anuncio da " + Economia.MonedasPorAnuncio + " monedas");
            ana.Monedas = Economia.PrecioLlenarVidas + Economia.PrecioPista;
            db.PerderVida(ana.Id);
            Comprobar(db.LlenarVidasConMonedas(ana.Id) && ana.Vidas == Economia.MaxVidas && ana.Monedas == Economia.PrecioPista, "Llenar las vidas cuesta " + Economia.PrecioLlenarVidas + " monedas");
            int pistas = ana.PistasDisponibles;
            Comprobar(db.ComprarPista(ana.Id) && ana.PistasDisponibles == pistas + 1 && ana.Monedas == 0, "Una pista cuesta " + Economia.PrecioPista + " monedas");
            Comprobar(!db.ComprarPista(ana.Id), "Sin monedas no se compran pistas");

            int vistos = Economia.AnunciosRecompensaPorDia - db.AnunciosRestantesHoy(ana.Id, t1);
            for (int i = vistos; i < Economia.AnunciosRecompensaPorDia; i++) db.Recompensar(ana.Id, Recompensa.Monedas, 0, t1);
            Comprobar(db.AnunciosRestantesHoy(ana.Id, t1) == 0 && !db.Recompensar(ana.Id, Recompensa.Monedas, 0, t1), "Máximo " + Economia.AnunciosRecompensaPorDia + " anuncios con premio al día");
            Comprobar(db.AnunciosRestantesHoy(ana.Id, t1.AddDays(1)) == Economia.AnunciosRecompensaPorDia, "Al día siguiente se reinicia el límite");

            // Monedas por jugar
            ana.Monedas = 0;
            var r1 = db.RegistrarNivel(ana.Id, new ResultadoIntento { NumeroNivel = 1, AcertijoId = 1, Respuesta = "10", Segundos = 10, Intentos = 1 });
            Comprobar(r1.MonedasGanadas == 3 * Economia.MonedasPorEstrella + r1.LogrosNuevos.Count * Economia.MonedasPorLogro && ana.Monedas == r1.MonedasGanadas,
                      "Completar un nivel da monedas por estrella y por logro");
            var r2 = db.RegistrarNivel(ana.Id, new ResultadoIntento { NumeroNivel = 1, AcertijoId = 1, Respuesta = "10", Segundos = 10, Intentos = 1 });
            Comprobar(r2.MonedasGanadas == 0, "Repetir un nivel con las mismas estrellas no da monedas");
            int monedas = ana.Monedas;
            var diario = db.ObtenerDiario(DateTime.Today);
            var rd = db.RegistrarDiario(ana.Id, new ResultadoIntento { AcertijoId = diario.Acertijo.Id, Respuesta = diario.Acertijo.Respuesta, Segundos = 20, Intentos = 1 });
            Comprobar(rd.MonedasGanadas >= Economia.MonedasDiario && ana.Monedas == monedas + rd.MonedasGanadas, "El acertijo diario da " + Economia.MonedasDiario + " monedas");
            Comprobar(db.Recompensar(ana.Id, Recompensa.DuplicarMonedas, rd.MonedasGanadas, t1.AddDays(2)) && ana.Monedas == monedas + 2 * rd.MonedasGanadas, "Duplicar suma otra vez las monedas ganadas");

            // Se guarda todo
            db.PerderVida(ana.Id);
            var otra = new ArchivoJuegoService(ruta).ObtenerPerfil(ana.Id);
            Comprobar(otra.Vidas == ana.Vidas && otra.Monedas == ana.Monedas && otra.RecargaVidaDesde.HasValue && otra.AnunciosHoy == ana.AnunciosHoy,
                      "Se guardan las vidas, las monedas, la recarga y los anuncios del día");

            // Un archivo de antes de las vidas empieza con todas
            string json = File.ReadAllText(ruta);
            File.WriteAllText(ruta, System.Text.RegularExpressions.Regex.Replace(json, "\"vidas\":\\s*-?\\d+,?", ""));
            Comprobar(new ArchivoJuegoService(ruta).ObtenerPerfil(ana.Id).Vidas == Economia.MaxVidas, "Un archivo viejo sin vidas empieza con el máximo");
        }

        /// <summary>El progreso guardado en .../{empresa anterior}/Mindlock pasa a .../LocalLow/Mindlock.</summary>
        static void ProbarCarpetaAnterior(string localLow)
        {
            string nueva = Path.Combine(localLow, "Mindlock");
            string grupo3 = Path.Combine(localLow, "Grupo3", "Mindlock");
            string unicaribe = Path.Combine(localLow, "Unicaribe", "Mindlock");
            Directory.CreateDirectory(Path.Combine(grupo3, "Unity"));
            File.WriteAllText(Path.Combine(grupo3, ArchivoJuegoService.NombreArchivo), "progreso");
            File.WriteAllText(Path.Combine(grupo3, ArchivoJuegoService.NombreArchivo + ".bak"), "respaldo");
            File.WriteAllText(Path.Combine(grupo3, "Player.log"), "registro");
            Directory.CreateDirectory(Path.Combine(unicaribe, "Unity"));

            Comprobar(ArchivoJuegoService.TraerDeCarpetaAnterior(nueva), "Encuentra el progreso en la carpeta anterior");
            Comprobar(File.ReadAllText(Path.Combine(nueva, ArchivoJuegoService.NombreArchivo)) == "progreso"
                      && File.Exists(Path.Combine(nueva, ArchivoJuegoService.NombreArchivo + ".bak")), "Mueve el archivo y su copia .bak a LocalLow/Mindlock");
            Comprobar(!Directory.Exists(Path.Combine(localLow, "Grupo3")), "Borra la carpeta Grupo3");
            Comprobar(!Directory.Exists(Path.Combine(localLow, "Unicaribe")), "Borra la carpeta Unicaribe sin progreso");
            Comprobar(!ArchivoJuegoService.TraerDeCarpetaAnterior(nueva), "La segunda vez no hace nada");

            // Si ya hay progreso en la carpeta nueva, no se pisa ni se borra el viejo
            Directory.CreateDirectory(unicaribe);
            File.WriteAllText(Path.Combine(unicaribe, ArchivoJuegoService.NombreArchivo), "viejo");
            Comprobar(!ArchivoJuegoService.TraerDeCarpetaAnterior(nueva)
                      && File.ReadAllText(Path.Combine(nueva, ArchivoJuegoService.NombreArchivo)) == "progreso"
                      && File.Exists(Path.Combine(unicaribe, ArchivoJuegoService.NombreArchivo)), "No reemplaza un progreso que ya existe");
        }

        /// <summary>Cada acertijo y logro tiene su versión en inglés, y su respuesta en inglés es válida.</summary>
        static void ProbarIngles()
        {
            int idioma = Idioma.Actual;
            bool modo = CatalogoAcertijos.ModoDesafio;
            try
            {
                Idioma.Usar(Idioma.Ingles);
                var todos = new List<Acertijo>(CatalogoAcertijos.AcertijosOriginales());
                CatalogoAcertijos.ModoDesafio = true;
                todos.AddRange(CatalogoAcertijos.AcertijosDeNiveles().Where(a => a.Id >= 200));
                todos.AddRange(CatalogoAcertijos.AcertijosDiarios());
                foreach (var a in todos)
                {
                    string n = "Acertijo " + a.Id + " (inglés)";
                    if (a.En == null || string.IsNullOrEmpty(a.En.Enunciado)) { fallos.Add(n + ": falta el enunciado"); continue; }
                    Comprobar(a.EnunciadoVisible == a.En.Enunciado, n + ": no se muestra en inglés");
                    Comprobar(a.Pistas.All(p => !string.IsNullOrEmpty(p.TextoEn) && p.TextoVisible == p.TextoEn), n + ": faltan pistas");
                    string respuesta = a.En.Respuesta ?? a.Respuesta;
                    var opciones = a.OpcionesVisibles;
                    switch (a.Tipo)
                    {
                        case TipoRespuesta.Texto:
                            foreach (var r in respuesta.Split(';'))
                                Comprobar(a.EsCorrecta(r), n + ": no acepta '" + r + "'");
                            break;
                        case TipoRespuesta.OpcionMultiple:
                            Comprobar(opciones.Contains(respuesta) && a.EsCorrecta(respuesta), n + ": la respuesta no está entre las opciones");
                            Comprobar(opciones.Count == a.Opciones.Count, n + ": no tiene las mismas opciones");
                            break;
                        case TipoRespuesta.Secuencia:
                            var fichas = new List<string>(opciones);
                            var partes = respuesta.Split('|');
                            foreach (var parte in partes)
                                Comprobar(fichas.Remove(parte), n + ": falta la ficha '" + parte + "'");
                            Comprobar(partes.Length == a.Casillas && a.EsCorrecta(respuesta), n + ": la secuencia no coincide");
                            break;
                    }
                    Comprobar(!a.EsCorrecta("clearly wrong answer"), n + " acepta cualquier cosa");
                }
                Comprobar(todos.First(a => a.Id == 2).EsCorrecta("The clock") && todos.First(a => a.Id == 2).EsCorrecta("reloj"),
                          "Acepta la respuesta en los dos idiomas y quita el artículo inglés");
                Comprobar(CatalogoAcertijos.Logros().All(l => !string.IsNullOrEmpty(l.NombreEn) && !string.IsNullOrEmpty(l.DescripcionEn)), "Todos los logros tienen nombre en inglés");
                Idioma.Usar(Idioma.Espanol);
                Comprobar(todos[0].EnunciadoVisible == todos[0].Enunciado, "En español se muestra el enunciado original");
            }
            finally
            {
                Idioma.Usar(idioma);
                CatalogoAcertijos.ModoDesafio = modo;
            }
        }

        /// <summary>Cada texto fijo de las pantallas y cada Idioma.T / Idioma.F del código tiene traducción al inglés.</summary>
        static void ProbarTextosIngles()
        {
            var ignorar = new HashSet<string> { "MINDL", "CK", "English", "Español", "Aurora", "Normal", "Total", "Frailes City Studios", "frailescitystudios@gmail.com", "© 2026 Frailes City Studios" };
            var letras = new Regex("[A-Za-zÁÉÍÓÚáéíóúñÑ]{2,}");
            foreach (var f in Directory.GetFiles("Assets/Mindlock/Resources/Mindlock/Pantallas", "*.uxml"))
                foreach (Match m in Regex.Matches(File.ReadAllText(f), "<ui:\\w+([^>]*?)text=\"([^\"]*)\""))
                {
                    string texto = m.Groups[2].Value.Replace("&quot;", "\"");
                    if (m.Groups[1].Value.Contains("name=\"") || !letras.IsMatch(texto) || ignorar.Contains(texto)) continue;
                    Comprobar(TextosIngles.Textos.ContainsKey(texto), "Falta en TextosIngles (" + Path.GetFileName(f) + "): " + texto);
                }
            foreach (var f in Directory.GetFiles("Assets/Mindlock/Scripts", "*.cs", SearchOption.AllDirectories))
                foreach (Match m in Regex.Matches(File.ReadAllText(f), "Idioma\\.[TF]\\(\\s*\"((?:[^\"\\\\]|\\\\.)*)\""))
                    Comprobar(TextosIngles.Textos.ContainsKey(m.Groups[1].Value.Replace("\\\"", "\"")), "Falta en TextosIngles (" + Path.GetFileName(f) + "): " + m.Groups[1].Value);
        }

        class AnunciosDePrueba : IProveedorAnuncios
        {
            public int Intersticiales, Recompensas;
            public void MostrarBanner(bool visible) { }
            public void MostrarIntersticial(Action alCerrar) { Intersticiales++; alCerrar(); }
            public void MostrarRecompensa(string premio, Action<bool> alTerminar) { Recompensas++; alTerminar(true); }
            public bool RecompensaLista { get { return true; } }
        }

        static void ProbarAnuncios()
        {
            var prueba = new AnunciosDePrueba();
            Anuncios.Configurar(prueba);
            int seguidos = 0;
            Action siguiente = () => seguidos++;

            for (int nivel = 1; nivel <= 2; nivel++) { Anuncios.RegistrarNivelCompletado(); Anuncios.DespuesDeNivel(nivel, siguiente); }
            Comprobar(prueba.Intersticiales == 0, "Sin anuncio en los primeros niveles");
            Anuncios.RegistrarNivelCompletado();
            Anuncios.DespuesDeNivel(3, siguiente);
            Comprobar(prueba.Intersticiales == 1, "Anuncio después de " + Anuncios.IntersticialCadaNiveles + " niveles");
            for (int nivel = 4; nivel <= 6; nivel++) { Anuncios.RegistrarNivelCompletado(); Anuncios.DespuesDeNivel(nivel, siguiente); }
            Comprobar(prueba.Intersticiales == 1, "Entre dos anuncios pasan al menos " + Anuncios.SegundosEntreIntersticiales + " s");
            Comprobar(Anuncios.TocaIntersticial(6, Time.realtimeSinceStartup + Anuncios.SegundosEntreIntersticiales + 1f), "Pasado ese tiempo vuelve a tocar");
            Comprobar(seguidos == 6, "El juego siempre continúa después del anuncio");

            bool pista = false;
            Anuncios.MostrarRecompensa("1 pista", vio => pista = vio);
            Comprobar(pista && prueba.Recompensas == 1, "El anuncio con recompensa avisa que se vio completo");

            Anuncios.Configurar(null);
            bool sinAnuncios = true;
            Anuncios.MostrarRecompensa("1 pista", vio => sinAnuncios = vio);
            Comprobar(!Anuncios.Activos && !sinAnuncios, "Con los anuncios apagados no hay recompensa");
        }
    }
}
