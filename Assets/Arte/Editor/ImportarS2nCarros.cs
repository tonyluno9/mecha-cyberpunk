using System.IO;
using System.IO.Compression;
using System.Linq;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Editor;
using MechaCyberpunk.Vehiculos.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-810: los carros de Tripo de la sesión 2 (V09 taxi, V10 pickup, V11 van) que llegan como .zip a imageassets/modelos3d. Las filas
    /// <see cref="ImportarS2n.Accion.CarroTripo"/> de <see cref="ImportarS2n.Entradas"/> pasan por aquí: si llegó el zip se desempaca en
    /// Assets/Arte/Vehiculos/&lt;código&gt;/ con los nombres de V02-V08 (&lt;código&gt;.fbx, _basecolor.jpg, _normal.jpg), se arma el prefab del arte
    /// (flujo de T-250, como ImportarT511: frente a +Z, a su largo real, pies en y = 0), el carro manejable con
    /// <see cref="CreadorCarrosT511.CrearUno"/> (ruedas de greybox invisibles, como los otros carros de Tripo), su prefab de tráfico
    /// CarroTrafico_&lt;código&gt; (como <see cref="CreadorTraficoT511"/>) y se agrega a TraficoCalle.prefabsCarro de Ciudad_Prologo.
    /// Sin zip no hace nada (solo anota "falta"). Correrlo dos veces no cambia nada.
    /// </summary>
    public static class ImportarS2nCarros
    {
        /// <summary>Código, largo real (m, como el carro parecido: taxi = sedán V02, pickup = V03, van = combi V04) y giro del visual (°) que lleva
        /// el frente a +Z (los Tripo de V05 y V06 venían con el frente a -Z). Si al llegar un modelo sale de lado o de reversa, cambia aquí su giro.</summary>
        public static readonly (string codigo, float largo, float giro)[] Medidas =
        {
            ("V09", 4.5f, 180f),
            ("V10", 5f, 180f),
            ("V11", 4.7f, 180f),
        };

        public const string CarpetaVehiculos = "Assets/Arte/Vehiculos/";
        // Lo mismo que CreadorTraficoT511 (privado allá): la raíz del carro real va 0.3 m sobre la calle.
        private const float AlturaRaizCarroReal = 0.3f;

        public static string Codigo(string rutaFbx) => Path.GetFileNameWithoutExtension(rutaFbx);
        public static string CarpetaDe(string codigo) => CarpetaVehiculos + codigo + "/";

        /// <summary>Desempaca el zip si llegó y no estaba ya. Devuelve true si el modelo está (nuevo o de antes).</summary>
        public static bool Meter(ImportarS2n.Entrada e)
        {
            string codigo = Codigo(e.Destino);
            if (e.Origen == null || !File.Exists(e.Origen))
            {
                ImportarS2n.Informe.AppendLine("falta " + Path.GetFileName(e.Origen) + " (" + Path.GetDirectoryName(e.Origen)?.Replace('\\', '/')
                    + "): el carro " + codigo + " entra cuando llegue" + (File.Exists(e.Destino) ? "; se queda el que ya está" : ""));
                return File.Exists(e.Destino);
            }
            if (File.Exists(e.Destino))
            {
                ImportarS2n.Informe.AppendLine("igual: " + e.Destino);
                return true;
            }
            string carpeta = CarpetaDe(codigo);
            Directory.CreateDirectory(carpeta);
            using (ZipArchive zip = ZipFile.OpenRead(e.Origen))
            {
                ZipArchiveEntry fbx = zip.Entries.FirstOrDefault(z => z.FullName.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase));
                if (fbx == null)
                {
                    ImportarS2n.Informe.AppendLine("ERROR: " + e.Origen + " no trae .fbx");
                    return false;
                }
                fbx.ExtractToFile(e.Destino, true);
                ZipArchiveEntry[] imagenes = zip.Entries.Where(EsImagen).ToArray();
                ZipArchiveEntry color = imagenes.FirstOrDefault(z => Tiene(z, "basecolor") || Tiene(z, "_0_0."))
                    ?? imagenes.Where(z => !Tiene(z, "normal") && !Tiene(z, "metallic") && !Tiene(z, "roughness")).OrderByDescending(z => z.Length).FirstOrDefault();
                ZipArchiveEntry normal = imagenes.FirstOrDefault(z => Tiene(z, "normal") || Tiene(z, "_0_3."));
                color?.ExtractToFile(carpeta + codigo + "_basecolor.jpg", true);
                if (normal != null && normal != color)
                {
                    normal.ExtractToFile(carpeta + codigo + "_normal.jpg", true);
                }
                ImportarS2n.Informe.AppendLine("desempacado: " + e.Origen + " -> " + carpeta + " (color " + (color?.Name ?? "no") + ", normal " + (normal?.Name ?? "no") + ")");
            }
            return true;
        }

        private static bool EsImagen(ZipArchiveEntry z)
        {
            string n = z.Name.ToLowerInvariant();
            return n.EndsWith(".jpg") || n.EndsWith(".jpeg") || n.EndsWith(".png");
        }

        private static bool Tiene(ZipArchiveEntry z, string texto) => z.Name.ToLowerInvariant().Contains(texto);

        /// <summary>Prefabs del arte, del carro y del tráfico para los modelos que ya están, y los agrega al tráfico de la ciudad.</summary>
        public static void Armar(ImportarS2n.Entrada[] carros)
        {
            var listos = carros.Where(e => File.Exists(e.Destino)).Select(e => Codigo(e.Destino)).ToArray();
            if (listos.Length == 0)
            {
                ImportarS2n.Informe.AppendLine("carros nuevos: ninguno (no llegó ningún zip); la escena no se toca");
                return;
            }
            foreach (string codigo in listos)
            {
                var medida = Medidas.First(m => m.codigo == codigo);
                ArteT250.Modelo modelo = new ArteT250.Modelo(codigo, ArteT250.Eje.Largo, medida.largo);
                ModelImporter importador = AssetImporter.GetAtPath(modelo.RutaFbx) as ModelImporter;
                if (importador != null && importador.materialImportMode != ModelImporterMaterialImportMode.None)
                {
                    importador.materialImportMode = ModelImporterMaterialImportMode.None;
                    importador.SaveAndReimport();
                }
                TextureImporter normal = AssetImporter.GetAtPath(modelo.Carpeta + "/" + codigo + "_normal.jpg") as TextureImporter;
                if (normal != null && normal.textureType != TextureImporterType.NormalMap)
                {
                    normal.textureType = TextureImporterType.NormalMap;
                    normal.SaveAndReimport();
                }
                ImportarS2n.Informe.AppendLine(ArteT250.CrearPrefab(modelo));
                Orientar(modelo, medida.giro);
                ImportarS2n.Informe.AppendLine(CreadorCarrosT511.CrearUno(codigo, codigo, 0f, 0f));
                ImportarS2n.Informe.AppendLine(CrearTrafico(codigo));
            }
            AssetDatabase.SaveAssets();
            AgregarAlTrafico(listos.Select(CreadorTraficoT511.RutaPrefab).ToArray());
        }

        // Como ImportarT511.Orientar: gira el Visual, lo escala a su largo real, lo centra con los pies en y = 0 y ajusta el BoxCollider.
        private static void Orientar(ArteT250.Modelo modelo, float giro)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(modelo.RutaPrefab);
            Transform visual = raiz.transform.Find("Visual");
            visual.localScale = Vector3.one;
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.Euler(0f, giro, 0f);
            Bounds b = ImportarT511.LimitesExactos(raiz);
            visual.localScale = Vector3.one * (modelo.Medida / b.size.z);
            b = ImportarT511.LimitesExactos(raiz);
            visual.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            b = ImportarT511.LimitesExactos(raiz);
            BoxCollider caja = raiz.GetComponent<BoxCollider>();
            caja.center = b.center;
            caja.size = b.size;
            PrefabUtility.SaveAsPrefabAsset(raiz, modelo.RutaPrefab);
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        // Lo mismo que hace CreadorTraficoT511.Crear con cada código de su lista (que no se toca: es de Tráfico).
        private static string CrearTrafico(string codigo)
        {
            GameObject origen = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorCarrosT511.RutaPrefab(codigo));
            if (origen == null)
            {
                return codigo + "\tFALTA " + CreadorCarrosT511.RutaPrefab(codigo);
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

        /// <summary>Agrega esos prefabs de tráfico a TraficoCalle.prefabsCarro de Ciudad_Prologo si no están (guarda la escena solo si cambió).</summary>
        public static int AgregarAlTrafico(string[] rutas)
        {
            ColocarS2.Abrir();
            TraficoCalle trafico = Object.FindFirstObjectByType<TraficoCalle>(FindObjectsInactive.Include);
            if (trafico == null)
            {
                ImportarS2n.Informe.AppendLine("FALTA TraficoCalle en " + ColocarS2.RutaEscena);
                return 0;
            }
            SerializedObject so = new SerializedObject(trafico);
            SerializedProperty lista = so.FindProperty("prefabsCarro");
            int agregados = 0;
            foreach (string ruta in rutas)
            {
                CarroTrafico prefab = AssetDatabase.LoadAssetAtPath<CarroTrafico>(ruta);
                if (prefab == null)
                {
                    continue;
                }
                bool esta = Enumerable.Range(0, lista.arraySize).Any(i => lista.GetArrayElementAtIndex(i).objectReferenceValue == prefab);
                if (!esta)
                {
                    lista.InsertArrayElementAtIndex(lista.arraySize);
                    lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = prefab;
                    agregados++;
                }
            }
            if (agregados > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(trafico.gameObject.scene);
                EditorSceneManager.SaveScene(trafico.gameObject.scene);
            }
            ImportarS2n.Informe.AppendLine("tráfico de la ciudad: " + agregados + " modelos agregados (hay " + lista.arraySize + ")");
            return agregados;
        }
    }
}
