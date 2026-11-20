using System.Collections.Generic;
using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1503: pone el agua nueva (shader MechaCyberpunk/Mundo/Charco con ondas animadas, reflejo de sonda de respaldo y ondas de las pisadas) a todos
    /// los charcos (quads de la capa Water con nombre "Charco*") de Ciudad_Prologo. Un material por máscara de forma, así que los charcos siguen
    /// agrupándose igual y no cuestan más. Se puede repetir. Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.AguaT1503.Aplicar
    /// </summary>
    public static class AguaT1503
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string CarpetaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        public const string PrefijoMaterial = "T1503_Agua_";
        public const string NombreShader = "MechaCyberpunk/Mundo/Charco";
        public const string PrefijoCharco = "Charco";
        private const float DistorsionOndas = 0.006f;
        private const float IntensidadReflejo = 1.1f;
        private const float FresnelMinimo = 0.5f;

        /// <summary>True si el renderer es un charco: capa Water y nombre que empieza por "Charco".</summary>
        public static bool EsCharco(Renderer r) => r != null && r.gameObject.layer == ReflejoCharcos.CapaCharcos && r.name.StartsWith(PrefijoCharco, System.StringComparison.Ordinal);

        [MenuItem("Mecha Cyberpunk/Mundo/T-1503 Agua de los charcos")]
        public static void Aplicar()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            int cambiados = AplicarEnEscena();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-1503: " + cambiados + " charcos con el agua nueva.");
        }

        /// <summary>Aplica el material nuevo a los charcos de las escenas abiertas; devuelve cuántos.</summary>
        public static int AplicarEnEscena()
        {
            Shader shader = Shader.Find(NombreShader);
            if (shader == null)
            {
                throw new System.Exception("T-1503: no existe el shader " + NombreShader);
            }
            Dictionary<string, Material> porMascara = new Dictionary<string, Material>();
            int n = 0;
            foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!EsCharco(r))
                {
                    continue;
                }
                Material actual = r.sharedMaterial;
                Texture mascara = actual != null && actual.HasProperty("_Mascara") ? actual.GetTexture("_Mascara") : null;
                string clave = mascara != null ? mascara.name : "Redonda";
                if (!porMascara.TryGetValue(clave, out Material nuevo))
                {
                    nuevo = ObtenerMaterial(shader, clave, mascara);
                    porMascara[clave] = nuevo;
                }
                if (r.sharedMaterial != nuevo)
                {
                    r.sharedMaterial = nuevo;
                    EditorUtility.SetDirty(r);
                }
                n++;
            }
            return n;
        }

        private static Material ObtenerMaterial(Shader shader, string clave, Texture mascara)
        {
            string ruta = CarpetaMateriales + "/" + PrefijoMaterial + clave + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.shader = shader;
            if (mascara != null)
            {
                m.SetTexture("_Mascara", mascara);
            }
            m.SetFloat("_Ondas", DistorsionOndas);
            m.SetFloat("_Intensidad", IntensidadReflejo);
            m.SetFloat("_FresnelMin", FresnelMinimo);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
