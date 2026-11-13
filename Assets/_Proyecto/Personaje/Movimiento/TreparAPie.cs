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
        private PuntoTrepar puntoActual;
        private Vector3 inicioAgarre;
        private Vector3 finAgarre;
        private float enfriamiento;
        private bool liberarPendiente;
        private Vector3 empujePendiente;

        /// <summary>True mientras sube por una escalera o tubo, o se agarra de un borde.</summary>
        public bool Trepando => regla.Estado != EstadoTrepar.Libre;
        public EstadoTrepar Estado => regla.Estado;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            cc = GetComponent<CharacterController>();
            visual = GetComponentInChildren<VisualPersonaje>(true);
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
        }

        private void OnTriggerEnter(Collider otro)
        {
            PuntoTrepar punto = otro.GetComponentInParent<PuntoTrepar>();
            if (punto != null)
            {
                puntoCercano = punto;
            }
        }

        private void OnTriggerExit(Collider otro)
        {
            PuntoTrepar punto = otro.GetComponentInParent<PuntoTrepar>();
            if (punto != null && punto == puntoCercano)
            {
                puntoCercano = null;
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
            if (puntoCercano != null && entrada.y > UmbralAdelante && PuedeEngancharse(puntoCercano))
            {
                EngancharEscalera(puntoCercano);
                return;
            }
            bool automatico = config.AgarreAutomaticoEnAire && !controlador.EnPiso && entrada.y > UmbralAdelante;
            bool manual = pulsoSaltar && !controlador.SaltoBloqueado;
            if ((manual || automatico) && IntentarAgarre(out Vector3 destino, out float segundos))
            {
                IniciarAgarre(destino, segundos);
            }
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
        }

        private void ActualizarEscalera(float dt, float entradaVertical, bool pulsoSaltar)
        {
            bool enPiso = Physics.Raycast(transform.position + Vector3.up * AlturaRayoPiso, Vector3.down,
                AlturaRayoPiso + DistanciaPisoEscalera, ~0, QueryTriggerInteraction.Ignore);
            AccionTrepar accion = regla.AvanzarEscalera(dt, entradaVertical, enPiso, pulsoSaltar);
            switch (accion)
            {
                case AccionTrepar.Solto:
                    Vector3 empuje = pulsoSaltar ? -puntoActual.Frente * config.EmpujeSoltar : Vector3.zero;
                    Terminar(empuje);
                    return;
                case AccionTrepar.LlegoCima:
                    ColocarEnEscalera();
                    IniciarAgarre(puntoActual.Salida, CalculoAgarre.Segundos(0f, config.SegundosAgarreBase, config.SegundosAgarrePorMetro));
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
            puntoActual = null;
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
