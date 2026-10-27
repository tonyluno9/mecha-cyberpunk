using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Apariencia;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// Crea los assets de T-123: catálogo de apariencia, material, CuerpoJugador.prefab y VistaPreviaApariencia.prefab, y
    /// genera la captura docs/capturas/T-123-1.png. Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.CreadorApariencia.Crear
    /// </summary>
    public static class CreadorApariencia
    {
        private const string RutaPersonaje = "Assets/_Proyecto/Personaje";
        private const string RutaCatalogo = RutaPersonaje + "/Datos/CatalogoApariencia_Base.asset";
        private const string RutaMaterial = RutaPersonaje + "/Materiales/Greybox_Apariencia.mat";
        private const string RutaPrefabCuerpo = RutaPersonaje + "/Prefabs/CuerpoJugador.prefab";
        private const string RutaPrefabVista = RutaPersonaje + "/Prefabs/VistaPreviaApariencia.prefab";
        private const string RutaCaptura = "docs/capturas/T-123-1.png";

        [MenuItem("Mecha Cyberpunk/Crear T-123 apariencia")]
        public static void Crear()
        {
            CatalogoApariencia catalogo = CrearCatalogo();
            Material material = CrearMaterial();
            CrearPrefabCuerpo(catalogo, material);
            CrearPrefabVista();
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-123 creada.");
        }

        private static CatalogoApariencia CrearCatalogo()
        {
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoApariencia>(RutaCatalogo);
            if (catalogo != null) return catalogo;
            catalogo = ScriptableObject.CreateInstance<CatalogoApariencia>();
            var so = new SerializedObject(catalogo);
            LlenarCaras(so.FindProperty("caras"));
            LlenarRopas(so.FindProperty("ropas"));
            LlenarImplantes(so.FindProperty("implantes"));
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(catalogo, RutaCatalogo);
            return catalogo;
        }

        private static void LlenarCaras(SerializedProperty lista)
        {
            lista.arraySize = 4;
            Cara(lista, 0, "Rapado", new Color(0.96f, 0.80f, 0.66f), PeinadoApariencia.Rapado, new Color(0.12f, 0.12f, 0.15f));
            Cara(lista, 1, "Cresta", new Color(0.78f, 0.58f, 0.42f), PeinadoApariencia.Cresta, new Color(0.9f, 0.2f, 0.6f));
            Cara(lista, 2, "Trenza", new Color(0.55f, 0.38f, 0.27f), PeinadoApariencia.Trenza, new Color(0.1f, 0.75f, 0.75f));
            Cara(lista, 3, "Gorra", new Color(0.35f, 0.24f, 0.18f), PeinadoApariencia.Gorra, new Color(0.95f, 0.5f, 0.1f));
        }

        private static void Cara(SerializedProperty lista, int i, string nombre, Color piel, PeinadoApariencia peinado, Color colorPeinado)
        {
            SerializedProperty e = lista.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("nombre").stringValue = nombre;
            e.FindPropertyRelative("piel").colorValue = piel;
            e.FindPropertyRelative("peinado").enumValueIndex = (int)peinado;
            e.FindPropertyRelative("colorPeinado").colorValue = colorPeinado;
        }

        private static void LlenarRopas(SerializedProperty lista)
        {
            lista.arraySize = 4;
            Ropa(lista, 0, "Chamarra magenta", new Color(0.85f, 0.1f, 0.55f), new Color(0.1f, 0.1f, 0.15f), true, PiezaRopaApariencia.Hombreras, new Color(0.2f, 0.2f, 0.25f));
            Ropa(lista, 1, "Sudadera turquesa", new Color(0.1f, 0.7f, 0.7f), new Color(0.15f, 0.18f, 0.3f), true, PiezaRopaApariencia.Capucha, new Color(0.07f, 0.5f, 0.5f));
            Ropa(lista, 2, "Chaleco naranja", new Color(0.95f, 0.5f, 0.1f), new Color(0.2f, 0.2f, 0.22f), false, PiezaRopaApariencia.Bandas, new Color(0.9f, 0.9f, 0.9f));
            Ropa(lista, 3, "Overol azul", new Color(0.1f, 0.25f, 0.8f), new Color(0.1f, 0.25f, 0.8f), false, PiezaRopaApariencia.Tirantes, new Color(0.05f, 0.15f, 0.55f));
        }

        private static void Ropa(SerializedProperty lista, int i, string nombre, Color color, Color pantalon, bool mangas, PiezaRopaApariencia pieza, Color colorPieza)
        {
            SerializedProperty e = lista.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("nombre").stringValue = nombre;
            e.FindPropertyRelative("color").colorValue = color;
            e.FindPropertyRelative("pantalon").colorValue = pantalon;
            e.FindPropertyRelative("conMangas").boolValue = mangas;
            e.FindPropertyRelative("pieza").enumValueIndex = (int)pieza;
            e.FindPropertyRelative("colorPieza").colorValue = colorPieza;
        }

        private static void LlenarImplantes(SerializedProperty lista)
        {
            lista.arraySize = 4;
            Implante(lista, 0, "Ninguno", ImplanteApariencia.Ninguno, Color.white);
            Implante(lista, 1, "Brazo cromado", ImplanteApariencia.BrazoCromado, new Color(0.8f, 0.82f, 0.88f));
            Implante(lista, 2, "Ojo LED rojo", ImplanteApariencia.OjoLed, new Color(1f, 0.05f, 0.05f));
            Implante(lista, 3, "Placa en la sien", ImplanteApariencia.PlacaSien, new Color(0.6f, 0.65f, 0.7f));
        }

        private static void Implante(SerializedProperty lista, int i, string nombre, ImplanteApariencia implante, Color color)
        {
            SerializedProperty e = lista.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("nombre").stringValue = nombre;
            e.FindPropertyRelative("implante").enumValueIndex = (int)implante;
            e.FindPropertyRelative("color").colorValue = color;
        }

        private static Material CrearMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(material, RutaMaterial);
            return material;
        }

        private static GameObject Primitiva(string nombre, PrimitiveType tipo, Transform padre, Vector3 posicion, Vector3 escala, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (padre != null) go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        private static void CrearPrefabCuerpo(CatalogoApariencia catalogo, Material m)
        {
            // Idempotente (T-192): regenerar cambia los ids internos del prefab, así que se conserva si ya tiene las piezas instaladas.
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabCuerpo);
            if (existente != null && existente.GetComponent<CuerpoJugador>() != null
                && existente.GetComponent<CuerpoJugador>().PiezasInstaladas.Length == 4)
            {
                return;
            }
            var raiz = new GameObject("CuerpoJugador");
            try
            {
                Transform t = raiz.transform;
                Renderer cabeza = Primitiva("Cabeza", PrimitiveType.Sphere, t, new Vector3(0f, 1.56f, 0f), new Vector3(0.26f, 0.28f, 0.26f), m).GetComponent<Renderer>();
                Renderer torso = Primitiva("Torso", PrimitiveType.Cube, t, new Vector3(0f, 1.1f, 0f), new Vector3(0.42f, 0.6f, 0.24f), m).GetComponent<Renderer>();
                Renderer[] brazos =
                {
                    Primitiva("BrazoIzq", PrimitiveType.Cube, t, new Vector3(-0.27f, 1.12f, 0f), new Vector3(0.12f, 0.55f, 0.14f), m).GetComponent<Renderer>(),
                    Primitiva("BrazoDer", PrimitiveType.Cube, t, new Vector3(0.27f, 1.12f, 0f), new Vector3(0.12f, 0.55f, 0.14f), m).GetComponent<Renderer>()
                };
                Renderer[] piernas =
                {
                    Primitiva("PiernaIzq", PrimitiveType.Cube, t, new Vector3(-0.1f, 0.4f, 0f), new Vector3(0.16f, 0.8f, 0.18f), m).GetComponent<Renderer>(),
                    Primitiva("PiernaDer", PrimitiveType.Cube, t, new Vector3(0.1f, 0.4f, 0f), new Vector3(0.16f, 0.8f, 0.18f), m).GetComponent<Renderer>()
                };
                GameObject[] peinados =
                {
                    Primitiva("Peinado_Rapado", PrimitiveType.Sphere, t, new Vector3(0f, 1.63f, -0.01f), new Vector3(0.29f, 0.2f, 0.29f), m),
                    Primitiva("Peinado_Cresta", PrimitiveType.Cube, t, new Vector3(0f, 1.73f, 0f), new Vector3(0.04f, 0.14f, 0.26f), m),
                    Primitiva("Peinado_Trenza", PrimitiveType.Capsule, t, new Vector3(0f, 1.5f, -0.15f), new Vector3(0.07f, 0.2f, 0.07f), m),
                    Primitiva("Peinado_Gorra", PrimitiveType.Cylinder, t, new Vector3(0f, 1.7f, 0.02f), new Vector3(0.3f, 0.04f, 0.34f), m)
                };
                GameObject[] piezasRopa =
                {
                    Primitiva("Ropa_Hombreras", PrimitiveType.Cube, t, new Vector3(0f, 1.42f, 0f), new Vector3(0.6f, 0.08f, 0.28f), m),
                    Primitiva("Ropa_Capucha", PrimitiveType.Sphere, t, new Vector3(0f, 1.42f, -0.12f), new Vector3(0.3f, 0.14f, 0.14f), m),
                    Primitiva("Ropa_Bandas", PrimitiveType.Cube, t, new Vector3(0f, 1.2f, 0f), new Vector3(0.44f, 0.06f, 0.26f), m),
                    Primitiva("Ropa_Tirantes", PrimitiveType.Cube, t, new Vector3(0f, 1.05f, 0.01f), new Vector3(0.3f, 0.3f, 0.26f), m)
                };
                GameObject[] implantes =
                {
                    null,
                    Primitiva("Implante_BrazoCromado", PrimitiveType.Cube, t, new Vector3(0.27f, 0.98f, 0f), new Vector3(0.15f, 0.4f, 0.17f), m),
                    Primitiva("Implante_OjoLed", PrimitiveType.Sphere, t, new Vector3(0.05f, 1.58f, 0.125f), new Vector3(0.06f, 0.06f, 0.06f), m),
                    Primitiva("Implante_PlacaSien", PrimitiveType.Cube, t, new Vector3(0.135f, 1.6f, 0.03f), new Vector3(0.02f, 0.08f, 0.07f), m)
                };

                // T-192: una pieza por espacio de implante instalado (apagadas; el cuerpo las prende según IImplantes).
                GameObject[] instaladas =
                {
                    Grupo("Instalado_Ojos", t, Primitiva("Banda", PrimitiveType.Cube, null, new Vector3(0f, 1.58f, 0.07f), new Vector3(0.25f, 0.04f, 0.12f), m)),
                    Grupo("Instalado_Brazos", t,
                        Primitiva("AntebrazoIzq", PrimitiveType.Cube, null, new Vector3(-0.27f, 0.95f, 0f), new Vector3(0.16f, 0.3f, 0.18f), m),
                        Primitiva("AntebrazoDer", PrimitiveType.Cube, null, new Vector3(0.27f, 0.95f, 0f), new Vector3(0.16f, 0.3f, 0.18f), m)),
                    Grupo("Instalado_Piernas", t,
                        Primitiva("EspinillaIzq", PrimitiveType.Cube, null, new Vector3(-0.1f, 0.2f, 0f), new Vector3(0.18f, 0.34f, 0.2f), m),
                        Primitiva("EspinillaDer", PrimitiveType.Cube, null, new Vector3(0.1f, 0.2f, 0f), new Vector3(0.18f, 0.34f, 0.2f), m)),
                    Grupo("Instalado_SistemaNervioso", t,
                        Primitiva("PlacaIzq", PrimitiveType.Cube, null, new Vector3(-0.06f, 1.46f, -0.095f), new Vector3(0.05f, 0.07f, 0.03f), m),
                        Primitiva("PlacaCentro", PrimitiveType.Cube, null, new Vector3(0f, 1.46f, -0.095f), new Vector3(0.05f, 0.07f, 0.03f), m),
                        Primitiva("PlacaDer", PrimitiveType.Cube, null, new Vector3(0.06f, 1.46f, -0.095f), new Vector3(0.05f, 0.07f, 0.03f), m))
                };
                GameObject[] franjas =
                {
                    Grupo("Franja_Ojos", t, Primitiva("Franja", PrimitiveType.Cube, null, new Vector3(0f, 1.607f, 0.07f), new Vector3(0.255f, 0.012f, 0.125f), m)),
                    Grupo("Franja_Brazos", t,
                        Primitiva("FranjaIzq", PrimitiveType.Cube, null, new Vector3(-0.27f, 1.0f, 0f), new Vector3(0.17f, 0.04f, 0.19f), m),
                        Primitiva("FranjaDer", PrimitiveType.Cube, null, new Vector3(0.27f, 1.0f, 0f), new Vector3(0.17f, 0.04f, 0.19f), m)),
                    Grupo("Franja_Piernas", t,
                        Primitiva("FranjaIzq", PrimitiveType.Cube, null, new Vector3(-0.1f, 0.3f, 0f), new Vector3(0.19f, 0.04f, 0.21f), m),
                        Primitiva("FranjaDer", PrimitiveType.Cube, null, new Vector3(0.1f, 0.3f, 0f), new Vector3(0.19f, 0.04f, 0.21f), m)),
                    Grupo("Franja_SistemaNervioso", t, Primitiva("Franja", PrimitiveType.Cube, null, new Vector3(0f, 1.5f, -0.095f), new Vector3(0.17f, 0.012f, 0.035f), m))
                };

                var cuerpo = raiz.AddComponent<CuerpoJugador>();
                var so = new SerializedObject(cuerpo);
                so.FindProperty("catalogo").objectReferenceValue = catalogo;
                so.FindProperty("cabeza").objectReferenceValue = cabeza;
                so.FindProperty("torso").objectReferenceValue = torso;
                AsignarLista(so.FindProperty("brazos"), brazos);
                AsignarLista(so.FindProperty("piernas"), piernas);
                AsignarLista(so.FindProperty("peinados"), peinados);
                AsignarLista(so.FindProperty("piezasRopa"), piezasRopa);
                AsignarLista(so.FindProperty("implantes"), implantes);
                AsignarLista(so.FindProperty("piezasInstaladas"), instaladas);
                AsignarLista(so.FindProperty("franjasInstaladas"), franjas);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabCuerpo);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        // Agrupa piezas bajo un objeto apagado, para prenderlas juntas.
        private static GameObject Grupo(string nombre, Transform padre, params GameObject[] piezas)
        {
            var grupo = new GameObject(nombre);
            grupo.transform.SetParent(padre, false);
            foreach (GameObject pieza in piezas)
            {
                pieza.transform.SetParent(grupo.transform, false);
            }
            grupo.SetActive(false);
            return grupo;
        }

        private static void AsignarLista(SerializedProperty lista, Object[] elementos)
        {
            lista.arraySize = elementos.Length;
            for (int i = 0; i < elementos.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = elementos[i];
            }
        }

        private static void CrearPrefabVista()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabVista) != null) return;
            var raiz = new GameObject("VistaPreviaApariencia");
            try
            {
                raiz.transform.position = new Vector3(0f, -1000f, 0f);
                var pivote = new GameObject("Pivote").transform;
                pivote.SetParent(raiz.transform, false);
                var cuerpoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabCuerpo);
                var cuerpo = (GameObject)PrefabUtility.InstantiatePrefab(cuerpoPrefab, pivote);

                var go = new GameObject("Camara");
                go.transform.SetParent(raiz.transform, false);
                go.transform.localPosition = new Vector3(0f, 0.95f, 3.7f);
                go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var camara = go.AddComponent<Camera>();
                camara.clearFlags = CameraClearFlags.SolidColor;
                camara.backgroundColor = new Color(0.04f, 0.05f, 0.09f, 1f);
                camara.fieldOfView = 30f;
                camara.nearClipPlane = 0.1f;
                camara.farClipPlane = 20f;
                camara.enabled = false;

                var luzGo = new GameObject("Luz");
                luzGo.transform.SetParent(raiz.transform, false);
                luzGo.transform.localRotation = Quaternion.Euler(35f, 150f, 0f);
                var luz = luzGo.AddComponent<Light>();
                luz.type = LightType.Directional;
                luz.intensity = 1.4f;
                luz.shadows = LightShadows.None;
                luz.enabled = false;

                var vista = raiz.AddComponent<VistaPreviaApariencia>();
                var so = new SerializedObject(vista);
                so.FindProperty("camara").objectReferenceValue = camara;
                so.FindProperty("luz").objectReferenceValue = luz;
                so.FindProperty("cuerpo").objectReferenceValue = cuerpo.GetComponent<CuerpoJugador>();
                so.FindProperty("pivote").objectReferenceValue = pivote;
                so.ApplyModifiedPropertiesWithoutUndo();

                var girar = new InputAction("Girar", InputActionType.Value);
                girar.AddCompositeBinding("1DAxis")
                    .With("Negative", "<Keyboard>/leftArrow")
                    .With("Positive", "<Keyboard>/rightArrow");
                vista.ConfigurarGiro(girar);

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabVista);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        /// <summary>Cuatro cuerpos lado a lado, renderizados en memoria a docs/capturas/T-123-1.png.</summary>
        [MenuItem("Mecha Cyberpunk/Capturar T-123")]
        public static void Capturar()
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cuerpoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabCuerpo);
            for (int i = 0; i < 4; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(cuerpoPrefab);
                go.transform.position = new Vector3(i * 1.2f, 0f, 0f);
                go.GetComponent<CuerpoJugador>().Aplicar(new AparienciaJugador(i, i, i));
            }
            var piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(1.8f, -0.05f, 0f);
            piso.transform.localScale = new Vector3(6f, 0.1f, 2f);
            piso.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(RutaPersonaje + "/Materiales/Greybox_Piso.mat");

            var luzGo = new GameObject("Luz");
            luzGo.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
            var luz = luzGo.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.4f;
            var camGo = new GameObject("Camara");
            camGo.transform.position = new Vector3(1.8f, 0.95f, 5.2f);
            camGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var camara = camGo.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.04f, 0.05f, 0.09f, 1f);
            camara.fieldOfView = 35f;

            RenderTexture rt = RenderTexture.GetTemporary(1600, 700, 24, RenderTextureFormat.ARGB32);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            var textura = new Texture2D(1600, 700, TextureFormat.RGB24, false);
            textura.ReadPixels(new Rect(0, 0, 1600, 700), 0, 0);
            textura.Apply();
            RenderTexture.active = null;
            camara.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            Directory.CreateDirectory(Path.GetDirectoryName(RutaCaptura));
            File.WriteAllBytes(RutaCaptura, textura.EncodeToPNG());
            Object.DestroyImmediate(textura);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Debug.Log("[Personaje] Captura T-123 guardada en " + RutaCaptura);
        }
    }
}
