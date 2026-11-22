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
        [Min(0)] [SerializeField] private int patrullasDeRonda = 2;
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

        [Header("Conducción de las patrullas (T-986)")]
        [Min(0f)] [Tooltip("Distancia de mira de la patrulla con el carro parado (m).")]
        [SerializeField] private float distanciaMiraMinima = 4f;
        [Min(0f)] [Tooltip("Metros más que mira por cada m/s de rapidez (s).")]
        [SerializeField] private float segundosMira = 0.45f;
        [Min(1f)] [Tooltip("Velocidad con la que toma una esquina de 90 grados (m/s).")]
        [SerializeField] private float velocidadCurva90 = 10f;
        [Min(1f)] [Tooltip("La velocidad más baja que pide en una curva, aunque sea vuelta en U (m/s).")]
        [SerializeField] private float velocidadMinimaCurva = 4f;
        [Range(0.1f, 1f)] [Tooltip("Qué parte del agarre lateral del carro usa al tomar una curva.")]
        [SerializeField] private float fraccionAgarreCurva = 1f;
        [Min(0.5f)] [Tooltip("Desaceleración con la que planea frenar antes de una curva o de la meta (m/s2); el freno del carro da más.")]
        [SerializeField] private float desaceleracionPlaneada = 5f;
        [Min(0.1f)] [Tooltip("Velocidad que le falta para pisar el acelerador a fondo (m/s).")]
        [SerializeField] private float rangoAceleracion = 3f;
        [Min(0.1f)] [Tooltip("Velocidad que le sobra para pisar el freno a fondo (m/s).")]
        [SerializeField] private float rangoFreno = 3f;
        [Min(0.1f)] [Tooltip("Si le sobra tanta velocidad frena a fondo de golpe (m/s).")]
        [SerializeField] private float errorFrenoFuerte = 4f;
        [Min(0f)] [Tooltip("Error de velocidad que deja pasar sin tocar los pedales (m/s).")]
        [SerializeField] private float zonaMuertaVelocidad = 0.4f;
        [Min(0.05f)] [Tooltip("Por debajo de esta rapidez la patrulla cuenta como quieta (m/s).")]
        [SerializeField] private float rapidezQuieta = 0.5f;
        [Min(0.5f)] [Tooltip("Segundos quieta queriendo avanzar para darse por atorada (s).")]
        [SerializeField] private float segundosParaAtoro = 2.5f;
        [Min(0.5f)] [Tooltip("Segundos que da reversa al atorarse (s).")]
        [SerializeField] private float segundosReversa = 1.8f;
        [Range(0f, 1f)] [Tooltip("Volante mínimo (0 a 1) con el que da reversa para sacar la trompa.")]
        [SerializeField] private float volanteReversaMinimo = 0.6f;
        [Range(10f, 180f)] [Tooltip("Ángulo al punto de mira desde el que un giro es tan cerrado que usa el freno de mano (grados).")]
        [SerializeField] private float anguloFrenoMano = 65f;
        [Min(1f)] [Tooltip("Rapidez mínima para usar el freno de mano en un giro muy cerrado (m/s).")]
        [SerializeField] private float velocidadFrenoMano = 6f;
        [Min(0.5f)] [Tooltip("A esta distancia de un cruce da el cruce por pasado y mira al siguiente (m).")]
        [SerializeField] private float radioLlegadaNodo = 4f;
        [Min(0.5f)] [Tooltip("A esta distancia de una miga de pan la da por recorrida (m).")]
        [SerializeField] private float radioLlegadaMiga = 3f;
        [Min(1f)] [Tooltip("Cuántos segundos volcada (boca abajo o de lado) hasta quitarla, si nadie la ve (s).")]
        [SerializeField] private float segundosVolcada = 4f;

        [Header("Atascos (T-1006)")]
        [Min(0.2f)] [Tooltip("Cada cuántos segundos se mide cuánto avanzó una patrulla que pide avanzar (s).")]
        [SerializeField] private float ventanaAtasco = 1.5f;
        [Min(0.05f)] [Tooltip("Si en esa ventana avanzó menos que esto, está atorada y da reversa (m).")]
        [SerializeField] private float distanciaMinimaAvance = 0.6f;
        [Min(1)] [Tooltip("Reversas seguidas en el mismo lugar para recolocarla en la calle.")]
        [SerializeField] private int reversasParaRecolocar = 2;
        [Min(1f)] [Tooltip("Segundos atorada en el mismo lugar, aunque dé reversa, para recolocarla (s).")]
        [SerializeField] private float segundosParaRecolocar = 10f;
        [Min(1f)] [Tooltip("Si se aleja tanto del lugar donde se atoró, se da por liberada (m).")]
        [SerializeField] private float radioLiberada = 6f;
        [Min(1f)] [Tooltip("Al recolocarla, el cruce elegido está al menos a esta distancia de donde se atoró (m).")]
        [SerializeField] private float distanciaMinimaRecolocar = 15f;
        [Min(1f)] [Tooltip("Al recolocarla, el cruce elegido está a lo más a esta distancia de donde se atoró (m).")]
        [SerializeField] private float distanciaMaximaRecolocar = 120f;
        [Min(0.5f)] [Tooltip("Cuánto puede cambiar el volante por segundo (1 es el giro completo); evita que tiemble (1/s).")]
        [SerializeField] private float velocidadVolante = 6f;

        public float RadioLlegadaNodo => radioLlegadaNodo;
        [Min(0.1f)] [Tooltip("Solo se considera atorada si pide al menos esta velocidad (m/s); así no cuenta la que se acomoda despacio junto al jugador.")]
        [SerializeField] private float velocidadMinimaAtasco = 2f;
        public float VelocidadMinimaAtasco => velocidadMinimaAtasco;
        public float DistanciaMinimaRecolocar => distanciaMinimaRecolocar;
        public float DistanciaMaximaRecolocar => distanciaMaximaRecolocar;
        public float VelocidadVolante => velocidadVolante;

        /// <summary>T-1006: los números del detector de atasco de las patrullas.</summary>
        public ParametrosAtasco ParametrosAtasco => new ParametrosAtasco
        {
            VentanaSegundos = ventanaAtasco,
            DistanciaMinima = distanciaMinimaAvance,
            ReversasParaRecolocar = reversasParaRecolocar,
            SegundosParaRecolocar = segundosParaRecolocar,
            RadioLiberada = radioLiberada,
        };
        public float RadioLlegadaMiga => radioLlegadaMiga;
        public float SegundosVolcada => segundosVolcada;
        public float DesaceleracionPlaneada => desaceleracionPlaneada;

        /// <summary>T-986: los números del conductor IA de las patrullas.</summary>
        public ParametrosConductorIA ParametrosConductor => new ParametrosConductorIA
        {
            DistanciaMiraMinima = distanciaMiraMinima,
            SegundosMira = segundosMira,
            VelocidadCurva90 = velocidadCurva90,
            VelocidadMinimaCurva = velocidadMinimaCurva,
            FraccionAgarre = fraccionAgarreCurva,
            DesaceleracionPlaneada = desaceleracionPlaneada,
            RangoAceleracion = rangoAceleracion,
            RangoFreno = rangoFreno,
            ErrorFrenoFuerte = errorFrenoFuerte,
            ZonaMuerta = zonaMuertaVelocidad,
            RapidezQuieta = rapidezQuieta,
            SegundosParaAtoro = segundosParaAtoro,
            SegundosReversa = segundosReversa,
            VolanteReversaMinimo = volanteReversaMinimo,
            AnguloFrenoMano = anguloFrenoMano,
            VelocidadFrenoMano = velocidadFrenoMano,
        };

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

        [Header("Vuelo del dron (T-1008)")]
        [Min(0.5f)] [Tooltip("Aceleración horizontal máxima del dron al arrancar o girar (m/s2).")]
        [SerializeField] private float aceleracionDron = 7f;
        [Min(0.5f)] [Tooltip("Desaceleración con la que planea frenar al llegar; menor que la aceleración para frenar suave (m/s2).")]
        [SerializeField] private float frenadoDron = 4f;
        [Min(0.1f)] [Tooltip("Velocidad vertical máxima del dron al ajustar su altura (m/s).")]
        [SerializeField] private float velocidadVerticalDron = 4f;
        [Min(0.1f)] [Tooltip("Aceleración vertical máxima del dron (m/s2).")]
        [SerializeField] private float aceleracionVerticalDron = 4f;
        [Min(10f)] [Tooltip("Qué tan rápido puede girar el rumbo del dron (grados/s).")]
        [SerializeField] private float giroRumboDron = 120f;
        [Range(0f, 45f)] [Tooltip("Inclinación máxima del dron al acelerar o girar (grados).")]
        [SerializeField] private float inclinacionMaximaDron = 20f;
        [Min(0f)] [Tooltip("Grados que se inclina el dron por cada m/s2 de aceleración (grados).")]
        [SerializeField] private float inclinacionPorAceleracionDron = 2.5f;
        [Min(0.5f)] [Tooltip("Qué tan rápido alcanza la inclinación pedida (1/s).")]
        [SerializeField] private float suavizadoInclinacionDron = 6f;
        [Min(0f)] [Tooltip("Cuánto sube y baja el dron flotando en el aire (m).")]
        [SerializeField] private float amplitudFloteDron = 0.12f;
        [Min(0f)] [Tooltip("Cuántas veces por segundo sube y baja al flotar (Hz).")]
        [SerializeField] private float frecuenciaFloteDron = 0.5f;
        [Min(0f)] [Tooltip("Balanceo leve del dron de lado a lado (grados).")]
        [SerializeField] private float amplitudBalanceoDron = 1.5f;
        [Min(0f)] [Tooltip("Cuántas veces por segundo se balancea el dron (Hz).")]
        [SerializeField] private float frecuenciaBalanceoDron = 0.35f;

        /// <summary>T-1008: los números del vuelo natural de los drones (la velocidad máxima es la velocidad del dron).</summary>
        public ParametrosVueloDron ParametrosVuelo => new ParametrosVueloDron
        {
            VelocidadMaxima = velocidadDron,
            AceleracionMaxima = aceleracionDron,
            AceleracionFrenado = frenadoDron,
            VelocidadVertical = velocidadVerticalDron,
            AceleracionVertical = aceleracionVerticalDron,
            GradosRumboPorSegundo = giroRumboDron,
            InclinacionMaxima = inclinacionMaximaDron,
            GradosPorAceleracion = inclinacionPorAceleracionDron,
            SuavizadoInclinacion = suavizadoInclinacionDron,
            AmplitudFlote = amplitudFloteDron,
            FrecuenciaFlote = frecuenciaFloteDron,
            AmplitudBalanceo = amplitudBalanceoDron,
            FrecuenciaBalanceo = frecuenciaBalanceoDron,
        };

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

        [Header("Cinco estrellas y radio (T-1322)")]
        [Min(0)] [Tooltip("Patrullas que se suman a las del nivel cuando tienes 5 estrellas (salen las camionetas si hay prefab).")]
        [SerializeField] private int patrullasExtraCincoEstrellas = 3;
        [Min(0)] [Tooltip("Drones que se suman a los del nivel cuando tienes 5 estrellas.")]
        [SerializeField] private int dronesExtraCincoEstrellas = 3;
        [Tooltip("ID de sonido de la radio de la policía. T-1476: POL_RADIO_AVISO. Vacío: no suena nada (el ID debe existir en CatalogoSonidos y en ReglaMezclaSonidos).")]
        [SerializeField] private string idSonidoRadio = "POL_RADIO_AVISO";
        [Tooltip("T-1476: ID de sonido de la radio cuando la central manda unidades (sube el nivel de búsqueda). Vacío: no suena.")]
        [SerializeField] private string idSonidoDespacho = "POL_RADIO_AVISO"; // T-1531: la policía solo usa las voces "Mexican Police"; POL_RADIO_DESPACHO sigue como alias
        public int PatrullasExtraCincoEstrellas => patrullasExtraCincoEstrellas;
        public int DronesExtraCincoEstrellas => dronesExtraCincoEstrellas;
        public string IdSonidoRadio => idSonidoRadio;
        public string IdSonidoDespacho => idSonidoDespacho;

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
