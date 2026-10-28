using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-250: los modelos FBX del lote de assets, cada uno con su prefab (raíz, hijo Visual, pivote en la base, BoxCollider),
    /// su material URP Lit y la escena Escenas/Tony/Pruebas_Assets.unity con todos en filas por zona.
    /// </summary>
    public static class ArteT250
    {
        public const string CarpetaArte = "Assets/Arte/";
        public const string RutaEscenaPruebas = "Assets/_Proyecto/Escenas/Tony/Pruebas_Assets.unity";
        public const string CarpetaCapturas = "docs/capturas";

        private const float Suavidad = 0.2f;
        private const float Metalico = 0f;
        private const float SeparacionFila = 2f;
        private const float DistanciaEntreFilas = 30f;

        public static readonly string[] Zonas = { "Calle", "Clandestino", "Callejon", "TallerCasa", "Vehiculos", "Exteriores", "Exos", "Personajes", "SinClasificar" };

        public enum Eje
        {
            Alto,
            Largo
        }

        /// <summary>Un modelo del lote: código, zona y la medida real a la que se escala.</summary>
        public sealed class Modelo
        {
            public readonly string Codigo;
            public readonly string Zona;
            public readonly Eje EjeMedida;
            public readonly float Medida;

            public Modelo(string codigo, Eje eje, float medida)
            {
                Codigo = codigo;
                Zona = ZonaDe(codigo);
                EjeMedida = eje;
                Medida = medida;
            }

            public string Carpeta => CarpetaArte + Zona + "/" + Codigo;
            public string RutaFbx => Carpeta + "/" + Codigo + ".fbx";
            public string RutaMaterial => Carpeta + "/" + Codigo + ".mat";
            public string RutaPrefab => CarpetaArte + Zona + "/Prefabs/" + Codigo + ".prefab";
        }

        /// <summary>Medidas creíbles (m): puerta 2.1, persona 1.8, carro 4.2, poste 7.</summary>
        public static readonly Modelo[] Modelos =
        {
            new Modelo("A04_Gradas", Eje.Alto, 3f),
            new Modelo("A05_LamparaColgante", Eje.Alto, 1.2f),
            new Modelo("A06_ParedSotano", Eje.Alto, 4f),
            new Modelo("A07_ColumnaSotano", Eje.Alto, 4f),
            new Modelo("C01_FachadaLocal", Eje.Alto, 4f),
            new Modelo("C02_FachadaVivienda", Eje.Alto, 3.5f),
            new Modelo("C03_FachadaPisoAlto", Eje.Alto, 3.2f),
            new Modelo("C04_EsquinaPilar", Eje.Alto, 3.5f),
            new Modelo("C05_PretilAzotea", Eje.Alto, 1f),
            new Modelo("C07_PosteLuz", Eje.Alto, 7f),
            new Modelo("C08_Transformador", Eje.Alto, 1.4f),
            new Modelo("C09_Tinaco", Eje.Alto, 1.6f),
            new Modelo("C10_AireAcondicionado", Eje.Alto, 0.6f),
            new Modelo("C11_Basurero", Eje.Alto, 1.1f),
            new Modelo("C12_PuestoTacos", Eje.Alto, 2.4f),
            new Modelo("C14_RegistroMedidores", Eje.Alto, 1.2f),
            new Modelo("C15a_LetreroHorizontal", Eje.Largo, 3f),
            new Modelo("C15b_LetreroBandera", Eje.Alto, 1.2f),
            new Modelo("H01a_ParedPuerta", Eje.Alto, 2.8f),
            new Modelo("H01b_ParedVentana", Eje.Alto, 2.8f),
            new Modelo("H01c_ParedLisa", Eje.Alto, 2.8f),
            new Modelo("H02_Cama", Eje.Largo, 2f),
            new Modelo("H03_Espejo", Eje.Alto, 1.7f),
            new Modelo("H04_MesaSillas", Eje.Alto, 0.95f),
            new Modelo("H05_Refrigerador", Eje.Alto, 1.8f),
            new Modelo("H10_Maletas", Eje.Alto, 0.7f),
            new Modelo("L01_Contenedor", Eje.Largo, 2f),
            new Modelo("L02_PuertaTrasera", Eje.Alto, 2.4f),
            new Modelo("L03_EscaleraEmergencia", Eje.Alto, 3.5f),
            new Modelo("L04_Llantas", Eje.Alto, 0.8f),
            new Modelo("L05_RejaPorton", Eje.Alto, 3f),
            new Modelo("T01_ParedTaller", Eje.Alto, 4f),
            new Modelo("T02_EstacionExo", Eje.Alto, 5f),
            new Modelo("T03_BancoTrabajo", Eje.Alto, 0.95f),
            new Modelo("T04_GabineteHerramientas", Eje.Alto, 1.2f),
            new Modelo("T05_Generador", Eje.Alto, 1f),
            new Modelo("V01a_CarroViejito", Eje.Largo, 4.2f),
            new Modelo("V01b_CarroPro", Eje.Largo, 4.5f),
        };

        public static string ZonaDe(string codigo)
        {
            switch (codigo[0])
            {
                case 'C': return "Calle";
                case 'A': return "Clandestino";
                case 'L': return "Callejon";
                case 'T':
                case 'H': return "TallerCasa";
                case 'V': return "Vehiculos";
                case 'E': return "Exteriores";
                case 'X': return "Exos";
                case 'P': return "Personajes";
                default: return "SinClasificar";
            }
        }

        public static bool EsRutaDeZona(string ruta)
        {
            if (!ruta.StartsWith(CarpetaArte))
            {
                return false;
            }
            string resto = ruta.Substring(CarpetaArte.Length);
            return Zonas.Any(z => resto.StartsWith(z + "/"));
        }

        public static Modelo Buscar(string codigo)
        {
            return Modelos.First(m => m.Codigo == codigo);
        }

        // ------------------------------------------------------------------ prefabs

        /// <summary>Batchmode: reimporta, crea materiales y prefabs, la escena de pruebas y su captura.</summary>
        [MenuItem("Mecha Cyberpunk/T-250/Crear prefabs y Pruebas_Assets")]
        public static void CrearTodo()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (Modelo modelo in Modelos)
            {
                informe.AppendLine(CrearPrefab(modelo));
            }
            File.WriteAllText("Logs/t250-prefabs.txt", informe.ToString());
            AssetDatabase.SaveAssets();
            CrearEscenaPruebas();
            CapturarPruebas();
            Debug.Log("[Arte] T-250 listo:\n" + informe);
        }

        private static string CrearPrefab(Modelo modelo)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(modelo.RutaFbx);
            if (fbx == null)
            {
                return modelo.Codigo + "\tFALTA FBX " + modelo.RutaFbx;
            }

            Material material = CrearMaterial(modelo);
            GameObject raiz = new GameObject(modelo.Codigo);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            visual.name = "Visual";
            visual.transform.SetParent(raiz.transform, false);
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
            }

            Bounds original = Limites(raiz);
            float actual = modelo.EjeMedida == Eje.Alto ? original.size.y : Mathf.Max(original.size.x, original.size.z);
            float escala = actual > 0.0001f ? modelo.Medida / actual : 1f;
            visual.transform.localScale = Vector3.one * escala;
            Bounds escalado = Limites(raiz);
            visual.transform.localPosition = new Vector3(-escalado.center.x, -escalado.min.y, -escalado.center.z);
            Bounds final = Limites(raiz);

            BoxCollider caja = raiz.AddComponent<BoxCollider>();
            caja.center = final.center;
            caja.size = final.size;

            Directory.CreateDirectory(Path.GetDirectoryName(modelo.RutaPrefab));
            PrefabUtility.SaveAsPrefabAsset(raiz, modelo.RutaPrefab);
            UnityEngine.Object.DestroyImmediate(raiz);
            int triangulos = fbx.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Sum(f => f.sharedMesh.triangles.Length / 3);
            return string.Format("{0}\toriginal {1:F2}x{2:F2}x{3:F2}\tescala {4:F3}\tfinal {5:F2}x{6:F2}x{7:F2}\ttriangulos {8}",
                modelo.Codigo, original.size.x, original.size.y, original.size.z, escala, final.size.x, final.size.y, final.size.z, triangulos);
        }

        private static Material CrearMaterial(Modelo modelo)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(modelo.RutaMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, modelo.RutaMaterial);
            }
            Texture2D color = BuscarTextura(modelo, "_basecolor");
            Texture2D normal = BuscarTextura(modelo, "_normal");
            material.SetTexture("_BaseMap", color);
            material.SetTexture("_MainTex", color);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", Suavidad);
            material.SetFloat("_Metallic", Metalico);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D BuscarTextura(Modelo modelo, string sufijo)
        {
            foreach (string extension in new[] { ".jpg", ".png" })
            {
                Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(modelo.Carpeta + "/" + modelo.Codigo + sufijo + extension);
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }

        public static Bounds Limites(GameObject objeto)
        {
            Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(objeto.transform.position, Vector3.zero);
            }
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        // ------------------------------------------------------------------ escena de pruebas

        public static void CrearEscenaPruebas()
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            float z = 0f;
            float anchoTotal = 0f;
            foreach (string zona in Zonas)
            {
                Modelo[] fila = Modelos.Where(m => m.Zona == zona).ToArray();
                if (fila.Length == 0)
                {
                    continue;
                }
                GameObject grupo = new GameObject("Fila_" + zona);
                grupo.transform.position = new Vector3(0f, 0f, z);
                float x = 0f;
                float fondo = 0f;
                foreach (Modelo modelo in fila)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelo.RutaPrefab);
                    if (prefab == null)
                    {
                        continue;
                    }
                    GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                    instancia.transform.SetParent(grupo.transform, false);
                    Bounds b = Limites(instancia);
                    instancia.transform.localPosition = new Vector3(x + b.size.x * 0.5f, 0f, 0f);
                    x += b.size.x + SeparacionFila;
                    fondo = Mathf.Max(fondo, b.size.z);
                }
                anchoTotal = Mathf.Max(anchoTotal, x);
                z -= Mathf.Max(DistanciaEntreFilas, fondo + SeparacionFila * 3f);
            }

            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "Suelo";
            suelo.transform.position = new Vector3(anchoTotal * 0.5f, 0f, z * 0.5f);
            suelo.transform.localScale = new Vector3(anchoTotal * 0.1f + 4f, 1f, -z * 0.1f + 4f);

            PonerLuz();
            GameObject camaraObjeto = new GameObject("Camara");
            camaraObjeto.tag = "MainCamera";
            Camera camara = camaraObjeto.AddComponent<Camera>();
            camara.transform.position = new Vector3(anchoTotal * 0.5f, 25f, z - 25f);
            camara.transform.LookAt(new Vector3(anchoTotal * 0.5f, 0f, z * 0.5f));
            Directory.CreateDirectory(Path.GetDirectoryName(RutaEscenaPruebas));
            EditorSceneManager.SaveScene(escena, RutaEscenaPruebas);
        }

        public static void PonerLuz()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);
            GameObject luz = new GameObject("Sol");
            Light sol = luz.AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.intensity = 1.2f;
            sol.shadows = LightShadows.Soft;
            luz.transform.rotation = Quaternion.Euler(45f, 200f, 0f);
        }

        /// <summary>Captura de cada fila de Pruebas_Assets en docs/capturas/T-250-fila-&lt;zona&gt;.png, vista desde el frente (-Z).</summary>
        public static void CapturarPruebas()
        {
            EditorSceneManager.OpenScene(RutaEscenaPruebas, OpenSceneMode.Single);
            Camera camara = Camera.main;
            foreach (string zona in Zonas)
            {
                GameObject fila = GameObject.Find("Fila_" + zona);
                if (fila == null)
                {
                    continue;
                }
                Encuadrar(camara, Limites(fila), new Vector3(0.15f, 0.25f, -1f));
                Guardar(camara, Path.Combine(CarpetaCapturas, "T-250-fila-" + zona + ".png"));
                Encuadrar(camara, Limites(fila), new Vector3(-0.2f, 0.3f, 1f));
                Guardar(camara, Path.Combine(CarpetaCapturas, "T-250-fila-" + zona + "-atras.png"));
            }
        }

        public static void Encuadrar(Camera camara, Bounds b, Vector3 direccion)
        {
            camara.fieldOfView = 40f;
            camara.nearClipPlane = 0.1f;
            camara.farClipPlane = 1000f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
            float radio = Mathf.Max(b.extents.magnitude, 1f);
            float distancia = radio / Mathf.Sin(camara.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.75f;
            camara.transform.position = b.center + direccion.normalized * distancia;
            camara.transform.LookAt(b.center);
        }

        public static void Guardar(Camera camara, string ruta)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            RenderTexture destino = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture.active = destino;
            Texture2D imagen = new Texture2D(destino.width, destino.height, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0, 0, destino.width, destino.height), 0, 0);
            imagen.Apply();
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            UnityEngine.Object.DestroyImmediate(imagen);
            destino.Release();
            UnityEngine.Object.DestroyImmediate(destino);
        }

        // ------------------------------------------------------------------ volcado

        /// <summary>Batchmode: vuelca la jerarquía de los prefabs y escenas de destino a Temp/t250-volcado.txt.</summary>
        public static void Volcar()
        {
            StringBuilder s = new StringBuilder();
            foreach (string ruta in ColocarT250.PrefabsDestino)
            {
                GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
                s.AppendLine("##### " + ruta);
                VolcarObjeto(raiz.transform, "", s);
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            foreach (string ruta in ColocarT250.EscenasDestino)
            {
                Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                s.AppendLine("##### " + ruta);
                foreach (GameObject g in escena.GetRootGameObjects())
                {
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(g))
                    {
                        s.AppendLine(g.name + " [instancia " + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) + "] pos " + g.transform.position);
                        continue;
                    }
                    VolcarObjeto(g.transform, "", s);
                }
            }
            File.WriteAllText("Logs/t250-volcado.txt", s.ToString());
        }

        private static void VolcarObjeto(Transform t, string ruta, StringBuilder s)
        {
            string aqui = ruta.Length == 0 ? t.name : ruta + "/" + t.name;
            if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && ruta.Length > 0)
            {
                s.AppendLine(aqui + " [instancia " + Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)) + "]" + Datos(t));
                return;
            }
            s.AppendLine(aqui + Datos(t));
            foreach (Transform hijo in t)
            {
                VolcarObjeto(hijo, aqui, s);
            }
        }

        private static string Datos(Transform t)
        {
            List<string> comps = t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name).ToList();
            Bounds b = Limites(t.gameObject);
            return string.Format(" |{0}| pos {1:F1},{2:F1},{3:F1} esc {4:F2},{5:F2},{6:F2} rotY {7:F0} medida {8:F2}x{9:F2}x{10:F2} {11}",
                t.gameObject.activeSelf ? "" : " INACTIVO", t.position.x, t.position.y, t.position.z, t.lossyScale.x, t.lossyScale.y, t.lossyScale.z,
                t.eulerAngles.y, b.size.x, b.size.y, b.size.z, string.Join(",", comps));
        }
    }
}
