using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Viste el callejón de la emboscada (T-088) que CreadorBarrio deja libre en Manzana_4: cajas, barriles y basura con collider,
    /// letrero de neón y luces en el fondo, el punto de aparición Callejon_Entrada, el punto de llamado del mecha en el centro
    /// (cielo abierto) y el anclaje donde las pruebas de Tony instancian Emboscada.prefab. No coloca Emboscada.prefab: se activa
    /// sola cuando el jugador (o la cámara libre) se acerca, y eso cambiaría el barrio de Alan (D-029).
    /// Corre después de CreadorCasa y se puede repetir (usa CreadorTodo.Crear).
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorCallejon.Crear
    /// </summary>
    public static class CreadorCallejon
    {
        private const string RutaMateriales = "Assets/_Proyecto/Mundo/Materiales";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";

        // Coordenadas locales de Manzana_4 (m). El callejón corre de x = CallejonX0 (fondo cerrado) a x = 37 (boca, banqueta hasta 40).
        private const float AlturaBanqueta = 0.2f;
        private const float EjeZ = (CreadorBarrio.CallejonZ0 + CreadorBarrio.CallejonZ1) / 2f;
        private const float XEntrada = 38f;        // donde aparece el jugador a pie
        private const float XAnclaje = 35f;         // origen de Emboscada.prefab; sus rivales quedan a 14 m, en x = 21
        private const float XLlegada = 28f;         // llegada del mecha: círculo libre de 3 m (x de 25 a 31)
        private const float RadioLlamado = 14f;
        private const float AlturaLetrero = 3.2f;

        [MenuItem("Mecha Cyberpunk/Crear T-088")]
        public static void Crear()
        {
            Material caja = Mat("Caja", new Color(0.35f, 0.25f, 0.15f), 0.2f, default);
            Material barril = Mat("Barril", new Color(0.2f, 0.28f, 0.3f), 0.4f, default);
            Material basura = Mat("Basura", new Color(0.08f, 0.1f, 0.08f), 0.3f, default);
            Material neon = Mat("Neon", new Color(0.9f, 0.1f, 0.6f), 0.2f, new Color(3.2f, 0.2f, 2f));
            Material neonCian = Mat("NeonCian", new Color(0.1f, 0.8f, 0.9f), 0.2f, new Color(0.3f, 2.8f, 3.2f));

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBarrio);
            Transform manzana = raiz.transform.Find("Manzana_4");
            Transform viejo = manzana.Find("Callejon");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            Transform callejon = Vacio("Callejon", manzana, new Vector3(0f, AlturaBanqueta, 0f), Quaternion.identity);

            // Cajas y barriles contra los muros, lejos del eje y del círculo de llegada.
            float zNorte = CreadorBarrio.CallejonZ1 - 0.6f;
            float zSur = CreadorBarrio.CallejonZ0 + 0.6f;
            Caja(callejon, "Caja1", new Vector3(8.5f, 0.45f, zNorte), new Vector3(0.9f, 0.9f, 0.9f), caja, true);
            Caja(callejon, "Caja2", new Vector3(8.5f, 1.35f, zNorte), new Vector3(0.8f, 0.9f, 0.8f), caja, true);
            Caja(callejon, "Caja3", new Vector3(10f, 0.45f, zNorte), new Vector3(0.9f, 0.9f, 0.9f), caja, true);
            Caja(callejon, "Caja4", new Vector3(16f, 0.4f, zSur), new Vector3(0.8f, 0.8f, 0.8f), caja, true);
            Cilindro(callejon, "Barril1", new Vector3(8.4f, 0.5f, zSur), barril);
            Cilindro(callejon, "Barril2", new Vector3(9.4f, 0.5f, zSur), barril);
            Caja(callejon, "Bolsas1", new Vector3(13f, 0.3f, zNorte), new Vector3(1.2f, 0.6f, 0.7f), basura, true);
            Caja(callejon, "Bolsas2", new Vector3(33f, 0.3f, zSur), new Vector3(1.2f, 0.6f, 0.7f), basura, true);
            Caja(callejon, "Contenedor", new Vector3(19f, 0.7f, zNorte - 0.1f), new Vector3(2.2f, 1.4f, 1.1f), barril, true);

            // Letrero de neón en el fondo cerrado y un par de tiras a lo largo de los muros (sin collider).
            GameObject letrero = Caja(callejon, "LetreroFondo", new Vector3(CreadorBarrio.CallejonX0 + 0.15f, AlturaLetrero, EjeZ), new Vector3(0.12f, 0.9f, 4f), neon, false);
            GameObject tiraNorte = Caja(callejon, "TiraNorte", new Vector3(20f, 2.4f, CreadorBarrio.CallejonZ1 - 0.1f), new Vector3(18f, 0.08f, 0.05f), neonCian, false);
            GameObject tiraSur = Caja(callejon, "TiraSur", new Vector3(20f, 2.4f, CreadorBarrio.CallejonZ0 + 0.1f), new Vector3(18f, 0.08f, 0.05f), neonCian, false);
            Light luzFondo = Luz(callejon, "LuzFondo", new Vector3(CreadorBarrio.CallejonX0 + 2f, 3f, EjeZ), new Color(0.9f, 0.1f, 0.6f), 8f, 10f);
            Light luzMedio = Luz(callejon, "LuzMedio", new Vector3(20f, 4f, EjeZ), new Color(0.1f, 0.8f, 0.9f), 6f, 12f);
            PonerLuzNocturna(luzFondo, letrero.GetComponent<Renderer>(), tiraNorte.GetComponent<Renderer>(), tiraSur.GetComponent<Renderer>());
            PonerLuzNocturna(luzMedio);

            // Anclaje de Emboscada.prefab: +Z hacia el fondo del callejón (-X de la manzana).
            Vacio("Emboscada_Anclaje", callejon, new Vector3(XAnclaje, 0f, EjeZ), Quaternion.Euler(0f, -90f, 0f));

            // Punto de llamado del mecha en el centro del callejón, mirando hacia el fondo.
            Transform llegada = Vacio("Llamado_Callejon", callejon, new Vector3(XLlegada, 0f, EjeZ), Quaternion.Euler(0f, -90f, 0f));
            PuntoLlamadoMundo llamado = llegada.gameObject.AddComponent<PuntoLlamadoMundo>();
            SerializedObject soLlamado = new SerializedObject(llamado);
            soLlamado.FindProperty("radio").floatValue = RadioLlamado;
            soLlamado.ApplyModifiedPropertiesWithoutUndo();

            // Punto de aparición a pie en la boca del callejón, mirando hacia adentro.
            Transform grupoPuntos = raiz.transform.Find("PuntosAparicion");
            Transform entrada = Vacio("Callejon_Entrada", grupoPuntos, Vector3.zero, Quaternion.identity);
            entrada.position = manzana.TransformPoint(new Vector3(XEntrada, AlturaBanqueta, EjeZ));
            entrada.rotation = manzana.rotation * Quaternion.Euler(0f, -90f, 0f);
            PuntoAparicion punto = entrada.gameObject.AddComponent<PuntoAparicion>();
            SerializedObject soPunto = new SerializedObject(punto);
            soPunto.FindProperty("id").stringValue = "Callejon_Entrada";
            soPunto.FindProperty("modo").enumValueIndex = (int)ModoJugador.APie;
            soPunto.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBarrio);
            PrefabUtility.UnloadPrefabContents(raiz);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mundo] T-088 creada: callejón de la emboscada con punto de llamado.");
        }

        private static Transform Vacio(string nombre, Transform padre, Vector3 posicion, Quaternion rotacion)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localRotation = rotacion;
            return go.transform;
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

        private static GameObject Cilindro(Transform padre, string nombre, Vector3 posicion, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static Light Luz(Transform padre, string nombre, Vector3 posicion, Color color, float intensidad, float alcance)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            Light luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = color;
            luz.intensity = intensidad;
            luz.range = alcance;
            luz.shadows = LightShadows.None;
            luz.enabled = false; // LuzNocturna la prende de noche
            return luz;
        }

        // De noche prende la luz y el brillo de los renderers; de día los apaga (como las farolas del barrio).
        private static void PonerLuzNocturna(Light luz, params Renderer[] renderers)
        {
            LuzNocturna nocturna = luz.gameObject.AddComponent<LuzNocturna>();
            SerializedObject so = new SerializedObject(nocturna);
            so.FindProperty("luz").objectReferenceValue = luz;
            SerializedProperty propRenderers = so.FindProperty("renderers");
            propRenderers.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
            {
                propRenderers.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material Mat(string nombre, Color color, float suavidad, Color emision)
        {
            string ruta = RutaMateriales + "/Callejon_" + nombre + ".mat";
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
