using System.Collections.Generic;
using MechaCyberpunk.Trafico.Calle;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-511: un prefab de tráfico por cada carro con modelo real (Trafico/Prefabs/CarroTrafico_V*.prefab): variante del carro manejable de
    /// Vehiculos/Prefabs/Carro_V*.prefab con cuerpo cinemático y <see cref="CarroTrafico"/>. Al robarlo el cuerpo se vuelve dinámico y es del jugador.
    /// Corre después de CreadorCarrosT511.Crear. Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.CreadorTraficoT511.Crear
    /// </summary>
    public static class CreadorTraficoT511
    {
        public static readonly string[] Codigos = { "V01a", "V01b", "V02", "V03", "V04", "V05", "V06", "V08" };
        public const string CarpetaVehiculos = "Assets/_Proyecto/Vehiculos/Prefabs/";
        public const string CarpetaTrafico = "Assets/_Proyecto/Trafico/Prefabs/";
        public const string RutaPeaton = CarpetaTrafico + "Peaton.prefab";
        private const float AlturaRaizCarroReal = 0.3f;

        public static string RutaPrefab(string codigo) => CarpetaTrafico + "CarroTrafico_" + codigo + ".prefab";

        /// <summary>Los prefabs de tráfico ya creados, en el orden de <see cref="Codigos"/>.</summary>
        public static List<CarroTrafico> CargarPrefabs()
        {
            List<CarroTrafico> lista = new List<CarroTrafico>();
            foreach (string codigo in Codigos)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(codigo));
                if (prefab != null)
                {
                    lista.Add(prefab.GetComponent<CarroTrafico>());
                }
            }
            return lista;
        }

        [MenuItem("Mecha Cyberpunk/T-511/Crear carros de tráfico")]
        public static void Crear()
        {
            GameObject peaton = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPeaton);
            foreach (string codigo in Codigos)
            {
                GameObject origen = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaVehiculos + "Carro_" + codigo + ".prefab");
                if (origen == null)
                {
                    Debug.LogError("[Trafico] T-511: falta " + CarpetaVehiculos + "Carro_" + codigo + ".prefab (corre CreadorCarrosT511.Crear).");
                    continue;
                }
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(origen);
                instancia.name = "CarroTrafico_" + codigo;
                instancia.GetComponent<Rigidbody>().isKinematic = true;
                CarroTrafico trafico = instancia.GetComponent<CarroTrafico>();
                if (trafico == null)
                {
                    trafico = instancia.AddComponent<CarroTrafico>();
                }
                SerializedObject so = new SerializedObject(trafico);
                so.FindProperty("alturaRaiz").floatValue = AlturaRaizCarroReal;
                so.FindProperty("prefabConductor").objectReferenceValue = peaton;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(instancia, RutaPrefab(codigo));
                Object.DestroyImmediate(instancia);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Trafico] T-511: " + Codigos.Length + " prefabs CarroTrafico_V* listos.");
        }
    }
}
