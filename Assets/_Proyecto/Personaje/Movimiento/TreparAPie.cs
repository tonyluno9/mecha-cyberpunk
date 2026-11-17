using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// S2: el jugador a pie trepa escaleras y tubos (PuntoTrepar) y se agarra de bordes (contenedores, bardas, techos).
    /// Corre antes que ControladorAPie y, mientras trepa, le suspende el movimiento (la cámara sigue).
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(ControladorAPie))]
    [RequireComponent(typeof(CharacterController))]
    public class TreparAPie : MonoBehaviour
    {
        private const float UmbralAdelante = 0.5f;
        private const int MaxGolpes = 16;
        private const float DistanciaPisoEscalera = 0.2f;
        private const float AlturaRayoPiso = 0.1f;

        [SerializeField] private ConfigTrepar config;
        [SerializeField] private InputActionReference mover;
        [SerializeField] private InputActionReference saltar;

        private ControladorAPie controlador;
        private CharacterController cc;
        private VisualPersonaje visual;
        private readonly ReglaTrepar regla = new ReglaTrepar();
        private readonly RaycastHit[] golpes = new RaycastHit[MaxGolpes];
        private readonly Collider[] choques = new Collider[MaxGolpes];
        private PuntoTrepar puntoCercano;
        private Collider triggerCercano;
        // T-1203: holgura para seguir "dentro" del trigger de la escalera (m).
        private const float HolguraTrigger = 0.35f;
        private const float AlturaCentroCuerpo = 0.9f;
        private PuntoTrepar puntoActual;
        private Vector3 inicioAgarre;
        private Vector3 finAgarre;
        private float enfriamiento;
        private bool liberarPendiente;
        private Vector3 empujePendiente;
        // T-1204: bajar desde el techo; mientras no se suelte W, W camina hacia abajo por la escalera.
        private const float AlcanceBordeAdelante = 1.2f;
        private const float BajaSondaBorde = 1.2f;
        private const float RadioSondaBorde = 1f;
        private const float ToleranciaAlturaCima = 0.8f;
        private const float ToleranciaHorizontalCima = 1.2f;
        private const float MargenInicioBajada = 0.05f;
        private const float UmbralSoltarTecla = 0.1f;
        private readonly Collider[] sondaBorde = new Collider[8];
        private bool invertirHastaSoltar;

        /// <summary>True mientras sube por una escalera o tubo, o se agarra de un borde.</summary>
        public bool Trepando => regla.Estado != EstadoTrepar.Libre;
        public EstadoTrepar Estado => regla.Estado;

        /// <summary>T-1204: True mientras sube o baja una escalera de mano (no tubos ni agarres).</summary>
        public bool EnEscaleraDeMano => regla.Estado == EstadoTrepar.EnEscalera && puntoActual != null && puntoActual.Tipo == TipoTrepar.Escalera;
        /// <summary>T-1204: metros trepados desde el pie de la escalera (m).</summary>
        public float ProgresoEscalera => regla.Progreso;
        /// <summary>T-1204: True si la última entrada vertical efectiva en la escalera fue hacia abajo.</summary>
        public bool BajandoEscalera { get; private set; }
        /// <summary>T-1204: True mientras pasa de la cima de una escalera de mano al techo (el agarre de salida).</summary>
        public bool SaliendoArriba => regla.Estado == EstadoTrepar.Agarre && agarreDesdeEscalera;
        /// <summary>T-1204: avance de 0 a 1 del paso de la escalera al techo.</summary>
        public float ProgresoSalida => regla.ProgresoAgarre;
        /// <summary>T-1204: se engancha a una escalera de mano desde el piso (para el clip de entrar).</summary>
        public event System.Action EntroDesdeAbajo;
        /// <summary>T-1204: se soltó de una escalera de mano al llegar al piso bajando (para el clip de salir abajo).</summary>
        public event System.Action SalioAbajo;
        private bool agarreDesdeEscalera;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            cc = GetComponent<CharacterController>();
            visual = GetComponentInChildren<VisualPersonaje>(true);
            if (GetComponent<AnimacionEscalera>() == null)
            {
                gameObject.AddComponent<AnimacionEscalera>();
            }
            if (config == null || mover == null || saltar == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en TreparAPie de " + name, this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (Trepando)
            {
                Terminar(Vector3.zero);
                liberarPendiente = false;
                controlador.ReanudarMovimiento(Vector3.zero);
            }
            puntoCercano = null;
            triggerCercano = null;
        }

        private void OnTriggerEnter(Collider otro)
        {
            PuntoTrepar punto = otro.GetComponentInParent<PuntoTrepar>();
            if (punto != null)
            {
                puntoCercano = punto;
                triggerCercano = otro;
            }
        }

        private void OnTriggerExit(Collider otro)
        {
            PuntoTrepar punto = otro.GetComponentInParent<PuntoTrepar>();
            if (punto != null && punto == puntoCercano)
            {
                puntoCercano = null;
                triggerCercano = null;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (liberarPendiente)
            {
                liberarPendiente = false;
                controlador.ReanudarMovimiento(empujePendiente);
            }
            enfriamiento = Mathf.Max(0f, enfriamiento - dt);
            if (!controlador.ControlActivo)
            {
                if (Trepando)
                {
                    Terminar(Vector3.zero);
                }
                return;
            }

            Vector2 entrada = mover.action.ReadValue<Vector2>();
            bool pulsoSaltar = saltar.action.WasPressedThisFrame();
            switch (regla.Estado)
            {
                case EstadoTrepar.EnEscalera:
                    ActualizarEscalera(dt, entrada.y, pulsoSaltar);
                    break;
                case EstadoTrepar.Agarre:
                    ActualizarAgarre(dt);
                    break;
                default:
                    ActualizarLibre(entrada, pulsoSaltar);
                    break;
            }
        }

        // ---------- Escaleras y tubos ----------

        private void ActualizarLibre(Vector2 entrada, bool pulsoSaltar)
        {
            if (enfriamiento > 0f)
            {
                return;
            }
            if (puntoCercano != null && !SigueJuntoAEscalera())
            {
                // T-1203: al subir se apaga el CharacterController y Unity ya no avisa la salida del trigger; sin esto, caminar en el techo
                // hacia el mismo lado de la escalera volvía a engancharla y regresaba al jugador a la orilla.
                puntoCercano = null;
                triggerCercano = null;
            }
            if (puntoCercano != null && entrada.y > UmbralAdelante && PuedeEngancharse(puntoCercano))
            {
                EngancharEscalera(puntoCercano);
                return;
            }
            if (entrada.y > UmbralAdelante && controlador.EnPiso && IntentarBajarDesdeTecho())
            {
                return;
            }
            bool automatico = config.AgarreAutomaticoEnAire && !controlador.EnPiso && entrada.y > UmbralAdelante;
            bool manual = pulsoSaltar && !controlador.SaltoBloqueado;
            if ((manual || automatico) && IntentarAgarre(out Vector3 destino, out float segundos))
            {
                IniciarAgarre(destino, segundos);
            }
        }

        // T-1203: el jugador de verdad está en el trigger de la escalera (al pie, frente a la pared), no en el techo ni lejos.
        private bool SigueJuntoAEscalera()
        {
            if (triggerCercano == null || !triggerCercano.enabled || !triggerCercano.gameObject.activeInHierarchy)
            {
                return false;
            }
            Vector3 centro = transform.position + Vector3.up * AlturaCentroCuerpo;
            return (triggerCercano.ClosestPoint(centro) - centro).sqrMagnitude <= HolguraTrigger * HolguraTrigger;
        }

        // T-1204: busca una escalera cuya cima queda justo bajo la orilla delante del jugador y se engancha desde arriba.
        private bool IntentarBajarDesdeTecho()
        {
            Vector3 frente = transform.forward;
            frente.y = 0f;
            if (frente.sqrMagnitude <= 0f)
            {
                return false;
            }
            frente.Normalize();
            // La sonda busca el trigger de la escalera bajo la orilla de enfrente; el jugador tiene que estar en la salida de esa escalera (donde
            // deja la subida) o junto a su cima, a la altura del techo y de espaldas a la pared.
            Vector3 centro = transform.position + frente * AlcanceBordeAdelante + Vector3.down * BajaSondaBorde;
            int n = Physics.OverlapSphereNonAlloc(centro, RadioSondaBorde, sondaBorde, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                PuntoTrepar punto = sondaBorde[i].GetComponentInParent<PuntoTrepar>();
                if (punto == null || punto.Tipo != TipoTrepar.Escalera)
                {
                    continue;
                }
                if (Mathf.Abs(transform.position.y - punto.Salida.y) > ToleranciaAlturaCima)
                {
                    continue;
                }
                if (Mathf.Min(DistanciaPlano(punto.Salida), DistanciaPlano(punto.Cima)) > ToleranciaHorizontalCima
                    || Vector3.Angle(frente, -punto.Frente) > config.AnguloMaximoEnganche)
                {
                    continue;
                }
                puntoActual = punto;
                regla.IniciarEscalera(punto.AlturaSubida, config.VelocidadEscalera, punto.AlturaSubida - MargenInicioBajada);
                invertirHastaSoltar = true;
                Suspender();
                ColocarEnEscalera();
                return true;
            }
            return false;
        }

        private float DistanciaPlano(Vector3 punto)
        {
            Vector3 d = punto - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        private bool PuedeEngancharse(PuntoTrepar punto)
        {
            Vector3 frente = transform.forward;
            frente.y = 0f;
            return frente.sqrMagnitude > 0f && Vector3.Angle(frente, punto.Frente) <= config.AnguloMaximoEnganche;
        }

        private void EngancharEscalera(PuntoTrepar punto)
        {
            puntoActual = punto;
            float velocidad = punto.Tipo == TipoTrepar.Tubo ? config.VelocidadTubo : config.VelocidadEscalera;
            regla.IniciarEscalera(punto.AlturaSubida, velocidad, transform.position.y - punto.transform.position.y);
            Suspender();
            ColocarEnEscalera();
            if (punto.Tipo == TipoTrepar.Escalera)
            {
                EntroDesdeAbajo?.Invoke();
            }
        }

        private void ActualizarEscalera(float dt, float entradaVertical, bool pulsoSaltar)
        {
            bool enPiso = Physics.Raycast(transform.position + Vector3.up * AlturaRayoPiso, Vector3.down,
                AlturaRayoPiso + DistanciaPisoEscalera, ~0, QueryTriggerInteraction.Ignore);
            if (invertirHastaSoltar && entradaVertical <= UmbralSoltarTecla)
            {
                invertirHastaSoltar = false;
            }
            float efectiva = invertirHastaSoltar ? -entradaVertical : entradaVertical;
            if (efectiva < 0f)
            {
                BajandoEscalera = true;
            }
            else if (efectiva > 0f)
            {
                BajandoEscalera = false;
            }
            AccionTrepar accion = regla.AvanzarEscalera(dt, efectiva, enPiso, pulsoSaltar);
            switch (accion)
            {
                case AccionTrepar.Solto:
                    Vector3 empuje = pulsoSaltar ? -puntoActual.Frente * config.EmpujeSoltar : Vector3.zero;
                    bool alPiso = !pulsoSaltar && enPiso && puntoActual.Tipo == TipoTrepar.Escalera;
                    Terminar(empuje);
                    if (alPiso)
                    {
                        SalioAbajo?.Invoke();
                    }
                    return;
                case AccionTrepar.LlegoCima:
                    ColocarEnEscalera();
                    // T-1204: con el clip de salir arriba, el paso al techo dura lo que el clip.
                    ConfigAnimacionEscalera anim = ConfigAnimacionEscalera.Actual;
                    bool deMano = puntoActual.Tipo == TipoTrepar.Escalera;
                    float segundos = deMano && anim != null && anim.SalirArriba != null
                        ? anim.SalirArriba.length
                        : CalculoAgarre.Segundos(0f, config.SegundosAgarreBase, config.SegundosAgarrePorMetro);
                    IniciarAgarre(puntoActual.Salida, segundos);
                    agarreDesdeEscalera = deMano;
                    return;
            }
            ColocarEnEscalera();
        }

        private void ColocarEnEscalera()
        {
            transform.position = puntoActual.PieAbajo + Vector3.up * regla.Progreso;
            transform.rotation = Quaternion.LookRotation(puntoActual.Frente, Vector3.up);
        }

        // ---------- Agarre de bordes ----------

        private void IniciarAgarre(Vector3 destino, float segundos)
        {
            if (regla.Estado == EstadoTrepar.Libre)
            {
                Suspender();
            }
            inicioAgarre = transform.position;
            finAgarre = destino;
            regla.IniciarAgarre(segundos);
            Vector3 hacia = destino - inicioAgarre;
            hacia.y = 0f;
            if (hacia.sqrMagnitude > 0f && puntoActual == null)
            {
                transform.rotation = Quaternion.LookRotation(hacia.normalized, Vector3.up);
            }
        }

        private void ActualizarAgarre(float dt)
        {
            bool termino = regla.AvanzarAgarre(dt);
            transform.position = CalculoAgarre.Posicion(inicioAgarre, finAgarre, termino ? 1f : regla.ProgresoAgarre);
            if (termino)
            {
                Terminar(Vector3.zero);
            }
        }

        // Sondeo: pared al frente, cara de arriba del borde, espacio libre y apoyo para los pies. Devuelve dónde quedan los pies.
        private bool IntentarAgarre(out Vector3 destino, out float segundos)
        {
            destino = Vector3.zero;
            segundos = 0f;
            Vector3 pos = transform.position;
            Vector3 frente = transform.forward;
            frente.y = 0f;
            if (frente.sqrMagnitude <= 0f)
            {
                return false;
            }
            frente.Normalize();
            float radio = cc.radius;
            float margen = config.MargenSuperficie;

            Vector3 origenPared = pos + Vector3.up * config.AlturaMinimaAgarre;
            if (!GolpeMasCercano(origenPared, frente, radio + config.DistanciaSondeo, out RaycastHit pared)
                || pared.normal.y >= config.NormalMinimaSuelo)
            {
                return false;
            }

            Vector3 sonda = pared.point + frente * (radio + margen);
            float alturaInicioRayo = pos.y + config.AlturaMaximaAgarre + config.SobrealturaSondeo;
            Vector3 origenArriba = new Vector3(sonda.x, alturaInicioRayo, sonda.z);
            float largo = alturaInicioRayo - (pos.y + config.AlturaMinimaAgarre);
            if (!GolpeMasCercano(origenArriba, Vector3.down, largo, out RaycastHit cara))
            {
                return false;
            }

            float alturaBorde = cara.point.y - pos.y;
            Vector3 final = new Vector3(sonda.x, cara.point.y + margen, sonda.z);
            Vector3 intermedio = new Vector3(pos.x, final.y, pos.z);
            bool hayEspacio = CapsulaLibre(final, radio) && CapsulaLibre(intermedio, radio);
            bool hayApoyo = HayApoyo(final, cara.point.y, radio, frente);

            ResultadoAgarre resultado = CalculoAgarre.Evaluar(alturaBorde, hayEspacio, hayApoyo, cara.normal.y,
                config.AlturaMinimaAgarre, config.AlturaMaximaAgarre, config.NormalMinimaSuelo,
                config.SegundosAgarreBase, config.SegundosAgarrePorMetro);
            if (!resultado.Valido)
            {
                return false;
            }
            destino = final;
            segundos = resultado.Segundos;
            return true;
        }

        private bool CapsulaLibre(Vector3 pies, float radio)
        {
            float r = Mathf.Max(0.01f, radio - config.MargenSuperficie);
            Vector3 abajo = pies + Vector3.up * radio;
            Vector3 arriba = pies + Vector3.up * Mathf.Max(radio, cc.height - radio);
            int n = Physics.OverlapCapsuleNonAlloc(abajo, arriba, r, choques, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (choques[i].transform.root != transform.root)
                {
                    return false;
                }
            }
            return true;
        }

        // Cuatro puntos alrededor de los pies tienen que tener piso casi a la misma altura: un pretil delgado no da apoyo.
        private bool HayApoyo(Vector3 final, float alturaCara, float radio, Vector3 frente)
        {
            Vector3 lado = Vector3.Cross(Vector3.up, frente);
            float alcance = radio - config.MargenSuperficie;
            Vector3[] desfases = { frente * alcance, -frente * alcance, lado * alcance, -lado * alcance };
            float tol = config.ToleranciaApoyo;
            for (int i = 0; i < desfases.Length; i++)
            {
                Vector3 origen = final + desfases[i] + Vector3.up * tol;
                if (!GolpeMasCercano(origen, Vector3.down, 2f * tol + config.MargenSuperficie, out RaycastHit golpe)
                    || golpe.point.y < alturaCara - tol)
                {
                    return false;
                }
            }
            return true;
        }

        private bool GolpeMasCercano(Vector3 origen, Vector3 direccion, float distancia, out RaycastHit mejor)
        {
            mejor = default;
            int n = Physics.RaycastNonAlloc(origen, direccion, golpes, distancia, ~0, QueryTriggerInteraction.Ignore);
            float menor = float.MaxValue;
            bool hay = false;
            for (int i = 0; i < n; i++)
            {
                if (golpes[i].transform.root == transform.root || golpes[i].distance <= 0f)
                {
                    continue;
                }
                if (golpes[i].distance < menor)
                {
                    menor = golpes[i].distance;
                    mejor = golpes[i];
                    hay = true;
                }
            }
            return hay;
        }

        // ---------- Entrar y salir del control propio ----------

        private void Suspender()
        {
            controlador.MovimientoSuspendido = true;
            cc.enabled = false;
            if (visual != null)
            {
                visual.ForzarSuelo = true;
            }
        }

        private void Terminar(Vector3 empuje)
        {
            regla.Soltar();
            invertirHastaSoltar = false;
            BajandoEscalera = false;
            agarreDesdeEscalera = false;
            puntoActual = null;
            puntoCercano = null; // T-1203: si sigue al pie, el trigger lo vuelve a avisar al prender el CharacterController
            triggerCercano = null;
            cc.enabled = true;
            if (visual != null)
            {
                visual.ForzarSuelo = false;
            }
            enfriamiento = config.SegundosEnfriamiento;
            // El controlador se reanuda en el cuadro siguiente: así el mismo salto que soltó la escalera no vuelve a saltar.
            liberarPendiente = true;
            empujePendiente = empuje;
        }
    }
}
