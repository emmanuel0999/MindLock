using System.Collections.Generic;
using UnityEngine;

namespace Mindlock
{
    public enum Sonido { Toque, Correcto, Error, Pista, Desbloqueo, Estrella, Logro, Alerta }

    /// <summary>
    /// Efectos y música de fondo sintetizados por código (no hacen falta archivos de audio).
    /// Respeta las opciones "Música" y "Efectos de sonido" de Preferencias.
    /// UIManager lo inicializa; las pantallas solo llaman a Sonidos.Reproducir(...).
    /// </summary>
    public static class Sonidos
    {
        const int Frecuencia = 22050;
        const float VolumenMusica = 0.22f;

        static AudioSource fuenteEfectos, fuenteMusica;
        static readonly Dictionary<Sonido, AudioClip> clips = new Dictionary<Sonido, AudioClip>();

        struct Nota
        {
            public float Hz, Inicio, Duracion;
            public Nota(float hz, float inicio, float duracion) { Hz = hz; Inicio = inicio; Duracion = duracion; }
        }

        // Frecuencias de las notas usadas (Hz)
        const float Sol3 = 196f, Si3 = 246.94f, Do4 = 261.63f, Re4 = 293.66f, Mi4 = 329.63f, Sol4 = 392f, La4 = 440f;
        const float Do5 = 523.25f, Mi5 = 659.25f, Sol5 = 783.99f, La5 = 880f, Do6 = 1046.5f, Mi6 = 1318.5f, Sol6 = 1567.98f;

        public static void Inicializar(GameObject host)
        {
            if (fuenteEfectos != null) return;

            fuenteEfectos = host.AddComponent<AudioSource>();
            fuenteEfectos.playOnAwake = false;

            fuenteMusica = host.AddComponent<AudioSource>();
            fuenteMusica.playOnAwake = false;
            fuenteMusica.loop = true;
            fuenteMusica.volume = VolumenMusica;

            clips.Clear();
            clips[Sonido.Toque] = Sintetizar("toque", 0.22f, 0f, new Nota(Mi6, 0f, 0.05f));
            clips[Sonido.Correcto] = Sintetizar("correcto", 0.26f, 0.4f,
                new Nota(Do5, 0f, 0.18f), new Nota(Mi5, 0.08f, 0.2f), new Nota(Sol5, 0.16f, 0.3f), new Nota(Do6, 0.24f, 0.5f));
            clips[Sonido.Error] = Sintetizar("error", 0.3f, 1f, new Nota(311.13f, 0f, 0.14f), new Nota(233.08f, 0.12f, 0.28f));
            clips[Sonido.Pista] = Sintetizar("pista", 0.2f, 0.2f, new Nota(La5, 0f, 0.4f), new Nota(Mi6, 0.07f, 0.55f));
            clips[Sonido.Desbloqueo] = Sintetizar("desbloqueo", 0.3f, 0.6f,
                new Nota(Sol3, 0f, 0.06f), new Nota(Sol4, 0.07f, 0.12f), new Nota(587.33f, 0.14f, 0.3f));
            clips[Sonido.Estrella] = Sintetizar("estrella", 0.18f, 0.1f, new Nota(Sol6, 0f, 0.25f), new Nota(2093f, 0.05f, 0.3f));
            clips[Sonido.Logro] = Sintetizar("logro", 0.22f, 0.5f,
                new Nota(Sol4, 0f, 0.15f), new Nota(Do5, 0.12f, 0.15f), new Nota(Mi5, 0.24f, 0.15f),
                new Nota(Sol5, 0.36f, 0.6f), new Nota(Mi5, 0.36f, 0.6f), new Nota(Do5, 0.36f, 0.6f));
            clips[Sonido.Alerta] = Sintetizar("alerta", 0.26f, 0.8f,
                new Nota(La4, 0f, 0.18f), new Nota(La4, 0.24f, 0.18f), new Nota(Mi4, 0.48f, 0.5f));

            fuenteMusica.clip = CrearMusica();
            ActualizarMusica();
        }

        public static void Reproducir(Sonido sonido)
        {
            AudioClip clip;
            if (fuenteEfectos == null || !Preferencias.Efectos || !clips.TryGetValue(sonido, out clip)) return;
            fuenteEfectos.PlayOneShot(clip);
        }

        /// <summary>Enciende o apaga la música según la preferencia guardada.</summary>
        public static void ActualizarMusica()
        {
            if (fuenteMusica == null || fuenteMusica.clip == null) return;
            if (Preferencias.Musica)
            {
                if (!fuenteMusica.isPlaying) fuenteMusica.Play();
            }
            else
            {
                fuenteMusica.Stop();
            }
        }

        // ------------------------------------------------------------------
        // Síntesis
        // ------------------------------------------------------------------

