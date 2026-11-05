using System;
using System.Collections.Generic;

namespace Mindlock.Datos
{
    // =====================================================================
    // Las 9 tablas del diccionario de datos, tal como se guardan en el
    // archivo local (JSON). Los nombres de los campos son los del informe;
    // las fechas van como texto ISO 8601 (AAAA-MM-DD HH:MM:SS) y un texto
    // vacío equivale a NULL. Los campos marcados "auxiliar" no están en el
    // diccionario: los necesitan los logros y la racha.
    // =====================================================================

    [Serializable]
    public class BaseDatosLocal
    {
        public int version = 1;
        public string guardado;

        public List<TablaJugador> jugador = new List<TablaJugador>();
        public List<TablaNivel> nivel = new List<TablaNivel>();
        public List<TablaAcertijo> acertijo = new List<TablaAcertijo>();
        public List<TablaPista> pista = new List<TablaPista>();
        public List<TablaProgreso> progreso = new List<TablaProgreso>();
        public List<TablaIntento> intento = new List<TablaIntento>();
        public List<TablaLogro> logro = new List<TablaLogro>();
        public List<TablaJugadorLogro> jugador_logro = new List<TablaJugadorLogro>();
        public List<TablaAcertijoDiario> acertijo_diario = new List<TablaAcertijoDiario>();
    }

    /// <summary>1. JUGADOR</summary>
    [Serializable]
    public class TablaJugador
    {
        public int id_jugador;
        public string nombre;
        public string fecha_creacion;
        public int puntaje_total;
        public int pistas_disponibles;
        public int racha_diaria;
        public string ultimo_diario;      // auxiliar
        public bool racha_perdida;        // auxiliar
        public int vidas = -1;            // -1: archivo anterior a las vidas (empieza con el máximo)
        public int monedas;
        public string recarga_vida_desde;
        public int anuncios_para_vida;
        public int anuncios_hoy;
        public string fecha_anuncios;
    }

    /// <summary>2. NIVEL</summary>
    [Serializable]
    public class TablaNivel
    {
        public int id_nivel;
        public int numero;
        public string dificultad;
        public int puntaje_base;
        public int tiempo_limite_seg;     // 0 = sin límite (NULL)
    }

    /// <summary>3. ACERTIJO</summary>
    [Serializable]
    public class TablaAcertijo
    {
        public int id_acertijo;
        public int id_nivel;              // 0 = solo modo diario (NULL)
        public string categoria;
        public string enunciado;
        public string tipo_respuesta;
        public string respuesta_correcta;
        public bool es_diario;
    }

    /// <summary>4. PISTA</summary>
    [Serializable]
    public class TablaPista
    {
        public int id_pista;
        public int id_acertijo;
        public int orden;
        public string texto;
        public int penalizacion;
    }

    /// <summary>5. PROGRESO</summary>
    [Serializable]
    public class TablaProgreso
    {
        public int id_progreso;
        public int id_jugador;
        public int id_nivel;
        public string estado;
        public int mejor_puntaje;
        public int estrellas;
        public string fecha_completado;
        public int mejor_tiempo;          // auxiliar
        public bool sin_pistas;           // auxiliar
        public bool primer_intento;       // auxiliar
    }

    /// <summary>6. INTENTO</summary>
    [Serializable]
    public class TablaIntento
    {
        public int id_intento;
        public int id_jugador;
        public int id_acertijo;
        public string respuesta_dada;
        public bool es_correcto;
        public int tiempo_seg;
        public int pistas_usadas;
        public int puntaje_obtenido;
        public string fecha_hora;
    }

    /// <summary>7. LOGRO</summary>
    [Serializable]
    public class TablaLogro
    {
        public int id_logro;
        public string clave;              // auxiliar: identificador usado por el código
        public string nombre;
        public string descripcion;
        public string condicion;
        public string icono;
    }

    /// <summary>8. JUGADOR_LOGRO</summary>
    [Serializable]
    public class TablaJugadorLogro
    {
        public int id_jugador;
        public int id_logro;
        public string fecha_obtencion;
    }

    /// <summary>9. ACERTIJO_DIARIO</summary>
    [Serializable]
    public class TablaAcertijoDiario
    {
        public int id_diario;
        public int id_acertijo;
        public string fecha;
        public int puntaje_bonus;
    }
}
