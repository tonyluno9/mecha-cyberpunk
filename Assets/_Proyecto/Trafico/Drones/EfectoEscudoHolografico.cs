using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Trafico.Drones
{
    /// <summary>
    /// T-1665: el destello holográfico cian de un impacto (un hexágono con borde y fondo tenue, semitransparente, que mira a la cámara,
    /// se expande y se desvanece en <see cref="ReglaEscudoDron.SegundosDestello"/>). Reutilizable: <see cref="Mostrar"/> funciona sobre cualquier objeto.
    /// Material URP Unlit transparente y textura creados en ejecución (no son assets); un grupo fijo de destellos se recicla.
    /// </summary>
    public class EfectoEscudoHolografico : MonoBehaviour
    {
        private const int MaximoSimultaneos = 10;
        private const int LadoTextura = 128;
        private static readonly Color ColorCian = new Color(0.25f, 0.95f, 1f, 1f);
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        private static readonly List<EfectoEscudoHolografico> Grupo = new List<EfectoEscudoHolografico>();
        private static Material material;
        private static Mesh malla;
        private static int siguiente;

        private MeshRenderer renderizador;
        private MaterialPropertyBlock bloque;
        private float inicio;
        private bool activo;

        /// <summary>Cuántos destellos hay mostrándose ahora mismo.</summary>
        public static int Activos
        {
            get
            {
                int n = 0;
                foreach (EfectoEscudoHolografico e in Grupo)
                {
                    if (e != null && e.activo)
                    {
                        n++;
                    }
                }
                return n;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            Grupo.Clear();
            material = null;
            malla = null;
            siguiente = 0;
        }

        /// <summary>Muestra un destello en el punto del mundo. Si hay padre, lo sigue (un dron en movimiento).</summary>
        public static EfectoEscudoHolografico Mostrar(Vector3 punto, Transform padre)
        {
            Grupo.RemoveAll(e => e == null);
            EfectoEscudoHolografico efecto = null;
            foreach (EfectoEscudoHolografico e in Grupo)
            {
                if (!e.activo)
                {
                    efecto = e;
                    break;
                }
            }
            if (efecto == null)
            {
                if (Grupo.Count < MaximoSimultaneos)
                {
                    efecto = Crear();
                    Grupo.Add(efecto);
                }
                else
                {
                    efecto = Grupo[siguiente % Grupo.Count]; // se recicla uno
                    siguiente++;
                }
            }
            efecto.Iniciar(punto, padre);
            return efecto;
        }

        private static EfectoEscudoHolografico Crear()
        {
            GameObject go = new GameObject("EfectoEscudoHolografico");
            go.AddComponent<MeshFilter>().sharedMesh = Malla();
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = CrearMaterial();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.SetActive(false);
            EfectoEscudoHolografico e = go.AddComponent<EfectoEscudoHolografico>();
            e.renderizador = r;
            e.bloque = new MaterialPropertyBlock();
            return e;
        }

        private void Iniciar(Vector3 punto, Transform padre)
        {
            transform.SetParent(padre, true);
            transform.position = punto;
            inicio = Time.time;
            activo = true;
            gameObject.SetActive(true);
            Aplicar(0f);
        }

        private void LateUpdate()
        {
            float edad = Time.time - inicio;
            if (edad >= ReglaEscudoDron.SegundosDestello)
            {
                activo = false;
                transform.SetParent(null, true);
                gameObject.SetActive(false);
                return;
            }
            Aplicar(ReglaEscudoDron.Progreso(edad));
        }

        private void Aplicar(float progreso)
        {
            Camera camara = Camera.main;
            if (camara != null)
            {
                Vector3 haciaFuera = transform.position - camara.transform.position;
                if (haciaFuera.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(haciaFuera, camara.transform.up);
                }
            }
            transform.localScale = Vector3.one * ReglaEscudoDron.Escala(progreso);
            Color color = ColorCian;
            color.a = ReglaEscudoDron.Alfa(progreso);
            renderizador.GetPropertyBlock(bloque);
            bloque.SetColor(IdColorBase, color);
            renderizador.SetPropertyBlock(bloque);
        }

        private static Mesh Malla()
        {
            if (malla != null)
            {
                return malla;
            }
            malla = new Mesh { name = "HexEscudo", hideFlags = HideFlags.DontSave };
            malla.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
            };
            malla.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            malla.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            malla.RecalculateBounds();
            return malla;
        }

        private static Material CrearMaterial()
        {
            if (material != null)
            {
                return material;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            material = new Material(shader) { name = "EscudoHolografico", hideFlags = HideFlags.DontSave };
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One); // aditivo: se ve como luz holográfica
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            Texture2D textura = TexturaHexagono();
            material.SetTexture("_BaseMap", textura);
            return material;
        }

        // Hexágono con borde brillante, un anillo interior más tenue y un fondo muy suave. El color lo pone el bloque (_BaseColor).
        private static Texture2D TexturaHexagono()
        {
            Texture2D t = new Texture2D(LadoTextura, LadoTextura, TextureFormat.RGBA32, false) { name = "HexEscudo", hideFlags = HideFlags.DontSave };
            t.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixeles = new Color32[LadoTextura * LadoTextura];
            for (int y = 0; y < LadoTextura; y++)
            {
                for (int x = 0; x < LadoTextura; x++)
                {
                    float px = Mathf.Abs((x + 0.5f) / LadoTextura * 2f - 1f);
                    float py = Mathf.Abs((y + 0.5f) / LadoTextura * 2f - 1f);
                    float d = Mathf.Max(px * 0.8660254f + py * 0.5f, py); // 1 en el borde de un hexágono
                    float borde = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.80f, 0.88f, d)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.90f, 0.97f, d)));
                    float interior = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 0.44f, d)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.46f, 0.52f, d))) * 0.5f;
                    float fondo = d < 0.9f ? 0.18f * (1f - d / 0.9f) + 0.08f : 0f;
                    float a = Mathf.Clamp01(Mathf.Max(borde, Mathf.Max(interior, fondo)));
                    pixeles[y * LadoTextura + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            t.SetPixels32(pixeles);
            t.Apply(false, true);
            return t;
        }
    }
}
