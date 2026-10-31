using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-449 del creador de la ciudad de pandillas: el ServicioMensajes (los mensajes de aliados y profes en el teléfono) en la raíz GTA_T449.
    /// Idempotente: rehace solo esa raíz.
    /// </summary>
    public static partial class CreadorCiudadPandillas
    {
        public const string RaizT449 = "GTA_T449";

        [MenuItem("Mecha Cyberpunk/Pandillas/T-449 Mensajes")]
        public static void T449()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            T449Interno();
            Guardar(escena);
        }

        private static void T449Interno()
        {
            GameObject anterior = BuscarRaiz(RaizT449);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
            }
            GameObject raiz = new GameObject(RaizT449);
            raiz.AddComponent<ServicioMensajes>();
            Debug.Log("[T-449] ServicioMensajes creado.");
        }
    }
}
