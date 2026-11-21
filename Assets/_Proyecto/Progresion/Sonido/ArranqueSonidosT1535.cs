using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-1535: al cargar la primera escena, si hay un gestor de sonidos, crea (una vez) el objeto que hace sonar el ambiente de objetos e interiores
    /// (<see cref="SonidosMundo"/>), el clima (<see cref="SonidosClima"/>) y los eventos (<see cref="SonidosEventos"/>). Así no se edita ninguna escena.
    /// Sobrevive a los cambios de escena; con otro gestor en otra escena sigue sirviendo, porque todo se busca por el registro.
    /// </summary>
    public static class ArranqueSonidosT1535
    {
        public const string NombreRaiz = "SonidosAmbienteT1535";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Crear()
        {
            if (Registro<ISonidos>.Actual == null || Object.FindFirstObjectByType<SonidosMundo>() != null) return;
            CrearAhora();
        }

        /// <summary>Crea el objeto (lo usan el arranque y las pruebas de PlayMode).</summary>
        public static GameObject CrearAhora()
        {
            GameObject raiz = new GameObject(NombreRaiz);
            raiz.AddComponent<SonidosMundo>();
            raiz.AddComponent<SonidosClima>();
            raiz.AddComponent<SonidosEventos>();
            Object.DontDestroyOnLoad(raiz);
            Debug.Log("[T-1535] Sonidos de ambiente, clima y eventos creados.");
            return raiz;
        }
    }
}
