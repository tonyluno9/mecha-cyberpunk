using UnityEngine;

namespace MechaCyberpunk.IA.Datos
{
    /// <summary>
    /// T-1304: quién viene de Nova cuando la búsqueda llega a 4-5 estrellas. Vive en IA/Resources/ConfigRespuestaNova (lo crea CrearRespuestaNovaT1304).
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/IA/Config respuesta Nova", fileName = "ConfigRespuestaNova")]
    public class ConfigRespuestaNova : ScriptableObject
    {
        public const string Recurso = "ConfigRespuestaNova";

        [SerializeField] private GameObject guardia;
        [SerializeField] private GameObject guardiaPesado;
        [SerializeField] private GameObject jefeFinal;
        [Min(0)] [SerializeField] private int guardiasConCuatro = 3;
        [Min(0)] [SerializeField] private int pesadosConCuatro = 1;
        [Min(0)] [SerializeField] private int guardiasConCinco = 5;
        [Min(0)] [SerializeField] private int pesadosConCinco = 2;
        [Min(1f)] [SerializeField] private float distanciaMinima = 22f;
        [Min(1f)] [SerializeField] private float distanciaMaxima = 34f;
        [Tooltip("Cada cuánto llegan refuerzos de Nova para reponer a los caídos (s).")]
        [Min(1f)] [SerializeField] private float segundosEntreOleadas = 25f;
        [Tooltip("Segundos bajo 4 estrellas antes de que Nova se retire.")]
        [Min(0f)] [SerializeField] private float segundosParaRetirarse = 6f;

        public GameObject Guardia => guardia;
        public GameObject GuardiaPesado => guardiaPesado;
        public GameObject JefeFinal => jefeFinal;
        public int Guardias(int estrellas) => estrellas >= 5 ? guardiasConCinco : guardiasConCuatro;
        public int Pesados(int estrellas) => estrellas >= 5 ? pesadosConCinco : pesadosConCuatro;
        public float DistanciaMinima => distanciaMinima;
        public float DistanciaMaxima => Mathf.Max(distanciaMinima, distanciaMaxima);
        public float SegundosEntreOleadas => segundosEntreOleadas;
        public float SegundosParaRetirarse => segundosParaRetirarse;

        /// <summary>Para el editor y las pruebas.</summary>
        public void Configurar(GameObject guardiaNova, GameObject pesado, GameObject jefe)
        {
            guardia = guardiaNova;
            guardiaPesado = pesado;
            jefeFinal = jefe;
        }
    }
}
