using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// Lo propio de una moto (T-592) sobre la física de <see cref="CarroViejo"/> (que ya usa el ConfigCarro_Moto: ligera, ágil, frena con inercia):
    /// - El visual se inclina hacia la curva (la física no se vuelca: sigue siendo la del carro con cuatro apoyos y torque antivuelco).
    /// - El caballete viene pegado en el modelo: mientras alguien maneja (o va de tráfico, o se mueve) se cambia la malla por una copia sin
    ///   caballete y el visual baja lo que ese pie la mantenía en el aire. Parada y vacía vuelve la malla completa.
    /// La pose del conductor y subir/bajar son de Personaje/Visual (PoseMoto, PuntosMoto); este componente no los toca.
    /// Lo agrega CreadorMotosT592 al prefab de cada moto.
    /// </summary>
    [RequireComponent(typeof(CarroViejo))]
    public class ManejoMoto : MonoBehaviour
    {
        private const float GravedadTierra = 9.81f;
        private const float RapidezEnMarcha = 1f;

        [Header("Inclinación")]
        [Tooltip("Envoltorio del modelo que se inclina (gira sobre el piso, en el origen de la moto).")]
        [SerializeField] private Transform envoltorioVisual;
        [Tooltip("Qué fracción de la inclinación de una moto real se muestra (0 a 1).")]
        [Range(0f, 1.5f)] [SerializeField] private float factorInclinacion = 1f;
        [Tooltip("Inclinación máxima a cada lado (°).")]
        [Range(0f, 60f)] [SerializeField] private float maximoGrados = 32f;
        [Tooltip("Qué tan rápido sigue la inclinación a la curva (1/s).")]
        [Min(0.1f)] [SerializeField] private float respuestaInclinacion = 7f;

        [Header("Caballete")]
        [Tooltip("MeshFilter del modelo de la moto.")]
        [SerializeField] private MeshFilter filtroModelo;
        [Tooltip("Malla original, con caballete.")]
        [SerializeField] private Mesh mallaCompleta;
        [Tooltip("Copia de la malla sin el caballete.")]
        [SerializeField] private Mesh mallaSinCaballete;
        [Tooltip("Cuánto baja el visual al quitar el caballete, para que las llantas toquen el piso (m).")]
        [Min(0f)] [SerializeField] private float bajarAlOcultar;

        private CarroViejo carro;
        private Rigidbody cuerpo;
        private Vector3 posicionVisualBase;
        private Quaternion rotacionVisualBase;
        private float inclinacion;

        /// <summary>Inclinación actual del visual (°, positiva a la derecha).</summary>
        public float Inclinacion => inclinacion;

        /// <summary>True si el modelo se ve sin caballete.</summary>
        public bool CaballeteOculto { get; private set; }

        public Transform EnvoltorioVisual => envoltorioVisual;
        public MeshFilter FiltroModelo => filtroModelo;

        private void Awake()
        {
            carro = GetComponent<CarroViejo>();
            cuerpo = GetComponent<Rigidbody>();
            if (envoltorioVisual == null || filtroModelo == null || mallaCompleta == null)
            {
                Debug.LogError($"[Vehiculos] {name}: ManejoMoto sin envoltorio visual, filtro de malla o malla completa.", this);
                enabled = false;
                return;
            }
            posicionVisualBase = envoltorioVisual.localPosition;
            rotacionVisualBase = envoltorioVisual.localRotation;
            PonerCaballeteOculto(false);
        }

        private void Update()
        {
            bool enUso = carro.Ocupado || carro.EstaEnTrafico || carro.Rapidez > RapidezEnMarcha;
            if (enUso != CaballeteOculto)
            {
                PonerCaballeteOculto(enUso);
            }
            float objetivo = 0f;
            if (!carro.EstaEnTrafico && !carro.EstaDormido)
            {
                objetivo = CalculoInclinacionMoto.Grados(carro.Velocidad, cuerpo.angularVelocity.y, GravedadTierra, factorInclinacion, maximoGrados);
            }
            inclinacion = CalculoInclinacionMoto.Suavizar(inclinacion, objetivo, respuestaInclinacion, Time.deltaTime);
            // Inclinarse a la derecha es girar sobre el eje z en sentido negativo.
            AplicarPose();
        }

        private void PonerCaballeteOculto(bool oculto)
        {
            CaballeteOculto = oculto && mallaSinCaballete != null;
            filtroModelo.sharedMesh = CaballeteOculto ? mallaSinCaballete : mallaCompleta;
            AplicarPose();
        }

        // La moto se inclina girando sobre su punto en el piso (el origen), no sobre el centro del modelo.
        private void AplicarPose()
        {
            Quaternion giro = Quaternion.Euler(0f, 0f, -inclinacion);
            Vector3 basePos = posicionVisualBase + (CaballeteOculto ? Vector3.down * bajarAlOcultar : Vector3.zero);
            envoltorioVisual.localPosition = giro * basePos;
            envoltorioVisual.localRotation = giro * rotacionVisualBase;
        }
    }
}
