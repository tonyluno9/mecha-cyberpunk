using System;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Mecha.Kits
{
    /// <summary>
    /// Con la tecla G, dentro de una pelea, gasta un kit de reparación del inventario y sube la vida del mecha de forma pareja
    /// (T-148, D-tony-83). Solo responde con el jugador piloteando y con el control activo (no en el conteo ni en KO).
    /// Sin <see cref="ServicioInventario"/> en la escena no hace nada.
    /// </summary>
    [RequireComponent(typeof(VidaMecha))]
    [RequireComponent(typeof(PeleadorMecha))]
    public class UsoKitReparacion : MonoBehaviour
    {
        [SerializeField] private ConfigKitReparacion config;
        [Tooltip("El objeto del inventario que se gasta (el kit de reparación).")]
        [SerializeField] private DatosObjeto objetoKit;
        [SerializeField] private InputAction usarKit = new InputAction("UsarKit", InputActionType.Button, "<Keyboard>/g");

        private VidaMecha vida;
        private PeleadorMecha peleador;
        private bool enPelea;
        private int usados;
        private bool curando;
        private float curaTotal;
        private float curaRestante;

        public bool EnPelea => enPelea;
        public int Usados => usados;
        public bool Curando => curando;
        public int Tope => config != null ? config.TopePorPelea : 0;
        public ConfigKitReparacion Config => config;

        /// <summary>Cuántos kits lleva el jugador, o -1 si no hay inventario en la escena.</summary>
        public int KitsEnInventario
        {
            get
            {
                ServicioInventario inventario = Registro<ServicioInventario>.Actual;
                return inventario != null && objetoKit != null ? inventario.Cantidad(objetoKit) : -1;
            }
        }

        /// <summary>Avisa cuando cambia algo que muestra la pantalla: usos, curación o pelea.</summary>
        public event Action Cambio;

        private void Awake()
        {
            vida = GetComponent<VidaMecha>();
            peleador = GetComponent<PeleadorMecha>();
            if (config == null || objetoKit == null)
            {
                Debug.LogError("[Mecha] Faltan referencias en UsoKitReparacion de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            EventosPelea.PeleaIniciada += AlIniciarPelea;
            EventosPelea.PeleaTerminada += AlTerminarPelea;
            usarKit.performed += AlUsar;
            usarKit.Enable();
        }

        private void OnDisable()
        {
            EventosPelea.PeleaIniciada -= AlIniciarPelea;
            EventosPelea.PeleaTerminada -= AlTerminarPelea;
            usarKit.performed -= AlUsar;
            usarKit.Disable();
        }

        private void AlIniciarPelea()
        {
            enPelea = true;
            usados = 0;
            Cortar();
            Cambio?.Invoke();
        }

        private void AlTerminarPelea(ResultadoPelea resultado)
        {
            enPelea = false;
            Cortar();
            Cambio?.Invoke();
        }

        private void Cortar()
        {
            curando = false;
            curaRestante = 0f;
        }

        private void AlUsar(InputAction.CallbackContext contexto) => IntentarUsar();

        /// <summary>Intenta usar un kit. Devuelve true si lo gastó.</summary>
        public bool IntentarUsar()
        {
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            if (inventario == null || !peleador.PermisoPiloto || !peleador.ControlActivo)
            {
                return false;
            }
            MotivoKit motivo = ReglaKitReparacion.Decidir(enPelea, vida.EstaKO, vida.Actual, vida.Maxima, inventario.Cantidad(objetoKit),
                usados, config.TopePorPelea, curando);
            if (motivo != MotivoKit.Puede || !inventario.Quitar(objetoKit, 1))
            {
                return false;
            }
            usados++;
            curando = true;
            curaTotal = ReglaKitReparacion.Curacion(vida.Actual, vida.Maxima, config.FraccionCuracion);
            curaRestante = curaTotal;
            Cambio?.Invoke();
            return true;
        }

        private void Update()
        {
            if (!curando)
            {
                return;
            }
            if (vida.EstaKO)
            {
                Cortar();
                Cambio?.Invoke();
                return;
            }
            float paso = ReglaKitReparacion.CuracionDelCuadro(curaTotal, curaRestante, config.SegundosCuracion, Time.deltaTime);
            curaRestante -= paso;
            if (paso > 0f)
            {
                vida.FijarVida(vida.Actual + paso);
            }
            if (curaRestante <= 0f)
            {
                curando = false;
                Cambio?.Invoke();
            }
        }
    }
}
