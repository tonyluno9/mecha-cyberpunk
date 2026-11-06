using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-807 (noche 6-nov, sesión 2): líneas pintadas de la calle encima del asfalto, como máscara (materiales de <see cref="ImportarS2n"/>:
    /// M01 doble amarilla al centro, M03 blanca de carril discontinua, M02 cebras en cada boca de los cruces). Son quads sin collider ni sombra,
    /// 2 cm sobre el asfalto (material transparente que no escribe profundidad: no hay z-fighting). Cubren las tres calles del barrio, las
    /// seis calles de las afueras de T-530 y la calle nueva de T-805. Las rayas en cubo de Barrio.prefab (Calles/Marcas), de T-530 y las cebras
    /// de VestirFinales se quedan, con el renderer apagado en la escena (no se regenera el prefab). Idempotente: rehace la raíz S2n_Lineas.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nLineas.Colocar
    /// </summary>
    public static class ColocarS2nLineas
    {
        public const string NombreRaiz = "S2n_Lineas";
        public const float Altura = 0.02f;
        private const float AlturaCebra = 0.022f;
        public const float AnchoCalle = 14f;
        /// <summary>Doble amarilla: el quad mide esto de ancho (las dos rayas ocupan el 23 % del centro de la textura: ~0.36 m).</summary>
        private const float AnchoAmarilla = 1.58f;
        /// <summary>Largo (m) de una repetición de la textura M01 a ese ancho (768x1376 px).</summary>
        private const float LargoAmarilla = AnchoAmarilla * 1376f / 768f;
        private const int RepeticionesAmarilla = ImportarS2n.RepeticionesAmarilla;
        /// <summary>Raya blanca de carril: 0.6 m de quad (la raya es el 40 % del centro), 3 m pintados cada 8 m, a 4.6 m del centro
        /// (entre el carril del tráfico, a 2.4 m, y los carros estacionados, a 5.8 m).</summary>
        private const float AnchoBlanca = 0.6f;
        private const float LargoBlanca = 3f;
        private const float PasoBlanca = 8f;
        public const float CarrilBlanca = 4.6f;
        /// <summary>Cebra: del centro del cruce a la cebra (como VestirFinales), y su fondo.</summary>
        public const float BocaCruce = 9.5f;
        private const float FondoCebra = 3f;
        /// <summary>Sin líneas a menos de esto (m) del centro de un cruce: el cruce y su cebra.</summary>
        private const float HuecoCruce = BocaCruce + FondoCebra * 0.5f + 0.5f;

        /// <summary>Calles (centro de inicio, centro de fin). Las del barrio (z = 0, x = ±47) siguen por las calles de las afueras de T-530.</summary>
        public static readonly (Vector3 a, Vector3 b)[] Calles =
        {
            (new Vector3(-234f, 0f, 0f), new Vector3(234f, 0f, 0f)),
            (new Vector3(-47f, 0f, -186f), new Vector3(-47f, 0f, 186f)),
            (new Vector3(47f, 0f, -186f), new Vector3(47f, 0f, 186f)),
            (new Vector3(-ColocarS2nCalles.CruceX, 0f, CentroCruceSur), new Vector3(ColocarS2nCalles.CruceX, 0f, CentroCruceSur)),
        };

        public const float CentroCruceSur = (ColocarS2nCalles.CruceZ0 + ColocarS2nCalles.CruceZ1) * 0.5f;

        /// <summary>Cruces: centro y si tiene boca en cada dirección (+X, -X, +Z, -Z).</summary>
        public static readonly (Vector3 centro, bool[] bocas)[] Cruces =
        {
            (new Vector3(-47f, 0f, 0f), new[] { true, true, true, true }),
            (new Vector3(47f, 0f, 0f), new[] { true, true, true, true }),
            (new Vector3(-47f, 0f, CentroCruceSur), new[] { true, false, true, true }),   // T-805: la calle nueva sale al este
            (new Vector3(47f, 0f, CentroCruceSur), new[] { false, true, true, true }),
        };

        private static readonly Vector3[] Direcciones = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };

        [MenuItem("Mecha Cyberpunk/S2n/6 Lineas de la calle (M01, M02, carriles)")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ImportarS2n.MeterTodo();
            ColocarS2.Abrir();
            Material amarilla = AssetDatabase.LoadAssetAtPath<Material>(ImportarS2n.MaterialAmarilla);
            Material blanca = AssetDatabase.LoadAssetAtPath<Material>(ImportarS2n.MaterialBlanca);
            Material cebra = AssetDatabase.LoadAssetAtPath<Material>(ImportarS2n.MaterialCebra);
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Transform raiz = new GameObject(NombreRaiz).transform;
            Transform gAmarillas = Grupo(raiz, "Amarillas");
            Transform gBlancas = Grupo(raiz, "Blancas");
            Transform gCebras = Grupo(raiz, "Cebras");
            Mesh quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            int nA = 0, nB = 0, nC = 0;
            foreach ((Vector3 a, Vector3 b) in Calles)
            {
                Vector3 dir = (b - a).normalized;
                Vector3 lado = Vector3.Cross(Vector3.up, dir);
                foreach ((float d0, float d1) in Tramos(a, b))
                {
                    float largo = d1 - d0;
                    float pieza = LargoAmarilla * RepeticionesAmarilla;
                    int n = Mathf.Max(1, Mathf.RoundToInt(largo / pieza));
                    for (int i = 0; i < n; i++)
                    {
                        float m = d0 + largo * (i + 0.5f) / n;
                        Quad(gAmarillas, "LineaAmarilla", quad, amarilla, a + dir * m, dir, AnchoAmarilla, largo / n, Altura);
                        nA++;
                    }
                    for (float m = d0 + LargoBlanca * 0.5f + 1f; m + LargoBlanca * 0.5f <= d1; m += PasoBlanca)
                    {
                        foreach (float s in new[] { -1f, 1f })
                        {
                            Quad(gBlancas, "LineaBlanca", quad, blanca, a + dir * m + lado * (s * CarrilBlanca), dir, AnchoBlanca, LargoBlanca, Altura);
                            nB++;
                        }
                    }
                }
            }
            foreach ((Vector3 centro, bool[] bocas) in Cruces)
            {
                for (int i = 0; i < Direcciones.Length; i++)
                {
                    if (!bocas[i])
                    {
                        continue;
                    }
                    // La cebra cruza la calle de esa boca: sus franjas van en la dirección de los carros y se repiten a lo ancho.
                    Quad(gCebras, "Cebra", quad, cebra, centro + Direcciones[i] * BocaCruce, Direcciones[i], AnchoCalle - 1f, FondoCebra, AlturaCebra);
                    nC++;
                }
            }
            int viejas = ApagarViejas();
            ColocarS2.Informe.AppendLine("Líneas: " + nA + " tramos amarillos, " + nB + " rayas blancas, " + nC + " cebras; " + viejas + " rayas y cebras de cubo apagadas");
            ColocarS2.Guardar("s2n-lineas");
        }

        /// <summary>Tramos (distancia desde a) de la calle a-b fuera de los cruces.</summary>
        public static List<(float, float)> Tramos(Vector3 a, Vector3 b)
        {
            Vector3 dir = (b - a).normalized;
            float largo = Vector3.Distance(a, b);
            List<(float, float)> fuera = new List<(float, float)>();
            foreach ((Vector3 c, bool[] _) in Cruces)
            {
                Vector3 v = c - a;
                float d = Vector3.Dot(v, dir);
                float aLado = (v - dir * d).magnitude;
                if (aLado < 1f && d > -HuecoCruce && d < largo + HuecoCruce)
                {
                    fuera.Add((d - HuecoCruce, d + HuecoCruce));
                }
            }
            List<(float, float)> tramos = new List<(float, float)>();
            float z = 0f;
            foreach ((float f0, float f1) in fuera.OrderBy(f => f.Item1))
            {
                if (f0 - z > 1f)
                {
                    tramos.Add((z, f0));
                }
                z = Mathf.Max(z, f1);
            }
            if (largo - z > 1f)
            {
                tramos.Add((z, largo));
            }
            return tramos;
        }

        // Quad plano mirando arriba: X local a lo ancho, Y local (V de la textura) a lo largo de 'dir'.
        private static void Quad(Transform padre, string nombre, Mesh malla, Material m, Vector3 centro, Vector3 dir, float ancho, float largo, float y)
        {
            GameObject g = new GameObject(nombre);
            g.transform.SetParent(padre, false);
            g.transform.SetPositionAndRotation(new Vector3(centro.x, y, centro.z), Quaternion.LookRotation(Vector3.down, dir));
            g.transform.localScale = new Vector3(ancho, largo, 1f);
            g.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = true;
            r.lightProbeUsage = LightProbeUsage.BlendProbes;
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
        }

        // Rayas en cubo de Barrio.prefab (Calles/Marcas/Raya), de T-530 (Salida_*/Raya) y cebras de VestirFinales: renderer apagado.
        private static int ApagarViejas()
        {
            int n = 0;
            foreach (Transform t in ColocarS2.Todos().Where(t => (t.name == "Raya" || t.name == "Cebra") && t.root.name != NombreRaiz))
            {
                Renderer r = t.GetComponent<Renderer>();
                if (r != null && r.enabled)
                {
                    ColocarS2.Informe.AppendLine("  apagada: " + ColocarS2.Ruta(t));
                    r.enabled = false;
                    EditorUtility.SetDirty(r);
                    n++;
                }
            }
            return n;
        }

        private static Transform Grupo(Transform padre, string nombre)
        {
            Transform g = new GameObject(nombre).transform;
            g.SetParent(padre, false);
            return g;
        }

        // ---------------------------------------------------------------- Capturas

        /// <summary>Capturas de día (calle y cenital) en docs/capturas/s2n-lineas-*.png y lado a lado con la referencia. No guarda la escena.</summary>
        public static void Fotos()
        {
            ColocarS2.Abrir();
            CicloDiaNoche ciclo = Object.FindFirstObjectByType<CicloDiaNoche>(FindObjectsInactive.Include);
            if (ciclo != null)
            {
                ciclo.Aplicar(13f);
            }
            const string P = "docs/capturas/s2n-lineas-";
            ColocarS2.Foto(new Vector3(-47f + 2.4f, 1.6f, -40f), new Vector3(-47f, 0f, -10f), P + "calle.png", 65f);
            ColocarS2.Foto(new Vector3(-47f - 3.5f, 1.7f, 24f), new Vector3(-47f + 2f, 0f, 0f), P + "cruce.png", 70f);
            ColocarS2.Foto(new Vector3(-47f, 1.7f, -100f), new Vector3(-30f, 0f, CentroCruceSur), P + "vuelta.png", 70f);
            ColocarS2.Foto(new Vector3(-47f, 45f, 25f), new Vector3(-47f, 0f, -2f), P + "cenital.png", 60f);
            ColocarS2.LadoALado("docs/referencias-ciudad/cruce-dia-lluvia.png", P + "cruce.png", P + "referencia.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single); // sin guardar la hora de la foto
        }
    }
}
