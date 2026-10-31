using UnityEngine;

namespace MechaCyberpunk.IA.Datos
{
    /// <summary>
    /// Valores del pandillero a pie y de su pandilla (T-195, D-tony-169): vida, golpe, tiempos, distancias, dinero, tamaño del grupo y
    /// colores del cuerpo y del aviso. Asset: DatosPandilleroAPie_Base.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos del pandillero a pie", fileName = "DatosPandilleroAPie_Nuevo")]
    public class DatosPandilleroAPie : ScriptableObject
    {
        [Header("Pandillero")]
        [Tooltip("Nombre que leen los OJOS.")]
        [SerializeField] private string nombre = "PANDILLERO";
        [Tooltip("Vida máxima (puntos).")]
        [Min(1f)] [SerializeField] private float vidaMaxima = 60f;
        [Tooltip("Daño de su golpe (puntos).")]
        [Min(0f)] [SerializeField] private float danioGolpe = 10f;
        [Tooltip("Aviso del golpe: del inicio al impacto, con el brazo en naranja (s).")]
        [Min(0.05f)] [SerializeField] private float preparacionGolpe = 0.5f;
        [Tooltip("Espera después del impacto antes del siguiente golpe (s).")]
        [Min(0.05f)] [SerializeField] private float recargaGolpe = 1.4f;
        [Tooltip("Velocidad al perseguir y al volver (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidad = 5.5f;
        [Tooltip("Velocidad de giro hacia el jugador (°/s).")]
        [Min(1f)] [SerializeField] private float velocidadGiro = 540f;
        [Tooltip("Distancia a la que detecta al jugador y lo persigue (m).")]
        [Min(0.1f)] [SerializeField] private float radioDeteccion = 10f;
        [Tooltip("Si el jugador se aleja de su punto más que esto, se rinde y vuelve (m).")]
        [Min(0.1f)] [SerializeField] private float correa = 25f;
        [Tooltip("Distancia a la que empieza y conecta su golpe (m, en el suelo).")]
        [Min(0.1f)] [SerializeField] private float alcanceGolpe = 1.8f;
        [Tooltip("Cuánto más lejos puede estar el jugador en el impacto y aún recibir el golpe (m).")]
        [Min(0f)] [SerializeField] private float margenImpacto = 0.4f;
        [Tooltip("Mitad del ángulo frente a él en que conecta el golpe (°).")]
        [Range(1f, 180f)] [SerializeField] private float anguloGolpe = 70f;
        [Tooltip("Al volver a su punto, distancia a la que se considera que llegó (m).")]
        [Min(0.05f)] [SerializeField] private float margenLlegada = 0.6f;
        [Tooltip("Dinero que suelta al caer (puntos de dinero).")]
        [Min(0)] [SerializeField] private int dinero = 40;
        [Tooltip("Segundos después del KO en que desaparece (s).")]
        [Min(0.1f)] [SerializeField] private float segundosParaDesaparecer = 4f;
        [Tooltip("Duración del parpadeo rojo al recibir daño (s).")]
        [Min(0.01f)] [SerializeField] private float duracionParpadeo = 0.12f;
        [Tooltip("Cuánto sube el brazo en el aviso del golpe (°).")]
        [Range(0f, 180f)] [SerializeField] private float anguloBrazoAviso = 130f;

        [Header("Pandilla")]
        [Tooltip("Cuántos pandilleros trae el grupo (el prefab arma tantos).")]
        [Min(1)] [SerializeField] private int tamanoPandilla = 3;
        [Tooltip("Cuántos pueden estar atacando a la vez (avisando o recargando).")]
        [Min(1)] [SerializeField] private int maxAtacando = 2;
        [Tooltip("Distancia de cada pandillero al centro de su pandilla (m).")]
        [Min(0f)] [SerializeField] private float radioFormacion = 2.5f;

        [Header("Armado y territorio (T-402)")]
        [Tooltip("Si es true usa pistola en vez de puños: mantiene la distancia, avisa con un láser y dispara.")]
        [SerializeField] private bool usaPistola;
        [Tooltip("Perdigones por disparo (1 = pistola; el jefe lanza 8 como escopeta).")]
        [Min(1)] [SerializeField] private int perdigones = 1;
        [Tooltip("Daño de cada disparo que acierta (puntos de vida).")]
        [Min(0f)] [SerializeField] private float danioDisparo = 8f;
        [Tooltip("Distancia mínima a la que se queda del jugador; más cerca retrocede (m).")]
        [Min(0f)] [SerializeField] private float distanciaMinTiro = 6f;
        [Tooltip("Distancia máxima a la que dispara; más lejos se acerca (m).")]
        [Min(1f)] [SerializeField] private float distanciaMaxTiro = 12f;
        [Tooltip("Distancia a la que la probabilidad de acierto es la de 'acierto cerca' (m).")]
        [Min(0f)] [SerializeField] private float distanciaAciertoCerca = 10f;
        [Tooltip("Distancia a la que la probabilidad de acierto es la de 'acierto lejos' (m).")]
        [Min(1f)] [SerializeField] private float distanciaAciertoLejos = 30f;
        [Tooltip("Probabilidad de acertar a la distancia 'cerca' (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float aciertoCerca = 0.7f;
        [Tooltip("Probabilidad de acertar a la distancia 'lejos' (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float aciertoLejos = 0.3f;
        [Tooltip("Cada cuántos segundos cambia de lado al moverse de costado para cubrirse (s).")]
        [Min(0.2f)] [SerializeField] private float periodoCambioLado = 2f;
        [Tooltip("Calor policial que suma cada pandillero vencido (puntos de calor). 0 no suma.")]
        [Min(0f)] [SerializeField] private float calorAlVencer;
        [Tooltip("Radio del territorio: si el jugador entra a esta distancia del centro del grupo, atacan (m).")]
        [Min(1f)] [SerializeField] private float radioTerritorio = 35f;
        [Tooltip("Cuánto más allá del radio lo siguen antes de rendirse y volver (m).")]
        [Min(0f)] [SerializeField] private float margenPersecucion = 10f;
        [Tooltip("Segundos que tarda en reaparecer un grupo vencido (s).")]
        [Min(1f)] [SerializeField] private float segundosReaparicion = 40f;
        [Tooltip("El grupo solo reaparece si el jugador está más lejos que esto de su centro (m).")]
        [Min(0f)] [SerializeField] private float distanciaReaparicion = 30f;
        [Tooltip("Si es true el grupo está siempre, no solo de noche.")]
        [SerializeField] private bool siempre;

        [Header("Patrulla (T-446)")]
        [Tooltip("Hasta qué distancia de su punto se aleja al patrullar mientras espera (m). 0 = se queda quieto. Menor que la correa.")]
        [Min(0f)] [SerializeField] private float radioPatrulla = 6f;
        [Tooltip("Velocidad al patrullar, más lenta que al perseguir (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidadPatrulla = 1.6f;
        [Tooltip("Espera mínima al llegar a un punto de la patrulla (s).")]
        [Min(0f)] [SerializeField] private float esperaPatrullaMinima = 1.5f;
        [Tooltip("Espera máxima al llegar a un punto de la patrulla (s).")]
        [Min(0f)] [SerializeField] private float esperaPatrullaMaxima = 5f;
        [Tooltip("Si en este tiempo no se acerca a su punto de patrulla (una pared), elige otro (s).")]
        [Min(0.5f)] [SerializeField] private float segundosAtascoPatrulla = 2f;

        [Header("Táctica (T-423)")]
        [Tooltip("Debajo de esta fracción de vida huye (0 a 1); si lo acorralan pelea.")]
        [Range(0f, 1f)] [SerializeField] private float fraccionHuida = 0.25f;
        [Tooltip("Si el jugador se acerca a esta distancia mientras huye, se vuelve a pelear (m).")]
        [Min(0.5f)] [SerializeField] private float distanciaAcorralado = 3f;
        [Tooltip("Hasta dónde corre al huir; después se queda quieto (m).")]
        [Min(5f)] [SerializeField] private float distanciaHuida = 45f;
        [Tooltip("Radio en que busca cubierta entre ráfagas (m).")]
        [Min(1f)] [SerializeField] private float radioCubierta = 12f;
        [Tooltip("Segundos que se queda cubierto sin disparar (s).")]
        [Min(0.1f)] [SerializeField] private float segundosCubierto = 1.8f;
        [Tooltip("Segundos máximos asomado para disparar (s).")]
        [Min(0.1f)] [SerializeField] private float segundosAsomado = 2f;
        [Tooltip("Arco en que se reparten alrededor del jugador (grados).")]
        [Range(30f, 360f)] [SerializeField] private float arcoRodeo = 240f;
        [Tooltip("Cuánto giran los puestos alrededor del jugador (grados por segundo).")]
        [SerializeField] private float giroRodeo = 8f;
        [Tooltip("Radio en que avisa a los de su grupo al ver al jugador o recibir daño (m).")]
        [Min(0f)] [SerializeField] private float radioRefuerzos = 40f;
        [Tooltip("Segundos que los avisados siguen en alerta aunque el jugador esté fuera del territorio (s).")]
        [Min(0f)] [SerializeField] private float segundosRefuerzo = 8f;

        [Header("Colores")]
        [SerializeField] private Color colorNormal = new Color(0.55f, 0.15f, 0.65f);
        [SerializeField] private Color colorGolpe = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color colorKO = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color colorAviso = new Color(1f, 0.55f, 0.1f);

        public float FraccionHuida => fraccionHuida;
        public float DistanciaAcorralado => distanciaAcorralado;
        public float DistanciaHuida => distanciaHuida;
        public float RadioCubierta => radioCubierta;
        public float SegundosCubierto => segundosCubierto;
        public float SegundosAsomado => segundosAsomado;
        public float ArcoRodeo => arcoRodeo;
        public float GiroRodeo => giroRodeo;
        public float RadioRefuerzos => radioRefuerzos;
        public float SegundosRefuerzo => segundosRefuerzo;
        public bool UsaPistola => usaPistola;
        public int Perdigones => perdigones;
        public float DanioDisparo => danioDisparo;
        public float DistanciaMinTiro => distanciaMinTiro;
        public float DistanciaMaxTiro => distanciaMaxTiro;
        public float DistanciaAciertoCerca => distanciaAciertoCerca;
        public float DistanciaAciertoLejos => distanciaAciertoLejos;
        public float AciertoCerca => aciertoCerca;
        public float AciertoLejos => aciertoLejos;
        public float PeriodoCambioLado => periodoCambioLado;
        public float CalorAlVencer => calorAlVencer;
        public float RadioTerritorio => radioTerritorio;
        public float MargenPersecucion => margenPersecucion;
        public float SegundosReaparicion => segundosReaparicion;
        public float RadioPatrulla => radioPatrulla;
        public float VelocidadPatrulla => velocidadPatrulla;
        public float EsperaPatrullaMinima => esperaPatrullaMinima;
        public float EsperaPatrullaMaxima => esperaPatrullaMaxima;
        public float SegundosAtascoPatrulla => segundosAtascoPatrulla;
        public float DistanciaReaparicion => distanciaReaparicion;
        public bool Siempre => siempre;
        public string Nombre => nombre;
        public float VidaMaxima => vidaMaxima;
        public float DanioGolpe => danioGolpe;
        public float PreparacionGolpe => preparacionGolpe;
        public float RecargaGolpe => recargaGolpe;
        public float Velocidad => velocidad;
        public float VelocidadGiro => velocidadGiro;
        public float RadioDeteccion => radioDeteccion;
        public float Correa => correa;
        public float AlcanceGolpe => alcanceGolpe;
        public float MargenImpacto => margenImpacto;
        public float AnguloGolpe => anguloGolpe;
        public float MargenLlegada => margenLlegada;
        public int Dinero => dinero;
        public float SegundosParaDesaparecer => segundosParaDesaparecer;
        public float DuracionParpadeo => duracionParpadeo;
        public float AnguloBrazoAviso => anguloBrazoAviso;
        public int TamanoPandilla => tamanoPandilla;
        public int MaxAtacando => maxAtacando;
        public float RadioFormacion => radioFormacion;
        public Color ColorNormal => colorNormal;
        public Color ColorGolpe => colorGolpe;
        public Color ColorKO => colorKO;
        public Color ColorAviso => colorAviso;
    }
}
