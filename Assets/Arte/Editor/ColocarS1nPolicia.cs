using System.IO;
using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-680: el policía a pie. Si C01 (Policia.fbx, lo mete <see cref="ImportarS1n"/>) trae esqueleto, arma su visual humanoide con el flujo de T-520
    /// (Visual_Policia, con las animaciones humanoides del proyecto) y el prefab PoliciaAPie lo usa. Si no trae (o todavía no llega), PoliciaAPie usa
    /// el modelo man con un material azul marino de policía. Se rehace cada vez que corre el importador, así que cuando llegue C01 con esqueleto
    /// el prefab lo cambia solo. PoliciaAPie sale de PandilleroArmado (colisión, vida, pistola, IA a pie) con datos propios (DatosPandilleroAPie_Policia).
    /// La lógica de bajarse de la patrulla y disparar va en un lote posterior.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS1nPolicia.Colocar
    /// </summary>
    public static class ColocarS1nPolicia
    {
        public const string RutaPrefab = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";
        public const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Policia.asset";
        private const string RutaDatosBase = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Nova.asset";
        private const string RutaSkulls = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        public static readonly Color AzulMarino = new Color(0.12f, 0.2f, 0.42f);

        /// <summary>True si el .fbx tiene una malla con huesos (más de uno).</summary>
        public static bool TieneEsqueleto(string rutaFbx)
        {
            // Hasta que se importa como Humanoid (PostprocesadorAssetsT250) Unity entrega la malla sin huesos: se reimporta una vez.
            ModelImporter importador = AssetImporter.GetAtPath(rutaFbx) as ModelImporter;
            if (importador != null && importador.animationType != ModelImporterAnimationType.Human)
            {
                AssetDatabase.ImportAsset(rutaFbx, ImportAssetOptions.ForceUpdate);
            }
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(rutaFbx);
            return g != null && g.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(s => s.bones.Length > 1);
        }

        [MenuItem("Mecha Cyberpunk/S1n/Armar policía a pie (T-680)")]
        public static void Colocar()
        {
            bool esqueleto = File.Exists(ImportarS1n.RutaPolicia) && TieneEsqueleto(ImportarS1n.RutaPolicia);
            string visual;
            Material material = null;
            if (esqueleto)
            {
                string ruta = CreadorVisualPersonaje.RutaPrefab("Policia");
                if (!File.Exists(ruta) || File.GetLastWriteTimeUtc(ImportarS1n.RutaPolicia) > File.GetLastWriteTimeUtc(ruta))
                {
                    CreadorVisualTripo.Crear("Policia");
                }
                visual = "Policia";
            }
            else
            {
                ImportarS1n.Informe.AppendLine("policía: C01 sin esqueleto o sin llegar; PoliciaAPie usa el modelo man con uniforme azul marino (pasar C01 por Mixamo)");
                visual = "man";
                material = ConectarVisualesT520.Variante("man", "PoliciaMarino", AzulMarino);
            }
            Armar(RutaPrefab, visual, material);
            AssetDatabase.SaveAssets();
            ImportarS1n.Informe.AppendLine("policía a pie: " + RutaPrefab + " con el visual " + visual + (esqueleto ? " (esqueleto de C01)" : " (reemplazo)"));
        }

        /// <summary>Arma el prefab del policía con el visual dado (nombre del modelo de Tripo) y, si hay, un material para todo el cuerpo.</summary>
        public static GameObject Armar(string rutaDestino, string visual, Material material)
        {
            DatosEnPolicia();
            GameObject contenido = PrefabUtility.LoadPrefabContents(ConectarVisualesT520.RutaPandillero);
            try
            {
                Transform viejo = contenido.transform.Find("Modelo");
                GameObject nuevo = ConectarVisualesT520.Reemplazar(contenido, viejo, ConectarVisualesT520.Visual(visual), "Modelo", false);
                if (material != null)
                {
                    foreach (SkinnedMeshRenderer r in nuevo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        r.sharedMaterial = material;
                    }
                }
                PonerPistola(contenido, nuevo);
                contenido.name = Path.GetFileNameWithoutExtension(rutaDestino);
                Component pandillero = contenido.GetComponent("PandilleroAPie");
                SerializedObject so = new SerializedObject(pandillero);
                so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaDatos);
                so.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(contenido, rutaDestino);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        // La misma pistola W01 y la misma pose en la mano que los SKULLS.
        private static void PonerPistola(GameObject contenido, GameObject modelo)
        {
            Transform mano = modelo.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == CreadorVisualTripo.NombreSocket);
            GameObject skulls = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkulls);
            Transform ejemplo = skulls != null ? skulls.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == ColocarT545.NombreModelo) : null;
            if (mano == null || ejemplo == null)
            {
                return;
            }
            Transform viejo = mano.Find(ColocarT545.NombreModelo);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            GameObject w = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ColocarT545.RutaPrefab), mano);
            w.name = ColocarT545.NombreModelo;
            w.transform.localPosition = ejemplo.localPosition;
            w.transform.localRotation = ejemplo.localRotation;
            w.transform.localScale = ejemplo.localScale;
        }

        private static void DatosEnPolicia()
        {
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaDatos) == null)
            {
                AssetDatabase.CopyAsset(RutaDatosBase, RutaDatos);
            }
            ScriptableObject datos = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaDatos);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nombre").stringValue = "POLICÍA";
            so.FindProperty("colorNormal").colorValue = new Color(0.25f, 0.5f, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
        }
    }
}
