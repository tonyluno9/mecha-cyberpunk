using System.IO;
using MechaCyberpunk.Mundo.Ambiente;
using MechaCyberpunk.Mundo.Reloj;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-806 (noche 6-nov, sesión 2): cielo de día y de noche. <see cref="Colocar"/> cambia el material del cielo
    /// (Mundo/Materiales/Cielo_Procedural.mat, el que usan Ciudad, Ciudad_Prologo y Pruebas_Alan) al shader MechaCyberpunk/CieloGradiente y
    /// copia al asset ConfigCicloDia_Mundo los colores nuevos del cielo y de la niebla (los valores por defecto del código). Idempotente.
    /// Capturas del cielo a varias horas, en el editor y sin jugar. Pone la hora con
    /// <see cref="CicloDiaNoche.Aplicar"/>, arma las nubes y estrellas de T-635 (<see cref="CieloTexturizado"/>) y el domo de nubes de
    /// noche (<see cref="CieloNubes"/>) en la cámara de la foto, y toma dos vistas por hora (calle mirando arriba y horizonte desde lo
    /// alto) en Logs/s2n-cielo/. Con las de 13:00 y 22:00 arma docs/capturas/s2n-cielo-dia.png y s2n-cielo-noche.png (referencia | juego).
    /// No cambia la escena (la vuelve a abrir sin guardar).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nCielo.Fotos
    /// </summary>
    public static class ColocarS2nCielo
    {
        public const string Carpeta = "Logs/s2n-cielo/";
        public static readonly float[] Horas = { 6f, 9f, 13f, 17f, 19.5f, 22f, 2f };
        private const float Lejos = 1000f;

        // Calle del barrio (la de T-807) mirando al norte y hacia arriba, y vista desde lo alto hacia el horizonte.
        private static readonly Vector3 CalleDesde = new Vector3(-47f + 2.4f, 1.7f, -40f);
        private static readonly Vector3 CalleHacia = new Vector3(-47f, 22f, 10f);
        private static readonly Vector3 AltoDesde = new Vector3(-47f, 40f, -60f);
        private static readonly Vector3 AltoHacia = new Vector3(-47f, 48f, 60f);
        private static readonly Vector3 CenitDesde = new Vector3(-47f, 1.7f, -20f);
        private static readonly Vector3 CenitHacia = new Vector3(-46f, 60f, -14f);

        public const string RutaMaterial = "Assets/_Proyecto/Mundo/Materiales/Cielo_Procedural.mat";
        public const string RutaConfig = "Assets/_Proyecto/Mundo/Datos/ConfigCicloDia_Mundo.asset";
        public const string ShaderCielo = "MechaCyberpunk/CieloGradiente";
        private static readonly string[] CamposCopiados =
            { "colorNiebla", "colorCielo", "exposicionCielo", "colorBrilloCiudad", "fuerzaBrilloCiudad" };

        [MenuItem("Mecha Cyberpunk/S2n/7 Cielo de dia y de noche")]
        public static void Colocar()
        {
            Shader shader = Shader.Find(ShaderCielo);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (shader == null || material == null)
            {
                Debug.LogError("[Arte] S2n cielo: falta el shader " + ShaderCielo + " o " + RutaMaterial);
                return;
            }
            if (material.shader != shader)
            {
                material.shader = shader;
            }
            EditorUtility.SetDirty(material);

            Mundo.Datos.ConfigCicloDia asset = AssetDatabase.LoadAssetAtPath<Mundo.Datos.ConfigCicloDia>(RutaConfig);
            if (asset != null)
            {
                Mundo.Datos.ConfigCicloDia nuevo = ScriptableObject.CreateInstance<Mundo.Datos.ConfigCicloDia>();
                SerializedObject origen = new SerializedObject(nuevo);
                SerializedObject destino = new SerializedObject(asset);
                foreach (string campo in CamposCopiados)
                {
                    destino.CopyFromSerializedProperty(origen.FindProperty(campo));
                }
                destino.ApplyModifiedPropertiesWithoutUndo();
                Object.DestroyImmediate(nuevo);
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] S2n cielo: material con " + ShaderCielo + " y colores del cielo en " + RutaConfig);
        }

        /// <summary>Coloca y toma las capturas (para iterar desde batch).</summary>
        public static void ColocarYFotos()
        {
            Colocar();
            Fotos();
        }

        [MenuItem("Mecha Cyberpunk/S2n/Fotos del cielo")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            CicloDiaNoche ciclo = Object.FindFirstObjectByType<CicloDiaNoche>(FindObjectsInactive.Include);
            CieloNubes domo = Object.FindFirstObjectByType<CieloNubes>(FindObjectsInactive.Include);
            if (ciclo == null)
            {
                Debug.LogError("[Arte] S2n cielo: la escena no tiene CicloDiaNoche.");
                return;
            }
            Directory.CreateDirectory(Carpeta);
            CieloTexturizado capas = CieloTexturizado.Crear();
            foreach (float hora in Horas)
            {
                string h = Nombre(hora);
                Tomar(ciclo, capas, domo, hora, CalleDesde, CalleHacia, Carpeta + h + "-calle.png", 70f);
                Tomar(ciclo, capas, domo, hora, AltoDesde, AltoHacia, Carpeta + h + "-alto.png", 70f);
                Tomar(ciclo, capas, domo, hora, CenitDesde, CenitHacia, Carpeta + h + "-cenit.png", 80f);
            }
            capas?.Destruir();
            ColocarS2.LadoALado("docs/referencias-ciudad/cruce-dia-lluvia.png", Carpeta + "1300-calle.png", "docs/capturas/s2n-cielo-dia.png");
            ColocarS2.LadoALado("docs/referencias-ciudad/neon-alto-noche.png", Carpeta + "2200-alto.png", "docs/capturas/s2n-cielo-noche.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single); // sin guardar la hora de la foto
        }

        private static void Tomar(CicloDiaNoche ciclo, CieloTexturizado capas, CieloNubes domo, float hora, Vector3 desde, Vector3 hacia,
            string ruta, float campo)
        {
            ciclo.Aplicar(hora);
            SeguirNoche();
            // Las capas siguen a la cámara: una cámara vacía en el punto de la foto basta para colocarlas.
            GameObject marca = new GameObject("MarcaCieloS2n");
            Camera cam = marca.AddComponent<Camera>();
            cam.enabled = false;
            marca.transform.position = desde;
            capas?.Actualizar(cam, CicloPeso(ciclo, hora));
            if (domo != null)
            {
                domo.Colocar(cam, hora);
            }
            Object.DestroyImmediate(marca);
            ColocarS2.Foto(desde, hacia, ruta, campo, Lejos);
        }

        // En el juego VolumenSigueNoche (los volúmenes de zona, como el cálido de Suburbios) copia el peso de noche en LateUpdate;
        // en el editor no corre, y la foto de día salía con el tinte de noche de la zona.
        private static void SeguirNoche()
        {
            foreach (Mundo.Ambiente.VolumenSigueNoche sigue in Object.FindObjectsByType<Mundo.Ambiente.VolumenSigueNoche>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SerializedObject so = new SerializedObject(sigue);
                UnityEngine.Rendering.Volume origen = so.FindProperty("origen").objectReferenceValue as UnityEngine.Rendering.Volume;
                float maximo = so.FindProperty("pesoMaximo").floatValue;
                UnityEngine.Rendering.Volume propio = sigue.GetComponent<UnityEngine.Rendering.Volume>();
                if (origen != null && propio != null)
                {
                    propio.weight = origen.weight * maximo;
                    Shader.SetGlobalFloat("_T501Noche", origen.weight);
                }
            }
        }

        private static float CicloPeso(CicloDiaNoche ciclo, float hora)
        {
            SerializedObject so = new SerializedObject(ciclo);
            Mundo.Datos.ConfigCicloDia config = so.FindProperty("config").objectReferenceValue as Mundo.Datos.ConfigCicloDia;
            return config != null ? config.PesoNoche(hora) : 0f;
        }

        public static string Nombre(float hora)
        {
            int minutos = Mathf.RoundToInt(hora * 60f);
            return (minutos / 60).ToString("00") + (minutos % 60).ToString("00");
        }
    }
}