        /// <summary>Suma notas con envolvente percusiva. "brillo" agrega armónicos (0 = seno puro).</summary>
        static AudioClip Sintetizar(string nombre, float volumen, float brillo, params Nota[] notas)
        {
            float fin = 0f;
            foreach (var n in notas) fin = Mathf.Max(fin, n.Inicio + n.Duracion);
            int total = Mathf.CeilToInt((fin + 0.05f) * Frecuencia);
            var datos = new float[total];

            foreach (var n in notas)
            {
                int inicio = Mathf.RoundToInt(n.Inicio * Frecuencia);
                int largo = Mathf.RoundToInt(n.Duracion * Frecuencia);
                for (int i = 0; i < largo && inicio + i < total; i++)
                {
                    float t = (float)i / Frecuencia;
                    float fase = 2f * Mathf.PI * n.Hz * t;
                    float s = Mathf.Sin(fase) + brillo * (0.35f * Mathf.Sin(2f * fase) + 0.15f * Mathf.Sin(3f * fase));
                    datos[inicio + i] += s * Envolvente(t, n.Duracion) * volumen;
                }
            }

            Normalizar(datos, 0.9f);
            return CrearClip(nombre, datos, false);
        }

        static float Envolvente(float t, float duracion)
        {
            const float ataque = 0.006f;
            if (t < ataque) return t / ataque;
            float caida = Mathf.Exp(-4f * (t - ataque) / duracion);
            float cola = Mathf.Clamp01((duracion - t) / 0.02f);   // evita el "clic" al final
            return caida * cola;
        }

        /// <summary>
        /// Música ambiental en bucle (16 s): acordes suaves La m - Fa - Do - Sol
        /// con un arpegio tenue encima. El bucle empalma sin cortes.
        /// </summary>
        static AudioClip CrearMusica()
        {
            const float duracionAcorde = 4f;
            float[][] acordes =
            {
                new[] { 110f, 220f, Do4, Mi4 },          // La menor
                new[] { 87.31f, 174.61f, 220f, Do4 },    // Fa
                new[] { 130.81f, Sol3, Do4, Mi4 },       // Do
                new[] { 98f, Sol3, Si3, Re4 },           // Sol
            };

            int total = Mathf.RoundToInt(acordes.Length * duracionAcorde * Frecuencia);
            var datos = new float[total];
            const float solape = 0.6f;

            for (int c = 0; c < acordes.Length; c++)
            {
                float inicio = c * duracionAcorde - solape;
                float largo = duracionAcorde + 2f * solape;
                int muestras = Mathf.RoundToInt(largo * Frecuencia);
                int desplazamiento = Mathf.RoundToInt(inicio * Frecuencia);

                // Colchón: cada nota con dos osciladores levemente desafinados
                for (int i = 0; i < muestras; i++)
                {
                    float t = (float)i / Frecuencia;
                    float env = Mathf.Sin(Mathf.PI * t / largo);
                    env *= env;
                    float s = 0f;
                    foreach (var hz in acordes[c])
                        s += Mathf.Sin(2f * Mathf.PI * hz * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * hz * 1.003f * t);
                    int k = ((desplazamiento + i) % total + total) % total;
                    datos[k] += s * env * 0.06f;
                }

                // Arpegio: corcheas con las notas del acorde una octava arriba
                for (int paso = 0; paso < 8; paso++)
                {
                    float hz = acordes[c][1 + paso % 3] * 2f;
                    int ini = Mathf.RoundToInt((c * duracionAcorde + paso * 0.5f) * Frecuencia);
                    int dur = Mathf.RoundToInt(0.9f * Frecuencia);
                    for (int i = 0; i < dur; i++)
                    {
                        float t = (float)i / Frecuencia;
                        float env = Mathf.Exp(-5f * t) * Mathf.Clamp01(t / 0.01f);
                        int k = (ini + i) % total;
                        datos[k] += Mathf.Sin(2f * Mathf.PI * hz * t) * env * 0.08f;
                    }
                }
            }

            Normalizar(datos, 0.8f);
            return CrearClip("musica", datos, false);
        }

        static void Normalizar(float[] datos, float pico)
        {
            float max = 0f;
            foreach (var d in datos) max = Mathf.Max(max, Mathf.Abs(d));
            if (max <= pico || max <= 0f) return;
            float f = pico / max;
            for (int i = 0; i < datos.Length; i++) datos[i] *= f;
        }

        static AudioClip CrearClip(string nombre, float[] datos, bool stream)
        {
            var clip = AudioClip.Create("mindlock-" + nombre, datos.Length, 1, Frecuencia, stream);
            clip.SetData(datos, 0);
            return clip;
        }
    }
}
