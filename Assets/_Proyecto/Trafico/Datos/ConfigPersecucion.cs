using System.Collections.Generic;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Datos
{
    /// <summary>Los números de la persecución de drones y patrullas (T-340).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config de persecución", fileName = "ConfigPersecucion_Nueva")]
    public class ConfigPersecucion : ScriptableObject
    {
        [Tooltip("Drones en los niveles de calor 1, 2 y 3 (el primer elemento es el nivel 1).")]
        [SerializeField] private List<int> dronesPorNivel = new List<int> { 0, 1, 2 };
        [Tooltip("Patrullas en los mismos niveles.")]
        [SerializeField] private List<int> patrullasPorNivel = new List<int> { 0, 0, 1 };
        [Min(0.1f)] [Tooltip("Velocidad del dron (m/s).")]
        [SerializeField] private float velocidadDron = 9f;
        [Min(0.1f)] [Tooltip("Velocidad de la patrulla (m/s).")]
        [SerializeField] private float velocidadPatrulla = 12f;
        [Min(0f)] [Tooltip("Distancia a la que un perseguidor se queda del jugador (m).")]
        [SerializeField] private float distanciaSeguimiento = 6f;
        [Min(0f)] [Tooltip("Distancia a la que alcanza al jugador (m); menor que la de seguimiento.")]
        [SerializeField] private float distanciaCaptura = 3f;
        [Min(1f)] [Tooltip("Altura del dron sobre el jugador (m).")]
        [SerializeField] private float alturaDron = 7f;
        [Min(1f)] [Tooltip("A qué distancia del jugador aparece un dron (m).")]
        [SerializeField] private float distanciaAparicionDron = 90f;
        [Min(0f)] [Tooltip("T-938: radio del círculo alrededor del jugador donde se coloca cada dron, uno por índice (m).")]
        [SerializeField] private float radioOrbitaDron = 4f;
        [Min(0f)] [Tooltip("T-938: dos drones nunca quedan a menos de esta distancia (m).")]
        [SerializeField] private float distanciaMinimaEntreDrones = 3f;
        [Min(0.05f)] [Tooltip("Cada cuánto cambian de color las luces de la patrulla (s).")]
        [SerializeField] private float periodoSirena = 0.4f;

        [Header("Disparos del dron (T-402)")]
        [Min(0)] [Tooltip("Desde qué nivel de calor los drones disparan al jugador a pie.")]
        [SerializeField] private int nivelDisparoDron = 2;
        [Min(0.1f)] [Tooltip("Segundos entre disparos de cada dron (s).")]
        [SerializeField] private float segundosEntreTirosDron = 1.5f;
        [Min(0f)] [Tooltip("Daño de cada disparo del dron (puntos de vida).")]
        [SerializeField] private float danioDron = 4f;
        [Range(0f, 1f)] [Tooltip("Probabilidad de acertar (0 a 1).")]
        [SerializeField] private float aciertoDron = 0.7f;
        [Min(0.05f)] [Tooltip("Aviso antes del disparo, con la luz del dron en rojo (s).")]
        [SerializeField] private float avisoDron = 0.3f;

        [Header("Policías a pie (T-709)")]
        [Min(0)] [Tooltip("Cuántos policías se bajan de cada patrulla al llegar.")]
        [SerializeField] private int policiasPorPatrulla = 2;
        [Min(0f)] [Tooltip("La patrulla baja a sus policías cuando queda a esta distancia extra de la de seguimiento (m).")]
        [SerializeField] private float margenParaBajarPolicias = 1.5f;

        [Header("Vista y retirada de las patrullas (T-587)")]
        [Min(5f)] [Tooltip("Desde aquí una patrulla te ve si no hay una pared en medio (m).")]
        [SerializeField] private float radioVista = 70f;
        [Min(1f)] [Tooltip("Segundos sin que ninguna patrulla te vea para que te den por perdido y el calor baje de más (s).")]
        [SerializeField] private float segundosParaPerder = 12f;
        [Min(0f)] [Tooltip("Calor que baja por segundo, además del enfriamiento normal, mientras te han perdido (puntos/s).")]
        [SerializeField] private float enfriamientoSinVista = 6f;
        [Min(0f)] [Tooltip("Las patrullas que cortan el paso van a donde estarás en estos segundos si corres (s).")]
        [SerializeField] private float segundosAdelantoCorte = 3f;
        [Min(0f)] [Tooltip("Velocidad mínima del jugador para que lo intenten cortar (m/s).")]
        [SerializeField] private float velocidadMinimaCorte = 3f;
        [Min(10f)] [Tooltip("Una patrulla nueva sale al menos a esta distancia del jugador y fuera de la vista de la cámara (m).")]
        [SerializeField] private float distanciaMinimaSalida = 60f;
        [Min(1f)] [Tooltip("T-708: una patrulla nueva sale de modo que tarde al menos estos segundos en llegar por las calles (s).")]
        [SerializeField] private float segundosMinimosLlegadaPatrulla = 7f;
        [Min(1f)] [Tooltip("T-708: tiempo de llegada que busca al elegir dónde sale (s).")]
        [SerializeField] private float segundosLlegadaPatrulla = 12f;
        [Min(1f)] [Tooltip("Una patrulla que se retira desaparece solo fuera de la vista y a más de esta distancia del jugador (m).")]
        [SerializeField] private float distanciaRetiro = 60f;
        [Min(1f)] [Tooltip("Si tras tantos segundos de retirada sigue a la vista, desaparece de todos modos (s).")]
        [SerializeField] private float segundosMaximosRetiro = 60f;

        [Header("Ronda (T-933)")]
        [Tooltip("Patrullas que circulan sin sirena: solo ellas pueden verte cometer un delito.")]
        [Min(0)] [SerializeField] private int patrullasDeRonda = 3;
        [Tooltip("Qué tan rápido va una patrulla de ronda comparada con una en persecución (0 a 1).")]
        [Range(0.1f, 1f)] [SerializeField] private float factorVelocidadRonda = 0.55f;
        [Tooltip("Segundos sin criminales a la vista para que una patrulla de ronda se vaya.")]
        [Min(0f)] [SerializeField] private float segundosTrasCriminal = 10f;

        [Header("Búsqueda y arresto (T-665)")]
        [Min(10f)] [Tooltip("Radio del círculo de búsqueda, centrado donde te vieron por última vez (m).")]
        [SerializeField] private float radioBusqueda = 90f;
        [Min(1f)] [Tooltip("T-729: segundos seguidos sin que te vea la policía (patrulla, dron o cámara) para que dejes de ser buscado (s). Los últimos 8 las estrellas parpadean (45 s: 37 sin que te vean y empiezan a parpadear).")]
        [SerializeField] private float segundosParaEscapar = 45f;
        [Min(1f)] [Tooltip("Una patrulla a menos de esta distancia que te ve puede arrestarte (m).")]
        [SerializeField] private float distanciaArresto = 10f;
        [Min(0.5f)] [Tooltip("Segundos seguidos, cerca de una patrulla y casi quieto, para que te arresten (s).")]
        [SerializeField] private float segundosArresto = 4f;
        [Min(0f)] [Tooltip("Por debajo de esta velocidad cuentas como quieto para el arresto (m/s).")]
        [SerializeField] private float velocidadArresto = 2f;

        public int PatrullasDeRonda => patrullasDeRonda;
        public float FactorVelocidadRonda => factorVelocidadRonda;
        public float SegundosTrasCriminal => segundosTrasCriminal;
        public float RadioBusqueda => radioBusqueda;
        public float SegundosParaEscapar => segundosParaEscapar;
        public float DistanciaArresto => distanciaArresto;
        public float SegundosArresto => segundosArresto;
        public float VelocidadArresto => velocidadArresto;

        [Min(5f)] [Tooltip("Un dron te ve desde aquí, sin contar paredes (vuela por encima) (m).")]
        [SerializeField] private float radioVistaDron = 45f;
        public float RadioVistaDron => radioVistaDron;

        [Header("Dron derribable (T-730)")]
        [Min(1f)] [Tooltip("Puntos de daño que aguanta el dron antes de caer. Una pistola hace 10 a 20 por bala.")]
        [SerializeField] private float vidaDron = 40f;
        [Min(0f)] [Tooltip("Cuánto baja la búsqueda (puntos de calor) cada vez que derribas un dron. 25 es un nivel.")]
        [SerializeField] private float calorAlDerribarDron = 25f;
        public float VidaDron => vidaDron;
        public float CalorAlDerribarDron => calorAlDerribarDron;

        public float RadioVista => radioVista;
        public float SegundosParaPerder => segundosParaPerder;
        public float EnfriamientoSinVista => enfriamientoSinVista;
        public float SegundosAdelantoCorte => segundosAdelantoCorte;
        public float VelocidadMinimaCorte => velocidadMinimaCorte;
        public float DistanciaMinimaSalida => distanciaMinimaSalida;
        public float DistanciaRetiro => distanciaRetiro;
        public float SegundosMinimosLlegadaPatrulla => segundosMinimosLlegadaPatrulla;
        public float SegundosLlegadaPatrulla => segundosLlegadaPatrulla;
        public float VelocidadPatrulla => velocidadPatrulla;
        public int PoliciasPorPatrulla => policiasPorPatrulla;
        public float DistanciaParaBajarPolicias => distanciaSeguimiento + margenParaBajarPolicias;
        public float SegundosMaximosRetiro => segundosMaximosRetiro;

        public int NivelDisparoDron => nivelDisparoDron;
        public float SegundosEntreTirosDron => segundosEntreTirosDron;
        public float DanioDron => danioDron;
        public float AciertoDron => aciertoDron;
        public float AvisoDron => avisoDron;
        public float AlturaDron => alturaDron;
        public float DistanciaAparicionDron => distanciaAparicionDron;
        public float RadioOrbitaDron => radioOrbitaDron;
        public float DistanciaMinimaEntreDrones => distanciaMinimaEntreDrones;
        public float PeriodoSirena => periodoSirena;

        public CalculoPersecucion Crear()
        {
            return new CalculoPersecucion(dronesPorNivel, patrullasPorNivel, velocidadDron, velocidadPatrulla, distanciaSeguimiento, distanciaCaptura);
        }
    }
}
