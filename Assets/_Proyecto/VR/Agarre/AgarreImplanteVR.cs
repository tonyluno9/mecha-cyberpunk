using MechaCyberpunk.CampoTiro;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR3: con el implante de BRAZOS instalado, el grip de cada mano agarra al enemigo que tenga a menos de 0.4 m de la palma
    /// (SKULLS y demás <see cref="PandilleroAPie"/> que no son jefes, y los maniquíes <see cref="ObjetivoTiro"/> del campo de tiro).
    /// Al soltar el grip sale lanzado con la velocidad de la mano por el multiplicador del implante (×3 nivel 1, ×4 nivel 2);
    /// golpearlo con la otra mano mientras se sostiene lo suelta y lo manda a volar en la dirección del golpe. Sin implante el grip
    /// no agarra a nadie. Se agrega solo al jugador cuando el rig VR está armado (<see cref="AdaptadorVR.Listo"/>).
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
        private const float AmplitudGolpe = 1f;
        private const float SegundosGolpe = 0.2f;

        /// <summary>Para pruebas: true/false fuerza tener o no el implante (nivel 1); null lee <see cref="IImplantes"/>.</summary>
        public static bool? ForzarImplante { get; set; }

        private static readonly Collider[] candidatos = new Collider[MaximoCandidatos];

        private readonly SeguidorImplantes seguidor = new SeguidorImplantes();
        private readonly CuerpoAgarradoVR[] sostenidos = new CuerpoAgarradoVR[2];
        private AdaptadorVR adaptador;

        /// <summary>Nivel de BRAZOS que cuenta para agarrar (0 = sin implante).</summary>
        public int NivelBrazos => ForzarImplante.HasValue ? (ForzarImplante.Value ? 1 : 0) : seguidor.Nivel(EspacioImplante.Brazos);

        public bool TieneImplante => NivelBrazos > 0;

        /// <summary>El enemigo sostenido por la mano derecha, o si no por la izquierda (null si ninguna sostiene).</summary>
        public CuerpoAgarradoVR Agarrado => Sostenido(true) ?? Sostenido(false);

        /// <summary>Cuántos golpes con la otra mano mandaron a volar a un sostenido.</summary>
        public int Golpes { get; private set; }

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
            RevisarGolpe(adaptador.ManoDerecha, adaptador.ManoIzquierda);
            RevisarGolpe(adaptador.ManoIzquierda, adaptador.ManoDerecha);
        }

        private void LeerGrip(ManoVR mano, InputAction grip)
        {
            if (mano == null)
            {
                return;
            }
            if (EntradaVR.Pulso(grip))
            {
                IntentarAgarrar(mano);
            }
            else if (EntradaVR.Solto(grip))
            {
                Soltar(mano);
            }
        }

        /// <summary>Agarra al enemigo más cercano a la palma (a menos de <see cref="ReglaAgarreVR.RadioAgarre"/>). True si agarró.</summary>
        public bool IntentarAgarrar(ManoVR mano)
        {
            if (mano == null || !TieneImplante || Sostenido(mano.Derecha) != null)
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
            Debug.Log("[VR] Agarre con implante: " + mejor.name + " (mano " + (mano.Derecha ? "derecha" : "izquierda") + ").");
            return true;
        }

        /// <summary>Suelta lo que sostiene la mano y lo lanza con la velocidad de la mano por el multiplicador del implante.</summary>
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
            cuerpo.Lanzar(ReglaAgarreVR.VelocidadLanzamiento(mano.Velocidad, NivelBrazos), mano.VelocidadAngular * Mathf.Deg2Rad);
            mano.Vibrar(AmplitudLanzar, SegundosLanzar);
        }

        // La otra mano llega rápido al enemigo sostenido: lo suelta y sale volando en la dirección del golpe, con daño.
        private void RevisarGolpe(ManoVR sostiene, ManoVR otra)
        {
            if (sostiene == null || otra == null || Sostenido(otra.Derecha) != null)
            {
                return;
            }
            CuerpoAgarradoVR cuerpo = Sostenido(sostiene.Derecha);
            if (cuerpo == null)
            {
                return;
            }
            Vector3 palma = otra.Palma;
            Vector3 velocidad = otra.VelocidadRelativa;
            if (!ReglaAgarreVR.EsGolpe(cuerpo.DistanciaA(palma), velocidad.magnitude))
            {
                return;
            }
            sostenidos[sostiene.Derecha ? 1 : 0] = null;
            Golpes++;
            Vector3 direccion = velocidad.normalized;
            cuerpo.Daniar(ReglaAgarreVR.DanioGolpe(seguidor.Efectos.multiplicadorGolpeAPie), palma, direccion);
            cuerpo.Lanzar(ReglaAgarreVR.VelocidadGolpe(velocidad, NivelBrazos), Vector3.Cross(Vector3.up, direccion));
            Sonidos.ReproducirEn(ReglaSonidosJugador.GolpeCarne, palma);
            otra.Vibrar(AmplitudGolpe, SegundosGolpe);
            sostiene.Vibrar(AmplitudLanzar, SegundosLanzar);
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
