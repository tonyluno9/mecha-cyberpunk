using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// Usa los consumibles del inventario (T-685). Teclas: Tab elige el siguiente objeto que lleva, F usa el elegido y 6, 7, 8, 9 y 0
    /// usan directo el 1.o a 5.o consumible del catálogo (bebida, papitas, venda, café, batería). Solo a pie, con control y vivo.
    /// Cura (de golpe o repartida), da segundos sin recibir daño, recupera escudo o acelera, según los datos del objeto.
    /// La agrega <see cref="JugadorAPie"/> al nacer.
    /// </summary>
    public class ConsumiblesJugador : MonoBehaviour
    {
        private readonly List<DatosConsumible> disponibles = new List<DatosConsumible>();
        private readonly List<DatosConsumible> catalogo = new List<DatosConsumible>();
        private static readonly string[] TeclasDirectas = { "<Keyboard>/6", "<Keyboard>/7", "<Keyboard>/8", "<Keyboard>/9", "<Keyboard>/0" };

        // Las teclas avisan por evento (performed) y Update atiende lo pendiente: así no se pierde una pulsación corta.
        private readonly InputAction siguiente = new InputAction("ConsumibleSiguiente", InputActionType.Button, "<Keyboard>/tab");
        private readonly InputAction usar = new InputAction("ConsumibleUsar", InputActionType.Button, "<Keyboard>/f");
        private readonly InputAction[] directas = new InputAction[TeclasDirectas.Length];
        private bool pidioSiguiente;
        private bool pidioUsar;
        private int pidioDirecta = -1;
        private readonly CuracionGradual gradual = new CuracionGradual();

        private JugadorAPie jugador;
        private ControladorAPie controlador;
        private VidaAPie vida;
        private ServicioInventario inventario;
        private int indice;
        private float velocidadHasta = -1f;
        private float factorVelocidad = 1f;

        /// <summary>Los consumibles que lleva ahora, en orden del catálogo.</summary>
        public IReadOnlyList<DatosConsumible> Disponibles => disponibles;

        /// <summary>El objeto elegido con Tab, o null si no lleva ninguno.</summary>
        public DatosConsumible Seleccionado => indice >= 0 && indice < disponibles.Count ? disponibles[indice] : null;

        /// <summary>Segundos que le quedan a la velocidad extra del café.</summary>
        public float VelocidadRestante => Mathf.Max(0f, velocidadHasta - Time.time);

        /// <summary>True mientras una curación gradual (la venda) sigue dando vida.</summary>
        public bool Curando => gradual.Activa;

        /// <summary>Se avisa al usar un objeto, con el objeto.</summary>
        public event Action<DatosConsumible> Usado;

        /// <summary>Se avisa cuando cambia el objeto elegido o lo que lleva.</summary>
        public event Action Cambio;

        private void Awake()
        {
            jugador = GetComponent<JugadorAPie>();
            controlador = GetComponent<ControladorAPie>();
            vida = GetComponent<VidaAPie>();
            siguiente.performed += _ => pidioSiguiente = true;
            usar.performed += _ => pidioUsar = true;
            for (int i = 0; i < directas.Length; i++)
            {
                int numero = i;
                directas[i] = new InputAction("ConsumibleDirecto" + (i + 1), InputActionType.Button, TeclasDirectas[i]);
                directas[i].performed += _ => pidioDirecta = numero;
            }
        }

        private void OnEnable()
        {
            siguiente.Enable();
            usar.Enable();
            foreach (InputAction accion in directas)
            {
                accion.Enable();
            }
        }

        private void OnDisable()
        {
            siguiente.Disable();
            usar.Disable();
            foreach (InputAction accion in directas)
            {
                accion.Disable();
            }
            Desuscribir();
            RestablecerVelocidad();
        }

        private void Desuscribir()
        {
            if (inventario != null)
            {
                inventario.Cambio -= Refrescar;
                inventario = null;
            }
        }

        private void Update()
        {
            if (inventario == null)
            {
                inventario = Registro<ServicioInventario>.Actual;
                if (inventario != null)
                {
                    inventario.Cambio += Refrescar;
                    Refrescar();
                }
            }
            float dt = Time.deltaTime;
            if (gradual.Activa && vida != null)
            {
                vida.Curar(gradual.Avanzar(dt));
            }
            if (velocidadHasta >= 0f && Time.time >= velocidadHasta)
            {
                RestablecerVelocidad();
            }
            bool siguientePedido = pidioSiguiente;
            bool usarPedido = pidioUsar;
            int directaPedida = pidioDirecta;
            pidioSiguiente = false;
            pidioUsar = false;
            pidioDirecta = -1;
            if (!PuedeActuar())
            {
                return;
            }
            if (siguientePedido)
            {
                indice = ReglaConsumibles.Siguiente(indice, disponibles.Count);
                Cambio?.Invoke();
            }
            if (usarPedido)
            {
                Usar(Seleccionado);
            }
            if (directaPedida >= 0 && directaPedida < catalogo.Count)
            {
                Usar(catalogo[directaPedida]);
            }
        }

        private bool PuedeActuar()
        {
            return jugador != null && jugador.Modo == ModoJugador.APie && controlador != null && controlador.ControlActivo
                && vida != null && !vida.EstaKO;
        }

        private void Refrescar()
        {
            catalogo.Clear();
            disponibles.Clear();
            if (inventario != null && inventario.Catalogo != null)
            {
                foreach (DatosObjeto objeto in inventario.Catalogo.Objetos)
                {
                    if (objeto is DatosConsumible consumible)
                    {
                        catalogo.Add(consumible);
                        if (inventario.Cantidad(consumible) > 0)
                        {
                            disponibles.Add(consumible);
                        }
                    }
                }
            }
            indice = ReglaConsumibles.Acomodar(indice, disponibles.Count);
            Cambio?.Invoke();
        }

        /// <summary>True si el objeto serviría ahora: no se gasta una cura con la vida llena.</summary>
        public bool Sirve(DatosConsumible c)
        {
            if (c == null || vida == null)
            {
                return false;
            }
            bool soloCura = c.Curacion > 0f && c.SegundosSinDanio <= 0f && c.Escudo <= 0f && c.SegundosVelocidad <= 0f;
            if (soloCura && vida.Actual >= vida.Maxima)
            {
                return false;
            }
            bool soloEscudo = c.Escudo > 0f && c.Curacion <= 0f && c.SegundosSinDanio <= 0f && c.SegundosVelocidad <= 0f;
            if (soloEscudo && vida.Escudo != null && vida.Escudo.Actual >= vida.Escudo.Maximo)
            {
                return false;
            }
            return true;
        }

        /// <summary>Usa un consumible (descuenta uno del inventario). False si no lo lleva, no sirve ahora o no se puede actuar.</summary>
        public bool Usar(DatosConsumible c)
        {
            if (c == null || inventario == null || !PuedeActuar() || inventario.Cantidad(c) <= 0 || !Sirve(c))
            {
                return false;
            }
            if (!inventario.Quitar(c, 1))
            {
                return false;
            }
            if (c.Curacion > 0f)
            {
                if (c.SegundosCuracion > 0f)
                {
                    gradual.Iniciar(c.Curacion, c.SegundosCuracion);
                }
                else
                {
                    vida.Curar(c.Curacion);
                }
            }
            vida.DarInvulnerabilidad(c.SegundosSinDanio);
            if (c.Escudo > 0f)
            {
                vida.Escudo?.Recuperar(c.Escudo);
            }
            if (c.SegundosVelocidad > 0f && c.FactorVelocidad > 1f)
            {
                factorVelocidad = Mathf.Max(factorVelocidad, c.FactorVelocidad);
                velocidadHasta = Mathf.Max(velocidadHasta, Time.time + c.SegundosVelocidad);
                controlador.FactorExtraVelocidad = factorVelocidad;
            }
            Sonidos.Reproducir("UI_ACEPTAR");
            Usado?.Invoke(c);
            return true;
        }

        private void RestablecerVelocidad()
        {
            velocidadHasta = -1f;
            factorVelocidad = 1f;
            if (controlador != null)
            {
                controlador.FactorExtraVelocidad = 1f;
            }
        }
    }
}
