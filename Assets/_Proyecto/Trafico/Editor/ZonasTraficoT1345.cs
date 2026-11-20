using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Vehiculos.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>T-1345: crea los CarroTrafico_V12..V24, la config de carros por zona y un prefab ZonaTrafico por zona.</summary>
    public static class ZonasTraficoT1345
    {
        public const string RutaConfig = "Assets/_Proyecto/Trafico/Resources/ConfigCarrosZonas.asset";
        public const string CarpetaZonas = "Assets/_Proyecto/Trafico/Prefabs/Zonas/";
        public const string RutaInforme = "Logs/t1345-zonas.txt";
        private const float AlturaRaizCarroReal = 0.3f;

        public static readonly string[] Codigos =
        {
            "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20", "V21", "V22", "V23", "V24",
        };

        public static readonly string[] Extras = { "V12", "V13", "V14", "V19" };

        public static readonly (string nombre, string[] carros, float mezcla)[] Zonas =
        {
            ("Deshuesadero", new[] { "V20", "V21" }, 0.7f),
            ("NeonAlto", new[] { "V22", "V23", "V24", "V12" }, 0.5f),
            ("LasLomas", new[] { "V15", "V13" }, 0.7f),
            ("Avenida", new[] { "V16", "V17", "V18", "V13", "V14" }, 0.4f),
        };

        public static string RutaZona(string nombre) => CarpetaZonas + "ZonaTrafico_" + nombre + ".prefab";

        [MenuItem("Mecha Cyberpunk/T-1345/Zonas de tráfico")]
        public static void Crear()
        {
            StringBuilder informe = new StringBuilder();
            foreach (string codigo in Codigos)
            {
                informe.AppendLine(CrearTrafico(codigo));
            }
            AssetDatabase.Refresh();
            CrearConfig(informe);
            Directory.CreateDirectory(CarpetaZonas);
            foreach (var zona in Zonas)
            {
                GameObject go = new GameObject("ZonaTrafico_" + zona.nombre);
                go.AddComponent<ZonaTrafico>().Definir(zona.nombre, new Vector3(80f, 20f, 80f));
                PrefabUtility.SaveAsPrefabAsset(go, RutaZona(zona.nombre));
                Object.DestroyImmediate(go);
                informe.AppendLine("zona\t" + RutaZona(zona.nombre));
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Trafico] T-1345 listo:\n" + informe);
        }

        private static CarroTrafico Carro(string codigo) => AssetDatabase.LoadAssetAtPath<CarroTrafico>(CreadorTraficoT511.RutaPrefab(codigo));

        private static void CrearConfig(StringBuilder informe)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaConfig));
            ConfigCarrosZonas config = AssetDatabase.LoadAssetAtPath<ConfigCarrosZonas>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigCarrosZonas>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            SerializedObject so = new SerializedObject(config);
            LlenarLista(so.FindProperty("extrasGenerales"), Extras, informe);
            SerializedProperty zonas = so.FindProperty("zonas");
            zonas.arraySize = Zonas.Length;
            for (int i = 0; i < Zonas.Length; i++)
            {
                SerializedProperty z = zonas.GetArrayElementAtIndex(i);
                z.FindPropertyRelative("nombre").stringValue = Zonas[i].nombre;
                z.FindPropertyRelative("mezcla").floatValue = Zonas[i].mezcla;
                LlenarLista(z.FindPropertyRelative("carros"), Zonas[i].carros, informe);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            informe.AppendLine("config\t" + RutaConfig);
        }

        // Solo mete los carros que existen (un prefab que falta se anota y se salta).
        private static void LlenarLista(SerializedProperty lista, string[] codigos, StringBuilder informe)
        {
            List<CarroTrafico> validos = new List<CarroTrafico>();
            foreach (string c in codigos)
            {
                CarroTrafico carro = Carro(c);
                if (carro == null)
                {
                    informe.AppendLine("FALTA CarroTrafico_" + c + " (no entra en " + lista.propertyPath + ")");
                    continue;
                }
                validos.Add(carro);
            }
            lista.arraySize = validos.Count;
            for (int i = 0; i < validos.Count; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = validos[i];
            }
        }

        // Lo mismo que ImportarS2nCarros.CrearTrafico (vive en Arte.Editor, que Trafico.Editor no ve): CarroTrafico_<cod> desde Carro_<cod>.
        internal static string CrearTrafico(string codigo)
        {
            string rutaOrigen = CreadorCarrosT511.RutaPrefab(codigo);
            GameObject origen = AssetDatabase.LoadAssetAtPath<GameObject>(rutaOrigen);
            if (origen == null)
            {
                return codigo + "\tFALTA " + rutaOrigen;
            }
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(origen);
            instancia.name = "CarroTrafico_" + codigo;
            instancia.GetComponent<Rigidbody>().isKinematic = true;
            CarroTrafico trafico = instancia.GetComponent<CarroTrafico>() ?? instancia.AddComponent<CarroTrafico>();
            SerializedObject so = new SerializedObject(trafico);
            so.FindProperty("alturaRaiz").floatValue = AlturaRaizCarroReal;
            so.FindProperty("prefabConductor").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorTraficoT511.RutaPeaton);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(instancia, CreadorTraficoT511.RutaPrefab(codigo));
            Object.DestroyImmediate(instancia);
            return codigo + "\ttráfico " + CreadorTraficoT511.RutaPrefab(codigo);
        }
    }
}
