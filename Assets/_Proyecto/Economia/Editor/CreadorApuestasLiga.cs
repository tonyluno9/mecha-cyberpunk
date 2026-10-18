using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Economia.Editor
{
    /// <summary>
    /// Crea las 8 apuestas de las ligas (T-158, D-tony-98): DatosApuestaPelea_Liga1..4 y _Jefe1..4. Idempotente: un asset que ya existe
    /// no se toca, para no pisar valores ajustados en el inspector.
    /// Batchmode: -executeMethod MechaCyberpunk.Economia.Editor.CreadorApuestasLiga.Crear
    /// </summary>
    public static class CreadorApuestasLiga
    {
        private const string Carpeta = "Assets/_Proyecto/Economia/Datos/";
        private const string RutaBrazo = "Assets/_Proyecto/Economia/Datos/DatosObjeto_BrazoChatarra.asset";
        private const int MinimoPiezaLiga1 = 250;
        private const int MinimoPiezaJefe1 = 500;

        private struct Fila
        {
            public string nombre;
            public float momio;
            public int maxima;
            public int[] montos;
            public int minimoPieza; // 0 = sin pieza en juego
        }

        private static readonly Fila[] Filas =
        {
            new Fila { nombre = "Liga1", momio = 1.0f, maxima = 500, montos = new[] { 100, 250, 500 }, minimoPieza = MinimoPiezaLiga1 },
            new Fila { nombre = "Jefe1", momio = 1.5f, maxima = 1000, montos = new[] { 250, 500, 1000 }, minimoPieza = MinimoPiezaJefe1 },
            new Fila { nombre = "Liga2", momio = 1.2f, maxima = 1000, montos = new[] { 250, 500, 1000 } },
            new Fila { nombre = "Jefe2", momio = 1.8f, maxima = 2000, montos = new[] { 500, 1000, 2000 } },
            new Fila { nombre = "Liga3", momio = 1.4f, maxima = 1500, montos = new[] { 500, 1000, 1500 } },
            new Fila { nombre = "Jefe3", momio = 2.0f, maxima = 3000, montos = new[] { 1000, 2000, 3000 } },
            new Fila { nombre = "Liga4", momio = 1.6f, maxima = 2000, montos = new[] { 500, 1000, 2000 } },
            new Fila { nombre = "Jefe4", momio = 2.5f, maxima = 5000, montos = new[] { 1000, 2500, 5000 } },
        };

        public static string Ruta(string nombre) => Carpeta + "DatosApuestaPelea_" + nombre + ".asset";

        [MenuItem("Mecha Cyberpunk/Crear T-158 apuestas de liga")]
        public static void Crear()
        {
            foreach (Fila f in Filas)
            {
                if (AssetDatabase.LoadAssetAtPath<DatosApuestaPelea>(Ruta(f.nombre)) != null) continue;
                var datos = ScriptableObject.CreateInstance<DatosApuestaPelea>();
                AssetDatabase.CreateAsset(datos, Ruta(f.nombre));
                var so = new SerializedObject(datos);
                so.FindProperty("momio").floatValue = f.momio;
                so.FindProperty("apuestaMaxima").intValue = f.maxima;
                SerializedProperty montos = so.FindProperty("montos");
                montos.arraySize = f.montos.Length;
                for (int i = 0; i < f.montos.Length; i++) montos.GetArrayElementAtIndex(i).intValue = f.montos[i];
                so.FindProperty("piezaEnJuego").objectReferenceValue =
                    f.minimoPieza > 0 ? AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaBrazo) : null;
                so.FindProperty("apuestaMinimaPorPieza").intValue = f.minimoPieza;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(datos);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Economia] T-158 apuestas de liga creadas.");
        }
    }
}
