using UnityEngine;

namespace MechaCyberpunk.IA.Datos
{
    /// <summary>
    /// Números del acoso (S2, 14-nov): un grupo de SKULLS molesta a un civil hasta que ve al jugador, lo emboscan o los vencen.
    /// Lo leen <c>GrupoAcoso</c>, <c>ReglaAcoso</c> y <c>CivilAcosado</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "DatosAcoso", menuName = "Mecha Cyberpunk/IA/Datos acoso")]
    public class DatosAcoso : ScriptableObject
    {
        [Header("Círculo")]
        [Tooltip("Radio mínimo del círculo alrededor del civil (m). Lo usa el colocador.")]
        [Min(0.5f)] [SerializeField] private float radioCirculoMinimo = 1.8f;
        [Tooltip("Radio máximo del círculo alrededor del civil (m). Lo usa el colocador.")]
        [Min(0.5f)] [SerializeField] private float radioCirculoMaximo = 2.4f;
        [Tooltip("Giro con el que los pandilleros miran al civil (°/s).")]
        [Min(1f)] [SerializeField] private float velocidadGiro = 360f;

        [Header("Gestos")]
        [Tooltip("Segundos mínimos entre un gesto y el siguiente (s).")]
        [Min(0.1f)] [SerializeField] private float periodoGestoMinimo = 2.5f;
        [Tooltip("Segundos máximos entre un gesto y el siguiente (s).")]
        [Min(0.1f)] [SerializeField] private float periodoGestoMaximo = 5f;
        [Tooltip("Probabilidad de que el gesto sea un empujón (el resto son burlas).")]
        [Range(0f, 1f)] [SerializeField] private float probabilidadEmpujon = 0.5f;
        [Tooltip("Probabilidad de que un empujón tire al civil.")]
        [Range(0f, 1f)] [SerializeField] private float probabilidadCaida = 0.35f;

        [Header("Detección (te ven)")]
        [Tooltip("Cada cuánto miran si te ven (s).")]
        [Min(0.05f)] [SerializeField] private float periodoDeteccion = 0.2f;
        [Tooltip("Distancia a la que ven al jugador (m).")]
        [Min(1f)] [SerializeField] private float distanciaVista = 18f;
        [Tooltip("Ancho del cono de visión horizontal (°).")]
        [Range(10f, 360f)] [SerializeField] private float anguloVista = 120f;
        [Tooltip("Cuánto por encima de su horizonte pueden ver al jugador (°): una azotea alta casi no se ve.")]
        [Range(0f, 89f)] [SerializeField] private float inclinacionMaximaVista = 30f;
        [Tooltip("Distancia a la que te oyen en cualquier dirección (m).")]
        [Min(0f)] [SerializeField] private float distanciaOido = 4f;
        [Tooltip("Altura de los ojos del pandillero para la línea de vista (m).")]
        [Min(0.1f)] [SerializeField] private float alturaOjos = 1.5f;
        [Tooltip("Altura del pecho del jugador para la línea de vista (m).")]
        [Min(0.1f)] [SerializeField] private float alturaPechoJugador = 1.1f;

        [Header("Emboscada (te atacan sin verte)")]
        [Tooltip("Segundos que buscan antes de atacar (s).")]
        [Min(0f)] [SerializeField] private float retrasoEmboscada = 1.6f;
        [Tooltip("Lo mismo si el jugador está bastante más alto que ellos (s).")]
        [Min(0f)] [SerializeField] private float retrasoEmboscadaDesdeArriba = 2.6f;
        [Tooltip("Cuánto más alto debe estar el jugador para contar como desde arriba (m).")]
        [Min(0f)] [SerializeField] private float alturaDesdeArriba = 2.5f;
        [Tooltip("Giro con el que miran alrededor buscando (°/s).")]
        [Min(1f)] [SerializeField] private float giroBusqueda = 140f;

        [Header("Gritos")]
        [SerializeField] private string gritoAlerta = "¡Ahí está!";
        [SerializeField] private string gritoSorpresa = "¿¡Quién fue!?";
        [SerializeField] private string gritoGracias = "¡Gracias!";
        [SerializeField] private string gritoEmpujon = "¡Ya cállate!";

