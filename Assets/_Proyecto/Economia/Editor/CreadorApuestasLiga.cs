using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Economia.Editor
{
    /// <summary>
    /// Crea las 8 apuestas de las ligas (T-158, D-tony-98): DatosApuestaPelea_Liga1..4 y _Jefe1..4. Idempotente: un asset que ya existe
    /// no se toca, para no pisar valores ajustados en el inspector. <see cref="AjustarPiezasEnJuego"/> (T-171, D-tony-121) es el paso
    /// explícito que pone en los 8 la pieza de rama de su nivel y su apuesta mínima; solo escribe si algo cambia.
    /// Batchmode: -executeMethod MechaCyberpunk.Economia.Editor.CreadorApuestasLiga.Crear / .AjustarPiezasEnJuego
    /// </summary>
    public static class CreadorApuestasLiga
    {
        private const string Carpeta = "Assets/_Proyecto/Economia/Datos/";
        private const string CarpetaPiezas = "Assets/_Proyecto/Mecha/Datos/Piezas/DatosPieza_";

        private struct Fila
        {
            public string nombre;
            public float momio;
            public int maxima;
            public int[] montos;
            public string pieza; // id del asset de la pieza en Mecha/Datos/Piezas (la carga por ruta como DatosObjeto)
            public int minimoPieza; // la mitad del tope de la apuesta
        }

        private static readonly Fila[] Filas =
        {
            new Fila { nombre = "Liga1", momio = 1.0f, maxima = 500, montos = new[] { 100, 250, 500 }, pieza = "piernas_cuerpoacuerpo_1", minimoPieza = 250 },
            new Fila { nombre = "Jefe1", momio = 1.5f, maxima = 1000, montos = new[] { 250, 500, 1000 }, pieza = "torso_militar_1", minimoPieza = 500 },
            new Fila { nombre = "Liga2", momio = 1.2f, maxima = 1000, montos = new[] { 250, 500, 1000 }, pieza = "brazoizq_tech_2", minimoPieza = 500 },
            new Fila { nombre = "Jefe2", momio = 1.8f, maxima = 2000, montos = new[] { 500, 1000, 2000 }, pieza = "torso_cuerpoacuerpo_2", minimoPieza = 1000 },
            new Fila { nombre = "Liga3", momio = 1.4f, maxima = 1500, montos = new[] { 500, 1000, 1500 }, pieza = "brazoder_fuego_3", minimoPieza = 750 },
            new Fila { nombre = "Jefe3", momio = 2.0f, maxima = 3000, montos = new[] { 1000, 2000, 3000 }, pieza = "cabina_tech_3", minimoPieza = 1500 },
            new Fila { nombre = "Liga4", momio = 1.6f, maxima = 2000, montos = new[] { 500, 1000, 2000 }, pieza = "piernas_militar_4", minimoPieza = 1000 },
            new Fila { nombre = "Jefe4", momio = 2.5f, maxima = 5000, montos = new[] { 1000, 2500, 5000 }, pieza = "torso_fuego_4", minimoPieza = 2500 },
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
                PonerPieza(so, f);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(datos);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Economia] T-158 apuestas de liga creadas.");
        }

        [MenuItem("Mecha Cyberpunk/Ajustar T-171 piezas en juego de las ligas")]
        public static void AjustarPiezasEnJuego()
        {
            int cambiados = 0;
            foreach (Fila f in Filas)
            {
                var datos = AssetDatabase.LoadAssetAtPath<DatosApuestaPelea>(Ruta(f.nombre));
                if (datos == null)
                {
                    Debug.LogError("[Economia] Falta " + Ruta(f.nombre) + ": corre primero CreadorApuestasLiga.Crear.");
                    continue;
                }
                var so = new SerializedObject(datos);
                if (!PonerPieza(so, f)) continue;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(datos);
                cambiados++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Economia] T-171 piezas en juego ajustadas: " + cambiados + " apuestas cambiadas.");
        }

        // Devuelve true si cambió algo (así correrlo dos veces no toca los assets).
        private static bool PonerPieza(SerializedObject so, Fila f)
        {
            var pieza = AssetDatabase.LoadAssetAtPath<DatosObjeto>(CarpetaPiezas + f.pieza + ".asset");
            if (pieza == null)
            {
                Debug.LogError("[Economia] No existe la pieza " + f.pieza + " para " + f.nombre);
                return false;
            }
            SerializedProperty propPieza = so.FindProperty("piezaEnJuego");
            SerializedProperty propMinimo = so.FindProperty("apuestaMinimaPorPieza");
            if (propPieza.objectReferenceValue == pieza && propMinimo.intValue == f.minimoPieza) return false;
            propPieza.objectReferenceValue = pieza;
            propMinimo.intValue = f.minimoPieza;
            return true;
        }
    }
}
