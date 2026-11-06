using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-679: arma las tres armas nuevas (W02 escopeta, W03 fusil, W04 francotirador; las mete <see cref="ImportarS1n"/>) igual que la pistola W01:
    /// - Material URP Lit con su color y su normal, y el .fbx sin materiales ni animación.
    /// - Prefab Wxx_Nombre.prefab: cañón a +Z, arriba +Y y la empuñadura (donde la agarra la palma) en el origen; hijo "Boca" en la punta del cañón.
    /// - Prefab Wxx_Nombre_Piso.prefab: el arma tirada de lado, con <see cref="ArmaEnPiso"/> (la que gira y brilla), centrada en su pivote.
    /// - En la mano del jugador (JugadorAPie.prefab): el modelo en el objeto Escopeta / Fusil / Sniper del socket de la mano derecha, sin las cajas, con
    ///   la luz de la boca en la punta del cañón y los agarres de <see cref="AgarresArmas"/>.
    /// Se salta las armas cuyo modelo no ha llegado. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS1nArmas.Colocar
    /// </summary>
    public static class ColocarS1nArmas
    {
        public const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";

        /// <summary>Medidas del modelo tal como sale del .fbx (X a lo largo con el cañón a -X, Y arriba), medidas a ojo sobre el render de lado.</summary>
        public readonly struct Medida
        {
            public readonly string Codigo;
            public readonly string Nombre;
            /// <summary>Nombre del objeto del arma en el socket del jugador (el que lee ArmasAPie).</summary>
            public readonly string Objeto;
            public readonly ArmaAPie Arma;
            public readonly string Articulo;
            /// <summary>Factor al tamaño del .fbx (largo de unos 0.98 m): escopeta 1.0, fusil compacto 0.85, francotirador largo 1.1.</summary>
            public readonly float Escala;
            /// <summary>Palma en la empuñadura (X, Y del .fbx).</summary>
            public readonly Vector2 Empunadura;
            /// <summary>Punta del cañón (X, Y del .fbx).</summary>
            public readonly Vector2 Boca;

            public Medida(string codigo, string nombre, string objeto, ArmaAPie arma, string articulo, float escala, Vector2 empunadura, Vector2 boca)
            {
                Codigo = codigo;
                Nombre = nombre;
                Objeto = objeto;
                Arma = arma;
                Articulo = articulo;
                Escala = escala;
                Empunadura = empunadura;
                Boca = boca;
            }

            public string Carpeta => ImportarS1n.CarpetaArmas + Codigo + "_" + Nombre + "/";
            public string RutaFbx => Carpeta + Codigo + "_" + Nombre + ".fbx";
            public string RutaMaterial => Carpeta + Codigo + "_" + Nombre + ".mat";
            public string RutaPrefab => Carpeta + Codigo + "_" + Nombre + ".prefab";
            public string RutaPiso => Carpeta + Codigo + "_" + Nombre + "_Piso.prefab";

            /// <summary>La boca del cañón en el espacio del prefab (cañón a +Z, empuñadura en el origen).</summary>
            public Vector3 BocaEnPrefab => new Vector3(0f, Escala * (Boca.y - Empunadura.y), Escala * (Empunadura.x - Boca.x));
        }

        public static readonly Medida[] Medidas =
        {
            new Medida("W02", "Escopeta", "Escopeta", ArmaAPie.Escopeta, "la escopeta", 1.0f, new Vector2(0.158f, 0.09f), new Vector2(-0.49f, 0.285f)),
            new Medida("W03", "Fusil", "Fusil", ArmaAPie.Fusil, "el fusil", 0.85f, new Vector2(0.2f, 0.17f), new Vector2(-0.49f, 0.348f)),
            new Medida("W04", "Francotirador", "Sniper", ArmaAPie.Sniper, "el francotirador", 1.1f, new Vector2(0.27f, 0.09f), new Vector2(-0.49f, 0.196f)),
        };

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/S1n/Armar armas nuevas (T-679)")]
        public static void Colocar()
        {
            Informe.Clear();
            Medida[] listas = Medidas.Where(m => File.Exists(m.RutaFbx)).ToArray();
            foreach (Medida m in Medidas.Except(listas))
            {
                Informe.AppendLine("falta " + m.RutaFbx + " (se arma cuando llegue)");
            }
            foreach (Medida m in listas)
            {
                Importacion(m);
                Material material = Material(m);
                GameObject prefab = Prefab(m, material);
                Piso(m, prefab);
            }
            if (listas.Length > 0)
            {
                Jugador(listas);
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/s1n-armas.txt", Informe.ToString());
            Debug.Log("[Arte] T-679 armas:\n" + Informe);
        }

        // Como W01: el .fbx sin materiales (los pone el prefab) y sin animación.
        private static void Importacion(Medida m)
        {
            ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(m.RutaFbx);
            if (importador != null && (importador.materialImportMode != ModelImporterMaterialImportMode.None || importador.importAnimation))
            {
                importador.materialImportMode = ModelImporterMaterialImportMode.None;
                importador.importAnimation = false;
                importador.SaveAndReimport();
            }
        }

        private static Material Material(Medida m)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(m.RutaMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, m.RutaMaterial);
            }
            string texturas = m.Carpeta + "Texturas/";
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(texturas + m.Codigo + "_basecolor.JPEG");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(texturas + m.Codigo + "_normal.JPEG");
            if (normal != null)
            {
                TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal));
                if (ti.textureType != TextureImporterType.NormalMap)
                {
                    ti.textureType = TextureImporterType.NormalMap;
                    ti.SaveAndReimport();
                }
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            material.SetTexture("_BaseMap", color);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0.5f);
            material.SetFloat("_Smoothness", 0.45f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            Informe.AppendLine("material " + m.RutaMaterial + ": color " + (color != null ? color.name : "FALTA") + ", normal " + (normal != null ? normal.name : "no"));
            return material;
        }

        private static GameObject Prefab(Medida m, Material material)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(m.RutaFbx);
            GameObject raiz = new GameObject(m.Codigo + "_" + m.Nombre);
            // "Modelo" es el pivote que gira, escala y baja; adentro va el .fbx tal como se importó (su nodo ya trae el giro de eje).
            GameObject modelo = new GameObject("Modelo");
            modelo.transform.SetParent(raiz.transform, false);
            GameObject malla = (GameObject)PrefabUtility.InstantiatePrefab(fbx, modelo.transform);
            PrefabUtility.UnpackPrefabInstance(malla, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            malla.name = "Malla";
            foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = material;
            }
            // El .fbx tiene el cañón a -X: se gira para que mire a +Z, se escala y se baja para que la empuñadura quede en el origen.
            modelo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            modelo.transform.localScale = Vector3.one * m.Escala;
            modelo.transform.localPosition = new Vector3(0f, -m.Escala * m.Empunadura.y, m.Escala * m.Empunadura.x);
            GameObject boca = new GameObject("Boca");
            boca.transform.SetParent(raiz.transform, false);
            boca.transform.localPosition = m.BocaEnPrefab;
            Bounds b = Limites(raiz);
            GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, m.RutaPrefab);
            Object.DestroyImmediate(raiz);
            Informe.AppendLine("prefab " + m.RutaPrefab + ": largo " + b.size.z.ToString("F2") + " m, alto " + b.size.y.ToString("F2") + ", boca " + m.BocaEnPrefab.ToString("F3"));
            return guardado;
        }

        /// <summary>El arma tirada de lado (acostada sobre su costado) con ArmaEnPiso; el modelo queda centrado en el pivote que gira.</summary>
        private static void Piso(Medida m, GameObject arma)
        {
            GameObject raiz = new GameObject(m.Codigo + "_" + m.Nombre + "_Piso");
            GameObject pivote = new GameObject("Modelo");
            pivote.transform.SetParent(raiz.transform, false);
            GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(arma, pivote.transform);
            modelo.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Bounds b = Limites(raiz);
            modelo.transform.localPosition -= b.center - raiz.transform.position;
            ArmaEnPiso piso = raiz.AddComponent<ArmaEnPiso>();
            SerializedObject so = new SerializedObject(piso);
            so.FindProperty("arma").enumValueIndex = (int)m.Arma;
            so.FindProperty("nombre").stringValue = m.Articulo;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, m.RutaPiso);
            Object.DestroyImmediate(raiz);
            Informe.AppendLine("prefab del piso " + m.RutaPiso);
        }

        private static void Jugador(Medida[] listas)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaJugador);
            try
            {
                ApuntadoIK ik = contenido.GetComponentsInChildren<ApuntadoIK>(true).FirstOrDefault(a => a.SocketArma != null);
                if (ik == null)
                {
                    Informe.AppendLine("ERROR: JugadorAPie no tiene ApuntadoIK con socket de arma");
                    return;
                }
                foreach (Medida m in listas)
                {
                    Transform objeto = ik.SocketArma.Find(m.Objeto);
                    if (objeto == null)
                    {
                        Informe.AppendLine("ERROR: el socket no tiene el objeto " + m.Objeto);
                        continue;
                    }
                    Transform viejo = objeto.Find(m.Codigo);
                    if (viejo != null)
                    {
                        Object.DestroyImmediate(viejo.gameObject);
                    }
                    // Las cajas de antes se quedan en la jerarquía pero no se ven.
                    foreach (Renderer r in objeto.GetComponentsInChildren<Renderer>(true))
                    {
                        r.enabled = false;
                    }
                    GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(m.RutaPrefab), objeto);
                    modelo.name = m.Codigo;
                    modelo.transform.localPosition = Vector3.zero;
                    modelo.transform.localRotation = Quaternion.identity;
                    modelo.transform.localScale = Vector3.one;
                    foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
                    {
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    Transform boca = objeto.Find("Boca");
                    if (boca != null)
                    {
                        boca.localPosition = m.BocaEnPrefab;
                    }
                    Informe.AppendLine("jugador: " + m.Codigo + " en la mano como " + m.Objeto);
                }
                AplicarAgarresArmas.Aplicar(contenido);
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaJugador);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static Bounds Limites(GameObject raiz)
        {
            Renderer[] renderers = raiz.GetComponentsInChildren<Renderer>(true);
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers.Skip(1))
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}
