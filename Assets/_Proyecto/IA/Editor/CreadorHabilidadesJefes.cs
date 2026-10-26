using MechaCyberpunk.Combate.Datos;
using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// Crea lo de T-178: las habilidades de los 4 jefes de liga (DatosHabilidadRival_Jefe1..4) y les pone LanzadorHabilidades y
    /// HabilidadRival a sus prefabs. Valores de D-tony-139. Idempotente: lo que ya existe no se toca (D-tony-109), así los valores
    /// que Tony cambie se quedan, y un prefab que ya está completo no se vuelve a guardar.
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorHabilidadesJefes.Crear
    /// </summary>
    public static class CreadorHabilidadesJefes
    {
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/Ligas";
        private const string RutaHabilidades = "Assets/_Proyecto/Combate/Datos/Habilidades/DatosHabilidad_";
        private const string RutaConfigEfectos = "Assets/_Proyecto/Combate/Datos/ConfigEfectosHabilidad_Base.asset";
        private const int JefePorLiga = 5;

        private struct Fila
        {
            public string Habilidad;
            public int Nivel;
            public float DistanciaMaxima;

            public Fila(string habilidad, int nivel, float distanciaMaxima)
            {
                Habilidad = habilidad;
                Nivel = nivel;
                DistanciaMaxima = distanciaMaxima;
            }
        }

        // Índice [liga - 1]. La distancia máxima es lo que alcanza la habilidad (el jefe se queda a unos 2 a 3 m del jugador).
        private static readonly Fila[] Tabla =
        {
            new Fila("Embestida", 1, 6f),
            new Fila("Descarga", 2, 3.2f),
            new Fila("Misil", 3, 20f),
            new Fila("Lanzallamas", 4, 3.8f),
        };

        public static string RutaDatosJefe(int liga) => RutaDatos + "/DatosHabilidadRival_Jefe" + liga + ".asset";

        [MenuItem("Mecha Cyberpunk/Crear T-178 habilidades de los jefes")]
        public static void Crear()
        {
            for (int l = 1; l <= CreadorRivalesLiga.Ligas; l++)
            {
                Fila fila = Tabla[l - 1];
                DatosHabilidadRival datos = CrearDatos(l, fila);
                PonerComponentes(CreadorRivalesLiga.RutaPrefab(l, JefePorLiga), datos);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-178 habilidades de los jefes creadas.");
        }

        private static DatosHabilidadRival CrearDatos(int liga, Fila fila)
        {
            string ruta = RutaDatosJefe(liga);
            DatosHabilidadRival datos = AssetDatabase.LoadAssetAtPath<DatosHabilidadRival>(ruta);
            if (datos != null)
            {
                return datos;
            }
            datos = ScriptableObject.CreateInstance<DatosHabilidadRival>();
            AssetDatabase.CreateAsset(datos, ruta);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("habilidad").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosHabilidad>(RutaHabilidades + fila.Habilidad + ".asset");
            so.FindProperty("nivel").intValue = fila.Nivel;
            so.FindProperty("distanciaMaxima").floatValue = fila.DistanciaMaxima;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            return datos;
        }

        private static void PonerComponentes(string rutaPrefab, DatosHabilidadRival datos)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
            if (prefab == null)
            {
                Debug.LogError("[IA] Falta el prefab " + rutaPrefab + ": corre antes CreadorRivalesLiga.");
                return;
            }
            if (prefab.GetComponent<LanzadorHabilidades>() != null && prefab.GetComponent<HabilidadRival>() != null)
            {
                return;
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(rutaPrefab);
            try
            {
                if (contenido.GetComponent<LanzadorHabilidades>() == null)
                {
                    LanzadorHabilidades lanzador = contenido.AddComponent<LanzadorHabilidades>();
                    Asignar(lanzador, "config", AssetDatabase.LoadAssetAtPath<ConfigEfectosHabilidad>(RutaConfigEfectos));
                }
                if (contenido.GetComponent<HabilidadRival>() == null)
                {
                    HabilidadRival habilidad = contenido.AddComponent<HabilidadRival>();
                    Asignar(habilidad, "datos", datos);
                }
                PrefabUtility.SaveAsPrefabAsset(contenido, rutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void Asignar(Object componente, string campo, Object valor)
        {
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
