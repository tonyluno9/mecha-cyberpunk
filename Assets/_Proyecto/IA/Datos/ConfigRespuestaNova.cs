using System.Collections.Generic;
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
        [Header("T-1389b: variantes nuevas")]
        [Tooltip("Otros guardias de Nova (centinela E15, élite E18...): se reparten por turnos con el guardia base.")]
        [SerializeField] private GameObject[] guardiasExtra = new GameObject[0];
        [Tooltip("Otros guardias pesados (robot pesado E16...): se reparten por turnos con el pesado base.")]
        [SerializeField] private GameObject[] pesadosExtra = new GameObject[0];
        [Tooltip("Perros robot de Nova (E17): unidades de cuerpo a cuerpo.")]
        [SerializeField] private GameObject[] perros = new GameObject[0];
        [Min(0)] [SerializeField] private int perrosConCuatro;
        [Min(0)] [SerializeField] private int perrosConCinco;
        [Header("Cantidades")]
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
        [Tooltip("ID del sonido que suena cuando llega Nova con 5 estrellas. T-1531: NOV_CAIDA_Y_ENCENDIDO, el agente cae del cielo y se enciende.")]
        [SerializeField] private string sonidoLlegada = "NOV_CAIDA_Y_ENCENDIDO"; // T-1531: antes POL_NOVA_AVISO (sigue como alias)
        [Tooltip("Cuánto se agranda el modelo del guardia pesado para igualar a los otros agentes de Nova (x).")]
        [Min(0.5f)] [SerializeField] private float escalaPesado = 1.43f; // T-1334: medido con BakeMesh, así mide lo mismo que el guardia (2.62 m)
        [Tooltip("Un agente de Nova que estuvo a esta distancia del jugador ya lo vio: lo persigue aunque baje la búsqueda (m).")]
        [Min(1f)] [SerializeField] private float distanciaVista = 40f;

        [Header("T-1498: naves que bajan soldados")]
        [Tooltip("Naves (prefabs Dron_NV01, Dron_NV03_a, Dron_NV03_b): una a la vez, por turnos. Vacío: no hay naves.")]
        [SerializeField] private GameObject[] naves = new GameObject[0];
        [Min(0)] [SerializeField] private int soldadosNaveConCuatro = 2;
        [Min(0)] [SerializeField] private int soldadosNaveConCinco = 4;
        [Tooltip("Segundos tras empezar la búsqueda de Nova para que llegue la primera nave (s).")]
        [Min(0f)] [SerializeField] private float segundosPrimeraNave = 8f;
        [Tooltip("Segundos entre que una nave se va y llega la siguiente (s).")]
        [Min(1f)] [SerializeField] private float segundosEntreNaves = 40f;
        [Tooltip("Altura a la que flota la nave sobre el piso (m).")]
        [Min(2f)] [SerializeField] private float alturaFlote = 10f;
        [Tooltip("Altura mínima de flote aceptable si hay techo cerca (m).")]
        [Min(2f)] [SerializeField] private float alturaMinimaFlote = 6f;
        [Tooltip("Aire libre que necesita la nave sobre el piso además de su altura (m) y radio de su caja libre.")]
        [Min(0f)] [SerializeField] private float margenAireNave = 2f;
        [Min(1f)] [SerializeField] private float radioLibreNave = 5f;
        [Tooltip("Velocidad de la nave al llegar y al irse (m/s).")]
        [Min(1f)] [SerializeField] private float velocidadNave = 28f;
        [Tooltip("Velocidad con que baja cada soldado por la cuerda (m/s).")]
        [Min(0.5f)] [SerializeField] private float velocidadDescenso = 6f;
        [Min(0.1f)] [SerializeField] private float segundosEntreSoldados = 0.7f;
        [Tooltip("Distancia (m) desde donde llega y hacia donde se va.")]
        [Min(20f)] [SerializeField] private float distanciaLlegadaNave = 140f;
        [Tooltip("Material de la cuerda (opcional: sin él se usa uno sencillo).")]
        [SerializeField] private Material materialCuerda;

        public IReadOnlyList<GameObject> Naves => naves;
        public int SoldadosNave(int estrellas) => MechaCyberpunk.IA.APie.ReglaNaveNova.Soldados(estrellas, soldadosNaveConCuatro, soldadosNaveConCinco);
        public float SegundosPrimeraNave => segundosPrimeraNave;
        public float SegundosEntreNaves => segundosEntreNaves;
        public float AlturaFlote => alturaFlote;
        public float AlturaMinimaFlote => alturaMinimaFlote;
        public float MargenAireNave => margenAireNave;
        public float RadioLibreNave => radioLibreNave;
        public float VelocidadNave => velocidadNave;
        public float VelocidadDescenso => velocidadDescenso;
        public float SegundosEntreSoldados => segundosEntreSoldados;
        public float DistanciaLlegadaNave => distanciaLlegadaNave;
        public Material MaterialCuerda => materialCuerda;

        /// <summary>Para el editor y las pruebas.</summary>
        public void ConfigurarNaves(GameObject[] nuevas, int conCuatro, int conCinco)
        {
            naves = nuevas ?? new GameObject[0];
            soldadosNaveConCuatro = conCuatro;
            soldadosNaveConCinco = conCinco;
        }

        public void PonerMaterialCuerda(Material material) => materialCuerda = material;

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
        public IReadOnlyList<GameObject> GuardiasExtra => guardiasExtra;
        public IReadOnlyList<GameObject> PesadosExtra => pesadosExtra;
        public IReadOnlyList<GameObject> Perros => perros;
        public int CantidadPerros(int estrellas) => estrellas >= 5 ? perrosConCinco : perrosConCuatro;

        /// <summary>Guardias que pueden salir: el base y sus variantes (el base primero).</summary>
        public List<GameObject> TodosLosGuardias() => Juntar(guardia, guardiasExtra);

        /// <summary>Pesados que pueden salir: el base y sus variantes (el base primero).</summary>
        public List<GameObject> TodosLosPesados() => Juntar(guardiaPesado, pesadosExtra);

        private static List<GameObject> Juntar(GameObject baseGo, GameObject[] extra)
        {
            var lista = new List<GameObject>();
            if (baseGo != null) { lista.Add(baseGo); }
            if (extra != null)
            {
                foreach (GameObject g in extra)
                {
                    if (g != null) { lista.Add(g); }
                }
            }
            return lista;
        }

        /// <summary>Para el editor y las pruebas: pone las variantes nuevas.</summary>
        public void ConfigurarVariantes(GameObject[] guardiasNuevos, GameObject[] pesadosNuevos, GameObject[] perrosNuevos, int perros4, int perros5)
        {
            guardiasExtra = guardiasNuevos ?? new GameObject[0];
            pesadosExtra = pesadosNuevos ?? new GameObject[0];
            perros = perrosNuevos ?? new GameObject[0];
            perrosConCuatro = perros4;
            perrosConCinco = perros5;
        }

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
