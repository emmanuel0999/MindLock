using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mindlock.Herramientas
{
    /// <summary>Compila el juego en la carpeta Builds/ del proyecto.</summary>
    public static class CompilarMindlock
    {
        [MenuItem("Mindlock/Compilar/Windows", priority = 60)]
        public static void Windows()
        {
            Compilar(BuildTarget.StandaloneWindows64, "Builds/Windows/Mindlock.exe");
        }

        [MenuItem("Mindlock/Compilar/Android (APK)", priority = 61)]
        public static void Android()
        {
            Compilar(BuildTarget.Android, "Builds/Android/Mindlock.apk");
        }

        [MenuItem("Mindlock/Compilar/Web (WebGL)", priority = 62)]
        public static void Web()
        {
            // Sin compresión: se puede abrir con cualquier servidor local sin configurar cabeceras
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            Compilar(BuildTarget.WebGL, "Builds/Web");
        }

        /// <summary>
        /// Ajusta la página web: forma de teléfono (la plantilla por defecto es 960 x 600)
        /// y guardado automático del progreso en el navegador (IndexedDB).
        /// </summary>
        static void PonerVertical(string carpeta)
        {
            string index = Path.Combine(carpeta, "index.html");
            if (!File.Exists(index)) return;
            string html = File.ReadAllText(index)
                .Replace("// config.autoSyncPersistentDataPath = true;", "config.autoSyncPersistentDataPath = true;")
                .Replace("width=960 height=600", "width=390 height=844")
                .Replace("canvas.style.width = \"960px\";", "canvas.style.width = \"390px\";")
                .Replace("canvas.style.height = \"600px\";", "canvas.style.height = \"844px\";");
            File.WriteAllText(index, html);
        }

        static void Compilar(BuildTarget destino, string salida)
        {
            if (!File.Exists(ConfigurarMindlock.RutaEscena)) ConfigurarMindlock.Ejecutar();

            var carpeta = Path.GetDirectoryName(salida);
            if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);

            var opciones = new BuildPlayerOptions
            {
                scenes = new[] { ConfigurarMindlock.RutaEscena },
                locationPathName = salida,
                target = destino,
                options = BuildOptions.None
            };
            var reporte = BuildPipeline.BuildPlayer(opciones);
            bool ok = reporte.summary.result == BuildResult.Succeeded;
            if (ok && destino == BuildTarget.WebGL) PonerVertical(salida);
            if (ok) Debug.Log("[Mindlock] Compilado en " + Path.GetFullPath(salida));
            else Debug.LogError("[Mindlock] La compilación falló: " + reporte.summary.result + " (" + reporte.summary.totalErrors + " errores)");

            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
