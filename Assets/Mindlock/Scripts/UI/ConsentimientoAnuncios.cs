using System;
using System.Threading;
using UnityEngine;

namespace Mindlock.UI
{
    /// <summary>
    /// Consentimiento de anuncios con Google UMP (la librería ya viene con el adaptador de AdMob).
    /// En el Espacio Económico Europeo, Reino Unido y Suiza muestra el mensaje que se configura en
    /// AdMob (Privacidad y mensajes); en el resto del mundo no muestra nada y sigue enseguida.
    /// La respuesta queda guardada en el teléfono (cadena IAB TCF): AdMob la lee sola y a LevelPlay
    /// se le pasa con SetConsent para Unity Ads.
    /// </summary>
    public static class ConsentimientoAnuncios
    {
        const string Ump = "com.google.android.ump.";

        static SynchronizationContext hiloUnity;

        /// <summary>Pide el consentimiento si hace falta y luego llama a <paramref name="alTerminar"/> en el hilo de Unity.</summary>
        public static void Pedir(Action alTerminar)
        {
            hiloUnity = SynchronizationContext.Current;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var actividad = Actividad();
                actividad.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        var info = Informacion(actividad);
                        info.Call("requestConsentInfoUpdate", actividad, Parametros(actividad),
                            new AlActualizar(() =>
                            {
                                using (var umpClase = new AndroidJavaClass(Ump + "UserMessagingPlatform"))
                                    umpClase.CallStatic("loadAndShowConsentFormIfRequired", actividad,
                                        new AlCerrarFormulario(error => Terminar(error, alTerminar)));
                            }),
                            new AlFallarActualizar(error => Terminar(error, alTerminar)));
                    }
                    catch (Exception e) { Terminar(e.Message, alTerminar); }
                }));
                return;
            }
            catch (Exception e) { Debug.LogWarning("[Mindlock] No se pudo pedir el consentimiento: " + e.Message); }
#endif
            alTerminar();
        }

        /// <summary>True si el jugador está en una región donde debe poder cambiar su decisión (botón en Opciones).</summary>
        public static bool OpcionesDisponibles
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using (var estado = Informacion(Actividad()).Call<AndroidJavaObject>("getPrivacyOptionsRequirementStatus"))
                        return estado.Call<string>("name") == "REQUIRED";
                }
                catch (Exception) { }
#endif
                return false;
            }
        }

        /// <summary>Vuelve a abrir el mensaje de Google para cambiar la decisión.</summary>
        public static void MostrarOpciones(Action alCerrar)
        {
            hiloUnity = SynchronizationContext.Current;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var actividad = Actividad();
                actividad.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        using (var umpClase = new AndroidJavaClass(Ump + "UserMessagingPlatform"))
                            umpClase.CallStatic("showPrivacyOptionsForm", actividad,
                                new AlCerrarFormulario(error => Terminar(error, alCerrar)));
                    }
                    catch (Exception e) { Terminar(e.Message, alCerrar); }
                }));
                return;
            }
            catch (Exception e) { Debug.LogWarning("[Mindlock] No se pudieron abrir las opciones de privacidad: " + e.Message); }
#endif
            if (alCerrar != null) alCerrar();
        }

        /// <summary>
        /// Pasa la decisión a LevelPlay. Solo cuando las reglas de privacidad aplican (IABTCF_gdprApplies = 1):
        /// consiente si aceptó el propósito 1 (guardar o leer información en el dispositivo).
        /// Hay que llamarlo antes de LevelPlay.Init y después de cambiar la decisión.
        /// </summary>
        public static void AplicarALevelPlay()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var actividad = Actividad();
                string archivo = actividad.Call<string>("getPackageName") + "_preferences";
                using (var prefs = actividad.Call<AndroidJavaObject>("getSharedPreferences", archivo, 0))
                {
                    if (prefs.Call<int>("getInt", "IABTCF_gdprApplies", 0) != 1) return;
                    string propositos = prefs.Call<string>("getString", "IABTCF_PurposeConsents", "");
                    bool acepto = !string.IsNullOrEmpty(propositos) && propositos[0] == '1';
                    Unity.Services.LevelPlay.LevelPlay.SetConsent(acepto);
                    Debug.Log("[Mindlock] Consentimiento de anuncios: " + (acepto ? "aceptado" : "rechazado"));
                }
            }
            catch (Exception e) { Debug.LogWarning("[Mindlock] No se pudo leer el consentimiento: " + e.Message); }
