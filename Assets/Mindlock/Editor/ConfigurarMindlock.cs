using System.IO;
using Mindlock.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.Herramientas
{
    /// <summary>
    /// Deja el proyecto listo para jugar: Panel Settings, escena principal y ajustes del jugador.
    /// Menú: Mindlock > Configurar proyecto (se puede repetir sin problema).
    /// </summary>
    public static class ConfigurarMindlock
    {
        public const string RutaPanel = "Assets/Mindlock/MindlockPanelSettings.asset";
        public const string RutaTema = "Assets/Mindlock/Resources/Mindlock/Estilos/MindlockTheme.tss";
        public const string RutaEscena = "Assets/Scenes/Mindlock.unity";
        static readonly Color ColorFondo = new Color32(15, 23, 42, 255);

        [MenuItem("Mindlock/Configurar proyecto", priority = 0)]
        public static void Ejecutar()
        {
            var panel = CrearPanelSettings();
            CrearEscena(panel);
            AjustarJugador();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mindlock] Proyecto configurado. Abre " + RutaEscena + " y pulsa Play.");
        }

        [MenuItem("Mindlock/Abrir carpeta de datos guardados", priority = 20)]
        static void AbrirDatos()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        [MenuItem("Mindlock/Borrar datos guardados", priority = 21)]
        static void BorrarDatos()
        {
            if (!EditorUtility.DisplayDialog("Mindlock", "¿Borrar perfiles y progreso guardados en este equipo?", "Borrar", "Cancelar")) return;
            string ruta = Path.Combine(Application.persistentDataPath, Datos.ArchivoJuegoService.NombreArchivo);
            foreach (var f in new[] { ruta, ruta + ".bak", ruta + ".tmp" })
                if (File.Exists(f)) File.Delete(f);
            Debug.Log("[Mindlock] Datos borrados.");
        }

        static PanelSettings CrearPanelSettings()
        {
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(RutaPanel);
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, RutaPanel);
            }
            ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(RutaTema);
            if (ps.themeStyleSheet == null) Debug.LogError("[Mindlock] No se encontró " + RutaTema);
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(390, 844);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            EditorUtility.SetDirty(ps);
            return ps;
        }

        static void CrearEscena(PanelSettings panel)
        {
            Directory.CreateDirectory("Assets/Scenes");
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camara = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camara.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ColorFondo;
            cam.orthographic = true;
            cam.cullingMask = 0;
            camara.AddComponent<AudioListener>();

            var ui = new GameObject("UI");
            var documento = ui.AddComponent<UIDocument>();
            documento.panelSettings = panel;
            ui.AddComponent<UIManager>();

            EditorSceneManager.SaveScene(escena, RutaEscena);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true) };
        }

        static void AjustarJugador()
        {
            PlayerSettings.companyName = "Unicaribe";
            PlayerSettings.productName = "Mindlock";
            PlayerSettings.bundleVersion = "1.0";

            // Juego de teléfono en vertical
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            foreach (var destino in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.Standalone })
                PlayerSettings.SetApplicationIdentifier(destino, "com.unicaribe.mindlock");

            // En PC se abre como una ventana con forma de teléfono
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 450;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;

            PlayerSettings.SplashScreen.backgroundColor = ColorFondo;
        }
    }
}
