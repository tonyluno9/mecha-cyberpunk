using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Vehiculos.Efectos
{
    /// <summary>
    /// T-745: al cargar una escena, a cada barril que ya existe (objetos cuyo nombre empieza con "Barril" o es un tambo de la calle, con collider)
    /// le pone <see cref="BarrilExplosivo"/>. Así no hay que tocar la ciudad ni sus prefabs: los barriles de cualquier escena revientan.
    /// </summary>
    public static class InstaladorBarriles
    {
        /// <summary>True si el nombre es el de un barril o tambo.</summary>
        public static bool EsNombreDeBarril(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
            {
                return false;
            }
            return nombre.StartsWith("Barril", System.StringComparison.OrdinalIgnoreCase) || nombre.IndexOf("Tambo", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AlIniciar()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
            Instalar(SceneManager.GetActiveScene());
        }

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo) => Instalar(escena);

        /// <summary>Recorre la escena y marca los barriles. Devuelve cuántos puso.</summary>
        public static int Instalar(Scene escena)
        {
            if (!escena.IsValid() || !escena.isLoaded)
            {
                return 0;
            }
            int puestos = 0;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                puestos += Recorrer(raiz.transform);
            }
            return puestos;
        }

        private static int Recorrer(Transform actual)
        {
            if (EsNombreDeBarril(actual.name) && actual.GetComponentInChildren<Collider>(true) != null)
            {
                if (actual.GetComponent<BarrilExplosivo>() == null && actual.GetComponentInParent<BarrilExplosivo>() == null)
                {
                    actual.gameObject.AddComponent<BarrilExplosivo>();
                    return 1;
                }
                return 0;
            }
            int total = 0;
            for (int i = 0; i < actual.childCount; i++)
            {
                total += Recorrer(actual.GetChild(i));
            }
            return total;
        }
    }
}
