using System;
using System.Collections.Generic;
using Mindlock.Datos;
using UnityEngine.UIElements;

namespace Mindlock.UI
{
    /// <summary>
    /// Ventanas de vidas y de tienda (modalVidas y modalTienda de Raiz.uxml).
    /// Las usan el mapa, el juego y el resultado; avisa con "Cambio" cuando cambian vidas, pistas o monedas.
    /// </summary>
    public class PanelEconomia
    {
        readonly UIManager ui;
        readonly VisualElement modalVidas, modalTienda;
        readonly List<VisualElement> corazones = new List<VisualElement>();
        readonly Label lblVidasTitulo, lblVidasTiempo, lblVidaAnuncio, lblVidaMonedas, lblVidasSaldo;
        readonly Label lblTiendaMonedas, lblTiendaVidasEstado, lblTiendaPistasEstado, lblTiendaAnuncio, lblTiendaAnunciosHoy;
        readonly Button botonVidaAnuncio, botonVidaMonedas, botonEsperar, botonTiendaVidas, botonTiendaPista, botonTiendaAnuncio;
        readonly IVisualElementScheduledItem reloj;

        Action alTenerVida, alCancelar;

        /// <summary>Se llama cuando cambian las vidas, las pistas o las monedas del jugador.</summary>
        public event Action Cambio;

        public bool Visible { get { return UIUtil.EstaVisible(modalVidas) || UIUtil.EstaVisible(modalTienda); } }

        static IEconomiaService Economia { get { return Servicios.Economia; } }
        static Jugador Jugador { get { return Sesion.Jugador; } }

        public PanelEconomia(UIManager manager, VisualElement raiz)
        {
            ui = manager;
            modalVidas = raiz.Q("modalVidas");
            modalTienda = raiz.Q("modalTienda");
            modalVidas.Q("filaCorazones").Query(className: "corazon").ForEach(corazones.Add);
            lblVidasTitulo = modalVidas.Q<Label>("lblVidasTitulo");
            lblVidasTiempo = modalVidas.Q<Label>("lblModalVidasTiempo");
            lblVidaAnuncio = modalVidas.Q<Label>("lblVidaAnuncio");
            lblVidaMonedas = modalVidas.Q<Label>("lblVidaMonedas");
            lblVidasSaldo = modalVidas.Q<Label>("lblVidasSaldo");
            lblTiendaMonedas = modalTienda.Q<Label>("lblTiendaMonedas");
            lblTiendaVidasEstado = modalTienda.Q<Label>("lblTiendaVidasEstado");
            lblTiendaPistasEstado = modalTienda.Q<Label>("lblTiendaPistasEstado");
            lblTiendaAnuncio = modalTienda.Q<Label>("lblTiendaAnuncio");
            lblTiendaAnunciosHoy = modalTienda.Q<Label>("lblTiendaAnunciosHoy");
            botonVidaAnuncio = modalVidas.Q<Button>("botonVidaAnuncio");
            botonVidaMonedas = modalVidas.Q<Button>("botonVidaMonedas");
            botonEsperar = modalVidas.Q<Button>("botonEsperarVidas");
            botonTiendaVidas = modalTienda.Q<Button>("botonTiendaVidas");
            botonTiendaPista = modalTienda.Q<Button>("botonTiendaPista");
            botonTiendaAnuncio = modalTienda.Q<Button>("botonTiendaAnuncio");

            modalTienda.Q<Label>("lblPrecioVidas").text = Datos.Economia.PrecioLlenarVidas.ToString();
            modalTienda.Q<Label>("lblPrecioPista").text = Datos.Economia.PrecioPista.ToString();
            lblVidaMonedas.text = Idioma.F("Llenar vidas: {0} monedas", Datos.Economia.PrecioLlenarVidas);
            lblTiendaAnuncio.text = Idioma.F("Ver anuncio: +{0} monedas", Datos.Economia.MonedasPorAnuncio);

            botonVidaAnuncio.clicked += VerAnuncioPorVida;
            botonVidaMonedas.clicked += LlenarConMonedas;
            botonEsperar.clicked += PulsarEsperar;
            modalVidas.Q<Button>("botonCerrarVidas").clicked += () => CerrarVidas(false);
            modalTienda.Q<Button>("botonCerrarTienda").clicked += () => UIUtil.CerrarModal(modalTienda);
            botonTiendaVidas.clicked += () => { if (Economia.LlenarVidasConMonedas(Jugador.Id)) Avisar(Idioma.T("¡Vidas llenas!"), "corazon"); };
            botonTiendaPista.clicked += () => { if (Economia.ComprarPista(Jugador.Id)) Avisar(Idioma.T("¡Compraste 1 pista!"), "bombilla"); };
            botonTiendaAnuncio.clicked += VerAnuncioPorMonedas;

            // Mientras una ventana está abierta, el tiempo de la próxima vida avanza cada segundo
            reloj = modalVidas.schedule.Execute(() =>
            {
                if (UIUtil.EstaVisible(modalVidas)) PintarVidas();
                if (UIUtil.EstaVisible(modalTienda)) PintarTienda();
            }).Every(1000);
        }

