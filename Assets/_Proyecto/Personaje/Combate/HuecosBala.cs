using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Huecos de bala (T-808): escucha <see cref="PistolaAPie.BalaImpacto"/> de todas las pistolas (las de la escena al cargarla y la del jugador
    /// cuando se registra en <see cref="Registro{T}"/> de <see cref="IJugador"/>) y, si la bala no pegó en una persona, pega un hueco en la
    /// superficie: un quad alineado con la normal y un poco separado, de concreto (D01) o de metal (D02) si es un carro (cuerpo rígido,
    /// <see cref="IMontable"/> o nombre con "Carro", "Metal", "Patrulla"...). Los huecos salen de un montón fijo (el más viejo se reusa), siguen
    /// al carro si se mueve y se apagan solos. Se crea solo al arrancar si existe <see cref="ConfigHuecosBala"/> en Resources; no toca PistolaAPie
    /// (su cubito MarcaImpacto queda invisible con Greybox_Marca).
    /// </summary>
    public class HuecosBala : MonoBehaviour
    {
        public const string NombreRecurso = "ConfigHuecosBala";
        private const float NormalMinima = 0.0001f;
        private const float GradosPorVuelta = 360f;

        private static readonly int IdColor = Shader.PropertyToID("_Color");
        private static HuecosBala actual;

        private sealed class Hueco
        {
            public Transform Transform;
            public Renderer Render;
            public float Edad;
            public bool Activo;
            public bool Metal;
            // T-931: la superficie donde quedó y, si no está pegado a un cuerpo ni es estática, el punto en su espacio local para ver si se movió.
            public Transform Superficie;
            public Vector3 Local;
            public Vector3 Mundo;
            public bool Vigila;
        }

        [SerializeField] private ConfigHuecosBala config;

        private readonly List<Hueco> huecos = new List<Hueco>();
        private readonly HashSet<PistolaAPie> suscritas = new HashSet<PistolaAPie>();
        private readonly List<string> nombres = new List<string>();
        private MaterialPropertyBlock bloque;
        private int siguiente;

        /// <summary>El administrador de la partida, o null si no hay config.</summary>
        public static HuecosBala Actual => actual;

        public ConfigHuecosBala Config => config;

        /// <summary>Huecos que se ven ahora.</summary>
        public int Activos
        {
            get
            {
                int n = 0;
                foreach (Hueco h in huecos)
                {
                    n += h.Activo && h.Transform != null ? 1 : 0;
                }
                return n;
            }
        }

        /// <summary>El último hueco puesto (para las pruebas), o null.</summary>
        public Transform Ultimo { get; private set; }

        /// <summary>True si el último hueco fue de metal.</summary>
        public bool UltimoEsMetal { get; private set; }

        /// <summary>True si ya escucha a esa pistola.</summary>
        public bool Escucha(PistolaAPie pistola) => suscritas.Contains(pistola);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => actual = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar() => Crear();

        /// <summary>Crea el administrador (uno por partida) si hay config en Resources; si ya existe, lo devuelve.</summary>
        public static HuecosBala Crear()
        {
            if (actual != null)
            {
                return actual;
            }
            ConfigHuecosBala datos = Resources.Load<ConfigHuecosBala>(NombreRecurso);
            if (datos == null)
            {
                return null;
            }
            GameObject go = new GameObject("HuecosBala");
            go.SetActive(false);
            HuecosBala nuevo = go.AddComponent<HuecosBala>();
            nuevo.config = datos;
            DontDestroyOnLoad(go);
            go.SetActive(true);
            return nuevo;
        }

        private void Awake()
        {
            if (actual != null && actual != this)
            {
                Destroy(gameObject);
                return;
            }
            if (config == null)
            {
                Debug.LogError("[Personaje] HuecosBala sin ConfigHuecosBala en " + name, this);
                enabled = false;
                return;
            }
            actual = this;
            bloque = new MaterialPropertyBlock();
        }

        private void OnDestroy()
        {
            if (actual == this)
            {
                actual = null;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += AlCargarEscena;
            Registro<IJugador>.Agregado += AlAgregarJugador;
            BuscarPistolas();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            Registro<IJugador>.Agregado -= AlAgregarJugador;
            foreach (PistolaAPie p in suscritas)
            {
                if (p != null)
                {
                    p.BalaImpacto -= AlImpactar;
                }
            }
            suscritas.Clear();
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (modo == LoadSceneMode.Single)
            {
                ApagarTodos();
            }
            BuscarPistolas();
        }

        private void AlAgregarJugador(IJugador jugador)
        {
            if (jugador is Component componente && componente != null)
            {
                foreach (PistolaAPie p in componente.GetComponentsInChildren<PistolaAPie>(true))
                {
                    Escuchar(p);
                }
            }
        }

        /// <summary>Busca las pistolas de la escena (al cargarla, no cada cuadro).</summary>
        public void BuscarPistolas()
        {
            suscritas.RemoveWhere(p => p == null);
            foreach (PistolaAPie p in FindObjectsByType<PistolaAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Escuchar(p);
            }
        }

        /// <summary>Empieza a escuchar una pistola (para las que aparecen después).</summary>
        public void Escuchar(PistolaAPie pistola)
        {
            if (pistola != null && suscritas.Add(pistola))
            {
                pistola.BalaImpacto += AlImpactar;
            }
        }

        private void AlImpactar(ArmaAPie arma, ImpactoBala impacto) => SimularImpacto(impacto);

        /// <summary>
        /// Lo mismo que un impacto de pistola (también lo usan las pruebas): si no fue en una persona, pone el hueco. True si lo puso.
        /// </summary>
        public bool SimularImpacto(ImpactoBala impacto)
        {
            if (!isActiveAndEnabled || impacto.EnPersona || impacto.Normal.sqrMagnitude < NormalMinima)
            {
                return false;
            }
            Vector3 normal = impacto.Normal.normalized;
            Collider superficie = null;
            if (Physics.Raycast(impacto.Punto + normal * config.Sondeo, -normal, out RaycastHit golpe, config.Sondeo * 2f, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                superficie = golpe.collider;
            }
            // T-931: sin superficie visible en ese punto (aire, colliders invisibles, triggers) no hay hueco: nada de marcas flotantes.
            if (superficie == null || !ReglaHuecosBala.HayHueco(superficie.isTrigger, impacto.EnPersona, SuperficieImpacto.Visible(superficie, impacto.Punto)))
            {
                return false;
            }
            bool metal = EsMetal(superficie);
            Hueco h = Tomar();
            float tamano = metal ? config.TamanoMetal : config.TamanoConcreto;
            h.Transform.SetParent(transform, false);
            h.Transform.position = impacto.Punto + normal * config.Separacion;
            // El quad se ve por -Z: su frente queda hacia afuera de la superficie; un giro al azar para que no se vean todos iguales.
            h.Transform.rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, Random.value * GradosPorVuelta);
            h.Transform.localScale = Vector3.one * tamano;
            // En un carro el hueco se va con él.
            bool pegado = metal && superficie.attachedRigidbody != null;
            if (pegado)
            {
                h.Transform.SetParent(superficie.attachedRigidbody.transform, true);
            }
            // T-931: si la superficie no es estática y el hueco no va pegado a su cuerpo, se guarda dónde quedó para borrarlo si se mueve.
            h.Superficie = superficie.transform;
            h.Mundo = impacto.Punto;
            h.Local = superficie.transform.InverseTransformPoint(impacto.Punto);
            h.Vigila = !pegado && !superficie.gameObject.isStatic;
            h.Render.sharedMaterial = metal ? config.MaterialMetal : config.MaterialConcreto;
            h.Edad = 0f;
            h.Metal = metal;
            h.Activo = true;
            PintarAlfa(h, 1f);
            h.Transform.gameObject.SetActive(true);
            Ultimo = h.Transform;
            UltimoEsMetal = metal;
            return true;
        }

        private bool EsMetal(Collider c)
        {
            nombres.Clear();
            for (Transform t = c.transform; t != null; t = t.parent)
            {
                nombres.Add(t.name);
            }
            Renderer r = c.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                nombres.Add(r.sharedMaterial.name);
            }
            if (c.sharedMaterial != null)
            {
                nombres.Add(c.sharedMaterial.name);
            }
            return ReglaHuecosBala.EsMetal(nombres, c.attachedRigidbody != null, c.GetComponentInParent<IMontable>() != null, config.PalabrasMetal);
        }

        // El siguiente del montón: uno libre o el más viejo. Si su carro se destruyó con él, se hace otro.
        private Hueco Tomar()
        {
            if (huecos.Count < config.MaximoHuecos)
            {
                Hueco nuevo = CrearHueco();
                huecos.Add(nuevo);
                return nuevo;
            }
            siguiente %= huecos.Count;
            Hueco h = huecos[siguiente];
            siguiente++;
            if (h.Transform == null)
            {
                Hueco otro = CrearHueco();
                h.Transform = otro.Transform;
                h.Render = otro.Render;
            }
            return h;
        }

        private Hueco CrearHueco()
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "HuecoBala";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);
            Renderer r = quad.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            quad.SetActive(false);
            return new Hueco { Transform = quad.transform, Render = r };
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (Hueco h in huecos)
            {
                if (!h.Activo)
                {
                    continue;
                }
                if (h.Transform == null)
                {
                    h.Activo = false;
                    continue;
                }
                // T-931: la superficie se destruyó, se apagó o se movió más de 2 cm: el hueco se va con ella.
                if (h.Superficie == null || !h.Superficie.gameObject.activeInHierarchy
                    || (h.Vigila && ReglaHuecosBala.SeMovio(h.Mundo, h.Superficie.TransformPoint(h.Local))))
                {
                    Apagar(h);
                    continue;
                }
                h.Edad += dt;
                if (h.Edad <= config.SegundosVisible)
                {
                    continue;
                }
                float alfa = ReglaHuecosBala.Alfa(h.Edad, config.SegundosVisible, config.SegundosApagado);
                if (alfa <= 0f)
                {
                    Apagar(h);
                }
                else
                {
                    PintarAlfa(h, alfa);
                }
            }
        }

        private void PintarAlfa(Hueco h, float alfa)
        {
            h.Render.GetPropertyBlock(bloque);
            bloque.SetColor(IdColor, new Color(1f, 1f, 1f, alfa));
            h.Render.SetPropertyBlock(bloque);
        }

        private void Apagar(Hueco h)
        {
            h.Activo = false;
            if (h.Transform != null)
            {
                h.Transform.gameObject.SetActive(false);
                h.Transform.SetParent(transform, false);
            }
        }

        private void ApagarTodos()
        {
            foreach (Hueco h in huecos)
            {
                Apagar(h);
            }
        }
    }
}
