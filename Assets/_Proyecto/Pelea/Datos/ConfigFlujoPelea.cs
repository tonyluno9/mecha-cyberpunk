using UnityEngine;

namespace MechaCyberpunk.Pelea.Datos
{
    /// <summary>Ritmo, textos y colores del flujo de pelea y de su pantalla de la arena.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Pelea/Config flujo de pelea", fileName = "ConfigFlujoPelea")]
    public class ConfigFlujoPelea : ScriptableObject
    {
        [Header("Ritmo")]
        [Tooltip("Duración de cada número del conteo (s).")]
        [Min(0.01f)] [SerializeField] private float segundosPorNumero = 1f;
        [Tooltip("Pausa entre el KO y la pantalla de resultado (s).")]
        [Min(0f)] [SerializeField] private float pausaDespuesKO = 2f;
        [Tooltip("Cuánto se queda en pantalla el texto de pelea después del conteo (s).")]
        [Min(0f)] [SerializeField] private float segundosTextoPelea = 1f;

        [Header("Textos")]
        [SerializeField] private string textoPelea = "¡PELEA!";
        [SerializeField] private string textoKO = "KO";
        [SerializeField] private string textoGanaste = "GANASTE";
        [SerializeField] private string textoPerdiste = "PERDISTE";
        [SerializeField] private string textoRevancha = "R: revancha";
        [Tooltip("Letrero de la pelea de jefe, de la presentación a '¡PELEA!' (T-161).")]
        [SerializeField] private string textoJefe = "PELEA DE JEFE";
        [Tooltip("Letrero al ganar una pelea de jefe (T-161).")]
        [SerializeField] private string textoVencisteCampeon = "¡VENCISTE AL CAMPEÓN!";

        [Header("Pantalla")]
        [Tooltip("Tamaño de la letra del texto grande (puntos).")]
        [Min(1)] [SerializeField] private int tamanoLetra = 280;
        [Tooltip("Tamaño de la letra de la línea de revancha (puntos).")]
        [Min(1)] [SerializeField] private int tamanoLetraRevancha = 80;
        [Tooltip("Tamaño de la letra de la línea del premio (puntos).")]
        [Min(1)] [SerializeField] private int tamanoLetraPremio = 160;
        [Tooltip("Tamaño de la letra del letrero de jefe (puntos).")]
        [Min(1)] [SerializeField] private int tamanoLetraJefe = 130;
        [Header("Pantalla de respaldo (si la arena no da una válida)")]
        [Tooltip("Ancho (x) y alto (y) de la zona útil de respaldo (m).")]
        [SerializeField] private Vector2 tamanoPantallaRespaldo = new Vector2(8f, 4.5f);
        [Tooltip("Cuánto detrás del rival se pone la pantalla de respaldo (m).")]
        [Min(0f)] [SerializeField] private float distanciaPantallaRespaldo = 2f;
        [Tooltip("Altura sobre el piso de la pantalla de respaldo (m).")]
        [Min(0f)] [SerializeField] private float alturaPantallaRespaldo = 6f;
        [SerializeField] private Color colorConteo = Color.white;
        [SerializeField] private Color colorPelea = new Color(1f, 0.55f, 0.1f);
        [SerializeField] private Color colorKO = new Color(1f, 0.15f, 0.15f);
        [SerializeField] private Color colorGanaste = new Color(0.2f, 1f, 0.4f);
        [SerializeField] private Color colorPerdiste = new Color(1f, 0.15f, 0.15f);
        [SerializeField] private Color colorPremio = new Color(1f, 0.85f, 0.2f);
        [Tooltip("Color del letrero de la pelea de jefe.")]
        [SerializeField] private Color colorJefe = new Color(1f, 0.55f, 0.1f);

        public float SegundosPorNumero => segundosPorNumero;
        public float PausaDespuesKO => pausaDespuesKO;
        public float SegundosTextoPelea => segundosTextoPelea;
        public string TextoPelea => textoPelea;
        public string TextoKO => textoKO;
        public string TextoGanaste => textoGanaste;
        public string TextoPerdiste => textoPerdiste;
        public string TextoRevancha => textoRevancha;
        public string TextoJefe => textoJefe;
        public string TextoVencisteCampeon => textoVencisteCampeon;
        public int TamanoLetraJefe => tamanoLetraJefe;
        public Color ColorJefe => colorJefe;
        public int TamanoLetra => tamanoLetra;
        public int TamanoLetraRevancha => tamanoLetraRevancha;
        public int TamanoLetraPremio => tamanoLetraPremio;
        public Vector2 TamanoPantallaRespaldo => tamanoPantallaRespaldo;
        public float DistanciaPantallaRespaldo => distanciaPantallaRespaldo;
        public float AlturaPantallaRespaldo => alturaPantallaRespaldo;
        public Color ColorConteo => colorConteo;
        public Color ColorPelea => colorPelea;
        public Color ColorKO => colorKO;
        public Color ColorGanaste => colorGanaste;
        public Color ColorPerdiste => colorPerdiste;
        public Color ColorPremio => colorPremio;
    }
}
