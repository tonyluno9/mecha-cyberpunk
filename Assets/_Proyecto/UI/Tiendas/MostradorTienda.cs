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
    /// Mostrador de una tienda (T-146, D-tony-81): un <see cref="IInteractuable"/> donde el jugador a pie pulsa E para abrir la
    /// <see cref="PantallaTienda"/>. Crea su <see cref="TiendaBasica"/> y la registra como <see cref="ITienda"/> mientras está activo.
    /// Abierto según el <see cref="ILugar"/> registrado con el mismo <see cref="IdLugar"/> (sin ese lugar, abierto): el horario es de Alan.
    /// Solo lista los objetos del catálogo cuya oferta vende la tienda.
    /// </summary>
    public class MostradorTienda : MonoBehaviour, IInteractuable
    {
        [SerializeField] private DatosTienda datos;
        [SerializeField] private CatalogoObjetos catalogo;
        [SerializeField] private PantallaTienda pantalla;
        [Tooltip("Tipos de objeto que este mostrador compra (T-147). Vacía: la pestaña VENDER no aparece.")]
        [SerializeField] private TipoObjeto[] compra = new TipoObjeto[0];
        [Tooltip("True: con un favor de DESCUENTO, la siguiente compra de este mostrador cuesta menos (T-187; solo el mercado negro).")]
        [SerializeField] private bool aceptaFavorDescuento;
        [Tooltip("{0} = hora a la que abre (HH:MM).")]
        [SerializeField] private string formatoCerrado = "CERRADO · abre {0}";
        [SerializeField] private string textoCerrado = "CERRADO";

        private readonly List<DatosObjeto> enVenta = new List<DatosObjeto>();
        private TiendaBasica tienda;
        private ControladorAPie controlCongelado;
        private bool registrado;
        private bool abierta;

        public ITienda Tienda => tienda;
        /// <summary>True mientras la pantalla de la tienda está abierta.</summary>
        public bool PantallaAbierta => abierta;
        /// <summary>Los objetos que lista la pestaña COMPRAR.</summary>
        public IReadOnlyList<DatosObjeto> EnVenta => enVenta;
        /// <summary>Los tipos de objeto que el mostrador compra al jugador.</summary>
        public IReadOnlyList<TipoObjeto> Compra => compra;

        public bool AceptaFavorDescuento => aceptaFavorDescuento;

        public Vector3 Posicion => transform.position;

        public bool PuedeUsar => !abierta && Decidir(out _);

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
            if (datos == null || catalogo == null || pantalla == null)
            {
                Debug.LogError("[UI] Faltan referencias en MostradorTienda de " + name, this);
                enabled = false;
                return;
            }
            tienda = new TiendaBasica(datos);
            foreach (DatosOferta oferta in tienda.Ofertas)
            {
                DatosObjeto objeto = ObjetoDeOferta(oferta);
                if (objeto != null)
                {
                    enVenta.Add(objeto);
                }
            }
        }

        private void OnEnable()
        {
            Registro<IInteractuable>.Agregar(this);
            Registro<ITienda>.Agregar(tienda);
            registrado = true;
        }

        private void OnDisable()
        {
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                Registro<ITienda>.Quitar(tienda);
                registrado = false;
            }
            if (abierta)
            {
                Cerrar(true);
            }
        }

        public void Usar()
        {
            if (!PuedeUsar)
            {
                return;
            }
            abierta = true;
            pantalla.Mostrar(tienda, enVenta, AlSalir, compra, aceptaFavorDescuento);
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            ControladorAPie controlador = jugador != null ? jugador.GetComponent<ControladorAPie>() : null;
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
                controlCongelado = controlador;
            }
        }

        private void AlSalir()
        {
            Cerrar(false);
        }

        // La E que cierra es la misma que abre: el control vuelve un cuadro después para que no reabra la pantalla (D-tony-67).
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

        private DatosObjeto ObjetoDeOferta(DatosOferta oferta)
        {
            foreach (DatosObjeto objeto in catalogo.Objetos)
            {
                if (objeto != null && objeto.Oferta == oferta)
                {
                    return objeto;
                }
            }
            return null;
        }

        // Decide si se puede usar y qué dice el aviso. Texto vacío = no se ofrece.
        private bool Decidir(out string texto)
        {
            texto = string.Empty;
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (jugador == null || jugador.Modo != ModoJugador.APie)
            {
                return false;
            }
            ILugar lugar = BuscarLugar();
            if (lugar != null && !lugar.EstaAbierto)
            {
                HorarioLugar horario = lugar.Horario;
                bool hayHora = !horario.SiempreAbierto && horario.Abre != horario.Cierra;
                texto = hayHora ? string.Format(formatoCerrado, HorarioLugar.TextoHora(horario.Abre)) : textoCerrado;
                return false;
            }
            texto = tienda.Nombre;
            return true;
        }

        private ILugar BuscarLugar()
        {
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                if (lugar != null && lugar.Id == datos.Lugar)
                {
                    return lugar;
                }
            }
            return null;
        }
    }
}
