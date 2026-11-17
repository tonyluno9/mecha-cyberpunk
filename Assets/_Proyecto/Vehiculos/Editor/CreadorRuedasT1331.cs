using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-1331: V01a, V01b, V05 y la patrulla seguían con la llanta vieja RU01 (T-742); T-1312 solo cambió V02, V03, V04, V06, V09, V10 y la moto C19.
    /// Pone en esos carros la rueda de Blender de V09 (malla y material de Assets/Arte/Vehiculos/Piezas/V09), con el mismo radio de la llanta que reemplaza,
    /// centrada en el pivote Giro y con su eje en X (igual que <see cref="CreadorCarrosBlenderT1312"/>). No toca la suspensión, la dirección ni <see cref="RuedasVisualesCarro"/>.
    /// V07 y V08 siguen con las ruedas pegadas a la carrocería: no hay FBX de Blender con ruedas separadas para ellos.
    /// Idempotente. Lo llama también <see cref="CreadorCarrosPiezasT742.Armar"/> al rearmar. Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CreadorRuedasT1331.Aplicar
    /// </summary>
    public static class CreadorRuedasT1331
    {
        public const string CodigoRueda = "V09";
        public const string RutaFbxRueda = CreadorCarrosPiezasT742.CarpetaPiezas + CodigoRueda + "/" + CodigoRueda + ".fbx";
        public const string RutaMaterialRueda = CreadorCarrosPiezasT742.CarpetaPiezas + CodigoRueda + "/" + CodigoRueda + "_Blender_Rueda.mat";
        public const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        public static readonly string[] Carros = { "V01a", "V01b", "V05" };

        public static string[] Prefabs() => Carros.Select(c => CreadorCarrosPiezasT742.RutaPrefab(c)).Append(RutaPatrulla).ToArray();

        [MenuItem("Mecha Cyberpunk/T-1331/Ruedas de Blender en V01, V05 y patrulla")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (string ruta in Prefabs())
            {
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    informe.AppendLine(ruta + "\t" + CambiarLlantas(raiz));
                    PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(raiz);
                }
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t1331-ruedas.txt", informe.ToString());
            Debug.Log("[Vehiculos] T-1331:\n" + informe);
        }

        /// <summary>Cambia el Modelo de cada Rueda_xx/Direccion/Giro por la rueda de Blender. Devuelve una línea de informe.</summary>
        public static string CambiarLlantas(GameObject raiz)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbxRueda);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialRueda);
            if (fbx == null || material == null)
            {
                return "FALTA " + RutaFbxRueda + " o " + RutaMaterialRueda;
            }
            Quaternion deRaiz = fbx.transform.localRotation;
            StringBuilder sb = new StringBuilder();
            foreach (string nombre in CreadorCarrosPiezasT742.NombresRuedas)
            {
                Transform ancla = raiz.transform.Find(nombre);
                Transform giro = ancla != null ? ancla.Find("Direccion/Giro") : null;
                Transform fuente = fbx.transform.Find(nombre);
                if (giro == null || fuente == null || fuente.GetComponent<MeshFilter>() == null)
                {
                    sb.Append(nombre + " sin cadena; ");
                    continue;
                }
                Transform viejo = giro.Find("Modelo");
                float radio = 0f;
                if (viejo != null)
                {
                    Bounds bv = CreadorCarrosBlenderT1312.LimitesEn(viejo.gameObject, giro);
                    radio = Mathf.Max(bv.size.y, bv.size.z) * 0.5f;
                    Object.DestroyImmediate(viejo.gameObject);
                }
                if (radio <= 0.01f)
                {
                    radio = 0.33f;
                }
                GameObject modelo = new GameObject("Modelo");
                modelo.transform.SetParent(giro, false);
                modelo.AddComponent<MeshFilter>().sharedMesh = fuente.GetComponent<MeshFilter>().sharedMesh;
                modelo.AddComponent<MeshRenderer>().sharedMaterial = material;
                modelo.transform.rotation = raiz.transform.rotation * Quaternion.Euler(0f, 180f, 0f) * deRaiz;
                Bounds b = CreadorCarrosBlenderT1312.LimitesEn(modelo, giro);
                modelo.transform.localScale = Vector3.one * (2f * radio / Mathf.Max(b.size.y, b.size.z));
                b = CreadorCarrosBlenderT1312.LimitesEn(modelo, giro);
                modelo.transform.localPosition = -b.center;
                sb.Append(nombre + " r " + radio.ToString("F3") + " m; ");
            }
            return sb.ToString();
        }
    }
}
