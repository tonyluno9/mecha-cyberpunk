using MechaCyberpunk.CampoTiro;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR3/VR35: el grip de cada mano sujeta al enemigo que tenga a menos de 0.5 m de la palma (SKULLS y demás <see cref="PandilleroAPie"/> que no
    /// son jefes, y los maniquíes <see cref="ObjetivoTiro"/> del campo de tiro), con o sin implante: su IA se pausa y queda pegado a la mano
    /// (<see cref="CuerpoAgarradoVR"/>). Los golpes de la otra mano le pegan (<see cref="GolpesManosVR"/>); al morir se suelta solo. Al soltar el
    /// grip se libera; con el implante de BRAZOS además sale lanzado con la velocidad de la mano por el multiplicador (×3 nivel 1, ×4 nivel 2).
    /// Se agrega solo al jugador cuando el rig VR está armado (<see cref="AdaptadorVR.Listo"/>).
    /// No agarra al jugador, a civiles, peatones ni policías de tráfico (solo los tipos de arriba).
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class AgarreImplanteVR : MonoBehaviour
    {
        private const int MaximoCandidatos = 32;
        private const float AmplitudAgarre = 0.5f;
        private const float SegundosAgarre = 0.08f;
        private const float AmplitudLanzar = 0.8f;
        private const float SegundosLanzar = 0.12f;

        /// <summary>Para pruebas: true/false fuerza tener o no el implante (nivel 1); null lee <see cref="IImplantes"/>.</summary>
        public static bool? ForzarImplante { get; set; }

        private static readonly Collider[] candidatos = new Collider[MaximoCandidatos];

        private readonly SeguidorImplantes seguidor = new SeguidorImplantes();
        private readonly CuerpoAgarradoVR[] sostenidos = new CuerpoAgarradoVR[2];
        private AdaptadorVR adaptador;
        private ArmasAPie armas;

        /// <summary>Nivel de BRAZOS que cuenta para agarrar (0 = sin implante).</summary>
        public int NivelBrazos => ForzarImplante.HasValue ? (ForzarImplante.Value ? 1 : 0) : seguidor.Nivel(EspacioImplante.Brazos);

        public bool TieneImplante => NivelBrazos > 0;

        /// <summary>El enemigo sostenido por la mano derecha, o si no por la izquierda (null si ninguna sostiene).</summary>
        public CuerpoAgarradoVR Agarrado => Sostenido(true) ?? Sostenido(false);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            ForzarImplante = null;
            AdaptadorVR.Listo -= AlListo;
            AdaptadorVR.Listo += AlListo;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RevisarActual()
        {
            if (AdaptadorVR.Actual != null && AdaptadorVR.Actual.Armado)
            {
                AlListo(AdaptadorVR.Actual);
            }
        }

        private static void AlListo(AdaptadorVR rig)
        {
            if (rig != null && rig.GetComponent<AgarreImplanteVR>() == null)
            {
                rig.gameObject.AddComponent<AgarreImplanteVR>();
            }
        }

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            armas = GetComponent<ArmasAPie>();
            if (adaptador == null)
            {
                Debug.LogError("[VR] AgarreImplanteVR sin AdaptadorVR en " + name, this);
                enabled = false;
            }
        }

        private void OnEnable() => seguidor.Activar();

        private void OnDisable()
        {
            seguidor.Desactivar();
            SoltarTodo();
        }

        /// <summary>El enemigo que sostiene la mano (null si ninguno).</summary>
        public CuerpoAgarradoVR Sostenido(bool derecha)
        {
            CuerpoAgarradoVR c = sostenidos[derecha ? 1 : 0];
            if (c == null || c.EstadoActual != CuerpoAgarradoVR.Estado.Sostenido)
            {
                sostenidos[derecha ? 1 : 0] = null;
                return null;
            }
            return c;
        }

        private void Update()
        {
            if (adaptador == null || !adaptador.Armado || Pausa.Activa)
            {
                return;
            }
            LeerGrip(adaptador.ManoDerecha, EntradaVR.GripDerecho);
            LeerGrip(adaptador.ManoIzquierda, EntradaVR.GripIzquierdo);
        }

        private void LeerGrip(ManoVR mano, InputAction grip)
        {
            if (mano == null)
            {
                return;
            }
            if (EntradaVR.Pulso(grip))
            {
                // VR5: con la granada en la mano el grip derecho toma la granada (ArmasEspecialesVR), no agarra.
                // VR20: con las dos manos en la pose de un gesto de prótesis el grip es el gesto, no agarra.
                // VR27: un grip en la cadera o el hombro es de sacar arma o tomar cargador (GestosArmaVR), no agarra.
                if (!MechaCyberpunk.VR.Guia.GestosProtesisVR.EnPose && !GestosArmaVR.GripTomado(mano.Derecha) && !(mano.Derecha && armas != null && ArmasEspecialesVR.TomaElGrip(armas.ArmaActiva)))
                {
                    IntentarAgarrar(mano);
                }
            }
            else if (EntradaVR.Solto(grip))
            {
                Soltar(mano);
            }
        }

        /// <summary>Agarra al enemigo más cercano a la palma (a menos de <see cref="ReglaAgarreVR.RadioAgarre"/>). True si agarró.</summary>
        public bool IntentarAgarrar(ManoVR mano)
        {
            if (mano == null || Sostenido(mano.Derecha) != null)
            {
                return false;
            }
            Vector3 palma = mano.Palma;
            int n = Physics.OverlapSphereNonAlloc(palma, ReglaAgarreVR.RadioAgarre, candidatos, ~0, QueryTriggerInteraction.Collide);
            GameObject mejor = null;
            float mejorDistancia = float.MaxValue;
            CuerpoAgarradoVR otraMano = Sostenido(!mano.Derecha);
            for (int i = 0; i < n; i++)
            {
                Collider c = candidatos[i];
                candidatos[i] = null;
                if (c == null || c.transform.IsChildOf(transform))
                {
                    continue;
                }
                GameObject raiz = EnemigoAgarrable(c);
                if (raiz == null || (otraMano != null && otraMano.gameObject == raiz))
                {
                    continue;
                }
                float d = Distancia(c, palma);
                if (d < mejorDistancia)
                {
                    mejorDistancia = d;
                    mejor = raiz;
                }
            }
            if (mejor == null)
            {
                return false;
            }
            CuerpoAgarradoVR cuerpo = CuerpoAgarradoVR.Agarrar(mejor, mano, transform);
            if (cuerpo == null)
            {
                return false;
            }
            sostenidos[mano.Derecha ? 1 : 0] = cuerpo;
            mano.Vibrar(AmplitudAgarre, SegundosAgarre);
            Debug.Log("[VR] Agarre" + (TieneImplante ? " con implante: " : ": ") + mejor.name + " (mano " + (mano.Derecha ? "derecha" : "izquierda") + ").");
            return true;
        }

        /// <summary>Suelta lo que sostiene la mano; con el implante lo lanza con la velocidad de la mano por el multiplicador.</summary>
        public void Soltar(ManoVR mano)
        {
            if (mano == null)
            {
                return;
            }
            CuerpoAgarradoVR cuerpo = Sostenido(mano.Derecha);
            if (cuerpo == null)
            {
                return;
            }
            sostenidos[mano.Derecha ? 1 : 0] = null;
            if (TieneImplante)
            {
                cuerpo.Lanzar(ReglaAgarreVR.VelocidadLanzamiento(mano.Velocidad, NivelBrazos), mano.VelocidadAngular * Mathf.Deg2Rad);
                mano.Vibrar(AmplitudLanzar, SegundosLanzar);
            }
            else
            {
                // VR35: sin implante solo lo libera (cae donde estaba y vuelve a su IA).
                cuerpo.Lanzar(Vector3.zero, Vector3.zero);
            }
        }

        private void SoltarTodo()
        {
            for (int i = 0; i < sostenidos.Length; i++)
            {
                CuerpoAgarradoVR c = sostenidos[i];
                sostenidos[i] = null;
                if (c != null && c.EstadoActual == CuerpoAgarradoVR.Estado.Sostenido)
                {
                    c.Lanzar(Vector3.zero, Vector3.zero);
                }
            }
        }

        /// <summary>La raíz del enemigo agarrable al que pertenece <paramref name="c"/>, o null si no es uno.</summary>
        public static GameObject EnemigoAgarrable(Collider c)
        {
            PandilleroAPie pandillero = c.GetComponentInParent<PandilleroAPie>();
            if (pandillero != null)
            {
                CuerpoAgarradoVR volando = pandillero.GetComponent<CuerpoAgarradoVR>();
                bool vivo = !pandillero.EstaKO && !pandillero.EsJefe;
                bool activo = pandillero.isActiveAndEnabled || (volando != null && volando.EstadoActual == CuerpoAgarradoVR.Estado.Volando);
                return vivo && activo ? pandillero.gameObject : null;
            }
            ObjetivoTiro objetivo = c.GetComponentInParent<ObjetivoTiro>();
            if (objetivo != null && objetivo.EstaListo && !objetivo.EstaCaido)
            {
                return objetivo.gameObject;
            }
            return null;
        }

        private static float Distancia(Collider c, Vector3 punto)
        {
            bool soportado = !(c is MeshCollider malla) || malla.convex;
            Vector3 cercano = soportado ? c.ClosestPoint(punto) : c.bounds.ClosestPoint(punto);
            return Vector3.Distance(cercano, punto);
        }
    }
}
