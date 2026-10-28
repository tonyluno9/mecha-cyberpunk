using System.IO;
using MechaCyberpunk.Casino.Datos;
using MechaCyberpunk.Casino.Tragamonedas;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Casino.Editor
{
    /// <summary>
    /// Crea Casino/Prefabs/Tragamonedas.prefab (T-301): cuerpo low-poly, pantalla con tres carretes de uGUI Text en World Space y luz de neón.
    /// Reconstruye el prefab cada vez (conserva su GUID). Requiere DatosTragamonedas_Base.asset (T-300).
    /// Batchmode: -executeMethod MechaCyberpunk.Casino.Editor.CreadorTragamonedas.Crear
    /// </summary>
    public static class CreadorTragamonedas
    {
        public const string RutaPrefab = "Assets/_Proyecto/Casino/Prefabs/Tragamonedas.prefab";
        private const string RutaMateriales = "Assets/_Proyecto/Casino/Materiales";
        private const string ShaderLit = "Universal Render Pipeline/Lit";
        private const string ShaderUnlit = "Universal Render Pipeline/Unlit";

        // Medidas de la máquina (m). El frente mira a +Z.
        private static readonly Vector3 TamanioCuerpo = new Vector3(0.9f, 1.7f, 0.7f);
        private static readonly Vector3 TamanioBase = new Vector3(1f, 0.15f, 0.8f);
        private static readonly Vector3 TamanioCorona = new Vector3(0.96f, 0.3f, 0.76f);
        private static readonly Vector3 TamanioPantalla = new Vector3(0.7f, 0.32f, 0.04f);
        private static readonly Vector3 TamanioPalanca = new Vector3(0.06f, 0.5f, 0.06f);
        private static readonly Vector3 TamanioBola = new Vector3(0.14f, 0.14f, 0.14f);
        private static readonly Vector3 TamanioTira = new Vector3(0.7f, 0.05f, 0.03f);
        private const float AlturaPantalla = 1.25f;
        private const float AlturaMarcador = 1.03f;
        private const float AnchoCarrete = 110f;
        private const float AltoCarrete = 100f;
        private const float EscalaCanvas = 0.002f;
        private const int TamanioLetra = 16;
        private const int TamanioLetraMarcador = 20;
        private const float AltoMarcador = 40f;
        private const float AnchoCanvas = 330f;
        private const float AltoCanvas = 130f;
        private static readonly Color ColorCuerpo = new Color(0.12f, 0.1f, 0.18f);
        private static readonly Color ColorCorona = new Color(0.55f, 0.08f, 0.35f);
        private static readonly Color ColorPantalla = new Color(0.03f, 0.05f, 0.08f);
        private static readonly Color ColorNeon = new Color(1f, 0.1f, 0.65f);
        private static readonly Color ColorTexto = new Color(0.4f, 1f, 0.9f);
        private static readonly Color ColorMarcador = new Color(1f, 0.85f, 0.2f);
        private static readonly Color ColorLuz = new Color(1f, 0.2f, 0.7f);

        [MenuItem("Mecha Cyberpunk/Crear T-301")]
        public static void Crear()
        {
            DatosTragamonedas datos = AssetDatabase.LoadAssetAtPath<DatosTragamonedas>(CreadorDatosTragamonedas.RutaDatos);
            if (datos == null)
            {
                Debug.LogError("[Casino] Falta " + CreadorDatosTragamonedas.RutaDatos + "; corre CreadorDatosTragamonedas.Crear primero.");
                return;
            }
            Directory.CreateDirectory(Path.GetFullPath(RutaMateriales));
            Material cuerpo = CrearMaterial(ShaderLit, "Tragamonedas_Cuerpo", ColorCuerpo, false);
            Material corona = CrearMaterial(ShaderLit, "Tragamonedas_Corona", ColorCorona, false);
            Material pantalla = CrearMaterial(ShaderUnlit, "Tragamonedas_Pantalla", ColorPantalla, false);
            Material neon = CrearMaterial(ShaderLit, "Tragamonedas_Neon", ColorNeon, true);

            GameObject raiz = new GameObject("Tragamonedas");
            try
            {
                Pieza(raiz.transform, "Base", new Vector3(0f, TamanioBase.y * 0.5f, 0f), TamanioBase, cuerpo);
                Pieza(raiz.transform, "Cuerpo", new Vector3(0f, TamanioBase.y + TamanioCuerpo.y * 0.5f, 0f), TamanioCuerpo, cuerpo);
                Pieza(raiz.transform, "Corona", new Vector3(0f, TamanioBase.y + TamanioCuerpo.y + TamanioCorona.y * 0.5f, 0f), TamanioCorona, corona);
                float frente = TamanioCuerpo.z * 0.5f;
                Pieza(raiz.transform, "Pantalla", new Vector3(0f, AlturaPantalla, frente), TamanioPantalla, pantalla);
                Pieza(raiz.transform, "TiraNeonArriba", new Vector3(0f, AlturaPantalla + TamanioPantalla.y * 0.5f + TamanioTira.y, frente), TamanioTira, neon);
                Pieza(raiz.transform, "TiraNeonAbajo", new Vector3(0f, AlturaPantalla - TamanioPantalla.y * 0.5f - TamanioTira.y, frente), TamanioTira, neon);
                float ladoX = TamanioCuerpo.x * 0.5f + TamanioPalanca.x * 0.5f;
                Pieza(raiz.transform, "Palanca", new Vector3(ladoX, AlturaPantalla - TamanioPalanca.y * 0.2f, 0f), TamanioPalanca, cuerpo);
                Pieza(raiz.transform, "PalancaBola", new Vector3(ladoX, AlturaPantalla + TamanioPalanca.y * 0.3f, 0f), TamanioBola, neon);

                GameObject luzObjeto = new GameObject("LuzNeon");
                luzObjeto.transform.SetParent(raiz.transform, false);
                luzObjeto.transform.localPosition = new Vector3(0f, TamanioBase.y + TamanioCuerpo.y + TamanioCorona.y, frente + 0.4f);
                Light luz = luzObjeto.AddComponent<Light>();
                luz.type = LightType.Point;
                luz.color = ColorLuz;
                luz.range = 3.5f;
                luz.intensity = 1.5f;

                Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                GameObject canvasObjeto = new GameObject("PantallaCanvas", typeof(RectTransform), typeof(Canvas));
                canvasObjeto.transform.SetParent(raiz.transform, false);
                canvasObjeto.transform.localPosition = new Vector3(0f, AlturaPantalla, frente + TamanioPantalla.z * 0.5f + 0.002f);
                canvasObjeto.transform.localScale = Vector3.one * EscalaCanvas;
                // El texto de un Canvas se lee desde su cara -Z; se gira para que se lea desde el frente (+Z) de la máquina.
                canvasObjeto.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                canvasObjeto.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                RectTransform rectCanvas = canvasObjeto.GetComponent<RectTransform>();
                rectCanvas.sizeDelta = new Vector2(AnchoCanvas, AltoCanvas);

                Text[] carretes = new Text[CalculoTragamonedas.Carretes];
                for (int i = 0; i < carretes.Length; i++)
                {
                    float x = (i - (carretes.Length - 1) * 0.5f) * AnchoCarrete;
                    carretes[i] = Texto(canvasObjeto.transform, "Carrete" + (i + 1), fuente, new Vector2(x, AltoMarcador * 0.5f),
                        new Vector2(AnchoCarrete, AltoCarrete), TamanioLetra, ColorTexto);
                }
                Text marcador = Texto(canvasObjeto.transform, "Marcador", fuente, new Vector2(0f, -AltoCarrete * 0.5f + AltoMarcador * 0.5f - 6f),
                    new Vector2(AnchoCanvas, AltoMarcador), TamanioLetraMarcador, ColorMarcador);

                TragamonedasMundo maquina = raiz.AddComponent<TragamonedasMundo>();
                SerializedObject so = new SerializedObject(maquina);
                so.FindProperty("datos").objectReferenceValue = datos;
                SerializedProperty lista = so.FindProperty("carretes");
                lista.arraySize = carretes.Length;
                for (int i = 0; i < carretes.Length; i++)
                {
                    lista.GetArrayElementAtIndex(i).objectReferenceValue = carretes[i];
                }
                so.FindProperty("marcador").objectReferenceValue = marcador;
                so.FindProperty("luzNeon").objectReferenceValue = luz;
                so.ApplyModifiedPropertiesWithoutUndo();

                Directory.CreateDirectory(Path.GetFullPath(Path.GetDirectoryName(RutaPrefab)));
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Casino] T-301 creada: " + RutaPrefab);
        }

        private static void Pieza(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Text Texto(Transform padre, string nombre, Font fuente, Vector2 posicion, Vector2 tamanio, int letra, Color color)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(padre, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamanio;
            Text texto = go.GetComponent<Text>();
            texto.font = fuente;
            texto.fontSize = letra;
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.color = color;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.raycastTarget = false;
            return texto;
        }

        // Crea o actualiza el material; así el prefab conserva la referencia al mismo asset.
        private static Material CrearMaterial(string shader, string nombre, Color color, bool emisivo)
        {
            string ruta = RutaMateriales + "/" + nombre + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetColor("_BaseColor", color);
            if (emisivo)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                material.SetColor("_EmissionColor", color * 2f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