        [Header("Civil")]
        [Tooltip("Cuánto retrocede el civil con un empujón (m).")]
        [Min(0f)] [SerializeField] private float retrocesoEmpujon = 0.4f;
        [Tooltip("Duración del retroceso (s).")]
        [Min(0.05f)] [SerializeField] private float segundosRetroceso = 0.25f;
        [Tooltip("Cuánto se queda en el suelo si lo tiran (s).")]
        [Min(0.1f)] [SerializeField] private float segundosCaido = 2f;
        [Tooltip("Cuánto tarda en levantarse antes de correr (s).")]
        [Min(0.1f)] [SerializeField] private float segundosLevantarse = 0.7f;
        [Tooltip("Velocidad al huir (m/s).")]
        [Min(0.5f)] [SerializeField] private float velocidadHuida = 5.5f;
        [Tooltip("Cuánto corre antes de desaparecer (m).")]
        [Min(1f)] [SerializeField] private float distanciaHuida = 25f;
        [Tooltip("Distancia del rayo que busca paredes al correr (m).")]
        [Min(0.2f)] [SerializeField] private float distanciaRayoHuida = 1.2f;
        [Tooltip("Cuánto gira al chocar con una pared (°).")]
        [Range(5f, 180f)] [SerializeField] private float giroPared = 45f;
        [Tooltip("Caída máxima que acepta al correr: más que eso es borde de azotea (m).")]
        [Min(0.1f)] [SerializeField] private float caidaMaximaHuida = 0.8f;
        [Tooltip("Giro del civil al correr (°/s).")]
        [Min(1f)] [SerializeField] private float giroHuida = 540f;
        [Tooltip("Escala vertical del civil mientras lo acosan (1 = de pie normal).")]
        [Range(0.5f, 1f)] [SerializeField] private float escalaEncogido = 0.92f;
        [Tooltip("Dinero que suelta el grupo como bono del civil al vencerlo.")]
        [Min(0)] [SerializeField] private int bonoCivil = 40;

        public float RadioCirculoMinimo => radioCirculoMinimo;
        public float RadioCirculoMaximo => radioCirculoMaximo;
        public float VelocidadGiro => velocidadGiro;
        public float PeriodoGestoMinimo => periodoGestoMinimo;
        public float PeriodoGestoMaximo => periodoGestoMaximo;
        public float ProbabilidadEmpujon => probabilidadEmpujon;
        public float ProbabilidadCaida => probabilidadCaida;
        public float PeriodoDeteccion => periodoDeteccion;
        public float DistanciaVista => distanciaVista;
        public float AnguloVista => anguloVista;
        public float InclinacionMaximaVista => inclinacionMaximaVista;
        public float DistanciaOido => distanciaOido;
        public float AlturaOjos => alturaOjos;
        public float AlturaPechoJugador => alturaPechoJugador;
        public float RetrasoEmboscada => retrasoEmboscada;
        public float RetrasoEmboscadaDesdeArriba => retrasoEmboscadaDesdeArriba;
        public float AlturaDesdeArriba => alturaDesdeArriba;
        public float GiroBusqueda => giroBusqueda;
        public string GritoAlerta => gritoAlerta;
        public string GritoSorpresa => gritoSorpresa;
        public string GritoGracias => gritoGracias;
        public string GritoEmpujon => gritoEmpujon;
        public float RetrocesoEmpujon => retrocesoEmpujon;
        public float SegundosRetroceso => segundosRetroceso;
        public float SegundosCaido => segundosCaido;
        public float SegundosLevantarse => segundosLevantarse;
        public float VelocidadHuida => velocidadHuida;
        public float DistanciaHuida => distanciaHuida;
        public float DistanciaRayoHuida => distanciaRayoHuida;
        public float GiroPared => giroPared;
        public float CaidaMaximaHuida => caidaMaximaHuida;
        public float GiroHuida => giroHuida;
        public float EscalaEncogido => escalaEncogido;
        public int BonoCivil => bonoCivil;
    }
}
