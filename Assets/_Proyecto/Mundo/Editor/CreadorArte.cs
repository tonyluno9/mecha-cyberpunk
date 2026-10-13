using System;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Mundo.Datos;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Arte de Blender a Unity (T-076): crea los materiales URP/Lit desde materiales.json, reimporta los FBX y arma un prefab
    /// por pieza en Mundo/Prefabs/Kit/ (raíz vacía con el modelo de hijo y colliders simples). Se puede repetir sin cambiar nada.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorArte.Crear
    /// </summary>
    public static class CreadorArte
    {
        public const string RutaMateriales = "Assets/Arte/Materiales";
        public const string RutaJsonMateriales = "Assets/Arte/Materiales/materiales.json";
        public const string RutaJsonDimensiones = "Assets/Arte/Mundo/dimensiones.json";
        public const string RutaPrefabs = "Assets/_Proyecto/Mundo/Prefabs/Kit";
        public const string RutaConfig = "Assets/_Proyecto/Mundo/Datos/ConfigArte_Mundo.asset";

        /// <summary>Pieza del kit: nombre en Blender (JSON y FBX), carpeta del FBX, nombre del prefab y si choca.</summary>
        public readonly struct Pieza
        {
            public readonly string Fuente;
            public readonly string Carpeta;
            public readonly string Prefab;
            public readonly bool Collider;

            public Pieza(string fuente, string carpeta, string prefab, bool collider)
            {
                Fuente = fuente;
                Carpeta = carpeta;
                Prefab = prefab;
                Collider = collider;
            }

            public string RutaFbx => "Assets/Arte/Mundo/" + Carpeta + "/" + Fuente + ".fbx";
            public string RutaPrefab => RutaPrefabs + "/" + Prefab + ".prefab";
        }

        public static readonly Pieza[] Piezas =
        {
            new Pieza("poste_luz", "KitUrbano", "Poste_Luz", true),
            new Pieza("puesto_tacos", "KitUrbano", "Puesto_Tacos", true),
            new Pieza("letrero_tacos", "KitUrbano", "Letrero_Tacos", false),
            new Pieza("letrero_farmacia", "KitUrbano", "Letrero_Farmacia", false),
            new Pieza("letrero_cantina", "KitUrbano", "Letrero_Cantina", false),
            new Pieza("azotea_ac_tinaco", "KitUrbano", "Azotea_AC_Tinaco", false),
            new Pieza("transformador_poste", "KitUrbano", "Transformador_Poste", true),
            new Pieza("basurero_grafiti", "KitUrbano", "Basurero_Grafiti", true),
            new Pieza("fachada_modular", "KitUrbano", "Fachada_Modular", false),
            new Pieza("prop_barra", "Bar", "Prop_Barra", true),
            new Pieza("prop_mesa", "Bar", "Prop_Mesa", true),
            new Pieza("prop_dj", "Bar", "Prop_DJ", true),
            new Pieza("prop_letrero", "Bar", "Prop_Letrero", false),
        };

        [Serializable]
        private class DatosMaterial
        {
            public string nombre;
            public float[] color_base;
            public float alfa;
            public float rugosidad;
            public float metalico;
            public float[] emision_color;
            public float emision_fuerza;
        }

        [Serializable]
        private class ListaMateriales
        {
            public DatosMaterial[] materiales;
        }

        [MenuItem("Mecha Cyberpunk/Crear arte (T-076)")]
        public static void Crear()
        {
            ConfigArte config = CargarOCrearConfig();
            CrearMateriales(config);
            AssetDatabase.SaveAssets();

            // Los FBX ya están en disco; se reimportan para que el postprocesador remapee los materiales recién creados.
            foreach (Pieza pieza in Piezas)
            {
                AssetDatabase.ImportAsset(pieza.RutaFbx, ImportAssetOptions.ForceUpdate);
            }

            AsegurarCarpeta(RutaPrefabs);
            foreach (Pieza pieza in Piezas)
            {
                CrearPrefab(pieza);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mundo] Arte creado: " + Piezas.Length + " prefabs en " + RutaPrefabs);
        }

        // ------------------------------------------------------------------ materiales

        private static ConfigArte CargarOCrearConfig()
        {
            ConfigArte config = AssetDatabase.LoadAssetAtPath<ConfigArte>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigArte>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            return config;
        }

        private static void CrearMateriales(ConfigArte config)
        {
            ListaMateriales lista = JsonUtility.FromJson<ListaMateriales>(File.ReadAllText(RutaJsonMateriales));
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (DatosMaterial datos in lista.materiales)
            {
                string ruta = RutaMateriales + "/" + datos.nombre + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (material == null)
                {
                    material = new Material(lit);
                    AssetDatabase.CreateAsset(material, ruta);
                }
                material.shader = lit;
                Configurar(material, datos, config);
                EditorUtility.SetDirty(material);
            }
        }

        private static void Configurar(Material material, DatosMaterial datos, ConfigArte config)
        {
            // Blender da los colores en lineal; la propiedad de color de Unity se guarda en gamma.
            Color baseLineal = new Color(datos.color_base[0], datos.color_base[1], datos.color_base[2], 1f);
            Color baseColor = baseLineal.gamma;
            bool vidrio = datos.nombre.StartsWith("Vidrio", StringComparison.Ordinal);
            baseColor.a = vidrio ? (datos.alfa < 1f ? datos.alfa : config.OpacidadVidrio) : 1f;

            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", 1f - datos.rugosidad);
            material.SetFloat("_Metallic", datos.metalico);

            bool emite = datos.emision_fuerza > 0f;
            Color emision = emite
                ? new Color(datos.emision_color[0], datos.emision_color[1], datos.emision_color[2], 1f) * (datos.emision_fuerza * config.FactorEmision)
                : Color.black;
            material.SetColor("_EmissionColor", emision);
            if (emite)
            {
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }
            material.globalIlluminationFlags = emite ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.None;

            if (vidrio)
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetShaderPassEnabled("ShadowCaster", false);
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.SetOverrideTag("RenderType", "Opaque");
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.One);
                material.SetFloat("_DstBlend", (float)BlendMode.Zero);
                material.SetFloat("_ZWrite", 1f);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetShaderPassEnabled("ShadowCaster", true);
                material.renderQueue = -1;
            }
        }

        // ------------------------------------------------------------------ prefabs

        private static void CrearPrefab(Pieza pieza)
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(pieza.RutaFbx);
            if (modelo == null)
            {
                throw new InvalidOperationException("Falta el FBX " + pieza.RutaFbx + ": corre Assets/Arte/Fuentes~/exportar_fbx.py");
            }

            GameObject raiz = new GameObject(pieza.Prefab);
            GameObject hijo = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz.transform);
            hijo.name = "Modelo";
            hijo.transform.localPosition = Vector3.zero;
            hijo.transform.localRotation = Quaternion.identity;
            hijo.transform.localScale = Vector3.one;

            // Base en y = 0.
            Bounds limites = LimitesRenderers(raiz);
            hijo.transform.localPosition = new Vector3(0f, -limites.min.y, 0f);

            if (pieza.Collider)
            {
                AgregarCollider(raiz, hijo, pieza);
            }

            PrefabUtility.SaveAsPrefabAsset(raiz, pieza.RutaPrefab);
            UnityEngine.Object.DestroyImmediate(raiz);
        }

        private static void AgregarCollider(GameObject raiz, GameObject hijo, Pieza pieza)
        {
            BoxCollider caja = raiz.AddComponent<BoxCollider>();
            Bounds todo = LimitesRenderers(raiz);
            if (pieza.Prefab == "Poste_Luz")
            {
                // Solo el pie del poste: los vértices de abajo, con la altura del cuerpo del poste (máx. 3 m).
                Bounds pie = LimitesVertices(hijo, v => v.y < 0.6f);
                Vector3 tamano = new Vector3(Mathf.Max(pie.size.x, 0.3f), Mathf.Min(todo.size.y, 3f), Mathf.Max(pie.size.z, 0.3f));
                caja.center = new Vector3(pie.center.x, tamano.y * 0.5f, pie.center.z);
                caja.size = tamano;
            }
            else
            {
                caja.center = todo.center;
                caja.size = todo.size;
            }
        }

        private static Bounds LimitesRenderers(GameObject objeto)
        {
            Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>();
            Bounds limites = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                limites.Encapsulate(renderers[i].bounds);
            }
            return limites;
        }

        private static Bounds LimitesVertices(GameObject objeto, Func<Vector3, bool> filtro)
        {
            bool primero = true;
            Bounds limites = new Bounds();
            foreach (MeshFilter filtroMalla in objeto.GetComponentsInChildren<MeshFilter>())
            {
                foreach (Vector3 local in filtroMalla.sharedMesh.vertices)
                {
                    Vector3 v = filtroMalla.transform.TransformPoint(local);
                    if (!filtro(v))
                    {
                        continue;
                    }
                    if (primero)
                    {
                        limites = new Bounds(v, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        limites.Encapsulate(v);
                    }
                }
            }
            return limites;
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta))
            {
                return;
            }
            string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
            AsegurarCarpeta(padre);
            AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
        }

        // ------------------------------------------------------------------ vitrina

        /// <summary>Escena nueva SIN guardar: las 13 piezas en fila, de noche, con una cámara de frente.</summary>
        [MenuItem("Mecha Cyberpunk/Vitrina de arte (T-076)")]
        public static void Vitrina()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            float x = 0f;
            const float separacion = 1.5f;
            foreach (Pieza pieza in Piezas)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pieza.RutaPrefab);
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                float ancho = Mathf.Max(LimitesRenderers(instancia).size.x, 1f);
                instancia.transform.position = new Vector3(x + ancho * 0.5f, 0f, 0f);
                x += ancho + separacion;
            }

            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "Suelo";
            suelo.transform.position = new Vector3(x * 0.5f, 0f, 0f);
            suelo.transform.localScale = new Vector3(x * 0.2f + 2f, 1f, 3f);
            Material asfalto = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Proyecto/Mundo/Materiales/Barrio_Asfalto.mat");
            if (asfalto != null)
            {
                suelo.GetComponent<MeshRenderer>().sharedMaterial = asfalto;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.08f, 0.14f);
            RenderSettings.skybox = null;

            GameObject luz = new GameObject("Luna");
            Light luna = luz.AddComponent<Light>();
            luna.type = LightType.Directional;
            luna.color = new Color(0.45f, 0.55f, 0.9f);
            luna.intensity = 0.35f;
            luz.transform.rotation = Quaternion.Euler(40f, 150f, 0f);

            GameObject camaraObjeto = new GameObject("Camara");
            camaraObjeto.tag = "MainCamera";
            Camera camara = camaraObjeto.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.02f, 0.025f, 0.05f);
            camara.fieldOfView = 40f;
            camara.nearClipPlane = 0.1f;
            camara.farClipPlane = 200f;
            // Frente hacia +Z: la cámara va delante, en +Z, mirando hacia -Z.
            camara.transform.position = new Vector3(x * 0.5f, 3.2f, x * 0.62f);
            camara.transform.rotation = Quaternion.Euler(8f, 180f, 0f);
        }

        /// <summary>Batchmode: arma la vitrina y guarda docs/capturas/T-076-1.png (todas) y T-076-2.png (poste, puesto y fachada).</summary>
        public static void CapturarVitrina()
        {
            Vitrina();
            Camera camara = Camera.main;
            string carpeta = Path.GetFullPath("docs/capturas");
            Directory.CreateDirectory(carpeta);
            Guardar(camara, Path.Combine(carpeta, "T-076-1.png"));

            // De cerca: poste, puesto y fachada lado a lado.
            string[] nombres = { "Poste_Luz", "Puesto_Tacos", "Fachada_Modular" };
            float x = 0f;
            foreach (string nombre in nombres)
            {
                GameObject objeto = GameObject.Find(nombre);
                float ancho = Mathf.Max(LimitesRenderers(objeto).size.x, 1f);
                objeto.transform.position = new Vector3(x + ancho * 0.5f + 100f, 0f, 0f);
                x += ancho + 1.5f;
            }
            camara.transform.position = new Vector3(100f + x * 0.5f, 2.8f, 17f);
            camara.transform.rotation = Quaternion.Euler(6f, 180f, 0f);
            Guardar(camara, Path.Combine(carpeta, "T-076-2.png"));
        }

        private static void Guardar(Camera camara, string ruta)
        {
            RenderTexture destino = new RenderTexture(1600, 800, 24, RenderTextureFormat.ARGB32);
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
    }
}
