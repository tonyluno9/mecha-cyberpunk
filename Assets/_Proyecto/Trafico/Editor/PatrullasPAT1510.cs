using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Editor
{
    /// <summary>
    /// T-1510: Patrulla.prefab pasa a usar el cuerpo de Carro_PA01 (sedán de policía) y PatrullaCamioneta.prefab el de Carro_PA02 (SUV), en su mismo archivo (mismo GUID).
    /// Reusa <see cref="PatrullasT1507.Rehacer(string, string)"/> (conserva PatrullaMundo y las luces; llantas que giran, puertas que abren, cristal) y después ajusta la altura:
    /// <c>alturaRaiz</c> sale de lo que de verdad queda el modelo bajo la raíz, las luces de la sirena van a la barra del techo del modelo y la suspensión se alinea.
    /// Corre después de <c>CarrosPAT1510.ArmarTodos</c>. Correrlo otra vez deja lo mismo.
    /// Batchmode: -executeMethod MechaCyberpunk.Trafico.Editor.PatrullasPAT1510.Rehacer
    /// </summary>
    public static class PatrullasPAT1510
    {
        public const string RutaPatrulla = CreadorPatrullaT707.RutaPatrulla;
        public const string RutaCamioneta = CreadorPatrullaCamionetaT1389.RutaCamioneta;
        private const float MinimoAlturaRaiz = 0.05f;
        private const float MaximoAlturaRaiz = 1.2f;

        // Barra de sirena del modelo (m, en la raíz): separación de cada luz al centro (x) y altura (y). Medidas sobre PA01 y PA02 escaladas a 4.8 y 5.0 m.
        private const float SeparacionLuzSedan = 0.39f;
        private const float AlturaLuzSedan = 1.89f;
        private const float SeparacionLuzSuv = 0.49f;
        private const float AlturaLuzSuv = 2.1f;

        [MenuItem("Mecha Cyberpunk/T-1510/Rehacer patrullas con PA01 y PA02")]
        public static void Rehacer()
        {
            AssetDatabase.Refresh();
            Debug.Log("[Trafico] T-1510: " + Rehacer(RutaPatrulla, CarrosPAT1510.RutaPrefab("PA01"), SeparacionLuzSedan, AlturaLuzSedan));
            Debug.Log("[Trafico] T-1510: " + Rehacer(RutaCamioneta, CarrosPAT1510.RutaPrefab("PA02"), SeparacionLuzSuv, AlturaLuzSuv));
            AssetDatabase.SaveAssets();
        }

        public static string Rehacer(string rutaPatrulla, string rutaCarro, float separacionLuces, float alturaLuces)
        {
            string resumen = PatrullasT1507.Rehacer(rutaPatrulla, rutaCarro);
            if (resumen.StartsWith("FALTA"))
            {
                return resumen;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(rutaPatrulla);
            try
            {
                Bounds b = CreadorPatrullaT707.Limites(raiz);
                float bajoRaiz = Mathf.Clamp(raiz.transform.position.y - b.min.y, MinimoAlturaRaiz, MaximoAlturaRaiz);
                SerializedObject so = new SerializedObject(raiz.GetComponent<PatrullaMundo>());
                so.FindProperty("alturaRaiz").floatValue = bajoRaiz;
                so.ApplyModifiedPropertiesWithoutUndo();
                PonerLuz(raiz.transform.Find("LuzRoja"), separacionLuces, alturaLuces);
                PonerLuz(raiz.transform.Find("LuzAzul"), -separacionLuces, alturaLuces);
                PrefabUtility.SaveAsPrefabAsset(raiz, rutaPatrulla);
                resumen += " | alturaRaiz " + bajoRaiz.ToString("F2") + " m, luces a " + alturaLuces.ToString("F2") + " m";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            resumen += " | " + CarrosPAT1510.AlinearSuspension(rutaPatrulla);
            return resumen;
        }

        private static void PonerLuz(Transform luz, float x, float y)
        {
            if (luz != null)
            {
                luz.localPosition = new Vector3(x, y, 0f);
            }
        }
    }
}
