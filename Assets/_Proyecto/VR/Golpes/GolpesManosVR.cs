using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR37: esfera de nudillos de 0.13 m contra todo el cuerpo (cualquier dirección y altura); además, si la mano entra en el enemigo tras ir a más de
    /// 0.6 m/s en los últimos 0.25 s, también cuenta (aunque al llegar ya frene). Tocar despacio sigue sin contar.
    /// VR2/VR35: golpes con las manos. Cada cuadro, cada mano barre una cápsula de <see cref="Radio"/> m del punto de sus nudillos del cuadro anterior al
    /// actual (<see cref="ManoVR.Nudillos"/>; un golpe rápido no atraviesa) contra todas las capas, triggers incluidos. A lo que recibe daño
    /// (<see cref="IRecibeDanio"/>, que no sea el propio jugador) le pega si la mano va a más de 0.8 m/s (la mayor rapidez de los últimos 0.1 s: la del
    /// visor o la medida, relativa al rig) con el daño del puño de <see cref="GolpeAPie"/> por el factor de rapidez (<see cref="ReglaGolpeManoVR"/>),
    /// x2 con el implante de BRAZOS. Cada mano le pega al mismo enemigo a lo más cada 0.3 s (aunque la mano siga dentro). Vibra fuerte el control,
    /// suena el golpe conectado y el enemigo hace su reacción de golpe (la de <see cref="PandilleroAPie.RecibirDanio"/>, igual que en PC).
    /// Al enemigo sostenido con una mano (<see cref="CuerpoAgarradoVR"/>) le pega la otra, con reacción doble; si el golpe lo derriba, se suelta
    /// (con BRAZOS sale volando). La mano derecha también pega con el arma en la mano (cachazo, mismo daño de puño).
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class GolpesManosVR : MonoBehaviour
    {
        /// <summary>Radio de la esfera de los nudillos de cada mano (m). VR37: 0.13 (antes 0.10) para que cuente desde cualquier ángulo.</summary>
        public const float Radio = ReglaGolpeManoVR.RadioNudillos;
        private const int MaxColisiones = 32;
        private const int MaxEnfriamientos = 16;
        private const float AmplitudVibracion = 1f;
        private const float SegundosVibracion = 0.12f;
        private const float AmplitudSostiene = 0.6f;
        private const float SegundosSostiene = 0.08f;

        private sealed class EstadoMano
        {
            public ManoVR Mano;
            public Vector3 NudilloPrevio;
            public bool HayPrevio;
            public readonly Queue<(float t, float rapidez)> Rapideces = new Queue<(float, float)>();
            public readonly Dictionary<Component, float> ListoContra = new Dictionary<Component, float>();
            public readonly HashSet<Component> Vistos = new HashSet<Component>();
            /// <summary>VR37: a quién tocaba la mano el cuadro anterior (para saber si acaba de entrar en un enemigo).</summary>
            public HashSet<Component> Dentro = new HashSet<Component>();
            public HashSet<Component> DentroAhora = new HashSet<Component>();
        }

        private AdaptadorVR adaptador;
        private GolpeAPie golpe;
        private VidaAPie vida;
        private ControladorAPie controlador;
        private JugadorAPie jugadorAPie;
        private AgarreImplanteVR agarre;
        private EstadoMano izquierda;
        private EstadoMano derecha;
        private readonly Collider[] colisiones = new Collider[MaxColisiones];
        private readonly List<Component> vencidos = new List<Component>();

        /// <summary>Golpes que conectaron (con las dos manos).</summary>
        public int Golpes { get; private set; }
        /// <summary>Daño pedido en el último golpe (antes de bloqueos).</summary>
        public float UltimoDanio { get; private set; }
        /// <summary>Rapidez de la mano en el último golpe (m/s).</summary>
        public float UltimaVelocidad { get; private set; }
        /// <summary>A quién le pegó el último golpe.</summary>
        public Component UltimoObjetivo { get; private set; }
        /// <summary>True si el último golpe fue con la mano derecha.</summary>
        public bool UltimoConDerecha { get; private set; }
        /// <summary>True si el último golpe fue a un enemigo sostenido con la otra mano.</summary>
        public bool UltimoASostenido { get; private set; }

        /// <summary>Se dispara en cada golpe: la mano (true derecha), el objetivo y el resultado.</summary>
        public event Action<bool, Component, ResultadoDanio> Golpeo;

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            golpe = GetComponent<GolpeAPie>();
            vida = GetComponent<VidaAPie>();
            controlador = GetComponent<ControladorAPie>();
            jugadorAPie = GetComponent<JugadorAPie>();
            if (adaptador == null || golpe == null)
            {
                Debug.LogWarning("[VR] GolpesManosVR sin AdaptadorVR o GolpeAPie en " + name + ".", this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (adaptador == null)
            {
                Destroy(this);
                return;
            }
            if (!adaptador.Armado)
            {
                return;
            }
            if (izquierda == null && adaptador.ManoIzquierda != null)
            {
                izquierda = new EstadoMano { Mano = adaptador.ManoIzquierda };
            }
            if (derecha == null && adaptador.ManoDerecha != null)
            {
                derecha = new EstadoMano { Mano = adaptador.ManoDerecha };
            }
            if (agarre == null)
            {
                // AgarreImplanteVR se agrega solo al armarse el rig (puede llegar después de este Awake).
                agarre = GetComponent<AgarreImplanteVR>();
            }
            bool puede = PuedeGolpear();
            Revisar(izquierda, puede);
            Revisar(derecha, puede);
        }

        // VR35: ya no se exige ControladorAPie.ControlActivo (en VR el cuerpo lo mueve el rig); solo no pega KO, en pausa, con la rueda abierta o fuera de a pie.
        private bool PuedeGolpear()
        {
            if (Pausa.Activa || (vida != null && vida.EstaKO))
            {
                return false;
            }
            if (controlador != null && controlador.EntradaBloqueada)
            {
                return false;
            }
            return jugadorAPie == null || jugadorAPie.Modo == ModoJugador.APie;
        }

        // La mayor rapidez de la mano en los últimos SegundosPico (la del visor si la da, si no la medida por ManoVR; las dos sin lo que camina el jugador).
        // VR37: también la mayor de los últimos SegundosPicoEntrada (para la mano que entra en el enemigo ya frenando).
        private static float Rapidez(EstadoMano estado, out float picoEntrada)
        {
            float ahora = estado.Mano.VelocidadRelativa.magnitude;
            if (estado.Mano.VelocidadDispositivo(out Vector3 delVisor))
            {
                ahora = Mathf.Max(ahora, delVisor.magnitude);
            }
            float t = Time.time;
            estado.Rapideces.Enqueue((t, ahora));
            while (estado.Rapideces.Count > 1 && t - estado.Rapideces.Peek().t > ReglaGolpeManoVR.SegundosPicoEntrada)
            {
                estado.Rapideces.Dequeue();
            }
            float pico = 0f;
            picoEntrada = 0f;
            foreach ((float t, float rapidez) muestra in estado.Rapideces)
            {
                picoEntrada = Mathf.Max(picoEntrada, muestra.rapidez);
                if (t - muestra.t <= ReglaGolpeManoVR.SegundosPico)
                {
                    pico = Mathf.Max(pico, muestra.rapidez);
                }
            }
            return pico;
        }

        private Vector3 Direccion(EstadoMano estado)
        {
            Vector3 v = estado.Mano.VelocidadRelativa;
            return v.sqrMagnitude > 1e-6f ? v.normalized : estado.Mano.transform.forward;
        }

        private void Revisar(EstadoMano estado, bool puede)
        {
            if (estado == null || estado.Mano == null)
            {
                return;
            }
            float rapidez = Rapidez(estado, out float picoEntrada);
            Vector3 nudillos = estado.Mano.Nudillos;
            Vector3 desde = estado.HayPrevio ? estado.NudilloPrevio : nudillos;
            estado.NudilloPrevio = nudillos;
            estado.HayPrevio = true;
            estado.DentroAhora.Clear();
            // VR37: se barre también a menos de 0.8 m/s si la mano venía rápida, para que cuente al entrar en el enemigo; el golpe lo decide la regla.
            if (!puede || !ReglaGolpeManoVR.ValeLaPenaBarrer(rapidez, picoEntrada))
            {
                CambiarDentro(estado);
                return;
            }
            LimpiarEnfriamientos(estado);
            estado.Vistos.Clear();
            int n = Physics.OverlapCapsuleNonAlloc(desde, nudillos, Radio, colisiones, ~0, QueryTriggerInteraction.Collide);
            Transform raiz = transform.root;
            for (int i = 0; i < n; i++)
            {
                Collider c = colisiones[i];
                colisiones[i] = null;
                if (c == null || c.transform.root == raiz || c.transform.IsChildOf(transform))
                {
                    continue;
                }
                Component objetivo = c.GetComponentInParent<IRecibeDanio>() as Component;
                if (objetivo == null || objetivo.transform.root == raiz || !estado.Vistos.Add(objetivo))
                {
                    continue;
                }
                estado.DentroAhora.Add(objetivo);
                bool entrando = !estado.Dentro.Contains(objetivo);
                if (!ReglaGolpeManoVR.GolpeaAl(rapidez, picoEntrada, entrando))
                {
                    continue;
                }
                CuerpoAgarradoVR agarrado = objetivo.GetComponentInParent<CuerpoAgarradoVR>();
                bool sostenido = agarrado != null && agarrado.EstadoActual == CuerpoAgarradoVR.Estado.Sostenido;
                if (agarrado != null && !ReglaGolpeManoVR.PuedePegarAlAgarrado(sostenido, agarrado.Mano == estado.Mano))
                {
                    continue;
                }
                if (estado.ListoContra.TryGetValue(objetivo, out float listo) && Time.time < listo)
                {
                    continue;
                }
                Golpear(estado, objetivo, c, nudillos, Mathf.Max(rapidez, entrando ? picoEntrada : 0f), sostenido ? agarrado : null);
            }
            CambiarDentro(estado);
        }

        private static void CambiarDentro(EstadoMano estado)
        {
            HashSet<Component> previo = estado.Dentro;
            estado.Dentro = estado.DentroAhora;
            estado.DentroAhora = previo;
            estado.DentroAhora.Clear();
        }

        private void LimpiarEnfriamientos(EstadoMano estado)
        {
            if (estado.ListoContra.Count < MaxEnfriamientos)
            {
                return;
            }
            vencidos.Clear();
            foreach (KeyValuePair<Component, float> par in estado.ListoContra)
            {
                if (par.Key == null || par.Value <= Time.time)
                {
                    vencidos.Add(par.Key);
                }
            }
            foreach (Component c in vencidos)
            {
                estado.ListoContra.Remove(c);
            }
        }

        private void Golpear(EstadoMano estado, Component objetivo, Collider choque, Vector3 nudillos, float rapidez, CuerpoAgarradoVR sostenido)
        {
            estado.ListoContra[objetivo] = Time.time + ReglaGolpeManoVR.SegundosEnfriamiento;
            Vector3 direccion = Direccion(estado);
            Vector3 punto = choque is MeshCollider malla && !malla.convex ? nudillos : choque.ClosestPoint(nudillos);
            bool conImplante = agarre != null && agarre.TieneImplante;
            float danio = ReglaGolpeManoVR.Danio(golpe.DanioDelGolpe(objetivo), rapidez, conImplante);
            ResultadoDanio resultado = ((IRecibeDanio)objetivo).RecibirDanio(new InfoDanio(danio, TipoDanio.Golpe, punto, direccion, null));
            Golpes++;
            UltimoDanio = danio;
            UltimaVelocidad = rapidez;
            UltimoObjetivo = objetivo;
            UltimoConDerecha = estado.Mano.Derecha;
            UltimoASostenido = sostenido != null;
            estado.Mano.Vibrar(AmplitudVibracion, SegundosVibracion);
            if (sostenido != null)
            {
                // Reacción doble: el sostenido se sacude en la mano y hace su golpe fuerte; la mano que lo sostiene también vibra.
                sostenido.Reaccionar(direccion);
                if (sostenido.Mano != null)
                {
                    sostenido.Mano.Vibrar(AmplitudSostiene, SegundosSostiene);
                }
                if (resultado.CausoKO || sostenido.EstaDerrotado)
                {
                    int nivel = agarre != null ? agarre.NivelBrazos : 0;
                    sostenido.Lanzar(nivel > 0 ? ReglaAgarreVR.VelocidadGolpe(estado.Mano.VelocidadRelativa, nivel) : Vector3.zero, Vector3.zero);
                }
            }
            // El mismo sonido que el golpe a pie al conectar (SensacionGolpe): carne o metal según el objetivo.
            Sonidos.ReproducirEn(ReglaSonidosJugador.IdGolpeConectado(objetivo.transform.root.name + " " + objetivo.name), punto);
            Sonidos.Reproducir(VozJugador.Id(VozJugador.Esfuerzo)); // Voces de main: el esfuerzo del jugador también al golpear con las manos en VR.
            Golpeo?.Invoke(estado.Mano.Derecha, objetivo, resultado);
        }
    }
}
