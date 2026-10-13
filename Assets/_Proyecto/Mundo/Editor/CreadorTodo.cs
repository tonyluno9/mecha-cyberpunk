using MechaCyberpunk.Arena.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// Punto de entrada único de los creadores de Alan: corre todos en el orden correcto para que ninguna tarea borre a otra.
    /// Orden: CreadorArte (T-076, materiales y prefabs del kit que usan los creadores siguientes), CreadorArena (T-020, crea la jaula y Pruebas_Alan que luego rehace CreadorBarrio), CreadorBarrio (T-040),
    /// CreadorReloj (T-041), CreadorLugares (T-042), CreadorBar (T-043) y CreadorPeleaClandestino (T-044). Correrlo dos veces deja las escenas y prefabs iguales.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.CreadorTodo.Crear
    /// </summary>
    public static class CreadorTodo
    {
        [MenuItem("Mecha Cyberpunk/Crear todo (Alan)")]
        public static void Crear()
        {
            CreadorArte.Crear();
            CreadorArena.Crear();
            CreadorBarrio.Crear();
            CreadorReloj.Crear();
            CreadorLugares.Crear();
            CreadorBar.Crear();
            CreadorPeleaClandestino.Crear();
            Debug.Log("[Mundo] Creadores de Alan corridos en orden.");
        }
    }
}
