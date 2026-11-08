using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.UI.Inicio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-893 (C1): fotos de las calles reales para el fondo del menú de inicio y el logo LG04 con transparencia, como sprites en Resources.
    /// Los personajes se apagan para la foto (en el editor salen en pose T); la escena no se guarda.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2mInicio.Capturar
    /// </summary>
    public static class ColocarS2mInicio
    {
        public const string Carpeta = "Assets/_Proyecto/UI/Resources/Inicio";
        private const string LogoOrigen = "imageassets/referencias-privadas/logo/LG24_Pintado_Amarillo.png";
        private const int PasadasLimpieza = 6;
        private const float CercaCamara = 0.2f;
        private const float LejosCamara = 1200f;
        private const float CampoFoto = 66f;
        private const float BrilloParaOpaco = 2.2f;
        private const float AlfaMinimaRecorte = 0.08f;

        [MenuItem("Mecha Cyberpunk/S2m/1 Fondo y logo del menú de inicio")]
        public static void Capturar()
        {
            Directory.CreateDirectory(Carpeta);
            ColocarS2.Abrir();
            foreach (Animator a in Object.FindObjectsByType<Animator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                a.gameObject.SetActive(false);
            }
            (Vector3 desde, Vector3 hacia)[] tomas = Tomas();
            for (int i = 0; i < tomas.Length; i++)
            {
                ColocarS2cLook.Foto(tomas[i].desde, tomas[i].hacia, Carpeta + "/FondoCalle_" + i + ".png", CampoFoto);
                Importar(Carpeta + "/FondoCalle_" + i + ".png", false);
            }
            Logo();
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
        }

        private static (Vector3 desde, Vector3 hacia)[] Tomas() => new[]
        {
            (ColocarS2cCalle.CamaraFoto + Vector3.up * 0.2f, ColocarS2cCalle.MiraFoto + Vector3.up * 0.2f),
            (ColocarS2cMercado.Tomas[0].desde, ColocarS2cMercado.Tomas[0].hacia),
            (ColocarS2cNeonAlto.Tomas[0].desde, ColocarS2cNeonAlto.Tomas[0].hacia),
            (new Vector3(58f, 2.6f, ColocarS2cNova.ZCentro - 0.5f), new Vector3(130f, 5f, ColocarS2cNova.ZCentro)),
        };

        /// <summary>T-898: solo el logo. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2mInicio.SoloLogo</summary>
        public static void SoloLogo()
        {
            Logo();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// T-898: genera Menu_Calles.unity: una copia de la ciudad sin lógica (sin scripts, físicas, personajes ni audio), solo lo que se ve y las luces, con
        /// la cámara y sus cuatro tomas. No se guarda la escena de la ciudad. Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2mInicio.GenerarEscena
        /// </summary>
        [MenuItem("Mecha Cyberpunk/S2m/5 Escena de calles del menú")]
        public static void GenerarEscena()
        {
            ColocarS2.Abrir();
            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                    {
                        PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    }
                }
            }
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                Limpiar(raiz);
            }

            GameObject recorrido = new GameObject("RecorridoCallesMenu");
            Camera camara = new GameObject("CamaraMenu").AddComponent<Camera>();
            camara.transform.SetParent(recorrido.transform, false);
            camara.fieldOfView = CampoFoto;
            camara.nearClipPlane = CercaCamara;
            camara.farClipPlane = LejosCamara;
            camara.depth = 10f;
            camara.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            (Vector3 desde, Vector3 hacia)[] tomas = Tomas();
            Transform[] desdes = new Transform[tomas.Length];
            Transform[] hacias = new Transform[tomas.Length];
            for (int i = 0; i < tomas.Length; i++)
            {
                desdes[i] = Marca(recorrido.transform, "Desde_" + i, tomas[i].desde);
                hacias[i] = Marca(recorrido.transform, "Hacia_" + i, tomas[i].hacia);
            }
            recorrido.AddComponent<RecorridoCallesMenu>().Configurar(camara, desdes, hacias);

            EditorSceneManager.SaveScene(escena, FondoCallesInicio.EscenaCalles, true);
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
        }

        private static Transform Marca(Transform padre, string nombre, Vector3 posicion)
        {
            Transform t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            t.position = posicion;
            return t;
        }

        // Deja solo lo que se ve: fuera lo apagado, los personajes y cualquier cosa con lógica o sonido.
        private static void Limpiar(GameObject raiz)
        {
            List<GameObject> fuera = new List<GameObject>();
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                if (!go.activeInHierarchy || go.GetComponent<Animator>() != null || go.GetComponent<Canvas>() != null || go.GetComponent<Camera>() != null
                    || go.GetComponent<SkinnedMeshRenderer>() != null || go.GetComponent<CharacterController>() != null)
                {
                    fuera.Add(go);
                }
            }
            foreach (GameObject go in fuera)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            if (raiz == null)
            {
                return;
            }
            // Varias pasadas: un script no se puede quitar mientras otro lo exija.
            for (int pasada = 0; pasada < PasadasLimpieza; pasada++)
            {
                foreach (Component c in raiz.GetComponentsInChildren<Component>(true))
                {
                    if (c is MonoBehaviour || c is Collider || c is Rigidbody || c is AudioSource || c is AudioListener || c is NavMeshObstacle || c is NavMeshAgent
                        || c is CharacterController)
                    {
                        Object.DestroyImmediate(c);
                    }
                }
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(raiz);
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                }
            }
        }

        /// <summary>El logo viene sobre negro: el brillo se vuelve transparencia y se recorta a lo que se ve.</summary>
        private static void Logo()
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(LogoOrigen));
            Color[] p = t.GetPixels();
            int x0 = t.width, x1 = 0, y0 = t.height, y1 = 0;
            for (int y = 0; y < t.height; y++)
            {
                for (int x = 0; x < t.width; x++)
                {
                    Color c = p[y * t.width + x];
                    float alfa = Mathf.Clamp01(Mathf.Max(c.r, c.g, c.b) * BrilloParaOpaco);
                    p[y * t.width + x] = new Color(c.r, c.g, c.b, alfa);
                    if (alfa > AlfaMinimaRecorte)
                    {
                        x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
                    }
                }
            }
            int ancho = x1 - x0 + 1, alto = y1 - y0 + 1;
            Texture2D r = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    r.SetPixel(x, y, p[(y + y0) * t.width + x + x0]);
                }
            }
            r.Apply();
            string ruta = Carpeta + "/Logo.png";
            File.WriteAllBytes(ruta, r.EncodeToPNG());
            Object.DestroyImmediate(t);
            Object.DestroyImmediate(r);
            Importar(ruta, true);
        }

        private static void Importar(string ruta, bool alfa)
        {
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = alfa;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
    }
}
