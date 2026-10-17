using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-098: pulido de calles y de los finales del barrio. Pasos de cebra en las cuatro bocas de los dos cruces, tapas de alcantarilla y
    /// grietas en el asfalto, una autopista elevada con pilares, barandales y carros falsos de luces al norte, y una grúa y un andamio detrás
    /// de la obra del oeste. Todo es decorado sin collider fuera de la franja jugable de las calles, así que no cambia por dónde pasa el carro.
    /// Corre después de CreadorAudio y se puede repetir (usa CreadorTodo.Crear). Semilla fija.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.VestirFinales.Crear
    /// </summary>
    public static class VestirFinales
    {
        public const string NombreGrupo = "PulidoFinales";
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const int Semilla = 9898;

        // Medidas (m), las mismas de CreadorBarrio.
        private const float AnchoCalle = 14f;
        private const float CentroCalleNorteSur = 47f;
        private const float MitadBarrioX = 134f;
        private const float MitadBarrioZ = 87f;
        private const float AlturaAsfalto = 0.015f;
        private const float BocaCruce = 9.5f;        // distancia del centro del cruce al paso de cebra

        // Autopista elevada al norte.
        private const float ZAutopista = MitadBarrioZ + 16f;
        private const float AlturaLosa = 10f;
        private const float MitadAutopista = 170f;
        private const float AnchoAutopista = 16f;
        private const float PasoPilares = 17f;
        private const int CochesPorCarril = 6;
        private const float VelocidadCoche = 14f;

        [MenuItem("Mecha Cyberpunk/Crear T-098")]
        public static void Crear()
        {
            Material raya = Mat("Raya", new Color(0.55f, 0.55f, 0.5f), 0.2f, default);
            Material tapa = Mat("Tapa", new Color(0.08f, 0.08f, 0.09f), 0.5f, default);
            Material grieta = Mat("Grieta", new Color(0.01f, 0.01f, 0.012f), 0.1f, default);
            Material concreto = Mat("Concreto", new Color(0.28f, 0.28f, 0.3f), 0.1f, default);
            Material acero = Mat("Acero", new Color(0.3f, 0.32f, 0.36f), 0.5f, default);
            Material faroBlanco = Mat("FaroBlanco", new Color(0.9f, 0.9f, 0.8f), 0.2f, new Color(3f, 3f, 2.6f));
            Material faroRojo = Mat("FaroRojo", new Color(0.9f, 0.1f, 0.1f), 0.2f, new Color(3f, 0.2f, 0.2f));
            Material carroceria = Mat("Carroceria", new Color(0.06f, 0.07f, 0.09f), 0.6f, default);
            Material naranja = Mat("Obra", new Color(0.95f, 0.5f, 0.1f), 0.2f, default);

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform viejo = raiz.transform.Find(NombreGrupo);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            Transform grupo = new GameObject(NombreGrupo).transform;
            grupo.SetParent(raiz.transform, false);
            Random azar = new Random(Semilla);

            PasosDeCebra(grupo, raya);
            Alcantarillas(grupo, tapa);
            Grietas(grupo, grieta, azar);
            Autopista(grupo, concreto, acero, faroBlanco, faroRojo, carroceria);
            GruaYAndamio(grupo, acero, naranja);

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-098 creada: pasos de cebra, alcantarillas, grietas, autopista elevada y grúa de obra.");
        }

        // Ocho pasos de cebra: cuatro bocas de cada uno de los dos cruces (calle este-oeste en z = 0 y calles norte-sur en x = ±47).
        private static void PasosDeCebra(Transform padre, Material raya)
        {
            Transform grupo = Hijo(padre, "PasosDeCebra");
            float mitad = AnchoCalle / 2f - 0.5f;
            foreach (float cx in new[] { -CentroCalleNorteSur, CentroCalleNorteSur })
            {
                foreach (float lado in new[] { -1f, 1f })
                {
                    // Cruzan la calle este-oeste: franjas largas en X, una cada metro a lo ancho (z).
                    for (float z = -mitad + 0.5f; z <= mitad; z += 1f)
                    {
                        Cubo(grupo, "Cebra", new Vector3(cx + lado * BocaCruce, AlturaAsfalto, z), new Vector3(3f, 0.02f, 0.5f), raya);
                    }
                    // Cruzan la calle norte-sur: franjas largas en Z, una cada metro a lo ancho (x).
                    for (float x = -mitad + 0.5f; x <= mitad; x += 1f)
                    {
                        Cubo(grupo, "Cebra", new Vector3(cx + x, AlturaAsfalto, lado * BocaCruce), new Vector3(0.5f, 0.02f, 3f), raya);
                    }
                }
            }
        }

        private static void Alcantarillas(Transform padre, Material tapa)
        {
            Transform grupo = Hijo(padre, "Alcantarillas");
            float[] xs = { -100f, -70f, -25f, 25f, 70f, 100f };
            for (int i = 0; i < xs.Length; i++)
            {
                Tapa(grupo, new Vector3(xs[i], AlturaAsfalto, i % 2 == 0 ? 2.5f : -2.5f), tapa);
            }
            float[] zs = { -60f, -35f, 35f, 60f };
            for (int i = 0; i < zs.Length; i++)
            {
                Tapa(grupo, new Vector3(CentroCalleNorteSur + 2.5f, AlturaAsfalto, zs[i]), tapa);
                Tapa(grupo, new Vector3(-CentroCalleNorteSur - 2.5f, AlturaAsfalto, zs[i]), tapa);
            }
        }

        private static void Tapa(Transform padre, Vector3 posicion, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Tapa";
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = new Vector3(0.9f, 0.01f, 0.9f);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static void Grietas(Transform padre, Material grieta, Random azar)
        {
            Transform grupo = Hijo(padre, "Grietas");
            for (int i = 0; i < 40; i++)
            {
                bool horizontal = i % 2 == 0;
                float x = horizontal ? (float)(azar.NextDouble() * 2.0 - 1.0) * (MitadBarrioX - 10f) : (azar.NextDouble() < 0.5 ? -CentroCalleNorteSur : CentroCalleNorteSur) + (float)(azar.NextDouble() * 2.0 - 1.0) * 5.5f;
                float z = horizontal ? (float)(azar.NextDouble() * 2.0 - 1.0) * 5.5f : (float)(azar.NextDouble() * 2.0 - 1.0) * (MitadBarrioZ - 10f);
                GameObject go = Cubo(grupo, "Grieta", new Vector3(x, AlturaAsfalto + 0.002f * (i % 5), z), new Vector3(0.06f, 0.01f, 1f + (float)azar.NextDouble() * 2.5f), grieta);
                go.transform.localRotation = Quaternion.Euler(0f, (float)azar.NextDouble() * 180f, 0f);
            }
        }

        // Losa, pilares, barandales y carros falsos con luces, al norte, fuera del barrio.
        private static void Autopista(Transform padre, Material concreto, Material acero, Material faroBlanco, Material faroRojo, Material carroceria)
        {
            Transform grupo = Hijo(padre, "Autopista");
            Cubo(grupo, "Losa", new Vector3(0f, AlturaLosa, ZAutopista), new Vector3(2f * MitadAutopista, 1f, AnchoAutopista), concreto);
            foreach (float lado in new[] { -1f, 1f })
            {
                Cubo(grupo, "Barandal", new Vector3(0f, AlturaLosa + 1.1f, ZAutopista + lado * (AnchoAutopista / 2f - 0.2f)), new Vector3(2f * MitadAutopista, 1.2f, 0.4f), concreto);
            }
            for (float x = -MitadAutopista + 8f; x <= MitadAutopista; x += PasoPilares)
            {
                Cubo(grupo, "Pilar", new Vector3(x, AlturaLosa / 2f, ZAutopista), new Vector3(1.6f, AlturaLosa, 1.6f), concreto);
                Cubo(grupo, "Poste", new Vector3(x, AlturaLosa + 3f, ZAutopista + AnchoAutopista / 2f - 0.2f), new Vector3(0.2f, 5f, 0.2f), acero, false);
            }

            Transform carros = Hijo(grupo, "Carros");
            carros.localPosition = new Vector3(0f, AlturaLosa + 1.1f, ZAutopista);
            int total = 2 * CochesPorCarril;
            Transform[] transformes = new Transform[total];
            float[] velocidades = new float[total];
            for (int i = 0; i < total; i++)
            {
                bool haciaElEste = i < CochesPorCarril;
                float x = -MitadAutopista + (i % CochesPorCarril) * (2f * MitadAutopista / CochesPorCarril) + (haciaElEste ? 0f : 25f);
                float z = haciaElEste ? -3.2f : 3.2f;
                Transform coche = Hijo(carros, "Carro_" + i);
                coche.localPosition = new Vector3(x, 0f, z);
                Cubo(coche, "Carroceria", new Vector3(0f, 0.6f, 0f), new Vector3(4.2f, 1.2f, 1.8f), carroceria, false);
                float frente = haciaElEste ? 1f : -1f;
                foreach (float lateral in new[] { -0.6f, 0.6f })
                {
                    Cubo(coche, "Faro", new Vector3(frente * 2.15f, 0.6f, lateral), new Vector3(0.1f, 0.25f, 0.4f), faroBlanco, false);
                    Cubo(coche, "Calavera", new Vector3(-frente * 2.15f, 0.6f, lateral), new Vector3(0.1f, 0.25f, 0.4f), faroRojo, false);
                }
                transformes[i] = coche;
                velocidades[i] = haciaElEste ? VelocidadCoche : -VelocidadCoche;
            }
            TraficoAutopista trafico = carros.gameObject.AddComponent<TraficoAutopista>();
            SerializedObject so = new SerializedObject(trafico);
            SerializedProperty propCarros = so.FindProperty("carros");
            SerializedProperty propVelocidades = so.FindProperty("velocidades");
            propCarros.arraySize = total;
            propVelocidades.arraySize = total;
            for (int i = 0; i < total; i++)
            {
                propCarros.GetArrayElementAtIndex(i).objectReferenceValue = transformes[i];
                propVelocidades.GetArrayElementAtIndex(i).floatValue = velocidades[i];
            }
            so.FindProperty("mitadRecorrido").floatValue = MitadAutopista;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Una grúa torre y un andamio detrás de la valla de la obra del oeste.
        private static void GruaYAndamio(Transform padre, Material acero, Material naranja)
        {
            Transform grupo = Hijo(padre, "GruaYAndamio");
            float x = -MitadBarrioX - 14f;
            Cubo(grupo, "MastilGrua", new Vector3(x, 16f, -22f), new Vector3(1.2f, 32f, 1.2f), naranja, false);
            Cubo(grupo, "Aguilon", new Vector3(x + 8f, 32.5f, -22f), new Vector3(28f, 1f, 1f), naranja, false);
            Cubo(grupo, "Contrapeso", new Vector3(x - 8f, 31f, -22f), new Vector3(3f, 3f, 2f), acero, false);
            Cubo(grupo, "Cable", new Vector3(x + 18f, 26f, -22f), new Vector3(0.1f, 12f, 0.1f), acero, false);
            Cubo(grupo, "Gancho", new Vector3(x + 18f, 19.5f, -22f), new Vector3(1.2f, 1f, 1.2f), naranja, false);
            // Andamio: dos pisos de 2 × 6 m con postes y travesaños.
            float xa = -MitadBarrioX - 10f;
            for (int piso = 0; piso < 4; piso++)
            {
                float y = 1.5f + piso * 3f;
                Cubo(grupo, "TablonAndamio", new Vector3(xa, y, 22f), new Vector3(2f, 0.15f, 8f), acero, false);
                foreach (float lateral in new[] { -4f, 0f, 4f })
                {
                    Cubo(grupo, "Travesano", new Vector3(xa, y - 1.5f, 22f + lateral), new Vector3(2f, 0.1f, 0.1f), acero, false);
                }
            }
            foreach (float dx in new[] { -1f, 1f })
            {
                foreach (float dz in new[] { -4f, 0f, 4f })
                {
                    Cubo(grupo, "PosteAndamio", new Vector3(xa + dx, 6f, 22f + dz), new Vector3(0.12f, 12f, 0.12f), acero, false);
                }
            }
        }

        private static Transform Hijo(Transform padre, string nombre)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            return go.transform;
        }

        // Todo es decorado: sin collider (la autopista y la grúa quedan fuera del barrio; el pulido va a ras de piso).
        private static GameObject Cubo(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material, bool conCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static Material Mat(string nombre, Color color, float suavidad, Color emision)
        {
            string ruta = RutaMateriales + "/Final_" + nombre + ".mat";
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