#endif
        }

        static void Terminar(object error, Action alTerminar)
        {
            if (error != null)
            {
                string texto = error as string;
                if (texto == null && error is AndroidJavaObject formError)
                {
                    try { texto = formError.Call<int>("getErrorCode") + ": " + formError.Call<string>("getMessage"); }
                    catch (Exception) { texto = "sin detalle"; }
                }
                Debug.LogWarning("[Mindlock] Consentimiento de anuncios: " + texto);
            }
            if (alTerminar == null) return;
            if (hiloUnity != null) hiloUnity.Post(_ => alTerminar(), null);
            else alTerminar();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject actividadGuardada;

        static AndroidJavaObject Actividad()
        {
            if (actividadGuardada == null)
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    actividadGuardada = unity.GetStatic<AndroidJavaObject>("currentActivity");
            return actividadGuardada;
        }

        static AndroidJavaObject Informacion(AndroidJavaObject actividad)
        {
            using (var umpClase = new AndroidJavaClass(Ump + "UserMessagingPlatform"))
                return umpClase.CallStatic<AndroidJavaObject>("getConsentInformation", actividad);
        }

        static AndroidJavaObject Parametros(AndroidJavaObject actividad)
        {
            var constructor = new AndroidJavaObject(Ump + "ConsentRequestParameters$Builder");
            constructor.Call<AndroidJavaObject>("setTagForUnderAgeOfConsent", false);
            if (ClavesAnuncios.ConsentimientoDePrueba)
            {
                // Prueba: el teléfono se comporta como si estuviera en Europa y el mensaje sale siempre
                Informacion(actividad).Call("reset");
                var depuracion = new AndroidJavaObject(Ump + "ConsentDebugSettings$Builder", actividad);
                depuracion.Call<AndroidJavaObject>("setDebugGeography", 1); // DEBUG_GEOGRAPHY_EEA
                depuracion.Call<AndroidJavaObject>("setForceTesting", true);
                constructor.Call<AndroidJavaObject>("setConsentDebugSettings", depuracion.Call<AndroidJavaObject>("build"));
            }
            return constructor.Call<AndroidJavaObject>("build");
        }

        class AlActualizar : AndroidJavaProxy
        {
            readonly Action accion;
            public AlActualizar(Action accion) : base(Ump + "ConsentInformation$OnConsentInfoUpdateSuccessListener") { this.accion = accion; }
            [UnityEngine.Scripting.Preserve] public void onConsentInfoUpdateSuccess() { accion(); }
        }

        class AlFallarActualizar : AndroidJavaProxy
        {
            readonly Action<AndroidJavaObject> accion;
            public AlFallarActualizar(Action<AndroidJavaObject> accion) : base(Ump + "ConsentInformation$OnConsentInfoUpdateFailureListener") { this.accion = accion; }
            [UnityEngine.Scripting.Preserve] public void onConsentInfoUpdateFailure(AndroidJavaObject error) { accion(error); }
        }

        class AlCerrarFormulario : AndroidJavaProxy
        {
            readonly Action<AndroidJavaObject> accion;
            public AlCerrarFormulario(Action<AndroidJavaObject> accion) : base(Ump + "ConsentForm$OnConsentFormDismissedListener") { this.accion = accion; }
            [UnityEngine.Scripting.Preserve] public void onConsentFormDismissed(AndroidJavaObject error) { accion(error); }
        }
#endif
    }
}
