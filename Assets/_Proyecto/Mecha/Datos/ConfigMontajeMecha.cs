using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Valores de sensación al subir y bajar del mecha.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config montaje mecha", fileName = "ConfigMontajeMecha")]
    public class ConfigMontajeMecha : ScriptableObject
    {
        [Tooltip("Velocidad horizontal por debajo de la cual se puede bajar (m/s).")]
        [Min(0f)] [SerializeField] private float velocidadMaximaParaBajar = 0.5f;
        [Tooltip("Distancia desde la raíz del mecha a la que se busca la pose de salida (m).")]
        [Min(0.5f)] [SerializeField] private float distanciaSalida = 2f;
        [Tooltip("Altura de la cápsula de una persona al comprobar que la salida está libre (m).")]
        [Min(0.5f)] [SerializeField] private float alturaPersona = 1.8f;
        [Tooltip("Radio de la cápsula de una persona al comprobar que la salida está libre (m).")]
        [Min(0.05f)] [SerializeField] private float radioPersona = 0.35f;
        [Tooltip("Separación entre el piso y la cápsula al comprobar la salida, y entre el piso y la pose entregada (m).")]
        [Min(0f)] [SerializeField] private float separacionPiso = 0.05f;
        [Tooltip("Altura sobre la pose candidata desde la que se busca el piso (m).")]
        [Min(0.1f)] [SerializeField] private float alturaBusquedaPiso = 2f;
        [Tooltip("Distancia hacia abajo que se busca el piso desde esa altura (m).")]
        [Min(0.1f)] [SerializeField] private float alcanceBusquedaPiso = 5f;

        public float VelocidadMaximaParaBajar => velocidadMaximaParaBajar;
        public float DistanciaSalida => distanciaSalida;
        public float AlturaPersona => alturaPersona;
        public float RadioPersona => radioPersona;
        public float SeparacionPiso => separacionPiso;
        public float AlturaBusquedaPiso => alturaBusquedaPiso;
        public float AlcanceBusquedaPiso => alcanceBusquedaPiso;
    }
}
