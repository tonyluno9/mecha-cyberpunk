using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-931: decide si en el punto de un impacto hay una superficie que se ve (para no dejar huecos ni marcas flotando en el aire).
    /// Hay superficie si el collider es un terreno, si tiene un Renderer propio activo, o si un Renderer activo de sus padres o hijos
    /// tiene los límites (ampliados <see cref="ReglaHuecosBala.MargenSuperficie"/>) que contienen el punto.
    /// </summary>
    public static class SuperficieImpacto
    {
        private static readonly List<Renderer> vecinos = new List<Renderer>(16);

        public static bool Visible(Collider collider, Vector3 punto)
        {
            if (collider == null || !collider.gameObject.activeInHierarchy)
            {
                return false;
            }
            if (collider is TerrainCollider)
            {
                return true;
            }
            if (collider.TryGetComponent(out Renderer propio) && EsDeSuperficie(propio))
            {
                return true;
            }
            collider.GetComponentsInParent(false, vecinos);
            if (ContieneElPunto(punto))
            {
                return true;
            }
            collider.GetComponentsInChildren(false, vecinos);
            return ContieneElPunto(punto);
        }

        private static bool ContieneElPunto(Vector3 punto)
        {
            for (int i = 0; i < vecinos.Count; i++)
            {
                Renderer r = vecinos[i];
                if (EsDeSuperficie(r))
                {
                    Bounds b = r.bounds;
                    if (ReglaHuecosBala.PuntoEnCaja(b.center, b.extents, punto, ReglaHuecosBala.MargenSuperficie))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Un Renderer cuenta si está encendido y no es un efecto (partículas, líneas, estelas).
        private static bool EsDeSuperficie(Renderer r) =>
            r != null && r.enabled && r.gameObject.activeInHierarchy
            && !(r is ParticleSystemRenderer) && !(r is LineRenderer) && !(r is TrailRenderer);
    }
}
