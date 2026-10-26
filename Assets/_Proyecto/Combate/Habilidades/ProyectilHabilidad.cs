using System.Collections.Generic;
using MechaCyberpunk.Combate.Datos;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Combate.Habilidades
{
    /// <summary>
    /// Un misil: avanza con su velocidad, revisa choques con un SphereCast cada cuadro y explota al tocar algo o al pasar su alcance.
    /// La explosión daña (una vez por raíz) a todo <see cref="IRecibeDanio"/> en su radio, menos al mecha que lo lanzó.
    /// Se destruye solo y ninguno vive más de <see cref="ReglaEfectosHabilidad.VidaMaximaProyectil"/> s.
    /// </summary>
    public class ProyectilHabilidad : MonoBehaviour
    {
        private const int MaxImpactos = 16;
        private const int MaxExplotados = 32;
        private const float GrosorEstela = 0.12f;

        private static readonly RaycastHit[] Impactos = new RaycastHit[MaxImpactos];
        private static readonly Collider[] Cercanos = new Collider[MaxExplotados];

        private Transform propio;
        private IPeleador atacante;
        private Vector3 direccion;
        private float velocidad;
        private float alcance;
        private float radioExplosion;
        private float danio;
        private float radioCasco;
        private float recorrido;
        private float edad;
        private bool terminado;

        /// <summary>Crea un proyectil en <paramref name="origen"/> que viaja hacia <paramref name="direccion"/>.</summary>
        public static ProyectilHabilidad Crear(Vector3 origen, Vector3 direccion, DatosHabilidad datos, float danio,
            Transform propio, ConfigEfectosHabilidad config)
        {
            GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cuerpo.name = "Proyectil " + datos.Nombre;
            Collider colision = cuerpo.GetComponent<Collider>();
            Destroy(colision);
            // Se desactiva ya: el barrido de este cuadro no debe verlo.
            colision.enabled = false;

            // La cápsula mide 2 de alto por 1 de ancho; se acuesta sobre el eje de avance.
            cuerpo.transform.SetPositionAndRotation(origen, Quaternion.LookRotation(direccion) * Quaternion.Euler(90f, 0f, 0f));
            cuerpo.transform.localScale = new Vector3(config.RadioProyectil * 2f, config.LargoProyectil * 0.5f, config.RadioProyectil * 2f);

            MeshRenderer renderer = cuerpo.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = MaterialesEfecto.Opaco;
            MaterialesEfecto.Pintar(renderer, datos.ColorRama, new MaterialPropertyBlock());

            TrailRenderer estela = cuerpo.AddComponent<TrailRenderer>();
            estela.sharedMaterial = MaterialesEfecto.Opaco;
            estela.time = config.DuracionEstela;
            estela.widthMultiplier = GrosorEstela;
            estela.startColor = datos.ColorRama;
            estela.endColor = new Color(datos.ColorRama.r, datos.ColorRama.g, datos.ColorRama.b, 0f);

            ProyectilHabilidad proyectil = cuerpo.AddComponent<ProyectilHabilidad>();
            proyectil.propio = propio;
            proyectil.atacante = propio != null ? propio.GetComponent<IPeleador>() : null;
            proyectil.direccion = direccion.normalized;
            proyectil.velocidad = datos.Velocidad;
            proyectil.alcance = datos.Alcance;
            proyectil.radioExplosion = datos.RadioExplosion;
            proyectil.danio = danio;
            proyectil.radioCasco = config.RadioProyectil;
            return proyectil;
        }

        private void Update()
        {
            if (terminado)
            {
                return;
            }
            float dt = Time.deltaTime;
            edad += dt;
            float paso = ReglaEfectosHabilidad.AvanceProyectil(velocidad, dt, recorrido, alcance);

            if (BuscarChoque(paso, out RaycastHit choque))
            {
                transform.position += direccion * choque.distance;
                // Si ya traslapaba al empezar el barrido, Unity da distancia 0 y punto cero: se explota donde está el misil.
                bool traslape = choque.distance <= 0f || choque.point == Vector3.zero;
                Explotar(traslape ? transform.position : choque.point);
                return;
            }
            transform.position += direccion * paso;
            recorrido += paso;
            if (recorrido >= alcance || edad >= ReglaEfectosHabilidad.VidaMaximaProyectil)
            {
                Explotar(transform.position);
            }
        }

        // El primer choque que no sea del mecha que lo lanzó. Los triggers no cuentan.
        private bool BuscarChoque(float paso, out RaycastHit mejor)
        {
            mejor = default;
            int cantidad = Physics.SphereCastNonAlloc(transform.position, radioCasco, direccion, Impactos, paso,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float menor = float.MaxValue;
            bool hay = false;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit golpe = Impactos[i];
                if ((propio != null && golpe.collider.transform.root == propio.root) || golpe.distance >= menor)
                {
                    continue;
                }
                menor = golpe.distance;
                mejor = golpe;
                hay = true;
            }
            return hay;
        }

        private void Explotar(Vector3 punto)
        {
            terminado = true;
            int cantidad = Physics.OverlapSphereNonAlloc(punto, radioExplosion, Cercanos, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            List<Transform> raices = new List<Transform>();
            for (int i = 0; i < cantidad; i++)
            {
                Transform raiz = Cercanos[i].transform.root;
                if ((propio != null && raiz == propio.root) || raices.Contains(raiz))
                {
                    continue;
                }
                IRecibeDanio recibe = Cercanos[i].GetComponentInParent<IRecibeDanio>();
                if (recibe == null)
                {
                    continue;
                }
                raices.Add(raiz);
                Vector3 hacia = Cercanos[i].bounds.ClosestPoint(punto) - punto;
                Vector3 dir = hacia.sqrMagnitude > 1e-6f ? hacia.normalized : direccion;
                recibe.RecibirDanio(new InfoDanio(danio, TipoDanio.Disparo, punto, dir, atacante));
            }
            Destroy(gameObject);
        }
    }
}
