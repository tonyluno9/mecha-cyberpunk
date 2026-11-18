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

        [Header("T-1323: llegada desde el cielo")]
        [Tooltip("Altura sobre el piso desde la que caen al llegar (m). Si hay un techo encima, caen desde lo que quepa.")]
        [Min(0f)] [SerializeField] private float alturaCaida = 12f;
        [Tooltip("Cuánto dura el destello de la llegada (s).")]
        [Min(0.05f)] [SerializeField] private float segundosDestello = 0.6f;
        [Tooltip("Intensidad de la luz del destello.")]
        [Min(0f)] [SerializeField] private float intensidadDestello = 25f;
        [Tooltip("Alcance de la luz del destello (m).")]
        [Min(1f)] [SerializeField] private float alcanceDestello = 12f;
        [SerializeField] private Color colorDestello = new Color(0.3f, 0.65f, 1f, 1f);
        [Tooltip("Material aditivo de las chispas del destello (opcional: sin él solo hay luz). Lo pone AplicarNovaT1323.")]
        [SerializeField] private Material materialDestello;
        [Tooltip("ID del sonido que suena cuando llega Nova con 5 estrellas. Hueco: falta un sonido propio (por ahora la entrada de un jefe).")]
        [SerializeField] private string sonidoLlegada = "PAN_JEFE_ENTRADA";
        [Tooltip("Cuánto se agranda el modelo del guardia pesado para igualar a los otros agentes de Nova (x).")]
        [Min(0.5f)] [SerializeField] private float escalaPesado = 1.43f; // T-1334: medido con BakeMesh, así mide lo mismo que el guardia (2.62 m)
        [Tooltip("Un agente de Nova que estuvo a esta distancia del jugador ya lo vio: lo persigue aunque baje la búsqueda (m).")]
        [Min(1f)] [SerializeField] private float distanciaVista = 40f;

        public float AlturaCaida => alturaCaida;
        public float SegundosDestello => segundosDestello;
        public float IntensidadDestello => intensidadDestello;
        public float AlcanceDestello => alcanceDestello;
        public Color ColorDestello => colorDestello;
        public Material MaterialDestello => materialDestello;
        public string SonidoLlegada => sonidoLlegada;
        public float EscalaPesado => escalaPesado;
        public float DistanciaVista => distanciaVista;

        public void PonerMaterialDestello(Material material) => materialDestello = material;

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
