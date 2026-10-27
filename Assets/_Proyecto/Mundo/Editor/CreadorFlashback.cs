using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Crea Flashback.unity (T-090): el recuerdo de la niñez en la casa, de tarde y con luz cálida. Un cuarto de 10 × 8 m con una
    /// mesa con tres sillas (dos de adulto, una de niño), maletas junto a la puerta y el mecha cubierto con una lona en una esquina.
    /// Publica el punto de aparición Flashback_Inicio (a pie). La secuencia, los diálogos y el cambio de escena son de Tony (T-F6).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorFlashback.Crear
    /// </summary>
    public static class CreadorFlashback
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Flashback.unity";
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaPerfilDia = "Assets/_Proyecto/Mundo/Datos/PerfilDia.asset";

        private const string RutaPrefabFlashback = "Assets/_Proyecto/Progresion/Prologo/Prefabs/Flashback.prefab";
        public const float AlturaOjosNino = 1.1f;

        // Pose de cada plano: dónde está la cámara y qué mira (un niño de ocho años). Los textos y tiempos son de Tony (DatosFlashback_Base).
        public static readonly (Vector3 Posicion, Vector3 Mira)[] Planos =
        {
            (new Vector3(-1f, AlturaOjosNino, 1.5f), new Vector3(1.5f, 0.9f, 4.5f)),   // 1: la mesa con las tres sillas
            (new Vector3(0.5f, AlturaOjosNino, 3.2f), new Vector3(3.2f, 1.5f, 6.2f)),  // 2: el mecha bajo la lona
            (new Vector3(1.5f, AlturaOjosNino, 5.9f), new Vector3(0f, 1.5f, 0.2f)),    // 3: la puerta, desde la silla del niño
            (new Vector3(0f, AlturaOjosNino, 1.5f), new Vector3(0f, 1.2f, -6f)),       // 4: el porche, por donde se fue la camioneta
            (new Vector3(-3.5f, 1f, 3.5f), new Vector3(-1.8f, 0.4f, 1.2f)),            // 5: las maletas que quedaron
            (new Vector3(0.5f, 1f, 2f), new Vector3(3.2f, 2f, 6.2f)),                  // 6: el mecha, solo
        };

        private const float Ancho = 10f;    // x
        private const float Fondo = 8f;     // z
        private const float Alto = 3.4f;
        private const float Grosor = 0.3f;
        private const float AnchoPuerta = 1.8f;
        private const float AltoPuerta = 2.3f;

        [MenuItem("Mecha Cyberpunk/Crear T-090")]
        public static void Crear()
        {
            Material piso = Mat("Piso", new Color(0.42f, 0.3f, 0.2f), 0.25f, default);
            Material muro = Mat("Muro", new Color(0.75f, 0.62f, 0.45f), 0.05f, default);
            Material techo = Mat("Techo", new Color(0.6f, 0.5f, 0.38f), 0f, default);
            Material madera = Mat("Madera", new Color(0.4f, 0.24f, 0.12f), 0.3f, default);
            Material lona = Mat("Lona", new Color(0.55f, 0.55f, 0.5f), 0.05f, default);
            Material maleta = Mat("Maleta", new Color(0.25f, 0.18f, 0.12f), 0.2f, default);
            Material ventana = Mat("Ventana", new Color(1f, 0.85f, 0.6f), 0.2f, new Color(3f, 2.2f, 1.2f));

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform cuarto = new GameObject("Cuarto").transform;

            // Piso, techo y muros con collider. El muro sur (z = 0) lleva el hueco de la puerta.
            Caja(cuarto, "Piso", new Vector3(0f, -Grosor / 2f, Fondo / 2f), new Vector3(Ancho + 2f * Grosor, Grosor, Fondo + 2f * Grosor), piso, true);
            Caja(cuarto, "Techo", new Vector3(0f, Alto + Grosor / 2f, Fondo / 2f), new Vector3(Ancho + 2f * Grosor, Grosor, Fondo + 2f * Grosor), techo, true);
            Caja(cuarto, "MuroNorte", new Vector3(0f, Alto / 2f, Fondo + Grosor / 2f), new Vector3(Ancho + 2f * Grosor, Alto, Grosor), muro, true);
            Caja(cuarto, "MuroOeste", new Vector3(-Ancho / 2f - Grosor / 2f, Alto / 2f, Fondo / 2f), new Vector3(Grosor, Alto, Fondo), muro, true);
            Caja(cuarto, "MuroEste", new Vector3(Ancho / 2f + Grosor / 2f, Alto / 2f, Fondo / 2f), new Vector3(Grosor, Alto, Fondo), muro, true);
            float lado = (Ancho - AnchoPuerta) / 2f;
            Caja(cuarto, "MuroSurIzq", new Vector3(-(AnchoPuerta / 2f + lado / 2f), Alto / 2f, -Grosor / 2f), new Vector3(lado + Grosor, Alto, Grosor), muro, true);
            Caja(cuarto, "MuroSurDer", new Vector3(AnchoPuerta / 2f + lado / 2f, Alto / 2f, -Grosor / 2f), new Vector3(lado + Grosor, Alto, Grosor), muro, true);
            Caja(cuarto, "DinteLPuerta", new Vector3(0f, AltoPuerta + (Alto - AltoPuerta) / 2f, -Grosor / 2f), new Vector3(AnchoPuerta, Alto - AltoPuerta, Grosor), muro, true);

            // Ventana en el muro norte: un cuadro emisivo cálido con su luz de tarde.
            Caja(cuarto, "Ventana", new Vector3(-2.5f, 1.8f, Fondo - 0.02f), new Vector3(2.2f, 1.4f, 0.05f), ventana, false);
            Luz(cuarto, "LuzVentana", LightType.Spot, new Vector3(-2.5f, 2.2f, Fondo - 0.3f), Quaternion.Euler(60f, 180f, 0f), new Color(1f, 0.75f, 0.45f), 25f, 12f, 80f);
            Luz(cuarto, "LuzCuarto", LightType.Point, new Vector3(0f, Alto - 0.4f, Fondo / 2f), Quaternion.identity, new Color(1f, 0.8f, 0.55f), 9f, 11f, 0f);
            Luz(cuarto, "LuzPuerta", LightType.Point, new Vector3(0f, 2.6f, 1.2f), Quaternion.identity, new Color(1f, 0.7f, 0.4f), 4f, 6f, 0f);

            // Mesa con tres sillas: dos de adulto y una pequeña de niño; el mecha cubierto en la esquina noreste; maletas en la puerta.
            Caja(cuarto, "Mesa", new Vector3(1.5f, 0.75f, 4.5f), new Vector3(2.2f, 0.08f, 1.2f), madera, true);
            foreach (float x in new[] { 0.8f, 2.2f })
            {
                Caja(cuarto, "PataMesa", new Vector3(x, 0.37f, 4.5f), new Vector3(0.1f, 0.74f, 0.1f), madera, false);
            }
            Caja(cuarto, "SillaAdulto1", new Vector3(0.8f, 0.25f, 3.5f), new Vector3(0.5f, 0.5f, 0.5f), madera, true);
            Caja(cuarto, "SillaAdulto2", new Vector3(2.2f, 0.25f, 3.5f), new Vector3(0.5f, 0.5f, 0.5f), madera, true);
            Caja(cuarto, "SillaNino", new Vector3(1.5f, 0.15f, 5.5f), new Vector3(0.3f, 0.3f, 0.3f), madera, true);
            Caja(cuarto, "MechaCubierto", new Vector3(3.2f, 1.6f, 6.2f), new Vector3(2.2f, 3.2f, 2.4f), lona, true);
            Caja(cuarto, "Maleta1", new Vector3(-1.4f, 0.3f, 1.3f), new Vector3(0.8f, 0.6f, 0.4f), maleta, true);
            Caja(cuarto, "Maleta2", new Vector3(-2.2f, 0.25f, 1.1f), new Vector3(0.6f, 0.5f, 0.4f), maleta, true);

            // Un camino de tarde afuera: piso de banqueta para que la puerta no dé al vacío.
            Caja(cuarto, "Porche", new Vector3(0f, -0.1f, -3f), new Vector3(8f, 0.2f, 6f), piso, true);

            // Punto de aparición a pie, en el centro del cuarto, mirando al mecha cubierto.
            Transform inicio = new GameObject("Flashback_Inicio").transform;
            inicio.position = new Vector3(0f, 0f, 2f);
            inicio.rotation = Quaternion.LookRotation(new Vector3(3.2f, 0f, 6.2f) - inicio.position, Vector3.up);
            PuntoAparicion punto = inicio.gameObject.AddComponent<PuntoAparicion>();
            SerializedObject so = new SerializedObject(punto);
            so.FindProperty("id").stringValue = "Flashback_Inicio";
            so.FindProperty("modo").enumValueIndex = (int)ModoJugador.APie;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Ambiente de tarde: luz solar baja, ambiente cálido, sin la niebla azul de la noche, con el posproceso de día.
            GameObject sol = new GameObject("LuzTarde");
            sol.transform.rotation = Quaternion.Euler(20f, 200f, 0f);
            Light luzSol = sol.AddComponent<Light>();
            luzSol.type = LightType.Directional;
            luzSol.color = new Color(1f, 0.7f, 0.4f);
            luzSol.intensity = 1.1f;
            luzSol.shadows = LightShadows.Soft;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.4f, 0.28f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.8f, 0.55f, 0.35f);
            RenderSettings.fogDensity = 0.01f;
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RutaPerfilDia);
            if (perfil != null)
            {
                GameObject volumen = new GameObject("PosprocesoTarde");
                Volume volume = volumen.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.sharedProfile = perfil;
            }

            // T-093 (D-tony-72): seis planos de cámara a la altura de los ojos de un niño y el prefab del flashback de Tony (director, cámara y subtítulos).
            Transform planos = new GameObject("PlanosFlashback").transform;
            for (int i = 0; i < Planos.Length; i++)
            {
                GameObject plano = new GameObject("Flashback_Plano_" + (i + 1));
                plano.transform.SetParent(planos, false);
                plano.transform.position = Planos[i].Posicion;
                plano.transform.rotation = Quaternion.LookRotation(Planos[i].Mira - Planos[i].Posicion, Vector3.up);
                SerializedObject soPlano = new SerializedObject(plano.AddComponent<PuntoHistoriaMundo>());
                soPlano.FindProperty("id").stringValue = plano.name;
                soPlano.ApplyModifiedPropertiesWithoutUndo();
            }
            GameObject prefabFlashback = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabFlashback);
            if (prefabFlashback == null)
            {
                throw new System.InvalidOperationException("No existe " + RutaPrefabFlashback + "; falta mezclar origin/main.");
            }
            PrefabUtility.InstantiatePrefab(prefabFlashback);

            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-090 creada: Flashback.unity con Flashback_Inicio, seis planos y el prefab del flashback.");
        }

        private static GameObject Caja(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material, bool conCollider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!conCollider)
            {
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        private static void Luz(Transform padre, string nombre, LightType tipo, Vector3 posicion, Quaternion rotacion, Color color, float intensidad, float alcance, float angulo)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localRotation = rotacion;
            Light luz = go.AddComponent<Light>();
            luz.type = tipo;
            luz.color = color;
            luz.intensity = intensidad;
            luz.range = alcance;
            if (tipo == LightType.Spot)
            {
                luz.spotAngle = angulo;
            }
            luz.shadows = LightShadows.None;
        }

        private static Material Mat(string nombre, Color color, float suavidad, Color emision)
        {
            string ruta = RutaMateriales + "/Flashback_" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", suavidad);
            if (emision.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emision);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
