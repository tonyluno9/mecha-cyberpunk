using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Misiones;
using UnityEngine;

namespace MechaCyberpunk.UI.Clinica
{
    /// <summary>
    /// Mostrador de la clínica (T-193, D-tony-81): un <see cref="IInteractuable"/> donde el jugador a pie pulsa E para abrir la
    /// <see cref="PantallaClinica"/>. Abierto según el <see cref="ILugar"/> con <see cref="IdLugar.Clinica"/> (sin ese lugar, abierto).
    /// Al confirmar instala y cobra con <see cref="ServicioImplantes"/>, funde a negro, adelanta el reloj, el doc dice una línea
    /// y el HUD avisa. Sin <see cref="PantallaFundido"/> en la escena instala sin fundido.
    /// </summary>
    public class MostradorClinica : MonoBehaviour, IInteractuable
    {
        [SerializeField] private ConfigPantallaClinica config;
        [SerializeField] private PantallaClinica pantalla;

        private readonly CongelaControlAPie congela = new CongelaControlAPie();
        private bool registrado;
        private bool abierta;
        private bool operando;

        /// <summary>True mientras la pantalla de la clínica está abierta.</summary>
        public bool PantallaAbierta => abierta;

        /// <summary>True desde que se confirma hasta que termina la operación.</summary>
        public bool Operando => operando;

        public PantallaClinica Pantalla => pantalla;

        public Vector3 Posicion => transform.position;

        public bool PuedeUsar => !abierta && !operando && Decidir(out _);

        /// <summary>T-990: EL DOC está presente, así que abre la clínica sin mirar el horario de la puerta (sí respeta abierta y operando).</summary>
        public bool PuedeAbrirDesdeDoc => !abierta && !operando && PuedeAbrirDocAhora();

        public string Texto
        {
            get
            {
                Decidir(out string texto);
                return texto;
            }
        }

