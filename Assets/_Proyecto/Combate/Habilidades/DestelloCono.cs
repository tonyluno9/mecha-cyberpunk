using MechaCyberpunk.Combate.Datos;
using UnityEngine;

namespace MechaCyberpunk.Combate.Habilidades
{
    /// <summary>Greybox del cono: un abanico plano del color de la rama que se desvanece y se destruye solo.</summary>
    public class DestelloCono : MonoBehaviour
    {
        private const int Segmentos = 24;
        private const float AlturaRelativa = 0.02f;

        private MeshRenderer renderer;
        private MaterialPropertyBlock bloque;
        private Color color;
        private float alfaMaximo;
        private float duracion;
        private float edad;
        private Mesh malla;

        /// <summary>Crea el destello en <paramref name="origen"/>, mirando a <paramref name="frente"/> (se aplana al plano).</summary>
        public static DestelloCono Crear(Vector3 origen, Vector3 frente, float alcance, float angulo, Color color, ConfigEfectosHabilidad config)
        {
            frente.y = 0f;
            if (frente.sqrMagnitude < 1e-6f)
            {
                frente = Vector3.forward;
            }
            GameObject objeto = new GameObject("Destello cono");
            objeto.transform.SetPositionAndRotation(origen, Quaternion.LookRotation(frente.normalized));
            MeshFilter filtro = objeto.AddComponent<MeshFilter>();
            DestelloCono destello = objeto.AddComponent<DestelloCono>();
            destello.malla = Construir(alcance, angulo);
            filtro.sharedMesh = destello.malla;
            destello.renderer = objeto.AddComponent<MeshRenderer>();
            destello.renderer.sharedMaterial = MaterialesEfecto.Transparente;
            destello.bloque = new MaterialPropertyBlock();
            destello.color = color;
            destello.alfaMaximo = config.AlfaDestello;
            destello.duracion = config.DuracionDestello;
            destello.Pintar(1f);
            return destello;
        }

        private void Update()
        {
            edad += Time.deltaTime;
            if (edad >= duracion)
            {
                Destroy(gameObject);
                return;
            }
            Pintar(1f - edad / duracion);
        }

        private void OnDestroy()
        {
            if (malla != null)
            {
                Destroy(malla);
            }
        }

        private void Pintar(float visibilidad)
        {
            MaterialesEfecto.Pintar(renderer, new Color(color.r, color.g, color.b, alfaMaximo * visibilidad), bloque);
        }

        // Abanico de triángulos desde el origen, visibles por los dos lados. Con 360 grados es un disco.
        private static Mesh Construir(float alcance, float angulo)
        {
            float total = Mathf.Clamp(angulo, 1f, 360f);
            Vector3[] vertices = new Vector3[Segmentos + 2];
            int[] triangulos = new int[Segmentos * 6];
            vertices[0] = Vector3.up * (alcance * AlturaRelativa);
            for (int i = 0; i <= Segmentos; i++)
            {
                float giro = Mathf.Lerp(-total * 0.5f, total * 0.5f, i / (float)Segmentos);
                Vector3 borde = Quaternion.Euler(0f, giro, 0f) * Vector3.forward * alcance;
                borde.y = alcance * AlturaRelativa;
                vertices[i + 1] = borde;
            }
            for (int i = 0; i < Segmentos; i++)
            {
                int t = i * 6;
                triangulos[t] = 0;
                triangulos[t + 1] = i + 1;
                triangulos[t + 2] = i + 2;
                triangulos[t + 3] = 0;
                triangulos[t + 4] = i + 2;
                triangulos[t + 5] = i + 1;
            }
            Mesh malla = new Mesh { name = "DestelloCono" };
            malla.vertices = vertices;
            malla.triangles = triangulos;
            malla.RecalculateBounds();
            return malla;
        }
    }
}
