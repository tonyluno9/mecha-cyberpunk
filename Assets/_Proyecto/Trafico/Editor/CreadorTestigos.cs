using MechaCyberpunk.Trafico.Peatones;
using MechaCyberpunk.Trafico.Testigos;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// Crea los assets de los testigos del delito (T-662, T-663): el prefab CamaraSeguridad (un poste con brazo y una cámara con carcasa, lente y un foquito
    /// que parpadea) en Trafico/Resources, para que GestorPersecucion lo cargue y lo ponga en las esquinas sin tocar la escena; y el material del icono
    /// del teléfono que flota sobre el civil que llama. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.CreadorTestigos.Crear
    /// </summary>
    public static class CreadorTestigos
    {
        public const string RutaPrefabCamara = "Assets/_Proyecto/Trafico/Resources/CamaraSeguridad.prefab";
        private const string RutaResources = "Assets/_Proyecto/Trafico/Resources";
        private const string RutaMateriales = "Assets/_Proyecto/Trafico/Materiales";
        private const float AlturaPoste = 5.2f;
        private const float RadioPoste = 0.09f;
        private const float LargoBrazo = 1.0f;
        private const float InclinacionCamara = 24f;

        [MenuItem("Mecha Cyberpunk/Crear T-662 testigos")]
        public static void Crear()
        {
            CrearCarpeta(RutaResources);
            Material metal = Mat("CamaraSeguridad_Poste", new Color(0.13f, 0.14f, 0.16f), 0.55f, default, 0.7f);
            Material carcasa = Mat("CamaraSeguridad_Carcasa", new Color(0.72f, 0.74f, 0.78f), 0.6f, default, 0.2f);
            Material lente = Mat("CamaraSeguridad_Lente", new Color(0.02f, 0.03f, 0.05f), 0.95f, default, 0.1f);
            Material foquito = Mat("CamaraSeguridad_Foquito", new Color(0.1f, 0.1f, 0.1f), 0.2f, new Color(0.4f, 2.2f, 0.5f), 0f);

            GameObject raiz = new GameObject("CamaraSeguridad");
            Pieza(raiz.transform, PrimitiveType.Cylinder, "Base", new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.38f, 0.05f, 0.38f), metal);
            GameObject poste = Pieza(raiz.transform, PrimitiveType.Cylinder, "Poste", new Vector3(0f, AlturaPoste / 2f, 0f), Vector3.zero,
                new Vector3(RadioPoste * 2f, AlturaPoste / 2f, RadioPoste * 2f), metal);
            CapsuleCollider cuerpo = poste.AddComponent<CapsuleCollider>();
            cuerpo.radius = 0.5f;
            cuerpo.height = 2f;
            Pieza(raiz.transform, PrimitiveType.Cube, "Brazo", new Vector3(0f, AlturaPoste - 0.12f, LargoBrazo / 2f), Vector3.zero,
                new Vector3(0.07f, 0.07f, LargoBrazo), metal);
            Pieza(raiz.transform, PrimitiveType.Cube, "Refuerzo", new Vector3(0f, AlturaPoste - 0.45f, 0.3f), new Vector3(-48f, 0f, 0f),
                new Vector3(0.05f, 0.05f, 0.8f), metal);

            // La cámara: un cuerpo alargado inclinado hacia la calle, con visera, lente y foquito.
            GameObject giro = new GameObject("Camara");
            giro.transform.SetParent(raiz.transform, false);
            giro.transform.localPosition = new Vector3(0f, AlturaPoste - 0.28f, LargoBrazo - 0.05f);
            giro.transform.localRotation = Quaternion.Euler(InclinacionCamara, 0f, 0f);
            Pieza(giro.transform, PrimitiveType.Cube, "Carcasa", new Vector3(0f, 0f, 0.05f), Vector3.zero, new Vector3(0.2f, 0.2f, 0.55f), carcasa);
            Pieza(giro.transform, PrimitiveType.Cube, "Visera", new Vector3(0f, 0.12f, 0.1f), Vector3.zero, new Vector3(0.24f, 0.025f, 0.6f), metal);
            Pieza(giro.transform, PrimitiveType.Cylinder, "Lente", new Vector3(0f, 0f, 0.34f), new Vector3(90f, 0f, 0f), new Vector3(0.15f, 0.03f, 0.15f), lente);
            GameObject led = Pieza(giro.transform, PrimitiveType.Sphere, "Foquito", new Vector3(0.07f, 0.06f, 0.325f), Vector3.zero, Vector3.one * 0.045f, foquito);
            GameObject ojo = new GameObject("Ojo");
            ojo.transform.SetParent(giro.transform, false);
            ojo.transform.localPosition = new Vector3(0f, 0f, 0.36f);

            CamaraSeguridad camara = raiz.AddComponent<CamaraSeguridad>();
            SerializedObject so = new SerializedObject(camara);
            so.FindProperty("ojo").objectReferenceValue = ojo.transform;
            so.FindProperty("indicador").objectReferenceValue = led.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabCamara);
            Object.DestroyImmediate(raiz);

            CrearMaterialIcono();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Trafico] T-662/T-663: prefab de la cámara de seguridad y material del icono de llamada creados.");
        }

        private static void CrearMaterialIcono()
        {
            string ruta = RutaResources + "/" + IndicadorLlamada.NombreMaterial + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetFloat("_Surface", 1f); // transparente
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
        }

        private static GameObject Pieza(Transform padre, PrimitiveType tipo, string nombre, Vector3 posicion, Vector3 giro, Vector3 escala, Material material)
        {
            GameObject pieza = GameObject.CreatePrimitive(tipo);
            pieza.name = nombre;
            Object.DestroyImmediate(pieza.GetComponent<Collider>());
            pieza.transform.SetParent(padre, false);
            pieza.transform.localPosition = posicion;
            pieza.transform.localRotation = Quaternion.Euler(giro);
            pieza.transform.localScale = escala;
            pieza.GetComponent<MeshRenderer>().sharedMaterial = material;
            return pieza;
        }

        private static Material Mat(string nombre, Color color, float suavidad, Color emision, float metalico)
        {
            string ruta = RutaMateriales + "/" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", suavidad);
            material.SetFloat("_Metallic", metalico);
            if (emision.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emision);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CrearCarpeta(string ruta)
        {
            if (!AssetDatabase.IsValidFolder(ruta))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/Trafico", "Resources");
            }
        }
    }
}
