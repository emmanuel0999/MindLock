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
            EditorUserBuildSettings.buildAppBundle = false;
            Compilar(BuildTarget.Android, "Builds/Android/Mindlock.apk");
        }

        /// <summary>
        /// Paquete para Google Play (.aab): firmado con el keystore propio, IL2CPP y 32 + 64 bits.
        /// Unity no guarda las contraseñas del keystore: hay que escribirlas cada vez que se abre el proyecto.
        /// </summary>
        [MenuItem("Mindlock/Compilar/Android (AAB para Google Play)", priority = 62)]
        public static void AndroidPlay()
        {
            if (!PlayerSettings.Android.useCustomKeystore)
            {
                Fallar("Para Google Play hace falta tu propio keystore: Player Settings > Publishing Settings > Custom Keystore.");
                return;
            }
            if (ClavesAnuncios.HerramientaDePrueba || ClavesAnuncios.ConsentimientoDePrueba)
            {
                Fallar("Pon HerramientaDePrueba y ConsentimientoDePrueba en false (Scripts/Nucleo/ClavesAnuncios.cs) antes de compilar para Google Play.");
                return;
            }
            if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
            {
                Fallar("Escribe las contraseñas del keystore y del alias en Player Settings > Publishing Settings (Unity no las guarda al cerrar).");
                return;
            }

            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = true;
            Debug.Log("[Mindlock] Versión " + PlayerSettings.bundleVersion + " (código " + PlayerSettings.Android.bundleVersionCode
                + "). Cada subida a Google Play necesita un código mayor que el anterior.");
            try { Compilar(BuildTarget.Android, "Builds/Android/Mindlock.aab"); }
            finally { EditorUserBuildSettings.buildAppBundle = false; }
        }

        static void Fallar(string mensaje)
        {
            Debug.LogError("[Mindlock] " + mensaje);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else EditorUtility.DisplayDialog("Mindlock", mensaje, "Entendido");
        }

        [MenuItem("Mindlock/Compilar/Web (WebGL)", priority = 63)]
        public static void Web()
        {
            // Sin compresión: se puede abrir con cualquier servidor local sin configurar cabeceras
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            Compilar(BuildTarget.WebGL, "Builds/Web");
        }

        /// <summary>
        /// Ajusta la página web: el juego ocupa toda la ventana del navegador en cualquier
        /// tamaño (la plantilla por defecto usa un recuadro fijo de 960 x 600 en PC) y el
        /// progreso se guarda solo en el navegador (IndexedDB).
        /// </summary>
        static void AjustarPagina(string carpeta)
        {
            string index = Path.Combine(carpeta, "index.html");
            if (!File.Exists(index)) return;
            string html = File.ReadAllText(index)
                .Replace("// config.autoSyncPersistentDataPath = true;", "config.autoSyncPersistentDataPath = true;")
                .Replace("if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {", "if (true) {  // Mindlock: pantalla completa también en PC");
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
            if (ok && destino == BuildTarget.WebGL) AjustarPagina(salida);
            if (ok) Debug.Log("[Mindlock] Compilado en " + Path.GetFullPath(salida));
            else Debug.LogError("[Mindlock] La compilación falló: " + reporte.summary.result + " (" + reporte.summary.totalErrors + " errores)");

            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
