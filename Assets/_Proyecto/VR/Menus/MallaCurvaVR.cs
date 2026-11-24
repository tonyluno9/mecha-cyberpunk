using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR14/VR15: superficie curva (un trozo de cilindro vertical) vista desde dentro, con el eje en el origen local (donde va la cabeza).
    /// El centro de la superficie queda en +Z a <c>radio</c>. UV (0,0) abajo a la izquierda y (1,1) arriba a la derecha, vista desde el origen.
    /// La usan la pantalla de cine (Inicio, carga, cinemáticas) y el HUD de casco.
    /// </summary>
    public static class MallaCurvaVR
    {
        /// <summary>Crea la malla: <paramref name="grados"/> de ancho (de lado a lado), <paramref name="alto"/> en metros, centrada en y = 0.</summary>
        public static Mesh Crear(float radio, float grados, float alto, int segmentos = 48)
        {
            segmentos = Mathf.Max(2, segmentos);
            var vertices = new Vector3[(segmentos + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var colores = new Color32[vertices.Length];
            var normales = new Vector3[vertices.Length];
            var triangulos = new int[segmentos * 6];
            float medio = grados * 0.5f * Mathf.Deg2Rad;
            for (int i = 0; i <= segmentos; i++)
            {
                float u = (float)i / segmentos;
                float a = Mathf.Lerp(-medio, medio, u);
                var punto = new Vector3(Mathf.Sin(a) * radio, 0f, Mathf.Cos(a) * radio);
                Vector3 normal = -new Vector3(punto.x, 0f, punto.z).normalized;
                vertices[i * 2] = punto + Vector3.down * (alto * 0.5f);
                vertices[i * 2 + 1] = punto + Vector3.up * (alto * 0.5f);
                uvs[i * 2] = new Vector2(u, 0f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);
                normales[i * 2] = normales[i * 2 + 1] = normal;
                colores[i * 2] = colores[i * 2 + 1] = new Color32(255, 255, 255, 255);
            }
            for (int i = 0; i < segmentos; i++)
            {
                int b = i * 2;
                int t = i * 6;
                // Visto desde el origen (dentro del cilindro): sentido horario.
                triangulos[t] = b;
                triangulos[t + 1] = b + 1;
                triangulos[t + 2] = b + 2;
                triangulos[t + 3] = b + 2;
                triangulos[t + 4] = b + 1;
                triangulos[t + 5] = b + 3;
            }
            var malla = new Mesh { name = "MallaCurvaVR" };
            malla.vertices = vertices;
            malla.uv = uvs;
            malla.normals = normales;
            malla.colors32 = colores;
            malla.triangles = triangulos;
            malla.RecalculateBounds();
            return malla;
        }

        /// <summary>
        /// Dónde toca un rayo (en el espacio local de la malla) la superficie curva: UV 0..1. False si no la toca dentro de sus bordes.
        /// Sirve para el láser sobre la pantalla grande.
        /// </summary>
        public static bool Tocar(Ray rayoLocal, float radio, float grados, float alto, out Vector2 uv, out float distancia)
        {
            uv = Vector2.zero;
            distancia = 0f;
            // Intersección con el cilindro x² + z² = r² (el rayo sale de dentro: se toma la raíz positiva más lejana).
            Vector3 o = rayoLocal.origin;
            Vector3 d = rayoLocal.direction;
            float a = d.x * d.x + d.z * d.z;
            if (a < 1e-8f)
            {
                return false;
            }
            float b = 2f * (o.x * d.x + o.z * d.z);
            float c = o.x * o.x + o.z * o.z - radio * radio;
            float disc = b * b - 4f * a * c;
            if (disc < 0f)
            {
                return false;
            }
            float raiz = Mathf.Sqrt(disc);
            float t = (-b + raiz) / (2f * a);
            if (t < 0f)
            {
                return false;
            }
            Vector3 p = o + d * t;
            float angulo = Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg;
            float u = angulo / grados + 0.5f;
            float v = p.y / alto + 0.5f;
            if (u < 0f || u > 1f || v < 0f || v > 1f)
            {
                return false;
            }
            uv = new Vector2(u, v);
            distancia = t;
            return true;
        }

        /// <summary>Material sin luz que muestra una textura y va bien en estéreo (UI/Default siempre está en el build).
        /// <paramref name="encimaDeTodo"/>: se dibuja aunque haya paredes delante.</summary>
        public static Material Material(Texture textura, bool encimaDeTodo, int cola = 3000)
        {
            Shader shader = Shader.Find("UI/Default");
            var m = new Material(shader) { name = "PantallaCurvaVR", mainTexture = textura, renderQueue = cola };
            if (encimaDeTodo)
            {
                m.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
            }
            return m;
        }
    }
}
