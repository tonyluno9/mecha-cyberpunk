using MechaCyberpunk.Economia;
using MechaCyberpunk.Pelea.Flujo;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Pelea.Editor
{
    /// <summary>
    /// T-106: da a FlujoPelea.prefab el premio de la casera y a FlujoPeleaTutorial.prefab el del campeonato, y les agrega la
    /// línea del premio en la pantalla (entre el resultado y la revancha). Idempotente y conserva todo lo demás de los prefabs.
    /// Batchmode: -executeMethod MechaCyberpunk.Pelea.Editor.CreadorPremiosPelea.Crear
    /// </summary>
    public static class CreadorPremiosPelea
    {
        public const string RutaPremioCasera = "Assets/_Proyecto/Economia/Datos/DatosPremioPelea_Casera.asset";
        public const string RutaPremioTutorial = "Assets/_Proyecto/Economia/Datos/DatosPremioPelea_Tutorial.asset";
        private const string RutaFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";
        private const string RutaFlujoTutorial = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaTutorial.prefab";

        // Franja de abajo del lienzo: la revancha en la parte baja y el premio justo encima (la parte de arriba es del resultado).
        private const float FraccionPremio = 0.17f;
        private const float FraccionResultado = 0.35f;

        [MenuItem("Mecha Cyberpunk/Crear T-106 premios en los flujos")]
        public static void Crear()
        {
            Aplicar(RutaFlujo, RutaPremioCasera);
            Aplicar(RutaFlujoTutorial, RutaPremioTutorial);
            AssetDatabase.SaveAssets();
            Debug.Log("[Pelea] T-106 premios en los flujos creados.");
        }

        /// <summary>Abre el prefab en la ruta, le pone el premio y la línea del premio, y lo guarda.</summary>
        public static void Aplicar(string rutaPrefab, string rutaPremio)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(rutaPrefab);
            try
            {
                Aplicar(raiz, rutaPremio);
                PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>Pone el premio y la línea del premio a un flujo ya abierto (sin guardarlo).</summary>
        public static void Aplicar(GameObject raiz, string rutaPremio)
        {
            DatosPremioPelea premio = AssetDatabase.LoadAssetAtPath<DatosPremioPelea>(rutaPremio);
            if (premio == null)
            {
                Debug.LogError("[Pelea] Falta " + rutaPremio + ": corre CreadorDatosPremioPelea.Crear primero.");
                return;
            }
            SerializedObject soDirector = new SerializedObject(raiz.GetComponent<DirectorPelea>());
            soDirector.FindProperty("premio").objectReferenceValue = premio;
            soDirector.ApplyModifiedPropertiesWithoutUndo();

            PantallaPelea pantalla = raiz.GetComponentInChildren<PantallaPelea>(true);
            SerializedObject soPantalla = new SerializedObject(pantalla);
            SerializedProperty propiedad = soPantalla.FindProperty("textoPremio");
            if (propiedad.objectReferenceValue != null)
            {
                return;
            }
            RectTransform lienzo = (RectTransform)pantalla.transform;
            Text revancha = (Text)soPantalla.FindProperty("textoRevancha").objectReferenceValue;
            RectTransform rectRevancha = revancha.rectTransform;
            rectRevancha.anchorMax = new Vector2(1f, FraccionPremio);
            Text texto = CreadorFlujoPelea.CrearTexto("Premio", lienzo, new Vector2(0f, FraccionPremio),
                new Vector2(1f, FraccionResultado));
            propiedad.objectReferenceValue = texto;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
