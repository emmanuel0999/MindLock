using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                CatalogoAcertijos.ModoDesafio = modo;
                ProbarPuntaje();
                ProbarGuardado(ruta);
            }
            catch (Exception e)
            {
                fallos.Add("Excepción: " + e);
            }
            finally
            {
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
    }
}
