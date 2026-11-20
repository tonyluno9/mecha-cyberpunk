using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1523: crea en Personaje/Resources los prefabs de los pickups sin primitivas. No hay billetes ni cargadores en Assets/Arte, así que
    /// el dinero usa el maletín MA01 y la munición la caja de metal CB03, los dos a escala de pickup. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.ModelosPickupsT1523.Aplicar
    /// </summary>
    public static class ModelosPickupsT1523
    {
        public const string CarpetaResources = "Assets/_Proyecto/Personaje/Resources";
        public const string RutaDinero = CarpetaResources + "/DineroEnPiso_Modelo.prefab";
        public const string RutaMunicion = CarpetaResources + "/MunicionEnPiso_Modelo.prefab";
        private const string FuenteDinero = "Assets/Arte/Objetos/MA01_Maletin.prefab";
        private const string FuenteMunicion = "Assets/Arte/Zonas/CB/CB03_CajasMetal_a.prefab";
        /// <summary>Lado mayor (m) del modelo ya como pickup.</summary>
        private const float LadoDinero = 0.32f;
        private const float LadoMunicion = 0.3f;

        [MenuItem("Mecha Cyberpunk/T-1523 Modelos de pickups")]
        public static void Aplicar()
        {
            Directory.CreateDirectory(CarpetaResources);
            AssetDatabase.Refresh();
            int n = Crear(FuenteDinero, RutaDinero, LadoDinero) + Crear(FuenteMunicion, RutaMunicion, LadoMunicion);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1523] Pickups: " + n + " prefabs creados o actualizados en " + CarpetaResources);
        }

        private static int Crear(string fuente, string destino, float lado)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fuente);
            if (prefab == null)
            {
                Debug.LogError("[T-1523] Falta " + fuente);
                return 0;
            }
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instancia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instancia.transform.localScale = Vector3.one;
            Renderer[] rs = instancia.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0)
            {
                Object.DestroyImmediate(instancia);
                Debug.LogError("[T-1523] " + fuente + " no tiene renderers");
                return 0;
            }
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            float mayor = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            float escala = lado / Mathf.Max(0.001f, mayor);
            instancia.transform.localScale = Vector3.one * escala;
            // Centrado en x/z con la base en y = 0; el pickup lo sube y lo gira desde su padre.
            instancia.transform.position = new Vector3(-b.center.x * escala, -b.min.y * escala, -b.center.z * escala);
            GameObject guardado = PrefabUtility.SaveAsPrefabAsset(instancia, destino);
            Object.DestroyImmediate(instancia);
            return guardado != null ? 1 : 0;
        }
    }
}
