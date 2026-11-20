using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.UI.Inicio.Editor
{
    /// <summary>
    /// T-1523: llena en FondoCiudad3D las listas de modelos reales (edificios, carros, vehículos aéreos, antenas y autopista) con prefabs de
    /// Assets/Arte, para que el fondo del menú no use cajas. UI no puede referenciar Mundo, así que los prefabs van serializados como GameObject.
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.UI.Inicio.Editor.ModelosFondoInicioT1523.Aplicar (abre Inicio.unity y la guarda).
    /// CreadorEscenaInicio también llama a <see cref="AsignarA"/> al armar la escena.
    /// </summary>
    public static class ModelosFondoInicioT1523
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Inicio.unity";
        private const string Ed = "Assets/Arte/Zonas/ED/";
        private const string Calle = "Assets/Arte/Calle/Prefabs/";
        private const string Veh = "Assets/Arte/Vehiculos/";

        public static readonly string[] Edificios =
        {
            Ed + "ED06_EdificioNeon_a.prefab", Ed + "ED07_HotelNeon_a.prefab", Ed + "ED08_OficinaNova_a.prefab", Ed + "ED08_OficinaNova_b.prefab",
            Ed + "ED09_PlantaBajaNova_b.prefab", Ed + "ED03_EdificioDeptos_b.prefab", Ed + "ED05_EdificioMercado_a.prefab", Ed + "ED11_EdificioAbandonado_a.prefab",
            Calle + "NV01_FachadaOficinas.prefab", "Assets/Arte/Exteriores/Prefabs/P11_Fachada.prefab", "Assets/Arte/Exteriores/Prefabs/P12_Fachada.prefab"
        };

        public static readonly string[] Autos =
        {
            Veh + "Prefabs/V02.prefab", Veh + "Prefabs/V03.prefab", Veh + "Prefabs/V04.prefab", Veh + "Prefabs/V05.prefab",
            Veh + "Prefabs/V06.prefab", Veh + "Prefabs/V07.prefab", Veh + "Prefabs/V08.prefab", Veh + "Prefabs/V09.prefab", Veh + "Prefabs/V10.prefab"
        };

        public static readonly string[] Aereos =
        {
            Veh + "Drones/Prefabs/Dron_NV01.prefab", Veh + "Drones/Prefabs/Dron_DR01.prefab", Veh + "Drones/Prefabs/Dron_DR02_a.prefab",
            Veh + "Drones/Prefabs/Dron_DR03_a.prefab", Veh + "Drones/Prefabs/Dron_NV03_a.prefab"
        };

        public static readonly string[] Antenas = { Calle + "CY04_AntenaAzotea.prefab" };
        public static readonly string[] Pasos = { "Assets/Arte/Zonas/Modulos/AU01_AutopistaRecta.prefab" };

        [MenuItem("Mecha Cyberpunk/T-1523 Modelos del fondo de Inicio")]
        public static void Aplicar()
        {
            var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            FondoCiudad3D fondo = Object.FindFirstObjectByType<FondoCiudad3D>(FindObjectsInactive.Include);
            if (fondo == null)
            {
                Debug.LogError("[T-1523] Inicio.unity no tiene FondoCiudad3D");
                return;
            }
            AsignarA(fondo);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }

        /// <summary>Llena las cinco listas del fondo con los prefabs que existan.</summary>
        public static void AsignarA(FondoCiudad3D fondo)
        {
            SerializedObject so = new SerializedObject(fondo);
            int total = Llenar(so, "edificios", Edificios) + Llenar(so, "autos", Autos) + Llenar(so, "vehiculosAereos", Aereos)
                + Llenar(so, "antenas", Antenas) + Llenar(so, "pasos", Pasos);
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[T-1523] FondoCiudad3D: " + total + " modelos asignados (edificios " + Edificios.Length + ", autos " + Autos.Length
                + ", aereos " + Aereos.Length + ", antenas " + Antenas.Length + ", pasos " + Pasos.Length + " pedidos)");
        }

        private static int Llenar(SerializedObject so, string campo, string[] rutas)
        {
            List<GameObject> prefabs = rutas.Select(r => AssetDatabase.LoadAssetAtPath<GameObject>(r)).Where(g => g != null).ToList();
            SerializedProperty p = so.FindProperty(campo);
            if (p == null)
            {
                Debug.LogError("[T-1523] FondoCiudad3D no tiene el campo " + campo);
                return 0;
            }
            p.arraySize = prefabs.Count;
            for (int i = 0; i < prefabs.Count; i++)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
            }
            return prefabs.Count;
        }
    }
}
