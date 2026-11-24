using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR2: golpes con las manos (sin implante). Cada mano es una esfera de <see cref="Radio"/> m en su palma: al entrar en algo que recibe daño
    /// (<see cref="IRecibeDanio"/>, que no sea el propio jugador) a más de 1.5 m/s (velocidad relativa al rig) le hace el daño del puño de
    /// <see cref="GolpeAPie"/> por el factor de rapidez (<see cref="ReglaGolpeManoVR"/>), vibra el control y suena el golpe conectado.
    /// La esfera se barre del punto del cuadro anterior al actual (un golpe rápido no atraviesa). Cada mano espera 0.35 s entre golpes y no
    /// vuelve a pegarle a lo que ya tocaba hasta salir. La mano derecha también pega con el arma en la mano (cachazo, mismo daño de puño).
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class GolpesManosVR : MonoBehaviour
    {
        /// <summary>Radio de la esfera de cada mano (m).</summary>
        public const float Radio = 0.08f;
        private const int MaxColisiones = 16;
        private const float AmplitudVibracion = 0.7f;
        private const float SegundosVibracion = 0.08f;

        private sealed class EstadoMano
        {
            public ManoVR Mano;
            public Vector3 PalmaPrevia;
            public bool HayPrevia;
            public float ListaEn;
            public HashSet<Component> Tocando = new HashSet<Component>();
            public HashSet<Component> TocandoAhora = new HashSet<Component>();
        }

        private AdaptadorVR adaptador;
        private GolpeAPie golpe;
        private VidaAPie vida;
        private ControladorAPie controlador;
        private JugadorAPie jugadorAPie;
        private EstadoMano izquierda;
        private EstadoMano derecha;
        private readonly Collider[] colisiones = new Collider[MaxColisiones];

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
            bool puede = PuedeGolpear();
            Revisar(izquierda, puede);
            Revisar(derecha, puede);
        }

        private bool PuedeGolpear()
        {
            if (vida != null && vida.EstaKO)
            {
                return false;
            }
            if (controlador != null && (!controlador.ControlActivo || controlador.EntradaBloqueada))
            {
                return false;
            }
            return jugadorAPie == null || jugadorAPie.Modo == ModoJugador.APie;
        }

        private void Revisar(EstadoMano estado, bool puede)
        {
            if (estado == null || estado.Mano == null)
            {
                return;
            }
            Vector3 palma = estado.Mano.Palma;
            Vector3 desde = estado.HayPrevia ? estado.PalmaPrevia : palma;
            estado.PalmaPrevia = palma;
            estado.HayPrevia = true;
            estado.TocandoAhora.Clear();
            int n = Physics.OverlapCapsuleNonAlloc(desde, palma, Radio, colisiones, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Transform raiz = transform.root;
            for (int i = 0; i < n; i++)
            {
                Collider c = colisiones[i];
                if (c == null || c.transform.root == raiz)
                {
                    continue;
                }
                Component objetivo = c.GetComponentInParent<IRecibeDanio>() as Component;
                if (objetivo == null || objetivo.transform.root == raiz || !estado.TocandoAhora.Add(objetivo))
                {
                    continue;
                }
                if (puede && !estado.Tocando.Contains(objetivo) && Time.time >= estado.ListaEn)
                {
                    float rapidez = estado.Mano.VelocidadRelativa.magnitude;
                    if (ReglaGolpeManoVR.Golpea(rapidez))
                    {
                        Golpear(estado, objetivo, c, palma, rapidez);
                    }
                }
            }
            // Lo que se tocaba y ya no, se puede volver a golpear.
            HashSet<Component> previo = estado.Tocando;
            estado.Tocando = estado.TocandoAhora;
            estado.TocandoAhora = previo;
        }

        private void Golpear(EstadoMano estado, Component objetivo, Collider choque, Vector3 palma, float rapidez)
        {
            estado.ListaEn = Time.time + ReglaGolpeManoVR.SegundosEnfriamiento;
            Vector3 direccion = estado.Mano.VelocidadRelativa.sqrMagnitude > 1e-6f ? estado.Mano.VelocidadRelativa.normalized : estado.Mano.transform.forward;
            Vector3 punto = choque is MeshCollider malla && !malla.convex ? palma : choque.ClosestPoint(palma);
            float danio = golpe.DanioDelGolpe(objetivo) * ReglaGolpeManoVR.Factor(rapidez);
            ResultadoDanio resultado = ((IRecibeDanio)objetivo).RecibirDanio(new InfoDanio(danio, TipoDanio.Golpe, punto, direccion, null));
            Golpes++;
            UltimoDanio = danio;
            UltimaVelocidad = rapidez;
            UltimoObjetivo = objetivo;
            UltimoConDerecha = estado.Mano.Derecha;
            estado.Mano.Vibrar(AmplitudVibracion, SegundosVibracion);
            // El mismo sonido que el golpe a pie al conectar (SensacionGolpe): carne o metal según el objetivo.
            Sonidos.ReproducirEn(ReglaSonidosJugador.IdGolpeConectado(objetivo.transform.root.name + " " + objetivo.name), punto);
            Golpeo?.Invoke(estado.Mano.Derecha, objetivo, resultado);
        }
    }
}
