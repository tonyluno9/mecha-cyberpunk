using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Personaje.Combate;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-736 (A6): la pistola del jugador con el modelo W01_Pistola_agarre.fbx de Tony (pivote en la empuñadura) para que el agarre del caso A de agarre.txt (R_Hand de PJ01)
    /// deje la palma en la empuñadura, el cañón al frente y la corredera arriba. El FBX se importa con el cañón hacia -Z y la corredera hacia +Y: el hijo Modelo lo gira
    /// 180° sobre Y (cañón a +Z, corredera a +Y) y el nodo Pistola lleva el agarre de Tony compuesto con el giro del FBX de Tony (ver AgarresArmas.GiroPJ01). Crea el prefab W01_Pistola_agarre con la animación de la corredera y el
    /// cargador (AnimacionPistola con los ejes girados) y lo pone en lugar del W01 de la pistola de JugadorAPie, con la boca (Boca) en la punta del cañón. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.ColocarAgarrePistolaT736.Colocar
    /// </summary>
    public static class ColocarAgarrePistolaT736
    {
        public const string Carpeta = "Assets/Arte/Armas/W01_Pistola/";
        public const string RutaFbx = Carpeta + "W01_Pistola_agarre.fbx";
        public const string RutaPrefab = Carpeta + "W01_Pistola_agarre.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string NombreModelo = "W01";
        /// <summary>Giro 180° sobre Y: el cañón del FBX (-Z) a +Z y la corredera sigue en +Y.</summary>
        public static readonly Quaternion GiroModelo = Quaternion.Euler(0f, 180f, 0f);
        /// <summary>De los ejes de las curvas de W01 (cañón +X, arriba +Y) a los del FBX de agarre (cañón -Z, arriba +Y).</summary>
        public static readonly Quaternion EjesCurvas = Quaternion.Euler(0f, 90f, 0f);

        [MenuItem("Mecha Cyberpunk/T-736/Pistola con el modelo de agarre")]
        public static void Colocar()
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(RutaFbx);
            if (imp.materialImportMode != ModelImporterMaterialImportMode.None || imp.importAnimation)
            {
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.importAnimation = false;
                imp.SaveAndReimport();
            }
            CrearPrefab();
            ColocarMiraEnPrefab(RutaPrefab, true);
            PonerEnJugador(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            AssetDatabase.SaveAssets();
        }

        private static GameObject CrearPrefab()
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx);
            GameObject raiz = new GameObject("W01_Pistola_agarre");
            try
            {
                GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx, raiz.transform);
                PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                modelo.name = "Modelo";
                modelo.transform.localPosition = Vector3.zero;
                modelo.transform.localRotation = GiroModelo;
                Transform armazon = modelo.transform.Find("Armazon");
                Transform corredera = modelo.transform.Find("Corredera");
                Transform bateria = modelo.transform.Find("Bateria");
                armazon.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Carpeta + "W01_Armazon.mat");
                corredera.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Carpeta + "W01_Corredera.mat");
                bateria.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Carpeta + "W01_Cargador.mat");
                AnimacionPistola anim = raiz.AddComponent<AnimacionPistola>();
                anim.Configurar(corredera, bateria, 1f);
                anim.ConfigurarEjes(EjesCurvas);
                return PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        // ---- T-971: la mira de punto rojo de Blender sobre la corredera ----
        public const string RutaFbxMira = Carpeta + "Mira_PuntoRojo.fbx";
        public const string RutaPrefabBanco = Carpeta + "W01_Pistola.prefab";
        public const string NombreMira = "Mira";
        /// <summary>La base de la mira mide esta parte del largo de la corredera.</summary>
        public const float FraccionBase = 0.25f;
        private const float Hundido = 0.0015f;
        /// <summary>Dónde queda el centro de la base, desde la parte trasera de la corredera (0 atrás, 1 al frente): en la mitad trasera.</summary>
        private const float PosicionTrasera = 0.3f;

        /// <summary>Importa Mira_PuntoRojo.fbx (sin animación) y remapea sus 4 materiales. Idempotente.</summary>
        public static void ImportarMira()
        {
            AssetDatabase.ImportAsset(RutaFbxMira, ImportAssetOptions.ForceUpdate);
            var imp = (ModelImporter)AssetImporter.GetAtPath(RutaFbxMira);
            imp.animationType = ModelImporterAnimationType.None;
            imp.importAnimation = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.isReadable = false;
            imp.useFileScale = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
            imp.SaveAndReimport();
            Dictionary<string, Color> colores = new Dictionary<string, Color>();
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(RutaFbxMira))
            {
                if (o is Material m)
                {
                    colores[m.name] = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : (m.HasProperty("_Color") ? m.color : Color.white);
                }
            }
            Debug.Log("[Personaje] T-971: materiales del FBX " + string.Join(", ", colores.Select(k => k.Key + "=" + k.Value)));
            Color Elegir(string nombre, Color porDefecto) => colores.TryGetValue(nombre, out Color c) && c.maxColorComponent > 0.01f ? c : porDefecto;
            Material metal = CrearMaterial("Mira_Metal", Elegir("Mira_Metal", new Color(0.18f, 0.19f, 0.2f)), 0.8f, 0.55f);
            Material acento = CrearMaterial("Mira_Acento", Elegir("Mira_Acento", new Color(0.78f, 0.66f, 0.45f)), 0.1f, 0.35f);
            Material vidrio = CrearMaterial("Mira_Vidrio", Elegir("Mira_Vidrio", new Color(0.1f, 0.75f, 0.7f)), 0f, 0.95f);
            Material punto = CrearMaterial("Mira_Punto", Elegir("Mira_Punto", new Color(1f, 0.05f, 0.05f)), 0f, 0.5f);
            Transparente(vidrio, 0.35f);
            Emisivo(punto, new Color(1f, 0.08f, 0.08f), 4f);
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(RutaFbxMira))
            {
                if (o is Material m)
                {
                    Material destino = m.name == "Mira_Metal" ? metal : m.name == "Mira_Acento" ? acento : m.name == "Mira_Vidrio" ? vidrio : m.name == "Mira_Punto" ? punto : null;
                    if (destino != null)
                    {
                        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), destino);
                    }
                    else
                    {
                        Debug.LogWarning("[Personaje] T-971: material de la mira sin destino: " + m.name);
                    }
                }
            }
            imp.SaveAndReimport();
            AssetDatabase.SaveAssets();
        }

        private static Material CrearMaterial(string nombre, Color color, float metalico, float suavidad)
        {
            string ruta = Carpeta + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = nombre };
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metalico);
            m.SetFloat("_Smoothness", suavidad);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void Transparente(Material m, float alfa)
        {
            Color c = m.GetColor("_BaseColor");
            c.a = alfa;
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
        }

        private static void Emisivo(Material m, Color color, float intensidad)
        {
            m.SetColor("_BaseColor", color);
            m.SetColor("_EmissionColor", color * intensidad);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
        }

        /// <summary>
        /// Pone la mira (hijo Mira de la Corredera) en el prefab de agarre (con MiraSegunNivel en la raíz) y en el del banco (siempre visible).
        /// Idempotente.
        /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.ColocarAgarrePistolaT736.ColocarMirasPistola
        /// </summary>
        public static void ColocarMirasPistola()
        {
            ImportarMira();
            ColocarMiraEnPrefab(RutaPrefab, true);
            ColocarMiraEnPrefab(RutaPrefabBanco, false);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Abre el prefab, le pone la mira y lo guarda. <paramref name="conNivel"/>: MiraSegunNivel en la raíz (la mira apagada).</summary>
        public static void ColocarMiraEnPrefab(string rutaPrefab, bool conNivel)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbxMira) == null || AssetDatabase.LoadAssetAtPath<Material>(Carpeta + "Mira_Vidrio.mat") == null)
            {
                ImportarMira();
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(rutaPrefab);
            try
            {
                Transform mira = ColocarMira(raiz.transform);
                MiraSegunNivel nivel = raiz.GetComponent<MiraSegunNivel>();
                if (conNivel)
                {
                    nivel = nivel != null ? nivel : raiz.AddComponent<MiraSegunNivel>();
                    SerializedObject so = new SerializedObject(nivel);
                    so.FindProperty("mira").objectReferenceValue = mira.gameObject;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    mira.gameObject.SetActive(false);
                }
                else if (nivel != null)
                {
                    Object.DestroyImmediate(nivel);
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>
        /// Pone (o vuelve a poner) la mira como hijo Mira de la Corredera de <paramref name="raiz"/>: apoyada en lo alto de la corredera
        /// (hundida 1.5 mm en lo más alto bajo su huella), centrada a lo ancho, a 30 % del largo desde atrás, con el frente hacia la boca y la base del 25 % del largo.
        /// El frente es la dirección a Boca si existe; si no, la del cargador a la corredera sobre su eje largo. Devuelve la mira.
        /// </summary>
        public static Transform ColocarMira(Transform raiz)
        {
            Transform corredera = BuscarHijo(raiz, "Corredera");
            Transform viejo = BuscarHijo(corredera, NombreMira);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            Bounds b = LimitesLocales(raiz, corredera);
            Vector3 frente;
            Transform boca = BuscarHijo(raiz, "Boca");
            Transform bateria = BuscarHijo(raiz, "Bateria");
            if (boca != null)
            {
                frente = raiz.InverseTransformPoint(boca.position) - b.center;
            }
            else
            {
                frente = bateria != null ? b.center - LimitesLocales(raiz, bateria).center : Vector3.forward;
            }
            bool largoEnX = b.size.x > b.size.z;
            float signo = (largoEnX ? frente.x : frente.z) >= 0f ? 1f : -1f;
            frente = largoEnX ? new Vector3(signo, 0f, 0f) : new Vector3(0f, 0f, signo);
            float largo = largoEnX ? b.size.x : b.size.z;
            float atras = largoEnX ? (signo > 0f ? b.min.x : b.max.x) : (signo > 0f ? b.min.z : b.max.z);
            Vector3 baseMira = new Vector3(b.center.x, b.max.y - Hundido, b.center.z);
            if (largoEnX)
            {
                baseMira.x = atras + signo * largo * PosicionTrasera;
            }
            else
            {
                baseMira.z = atras + signo * largo * PosicionTrasera;
            }

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbxMira);
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(fbx, corredera);
            go.name = NombreMira;
            Transform t = go.transform;
            // El nodo raíz del FBX trae su propio giro (89.98° en X: el eje largo de la malla sobre Y y la altura sobre -Z): se conserva.
            t.rotation = raiz.rotation * Quaternion.LookRotation(frente, Vector3.up) * fbx.transform.localRotation;
            t.localScale = Vector3.one;
            t.position = raiz.TransformPoint(baseMira);
            // Se mide la malla real (el pivote del FBX puede no estar en la base): escala por el largo medido de la base y se corrige la posición.
            Bounds m0 = LimitesLocales(raiz, t, true);
            float largoMira = largoEnX ? m0.size.x : m0.size.z;
            float escala = largo * FraccionBase / largoMira;
            Vector3 padre = corredera.lossyScale;
            t.localScale = new Vector3(escala / padre.x, escala / padre.y, escala / padre.z);
            Bounds m1 = LimitesLocales(raiz, t, true);
            // Se apoya en lo más alto de la corredera justo bajo su huella (la corredera tiene miras de hierro más altas que su lomo, y no debe quedar flotando).
            baseMira.y = TopeBajo(raiz, corredera, new Vector2(baseMira.x, baseMira.z), new Vector2(m1.size.x, m1.size.z), b.max.y) - Hundido;
            Vector3 destino = new Vector3(baseMira.x, baseMira.y, baseMira.z);
            Vector3 delta = new Vector3(destino.x - m1.center.x, destino.y - m1.min.y, destino.z - m1.center.z);
            t.position += raiz.TransformVector(delta);
            Debug.Log("[Personaje] T-971: Mira en " + raiz.name + " base " + baseMira.ToString("F4") + " frente " + frente + " escala " + escala.ToString("F3")
                + " localScale " + t.localScale.ToString("F3") + " localPos " + t.localPosition.ToString("F4") + " largo corredera " + largo.ToString("F4")
                + " tam mira " + LimitesLocales(raiz, t, true).size.ToString("F4"));
            return t;
        }

        /// <summary>El Y más alto de los vértices de la corredera dentro de la huella (centro y tamaño en X y Z de la raíz); si no hay, <paramref name="porDefecto"/>.</summary>
        public static float TopeBajo(Transform raiz, Transform corredera, Vector2 centro, Vector2 tamano, float porDefecto)
        {
            MeshFilter f = corredera.GetComponent<MeshFilter>();
            Matrix4x4 m = raiz.worldToLocalMatrix * f.transform.localToWorldMatrix;
            List<Vector3> v = new List<Vector3>();
            f.sharedMesh.GetVertices(v);
            float tope = float.MinValue;
            foreach (Vector3 p in v)
            {
                Vector3 q = m.MultiplyPoint3x4(p);
                if (Mathf.Abs(q.x - centro.x) <= tamano.x * 0.5f && Mathf.Abs(q.z - centro.y) <= tamano.y * 0.5f)
                {
                    tope = Mathf.Max(tope, q.y);
                }
            }
            return tope > float.MinValue ? tope : porDefecto;
        }

        // Límites (con los vértices reales) de las mallas de "de" en el espacio local de la raíz, sin contar la mira.
        private static Bounds LimitesLocales(Transform raiz, Transform de, bool conMira = false)
        {
            Bounds b = new Bounds();
            bool primero = true;
            List<Vector3> v = new List<Vector3>();
            foreach (MeshFilter f in de.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null || (!conMira && f.GetComponentsInParent<Transform>(true).Any(p => p.name == NombreMira)))
                {
                    continue;
                }
                Matrix4x4 m = raiz.worldToLocalMatrix * f.transform.localToWorldMatrix;
                v.Clear();
                f.sharedMesh.GetVertices(v);
                foreach (Vector3 p in v)
                {
                    Vector3 q = m.MultiplyPoint3x4(p);
                    if (primero)
                    {
                        b = new Bounds(q, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        b.Encapsulate(q);
                    }
                }
            }
            return b;
        }

        private static Transform BuscarHijo(Transform raiz, string nombre)
        {
            return raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == nombre);
        }

        private static void PonerEnJugador(GameObject prefab)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaJugador);
            try
            {
                Transform pistola = contenido.GetComponentsInChildren<Transform>(true).First(t => t.name == "Pistola" && t.Find("Canon") != null);
                Transform viejo = pistola.Find(NombreModelo);
                int indice = viejo != null ? viejo.GetSiblingIndex() : pistola.childCount;
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
                GameObject nuevo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pistola);
                nuevo.name = NombreModelo;
                nuevo.transform.localPosition = Vector3.zero;
                nuevo.transform.localRotation = Quaternion.identity;
                nuevo.transform.localScale = Vector3.one;
                nuevo.transform.SetSiblingIndex(Mathf.Min(indice, pistola.childCount - 1));
                // La boca en la punta del cañón (en el espacio de la pistola) y a la altura de la corredera.
                Bounds corredera = nuevo.transform.Find("Modelo/Corredera").GetComponent<Renderer>().bounds;
                float zMax = float.MinValue;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 esquina = corredera.center + Vector3.Scale(corredera.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    zMax = Mathf.Max(zMax, pistola.InverseTransformPoint(esquina).z);
                }
                Transform boca = pistola.Find("Boca");
                if (boca != null)
                {
                    Vector3 centro = pistola.InverseTransformPoint(corredera.center);
                    boca.localPosition = new Vector3(0f, centro.y, zMax);
                    Debug.Log("[Personaje] T-736: Boca en " + boca.localPosition.ToString("F3"));
                }
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaJugador);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }
    }
}