        private void Awake()
        {
            if (config == null || pantalla == null)
            {
                Debug.LogError("[UI] Faltan referencias en MostradorClinica de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Registro<IInteractuable>.Agregar(this);
            registrado = true;
        }

        private void OnDisable()
        {
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                registrado = false;
            }
            if (abierta)
            {
                Cerrar(true);
            }
            if (operando)
            {
                StopAllCoroutines();
                operando = false;
                PantallaFundido.Actual?.Fijar(0f);
                congela.Liberar();
            }
        }

        public void Usar()
        {
            if (!PuedeUsar)
            {
                return;
            }
            Abrir();
        }

        /// <summary>T-990: lo que hace la E frente a EL DOC; abre la pantalla aunque la clínica esté "cerrada" por horario.</summary>
        public void AbrirDesdeDoc()
        {
            if (!PuedeAbrirDesdeDoc)
            {
                return;
            }
            Abrir();
        }

        private void Abrir()
        {
            abierta = true;
            congela.Congelar();
            // T-78: en la primera visita (el suero) solo se puede instalar las PIERNAS, gratis, y no se sale sin instalarlas.
            bool primeraVisita = PrimeraVisitaClinica.Pendiente;
            pantalla.RestringirA(primeraVisita ? EspacioImplante.Piernas : (EspacioImplante?)null);
            pantalla.Mostrar(Registro<ServicioImplantes>.Actual, AlInstalar, AlSalir);
            // T-1017: con Tab, las prótesis de ataque del Doc (brazo láser, hombrera de misiles) en la pantalla de tienda.
            MechaCyberpunk.Personaje.CatalogoProtesisAtaque catalogo = MechaCyberpunk.Personaje.CatalogoProtesisAtaque.Actual;
            bool hay = !primeraVisita && catalogo != null && catalogo.Tienda != null && Tienda() != null;
            pantalla.OfrecerAtaque(hay ? AbrirAtaque : (System.Action)null, TextoPestanaAtaque);
        }

        /// <summary>T-1017: el texto de la pestaña de prótesis de ataque en la clínica.</summary>
        public const string TextoPestanaAtaque = "[TAB] PRÓTESIS DE ATAQUE";

        private MechaCyberpunk.UI.Tiendas.PantallaTienda tiendaPantalla;

        private MechaCyberpunk.UI.Tiendas.PantallaTienda Tienda()
        {
            if (tiendaPantalla == null)
            {
                // T-1330: primero la que lleva el propio mostrador (así también sale en Ciudad, que no trae otra), si no la de la escena.
                tiendaPantalla = GetComponentInChildren<MechaCyberpunk.UI.Tiendas.PantallaTienda>(true);
                if (tiendaPantalla == null)
                {
                    tiendaPantalla = FindFirstObjectByType<MechaCyberpunk.UI.Tiendas.PantallaTienda>(FindObjectsInactive.Include);
                }
            }
            return tiendaPantalla;
        }

        /// <summary>T-1017: cambia de la clínica a la tienda de prótesis de ataque; Esc vuelve a la clínica.</summary>
        public void AbrirAtaque()
        {
            MechaCyberpunk.Personaje.CatalogoProtesisAtaque catalogo = MechaCyberpunk.Personaje.CatalogoProtesisAtaque.Actual;
            MechaCyberpunk.UI.Tiendas.PantallaTienda tienda = Tienda();
            if (catalogo == null || catalogo.Tienda == null || tienda == null)
            {
                return;
            }
            pantalla.Ocultar();
            var objetos = new System.Collections.Generic.List<MechaCyberpunk.Economia.DatosObjeto>();
            foreach (MechaCyberpunk.Personaje.DatosProtesisAtaque p in catalogo.Protesis)
            {
                if (p != null)
                {
                    objetos.Add(p);
                }
            }
            tienda.Mostrar(new MechaCyberpunk.Economia.TiendaBasica(catalogo.Tienda), objetos, () =>
            {
                tienda.Ocultar();
                if (abierta)
                {
                    Abrir();
                }
            });
        }

        private void AlSalir()
        {
            Cerrar(false);
        }

        /// <summary>
        /// T90: cierra la clínica (y la tienda de prótesis de ataque, si estaba en esa pestaña) al empezar una cinemática, con el control devuelto
        /// en el acto. No toca una operación en curso. True si estaba abierta.
        /// </summary>
        public bool CerrarPorCinematica()
        {
            if (!abierta)
            {
                return false;
            }
            if (tiendaPantalla != null && tiendaPantalla.Visible)
            {
                tiendaPantalla.Ocultar();
            }
            Cerrar(true);
            return true;
        }

        // La E que abre no cierra, y el control vuelve un cuadro después de salir para que ninguna tecla reabra la pantalla.
        private void Cerrar(bool inmediato)
        {
            pantalla.Ocultar();
            abierta = false;
            if (!inmediato && isActiveAndEnabled)
            {
                StartCoroutine(DevolverControlDespues());
            }
            else
            {
                congela.Liberar();
            }
        }

        private IEnumerator DevolverControlDespues()
        {
            yield return null;
            congela.Liberar();
        }

        private void AlInstalar(DatosImplante implante)
        {
            ServicioImplantes servicio = Registro<ServicioImplantes>.Actual;
            ResultadoImplante resultado = servicio.Instalar(implante);
            if (resultado != ResultadoImplante.Instalado)
            {
                pantalla.MostrarAviso(config.TextoAvisoSinDinero);
                return;
            }
            pantalla.Ocultar();
            abierta = false;
            StartCoroutine(Operar(implante));
        }

        private IEnumerator Operar(DatosImplante implante)
        {
            operando = true;
            // T-896: el ensamblado del implante se ve completo antes de irse a negro.
            while (pantalla.Destellando)
            {
                yield return null;
            }
            PantallaFundido fundido = PantallaFundido.Actual;
            if (fundido != null)
            {
                yield return fundido.Fundir(1f, config.SegundosFundidoNegro);
                yield return new WaitForSecondsRealtime(config.SegundosEnNegro);
            }
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            reloj?.AvanzarHasta(reloj.Hora + config.HorasOperacion);
            if (fundido != null)
            {
                yield return fundido.Fundir(0f, config.SegundosFundidoVuelta);
            }

            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            HudAPie hud = jugador != null ? jugador.GetComponentInChildren<HudAPie>(true) : null;
            hud?.MostrarAvisoImplante(implante.Nombre);
            PantallaSubtitulos subtitulos = FindFirstObjectByType<PantallaSubtitulos>();
            if (subtitulos != null)
            {
                subtitulos.Mostrar(config.TextoSubtitulo);
                yield return new WaitForSecondsRealtime(config.SegundosSubtitulo);
                subtitulos.Ocultar();
            }
            yield return null;
            congela.Liberar();
            operando = false;
        }

        // Decide si se puede usar y qué dice el aviso. Texto vacío = no se ofrece.
        private static bool PuedeAbrirDocAhora()
        {
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            return jugador != null && jugador.Modo == ModoJugador.APie && Registro<ServicioImplantes>.Actual != null;
        }

        private bool Decidir(out string texto)
        {
            texto = string.Empty;
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (jugador == null || jugador.Modo != ModoJugador.APie || Registro<ServicioImplantes>.Actual == null)
            {
                return false;
            }
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                if (lugar != null && lugar.Id == IdLugar.Clinica && !lugar.EstaAbierto)
                {
                    texto = config.TextoCerrado;
                    return false;
                }
            }
            texto = config.TextoMostrador;
            return true;
        }
    }
}
