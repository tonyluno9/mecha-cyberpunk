using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Rutas de las escenas del prólogo y a cuál pertenece cada etapa (T-124). Dos assets: pruebas y demo.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de escenas", fileName = "DatosEscenas_Nuevo")]
    public class DatosEscenas : ScriptableObject
    {
        [Tooltip("Ruta de la escena del título y la creación de personaje.")]
        [SerializeField] private string rutaTitulo;
        [Tooltip("Ruta de la escena de la ciudad.")]
        [SerializeField] private string rutaCiudad;
        [Tooltip("Ruta de la escena del flashback.")]
        [SerializeField] private string rutaFlashback;

        public string RutaTitulo => rutaTitulo;
        public string RutaCiudad => rutaCiudad;
        public string RutaFlashback => rutaFlashback;

        /// <summary>Ninguna y CreacionPersonaje: título; Flashback: flashback; las demás: ciudad.</summary>
        public string RutaPara(EtapaPrologo etapa)
        {
            switch (etapa)
            {
                case EtapaPrologo.Ninguna:
                case EtapaPrologo.CreacionPersonaje:
                    return rutaTitulo;
                case EtapaPrologo.Flashback:
                    return rutaFlashback;
                default:
                    return rutaCiudad;
            }
        }

        /// <summary>Para crear los datos desde código (editor y pruebas).</summary>
        public void Fijar(string titulo, string ciudad, string flashback)
        {
            rutaTitulo = titulo;
            rutaCiudad = ciudad;
            rutaFlashback = flashback;
        }
    }
}