        // ------------------------------------------------------------------
        // Vidas
        // ------------------------------------------------------------------

        /// <summary>Juega si hay vidas; si no, abre la ventana y juega cuando consiga una.</summary>
        public void ConVida(Action jugar, Action cancelar = null)
        {
            if (Economia == null || Jugador == null || Economia.Vidas(Jugador.Id) > 0) { jugar(); return; }
            AbrirVidas(jugar, cancelar);
        }

        /// <summary>alTenerVida: qué hacer en cuanto haya una vida (null si solo se consultan).</summary>
        public void AbrirVidas(Action tenerVida = null, Action cancelar = null)
        {
            if (Economia == null || Jugador == null) return;
            alTenerVida = tenerVida;
            alCancelar = cancelar;
            PintarVidas();
            modalVidas.BringToFront();
            UIUtil.AbrirModal(modalVidas);
        }

        void PintarVidas()
        {
            if (Economia == null || Jugador == null) return;
            int vidas = Economia.Vidas(Jugador.Id);
            for (int i = 0; i < corazones.Count; i++) Iconos.Rellenar(corazones[i], i < vidas);

            bool llenas = vidas >= Datos.Economia.MaxVidas;
            lblVidasTitulo.text = Idioma.T(vidas == 0 ? "Te quedaste sin vidas" : llenas ? "Tus vidas están llenas" : "Tus vidas");
            lblVidasTiempo.text = llenas ? Idioma.T("Pierdes una vida con cada respuesta incorrecta")
                                : Idioma.F("Próxima vida en {0}", Formato.Cuenta(Economia.TiempoParaVida(Jugador.Id)));

            int restantes = Economia.AnunciosRestantesHoy(Jugador.Id);
            int vistos = Jugador.AnunciosParaVida;
            UIUtil.Mostrar(botonVidaAnuncio, Anuncios.Activos && !llenas);
            botonVidaAnuncio.SetEnabled(restantes > 0);
            lblVidaAnuncio.text = restantes <= 0 ? Idioma.T("Ya viste todos los anuncios de hoy")
                                : vistos == 0 ? Idioma.F("Ver {0} anuncios: +1 vida", Datos.Economia.AnunciosPorVida)
                                : Idioma.F("Ver otro anuncio ({0} de {1})", vistos, Datos.Economia.AnunciosPorVida);

            UIUtil.Mostrar(botonVidaMonedas, !llenas);
            botonVidaMonedas.SetEnabled(Jugador.Monedas >= Datos.Economia.PrecioLlenarVidas);
            lblVidasSaldo.text = Idioma.F("Tienes {0} monedas", Formato.Numero(Jugador.Monedas));

            botonEsperar.text = Idioma.T(alTenerVida != null && vidas > 0 ? "Seguir jugando" : alTenerVida != null ? "Esperar" : "Cerrar");
        }

        void VerAnuncioPorVida()
        {
            string premio = Idioma.F("1 vida (anuncio {0} de {1})", Jugador.AnunciosParaVida + 1, Datos.Economia.AnunciosPorVida);
            Anuncios.MostrarRecompensa(premio, vio =>
            {
                if (vio && Economia.Recompensar(Jugador.Id, Recompensa.Vida))
                {
                    bool gano = Jugador.AnunciosParaVida == 0;
                    Avisar(Idioma.T(gano ? "¡Ganaste 1 vida!" : "Te falta 1 anuncio para la vida"), "corazon");
                    if (gano && alTenerVida != null) { CerrarVidas(true); return; }
                }
                PintarVidas();
            });
        }

