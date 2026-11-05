using System;
using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>
    /// Anuncios reales con Unity LevelPlay (Unity Ads y AdMob compiten en la mediación).
    /// Solo funciona en Android e iOS con ClavesAnuncios configuradas; UIManager decide cuándo usarlo.
    /// El banner es nativo y se dibuja encima de la app, así que el elemento "banner" de Raiz.uxml
    /// queda vacío y solo reserva su altura abajo de todo.
    /// </summary>
    public class AnunciosLevelPlay : IProveedorAnuncios
    {
        const float AltoBannerDp = 50f;
        const long MsReintento = 15000;

        readonly UIManager ui;
        VisualElement espacioBanner;
        LevelPlayBannerAd banner;
        LevelPlayInterstitialAd intersticial;
        LevelPlayRewardedAd recompensa;

        bool iniciado, bannerCargado, bannerVisible, recompensaGanada;
        float altoEspacio;
        Action alCerrarIntersticial;
        Action<bool> alTerminarRecompensa;

        public AnunciosLevelPlay(UIManager manager, VisualElement raiz)
        {
            ui = manager;
            Enlazar(raiz);

            // Primero el consentimiento (solo pregunta en Europa, Reino Unido y Suiza), después los anuncios
            ConsentimientoAnuncios.Pedir(IniciarLevelPlay);
        }

        void IniciarLevelPlay()
        {
            ConsentimientoAnuncios.AplicarALevelPlay();
            LevelPlay.OnInitSuccess += _ => Iniciado();
            LevelPlay.OnInitFailed += e => Debug.LogWarning("[Mindlock] LevelPlay no inició: " + e);
            if (ClavesAnuncios.HerramientaDePrueba) LevelPlay.SetMetaData("is_test_suite", "enable");
            LevelPlay.Init(ClavesAnuncios.AppKey);
        }

        /// <summary>Usa el elemento "banner" de la interfaz (también al reconstruirla por un cambio de idioma).</summary>
        public void Enlazar(VisualElement raiz)
        {
            espacioBanner = raiz.Q("banner");
            espacioBanner.Clear();
            espacioBanner.AddToClassList("banner--nativo");
            espacioBanner.pickingMode = PickingMode.Ignore;
            espacioBanner.RegisterCallback<GeometryChangedEvent>(_ => AjustarEspacio());
            UIUtil.Mostrar(espacioBanner, bannerVisible && bannerCargado);
            AjustarModales();
        }

        public bool RecompensaLista { get { return iniciado && recompensa != null && recompensa.IsAdReady(); } }

        void Iniciado()
        {
            iniciado = true;
            if (ClavesAnuncios.HerramientaDePrueba) LevelPlay.LaunchTestSuite();

            banner = new LevelPlayBannerAd(ClavesAnuncios.Banner, new LevelPlayBannerAd.Config.Builder()
                .SetSize(LevelPlayAdSize.BANNER)
                .SetPosition(LevelPlayBannerPosition.BottomCenter)
                .SetDisplayOnLoad(false)
                // false: LevelPlay cuenta la barra de navegación aunque el juego la oculte y subía el banner
                // un alto de banner, tapando la barra de abajo. Pegado al borde coincide con el espacio reservado.
                .SetRespectSafeArea(false)
                .Build());
            banner.OnAdLoaded += info => { Debug.Log("[Mindlock] Banner cargado: " + info); bannerCargado = true; MostrarBanner(bannerVisible); };
            banner.OnAdLoadFailed += e => { Registrar("banner", e); Reintentar(() => banner.LoadAd()); };
            banner.LoadAd();

            intersticial = new LevelPlayInterstitialAd(ClavesAnuncios.Intersticial);
            intersticial.OnAdLoadFailed += e => { Registrar("intersticial", e); Reintentar(() => intersticial.LoadAd()); };
            intersticial.OnAdClosed += _ => TerminarIntersticial();
            intersticial.OnAdDisplayFailed += (info, e) => TerminarIntersticial();
            intersticial.LoadAd();

            recompensa = new LevelPlayRewardedAd(ClavesAnuncios.Recompensa);
            recompensa.OnAdLoadFailed += e => { Registrar("recompensa", e); Reintentar(() => recompensa.LoadAd()); };
            recompensa.OnAdRewarded += (info, premio) => recompensaGanada = true;
            recompensa.OnAdClosed += _ => TerminarRecompensa();
            recompensa.OnAdDisplayFailed += (info, e) => TerminarRecompensa();
            recompensa.LoadAd();
        }

        /// <summary>Deja en el registro del teléfono (adb logcat -s Unity) por qué no cargó un anuncio.
        /// Código 509 = no hubo anuncio disponible (sin pujas ni relleno), no es un error del juego.</summary>
        static void Registrar(string formato, LevelPlayAdError e)
        {
            Debug.LogWarning("[Mindlock] No cargó el " + formato + " (" + (e != null ? e.ErrorCode + ": " + e.ErrorMessage : "sin detalle") + "). Reintento en " + MsReintento / 1000 + " s");
        }

        void Reintentar(Action cargar)
        {
            espacioBanner.schedule.Execute(cargar).StartingIn(MsReintento);
        }

        // ------------------------------------------------------------------
        // Banner
        // ------------------------------------------------------------------

        public void MostrarBanner(bool visible)
        {
            bannerVisible = visible;
            UIUtil.Mostrar(espacioBanner, visible && bannerCargado);
            AjustarModales();
            if (banner == null || !bannerCargado) return;
            if (visible) banner.ShowAd();
            else banner.HideAd();
        }

        /// <summary>
        /// El banner nativo mide 50 dp y va pegado al borde de abajo de la pantalla. La app termina donde empieza
        /// la zona segura de abajo, así que el espacio reservado es el alto del banner menos esa zona.
        /// </summary>
        void AjustarEspacio()
        {
            var panel = espacioBanner.panel;
            if (panel == null) return;
            float pixeles = Mathf.Max(0f, AltoBannerDp * Densidad() - Screen.safeArea.yMin);
            float alto = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(0, pixeles)).y - RuntimePanelUtils.ScreenToPanel(panel, Vector2.zero).y;
            altoEspacio = alto;
            if (Mathf.Abs(espacioBanner.resolvedStyle.height - alto) > 0.5f) espacioBanner.style.height = alto;
            AjustarModales();
        }

        /// <summary>
        /// El banner nativo se dibuja encima de todo, también de las ventanas de vidas, tienda y anuncios
        /// (que cubren toda la app). Mientras se ve, esas ventanas terminan donde empieza el banner.
        /// Las ventanas de cada pantalla no hace falta moverlas: quedan arriba de la barra de navegación.
        /// </summary>
        void AjustarModales()
        {
            var app = espacioBanner.parent;
            if (app == null) return;
            float abajo = bannerVisible && bannerCargado ? altoEspacio : 0f;
            foreach (var hijo in app.Children())
                if (hijo.ClassListContains("modal-fondo")) hijo.style.bottom = abajo;
        }

        static float densidad;

        /// <summary>Píxeles por dp del teléfono (DisplayMetrics.density en Android; Screen.dpi da el valor físico y no sirve).</summary>
        static float Densidad()
        {
            if (densidad > 0f) return densidad;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var actividad = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var recursos = actividad.Call<AndroidJavaObject>("getResources"))
                using (var medidas = recursos.Call<AndroidJavaObject>("getDisplayMetrics"))
                    densidad = medidas.Get<float>("density");
            }
            catch (Exception e) { Debug.LogWarning("[Mindlock] No se pudo leer la densidad: " + e.Message); }
