using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>Mueve al jugador a pie: caminar, correr, saltar y mirar con el ratón, con cabeceo de cámara.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class ControladorAPie : MonoBehaviour
    {
        private const float PendienteMaxima = 45f;
        private const float EscalonMaximo = 0.35f;
        private const float UmbralPaso = 0.5f;
        // T-596: los pasos suenan por distancia recorrida (zancada), no por temporizador.
        private const float MetrosPorPasoCaminar = 1.4f;
        private const float MetrosPorPasoCorrer = 2f;
        private const float FraccionPrimerPaso = 0.5f;
        private const float AlcanceRayoPiso = 1.6f;
        private readonly ReglaZancada zancada = new ReglaZancada(MetrosPorPasoCaminar, MetrosPorPasoCorrer);
        private IdLugar lugarActual = IdLugar.Calle;
        private bool estabaMoviendo;

        /// <summary>Pasos dados (para pruebas) y el ID del último paso que sonó.</summary>
        public int PasosDados => zancada.Pasos;
        public string UltimoIdPaso { get; private set; }
        private const float SegundosMinimosAire = 0.25f;
        // T-442: la respiración se oye tras correr este rato seguido y no más seguido que cada tanto.
        private const float SegundosParaRespirar = 4f;
        private const float SegundosEntreRespiras = 10f;
        private float tiempoCorriendo;
        private float proximaRespira;
        private float tiempoAire;

        [SerializeField] private DatosPersonaje datos;
        [SerializeField] private ConfigControlesAPie config;
        [Tooltip("Cámara hija del jugador. Se inclina para mirar y se mece al caminar.")]
        [SerializeField] private Transform camara;
        [SerializeField] private InputActionReference mover;
        [SerializeField] private InputActionReference mirar;
        [SerializeField] private InputActionReference correr;
        [SerializeField] private InputActionReference saltar;
        [SerializeField] private InputActionReference atacar;
        [Tooltip("Clic derecho: apuntar (T-400). Opcional.")]
        [SerializeField] private InputActionReference apuntar;
        [Tooltip("Cámara de hombro: límites de inclinación y giro del cuerpo (T-400). Opcional.")]
        [SerializeField] private ConfigCamaraHombro configCamara;

        private CharacterController controlador;
        private readonly MovimientoAPie movimiento = new MovimientoAPie();
        private readonly MiradaAPie mirada = new MiradaAPie();
        private readonly SeguidorImplantes implantes = new SeguidorImplantes();
        private bool cursorBloqueado;
        private bool controlActivo = true;
        private bool enPiso;
        private float giroCamara;
        private bool apuntando;
        private float segundosMirarAlDisparo;

        /// <summary>Velocidad horizontal en el espacio del jugador (x lateral, y adelante, m/s).</summary>
        public Vector2 Velocidad => movimiento.VelocidadHorizontal;
        /// <summary>Velocidad vertical (m/s, positivo sube).</summary>
        public float VelocidadVertical => movimiento.VelocidadVertical;
        public bool EnPiso => enPiso;
        /// <summary>Inclinación vertical de la cámara (°, positivo mira hacia abajo).</summary>
        public float Inclinacion => mirada.Inclinacion;
        public Transform Camara => camara;
        /// <summary>Giro horizontal de la cámara de hombro (° en el mundo). Movimiento y apuntado son relativos a él.</summary>
        public float GiroCamara => giroCamara;
        /// <summary>True mientras se mantiene el clic derecho con el control activo.</summary>
        public bool Apuntando => apuntando;
        /// <summary>Multiplica la sensibilidad del ratón (T-416: baja con el zoom del sniper). 1 = normal.</summary>
        public float FactorSensibilidad { get; set; } = 1f;
        /// <summary>Multiplica la velocidad de caminar (T-421: agachado en cubierta, 0 al rodar). 1 = normal.</summary>
        public float FactorVelocidad { get; set; } = 1f;
        /// <summary>Multiplicador extra de velocidad que no pisa la cubierta (T-685: el café). 1 = normal.</summary>
        public float FactorExtraVelocidad { get; set; } = 1f;
        /// <summary>Velocidad horizontal extra en el mundo (m/s), la usa la rodada y el pegarse a la cubierta (T-421).</summary>
        public Vector3 DesplazamientoExterno { get; set; }
        /// <summary>Cuánto baja la cámara de hombro (m), por agacharse (T-421).</summary>
        public float AjusteAlturaCamara { get; set; }

        /// <summary>True mientras se mantiene SHIFT (correr). T-733: apuntando no se corre (correr es solo Loco_Correr, sin pistola).</summary>
        public bool PidiendoCorrer => correr != null && correr.action.IsPressed() && !apuntando;

        /// <summary>Con true el espacio no salta (agachado el espacio rueda, T-673).</summary>
        public bool SaltoBloqueado { get; set; }
        public bool CursorBloqueado => cursorBloqueado;
        public bool ControlActivo => controlActivo;

        /// <summary>T-991: true mientras la rueda de armas y objetos está abierta: el ratón no gira la cámara ni dispara (caminar sigue igual).</summary>
        public bool EntradaBloqueada { get; set; }

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            if (datos == null || config == null || camara == null || mover == null || mirar == null
                || correr == null || saltar == null || atacar == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en ControladorAPie de " + name, this);
                enabled = false;
                return;
            }
            controlador.stepOffset = EscalonMaximo;
            controlador.slopeLimit = PendienteMaxima;
            controlador.minMoveDistance = 0f;
            giroCamara = transform.eulerAngles.y;
            if (GetComponent<CubiertaYRodada>() == null)
            {
                gameObject.AddComponent<CubiertaYRodada>();
            }
        }

        // El piso: el lugar donde está el jugador (interior) y el material del suelo bajo los pies.
        private string IdPasoDelPiso()
        {
            string nombre = null;
            bool montable = false;
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit golpe, AlcanceRayoPiso, ~0, QueryTriggerInteraction.Ignore))
            {
                montable = golpe.collider.GetComponentInParent<IMontable>() != null;
                nombre = golpe.collider.sharedMaterial != null ? golpe.collider.sharedMaterial.name : null;
                if (string.IsNullOrEmpty(nombre))
                {
                    Renderer r = golpe.collider.GetComponent<Renderer>();
                    nombre = r != null && r.sharedMaterial != null ? r.sharedMaterial.name : golpe.collider.name;
                }
            }
            return ReglaSuperficiePaso.IdPaso(lugarActual, nombre, montable);
        }

        private void AlEntrarLugar(IdLugar lugar) => lugarActual = lugar;

        private void AlSalirLugar(IdLugar lugar)
        {
            if (lugarActual == lugar) lugarActual = IdLugar.Calle;
        }

        private void OnEnable()
        {
            EventosLugares.JugadorEntro += AlEntrarLugar;
            EventosLugares.JugadorSalio += AlSalirLugar;
            mover.action.Enable();
            mirar.action.Enable();
            correr.action.Enable();
            saltar.action.Enable();
            atacar.action.Enable();
            apuntar?.action.Enable();
            implantes.Activar();
            AplicarCursor(true);
        }

        private void OnDisable()
        {
            EventosLugares.JugadorEntro -= AlEntrarLugar;
            EventosLugares.JugadorSalio -= AlSalirLugar;
            mover.action.Disable();
            mirar.action.Disable();
            correr.action.Disable();
            saltar.action.Disable();
            atacar.action.Disable();
            apuntar?.action.Disable();
            implantes.Desactivar();
        }

        /// <summary>Congela o libera el control (lo usan el KO y el carro). Al congelar se detiene el movimiento.</summary>
        public void ActivarControl(bool activo)
        {
            controlActivo = activo;
            if (!activo)
            {
                apuntando = false;
                movimiento.Reiniciar();
                // T-728: el CharacterController guarda la velocidad del último Move. Sin esto el visual del jugador seguía "caminando" quieto durante una
                // cinemática (lee controlador.velocity): un Move en cero la deja en cero y el muñeco queda en reposo.
                if (controlador != null && controlador.enabled && controlador.gameObject.activeInHierarchy)
                {
                    controlador.Move(Vector3.zero);
                }
            }
        }

        /// <summary>Gira la cámara (°) sin el ratón: lo usa el apuntado asistido (T-424). Respeta los límites de inclinación.</summary>
        public void Empujar(float giro, float inclinacion)
        {
            giroCamara = Mathf.Repeat(giroCamara + giro, 360f);
            mirada.Sumar(inclinacion);
            mirada.Limitar(configCamara != null ? configCamara.InclinacionMinima : -40f, configCamara != null ? configCamara.InclinacionMaxima : 60f);
        }

        /// <summary>Detiene el movimiento y la mirada pendiente; se usa tras teletransportar.</summary>
        public void ReiniciarMovimiento()
        {
            movimiento.Reiniciar();
            giroCamara = transform.eulerAngles.y;
        }

        private void Update()
        {
            if (!controlActivo)
            {
                return;
            }

            float dt = Time.deltaTime;

            if (cursorBloqueado && !EntradaBloqueada)
            {
                float minima = configCamara != null ? configCamara.InclinacionMinima : -40f;
                float maxima = configCamara != null ? configCamara.InclinacionMaxima : 60f;
                // T-411: las opciones multiplican la sensibilidad (encima del factor del zoom) y pueden invertir Y.
                IOpciones opciones = Registro<IOpciones>.Actual;
                Vector2 delta = mirar.action.ReadValue<Vector2>();
                if (opciones != null && opciones.InvertirY)
                {
                    delta.y = -delta.y;
                }
                float sensibilidad = config.SensibilidadRaton * FactorSensibilidad * (opciones != null ? opciones.Sensibilidad : 1f);
                float giro = mirada.Actualizar(delta, sensibilidad, Mathf.Max(Mathf.Abs(minima), maxima));
                giroCamara = Mathf.Repeat(giroCamara + giro, 360f);
                mirada.Limitar(minima, maxima);
            }
            apuntando = apuntar != null && apuntar.action.IsPressed();

            Vector2 entrada = mover.action.ReadValue<Vector2>();
            bool quiereCorrer = correr.action.IsPressed() && !apuntando;
            Vector2 velocidad = movimiento.ActualizarHorizontal(entrada, quiereCorrer,
                enPiso, dt, datos.VelocidadCaminar, EfectosAPie.VelocidadCorrer(datos, implantes.Efectos), config.AceleracionPiso, config.FrenadoPiso, config.ControlAire);
            bool quiereSaltar = !SaltoBloqueado && saltar.action.WasPressedThisFrame();
            float vertical = movimiento.ActualizarVertical(enPiso, quiereSaltar, dt,
                EfectosAPie.AlturaSalto(datos, implantes.Efectos), config.Gravedad);
            if (enPiso && quiereSaltar && vertical > 0f)
            {
                Sonidos.Reproducir(VozJugador.Id("JUG_SALTO"));
            }

            // Tercera persona: el movimiento es relativo a la cámara; el cuerpo gira hacia donde camina, o hacia donde mira si apunta.
            Quaternion marco = Quaternion.Euler(0f, giroCamara, 0f);
            Vector3 horizontal = marco * new Vector3(velocidad.x, 0f, velocidad.y);
            horizontal = horizontal * FactorVelocidad * FactorExtraVelocidad + DesplazamientoExterno;
            GirarCuerpo(dt, horizontal);
            controlador.Move(horizontal * dt + Vector3.up * vertical);
            enPiso = controlador.isGrounded;
            if (enPiso && tiempoAire > SegundosMinimosAire)
            {
                Sonidos.ReproducirEn("JUG_CAER", transform.position);
            }
            tiempoAire = enPiso ? 0f : tiempoAire + dt;
            bool corriendo = enPiso && velocidad.sqrMagnitude > UmbralPaso * UmbralPaso && quiereCorrer;
            tiempoCorriendo = corriendo ? tiempoCorriendo + dt : 0f;
            if (tiempoCorriendo >= SegundosParaRespirar && Time.time >= proximaRespira)
            {
                proximaRespira = Time.time + SegundosEntreRespiras;
                tiempoCorriendo = 0f;
                Sonidos.Reproducir(VozJugador.Id("JUG_CORRER_RESPIRA"));
            }
            bool moviendo = enPiso && velocidad.sqrMagnitude > UmbralPaso * UmbralPaso;
            if (moviendo && !estabaMoviendo)
            {
                zancada.Reiniciar(MetrosPorPasoCaminar * FraccionPrimerPaso);
            }
            estabaMoviendo = moviendo;
            if (moviendo && zancada.Avanzar(new Vector2(horizontal.x, horizontal.z).magnitude * dt, quiereCorrer, enPiso) > 0)
            {
                UltimoIdPaso = IdPasoDelPiso();
                Sonidos.ReproducirEn(UltimoIdPaso, transform.position);
            }
        }

        /// <summary>T-943: al disparar sin apuntar, el cuerpo gira un momento hacia donde mira la cámara.</summary>
        public void MirarAlDisparar()
        {
            segundosMirarAlDisparo = configCamara != null ? configCamara.SegundosGiroAlDisparar : 0f;
        }

        private void GirarCuerpo(float dt, Vector3 horizontal)
        {
            float giroMaximo = (configCamara != null ? configCamara.GiroCuerpo : 720f) * dt;
            float objetivo = transform.eulerAngles.y;
            bool mirandoAlDisparo = segundosMirarAlDisparo > 0f;
            segundosMirarAlDisparo = Mathf.Max(0f, segundosMirarAlDisparo - dt);
            if (apuntando || mirandoAlDisparo)
            {
                objetivo = giroCamara;
            }
            else if (horizontal.sqrMagnitude > 0.01f)
            {
                objetivo = Mathf.Atan2(horizontal.x, horizontal.z) * Mathf.Rad2Deg;
            }
            else
            {
                return;
            }
            float angulo = Mathf.MoveTowardsAngle(transform.eulerAngles.y, objetivo, giroMaximo);
            transform.rotation = Quaternion.Euler(0f, angulo, 0f);
        }

        // T-410: Esc ya no suelta el cursor; lo abre la pausa (el kit de UI muestra y bloquea el cursor al abrir y cerrar menús).
        private void AplicarCursor(bool bloquear)
        {
            cursorBloqueado = bloquear;
            Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !bloquear;
        }
    }
}
