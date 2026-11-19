using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1013: la granada que vuela en arco (del lanzagranadas o lanzada con la mano). Avanza con su velocidad y la gravedad y revisa con un SphereCast
    /// cada tramo (sin chocar con quien la lanzó). La del lanzagranadas explota al tocar algo; la lanzada rebota y explota al acabarse la mecha o al tocar
    /// a alguien. La explosión es <see cref="ExplosionAPie"/>.
    /// </summary>
    public class ProyectilAPie : MonoBehaviour
    {
        private const float RadioChoque = 0.08f;
        private const float VelocidadMinimaRebote = 0.8f;
        private const float SegundosMaximos = 12f;

        private Vector3 velocidad;
        private float radio;
        private float danio;
        private bool explotaAlChocar;
        private float mecha;
        private float rebote;
        private Transform raizLanzador;
        private float vida;
        private bool explotado;
        private bool quieta;
        private float proximoRebote;
        private const float SegundosEntreRebotes = 0.15f;

        /// <summary>True mientras sigue en el aire o en el piso sin explotar.</summary>
        public bool Viva => !explotado;

        public Vector3 Velocidad => velocidad;

        /// <summary>Crea y lanza una granada desde <paramref name="origen"/> con <paramref name="velocidadInicial"/>.</summary>
        public static ProyectilAPie Lanzar(Vector3 origen, Vector3 velocidadInicial, float radio, float danio, bool explotaAlChocar, float mecha,
            float rebote, Transform raizLanzador)
        {
            ConfigArmasEspeciales cfg = ConfigArmasEspeciales.Actual;
            GameObject objeto;
            if (cfg.PrefabGranada != null)
            {
                objeto = Instantiate(cfg.PrefabGranada, origen, Quaternion.LookRotation(velocidadInicial.sqrMagnitude > 0.0001f ? velocidadInicial : Vector3.forward));
                foreach (Collider c in objeto.GetComponentsInChildren<Collider>(true))
                {
                    Destroy(c);
                }
            }
            else
            {
                objeto = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(objeto.GetComponent<Collider>());
                objeto.transform.position = origen;
                objeto.transform.localScale = Vector3.one * cfg.TamanoGranada;
                Renderer r = objeto.GetComponent<Renderer>();
                r.material.color = new Color(0.18f, 0.22f, 0.16f, 1f);
            }
            objeto.name = explotaAlChocar ? "ProyectilLanzagranadas" : "Granada";
            TrailRenderer estela = objeto.AddComponent<TrailRenderer>();
            estela.time = 0.25f;
            estela.startWidth = cfg.TamanoGranada * 0.6f;
            estela.endWidth = 0f;
            estela.sharedMaterial = cfg.MaterialRayo != null ? cfg.MaterialRayo : new Material(Shader.Find("Sprites/Default"));
            estela.startColor = new Color(1f, 0.8f, 0.5f, 0.6f);
            estela.endColor = new Color(1f, 0.5f, 0.2f, 0f);
            estela.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ProyectilAPie p = objeto.AddComponent<ProyectilAPie>();
            p.velocidad = velocidadInicial;
            p.radio = radio;
            p.danio = danio;
            p.explotaAlChocar = explotaAlChocar;
            p.mecha = mecha;
            p.rebote = rebote;
            p.raizLanzador = raizLanzador;
            return p;
        }

        private void Update()
        {
            if (explotado)
            {
                return;
            }
            float dt = Time.deltaTime;
            vida += dt;
            if ((!explotaAlChocar && vida >= mecha) || vida >= SegundosMaximos)
            {
                Explotar(transform.position);
                return;
            }
            if (quieta)
            {
                return;
            }
            Vector3 desde = transform.position;
            Vector3 nueva = velocidad + Physics.gravity * dt;
            Vector3 paso = (velocidad + nueva) * 0.5f * dt;
            velocidad = nueva;
            float largo = paso.magnitude;
            if (largo > 0.0001f && Chocar(desde, paso / largo, largo, out RaycastHit golpe))
            {
                bool esAlguien = golpe.collider.GetComponentInParent<IRecibeDanio>() != null && golpe.collider.GetComponentInParent<IObjetoInerte>() == null;
                if (explotaAlChocar || esAlguien)
                {
                    Explotar(golpe.point + golpe.normal * 0.05f);
                    return;
                }
                // T-1472: el rebote suena, con un tiempo mínimo para que no sea una ametralladora de golpecitos.
                if (Time.time >= proximoRebote)
                {
                    proximoRebote = Time.time + SegundosEntreRebotes;
                    Sonidos.ReproducirEn(SonidosArmasT1472.GranadaRebote, golpe.point);
                }
                // Rebota: refleja la velocidad y pierde fuerza; si ya casi no se mueve, se queda en el piso.
                transform.position = golpe.point + golpe.normal * RadioChoque;
                velocidad = Vector3.Reflect(velocidad, golpe.normal) * rebote;
                if (velocidad.magnitude < VelocidadMinimaRebote && golpe.normal.y > 0.5f)
                {
                    velocidad = Vector3.zero;
                    quieta = true;
                }
                return;
            }
            transform.position = desde + paso;
            if (velocidad.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(velocidad);
            }
        }

        private readonly RaycastHit[] golpes = new RaycastHit[8];

        private bool Chocar(Vector3 desde, Vector3 direccion, float largo, out RaycastHit mejor)
        {
            mejor = default;
            bool hay = false;
            int n = Physics.SphereCastNonAlloc(desde, RadioChoque, direccion, golpes, largo, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                RaycastHit g = golpes[i];
                if (g.collider.transform.root == transform || (raizLanzador != null && g.collider.transform.root == raizLanzador) || g.distance <= 0f)
                {
                    continue;
                }
                if (!hay || g.distance < mejor.distance)
                {
                    mejor = g;
                    hay = true;
                }
            }
            return hay;
        }

        /// <summary>Explota ya (también lo usan las pruebas).</summary>
        public void Explotar(Vector3 donde)
        {
            if (explotado)
            {
                return;
            }
            explotado = true;
            ExplosionAPie.Explotar(donde, radio, danio, raizLanzador);
            Destroy(gameObject);
        }
    }
}
