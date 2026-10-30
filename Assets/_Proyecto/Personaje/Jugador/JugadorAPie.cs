using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// El jugador a pie en el mundo: implementa IJugador en la raíz (que tiene el CharacterController)
    /// y se registra en Registro&lt;IJugador&gt; mientras está habilitado. También sube y baja de un
    /// <see cref="IMontable"/>: mientras va dentro apaga lo suyo y su raíz sigue al asiento.
    /// T-412: es <see cref="IGuardable"/> con la clave <c>jugador</c> (escena, posición, giro y arma en la mano). Dentro de un carro guarda el
    /// punto de bajada junto al carro. Al cargar en la misma escena se coloca ahí, sobre el piso, y la aparición por id ya no lo mueve.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class JugadorAPie : MonoBehaviour, IJugador, IGuardable
    {
        public const string Clave = "jugador";

        [Tooltip("Cámara hija del jugador.")]
        [SerializeField] private Camera camara;
        [Tooltip("Al cargar, desde qué altura sobre el punto guardado se busca el piso (m).")]
        [Min(0.1f)] [SerializeField] private float alturaBusquedaPiso = 1.5f;
        [Tooltip("Al cargar, hasta qué distancia hacia abajo se busca el piso (m).")]
        [Min(0.1f)] [SerializeField] private float distanciaBusquedaPiso = 4f;

        private CharacterController controlador;
        private ControladorAPie controladorAPie;
        private ArmasAPie armas;
        private AudioListener oyente;
        private GameObject modelo;
        private Camera camaraActual;
        private bool avisoDuplicadoDado;

        public Transform Raiz => transform;
        public ModoJugador Modo { get; private set; } = ModoJugador.APie;
        public Camera Camara => camaraActual != null ? camaraActual : camara;

        /// <summary>El montable en el que va el jugador, o null si va a pie.</summary>
        public IMontable MontableActual { get; private set; }

        /// <summary>Se dispara al cambiar de modo, con el modo nuevo.</summary>
        public event Action<ModoJugador> ModoCambio;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            controladorAPie = GetComponent<ControladorAPie>();
            armas = GetComponent<ArmasAPie>();
            if (camara == null)
            {
                Debug.LogError("[Personaje] Falta la cámara en JugadorAPie de " + name, this);
                enabled = false;
                return;
            }
            oyente = camara.GetComponent<AudioListener>();
            // Tercera persona (T-400): el modelo se oculta en el carro. Las manos de primera persona quedan apagadas en el prefab.
            Animator animador = GetComponentInChildren<Animator>(true);
            modelo = animador != null ? animador.gameObject : null;
            camaraActual = camara;
        }

        private void OnEnable()
        {
            if (Registro<IJugador>.Cantidad > 0 && !avisoDuplicadoDado)
            {
                avisoDuplicadoDado = true;
                Debug.LogWarning("[Personaje] Ya hay un IJugador registrado; solo debe haber uno (" + name + ").", this);
            }
            Registro<IJugador>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IJugador>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
        }

        public string ClaveGuardado => Clave;

        public string Guardar()
        {
            Vector3 posicion = transform.position;
            Vector3 frente = transform.forward;
            if (MontableActual != null)
            {
                // En el carro: el punto de bajada junto a la puerta, mirando hacia donde va el carro.
                posicion = MontableActual.PuntoSubida;
                if (MontableActual.Asiento != null)
                {
                    frente = MontableActual.Asiento.forward;
                }
            }
            frente.y = 0f;
            var datos = new PosicionGuardada
            {
                escena = SceneManager.GetActiveScene().path,
                posicion = posicion,
                giro = frente.sqrMagnitude > 0f ? Quaternion.LookRotation(frente.normalized, Vector3.up).eulerAngles.y : 0f,
                arma = armas != null ? armas.ArmaActiva.ToString() : ArmaAPie.Punos.ToString(),
            };
            return JsonUtility.ToJson(datos);
        }

        public void Cargar(string datos)
        {
            PosicionGuardada leida = null;
            try
            {
                leida = JsonUtility.FromJson<PosicionGuardada>(datos);
            }
            catch (Exception)
            {
                leida = null;
            }
            if (leida == null)
            {
                Debug.LogWarning("[Personaje] La posición guardada del jugador no se entiende; no se mueve.", this);
                return;
            }
            // Otra escena: la posición no sirve aquí.
            if (leida.escena != SceneManager.GetActiveScene().path || MontableActual != null)
            {
                return;
            }
            Pose pose = new Pose(SobreElPiso(leida.posicion), Quaternion.Euler(0f, leida.giro, 0f));
            AparicionJugador aparicion = GetComponent<AparicionJugador>();
            if (aparicion != null)
            {
                aparicion.Cancelar();
            }
            Colocar(pose);
            bool hayArma = Enum.TryParse(leida.arma, out ArmaAPie arma);
            if (isActiveAndEnabled)
            {
                StartCoroutine(TerminarCarga(pose, hayArma, arma));
            }
        }

        // Un cuadro después ya cargó el inventario: pone el arma y vuelve a colocarlo por si algo lo movió al arrancar.
        private IEnumerator TerminarCarga(Pose pose, bool hayArma, ArmaAPie arma)
        {
            yield return null;
            if (MontableActual == null)
            {
                Colocar(pose);
            }
            if (hayArma && armas != null)
            {
                armas.Equipar(arma);
            }
        }

        // El piso más cercano bajo el punto (sin contar al jugador); si no hay, el punto tal cual.
        private Vector3 SobreElPiso(Vector3 punto)
        {
            RaycastHit[] golpes = Physics.RaycastAll(punto + Vector3.up * alturaBusquedaPiso, Vector3.down,
                alturaBusquedaPiso + distanciaBusquedaPiso, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            Vector3 resultado = punto;
            foreach (RaycastHit golpe in golpes)
            {
                if (golpe.collider.transform.root == transform.root || golpe.distance >= mejor)
                {
                    continue;
                }
                mejor = golpe.distance;
                resultado = golpe.point;
            }
            return resultado;
        }

        private void LateUpdate()
        {
            if (MontableActual == null || MontableActual.Asiento == null)
            {
                return;
            }
            Transform asiento = MontableActual.Asiento;
            transform.SetPositionAndRotation(asiento.position, asiento.rotation);
        }

        public void Colocar(Pose pose)
        {
            if (MontableActual != null)
            {
                // Dentro de un montable el CharacterController sigue apagado: solo se mueve el transform.
                transform.SetPositionAndRotation(pose.position, pose.rotation);
                return;
            }
            controlador.enabled = false;
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            controlador.enabled = true;
            if (controladorAPie != null)
            {
                controladorAPie.ReiniciarMovimiento();
            }
        }

        /// <summary>Sube al montable si puede. Apaga el control a pie, la cámara y el oído propios y toma los del montable.</summary>
        public bool Subir(IMontable montable)
        {
            if (montable == null || MontableActual != null || Modo != ModoJugador.APie)
            {
                return false;
            }
            // El oído propio se apaga antes de que el montable prenda el suyo: Unity avisa si hay dos prendidos a la vez.
            bool oyenteEstabaPrendido = oyente != null && oyente.enabled;
            if (oyenteEstabaPrendido)
            {
                oyente.enabled = false;
            }
            if (!montable.IntentarSubir(this))
            {
                if (oyenteEstabaPrendido)
                {
                    oyente.enabled = true;
                }
                return false;
            }

            MontableActual = montable;
            controladorAPie.ActivarControl(false);
            controladorAPie.enabled = false;
            if (armas != null)
            {
                armas.enabled = false;
            }
            controlador.enabled = false;
            camara.enabled = false;
            if (oyente != null)
            {
                oyente.enabled = false;
            }
            if (modelo != null)
            {
                modelo.SetActive(false);
            }
            camaraActual = montable.Camara;
            Modo = montable.Modo;
            Debug.Log("[Personaje] Sube a " + montable.Nombre + " (" + Modo + ")");
            ModoCambio?.Invoke(Modo);
            return true;
        }

        /// <summary>Baja del montable si puede. Se coloca en la pose de salida y recupera lo suyo.</summary>
        public bool Bajar()
        {
            if (MontableActual == null)
            {
                return false;
            }
            IMontable montable = MontableActual;
            if (!montable.IntentarBajar(out Pose salida))
            {
                return false;
            }

            MontableActual = null;
            Colocar(salida);
            controladorAPie.enabled = true;
            controladorAPie.ActivarControl(true);
            if (armas != null)
            {
                armas.enabled = true;
            }
            camara.enabled = true;
            if (oyente != null)
            {
                oyente.enabled = true;
            }
            if (modelo != null)
            {
                modelo.SetActive(true);
            }
            camaraActual = camara;
            Modo = ModoJugador.APie;
            Debug.Log("[Personaje] Baja de " + montable.Nombre);
            ModoCambio?.Invoke(Modo);
            return true;
        }
    }
}
