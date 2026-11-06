using UnityEngine;

namespace MechaCyberpunk.Progresion.Apoyo
{
    /// <summary>Textos, costo, tiempos y presentación de la silla del Doc (T-813): sentarse, brazo robótico, luz, barra de vida y levantarse.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config silla de la clínica", fileName = "ConfigSillaClinica_Nueva")]
    public class ConfigSillaClinica : ScriptableObject
    {
        [Header("Cobro")]
        [Tooltip("Lo que cuesta curarte en la silla (Ƀ). El mismo que EL DOC.")]
        [Min(0)] [SerializeField] private int costo = 50;

        [Header("Textos")]
        [Tooltip("Lo que dice EL DOC cuando le hablas herido.")]
        [SerializeField] private string fraseDoc = "Siéntate en la silla y te curo.";
        [Tooltip("Lo que dice EL DOC al terminar.")]
        [SerializeField] private string fraseFin = "Listo, como nuevo. Ten más cuidado, mijo.";
        [Tooltip("{0} = costo con Ƀ. El HUD le antepone \"E · \".")]
        [SerializeField] private string formatoAviso = "SENTARTE · CURARTE {0}";
        [SerializeField] private string textoSano = "SILLA DEL DOC · ESTÁS SANO";
        [SerializeField] private string textoSinDinero = "SILLA DEL DOC · NO TE ALCANZA";
        [SerializeField] private string textoBarra = "CURANDO";
        [SerializeField] private string nombreDoc = "EL DOC · JOEL";

        [Header("Tiempos (s)")]
        [Min(0f)] [SerializeField] private float segundosSentarse = 0.5f;
        [Min(0f)] [SerializeField] private float segundosBajarBrazo = 0.8f;
        [Min(0.1f)] [SerializeField] private float segundosCurar = 2f;
        [Min(0f)] [SerializeField] private float segundosSubirBrazo = 0.6f;
        [Min(0f)] [SerializeField] private float segundosLevantarse = 0.3f;
        [Tooltip("Lo que dura el subtítulo del Doc (s).")]
        [Min(0f)] [SerializeField] private float segundosSubtitulo = 3f;

        [Header("Escena")]
        [Tooltip("Cuánto baja el brazo robótico sobre la silla (m).")]
        [Min(0f)] [SerializeField] private float bajadaBrazo = 0.35f;
        [Tooltip("Dónde va la cámara cercana respecto al asiento: x a un lado, y arriba, z al frente (m).")]
        [SerializeField] private Vector3 posicionCamara = new Vector3(1.1f, 1.25f, 1.9f);
        [Tooltip("A qué altura sobre el asiento mira la cámara cercana (m).")]
        [SerializeField] private float alturaMira = 0.35f;
        [Range(20f, 80f)] [SerializeField] private float campoVision = 50f;
        [SerializeField] private Color colorLuz = new Color(0.3f, 1f, 0.75f, 1f);
        [Tooltip("Intensidad media de la luz del brazo.")]
        [Min(0f)] [SerializeField] private float intensidadLuz = 3f;
        [Tooltip("Alcance de la luz del brazo (m).")]
        [Min(0.1f)] [SerializeField] private float alcanceLuz = 3f;
        [Tooltip("Pulsos de la luz por segundo (Hz).")]
        [Min(0f)] [SerializeField] private float pulsoLuz = 2.5f;
        [Tooltip("Altura de la cadera sobre el asiento en la pose (m).")]
        [SerializeField] private float alturaCadera = 0.05f;
        [Tooltip("Qué tan recostada va la espalda (0 = derecha, 1 = acostada).")]
        [Range(0f, 1f)] [SerializeField] private float recostado = 0.35f;
        [SerializeField] private Color colorBarra = new Color(0.35f, 1f, 0.55f, 1f);

        public int Costo => costo;
        public string FraseDoc => fraseDoc;
        public string FraseFin => fraseFin;
        public string FormatoAviso => formatoAviso;
        public string TextoSano => textoSano;
        public string TextoSinDinero => textoSinDinero;
        public string TextoBarra => textoBarra;
        public string NombreDoc => nombreDoc;
        public float SegundosSentarse => segundosSentarse;
        public float SegundosBajarBrazo => segundosBajarBrazo;
        public float SegundosCurar => segundosCurar;
        public float SegundosSubirBrazo => segundosSubirBrazo;
        public float SegundosLevantarse => segundosLevantarse;
        public float SegundosSubtitulo => segundosSubtitulo;
        public float BajadaBrazo => bajadaBrazo;
        public Vector3 PosicionCamara => posicionCamara;
        public float AlturaMira => alturaMira;
        public float CampoVision => campoVision;
        public Color ColorLuz => colorLuz;
        public float IntensidadLuz => intensidadLuz;
        public float AlcanceLuz => alcanceLuz;
        public float PulsoLuz => pulsoLuz;
        public float AlturaCadera => alturaCadera;
        public float Recostado => recostado;
        public Color ColorBarra => colorBarra;

        /// <summary>Lo que dura toda la secuencia (s).</summary>
        public float Duracion => segundosSentarse + segundosBajarBrazo + segundosCurar + segundosSubirBrazo + segundosLevantarse;
    }
}
