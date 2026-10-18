using System.Globalization;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// Crea lo de T-157: los 20 rivales de las 4 ligas (DatosMecha, DatosRival y prefab por rival) y 8 materiales de cuerpo.
    /// Valores provisionales de D-tony-97. Idempotente: lo que ya existe no se toca, asi los valores que Tony cambie se quedan.
    /// Los prefabs son variantes de RivalBasico (que no lleva PiedadRival); el quinto de cada liga lleva FuriaJefe.
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorRivalesLiga.Crear
    /// </summary>
    public static class CreadorRivalesLiga
    {
        public const int Ligas = 4;
        public const int RivalesPorLiga = 5;

        private const string RutaIA = "Assets/_Proyecto/IA";
        private const string RutaDatos = RutaIA + "/Datos/Ligas";
        private const string RutaPrefabs = RutaIA + "/Prefabs/Ligas";
        private const string RutaMateriales = RutaIA + "/Materiales";
        private const string RutaPrefabBase = RutaIA + "/Prefabs/RivalBasico.prefab";
        private const string RutaConfigFuria = RutaIA + "/Datos/ConfigFuriaJefe_Base.asset";
        private const float DistanciaAtaque = 2.6f;
        private const float DistanciaMinima = 2f;

        private struct Fila
        {
            public string Alias;
            public float Vida;
            public float Danio;
            public float Velocidad;
            public float Preparacion;
            public float EsperaMin;
            public float EsperaMax;

            public Fila(string alias, float vida, float danio, float velocidad, float preparacion, float esperaMin, float esperaMax)
            {
                Alias = alias;
                Vida = vida;
                Danio = danio;
                Velocidad = velocidad;
                Preparacion = preparacion;
                EsperaMin = esperaMin;
                EsperaMax = esperaMax;
            }
        }

        // Indice [liga - 1][n - 1]. El quinto de cada liga es el jefe.
        private static readonly Fila[][] Tabla =
        {
            new[]
            {
                new Fila("TORNILLO", 80f, 8f, 3.8f, 0.6f, 1.4f, 2.6f),
                new Fila("LA RATA", 90f, 8f, 4.6f, 0.6f, 1.0f, 2.0f),
                new Fila("EL BACHE", 110f, 11f, 3.2f, 0.7f, 1.6f, 2.8f),
                new Fila("MOFLE", 100f, 10f, 4.0f, 0.55f, 1.2f, 2.4f),
                new Fila("DON FIERRO", 150f, 12f, 4.0f, 0.55f, 1.1f, 2.2f),
            },
            new[]
            {
                new Fila("NEÓN", 120f, 11f, 4.2f, 0.55f, 1.2f, 2.4f),
                new Fila("CUMBIA", 125f, 11f, 4.8f, 0.5f, 0.9f, 1.9f),
                new Fila("EL TOPE", 160f, 14f, 3.3f, 0.65f, 1.5f, 2.7f),
                new Fila("LA PATRULLA", 140f, 12f, 4.2f, 0.5f, 1.1f, 2.2f),
                new Fila("LA MATRACA", 190f, 14f, 4.3f, 0.5f, 1.0f, 2.0f),
            },
            new[]
            {
                new Fila("VOLTIO", 165f, 14f, 4.4f, 0.5f, 1.1f, 2.2f),
                new Fila("LA DOÑA", 175f, 14f, 4.6f, 0.45f, 1.0f, 2.0f),
                new Fila("GRÚA", 210f, 17f, 3.4f, 0.6f, 1.4f, 2.6f),
                new Fila("EL PERITO", 190f, 15f, 4.4f, 0.45f, 1.0f, 2.0f),
                new Fila("EL ALCALDE", 250f, 17f, 4.5f, 0.45f, 0.9f, 1.8f),
            },
            new[]
            {
                new Fila("OBSIDIANA", 220f, 17f, 4.5f, 0.45f, 1.0f, 2.0f),
                new Fila("XOLO", 225f, 17f, 5.0f, 0.4f, 0.8f, 1.6f),
                new Fila("TLÁLOC", 270f, 20f, 3.6f, 0.55f, 1.3f, 2.4f),
                new Fila("LA LLORONA", 240f, 18f, 4.6f, 0.4f, 0.9f, 1.8f),
                new Fila("EL CACIQUE", 320f, 22f, 4.6f, 0.4f, 0.8f, 1.6f),
            },
        };

        // Color de la liga: oxido, magenta, turquesa, naranja. El jefe usa la version mas brillante.
        private static readonly Color[] ColoresLiga =
        {
            new Color(0.45f, 0.2f, 0.08f),
            new Color(0.6f, 0.08f, 0.45f),
            new Color(0.05f, 0.45f, 0.45f),
            new Color(0.7f, 0.3f, 0.05f),
        };

        private static readonly Color[] ColoresJefe =
        {
            new Color(1f, 0.4f, 0.12f),
            new Color(1f, 0.15f, 0.8f),
            new Color(0.1f, 1f, 0.95f),
            new Color(1f, 0.6f, 0.05f),
        };

        public static string RutaPrefab(int liga, int n) => RutaPrefabs + "/RivalLiga" + liga + "_" + n + ".prefab";

        [MenuItem("Mecha Cyberpunk/Crear T-157 rivales de liga")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaDatos);
            AsegurarCarpeta(RutaPrefabs);
            AsegurarCarpeta(RutaMateriales);
            AssetDatabase.Refresh();

            Material[] materialesLiga = new Material[Ligas];
            Material[] materialesJefe = new Material[Ligas];
            for (int l = 0; l < Ligas; l++)
            {
                materialesLiga[l] = CrearMaterial(RutaMateriales + "/RivalLiga" + (l + 1) + "_Cuerpo.mat", ColoresLiga[l]);
                materialesJefe[l] = CrearMaterial(RutaMateriales + "/RivalLiga" + (l + 1) + "_Jefe_Cuerpo.mat", ColoresJefe[l]);
            }
            AssetDatabase.SaveAssets();

            for (int l = 0; l < Ligas; l++)
            {
                for (int n = 0; n < RivalesPorLiga; n++)
                {
                    Fila fila = Tabla[l][n];
                    string sufijo = (l + 1) + "_" + (n + 1);
                    bool esJefe = n == RivalesPorLiga - 1;
                    DatosMecha mecha = CrearDatosMecha(RutaDatos + "/DatosMecha_Liga" + sufijo + ".asset", fila);
                    DatosRival rival = CrearDatosRival(RutaDatos + "/DatosRival_Liga" + sufijo + ".asset", fila, mecha);
                    AssetDatabase.SaveAssets();
                    string rutaPrefab = RutaPrefab(l + 1, n + 1);
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab) == null)
                    {
                        ArmarPrefab(rutaPrefab, "RivalLiga" + sufijo, mecha, rival, esJefe ? materialesJefe[l] : materialesLiga[l], esJefe);
                    }
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-157 rivales de liga creados.");
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }
        }

        private static string NombreMecha(string alias)
        {
            CultureInfo cultura = CultureInfo.GetCultureInfo("es-MX");
            string minusculas = alias.ToLower(cultura);
            return char.ToUpper(minusculas[0], cultura) + minusculas.Substring(1);
        }

        private static DatosMecha CrearDatosMecha(string ruta, Fila fila)
        {
            DatosMecha datos = AssetDatabase.LoadAssetAtPath<DatosMecha>(ruta);
            if (datos != null)
            {
                return datos;
            }
            datos = ScriptableObject.CreateInstance<DatosMecha>();
            AssetDatabase.CreateAsset(datos, ruta);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nombre").stringValue = NombreMecha(fila.Alias);
            so.FindProperty("vidaMaxima").floatValue = fila.Vida;
            so.FindProperty("velocidadMaxima").floatValue = fila.Velocidad;
            so.FindProperty("danioGolpe").floatValue = fila.Danio;
            so.FindProperty("preparacionGolpe").floatValue = fila.Preparacion;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return datos;
        }

        private static DatosRival CrearDatosRival(string ruta, Fila fila, DatosMecha mecha)
        {
            DatosRival datos = AssetDatabase.LoadAssetAtPath<DatosRival>(ruta);
            if (datos != null)
            {
                return datos;
            }
            datos = ScriptableObject.CreateInstance<DatosRival>();
            AssetDatabase.CreateAsset(datos, ruta);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("alias").stringValue = fila.Alias;
            so.FindProperty("mecha").objectReferenceValue = mecha;
            so.FindProperty("distanciaAtaque").floatValue = DistanciaAtaque;
            so.FindProperty("distanciaMinima").floatValue = DistanciaMinima;
            so.FindProperty("esperaEntreAtaquesMin").floatValue = fila.EsperaMin;
            so.FindProperty("esperaEntreAtaquesMax").floatValue = fila.EsperaMax;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return datos;
        }

        private static Material CrearMaterial(string ruta, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material != null)
            {
                return material;
            }
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, ruta);
            return material;
        }

        private static void ArmarPrefab(string ruta, string nombre, DatosMecha mecha, DatosRival rival, Material cuerpo, bool esJefe)
        {
            GameObject baseRival = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabBase);
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(baseRival);
            try
            {
                instancia.name = nombre;
                Asignar(instancia.GetComponent<RivalBasico>(), "datos", rival);
                Asignar(instancia.GetComponent<VidaMecha>(), "datos", mecha);
                instancia.GetComponent<PresentacionRival>().Cuerpo.sharedMaterial = cuerpo;
                if (esJefe)
                {
                    FuriaJefe furia = instancia.AddComponent<FuriaJefe>();
                    Asignar(furia, "config", AssetDatabase.LoadAssetAtPath<ConfigFuriaJefe>(RutaConfigFuria));
                }
                PrefabUtility.SaveAsPrefabAsset(instancia, ruta);
            }
            finally
            {
                Object.DestroyImmediate(instancia);
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
