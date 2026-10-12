using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Crea la cabina greybox de T-011 dentro de MechaJugador.prefab (idempotente: reemplaza "Cabina").
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorCabina.Crear
    /// Las medidas de las piezas del marco están en metros relativos a la cámara en reposo (x derecha, y arriba, z adelante).
    /// </summary>
    public static class CreadorCabina
    {
        private const string RutaMecha = "Assets/_Proyecto/Mecha";
        private const string RutaPrefab = RutaMecha + "/Prefabs/MechaJugador.prefab";
        private const string RutaMateriales = RutaMecha + "/Materiales/";
        private const float AlturaCamara = 3.5f;
        private const float PlanoCercano = 0.05f;
        private const float DistanciaMarco = 0.84f;
        private const float InclinacionPantalla = 60f;

        [MenuItem("Mecha Cyberpunk/Crear T-011")]
        public static void Crear()
        {
            Material estructura = CrearMaterial(RutaMateriales + "Cabina_Estructura.mat", new Color(0.1f, 0.11f, 0.14f), null);
            Material tablero = CrearMaterial(RutaMateriales + "Cabina_Tablero.mat", new Color(0.16f, 0.17f, 0.21f), null);
            Material brazo = CrearMaterial(RutaMateriales + "Cabina_Brazo.mat", new Color(0.35f, 0.2f, 0.12f), null);
            Material marcoPantalla = CrearMaterial(RutaMateriales + "Cabina_MarcoPantalla.mat", new Color(0.02f, 0.02f, 0.03f), null);
            Material neonTurquesa = CrearMaterial(RutaMateriales + "Cabina_NeonTurquesa.mat", new Color(0f, 0.1f, 0.1f), new Color(0f, 1f, 0.9f) * 3f);
            Material neonMagenta = CrearMaterial(RutaMateriales + "Cabina_NeonMagenta.mat", new Color(0.1f, 0f, 0.07f), new Color(1f, 0.1f, 0.7f) * 3f);
            Material cristal = CrearCristal(RutaMateriales + "Cabina_Cristal.mat");
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform vieja = raiz.transform.Find("Cabina");
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                }

                raiz.transform.Find("Camara").GetComponent<Camera>().nearClipPlane = PlanoCercano;

                Transform cabina = new GameObject("Cabina").transform;
                cabina.SetParent(raiz.transform, false);

                // Marco del cristal: dos postes, viga superior, dos paneles laterales y techo.
                Pieza("PosteIzquierdo", cabina, estructura, EnCabina(-0.7f, 0f, DistanciaMarco), new Vector3(0.07f, 1.1f, 0.06f));
                Pieza("PosteDerecho", cabina, estructura, EnCabina(0.7f, 0f, DistanciaMarco), new Vector3(0.07f, 1.1f, 0.06f));
                Pieza("VigaSuperior", cabina, estructura, EnCabina(0f, 0.4f, DistanciaMarco), new Vector3(1.5f, 0.08f, 0.06f));
                Pieza("PanelIzquierdo", cabina, estructura, EnCabina(-0.78f, 0f, 0.5f), new Vector3(0.04f, 0.9f, 1.0f));
                Pieza("PanelDerecho", cabina, estructura, EnCabina(0.78f, 0f, 0.5f), new Vector3(0.04f, 0.9f, 1.0f));
                Pieza("Techo", cabina, estructura, EnCabina(0f, 0.5f, 0.5f), new Vector3(1.6f, 0.04f, 1.0f));

                // Cristal: quad transparente justo detrás del marco (el quad mira hacia -z, hacia el piloto).
                Pieza("Cristal", cabina, cristal, EnCabina(0f, 0f, DistanciaMarco + 0.04f), new Vector3(1.8f, 1.0f, 1f), PrimitiveType.Quad);

                // Neón: turquesa bajo la viga, magenta en el borde del tablero.
                Pieza("NeonViga", cabina, neonTurquesa, EnCabina(0f, 0.355f, DistanciaMarco - 0.035f), new Vector3(1.3f, 0.02f, 0.02f));
                Pieza("NeonTablero", cabina, neonMagenta, EnCabina(0f, -0.295f, 0.86f), new Vector3(0.6f, 0.02f, 0.02f));

                // Tablero con el ancla y el marco de la pantalla de vida (T-013).
                Pieza("Tablero", cabina, tablero, EnCabina(0f, -0.38f, 0.6f), new Vector3(0.6f, 0.16f, 0.5f));
                GameObject marco = Pieza("MarcoPantallaVida", cabina, marcoPantalla, EnCabina(0f, -0.292f, 0.6f),
                    new Vector3(0.3f, 0.18f, 1f), PrimitiveType.Quad);
                marco.transform.localRotation = Quaternion.Euler(InclinacionPantalla, 0f, 0f);
                GameObject ancla = new GameObject("AnclaPantallaVida");
                ancla.transform.SetParent(cabina, false);
                ancla.transform.localPosition = EnCabina(0f, -0.285f, 0.6f);
                ancla.transform.localRotation = Quaternion.Euler(InclinacionPantalla, 0f, 0f);

                CrearBrazo("BrazoIzquierdo", cabina, brazo, -0.42f);
                CrearBrazo("BrazoDerecho", cabina, brazo, 0.42f);

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-011 creada.");
        }

        // Posición relativa a la cámara en reposo -> local del mecha.
        private static Vector3 EnCabina(float x, float y, float z)
        {
            return new Vector3(x, y + AlturaCamara, z);
        }

        // Pivote en el hombro; las tres partes miran hacia adelante (+z). T-012 mueve el pivote.
        private static void CrearBrazo(string nombre, Transform padre, Material material, float x)
        {
            GameObject pivote = new GameObject(nombre);
            pivote.transform.SetParent(padre, false);
            pivote.transform.localPosition = EnCabina(x, -0.2f, 0.15f);
            Pieza("Brazo", pivote.transform, material, new Vector3(0f, 0f, 0.15f), new Vector3(0.1f, 0.1f, 0.3f));
            Pieza("Antebrazo", pivote.transform, material, new Vector3(0f, 0f, 0.42f), new Vector3(0.09f, 0.09f, 0.26f));
            Pieza("Puno", pivote.transform, material, new Vector3(0f, 0f, 0.62f), new Vector3(0.14f, 0.14f, 0.14f));
        }

        private static GameObject Pieza(string nombre, Transform padre, Material material, Vector3 posicionLocal, Vector3 escala,
            PrimitiveType tipo = PrimitiveType.Cube)
        {
            GameObject pieza = GameObject.CreatePrimitive(tipo);
            pieza.name = nombre;
            Object.DestroyImmediate(pieza.GetComponent<Collider>());
            pieza.transform.SetParent(padre, false);
            pieza.transform.localPosition = posicionLocal;
            pieza.transform.localScale = escala;
            MeshRenderer render = pieza.GetComponent<MeshRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
            return pieza;
        }

        private static Material CrearMaterial(string ruta, Color color, Color? emision)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.3f);
            if (emision.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emision.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material CrearCristal(string ruta)
        {
            Material m = CrearMaterial(ruta, new Color(0.35f, 0.6f, 1f, 0.12f), null);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Smoothness", 0.9f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
