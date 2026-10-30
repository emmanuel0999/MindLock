using System;
using Mindlock.Datos;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>
    /// PANTALLA 7 - Acertijo diario: pendiente, ya resuelto (con cuenta regresiva)
    /// o racha perdida. Recibe un ResultadoNivel cuando se acaba de resolver.
    /// </summary>
    public class PantallaDiario : Pantalla
    {
        Label lblFecha, lblCategoria, lblBonus, lblRachaPendiente, lblResueltoTitulo, lblRachaResuelto, lblCuenta;
        VisualElement estadoPendiente, estadoResuelto, avisoRachaPerdida, filaSemana, llamaResuelto;
        IVisualElementScheduledItem cuenta;

        static readonly string[] LetrasDias = { "L", "M", "X", "J", "V", "S", "D" };

        protected override void Configurar()
        {
            lblFecha = Q<Label>("lblFechaDiario");
            lblCategoria = Q<Label>("lblCategoriaDiario");
            lblBonus = Q<Label>("lblBonusDiario");
            lblRachaPendiente = Q<Label>("lblRachaPendiente");
            lblResueltoTitulo = Q<Label>("lblResueltoTitulo");
            lblRachaResuelto = Q<Label>("lblRachaResuelto");
            lblCuenta = Q<Label>("lblCuentaRegresiva");
            estadoPendiente = Q<VisualElement>("estadoPendiente");
            estadoResuelto = Q<VisualElement>("estadoResuelto");
            avisoRachaPerdida = Q<VisualElement>("avisoRachaPerdida");
            filaSemana = Q<VisualElement>("filaSemana");
            llamaResuelto = Q<VisualElement>("llamaResuelto");
            UIUtil.OcultarBarras(Q<ScrollView>("scrollDiario"));

            Boton("botonVolverDiario", () => ui.IrA(PantallaId.Mapa));
            Boton("botonDiarioMapa", () => ui.IrA(PantallaId.Mapa));
            Boton("botonJugarDiario", () => ui.IrA(PantallaId.Juego, ParametrosJuego.DeDiario()));

            cuenta = Raiz.schedule.Execute(ActualizarCuenta).Every(1000);
            cuenta.Pause();
        }

        public override void AlMostrar(object datos)
        {
            var j = Sesion.Jugador;
            if (j == null) { ui.IrA(PantallaId.Perfil); return; }

            var resultado = datos as ResultadoNivel;
            var diario = Servicios.Diario.ObtenerDiario(DateTime.Today);
            lblFecha.text = Formato.Fecha(diario.Fecha);
            lblCategoria.text = "Categoría: " + Formato.NombreCategoria(diario.Acertijo.Categoria);
            lblBonus.text = "+" + diario.PuntosBonus + " pts";

            bool resuelto = Servicios.Diario.ResueltoHoy(j.Id);
            bool perdida = Servicios.Diario.RachaPerdida(j.Id);
            int racha = Servicios.Diario.RachaActual(j.Id);

            UIUtil.Mostrar(estadoPendiente, !resuelto);
            UIUtil.Mostrar(estadoResuelto, resuelto);

            if (!resuelto)
            {
                UIUtil.Mostrar(avisoRachaPerdida, perdida);
                lblRachaPendiente.text = racha > 0 ? Formato.Racha(racha) : "Empieza tu racha hoy";
            }
            else
            {
                lblResueltoTitulo.text = resultado != null
                    ? "¡Resuelto! +" + Formato.Numero(resultado.Puntaje) + " pts"
                    : "¡Ya resolviste el de hoy!";

                if (resultado != null && racha > 1)
                {
                    // La racha "sube" con una pequeña animación
                    lblRachaResuelto.text = Formato.Racha(racha - 1);
                    Raiz.schedule.Execute(() =>
                    {
                        lblRachaResuelto.text = Formato.Racha(racha);
                        llamaResuelto.AddToClassList("llama-racha--grande");
                    }).StartingIn(700);
                    Raiz.schedule.Execute(() => llamaResuelto.RemoveFromClassList("llama-racha--grande")).StartingIn(1100);
                }
                else
                {
                    lblRachaResuelto.text = Formato.Racha(racha);
                }
            }

            ConstruirSemana(j);
            ActualizarCuenta();
            cuenta.Resume();
        }

        public override void AlOcultar()
        {
            cuenta.Pause();
        }

        void ConstruirSemana(Jugador j)
        {
            filaSemana.Clear();
            bool[] dias = Servicios.Diario.DiasSemana(j.Id, DateTime.Today);
            int hoy = ((int)DateTime.Today.DayOfWeek + 6) % 7;

            for (int i = 0; i < 7; i++)
            {
                var dia = UIUtil.Caja("dia");
                dia.Add(UIUtil.Texto(LetrasDias[i], "dia__letra"));
                var circulo = UIUtil.Caja("dia__circulo");
                if (dias[i])
                {
                    circulo.AddToClassList("dia__circulo--hecho");
                    circulo.Add(Iconos.Crear("llama", "ico-sm", "color-llama", Iconos.Relleno));
                }
                if (i == hoy) circulo.AddToClassList("dia__circulo--hoy");
                dia.Add(circulo);
                filaSemana.Add(dia);
            }
        }

        void ActualizarCuenta()
        {
            var falta = DateTime.Today.AddDays(1) - DateTime.Now;
            UIUtil.PonerTexto(lblCuenta, Formato.TiempoLargo(falta));
        }
    }
}
