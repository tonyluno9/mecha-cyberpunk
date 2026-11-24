using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;

namespace MechaCyberpunk.UI.Tiendas
{
    /// <summary>
    /// Máquina expendedora de la calle (T-685): con E abre el menú de tienda con las bebidas y botanas. Usa la misma
    /// <see cref="PantallaTienda"/> que los mostradores (comprar con Enter, salir con Esc). Congela el control mientras
    /// está abierta. Siempre abierta, a toda hora. La arma <see cref="ColocadorExpendedoras"/> en tiempo de ejecución.
    /// </summary>
    public class Expendedora : MonoBehaviour, IInteractuable
    {
        private const float AlturaAviso = 1.0f;

        /// <summary>T-1662 (T44): se avisa cada vez que alguien compra algo en una máquina (la historia lo usa en el paso del hambre).</summary>
        public static event System.Action<Expendedora> Compra;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEventos() => Compra = null;

        private readonly List<DatosObjeto> enVenta = new List<DatosObjeto>();
        private TiendaBasica tienda;
        private PantallaTienda pantalla;
        private ControladorAPie controlCongelado;
        private bool abierta;
        private bool registrada;

        public ITienda Tienda => tienda;
        public bool PantallaAbierta => abierta;
        public IReadOnlyList<DatosObjeto> EnVenta => enVenta;
        public Vector3 Posicion => transform.position + Vector3.up * AlturaAviso;
        public string Texto => tienda != null ? tienda.Nombre : "Expendedora";
        public bool PuedeUsar => !abierta && Registro<IJugador>.Actual is JugadorAPie jugador && jugador.Modo == ModoJugador.APie;

        /// <summary>Prepara la máquina con su tienda, su catálogo y la pantalla. Lo llama el colocador.</summary>
        public void Iniciar(DatosTienda datos, CatalogoObjetos catalogo, PantallaTienda pantallaTienda)
        {
            tienda = new TiendaBasica(datos);
            tienda.Comprado += _ => Compra?.Invoke(this);
            pantalla = pantallaTienda;
            enVenta.Clear();
            foreach (DatosOferta oferta in tienda.Ofertas)
            {
                foreach (DatosObjeto objeto in catalogo.Objetos)
                {
                    if (objeto != null && objeto.Oferta == oferta)
                    {
                        enVenta.Add(objeto);
                        break;
                    }
                }
            }
            if (isActiveAndEnabled)
            {
                Registrar();
            }
        }

        private void OnEnable()
        {
            if (tienda != null)
            {
                Registrar();
            }
        }

        private void OnDisable()
        {
            if (registrada)
            {
                Registro<IInteractuable>.Quitar(this);
                registrada = false;
            }
            if (abierta)
            {
                Cerrar(true);
            }
        }

        private void Registrar()
        {
            if (!registrada)
            {
                Registro<IInteractuable>.Agregar(this);
                registrada = true;
            }
        }

        public void Usar()
        {
            if (!PuedeUsar || pantalla == null)
            {
                return;
            }
            abierta = true;
            pantalla.Mostrar(tienda, enVenta, AlSalir);
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            ControladorAPie controlador = jugador != null ? jugador.GetComponent<ControladorAPie>() : null;
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
                controlCongelado = controlador;
            }
        }

        private void AlSalir() => Cerrar(false);

        /// <summary>T90: cierra al empezar una cinemática, con el control devuelto en el acto. True si estaba abierta.</summary>
        public bool CerrarPorCinematica()
        {
            if (!abierta)
            {
                return false;
            }
            Cerrar(true);
            return true;
        }

        // La E que cierra es la misma que abre: el control vuelve un cuadro después (como MostradorTienda).
        private void Cerrar(bool inmediato)
        {
            if (pantalla != null)
            {
                pantalla.Ocultar();
            }
            abierta = false;
            if (!inmediato && isActiveAndEnabled)
            {
                StartCoroutine(DevolverControlDespues());
            }
            else
            {
                DevolverControl();
            }
        }

        private IEnumerator DevolverControlDespues()
        {
            yield return null;
            DevolverControl();
        }

        private void DevolverControl()
        {
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
        }
    }
}
