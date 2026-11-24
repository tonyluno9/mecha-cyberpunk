using System;
using System.Collections.Generic;
using MechaCyberpunk.CampoTiro;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;
using UnityEngine.AI;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR3: un enemigo agarrado con el implante de BRAZOS. Se pone solo (con <see cref="Agarrar"/>) en la raíz del enemigo y se quita
    /// solo al terminar. Mientras se sostiene, su IA, su CharacterController y su NavMeshAgent quedan apagados y un Rigidbody
    /// cinemático con una cápsula temporal lo pega a la palma. Al lanzarlo pasa a cuerpo físico; chocar rápido le hace daño a él y
    /// a lo que golpee. Cuando se detiene (o a los 3 s) se endereza en el piso y vuelve a su IA; si quedó derrotado, su propio flujo
    /// de muerte sigue (no se le vuelve a prender el CharacterController). Ningún enemigo del juego tiene ragdoll: siempre usa la cápsula.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class CuerpoAgarradoVR : MonoBehaviour
    {
        private const float MasaCuerpo = 80f;
        private const float FrenoGiro = 2f;
        private const float RadioPorDefecto = 0.3f;
        private const float AlturaPorDefecto = 1.8f;
        private const float AlturaBusquedaPiso = 2f;
        private const float DistanciaBusquedaPiso = 6f;
        private const float SobrePiso = 0.02f;
        private const float RadioNavMesh = 2f;

        public enum Estado
        {
            Sostenido,
            Volando,
            Terminado
        }

        private ManoVR mano;
        private Transform raizJugador;
        private Vector3 desfaseEnMano;
        private Pose poseInicial;
        private Rigidbody cuerpo;
        private bool cuerpoPropio;
        private bool cinematicoPrevio;
        private bool gravedadPrevia;
        private CapsuleCollider capsula;
        private readonly List<Collider> ignorados = new List<Collider>();

        private PandilleroAPie pandillero;
        private bool pandilleroEncendido;
        private readonly List<MonoBehaviour> conductasApagadas = new List<MonoBehaviour>();
        private ObjetivoTiro objetivoTiro;
        private bool objetivoEncendido;
        private BoxCollider cajaObjetivo;
        private bool cajaEncendida;
        private CharacterController controlador;
        private bool controladorEncendido;
        private NavMeshAgent agente;
        private bool agenteEncendido;

        private float segundosQuieto;
        private float proximoChoque;
        private Vector3 velocidadPrevia;

        public Estado EstadoActual { get; private set; }
        public ManoVR Mano => mano;
        public Rigidbody Cuerpo => cuerpo;
        public Collider Choque => capsula;
        public float SegundosVuelo { get; private set; }
        /// <summary>Cuántos choques le hicieron daño en este vuelo.</summary>
        public int ChoquesConDanio { get; private set; }
        /// <summary>Daño total (puntos) que recibió por choques en este vuelo.</summary>
        public float DanioPorChoques { get; private set; }

        /// <summary>Quién recibe el daño de este enemigo.</summary>
        public IRecibeDanio Recibe => pandillero != null ? pandillero : (IRecibeDanio)objetivoTiro;

        /// <summary>Vida actual (puntos) del enemigo.</summary>
        public float Vida => pandillero != null ? pandillero.Actual : (objetivoTiro != null ? objetivoTiro.Vida : 0f);

        /// <summary>True si el enemigo quedó derrotado (KO o caído).</summary>
        public bool EstaDerrotado => (pandillero != null && pandillero.EstaKO) || (objetivoTiro != null && objetivoTiro.EstaCaido);

        /// <summary>Se avisa al terminar el vuelo (ya restaurado), justo antes de quitarse.</summary>
        public event Action<CuerpoAgarradoVR> Terminado;

        /// <summary>
        /// Agarra al enemigo <paramref name="raiz"/> con <paramref name="manoQueAgarra"/>. Si ya estaba volando (agarrado antes), lo vuelve
        /// a sostener sin perder lo que se apagó.
        /// </summary>
        public static CuerpoAgarradoVR Agarrar(GameObject raiz, ManoVR manoQueAgarra, Transform jugador)
        {
            CuerpoAgarradoVR existente = raiz.GetComponent<CuerpoAgarradoVR>();
            if (existente != null)
            {
                if (existente.EstadoActual != Estado.Volando)
                {
                    return null;
                }
                existente.Sostener(manoQueAgarra);
                return existente;
            }
            CuerpoAgarradoVR nuevo = raiz.AddComponent<CuerpoAgarradoVR>();
            nuevo.raizJugador = jugador;
            nuevo.Apagar();
            nuevo.Sostener(manoQueAgarra);
            return nuevo;
        }

        /// <summary>Distancia (m) de <paramref name="punto"/> a la superficie del enemigo (0 si está dentro).</summary>
        public float DistanciaA(Vector3 punto)
        {
            if (capsula == null)
            {
                return float.MaxValue;
            }
            return Vector3.Distance(capsula.ClosestPoint(punto), punto);
        }

        /// <summary>Lo suelta con <paramref name="velocidad"/> (m/s) y <paramref name="giro"/> (rad/s): pasa a cuerpo físico.</summary>
        public void Lanzar(Vector3 velocidad, Vector3 giro)
        {
            if (EstadoActual != Estado.Sostenido)
            {
                return;
            }
            mano = null;
            EstadoActual = Estado.Volando;
            SegundosVuelo = 0f;
            segundosQuieto = 0f;
            ChoquesConDanio = 0;
            DanioPorChoques = 0f;
            cuerpo.isKinematic = false;
            cuerpo.useGravity = true;
            cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
            cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            cuerpo.linearVelocity = velocidad;
            cuerpo.angularVelocity = giro;
            velocidadPrevia = velocidad;
        }

        /// <summary>Le aplica daño de golpe (puntos) en <paramref name="punto"/>, en la dirección <paramref name="direccion"/>.</summary>
        public ResultadoDanio Daniar(float cantidad, Vector3 punto, Vector3 direccion)
        {
            IRecibeDanio recibe = Recibe;
            if (recibe == null)
            {
                return ResultadoDanio.Ignorado;
            }
            return recibe.RecibirDanio(new InfoDanio(cantidad, TipoDanio.Golpe, punto, direccion, null));
        }

        private void Sostener(ManoVR manoQueAgarra)
        {
            mano = manoQueAgarra;
            EstadoActual = Estado.Sostenido;
            cuerpo.isKinematic = true;
            cuerpo.useGravity = false;
            // Sin interpolación mientras se mueve a mano el transform (la interpolación lo pisaría).
            cuerpo.interpolation = RigidbodyInterpolation.None;
            cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            desfaseEnMano = mano.transform.InverseTransformPoint(transform.position);
        }

        // Apaga la IA y lo que lo mueve, y le pone un cuerpo físico con una cápsula del tamaño del enemigo.
        private void Apagar()
        {
            poseInicial = new Pose(transform.position, transform.rotation);
            pandillero = GetComponent<PandilleroAPie>();
            objetivoTiro = GetComponent<ObjetivoTiro>();
            controlador = GetComponent<CharacterController>();
            agente = GetComponent<NavMeshAgent>();

            if (pandillero != null)
            {
                pandilleroEncendido = pandillero.enabled;
                pandillero.enabled = false;
                foreach (MonoBehaviour guion in GetComponents<MonoBehaviour>())
                {
                    if (guion is IConductaAPie && guion.enabled)
                    {
                        guion.enabled = false;
                        conductasApagadas.Add(guion);
                    }
                }
            }
            if (objetivoTiro != null)
            {
                objetivoEncendido = objetivoTiro.enabled;
                objetivoTiro.enabled = false;
                cajaObjetivo = GetComponent<BoxCollider>();
            }

            capsula = gameObject.AddComponent<CapsuleCollider>();
            capsula.direction = 1;
            if (controlador != null)
            {
                capsula.center = controlador.center;
                capsula.radius = controlador.radius;
                capsula.height = controlador.height;
            }
            else if (cajaObjetivo != null)
            {
                capsula.center = cajaObjetivo.center;
                capsula.radius = Mathf.Min(cajaObjetivo.size.x, cajaObjetivo.size.z) * 0.5f;
                capsula.height = Mathf.Max(cajaObjetivo.size.y, capsula.radius * 2f);
            }
            else
            {
                capsula.center = Vector3.up * AlturaPorDefecto * 0.5f;
                capsula.radius = RadioPorDefecto;
                capsula.height = AlturaPorDefecto;
            }

            if (controlador != null)
            {
                controladorEncendido = controlador.enabled;
                controlador.enabled = false;
            }
            if (cajaObjetivo != null)
            {
                cajaEncendida = cajaObjetivo.enabled;
                cajaObjetivo.enabled = false;
            }
            if (agente != null)
            {
                agenteEncendido = agente.enabled;
                agente.enabled = false;
            }

            cuerpo = GetComponent<Rigidbody>();
            cuerpoPropio = cuerpo == null;
            if (cuerpoPropio)
            {
                cuerpo = gameObject.AddComponent<Rigidbody>();
                cuerpo.mass = MasaCuerpo;
                cuerpo.angularDamping = FrenoGiro;
            }
            else
            {
                cinematicoPrevio = cuerpo.isKinematic;
                gravedadPrevia = cuerpo.useGravity;
            }

            // No choca con el jugador que lo sostiene.
            if (raizJugador != null)
            {
                foreach (Collider propio in raizJugador.GetComponentsInChildren<Collider>(true))
                {
                    Physics.IgnoreCollision(capsula, propio, true);
                    ignorados.Add(propio);
                }
            }
        }

        private void LateUpdate()
        {
            if (EstadoActual != Estado.Sostenido)
            {
                return;
            }
            if (mano == null)
            {
                // La mano desapareció (el jugador se fue): se cae sin fuerza.
                Lanzar(Vector3.zero, Vector3.zero);
                return;
            }
            transform.position = mano.transform.TransformPoint(desfaseEnMano);
        }

        private void FixedUpdate()
        {
            if (EstadoActual != Estado.Volando)
            {
                return;
            }
            float dt = Time.fixedDeltaTime;
            SegundosVuelo += dt;
            velocidadPrevia = cuerpo.linearVelocity;
            segundosQuieto = velocidadPrevia.magnitude < ReglaAgarreVR.RapidezQuieto ? segundosQuieto + dt : 0f;
            if (ReglaAgarreVR.VueloTermino(SegundosVuelo, segundosQuieto))
            {
                Restaurar();
            }
        }

        private void OnCollisionEnter(Collision choque)
        {
            if (EstadoActual != Estado.Volando || Time.time < proximoChoque)
            {
                return;
            }
            if (raizJugador != null && choque.transform.IsChildOf(raizJugador))
            {
                return;
            }
            // Solo cuenta la rapidez contra la superficie (no lo que resbala por el piso).
            float rapidez = choque.contactCount > 0 ? Mathf.Abs(Vector3.Dot(choque.relativeVelocity, choque.GetContact(0).normal)) : choque.relativeVelocity.magnitude;
            float danio = ReglaAgarreVR.DanioChoque(rapidez);
            if (danio <= 0f)
            {
                return;
            }
            proximoChoque = Time.time + ReglaAgarreVR.SegundosEntreChoques;
            Vector3 punto = choque.contactCount > 0 ? choque.GetContact(0).point : transform.position;
            Vector3 direccion = velocidadPrevia.sqrMagnitude > 1e-4f ? velocidadPrevia.normalized : transform.forward;
            ResultadoDanio resultado = Daniar(danio, punto, direccion);
            ChoquesConDanio++;
            DanioPorChoques += resultado.DanioAplicado;
            IRecibeDanio otro = choque.collider.GetComponentInParent<IRecibeDanio>();
            if (otro != null && !ReferenceEquals(otro, Recibe))
            {
                otro.RecibirDanio(new InfoDanio(danio, TipoDanio.Golpe, punto, direccion, null));
            }
            Sonidos.ReproducirEn(ReglaSonidosJugador.GolpeCarne, punto);
        }

        // Lo endereza en el piso, le devuelve lo que se apagó y se quita.
        private void Restaurar()
        {
            EstadoActual = Estado.Terminado;
            bool derrotado = EstaDerrotado;
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            cuerpo.isKinematic = true;
            capsula.enabled = false;

            Vector3 frente = transform.forward;
            frente.y = 0f;
            Quaternion derecho = frente.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(frente.normalized, Vector3.up) : Quaternion.Euler(0f, poseInicial.rotation.eulerAngles.y, 0f);
            if (objetivoTiro != null)
            {
                // Un maniquí del campo de tiro vuelve a su lugar (sobre su plataforma).
                transform.SetPositionAndRotation(poseInicial.position, poseInicial.rotation);
            }
            else
            {
                transform.SetPositionAndRotation(PuntoEnPiso(transform.position), derecho);
            }
            Physics.SyncTransforms();

            if (cuerpoPropio)
            {
                Destroy(cuerpo);
            }
            else
            {
                cuerpo.isKinematic = cinematicoPrevio;
                cuerpo.useGravity = gravedadPrevia;
            }
            Destroy(capsula);

            if (controlador != null)
            {
                controlador.enabled = controladorEncendido && !derrotado;
            }
            if (agente != null && agenteEncendido && !derrotado)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit golpe, RadioNavMesh, NavMesh.AllAreas))
                {
                    agente.enabled = true;
                    agente.Warp(golpe.position);
                }
            }
            if (pandillero != null)
            {
                // Derrotado o no, su Update sigue: si quedó KO, ahí corre su limpieza normal.
                pandillero.enabled = pandilleroEncendido;
            }
            foreach (MonoBehaviour guion in conductasApagadas)
            {
                if (guion != null)
                {
                    guion.enabled = true;
                }
            }
            if (objetivoTiro != null)
            {
                objetivoTiro.enabled = objetivoEncendido;
                if (cajaObjetivo != null)
                {
                    // Si cayó, su caja se queda apagada hasta que reaparezca (Reaparecer la prende).
                    cajaObjetivo.enabled = cajaEncendida && !objetivoTiro.EstaCaido;
                }
            }
            Terminado?.Invoke(this);
            Destroy(this);
        }

        private Vector3 PuntoEnPiso(Vector3 posicion)
        {
            Vector3 origen = posicion + Vector3.up * AlturaBusquedaPiso;
            RaycastHit[] golpes = Physics.RaycastAll(origen, Vector3.down, DistanciaBusquedaPiso, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            Vector3 punto = posicion;
            foreach (RaycastHit g in golpes)
            {
                if (g.collider.transform.IsChildOf(transform) || (raizJugador != null && g.collider.transform.IsChildOf(raizJugador)))
                {
                    continue;
                }
                if (g.distance < mejor)
                {
                    mejor = g.distance;
                    punto = g.point + Vector3.up * SobrePiso;
                }
            }
            return punto;
        }

        private void OnDestroy()
        {
            if (capsula != null)
            {
                foreach (Collider propio in ignorados)
                {
                    if (propio != null)
                    {
                        Physics.IgnoreCollision(capsula, propio, false);
                    }
                }
            }
        }
    }
}
