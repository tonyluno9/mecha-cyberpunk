using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Cámara de hombro del jugador a pie (T-400, Cinemachine desde T-723): posición normal y apuntando, FOV, inclinación y suavizado.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config cámara de hombro", fileName = "ConfigCamaraHombro_Nuevo")]
    public class ConfigCamaraHombro : ScriptableObject
    {
        [Header("Normal")]
        [Tooltip("Desplazamiento a la derecha del hombro (m).")]
        [SerializeField] private float lateralNormal = 0.55f;
        [Tooltip("Altura del punto de giro (el cuello, a donde mira la cámara) sobre los pies (m).")]
        [Min(0.1f)] [SerializeField] private float alturaNormal = 1.55f;
        [Tooltip("Distancia hacia atrás (m).")]
        [Min(0.1f)] [SerializeField] private float distanciaNormal = 3.2f;
        [Tooltip("Campo de visión (°).")]
        [Range(20f, 120f)] [SerializeField] private float fovNormal = 60f;

        [Tooltip("T-723: cuánto sube la cámara por encima del punto de giro, sobre el hombro (m).")]
        [Min(0f)] [SerializeField] private float brazoVertical = 0.3f;

        [Tooltip("T-723: cuánto se acerca la cámara (fracción de la distancia) al mirar 30° hacia abajo o más; cerca, como CAM02_APieCerca.png.")]
        [Range(0f, 0.6f)] [SerializeField] private float acercarMirandoAbajo = 0.4f;

        [Header("Apuntando")]
        [SerializeField] private float lateralApuntando = 0.7f;
        [Min(0.1f)] [SerializeField] private float alturaApuntando = 1.5f;
        [Min(0.1f)] [SerializeField] private float distanciaApuntando = 1.6f;
        [Range(20f, 120f)] [SerializeField] private float fovApuntando = 48f;

        [Header("Cambio de hombro (T-1001)")]
        [Tooltip("Rapidez con la que la cámara pasa de un hombro al otro (1/s).")]
        [Min(0.1f)] [SerializeField] private float rapidezCambioHombro = 6f;
        [Tooltip("Margen libre que pide el hombro derecho, además del desplazamiento lateral, para quedarse en él (m).")]
        [Min(0f)] [SerializeField] private float margenHombro = 0.25f;

        [Header("Zoom del sniper (T-416)")]
        [Tooltip("Campo de visión con el zoom del sniper (°).")]
        [Range(5f, 60f)] [SerializeField] private float fovSniper = 15f;
        [Tooltip("Cuánto tarda el zoom en entrar o salir (s).")]
        [Min(0.01f)] [SerializeField] private float duracionZoom = 0.15f;
        [Tooltip("Factor de la sensibilidad del ratón mientras dura el zoom.")]
        [Range(0.05f, 1f)] [SerializeField] private float sensibilidadZoom = 0.35f;
        [Tooltip("Sacudida hacia arriba de cada tiro con zoom (°).")]
        [Min(0f)] [SerializeField] private float sacudidaZoom = 0.3f;
        [Tooltip("Cuánto tarda la sacudida en volver (s).")]
        [Min(0.01f)] [SerializeField] private float duracionSacudida = 0.3f;

        [Header("Mirada y choque")]
        [Tooltip("Inclinación mínima (°, negativo mira hacia arriba).")]
        [Range(-89f, 0f)] [SerializeField] private float inclinacionMinima = -40f;
        [Tooltip("Inclinación máxima (°, positivo mira hacia abajo).")]
        [Range(0f, 89f)] [SerializeField] private float inclinacionMaxima = 60f;
        [Tooltip("Rapidez del suavizado de la cámara (1/s).")]
        [Min(0.1f)] [SerializeField] private float suavizado = 12f;
        [Tooltip("Radio de la esfera que evita que la cámara atraviese muros (m).")]
        [Min(0.01f)] [SerializeField] private float radioColision = 0.2f;
        [Tooltip("Giro del cuerpo hacia donde camina (°/s).")]
        [Min(1f)] [SerializeField] private float giroCuerpo = 720f;
        [Tooltip("Segundos que el cuerpo mira hacia donde dispara sin estar apuntando (s).")]
        [Min(0f)] [SerializeField] private float segundosGiroAlDisparar = 0.6f;

        [Header("Pulido (T-1485)")]
        [Tooltip("FOV extra al correr (°). Se suma al FOV normal; apuntando no se aplica.")]
        [Range(0f, 15f)] [SerializeField] private float fovExtraCorrer = 7f;
        [Tooltip("Rapidez del jugador (m/s) a la que empieza a abrirse el FOV.")]
        [Min(0f)] [SerializeField] private float velocidadInicioFovCorrer = 5f;
        [Tooltip("Rapidez del jugador (m/s) a la que el FOV extra está completo (la de correr).")]
        [Min(0.1f)] [SerializeField] private float velocidadFinFovCorrer = 7.5f;
        [Tooltip("Rapidez del suavizado del FOV al correr o dejar de correr (1/s). Menor = más lento.")]
        [Min(0.1f)] [SerializeField] private float suavizadoFov = 5f;
        [Tooltip("Rapidez del suavizado al entrar o salir de apuntar (1/s): mueve hombro, distancia y FOV. Menor = más lento.")]
        [Min(0.1f)] [SerializeField] private float suavizadoApuntar = 10f;
        [Tooltip("Balanceo: subida y bajada máxima de la cámara al correr (m).")]
        [Min(0f)] [SerializeField] private float balanceoVertical = 0.0175f;
        [Tooltip("Balanceo: vaivén lateral máximo al correr (m).")]
        [Min(0f)] [SerializeField] private float balanceoLateral = 0.01f;
        [Tooltip("Balanceo: pasos completos (dos pasos) por metro recorrido (ciclos/m).")]
        [Min(0.01f)] [SerializeField] private float balanceoCiclosPorMetro = 0.25f;
        [Tooltip("Balanceo: rapidez de la velocidad (m/s) con la que alcanza su amplitud completa (la de correr).")]
        [Min(0.1f)] [SerializeField] private float balanceoVelocidadReferencia = 7.5f;
        [Tooltip("Balanceo: rapidez con la que aparece o desaparece (1/s). Quieto o apuntando baja a 0.")]
        [Min(0.1f)] [SerializeField] private float balanceoSuavizado = 6f;
        [Tooltip("Colisión: rapidez con la que la cámara recupera su distancia tras quedar pegada a un muro (s de amortiguado). Mayor = más lenta.")]
        [Min(0f)] [SerializeField] private float recuperacionColision = 0.35f;
        [Tooltip("Temblor (impulsos de Cinemachine): ganancia del listener de la cámara. Multiplica la fuerza de TemblorCamara.Generar. T-1490: 0.18 (playtest 19-nov, casi sin temblor).")]
        [Min(0f)] [SerializeField] private float gananciaTemblor = 0.18f;

        public float FovExtraCorrer => fovExtraCorrer;
        public float VelocidadInicioFovCorrer => velocidadInicioFovCorrer;
        public float VelocidadFinFovCorrer => velocidadFinFovCorrer;
        public float SuavizadoFov => suavizadoFov;
        public float SuavizadoApuntar => suavizadoApuntar;
        public float BalanceoVertical => balanceoVertical;
        public float BalanceoLateral => balanceoLateral;
        public float BalanceoCiclosPorMetro => balanceoCiclosPorMetro;
        public float BalanceoVelocidadReferencia => balanceoVelocidadReferencia;
        public float BalanceoSuavizado => balanceoSuavizado;
        public float RecuperacionColision => recuperacionColision;
        public float GananciaTemblor => gananciaTemblor;
        public float LateralNormal => lateralNormal;
        public float AlturaNormal => alturaNormal;
        public float DistanciaNormal => distanciaNormal;
        public float BrazoVertical => brazoVertical;
        public float AcercarMirandoAbajo => acercarMirandoAbajo;
        public float FovNormal => fovNormal;
        public float LateralApuntando => lateralApuntando;
        public float AlturaApuntando => alturaApuntando;
        public float DistanciaApuntando => distanciaApuntando;
        public float FovApuntando => fovApuntando;
        public float RapidezCambioHombro => rapidezCambioHombro;
        public float MargenHombro => margenHombro;
        public float FovSniper => fovSniper;
        public float DuracionZoom => duracionZoom;
        public float SensibilidadZoom => sensibilidadZoom;
        public float SacudidaZoom => sacudidaZoom;
        public float DuracionSacudida => duracionSacudida;
        public float InclinacionMinima => inclinacionMinima;
        public float InclinacionMaxima => inclinacionMaxima;
        public float Suavizado => suavizado;
        public float RadioColision => radioColision;
        public float GiroCuerpo => giroCuerpo;
        public float SegundosGiroAlDisparar => segundosGiroAlDisparar;
    }
}
