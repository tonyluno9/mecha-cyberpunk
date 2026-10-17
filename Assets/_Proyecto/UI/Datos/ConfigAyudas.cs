using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>
    /// Textos y tiempos de las ayudas de controles (T-134): una línea por contexto, cuánto dura como máximo y qué tiene que hacer
    /// la persona para que se vaya antes. Los textos son dato: el del carro lo ajusta T-136 si el carro usa otras teclas.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config ayudas de controles", fileName = "ConfigAyudas_Nueva")]
    public class ConfigAyudas : ScriptableObject
    {
        [Header("Textos")]
        [SerializeField] private string textoAPie = "WASD caminar · SHIFT correr · ESPACIO saltar · E usar";
        [SerializeField] private string textoCarro = "FLECHAS manejar · E bajar (detenido)";
        [SerializeField] private string textoMechaPelea = "CLIC IZQ golpe · CLIC DER bloquear · justo antes del golpe = bloqueo perfecto · WASD mover";
        [SerializeField] private string textoMechaCalle = "WASD mover · RATÓN mirar · E bajar (quieto)";
        [SerializeField] private string textoLlamar = "M · llamar a tu mecha";

        [Header("Tiempos")]
        [Tooltip("Tiempo máximo que se ve una ayuda a pie, en carro o del mecha en la calle (s).")]
        [Min(1f)] [SerializeField] private float segundosMaximos = 12f;
        [Tooltip("Tiempo máximo que se ve la ayuda de la primera pelea en el mecha (s).")]
        [Min(1f)] [SerializeField] private float segundosMaximosPelea = 20f;
        [Tooltip("Segundos caminando que hacen que la ayuda a pie se vaya (s).")]
        [Min(0.1f)] [SerializeField] private float segundosCaminando = 3f;
        [Tooltip("Segundos manejando que hacen que la ayuda del carro se vaya (s).")]
        [Min(0.1f)] [SerializeField] private float segundosManejando = 3f;
        [Tooltip("Rapidez horizontal desde la que se cuenta como caminar o manejar (m/s).")]
        [Min(0.01f)] [SerializeField] private float rapidezMinima = 0.5f;
        [Tooltip("Rapidez por encima de la cual el movimiento no se cuenta, porque es un salto de posición (m/s).")]
        [Min(1f)] [SerializeField] private float rapidezMaxima = 50f;

        [Header("Primera pelea en el mecha")]
        [Tooltip("Golpes que hacen que la ayuda de pelea se vaya (junto con las subidas de guardia).")]
        [Min(0)] [SerializeField] private int golpesNecesarios = 3;
        [Tooltip("Veces que hay que subir la guardia para que la ayuda de pelea se vaya (junto con los golpes).")]
        [Min(0)] [SerializeField] private int guardiasNecesarias = 1;

        [Header("Llamar al mecha")]
        [Tooltip("Si el texto del objetivo contiene esta palabra (sin importar mayúsculas), se ofrece la ayuda de llamar al mecha.")]
        [SerializeField] private string palabraObjetivoLlamar = "llam";

        [Header("Pantalla de la cabina")]
        [SerializeField] private Color colorTextoCabina = new Color(0f, 1f, 0.9f, 1f);
        [SerializeField] private Color colorFondoCabina = new Color(0.02f, 0.04f, 0.06f, 0.92f);
        [Tooltip("Tamaño de la letra de la pantalla de la cabina (unidades del canvas).")]
        [Min(8)] [SerializeField] private int tamanoLetraCabina = 20;

        public float SegundosMaximos => segundosMaximos;
        public float SegundosMaximosPelea => segundosMaximosPelea;
        public float SegundosCaminando => segundosCaminando;
        public float SegundosManejando => segundosManejando;
        public float RapidezMinima => rapidezMinima;
        public float RapidezMaxima => rapidezMaxima;
        public int GolpesNecesarios => golpesNecesarios;
        public int GuardiasNecesarias => guardiasNecesarias;
        public string PalabraObjetivoLlamar => palabraObjetivoLlamar;
        public Color ColorTextoCabina => colorTextoCabina;
        public Color ColorFondoCabina => colorFondoCabina;
        public int TamanoLetraCabina => tamanoLetraCabina;

        /// <summary>La línea de ayuda de ese contexto.</summary>
        public string Texto(ContextoAyuda contexto)
        {
            switch (contexto)
            {
                case ContextoAyuda.Carro: return textoCarro;
                case ContextoAyuda.MechaPelea: return textoMechaPelea;
                case ContextoAyuda.MechaCalle: return textoMechaCalle;
                case ContextoAyuda.Llamar: return textoLlamar;
                default: return textoAPie;
            }
        }
    }
}
