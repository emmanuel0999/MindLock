namespace Mindlock
{
    /// <summary>
    /// Claves de LevelPlay (Unity Ads y AdMob entran como redes de la mediación).
    /// Se copian del panel de LevelPlay: Apps > tu app > App key, y Ad units > el id de cada anuncio.
    /// Mientras falte alguna, el juego usa los anuncios propios (AnunciosDemo).
    /// El App ID de AdMob va en Ads Mediation > Network Settings (archivo
    /// Assets/LevelPlay/Resources/LevelPlayMediatedNetworkSettings.asset).
    /// </summary>
    public static class ClavesAnuncios
    {
#if UNITY_IOS
        // Todavía no hay app de iOS en LevelPlay
        public const string AppKey = "";
        public const string Banner = "";
        public const string Intersticial = "";
        public const string Recompensa = "";
#else
        // App "Mindlock" de Android en LevelPlay (Frailes City Studios)
        public const string AppKey = "294620cdd";
        public const string Banner = "tkanmiijx0t3bbos";         // Banner_Android
        public const string Intersticial = "jputcemwx0ol1jwg";   // Interstitial_Android
        public const string Recompensa = "5oexzynfdro8yq9g";     // Rewarded_Android

        // Unity Ads (organización Frailes City Studios, proyecto Mindlock): Game ID 800424609.
        // Placements con pujas: BP_Rewarded_Android, BP_Interstitial_Android, BP_Banner_Android.
        // Se ponen en el panel de LevelPlay (red UnityAds), no aquí.
        // AdMob (cuenta pub-9840287727615214). El App ID está en LevelPlayMediatedNetworkSettings.asset;
        // los ids de las unidades se ponen en el panel de LevelPlay (red AdMob), no aquí:
        //   App ID:       ca-app-pub-9840287727615214~3929809511
        // Unidades con "Ofertas para socios" (pujas de Google en LevelPlay), las que se usan:
        //   Banner_LevelPlay:       ca-app-pub-9840287727615214/6476256816
        //   Interstitial_LevelPlay: ca-app-pub-9840287727615214/3850093473
        //   Rewarded_LevelPlay:     ca-app-pub-9840287727615214/6173004489
        // Unidades sin pujas (no se usan: LevelPlay solo acepta AdMob con pujas para estos formatos):
        //   Banner_Android:         ca-app-pub-9840287727615214/5633948437
        //   Interstitial_Android:   ca-app-pub-9840287727615214/6699830126
        //   Rewarded_Android:       ca-app-pub-9840287727615214/1463428278
#endif

        /// <summary>
        /// true: al iniciar abre la herramienta de prueba de LevelPlay (Test Suite) para revisar
        /// que Unity Ads y AdMob respondan. Ponlo en false antes de publicar.
        /// </summary>
        public static readonly bool HerramientaDePrueba = false;

        /// <summary>
        /// true: el teléfono se comporta como si estuviera en Europa y el mensaje de consentimiento
        /// de Google sale en cada arranque. Sirve para probarlo; ponlo en false antes de publicar.
        /// </summary>
        public static readonly bool ConsentimientoDePrueba = false;

        /// <summary>Hacen falta la App key y los tres anuncios; si no, se usan los anuncios propios.</summary>
        public static bool Configuradas
        {
            get
            {
                return !string.IsNullOrEmpty(AppKey) && !string.IsNullOrEmpty(Banner)
                    && !string.IsNullOrEmpty(Intersticial) && !string.IsNullOrEmpty(Recompensa);
            }
        }
    }
}
