using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Habilidades
{
    /// <summary>
    /// T-1017: lo que hacen las prótesis de ataque del Doc al usarlas desde la barra (M31):
    /// - [4] LÁSER: mientras dura, el brazo derecho dispara un rayo rojo hacia la mira que daña por segundo a todo lo que toca (atraviesa a la gente,
    ///   para en las paredes).
    /// - [5] MISILES: la hombrera lanza varios misiles teledirigidos, uno tras otro, a los enemigos más cercanos al frente (o a la mira si no hay); cada uno
    ///   explota en área (<see cref="ExplosionAPie"/>).
    /// La agrega <see cref="HabilidadesProtesis"/>; los números salen de <see cref="ConfigHabilidadesProtesis"/>.
    /// </summary>
    [RequireComponent(typeof(HabilidadesProtesis))]
    public class ProtesisAtaqueAPie : MonoBehaviour
    {
        private const float TicDanio = 0.1f;
        private const int MaxToques = 32;
        private const int MaxCercanos = 64;
        private const float AlturaHombro = 1.65f;
        private const float AlturaMano = 1.3f;
        private const float LadoHombro = 0.28f;
        private const float AdelanteMano = 0.45f;
        private const float LatidoLaser = 22f;

        private HabilidadesProtesis habilidades;
        private ControladorAPie controlador;
        private PistolaAPie pistola;
        private ProtesisVisiblesAPie visibles;
        private Animator animador;
        private ConfigHabilidadesProtesis cfg;
        private LineRenderer laser;
        private Light luzLaser;
        private float acumulado;
        private Vector3 finLaser;
        private readonly RaycastHit[] toques = new RaycastHit[MaxToques];
        private readonly Collider[] cercanos = new Collider[MaxCercanos];
        private readonly HashSet<IRecibeDanio> tocados = new HashSet<IRecibeDanio>();
        private readonly List<Transform> blancos = new List<Transform>();

        /// <summary>True mientras se dibuja el láser.</summary>
        public bool LaserVisible => laser != null && laser.enabled;

        /// <summary>Donde termina el láser ahora.</summary>
        public Vector3 FinLaser => finLaser;

        /// <summary>Cuántos misiles se han lanzado (para las pruebas).</summary>
        public int MisilesLanzados { get; private set; }

        private void Awake()
        {
            habilidades = GetComponent<HabilidadesProtesis>();
            controlador = GetComponent<ControladorAPie>();
            pistola = GetComponent<PistolaAPie>();
            visibles = GetComponent<ProtesisVisiblesAPie>();
        }

        private void Start()
        {
            cfg = habilidades.Config;
            animador = GetComponentInChildren<Animator>();
            ConfigArmasEspeciales especiales = ConfigArmasEspeciales.Actual;
            laser = new GameObject("LaserBrazo").AddComponent<LineRenderer>();
            laser.useWorldSpace = true;
            laser.positionCount = 2;
            laser.numCapVertices = 3;
            laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            laser.receiveShadows = false;
            laser.sharedMaterial = especiales.MaterialRayo != null ? especiales.MaterialRayo : new Material(Shader.Find("Sprites/Default"));
            if (cfg != null)
            {
                laser.startColor = cfg.ColorLaser;
                laser.endColor = cfg.ColorLaser;
                laser.widthMultiplier = cfg.AnchoLaser;
            }
            laser.enabled = false;
            luzLaser = new GameObject("LuzLaser").AddComponent<Light>();
            luzLaser.type = LightType.Point;
            luzLaser.range = 3f;
            luzLaser.color = cfg != null ? cfg.ColorLaser : Color.red;
            luzLaser.enabled = false;
        }

        private void OnEnable()
        {
            if (habilidades == null)
            {
                habilidades = GetComponent<HabilidadesProtesis>();
            }
            habilidades.Activada += AlActivar;
        }

        private void OnDisable()
        {
            habilidades.Activada -= AlActivar;
            Apagar();
        }

        private void OnDestroy()
        {
            if (laser != null) Destroy(laser.gameObject);
            if (luzLaser != null) Destroy(luzLaser.gameObject);
        }

        private void AlActivar(HabilidadProtesis h)
        {
            if (h == HabilidadProtesis.Misiles && cfg != null)
            {
                StartCoroutine(LanzarMisiles());
            }
            if (h == HabilidadProtesis.Laser)
            {
                acumulado = TicDanio;
                Sonidos.Reproducir(MechaCyberpunk.Personaje.Combate.SonidosArmasT1472.LaserDisparo); // T-1472: láser del jugador, 2D
            }
        }

        private void Update()
        {
            if (cfg == null)
            {
                return;
            }
            if (habilidades.Estado(HabilidadProtesis.Laser) == EstadoHabilidad.Activa)
            {
                Laser();
            }
            else
            {
                Apagar();
            }
        }

        private void Apagar()
        {
            if (laser != null)
            {
                laser.enabled = false;
            }
            if (luzLaser != null)
            {
                luzLaser.enabled = false;
            }
        }

        // La mano derecha (del Animator si hay) o un punto al frente a la altura de la mano.
        // T-1018: el rayo sale de la punta del brazo láser si está puesto.
        private Vector3 Mano()
        {
            if (visibles != null && visibles.PuntaLaser != null)
            {
                return visibles.PuntaLaser.position;
            }
            Transform hueso = animador != null && animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.RightHand) : null;
            return hueso != null ? hueso.position : transform.position + Vector3.up * AlturaMano + transform.forward * AdelanteMano + transform.right * LadoHombro;
        }

        // T-1018: los misiles salen de la hombrera si está puesta.
        private Vector3 Hombro()
        {
            if (visibles != null && visibles.SalidaMisiles != null)
            {
                return visibles.SalidaMisiles.position;
            }
            Transform hueso = animador != null && animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.LeftShoulder) : null;
            Vector3 baseHombro = hueso != null ? hueso.position : transform.position + Vector3.up * AlturaHombro - transform.right * LadoHombro;
            return baseHombro + Vector3.up * 0.25f;
        }

        private Vector3 Mira(float alcance)
        {
            if (pistola != null)
            {
                return pistola.PuntoDeMira(alcance);
            }
            Transform camara = controlador != null ? controlador.Camara : null;
            return camara != null ? camara.position + camara.forward * alcance : transform.position + transform.forward * alcance;
        }

        // ---- Láser ----

        private void Laser()
        {
            controlador?.MirarAlDisparar();
            Vector3 origen = Mano();
            Vector3 hacia = Mira(cfg.AlcanceLaser) - origen;
            Vector3 direccion = hacia.sqrMagnitude > 0.0001f ? hacia.normalized : transform.forward;
            float largo = cfg.AlcanceLaser;
            tocados.Clear();
            int n = Physics.SphereCastNonAlloc(origen, cfg.RadioLaser, direccion, toques, cfg.AlcanceLaser, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(toques, 0, n, Comparador);
            for (int i = 0; i < n; i++)
            {
                RaycastHit t = toques[i];
                if (t.collider.transform.root == transform.root)
                {
                    continue;
                }
                IRecibeDanio objetivo = t.collider.GetComponentInParent<IRecibeDanio>();
                if (objetivo != null)
                {
                    tocados.Add(objetivo);
                }
                if (objetivo == null || objetivo is IObjetoInerte)
                {
                    largo = Mathf.Max(0f, t.distance);
                    break;
                }
            }
            finLaser = origen + direccion * largo;
            acumulado += Time.deltaTime;
            if (acumulado >= TicDanio)
            {
                float danio = cfg.DanioLaser * acumulado;
                acumulado = 0f;
                foreach (IRecibeDanio objetivo in tocados)
                {
                    objetivo.RecibirDanio(new InfoDanio(danio, TipoDanio.Disparo, finLaser, direccion, null));
                }
            }
            laser.enabled = true;
            laser.SetPosition(0, origen);
            laser.SetPosition(1, finLaser);
            laser.widthMultiplier = cfg.AnchoLaser * (0.8f + 0.2f * Mathf.Sin(Time.time * LatidoLaser));
            luzLaser.enabled = true;
            luzLaser.transform.position = finLaser - direccion * 0.2f;
            luzLaser.intensity = 3f;
        }

        private static readonly IComparer<RaycastHit> Comparador = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        // ---- Misiles ----

        private IEnumerator LanzarMisiles()
        {
            BuscarBlancos();
            if (blancos.Count > 0)
            {
                Sonidos.Reproducir(MechaCyberpunk.Personaje.Combate.SonidosArmasT1472.MisilesFijar); // T-1472: fijó blancos
            }
            Sonidos.Reproducir("ARM_W02_DISPARO"); // T-1472: 2D, es del jugador
            for (int i = 0; i < cfg.CantidadMisiles; i++)
            {
                Transform blanco = blancos.Count > 0 ? blancos[i % blancos.Count] : null;
                Vector3 punto = blanco == null ? Mira(cfg.BusquedaMisil) : Vector3.zero;
                Vector3 salida = (Vector3.up * 1.2f + transform.forward * 0.6f + (i % 2 == 0 ? transform.right : -transform.right) * 0.35f).normalized;
                MisilAPie.Lanzar(Hombro(), salida * cfg.VelocidadMisil, blanco, punto, cfg, transform.root);
                MisilesLanzados++;
                EventosRuido.PublicarDisparo(transform.position);
                if (cfg.SegundosEntreMisiles > 0f)
                {
                    yield return new WaitForSeconds(cfg.SegundosEntreMisiles);
                }
            }
        }

        // Los que reciben daño y no son objetos, al frente de la cámara y a la distancia de búsqueda, del más cercano al más lejano.
        private void BuscarBlancos()
        {
            blancos.Clear();
            Transform camara = controlador != null ? controlador.Camara : null;
            Vector3 frente = camara != null ? camara.forward : transform.forward;
            frente.y = 0f;
            frente = frente.sqrMagnitude > 0.0001f ? frente.normalized : transform.forward;
            HashSet<Transform> vistos = new HashSet<Transform>();
            List<(float d, Transform t)> lista = new List<(float, Transform)>();
            int n = Physics.OverlapSphereNonAlloc(transform.position, cfg.BusquedaMisil, cercanos, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = cercanos[i];
                if (c.transform.root == transform.root)
                {
                    continue;
                }
                IRecibeDanio objetivo = c.GetComponentInParent<IRecibeDanio>();
                if (objetivo == null || objetivo is IObjetoInerte)
                {
                    continue;
                }
                IVida vida = c.GetComponentInParent<IVida>();
                if (vida != null && vida.EstaKO)
                {
                    continue;
                }
                Transform t = ((Component)objetivo).transform;
                if (!vistos.Add(t))
                {
                    continue;
                }
                Vector3 hacia = t.position - transform.position;
                hacia.y = 0f;
                if (hacia.sqrMagnitude > 0.01f && Vector3.Angle(frente, hacia) > cfg.ConoMisil)
                {
                    continue;
                }
                lista.Add((hacia.magnitude, t));
            }
            lista.Sort((a, b) => a.d.CompareTo(b.d));
            foreach (var x in lista)
            {
                blancos.Add(x.t);
            }
        }
    }
}
