using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1013: la explosión de la granada y del lanzagranadas. Daña con caída (<see cref="ReglaBalistica.DanioExplosion"/>) todo lo que tenga
    /// <see cref="IRecibeDanio"/> dentro del radio (al jugador que la lanzó, solo una parte), empuja lo que tenga Rigidbody, suena y pone el efecto del
    /// Particle Pack con un destello de luz. El efecto y los números de presentación salen de <see cref="ConfigArmasEspeciales"/>.
    /// </summary>
    public static class ExplosionAPie
    {
        private const string Sonido = SonidosArmasT1472.ExplosionGranada;
        private static readonly LimiteExplosionesSonido limiteSonido = new LimiteExplosionesSonido();
        private const int MaxCercanos = 64;
        /// <summary>T-1325: daño de la explosión dentro del radio letal.</summary>
        public const float DanioLetal = 9999f;
        private static readonly HashSet<IDetonable> detonables = new HashSet<IDetonable>();
        private static readonly Collider[] cercanos = new Collider[MaxCercanos];
        private static readonly HashSet<IRecibeDanio> vistos = new HashSet<IRecibeDanio>();
        private static readonly HashSet<Rigidbody> empujados = new HashSet<Rigidbody>();

        /// <summary>Cuántas explosiones ha habido (para las pruebas).</summary>
        public static int Cuantas { get; private set; }

        /// <summary>El centro de la última explosión (para las pruebas).</summary>
        public static Vector3 Ultima { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            Cuantas = 0;
            Ultima = Vector3.zero;
            vistos.Clear();
            empujados.Clear();
            limiteSonido.Reiniciar();
        }

        /// <summary>Explota en <paramref name="centro"/>. <paramref name="raizJugador"/> es quien la lanzó (recibe solo una parte). Devuelve cuántos recibieron daño.</summary>
        public static int Explotar(Vector3 centro, float radio, float danio, Transform raizJugador)
        {
            ConfigArmasEspeciales cfg = ConfigArmasEspeciales.Actual;
            Cuantas++;
            Ultima = centro;
            vistos.Clear();
            empujados.Clear();
            int danados = 0;
            Detonar(centro, radio);
            int cantidad = Physics.OverlapSphereNonAlloc(centro, radio, cercanos, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cantidad; i++)
            {
                Collider c = cercanos[i];
                Vector3 punto = c.bounds.ClosestPoint(centro);
                float distancia = Vector3.Distance(centro, punto);
                Vector3 hacia = punto - centro;
                Vector3 direccion = hacia.sqrMagnitude > 0.0001f ? hacia.normalized : Vector3.up;
                IRecibeDanio objetivo = c.GetComponentInParent<IRecibeDanio>();
                if (objetivo != null && vistos.Add(objetivo))
                {
                    float cuanto = ReglaBalistica.DanioExplosion(danio, radio, distancia, cfg.DanioEnElBorde);
                    bool esElJugador = raizJugador != null && c.transform.root == raizJugador;
                    if (esElJugador)
                    {
                        cuanto *= cfg.DanioAlJugador;
                    }
                    else if (EsLetal(distancia, radio, cfg.FraccionLetal) && c.GetComponentInParent<IObjetoInerte>() == null)
                    {
                        cuanto = Mathf.Max(cuanto, DanioLetal);
                    }
                    if (cuanto > 0f)
                    {
                        objetivo.RecibirDanio(new InfoDanio(cuanto, TipoDanio.Explosion, punto, direccion, null));
                        danados++;
                    }
                }
                Rigidbody cuerpo = c.attachedRigidbody;
                if (cuerpo != null && !cuerpo.isKinematic && empujados.Add(cuerpo))
                {
                    float fuerza = cfg.Empuje * Mathf.Clamp01(1f - distancia / Mathf.Max(0.01f, radio));
                    cuerpo.AddForce((direccion + Vector3.up * 0.5f).normalized * fuerza, ForceMode.VelocityChange);
                }
            }
            if (limiteSonido.Permite(Time.time)) // T-1472: en cadena no se suman diez estruendos iguales
            {
                Sonidos.ReproducirEn(Sonido, centro);
            }
            EventosRuido.PublicarDisparo(centro);
            Efecto(centro, radio, cfg);
            return danados;
        }

        /// <summary>T-1325: true si a esa distancia la explosión mata a una persona (dentro de <paramref name="fraccion"/> del radio).</summary>
        public static bool EsLetal(float distancia, float radio, float fraccion)
        {
            return radio > 0f && distancia <= radio * fraccion;
        }

        // T-1325: lo que se destroza de un golpe (carros) dentro del radio revienta al instante, antes de repartir el daño: así los policías que salen
        // de una patrulla destrozada ya están en su sitio cuando llega el daño letal.
        private static void Detonar(Vector3 centro, float radio)
        {
            int n = Physics.OverlapSphereNonAlloc(centro, radio, cercanos, ~0, QueryTriggerInteraction.Ignore);
            detonables.Clear();
            for (int i = 0; i < n; i++)
            {
                IDetonable d = cercanos[i].GetComponentInParent<IDetonable>();
                if (d != null)
                {
                    detonables.Add(d);
                }
            }
            foreach (IDetonable d in detonables)
            {
                d.Detonar(0f);
            }
            if (detonables.Count > 0)
            {
                Physics.SyncTransforms();
            }
        }

        private static void Efecto(Vector3 centro, float radio, ConfigArmasEspeciales cfg)
        {
            GameObject raiz = new GameObject("ExplosionAPie");
            raiz.transform.position = centro;
            if (cfg.PrefabExplosion != null)
            {
                GameObject efecto = Object.Instantiate(cfg.PrefabExplosion, centro, Quaternion.identity, raiz.transform);
                efecto.transform.localScale = Vector3.one * (radio / cfg.RadioDelEfecto);
                foreach (ParticleSystem p in efecto.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ParticleSystem.MainModule principal = p.main;
                    principal.loop = false;
                    principal.scalingMode = ParticleSystemScalingMode.Hierarchy;
                    p.Play();
                }
            }
            Light luz = new GameObject("Destello").AddComponent<Light>();
            luz.transform.SetParent(raiz.transform, false);
            luz.transform.localPosition = Vector3.up * 0.5f;
            luz.type = LightType.Point;
            luz.color = cfg.ColorDestello;
            luz.range = radio * 2.5f;
            luz.intensity = cfg.IntensidadDestello;
            luz.gameObject.AddComponent<ApagarLuz>().Iniciar(cfg.IntensidadDestello, 0.35f);
            Object.Destroy(raiz, cfg.SegundosEfecto);
        }
    }
}
