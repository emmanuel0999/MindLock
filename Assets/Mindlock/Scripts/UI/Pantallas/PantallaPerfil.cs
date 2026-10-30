using Mindlock.Datos;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>PANTALLA 1 - Inicio y selección / creación de perfil.</summary>
    public class PantallaPerfil : Pantalla
    {
        VisualElement seccionPerfiles, listaPerfiles, estadoVacio, modalCrear;
        TextField campoNombre;
        Label mensajeError, contador;

        protected override void Configurar()
        {
            seccionPerfiles = Q<VisualElement>("seccionPerfiles");
            listaPerfiles = Q<VisualElement>("listaPerfiles");
            estadoVacio = Q<VisualElement>("estadoVacio");
            modalCrear = Q<VisualElement>("modalCrear");
            campoNombre = Q<TextField>("campoNombre");
            mensajeError = Q<Label>("mensajeError");
            contador = Q<Label>("contadorCaracteres");
            UIUtil.OcultarBarras(Q<ScrollView>("scrollPerfiles"));

            Boton("botonCrearPerfil", AbrirModal);
            Boton("botonCrearPrimero", AbrirModal);
            Boton("botonCerrarModal", CerrarModal);
            Boton("botonCancelarModal", CerrarModal);
            Boton("botonConfirmarCrear", Crear);

            campoNombre.maxLength = Reglas.MaxLargoNombre;
            UIUtil.Placeholder(campoNombre, "Ej. Ana");
            campoNombre.RegisterValueChangedCallback(_ =>
            {
                ActualizarContador();
                MostrarError(null);
            });
            campoNombre.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) Crear();
            });
        }

        public override void AlMostrar(object datos)
        {
            UIUtil.CerrarModalInmediato(modalCrear);
            Refrescar();
        }

        public override bool AlPulsarAtras()
        {
            if (UIUtil.EstaVisible(modalCrear)) CerrarModal();
            return true;
        }

        void Refrescar()
        {
            listaPerfiles.Clear();
            var perfiles = Servicios.Perfiles.ObtenerPerfiles();
            bool hay = perfiles.Count > 0;
            UIUtil.Mostrar(seccionPerfiles, hay);
            UIUtil.Mostrar(estadoVacio, !hay);
            foreach (var j in perfiles) listaPerfiles.Add(CrearTarjeta(j));
        }

        VisualElement CrearTarjeta(Jugador j)
        {
            var tarjeta = UIUtil.Boton(null, "tarjeta-perfil");
            tarjeta.Add(UIUtil.Avatar(j));

            var info = UIUtil.Caja("crecer", "ml-md");
            info.Add(UIUtil.Texto(j.Nombre, "tarjeta-perfil__nombre"));
            var datos = UIUtil.Caja("fila", "mt-xs");
            datos.Add(UIUtil.Dato("trofeo", Formato.Puntos(j.PuntajeTotal)));
            datos.Add(UIUtil.Dato("estrella", Servicios.Progreso.EstrellasTotales(j.Id) + "/" + (Reglas.TotalNiveles * Reglas.MaxEstrellas)));
            datos.Add(UIUtil.Dato("candado-abierto", Servicios.Progreso.NivelesCompletados(j.Id) + "/" + Reglas.TotalNiveles));
            info.Add(datos);
            tarjeta.Add(info);

            tarjeta.Add(Iconos.Crear("flecha-der", "ico-md", "color-sec"));
            UIUtil.IgnorarToquesHijos(tarjeta);
            tarjeta.clicked += () => Entrar(j);
            return tarjeta;
        }

        void Entrar(Jugador j)
        {
            Sesion.Jugador = j;
            ui.IrA(PantallaId.Mapa);
        }

        void AbrirModal()
        {
            campoNombre.SetValueWithoutNotify(string.Empty);
            campoNombre.RemoveFromClassList("campo-texto--error");
            UIUtil.Visible(mensajeError, false);
            ActualizarContador();
            UIUtil.AbrirModal(modalCrear);
            campoNombre.schedule.Execute(() => campoNombre.Focus()).StartingIn(220);
        }

        void CerrarModal()
        {
            UIUtil.CerrarModal(modalCrear);
        }

        void Crear()
        {
            if (!UIUtil.EstaVisible(modalCrear)) return;
            string nombre = (campoNombre.value ?? string.Empty).Trim();

            if (nombre.Length == 0) { MostrarError("Escribe un nombre"); return; }
            if (nombre.Length > Reglas.MaxLargoNombre) { MostrarError("Máximo " + Reglas.MaxLargoNombre + " caracteres"); return; }
            if (Servicios.Perfiles.ExisteNombre(nombre)) { MostrarError("Ya existe un perfil con ese nombre"); return; }

            var jugador = Servicios.Perfiles.CrearPerfil(nombre);
            UIUtil.CerrarModal(modalCrear, () => Entrar(jugador));
        }

        void MostrarError(string mensaje)
        {
            bool hay = !string.IsNullOrEmpty(mensaje);
            if (hay) mensajeError.text = mensaje;
            UIUtil.Visible(mensajeError, hay);
            campoNombre.EnableInClassList("campo-texto--error", hay);
            if (hay)
            {
                UIUtil.Sacudir(campoNombre);
                ui.Vibrar();
            }
        }

        void ActualizarContador()
        {
            int n = campoNombre.value == null ? 0 : campoNombre.value.Length;
            contador.text = n + "/" + Reglas.MaxLargoNombre;
            contador.EnableInClassList("texto-error", n >= Reglas.MaxLargoNombre);
        }
    }
}
