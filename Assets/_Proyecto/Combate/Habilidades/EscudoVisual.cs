using MechaCyberpunk.Combate.Datos;
using UnityEngine;

namespace MechaCyberpunk.Combate.Habilidades
{
    /// <summary>Greybox del escudo: una esfera translúcida turquesa alrededor del mecha. Sin colisión; el color va por MaterialPropertyBlock.</summary>
    public class EscudoVisual : MonoBehaviour
    {
        /// <summary>Crea la esfera como hija de <paramref name="mecha"/>, centrada en <paramref name="centro"/> (local).</summary>
        public static EscudoVisual Crear(Transform mecha, Vector3 centro, ConfigEfectosHabilidad config)
        {
            GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            esfera.name = "Escudo visual";
            Collider colision = esfera.GetComponent<Collider>();
            colision.enabled = false;
            Destroy(colision);
            esfera.transform.SetParent(mecha, false);
            esfera.transform.localPosition = centro;
            esfera.transform.localScale = Vector3.one * config.DiametroEscudo;

            MeshRenderer renderer = esfera.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = MaterialesEfecto.Transparente;
            Color color = config.ColorEscudo;
            MaterialesEfecto.Pintar(renderer, new Color(color.r, color.g, color.b, config.AlfaEscudo), new MaterialPropertyBlock());
            return esfera.AddComponent<EscudoVisual>();
        }
    }
}
