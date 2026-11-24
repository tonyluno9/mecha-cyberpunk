using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
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
        private readonly LimitadorPasos limitadorPasos = new LimitadorPasos();
        private float alturaMaximaAire;

        // T-1505: caminar de verdad (tecla que alterna) y pasos por evento de animación (los clips de locomoción de PJ01 traen la pisada).
        private readonly InputAction caminarLento = new InputAction("CaminarLento", InputActionType.Button, ReglaCaminarLento.TeclaCaminar);
        private readonly LimitadorPasos limitadorPasosAnimados = new LimitadorPasos(SegundosMinimosEntrePasosAnimados);
        private const float SegundosMinimosEntrePasosAnimados = 0.12f;
        private ReceptorPasosPJ01 receptorPasos;
        private bool pasosPorAnimacion;
        private bool corriendoAhora;

        /// <summary>T-1505: true si el jugador va caminando de verdad (1.6 m/s) por la tecla de caminar.</summary>
        public bool CaminandoLento { get; private set; }

        /// <summary>T-1505: pasos que sonaron por evento de animación (para pruebas).</summary>
        public int PasosAnimados { get; private set; }

        /// <summary>T-1505: true mientras el clip activo lleva eventos de pisada: el sonido por distancia está apagado.</summary>
        public bool PasosPorAnimacion => pasosPorAnimacion;

        /// <summary>Pasos dados (para pruebas) y el ID del último paso que sonó.</summary>
        public int PasosDados => zancada.Pasos + PasosAnimados;
        public string UltimoIdPaso { get; private set; }
        /// <summary>T-1489: al aterrizar, con los metros que cayó (para el polvo).</summary>
        public event System.Action<float> Aterrizo;
        /// <summary>T-1489: en cada paso que suena, con la posición, el ID del paso y si corría (para la salpicadura de los charcos).</summary>
        public event System.Action<Vector3, string, bool> PasoDado;
        private const float SegundosMinimosAire = 0.25f;
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
        /// <summary>T-1324: true mientras se mantiene el clic derecho aunque no se pueda apuntar (arma sin balas).</summary>
        public bool QuiereApuntar { get; private set; }
        /// <summary>T-1324: lo pone PistolaAPie: con el arma sin balas en el cargador no se apunta (primero recarga).</summary>
        public bool ApuntarBloqueado { get; set; }
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
        /// <summary>T-1628: la entrada de movimiento (WASD) de este cuadro; cero si no hay acción. La usa quien se levanta al moverse (sentado).</summary>
        public Vector2 LeerMovimiento() => mover != null ? mover.action.ReadValue<Vector2>() : Vector2.zero;
        /// <summary>T-1327: True mientras un carro lo tiró y está en el suelo o levantándose (AtropelloJugador): no ataca ni salta ni camina.</summary>
        public bool Derribado { get; set; }

        // T-992: las habilidades de prótesis suman su multiplicador temporal encima de los implantes (1 si no hay o no están activas).
        private bool saltoSuper; // T-1535: el salto en curso lo dio el exo

        /// <summary>T-58: el salto en curso lo dio el exo con las piernas activas (sigue en true cuando suena Aterrizo).</summary>
        public bool SaltoSuperEnCurso => saltoSuper;

        private MechaCyberpunk.Personaje.Habilidades.HabilidadesProtesis habilidades;
        private MechaCyberpunk.Personaje.Habilidades.HabilidadesProtesis Habilidades =>
            habilidades != null ? habilidades : (habilidades = GetComponent<MechaCyberpunk.Personaje.Habilidades.HabilidadesProtesis>());

        /// <summary>T-991: true mientras la rueda de armas y objetos está abierta: el ratón no gira la cámara ni dispara (caminar sigue igual).</summary>
        public bool EntradaBloqueada { get; set; }
        /// <summary>S2: con true no se mueve al jugador ni se aplica gravedad (trepa o agarre de borde); la mirada sigue funcionando.</summary>
        public bool MovimientoSuspendido { get; set; }
        private Vector3 impulso;

        // T-1493: mientras el Animator juega una animación que bloquea (ReglaBloqueoAnimacion) no se camina, gira ni salta; con tope de tiempo.
        private VisualPersonaje visualBloqueo;
        private float segundosBloqueado;
        /// <summary>T-1493: true si ahora mismo una animación bloqueante (bajar de la moto, abrir puerta...) detiene el movimiento.</summary>
        public bool BloqueadoPorAnimacion { get; private set; }

        private bool ActualizarBloqueoAnimacion(float dt)
        {
            if (visualBloqueo == null)
            {
                visualBloqueo = GetComponentInChildren<VisualPersonaje>();
            }
            Animator a = visualBloqueo != null ? visualBloqueo.Animador : null;
            bool estadoBloquea = false;
            if (a != null && a.isActiveAndEnabled && a.runtimeAnimatorController != null)
            {
                estadoBloquea = ReglaBloqueoAnimacion.HashBloquea(a.GetCurrentAnimatorStateInfo(0).shortNameHash)
                    || (a.IsInTransition(0) && ReglaBloqueoAnimacion.HashBloquea(a.GetNextAnimatorStateInfo(0).shortNameHash));
            }
            segundosBloqueado = estadoBloquea ? segundosBloqueado + dt : 0f;
            BloqueadoPorAnimacion = ReglaBloqueoAnimacion.DebeBloquear(estadoBloquea, segundosBloqueado);
            return BloqueadoPorAnimacion;
        }

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
            if (GetComponent<EfectosPieJugador>() == null)
            {
                gameObject.AddComponent<EfectosPieJugador>(); // T-1489
            }
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
                // T-1473: se juntan todos los nombres (material físico, material del renderer, collider y objeto raíz) para buscar palabras clave del piso.
                Renderer r = golpe.collider.GetComponent<Renderer>();
                nombre = (golpe.collider.sharedMaterial != null ? golpe.collider.sharedMaterial.name : "") + "|"
                    + (r != null && r.sharedMaterial != null ? r.sharedMaterial.name : "") + "|"
                    + golpe.collider.name + "|" + golpe.collider.transform.root.name;
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
            caminarLento.Enable();
            implantes.Activar();
            AplicarCursor(true);
        }

        // T-voces-23nov: tras correr un buen rato jadea (JUG_RESPIRACION_CANSADO, bucle 2D con fundido de entrada y salida en el gestor).
        private readonly ReglaRespiracionCansado respiracion = new ReglaRespiracionCansado();
        private int bucleRespiracion = Sonidos.SinBucle;

        /// <summary>T-voces-23nov: true mientras suena la respiración de cansado.</summary>
        public bool Jadeando => respiracion.Sonando;

        private void ActualizarRespiracion(float dt, bool corriendo)
        {
            bool sonar = respiracion.Avanzar(dt, corriendo);
            if (sonar && bucleRespiracion == Sonidos.SinBucle)
            {
                bucleRespiracion = Sonidos.ReproducirBucle(VozJugador.RespiracionCansado, null);
            }
            else if (!sonar && bucleRespiracion != Sonidos.SinBucle)
            {
                PararRespiracion();
            }
        }

        private void PararRespiracion()
        {
            if (bucleRespiracion != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucleRespiracion);
                bucleRespiracion = Sonidos.SinBucle;
            }
        }

        private void OnDisable()
        {
            PararRespiracion();
            respiracion.Reiniciar();
            EventosLugares.JugadorEntro -= AlEntrarLugar;
            EventosLugares.JugadorSalio -= AlSalirLugar;
            mover.action.Disable();
            mirar.action.Disable();
            correr.action.Disable();
            saltar.action.Disable();
            atacar.action.Disable();
            apuntar?.action.Disable();
            caminarLento.Disable();
            implantes.Desactivar();
            SuscribirReceptor(null);
        }

        private void OnDestroy()
        {
            caminarLento.Dispose();
        }

        // T-1505: el visual puede cambiar de modelo: se vuelve a suscribir al receptor de pisadas del visual actual.
        private void SuscribirReceptor(ReceptorPasosPJ01 nuevo)
        {
            if (receptorPasos == nuevo)
            {
                return;
            }
            if (receptorPasos != null)
            {
                receptorPasos.Paso -= AlPasoAnimado;
            }
            receptorPasos = nuevo;
            if (receptorPasos != null)
            {
                receptorPasos.Paso += AlPasoAnimado;
            }
        }

        private void AlPasoAnimado(int pie)
        {
            if (!enPiso || !limitadorPasosAnimados.Permitir(Time.time))
            {
                return;
            }
            PasosAnimados++;
            UltimoIdPaso = IdPasoDelPiso();
            Sonidos.ReproducirEn(UltimoIdPaso, transform.position);
            PasoDado?.Invoke(transform.position, UltimoIdPaso, corriendoAhora);
        }

        /// <summary>Congela o libera el control (lo usan el KO y el carro). Al congelar se detiene el movimiento.</summary>
        public void ActivarControl(bool activo)
        {
            controlActivo = activo;
            if (!activo)
            {
                apuntando = false;
                QuiereApuntar = false;
                movimiento.Reiniciar();
                if (visualBloqueo != null)
                {
                    visualBloqueo.PreparandoSalto = false;
                }
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

        /// <summary>S2: termina la suspensión del movimiento con la velocidad reiniciada y, opcional, un empujón horizontal (m/s) que se apaga solo.</summary>
        public void ReanudarMovimiento(Vector3 empuje)
        {
            movimiento.Reiniciar();
            impulso = empuje;
            tiempoAire = 0f;
            MovimientoSuspendido = false;
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
            QuiereApuntar = apuntar != null && apuntar.action.IsPressed();
            apuntando = QuiereApuntar && !ApuntarBloqueado;

            if (MovimientoSuspendido)
            {
                return;
            }

            bool bloqueadoAnim = ActualizarBloqueoAnimacion(dt);
            Vector2 entrada = bloqueadoAnim ? Vector2.zero : mover.action.ReadValue<Vector2>();
            bool quiereCorrer = correr.action.IsPressed() && !apuntando && !bloqueadoAnim;
            CaminandoLento = ReglaCaminarLento.Alternar(CaminandoLento, caminarLento.WasPressedThisFrame());
            corriendoAhora = quiereCorrer;
            if (visualBloqueo == null)
            {
                visualBloqueo = GetComponentInChildren<VisualPersonaje>();
            }
            SuscribirReceptor(visualBloqueo != null ? visualBloqueo.ReceptorPasos : null);
            pasosPorAnimacion = receptorPasos != null && receptorPasos.ClipActivoTienePasos();
            Vector2 velocidad = movimiento.ActualizarHorizontal(entrada, quiereCorrer,
                enPiso, dt, ReglaCaminarLento.VelocidadCaminar(CaminandoLento, datos.VelocidadCaminar) * (Habilidades != null ? Habilidades.FactorCaminar : 1f),
                // T-1550: Shift trota a 4.5; con velocidad rápida (implante, habilidad o consumible) la carrera es de 6.25 m/s o más (PJ01_CorrerRapido).
                ReglaVelocidadRapida.VelocidadCorrer(datos.VelocidadCorrer, implantes.Efectos.multiplicadorCorrer, Habilidades != null ? Habilidades.FactorCorrer : 1f, FactorExtraVelocidad),
                config.AceleracionPiso, config.FrenadoPiso, config.ControlAire);
            bool quiereSaltar = !SaltoBloqueado && !bloqueadoAnim && saltar.action.WasPressedThisFrame();
            float vertical = movimiento.ActualizarVertical(enPiso, quiereSaltar, dt,
                EfectosAPie.AlturaSalto(datos, implantes.Efectos) * (Habilidades != null ? Habilidades.FactorSalto : 1f), config.Gravedad);
            // T-1550: el impulso sale 0.1 s después de pedir el salto (decisión de Tony del 20-nov: despega en el cuadro 4 del clip); la animación arranca al pedirlo (PreparandoSalto).
            if (visualBloqueo != null)
            {
                visualBloqueo.PreparandoSalto = movimiento.PreparandoSalto || movimiento.DespegoEsteFrame;
            }
            if (enPiso && movimiento.DespegoEsteFrame)
            {
                Sonidos.Reproducir(VozJugador.Id(VozJugador.Salto)); // T-voces-23nov: la voz grabada del salto (2D)
                saltoSuper = Habilidades != null && Habilidades.FactorSalto > 1f; // T-1535: con las piernas activas el salto es súper
                if (saltoSuper)
                {
                    Sonidos.ReproducirEn(MechaCyberpunk.Personaje.Habilidades.ReglaSonidoExo.SaltoSuper, transform.position);
                }
            }

            // Tercera persona: el movimiento es relativo a la cámara; el cuerpo gira hacia donde camina, o hacia donde mira si apunta.
            Quaternion marco = Quaternion.Euler(0f, giroCamara, 0f);
            Vector3 horizontal = marco * new Vector3(velocidad.x, 0f, velocidad.y);
            horizontal = horizontal * FactorVelocidad * FactorExtraVelocidad + DesplazamientoExterno + impulso;
            impulso = Vector3.MoveTowards(impulso, Vector3.zero, config.FrenadoPiso * config.ControlAire * dt);
            if (!bloqueadoAnim)
            {
                GirarCuerpo(dt, horizontal);
            }
            bool estabaEnPiso = enPiso;
            controlador.Move(horizontal * dt + Vector3.up * vertical);
            enPiso = controlador.isGrounded;
            if (!enPiso && estabaEnPiso && movimiento.VelocidadVertical <= 0f)
            {
                enPiso = PegarAlPiso();
            }
            if (!enPiso)
            {
                alturaMaximaAire = estabaEnPiso ? transform.position.y : Mathf.Max(alturaMaximaAire, transform.position.y);
            }
            if (enPiso && tiempoAire > SegundosMinimosAire)
            {
                Aterrizo?.Invoke(alturaMaximaAire - transform.position.y); // T-1489
            }
            if (enPiso && tiempoAire > SegundosMinimosAire && (saltoSuper || ReglaSonidosJugador.DebeSonarAterrizaje(alturaMaximaAire - transform.position.y)))
            {
                Sonidos.ReproducirEn(ReglaSonidosJugador.Aterrizaje, transform.position);
                // T-1658: sin explosión (EXP_ONDA) al caer con el implante de piernas; solo el golpe normal del aterrizaje.
            }
            if (enPiso && tiempoAire > SegundosMinimosAire)
            {
                saltoSuper = false;
            }
            else if (enPiso && tiempoAire > SegundosMinimosAire && limitadorPasosAnimados.Permitir(Time.time))
            {
                // T-1534: un salto corto (sin caída) también pisa: un solo sonido de paso del piso, en el cuadro en que toca (Salto_Aterrizaje: toca en 3).
                UltimoIdPaso = IdPasoDelPiso();
                Sonidos.ReproducirEn(UltimoIdPaso, transform.position);
                PasoDado?.Invoke(transform.position, UltimoIdPaso, corriendoAhora);
            }
            tiempoAire = enPiso ? 0f : tiempoAire + dt;
            // T55: sin la respiración al correr (JUG_CORRER_RESPIRA era un sonido de la primera versión).
            bool moviendo = enPiso && velocidad.sqrMagnitude > UmbralPaso * UmbralPaso;
            ActualizarRespiracion(dt, moviendo && quiereCorrer);
            if (moviendo && !estabaMoviendo)
            {
                zancada.Reiniciar(MetrosPorPasoCaminar * FraccionPrimerPaso);
            }
            estabaMoviendo = moviendo;
            if (moviendo && !pasosPorAnimacion && zancada.Avanzar(new Vector2(horizontal.x, horizontal.z).magnitude * dt, quiereCorrer, enPiso) > 0
                && limitadorPasos.Permitir(Time.time))
            {
                UltimoIdPaso = IdPasoDelPiso();
                Sonidos.ReproducirEn(UltimoIdPaso, transform.position);
                PasoDado?.Invoke(transform.position, UltimoIdPaso, quiereCorrer); // T-1489
            }
        }

        /// <summary>
        /// T-1327: bajando escalones o una rampa el CharacterController pierde el piso un cuadro (el escalón baja más de lo que cae en un cuadro): el jugador "flotaba"
        /// y el animador veía aire. Si venía en el piso y no saltó, se baja pegado al piso hasta EscalonMaximo. Devuelve si quedó en el piso.
        /// </summary>
        private bool PegarAlPiso()
        {
            float radio = controlador.radius * 0.9f;
            Vector3 centro = transform.TransformPoint(controlador.center);
            Vector3 origen = centro + Vector3.down * (controlador.height * 0.5f - controlador.radius);
            float alcance = EscalonMaximo + controlador.skinWidth;
            // Primero un rayo recto desde el centro del pie: al dejar un escalón, la esfera choca con la esquina del de arriba (normal inclinada) y no veía el de abajo.
            Vector3 pie = centro + Vector3.down * (controlador.height * 0.5f - controlador.skinWidth - 0.02f);
            if (Physics.Raycast(pie, Vector3.down, out RaycastHit rayo, alcance + 0.02f, ~0, QueryTriggerInteraction.Ignore)
                && rayo.collider.transform.root != transform.root && rayo.normal.y >= Mathf.Cos(PendienteMaxima * Mathf.Deg2Rad))
            {
                controlador.Move(Vector3.down * (rayo.distance + controlador.skinWidth));
                return controlador.isGrounded;
            }
            if (!Physics.SphereCast(origen + Vector3.up * controlador.radius * 0.1f, radio, Vector3.down, out RaycastHit golpe, alcance, ~0, QueryTriggerInteraction.Ignore)
                || golpe.collider.transform.root == transform.root
                || golpe.normal.y < Mathf.Cos(PendienteMaxima * Mathf.Deg2Rad))
            {
                return false;
            }
            controlador.Move(Vector3.down * Mathf.Max(0f, golpe.distance));
            return controlador.isGrounded;
        }

        /// <summary>T-943: al disparar sin apuntar, el cuerpo gira un momento hacia donde mira la cámara.</summary>
        public void MirarAlDisparar()
        {
            segundosMirarAlDisparo = configCamara != null ? configCamara.SegundosGiroAlDisparar : 0f;
        }

        /// <summary>T-1484: rapidez del suavizado del giro del cuerpo al caminar (1/s): 90° se completan al 75 % en 0.1 s.</summary>
        public const float SuavizadoGiroCuerpo = 14f;

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
            // T-1484: caminando el cuerpo gira con suavizado (arranca rápido y llega suave); apuntando o al disparar sigue a la cámara sin retraso.
            float angulo = apuntando || mirandoAlDisparo
                ? Mathf.MoveTowardsAngle(transform.eulerAngles.y, objetivo, giroMaximo)
                : ReglasAnimacion.GirarSuave(transform.eulerAngles.y, objetivo, dt, configCamara != null ? configCamara.GiroCuerpo : 720f, SuavizadoGiroCuerpo);
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