#endif
            if (densidad <= 0f) densidad = (Screen.dpi > 0 ? Screen.dpi : 160f) / 160f;
            return densidad;
        }

        // ------------------------------------------------------------------
        // Pantalla completa
        // ------------------------------------------------------------------

        public void MostrarIntersticial(Action alCerrar)
        {
            if (intersticial == null || !intersticial.IsAdReady()) { if (alCerrar != null) alCerrar(); return; }
            alCerrarIntersticial = alCerrar;
            intersticial.ShowAd();
        }

        void TerminarIntersticial()
        {
            var aviso = alCerrarIntersticial;
            alCerrarIntersticial = null;
            intersticial.LoadAd();
            if (aviso != null) aviso();
        }

        public void MostrarRecompensa(string premio, Action<bool> alTerminar)
        {
            if (!RecompensaLista)
            {
                ui.MostrarAviso(Idioma.T("No hay anuncios disponibles. Inténtalo en un momento"), "alerta");
                alTerminar(false);
                return;
            }
            recompensaGanada = false;
            alTerminarRecompensa = alTerminar;
            recompensa.ShowAd();
        }

        void TerminarRecompensa()
        {
            var aviso = alTerminarRecompensa;
            alTerminarRecompensa = null;
            recompensa.LoadAd();
            // Algunas redes avisan la recompensa justo después de cerrar el anuncio
            if (aviso != null) espacioBanner.schedule.Execute(() => aviso(recompensaGanada)).StartingIn(400);
        }
    }
}
