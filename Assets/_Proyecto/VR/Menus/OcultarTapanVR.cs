using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR42: mientras está abierta una pantalla (la del Doc, Tino, Canepa, una máquina...), lo que queda entre la cabeza y el panel no se dibuja:
    /// el NPC que la abrió, el mostrador, otro peatón. Al cerrar vuelve a verse igual. Solo se apaga el dibujo (<see cref="Renderer.forceRenderingOff"/>):
    /// colisiones, animación y lógica siguen.
    /// <list type="bullet">
    /// <item>Busca con una caja del tamaño del panel desde un poco delante de la cabeza hasta casi el panel: los colliders sólidos que toca
    /// (de cada uno, su personaje entero: lo que cuelga de su Animator) y los personajes con piel cuyo volumen cae en la caja.</item>
    /// <item>Nunca oculta al jugador, a MenusVR, ni cosas grandes (paredes, pisos, edificios: más de <see cref="TamanoMaximo"/> m).</item>
    /// </list>
    /// </summary>
    public sealed class OcultarTapanVR
    {
        /// <summary>Lo más grande que se oculta (m, media diagonal de su volumen): una persona o un mueble sí, una pared o un cuarto no.</summary>
        public const float TamanoMaximo = 2.5f;
        /// <summary>La caja empieza tanto delante de la cabeza (m): las manos del jugador y lo pegado a la cara no cuentan.</summary>
        public const float InicioCaja = 0.15f;
        /// <summary>Y termina tanto antes del panel (m).</summary>
        public const float FinCaja = 0.05f;
        /// <summary>La caja es un poco más ancha y alta que el panel.</summary>
        public const float HolguraCaja = 1.1f;
        private const int MaximoGolpes = 64;

        private readonly List<Renderer> ocultos = new List<Renderer>();
        private readonly HashSet<Renderer> vistos = new HashSet<Renderer>();
        private readonly Collider[] golpes = new Collider[MaximoGolpes];
        private readonly List<Renderer> temporal = new List<Renderer>();

        /// <summary>Cuántos renderers están ocultos ahora.</summary>
        public int Ocultos => ocultos.Count;

        /// <summary>True si este renderer lo ocultó esto (pruebas).</summary>
        public bool Oculta(Renderer r) => r != null && vistos.Contains(r);

        /// <summary>
        /// Oculta lo que hay entre <paramref name="cabeza"/> y el panel (centro, giro, ancho y alto en m). No toca nada que cuelgue de
        /// <paramref name="raizJugador"/> ni de <paramref name="propio"/>. Se puede llamar otra vez (agrega lo nuevo).
        /// </summary>
        public void Ocultar(Vector3 cabeza, Vector3 centroPanel, float anchoPanel, float altoPanel, Transform raizJugador, Transform propio)
        {
            Vector3 hacia = centroPanel - cabeza;
            float largo = hacia.magnitude - InicioCaja - FinCaja;
            if (largo <= 0.05f)
            {
                return;
            }
            Quaternion giro = Quaternion.LookRotation(hacia.normalized, Vector3.up);
            Vector3 centro = cabeza + hacia.normalized * (InicioCaja + largo * 0.5f);
            var medio = new Vector3(anchoPanel * 0.5f * HolguraCaja, altoPanel * 0.5f * HolguraCaja, largo * 0.5f);
            int n = Physics.OverlapBoxNonAlloc(centro, medio, golpes, giro, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = golpes[i];
                if (c == null || Ajeno(c.transform, raizJugador, propio) || Grande(c.bounds))
                {
                    continue;
                }
                Animator animador = c.GetComponentInParent<Animator>();
                Transform entidad = animador != null && !Ajeno(animador.transform, raizJugador, propio) ? animador.transform : c.transform;
                OcultarEntidad(entidad, c.transform, raizJugador, propio);
            }
            // Personajes sin collider sólido (un NPC quieto con solo su zona de interacción): por el volumen de su piel.
            foreach (SkinnedMeshRenderer piel in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
            {
                if (piel == null || !piel.enabled || vistos.Contains(piel) || Ajeno(piel.transform, raizJugador, propio) || Grande(piel.bounds))
                {
                    continue;
                }
                if (!CruzaCaja(piel.bounds, centro, giro, medio))
                {
                    continue;
                }
                Animator animador = piel.GetComponentInParent<Animator>();
                Transform entidad = animador != null && !Ajeno(animador.transform, raizJugador, propio) ? animador.transform : piel.transform;
                OcultarEntidad(entidad, piel.transform, raizJugador, propio);
            }
        }

        /// <summary>Vuelve a dibujar todo lo que se ocultó.</summary>
        public void Mostrar()
        {
            foreach (Renderer r in ocultos)
            {
                if (r != null)
                {
                    r.forceRenderingOff = false;
                }
            }
            ocultos.Clear();
            vistos.Clear();
        }

        // La entidad entera (un personaje con todas sus piezas) si es de tamaño de persona o mueble; si no, solo el objeto tocado.
        private void OcultarEntidad(Transform entidad, Transform tocado, Transform raizJugador, Transform propio)
        {
            temporal.Clear();
            entidad.GetComponentsInChildren(false, temporal);
            if (temporal.Count == 0 || Grande(Juntar(temporal)))
            {
                temporal.Clear();
                tocado.GetComponents(temporal);
            }
            foreach (Renderer r in temporal)
            {
                if (r == null || !r.enabled || r.forceRenderingOff || vistos.Contains(r) || Ajeno(r.transform, raizJugador, propio)
                    || Grande(r.bounds) || r is ParticleSystemRenderer)
                {
                    continue;
                }
                r.forceRenderingOff = true;
                ocultos.Add(r);
                vistos.Add(r);
            }
            temporal.Clear();
        }

        private static Bounds Juntar(List<Renderer> renderers)
        {
            Bounds total = default;
            bool hay = false;
            foreach (Renderer r in renderers)
            {
                if (r == null)
                {
                    continue;
                }
                if (!hay)
                {
                    total = r.bounds;
                    hay = true;
                }
                else
                {
                    total.Encapsulate(r.bounds);
                }
            }
            return total;
        }

        private static bool Ajeno(Transform t, Transform raizJugador, Transform propio)
        {
            return (raizJugador != null && t.root == raizJugador) || (propio != null && t.IsChildOf(propio));
        }

        /// <summary>True si el volumen es más grande que una persona o un mueble.</summary>
        public static bool Grande(Bounds b)
        {
            return b.extents.magnitude > TamanoMaximo;
        }

        /// <summary>True si la caja del mundo <paramref name="b"/> se cruza con la caja girada (centro, giro, medio tamaño). Aproximado: caja contra caja en los ejes de la girada.</summary>
        public static bool CruzaCaja(Bounds b, Vector3 centro, Quaternion giro, Vector3 medio)
        {
            Vector3 local = Quaternion.Inverse(giro) * (b.center - centro);
            Vector3 e = b.extents;
            // Medio tamaño de la caja del mundo vista en los ejes de la girada.
            Vector3 x = giro * Vector3.right;
            Vector3 y = giro * Vector3.up;
            Vector3 z = giro * Vector3.forward;
            float ex = Mathf.Abs(x.x) * e.x + Mathf.Abs(x.y) * e.y + Mathf.Abs(x.z) * e.z;
            float ey = Mathf.Abs(y.x) * e.x + Mathf.Abs(y.y) * e.y + Mathf.Abs(y.z) * e.z;
            float ez = Mathf.Abs(z.x) * e.x + Mathf.Abs(z.y) * e.y + Mathf.Abs(z.z) * e.z;
            return Mathf.Abs(local.x) <= medio.x + ex && Mathf.Abs(local.y) <= medio.y + ey && Mathf.Abs(local.z) <= medio.z + ez;
        }
    }
}
