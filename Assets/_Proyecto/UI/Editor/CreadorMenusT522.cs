using MechaCyberpunk.UI.Guardado;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.UI.Opciones;
using MechaCyberpunk.UI.Tiendas;
using MechaCyberpunk.UI.Titulo;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-522: rehace con el kit de piezas los prefabs de menú que llevan sus hijos guardados: título y creación de pandilla, pausa, opciones, ranuras
    /// (se arman con <c>Armar()</c>, así que basta repetirlo) y la tienda de El Mercado y el CIC (se le pone el marco 9-slice de UI01 detrás del
    /// contenido y las fuentes Teko y Rajdhani). El mapa, los créditos y los avisos se arman al abrirse y no necesitan prefab. Idempotente.
    /// No toca los muñecos ni lo descartado (teléfono, guarida, negocios).
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorMenusT522.Crear
    /// </summary>
    public static class CreadorMenusT522
    {
        private const string RutaTienda = CreadorMostradorTienda.RutaPantalla;
        private const string NombreMarco = "MarcoTienda";
        private static readonly Vector2 TamanoMarcoTienda = new Vector2(1560f, 980f);

        [MenuItem("Mecha Cyberpunk/Crear T-522 Menús con piezas")]
        public static void Crear()
        {
            KitUI.FijarEstilo(CreadorKitUI.AsegurarEstilo());
            Rehacer<PantallaTitulo>("PantallaTitulo", CreadorPantallaTitulo.RutaTitulo, p => p.Armar());
            RehacerCreacion();
            Rehacer<PantallaPausa>("PantallaPausa", CreadorPantallaPausa.RutaPrefab, p => p.Armar());
            Rehacer<PantallaOpciones>("PantallaOpciones", CreadorPantallaOpciones.RutaPrefab, p => p.Armar());
            Rehacer<PantallaRanuras>("PantallaRanuras", CreadorPantallaRanuras.RutaPrefab, p =>
            {
                p.Armar();
                p.gameObject.AddComponent<AvisoGuardado>().Armar();
            });
            VestirTienda();
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-522: menús rehechos con las piezas.");
        }

        private static void Rehacer<T>(string nombre, string ruta, System.Action<T> armar) where T : Component
        {
            GameObject raiz = new GameObject(nombre);
            try
            {
                armar(raiz.AddComponent<T>());
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void RehacerCreacion()
        {
            GameObject raiz = new GameObject("PantallaCreacionPandilla");
            try
            {
                PantallaCreacionPandilla creacion = raiz.AddComponent<PantallaCreacionPandilla>();
                creacion.ConfigurarModelos(new[]
                {
                    AssetDatabase.LoadAssetAtPath<GameObject>(CreadorPantallaTitulo.RutaModeloA),
                    AssetDatabase.LoadAssetAtPath<GameObject>(CreadorPantallaTitulo.RutaModeloB),
                });
                creacion.Armar();
                PrefabUtility.SaveAsPrefabAsset(raiz, CreadorPantallaTitulo.RutaCreacion);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        // El marco de UI01 entre el fondo oscuro de pantalla completa y los textos, y las fuentes del estilo en todos los textos.
        private static void VestirTienda()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaTienda);
            try
            {
                Transform contenido = raiz.transform.Find("Contenido");
                Transform previo = contenido.Find(NombreMarco);
                if (previo != null)
                {
                    Object.DestroyImmediate(previo.gameObject);
                }
                Image marco = KitUI.CrearPanelMarco(contenido, NombreMarco, KitUI.AlfaPanelMenu);
                KitUI.Colocar(marco.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, TamanoMarcoTienda);
                marco.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                marco.transform.SetSiblingIndex(1);
                foreach (Text texto in raiz.GetComponentsInChildren<Text>(true))
                {
                    bool encabezado = texto.name == "Titulo" || texto.name == "Dinero";
                    texto.font = KitUI.Estilo.FuenteDe(encabezado ? RolTexto.Encabezado : RolTexto.Texto);
                    texto.fontStyle = FontStyle.Normal;
                    KitUI.Sombrear(texto);
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaTienda);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
