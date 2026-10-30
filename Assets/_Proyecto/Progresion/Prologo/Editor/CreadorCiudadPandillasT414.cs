using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-414 del creador de la ciudad de pandillas: los nombres del documento del juego (SUBURBIOS, SKULLS, SNAKES, EL MERCADO, I+D+I,
    /// EL FOX, VIVACOIN) en los assets de datos y los dos letreros de neón de la cantina y el casino bajo GTA_T414. Solo cambia texto visible:
    /// los ids, enums y nombres de objetos no cambian (D-tony-249). Idempotente.
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT414 = "GTA_T414";
        public const string TextoLetreroBar = "EL FOX";
        public const string TextoLetreroCasino = "VIVACOIN";

        private static readonly Color ColorLetreroBar = new Color(0.1f, 0.9f, 1f);
        private static readonly Color ColorLetreroCasino = new Color(1f, 0.5f, 0.1f);

        [MenuItem("Mecha Cyberpunk/Pandillas/T-414 Nombres y Biits")]
        public static void T414()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T414Interno();
            Guardar(escena);
        }

        private static void T414Interno()
        {
            // 1. Textos de los assets de datos (por SerializedObject; los ids y los nombres de archivo no cambian).
            PonerTextoT414("Assets/_Proyecto/Progresion/Datos/Territorio/DatosZona_fosa.asset", "nombre", "SUBURBIOS");
            PonerTextoT414("Assets/_Proyecto/Mundo/Datos/DatosLugar_MercadoNegro.asset", "nombre", "El Mercado");
            PonerTextoT414("Assets/_Proyecto/Mundo/Datos/DatosLugar_Clinica.asset", "nombre", "I+D+I");
            PonerTextoT414("Assets/_Proyecto/Mundo/Datos/DatosLugar_Bar.asset", "nombre", "El Fox");
            PonerTextoT414("Assets/_Proyecto/Mundo/Datos/DatosLugar_Casino.asset", "nombre", "VivaCoin");
            PonerTextoT414("Assets/_Proyecto/Economia/Datos/DatosTienda_MercadoNegro.asset", "nombre", "El Mercado");
            PonerTextoT414("Assets/_Proyecto/Economia/Datos/DatosTienda_Clinica.asset", "nombre", "I+D+I");
            PonerTextoT414("Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset", "nombre", "SKULL");
            PonerTextoT414("Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeCalaveras.asset", "nombre", "JEFE SKULL");
            PonerTextoT414("Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Viboras.asset", "nombre", "SNAKE");
            PonerTextoT414("Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeViboras.asset", "nombre", "JEFE SNAKE");
            PonerTextoT414("Assets/_Proyecto/UI/Datos/ConfigPantallaClinica_Base.asset", "textoTitulo", "I+D+I");
            PonerTextoT414("Assets/_Proyecto/UI/Datos/ConfigPantallaClinica_Base.asset", "textoMostrador", "I+D+I");
            PonerTextoT414("Assets/_Proyecto/UI/Datos/ConfigPantallaClinica_Base.asset", "textoCerrado", "I+D+I · CERRADA");
            PonerTextoT414("Assets/_Proyecto/UI/Datos/ConfigHudAPie_Base.asset", "nombresMotivo.Array.data[6]", "VIVACOIN");
            PonerTextoT414("Assets/_Proyecto/UI/Datos/ConfigHudAPie_Base.asset", "nombresMotivo.Array.data[7]", "I+D+I");
            RenombrarTextosPrefabT414("Assets/_Proyecto/UI/Prefabs/MostradorMercadoNegro.prefab", "MERCADO NEGRO", "EL MERCADO");
            RenombrarTextosPrefabT414("Assets/_Proyecto/UI/Prefabs/MostradorClinica.prefab", "CLÍNICA", "I+D+I");
            AssetDatabase.SaveAssets();

            // 2. Letreros de neón de El Fox y VivaCoin: los modelos viejos se apagan (override de la instancia, sin borrar) y en su lugar va texto 3D.
            GameObject anterior = BuscarRaiz(RaizT414);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            Transform raiz = new GameObject(RaizT414).transform;
            Material neonBar = MaterialT407("T414_NeonBar", ColorLetreroBar * 0.4f, ColorLetreroBar * 2f);
            Material neonCasino = MaterialT407("T414_NeonCasino", ColorLetreroCasino * 0.4f, ColorLetreroCasino * 2f);
            Material fondo = MaterialT407("T414_FondoLetrero", new Color(0.05f, 0.05f, 0.07f), Color.black);
            LetreroNeonT414(raiz, "Letrero_Cantina", "Lugar_Bar", "LetreroElFox", TextoLetreroBar, ColorLetreroBar, neonBar, fondo);
            LetreroNeonT414(raiz, "Letrero_Casino", "Lugar_Casino", "LetreroVivaCoin", TextoLetreroCasino, ColorLetreroCasino, neonCasino, fondo);
        }

        private static void PonerTextoT414(string ruta, string campo, string valor)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(ruta);
            if (asset == null)
            {
                Debug.LogWarning("[T-414] No existe " + ruta);
                return;
            }
            SerializedObject so = new SerializedObject(asset);
            SerializedProperty propiedad = so.FindProperty(campo);
            if (propiedad == null)
            {
                Debug.LogWarning("[T-414] " + ruta + " no tiene " + campo);
                return;
            }
            if (propiedad.stringValue != valor)
            {
                propiedad.stringValue = valor;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
        }

        // El letrero del mostrador del mercado está escrito en su prefab: se cambia el texto sin regenerarlo.
        private static void RenombrarTextosPrefabT414(string ruta, string viejo, string nuevo)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            bool cambio = false;
            foreach (Text t in contenido.GetComponentsInChildren<Text>(true))
            {
                if (t.text == viejo)
                {
                    t.text = nuevo;
                    cambio = true;
                }
            }
            foreach (TextMesh t in contenido.GetComponentsInChildren<TextMesh>(true))
            {
                if (t.text == viejo)
                {
                    t.text = nuevo;
                    cambio = true;
                }
            }
            if (cambio)
            {
                PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
            }
            PrefabUtility.UnloadPrefabContents(contenido);
        }

        private static void LetreroNeonT414(Transform raiz, string modelo, string lugarNombre, string nombre, string texto, Color color, Material neon, Material fondo)
        {
            Transform viejo = BuscarTransform(modelo);
            Transform lugar = BuscarTransform(lugarNombre);
            if (viejo == null || lugar == null)
            {
                Debug.LogWarning("[T-414] Falta " + (viejo == null ? modelo : lugarNombre));
                return;
            }
            Bounds limites = new Bounds(viejo.position, Vector3.zero);
            bool primero = true;
            foreach (Renderer r in viejo.GetComponentsInChildren<Renderer>(true))
            {
                if (primero)
                {
                    limites = r.bounds;
                    primero = false;
                }
                else
                {
                    limites.Encapsulate(r.bounds);
                }
            }
            viejo.gameObject.SetActive(false);

            // Hacia la calle: de lo que hay dentro del local (la posición del lugar) hacia su puerta.
            Vector3 afuera = lugar.forward;
            foreach (Transform hijo in lugar.GetComponentsInChildren<Transform>(true))
            {
                if (hijo.name == "Puerta")
                {
                    Vector3 haciaCalle = hijo.position - lugar.position;
                    haciaCalle.y = 0f;
                    if (haciaCalle.sqrMagnitude > 0.01f)
                    {
                        afuera = haciaCalle.normalized;
                    }
                    break;
                }
            }
            Vector3 lado = Vector3.Cross(Vector3.up, afuera);
            float ancho = Mathf.Max(Mathf.Abs(Vector3.Dot(limites.size, lado)), 1.6f);
            float alto = Mathf.Clamp(limites.size.y, 0.8f, 1.6f);
            float profundidad = Mathf.Abs(Vector3.Dot(limites.extents, afuera));
            Vector3 centro = limites.center + afuera * (profundidad + 0.06f);
            Quaternion giro = Quaternion.LookRotation(-afuera, Vector3.up);

            CajaT407(raiz, nombre + "_Fondo", centro, new Vector3(ancho, alto, 0.05f), fondo, false, giro);
            Text t = TextoT407(raiz, nombre, texto, centro + afuera * 0.04f, giro, alto * 0.7f, color, ancho);
            t.fontSize = Mathf.Min(t.fontSize, Mathf.RoundToInt(ancho / EscalaTextoMundo / (texto.Length * 0.75f)));
            CajaT407(raiz, nombre + "_Neon", centro + Vector3.down * (alto * 0.5f - 0.04f) + afuera * 0.04f, new Vector3(ancho, 0.05f, 0.05f), neon, false, giro);
            CajaT407(raiz, nombre + "_NeonArriba", centro + Vector3.up * (alto * 0.5f - 0.04f) + afuera * 0.04f, new Vector3(ancho, 0.05f, 0.05f), neon, false, giro);
        }
    }
}
