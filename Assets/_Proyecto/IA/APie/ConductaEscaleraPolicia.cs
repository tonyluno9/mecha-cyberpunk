using System.Collections.Generic;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.Personaje;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1303: si te buscan y estás en un techo, hasta <see cref="MaximoTrepando"/> policías van a la escalera de mano que más cerca les queda de ti, suben con los
    /// clips PJ01_Escalera_* (los mismos del jugador, de <see cref="ConfigAnimacionEscalera"/>) y salen al techo; ahí siguen con su IA de siempre. Los demás se
    /// quedan disparando desde abajo. PandilleroAPie la agrega sola a cada policía.
    /// </summary>
    [DisallowMultipleComponent]
    public class ConductaEscaleraPolicia : MonoBehaviour, IConductaAPie
    {
        public enum Fase { Libre, Yendo, Subiendo, Saliendo }

        /// <summary>Cuántos metros más arriba que el policía debe estar el jugador para que suba.</summary>
        public const float DiferenciaAltura = 2.2f;
        /// <summary>La salida de la escalera debe quedar a lo más esto (m, en altura) de los pies del jugador.</summary>
        public const float MargenTecho = 1.5f;
        public const float RadioBusqueda = 70f;
        public const float VelocidadIr = 3.2f;
        public const float VelocidadSubir = 1.2f;
        /// <summary>Rapidez del clip de salir arriba (el clip dura 2.6 s; a 1.5 queda en 1.7 s).</summary>
        public const float RapidezSalida = 1.5f;
        public const int MaximoTrepando = 2;
        private const float SegundosRevisar = 0.5f;
        private const float SegundosMaximoYendo = 30f;
        private const float Llegada = 0.35f;
        private const float SegundosRecargarEscaleras = 5f;

        private static readonly HashSet<ConductaEscaleraPolicia> trepando = new HashSet<ConductaEscaleraPolicia>();
        private static PuntoTrepar[] escaleras;
        private static float escalerasHasta = -1f;

        private PandilleroAPie pandillero;
        private CharacterController controlador;
        private Animator animator;
        private PlayableGraph grafo;
        private AnimationClipPlayable playable;
        private bool grafoVivo;
        private AnimationClip clipActual;
        private float proximaRevision;
        private float inicioFase;
        private float progreso;

        public Fase FaseActual { get; private set; }
        public PuntoTrepar Escalera { get; private set; }
        public bool Controla => FaseActual != Fase.Libre;
        public static int Trepando => trepando.Count;

        private void Awake()
        {
            pandillero = GetComponent<PandilleroAPie>();
            controlador = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (pandillero == null)
            {
                return;
            }
            if (pandillero.EstaKO)
            {
                if (FaseActual != Fase.Libre)
                {
                    Soltar(); // cae desde donde iba
                }
                return;
            }
            if (FaseActual == Fase.Libre && Time.time >= proximaRevision)
            {
                proximaRevision = Time.time + SegundosRevisar;
                Revisar();
            }
        }

        private void Revisar()
        {
            Transform jugador = pandillero.Jugador;
            if (jugador == null || !pandillero.JugadorAtacable || trepando.Count >= MaximoTrepando)
            {
                return;
            }
            if (jugador.position.y - transform.position.y < DiferenciaAltura)
            {
                return;
            }
            PuntoTrepar mejor = MejorEscalera(transform.position, jugador.position);
            if (mejor != null)
            {
                Empezar(mejor);
            }
        }

        /// <summary>La escalera de mano que sale al techo donde está el jugador con el camino más corto (policía → pie de la escalera → jugador). Null si no hay.</summary>
        public static PuntoTrepar MejorEscalera(Vector3 policia, Vector3 jugador)
        {
            if (escaleras == null || Time.time >= escalerasHasta)
            {
                escaleras = FindObjectsByType<PuntoTrepar>(FindObjectsSortMode.None);
                escalerasHasta = Time.time + SegundosRecargarEscaleras;
            }
            PuntoTrepar mejor = null;
            float menor = float.MaxValue;
            foreach (PuntoTrepar e in escaleras)
            {
                if (e == null || !e.isActiveAndEnabled || e.Tipo != TipoTrepar.Escalera || Mathf.Abs(e.Salida.y - jugador.y) > MargenTecho)
                {
                    continue;
                }
                if (Mathf.Abs(e.PieAbajo.y - policia.y) > MargenTecho)
                {
                    continue; // el pie de la escalera debe estar en el piso del policía
                }
                float ir = Plano(policia, e.PieAbajo);
                if (ir > RadioBusqueda)
                {
                    continue;
                }
                float total = ir + Plano(e.Salida, jugador);
                if (total < menor)
                {
                    menor = total;
                    mejor = e;
                }
            }
            return mejor;
        }

        private void Empezar(PuntoTrepar escalera)
        {
            Escalera = escalera;
            FaseActual = Fase.Yendo;
            inicioFase = Time.time;
            trepando.Add(this);
        }

        public void Actualizar(PandilleroAPie p, float dt)
        {
            if (Escalera == null)
            {
                Soltar();
                return;
            }
            switch (FaseActual)
            {
                case Fase.Yendo:
                    Ir(p);
                    break;
                case Fase.Subiendo:
                    Subir(dt);
                    break;
                case Fase.Saliendo:
                    Salir();
                    break;
            }
        }

        private void Ir(PandilleroAPie p)
        {
            Transform jugador = p.Jugador;
            bool sigueArriba = jugador != null && p.JugadorAtacable && jugador.position.y - transform.position.y >= DiferenciaAltura * 0.5f;
            if (!sigueArriba || Time.time - inicioFase > SegundosMaximoYendo)
            {
                Soltar();
                return;
            }
            Vector3 hacia = Escalera.PieAbajo - transform.position;
            hacia.y = 0f;
            if (hacia.magnitude <= Llegada)
            {
                controlador.enabled = false;
                transform.SetPositionAndRotation(Escalera.PieAbajo, Quaternion.LookRotation(Escalera.Frente, Vector3.up));
                progreso = 0f;
                FaseActual = Fase.Subiendo;
                inicioFase = Time.time;
                return;
            }
            Vector3 vel = hacia.normalized * Mathf.Min(VelocidadIr, hacia.magnitude / Mathf.Max(Time.deltaTime, 0.001f));
            p.MoverPlano(vel);
            p.MirarHacia(hacia, 720f);
        }

        private void Subir(float dt)
        {
            progreso = Mathf.Min(Escalera.AlturaSubida, progreso + VelocidadSubir * dt);
            transform.position = Escalera.PieAbajo + Vector3.up * progreso;
            ConfigAnimacionEscalera config = ConfigAnimacionEscalera.Actual;
            AnimationClip clip = config != null ? config.Subir : null;
            if (clip != null)
            {
                Poner(clip, Mathf.Repeat(progreso / Mathf.Max(0.1f, config.MetrosPorCiclo), 1f) * clip.length);
            }
            if (progreso >= Escalera.AlturaSubida)
            {
                FaseActual = Fase.Saliendo;
                inicioFase = Time.time;
            }
        }

        private void Salir()
        {
            ConfigAnimacionEscalera config = ConfigAnimacionEscalera.Actual;
            AnimationClip clip = config != null ? config.SalirArriba : null;
            float duracion = clip != null ? clip.length / RapidezSalida : 0.8f;
            float t = Mathf.Clamp01((Time.time - inicioFase) / duracion);
            Vector3 cima = Escalera.Cima;
            Vector3 salida = Escalera.Salida;
            // Primero sube a la altura del techo y luego avanza sobre él (no atraviesa el pretil).
            float subir = Mathf.Clamp01(t / 0.6f);
            float avanzar = Mathf.Clamp01((t - 0.35f) / 0.65f);
            transform.position = new Vector3(Mathf.Lerp(cima.x, salida.x, avanzar), Mathf.Lerp(cima.y, salida.y + 0.05f, subir), Mathf.Lerp(cima.z, salida.z, avanzar));
            if (clip != null)
            {
                Poner(clip, t * clip.length);
            }
            if (t >= 1f)
            {
                transform.position = salida + Vector3.up * 0.05f;
                Soltar();
            }
        }

        private void Poner(AnimationClip clip, double tiempo)
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    return;
                }
            }
            if (!grafoVivo || clip != clipActual)
            {
                Destruir();
                animator.applyRootMotion = false;
                playable = AnimationPlayableUtilities.PlayClip(animator, clip, out grafo);
                playable.SetSpeed(0d);
                clipActual = clip;
                grafoVivo = true;
            }
            playable.SetTime(tiempo);
        }

        /// <summary>Deja la escalera (llegó arriba, se cayó o ya no hace falta): vuelve la cápsula y la IA normal.</summary>
        public void Soltar()
        {
            FaseActual = Fase.Libre;
            Escalera = null;
            trepando.Remove(this);
            proximaRevision = Time.time + SegundosRevisar * 4f;
            if (controlador != null)
            {
                controlador.enabled = true;
            }
            Destruir();
        }

        private void Destruir()
        {
            if (grafoVivo && grafo.IsValid())
            {
                grafo.Destroy();
            }
            grafoVivo = false;
            clipActual = null;
        }

        private void OnDisable()
        {
            if (FaseActual != Fase.Libre)
            {
                Soltar();
            }
            trepando.Remove(this);
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }
    }
}
