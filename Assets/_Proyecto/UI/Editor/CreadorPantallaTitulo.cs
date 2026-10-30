using System.IO;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Titulo;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-413: viste los dos muñecos del jugador (A "CHAMARRA": ModeloJugador con chamarra naranja y cabello turquesa; B "MÁSCARA":
    /// ModeloJugador_B, variante de ModeloHumano con ropa negra y máscara) y crea UI/Titulo/PantallaTitulo.prefab y
    /// UI/Titulo/PantallaCreacionPandilla.prefab con el kit. Idempotente: los prefabs que ya existen se editan, no se borran (no cambian de GUID).
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorPantallaTitulo.Crear
    /// </summary>
    public static class CreadorPantallaTitulo
    {
        public const string RutaTitulo = "Assets/_Proyecto/UI/Titulo/PantallaTitulo.prefab";
        public const string RutaCreacion = "Assets/_Proyecto/UI/Titulo/PantallaCreacionPandilla.prefab";
        public const string CarpetaMuneco = "Assets/Arte/Personajes/MunecoHumano";
        public const string RutaModeloHumano = CarpetaMuneco + "/ModeloHumano.prefab";
        public const string RutaModeloA = CarpetaMuneco + "/ModeloJugador.prefab";
        public const string RutaModeloB = CarpetaMuneco + "/ModeloJugador_B.prefab";
        private const string CarpetaMateriales = CarpetaMuneco + "/Materiales";
        private const string Torso = "Cadera/Torso";
        private const string Cabeza = Torso + "/Cabeza";
        private static readonly string[] Chamarra = { Torso, Torso + "/BrazoSupDer", Torso + "/BrazoSupIzq" };
        private static readonly string[] Piernas = { "Cadera/PiernaSupDer", "Cadera/PiernaSupIzq", "Cadera/PiernaSupDer/PiernaInfDer", "Cadera/PiernaSupIzq/PiernaInfIzq" };

        private struct Pieza
        {
            public string Padre;
            public string Nombre;
            public Vector3 Centro;
            public Vector3 Tamano;
            public Material Material;
        }

        [MenuItem("Mecha Cyberpunk/Crear T-413 Título y creación")]
        public static void Crear()
        {
            KitUI.FijarEstilo(CreadorKitUI.AsegurarEstilo());
            CrearMunecos();
            Directory.CreateDirectory(Path.GetDirectoryName(RutaTitulo));
            GameObject raiz = new GameObject("PantallaTitulo");
            try
            {
                raiz.AddComponent<PantallaTitulo>().Armar();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaTitulo);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
            raiz = new GameObject("PantallaCreacionPandilla");
            try
            {
                PantallaCreacionPandilla creacion = raiz.AddComponent<PantallaCreacionPandilla>();
                creacion.ConfigurarModelos(new[]
                {
                    AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloA),
                    AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloB),
                });
                creacion.Armar();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaCreacion);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-413: " + RutaTitulo + " y " + RutaCreacion + " creadas.");
        }

        /// <summary>A: chamarra naranja, pantalón oscuro y cabello turquesa. B: todo negro con máscara gris y visor claro.</summary>
        public static void CrearMunecos()
        {
            Material chamarra = Material("Muneco_ChamarraA", new Color(1f, 0.45f, 0.08f));
            Material pantalon = Material("Muneco_PantalonA", new Color(0.12f, 0.14f, 0.22f));
            Material cabello = Material("Muneco_CabelloA", new Color(0.1f, 0.9f, 0.9f));
            Material ropaB = Material("Muneco_RopaB", new Color(0.07f, 0.07f, 0.09f));
            Material mascara = Material("Muneco_MascaraB", new Color(0.45f, 0.47f, 0.52f));
            Material visor = Material("Muneco_VisorB", new Color(0.92f, 0.95f, 1f));

            GameObject a = PrefabUtility.LoadPrefabContents(RutaModeloA);
            try
            {
                Pintar(a.transform, Chamarra, chamarra);
                Pintar(a.transform, Piernas, pantalon);
                Poner(a.transform, new Pieza { Padre = Cabeza, Nombre = "Cabello", Centro = new Vector3(0f, 0.285f, -0.01f), Tamano = new Vector3(0.29f, 0.07f, 0.29f), Material = cabello });
                Poner(a.transform, new Pieza { Padre = Cabeza, Nombre = "CabelloAtras", Centro = new Vector3(0f, 0.17f, -0.135f), Tamano = new Vector3(0.29f, 0.22f, 0.05f), Material = cabello });
                PrefabUtility.SaveAsPrefabAsset(a, RutaModeloA);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(a);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloB) == null)
            {
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaModeloHumano));
                instancia.name = "ModeloJugador_B";
                PrefabUtility.SaveAsPrefabAsset(instancia, RutaModeloB);
                Object.DestroyImmediate(instancia);
            }
            GameObject b = PrefabUtility.LoadPrefabContents(RutaModeloB);
            try
            {
                Pintar(b.transform, Chamarra, ropaB);
                Pintar(b.transform, Piernas, ropaB);
                Poner(b.transform, new Pieza { Padre = Cabeza, Nombre = "Mascara", Centro = new Vector3(0f, 0.13f, 0.125f), Tamano = new Vector3(0.27f, 0.2f, 0.04f), Material = mascara });
                Poner(b.transform, new Pieza { Padre = Cabeza, Nombre = "Visor", Centro = new Vector3(0f, 0.17f, 0.15f), Tamano = new Vector3(0.22f, 0.045f, 0.02f), Material = visor });
                Poner(b.transform, new Pieza { Padre = Cabeza, Nombre = "Capucha", Centro = new Vector3(0f, 0.2f, -0.03f), Tamano = new Vector3(0.3f, 0.2f, 0.27f), Material = ropaB });
                PrefabUtility.SaveAsPrefabAsset(b, RutaModeloB);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(b);
            }
            Debug.Log("[UI] T-413: muñecos A y B vestidos.");
        }

        private static Material Material(string nombre, Color color)
        {
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Pintar(Transform raiz, string[] rutas, Material material)
        {
            foreach (string ruta in rutas)
            {
                Transform malla = raiz.Find(ruta + "/Malla");
                if (malla == null)
                {
                    Debug.LogWarning("[UI] T-413: el muñeco no tiene " + ruta + "/Malla.");
                    continue;
                }
                malla.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        private static void Poner(Transform raiz, Pieza pieza)
        {
            Transform padre = raiz.Find(pieza.Padre);
            if (padre == null)
            {
                Debug.LogWarning("[UI] T-413: el muñeco no tiene " + pieza.Padre + ".");
                return;
            }
            Transform t = padre.Find(pieza.Nombre);
            if (t == null)
            {
                GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(cubo.GetComponent<Collider>());
                cubo.name = pieza.Nombre;
                cubo.transform.SetParent(padre, false);
                t = cubo.transform;
            }
            t.localPosition = pieza.Centro;
            t.localRotation = Quaternion.identity;
            t.localScale = pieza.Tamano;
            t.GetComponent<MeshRenderer>().sharedMaterial = pieza.Material;
        }
    }
}