        void LlenarConMonedas()
        {
            if (!Economia.LlenarVidasConMonedas(Jugador.Id)) return;
            Avisar(Idioma.T("¡Vidas llenas!"), "corazon");
            if (alTenerVida != null) CerrarVidas(true);
            else PintarVidas();
        }

        void PulsarEsperar()
        {
            CerrarVidas(alTenerVida != null && Economia.Vidas(Jugador.Id) > 0);
        }

        void CerrarVidas(bool seguir)
        {
            var seguirAccion = alTenerVida;
            var cancelarAccion = alCancelar;
            alTenerVida = alCancelar = null;
            UIUtil.CerrarModal(modalVidas, () =>
            {
                if (seguir) { if (seguirAccion != null) seguirAccion(); }
                else if (cancelarAccion != null) cancelarAccion();
            });
        }

        // ------------------------------------------------------------------
        // Tienda
        // ------------------------------------------------------------------

        public void AbrirTienda()
        {
            if (Economia == null || Jugador == null) return;
            PintarTienda();
            modalTienda.BringToFront();
            UIUtil.AbrirModal(modalTienda);
        }

        void PintarTienda()
        {
            if (Economia == null || Jugador == null) return;
            int vidas = Economia.Vidas(Jugador.Id);
            int restantes = Economia.AnunciosRestantesHoy(Jugador.Id);
            lblTiendaMonedas.text = Formato.Numero(Jugador.Monedas);
            lblTiendaVidasEstado.text = Idioma.F("Tienes {0} de {1}", vidas, Datos.Economia.MaxVidas);
            botonTiendaVidas.SetEnabled(vidas < Datos.Economia.MaxVidas && Jugador.Monedas >= Datos.Economia.PrecioLlenarVidas);
            lblTiendaPistasEstado.text = Idioma.F("Tienes {0} de {1}", Jugador.PistasDisponibles, Reglas.MaxPistasAcumuladas);
            botonTiendaPista.SetEnabled(Jugador.PistasDisponibles < Reglas.MaxPistasAcumuladas && Jugador.Monedas >= Datos.Economia.PrecioPista);
            UIUtil.Mostrar(botonTiendaAnuncio, Anuncios.Activos);
            botonTiendaAnuncio.SetEnabled(restantes > 0);
            lblTiendaAnunciosHoy.text = restantes > 0 ? Idioma.F("Te quedan {0} anuncios con premio hoy", restantes) : Idioma.T("Ya viste todos los anuncios con premio de hoy");
            UIUtil.Mostrar(lblTiendaAnunciosHoy, Anuncios.Activos);
        }

        void VerAnuncioPorMonedas()
        {
            Anuncios.MostrarRecompensa(Idioma.F("{0} monedas", Datos.Economia.MonedasPorAnuncio), vio =>
            {
                if (vio && Economia.Recompensar(Jugador.Id, Recompensa.Monedas))
                    Avisar(Idioma.F("¡Ganaste {0} monedas!", Datos.Economia.MonedasPorAnuncio), "moneda");
                else PintarTienda();
            });
        }

        // ------------------------------------------------------------------

        void Avisar(string texto, string icono)
        {
            ui.MostrarAviso(texto, icono);
            if (UIUtil.EstaVisible(modalTienda)) PintarTienda();
            if (UIUtil.EstaVisible(modalVidas)) PintarVidas();
            if (Cambio != null) Cambio();
        }

        public void AvisarCambio()
        {
            if (Cambio != null) Cambio();
        }

        /// <summary>Botón "atrás": cierra la ventana abierta.</summary>
        public bool AlPulsarAtras()
        {
            if (UIUtil.EstaVisible(modalTienda)) { UIUtil.CerrarModal(modalTienda); return true; }
            if (UIUtil.EstaVisible(modalVidas)) { CerrarVidas(false); return true; }
            return false;
        }
    }
}
