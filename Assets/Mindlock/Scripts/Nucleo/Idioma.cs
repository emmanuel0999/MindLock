using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock
{
    /// <summary>
    /// Idioma del juego: inglés (principal) o español. Los textos se escriben en español
    /// en el código y en los UXML, y TextosIngles.cs tiene su traducción.
    ///   T("Texto")                 → texto traducido
    ///   F("Te quedan {0} vidas", n) → texto con datos (los {0}, {1} se mantienen en la traducción)
    /// Los acertijos y los logros traen su versión en inglés aparte (CatalogoAcertijos.Ingles.cs).
    /// </summary>
    public static class Idioma
    {
        public const int Ingles = 0, Espanol = 1;

        static int actual = -1;
        static readonly HashSet<string> avisados = new HashSet<string>();

        public static int Actual
        {
            get
            {
                if (actual < 0) actual = Preferencias.Idioma;
                return actual;
            }
        }

        public static bool EsIngles { get { return Actual == Ingles; } }

        /// <summary>Cambia el idioma sin guardarlo (para las pruebas).</summary>
        public static void Usar(int idioma)
        {
            actual = idioma;
        }

        /// <summary>Guarda el idioma elegido. UIManager vuelve a construir las pantallas.</summary>
        public static void Cambiar(int idioma)
        {
            Preferencias.Idioma = idioma;
            actual = idioma;
        }

        public static string T(string es)
        {
            if (!EsIngles || string.IsNullOrEmpty(es)) return es;
            string en;
            if (TextosIngles.Textos.TryGetValue(es, out en)) return en;
#if UNITY_EDITOR
            if (avisados.Add(es)) Debug.LogWarning("[Mindlock] Falta la traducción al inglés de: " + es);
#endif
            return es;
        }

        public static string F(string es, params object[] datos)
        {
            return string.Format(T(es), datos);
        }

        /// <summary>Traduce los textos fijos de un UXML recién creado (se llama una sola vez por pantalla).</summary>
        public static void Traducir(VisualElement raiz)
        {
            if (!EsIngles || raiz == null) return;
            raiz.Query<TextElement>().ForEach(e =>
            {
                string en;
                if (!string.IsNullOrEmpty(e.text) && TextosIngles.Textos.TryGetValue(e.text, out en)) e.text = en;
            });
        }
    }
}
