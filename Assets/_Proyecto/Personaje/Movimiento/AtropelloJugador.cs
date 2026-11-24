using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// T-1327: si un carro (cualquier cosa con <see cref="IMontable"/> modo Carro) te pega a buena velocidad, el jugador cae (la animación de tirado de VisualPersonaje),
    /// el golpe lo empuja unos metros, queda en el suelo un momento y se levanta con la animación de levantarse. Mientras está derribado
    /// <see cref="ControladorAPie.Derribado"/> bloquea caminar, saltar y golpear. No quita vida (el daño del choque, si llega, lo pone el carro). La rapidez del carro sale de su Rigidbody
    /// o, si es cinemático (el tráfico), de lo que se mueve su transform. Lo agrega <see cref="TreparAPie"/>; corre antes de CaidaJugador y ControladorAPie.
    /// </summary>
    [DefaultExecutionOrder(-6)]
    [RequireComponent(typeof(ControladorAPie))]
    public class AtropelloJugador : MonoBehaviour
    {
        public enum FaseAtropello { Libre, EnElSuelo, Levantandose }

        /// <summary>Segundos que queda tirado antes de levantarse.</summary>
        public const float SegundosEnSuelo = 1.4f;
        /// <summary>Segundos de la animación de levantarse, con el control todavía bloqueado.</summary>
        public const float SegundosLevantandose = 1.1f;
        /// <summary>Segundos que dura el empujón (baja hasta 0).</summary>
        public const float SegundosEmpuje = 0.6f;
        public const float RapidezEmpuje = 6f;
        /// <summary>Segundos sin poder volver a ser derribado tras levantarse.</summary>
        public const float SegundosEnfriamiento = 1.5f;
        private const float MargenContacto = 0.2f;
        private const float MargenSeguimiento = 1.2f;
        private const int MaxCandidatos = 16;

        private ControladorAPie controlador;
        private CharacterController cuerpo;
        private VidaAPie vida;
        private JugadorAPie jugadorAPie;
        private VisualPersonaje visual;
        private readonly Collider[] candidatos = new Collider[MaxCandidatos];
        private Dictionary<Transform, Vector3> posicionesAntes = new Dictionary<Transform, Vector3>();
        private Dictionary<Transform, Vector3> posicionesAhora = new Dictionary<Transform, Vector3>();
        private float tiempoFase;
        private float libreDesde = -100f;
        private Vector3 direccionEmpuje;

        public FaseAtropello Fase { get; private set; } = FaseAtropello.Libre;
        public bool Derribado => Fase != FaseAtropello.Libre;
        /// <summary>Veces que lo han derribado, para las pruebas.</summary>
        public int Derribos { get; private set; }

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            cuerpo = GetComponent<CharacterController>();
            vida = GetComponent<VidaAPie>();
            jugadorAPie = GetComponent<JugadorAPie>();
            visual = GetComponentInChildren<VisualPersonaje>(true);
        }

        private void OnDisable()
        {
            if (Derribado)
            {
                Terminar();
            }
            posicionesAntes.Clear();
            posicionesAhora.Clear();
        }

        /// <summary>Derriba al jugador ahora, empujándolo hacia <paramref name="direccion"/> (plano horizontal). Lo usan el detector de carros y las pruebas.</summary>
        public bool Derribar(Vector3 direccion)
        {
            // VR (vr/quest3): con visor no se derriba ni empuja al jugador (marea).
            if (ModoVR.Activo || Derribado || (vida != null && vida.EstaKO) || (jugadorAPie != null && jugadorAPie.Modo != ModoJugador.APie))
            {
                return false;
            }
            direccion.y = 0f;
            direccionEmpuje = direccion.sqrMagnitude > 0.0001f ? direccion.normalized : transform.forward;
            Derribos++;
            Fase = FaseAtropello.EnElSuelo;
            tiempoFase = 0f;
            controlador.Derribado = true;
            controlador.SaltoBloqueado = true;
            if (visual == null)
            {
                visual = GetComponentInChildren<VisualPersonaje>(true);
            }
            if (visual != null && visual.gameObject.activeInHierarchy)
            {
                visual.RegistrarGolpe(direccionEmpuje);
                // T-1540: cae con PJ01_Muerte_Atropello. El empuje es de este script (DesplazamientoExterno), así que la raíz del clip (4 m) no mueve la cápsula.
                visual.MotivoDanio = MotivoDanioVisual.Atropello;
                visual.SuspenderRaiz(true);
                visual.Caido = true;
            }
            Debug.Log("[Personaje] T-1327: atropellado, cae");
            return true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (Derribado)
            {
                AvanzarDerribo(dt);
                return;
            }
            if (!controlador.ControlActivo || cuerpo == null || !cuerpo.enabled || Time.time < libreDesde + SegundosEnfriamiento
                || (jugadorAPie != null && jugadorAPie.Modo != ModoJugador.APie))
            {
                posicionesAntes.Clear();
                return;
            }
            Detectar(dt);
        }

        private void AvanzarDerribo(float dt)
        {
            if (vida != null && vida.EstaKO)
            {
                // Si lo mataron tirado, lo demás es del KO (AnimacionJugador): se suelta el bloqueo sin levantarlo.
                Terminar(false);
                return;
            }
            tiempoFase += dt;
            if (Fase == FaseAtropello.EnElSuelo)
            {
                controlador.FactorVelocidad = 0f;
                float resto = Mathf.Clamp01(1f - tiempoFase / SegundosEmpuje);
                controlador.DesplazamientoExterno = direccionEmpuje * (RapidezEmpuje * resto);
                if (tiempoFase >= SegundosEnSuelo)
                {
                    Fase = FaseAtropello.Levantandose;
                    tiempoFase = 0f;
                    controlador.DesplazamientoExterno = Vector3.zero;
                    if (visual != null)
                    {
                        visual.Caido = false;
                        visual.SuspenderRaiz(false); // Dano_Levantarse sí mueve la cápsula
                    }
                }
            }
            else if (tiempoFase >= SegundosLevantandose)
            {
                Terminar();
            }
        }

        private void Terminar(bool levantar = true)
        {
            Fase = FaseAtropello.Libre;
            libreDesde = Time.time;
            controlador.Derribado = false;
            controlador.FactorVelocidad = 1f;
            controlador.DesplazamientoExterno = Vector3.zero;
            controlador.SaltoBloqueado = false;
            if (visual != null)
            {
                visual.SuspenderRaiz(false);
            }
            if (levantar && visual != null)
            {
                visual.Caido = false;
            }
        }

        private void Detectar(float dt)
        {
            Capsula(out Vector3 a, out Vector3 b, out float radio);
            // Se siguen los carros cerca (más que el contacto) para tener su rapidez antes de que toquen.
            int cantidad = Physics.OverlapCapsuleNonAlloc(a, b, radio + MargenSeguimiento, candidatos, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            posicionesAhora.Clear();
            Vector3 centro = (a + b) * 0.5f;
            Transform golpeador = null;
            Vector3 velocidadGolpeador = Vector3.zero;
            for (int i = 0; i < cantidad; i++)
            {
                Collider c = candidatos[i];
                if (c.transform.root == transform.root)
                {
                    continue;
                }
                IMontable carro = c.GetComponentInParent<IMontable>();
                if (carro == null || carro.Modo != ModoJugador.Carro || !(carro is Component componente))
                {
                    continue;
                }
                Transform raiz = componente.transform;
                Rigidbody cuerpoCarro = c.attachedRigidbody;
                Vector3 velocidad = Vector3.zero;
                if (cuerpoCarro != null && !cuerpoCarro.isKinematic)
                {
                    velocidad = cuerpoCarro.linearVelocity;
                }
                else if (posicionesAntes.TryGetValue(raiz, out Vector3 antes) && dt > 0f)
                {
                    velocidad = (raiz.position - antes) / dt;
                }
                posicionesAhora[raiz] = raiz.position;
                if (golpeador != null || !TocaAlJugador(c, centro, radio))
                {
                    continue;
                }
                if (ReglaAtropello.Derriba(velocidad, centro - c.bounds.center))
                {
                    golpeador = raiz;
                    velocidadGolpeador = velocidad;
                }
            }
            Dictionary<Transform, Vector3> cambio = posicionesAntes;
            posicionesAntes = posicionesAhora;
            posicionesAhora = cambio;
            if (golpeador != null)
            {
                Derribar(ReglaAtropello.DireccionEmpuje(velocidadGolpeador, centro - golpeador.position));
            }
        }

        private static bool TocaAlJugador(Collider c, Vector3 centro, float radio)
        {
            Vector3 cercano = c is MeshCollider malla && !malla.convex ? c.bounds.ClosestPoint(centro) : c.ClosestPoint(centro);
            return (cercano - centro).sqrMagnitude <= (radio + MargenContacto) * (radio + MargenContacto);
        }

        private void Capsula(out Vector3 abajo, out Vector3 arriba, out float radio)
        {
            radio = cuerpo.radius;
            Vector3 centro = transform.TransformPoint(cuerpo.center);
            float mitad = Mathf.Max(0f, cuerpo.height * 0.5f - radio);
            abajo = centro + Vector3.down * mitad;
            arriba = centro + Vector3.up * mitad;
        }
    }
}
