using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.IA.APie;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1511: prefabs de cobertura CB01 a CB04 (barrera de concreto, costales, cajas de metal, barricada de policía) y su colocación en Ciudad_Prologo
    /// alrededor de los grupos que pelean. Idempotente: Importar rehace los prefabs y Colocar borra y vuelve a poner el grupo Cobertura_CB.
    /// Uso, en este orden:
    ///   Unity -batchmode -quit -executeMethod MechaCyberpunk.IA.Editor.CoberturaCbT1511.Importar
    ///   Unity -batchmode -quit -executeMethod MechaCyberpunk.IA.Editor.CoberturaCbT1511.Colocar
    /// </summary>
    public static class CoberturaCbT1511
    {
        public const string Carpeta = "Assets/Arte/Zonas/CB/";
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string NombreGrupo = "Cobertura_CB";
        public const string RutaInforme = "Logs/t1511-cobertura.txt";

        /// <summary>Un prop de cobertura: nombre del prefab y altura real (m) a la que se escala.</summary>
        public readonly struct EntradaCb
        {
            public readonly string Nombre;
            public readonly float Altura;

            public EntradaCb(string nombre, float altura)
            {
                Nombre = nombre;
                Altura = altura;
            }
        }

        public static readonly EntradaCb[] Entradas =
        {
            new EntradaCb("CB01_BarreraConcreto_a", 1.0f),
            new EntradaCb("CB01_BarreraConcreto_b", 1.0f),
            new EntradaCb("CB02_Costales_a", 1.0f),
            new EntradaCb("CB02_Costales_b", 1.0f),
            new EntradaCb("CB03_CajasMetal_a", 1.2f),
            new EntradaCb("CB03_CajasMetal_b", 1.2f),
            new EntradaCb("CB04_BarricadaPolicia_a", 1.1f),
        };

        private const float SuavidadMaterial = 0.8f;
        private const float MetalicoMaterial = 1f;
        private const int TamanoTextura = 1024;
        private const float AlturaSobrePiso = 30f;
        private const float MargenLibre = 0.6f;
        private const float AlturaRevision = 0.45f;
        private const float RadioMinimo = 5f;
        private const float RadioMaximo = 9f;
        private const float PasoRadio = 1.5f;
        private const int PasosAngulo = 24;
        private const float NormalPisoMinima = 0.9f;
        private const float DesnivelMaximo = 1f;
        private const float AlturaMaximaAnclaje = 3f;
        private const int CoberturasPorGrupo = 3;
        private const int CoberturasPorRecinto = 4;
        private const float SeparacionEntreCb = 3f;

        public static string RutaPrefab(string nombre)
        {
            return Carpeta + nombre + ".prefab";
        }

        private static string RutaCarpeta(string nombre)
        {
            return Carpeta + nombre + "/";
        }

        private static string RutaArchivo(string nombre, string sufijo)
        {
            return RutaCarpeta(nombre) + nombre + sufijo;
        }

        // ------------------------------------------------------------------ importar

        [MenuItem("Mecha Cyberpunk/T-1511/Importar cobertura CB")]
        public static void Importar()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder("T-1511: prefabs CB (tamano en metros x y z, collider)\n");
            foreach (EntradaCb e in Entradas)
            {
                if (!File.Exists(RutaArchivo(e.Nombre, ".fbx")))
                {
                    informe.AppendLine(e.Nombre + "\tFALTA FBX");
                    continue;
                }
                ConfigurarImportadores(e.Nombre);
                Material material = CrearMaterial(e.Nombre);
                informe.AppendLine(CrearPrefab(e, material));
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[IA] T-1511 importado:\n" + informe);
        }

        private static void ConfigurarImportadores(string nombre)
        {
            foreach (string sufijo in new[] { "_basecolor.jpg", "_normal.jpg", "_metalsuave.png" })
            {
                string ruta = RutaArchivo(nombre, sufijo);
                AssetDatabase.ImportAsset(ruta);
                TextureImporter t = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (t == null)
                {
                    continue;
                }
                bool cambio = t.maxTextureSize != TamanoTextura;
                t.maxTextureSize = TamanoTextura;
                if (sufijo == "_normal.jpg" && t.textureType != TextureImporterType.NormalMap)
                {
                    t.textureType = TextureImporterType.NormalMap;
                    cambio = true;
                }
                if (sufijo == "_metalsuave.png" && (t.sRGBTexture || t.alphaSource != TextureImporterAlphaSource.FromInput))
                {
                    t.sRGBTexture = false;
                    t.alphaSource = TextureImporterAlphaSource.FromInput;
                    cambio = true;
                }
                if (cambio)
                {
                    t.SaveAndReimport();
                }
            }
            ModelImporter imp = AssetImporter.GetAtPath(RutaArchivo(nombre, ".fbx")) as ModelImporter;
            if (imp != null && (imp.materialImportMode != ModelImporterMaterialImportMode.None || imp.importCameras || imp.importLights))
            {
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.importCameras = false;
                imp.importLights = false;
                imp.SaveAndReimport();
            }
        }

        private static Material CrearMaterial(string nombre)
        {
            string ruta = RutaArchivo(nombre, ".mat");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaArchivo(nombre, "_basecolor.jpg"));
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaArchivo(nombre, "_normal.jpg"));
            Texture2D metalSuave = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaArchivo(nombre, "_metalsuave.png"));
            material.SetTexture("_BaseMap", color);
            material.SetTexture("_MainTex", color);
            material.SetColor("_BaseColor", Color.white);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            if (metalSuave != null)
            {
                material.SetTexture("_MetallicGlossMap", metalSuave);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            material.SetFloat("_Smoothness", SuavidadMaterial);
            material.SetFloat("_Metallic", MetalicoMaterial);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Bounds Limites(GameObject objeto)
        {
            Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>(true);
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        private static string CrearPrefab(EntradaCb e, Material material)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaArchivo(e.Nombre, ".fbx"));
            if (fbx == null)
            {
                return e.Nombre + "\tFALTA FBX";
            }
            GameObject raiz = new GameObject(e.Nombre);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visual.name = "Visual";
            visual.transform.SetParent(raiz.transform, false);
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materiales = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < materiales.Length; i++)
                {
                    materiales[i] = material;
                }
                r.sharedMaterials = materiales;
            }
            Bounds original = Limites(raiz);
            float escala = original.size.y > 0.0001f ? e.Altura / original.size.y : 1f;
            visual.transform.localScale = Vector3.one * escala;
            Bounds escalado = Limites(raiz);
            visual.transform.localPosition = new Vector3(-escalado.center.x, -escalado.min.y, -escalado.center.z);
            Bounds final = Limites(raiz);

            BoxCollider caja = raiz.AddComponent<BoxCollider>();
            caja.center = final.center;
            caja.size = final.size;
            CoberturaCB marca = raiz.AddComponent<CoberturaCB>();
            marca.Altura = final.size.y;

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab(e.Nombre));
            Object.DestroyImmediate(raiz);
            return string.Format("{0}\toriginal {1:F2}x{2:F2}x{3:F2}\tfinal {4:F2}x{5:F2}x{6:F2}\tBoxCollider",
                e.Nombre, original.size.x, original.size.y, original.size.z, final.size.x, final.size.y, final.size.z);
        }

        // ------------------------------------------------------------------ colocar

        private sealed class Ancla
        {
            public string Nombre;
            public Vector3 Posicion;
            public int Cantidad;
            public bool Policia;
            public bool Nova;
        }

        [MenuItem("Mecha Cyberpunk/T-1511/Colocar cobertura CB")]
        public static void Colocar()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject viejo = GameObject.Find(NombreGrupo);
            while (viejo != null)
            {
                Object.DestroyImmediate(viejo);
                viejo = GameObject.Find(NombreGrupo);
            }
            Physics.SyncTransforms();

            List<Ancla> anclas = BuscarAnclas();
            GameObject grupo = new GameObject(NombreGrupo);
            List<Bounds> puestos = new List<Bounds>();
            StringBuilder informe = new StringBuilder("T-1511: cobertura colocada en " + RutaEscena + "\n");
            int total = 0;
            int indicePrefab = 0;
            foreach (Ancla a in anclas)
            {
                int colocados = 0;
                for (int k = 0; k < a.Cantidad; k++)
                {
                    GameObject molde = ElegirPrefab(a, indicePrefab++);
                    if (molde == null || !ColocarUno(molde, a, k, grupo.transform, puestos))
                    {
                        continue;
                    }
                    colocados++;
                }
                total += colocados;
                informe.AppendLine(string.Format("{0}\t({1:F1}, {2:F1}, {3:F1})\tpedidos {4}\tcolocados {5}",
                    a.Nombre, a.Posicion.x, a.Posicion.y, a.Posicion.z, a.Cantidad, colocados));
            }
            informe.AppendLine("TOTAL\t" + total);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[IA] T-1511 colocado:\n" + informe);
        }

        private static List<Ancla> BuscarAnclas()
        {
            List<Ancla> lista = new List<Ancla>();
            foreach (PandillaAPie p in Object.FindObjectsByType<PandillaAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.transform.position.y > AlturaMaximaAnclaje)
                {
                    continue; // azoteas: sin piso de calle donde apoyar
                }
                lista.Add(new Ancla
                {
                    Nombre = p.name,
                    Posicion = p.transform.position,
                    Cantidad = CoberturasPorGrupo,
                    Policia = p.Datos != null && p.Datos.EsPolicia,
                    Nova = p.name.Contains("Nova") || p.name.Contains("Escoltas") || p.name.Contains("Refuerzos"),
                });
            }
            foreach (RecintoJefe r in Object.FindObjectsByType<RecintoJefe>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                lista.Add(new Ancla { Nombre = r.name, Posicion = r.transform.position, Cantidad = CoberturasPorRecinto, Nova = true });
            }
            foreach (JefePandilla j in Object.FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (j.transform.position.y <= AlturaMaximaAnclaje)
                {
                    lista.Add(new Ancla { Nombre = j.name, Posicion = j.transform.position, Cantidad = CoberturasPorGrupo });
                }
            }
            lista.Sort((x, y) => string.CompareOrdinal(x.Nombre + x.Posicion, y.Nombre + y.Posicion));
            return lista;
        }

        // Policía: barricada CB04; el resto, una mezcla estable de barrera, costales y cajas.
        private static GameObject ElegirPrefab(Ancla a, int indice)
        {
            string nombre;
            if (a.Policia)
            {
                nombre = "CB04_BarricadaPolicia_a";
            }
            else
            {
                nombre = Entradas[indice % (Entradas.Length - 1)].Nombre; // sin CB04 fuera de la policía
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(nombre));
        }

        private static bool ColocarUno(GameObject molde, Ancla a, int indice, Transform padre, List<Bounds> puestos)
        {
            BoxCollider caja = molde.GetComponent<BoxCollider>();
            Vector3 mitad = caja.size * 0.5f;
            // Cada cobertura parte de un ángulo distinto alrededor del grupo; el primer sitio libre gana.
            float anguloBase = 360f * indice / Mathf.Max(1, a.Cantidad) + 37f;
            for (float radio = RadioMinimo; radio <= RadioMaximo; radio += PasoRadio)
            {
                for (int i = 0; i < PasosAngulo; i++)
                {
                    float angulo = (anguloBase + 360f * i / PasosAngulo) * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Sin(angulo), 0f, Mathf.Cos(angulo));
                    Vector3 xz = a.Posicion + dir * radio;
                    if (!BuscarPiso(xz, a.Posicion.y, out Vector3 piso))
                    {
                        continue;
                    }
                    // La cobertura mira al grupo: su lado largo queda perpendicular a la línea hacia él.
                    Quaternion rot = Quaternion.LookRotation(-dir, Vector3.up);
                    if (!EspacioLibre(piso, rot, caja, mitad, puestos))
                    {
                        continue;
                    }
                    GameObject nueva = (GameObject)PrefabUtility.InstantiatePrefab(molde, padre);
                    nueva.transform.SetPositionAndRotation(piso, rot);
                    Physics.SyncTransforms();
                    puestos.Add(new Bounds(piso + rot * caja.center, caja.size + Vector3.one * SeparacionEntreCb));
                    return true;
                }
            }
            return false;
        }

        private static bool BuscarPiso(Vector3 xz, float yAncla, out Vector3 piso)
        {
            piso = default;
            Vector3 origen = new Vector3(xz.x, yAncla + AlturaSobrePiso, xz.z);
            RaycastHit[] golpes = Physics.RaycastAll(origen, Vector3.down, AlturaSobrePiso + 5f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(golpes, (x, y) => x.distance.CompareTo(y.distance));
            foreach (RaycastHit g in golpes)
            {
                if (g.collider.GetComponentInParent<CoberturaCB>() != null)
                {
                    continue;
                }
                // El primer golpe desde arriba tiene que ser un piso (si es un techo o una pared, no sirve).
                if (g.normal.y < NormalPisoMinima || Mathf.Abs(g.point.y - yAncla) > DesnivelMaximo || EsCalle(g.collider))
                {
                    return false;
                }
                piso = g.point;
                return true;
            }
            return false;
        }

        // No se pone cobertura sobre la calzada: bloquearía el tráfico.
        private static bool EsCalle(Collider c)
        {
            string nombre = c.name;
            return (nombre.StartsWith("Calle") && !nombre.StartsWith("Callejon")) || nombre.StartsWith("Camino") || nombre.StartsWith("Tramo");
        }

        private static bool EspacioLibre(Vector3 piso, Quaternion rot, BoxCollider caja, Vector3 mitad, List<Bounds> puestos)
        {
            Vector3 centro = piso + rot * caja.center;
            foreach (Bounds b in puestos)
            {
                if (b.Contains(centro))
                {
                    return false;
                }
            }
            // Una caja un poco más grande que el prop, levantada del suelo: nada (casas, muros, puertas, postes, carros, otros props) puede tocarla.
            Vector3 mitadLibre = new Vector3(mitad.x + MargenLibre, Mathf.Max(0.05f, mitad.y - AlturaRevision * 0.5f), mitad.z + MargenLibre);
            Vector3 centroLibre = piso + Vector3.up * (AlturaRevision + mitadLibre.y);
            return !Physics.CheckBox(centroLibre, mitadLibre, rot, ~0, QueryTriggerInteraction.Ignore);
        }
    }
}
