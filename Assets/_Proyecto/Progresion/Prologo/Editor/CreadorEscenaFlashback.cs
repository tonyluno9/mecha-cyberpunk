using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Historia;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Crea lo de T-131: DatosFlashback_Base.asset, Flashback.prefab (director, cámara, subtítulos y servicios) y la escena de
    /// pruebas Pruebas_Flashback.unity (un cuarto greybox de la casa de la infancia con seis puntos de historia de prueba). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorEscenaFlashback.Crear
    /// </summary>
    public static class CreadorEscenaFlashback
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Flashback.unity";
        public const string RutaDatos = "Assets/_Proyecto/Progresion/Prologo/Datos/DatosFlashback_Base.asset";
        public const string RutaPrefab = "Assets/_Proyecto/Progresion/Prologo/Prefabs/Flashback.prefab";

        private const string RutaMateriales = "Assets/_Proyecto/Progresion/Prologo/Materiales";
        private const string RutaPantallasHistoria = "Assets/_Proyecto/UI/Prefabs/PantallasHistoria.prefab";
        private const string RutaServicios = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const string RutaDatosEscenasDemo = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Demo.asset";
        private const string RutaDatosEscenasPruebas = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Pruebas.asset";

        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const int OrdenSubtitulos = 100;
        private const float CampoDeVision = 60f;
        private const float AlturaOjos = 1.1f;
        private const float AltoMuro = 3f;
        private const float GrosorMuro = 0.2f;
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.9f);

        private static Material matPiso;
        private static Material matMuro;
        private static Material matMadera;
        private static Material matMecha;
        private static Material matCabina;
        private static Material matCamioneta;
        private static Material matCalle;

        private struct Plano
        {
            public string Id;
            public float Segundos;
            public string Subtitulo;
            public Vector3 Posicion;
            public Vector3 Mira;
        }

        // Textos y tiempos: propuesta (D-tony-68). Tony los cambia en DatosFlashback_Base.asset.
        private static readonly Plano[] Planos =
        {
            new Plano { Id = "Flashback_Plano_1", Segundos = 9f, Subtitulo = "Tenías ocho años la última vez que los viste.",
                Posicion = new Vector3(2.5f, AlturaOjos, -2.5f), Mira = new Vector3(-2f, 0.9f, -1.5f) },
            new Plano { Id = "Flashback_Plano_2", Segundos = 9f, Subtitulo = "Mamá le dio una vuelta más a cada tornillo del mecha.",
                Posicion = new Vector3(0f, AlturaOjos, 0.3f), Mira = new Vector3(-2.5f, 0.9f, 1.8f) },
            new Plano { Id = "Flashback_Plano_3", Segundos = 9f, Subtitulo = "Papá dijo que volverían antes de que amaneciera.",
                Posicion = new Vector3(-1.5f, AlturaOjos, 0f), Mira = new Vector3(4f, 1.3f, 0f) },
            new Plano { Id = "Flashback_Plano_4", Segundos = 10f, Subtitulo = "Se fueron en una camioneta sin placas, con un logo que no conocías.",
                Posicion = new Vector3(3f, AlturaOjos, 0.4f), Mira = new Vector3(9f, 1f, 0f) },
            new Plano { Id = "Flashback_Plano_5", Segundos = 8f, Subtitulo = "No volvieron.",
                Posicion = new Vector3(-3.5f, 0.9f, -2.6f), Mira = new Vector3(4f, 1.4f, 0f) },
            new Plano { Id = "Flashback_Plano_6", Segundos = 10f, Subtitulo = "Solo quedó el mecha. Y las preguntas.",
                Posicion = new Vector3(1f, 1f, -1f), Mira = new Vector3(-2.5f, 2.4f, 1.8f) },
        };

        [MenuItem("Mecha Cyberpunk/Crear T-131 flashback")]
        public static void Crear()
        {
            CrearMateriales();
            CrearDatos();
            CrearPrefab();
            CrearEscena();
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] T-131 flashback creado.");
        }

        // ---------- Datos ----------

        private static void CrearDatos()
        {
            if (AssetDatabase.LoadAssetAtPath<DatosFlashback>(RutaDatos) != null)
            {
                return;
            }
            List<PlanoFlashback> lista = new List<PlanoFlashback>();
            foreach (Plano plano in Planos)
            {
                lista.Add(new PlanoFlashback { idPunto = plano.Id, segundos = plano.Segundos, subtitulo = plano.Subtitulo });
            }
            DatosFlashback datos = ScriptableObject.CreateInstance<DatosFlashback>();
            datos.Fijar(lista, 0.5f, "Mantén ESPACIO para saltar", 1f);
            AssetDatabase.CreateAsset(datos, RutaDatos);
        }

        // ---------- Prefab ----------

        private static void CrearPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) != null)
            {
                return;
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Proyecto/Progresion/Prologo/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/Progresion/Prologo", "Prefabs");
            }
            GameObject raiz = new GameObject("Flashback");
            try
            {
                GameObject objetoCamara = new GameObject("Camara");
                objetoCamara.transform.SetParent(raiz.transform, false);
                objetoCamara.tag = "MainCamera";
                Camera camara = objetoCamara.AddComponent<Camera>();
                camara.fieldOfView = CampoDeVision;
                camara.nearClipPlane = 0.05f;
                camara.farClipPlane = 200f;
                camara.clearFlags = CameraClearFlags.SolidColor;
                camara.backgroundColor = new Color(0.04f, 0.04f, 0.07f);
                objetoCamara.AddComponent<AudioListener>();

                PantallaSubtitulos subtitulos = CrearSubtitulos(raiz.transform);

                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallasHistoria), raiz.transform);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaServicios), raiz.transform);

                DirectorFlashback director = raiz.AddComponent<DirectorFlashback>();
                SerializedObject so = new SerializedObject(director);
                so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosFlashback>(RutaDatos);
                so.FindProperty("datosEscenas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosEscenas>(RutaDatosEscenasDemo);
                so.FindProperty("camara").objectReferenceValue = camara;
                so.FindProperty("subtitulos").objectReferenceValue = subtitulos;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static PantallaSubtitulos CrearSubtitulos(Transform padre)
        {
            GameObject raiz = Hijo("Subtitulos", padre);
            Canvas canvas = raiz.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = OrdenSubtitulos;
            CanvasScaler escala = raiz.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(AnchoReferencia, AltoReferencia);
            escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escala.matchWidthOrHeight = 0.5f;

            Text linea = CrearTexto("Linea", raiz.transform, 54, FontStyle.Bold, Color.white, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1600f, 220f));
            linea.horizontalOverflow = HorizontalWrapMode.Wrap;
            Outline contorno = linea.gameObject.AddComponent<Outline>();
            contorno.effectColor = ColorSombra;
            contorno.effectDistance = new Vector2(2f, -2f);

            Text aviso = CrearTexto("Aviso", raiz.transform, 26, FontStyle.Normal, new Color(0.8f, 0.85f, 0.9f, 0.9f), TextAnchor.LowerRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 36f), new Vector2(700f, 50f));

            PantallaSubtitulos pantalla = raiz.AddComponent<PantallaSubtitulos>();
            SerializedObject so = new SerializedObject(pantalla);
            so.FindProperty("textoSubtitulo").objectReferenceValue = linea;
            so.FindProperty("textoAviso").objectReferenceValue = aviso;
            so.ApplyModifiedPropertiesWithoutUndo();
            return pantalla;
        }

        private static Text CrearTexto(string nombre, Transform padre, int tamano, FontStyle estilo, Color color, TextAnchor alineacion,
            Vector2 ancla, Vector2 pivote, Vector2 posicion, Vector2 tamanoRect)
        {
            GameObject objeto = Hijo(nombre, padre);
            RectTransform rect = (RectTransform)objeto.transform;
            rect.anchorMin = rect.anchorMax = ancla;
            rect.pivot = pivote;
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamanoRect;
            Text texto = objeto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = tamano;
            texto.fontStyle = estilo;
            texto.alignment = alineacion;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.color = color;
            Shadow sombra = objeto.AddComponent<Shadow>();
            sombra.effectColor = ColorSombra;
            sombra.effectDistance = new Vector2(3f, -3f);
            return texto;
        }

        // ---------- Escena ----------

        private static void CrearEscena()
        {
            string firmaActual = null;
            if (File.Exists(RutaEscena))
            {
                firmaActual = Firma(EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single));
            }

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ArmarLuz();
            ArmarCuarto();
            ArmarMecha();
            ArmarExterior();
            ArmarPuntos();
            ArmarDirector();

            string firmaNueva = Firma(escena);
            if (firmaActual != null && firmaActual == firmaNueva)
            {
                Debug.Log("[Progresion] Pruebas_Flashback ya estaba completa; no se guarda de nuevo.");
                return;
            }
            EditorSceneManager.SaveScene(escena, RutaEscena);
            Debug.Log("[Progresion] Pruebas_Flashback creada.");
        }

        private static void ArmarLuz()
        {
            GameObject sol = new GameObject("Luz direccional");
            Light luz = sol.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.color = new Color(1f, 0.85f, 0.65f);
            luz.intensity = 1.1f;
            sol.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            GameObject foco = new GameObject("Foco del cuarto");
            Light puntual = foco.AddComponent<Light>();
            puntual.type = LightType.Point;
            puntual.color = new Color(1f, 0.8f, 0.5f);
            puntual.intensity = 1.5f;
            puntual.range = 9f;
            foco.transform.position = new Vector3(0f, 2.6f, 0f);
        }

        // Cuarto de 8 x 6 m (x de -4 a 4, z de -3 a 3). La puerta es el hueco del muro este, con la hoja abierta hacia afuera.
        private static void ArmarCuarto()
        {
            Caja("Piso", new Vector3(0f, -0.1f, 0f), new Vector3(8.4f, 0.2f, 6.4f), matPiso, true);
            Caja("Muro norte", new Vector3(0f, AltoMuro / 2f, 3.1f), new Vector3(8.4f, AltoMuro, GrosorMuro), matMuro, true);
            Caja("Muro sur", new Vector3(0f, AltoMuro / 2f, -3.1f), new Vector3(8.4f, AltoMuro, GrosorMuro), matMuro, true);
            Caja("Muro oeste", new Vector3(-4.1f, AltoMuro / 2f, 0f), new Vector3(GrosorMuro, AltoMuro, 6.4f), matMuro, true);
            Caja("Muro este A", new Vector3(4.1f, AltoMuro / 2f, 1.9f), new Vector3(GrosorMuro, AltoMuro, 2.6f), matMuro, true);
            Caja("Muro este B", new Vector3(4.1f, AltoMuro / 2f, -1.9f), new Vector3(GrosorMuro, AltoMuro, 2.6f), matMuro, true);
            Caja("Dintel", new Vector3(4.1f, 2.6f, 0f), new Vector3(GrosorMuro, 0.8f, 1.2f), matMuro, true);
            Caja("Puerta abierta", new Vector3(4.9f, 1.1f, 0.9f), new Vector3(1.1f, 2.2f, 0.08f), matMadera, false);
            Caja("Mesa tablero", new Vector3(-2f, 0.75f, -1.5f), new Vector3(1.8f, 0.08f, 1f), matMadera, true);
            Caja("Mesa pata 1", new Vector3(-2.8f, 0.36f, -1.9f), new Vector3(0.08f, 0.72f, 0.08f), matMadera, false);
            Caja("Mesa pata 2", new Vector3(-1.2f, 0.36f, -1.9f), new Vector3(0.08f, 0.72f, 0.08f), matMadera, false);
            Caja("Mesa pata 3", new Vector3(-2.8f, 0.36f, -1.1f), new Vector3(0.08f, 0.72f, 0.08f), matMadera, false);
            Caja("Mesa pata 4", new Vector3(-1.2f, 0.36f, -1.1f), new Vector3(0.08f, 0.72f, 0.08f), matMadera, false);
        }

        // El mecha viejo, de cajas, en la esquina noroeste.
        private static void ArmarMecha()
        {
            Vector3 c = new Vector3(-2.5f, 0f, 1.8f);
            Caja("Mecha pie izquierdo", c + new Vector3(-0.45f, 0.1f, 0f), new Vector3(0.5f, 0.2f, 0.8f), matMecha, true);
            Caja("Mecha pie derecho", c + new Vector3(0.45f, 0.1f, 0f), new Vector3(0.5f, 0.2f, 0.8f), matMecha, true);
            Caja("Mecha pierna izquierda", c + new Vector3(-0.45f, 0.8f, 0f), new Vector3(0.35f, 1.4f, 0.35f), matMecha, false);
            Caja("Mecha pierna derecha", c + new Vector3(0.45f, 0.8f, 0f), new Vector3(0.35f, 1.4f, 0.35f), matMecha, false);
            Caja("Mecha torso", c + new Vector3(0f, 1.95f, 0f), new Vector3(1.4f, 1f, 0.9f), matMecha, false);
            Caja("Mecha cabina", c + new Vector3(0f, 2.65f, -0.1f), new Vector3(0.7f, 0.5f, 0.6f), matCabina, false);
            Caja("Mecha brazo izquierdo", c + new Vector3(-1f, 1.9f, 0f), new Vector3(0.3f, 1.1f, 0.3f), matMecha, false);
            Caja("Mecha brazo derecho", c + new Vector3(1f, 1.9f, 0f), new Vector3(0.3f, 1.1f, 0.3f), matMecha, false);
        }

        // Afuera, al este: la calle y una camioneta de cajas.
        private static void ArmarExterior()
        {
            Caja("Calle", new Vector3(10f, -0.1f, 0f), new Vector3(10f, 0.2f, 16f), matCalle, true);
            Caja("Camioneta caja", new Vector3(9.4f, 0.9f, -0.9f), new Vector3(3.2f, 1.1f, 1.8f), matCamioneta, false);
            Caja("Camioneta cabina", new Vector3(9.4f, 1.4f, 1.3f), new Vector3(1.8f, 1.2f, 1.6f), matCamioneta, false);
            Caja("Camioneta llantas", new Vector3(9.4f, 0.3f, 0f), new Vector3(3.2f, 0.6f, 2f), matCabina, false);
        }

        private static void ArmarPuntos()
        {
            foreach (Plano plano in Planos)
            {
                GameObject punto = new GameObject("PuntoHistoria " + plano.Id);
                punto.transform.SetPositionAndRotation(plano.Posicion, Quaternion.LookRotation(plano.Mira - plano.Posicion));
                SerializedObject so = new SerializedObject(punto.AddComponent<PuntoHistoriaDePrueba>());
                so.FindProperty("id").stringValue = plano.Id;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ArmarDirector()
        {
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            SerializedObject so = new SerializedObject(instancia.GetComponent<DirectorFlashback>());
            so.FindProperty("datosEscenas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosEscenas>(RutaDatosEscenasPruebas);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Utilidades ----------

        private static GameObject Hijo(string nombre, Transform padre)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            return objeto;
        }

        private static void Caja(string nombre, Vector3 centro, Vector3 tamano, Material material, bool conCollider)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.name = nombre;
            caja.transform.position = centro;
            caja.transform.localScale = tamano;
            caja.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!conCollider)
            {
                Object.DestroyImmediate(caja.GetComponent<Collider>());
            }
        }

        private static void CrearMateriales()
        {
            if (!AssetDatabase.IsValidFolder(RutaMateriales))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/Progresion/Prologo", "Materiales");
            }
            matPiso = CrearMaterial("Flashback_Piso", new Color(0.35f, 0.25f, 0.18f));
            matMuro = CrearMaterial("Flashback_Muro", new Color(0.75f, 0.68f, 0.55f));
            matMadera = CrearMaterial("Flashback_Madera", new Color(0.45f, 0.28f, 0.14f));
            matMecha = CrearMaterial("Flashback_Mecha", new Color(0.55f, 0.58f, 0.62f));
            matCabina = CrearMaterial("Flashback_Cabina", new Color(0.15f, 0.2f, 0.28f));
            matCamioneta = CrearMaterial("Flashback_Camioneta", new Color(0.2f, 0.22f, 0.25f));
            matCalle = CrearMaterial("Flashback_Calle", new Color(0.12f, 0.12f, 0.14f));
        }

        private static Material CrearMaterial(string nombre, Color color)
        {
            string ruta = RutaMateriales + "/" + nombre + ".mat";
            Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (existente != null)
            {
                return existente;
            }
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, ruta);
            return material;
        }

        // Texto con todo lo que importa de la escena: así, regenerar una escena igual no cambia el archivo.
        private static string Firma(Scene escena)
        {
            StringBuilder texto = new StringBuilder();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                FirmarObjeto(raiz.transform, texto);
            }
            return texto.ToString();
        }

        private static void FirmarObjeto(Transform objeto, StringBuilder texto)
        {
            texto.Append(objeto.name).Append('|').Append(objeto.gameObject.activeSelf).Append('|')
                .Append(objeto.position).Append(objeto.rotation).Append('|')
                .Append(PrefabUtility.GetCorrespondingObjectFromSource(objeto.gameObject)).Append('\n');
            foreach (Component componente in objeto.GetComponents<Component>())
            {
                if (componente == null)
                {
                    continue;
                }
                texto.Append(componente.GetType().Name).Append('\n');
                if (componente is Transform || componente is RectTransform)
                {
                    continue;
                }
                SerializedObject so = new SerializedObject(componente);
                SerializedProperty propiedad = so.GetIterator();
                while (propiedad.NextVisible(true))
                {
                    if (propiedad.propertyPath.EndsWith("m_Id"))
                    {
                        continue;
                    }
                    if (propiedad.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        Object referencia = propiedad.objectReferenceValue;
                        texto.Append(propiedad.propertyPath).Append('=').Append(referencia != null ? referencia.name : "null").Append('\n');
                    }
                    else if (propiedad.propertyType != SerializedPropertyType.Generic)
                    {
                        texto.Append(propiedad.propertyPath).Append('=').Append(propiedad.boxedValue).Append('\n');
                    }
                }
            }
            foreach (Transform hijo in objeto)
            {
                FirmarObjeto(hijo, texto);
            }
        }
    }
}
