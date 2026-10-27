using MechaCyberpunk.Combate.Habilidades;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma lo de T-176 en MechaJugador.prefab (y por herencia en MechaEstacionado.prefab): las acciones Habilidad1 y Habilidad2
    /// (teclas 1 y 2) en ControlesMecha.inputactions, LanzadorHabilidades con sus orígenes y UsoHabilidades.
    /// Idempotente: una segunda corrida no cambia el prefab ni las entradas.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorHabilidadesMecha.Crear
    /// </summary>
    public static class CreadorHabilidadesMecha
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string NombreOrigenBrazo = "OrigenHabilidadBrazo";
        private const string NombreOrigenTorso = "OrigenHabilidadTorso";

        // Posiciones locales de la cabina (la cámara está a AlturaCamara): el brazo izquierdo sale a la vista, a la izquierda del
        // tablero; el torso, del pecho, al centro y más abajo.
        private const float AlturaCamara = 3.5f;
        private static readonly Vector3 PosicionBrazo = new Vector3(-0.42f, AlturaCamara - 0.2f, 0.8f);
        private static readonly Vector3 PosicionTorso = new Vector3(0f, AlturaCamara - 0.6f, 0.5f);

        [MenuItem("Mecha Cyberpunk/Crear T-176 habilidades del mecha")]
        public static void Crear()
        {
            EntradaMechaEditor.AgregarAccion("Habilidad1", "<Keyboard>/1");
            EntradaMechaEditor.AgregarAccion("Habilidad2", "<Keyboard>/2");
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                bool cambio = false;
                Transform cabina = raiz.transform.Find("Sacudida/Cabina");
                if (cabina == null)
                {
                    cabina = raiz.transform.Find("Cabina");
                }
                Transform origenBrazo = ObtenerOrigen(cabina, NombreOrigenBrazo, PosicionBrazo, ref cambio);
                Transform origenTorso = ObtenerOrigen(cabina, NombreOrigenTorso, PosicionTorso, ref cambio);

                LanzadorHabilidades lanzador = raiz.GetComponent<LanzadorHabilidades>();
                if (lanzador == null)
                {
                    lanzador = raiz.AddComponent<LanzadorHabilidades>();
                    cambio = true;
                }
                SerializedObject soLanzador = new SerializedObject(lanzador);
                soLanzador.FindProperty("origenBrazo").objectReferenceValue = origenBrazo;
                soLanzador.FindProperty("origenTorso").objectReferenceValue = origenTorso;
                cambio |= soLanzador.ApplyModifiedPropertiesWithoutUndo();

                UsoHabilidades uso = raiz.GetComponent<UsoHabilidades>();
                if (uso == null)
                {
                    uso = raiz.AddComponent<UsoHabilidades>();
                    cambio = true;
                }
                SerializedObject soUso = new SerializedObject(uso);
                soUso.FindProperty("habilidad1").objectReferenceValue = EntradaMechaEditor.Referencia("Habilidad1");
                soUso.FindProperty("habilidad2").objectReferenceValue = EntradaMechaEditor.Referencia("Habilidad2");
                cambio |= soUso.ApplyModifiedPropertiesWithoutUndo();

                if (cambio)
                {
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-176 habilidades del mecha creadas.");
        }

        private static Transform ObtenerOrigen(Transform cabina, string nombre, Vector3 posicion, ref bool cambio)
        {
            Transform origen = cabina.Find(nombre);
            if (origen == null)
            {
                origen = new GameObject(nombre).transform;
                origen.SetParent(cabina, false);
                origen.localPosition = posicion;
                cambio = true;
            }
            return origen;
        }
    }
}
