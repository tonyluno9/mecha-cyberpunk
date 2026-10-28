using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.Personaje.Combate;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-192: parcha JugadorAPie.prefab con el puño que se ve cromado (PunoImplantes y su franja) sin regenerarlo, porque otros
    /// creadores le agregan cosas después. Es idempotente. También genera las capturas docs/capturas/T-192-1.png y T-192-2.png.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CreadorImplantesAPie.Crear (y .Capturar).
    /// </summary>
    public static class CreadorImplantesAPie
    {
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaCuerpo = "Assets/_Proyecto/Personaje/Prefabs/CuerpoJugador.prefab";
        private const string RutaPiso = "Assets/_Proyecto/Personaje/Materiales/Greybox_Piso.mat";
        private const string RutaMunecoPrefab = "Assets/_Proyecto/Personaje/Pruebas/MunecoAPie.prefab";

        [MenuItem("Mecha Cyberpunk/Crear T-192 implantes a pie")]
        public static void Crear()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            bool cambio = false;
            try
            {
                Transform puno = raiz.transform.Find("Camara/Manos/Puno");
                if (puno == null)
                {
                    Debug.LogError("[Personaje] T-192: no se encontró Camara/Manos/Puno en " + RutaPrefab);
                    return;
                }
                if (puno.GetComponent<PunoImplantes>() != null) return;

                var render = puno.GetComponent<MeshRenderer>();
                GameObject franja = GameObject.CreatePrimitive(PrimitiveType.Cube);
                franja.name = "FranjaImplante";
                Object.DestroyImmediate(franja.GetComponent<Collider>());
                franja.transform.SetParent(puno, false);
                franja.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                franja.transform.localScale = new Vector3(1.06f, 0.14f, 0.3f);
                var renderFranja = franja.GetComponent<MeshRenderer>();
                renderFranja.sharedMaterial = render.sharedMaterial;
                renderFranja.shadowCastingMode = ShadowCastingMode.Off;
                franja.SetActive(false);

                var so = new SerializedObject(puno.gameObject.AddComponent<PunoImplantes>());
                so.FindProperty("puno").objectReferenceValue = render;
                so.FindProperty("franja").objectReferenceValue = franja;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                cambio = true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            Debug.Log(cambio ? "[Personaje] T-192 creada." : "[Personaje] T-192 ya estaba.");
        }

        /// <summary>T-192-1: el cuerpo con los 4 implantes de nivel 2 y el cosmético "Ojo LED rojo", de frente, de lado y de espaldas. T-192-2: el puño en primera persona.</summary>
        [MenuItem("Mecha Cyberpunk/Capturar T-192")]
        public static void Capturar()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cuerpoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCuerpo);
            // Orden: de frente, de lado, de espaldas (giro en Y).
            float[] giros = { 0f, 90f, 180f };
            for (int i = 0; i < giros.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(cuerpoPrefab);
                go.transform.position = new Vector3(i * 1.2f, 0f, 0f);
                go.transform.rotation = Quaternion.Euler(0f, giros[i], 0f);
                var cuerpo = go.GetComponent<CuerpoJugador>();
                cuerpo.Aplicar(new AparienciaJugador(0, 0, 2));
                foreach (EspacioImplante espacio in System.Enum.GetValues(typeof(EspacioImplante)))
                {
                    cuerpo.MostrarPrueba(espacio, 2);
                }
            }
            Piso(new Vector3(1.2f, -0.05f, 0f), new Vector3(5f, 0.1f, 2f));
            Luz();
            Foto(new Vector3(1.2f, 0.95f, 5.2f), Quaternion.Euler(0f, 180f, 0f), 35f, 1600, 700, "docs/capturas/T-192-1.png");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            jugador.transform.position = Vector3.zero;
            Piso(new Vector3(0f, -0.05f, 3f), new Vector3(8f, 0.1f, 10f));
            Luz();
            var muneco = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMunecoPrefab));
            muneco.transform.position = new Vector3(0f, 0f, 2.5f);
            jugador.transform.Find("Camara/Manos/Pistola").gameObject.SetActive(false);
            jugador.transform.Find("Camara/Manos/Puno").GetComponent<PunoImplantes>().Aplicar(2);
            Foto(jugador.GetComponentInChildren<Camera>(), 1280, 720, "docs/capturas/T-192-2.png");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Debug.Log("[Personaje] Capturas T-192 guardadas.");
        }

        private static void Piso(Vector3 posicion, Vector3 escala)
        {
            var piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = posicion;
            piso.transform.localScale = escala;
            piso.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(RutaPiso);
        }

        private static void Luz()
        {
            var luzGo = new GameObject("Luz");
            luzGo.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
            var luz = luzGo.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.4f;
        }

        private static void Foto(Vector3 posicion, Quaternion rotacion, float campo, int ancho, int alto, string ruta)
        {
            var camGo = new GameObject("CamaraCaptura");
            camGo.transform.SetPositionAndRotation(posicion, rotacion);
            var camara = camGo.AddComponent<Camera>();
            camara.fieldOfView = campo;
            Foto(camara, ancho, alto, ruta);
        }

        private static void Foto(Camera camara, int ancho, int alto, string ruta)
        {
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.04f, 0.05f, 0.09f, 1f);
            RenderTexture rt = RenderTexture.GetTemporary(ancho, alto, 24, RenderTextureFormat.ARGB32);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            var textura = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            textura.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            textura.Apply();
            RenderTexture.active = null;
            camara.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, textura.EncodeToPNG());
            Object.DestroyImmediate(textura);
        }
    }
}
