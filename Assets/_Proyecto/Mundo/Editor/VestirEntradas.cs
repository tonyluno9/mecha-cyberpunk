using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-311: hace reconocibles las entradas de los destinos desde la calle, de día y de noche. Alrededor de cada puerta agrega un marco
    /// de acero oscuro, un cajón de luz sobre el dintel, tiras de neón verticales, un zócalo de concreto y un toldo inclinado (low-poly).
    /// Los abiertos (casa, clandestino) llevan neón brillante y una luz puntual sin sombras; los cerrados (mercado negro),
    /// neón tenue y ninguna luz. Todo es decorativo y sin collider: no toca la puerta (3 × 3.2 m), la Entrada (a 1.5 m de la fachada),
    /// los puntos de aparición ni el letrero (a 4.4 m de altura), y nada sobresale más de <see cref="SalienteMaximo"/> de la fachada.
    /// Corre después de VestirCerrados y se puede repetir (CreadorTodo.Crear).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.VestirEntradas.Crear
    /// </summary>
    public static class VestirEntradas
    {
        public const float SalienteMaximo = 0.9f;       // m desde la fachada hacia la calle
        public const string NombreGrupo = "EntradaNeon";
        public const int LucesMaximasPorLugar = 1;
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const float AnchoPuerta = 3f;
        private const float AltoPuerta = 3.2f;
        private const float DistanciaEntrada = 1.5f;
        private const float GrosorMarco = 0.22f;
        private const float XNeonLateral = 2.55f;
        private const float AlturaNeonLateral = 2.6f;

        private struct Estilo
        {
            public string lugar;
            public string material;
            public Color neon;
            public float intensidad;     // multiplicador de la emisión
            public bool conLuz;
        }

        [MenuItem("Mecha Cyberpunk/Crear T-311")]
        public static void Crear()
        {
            Estilo[] estilos =
            {
                new Estilo { lugar = "Lugar_Casa", material = "Casa", neon = new Color(1f, 0.5f, 0.1f), intensidad = 2.2f, conLuz = true },
                new Estilo { lugar = "Lugar_Clandestino", material = "Clandestino", neon = new Color(1f, 0.1f, 0.65f), intensidad = 2.5f, conLuz = true },
                new Estilo { lugar = "Lugar_Casino", material = "Casino", neon = new Color(1f, 0.75f, 0.2f), intensidad = 2.4f, conLuz = true },
                new Estilo { lugar = "Lugar_MercadoNegro", material = "Mercado", neon = new Color(0.1f, 0.9f, 0.8f), intensidad = 0.6f, conLuz = false },
            };
            Material acero = Mat("Marco", new Color(0.07f, 0.08f, 0.1f), 0.5f, default);
            Material concreto = Mat("Zocalo", new Color(0.16f, 0.16f, 0.18f), 0.15f, default);

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            foreach (Estilo e in estilos)
            {
                Transform lugar = Buscar(raiz.transform, e.lugar);
                if (lugar == null || lugar.Find("Entrada") == null)
                {
                    Debug.LogWarning("[Mundo] T-311: se omite " + e.lugar + " (no existe o no tiene Entrada).");
                    continue;
                }
                Material neon = Mat("Neon_" + e.material, e.neon * 0.35f, 0.3f, e.neon * e.intensidad);
                Vestir(lugar, e, acero, concreto, neon);
            }
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-311 creada: marcos y neón en las entradas de casa, clandestino, casino y mercado negro.");
        }

        private static void Vestir(Transform lugar, Estilo e, Material acero, Material concreto, Material neon)
        {
            Transform viejo = lugar.Find(NombreGrupo);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            GameObject go = new GameObject(NombreGrupo);
            go.transform.SetParent(lugar, false);
            Transform g = go.transform;
            float z = lugar.Find("Entrada").localPosition.z - DistanciaEntrada;
            float mitad = AnchoPuerta / 2f;
            float anchoMarco = AnchoPuerta + GrosorMarco * 2f;

            // Marco: dos jambas y un dintel pegados a la fachada, alrededor del hueco de la puerta.
            foreach (float x in new[] { -(mitad + GrosorMarco / 2f), mitad + GrosorMarco / 2f })
            {
                Caja(g, "Jamba", new Vector3(x, AltoPuerta / 2f, z + 0.1f), new Vector3(GrosorMarco, AltoPuerta, 0.2f), acero);
            }
            Caja(g, "Dintel", new Vector3(0f, AltoPuerta + GrosorMarco / 2f, z + 0.1f), new Vector3(anchoMarco, GrosorMarco, 0.2f), acero);
            // Cajón de luz sobre el dintel, entre el marco y el letrero.
            Caja(g, "CajonLuz", new Vector3(0f, AltoPuerta + GrosorMarco + 0.06f, z + 0.14f), new Vector3(anchoMarco, 0.12f, 0.1f), neon);
            // Neón vertical pegado por fuera de cada jamba y a los lados.
            foreach (float x in new[] { -(mitad + GrosorMarco + 0.06f), mitad + GrosorMarco + 0.06f })
            {
                Caja(g, "NeonJamba", new Vector3(x, AltoPuerta / 2f, z + 0.06f), new Vector3(0.08f, AltoPuerta, 0.08f), neon);
            }
            foreach (float x in new[] { -XNeonLateral, XNeonLateral })
            {
                Caja(g, "NeonLateral", new Vector3(x, AlturaNeonLateral / 2f + 0.4f, z + 0.05f), new Vector3(0.1f, AlturaNeonLateral, 0.06f), neon);
            }
            // Zócalo de concreto a ambos lados, a ras de la fachada, para romper el muro liso.
            foreach (float x in new[] { -(mitad + 1.9f), mitad + 1.9f })
            {
                Caja(g, "Zocalo", new Vector3(x, 0.35f, z + 0.05f), new Vector3(3f, 0.7f, 0.1f), concreto);
            }
            // Toldo: placa inclinada sobre el cajón (sobresale unos 0.7 m) con un filo de neón.
            GameObject toldo = Caja(g, "Toldo", new Vector3(0f, AltoPuerta + GrosorMarco + 0.35f, z + 0.35f), new Vector3(anchoMarco + 0.4f, 0.06f, 0.7f), acero);
            toldo.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            Caja(g, "BordeToldo", new Vector3(0f, AltoPuerta + GrosorMarco + 0.27f, z + 0.69f), new Vector3(anchoMarco + 0.4f, 0.05f, 0.05f), neon);

            if (e.conLuz)
            {
                GameObject luzObjeto = new GameObject("LuzEntrada");
                luzObjeto.transform.SetParent(g, false);
                luzObjeto.transform.localPosition = new Vector3(0f, AltoPuerta - 0.2f, z + 0.8f);
                Light luz = luzObjeto.AddComponent<Light>();
                luz.type = LightType.Point;
                luz.color = e.neon;
                luz.intensity = 2.5f;
                luz.range = 5f;
                luz.shadows = LightShadows.None;
                // De día la luz se apaga (el neón de la entrada queda como señal); de noche se prende.
                LuzNocturna nocturna = luzObjeto.AddComponent<LuzNocturna>();
                SerializedObject so = new SerializedObject(nocturna);
                so.FindProperty("luz").objectReferenceValue = luz;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject Caja(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // decorativo: la puerta es lo que bloquea
            return go;
        }

        private static Transform Buscar(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            return null;
        }

        private static Material Mat(string nombre, Color color, float suavidad, Color emision)
        {
            string ruta = RutaMateriales + "/Entrada_" + nombre + ".mat";
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
