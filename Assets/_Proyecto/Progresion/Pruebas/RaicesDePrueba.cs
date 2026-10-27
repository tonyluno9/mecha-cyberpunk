#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>
    /// Anota las raíces de la escena activa al empezar una prueba y destruye solo las nuevas al terminar. Destruir todas las raíces
    /// de la escena activa arrastra objetos del test runner (la escena de inicio) cuando la prueba no llegó a cargar su escena
    /// (D-tony-77, T-140). Úsalo: <see cref="Anotar"/> en el Setup y <see cref="DestruirNuevas"/> en el UnityTearDown.
    /// </summary>
    public static class RaicesDePrueba
    {
        public static HashSet<GameObject> Anotar()
        {
            return new HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
        }

        public static void DestruirNuevas(HashSet<GameObject> iniciales)
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (iniciales == null || !iniciales.Contains(raiz))
                {
                    Object.Destroy(raiz);
                }
            }
        }
    }
}
#endif
