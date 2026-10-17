using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-094: viste por fuera los dos lugares cerrados de la demo. El casino lleva marquesina, columnas doradas, alfombra gastada,
    /// postes con cadena y tubos de neón tenues; el mercado negro, una cortina metálica baja con candados, cajas y una manta
    /// de "VOLVEMOS PRONTO". Todo es decorativo, sin collider (no estorba a la puerta ni a la franja de la banqueta). Nada se pone sobre la Entrada (a 1.5 m de la fachada) ni más allá de 1.3 m de ella.
    /// Corre después de CreadorLugares y se puede repetir (usa CreadorTodo.Crear).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.VestirCerrados.Crear
    /// </summary>
    public static class VestirCerrados
    {
        public const float SalienteMaximo = 1.3f;     // m desde la fachada hacia la calle
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const float AnchoPuerta = 3f;
        private const float AltoPuerta = 3.2f;
        private const float DistanciaEntrada = 1.5f;
        private const float PixelesPorMetro = 100f;

        [MenuItem("Mecha Cyberpunk/Crear T-094")]
        public static void Crear()
        {
            Material dorado = Mat("Dorado", new Color(0.65f, 0.5f, 0.15f), 0.6f, default);
            Material alfombra = Mat("Alfombra", new Color(0.35f, 0.05f, 0.08f), 0.1f, default);
            Material metal = Mat("Cortina", new Color(0.32f, 0.34f, 0.36f), 0.55f, default);
            Material herrumbre = Mat("Herrumbre", new Color(0.35f, 0.2f, 0.1f), 0.2f, default);
            Material neonTenue = Mat("NeonTenue", new Color(0.7f, 0.5f, 0.1f), 0.2f, new Color(0.9f, 0.6f, 0.1f));
            Material manta = Mat("Manta", new Color(0.55f, 0.18f, 0.12f), 0.05f, default);
            Material caja = Mat("Caja", new Color(0.4f, 0.28f, 0.16f), 0.15f, default);
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform casino = BuscarPorNombre(raiz.transform, "Lugar_Casino");
            Transform mercado = BuscarPorNombre(raiz.transform, "Lugar_MercadoNegro");
            VestirCasino(casino, dorado, alfombra, neonTenue, metal);
            VestirMercado(mercado, metal, herrumbre, manta, caja, fuente);
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-094 creada: exteriores cerrados del casino y el mercado negro.");
        }

        private static float ZFrente(Transform lugar)
        {
            return lugar.Find("Entrada").localPosition.z - DistanciaEntrada;
        }

        private static Transform Grupo(Transform lugar)
        {
            Transform viejo = lugar.Find("Cerrado");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            GameObject go = new GameObject("Cerrado");
            go.transform.SetParent(lugar, false);
            return go.transform;
        }

        private static void VestirCasino(Transform lugar, Material dorado, Material alfombra, Material neon, Material metal)
        {
            Transform g = Grupo(lugar);
            float z = ZFrente(lugar);
            float mitad = AnchoCuerpo(lugar) / 2f;
            foreach (float x in new[] { -3.6f, 3.6f })
            {
                Cilindro(g, "Columna", new Vector3(x, 2.1f, z + 0.7f), new Vector3(0.7f, 2.1f, 0.7f), dorado, true);
            }
            Caja(g, "Marquesina", new Vector3(0f, 3.45f, z + 0.65f), new Vector3(7.9f, 0.2f, 1.3f), dorado, false);
            Caja(g, "BordeMarquesina", new Vector3(0f, 3.3f, z + 1.28f), new Vector3(7.9f, 0.12f, 0.06f), neon, false);
            Caja(g, "Alfombra", new Vector3(0f, 0.02f, z + 0.65f), new Vector3(3.2f, 0.03f, 1.2f), alfombra, false);
            foreach (float x in new[] { -1.9f, 1.9f })
            {
                Cilindro(g, "PosteCadena", new Vector3(x, 0.5f, z + 0.45f), new Vector3(0.12f, 0.5f, 0.12f), dorado, true);
            }
            Caja(g, "Cadena", new Vector3(0f, 0.9f, z + 0.45f), new Vector3(3.8f, 0.05f, 0.05f), metal, false);
            foreach (float x in new[] { -(mitad - 0.5f), mitad - 0.5f })
            {
                Caja(g, "TuboNeon", new Vector3(x, 4f, z + 0.1f), new Vector3(0.1f, 6f, 0.1f), neon, false);
            }
        }

        private static float AnchoCuerpo(Transform lugar)
        {
            Transform izquierdo = lugar.Find("CuerpoLadoIzquierdo");
            return izquierdo != null ? AnchoPuerta + 2f * izquierdo.localScale.x : 0f;
        }

        private static void VestirMercado(Transform lugar, Material metal, Material herrumbre, Material manta, Material caja, Font fuente)
        {
            Transform g = Grupo(lugar);
            float z = ZFrente(lugar);
            // Cortina: ocho tablillas apiladas que cubren la puerta, entre dos guías, con dos candados.
            int tablillas = 8;
            float alto = AltoPuerta / tablillas;
            for (int i = 0; i < tablillas; i++)
            {
                Caja(g, "Tablilla" + i, new Vector3(0f, alto * (i + 0.5f), z + 0.2f), new Vector3(AnchoPuerta, alto - 0.03f, 0.08f), metal, true);
            }
            foreach (float x in new[] { -AnchoPuerta / 2f - 0.08f, AnchoPuerta / 2f + 0.08f })
            {
                Caja(g, "Guia", new Vector3(x, AltoPuerta / 2f, z + 0.17f), new Vector3(0.16f, AltoPuerta, 0.14f), herrumbre, true);
            }
            Caja(g, "CajonCortina", new Vector3(0f, AltoPuerta + 0.15f, z + 0.17f), new Vector3(AnchoPuerta + 0.4f, 0.3f, 0.2f), herrumbre, false);
            foreach (float x in new[] { -1f, 1f })
            {
                Caja(g, "Candado", new Vector3(x, 0.22f, z + 0.3f), new Vector3(0.14f, 0.18f, 0.08f), herrumbre, false);
            }
            Caja(g, "CajaApilada1", new Vector3(-3.6f, 0.4f, z + 0.5f), new Vector3(0.8f, 0.8f, 0.8f), caja, true);
            Caja(g, "CajaApilada2", new Vector3(-3.6f, 1.15f, z + 0.5f), new Vector3(0.7f, 0.7f, 0.7f), caja, true);

            // Manta con el aviso, colgada de la fachada a la izquierda de la puerta.
            Caja(g, "Manta", new Vector3(-3.7f, 2.3f, z + 0.06f), new Vector3(1.6f, 1f, 0.03f), manta, false);
            GameObject canvasObjeto = new GameObject("AvisoCanvas", typeof(RectTransform));
            canvasObjeto.transform.SetParent(g, false);
            canvasObjeto.transform.localPosition = new Vector3(-3.7f, 2.3f, z + 0.1f);
            canvasObjeto.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasObjeto.transform.localScale = Vector3.one / PixelesPorMetro;
            Canvas canvas = canvasObjeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObjeto.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 90f);
            GameObject textoObjeto = new GameObject("Texto", typeof(RectTransform));
            textoObjeto.transform.SetParent(canvasObjeto.transform, false);
            Text texto = textoObjeto.AddComponent<Text>();
            texto.font = fuente;
            texto.text = "VOLVEMOS\nPRONTO";
            texto.alignment = TextAnchor.MiddleCenter;
            texto.fontSize = 22;
            texto.fontStyle = FontStyle.Bold;
            texto.color = new Color(1f, 0.9f, 0.7f);
            texto.raycastTarget = false;
            RectTransform rect = texto.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject Caja(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material, bool conCollider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (true) // T-094: todo es decorativo; la puerta es lo que bloquea y la franja de caminar queda libre
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        private static GameObject Cilindro(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material, bool conCollider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (true) // T-094: todo es decorativo; la puerta es lo que bloquea y la franja de caminar queda libre
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        private static Transform BuscarPorNombre(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            throw new System.InvalidOperationException("No existe " + nombre + " en el barrio; corre CreadorBarrio y CreadorLugares antes.");
        }

        private static Material Mat(string nombre, Color color, float suavidad, Color emision)
        {
            string ruta = RutaMateriales + "/Cerrado_" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", suavidad);
            if (emision.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emision);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
