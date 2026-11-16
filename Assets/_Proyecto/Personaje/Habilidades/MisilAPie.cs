using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Habilidades
{
    /// <summary>
    /// T-1017: un misil teledirigido de la hombrera. Sale hacia arriba y gira hacia su blanco (o hacia un punto fijo) con un giro limitado, con estela;
    /// explota al tocar algo que no sea quien lo lanzó, al llegar o al acabarse su vida (<see cref="ExplosionAPie"/>).
    /// </summary>
    public class MisilAPie : MonoBehaviour
    {
        private const float RadioChoque = 0.12f;
        private const float AlturaBlanco = 1.1f;
        private const float DistanciaLlegada = 0.6f;

        private Vector3 velocidad;
        private Transform blanco;
        private Vector3 punto;
        private ConfigHabilidadesProtesis cfg;
        private Transform raizLanzador;
        private float vida;
        private bool explotado;
        private readonly RaycastHit[] golpes = new RaycastHit[8];

        public Transform Blanco => blanco;

        public static MisilAPie Lanzar(Vector3 origen, Vector3 velocidadInicial, Transform blanco, Vector3 punto, ConfigHabilidadesProtesis cfg, Transform raizLanzador)
        {
            GameObject objeto = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(objeto.GetComponent<Collider>());
            objeto.name = "MisilHombrera";
            objeto.transform.position = origen;
            objeto.transform.localScale = new Vector3(0.08f, 0.16f, 0.08f);
            Renderer r = objeto.GetComponent<Renderer>();
            r.material.color = new Color(0.75f, 0.78f, 0.8f, 1f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            TrailRenderer estela = objeto.AddComponent<TrailRenderer>();
            ConfigArmasEspeciales especiales = ConfigArmasEspeciales.Actual;
            estela.sharedMaterial = especiales.MaterialRayo != null ? especiales.MaterialRayo : new Material(Shader.Find("Sprites/Default"));
            estela.time = 0.45f;
            estela.startWidth = 0.12f;
            estela.endWidth = 0f;
            estela.startColor = new Color(1f, 0.7f, 0.3f, 0.9f);
            estela.endColor = new Color(0.6f, 0.6f, 0.6f, 0f);
            MisilAPie m = objeto.AddComponent<MisilAPie>();
            m.velocidad = velocidadInicial;
            m.blanco = blanco;
            m.punto = punto;
            m.cfg = cfg;
            m.raizLanzador = raizLanzador;
            return m;
        }

        private void Update()
        {
            if (explotado)
            {
                return;
            }
            float dt = Time.deltaTime;
            vida += dt;
            if (vida >= cfg.VidaMisil)
            {
                Explotar(transform.position);
                return;
            }
            Vector3 meta = blanco != null ? blanco.position + Vector3.up * AlturaBlanco : punto;
            Vector3 hacia = meta - transform.position;
            if (hacia.magnitude < DistanciaLlegada)
            {
                Explotar(transform.position);
                return;
            }
            // Gira hacia la meta con un giro limitado y va a su velocidad.
            Vector3 dirActual = velocidad.sqrMagnitude > 0.0001f ? velocidad.normalized : hacia.normalized;
            Vector3 dirNueva = Vector3.RotateTowards(dirActual, hacia.normalized, cfg.GiroMisil * Mathf.Deg2Rad * dt, 0f);
            velocidad = dirNueva * cfg.VelocidadMisil;
            Vector3 paso = velocidad * dt;
            float largo = paso.magnitude;
            if (largo > 0.0001f && Chocar(transform.position, paso / largo, largo, out RaycastHit golpe))
            {
                Explotar(golpe.point + golpe.normal * 0.05f);
                return;
            }
            transform.position += paso;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, dirNueva);
        }

        private bool Chocar(Vector3 desde, Vector3 direccion, float largo, out RaycastHit mejor)
        {
            mejor = default;
            bool hay = false;
            int n = Physics.SphereCastNonAlloc(desde, RadioChoque, direccion, golpes, largo, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                RaycastHit g = golpes[i];
                if (g.distance <= 0f || g.collider.transform.root == transform || (raizLanzador != null && g.collider.transform.root == raizLanzador))
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

        private void Explotar(Vector3 donde)
        {
            explotado = true;
            ExplosionAPie.Explotar(donde, cfg.RadioMisil, cfg.DanioMisil, raizLanzador);
            Destroy(gameObject);
        }
    }
}
