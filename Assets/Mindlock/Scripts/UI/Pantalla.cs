using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    public enum PantallaId { Perfil, Mapa, Juego, Resultado, Diario, Logros, Opciones }

    /// <summary>
    /// Base de todas las pantallas. Cada pantalla es una clase C# normal
    /// (no MonoBehaviour) que controla su propio UXML.
    /// </summary>
    public abstract class Pantalla
    {
        protected UIManager ui;
        public VisualElement Raiz { get; private set; }

        /// <summary>True si la pantalla muestra la barra inferior (Niveles / Logros / Opciones).</summary>
        public virtual bool MuestraNavegacion { get { return false; } }

        public void Inicializar(UIManager manager, VisualElement raiz)
        {
            ui = manager;
            Raiz = raiz;
            Configurar();
        }

        /// <summary>Se llama una sola vez: buscar elementos y registrar eventos.</summary>
        protected abstract void Configurar();

        /// <summary>Se llama cada vez que la pantalla aparece. "datos" depende de la pantalla.</summary>
        public virtual void AlMostrar(object datos) { }

        /// <summary>Se llama cuando se sale de la pantalla (detener temporizadores, cerrar modales).</summary>
        public virtual void AlOcultar() { }

        /// <summary>Botón "atrás" de Android. Devuelve true si la pantalla lo manejó.</summary>
        public virtual bool AlPulsarAtras() { return false; }

        /// <summary>Busca un elemento por nombre y avisa en consola si no existe en el UXML.</summary>
        protected T Q<T>(string nombre) where T : VisualElement
        {
            var e = Raiz.Q<T>(nombre);
            if (e == null) Debug.LogError("[Mindlock] " + GetType().Name + ": no se encontró '" + nombre + "' en el UXML.");
            return e;
        }

        /// <summary>Atajo: busca un botón y le asigna la acción del clic.</summary>
        protected Button Boton(string nombre, System.Action alPulsar)
        {
            var b = Q<Button>(nombre);
            if (b != null && alPulsar != null) b.clicked += alPulsar;
            return b;
        }
    }
}
