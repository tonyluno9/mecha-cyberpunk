using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Vehiculos.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1389a: el tráfico usa todos los carros nuevos, repartidos por zona. Arma Carro_V11 (camioneta de policía) y su patrulla, crea CarroTrafico_MT01/MT02,
    /// rehace ConfigCarrosZonas con <see cref="DisenoZonasTrafico"/>, deja un prefab ZonaTrafico por zona y coloca las cajas en Ciudad_Prologo (raíz ZonasTrafico_T1389, idempotente).
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.ZonasTraficoT1389.Aplicar
    /// Por pasos: .ArmarV11, .CrearMotos, .Crear (config), .CrearZonas y .ColocarEnEscena.
    /// </summary>
    public static class ZonasTraficoT1389
    {
        public const string RutaInforme = "Logs/t1389a-trafico.txt";
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string NombreRaiz = "ZonasTrafico_T1389";
        private const float LargoV11 = 4.7f;

        [MenuItem("Mecha Cyberpunk/T-1389a/Aplicar todo")]
        public static void Aplicar()
        {
            StringBuilder informe = new StringBuilder();
            informe.AppendLine(ArmarV11());
            informe.AppendLine(CrearMotos());
            informe.AppendLine(Crear());
            informe.AppendLine(CrearZonas());
            informe.AppendLine(CreadorPatrullaCamionetaT1389.Crear());
            informe.AppendLine(ColocarEnEscena());
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Trafico] T-1389a:\n" + informe);
        }

        public static string ArmarV11()
        {
            AssetDatabase.Refresh();
            CreadorCarrosBlenderT1312.ConfigurarImportador("V11");
            AssetDatabase.Refresh();
            string linea = CreadorCarrosT1342.ArmarUno("V11", LargoV11, new StringBuilder());
            AssetDatabase.SaveAssets();
            return linea;
        }

        public static string CrearMotos()
        {
            StringBuilder s = new StringBuilder();
            foreach (string codigo in DisenoZonasTrafico.MotosNuevas)
            {
                s.AppendLine(ZonasTraficoT1345.CrearTrafico(codigo));
            }
            AssetDatabase.SaveAssets();
            return s.ToString().TrimEnd();
        }

        private static CarroTrafico Carro(string codigo) => AssetDatabase.LoadAssetAtPath<CarroTrafico>(CreadorTraficoT511.RutaPrefab(codigo));

        /// <summary>Rehace ConfigCarrosZonas con la tabla de DisenoZonasTrafico.</summary>
        public static string Crear()
        {
            StringBuilder faltan = new StringBuilder();
            ConfigCarrosZonas config = AssetDatabase.LoadAssetAtPath<ConfigCarrosZonas>(ZonasTraficoT1345.RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigCarrosZonas>();
                AssetDatabase.CreateAsset(config, ZonasTraficoT1345.RutaConfig);
            }
            SerializedObject so = new SerializedObject(config);
            Llenar(so.FindProperty("extrasGenerales"), DisenoZonasTrafico.Extras, faltan);
            SerializedProperty zonas = so.FindProperty("zonas");
            zonas.arraySize = DisenoZonasTrafico.Zonas.Length;
            for (int i = 0; i < DisenoZonasTrafico.Zonas.Length; i++)
            {
                var d = DisenoZonasTrafico.Zonas[i];
                SerializedProperty z = zonas.GetArrayElementAtIndex(i);
                z.FindPropertyRelative("nombre").stringValue = d.nombre;
                z.FindPropertyRelative("mezcla").floatValue = d.mezcla;
                Llenar(z.FindPropertyRelative("carros"), d.carros, faltan);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return "config " + ZonasTraficoT1345.RutaConfig + (faltan.Length == 0 ? "" : "\n" + faltan.ToString().TrimEnd());
        }

        private static void Llenar(SerializedProperty lista, string[] codigos, StringBuilder faltan)
        {
            List<CarroTrafico> validos = new List<CarroTrafico>();
            foreach (string c in codigos)
            {
                CarroTrafico carro = Carro(c);
                if (carro == null)
                {
                    faltan.AppendLine("FALTA CarroTrafico_" + c + " (no entra en " + lista.propertyPath + ")");
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

        /// <summary>Un prefab ZonaTrafico_&lt;zona&gt; por nombre (con el tamaño de su primera caja). No toca los de T-1354 a T-1357 porque esos son de arte, no de tráfico.</summary>
        public static string CrearZonas()
        {
            Directory.CreateDirectory(ZonasTraficoT1345.CarpetaZonas);
            StringBuilder s = new StringBuilder();
            foreach (string nombre in DisenoZonasTrafico.NombresDeZonas())
            {
                DisenoZonasTrafico.Caja caja = DisenoZonasTrafico.Cajas.First(c => c.nombre == nombre);
                GameObject go = new GameObject("ZonaTrafico_" + nombre);
                go.AddComponent<ZonaTrafico>().Definir(nombre, caja.tamano);
                PrefabUtility.SaveAsPrefabAsset(go, ZonasTraficoT1345.RutaZona(nombre));
                Object.DestroyImmediate(go);
                s.AppendLine("zona " + ZonasTraficoT1345.RutaZona(nombre));
            }
            AssetDatabase.SaveAssets();
            return s.ToString().TrimEnd();
        }

        /// <summary>Instancia cada caja de DisenoZonasTrafico en Ciudad_Prologo bajo ZonasTrafico_T1389 (borra y rehace solo esa raíz) y guarda la escena.</summary>
        public static string ColocarEnEscena()
        {
            if (EditorSceneManager.GetActiveScene().path != RutaEscena)
            {
                EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            }
            var escena = EditorSceneManager.GetActiveScene();
            foreach (GameObject viejo in escena.GetRootGameObjects().Where(g => g.name == NombreRaiz).ToList())
            {
                Object.DestroyImmediate(viejo);
            }
            GameObject raiz = new GameObject(NombreRaiz);
            int n = 0;
            foreach (DisenoZonasTrafico.Caja caja in DisenoZonasTrafico.Cajas)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZonasTraficoT1345.RutaZona(caja.nombre));
                if (prefab == null)
                {
                    continue;
                }
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                inst.name = "ZonaTrafico_" + caja.nombre + "_" + (++n);
                inst.transform.SetParent(raiz.transform, false);
                inst.transform.position = caja.centro;
                inst.GetComponent<ZonaTrafico>().Definir(caja.nombre, caja.tamano);
                PrefabUtility.RecordPrefabInstancePropertyModifications(inst.GetComponent<ZonaTrafico>());
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            return n + " cajas de zona en " + NombreRaiz;
        }
    }
}
